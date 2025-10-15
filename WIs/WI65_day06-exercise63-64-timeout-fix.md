# WI65: Fix Exercise63 & Exercise64 Temporal Test Timeouts

**File**: `WIs/WI65_day06-exercise63-64-timeout-fix.md`
**Title**: Day06 - Fix Exercise63 & Exercise64 timeout issues
**Description**: Exercise63 and Exercise64 timeout after 3 minutes with no output during test execution
**Priority**: High
**Component**: LearningCourse/Day06-Temporal-Workflows
**Type**: Bug Fix
**Created**: 2025-10-15
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI63: Day06 Temporal workflows full integration - established namespace verification pattern
### Lessons Applied
- Use namespace verification in retry logic (`DescribeNamespaceAsync`)
- Implement exponential backoff for Temporal connections (10 retries, 500ms base delay)
- Applied fixes successfully to Exercise61 and Exercise62
### Problems Prevented
- Avoided "Namespace default is not found" errors by verifying namespace before returning client

## Phase 1: Investigation
### Requirements
- Identify why Exercise63 and Exercise64 timeout while Exercise61 and Exercise62 pass
- Understand differences in workflow execution patterns
- Determine root cause of zero output during test execution

### Debug Information (MANDATORY)
- **Error Messages**: `System.TimeoutException: Exercise Day06-Temporal-Workflows/Exercise-Solutions/Exercise63 timed out after 00:03:00`
- **Test Results**: 
  - Exercise61 ✅ PASS (completed in ~3-4 seconds)
  - Exercise62 ✅ PASS (completed in ~3-4 seconds) 
  - Exercise63 ❌ TIMEOUT (3 minutes, zero output)
  - Exercise64 ❌ TIMEOUT (3 minutes, zero output)
- **Manual Execution**: Exercise63 runs successfully outside test harness, produces diagnostic output
- **System State**:
  - Temporal endpoint discovered: `localhost:46623` (dynamic port via Aspire)
  - Namespace 'default' verified as ready during infrastructure setup
  - All 4 tests run in parallel (NUnit default behavior)
- **Reproduction Steps**:
  1. Run `dotnet test LearningCourse/IntegrationTests.sln --filter "Day06Tests"`
  2. Exercise61 and Exercise62 complete quickly
  3. Exercise63 and Exercise64 hang for full 3-minute timeout
  4. No output captured from Exercise63/64 (not even diagnostic messages at program start)
- **Evidence**:
  - Manual run shows: Exercise63 outputs `[DIAGNOSTIC] Exercise63 starting` immediately
  - Test run shows: NO output at all from Exercise63/64, suggesting they never produce output OR test infrastructure kills them before capturing output
  - Test infrastructure uses `process.WaitForExit(timeoutMilliseconds)` which is non-async
  - Output/error streams are read with `ReadToEndAsync()` but process is killed if timeout expires before completion

### Findings
#### Key Discovery 1: Test Infrastructure Output Capture Issue
- `LearningCourseTestBase.ExecuteExerciseAsync()` (lines 971-978) uses blocking `WaitForExit(timeout)`
- If process times out, it's killed at line 977 before output tasks complete
- Exercise63/64 ARE producing output, but test kills them before retrieving it

#### Key Discovery 2: All Exercises Use Same Pattern
- All 4 exercises use identical `worker.ExecuteAsync(async () => { ... })` pattern
- Exercise61/62: Complete workflow processing and exit lambda function
- Exercise63: Processes 3 booking scenarios (success + 2 failures with compensation)
- Exercise64: Starts workflow, sends signals/queries, waits for completion

#### Key Discovery 3: worker.ExecuteAsync() Lifecycle
- `worker.ExecuteAsync()` blocks until the lambda completes
- The lambda must complete for the worker to shut down gracefully
- If workflows don't complete or signals aren't processed, lambda hangs indefinitely

#### Root Cause Hypothesis
Exercise63 and Exercise64's workflows may not be completing due to:
1. **Workflow execution deadlock**: Worker waiting for workflows that never complete
2. **Signal/query processing issue**: Exercise64's signals might not be reaching the workflow
3. **Activity execution timeout**: Activities timing out and workflow stuck retrying
4. **Temporal worker shutdown issue**: Worker not releasing after workflows complete

### Lessons Learned
- Test infrastructure needs async-friendly process management to capture output during timeouts
- Temporal worker lifecycle requires careful management to ensure clean shutdown
- Parallel test execution can cause resource contention with long-running Temporal workflows

## Phase 2: Design
### Requirements
- Fix Exercise63 and Exercise64 to complete within reasonable timeframe (<30 seconds)
- Ensure proper worker lifecycle management
- Add explicit timeout handling to prevent indefinite hangs

### Architecture Decisions
**Option 1: Add CancellationToken Support** ✅ CHOSEN
- Add `CancellationTokenSource` with timeout to worker.ExecuteAsync()
- Ensures worker shuts down even if workflows hang
- Clean, explicit control over execution lifetime

**Option 2: Reduce Workflow Timeouts**
- Reduce Activity timeouts from 30s to 5s
- Risk: May cause legitimate activities to timeout
- Doesn't address root cause of hanging

**Option 3: Sequential Test Execution**
- Add `[NonParallelizable]` attribute to Day06Tests
- Prevents resource contention between tests
- Doesn't fix the actual timeout issue

### Why Option 1
- Explicit control over worker lifetime prevents indefinite hangs
- Allows workflows to complete naturally but enforces maximum execution time
- Standard pattern for long-running operations in .NET
- Clean shutdown even if workflow logic has issues

## Phase 3: TDD/BDD
### Test Specifications
- All 4 Day06 tests must pass within 3-minute timeout
- Exercise63 should complete 3 workflow scenarios (1 success, 2 compensations)
- Exercise64 should complete signal/query demonstration workflow
- No test should hang indefinitely

## Phase 4: Implementation
### Code Changes

#### Change 1: Add CancellationToken to Exercise63 worker.ExecuteAsync()
**File**: `LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise63/Program.cs`
**Lines**: 95-181
**Change**: Wrap worker.ExecuteAsync() with CancellationTokenSource (60-second timeout)

```csharp
// Start worker with cancellation token (60-second timeout)
using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
await worker.ExecuteAsync(async () =>
{
    // ... existing workflow execution code ...
}, cts.Token);
```

#### Change 2: Add CancellationToken to Exercise64 worker.ExecuteAsync()
**File**: `LearningCourse/Day06-Temporal-Workflows/Exercise-Solutions/Exercise64/Program.cs`
**Lines**: 100-193
**Change**: Same as Exercise63 - add CancellationTokenSource

### Challenges Encountered
- Need to balance timeout value: long enough for legitimate workflows, short enough to prevent test hangs
- Must ensure cancellation doesn't corrupt workflow state

## Phase 5: Testing & Validation
### Test Results
- Pending implementation

## Phase 6: Owner Acceptance
### Demonstration
- Pending

## Phase 7: Work Item Closure
### Requirements
- All 4 Day06 tests pass consistently
- No timeouts during test execution
- Proper error handling and logging

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Namespace verification pattern successfully fixed Exercise61 and Exercise62
- Manual execution testing helped identify that exercises DO work outside test harness

### What Could Be Improved
- Test infrastructure should use async-friendly process management
- Consider adding timeout guards to all worker.ExecuteAsync() calls

### Key Insights for Similar Tasks
- Temporal worker.ExecuteAsync() blocks until lambda completes - ensure lambda has exit condition
- Always add explicit timeouts to long-running operations
- Test parallel execution scenarios to catch resource contention issues

### Specific Problems to Avoid in Future
- Don't assume worker.ExecuteAsync() will terminate automatically
- Always add cancellation token support for blocking operations in test scenarios
- Verify workflow completion logic to ensure lambda function exits

### Reference for Future WIs
- When implementing Temporal workflows in test scenarios, always add cancellation token support
- Balance timeout values between legitimate execution time and failure detection
- Consider sequential test execution for resource-intensive integration tests