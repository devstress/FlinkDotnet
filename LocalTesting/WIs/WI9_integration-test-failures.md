# WI9: Investigate Integration Test Failures

**File**: `LocalTesting/WIs/WI9_integration-test-failures.md`
**Title**: Investigate and fix integration test failures (SQL pattern tests failing)
**Description**: Some integration tests are failing with Flink job submission errors
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Investigation
**Assignee**: GitHub Copilot
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI7_remove-kafka-flink-only-smoke-test.md - Test removal and infrastructure validation
- WI8_maven-build-resilience.md - Maven build improvements

### Lessons Applied  
- Debug-first approach to understand failures
- Run tests locally to reproduce issues
- Check infrastructure before blaming code

### Problems Prevented
- Avoiding guessing without data
- Not making changes without understanding root cause

## Phase 1: Investigation

### Requirements
- Understand why integration tests are failing
- Fix root cause to make all 9 tests pass
- Ensure tests are reliable in both local and CI environments

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement from Comment**:
- User reports: "Total tests: 9, Passed: 1, Failed: 8" in CI
- Requested: "investigate and fix the root cause to make all 9 tests pass"
- Must reproduce locally first

**Local Test Results**:
```
Total: 9 tests
Passed: 6 tests
  - Gateway_Pattern1_Uppercase_ShouldWork ✅
  - Gateway_Pattern2_Filter_ShouldWork ✅
  - Gateway_Pattern3_SplitConcat_ShouldWork ✅
  - Gateway_Pattern4_Timer_ShouldWork ✅
  - Gateway_Pattern7_Composite_ShouldWork ✅ (initially thought failing, but passed in full run)
  - Pattern1_Uppercase_ShouldTransformMessages ✅ (Native Flink)

Failed: 3 tests
  - Gateway_Pattern5_SqlPassthrough_ShouldWork ❌
  - Gateway_Pattern6_SqlTransform_ShouldWork ❌
  - (One more, likely Gateway_Pattern7_Composite based on earlier output) ❌
```

**Error Pattern**:
All failures show the same error:
```
Job must submit successfully. Error: HTTP BadRequest
org.apache.flink.runtime.rest.handler.RestHandlerException: Could not execute application
```

**Infrastructure Status**:
- ✅ Docker installed and working (version 28.0.4)
- ✅ Kafka starts successfully
- ✅ Flink JobManager starts successfully
- ✅ Flink TaskManager starts successfully  
- ✅ Gateway starts successfully
- ✅ Topics created successfully
- ❌ SQL pattern jobs fail to submit to Flink

**Key Observations**:
1. **Discrepancy**: User reports 8/9 failures in CI, but locally only 3/9 fail
2. **Pattern**: Only SQL-based FlinkDotNet jobs fail (SqlPassthrough, SqlTransform)
3. **Non-SQL jobs pass**: Uppercase, Filter, SplitConcat, Timer all work
4. **Infrastructure OK**: Kafka, Flink, Gateway all report healthy
5. **Submission fails**: Jobs fail at Flink execution, not at submission API level

**Environment Differences**:
- Local: Java 17 (OpenJDK Temurin 17.0.16)
- CI: Java 25 (per workflow file)
- This could explain different failure rates

### Findings

**Possible Root Causes**:
1. **Flink IR Runner JAR issue**: SQL jobs may require specific dependencies not in JAR
2. **Java version incompatibility**: Java 17 vs Java 25 might cause different behaviors
3. **Flink SQL connector missing**: SQL jobs need Kafka SQL connector in Flink classpath
4. **Job definition issue**: SQL job definitions might be malformed
5. **Recent code change**: Something in recent commits broke SQL jobs

**Next Steps**:
1. Get CI logs from user to understand 8/9 failure pattern
2. Check Flink logs for detailed error messages
3. Verify Flink IR Runner JAR contains necessary SQL dependencies
4. Test with same Java version as CI (Java 25)
5. Check if this is a pre-existing issue or regression

## Phase 2: Design

### Requirements
(To be updated after understanding root cause)

## Phase 3: TDD/BDD

### Test Specifications
(To be updated after understanding root cause)

## Phase 4: Implementation

### Code Changes
(To be updated after understanding root cause)

## Phase 5: Testing & Validation

### Test Results
(To be updated after implementation)

## Phase 6: Owner Acceptance

### Demonstration
(To be updated after implementation)

### Owner Feedback
Awaiting clarification on CI failures vs local failures

### Final Approval
(Pending)

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
(To be updated after completion)

### What Could Be Improved  
(To be updated after completion)

### Key Insights for Similar Tasks
(To be updated after completion)

### Specific Problems to Avoid in Future
(To be updated after completion)

### Reference for Future WIs
(To be updated after completion)
