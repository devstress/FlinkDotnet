# WI11: Debug and Fix Kafka-TaskManager Connectivity Issues

**File**: `LocalTesting/WIs/WI11_kafka-taskmanager-connectivity-debug.md`
**Title**: Debug root cause why TaskManagers fail to connect with Kafka in LocalTesting integration tests
**Description**: Investigate and fix the root cause of TaskManager-Kafka connectivity failures. Debug containers to understand the issue, fix containers first to prove working, then adjust Aspire project and tests.
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix
**Assignee**: GitHub Copilot
**Created**: 2025-01-28
**Status**: Investigation

## Lessons Applied from Previous WIs

### Previous WI References
- WI2_aspire-dcp-networking-fix.md - Kafka networking configuration (ports 9092 vs 9093)
- WI6_kafka-connectivity-fix.md - Previous Kafka connectivity investigation
- WI10_fix-integration-test-failures.md - Recent integration test failures investigation

### Lessons Applied
- **Debug-first approach**: Must debug containers and gather evidence before proposing solutions
- **Container inspection**: Check actual container status, logs, and network configuration
- **Kafka port configuration**: Understanding of kafka:9092 (internal) vs localhost:9093 (external)
- **Previous attempts**: WI2 identified kafka:9093 for container-to-container, WI10 tried kafka:9092
- **Evidence-based fixes**: Prove containers work standalone before adjusting Aspire

### Problems Prevented
- Making code changes without understanding actual root cause
- Not debugging live containers to see what's actually happening
- Repeating previous failed approaches without new evidence

## Phase 1: Investigation

### Requirements
- Debug why TaskManagers cannot connect to Kafka
- Examine actual container logs and network configuration
- Identify the real root cause with evidence
- Fix containers first to prove they can work
- Then adjust Aspire project and tests to match working configuration

### Debug Information (MANDATORY - Update this section for every investigation)

**Problem Statement from User**:
- "TaskManagers fail to connect with Kafka"
- "Please debug containers to see what is the root cause"
- "Fix the containers first to prove it is working"
- "Then adjust the aspire project and the tests"

**Environment**:
- .NET: 9.0.305 ✅ Installed
- Docker: 28.0.4 ✅ Installed
- No containers currently running (clean slate)

**Historical Context**:
- WI2: Changed to kafka:9093 for container-to-container (based on Aspire source)
- WI10: Changed back to kafka:9092, but tests still failed
- Current Ports.cs: KafkaContainerBootstrap = "kafka:9092"
- Previous investigations found jobs submit and reach RUNNING but process 0 messages

**Next Steps**:
1. ✅ Check .NET version and environment
2. ⏳ Run integration tests to reproduce the failures
3. ⏳ Examine container logs during test execution
4. ⏳ Debug container networking and Kafka connectivity
5. ⏳ Identify root cause with concrete evidence
6. ⏳ Fix containers to prove connectivity works
7. ⏳ Adjust Aspire project and tests accordingly

### Findings
(To be updated during investigation)

## Phase 2: Design
(To be completed after investigation)

## Phase 3: TDD/BDD
(To be completed after design)

## Phase 4: Implementation
(To be completed after testing)

## Phase 5: Testing & Validation
(To be completed after implementation)

## Phase 6: Owner Acceptance
(To be completed after validation)

## Lessons Learned & Future Reference (MANDATORY)
(To be completed at end of WI)
