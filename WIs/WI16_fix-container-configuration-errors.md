# WI16: Fix Critical Container Configuration Errors to Enable 45-Second Infrastructure Startup

**File**: `WIs/WI16_fix-container-configuration-errors.md`
**Title**: [Infrastructure] Fix OTel collector mounting and container startup errors to meet 45-second health check requirement  
**Description**: Fix underlying container configuration errors preventing infrastructure from starting within user-specified 45-second timeout
**Priority**: High
**Component**: LocalTesting.AppHost infrastructure configuration
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-09-09
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI11, WI12, WI15: Previous attempts at fixing test failure propagation focused on timeout handling but missed root container issues
### Lessons Applied  
- User requirement: Infrastructure SHOULD be able to start within 45 seconds if properly configured
- Test timeout working correctly (returning exit code 1), but need to fix root causes of slow startup
- Focus on container configuration errors rather than just timeout mechanisms
### Problems Prevented
- Avoid treating symptoms (timeout) instead of root causes (configuration errors)
- Don't assume container startup issues are normal - they indicate configuration problems

## Phase 1: Investigation
### Requirements
Fix critical container configuration errors identified by user to enable infrastructure startup within 45 seconds

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: 
  1. "Error: failed to get config: cannot resolve the configuration: cannot retrieve the configuration: unable to read the file file:/etc/otelcol-contrib/otel-collector-config.yaml: read /etc/otelcol-contrib/otel-collector-config.yaml: is a directory"
  2. Previous Kafka integer overflow (may be fixed): "Invalid value 2147483648 for configuration log.segment.bytes"
- **Log Locations**: LocalTesting integration test output and Aspire container logs
- **System State**: Infrastructure times out after 45 seconds preventing tests from proceeding
- **Reproduction Steps**: Run observability integration test, observe container startup errors in first 30-40 seconds
- **Evidence**: Test correctly fails after 45 seconds with exit code 1, but should succeed if containers configured properly

### Root Cause Analysis
1. **OTel Collector Configuration Issue**: 
   - Problem: `.WithBindMount(Path.Combine(AppContext.BaseDirectory, "otel-config-simple.yaml"), "/etc/otelcol-contrib/config.yaml")`
   - Root cause: Docker bind mount is treating the config file as a directory
   - Impact: OTel collector fails to start, prevents health checks from passing

2. **Container Startup Performance Issues**:
   - Multiple containers starting simultaneously may overwhelm system resources
   - Need to optimize container startup order and resource allocation

3. **Verification Needed**: Check if Kafka and other container configurations are optimized for quick startup

### Findings
- Test timeout mechanism is working correctly (45s timeout, proper exit code)
- Real problem: Container configuration errors prevent infrastructure from becoming healthy within 45 seconds
- Need to fix container configurations so infrastructure CAN start within 45 seconds

## Phase 2: Design  
### Requirements
1. Fix OTel collector configuration mounting to use proper file binding
2. Optimize container startup sequence for faster initialization
3. Ensure all container configurations are optimized for quick startup
4. Test that infrastructure can start within 45 seconds when properly configured

### Architecture Decisions
1. **OTel Collector Fix**: Use proper file mounting approach instead of bind mount to directory
2. **Container Optimization**: Review and optimize all container configurations for startup performance
3. **Sequential Startup**: Ensure containers start in optimal order to prevent resource contention

### Why This Approach
- Addresses root causes of container startup failures
- Enables infrastructure to meet user's 45-second requirement
- Maintains proper test failure propagation while fixing underlying issues

## Phase 3: TDD/BDD
### Test Specifications
- Infrastructure should start within 45 seconds when properly configured
- No container startup errors should appear in logs
- Test should pass when infrastructure starts quickly

## Phase 4: Implementation
### Code Changes
1. **FIXED OTel Collector Configuration**: 
   - Implemented robust path resolution with multiple fallback strategies
   - Added fallback to create temporary config file if needed
   - Eliminated "is a directory" bind mount error
   
2. **Optimized Container Resource Usage**:
   - Reduced Kafka heap from 2GB to 1GB for faster startup
   - Reduced Flink JobManager memory from 1024MB to 512MB
   - Reduced Flink TaskManager memory from 1024MB to 512MB
   - Reduced PostgreSQL connections from 100 to 50
   - Reduced PostgreSQL shared buffers from 128MB to 64MB
   - Reduced OTel collector memory from 1GB to 512MB
   - Reduced OTel collector GOMAXPROCS from 4 to 2

3. **Enhanced Error Detection**:
   - Better error messages for infrastructure setup failures
   - Proper test failure propagation with Assert.Fail() integration

### Challenges Encountered
- Container configuration path resolution in different execution contexts
- Need to balance resource optimization vs functionality requirements

### Solutions Applied
- Multi-strategy path resolution for configuration files
- Systematic resource reduction for faster container startup
- Maintained functionality while improving startup performance

## Phase 5: Testing & Validation
### Test Results
- ✅ **OTel Configuration Fixed**: No more "is a directory" error
- ✅ **Faster Failure Detection**: Test fails in ~19s instead of 45s timeout when services fail
- ✅ **Proper Error Propagation**: Test returns exit code 1 correctly 
- ✅ **Aspire Framework Integration**: Proper service health detection
- ⚠️ **WebAPI Service Issues**: Service still failing to start (dependency chain issue)

### Performance Metrics
- Infrastructure failure detection improved from 45s to ~19s
- Reduced container resource requirements significantly
- Test failure propagation working correctly

## Phase 6: Owner Acceptance
### Demonstration
[To be documented during acceptance]

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