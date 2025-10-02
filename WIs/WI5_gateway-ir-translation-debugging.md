# WI5: Gateway IR Translation Debugging

**File**: `WIs/WI5_gateway-ir-translation-debugging.md`
**Title**: Debug Gateway IR Translation Issues After Infrastructure Validation
**Description**: Native Flink job test passed, proving Kafka + Flink infrastructure works. Need to debug Gateway's IR translation and Kafka configuration in generated Flink jobs.
**Priority**: High
**Component**: Gateway IR Translation
**Type**: Investigation
**Assignee**: AI Agent
**Created**: 2025-10-02
**Status**: Investigation

## Context
- Native Flink job successfully processed messages with kafka:9093 bootstrap configuration
- Infrastructure (Kafka + Flink) validated and working correctly
- Gateway likely has issues with IR translation or Kafka configuration in generated jobs
- Need to test Gateway's IR runner JAR directly to isolate the problem

## Lessons Applied from Previous WIs
### Previous WI References
- WI4: Aspire FlinkDotNet Testing - learned about container networking and service discovery
- WI3: Aspire FlinkDotNet Setup Testing - learned about proper Kafka bootstrap server configuration
- WI2: Aspire DCP Networking Fix - learned about container-to-container vs host-to-container communication

### Lessons Applied
- Use container-internal hostnames (kafka:9093) for container-to-container communication
- Verify all components with minimal test cases first
- Focus on one layer at a time (now focusing on Gateway IR translation layer)

### Problems Prevented
- Already validated infrastructure, so won't waste time debugging Kafka/Flink setup
- Will test Gateway IR runner directly to isolate translation issues

## Phase 1: Investigation

### Requirements
1. Run FlinkRunnerDirectTest to test Gateway's IR runner JAR directly
2. Compare working native job configuration with Gateway-generated configuration
3. Check if Gateway uses correct Kafka bootstrap servers
4. Verify Gateway's IR-to-Flink translation logic

### Debug Information (MANDATORY - Update this section for every investigation)

#### Initial State
- **Test to Run**: `FlinkRunnerDirectTest` in LocalTesting.IntegrationTests
- **Expected Behavior**: Gateway IR runner should successfully translate IR to Flink job and process messages
- **Known Working Configuration**: Native Flink job with kafka:9093 bootstrap servers
- **Test Command**: `dotnet test LocalTesting.IntegrationTests --filter FlinkRunnerDirectTest`

#### Test Execution Results
**Error**: `System.IO.FileNotFoundException: flink-ir-runner.jar not found`

**Search Paths Attempted** (from line 182-193 of FlinkRunnerDirectTest.cs):
1. `TestContext.CurrentContext.TestDirectory/flink-ir-runner.jar`
2. `FlinkIRRunner/target/flink-ir-runner.jar`
3. `../FlinkIRRunner/target/flink-ir-runner.jar`
4. `../../FlinkIRRunner/target/flink-ir-runner.jar`

**Infrastructure Status**: ✅ All infrastructure ready (Kafka, Flink JobManager, TaskManager)
- Flink JobManager accessible at http://localhost:57415/
- Kafka topics created successfully
- Job definition JSON correctly formatted with kafka:9093 bootstrap servers

**Root Cause**: The flink-ir-runner.jar is not being built or copied to test directory

### Findings
**Primary Issue**: JAR naming mismatch between build output and test expectations
- Infrastructure is working correctly (Kafka, Flink, all ready)
- Job definition format is correct (using kafka:9093)
- JAR files exist in `FlinkIRRunner/target/` but with wrong name:
  - Built: `flink-ir-runner-java17.jar` (19.5 MB)
  - Expected: `flink-ir-runner.jar`
- Root cause: The pom.xml builds with profile-specific naming, but test expects canonical name

**Resolution Applied**:
- Copied `flink-ir-runner-java17.jar` to `flink-ir-runner.jar` in target directory
- This allows test to find and use the JAR

**Next Step**: Re-run FlinkRunnerDirectTest to validate IR translation logic

#### Second Test Run - JAR Found, Flink 500 Error
**Status**: JAR successfully uploaded to Flink, but job submission failed with HTTP 500

**Key Findings**:
- ✅ JAR found and uploaded: `C:\GitHub\FlinkDotnet\LocalTesting\LocalTesting.IntegrationTests\bin\Debug\net9.0\flink-ir-runner.jar`
- ✅ Infrastructure ready (Kafka, Flink JobManager, TaskManager all operational)
- ✅ Job definition properly formatted with kafka:9093 bootstrap servers
- ❌ Flink REST API returned 500 Internal Server Error at line 172 of test
- Error occurred during job submission to `/jars/{jarId}/run` endpoint

**Root Cause IDENTIFIED**: The IR runner JAR execution fails with HTTP 500 when Flink tries to run it

#### Third Test Run - HTTP 500 Confirmed, Root Cause Analysis
**Status**: ✅ Test infrastructure working, ❌ IR Runner failing with HTTP 500

**Test Execution Results**:
- ✅ JAR successfully copied to test directory
- ✅ Infrastructure ready (Kafka, Flink JobManager, TaskManager operational)
- ✅ Job definition JSON properly formatted
- ✅ JAR uploaded to Flink successfully
- ❌ **HTTP 500 Internal Server Error** when submitting job to `/jars/{jarId}/run` endpoint

**Root Cause Analysis** (based on code review):

**Most Likely Issue: Missing `startingOffsets` field in JSON deserialization**

From [`FlinkRunnerDirectTest.cs`](LocalTesting/LocalTesting.IntegrationTests/FlinkRunnerDirectTest.cs:49-72):
```csharp
Source = new KafkaSourceDefinition
{
    Topic = InputTopic,
    BootstrapServers = KafkaContainerConnectionString, // kafka:9093
    GroupId = "runner-direct-test"
    // NOTE: No StartingOffsets specified!
}
```

From [`FlinkJobRunner.java`](FlinkIRRunner/src/main/java/com/flink/jobgateway/FlinkJobRunner.java:329-336):
```java
public static class KafkaSourceDefinition implements Source {
    public String type;
    public String topic;
    public String bootstrapServers;
    public String groupId;
    @JsonProperty("startingOffsets")  // <-- This annotation expects this field!
    public String startingOffsets;
}
```

The test JSON output shows:
```json
"source": {
  "type": "kafka",
  "topic": "lt.runner.direct.input",
  "bootstrapServers": "kafka:9093",
  "groupId": "runner-direct-test",
  "startingOffsets": "latest",  // <-- This IS being set correctly!
  "properties": {}
}
```

**Wait - the JSON is correct!** So the issue must be elsewhere...

**ACTUAL Root Cause: IR JSON Schema Mismatch**

Looking at the serialized JSON more carefully, I see extra fields that FlinkJobRunner.java doesn't expect:
- `properties: {}` on source (line 329-336 of FlinkJobRunner.java has no `properties` field)
- `serializer: "json"` on sink (not in KafkaSinkDefinition)
- `properties: {}` on sink
- `properties: {}` on metadata
- `outputType: null` on operations

**FlinkJobRunner.java** uses `@JsonIgnoreProperties(ignoreUnknown = true)` so these shouldn't cause issues...

**REVISED Root Cause: Entry Class or Classpath Issue**

The test submits with:
```csharp
entryClass = "com.flink.jobgateway.FlinkJobRunner"
```

This is correct based on the Java file's package declaration. The HTTP 500 suggests Flink can't execute the main method.

**ROOT CAUSE IDENTIFIED: Incorrect JAR ID in Run Request**

The error message from Flink is clear:
```
Jar file /tmp/flink-web-5a8dd6d0-8633-456e-9f91-d288830d6da9/flink-web-upload/flink-ir-runner.jar does not exist
```

**Problem**: The test uses `Path.GetFileName(jarPath)` as the JAR ID (line 152), but Flink returns a specific JAR ID from the upload response that must be used for the run request.

**From test code** (LocalTesting.IntegrationTests/FlinkRunnerDirectTest.cs:148-153):
```csharp
var uploadResponse = await httpClient.PostAsync("/jars/upload", form, ct);
uploadResponse.EnsureSuccessStatusCode();

var jarId = Path.GetFileName(jarPath);  // WRONG! Should parse from uploadResponse
await Task.Delay(TimeSpan.FromSeconds(2), ct);
```

**Flink JAR Upload API** returns JSON like:
```json
{
  "filename": "/tmp/flink-web-xxx/flink-web-upload/{uploaded-jar-id}",
  "status": "success"
}
```

We need to extract the `filename` from the upload response and use that as the `jarId` for the run request.

### Lessons Learned
- [To be documented as investigation progresses]

## Summary of Investigation

### Key Findings
1. **Infrastructure Validation**: ✅ Kafka and Flink are working correctly
   - Native Flink job successfully processed messages
   - Container networking properly configured (kafka:9093)

2. **JAR Availability**: ✅ Resolved
   - JAR exists but had naming mismatch issue
   - Fixed by copying `flink-ir-runner-java17.jar` to `flink-ir-runner.jar`

3. **Gateway IR Translation Issue**: ❌ IDENTIFIED
   - **HTTP 500 Internal Server Error** when submitting IR-based job to Flink
   - JAR uploaded successfully to Flink
   - Error occurs during job execution, not upload
   - Job definition JSON is properly formatted with correct Kafka bootstrap servers

### Root Cause Analysis
The Gateway's IR runner (FlinkJobRunner.java) has a problem when:
- Deserializing the IR JSON, OR
- Translating IR operations (like "toUpper") to Flink operations, OR
- Configuring Kafka connections from IR, OR
- Setting up the Flink job pipeline

### Resolution Applied
**Fix**: Parse the correct JAR ID from Flink's upload response instead of using the filename

**Changes Made** (LocalTesting.IntegrationTests/FlinkRunnerDirectTest.cs:144-163):
```csharp
// OLD CODE (WRONG):
var jarId = Path.GetFileName(jarPath);  // Used just the filename

// NEW CODE (CORRECT):
var uploadResult = await uploadResponse.Content.ReadAsStringAsync(ct);
var uploadDoc = JsonDocument.Parse(uploadResult);
var filename = uploadDoc.RootElement.GetProperty("filename").GetString();
var jarId = Path.GetFileName(filename);  // Extract JAR ID from Flink's response
```

**Test Results**: ✅ **PASSED**
- JAR uploaded successfully to Flink
- Job submitted and ran successfully
- All 10 messages processed correctly (lowercase → uppercase transformation)
- Kafka communication working with `kafka:9093` bootstrap servers
- Test completed in 34.8 seconds

**Key Output**:
```
Produced: test-0 → Consumed: TEST-0
Produced: test-1 → Consumed: TEST-1
...
Produced: test-9 → Consumed: TEST-9
📊 Consumed 10 messages (expected: 10)
✓ All messages uppercase: True
✅ DIRECT RUNNER TEST PASSED in 34.8s
   FlinkJobRunner.java works correctly with kafka:9093
```

### Debug Tools Created
**Log Capture Script**: `scripts/capture-flink-logs-during-test.ps1`
- Captures Flink JobManager logs in real-time during test execution
- Saves timestamped logs to `./test-logs/` directory
- Performs automatic analysis to extract errors and IR runner logs
- Usage: `.\scripts\capture-flink-logs-during-test.ps1`

**FlinkJobRunner.java Analysis**:
- ✅ Comprehensive logging already implemented in FlinkJobRunner.java
- Logs are clearly marked with [KAFKA SOURCE], [KAFKA SINK], [MAP OPERATION] tags
- Lines 83-91: Kafka source configuration logging
- Lines 117-123: Map operation translation logging
- Lines 230-237: Kafka sink configuration logging
- Lines 432-473: Detailed Kafka consumer logging with poll counts
- Lines 496-520: Detailed Kafka producer logging

The Java runner already has all the debug logging we need - we just need to capture it!

## Phase 2: Design
### Requirements
- Once root cause is identified from Flink logs, create fix strategy
- May need to update FlinkJobRunner.java IR translation
- May need to fix Kafka connection configuration in IR runner
- May need to update operation translation (toUpper)

## Phase 3: TDD/BDD
### Test Specifications
- [To be filled after design phase]

## Phase 4: Implementation
### Code Changes
- [To be filled after test specifications]

## Phase 5: Testing & Validation
### Test Results
- [To be filled after implementation]

## Phase 6: Owner Acceptance
### Demonstration
- [To be filled after validation]

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
1. **Systematic debugging approach**: Modified test code to capture error response bodies before throwing exceptions
2. **Error message analysis**: Flink's error message clearly indicated the root cause once we captured it
3. **Test infrastructure**: The FlinkRunnerDirectTest successfully isolated the issue from Gateway complexity
4. **Quick iteration**: Made small, targeted changes and validated immediately

### What Could Be Improved
1. **Initial investigation**: Should have checked Flink API documentation for JAR upload/run workflow first
2. **Response parsing**: Should always parse API responses rather than assuming filename conventions
3. **Log capture approach**: The background job approach for log capture was over-complicated; capturing error response bodies was sufficient

### Key Insights for Similar Tasks
1. **Always capture error response bodies**: Never just throw on HTTP error codes without logging the response content
2. **Read API documentation**: Flink REST API returns specific JAR IDs that must be used for subsequent operations
3. **Trust error messages**: Once we captured the actual error, it told us exactly what was wrong
4. **Test incrementally**: The step-by-step approach (infrastructure validation → JAR isolation → error capture → fix) worked well

### Specific Problems to Avoid in Future
1. **Incorrect API usage**: Using filename instead of API-returned IDs for subsequent API calls
2. **Silent failures**: Not capturing error response bodies makes debugging nearly impossible
3. **Over-engineering**: Don't create complex log capture mechanisms when simple error logging would suffice

### Reference for Future WIs
**Problem**: HTTP 500 errors when submitting Flink jobs via REST API

**Root Cause**: Using `Path.GetFileName(jarPath)` as JAR ID instead of parsing Flink's upload response

**Solution**: Parse the `filename` property from Flink's JAR upload response and extract the JAR ID from it

**Code Pattern**:
```csharp
// Upload JAR
var uploadResponse = await httpClient.PostAsync("/jars/upload", form, ct);
uploadResponse.EnsureSuccessStatusCode();

// Parse JAR ID from response
var uploadResult = await uploadResponse.Content.ReadAsStringAsync(ct);
var uploadDoc = JsonDocument.Parse(uploadResult);
var filename = uploadDoc.RootElement.GetProperty("filename").GetString();
var jarId = Path.GetFileName(filename);

// Use jarId for /jars/{jarId}/run endpoint
```

**Testing**: FlinkRunnerDirectTest validates IR translation end-to-end, proving infrastructure works correctly with proper API usage