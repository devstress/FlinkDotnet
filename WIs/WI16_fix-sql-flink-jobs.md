# WI16: Fix SQL Flink Job Integration Tests

**File**: `WIs/WI16_fix-sql-flink-jobs.md`
**Title**: Fix remaining integration tests - SQL Flink job architecture root cause
**Description**: Fix the 4 failing SQL Flink job tests by learning from Flink SQL online tutorials and fixing root cause architecture issues
**Priority**: High
**Component**: LocalTesting.IntegrationTests, FlinkIRRunner, Flink.JobBuilder
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-01-30
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI9_integration-test-failures.md - JAR selection priority fix for Java compatibility
- WI10_integration-test-loop-fix.md - Iterative debugging approach and infrastructure validation

### Lessons Applied  
- **Debug-first approach**: Must understand failures before proposing fixes
- **Run tests locally**: Reproduce issues in local environment first
- **Learn from previous work**: WI9 fixed Java compatibility, 5/9 tests now pass
- **Iterative testing**: Fix one issue at a time, retest, repeat
- **Study Flink SQL tutorials**: Understand proper Flink SQL job setup

### Problems Prevented
- Avoiding guessing without evidence
- Not making changes without understanding root cause
- Skipping SQL-specific Flink requirements

## Phase 1: Investigation

### Requirements
- Understand why SQL Flink jobs fail while DataStream jobs succeed
- Study Apache Flink SQL documentation and tutorials
- Debug SQL job submission to identify root cause
- Fix architecture issues in SQL job processing
- Ensure all 9 tests pass (currently 5/9 passing)

### Debug Information (MANDATORY - Update this section for every investigation)

**Current Test Status**: Unknown - need to run tests
**Environment**:
- .NET Version: 9.0.305
- Java: Unknown (need to check)
- Flink: 2.1.0-java17 (from WI9)
- OS: Linux (GitHub Actions runner)

**Test Execution Strategy**:
1. Build all solutions successfully
2. Run integration tests to capture current failure state
3. Identify which specific SQL tests are failing
4. Debug SQL job submission process
5. Learn from Flink SQL tutorials and documentation
6. Implement minimal fix for root cause
7. Retest until all 9 tests pass

**Expected Failures** (based on WI9):
- Gateway_Pattern5_SqlPassthrough_ShouldWork ❌
- Gateway_Pattern6_SqlTransform_ShouldWork ❌
- Possibly 2 more SQL-related tests

### Test Run #1 - Initial Status Check

**Status**: TO BE EXECUTED

(Debug information will be added after test run)

### Findings

(To be updated after initial test run and investigation)

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
(To be documented as we progress)

### Specific Problems to Avoid in Future
(To be documented based on issues encountered)

### Reference for Future WIs
(To be documented with specific files and patterns)
