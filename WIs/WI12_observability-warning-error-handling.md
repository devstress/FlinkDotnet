# WI12: Observability Test Warning and Error Handling

**File**: `WIs/WI12_observability-warning-error-handling.md`
**Title**: Fix observability test to treat infrastructure warnings as errors  
**Description**: Add comprehensive infrastructure validation and warning detection to fail observability tests when Kafka broker or other infrastructure warnings occur
**Priority**: High
**Component**: LocalTesting Observability
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-09
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI11: Observability workflow and metrics implementation
- Previous commits showing Kafka infrastructure setup
### Lessons Applied  
- Use existing health check service infrastructure (AspireHealthCheckService)
- Monitor container logs for warnings to detect infrastructure issues
- Implement proper validation before running flow execution
### Problems Prevented
- Avoiding proceeding with tests when infrastructure is unhealthy
- Preventing false positive test results when services have warnings

## Phase 1: Investigation
### Requirements
Fix observability test to treat ALL warnings as errors per user request

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - `[2025-09-04 13:57:04,413] WARN [RaftManager id=2] Connection to node 1 (kafka-broker-1/172.18.0.3:9093) could not be established. Node may not be available.`
  - `[2025-09-04 13:57:04,436] WARN [RaftManager id=2] Error connecting to node kafka-broker-3:9093 (id: 3 rack: null)`
  - `java.net.UnknownHostException: kafka-broker-3`
- **Log Locations**: Container logs from kafka-broker-2 showing inter-broker communication issues
- **System State**: 
  - 3-broker KRaft Kafka cluster configuration
  - Brokers start simultaneously without WaitFor dependencies
  - Test proceeds despite broker communication warnings
- **Reproduction Steps**: Run observability test and observe Kafka broker warning logs
- **Evidence**: Logs show broker connectivity issues during cluster formation
### Findings
- Kafka KRaft cluster has inter-broker communication issues during startup
- Current test doesn't validate infrastructure health before proceeding
- No warning/error monitoring in place to fail test on infrastructure issues
- AspireHealthCheckService exists but isn't used in observability test validation

### Lessons Learned
- Infrastructure warnings indicate real problems that can affect test reliability
- Need comprehensive health validation before flow execution
- Container log monitoring is required to detect and fail on warnings

## Phase 2: Design  
### Requirements
Add comprehensive infrastructure validation and warning detection

### Architecture Decisions
1. **Pre-Test Health Validation**: Use AspireHealthCheckService to validate all infrastructure before flow execution
2. **Container Log Monitoring**: Monitor Aspire container logs for warnings/errors during test execution
3. **Fail-Fast Strategy**: Immediately fail test if any warnings/errors detected
4. **Enhanced Kafka Broker Validation**: Specifically validate KRaft cluster formation and inter-broker connectivity

### Why This Approach
- Leverages existing health check infrastructure
- Provides early detection of infrastructure issues
- Ensures test reliability by preventing execution on unhealthy infrastructure
- Gives clear failure reasons when infrastructure has issues

### Alternatives Considered
- Fix root cause of Kafka broker connectivity (more complex, requires infrastructure changes)
- Ignore warnings (not acceptable per user requirements)
- Add retry logic (doesn't address the core requirement to fail on warnings)

## Phase 3: TDD/BDD
### Test Specifications
- Test should fail immediately if any infrastructure warnings detected
- Health check validation must pass before flow execution
- Clear error messages when infrastructure validation fails

### Behavior Definitions
- Given infrastructure warnings exist, When test runs, Then test should fail with error
- Given infrastructure is healthy, When test runs, Then test should proceed and pass

## Phase 4: Implementation
### Code Changes
1. ✅ Enhanced observability test step definitions to add infrastructure validation
   - Added `ValidateInfrastructureHealthOrFail()` method that fails test on any warnings
   - Added `ValidateServiceHealthResults()` to check service health status
   - Added `MonitorContainerLogsForWarnings()` for warning detection
   - Added `ValidateKafkaClusterHealth()` specific validation for Kafka broker issues

2. ✅ Added container log monitoring capabilities 
   - Monitor for infrastructure warnings during test execution
   - Fail-fast approach when warnings detected

3. ✅ Integrated AspireHealthCheckService validation before flow execution
   - Call infrastructure validation before running the observability flow
   - Enhanced constructor to inject AspireHealthCheckService

4. ✅ Added warning/error detection patterns for Kafka and other services
   - Created `/api/observability/validate-infrastructure` endpoint for comprehensive validation
   - Created `/api/observability/kafka-cluster-health` endpoint for Kafka-specific validation
   - Added proper error messages and status codes for different failure scenarios

### Challenges Encountered
- CI environment has .NET 8.0 but project requires .NET 9.0 (per enforcement rules)
- Limited access to container logs in Aspire testing framework
- Need to balance comprehensive validation with test execution time

### Solutions Applied
- Enhanced existing health check service instead of direct container log access
- Added specific Kafka broker validation to catch the warnings shown by user
- Clear error messages that distinguish between different types of infrastructure failures
- Fail-fast approach to prevent test execution on unhealthy infrastructure

## Phase 5: Testing & Validation
### Test Results
[To be filled during testing]

### Performance Metrics
[To be filled during testing]

## Phase 6: Owner Acceptance
### Demonstration
[To be filled during demonstration]

### Owner Feedback
[To be filled after feedback]

### Final Approval
[To be filled after approval]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented during implementation]

### What Could Be Improved  
[To be documented during implementation]

### Key Insights for Similar Tasks
[To be documented during implementation]

### Specific Problems to Avoid in Future
[To be documented during implementation]

### Reference for Future WIs
[To be documented during implementation]