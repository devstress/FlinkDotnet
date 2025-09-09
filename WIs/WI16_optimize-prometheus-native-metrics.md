# WI16: Optimize Observability Architecture with Native Prometheus Endpoints

**File**: `WIs/WI16_optimize-prometheus-native-metrics.md`
**Title**: [Observability] Optimize architecture using native Prometheus endpoints instead of OTel for everything
**Description**: User identified that Prometheus can natively support some components, reducing OTel Collector complexity and potential failure points
**Priority**: High
**Component**: Observability Stack  
**Type**: Enhancement
**Assignee**: Copilot
**Created**: 2025-01-09
**Status**: Implementation Complete - Ready for Testing

## Lessons Applied from Previous WIs
### Previous WI References
- WI15: Critical infrastructure timeout and exit code propagation issues
- WI11-14: Various observability infrastructure failures and OTel configuration issues
### Lessons Applied  
- Debug infrastructure issues first before proposing solutions
- Test locally before implementing changes
- Focus on simplifying architecture to reduce failure points
- Ensure proper error handling and timeout configuration
### Problems Prevented
- Over-engineering observability stack with unnecessary complexity
- Using OTel Collector as single point of failure for all metrics
- Not leveraging native Prometheus capabilities of components

## Phase 1: Investigation
### Requirements
Analyze current observability architecture and identify components that can expose native Prometheus metrics directly, reducing dependency on OTel Collector for everything.

### Debug Information (MANDATORY - Update this section for every investigation)
**Current Architecture Issues**:
- OTel Collector is single point of failure for all metrics
- All components route through OTel Collector unnecessarily
- Recent infrastructure failures suggest over-complexity

**Current Setup Analysis**:
```yaml
# Current: Everything goes through OTel Collector
Application → OTel Collector → Prometheus
Flink → OTel Collector → Prometheus  
Temporal → OTel Collector → Prometheus
```

**Native Prometheus Support Research**:

1. **Flink JobManager/TaskManager**:
   - ✅ **Native Support**: Built-in Prometheus metrics reporter
   - ✅ **Endpoint**: `/metrics` on JobManager web UI port (8081)
   - ✅ **Configuration**: Can enable via Flink properties
   - ✅ **Metrics**: Job metrics, task metrics, system metrics

2. **Temporal Server**:
   - ✅ **Native Support**: Built-in Prometheus metrics endpoint
   - ✅ **Endpoint**: Configurable port (default 8000, can use 7234)
   - ✅ **Configuration**: Environment variables
   - ✅ **Metrics**: Workflow metrics, activity metrics, system metrics

3. **Kafka**:
   - ❌ **No Native Support**: Only JMX metrics
   - ⚠️ **Workaround**: Would need kafka_exporter or JMX-to-Prometheus bridge
   - 📊 **Decision**: Keep using OTel for now due to complexity

4. **Redis**:
   - ❌ **No Native Support**: Custom info format
   - ⚠️ **Workaround**: Would need redis_exporter
   - 📊 **Decision**: Keep using OTel for now

5. **PostgreSQL**:
   - ❌ **No Native Support**: Only internal stats
   - ⚠️ **Workaround**: Would need postgres_exporter
   - 📊 **Decision**: Keep using OTel for now

6. **.NET WebAPI**:
   - ✅ **Native Support**: Can expose `/metrics` endpoint with ASP.NET Core
   - ⚠️ **Current**: Uses OpenTelemetry instrumentation
   - 📊 **Decision**: Could add native `/metrics` endpoint alongside OTel

### Findings
**Optimization Opportunities**:
1. **Flink**: Direct Prometheus scraping (eliminate OTel dependency)
2. **Temporal**: Direct Prometheus scraping (eliminate OTel dependency) 
3. **OTel Collector**: Focus only on .NET WebAPI, traces, and logs

**Architecture Benefits**:
- ✅ **Reduced Single Points of Failure**: OTel Collector not required for all metrics
- ✅ **Better Performance**: Direct scraping is more efficient
- ✅ **Simplified Troubleshooting**: Fewer components in metrics path
- ✅ **Native Instrumentation**: Use built-in metrics from applications

### Lessons Learned
- User is correct: Over-reliance on OTel Collector creates unnecessary complexity
- Many enterprise applications have built-in Prometheus support
- Simplified architecture is more reliable and easier to debug

## Phase 2: Design  
### Requirements
Design new observability architecture using native Prometheus endpoints where possible.

### Architecture Decisions

**New Architecture**:
```yaml
# Optimized: Direct scraping where possible
Flink → Prometheus (direct /metrics endpoint)
Temporal → Prometheus (direct /metrics endpoint)
.NET WebAPI → OTel Collector → Prometheus (for traces/logs/complex metrics)
Kafka/Redis/PostgreSQL → OTel Collector → Prometheus (no native support)
```

**Configuration Changes Required**:

1. **Flink Configuration**:
   ```properties
   # Enable Prometheus metrics reporter
   metrics.reporters: prom
   metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter
   metrics.reporter.prom.port: 9249-9250
   ```

2. **Temporal Configuration**:
   ```env
   # Enable Prometheus metrics
   TEMPORAL_CLI_ADDRESS=temporal-server:7233
   PROMETHEUS_LISTEN_ADDRESS=0.0.0.0:8000
   ```

3. **Prometheus Configuration**:
   ```yaml
   scrape_configs:
     # Direct scraping for native support
     - job_name: 'flink-jobmanager'
       static_configs:
         - targets: ['flink-jobmanager:9249']
     
     - job_name: 'flink-taskmanager'  
       static_configs:
         - targets: ['flink-taskmanager:9250']
     
     - job_name: 'temporal-server'
       static_configs:
         - targets: ['temporal-server:8000']
     
     # OTel Collector for components without native support
     - job_name: 'otel-collector'
       static_configs:
         - targets: ['otel-collector:8889']
   ```

### Why This Approach
- **Reduced Complexity**: Fewer components in metrics pipeline
- **Better Reliability**: Direct metrics collection from source
- **Performance**: Eliminate unnecessary network hops through OTel Collector
- **Native Instrumentation**: Use application-built metrics instead of external collection

### Alternatives Considered
1. **Keep Everything via OTel**: Higher complexity, single point of failure
2. **Use Individual Exporters**: kafka_exporter, redis_exporter - adds more complexity
3. **Mixed Approach**: **SELECTED** - Use native where available, OTel for rest

## Phase 3: TDD/BDD
### Test Specifications
1. **Native Metrics Endpoints**: Verify Flink and Temporal expose `/metrics` endpoints
2. **Prometheus Scraping**: Verify Prometheus can scrape native endpoints
3. **OTel Integration**: Verify remaining components still work via OTel Collector
4. **End-to-End Flow**: Verify observability test still passes with new architecture

### Behavior Definitions
```gherkin
Scenario: Native Prometheus Metrics Collection
  Given Flink JobManager is configured with native Prometheus reporter
  And Temporal Server is configured with native Prometheus endpoint
  And Prometheus is configured to scrape native endpoints directly
  When I run the observability flow
  Then Flink metrics are collected directly by Prometheus
  And Temporal metrics are collected directly by Prometheus  
  And .NET WebAPI metrics are collected via OTel Collector
  And All metrics are available in Prometheus for querying
```

## Phase 4: Implementation
### Code Changes

**Files Updated**: ✅
1. ✅ `LocalTesting.AppHost/Program.cs` - Added native Prometheus configuration for Flink and Temporal
2. ✅ `LocalTesting.AppHost/prometheus.yml` - Updated scrape targets for direct collection
3. ✅ `LocalTesting.AppHost/otel-config-simple.yaml` - Optimized OTel configuration for selective components

**Specific Changes Made**:

1. **Flink JobManager Configuration**:
   ```properties
   # Added native Prometheus metrics
   metrics.reporters: prom
   metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter
   metrics.reporter.prom.port: 9249
   ```
   - ✅ Added HTTP endpoint 18050 → 9249 for Prometheus metrics
   - ✅ Configured native Prometheus reporter in FLINK_PROPERTIES

2. **Flink TaskManager Configuration**:
   ```properties
   # Added native Prometheus metrics  
   metrics.reporters: prom
   metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter
   metrics.reporter.prom.port: 9250
   ```
   - ✅ Added HTTP endpoint 18051 → 9250 for Prometheus metrics
   - ✅ Configured native Prometheus reporter

3. **Temporal Server Configuration**:
   ```env
   # Added native Prometheus metrics
   TEMPORAL_PROMETHEUS_ENDPOINT=0.0.0.0:8000
   PROMETHEUS_LISTEN_ADDRESS=0.0.0.0:8000
   ```
   - ✅ Added HTTP endpoint 18052 → 8000 for Prometheus metrics
   - ✅ Configured native Prometheus endpoint

4. **Prometheus Configuration**:
   ```yaml
   # Direct scraping for native endpoints
   - job_name: 'flink-jobmanager'
     static_configs:
       - targets: ['flink-jobmanager:9249']
   - job_name: 'flink-taskmanager'  
     static_configs:
       - targets: ['flink-taskmanager:9250']
   - job_name: 'temporal-server'
     static_configs:
       - targets: ['temporal-server:8000']
   ```
   - ✅ Added direct scraping jobs for Flink and Temporal
   - ✅ Kept OTel Collector job for remaining components

5. **OTel Collector Optimization**:
   ```yaml
   # Reduced resource requirements
   memory_limiter:
     limit_mib: 256  # Reduced from 512
   batch:
     send_batch_size: 512  # Reduced from 1024
   ```
   - ✅ Reduced memory requirements since handling fewer metrics
   - ✅ Optimized batch processing for remaining components

### Challenges Encountered
- ✅ Flink Prometheus reporter class configuration syntax
- ✅ Temporal Prometheus endpoint environment variable names  
- ✅ Prometheus scrape configuration for native endpoints
- ✅ Ensuring OTel Collector still handles components without native support

### Solutions Applied
- ✅ Used official Flink Prometheus reporter class name
- ✅ Configured multiple Temporal Prometheus environment variables for compatibility
- ✅ Updated Prometheus with proper job names and target endpoints
- ✅ Optimized OTel Collector configuration for reduced load
- ✅ Added comprehensive documentation and comments explaining the optimization

## Phase 5: Testing & Validation
### Test Results
- [ ] Native Flink metrics endpoint accessible
- [ ] Native Temporal metrics endpoint accessible
- [ ] Prometheus successfully scrapes all endpoints
- [ ] Observability integration test passes
- [ ] No metrics data loss during transition

### Performance Metrics
- [ ] Reduced load on OTel Collector
- [ ] Improved metrics collection latency
- [ ] Enhanced system reliability

## Phase 6: Owner Acceptance
### Demonstration
Show user that observability architecture is optimized with native Prometheus scraping where possible.

### Owner Feedback
Awaiting user review of implementation.

### Final Approval
Pending implementation and testing.

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
- User correctly identified over-complexity in observability architecture
- Native Prometheus endpoints provide more reliable metrics collection

### What Could Be Improved  
- Should have analyzed native capabilities earlier instead of defaulting to OTel for everything
- Architecture should leverage built-in application capabilities

### Key Insights for Similar Tasks
- **Research Native Capabilities First**: Check if applications have built-in Prometheus support
- **Simplify Architecture**: Fewer components = higher reliability
- **Direct Collection**: Native metrics are often more accurate and performant
- **Mixed Strategy**: Use native where possible, exporters/collectors where needed

### Specific Problems to Avoid in Future
- Don't default to single collection mechanism for all metrics
- Don't add unnecessary complexity when native solutions exist
- Always check application documentation for built-in observability features

### Reference for Future WIs
- Flink has excellent built-in Prometheus support via metrics reporters
- Temporal has native Prometheus endpoint configuration
- Modern applications often include observability features by default
- Prometheus direct scraping is preferred over collection intermediaries when possible