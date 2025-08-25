# WI76: Fix LocalTesting Workflow Failure

**File**: `WIs/WI76_fix-localtesting-workflow-failure.md`
**Title**: [LocalTesting] Fix GitHub workflow failure - ComplexLogicStressTestService test-id not found error  
**Description**: LocalTesting GitHub workflow fails at Step 4 (produce-messages) with "ArgumentException: Test test-id not found" in ComplexLogicStressTestService.ProduceMessagesAsync(). This indicates missing test initialization in the API endpoints.
**Priority**: High
**Component**: LocalTesting.WebApi
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-02
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI30 (local-testing-workflow-fix): Previously identified 500 error with same root cause
- WI5 (fix-localtesting-api-startup-timeout): Resolved API startup timeout issues
- WI33 (localtesting-workflow-fixes): Addressed container startup issues
- WI2 (fix-localtesting-workflow-dotnet9-upgrade): Resolved .NET 9 environment setup

### Lessons Applied  
- Follow mandatory debugging first approach before implementing solutions
- Use systematic investigation methodology from previous WIs
- Install .NET 9.0 environment as prerequisite for LocalTesting functionality
- Debug API endpoints that manage test state/initialization

### Problems Prevented
- Avoided making changes without proper root cause analysis
- Prevented environment compatibility issues by ensuring .NET 9.0 setup
- Prevented skipping the mandatory debugging phase

## Phase 1: Investigation
### Requirements
- Fix LocalTesting GitHub workflow failure at business flow execution step
- Resolve "ArgumentException: Test test-id not found" error in ComplexLogicStressTestService
- Ensure proper test initialization in API workflow execution
- Maintain Aspire orchestration architecture and business flow functionality

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  Step 4: Producing messages with correlation IDs...
  ❌ Business flow test failed: Response status code does not indicate success: 500 (Internal Server Error).
  ArgumentException: Test test-id not found in ComplexLogicStressTestService.ProduceMessagesAsync()
  ```
- **Log Locations**: 
  - GitHub Actions workflow logs: `.github/workflows/local-testing.yml` (Step 4 failure)
  - LocalTesting API logs: ComplexLogicStressTestService component
  - Previous WI30 investigation confirmed same root cause
- **System State**: 
  - .NET 9.0.304 installed and Aspire workload configured
  - Environment setup meets project requirements
  - API service starts successfully but test initialization missing
- **Reproduction Steps**: 
  1. Run LocalTesting GitHub workflow
  2. Environment setup passes (infrastructure containers start)
  3. API accessibility validation passes
  4. Step 4 message production fails with test-id not found error
- **Evidence**: 
  - ComplexLogicStressTestService.ProduceMessagesAsync() expects test to exist in _activeTests
  - Controller creates new testId but never initializes test status in service
  - Missing test initialization causes ArgumentException which becomes 500 error
  - Need to investigate API endpoint flow for test setup

### Findings
**Root Cause Analysis Complete**:

1. **GitHub Workflow Call**: The LocalTesting workflow calls `step4/flink-concat-job` endpoint (not `step4/produce-messages`)
2. **Endpoint Implementation**: `step4/flink-concat-job` calls `_flinkJobService.StartComplexLogicJobAsync()` but doesn't initialize test state
3. **Error Source**: There are 3 methods in ComplexLogicStressTestService that throw "Test {testId} not found":
   - `StartFlinkJobAsync()` (line 124)
   - `ProcessBatchesAsync()` (line 138) 
   - `VerifyMessagesAsync()` (line 200)
4. **Test State Issue**: These methods expect test to exist in `_activeTests` but don't create it like `ProduceMessagesAsync()` does
5. **Workflow Flow Problem**: Steps 2-3 create test state, but Step 4 expects testId to match previous steps without explicit test creation
6. **Missing Integration**: Step 4 doesn't receive or use testId from previous steps, creating disconnected test execution

**Specific Issue**: The workflow is calling endpoints in sequence but testId is not being passed between steps consistently, causing Step 4 to fail when it tries to access test state that doesn't exist.

### Lessons Learned
[To be updated after investigation completion]

## Phase 2: Design  
[To be completed after investigation]

## Phase 3: TDD/BDD
[To be completed after design]

## Phase 4: Implementation
[To be completed after test design]

## Phase 5: Testing & Validation
[To be completed after implementation]

## Phase 6: Owner Acceptance
[To be completed after validation]

## Lessons Learned & Future Reference (MANDATORY)
[To be completed at end of WI]