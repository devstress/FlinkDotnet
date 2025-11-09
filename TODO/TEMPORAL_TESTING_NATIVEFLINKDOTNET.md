# Temporal Integration Testing in NativeFlinkDotnetTesting

## Overview
Comprehensive Temporal workflow and activity testing has been moved to the NativeFlinkDotnetTesting project to avoid slow WorkflowEnvironment initialization (15+ seconds per test) in the main unit test suite.

## Temporal Code Excluded from Coverage
The following Temporal-related code is excluded from code coverage in `coverlet.runsettings`:
- `[FlinkDotNet.JobManager]*Temporal*` - All classes/methods with "Temporal" in name
- `[FlinkDotNet.JobManager]*.Activities.*` - TaskExecutionActivity namespace
- `[FlinkDotNet.JobManager]*.Workflows.*` - FlinkJobWorkflow namespace
- `[FlinkDotNet.JobManager]*.Services.TemporalWorkerService` - Worker service

## Required Test Coverage in NativeFlinkDotnetTesting

### 1. FlinkJobWorkflow Tests (8 tests minimum)
**File**: `NativeFlinkDotnetTesting/NativeFlinkDotnet.IntegrationTests/TemporalWorkflowTests.cs`

#### Test Cases:
1. **SimpleJobExecution_CompletesSuccessfully**
   - Validates basic workflow execution
   - Tests RequestResourcesAsync → DeployTasksAsync → MonitorTaskExecutionAsync flow
   - Verifies JobExecutionResult with successful state

2. **MultiVertex_ExecutionGraph_CreatesCorrectTasks**
   - Tests job graph with multiple vertices
   - Validates task deployment descriptors
   - Confirms parallel execution of independent tasks

3. **JobCancellation_ViaSignal_StopsExecution**
   - Tests `CancelJobSignalAsync()` signal handling
   - Validates workflow cancellation propagates to activities
   - Confirms resources are released properly

4. **GetJobState_Query_ReturnsCurrentState**
   - Tests workflow query `GetJobState()`
   - Validates state transitions (INITIALIZING → DEPLOYING → RUNNING → FINISHED)
   - Confirms state accuracy during execution

5. **GetTaskStates_Query_ReturnsAllTaskStates**
   - Tests workflow query `GetTaskStates()`
   - Validates individual task state tracking
   - Confirms dictionary contains all task IDs with correct states

6. **RetryPolicy_OnActivityFailure_RetriesWithBackoff**
   - Tests exponential backoff retry policy
   - Validates max retry attempts (3 for resources, 5 for execution)
   - Confirms backoff coefficient (2.0) is applied

7. **HeartbeatMonitoring_LongRunningTask_SendsHeartbeats**
   - Tests 30-second heartbeat intervals
   - Validates heartbeat timeout detection
   - Confirms task state and metrics in heartbeat data

8. **WorkflowTimeout_24Hours_AllowsLongRunningJobs**
   - Tests workflow timeout configuration
   - Validates jobs can run for extended periods
   - Confirms timeout is properly enforced

### 2. TaskExecutionActivity Tests (6 tests minimum)
**File**: `NativeFlinkDotnetTesting/NativeFlinkDotnet.IntegrationTests/TemporalActivityTests.cs`

#### Test Cases:
1. **RequestTaskSlotsAsync_AllocatesSlots_ViaResourceManager**
   - Tests integration with IResourceManager
   - Validates slot allocation count matches request
   - Confirms TaskSlot objects are properly created

2. **ExecuteTaskAsync_MultiPhase_ProgressesThroughStates**
   - Tests DEPLOYING → RUNNING → FINISHED state progression
   - Validates heartbeat reporting during execution
   - Confirms execution metrics (records/bytes processed)

3. **ExecuteTaskAsync_WithHeartbeat_ReportsProgress**
   - Tests heartbeat monitoring (30-second intervals)
   - Validates progress tracking and metrics
   - Confirms ActivityContext.RecordHeartbeatAsync() is called

4. **CancelTaskAsync_StopsExecution_Gracefully**
   - Tests task cancellation handling
   - Validates cancellation token propagation
   - Confirms cleanup operations are performed

5. **RetryPolicy_OnTransientFailure_RetriesAutomatically**
   - Tests activity-level retry configuration
   - Validates exponential backoff behavior
   - Confirms max attempts are respected

6. **ActivityTimeout_30Minutes_EnforcedCorrectly**
   - Tests StartToCloseTimeout configuration
   - Validates timeout detection and handling
   - Confirms proper error reporting on timeout

### 3. TemporalWorkerService Tests (4 tests minimum)
**File**: `NativeFlinkDotnetTesting/NativeFlinkDotnet.IntegrationTests/TemporalWorkerTests.cs`

#### Test Cases:
1. **StartAsync_RegistersWorkflowsAndActivities**
   - Tests IHostedService.StartAsync() initialization
   - Validates workflow registration on "flink-job-queue"
   - Confirms activity registration with dependencies

2. **StopAsync_GracefulShutdown_CompletesWithin30Seconds**
   - Tests IHostedService.StopAsync() cleanup
   - Validates 30-second shutdown timeout
   - Confirms worker disposes properly

3. **DependencyInjection_InjectsRequiredServices**
   - Tests IHttpClientFactory injection
   - Validates IResourceManager injection
   - Confirms ILogger injection

4. **WorkerFault_RestartBehavior_RecoversProperly**
   - Tests worker recovery on transient failures
   - Validates workflow and activity re-registration
   - Confirms state recovery mechanisms

### 4. Dispatcher Temporal Integration Tests (5 tests minimum)
**File**: `NativeFlinkDotnetTesting/NativeFlinkDotnet.IntegrationTests/DispatcherTemporalTests.cs`

#### Test Cases:
1. **SubmitJobAsync_StartsTemporalWorkflow**
   - Tests workflow startup on job submission
   - Validates workflow ID format (`flink-job-{jobId}`)
   - Confirms WorkflowHandle storage in JobInfo

2. **CancelJobAsync_SendsWorkflowSignal**
   - Tests signal-based cancellation
   - Validates `CancelJobSignalAsync()` is sent
   - Confirms job state updates after cancellation

3. **GetJobStatus_QueriesWorkflow_ReturnsTaskStates**
   - Tests `GetTaskStates()` workflow query
   - Validates real-time job state retrieval
   - Confirms task state dictionary accuracy

4. **WorkflowTimeout_24Hours_ConfiguredCorrectly**
   - Tests workflow timeout configuration
   - Validates long-running job support
   - Confirms timeout enforcement

5. **WorkflowHandle_StoredInJobInfo_EnablesQueriesAndSignals**
   - Tests WorkflowHandle<FlinkJobWorkflow, JobExecutionResult> storage
   - Validates handle usage for queries
   - Confirms handle usage for signals

## Test Infrastructure Setup

### Required NuGet Packages:
```xml
<PackageReference Include="Temporalio" Version="1.9.0" />
<PackageReference Include="Temporalio.Testing" Version="1.9.0" />
<PackageReference Include="xunit" Version="2.9.2" />
<PackageReference Include="Moq" Version="4.20.70" />
```

### Test Base Class:
```csharp
public class TemporalTestBase : IAsyncLifetime
{
    protected WorkflowEnvironment WorkflowEnvironment { get; private set; } = null!;
    protected ITemporalClient TemporalClient { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        // Start time-skipping Temporal environment
        WorkflowEnvironment = await WorkflowEnvironment.StartTimeSkippingAsync();
        TemporalClient = WorkflowEnvironment.Client;
    }

    public async Task DisposeAsync()
    {
        await WorkflowEnvironment.ShutdownAsync();
    }
}
```

### Configuration:
```csharp
// Set workflow delays to 1ms for fast test execution
FlinkJobWorkflow.TaskMonitoringDelay = TimeSpan.FromMilliseconds(1);
```

## Implementation Priority
1. **Phase 1**: FlinkJobWorkflow tests (8 tests) - Core workflow behavior
2. **Phase 2**: TaskExecutionActivity tests (6 tests) - Activity integration
3. **Phase 3**: Dispatcher integration tests (5 tests) - End-to-end workflow startup
4. **Phase 4**: TemporalWorkerService tests (4 tests) - Worker lifecycle

**Total**: 23 comprehensive Temporal integration tests

## Success Criteria
- All 23 tests passing
- Workflow Environment initialization overhead acceptable (tests run separately from unit tests)
- Complete coverage of Temporal integration points
- Real WorkflowEnvironment used (not mocked) to validate actual Temporal behavior
- Time-skipping test environment for fast execution of workflow delays

## Timeline
- Target completion: Phase 5 implementation cycle
- Estimated effort: 2-3 days for comprehensive test suite
- Dependency: NativeFlinkDotnetTesting project setup

## Notes
- Tests use real Temporal WorkflowEnvironment to validate integration
- Time-skipping allows fast test execution despite workflow delays
- These tests complement the unit tests in FlinkDotNet.sln which focus on business logic without Temporal overhead
- Code coverage for Temporal code is tracked separately through these integration tests
