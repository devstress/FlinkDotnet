# WI10: Fix Integration Test Failures and Re-enable Container Cleanup

**File**: `LocalTesting/WIs/WI10_fix-integration-test-failures.md`
**Title**: Fix 8/9 failing integration tests and re-enable proper teardown
**Description**: Debug and fix root cause of integration test failures, then re-enable AppHost cleanup
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI9_integration-test-failures.md - Similar test failures investigation

### Lessons Applied  
- Debug-first approach before proposing solutions
- Run tests locally to reproduce issues
- Check infrastructure and environment compatibility
- Document all debug findings for future reference

### Problems Prevented
- Skipping proper debugging and going straight to code changes
- Not understanding the actual failure patterns
- Making assumptions without evidence

## Phase 1: Investigation

### Requirements
- Understand why 8/9 integration tests are failing
- Debug the root cause using Docker containers
- Fix all failing tests
- Re-enable AppHost.StopAsync() and AppHost.DisposeAsync() in GlobalTearDown()
- Ensure tests pass reliably

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement**:
- User reports: "8/9 integration tests fail"
- User disabled AppHost.StopAsync() and AppHost.DisposeAsync() in GlobalTearDown() to keep containers alive
- Need to debug using live containers, fix issues, then re-enable cleanup

**Environment**:
- Docker: 28.0.4 ✅ Installed
- .NET: 9.0.305 ✅ Installed
- Build: Both FlinkDotNet and LocalTesting solutions build successfully ✅

**Current GlobalTearDown State**:
- Container teardown is disabled (lines 137-138 in GlobalTestInfrastructure.cs)
- Containers remain running for debugging

**Next Steps**:
1. Run integration tests to see actual failures
2. Examine container logs and status
3. Identify root cause
4. Apply fixes
5. Verify all tests pass
6. Re-enable AppHost cleanup

### Findings

**Test Results - 8/9 Failed**:
```
Failed: 8 tests
- Gateway_Pattern1_Uppercase_ShouldWork ❌
- Gateway_Pattern2_Filter_ShouldWork ❌
- Gateway_Pattern3_SplitConcat_ShouldWork ❌
- Gateway_Pattern4_Timer_ShouldWork ❌
- Gateway_Pattern5_DirectFlinkSQL_ShouldWork ❌
- Gateway_Pattern6_SqlTransform_ShouldWork ❌
- Gateway_Pattern7_Composite_ShouldWork ❌
- (One more likely Native Flink test)

Passed: 1 test (unknown which one)
```

**Failure Pattern**:
1. ✅ Infrastructure starts: Kafka, Flink, Gateway all healthy
2. ✅ Jobs submit successfully: "Job submission: success=True"
3. ✅ Jobs reach RUNNING state: "Job is RUNNING"
4. ✅ Messages produced to input topics: "✅ Produced X messages"
5. ❌ **NO messages consumed from output topics**: "📊 Consumed 0 messages (expected: X)"

**Root Cause Analysis**:
- Jobs are running but not processing messages from input to output
- Kafka connectivity issue suspected - jobs can't read from input topics
- Checked FlinkJobRunner.java line 93 & 241: Default bootstrap = "kafka:9093" (WRONG!)
- **Should be "kafka:9092" for internal Flink container communication**
- Tests explicitly pass "kafka:9092" but there may be an override happening

**Key Evidence from FlinkJobRunner.java**:
```java
Line 93:  String bootstrap = orElse(k.bootstrapServers, System.getenv("KAFKA_BOOTSTRAP"), "kafka:9093");
Line 241: String bootstrap = orElse(s.bootstrapServers, ..., System.getenv("KAFKA_BOOTSTRAP"), "kafka:9093");
```

The default "kafka:9093" is incorrect - should be "kafka:9092"

**Containers After Test Run**:
- All containers were torn down (AppHost dispose was actually called despite being "disabled")
- Cannot examine live containers for debugging

### Lessons Learned
- Default Kafka port in FlinkJobRunner.java is incorrect (9093 vs 9092)
- AppHost teardown happened despite being commented out in GlobalTearDown
- Need to prevent actual teardown to debug live containers

## Phase 2: Design
(To be completed after root cause is identified)

## Phase 3: TDD/BDD
(To be completed after design)

## Phase 4: Implementation
(To be completed after test design)

## Phase 5: Testing & Validation
(To be completed after implementation)

## Phase 6: Owner Acceptance
(To be completed after validation)

## Lessons Learned & Future Reference (MANDATORY)
(To be completed at end of WI)
