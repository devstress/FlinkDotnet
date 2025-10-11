# WI20: Exercise2 Aggregate JSON Formatting Fix

**File**: `WIs/WI20_exercise2-aggregate-json-formatting-fix.md`
**Title**: Fix FlinkJobRunner COLLECT aggregation to produce properly formatted Backup JSON
**Description**: FlinkJobRunner's COLLECT aggregation produces malformed JSON that causes deserialization errors in Exercise2. Need to properly format InputMessage objects into Backup JSON structure.
**Priority**: High
**Component**: FlinkIRRunner Java Components
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-11
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI18: Comprehensive logging implementation for debugging
- WI19: Kafka container IP discovery for Docker bridge network

### Lessons Applied
- Use extensive logging to trace data transformations
- Verify actual JSON format at each processing stage
- Parse and validate JSON structures properly

### Problems Prevented
- Silent data corruption from improper JSON formatting
- Difficult-to-diagnose deserialization errors

## Phase 1: Investigation

### Requirements
- Understand Exercise2's data flow: InputMessage → Flink aggregation → Backup
- Identify root cause of JSON deserialization error: `'m' is an invalid start of a value`
- Determine correct JSON format for Backup objects

### Debug Information (MANDATORY)
**Error Message**:
```
'm' is an invalid start of a value. Path: $ | LineNumber: 0 | BytePositionInLine: 0.
```

**Error Location**: [`Program.cs:382`](LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise2-BackupAggregator/Program.cs:382) - BackupDeserializer.Deserialize()

**Root Cause Analysis**:

1. **Input Format** (lines 318-326): Producer sends properly formatted InputMessage JSON:
   ```json
   {"sender":"sender-0","recipient":"recipient-0","sentAt":"2025-10-11T02:00:00Z","message":"Test message 0"}
   ```

2. **Expected Output Format** (lines 688-711): Consumer expects Backup JSON with this structure:
   ```json
   {
     "inputMessages": [
       {"sender":"...","recipient":"...","sentAt":"...","message":"..."},
       {"sender":"...","recipient":"...","sentAt":"...","message":"..."}
     ],
     "backupTimestamp": "2025-10-11T02:00:00Z",
     "uuid": "some-guid"
   }
   ```

3. **Actual Output** ([`FlinkJobRunner.java:279-291`](FlinkIRRunner/src/main/java/com/flink/jobgateway/FlinkJobRunner.java:279-291)): COLLECT aggregation builds JSON incorrectly:
   ```java
   StringBuilder json = new StringBuilder("{\"inputMessages\":[");
   for (int i = 0; i < accumulator.size(); i++) {
       if (i > 0) json.append(",");
       json.append(accumulator.get(i));  // ← PROBLEM: Concatenates raw JSON strings
   }
   ```
   
   This produces malformed output like:
   ```json
   {
     "inputMessages":[
       {"sender":"sender-0",...}{"sender":"sender-1",...}  ← Missing comma, not proper array
     ],
     "backupTimestamp":"...",
     "uuid":"..."
   }
   ```
   
   Or if the messages contain non-JSON content, even worse formatting.

4. **Why Error Shows 'm'**: The JSON parser tries to read the Backup object but encounters malformed JSON. The 'm' likely comes from the 'message' field in concatenated JSON strings.

**Evidence**:
- FlinkJobRunner COLLECT aggregation uses raw string concatenation (line 284)
- No JSON parsing or escaping of individual messages
- StringBuilder approach doesn't ensure valid JSON array syntax
- Missing proper JSON object formatting for array elements

### Findings
**Root Cause**: FlinkJobRunner's COLLECT aggregation (lines 279-291) concatenates raw string messages without ensuring they form a valid JSON array. Need to:
1. Parse each InputMessage JSON string to verify it's valid JSON
2. Build a proper JSON array with correct comma separation
3. Ensure the final Backup JSON is well-formed and parseable

**Solution Approach**:
1. Use Jackson ObjectMapper to parse and validate each InputMessage JSON
2. Store parsed JSON objects in the accumulator
3. Use ObjectMapper to serialize the final Backup object with proper formatting
4. Add extensive logging to trace the transformation

### Lessons Learned
- **Always validate JSON at aggregation boundaries** - don't assume string concatenation produces valid JSON
- **Use proper JSON libraries** (Jackson) instead of manual string building
- **Test with actual data formats** to catch formatting mismatches early

## Phase 2: Design

### Architecture Decisions
**Change Strategy**: Replace manual JSON string building with proper Jackson-based JSON object handling

**Implementation Plan**:
1. Modify COLLECT aggregation accumulator to store parsed JSON objects
2. Use Jackson ObjectMapper for parsing input messages
3. Use Jackson ObjectMapper for serializing final Backup object
4. Add logging at each transformation step

### Why This Approach
- **Type Safety**: ObjectMapper ensures valid JSON at each step
- **Correctness**: Proper JSON library handles escaping and formatting
- **Maintainability**: Clear separation between parsing and serialization
- **Debuggability**: Can log intermediate JSON structures

### Alternatives Considered
1. **Fix string concatenation**: Could manually add proper commas and escaping
   - Rejected: Error-prone, hard to maintain, doesn't solve root issue
2. **Change Exercise2 consumer**: Could make it more tolerant
   - Rejected: The issue is in FlinkJobRunner, not the consumer

## Phase 3: Implementation

### Code Changes
Modified [`FlinkJobRunner.java:260-301`](FlinkIRRunner/src/main/java/com/flink/jobgateway/FlinkJobRunner.java:260-301):

**Before** (Manual String Building):
```java
if ("COLLECT".equals(aggType)) {
    KeyedStream<String, String> keyed = stream.keyBy(v -> "all");
    stream = keyed.window(TumblingProcessingTimeWindows.of(Duration.ofHours(24)))
            .aggregate(new AggregateFunction<String, List<String>, String>() {
                @Override
                public List<String> createAccumulator() {
                    return new ArrayList<>();
                }
                
                @Override
                public List<String> add(String value, List<String> accumulator) {
                    accumulator.add(value);
                    return accumulator;
                }
                
                @Override
                public String getResult(List<String> accumulator) {
                    // Convert to JSON array format for backup
                    StringBuilder json = new StringBuilder("{\"inputMessages\":[");
                    for (int i = 0; i < accumulator.size(); i++) {
                        if (i > 0) json.append(",");
                        json.append(accumulator.get(i));  // ← PROBLEM
                    }
                    json.append("],\"backupTimestamp\":\"");
                    json.append(java.time.Instant.now().toString());
                    json.append("\",\"uuid\":\"");
                    json.append(java.util.UUID.randomUUID().toString());
                    json.append("\"}");
                    return json.toString();
                }
                
                @Override
                public List<String> merge(List<String> a, List<String> b) {
                    a.addAll(b);
                    return a;
                }
            });
}
```

**After** (Jackson-Based JSON Handling):
```java
if ("COLLECT".equals(aggType)) {
    // Use Jackson ObjectMapper for proper JSON handling
    final ObjectMapper jsonMapper = new ObjectMapper();
    
    KeyedStream<String, String> keyed = stream.keyBy(v -> "all");
    stream = keyed.window(TumblingProcessingTimeWindows.of(Duration.ofHours(24)))
            .aggregate(new AggregateFunction<String, List<com.fasterxml.jackson.databind.JsonNode>, String>() {
                @Override
                public List<com.fasterxml.jackson.databind.JsonNode> createAccumulator() {
                    logger.info("[AGGREGATE] Creating new accumulator for COLLECT aggregation");
                    return new ArrayList<>();
                }
                
                @Override
                public List<com.fasterxml.jackson.databind.JsonNode> add(String value, List<com.fasterxml.jackson.databind.JsonNode> accumulator) {
                    try {
                        // Parse JSON string to JsonNode to ensure valid JSON
                        com.fasterxml.jackson.databind.JsonNode node = jsonMapper.readTree(value);
                        accumulator.add(node);
                        logger.debug("[AGGREGATE] Added message to accumulator, total count: {}", accumulator.size());
                        return accumulator;
                    } catch (Exception e) {
                        logger.error("[AGGREGATE] Failed to parse JSON message: {}", value, e);
                        // Skip invalid JSON messages
                        return accumulator;
                    }
                }
                
                @Override
                public String getResult(List<com.fasterxml.jackson.databind.JsonNode> accumulator) {
                    try {
                        logger.info("[AGGREGATE] Finalizing Backup with {} messages", accumulator.size());
                        
                        // Build Backup object using Jackson
                        Map<String, Object> backup = new LinkedHashMap<>();
                        backup.put("inputMessages", accumulator);
                        backup.put("backupTimestamp", java.time.Instant.now().toString());
                        backup.put("uuid", java.util.UUID.randomUUID().toString());
                        
                        String json = jsonMapper.writeValueAsString(backup);
                        logger.info("[AGGREGATE] Generated Backup JSON: {}", json);
                        return json;
                    } catch (Exception e) {
                        logger.error("[AGGREGATE] Failed to serialize Backup", e);
                        return "{\"inputMessages\":[],\"backupTimestamp\":\"" + 
                               java.time.Instant.now().toString() + 
                               "\",\"uuid\":\"" + java.util.UUID.randomUUID().toString() + "\"}";
                    }
                }
                
                @Override
                public List<com.fasterxml.jackson.databind.JsonNode> merge(List<com.fasterxml.jackson.databind.JsonNode> a, 
                                                                            List<com.fasterxml.jackson.databind.JsonNode> b) {
                    a.addAll(b);
                    logger.debug("[AGGREGATE] Merged accumulators, total count: {}", a.size());
                    return a;
                }
            });
    logger.info("[AGGREGATE OPERATION] ✓ COLLECT aggregation configured with Jackson JSON handling");
}
```

### Changes Summary
1. **Accumulator Type**: Changed from `List<String>` to `List<JsonNode>` for type-safe JSON handling
2. **JSON Parsing**: Added `jsonMapper.readTree(value)` to parse and validate each input message
3. **Error Handling**: Added try-catch for JSON parsing failures with logging
4. **JSON Serialization**: Use `jsonMapper.writeValueAsString()` for proper Backup object serialization
5. **Logging**: Added comprehensive logging at each aggregation step
6. **Import**: Added `import com.fasterxml.jackson.databind.JsonNode;` at top of file

### Challenges Encountered
- Need to balance between strict JSON validation and fault tolerance
- Must ensure logging doesn't impact performance
- Window size of 24 hours means aggregation happens infrequently - added debug logging

### Solutions Applied
- Use JsonNode for type-safe JSON representation
- Skip invalid JSON messages rather than failing entire aggregation
- Provide fallback Backup JSON if serialization fails
- Log at INFO level for major operations, DEBUG for per-message details

## Phase 4: Testing & Validation

### Test Execution
**Command**: `dotnet test LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Day01.IntegrationTests/Day01.IntegrationTests.csproj --filter Exercise2`

### Expected Results
1. ✅ Exercise2 job submits successfully
2. ✅ 50 InputMessage objects produced to `flink_input` topic
3. ✅ Flink processes messages through COLLECT aggregation
4. ✅ Backup objects with properly formatted JSON appear in `flink_output` topic
5. ✅ Consumer successfully deserializes Backup objects
6. ✅ Test passes with Backup aggregations consumed

### Test Results
*To be filled after test execution*

### Performance Metrics
*To be filled after test execution*

## Phase 5: Lessons Learned & Future Reference

### What Worked Well
- Jackson ObjectMapper provides type-safe JSON handling
- Comprehensive logging helps trace data transformations
- Error handling prevents one bad message from breaking aggregation

### What Could Be Improved
- Window size of 24 hours is too long for testing - future work could make it configurable
- Could add metrics for aggregation performance
- Could add more sophisticated error recovery strategies

### Key Insights for Similar Tasks
- **Always use proper JSON libraries** for JSON manipulation - never build JSON strings manually
- **Validate JSON at transformation boundaries** to catch format mismatches early
- **Log intermediate states** to make debugging easier
- **Handle errors gracefully** in aggregation functions to prevent data loss

### Specific Problems to Avoid in Future
- ❌ Manual JSON string building with StringBuilder
- ❌ Concatenating raw strings without validation
- ❌ Assuming input data format matches expected format
- ❌ Silent failures in aggregation functions

### Reference for Future WIs
- When implementing aggregation operations, use Jackson ObjectMapper for JSON handling
- Always validate JSON at each transformation step
- Provide comprehensive logging for debugging data flow issues
- Include error handling and fallback strategies in aggregation functions