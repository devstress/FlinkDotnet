# Temporal Integration Testing TODO

## Overview
Temporal integration validation needs to be added to NativeFlinkDotnetTesting project to provide comprehensive end-to-end testing of the Temporal workflow orchestration.

## Background
- **Reason for Separation**: Temporal `WorkflowEnvironment.StartTimeSkippingAsync()` takes 15+ seconds per test to initialize, making it unsuitable for fast CI unit tests
- **Current Status**: Temporal production code is complete and functional but excluded from FlinkDotNet.sln unit test coverage
- **Coverage Exclusion**: Temporal code excluded from coverage reporting via `coverlet.runsettings`

## Required Tests in NativeFlinkDotnetTesting

### 1. TemporalWorkerService Tests
- [ ] Worker lifecycle management (start, stop, graceful shutdown)
- [ ] Workflow registration on task queue
- [ ] Activity registration with dependency injection
- [ ] Integration with ASP.NET Core IHostedService

### 2. FlinkJobWorkflow Tests
- [ ] Simple job execution end-to-end
- [ ] Multi-vertex execution graph creation
- [ ] Job cancellation via signals (CancelJobSignalAsync)
- [ ] State queries (GetJobState, GetTaskStates)
- [ ] Workflow timeout handling (24-hour timeout)
- [ ] Error handling and retry logic

### 3. TaskExecutionActivity Tests
- [ ] Resource allocation via IResourceManager.AllocateSlotsAsync()
- [ ] Task deployment descriptor creation
- [ ] Multi-phase execution (DEPLOYING → RUNNING → FINISHED)
- [ ] Heartbeat monitoring (30-second intervals)
- [ ] Activity timeout handling (30-minute timeout)
- [ ] Exponential backoff retry (max 5 attempts)
- [ ] Metrics collection (records/bytes processed)

### 4. Dispatcher Temporal Integration Tests
- [ ] Workflow startup on job submission via REST API
- [ ] WorkflowHandle storage in JobInfo
- [ ] Signal-based job cancellation
- [ ] Query-based task state retrieval
- [ ] Long-running job support validation

### 5. End-to-End Integration Tests
- [ ] Full job lifecycle: Submit → Execute → Monitor → Complete
- [ ] Job cancellation during execution
- [ ] Resource allocation and slot management
- [ ] State persistence across JobManager restarts
- [ ] Automatic retry on transient failures
- [ ] Multiple concurrent jobs

## Test Infrastructure Requirements

### Dependencies
- `Temporalio` (>= 1.9.0) - Temporal .NET SDK
- `Temporalio.Testing` (>= 1.9.0) - Time-skipping test environment
- `xUnit` - Test framework
- `Moq` - Mocking framework (if needed for dependencies)

### Test Environment Setup
```csharp
// Use Temporalio.Testing for fast test execution
var env = await WorkflowEnvironment.StartTimeSkippingAsync();
var client = env.Client;

// Configure test worker
var worker = new TemporalWorker(
    client,
    new TemporalWorkerOptions("test-task-queue")
        .AddWorkflow<FlinkJobWorkflow>()
        .AddAllActivities(new TaskExecutionActivity(/* test dependencies */))
);
```

### Performance Target
- Individual test execution: < 1 second (excluding WorkflowEnvironment initialization)
- Total test suite: < 5 minutes
- Separate from fast FlinkDotNet.sln unit tests (15 seconds)

## Implementation Priority
1. **High**: Basic workflow execution and activity calls
2. **High**: Dispatcher integration and job lifecycle
3. **Medium**: Error handling and retry logic
4. **Medium**: Signals and queries
5. **Low**: Advanced fault tolerance scenarios

## Success Criteria
- [ ] All critical Temporal integration paths covered
- [ ] Tests validate production code behavior
- [ ] Tests run in separate CI workflow (not blocking unit tests)
- [ ] Comprehensive documentation for test scenarios
- [ ] No false positives or flaky tests

## Notes
- Tests should use real Temporal WorkflowEnvironment for accurate validation
- Mock external dependencies (HTTP clients, databases) for isolation
- Use time-skipping features to speed up workflow delays
- Document any Temporal SDK limitations or workarounds

## Related Files
- Production Code:
  - `FlinkDotNet.JobManager/Services/TemporalWorkerService.cs`
  - `FlinkDotNet.JobManager/Workflows/FlinkJobWorkflow.cs`
  - `FlinkDotNet.JobManager/Activities/TaskExecutionActivity.cs`
  - `FlinkDotNet.JobManager/Implementation/Dispatcher.cs`
- Coverage Exclusion:
  - `FlinkDotNet/coverlet.runsettings`
