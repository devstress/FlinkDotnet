# WI1: Slow Test Performance Fix

**File**: `WIs/WI1_slow-test-performance-fix.md`
**Title**: [Flink.JobBuilder.Tests] Fix slow test execution (1m 6s → milliseconds)
**Description**: The Flink.JobBuilder.Tests project takes 1 minute and 6 seconds to run 1652 tests, when they should complete in milliseconds. Investigation reveals JobClient tests are making actual HTTP requests to non-existent Flink cluster with 5-minute timeouts and 3 retries with exponential backoff.
**Priority**: High
**Component**: Flink.JobBuilder.Tests
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-14
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- No previous WIs found (first WI for this repository)
### Lessons Applied  
- N/A - First work item
### Problems Prevented
- N/A - First work item

## Phase 1: Investigation
### Requirements
- Identify why tests take 1m 6s instead of milliseconds
- Find root cause of slow test execution
- Document specific slow tests and their causes

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: Tests pass but run extremely slow (1m 6s for 1652 tests)
- **Log Locations**: Test output shows:
  ```
  Passed WaitForKafkaSetupAsync_WithDifferentBootstrapServers_HandlesCorrectly [3 s]
  Passed WaitForKafkaSetupAsync_WithShortTimeout_Succeeds [1 s]
  Passed WaitForKafkaSetupAsync_WithValidParameters_CompletesSuccessfully [1 s]
  Passed CancelAsync_WithCancellationToken_AcceptsToken [9 s]
  Passed CancelAsync_WithValidJobId_Succeeds [9 s]
  Passed GetJobExecutionResultAsync_ReturnsResult [9 s]
  Passed GetJobExecutionResultAsync_WithCancellationToken_AcceptsToken [9 s]
  Passed GetJobStatusAsync_ReturnsJobStatus [9 s]
  Passed GetJobStatusAsync_WithCancellationToken_AcceptsToken [9 s]
  ```
- **System State**: Tests are making real HTTP requests to non-existent Flink cluster
- **Environment Configuration**: 
  - `FlinkJobGatewayConfiguration.HttpTimeout = TimeSpan.FromMinutes(5)` (5 minutes!)
  - `FlinkJobGatewayConfiguration.MaxRetries = 3`
  - `FlinkJobGatewayConfiguration.RetryDelay = TimeSpan.FromSeconds(1)` with exponential backoff
  - JobClient also has 5-minute timeout
- **Reproduction Steps**: 
  1. Run `dotnet test Flink.JobBuilder.Tests/Flink.JobBuilder.Tests.csproj --configuration Release`
  2. Observe tests taking 9 seconds each in JobClientCoverageTests
- **Evidence**: 
  - JobClientCoverageTests.cs lines 118-143, 146-171, 178-201, etc. make real HTTP calls
  - Each test waits for HTTP timeout + retries = ~9 seconds per test
  - Total slow time: 9 tests × 9 seconds = 81 seconds ≈ 1m 6s execution time

### Findings
**Root Cause**: JobClientCoverageTests make actual HTTP requests to a Flink cluster that doesn't exist during testing. The tests catch HttpRequestException and pass, but only after waiting for:
1. HTTP timeout (5 minutes configured, but appears to fail faster)
2. Retry logic: 3 retries with exponential backoff (2s, 4s, 6s)
3. Total wait per test: ~9 seconds

**Affected Tests** (from JobClientCoverageTests.cs):
- CancelAsync_WithValidJobId_Succeeds (line 118)
- CancelAsync_WithCancellationToken_AcceptsToken (line 146)
- GetJobExecutionResultAsync_ReturnsResult (line 178)
- GetJobExecutionResultAsync_WithCancellationToken_AcceptsToken (line 204)
- GetJobStatusAsync_ReturnsJobStatus (line 232)
- GetJobStatusAsync_WithCancellationToken_AcceptsToken (line 259)
- And several more savepoint-related tests

**Solution Options**:
1. **Option A (Preferred)**: Configure tests to use mock HttpClient or mock FlinkJobGatewayService with immediate responses
2. **Option B**: Reduce timeouts significantly for test environment only
3. **Option C**: Use dependency injection to provide test-specific configuration with minimal timeouts

### Lessons Learned
- HTTP timeouts in tests should be minimal (milliseconds, not minutes)
- Tests should not make real network calls to external services
- Always investigate individual test durations when aggregate test time is slow
- Mock external dependencies to prevent test flakiness and slowness

## Phase 2: Design  
### Requirements
- Create minimal changes to fix test performance
- Ensure tests still validate the code correctly
- Maintain test coverage and quality

### Architecture Decisions
**Chosen Approach**: Option A - Mock the FlinkJobGatewayService for fast test execution

**Implementation Strategy**:
1. Use the existing TestMockFlinkJobGatewayService pattern (already exists in FlinkJobBuilderCoreTests.cs)
2. Modify JobClientCoverageTests to inject a mock gateway service with immediate responses
3. Alternatively, configure JobClient to use a test timeout (1 second instead of 5 minutes)

**Why This Approach**:
- Minimal code changes required
- Existing mock pattern already proven in codebase
- Tests will run in milliseconds instead of seconds
- No production code changes needed
- Tests remain valid for coverage purposes

### Alternatives Considered
- **Option B (Rejected)**: Modify production timeouts - would affect all users, not just tests
- **Option C (Rejected)**: Requires refactoring JobClient to support DI - too invasive for this bug fix

## Phase 3: TDD/BDD
### Test Specifications
- All existing tests must pass
- Test execution time must be < 10 seconds total (currently 66 seconds)
- No test should take more than 100ms (currently 9 seconds per slow test)

### Behavior Definitions
- Tests should validate method signatures and basic behavior
- Tests should not wait for network timeouts
- Tests should use mocks or reduced timeouts for external dependencies

## Phase 4: Implementation
### Code Changes
*To be filled during implementation*

### Challenges Encountered
*To be filled during implementation*

### Solutions Applied
*To be filled during implementation*

## Phase 5: Testing & Validation
### Test Results
*To be filled during testing*

### Performance Metrics
*To be filled during testing*

## Phase 6: Owner Acceptance
### Demonstration
*To be filled during acceptance*

### Owner Feedback
*To be filled during acceptance*

### Final Approval
*To be filled during acceptance*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be documented at completion*

### What Could Be Improved  
*To be documented at completion*

### Key Insights for Similar Tasks
*To be documented at completion*

### Specific Problems to Avoid in Future
*To be documented at completion*

### Reference for Future WIs
*To be documented at completion*
