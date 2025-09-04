# WI6: Observability Messages-Per-Second Metrics for Multi-Layer Flow

**File**: `WIs/WI6_observability-messages-per-second-metrics.md`
**Title**: Add Messages-Per-Second Observability Metrics Across Kafka → Flink → Temporal → Entire Flow  
**Description**: Implement comprehensive observability metrics about messages per second for multiple layers including Kafka production, Flink processing, Temporal workflows, and end-to-end flow monitoring. Ensure IntegrationTests and LocalTesting have these default Aspire setup behaviors. Cover in LearningCourse and create integration tests.
**Priority**: High
**Component**: Observability Infrastructure
**Type**: Enhancement
**Assignee**: AI Agent
**Created**: 2024-12-03
**Status**: Investigation

## Lessons Applied from Previous WIs
### Previous WI References
- WI5: Aspire platform differences documentation - learned about comprehensive documentation updates
- WI4: Container reconciliation fixes - learned about Aspire infrastructure patterns

### Lessons Applied  
- Start with investigation phase to understand current observability state
- Build and test validation before making changes
- Document comprehensive changes with enterprise-level quality
- Use existing infrastructure patterns from LocalTesting

### Problems Prevented
- Making changes without understanding current metrics implementation
- Breaking existing observability infrastructure
- Missing integration test coverage for new metrics

## Phase 1: Investigation
### Requirements
Analyze current observability implementation to identify gaps in messages-per-second metrics across:
1. **Kafka Layer**: Producer/consumer rate metrics
2. **Flink Layer**: Job processing throughput metrics  
3. **Temporal Layer**: Workflow execution rate metrics
4. **End-to-End Flow**: Complete pipeline rate metrics

### Debug Information (MANDATORY - Update this section for every investigation)
- **Current LocalTesting Observability**: Comprehensive stack with Prometheus, Grafana, Loki, OpenTelemetry
- **Current IntegrationTests Observability**: Simplified setup - "Full observability stack (Prometheus, Grafana, Loki, Temporal) available in LocalTesting but keeping minimal for reliable CI/CD execution"
- **Gap Analysis Needed**: 
  - OpenTelemetry configured with AspNetCoreInstrumentation, HttpClientInstrumentation
  - Prometheus scraping from otel-collector:8889, flink-jobmanager:8081, kafka brokers
  - Need to verify specific messages-per-second metrics exist
- **Files to Check**:
  - `LocalTesting/LocalTesting.AppHost/prometheus.yml` - scraping configuration
  - `LocalTesting/LocalTesting.AppHost/otel-config-training-minimal.yaml` - OTel metrics
  - `LocalTesting/LocalTesting.WebApi/Program.cs` - OpenTelemetry setup
  - Service implementations for Kafka, Flink, Temporal metrics
  - Integration test infrastructure for metrics validation

### Findings
**Current Observability State:**
- ✅ Prometheus configured to scrape from all components
- ✅ OpenTelemetry collector configured with metrics pipeline  
- ✅ Grafana with datasources for Prometheus, Loki, Aspire traces
- ❓ Need to verify: Specific messages-per-second custom metrics
- ❓ Need to check: Integration tests that validate these metrics
- ❓ Need to update: LearningCourse coverage of message rate patterns

**Key Configuration Files:**
- Prometheus: Scrapes 8 services including Kafka brokers, Flink JobManager, OTel collector
- OTel: Configured with metrics pipeline, prometheus exporter on port 8889
- WebAPI: Uses OpenTelemetry with AspNetCore + HttpClient instrumentation

### Lessons Learned
- LocalTesting has comprehensive infrastructure ready for enhanced metrics
- IntegrationTests intentionally simplified but can be enhanced
- Foundation exists, need to add specific message rate metrics

## Phase 2: Design  
### Requirements
Design messages-per-second metrics for each layer:

#### **Kafka Layer Metrics**
```
kafka_producer_messages_per_second_total{topic, partition}
kafka_consumer_messages_per_second_total{topic, partition, consumer_group}
kafka_producer_bytes_per_second_total{topic, partition}
kafka_consumer_lag_messages{topic, partition, consumer_group}
```

#### **Flink Layer Metrics**  
```
flink_job_messages_per_second_in{job_id, operator}
flink_job_messages_per_second_out{job_id, operator}
flink_job_throughput_records_per_second{job_id}
flink_job_latency_p99_milliseconds{job_id}
```

#### **Temporal Layer Metrics**
```
temporal_workflow_executions_per_second{workflow_type}
temporal_activity_executions_per_second{activity_type}
temporal_workflow_completion_rate{workflow_type}
temporal_workflow_duration_seconds{workflow_type}
```

#### **End-to-End Flow Metrics**
```
flow_messages_per_second_kafka_to_flink
flow_messages_per_second_flink_to_temporal  
flow_messages_per_second_end_to_end
flow_latency_end_to_end_seconds_p95
```

### Architecture Decisions
1. **Use OpenTelemetry Meter API** for custom metrics in WebAPI services
2. **Leverage Prometheus native metrics** from Kafka and Flink
3. **Add custom instrumentation** in service classes
4. **Create Grafana dashboards** for visualization
5. **Add integration tests** that validate metrics collection

### Why This Approach
- Builds on existing OpenTelemetry infrastructure
- Follows observability best practices
- Provides enterprise-level monitoring capabilities
- Enables performance debugging and optimization

### Alternatives Considered
- Custom metrics collector service (rejected - too complex)
- Log-based metrics parsing (rejected - not real-time enough)
- External APM tools (rejected - adds dependencies)

## Phase 3: TDD/BDD
### Test Specifications
1. **Messages Per Second Metrics Tests**:
   - Validate Kafka producer rate metrics are collected
   - Validate Flink processing rate metrics are collected  
   - Validate Temporal execution rate metrics are collected
   - Validate end-to-end flow rate metrics are collected

2. **Metrics Integration Tests**:
   - Stress test produces messages and metrics are recorded
   - Prometheus can query all message rate metrics
   - Grafana dashboards display rate information
   - Metrics data is accurate and real-time

### Behavior Definitions
```gherkin
Feature: Observability Messages Per Second Metrics

Scenario: Kafka Producer Messages Per Second Metrics
  Given LocalTesting infrastructure is running
  When 1000 messages are produced to Kafka topic
  Then kafka_producer_messages_per_second metric should show rate > 0
  And Prometheus should scrape these metrics successfully

Scenario: Flink Job Processing Rate Metrics  
  Given Flink job is processing Kafka messages
  When messages flow through Flink pipeline
  Then flink_job_messages_per_second metrics should show processing rate
  And Flink dashboard should expose throughput metrics

Scenario: End-to-End Flow Rate Metrics
  Given complete Kafka → Flink → Temporal flow is active
  When messages process through entire pipeline
  Then flow_messages_per_second_end_to_end metric should show total rate
  And metric should correlate with individual layer rates
```

## Phase 4: Implementation
### Code Changes

**1. Added Comprehensive Observability Metrics Service (`LocalTesting/LocalTesting.WebApi/Services/ObservabilityMetricsService.cs`)**
- Created comprehensive OpenTelemetry metrics for all layers
- Kafka layer: producer/consumer rate metrics with topic/partition/consumer_group tags
- Flink layer: job input/output rate metrics with job_id/operator tags
- Temporal layer: workflow/activity execution rate metrics with workflow_type/activity_type tags
- End-to-end flow: complete pipeline rate metrics
- Rate tracking implementation with 1-minute rolling windows for real-time calculation

**2. Enhanced LocalTesting WebAPI Configuration (`LocalTesting/LocalTesting.WebApi/Program.cs`)**
- Added OpenTelemetry meters for each layer (FlinkDotNet.Kafka, FlinkDotNet.Flink, FlinkDotNet.Temporal, FlinkDotNet.Flow)
- Registered ObservabilityMetricsService as singleton
- Enhanced telemetry instrumentation for comprehensive metrics collection

**3. Updated KafkaProducerService with Metrics Integration (`LocalTesting/LocalTesting.WebApi/Services/KafkaProducerService.cs`)**
- Added observability metrics recording for message production
- Records producer rate, bytes produced, and latency metrics
- Records consumer rate metrics with proper tagging
- Calculates and logs real-time messages-per-second rates
- Flow progression tracking from Kafka to Flink

**4. Added Observability REST API Controller (`LocalTesting/LocalTesting.WebApi/Controllers/ObservabilityController.cs`)**
- `/api/observability/metrics/messages-per-second` - comprehensive metrics across all layers
- `/api/observability/metrics/layer/{layer}` - layer-specific metrics (kafka, flink, temporal, flow)
- `/api/observability/metrics/simulate` - simulation endpoints for testing and demonstration
- Real-time rate calculations and aggregation capabilities

**5. Enhanced IntegrationTests with Observability Infrastructure (`IntegrationTests/FlinkDotNet.Aspire.AppHost/Program.cs`)**
- Added Prometheus metrics collection (simplified for CI/CD)
- Added OpenTelemetry collector with metrics pipeline
- Created integration-specific configuration files
- Optimized for CI/CD environments with reduced resource usage

**6. Added Integration Test Validation (`IntegrationTests/FlinkDotNet.Aspire.IntegrationTests/`)**
- `Features/ObservabilityMetrics.feature` - BDD scenarios for metrics validation
- `StepDefinitions/ObservabilityMetricsSteps.cs` - test implementations
- Comprehensive validation of all layer metrics
- Prometheus scraping validation
- Real-time metrics API testing

**7. Enhanced LearningCourse Documentation (`LearningCourse/Day04-Enterprise-Observability/Exercise-Solutions/README.md`)**
- Added comprehensive messages-per-second implementation guide
- Complete code examples for all layers
- Grafana dashboard configuration
- Integration test patterns
- Rate calculation implementation details

**8. Updated README.md with Integration Test Documentation**
- Added comprehensive observability integration test coverage section
- Documented which GitHub workflows test observability metrics
- Listed all metrics being validated
- Provided API endpoints and validation commands
- Added Prometheus and Grafana integration information

### Challenges Encountered
1. **Build Compatibility Issues**: Had to fix TagList compatibility by using KeyValuePair<string, object?> arrays instead
2. **Integration Test Framework**: Fixed Reqnroll (instead of SpecFlow) references and HTTP client extensions
3. **OpenTelemetry Observable Gauge**: Required proper callback function for gauge metrics
4. **Nullable Reference Types**: Fixed nullable warnings for robust code

### Solutions Applied
1. **Used .NET 9.0 compatible OpenTelemetry patterns** with proper meter configuration
2. **Implemented manual HTTP client calls** instead of PostAsJsonAsync extension to ensure compatibility
3. **Created comprehensive rate tracking system** with rolling time windows for accurate rate calculation
4. **Added proper error handling and fallbacks** for metrics collection scenarios

## Phase 5: Testing & Validation
### Test Results
*To be filled during testing*

### Performance Metrics
*To be filled during testing*

## Phase 6: Owner Acceptance
### Demonstration
*To be filled during demonstration*

### Owner Feedback
*To be filled after feedback*

### Final Approval
*To be filled after approval*

## Lessons Learned & Future Reference (MANDATORY)
### What Worked Well
*To be filled after completion*

### What Could Be Improved  
*To be filled after completion*

### Key Insights for Similar Tasks
*To be filled after completion*

### Specific Problems to Avoid in Future
*To be filled after completion*

### Reference for Future WIs
*To be filled after completion*