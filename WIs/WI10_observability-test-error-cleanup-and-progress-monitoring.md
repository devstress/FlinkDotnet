# WI10: Observability Test Error Cleanup and Progress Monitoring

**File**: `WIs/WI10_observability-test-error-cleanup-and-progress-monitoring.md`
**Title**: [LocalTesting] Clean up observability test errors/warnings and add progress monitoring  
**Description**: Fix errors and warnings in observability tests, ensure .NET 9 compatibility, and add background progress monitoring task
**Priority**: High
**Component**: LocalTesting.IntegrationTests
**Type**: Bug Fix + Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-28
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI1_observability-test-debug.md - Previous observability debugging work
- WI5_day04-enterprise-observability-implementation.md - Enterprise observability patterns
- WI6_comprehensive-course-testing-and-documentation-update.md - Testing validation patterns

### Lessons Applied  
- Follow established patterns from WI1 for observability debugging
- Use .NET 9.0 enforcement rules from existing guidelines
- Apply systematic debugging approach before proposing solutions
- Implement background service patterns from existing codebase examples

### Problems Prevented
- Avoid making changes without understanding current error state
- Prevent .NET version compatibility issues
- Avoid incomplete progress monitoring implementation

## Phase 1: Investigation
### Requirements
1. Upgrade local environment to .NET 9.0 SDK as required by global.json
2. Run observability tests to identify all errors and warnings in logs
3. Analyze root causes of log errors and warnings
4. Design background progress monitoring task for test timeout scenarios
5. Ensure no logs errors/warnings exist after fixes

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current Environment**: .NET 9.0.304 installed and Aspire workload configured ✅
- **Installation Status**: ✅ .NET 9.0 SDK installed and working
- **Test Framework**: Using Aspire testing framework with DistributedApplication pattern
- **Current Errors Identified**: 
  * **Redis warnings**: Memory overcommit and config file warnings
  * **Loki errors**: "error getting ingester clients" err="empty ring"
  * **Kafka warnings**: UnknownHostException for kafka-broker-2:9093 (missing broker)
  * **Prometheus connection failures**: "Resource temporarily unavailable (prometheus:9090)"
- **Progress Monitoring**: No existing background progress monitoring for test timeouts
- **Test Status**: Tests pass but with significant infrastructure log noise

### Findings
- ✅ Project requires .NET 9.0 SDK for Aspire testing framework functionality
- ✅ ObservabilityMetricsSteps.cs implements Aspire testing patterns
- 🔍 **Infrastructure Issues Identified**:
  * Redis container not properly configured (memory overcommit, missing config)
  * Loki ring configuration incomplete
  * Kafka cluster missing broker-2 (only broker-1 and broker-3 available) 
  * Prometheus experiencing connection reliability issues
- Background progress monitoring should follow BackgroundService patterns from existing codebase
- Tests technically pass but infrastructure logs are very noisy with warnings/errors

### Lessons Learned
- Always verify .NET version requirements before starting debugging
- Aspire testing framework has specific .NET 9.0 dependencies
- Systematic approach required: install dependencies → run tests → identify issues → fix systematically

## Phase 2: Design  
### Requirements
1. **Fix Redis warnings**:
   - Add Redis configuration file to eliminate "no config file specified" warning
   - Configure memory settings to handle memory overcommit warnings
2. **Fix Loki empty ring errors**:
   - Add proper Loki configuration file for single-node setup
   - Configure ingester properly for LocalTesting environment
3. **Reduce Kafka startup noise**:
   - Add initial delay or health checks to reduce connectivity warnings during cluster formation
4. **Improve Prometheus reliability**:
   - Add startup delays and retry mechanisms
   - Configure health checks properly
5. **Add background progress monitoring**:
   - Implement BackgroundService to monitor test progress
   - Stop test if no progress for 5+ seconds
   - Report progress every 5 seconds

### Architecture Decisions
- **Config file approach**: Add configuration files for Redis and Loki instead of environment variables only
- **Sequential startup**: Maintain existing sequential startup but add health checks
- **Progress monitoring**: Add BackgroundService in ObservabilityMetricsSteps
- **Log level control**: Set appropriate log levels to reduce noise without hiding real issues

### Why This Approach
- Configuration files provide better control over service behavior than environment variables alone
- Sequential startup with health checks reduces initial connection errors
- BackgroundService pattern is already established in the codebase
- Proper logging levels maintain visibility while reducing noise

### Alternatives Considered
- **Environment variables only**: Rejected due to limited configuration options
- **Parallel startup**: Rejected due to existing DCP reconciliation issues  
- **External monitoring tool**: Rejected for minimal change approach
- **Disabling logging**: Rejected as it hides real issues

## Phase 3: TDD/BDD
### Test Specifications
1. **Infrastructure logs test**: Verify Redis configuration eliminates warnings
2. **Loki ring test**: Verify proper single-node configuration eliminates empty ring errors
3. **Kafka connectivity test**: Verify reduced connection warnings during cluster formation
4. **Progress monitoring test**: Verify background monitoring detects stalled tests
5. **Test timeout test**: Verify progress monitoring stops tests after 5+ minutes of inactivity

### Behavior Definitions
- **Given** the observability test infrastructure is running
- **When** Redis starts with configuration file
- **Then** no "config file not specified" warnings appear
- **And** no memory overcommit warnings appear
- **When** Loki starts with proper single-node configuration
- **Then** no "empty ring" errors appear
- **When** Kafka cluster forms
- **Then** connection warnings are minimized during startup
- **When** observability test runs with progress monitoring
- **Then** progress is reported every 5 seconds
- **And** test stops if no progress for 5+ minutes

## Phase 4: Implementation
### Code Changes
1. **Created Redis configuration file** (`redis.conf`):
   - Added proper Redis configuration to eliminate warnings
   - Set log level to warning to reduce noise
   - Configured memory management settings
   
2. **Created Loki configuration file** (`loki-config.yml`):
   - Added single-node Loki configuration
   - Configured ingester properly to eliminate "empty ring" errors
   - Set log level to warn to reduce noise
   
3. **Updated Aspire Program.cs**:
   - Modified Redis container to use configuration file
   - Modified Loki container to use proper configuration
   - Added Kafka log level controls to reduce connection warnings
   
4. **Enhanced ObservabilityMetricsSteps.cs**:
   - Added background progress monitoring with CancellationTokenSource
   - Implemented 5-second progress reporting and 5-minute timeout detection
   - Added progress updates throughout test execution
   - Added proper progress monitoring cleanup

### Challenges Encountered
- Redis container needed specific mount path for configuration
- Loki required comprehensive single-node configuration
- Progress monitoring needed thread-safe implementation
- Kafka connection warnings during cluster formation are normal but noisy

### Solutions Applied
- Used bind mount for Redis configuration file
- Created comprehensive Loki single-node config
- Implemented thread-safe progress monitoring with proper cleanup
- Added Kafka log level controls to reduce startup noise

## Phase 5: Testing & Validation
### Test Results
TBD after investigation phase completes

### Performance Metrics
TBD after investigation phase completes

## Phase 6: Owner Acceptance
### Demonstration
TBD after investigation phase completes

### Owner Feedback
TBD after investigation phase completes

### Final Approval
TBD after investigation phase completes

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
TBD after completion

### What Could Be Improved  
TBD after completion

### Key Insights for Similar Tasks
TBD after completion

### Specific Problems to Avoid in Future
TBD after completion

### Reference for Future WIs
TBD after completion