# WI13: Implement Aggregate Operation for Flink 2.1.0

**File**: `WIs/WI13_implement-aggregate-operation-flink21.md`
**Title**: Add aggregate operation support to FlinkJobRunner for Flink 2.1.0
**Description**: Exercise 2 (BackupAggregator) fails because FlinkJobRunner Java code doesn't support 'aggregate' operation type. Need to implement this to enable time-windowed aggregations.
**Priority**: High
**Component**: FlinkIRRunner (Java), FlinkDotNet.DataStream
**Type**: Feature Implementation
**Assignee**: GitHub Copilot
**Created**: 2025-10-10
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI12_learningcourse-kafka-connectivity-fix.md - Recent Kafka connectivity fix
- LocalTesting/WIs/WI6_kafka-connectivity-fix.md - Flink job patterns

### Lessons Applied
- **Debug-first approach**: Understand error messages before implementing
- **Check existing patterns**: Review FlinkJobRunner.java for similar operations
- **Test incrementally**: Validate each operation type implementation

### Problems Prevented
- Not implementing without understanding Flink 2.1.0 aggregate API
- Not checking if similar operations already exist in codebase

## Phase 1: Investigation

### Requirements
- Add 'aggregate' operation support to FlinkJobRunner.java
- Support time-windowed aggregations (tumbling windows)
- Enable Exercise 2 (BackupAggregator) to work correctly
- Maintain compatibility with existing operations (map, filter, window, etc.)

### Debug Information (MANDATORY)

**Error Message from Test**:
```
Could not resolve type id 'aggregate' as a subtype of `com.flink.jobgateway.FlinkJobRunner$Operation`: 
known type ids = [async, filter, map, retry, side-output, state, timer, window]
```

**Analysis**:
- FlinkJobRunner.java knows about: async, filter, map, retry, side-output, state, timer, window
- Missing: aggregate operation type
- Exercise 2 uses: Time-windowed aggregation with BackupAggregator function

**Flink 2.1.0 Location**: User saved Flink to `C:\GitHub\flink`

**Exercise 2 Requirements** (from test output):
```
Baeldung Java API equivalent:
inputMessagesStream
  .timeWindowAll(Time.hours(24))
  .aggregate(new BackupAggregator())
  .addSink(flinkKafkaProducer);
```

**Current Operation Types in FlinkJobRunner**:
1. `map` - Transformation
2. `filter` - Filtering
3. `window` - Time windows
4. `async` - Async I/O
5. `state` - State management
6. `timer` - Timers
7. `retry` - Retry logic
8. `side-output` - Side outputs

**Missing Operation**: `aggregate` for windowed aggregations

### Next Steps
1. ✅ Examine FlinkJobRunner.java to understand operation structure
2. ⏳ Check Flink 2.1.0 aggregate API documentation
3. ⏳ Implement AggregateOperation class in FlinkJobRunner.java
4. ⏳ Update FlinkDotNet.DataStream to support aggregate operations
5. ⏳ Test with Exercise 2

## Phase 2: Design

### Requirements
- Add AggregateOperation to FlinkJobRunner.java Operation types
- Support AggregateFunction interface from Flink
- Handle generic types for input/accumulator/output
- Integrate with existing window operations

### Architecture Decision

**Solution**: Extend FlinkJobRunner.java with AggregateOperation

**Implementation Plan**:
1. Add `@JsonTypeName("aggregate")` operation class to FlinkJobRunner.java
2. Implement AggregateFunction deserialization from JSON
3. Support custom aggregator classes (like BackupAggregator)
4. Wire up with DataStream aggregate API

**Files to Modify**:
- `FlinkIRRunner/src/main/java/com/flink/jobgateway/FlinkJobRunner.java`
- `FlinkDotNet/FlinkDotNet.DataStream/DataStream.cs` (potentially)

## Phase 3: TDD/BDD
(To be completed after design)

## Phase 4: Implementation
(To be completed after testing)

## Phase 5: Testing & Validation
(To be completed after implementation)

## Phase 6: Owner Acceptance
(To be completed after validation)

## Lessons Learned & Future Reference (MANDATORY)
(To be completed at end of WI)