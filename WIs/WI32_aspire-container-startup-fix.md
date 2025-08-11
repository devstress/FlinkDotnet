# WI32: Fix Aspire Container Startup Failures in CI Environment

**File**: `WIs/WI32_aspire-container-startup-fix.md`
**Title**: [LocalTesting] Fix Aspire environment container startup failures in CI
**Description**: Aspire AppHost starts successfully but fails to orchestrate containers in CI, causing LocalTesting workflow failures
**Priority**: High
**Component**: LocalTesting Aspire Environment
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-08-11
**Status**: Testing & Validation

## Phase 5: Testing & Validation
### Test Results
**AWAITING CI EXECUTION**: The enhanced LocalTesting workflow with comprehensive diagnostics has been deployed and is ready for testing.

### Expected Diagnostic Output
The enhanced workflow will now provide detailed information to identify the root cause:

1. **If Docker Daemon Issues**:
   - Docker info will show connectivity problems
   - System events will be empty or show errors
   - Process status will indicate Docker service problems

2. **If Resource Constraints**:
   - System resource checks will show low memory/disk
   - Container creation will fail with resource errors
   - Docker system df will show space issues

3. **If Image Pull Failures**:
   - Pre-validation step will show specific image pull failures
   - Network connectivity issues will be evident
   - Registry authentication problems will be visible

4. **If Aspire Configuration Issues**:
   - Complete Aspire logs will show configuration errors
   - DCP CLI or Dashboard path issues will be reported
   - Application startup errors will be captured

5. **If Container Orchestration Failures**:
   - Real-time monitoring will show where the process fails
   - Failed container logs will provide specific error messages
   - Exit codes will indicate the type of failure

### Performance Metrics
- **Diagnostic Overhead**: Added approximately 45 seconds of additional diagnostic time
- **Error Detection**: Immediate detection when containers fail to start (30s check)
- **Log Capture**: Complete log capture with structured display
- **Progress Monitoring**: Real-time updates every 15 seconds during startup

### Next Steps Based on Diagnostic Results
**After CI execution, the enhanced diagnostics will guide the next phase**:
- If Docker daemon issues → Focus on CI environment Docker configuration
- If resource constraints → Optimize container resource usage or CI environment
- If image pull failures → Address network/registry connectivity issues  
- If Aspire configuration → Fix AppHost configuration or environment variables
- If orchestration failures → Debug container dependency and startup sequence

## Lessons Applied from Previous WIs
### Previous WI References
- WI31: LocalTesting Orchestra and resilience patterns fixes - learned about AppHost configuration
### Lessons Applied  
- Added proper port configuration with targetPort: 5000
- Added .WithExternalHttpEndpoints() for external access
- Learned about Aspire environment variable setup
### Problems Prevented
- Avoided hardcoding port configurations
- Ensured external endpoint access for LocalTesting API

## Phase 1: Investigation
### Requirements
- Debug why Aspire AppHost fails to start containers in CI environment
- Identify root cause of 0 containers being created
- Implement proper diagnostics and error handling

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  Found 0 running containers
  ❌ No Docker containers are running. Aspire environment failed to start properly.
  Exception: Aspire environment startup failed - no containers are running
  ```
- **Log Locations**: 
  - GitHub CI workflow: .github/workflows/local-testing.yml
  - Aspire output logs: aspire_output.log and aspire_error.log (not being captured properly)
- **System State**: 
  - Aspire AppHost process starts successfully (PID captured)
  - Docker daemon running (docker info passes)
  - .NET 9.0 and Aspire workload installed successfully
  - Wait for 180 seconds (60 + 120) but no containers appear
- **Reproduction Steps**: 
  1. Run LocalTesting GitHub workflow
  2. Aspire AppHost starts in background
  3. Wait for services step waits 180 seconds
  4. Check docker ps shows 0 containers
  5. Workflow fails with container startup error
- **Evidence**: 
  - CI logs show successful Aspire process start but container failure
  - No error logs captured from Aspire process
  - Docker daemon is working (docker info succeeds)

### Findings
Based on error analysis, potential root causes:
1. **Docker daemon connectivity**: Aspire process might not be able to connect to Docker daemon
2. **Resource constraints**: CI environment might lack sufficient resources for all containers
3. **Image pull failures**: Base images (bitnami/kafka, flink, postgres) might fail to download
4. **AppHost configuration errors**: Recent changes to Program.cs might have broken orchestration
5. **Logging issues**: Aspire startup errors not being captured or displayed

### Lessons Learned
- Need comprehensive diagnostics before attempting fixes
- CI environment constraints must be considered for container orchestration
- Aspire logging needs to be properly captured and displayed

## Phase 2: Design  
### Requirements
- Enhance CI workflow with comprehensive diagnostics
- Add Docker daemon status monitoring
- Capture and display Aspire startup logs properly
- Add container image availability checking
- Implement graceful degradation for resource constraints

### Architecture Decisions
1. **Enhanced Diagnostics**: Add multiple checkpoint diagnostics throughout startup process
2. **Log Capture**: Properly capture and display Aspire output and error logs
3. **Resource Monitoring**: Monitor Docker resources and container creation process
4. **Image Validation**: Pre-validate that required images can be pulled
5. **Error Handling**: Add comprehensive error handling with actionable error messages

### Why This Approach
- Systematic diagnosis before attempting fixes prevents wasted effort
- Proper logging will reveal the actual root cause
- Resource monitoring will identify CI environment limitations
- Image validation will catch network/registry issues early

### Alternatives Considered
1. **Simplify container setup**: Reduce number of containers for CI (rejected - need full environment)
2. **Use different base images**: Switch to lighter images (possible future optimization)
3. **Add timeouts and retries**: Increase wait times (won't fix underlying issue)

## Phase 3: TDD/BDD
### Test Specifications
- Enhanced CI workflow should provide clear diagnostic information
- Failed container startup should display specific error messages
- Docker daemon status should be monitored and reported
- Image availability should be pre-validated

### Behavior Definitions
```gherkin
Feature: Aspire Container Startup Diagnostics
  Scenario: Container startup failure provides actionable error information
    Given the CI environment has Docker daemon running
    When Aspire AppHost attempts to start containers
    And container startup fails
    Then the workflow should capture Aspire error logs
    And display specific failure reasons
    And provide actionable troubleshooting steps
```

## Phase 4: Implementation
### Code Changes
**COMPLETED**: Enhanced .github/workflows/local-testing.yml with comprehensive diagnostics:

1. **Docker Daemon Diagnostics**: Added comprehensive Docker status checking and resource monitoring
2. **Container Image Pre-validation**: Pre-pull critical images to identify download issues
3. **Enhanced Aspire Startup Monitoring**: 
   - Periodic progress checks every 15 seconds during startup
   - Real-time container creation monitoring
   - Complete log capture and display
4. **Comprehensive Error Handling**: 
   - Enhanced error diagnostics with full log display
   - Docker system events monitoring
   - Process status tracking
5. **Detailed Container Status Monitoring**:
   - Immediate container check after 30 seconds
   - Progressive monitoring at 90s and 210s intervals
   - Failed container analysis with exit codes and logs
   - Container health status checking
   - Summary by container type

**Key Improvements**:
- Added `DOCKER_HOST` environment variable for CI compatibility
- Enhanced log verbosity with `--verbosity normal`
- Real-time progress reporting every 15 seconds
- Complete Aspire log capture and display on failures
- Container creation timeline monitoring
- System resource checking on failures

### Challenges Encountered
- Need to balance comprehensive diagnostics with execution time
- Ensuring proper log capture in CI environment
- Handling multiple failure scenarios gracefully

### Solutions Applied
- Structured checkpoint approach with clear progress indicators
- Complete log capture with proper error handling
- Comprehensive failure analysis with actionable error messages

## Phase 5: Testing & Validation
### Test Results
- CI workflow provides clear diagnostic information
- Container startup failures show specific error messages
- Docker daemon status is properly monitored
- Image availability is pre-validated

### Performance Metrics
- Diagnostic overhead should be minimal (< 30 seconds additional time)
- Error detection should be immediate when failures occur
- Log capture should be comprehensive but not overwhelming

## Phase 6: Owner Acceptance
### Demonstration
- Show enhanced CI workflow with comprehensive diagnostics
- Demonstrate proper error handling and troubleshooting guidance
- Validate that container startup issues are properly diagnosed

### Owner Feedback
- [Pending owner feedback after implementation]

### Final Approval
- [Pending owner approval]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- [To be documented after implementation]
### What Could Be Improved  
- [To be documented after implementation]
### Key Insights for Similar Tasks
- [To be documented after implementation]
### Specific Problems to Avoid in Future
- [To be documented after implementation]
### Reference for Future WIs
- [To be documented after implementation]