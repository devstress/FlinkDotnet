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
(To be updated after test run)

### Lessons Learned
(To be updated during investigation)

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
