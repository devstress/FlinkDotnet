# WI76: Debug and Fix All Failed Integration Tests

**File**: `WIs/WI76_debug-20-failed-integration-tests.md`
**Title**: [LearningCourse] Debug and fix 20 failed integration tests out of 60 total
**Description**: Systematically debug timeout and infrastructure-related test failures across Days 01-15
**Priority**: High
**Component**: LearningCourse Integration Tests
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-14
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI38-75: All Learning Course conversion work with real infrastructure patterns
- Update-LearningCourse.md: Common Error #12 (Web Services vs Console Apps causing timeouts)
- Update-LearningCourse.md: Common Error #15 (Real Infrastructure vs Simulation decisions)

### Lessons Applied
- **Debug first** before proposing solutions (Rule 7)
- Check for web service pattern (`app.RunAsync()`) causing timeouts
- Verify exercises use proper service discovery (no hardcoded addresses)
- Look for infrastructure connectivity issues (Kafka, Flink, Docker)
- Check Day08 stress tests specifically for high-volume timeout patterns

### Problems Prevented
- Jumping to solutions without evidence-based debugging
- Ignoring systematic failure patterns across exercise types
- Missing infrastructure health validation before test execution

## Phase 1: Investigation

### Requirements
- Analyze test failure patterns from terminal output
- Review test logs in LocalTesting/test-logs directory
- Identify common failure causes (timeouts, infrastructure, code bugs)
- Document specific error messages and stack traces

### Debug Information (MANDATORY - Update this section for every investigation)
**Test Execution Context**:
- **Command**: `cd LearningCourse && dotnet test IntegrationTests.sln --configuration Release --logger "console;verbosity=detailed" --results-directory "../LocalTesting/test-logs" -- NUnit.DefaultTimeout=180000`
- **Test Timeout**: 180000ms (3 minutes)
- **Results**: 40 passed, 20 failed out of 60 total tests
- **Active Terminal**: Terminal 2 currently running tests

**Error Pattern Analysis** (from task description):
- Most failures are timeout-related
- Day08 tests (Exercise81, 82, 83) - Stress testing with high-volume Kafka
- Other infrastructure-heavy tests across various days
- Pattern suggests exercises may be running indefinitely or taking too long

**System State**:
- Terminal actively running test execution
- Test logs being written to LocalTesting/test-logs/
- Log files present: Flink.jobmanager.log, Flink.sql-gateway.log, FlinkDotNet.JobGateway.log, FlinkDotnet.log

**Initial Hypotheses** (to be validated with evidence):
1. **Web Service Pattern Issue**: Some exercises may use `app.RunAsync()` causing indefinite execution
2. **Stress Test Volume**: Day08 exercises may have unrealistic message volumes causing timeouts
3. **Infrastructure Latency**: Kafka/Flink operations may be slower than expected 3-minute timeout
4. **Resource Contention**: Multiple tests competing for Docker container resources
5. **Missing Completion Markers**: Exercises may not be printing expected completion messages

### Findings
[To be populated during investigation phase]

### Lessons Learned
[To be populated after investigation]

## Phase 2: Design
[To be populated after investigation phase]

## Phase 3: TDD/BDD
[To be populated after design phase]

## Phase 4: Implementation
[To be populated after TDD phase]

## Phase 5: Testing & Validation
[To be populated after implementation]

## Phase 6: Owner Acceptance
[To be populated after testing]

## Lessons Learned & Future Reference (MANDATORY)
[To be populated at completion]