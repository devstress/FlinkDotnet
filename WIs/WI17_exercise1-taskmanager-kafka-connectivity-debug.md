# WI17: Debug Exercise1 TaskManager Kafka Connectivity Issue

**File**: `WIs/WI17_exercise1-taskmanager-kafka-connectivity-debug.md`
**Title**: Fix TaskManager connecting to wrong Kafka address (localhost:9093 instead of kafka:9092)
**Description**: Exercise1 TaskManager shows error "Connection to node 1 (localhost/127.0.0.1:9093) could not be established" even though code uses kafka:9092
**Priority**: High
**Component**: LearningCourse Exercise1, FlinkDotNet DataStream API
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-10-10
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI12_learningcourse-kafka-connectivity-fix.md - Kafka port configuration patterns
- WI11_kafka-taskmanager-connectivity-debug.md - TaskManager connectivity debugging
- LocalTesting/WIs/WI6_kafka-connectivity-fix.md - Kafka bootstrap server resolution

### Lessons Applied
- **Debug-first approach**: Add comprehensive logging before making changes
- **Trace data flow**: Follow bootstrap servers value through entire pipeline
- **Java FlinkJobRunner logging**: Use existing extensive logging in FlinkJobRunner.java
- **Check environment variables**: Verify no environment variables are overriding configuration

### Problems Prevented
- Not adding enough logging to identify exact failure point
- Not checking if environment variables are interfering
- Not tracing configuration through entire .NET → Java pipeline

## Phase 1: Investigation

### Requirements
- Fix TaskManager Kafka connectivity in Exercise1
- Ensure TaskManager uses `kafka:9092` (internal Docker network)
- Add comprehensive logging to trace configuration flow
- Identify why TaskManager sees `localhost:9093` instead of `kafka:9092`

### Debug Information (MANDATORY)

**Problem Statement**:
- User reports: "TaskManager connecting to wrong kafka should be kafka:9092"
- Error message: "Connection to node 1 (localhost/127.0.0.1:9093) could not be established"
- Expected: TaskManager should use `kafka:9092` for internal Docker network

**Current Configuration** (Exercise1/Program.cs):
```csharp
// Line 146: FromKafka uses kafka:9092
var stringInputStream = environment.FromKafka(
    topic: InputTopic,
    bootstrapServers: "kafka:9092",  // Correct for containers
    groupId: ConsumerGroup,
    startingOffsets: "earliest"
);

// Line 155: SinkToKafka uses kafka:9092  
stringInputStream
    .Map(new WordsCapitalizer())
    .SinkToKafka(OutputTopic, "kafka:9092");  // Correct for containers
```

**Evidence**:
1. Exercise1 Program.cs correctly hardcodes `kafka:9092` for both source and sink
2. FlinkJobRunner.java has extensive logging that would show bootstrap servers
3. TaskManager error shows it's trying to use `localhost:9093`
4. This suggests configuration is being overridden somewhere in the pipeline

**Root Cause IDENTIFIED**:
The Kafka broker is advertising `localhost:9093` as its address in broker metadata. When the TaskManager's Kafka client connects to `kafka:9092`, it successfully connects but then receives broker metadata that says "I'm actually at localhost:9093". The TaskManager then tries to connect to `localhost:9093` which fails because localhost inside the container is not the Kafka broker.

**Evidence**:
1. C# logs show: Source/Sink bootstrapServers = `kafka:9092` ✅
2. Java JobManager logs show: bootstrap.servers = `kafka:9092` ✅
3. TaskManager logs show: Trying to connect to `localhost/127.0.0.1:9093` ❌

This is a classic Kafka advertised listeners problem!

### Debugging Strategy

**Step 1**: Add C# logging to trace JobDefinition creation
**Step 2**: Add logging to verify what JSON is sent to Java
**Step 3**: Check FlinkJobRunner Java logs for actual bootstrap servers used
**Step 4**: Verify environment variables in TaskManager container
**Step 5**: Test with explicit logging and fix identified issue

## Phase 2: Design

### Architecture Decision

**Add comprehensive logging throughout the pipeline**:

1. **StreamExecutionEnvironment.FromKafka()**: Log bootstrap servers parameter
2. **OperationCapture.CaptureKafkaSource()**: Log captured bootstrap servers
3. **DataStream.SinkToKafka()**: Log sink bootstrap servers
4. **OperationCapture.ToJobDefinition()**: Log final JobDefinition bootstrap servers
5. **Run test and examine all logs**: Identify where configuration goes wrong

### Solution: Configure Kafka with Proper Advertised Listeners

Aspire's `.AddKafka()` doesn't expose `KAFKA_ADVERTISED_LISTENERS` configuration. We need to replace it with a custom Kafka container that properly configures dual listeners:

1. **Internal listener**: `PLAINTEXT://kafka:9092` (for Flink containers)
2. **External listener**: `PLAINTEXT_HOST://localhost:9093` (for host machine)

The Kafka broker needs to advertise the correct address for each listener type.

## Phase 3: Implementation

### Adding Comprehensive Logging

Will add logging to:
- `StreamExecutionEnvironment.FromKafka()`
- `OperationCapture.CaptureKafkaSource()`  
- `DataStream<T>.SinkToKafka()`
- `OperationCapture.ToJobDefinition()`

This will create a complete audit trail showing exactly what bootstrap servers value flows through the system.