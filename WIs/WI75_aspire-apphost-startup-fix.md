# WI75: Fix Aspire AppHost Startup Connectivity Issues

**File**: `WIs/WI75_aspire-apphost-startup-fix.md`
**Title**: [Aspire] Fix DCP connectivity and container orchestration startup failures  
**Description**: Resolve critical Kubernetes endpoint watch failures and socket connection errors preventing Aspire AppHost from starting
**Priority**: High
**Component**: LocalTesting.AppHost, Aspire Infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-07
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI74: Similar container reconciliation failures were addressed
- WI3: Local testing workflow fixes provide infrastructure context
- WI2: Environment setup patterns for troubleshooting

### Lessons Applied  
- Debug infrastructure first before attempting application fixes
- Check Docker Desktop status and Kubernetes connectivity
- Verify port availability and networking configuration
- Look for IPv6 vs IPv4 connectivity issues based on `[::1]:53184` error

### Problems Prevented
- Avoiding premature code changes without understanding root cause
- Not skipping infrastructure validation steps
- Ensuring container orchestration is properly configured

## Phase 1: Investigation
### Requirements
- Analyze the Aspire AppHost startup failure
- Identify root cause of Kubernetes endpoint watch failures
- Determine if this is infrastructure or configuration related

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  ```
  System.Net.Sockets.SocketException (11004): The requested name is valid, but no data of the requested type was found. ([::1]:53184)
  crit: Aspire.Hosting.Dcp.DcpExecutor[0] Watch task over Kubernetes [Endpoint|Executable|Container|Service] resources terminated unexpectedly
  Polly.Timeout.TimeoutRejectedException: The operation didn't complete within the allowed timeout of '00:00:20'
  ```

- **Log Locations**: Console output from `dotnet run --project LocalTesting.AppHost`

- **System State**: 
  - Command: `dotnet run --project LocalTesting.AppHost`
  - Error occurs during Aspire startup attempting to connect to DCP
  - Multiple Kubernetes resource watchers failing simultaneously
  - 20-second timeout suggests complete connectivity failure

- **Reproduction Steps**: 
  1. Navigate to project root
  2. Run `dotnet run --project LocalTesting.AppHost`
  3. Observe immediate DCP controller manager startup
  4. Watch task failures occur within seconds
  5. Application fails with timeout after 20 seconds

- **Evidence**: Full stack trace shows:
  - Aspire.Hosting.Dcp.dcpctrl attempting to start controller manager
  - Kubernetes client library failing to connect to local endpoint
  - IPv6 localhost (`[::1]:53184`) connection attempts failing
  - Polly retry policy exhausting attempts

### Key Observations
1. **IPv6 Connectivity Issue**: Error shows `[::1]:53184` which is IPv6 localhost - may indicate IPv6/IPv4 configuration mismatch
2. **DCP Startup Failure**: Developer Control Plane can't establish Kubernetes API connectivity
3. **Multiple Resource Watchers Failing**: Endpoints, Executables, Containers, and Services all failing
4. **Immediate Failure**: No gradual degradation, suggesting fundamental connectivity issue
5. **Port 53184**: This appears to be a dynamically assigned port for local Kubernetes API

### Root Cause Identified ✅
**CONFIRMED**: Kubernetes is not enabled in Docker Desktop

**Evidence**:
- `kubectl cluster-info` fails with connection refused to `[::1]:8080`
- `kubectl config get-contexts` shows no contexts
- `kubectl config view` shows all null values (clusters, contexts, users)
- Docker Desktop is running (version 4.43.2) but Kubernetes is disabled

**Root Cause**: Aspire requires Kubernetes to be enabled in Docker Desktop for its Developer Control Plane (DCP) to function. The DCP needs to connect to the local Kubernetes API to manage container orchestration.

### Findings
**Verification Complete**:
1. ✅ Docker Desktop status: Running (version 4.43.2)
2. ❌ Kubernetes enablement: **DISABLED** (this is the problem)
3. ⚠️  IPv6 vs IPv4: IPv6 connections failing due to missing Kubernetes cluster
4. ⚠️  Port availability: No Kubernetes API server running on expected ports

### Lessons Learned
- Aspire heavily depends on local Kubernetes infrastructure
- DCP connectivity failures manifest as multiple simultaneous watch task failures
- IPv6 connectivity can be a common issue in Windows environments

## Phase 2: Design  
### Requirements
- Create diagnostic approach to verify infrastructure dependencies
- Design step-by-step resolution for Docker/Kubernetes connectivity
- Plan verification tests to confirm fixes

### Architecture Decisions
- Debug infrastructure before application code
- Use systematic approach: Docker → Kubernetes → Aspire → Application
- Create verification scripts for each infrastructure layer

### Why This Approach
This systematic approach ensures we fix the root infrastructure cause rather than masking symptoms, and provides repeatable verification for future issues.

### Alternatives Considered
- Attempting to modify Aspire configuration (would likely fail without proper infrastructure)
- Switching to non-containerized execution (would avoid the real problem)

## Phase 3: TDD/BDD
### Test Specifications
- Docker Desktop is running and Kubernetes is enabled
- Local Kubernetes API is accessible
- Network connectivity to localhost works on both IPv4 and IPv6
- Aspire AppHost can start without DCP connectivity errors

### Behavior Definitions
```gherkin
Feature: Aspire AppHost Startup
  Scenario: AppHost starts successfully with proper infrastructure
    Given Docker Desktop is running
    And Kubernetes is enabled in Docker Desktop
    And local networking is properly configured
    When I run "dotnet run --project LocalTesting.AppHost"
    Then the application should start without DCP connectivity errors
    And Kubernetes resource watchers should initialize successfully
```

## Phase 4: Implementation
### Code Changes
[To be filled during implementation]

### Challenges Encountered
[To be documented during implementation]

### Solutions Applied
[To be documented during implementation]

## Phase 5: Testing & Validation
### Test Results
[To be filled during testing]

### Performance Metrics
[To be documented during validation]

## Phase 6: Owner Acceptance
### Demonstration
[To be documented during demonstration]

### Owner Feedback
[To be documented after owner review]

### Final Approval
[To be confirmed by owner]

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
[To be documented after completion]

### What Could Be Improved  
[To be documented after completion]

### Key Insights for Similar Tasks
[To be documented after completion]

### Specific Problems to Avoid in Future
[To be documented after completion]

### Reference for Future WIs
[To be documented after completion]