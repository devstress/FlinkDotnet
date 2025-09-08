# WI11: Fix Kafka Producer Performance Configuration

**File**: `WIs/WI11_fix-kafka-producer-performance-configuration.md`
**Title**: Fix Kafka producer high-performance mode configuration issue
**Description**: The Kafka producer is running in slow mode (18 msg/sec) instead of high-performance mode due to configuration mismatch. Config is set as "HighPerformanceMode" but code looks for "Kafka:HighPerformanceMode"
**Priority**: High
**Component**: LocalTesting/KafkaProducerService
**Type**: Bug Fix
**Assignee**: AI Agent
**Created**: 2025-01-11
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI10_optimize-kafka-producer-performance.md - Previous Kafka optimization work

### Lessons Applied  
- Always verify configuration keys match between code and config files
- Test locally before submitting changes
- Use proper debugging to identify root causes

### Problems Prevented
- Avoid configuration mismatches by verifying keys
- Test performance locally to validate fixes

## Phase 1: Investigation
### Requirements
- Identify root cause of slow Kafka producer performance (18 msg/sec instead of thousands)
- Verify configuration mismatch between code and config files

### Debug Information (MANDATORY - Update this section for every investigation)
- **Error Messages**: OpenTelemetry collector container failing to start: "container start failed (current state is 'exited')"
- **Log Locations**: Docker logs and Aspire DCP logs showing otel-collector startup failures
- **System State**: 
  - Kafka configuration is correct: appsettings.json has "Kafka:HighPerformanceMode": true
  - Code correctly looks for "Kafka:HighPerformanceMode" configuration key
  - Real issue: otel-collector container failing prevents WebAPI health check from passing
- **Reproduction Steps**: 
  1. Run LocalTesting observability test
  2. otel-collector container fails to start due to invalid configuration
  3. WebAPI tries to connect to otel-collector but fails
  4. Health check times out, causing entire test to fail
- **Evidence**: 
  - Docker config validation showed multiple syntax errors in otel-config-high-performance.yaml
  - Invalid keys: max_recv_msg_size, loki exporter, telemetry.address

### Findings
- **ROOT CAUSE IDENTIFIED**: OpenTelemetry collector configuration file has syntax errors preventing container startup
- Kafka configuration is actually correct - the performance issue is secondary to infrastructure failure
- Configuration errors found:
  - `max_recv_msg_size` should be `max_message_size`
  - `loki` exporter not available in otel-collector version
  - Invalid `address` key in telemetry.metrics section
  - Duplicate `file/buffer` exporters
  - Missing required fields like `check_interval` in memory_limiter

### Lessons Learned
- Always validate container configurations before deployment
- Infrastructure connectivity issues can mask the real performance problems
- Test minimal configurations first, then add complexity

## Phase 2: Design  
### Requirements
- Fix otel-collector configuration syntax errors
- Create minimal working configuration for reliable container startup
- Ensure test failure propagation works correctly

### Architecture Decisions
- Replace complex otel-config-high-performance.yaml with simple working configuration
- Use absolute path for bind mount to avoid path resolution issues
- Maintain essential functionality: OTLP receivers + Prometheus export

### Why This Approach
- Minimal configuration reduces error potential and startup time
- Absolute paths eliminate bind mount issues
- Focus on making infrastructure work first, then optimize performance

### Alternatives Considered
- Could fix all syntax errors in complex config, but simple approach is more reliable
- Could disable otel-collector entirely, but that breaks observability pipeline

## Phase 3: TDD/BDD
### Test Specifications
- otel-collector container should start successfully without errors
- LocalTesting observability test should complete without connection failures
- GitHub workflow should properly fail when tests fail (already working)

### Behavior Definitions
- When running observability test, infrastructure should start within reasonable time
- Should see successful container startup logs, not "container start failed" errors
- Health checks should pass and test should measure actual Kafka performance

## Phase 4: Implementation
### Code Changes
- Fixed bind mount path in AppHost Program.cs: use `Path.GetFullPath()` for absolute path
- Created otel-config-simple.yaml with minimal working configuration
- Updated AppHost to use simple config instead of complex one

### Challenges Encountered
- Multiple syntax errors in OpenTelemetry collector configuration
- Complex config had version compatibility issues with loki exporter
- Required understanding of OTel collector configuration schema

### Solutions Applied
- Created minimal configuration with only essential components:
  - OTLP receivers (gRPC and HTTP)
  - Memory limiter with proper check_interval
  - Batch processor with reasonable settings
  - Prometheus exporter for metrics
  - Debug exporter for traces/logs
- Fixed all syntax errors and removed unsupported exporters

## Phase 5: Testing & Validation
### Test Results
- ✅ otel-collector configuration validates successfully with Docker test
- ✅ Infrastructure starts much faster (2 minutes vs 6+ minute timeout)
- ✅ No more "container start failed" errors in logs
- 🔄 Full test still in progress but infrastructure connectivity is now working

### Performance Metrics
- Infrastructure startup improved significantly
- otel-collector starts without errors
- Ready to measure actual Kafka performance with working infrastructure

## Phase 6: Owner Acceptance
### Demonstration
- Show improved performance metrics in observability test output

### Owner Feedback
- Awaiting owner review

### Final Approval
- Pending completion

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- To be documented after completion

### What Could Be Improved  
- To be documented after completion

### Key Insights for Similar Tasks
- Always verify configuration keys match between code and config files
- Test configuration changes locally before submitting

### Specific Problems to Avoid in Future
- Configuration key mismatches between implementation and settings
- Deploying performance changes without local validation

### Reference for Future WIs
- Check configuration key consistency in all performance-related changes
- Always test performance optimizations locally to verify they work as expected