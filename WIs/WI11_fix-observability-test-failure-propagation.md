# WI11: Fix Observability Test Failure Propagation and OpenTelemetry Collector Connection Issues

**File**: `WIs/WI11_fix-observability-test-failure-propagation.md`
**Title**: [ObservabilityTest] Fix test failure propagation and connection reset by peer errors  
**Description**: The observability test must fail when infrastructure fails, but currently creates results file even when validation fails, causing GitHub workflow to pass incorrectly. Also fix OpenTelemetry collector connection "reset by peer" errors.
**Priority**: High
**Component**: LocalTesting.IntegrationTests + OpenTelemetry Collector  
**Type**: Bug Fix
**Assignee**: copilot
**Created**: 2024-12-28
**Status**: Testing

## Lessons Applied from Previous WIs
### Previous WI References
- WI10: Kafka producer performance optimization - learned about proper validation and build testing

### Lessons Applied  
- ALWAYS debug first to find root cause during Investigation phase
- Validate builds and tests locally before making changes
- Create minimal, surgical fixes to address specific issues
- Document all error conditions and solutions for future reference

### Problems Prevented
- Avoided making changes without understanding the root cause
- Will test locally before submitting to prevent CI failures

## Phase 1: Investigation
### Requirements
- Debug why observability test creates results file even when validation fails
- Find root cause of "Connection reset by peer" OpenTelemetry collector errors
- Understand test failure propagation mechanism in GitHub workflow

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  System.Net.Http.HttpRequestException : An error occurred while sending the request.
  ---- System.IO.IOException : Unable to read data from the transport connection: Connection reset by peer.
  -------- System.Net.Sockets.SocketException : Connection reset by peer
  ```
- **Log Locations**: GitHub workflow output shows test passes despite connection errors
- **System State**: WebAPI configured to connect to otel-collector:4317/4318, AppHost maps to localhost:18007/18009  
- **Reproduction Steps**: Run LocalTesting integration tests, observe connection failures but test still passes
- **Evidence**: User reports "test failure is written to LocalTesting/Bin/observability-test-result.txt so the GitHub workflow won't fail"
- **Local Test Results**: Infrastructure starts successfully but takes ~5+ minutes, OpenTelemetry collector config is valid
- **Container Analysis**: otel-collector container starts correctly when tested standalone with same config

### Findings
**Root Cause Analysis:**

**PRIMARY ISSUE: Test failure detection bypass**
- The test creates observability-test-result.txt file even when HttpRequestException occurs
- Line 616-622 catch block for validation exceptions works correctly
- BUT HttpRequestException during API calls is not caught by validation logic
- The test continues execution and creates results file despite connection failures
- GitHub workflow checks for file existence, not test exit code

**SECONDARY ISSUE: OpenTelemetry connection timing**
1. **Connection Timing Issue**: WebAPI starts before OpenTelemetry collector is fully ready
   - Aspire service discovery resolves correctly to "otel-collector:4317/4318"
   - Collector container starts but may not be accepting connections immediately
   - WebAPI makes OTLP calls during startup causing connection reset
   - This is a timing race condition, not configuration error

2. **Test Infrastructure Robustness**: Test should handle infrastructure failures gracefully
   - Should fail the test when critical infrastructure (OpenTelemetry) isn't available
   - Should not create results file when core services are unreachable
   - Need better error handling for infrastructure connectivity

**SOLUTION APPROACH:**
1. Add connection error detection to fail test immediately when OpenTelemetry is unreachable
2. Implement retry logic for OpenTelemetry connections during startup
3. Ensure results file is only created when ALL infrastructure is working properly
4. Add explicit infrastructure health checks before running observability workload

### Lessons Learned
- Connection reset by peer during test indicates infrastructure timing issues, not just config problems
- Test must distinguish between validation failures and infrastructure failures - both should fail the test
- GitHub workflow relies on file existence - must prevent file creation when any critical errors occur
- Need robust infrastructure readiness checks before executing observability workload

## Phase 2: Design  
### Requirements
- Fix test failure propagation by preventing results file creation when infrastructure fails
- Add proper error handling for OpenTelemetry connection failures
- Implement infrastructure health checks before running workload
- Ensure HttpRequestException causes test failure, not silent continuation

### Architecture Decisions
**Solution Design:**
1. **Enhanced Error Handling in ObservabilityMetricsSteps.cs**:
   - Wrap all HTTP calls in try-catch blocks that detect infrastructure failures
   - Add specific handling for HttpRequestException and SocketException
   - Prevent results file creation when core infrastructure is unreachable
   - Throw InvalidOperationException for infrastructure failures to fail test

2. **Infrastructure Health Verification**:
   - Add explicit health checks for OpenTelemetry collector before workload execution
   - Verify Prometheus connectivity before attempting to retrieve metrics
   - Add timeout-based retry logic for infrastructure services
   - Fail fast when critical services are not available

3. **Test Result Creation Logic Enhancement**:
   - Add additional validation flag: "infrastructure_healthy" 
   - Only create results file when flow_completed + metrics_validated + infrastructure_healthy all true
   - Document infrastructure failure reasons in console output for debugging

### Why This Approach
- **Minimal Changes**: Surgical fixes to existing test logic without major refactoring
- **Fail-Fast Principle**: Detect infrastructure problems early and fail immediately
- **Clear Error Reporting**: Distinguish between validation failures and infrastructure failures
- **GitHub Workflow Compatibility**: Maintains existing file-based success detection while fixing false positives

## Phase 3: TDD/BDD
### Requirements
- Test that infrastructure health checks properly fail when OpenTelemetry is unavailable
- Verify results file is NOT created when connection failures occur
- Validate HttpRequestException with "Connection reset by peer" triggers test failure

## Phase 4: Implementation
### Requirements
**COMPLETED: Enhanced Error Handling Implementation**

**Changes Made in `ObservabilityMetricsSteps.cs`:**

1. **Added Infrastructure Health Verification Method:**
   - `VerifyInfrastructureHealth()` method checks API, OpenTelemetry collector, and Prometheus connectivity
   - Specific error handling for "Connection reset by peer" scenarios
   - Throws `InvalidOperationException` to fail test when infrastructure is unavailable

2. **Enhanced `WhenIRunTheEntireFlow()` Method:**
   - Added pre-workload infrastructure health check
   - Wrapped API calls in try-catch blocks to detect connection failures
   - Specific handling for `HttpRequestException` with nested `SocketException`
   - Prevents continuation when infrastructure is unavailable

3. **Enhanced `ThenWePrintTheMetricsToTheConsole()` Method:**
   - Added connection error handling for debug metrics retrieval
   - Added connection error handling for detailed metrics retrieval
   - Added `infrastructure_healthy` validation flag
   - Only sets flag when all infrastructure connections succeed

4. **Enhanced `ThenWeSaveTheMetricsToAFile()` Method:**
   - Added `infrastructure_healthy` flag requirement
   - Results file only created when flow_completed + metrics_validated + infrastructure_healthy all true
   - Clear error messages explaining which validation flags are missing

5. **Enhanced `GetDetailedMetrics()` Method:**
   - Wrapped HTTP calls in proper error handling
   - Specific detection of connection reset scenarios
   - Throws infrastructure-specific exceptions for proper test failure

**Key Improvements:**
- **Fail-Fast Infrastructure Checks**: Detects problems before running expensive workload
- **Comprehensive Error Handling**: Catches all HTTP and socket connection issues
- **Clear Error Reporting**: Distinguishes between validation and infrastructure failures
- **Proper Test Failure Propagation**: Uses InvalidOperationException to ensure test fails
- **GitHub Workflow Integration**: Prevents results file creation for any failure type

### Code Changes
**File**: `LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs`
- Added infrastructure health verification before workload execution
- Enhanced error handling for all HTTP API calls  
- Added infrastructure_healthy validation flag
- Comprehensive connection error detection and test failure logic

**Test Validation Script**: `test-observability-fix-validation.sh`
- Created validation script to test the fix implementation
- Verifies results file is NOT created when connection issues occur
- Validates proper error detection and test failure behavior

## Phase 5: Testing & Validation
### Requirements
[To be filled after implementation complete]

## Phase 6: Owner Acceptance
### Requirements
[To be filled after testing complete]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented after completion]

### What Could Be Improved  
[To be documented after completion]

### Key Insights for Similar Tasks
[To be documented after completion]

### Specific Problems to Avoid in Future
[To be documented after completion]

### Reference for Future WIs
[To be documented after completion]