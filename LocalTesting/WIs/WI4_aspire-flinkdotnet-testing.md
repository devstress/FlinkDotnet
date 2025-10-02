# WI4: Test Aspire Setup with FlinkDotNet

**File**: `WIs/WI4_aspire-flinkdotnet-testing.md`
**Title**: [LocalTesting] Test Aspire setup working with FlinkDotNet  
**Description**: Validate that the Aspire infrastructure in LocalTesting works correctly with FlinkDotNet integration tests, ensuring proper container orchestration, networking, and service communication.
**Priority**: High
**Component**: LocalTesting.FlinkSqlAppHost, LocalTesting.IntegrationTests  
**Type**: Investigation
**Assignee**: GitHub Copilot Agent
**Created**: 2025-01-17
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- Reviewed existing WI files in workspace
- Found WI1, WI2, WI3 focusing on LocalTesting improvements and Aspire/DCP networking
### Lessons Applied  
- Debug first approach from WI enforcement rules
- Complete infrastructure validation before proceeding
- Proper timeout handling for container startup
### Problems Prevented
- Skipping debugging phase and rushing to solutions
- Incomplete infrastructure validation
- Not testing end-to-end functionality

## Phase 1: Investigation
### Requirements
- Verify .NET 9.0 environment and Aspire workload installation
- Validate LocalTesting solution builds successfully
- Test that Aspire containers (Kafka, Flink JobManager/TaskManager) start correctly
- Verify infrastructure readiness through integration tests
- Ensure proper networking between services

### Debug Information (MANDATORY - Update this section for every investigation)
- **Build Status**: Initial build completed successfully ✅
  ```
  Build successful
  ```
- **Project Configuration**: Both projects target .NET 9.0 correctly
  - LocalTesting.FlinkSqlAppHost: Uses Aspire.AppHost.Sdk 9.3.1, contains Kafka and Flink container setup
  - LocalTesting.IntegrationTests: Uses Aspire.Hosting.Testing 9.3.1, has comprehensive test base class
- **Container Setup Analysis**:
  - Kafka: Configured on port 9092 with proper external access
  - Flink JobManager: Port 8081 with REST API and web UI
  - Flink TaskManager: 2 task slots, waits for JobManager
  - Flink Job Gateway: Optional (controlled by INCLUDE_FLINK_GATEWAY environment variable)
- **Test Infrastructure**: LocalTestingTestBase provides comprehensive infrastructure validation
  - Docker environment validation
  - Container networking checks
  - Service readiness validation with retries
  - Proper timeout handling and diagnostics

### Environment Verification Results
