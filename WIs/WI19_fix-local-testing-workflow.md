# WI19: Fix failed local testing GitHub workflow

**File**: `WIs/WI19_fix-local-testing-workflow.md`
**Title**: [LocalTesting] Fix DCP timeout issues in GitHub Actions environment  
**Description**: Fix the failed local testing GitHub workflow by resolving Aspire DCP connectivity issues in CI
**Priority**: High
**Component**: LocalTesting workflow
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-08-01
**Status**: Design

## Phase 1: Investigation (COMPLETED)

### Requirements
- Analyze why local testing workflow fails in GitHub Actions
- Identify root cause of Aspire DCP timeout issues

### Debug Information (MANDATORY - Updated for this investigation)
- **Error Messages**: `System.Net.Sockets.SocketException (61): No data available ([::1]:port)` when DCP API server tries to start
- **Log Locations**: `/LocalTesting/LocalTesting.AppHost/aspire_*.log` files show DCP connection timeouts
- **System State**: Docker running, .NET 8.0.118 available, Aspire 9.3.1 workload installed, IPv6 connectivity working
- **Reproduction Steps**: 
  1. Set required Aspire environment variables
  2. Run `dotnet run --configuration Release` in AppHost directory
  3. DCP starts API server but immediately becomes unreachable after connection attempts
- **Evidence**: DCP successfully detects Docker runtime but API server crashes with IPv6 socket errors

### Findings
- **Root Cause**: Aspire DCP (Distributed Component Platform) fails to maintain stable API server connections in GitHub Actions environment
- **Environment Issue**: DCP starts API server on IPv6 localhost `::1` but connections fail with "No data available" socket error
- **Not a configuration issue**: All required environment variables are properly set
- **Not an IPv6 issue**: IPv6 localhost connectivity works fine with ping tests
- **Scope**: Issue affects any Aspire application in CI environments with containerized/virtualized networking

### Lessons Learned
- Aspire DCP has known compatibility issues with certain CI environments like GitHub Actions
- The issue is at the orchestration platform level, not application configuration
- Need alternative approach for CI environments while keeping Aspire for local development

## Phase 2: Design

### Requirements
Create a solution that works in both local development and CI environments

### Architecture Decisions
**Option 1: Dual Mode Approach (RECOMMENDED)**
- Keep Aspire for local development (developers can use `dotnet run` locally)
- Use Docker Compose for CI environment testing
- Workflow detects environment and chooses appropriate orchestration

**Option 2: Docker Compose Only**
- Replace Aspire entirely with Docker Compose
- Simpler but loses Aspire development benefits

**Option 3: Environment Variable Bypass**
- Detect CI environment and skip DCP-dependent operations
- Run tests directly against manually started containers

### Why This Approach
- **Compatibility**: Docker Compose works reliably in all CI environments
- **Maintainability**: Keeps existing Aspire setup for developers
- **Reliability**: CI tests don't depend on complex orchestration platform
- **Performance**: Faster startup in CI without DCP overhead

### Alternatives Considered
- Fixing DCP IPv6 issues (too complex, may require Aspire framework changes)
- Using different Aspire versions (version-specific issue unlikely to be resolved quickly)
- Disabling DCP entirely (not supported in current Aspire version)

## Phase 3: Implementation Plan

### Code Changes
1. **Create Docker Compose alternative**: `LocalTesting/docker-compose.ci.yml`
2. **Update GitHub workflow**: Detect CI environment and use Docker Compose
3. **Maintain Aspire setup**: Keep existing setup for local development
4. **Add environment detection**: Script logic to choose orchestration method

### Test Approach
1. **Local testing**: Verify Aspire still works for developers
2. **CI testing**: Verify Docker Compose approach works in workflow
3. **Service validation**: Ensure all services start and are accessible
4. **API testing**: Validate that business logic tests still pass

### Configuration Strategy
- Use environment variable `CI=true` to detect GitHub Actions
- Docker Compose will start same services as Aspire with same port mappings
- Maintain service names and connection strings for compatibility

## Phase 4: Expected Outcomes

### Success Criteria
- ✅ Local testing workflow passes in GitHub Actions
- ✅ Developers can still use Aspire locally with `dotnet run`
- ✅ All services (Kafka, Redis, Flink, Temporal, etc.) start correctly in CI
- ✅ Business flow tests execute successfully
- ✅ No regression in local development experience

### Performance Expectations
- CI startup time: <5 minutes (vs current timeout failures)
- Service availability: >95% success rate in CI
- Test reliability: Consistent pass/fail results

## Lessons Learned & Future Reference (MANDATORY)

### What Worked Well
- **Systematic debugging**: Environment variables, minimal reproduction, log analysis
- **Root cause identification**: Focused on DCP layer rather than application configuration
- **Alternative approach**: Instead of fighting platform limitations, work around them

### What Could Be Improved  
- **Earlier environment considerations**: Should have considered CI compatibility during Aspire adoption
- **Fallback planning**: Should have had Docker Compose backup from start

### Key Insights for Similar Tasks
- **Aspire limitations**: DCP has known issues in virtualized/containerized CI environments
- **Hybrid approaches work**: Can use different orchestration for different environments
- **Environment detection**: Use CI environment variables to switch orchestration methods

### Specific Problems to Avoid in Future
- **Don't assume Aspire works everywhere**: Test in target CI environment early
- **Don't debug DCP issues extensively**: Known platform limitation, use workarounds
- **Don't force single orchestration**: Multiple approaches can coexist

### Reference for Future WIs
- **CI orchestration decisions**: Use this WI as reference for choosing CI orchestration tools
- **Environment compatibility**: Always validate platform compatibility in target environments
- **Dual-mode patterns**: Reference this implementation for other local/CI dual-mode needs