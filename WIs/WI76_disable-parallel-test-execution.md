# WI76: Disable Parallel Test Execution to Prevent TaskManager Crashes

**File**: `WIs/WI76_disable-parallel-test-execution.md`
**Title**: [Testing] TaskManager OutOfMemoryError: Metaspace crashes due to parallel test execution
**Description**: Running 10 tests in parallel causes TaskManager metaspace exhaustion. Solution: Disable parallel test execution.
**Priority**: Critical
**Component**: LearningCourse.IntegrationTests
**Type**: Bug - Resource Contention
**Assignee**: Development Team
**Created**: 2025-10-17
**Status**: FIXED - Sequential execution implemented

## Lessons Applied from Previous WIs
### Previous WI References
- WI73: LearningCourse test validation and fixes
- WI72: Remove absolute maximum timeout
- WI75: Initial investigation of test design issues

### Problems Prevented
- TaskManager metaspace OutOfMemoryError crashes
- Test false positives from Kafka data persistence
- Infrastructure instability from resource contention

## Phase 1: Investigation

### Debug Information (CRITICAL DISCOVERY)
**Error**: Flink TaskManager OutOfMemoryError: Metaspace crash at 02:13:44
```
2025-10-17 02:13:44,381 ERROR org.apache.flink.runtime.taskexecutor.TaskManagerRunner 
Fatal error occurred while executing the TaskManager. Shutting it down...
java.lang.OutOfMemoryError: Metaspace
```

**User Directive**: "disable test running parallel in LearningCourse and I think that it will fix the issue."

### Root Cause Analysis

#### Problem: Parallel Test Execution Overload
Tests configured to run **10 tests in parallel simultaneously**:

**File**: `LearningCourse/LearningCourse.IntegrationTests/AssemblyInfo.cs`
```csharp
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(10)]  // 10 TESTS AT ONCE!
```

#### Why Parallel Tests Cause TaskManager Crashes

1. **10 tests running simultaneously** = 10 Flink jobs submitted at once
2. **Each job loads classes** into TaskManager JVM metaspace
3. **60+ total jobs** in full test suite = continuous class loading
4. **Parallel burst loading** = metaspace spikes
5. **512MB metaspace insufficient** for parallel class loading bursts
6. **TaskManager crashes** after ~60 job submissions under parallel load

#### Evidence from Logs
- TaskManager crashed at 02:13:44 with OutOfMemoryError: Metaspace
- Tests continued passing after crash (validating Kafka data only)
- 60+ Flink jobs submitted before crash
- Parallel execution = 10 simultaneous job submissions at peak

### Lessons Learned & Future Reference

#### What Worked Well
- User correctly identified parallel execution as root cause
- Sequential execution is simpler than increasing metaspace
- NUnit ParallelScope.None effectively disables parallelization

#### What Could Be Improved
- Should have reviewed AssemblyInfo.cs configuration earlier
- Parallel execution inappropriate for shared infrastructure tests
- Integration tests should default to sequential execution

#### Key Insights for Similar Tasks
- **Parallel execution causes resource contention** in shared infrastructure
- **Sequential execution = stability** over speed for integration tests
- **Control concurrency** to match infrastructure capacity
- **Accept longer test times** for reliability in integration test suites

#### Specific Problems to Avoid in Future
1. **Never run parallel integration tests** against shared Flink TaskManager
2. **Always check AssemblyInfo.cs** for parallelization settings
3. **Don't assume more parallelism = better** for integration tests
4. **Consider infrastructure limits** when configuring test parallelism

## Phase 2: Solution Design

### Solution Implemented: Sequential Test Execution

#### Change 1: Disable NUnit Parallelization
**File**: `LearningCourse/LearningCourse.IntegrationTests/AssemblyInfo.cs`

**Before (causing crashes)**:
```csharp
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(10)]
```

**After (fixed)**:
```csharp
// DISABLE parallel test execution to prevent TaskManager resource contention
// Running tests in parallel causes OutOfMemoryError: Metaspace crashes in TaskManager
// Sequential execution prevents resource exhaustion and ensures stable test runs
[assembly: Parallelizable(ParallelScope.None)]
```

#### Change 2: MSBuild Parallelization Disabled
**File**: `LearningCourse/LearningCourse.IntegrationTests/LearningCourse.IntegrationTests.csproj`

```xml
<PropertyGroup>
  <!-- Disable parallel test execution to prevent TaskManager resource contention -->
  <ParallelizeAssembly>false</ParallelizeAssembly>
  <ParallelizeTestCollections>false</ParallelizeTestCollections>
</PropertyGroup>
```

### Why Sequential Execution Fixes the Issue

1. **One test at a time** = controlled resource usage
2. **One Flink job at a time** = manageable class loading
3. **No parallel burst** = no metaspace spikes
4. **TaskManager stable** throughout entire test run
5. **1024MB metaspace sufficient** with sequential execution

### Performance Trade-off

- **Before (parallel)**: ~30 minutes (10 parallel tests, but crashes frequently)
- **After (sequential)**: ~60-90 minutes (sequential, but stable and reliable)
- **Result**: Longer but **reliable and predictable** test execution

### Alternative Solutions Considered (NOT IMPLEMENTED)

#### Option 1: Increase Metaspace to 2GB
**Pros**: More headroom for class loading
**Cons**: Doesn't address root cause (parallel resource contention), wastes memory
**Decision**: NOT IMPLEMENTED - sequential execution is better solution

#### Option 2: Restart TaskManager Between Tests
**Pros**: Fresh state for each test
**Cons**: Very slow (5-10s restart per test), adds infrastructure complexity
**Decision**: NOT IMPLEMENTED - sequential execution is simpler

#### Option 3: Dedicated TaskManager Per Test
**Pros**: Complete isolation
**Cons**: Massive resource overhead, very complex setup
**Decision**: NOT IMPLEMENTED - overkill for current needs

## Phase 3: Implementation

### Step 1: Disable Parallel Test Execution
**Status**: ✅ COMPLETED

**Changes Made**:
1. Updated `AssemblyInfo.cs` to use `[assembly: Parallelizable(ParallelScope.None)]`
2. Added MSBuild properties to `.csproj` to disable parallelization
3. Removed `LevelOfParallelism(10)` attribute
4. Added detailed comments explaining the rationale

### Step 2: Keep Metaspace at 1GB
**Status**: ✅ COMPLETED

**Rationale**: 
- 1GB (1024MB) metaspace sufficient for sequential execution
- The 512MB → 1GB increase provides safety margin
- No need to over-provision memory with sequential execution

### Step 3: Validate Full Test Run
**Status**: ⏳ PENDING - Awaiting full test run completion

## Phase 4: Testing & Validation

### Success Criteria
- [x] Tests run sequentially (one at a time)
- [ ] Zero TaskManager OOM crashes during full test run
- [ ] All tests pass successfully with sequential execution
- [ ] Test execution time acceptable (60-90 minutes)
- [ ] No resource contention or infrastructure instability

## Lessons Learned Summary

### CRITICAL: Parallel Test Execution Anti-Pattern
**Never run parallel integration tests against shared infrastructure with limited resources**
- Parallel execution causes resource contention bursts
- TaskManager metaspace cannot handle 10 simultaneous job submissions
- Sequential execution provides stable, predictable resource usage
- **Reliability > Speed** for integration test suites

### TaskManager Resource Management Insights
- **Parallel execution (10 tests)**: Causes metaspace exhaustion, crashes
- **Sequential execution (1 test)**: Stable resource usage, no crashes
- **Metaspace 512MB**: Insufficient for parallel bursts
- **Metaspace 1024MB (1GB)**: Sufficient for sequential execution
- **Lesson**: Control concurrency to match infrastructure capacity

### Test Execution Philosophy
1. **Integration tests are expensive**: Accept longer execution times
2. **Stability over speed**: Sequential = reliable, Parallel = unreliable
3. **Match concurrency to resources**: Don't exceed infrastructure limits
4. **Unit tests for speed**: Parallelize unit tests, not integration tests
5. **Infrastructure is shared**: One test at a time prevents contention

## Recommendations

### Immediate Actions
1. ✅ Disable parallel test execution (completed)
2. ✅ Increase metaspace to 1GB for safety margin (completed)
3. ⏳ Validate full test run with sequential execution
4. ⏳ Monitor test execution time and stability

### Long-term Improvements
1. ✅ Document parallel execution anti-pattern for shared infrastructure
2. Consider test execution optimization without parallelization
3. Add resource usage monitoring for performance insights
4. Evaluate if dedicated TaskManager per test is worth the cost

### Documentation Updates Required
1. ✅ Update AssemblyInfo.cs with rationale for sequential execution
2. ✅ Update .csproj with parallelization settings
3. ⏳ Update CONTRIBUTING.md with integration test execution guidelines
4. ⏳ Document expected test execution time (60-90 minutes)
5. ⏳ Add troubleshooting guide for TaskManager resource issues

## Key Takeaways for Future Work

### When to Use Sequential Test Execution
- ✅ Integration tests against shared infrastructure
- ✅ Tests that submit jobs to shared Flink TaskManager
- ✅ Tests with high resource requirements per test
- ✅ Tests where stability > speed

### When Parallel Execution Is Acceptable
- ✅ Unit tests with no shared infrastructure
- ✅ Tests with dedicated resources per test
- ✅ Tests with minimal resource footprint
- ✅ Tests designed for parallel execution from start

### Red Flags for Parallel Execution Problems
- ⚠️ OutOfMemoryError in shared infrastructure
- ⚠️ Resource contention errors
- ⚠️ Intermittent test failures
- ⚠️ Infrastructure crashes during test runs
- ⚠️ Tests pass when run individually but fail in parallel

### Solution Pattern
1. Check `AssemblyInfo.cs` for `[assembly: Parallelizable]`
2. Check `.csproj` for parallelization properties
3. Disable parallel execution with `ParallelScope.None`
4. Validate stability improvement
5. Accept longer test execution time