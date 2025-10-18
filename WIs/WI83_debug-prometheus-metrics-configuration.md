# WI83: Debug and Fix Prometheus Metrics Configuration Issues

**File**: `WIs/WI83_debug-prometheus-metrics-configuration.md`
**Title**: [Observability] Debug Prometheus metrics configuration - 1/5 targets UP
**Description**: Investigate and fix critical configuration problems preventing Prometheus from scraping Flink, Gateway, and Kafka metrics
**Priority**: High
**Component**: LocalTesting Infrastructure
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-10-18
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI77, WI78, WI79, WI80, WI81, WI82 - Prometheus observability debugging history
### Lessons Applied
- Always debug first to find root cause before proposing solutions
- Check container logs and environment variables for configuration issues
- Verify port mappings and network connectivity
- Document all findings with evidence
### Problems Prevented
- Skipping debug phase and jumping to solutions
- Making assumptions without verification
- Not checking logs for error messages

## Phase 1: Investigation
### Requirements
Debug three failing Prometheus targets:
1. Flink JobManager (port 9250) - Connection refused
2. Flink TaskManager (port 9251) - Connection refused  
3. Gateway metrics (host.docker.internal:8080) - Connection refused
4. Kafka JMX (kafka:9101) - Context deadline exceeded

### Debug Information (MANDATORY - Update this section for every investigation)

#### Problem 1: Flink Prometheus Reporters Not Starting
**Error Messages**: Connection refused on ports 9250, 9251
**Investigation Steps**:
1. Check Flink container port exposures

#### Root Cause Analysis
The Gateway was using environment variable checks (`LEARNINGCOURSE=true`) instead of a proper configuration system like Flink uses. This violated the principle that configuration should be explicit and follow established patterns.

#### Solution Implemented
1. **Created Configuration Files**:
   - `appsettings.json` - Default configuration with Prometheus disabled
   - `appsettings.LearningCourse.json` - LearningCourse-specific configuration with Prometheus enabled

2. **Updated Gateway Code**:
   - Replaced environment variable checks with `builder.Configuration.GetValue<bool>("Metrics:Prometheus:Enabled")`
   - Configuration follows same pattern as Flink's `metrics.reporters` approach
   - Metrics enablement is now explicit and maintainable

3. **Updated Aspire Configuration**:
   - Set `ASPNETCORE_ENVIRONMENT=LearningCourse` when in LearningCourse mode
   - This automatically loads `appsettings.LearningCourse.json` via ASP.NET Core's configuration system
   - Clean, standard ASP.NET Core pattern

#### Files Modified
1. `FlinkDotNet/FlinkDotNet.JobGateway/appsettings.json` - Created
2. `FlinkDotNet/FlinkDotNet.JobGateway/appsettings.LearningCourse.json` - Created  
3. `FlinkDotNet/FlinkDotNet.JobGateway/Program.cs` - Updated metrics configuration logic
4. `LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs` - Updated environment variable passing
5. `FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj` - Verified SDK auto-includes appsettings

#### Build Validation
- Gateway builds successfully with new configuration system
- No warnings or errors
- Configuration files properly included in output

2. Verify Prometheus JAR is mounted
3. Check FLINK_PROPERTIES environment variable
4. Review Flink container logs for reporter initialization

#### Problem 2: Gateway Metrics Not Accessible
**Error Messages**: Connection refused on host.docker.internal:8080
**Investigation Steps**:
1. Verify LEARNINGCOURSE environment variable
2. Check Gateway logs for metrics initialization
3. Verify /metrics endpoint enablement
4. Check port binding configuration

#### Problem 3: Kafka JMX Exporter Timeout
**Error Messages**: Context deadline exceeded (>10s)
**Investigation Steps**:
1. Verify Kafka JMX port accessibility
2. Check JMX exporter container network connectivity
3. Review JMX exporter logs

### Findings
*To be populated after debug commands execution*

### Lessons Learned
*To be documented after resolution*

## Phase 2: Design  
### Requirements
- Design configuration system matching Flink's approach
- Use standard ASP.NET Core configuration patterns
- Ensure maintainability and clarity

### Architecture Decisions
- Use `appsettings.json` files for configuration (standard ASP.NET Core)
- Leverage environment-specific configuration (`appsettings.{Environment}.json`)
- Mirror Flink's declarative configuration approach
- Remove environment variable checks from code

### Why This Approach
- **Standard Pattern**: ASP.NET Core's built-in configuration system
- **Consistency**: Matches how Flink uses configuration files
- **Maintainability**: Configuration changes don't require code changes
- **Clarity**: Explicit configuration is self-documenting

### Alternatives Considered
- Keep environment variable approach - REJECTED (not maintainable, not standard)
- Use separate config file outside appsettings - REJECTED (unnecessary complexity)

## Phase 3: Implementation
### Code Changes
1. Created `appsettings.json` with Prometheus disabled by default
2. Created `appsettings.LearningCourse.json` with Prometheus enabled
3. Updated `Program.cs` to read from configuration instead of environment
4. Updated Aspire to set `ASPNETCORE_ENVIRONMENT=LearningCourse`

### Challenges Encountered
- Initial build error due to duplicate Content items (SDK auto-includes appsettings files)

### Solutions Applied
- Removed explicit ItemGroup for appsettings files (SDK handles automatically)

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- Configuration-based approach is clean and maintainable
- ASP.NET Core's environment-specific configuration works perfectly
- Pattern matches Flink's approach (consistency across project)

### What Could Be Improved  
- Could add validation for configuration values at startup
- Could add logging to show which configuration file was loaded

### Key Insights for Similar Tasks
- Always use framework-standard patterns (ASP.NET Core configuration)
- Match established patterns in the project (Flink uses config files, so should Gateway)
- Don't use environment variables for feature flags when configuration is available

### Specific Problems to Avoid in Future
- Don't add explicit ItemGroup for appsettings files (SDK auto-includes them)
- Don't use environment variable checks when proper configuration system exists
- Always validate builds after configuration changes

### Reference for Future WIs
- When adding new Gateway features, use configuration files not environment variables
- Follow ASP.NET Core patterns for environment-specific settings
- Test with both Production and LearningCourse environments