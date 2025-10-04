# WI10: Fix Failed Integration Tests Through Iterative Loop

**File**: `WIs/WI10_integration-test-loop-fix.md`
**Title**: Loop test -> capture error -> investigate -> fix root cause -> retest until all tests passed
**Description**: Execute systematic approach to fix all failing integration tests through debugging loop
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-10-04
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI9_integration-test-failures.md - JAR selection priority fix for Java compatibility
- WI1_localtesting-integration-tests-fix.md - Infrastructure readiness patterns
- WI8_maven-build-resilience.md - Maven build improvements

### Lessons Applied  
- **Debug-first approach**: Must understand failures before proposing fixes
- **Run tests locally**: Reproduce issues in local environment first
- **Infrastructure validation**: Check all components before blaming code
- **Iterative testing**: Fix one issue at a time, retest, repeat

### Problems Prevented
- Avoiding guessing without evidence
- Not making changes without understanding root cause
- Skipping tests rather than fixing them

## Phase 1: Investigation

### Requirements
- Run integration tests to capture current failure state
- For each failure: capture error -> debug -> investigate root cause
- Fix root causes iteratively
- Retest after each fix
- Continue loop until all 9 tests pass

### Debug Information (MANDATORY - Update this section for every investigation)

**Initial Build Status**: ✅ PASSED
```
Build succeeded.
    1 Warning(s)
    0 Error(s)
Time Elapsed 00:00:20.02
```

**Environment**:
- .NET Version: 9.0.305
- Java: To be determined
- OS: Linux (GitHub Actions runner)

**Test Execution Strategy**:
1. Run all integration tests
2. Capture failures and error messages
3. For each unique failure:
   - Debug to understand root cause
   - Implement minimal fix
   - Retest to verify fix
4. Repeat until all tests pass

### Test Run #1 - Initial State

**Status**: FAILED - Root cause identified

**Root Cause Found**: Missing `flink-json-2.1.0.jar` file
- File was a directory instead of a JAR file in `LocalTesting/connectors/flink/lib/`
- This caused Flink container bind mount to fail
- As a result, Aspire could not start Flink containers
- This prevented ALL integration tests from running

**Fix Applied**:
- Copied `flink-json-2.1.0.jar` from IntegrationTests output directory to connectors directory
- File size: 177K (correct JAR file)

**Additional Investigation Findings**:
- DistributedApplicationTestingBuilder is the correct approach for Aspire integration tests  
- Tests were failing because infrastructure (Flink containers) never started
- Once JAR file is fixed, containers should start properly

**Next Steps**:
1. Ensure flink-json JAR is always properly deployed during build
2. Revert process-based AppHost approach (not needed)
3. Return to DistributedApplicationTestingBuilder with proper JAR files
4. Retest

### Findings

(To be updated after initial test run)

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
(To be documented as we progress)

### Specific Problems to Avoid in Future
(To be documented based on issues encountered)

### Reference for Future WIs
(To be documented with specific files and patterns)
