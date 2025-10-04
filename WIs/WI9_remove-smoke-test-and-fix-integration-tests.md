# WI9: Remove KafkaFlinkOnlySmokeTest and Fix Integration Tests

**File**: `WIs/WI9_remove-smoke-test-and-fix-integration-tests.md`
**Title**: Remove redundant smoke test and ensure integration tests work with Docker
**Description**: Remove `KafkaFlinkOnlySmokeTest.KafkaAndFlink_StartWithoutGateway` test and ensure all other integration tests pass successfully with Docker installed
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Code Cleanup + Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-20
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_fix-integration-tests.md - Contains history of integration test fixes and slot availability issues
- WI1_localtesting-integration-tests-gateway-fix.md - Gateway dependency handling patterns

### Lessons Applied  
- Debug first before proposing solutions
- Learn from passing tests to fix failing ones
- Verify Docker environment before running tests
- Document all failures and root causes

### Problems Prevented
- Avoid removing tests without understanding their purpose
- Don't assume Docker installation alone fixes all issues
- Ensure understanding of test infrastructure before changes

## Phase 1: Investigation

### Requirements
- Understand why KafkaFlinkOnlySmokeTest needs to be removed
- Identify what integration tests are currently failing
- Determine what "Install Docker and fix failed integration tests" means
- Learn from passing integration tests to understand correct patterns

### Debug Information (MANDATORY - Update this section for every investigation)

**Error Messages**:
```
Setup failed for test fixture LocalTesting.IntegrationTests.GlobalTestInfrastructure
Aspire.Hosting.DistributedApplicationException : Stopped waiting for resource 'flink-job-gateway' to become healthy because it failed to start.
```

**Log Locations**:
- Test output: `/tmp/test-output.txt`
- GlobalTestInfrastructure.cs line 80-82: Gateway readiness check

**System State**:
- Docker: Version 28.0.4 installed and running ✅
- .NET: Version 9.0.305 installed ✅
- No Docker containers currently running (0 containers)
- Gateway failing to start in Aspire testing mode

**Reproduction Steps**:
1. Run: `cd LocalTesting && dotnet test LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj`
2. GlobalTestInfrastructure.GlobalSetUp runs
3. Gateway resource waits for healthy status
4. Gateway fails to become healthy
5. All 10 tests fail due to infrastructure setup failure

**Evidence**:
- Docker is installed and working
- KafkaFlinkOnlySmokeTest exists and validates infrastructure without Gateway
- GlobalTestInfrastructure waits for Gateway to be healthy
- Gateway resource "flink-job-gateway" fails to start
- All tests depend on GlobalTestInfrastructure setup completing successfully

### Current Test Status
Based on test execution:
- Total tests: 10
- Failed: 10 (all failing due to GlobalSetUp failure)
- Passed: 0
- Root cause: Gateway not starting, blocking all tests

### Test Analysis
Looking at the test files:
1. **KafkaFlinkOnlySmokeTest**: Tests Kafka + Flink without Gateway
2. **GatewayAllPatternsTests**: 7 tests that require Gateway
3. **NativeFlinkAllPatternsTests**: Tests using native Flink (no Gateway)
4. Other tests: Likely also Gateway-dependent

### Problem Identified
The issue is that **GlobalTestInfrastructure** tries to start and wait for the Gateway:
- Lines 79-83: Wait for "flink-job-gateway" to become healthy
- Gateway is failing to start in Aspire testing mode
- This blocks ALL tests, even those that don't need Gateway

### Potential Solution Direction
Based on WI1_localtesting-integration-tests-gateway-fix.md:
- Gateway has known issues in Aspire testing mode
- Tests can be categorized as Gateway-dependent vs infrastructure-only
- KafkaFlinkOnlySmokeTest is infrastructure-only but might be redundant
- Need to make Gateway optional in GlobalTestInfrastructure

### Findings
**Root Cause**: Gateway failing to start in GlobalTestInfrastructure blocks all tests

**Key Observations**:
1. Docker is installed and working correctly
2. Gateway has known limitations in Aspire testing framework
3. KafkaFlinkOnlySmokeTest validates infrastructure without Gateway
4. GlobalTestInfrastructure mandates Gateway readiness for all tests
5. This creates a single point of failure blocking all test execution

**Problem Statement Analysis**:
- "remove KafkaFlinkOnlySmokeTest.KafkaAndFlink_StartWithoutGateway" - Remove the test
- "Install Docker and fix failed integration tests" - Docker is installed, need to fix tests
- "Learn from other passed integration tests" - Currently no tests passing, need to fix infrastructure first

### Next Steps
1. Make Gateway optional in GlobalTestInfrastructure
2. Remove KafkaFlinkOnlySmokeTest (as requested)
3. Ensure remaining tests can run (either with or without Gateway)
4. Fix any Gateway startup issues if needed for Gateway-dependent tests

## Phase 2: Design

(To be filled after investigation approval)

## Phase 3: TDD/BDD

(To be filled after design approval)

## Phase 4: Implementation

(To be filled after test design)

## Phase 5: Testing & Validation

(To be filled after implementation)

## Phase 6: Owner Acceptance

(To be filled after testing)

## Lessons Learned & Future Reference (MANDATORY)

(To be filled at completion)
