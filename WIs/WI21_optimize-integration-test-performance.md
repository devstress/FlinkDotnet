# WI21: Optimize Integration Test Performance

**File**: `WIs/WI21_optimize-integration-test-performance.md`
**Title**: Optimize Integration Test Execution Speed
**Description**: Reduce test execution time from 52s (LocalTesting) and 92s (LearningCourse) to under 30s through parallelization and container readiness optimization
**Priority**: High
**Component**: Testing Infrastructure
**Type**: Performance Enhancement
**Assignee**: AI Agent
**Created**: 2025-10-11
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI18: Logging implementation patterns
- WI19: Container IP discovery infrastructure
### Lessons Applied
- Use comprehensive logging to identify bottlenecks
- Apply systematic debugging before optimization
### Problems Prevented
- Avoid premature optimization without measurements
- Ensure changes don't break existing test functionality

## Phase 1: Investigation

### Current State
- LocalTesting: 9 tests, 52.0s total (5.8s average per test)
- LearningCourse: 3 tests, 91.9s total (30.6s average per test)
- **Target**: Both test suites under 30s total

### Debug Information (MANDATORY)

#### Test Execution Analysis Needed
1. **Container startup time**: How long until containers are actually ready?
2. **Test parallelization**: Are tests running sequentially or in parallel?
3. **Wait/polling delays**: Are there unnecessary waits?
4. **Resource contention**: Are tests competing for resources?

#### Investigation Results

**LocalTesting Analysis (9 tests, 52s):**
- ✅ Parallel execution ENABLED: `[assembly: Parallelizable(ParallelScope.All)]` with 10 workers
- ❌ Container detection polling: 2s intervals, up to 30s max
- ❌ Kafka readiness: 250ms polling intervals
- ❌ Flink readiness: 500ms polling intervals
- ✅ Uses GlobalTestInfrastructure (good - shared setup)

**LearningCourse Analysis (3 tests, 92s):**
- ❌ **NO parallel execution configured** - tests run sequentially (30.6s each)
- ❌ Inherits same slow polling from LearningCourseTestBase
- ❌ Each test waits for full infrastructure setup independently

**Root Causes Identified:**
1. **LearningCourse runs sequentially** - missing NUnit parallel configuration
2. **Over-conservative polling delays** - can be reduced by 50-75%
3. **Container detection waits 2s** - containers ready much faster with Aspire
4. **GlobalSetup container polling** - wastes 20-30s waiting unnecessarily

### Optimization Strategy

**Phase 1: Enable Parallel Execution (Target: 70% time reduction)**
- Add `AssemblyInfo.cs` to LearningCourse.IntegrationTests
- Enable `Parallelizable(ParallelScope.All)`
- Set `LevelOfParallelism(5)` - enough for 3 tests plus overhead

**Phase 2: Optimize Polling Intervals (Target: 30-50% time reduction)**
- Reduce Kafka polling: 250ms → 100ms
- Reduce Flink polling: 500ms → 200ms
- Reduce container detection: 2s → 500ms
- Add exponential backoff for early readiness

**Phase 3: Smart Container Detection (Target: 20-30s savings)**
- Check for containers every 500ms instead of 2s
- Exit immediately when containers found (don't wait full interval)
- Use Aspire's ResourceNotifications for faster detection
