# WI18: Fix Remaining 3/9 Failed Integration Tests

**File**: `WIs/WI18_fix-remaining-integration-tests.md`
**Title**: Fix remaining 3 failed integration tests by debugging containers
**Description**: Debug and fix the 3 failing integration tests by accessing containers to identify root causes
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-10-05
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI16_fix-sql-flink-jobs.md - JAR combining and SQL connector issues
- WI10_integration-test-loop-fix.md - Infrastructure readiness validation
- WI9 - Java compatibility and JAR selection

### Lessons Applied  
- **Debug-first approach**: Access containers to see actual state
- **Container inspection**: Use docker exec to debug running containers
- **Infrastructure validation**: Ensure all components are properly connected
- **Incremental fixes**: Fix one test at a time

### Problems Prevented
- Guessing root causes without evidence
- Making changes without debugging
- Skipping container-level diagnostics

## Phase 1: Investigation

### Requirements
- Debug 3 failing tests by accessing containers
- Identify root causes for each failure
- Fix issues with minimal code changes
- Ensure all 9 tests pass

### Debug Information (MANDATORY - Update this section for every investigation)

**Current Test Status**: 6/9 passing, 3/9 failing

**Failed Tests**:
1. `Gateway_Pattern5_SqlPassthrough_ShouldWork` ❌
   - Error: "SQL Gateway submission failed. See inner exception for details."
   - Status: HTTP BadRequest
   
2. `Pattern1_Uppercase_ShouldTransformMessages` ❌ (NativeFlinkAllPatternsTests)
   - Error: Should consume exactly 2 messages but consumed 0
   - Kafka connection refused errors in logs
   
3. `DockerNetwork_FlinkCanReachKafka_ShouldSucceed` ❌
   - Error: System.InvalidOperationException: No Kafka container found

**Passed Tests**:
- Gateway_Pattern1_Uppercase_ShouldWork ✅
- Gateway_Pattern2_Filter_ShouldWork ✅
- Gateway_Pattern3_SplitConcat_ShouldWork ✅
- Gateway_Pattern4_Timer_ShouldWork ✅
- Gateway_Pattern6_SqlTransform_ShouldWork ✅
- Gateway_Pattern7_Composite_ShouldWork ✅

**Environment**:
- .NET Version: 9.0.305
- Container Runtime: Docker/Podman
- Flink: 2.1.0-java17
- OS: Linux

**Key Observations**:
- SQL Transform (Pattern6) passes but SQL Passthrough (Pattern5) fails - interesting!
- Kafka connection refused errors suggest container networking issues
- DockerNetwork test can't find Kafka container - suggests container naming or discovery issue

### Investigation Plan
1. ✅ Run tests and capture current status
2. ⏳ Access containers during test run to debug
3. ⏳ Check Kafka container naming and availability
4. ⏳ Debug SQL Gateway submission for Pattern5
5. ⏳ Debug Native Flink Pattern1 Kafka connectivity
6. ⏳ Fix root causes with minimal changes
7. ⏳ Retest until all 9 pass

## Phase 2: Design

(To be filled after investigation)

## Phase 3: TDD/BDD

(To be filled after design)

## Phase 4: Implementation

(To be filled after TDD/BDD)

## Phase 5: Testing & Validation

(To be filled after implementation)

## Phase 6: Owner Acceptance

(To be filled after testing)

## Lessons Learned & Future Reference

(To be filled at completion)
