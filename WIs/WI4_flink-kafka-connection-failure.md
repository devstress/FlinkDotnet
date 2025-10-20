# WI4: Flink Cannot Connect to Kafka - Topic Creation Timing Issue

**File**: `WIs/WI4_flink-kafka-connection-failure.md`
**Title**: [Flink/Kafka] Flink job starts before Kafka topics are created
**Description**: Flink job submission succeeds but consumer cannot connect to Kafka because topics don't exist yet
**Priority**: High
**Component**: Integration Testing
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-19
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI3_flink-not-consuming-kafka-messages.md: Dynamic topic names solution
- WI2_kafka-empty-results-playwright.md: Playwright DOM validation patterns
- WI1_prometheus-kafka-metrics-debug.md: Kafka JMX exporter configuration

### Lessons Applied  
- Using dynamic topic/consumer group names to avoid offset persistence issues
- Validating actual message flow through Flink metrics instead of Kafka topic counts
- Understanding Docker network topology: internal IPs vs host-mapped ports

### Problems Prevented
- Not repeating consumer group offset issues from WI3
- Not skipping proper debugging to find root cause

## Phase 1: Investigation

### Requirements
Debug why Flink TaskManager continuously fails to connect to Kafka despite correct bootstrap servers configuration.

### Debug Information (MANDATORY)

#### Error Messages
From Flink TaskManager logs (lines 391-640):
```
Connection to node -1 (/10.89.2.4:9093) could not be established. Node may not be available.
Bootstrap broker 10.89.2.4:9093 (id: -1 rack: null) disconnected
```

Repeated 140+ times over 76 seconds until job was cancelled.

#### Log Locations
- **Flink Gateway**: `LocalTesting/test-logs/FlinkDotNet.JobGateway.log.20251019`
- **Flink TaskManager**: `LocalTesting/test-logs/Flink.taskmanager.container.log.20251019`
- **Test Infrastructure**: `LocalTesting/test-logs/TestInfrastructure.Debug.log.20251019`

#### System State
**Docker Network Configuration** (from TestInfrastructure.Debug.log line 40):
```
kafka-xbfcyurr: 8082/tcp, 127.0.0.1:46759->9092/tcp, 127.0.0.1:42295->9093/tcp
```

**Kafka Internal IP** (line 32):
```
Kafka container IP discovered: 10.89.2.4:9093
```

**Job Configuration** (Gateway log lines 133-163):
```json
{
  "source": {
    "type": "kafka",
    "topic": "observability_input_bb036480d9cb4bdb9aed54a4b1cccaf8",
    "bootstrapServers": "10.89.2.4:9093",
    "groupId": "observability-demo-2a13010ae1f045eb8d961c1f4ca2c792",
    "startingOffsets": "earliest"
  }
}
```

#### Reproduction Steps
1. Test starts LearningCourse infrastructure with dynamic Kafka ports
2. Test waits for Flink Gateway to be ready
3. Test starts Exercise51 to produce messages and submit Flink job
4. Exercise51 submits Flink job with dynamic topic names
5. Flink job starts but cannot find Kafka broker
6. Flink consumer continuously retries connection for 76 seconds
7. Test times out or job gets cancelled with 0 messages processed

#### Evidence

**Test Results**:
- `numRecordsIn`: 0 (expected: 10,000)
- `numRecordsOut`: 0 (expected: 10,000)
- Test passed but metrics show no actual processing

**Network Connectivity**:
- Flink uses internal Docker IP: `10.89.2.4:9093` ✅ CORRECT
- Exercise51 should use host-mapped port: `127.0.0.1:46759` ✅ CORRECT
- Flink can reach Kafka network-wise but topic doesn't exist ❌ PROBLEM

### Root Cause Analysis

#### Timeline of Events
1. **T+0s**: Infrastructure starts, Kafka assigned dynamic ports
2. **T+11s**: Flink Gateway becomes ready
3. **T+12s**: Exercise51 process starts
4. **T+13s**: Flink job submitted to JobManager
5. **T+13s**: Job starts IMMEDIATELY, begins polling Kafka
6. **T+13-89s**: Flink consumer continuously fails to connect
7. **Exercise51 never produces messages to Kafka topics!**

#### The Critical Issue
**Flink job is submitted BEFORE Exercise51 produces any messages to Kafka**

When Flink tries to consume from topic `observability_input_bb036480d9cb4bdb9aed54a4b1cccaf8`:
1. Topic doesn't exist yet (Exercise51 hasn't run producer)
2. Kafka broker connection works, but metadata query fails
3. Consumer cannot coordinate with broker because topic is missing
4. Connection appears as "disconnected" even though network is fine

#### Why Connection Fails
From Kafka consumer perspective:
- Bootstrap server `10.89.2.4:9093` is reachable (network OK)
- But trying to get metadata for non-existent topic fails
- Kafka broker rejects metadata request with disconnection
- Consumer retries indefinitely waiting for topic to appear

#### Missing Piece
**We don't have Exercise51 output logs!** Need to verify:
- Did Exercise51 actually run the producer?
- Did it produce 10,000 messages to Kafka?
- Did it wait for production to complete before submitting job?
- What error messages did it print?

### Findings

**Root Cause**: Timing issue in test flow:
1. Test starts Exercise51 process
2. Exercise51 immediately submits Flink job (async)
3. Exercise51 starts producing messages (async)
4. Flink job starts before producer finishes
5. Topics don't exist when Flink tries to consume

**Solution Approach**: Ensure message production completes BEFORE submitting Flink job.

### Lessons Learned
- Docker network topology: internal IPs for container-to-container vs host ports for host-to-container
- Kafka "connection failed" can mean "topic doesn't exist" not just network issues
- Test timing: async operations need proper sequencing
- Always check if prerequisite data exists before starting consumers

## Phase 2: Design

### Requirements
Fix the timing issue to ensure:
1. Exercise51 produces all 10,000 messages to Kafka
2. Kafka topics are created and contain messages
3. THEN Flink job is submitted
4. Flink can immediately start consuming from existing topics

### Architecture Decisions

**Option 1: Sequential Execution in Exercise51** ⭐ RECOMMENDED
```csharp
// 1. Produce messages first
await ProduceMessagesAsync(10000);
await producer.FlushAsync(); // Ensure all messages sent

// 2. Wait for topic to be ready
await WaitForTopicAsync(InputTopic);

// 3. THEN submit Flink job
await SubmitFlinkJobAsync();
```

**Option 2: Test Orchestration**
```csharp
// In test code
await StartExercise51ProducerOnly();
await WaitForKafkaTopicsWithData();
await SubmitFlinkJobViaGateway();
```

**Decision**: Use Option 1 - modify Exercise51 to sequence operations properly.

### Why This Approach
- Exercise51 has full control over timing
- No test infrastructure changes needed
- Matches real-world usage pattern
- Clear separation of concerns

### Alternatives Considered
- Auto-create topics in Flink job: Doesn't help, still have race condition
- Retry logic in Flink: Already exists, didn't help because topic never appeared
- Pre-create topics in test: Couples test to Exercise51 internals

## Phase 3: Implementation

### Code Changes Required

#### 1. Exercise51/Program.cs
Ensure sequential execution:
```csharp
// Current (WRONG - async fire-and-forget):
Task.Run(() => ProduceMessages());
SubmitFlinkJob(); // Runs immediately!

// Fixed (RIGHT - sequential):
await ProduceMessagesAsync();
await producer.FlushAsync();
Console.WriteLine($"✅ Produced {messageCount} messages");
await Task.Delay(2000); // Let Kafka settle
await SubmitFlinkJobAsync();
```

#### 2. Add Exercise51 Logging
Need to capture Exercise51 output to verify execution:
```csharp
Console.WriteLine($"📤 Starting message production to {InputTopic}...");
// ... produce messages ...
Console.WriteLine($"✅ Production complete: {produced} messages");
Console.WriteLine($"🚀 Submitting Flink job...");
// ... submit job ...
Console.WriteLine($"✅ Flink job submitted: {jobId}");
```

### Next Steps
1. Read Exercise51/Program.cs to understand current flow
2. Identify where async operations happen
3. Add proper sequencing and logging
4. Test locally
5. Run Playwright test to verify

## Phase 4: Testing & Validation

(To be completed after implementation)

## Lessons Learned & Future Reference (MANDATORY)

### What We Discovered
- Kafka "connection failed" errors can indicate missing topics, not just network issues
- Docker internal IPs work for container-to-container but need host ports for host-to-container
- Async operations in tests need explicit sequencing
- Always verify prerequisite state before starting dependent operations

### Key Insights for Similar Tasks
- When debugging Kafka connectivity, check if topics exist first
- Flink consumers need topics to exist before they can connect properly
- Test infrastructure timing is critical for integration tests
- Log everything - we're missing Exercise51 output which would have shown the issue immediately

### Specific Problems to Avoid in Future
- Don't submit Flink jobs before producing test data to Kafka
- Don't assume async operations complete in expected order
- Don't skip logging in test utilities - it's critical for debugging
- Always capture child process output in tests

### Reference for Future WIs
When working with Kafka + Flink integration tests:
1. Always produce messages BEFORE submitting consumer jobs
2. Add flush() calls to ensure messages are committed
3. Wait for topics to be ready before starting consumers
4. Capture and log all process outputs for debugging
5. Verify timing/sequencing of async operations