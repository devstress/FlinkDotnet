# WI6: Fix LocalTesting Root Cause

**File**: `WIs/WI6_fix-localtesting-root-cause.md`
**Title**: Fix LocalTesting root cause failures
**Description**: Debug and fix the root cause of LocalTesting failures after previous connectivity fixes
**Priority**: High
**Component**: LocalTesting infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2024-09-15
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1: FlinkDotNet completion with Java build integration
- Previous commits: 12594ff, cfb23fb, eb12d29
### Lessons Applied  
- Follow TDD and debug-first approach
- Fix environment issues before code changes
- Check .NET SDK version compatibility
### Problems Prevented
- Avoiding quick fixes without understanding root cause
- Not skipping environment validation steps

## Phase 1: Investigation
### Requirements
- Debug LocalTesting failures completely
- Identify root cause of test hangs and connectivity issues
- Fix environment setup issues

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  - .NET SDK not found: "Requested SDK version: 9.0.100" but only 8.0.119 available - FIXED
  - LocalTesting still failing according to user comment  
  - NEW: "Connection refused (localhost:8081)" - Test trying to connect to wrong port
- **Log Locations**: 
  - Test output shows container networking working between Gateway and Flink cluster
  - Test fails when trying to access Flink REST API from host via localhost:8081
- **System State**: 
  - .NET 9.0 SDK now installed and working
  - Docker is running
  - Aspire workload installed
  - Container networking working (Gateway -> Flink cluster)
  - Test connectivity issue: localhost:8081 vs container port mapping
- **Reproduction Steps**: 
  1. Run LocalTesting tests
  2. Tests pass startup and container readiness
  3. Tests fail when trying direct access to Flink REST API
  4. Issue: Flink container port 8081 not accessible from test host
- **Evidence**: 
  - Gateway logs show successful connection to flink-jobmanager:8081
  - Test logs show "Connection refused (localhost:8081)"
  - Test runs for ~150 seconds before timing out

### Current Issues Identified
1. **Environment Issue**: .NET SDK path - FIXED
2. **Test Execution**: Tests now run much longer - PROGRESS
3. **Container Networking**: Gateway to Flink works - FIXED
4. **Host to Container**: Test cannot access Flink REST API on localhost:8081 - NEW ISSUE

### Root Cause Found
The test is trying to access the Flink REST API directly via `localhost:8081`, but Aspire containers don't automatically expose all ports to localhost. The Flink JobManager container is running with port 8081 internally, but the test needs to access it via the Aspire-mapped port.

### Next Steps
1. Check Aspire container port mapping for Flink JobManager
2. Update test to use correct external port for Flink access
3. Alternative: Use Gateway health check instead of direct Flink access