# WI20: Migrate Simulation Exercises to Real Implementations with Aspire Discovery

**File**: `WIs/WI20_migrate-simulations-to-real-implementations.md`
**Title**: [LearningCourse] Replace all simulation exercises with real Kafka/Flink/Temporal implementations
**Description**: Convert 30+ simulation-based exercises to use real infrastructure with Aspire service discovery
**Priority**: High
**Component**: LearningCourse
**Type**: Enhancement - Architectural Refactoring
**Assignee**: AI Agent
**Created**: 2025-01-13
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI16: Day02 integration tests fix - learned about test validation patterns
- WI17: Flink job cleanup - learned about proper resource management
- WI18: IJobClient pattern - learned about interface-based infrastructure access

### Lessons Applied
- Use environment variables for all infrastructure endpoints (from update-LearningCourse.md section)
- Follow Exercise1-StringCapitalize pattern for Kafka/Flink integration
- Ensure exercises are console apps that complete and exit (not web services)
- Implement proper error handling and resource cleanup

### Problems Prevented
- Hardcoded localhost addresses causing test failures
- Exercises running indefinitely causing test timeouts
- Port conflicts from static port assignments
- Infrastructure connection failures from missing environment variables

## Phase 1: Investigation

### Requirements

**User Request**: "Revisit the entire exercises and Remove all simulation exercises and replace by the real one with the aspire discovery method"

**Scope**: Convert all simulation-based exercises across Days 03-15 to use:
1. Real Kafka producers/consumers with dynamic port discovery
2. Real Flink job submission to actual Flink cluster
3. Real Temporal workflow executions
4. Proper environment variable usage for all endpoints

### Debug Information (MANDATORY)

**Error Messages**: Test failures from hardcoded localhost addresses
```
Failed to connect to localhost:9092
Connection refused when accessing Kafka
```

**Log Locations**: 
- Exercise35 logs show Kafka connection attempts to localhost:9092
- Test infrastructure sets KAFKA_BOOTSTRAP_SERVERS to dynamic ports (e.g., localhost:43175)

**System State**:
- Aspire assigns dynamic ports to containers
- Exercises use hardcoded static ports
- Environment variables not leveraged in most exercises

**Reproduction Steps**:
1. Run `dotnet test LearningCourse/IntegrationTests.sln`
2. Observe Exercise35 fails with exit code 1
3. Check logs show connection attempts to hardcoded localhost:9092

**Evidence**: Search found 34 files with simulation/demo code across Days 02-08

### Findings

**Affected Exercises by Day**:

**Day02 (Flink 2.1 Fundamentals)**:
- Exercise22: Has "simulation" comments but actual structure unknown

**Day03 (AI Stream Processing)**:
- MLPredictTVFImplementation: User history simulation (line 271)
- MLNetIntegration: Streaming inference simulation (line 43)

**Day04 (Production Backpressure)** - MOST CRITICAL:
- Exercise31: Simulation duration 10 seconds (lines 36-49)
- Exercise32: Simulation duration 10 seconds (lines 37-50)
- Exercise33: Simulation duration 15 seconds (lines 39-51)
- Exercise34: Simulation duration 30 seconds (lines 40-52)
- Exercise35: Scaled-down version, demo purposes (lines 147-148, 232-234)

**Day05 (Enterprise Observability)**:
- Exercise41: Netflix simulations (lines 302, 367, 429)
- Exercise42: Latency simulation, payment retry simulation (lines 249, 321)
- Exercise43: ELK stack simulation, enterprise simulation (lines 893, 921)
- Exercise44: Simulation time comments (lines 506, 557), availability measurement (line 662)

**Day08 (Stress Testing)**:
- Exercise71: Stress testing simulation (line 36), error simulation (line 303)

**Pattern Identified**: Most exercises use `Task.Delay()` for time-based simulations instead of real operations

### Architecture Analysis

**Current Pattern (Simulation)**:
```csharp
// Simulated background service
public class RateLimitingDemoService : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Simulate work
            await Task.Delay(100, stoppingToken);
            SimulateRequest();
        }
    }
}

// Hardcoded infrastructure
private const string KafkaBootstrapServers = "localhost:9092";
```

**Target Pattern (Real Implementation)**:
```csharp
// Real Kafka integration
private static string KafkaBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
    
private static string KafkaFlinkBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";

// Real producer
using var producer = new ProducerBuilder<string, string>(new ProducerConfig
{
    BootstrapServers = KafkaBootstrapServers
}).Build();

// Real message sending
for (int i = 0; i < messageCount; i++)
{
    var message = new Message<string, string>
    {
        Key = $"key-{i}",
        Value = $"value-{i}"
    };
    await producer.ProduceAsync(topicName, message);
}

// Real Flink job submission
var flinkJob = new FlinkJobBuilder()
    .WithKafkaSource(KafkaFlinkBootstrapServers, topicName)
    .WithProcessing(/* real processing logic */)
    .WithKafkaSink(KafkaFlinkBootstrapServers, outputTopic)
    .Build();
    
await flinkJob.SubmitAsync(FlinkGatewayUrl);
```

### Lessons Learned

**Investigation Phase Insights**:
- Most simulations use `Task.Delay()` and background services
- Hardcoded addresses are pervasive (34+ files affected)
- Exercise1-StringCapitalize is the gold standard reference
- Need systematic approach: one day at a time, starting with most critical

## Phase 2: Design

### Requirements

**Design Goals**:
1. All exercises use real Kafka producers/consumers
2. All Flink operations submit real jobs to cluster
3. All Temporal operations use real workflow executions
4. All infrastructure endpoints use environment variables
5. No simulations or `Task.Delay()` for business logic
6. Maintain 3-minute test timeout compliance
7. Exercises complete and exit cleanly

### Architecture Decisions

**Migration Strategy**: Phased approach by priority

**Priority 1 (Week 1)**: Day04 - Production Backpressure
- Most critical for testing patterns
- Most simulation-heavy (5 exercises)
- Will establish patterns for other days

**Priority 2 (Week 2)**: Day05 - Enterprise Observability
- 4 exercises with observability simulations
- Need real metrics collection

**Priority 3 (Week 3)**: Day08 - Stress Testing
- 4 exercises with stress test simulations
- Need real load generation

**Lower Priority**: Day02, Day03 (simulations are comments/supporting code, not core logic)

### Why This Approach

**Rationale**:
- Day04 blocks understanding of real backpressure patterns
- Provides maximum learning value when converted
- Establishes reusable patterns for other days
- Validates infrastructure integration early

### Alternatives Considered

**Alternative 1**: Fix all days simultaneously
- **Rejected**: Too risky, hard to test, overwhelming scope

**Alternative 2**: Start with simplest exercises first
- **Rejected**: Doesn't address highest-priority learning gaps

**Alternative 3**: Keep simulations, add environment variable support only
- **Rejected**: Doesn't meet user's requirement for "real implementations"

## Phase 3: TDD/BDD

### Test Specifications

**Acceptance Criteria for Each Exercise**:
1. ✅ Exercise uses environment variables for all endpoints
2. ✅ Exercise produces real Kafka messages
3. ✅ Exercise submits real Flink jobs (if applicable)
4. ✅ Exercise completes within 3 minutes
5. ✅ Exercise exits with code 0 on success
6. ✅ Test validation passes all checks
7. ✅ No hardcoded localhost addresses remain
8. ✅ Grep search finds no "simulation" or "demo purposes" comments

**Test Strategy**:
- Run existing tests after each exercise conversion
- Verify test still passes with same validation checks
- Add new validation for real infrastructure operations
- Ensure no regressions in other exercises

### Behavior Definitions

**Given**: An exercise with simulated operations
**When**: Converted to real Kafka/Flink/Temporal operations
**Then**: 
- Exercise completes successfully
- Real messages flow through infrastructure
- Test validation passes
- No simulation code remains

## Phase 4: Implementation

### Implementation Plan

**Step 1**: Start with Day04 Exercise31 (Netflix-style backpressure)

**Changes Required**:
1. Replace simulated workload with real Kafka producer
2. Add environment variable properties for endpoints
3. Submit real Flink job to process messages
4. Remove background service pattern
5. Add completion marker and exit cleanly
6. Update comments to remove "simulation" references

**Step 2**: Repeat pattern for Exercise32-35

**Step 3**: Validate all Day04 tests pass

**Step 4**: Document pattern in update-LearningCourse.md

**Step 5**: Move to Day05 with established pattern

### Code Changes

**Example Refactoring for Exercise31**:

**Before (Simulated)**:
```csharp
// Run simulation for 10 seconds
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await host.RunAsync(cts.Token);
```

**After (Real Implementation)**:
```csharp
// Kafka endpoints from environment
private static string KafkaBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
    
private static string KafkaFlinkBootstrapServers =>
    Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";

// Produce real messages
Console.WriteLine(">> Producing test messages to Kafka...");
await ProduceTestMessagesAsync(KafkaBootstrapServers, "backpressure-test", 10000);

// Submit real Flink job
Console.WriteLine(">> Submitting Flink job...");
var jobId = await SubmitFlinkJobAsync(KafkaFlinkBootstrapServers);

// Wait for processing
Console.WriteLine(">> Waiting for job to process messages...");
await WaitForJobCompletionAsync(jobId, TimeSpan.FromMinutes(2));

// Report results
Console.WriteLine("================================================================================");
Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
Console.WriteLine("================================================================================");
Console.WriteLine("✅ Netflix-style adaptive backpressure validated");
Environment.Exit(0);
```

### Challenges Encountered

*To be filled during implementation*

### Solutions Applied

*To be filled during implementation*

## Phase 5: Testing & Validation

### Test Results

*To be filled after implementation*

### Performance Metrics

*To be filled after testing*

## Phase 6: Owner Acceptance

### Demonstration

*To be filled when ready for review*

### Owner Feedback

*To be filled after owner review*

### Final Approval

*Pending*

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well

*To be documented during implementation*

### What Could Be Improved

*To be documented during implementation*

### Key Insights for Similar Tasks

**Before Starting Similar Migrations**:
1. Search for all simulation/demo code first (`grep -r "simulation|simulate|demo purposes"`)
2. Identify gold standard reference (Exercise1-StringCapitalize)
3. Create clear priority order by learning value
4. Establish pattern with highest-priority exercise first
5. Document pattern before mass migration
6. Test incrementally, one exercise at a time

### Specific Problems to Avoid in Future

1. **Don't migrate all at once** - High risk of breaking everything
2. **Don't skip environment variable conversion** - Tests will fail without it
3. **Don't forget completion markers** - Tests rely on "COMPLETED" / "SUCCESS" output
4. **Don't leave exercises running indefinitely** - Must exit within 3 minutes
5. **Don't forget to update comments** - Remove "simulation" references

### Reference for Future WIs

When converting simulation code to real implementations:
- Always use Exercise1-StringCapitalize as reference template
- Always add environment variable properties (not const fields)
- Always include both KAFKA_BOOTSTRAP_SERVERS (host) and KAFKA_FLINK_BOOTSTRAP_SERVERS (container)
- Always make exercises console apps that complete and exit
- Always test locally before committing
- Always document why changes were needed in WI

## Current Status

**Phase**: Investigation Complete
**Next Phase**: Design (selecting Day04 Exercise31 as starting point)
**Blockers**: None
**ETA**: Week 1 - Day04 complete, Week 2 - Day05 complete, Week 3 - Day08 complete

## References

- Exercise1-StringCapitalize: Gold standard reference implementation
- update-LearningCourse.md: Aspire Service Discovery section (lines 1786-2073)
- DockerInfrastructure.cs: Port discovery utilities
- LearningCourseTestBase.cs: Test infrastructure environment variable setup