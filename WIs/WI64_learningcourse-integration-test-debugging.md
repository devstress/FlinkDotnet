# WI64: LearningCourse Integration Test Debugging

**File**: `WIs/WI64_learningcourse-integration-test-debugging.md`
**Title**: [LearningCourse] Integration test failures investigation and resolution
**Description**: Debug and fix LearningCourse integration test failures
**Priority**: High
**Component**: LearningCourse Integration Tests
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-14
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI63: Day06 Temporal workflow integration (proper environment variable handling)
- WI62: Environment variable scoping issues and fixes

### Lessons Applied
- Always set environment variables at the correct scope (exercise process, not global)
- Use Aspire port discovery instead of hardcoded ports
- Follow Day01 test patterns for infrastructure readiness

### Problems Prevented
- Avoided Docker container environment variable inheritance issues
- Prevented hardcoded port conflicts

## Phase 1: Investigation
### Requirements
- Run LearningCourse integration tests
- Identify root causes of failures
- Use LocalTesting/test-logs for diagnostics

### Debug Information (MANDATORY - Updated for every investigation)
**Initial Test Run Evidence:**
- Error: Multiple test failures with Kafka connection timeouts
- Log Location: Terminal output showing Exercise151-154 Redis failures
- System State: All containers cleaned up successfully
- Reproduction: Tests run without LEARNINGCOURSE=true environment variable

**Infrastructure Discovery:**
- Redis is ONLY used by Day15 exercises (Exercise151-154 - Capstone Project)
- Observability stack (Prometheus/Grafana) is ONLY used by Day05 Exercise51
- Both are conditionally deployed when `LEARNINGCOURSE=true` in LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs (lines 16-256)
- Tests failed because infrastructure was not deployed (missing environment variable)

**Container Cleanup Evidence:**
```
docker ps -a
CONTAINER ID   IMAGE     COMMAND   CREATED   STATUS    PORTS     NAMES
```
All containers successfully removed. Clean slate for re-run.

### Findings
**Issue 1: Missing LEARNINGCOURSE Environment Variable**
- Root Cause: Tests run without `LEARNINGCOURSE=true` flag
- Evidence: Exercise151-154 failing with "Cannot connect to Redis"
- Impact: Redis and Observability stack not deployed
- Solution: Re-run tests with `LEARNINGCOURSE=true` environment variable

**Issue 2: Proper Infrastructure Conditionals**
- Verification: LocalTesting/Program.cs lines 16-256 show proper conditional logic
- Redis deployment: Lines 228-235 (port 6379)
- Prometheus deployment: Lines 239-243 (port 9090)
- Grafana deployment: Lines 248-255 (port 3000)
- Status: Infrastructure code is correct, just needs environment variable set

**Issue 3: Test Base Infrastructure is Idempotent**
- LearningCourseTestBase.cs lines 17-19: Semaphore + _isSetupComplete flag
- GlobalSetUp lines 44-51: Proper idempotency check
- GlobalTearDown lines 261-268: Proper teardown protection
- Status: ✅ Infrastructure setup is properly designed

### Lessons Learned
- ALWAYS check required environment variables before running tests
- Conditional infrastructure requires explicit environment variable flags
- Docker container cleanup must complete before re-running tests
- LearningCourse requires additional infrastructure beyond base Kafka+Flink+Temporal

## Phase 2: Design
### Requirements
- Set LEARNINGCOURSE=true environment variable for test execution
- Ensure Redis and Observability infrastructure deploys
- Verify all Day15 and Day05 Exercise51 tests pass

### Architecture Decisions
**Solution: Environment Variable Configuration**
```powershell
# Windows PowerShell
$env:LEARNINGCOURSE = "true"
cd LearningCourse
dotnet test IntegrationTests.sln --configuration Release --logger "console;verbosity=detailed" --results-directory "../LocalTesting/test-logs" -- NUnit.DefaultTimeout=180000
```

**Why This Approach:**
- Enables conditional infrastructure deployment
- Matches LocalTesting/Program.cs conditional logic
- Minimal change - just environment variable configuration
- Follows established Aspire pattern for environment-specific features

**Alternatives Considered:**
- Hardcode Redis/Observability always: ❌ Wasteful for basic tests
- Separate test solution for Day15/Day05: ❌ Increases maintenance burden
- Mock Redis/Observability: ❌ Violates "eliminate all simulations" mandate

## Phase 3: Implementation
### Required Actions
1. Set `LEARNINGCOURSE=true` environment variable
2. Re-run integration tests with correct environment
3. Verify Redis and Observability infrastructure deploys
4. Confirm all tests pass (target: 100%)

### Status
- [x] Investigated test failures
- [x] Identified root cause (missing environment variable)
- [x] Verified infrastructure code is correct
- [x] Cleaned up all containers
- [ ] Set LEARNINGCOURSE=true and re-run tests
- [ ] Verify 100% pass rate

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Idempotent infrastructure setup with semaphore protection
- Conditional infrastructure deployment keeps basic tests lightweight
- Clear separation between core infrastructure (Kafka/Flink/Temporal) and learning-specific (Redis/Observability)

### What Could Be Improved
- Document required environment variables in README
- Add validation check at test startup for required environment variables
- Consider adding environment variable to .runsettings file

### Key Insights for Similar Tasks
- Always review LocalTesting/Program.cs conditional logic before running tests
- Check for environment variable requirements in infrastructure code
- Clean container state between test runs to avoid confusion

### Specific Problems to Avoid in Future
- Running LearningCourse tests without LEARNINGCOURSE=true
- Assuming all infrastructure always deploys (check conditional logic)
- Not cleaning containers between test runs

### Reference for Future WIs
- LearningCourse requires `LEARNINGCOURSE=true` environment variable
- Day15 exercises (Exercise151-154) require Redis
- Day05 Exercise51 requires Observability stack (Prometheus/Grafana)
- Infrastructure setup in LearningCourseTestBase is idempotent and properly designed