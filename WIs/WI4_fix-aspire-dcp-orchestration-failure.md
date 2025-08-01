# WI4: Fix Aspire DCP Orchestration Failure in Local Testing

**File**: `WIs/WI4_fix-aspire-dcp-orchestration-failure.md`
**Title**: [LocalTesting] Fix Aspire DCP orchestration failure preventing container startup
**Description**: Fix critical DCP executor failure that prevents Aspire from starting containers, causing GitHub workflow to fail with "Watch task over Kubernetes Container resources terminated unexpectedly"
**Priority**: High
**Component**: LocalTesting Aspire AppHost
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-08-01
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_implement-real-production-aspire-localtesting.md: Extensive work on production environment implementation
### Lessons Applied  
- Start with health checks and real connectivity verification
- Implement services incrementally with real integration
- Use proven Aspire patterns instead of complex custom configurations
### Problems Prevented
- Avoid monolithic changes - make minimal surgical fixes
- Don't over-engineer - follow established Aspire patterns

## Phase 1: Investigation
### Requirements
Fix the GitHub workflow failure in `.github/workflows/local-testing.yml` by resolving the DCP orchestration failure that prevents containers from starting.

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - "Watch task over Kubernetes Container resources terminated unexpectedly"
  - "System.Net.Http.HttpRequestException: No data available ([::1]:45749)" 
  - "System.Net.Sockets.SocketException (61): No data available"
- **Log Locations**: 
  - `/home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting/LocalTesting.AppHost/aspire_error.log`
  - `/home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting/LocalTesting.AppHost/aspire_output.log`
- **System State**: 
  - Aspire starts DCP API server on IPv6 address `[::1]:port`
  - DCP controller manager attempts to connect but fails with "No data available"
  - No Docker containers are started
  - Aspire dashboard inaccessible on port 18888
- **Reproduction Steps**: 
  1. Even ultra-minimal configuration (just Redis + API) fails
  2. DCP binary exists and has proper permissions
  3. Error occurs regardless of configuration complexity
  4. Issue is consistent across different Aspire service combinations
- **Evidence**: 
  - DCP starts successfully: "API server started {"Address": "::1", "Port": 34477}"
  - Controller manager starts: "starting controller manager"
  - Critical failure: IPv6 connectivity issue - DCP cannot connect to its own API server
  - Version alignment fixed (all 9.3.1) but issue persists
  - Environment variables correctly configured

### Current Configuration Analysis
**LocalTesting.AppHost/Program.cs Issues:**
- Complex container configurations with many environment variables
- Multiple dependencies and wait conditions
- Custom Kafka broker setup with KRaft mode
- Complex Flink cluster setup with multiple containers
- Manual Temporal stack configuration
- May be overwhelming DCP orchestrator

**Aspire Version Compatibility:**
- Using Aspire 9.3.1 packages
- AppHost.Sdk version 9.0.0 (mismatch!)
- Workload version shows 8.x installed

### Root Cause Hypothesis
1. **IPv6 Connectivity Issue**: DCP starts API server on IPv6 `[::1]:port` but cannot connect to it in CI environment
2. **CI Environment Limitations**: GitHub Actions may have IPv6 connectivity restrictions
3. **DCP Orchestration Bug**: Known issue with DCP in certain Linux environments with IPv6/IPv4 configurations
4. **Container Runtime Configuration**: DCP detects multiple runtimes (podman, docker) which may cause internal connectivity issues

### Updated Analysis
The issue is **NOT** version mismatch or configuration complexity. Even with:
- Aligned versions (all 9.3.1)
- Ultra-minimal configuration (just Redis + API)
- Proper environment variables
- Valid DCP binary and permissions

The DCP orchestrator consistently fails with IPv6 connectivity issues. This suggests a fundamental incompatibility between Aspire DCP and the GitHub Actions environment.

### Lessons Learned
- DCP orchestration failures often stem from configuration complexity or version mismatches
- Aspire works best with simpler configurations that can be scaled up gradually
- Version alignment across all Aspire components is critical

## Phase 2: Design  
### Requirements
- Fix version alignment across all Aspire components
- Simplify container configuration to reduce DCP load
- Follow proven Aspire patterns for Temporal integration
- Ensure minimal changes that address root cause

### Architecture Decisions
**Approach 1 - Version Alignment**: Update all Aspire references to consistent version
**Approach 2 - Configuration Simplification**: Reduce container complexity and dependencies  
**Approach 3 - Reference Implementation**: Follow aspire-temporal patterns for proven configuration

### Why This Approach
- Minimal surgical changes to fix root cause
- Version alignment often resolves DCP orchestration issues
- Simpler configurations are more reliable in CI environments
- Following proven patterns reduces risk

### Alternatives Considered
- **Complete Rewrite**: Rejected - too invasive, risk of breaking working features
- **Docker Compose Fallback**: Rejected - requirement explicitly states "always use Aspire"
- **Disable Services**: Rejected - would break existing functionality

## Phase 3: TDD/BDD
### Test Specifications
- DCP orchestration starts successfully without errors
- Aspire dashboard accessible on port 18888
- All required containers start within timeout period
- GitHub workflow passes end-to-end

### Behavior Definitions
- Given Aspire configuration is valid, When AppHost starts, Then DCP should start containers successfully
- Given containers are running, When dashboard is accessed, Then it should be available
- Given all services are healthy, When workflow runs, Then it should pass without timeout

## Phase 4: Implementation
### Code Changes
*[Implementation steps will be documented here]*

## Phase 5: Testing & Validation
### Test Results
*[Test results will be documented here]*

## Phase 6: Owner Acceptance
### Demonstration
*[Demonstration results will be documented here]*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*[To be documented during implementation]*

### What Could Be Improved  
*[To be documented during implementation]*

### Key Insights for Similar Tasks
*[To be documented during implementation]*

### Specific Problems to Avoid in Future
*[To be documented during implementation]*

### Reference for Future WIs
*[To be documented during implementation]*