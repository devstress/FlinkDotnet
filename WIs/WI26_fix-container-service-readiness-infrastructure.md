# WI26: Fix Container Service Readiness Infrastructure Issues

**File**: `WIs/WI26_fix-container-service-readiness-infrastructure.md`
**Title**: Fix infrastructure configuration issue where services inside containers are not ready to accept connections
**Description**: Address the remaining infrastructure configuration problem where Kafka brokers, Flink services, and other containerized services are not ready to accept connections even after extended startup periods. This requires dedicated investigation focused on container service readiness optimization.
**Priority**: High
**Component**: LocalTesting Infrastructure
**Type**: Infrastructure Enhancement
**Assignee**: AI Agent
**Created**: 2024-01-20
**Status**: Implementation Complete - Issue Identified

## Lessons Applied from Previous WIs
### Previous WI References
- WI16_fix-localtesting-observability-test-reliability.md (test hanging issue resolved)
- WI23_fix-observability-test-infrastructure-reliability.md (error handling improvements)
- WI13_aspire-health-check-optimization.md (Aspire health check patterns)

### Lessons Applied  
- Remove problematic pre-test validation that causes connection failures (WI16 lesson)
- Use proper Aspire framework patterns for service readiness (WI13 lesson)
- Implement fast-fail with clear error messages instead of long hangs (WI16 lesson)
- Focus on infrastructure root causes rather than test workarounds (WI23 lesson)

### Problems Prevented
- Test hanging for 5+ minutes (WI16 prevented this successfully)
- Silent failures without clear error indication (WI16 prevented this)
- Poor developer experience with long feedback loops (WI16 prevented this)

## Phase 1: Investigation
### Requirements
- Debug why services inside containers (Kafka, Flink, Prometheus) are not ready to accept connections
- Identify container service initialization timing issues
- Analyze service dependency coordination problems
- Evaluate current health check and readiness probe implementation

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - `Connect to ipv4#127.0.0.1:40409 failed: Connection refused (after 0ms in state CONNECT)`
  - `Connect to ipv6#[::1]:40409 failed: Connection refused (after 0ms in state CONNECT)`
  - Kafka producer continuously failing to connect to broker on port 40409
- **Log Locations**: Aspire DCP logs, Kafka container logs, test console output
- **System State**: 
  - Aspire containers start successfully (confirmed in logs)
  - Kafka container is created and running but broker service not accepting connections
  - Port 40409 assigned to Kafka but service not listening on this port internally
- **Reproduction Steps**: 
  1. Run `dotnet test LocalTesting.IntegrationTests --filter "DisplayName~Simple Observability Flow"`
  2. Test reaches container initialization phase successfully 
  3. Kafka producer attempts to connect to localhost:40409
  4. Continuous "Connection refused" errors every 1-2 seconds for 2.5+ minutes
  5. Test eventually times out or fails with infrastructure connection errors
- **Evidence**: 
  - Real-time test execution shows Kafka connection failures after 2.5+ minutes
  - Port allocation appears correct (40409) but service not ready
  - IPv4 and IPv6 connection attempts both fail with "Connection refused"

### Current Infrastructure Analysis
**Container Services Status**:
- Kafka container: Starts but broker not ready for connections
- Flink JobManager: Starts but web UI/RPC not accepting connections  
- Flink TaskManager: Starts but not ready to process jobs
- Prometheus: Starts but may not be ready for metric scraping
- Redis: Works properly (Aspire native service)

**Known Issues**:
1. **Service Initialization Timing**: Containers start but internal services need additional time
2. **Health Check Gaps**: No proper readiness probes for containerized services
3. **Service Dependencies**: Services may be starting before their dependencies are ready
4. **Port Accessibility**: Internal container ports may not be properly exposed/accessible

### Findings
**Root Cause Identified**: Kafka container starts but broker service is not ready to accept connections on allocated port (40409).

**Key Issues**:
1. **Port Mapping Gap**: Aspire allocates external port 40409 but Kafka broker may not be configured to listen on the correct internal port
2. **Service Initialization Timing**: Kafka broker needs time to initialize after container starts
3. **Health Check Absence**: No health checks to verify Kafka broker is ready before allowing connections
4. **Configuration Mismatch**: Possible mismatch between Aspire port allocation and Kafka broker configuration

**Infrastructure Analysis**:
- Aspire DCP successfully starts containers
- Port 40409 correctly allocated to Kafka service
- Kafka producer configured with correct connection string 
- But broker service inside container not accepting connections on this port

### Lessons Learned
- Container startup ≠ service readiness (confirmed by real test execution)
- Kafka broker requires explicit readiness validation beyond container health
- Port allocation success doesn't guarantee service availability on that port
- Need dedicated Kafka broker health checks with proper retry logic

## Phase 2: Design  
### Requirements
Design comprehensive container service readiness solution addressing:
1. Proper health checks for Aspire-managed Kafka service
2. Enhanced service dependency coordination and startup sequencing
3. Service-specific readiness validation with retry logic
4. Configuration optimization for reliable service initialization

### Architecture Decisions
**Health Check Strategy**:
- Use Aspire's `.WaitFor()` with proper health check conditions for all services
- Add custom health checks for Kafka broker readiness beyond container startup
- Implement service-specific port connectivity validation before allowing connections
- Use exponential backoff retry logic for service connection attempts

**Service Coordination Enhancement**:
- Kafka must be fully ready (broker accepting connections) before Flink starts connecting
- Prometheus must be ready before metrics collection begins  
- WebAPI should validate all dependencies are ready before starting workload execution
- Add custom readiness validation methods for each containerized service

**Kafka-Specific Configuration**:
- Keep Aspire's native `AddKafka()` for automatic configuration benefits
- Add custom health checks to validate Kafka broker readiness on assigned port
- Implement port connectivity validation before workload execution
- Add retry logic with proper timeouts for Kafka producer initialization

**Implementation Approach**:
```csharp
// Enhanced Kafka configuration with health checks
var kafka = builder.AddKafka("kafka")
    .WithHealthCheck(); // Add Kafka-specific health validation

// Enhanced service dependencies with proper waiting
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(kafka)
    .WaitFor(kafka); // Wait for Kafka broker to be ready, not just container

// Add custom validation in test infrastructure
private async Task ValidateKafkaReadiness(Uri kafkaEndpoint)
{
    // Implement proper Kafka broker connectivity validation
    // Use AdminClient to verify broker is accepting connections
}
```

### Why This Approach
- Maintains Aspire framework benefits (automatic configuration, service discovery)
- Adds necessary service readiness validation without breaking existing patterns
- Provides reliable infrastructure for both development and CI environments
- Addresses root cause (service readiness) while preserving Aspire orchestration

### Alternatives Considered
- **Replace Aspire Kafka with custom container**: Loses automatic configuration benefits
- **Extended grace periods only**: Already proven insufficient (2.5+ minutes still fails)
- **Manual port configuration**: Could break Aspire service discovery patterns
- **Skip health checks**: Would not solve the fundamental readiness issue

## Phase 3: TDD/BDD
### Test Specifications
- Service readiness validation tests for each containerized service
- End-to-end infrastructure startup timing tests
- Connection reliability tests under various timing conditions

### Behavior Definitions
```gherkin
Given the LocalTesting infrastructure is starting
When all containers are started by Aspire
Then each service inside containers should be ready to accept connections within reasonable time
And the infrastructure should report ready status only when all services are truly ready
And connection attempts should succeed consistently without "Connection refused" errors
```

## Phase 4: Implementation
### Code Changes
**Infrastructure Enhancements Implemented**:
1. **Enhanced Test Infrastructure Validation**: Added `ValidateInfrastructureServiceReadiness()` method with 20-attempt retry logic (100 seconds maximum wait)
2. **Kafka Health Check Endpoint**: Added `/api/observability/kafka-health` endpoint with comprehensive broker connectivity validation
3. **Admin Client Validation**: Implemented `ValidateKafkaConnectivityAsync()` using AdminClient to verify broker metadata and readiness
4. **Aspire Configuration Cleanup**: Simplified Aspire configuration removing incorrect health check parameters

**Test Results After Implementation**:
- Containers still start successfully and Aspire reports healthy status
- Kafka broker still fails with "Connection refused" errors on dynamically allocated port (37393 in latest test)
- Issue persists: Container is running but Kafka service inside container not accepting connections
- Test continues to fail after 2+ minutes with consistent connection errors

### Challenges Encountered
**Root Cause Analysis**:
1. **Aspire Kafka Container Issue**: Aspire's `AddKafka()` creates containers but Kafka broker inside may not be properly configured
2. **Dynamic Port Allocation Problem**: Aspire allocates dynamic external ports (37393, 40409) but Kafka broker not configured to listen on these ports
3. **Service Discovery Gap**: Mismatch between Aspire's port allocation and actual Kafka broker configuration
4. **Container vs Service Readiness**: Container health doesn't guarantee service readiness inside container

**Technical Issues Identified**:
- Kafka broker expects to run on standard port 9092 internally
- Aspire port mapping may not be correctly configured for Kafka service
- Kafka container may need explicit environment configuration for external access
- AdminClient timeout occurs because broker service is genuinely not listening

### Solutions Applied
1. **Enhanced Service Readiness Validation**: Implemented comprehensive retry logic with AdminClient metadata validation
2. **Health Check API**: Added dedicated endpoint for Kafka broker health validation
3. **Improved Error Handling**: Better error messages and logging for debugging infrastructure issues
4. **Aspire Configuration Review**: Cleaned up incorrect health check parameters

**Next Steps Required**:
- Need to investigate Aspire Kafka container configuration and environment variables
- May need to switch to custom Kafka container with explicit port configuration
- Consider using Kafka health check script inside container to verify broker startup

## Phase 5: Testing & Validation
### Test Results
*To be completed during testing phase*

### Performance Metrics
*To be completed during testing phase*

## Phase 6: Owner Acceptance
### Demonstration
*To be completed when work is ready for review*

### Owner Feedback
*To be completed after owner review*

### Final Approval
*To be completed after owner approval*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be documented after completion*

### What Could Be Improved  
*To be documented after completion*

### Key Insights for Similar Tasks
*To be documented after completion*

### Specific Problems to Avoid in Future
*To be documented after completion*

### Reference for Future WIs
*To be documented after completion*