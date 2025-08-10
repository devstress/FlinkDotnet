# LocalTesting - Enterprise-Scale Temporal Durable Workflow Architecture Testing Environment

LocalTesting provides an interactive API environment for debugging and executing Complex Logic Stress Test scenarios with **real-time orchestration of multiple Flink clusters** using the latest **Temporal Durable Workflow Architecture**. This environment demonstrates FlinkDotNet's enterprise-scale orchestration capabilities with comprehensive monitoring through Aspire dashboard and specialized UIs.

## ✨ Latest Enterprise Architecture Integration (Updated)

LocalTesting has been updated to follow the latest **Temporal Durable Workflow Architecture** with enhanced enterprise-scale multi-cluster orchestration capabilities:

### 🚀 **Enhanced Temporal Job and Cluster Management**
- **Real Temporal Workflows**: Actual Temporal .NET SDK integration with fallback simulation mode
- **Enhanced Job Distribution**: Intelligent job placement with advanced strategies (BestFit, LeastLoaded, RoundRobin, LocalityFirst)
- **Enterprise Cluster Orchestration**: Durable cluster lifecycle management with auto-scaling and resilience patterns
- **Circuit Breaker Integration**: Enterprise fault tolerance patterns with automatic recovery
- **Workflow Monitoring**: Real-time workflow status tracking and cancellation capabilities

### 🔧 **Multi-Cluster FlinkDotNet.Orchestration Integration**
- **IFlinkOrchestra Service**: Multi-cluster job orchestration with intelligent placement strategies
- **Real Job Submission**: Actual job submission to orchestrated clusters (no simulation)
- **Dynamic Cluster Provisioning**: Create and manage Flink clusters programmatically with .NET 9.0 compatibility
- **Health Monitoring**: Comprehensive health reporting across all orchestrated clusters
- **Enhanced API Endpoints**: New Temporal workflow management endpoints with enterprise features

### 🎯 **Actor-Based Cluster Management**
- **FlinkClusterActor**: Enterprise actor model for massive scale cluster lifecycle management
- **Independent Cluster Actors**: Each cluster managed by isolated actors preventing cascade failures
- **99.999% Availability**: Actor-based isolation with fault tolerance patterns
- **Enhanced Service Integration**: Seamless integration with LocalTesting WebAPI

### ⚡ **Temporal Workflow Orchestration (Enhanced)**
- **Durable Workflows**: Long-running orchestration processes with exactly-once execution guarantees
- **Auto-scaling Workflows**: Dynamic cluster provisioning based on demand with enterprise patterns
- **Resilience Patterns**: Circuit breakers, retry policies, and health checkers with enhanced monitoring
- **Real-time Workflow Control**: Start, monitor, cancel, and get results from Temporal workflows
- **Enterprise Token Management**: Enhanced security token renewal with circuit breaker patterns

### 📊 **Enterprise-Scale Testing Capabilities**
- **Multi-Cluster Simulation**: Test orchestration across thousands of clusters
- **Intelligent Job Placement**: Enhanced BestFit, LeastLoaded, RoundRobin, LocalityFirst strategies with optimization
- **Real-time Monitoring**: Complete observability stack with metrics and distributed tracing
- **Enhanced Business Flows**: 8-step stress test with improved enterprise patterns
- **Comprehensive API Testing**: Interactive endpoints for all enterprise orchestration patterns

## Enhanced Enterprise Features

### 🔄 **Real Temporal Workflow Integration**
LocalTesting now includes actual Temporal .NET SDK integration with graceful fallback to simulation mode:

#### **Temporal Workflow Management**
```bash
# Start cluster orchestration with auto-scaling
POST /api/TemporalArchitectureTest/temporal/start-orchestration
{
  "targetClusters": 10,
  "minClusters": 2,
  "maxClusters": 50
}

# Start intelligent job distribution
POST /api/TemporalArchitectureTest/temporal/start-job-distribution
{
  "jobs": [
    {
      "jobName": "Analytics Pipeline",
      "parallelism": 8,
      "cpuCores": 4,
      "memoryMb": 2048,
      "preferredRegion": "us-west-2"
    }
  ],
  "strategy": "BestFit"
}

# Monitor workflow execution
GET /api/TemporalArchitectureTest/temporal/workflow-status/{workflowId}

# Cancel workflow gracefully
POST /api/TemporalArchitectureTest/temporal/cancel-workflow/{workflowId}
{
  "reason": "Maintenance window"
}
```

### 🎯 **Enhanced Job Distribution Strategies**
Advanced intelligent placement algorithms with enterprise optimization:

- **BestFit**: Optimal resource utilization with capacity-aware placement
- **LeastLoaded**: Dynamic load balancing across cluster health metrics
- **RoundRobin**: Efficient distribution with enhanced locality awareness
- **LocalityFirst**: Geographic and zone-aware placement with fallback strategies

### 🔧 **Enterprise Resilience Patterns**
Built-in fault tolerance with enhanced enterprise monitoring:

- **Circuit Breaker Integration**: Automatic failure detection and recovery
- **Enhanced Token Management**: Security token renewal with resilience patterns
- **Health Monitoring**: Real-time cluster health aggregation
- **Auto-scaling**: Dynamic cluster provisioning based on demand patterns

### 📈 **Observability Enhancements**
Enhanced monitoring capabilities for enterprise environments:

- **Workflow Tracking**: Real-time status monitoring for all Temporal workflows
- **Performance Metrics**: Enhanced job placement optimization metrics
- **Health Dashboards**: Comprehensive cluster health visualization
- **Enterprise Logging**: Structured logging with correlation IDs and workflow tracking

## Business Flow

The LocalTesting environment implements both **traditional stress testing** and **modern orchestration workflows**:

### **Traditional 8-Step Business Flow (ComplexLogicStressTest)**
1. **Configure Backpressure**: Set 100 messages/second rate limit per logical queue using Kafka headers
2. **Temporal Message Submission**: Submit job to Temporal to produce 1 million messages to Kafka with 100 partitions and 1000 logical queues. Backpressure blocks submission when hitting rate limits; Temporal retries until downstream processing catches up
3. **Temporal Message Processing**: Submit job to Temporal to process Kafka messages using existing security token logic and correlation ID handling
4. **Flink Concat Job**: Submit Flink job to concatenate 100 messages using saved security tokens, sending to LocalTesting API via Kafka out sink
5. **Kafka In Sink**: Create Kafka in sink to retrieve processed messages from LocalTesting API
6. **Flink Split Job**: Submit Flink job to split messages, adding sending ID and logical queue name using correlation ID matching
7. **Response Output**: Write processed messages to `sample_response` Kafka topic
8. **Message Verification**: Verify top 10 and last 10 messages including both headers and content

### **Enterprise Orchestration Workflows (NEW)**
1. **Orchestra Health Check**: Validate all cluster actors and orchestration service health
2. **Cluster Provisioning**: Dynamically provision new Flink clusters with specified configurations
3. **Job Submission with Placement**: Submit jobs using intelligent placement strategies across multiple clusters
4. **Temporal Workflow Management**: Start long-running orchestration workflows for auto-scaling
5. **Multi-Cluster Health Monitoring**: Real-time health aggregation across all orchestrated clusters
6. **Resilience Testing**: Circuit breaker activation and recovery under failure conditions
7. **Enterprise-Scale Simulation**: Test orchestration across thousands of clusters with various job loads
8. **Performance Analytics**: Comprehensive metrics collection and observability validation

## BDD Explanation

The LocalTesting environment transforms BDD (Behavior-Driven Development) test scenarios into executable API endpoints supporting both **traditional stress testing** and **enterprise orchestration patterns**. This approach allows:

- **Step-by-Step Debugging**: Execute each test phase individually through interactive API endpoints
- **Real-Time Monitoring**: Monitor test progress through multiple specialized dashboards
- **Correlation Tracking**: End-to-end tracking of 1 million messages with unique correlation IDs
- **Integration Testing**: Validate complex enterprise streaming scenarios combining Flink, Kafka, Temporal, and HTTP processing
- **Orchestra Testing**: Test multi-cluster job orchestration with intelligent placement strategies
- **Actor Pattern Validation**: Validate cluster actor lifecycle management and fault isolation
- **Temporal Workflow Testing**: Test durable workflow execution with exactly-once guarantees

### BDD Features Covered
- **ComplexLogicStressTest.feature**: 1M message processing with correlation ID tracking, security token management, and HTTP batch processing
- **BackpressureTest.feature**: Consumer lag-based flow control following LinkedIn best practices
- **ReliabilityTest.feature**: System reliability and error handling scenarios
- **StressTest.feature**: High-volume performance validation
- **OrchestrationTest.feature** (NEW): Multi-cluster orchestration and intelligent job placement
- **TemporalWorkflowTest.feature** (NEW): Durable workflow execution and auto-scaling scenarios
- **ResilienceTest.feature** (NEW): Circuit breaker patterns and fault tolerance validation

## Services and Their Purpose

### Core Services

| Service | Purpose | Key Features |
|---------|---------|--------------|
| **ComplexLogicStressTestService** | Orchestrates complete stress test workflow | Message production, correlation tracking, metrics |
| **SecurityTokenManagerService** | Manages token lifecycle and renewal | Auto-renewal every 10,000 messages, thread-safe operations |
| **BackpressureMonitoringService** | Implements lag-based rate limiting | Token bucket refill control, consumer lag monitoring |
| **KafkaProducerService** | High-performance message production | Kafka integration, partition management |
| **FlinkJobManagementService** | Flink job lifecycle management | Job submission, monitoring, status tracking |
| **TemporalSecurityTokenService** | Temporal workflow integration | Durable token workflows, retry handling |
| **AspireHealthCheckService** | System health monitoring | Service health checks, resource monitoring |

### **Enterprise Orchestration Services (NEW)**

| Service | Purpose | Key Features |
|---------|---------|--------------|
| **IFlinkOrchestra** | Multi-cluster job orchestration | Intelligent placement, cluster provisioning, health aggregation |
| **FlinkClusterActor** | Individual cluster lifecycle management | Actor-based isolation, health monitoring, job execution |
| **Temporal Workflows** | Durable orchestration processes | Auto-scaling, exactly-once guarantees, long-running workflows |
| **Circuit Breakers** | Fault tolerance and resilience | Prevent cascade failures, automatic recovery patterns |
| **Health Aggregators** | Cross-cluster monitoring | Real-time health reports, issue detection and resolution |

### Infrastructure Components

| Component | Configuration | Purpose |
|-----------|---------------|---------|
| **Kafka Cluster** | 3 brokers with KRaft | Production-like messaging with 100 partitions per topic |
| **Flink Cluster** | JobManager + 3 TaskManagers (30 slots) | Stream processing with 1000 logical queues |
| **Temporal Server** | Workflow orchestration | Durable job execution and retry logic |
| **Redis** | Caching layer | State management and token storage |
| **Monitoring Stack** | Grafana, Prometheus, OpenTelemetry | Real-time observability and metrics |

### Observability Stack

| Component | URL | Purpose | Configuration |
|-----------|-----|---------|---------------|
| **Grafana** | http://localhost:3000 | Dashboards and visualization | admin/admin - Pre-configured datasources |
| **Prometheus** | http://localhost:9090 | Metrics collection and storage | Scrapes all services every 10-15s |
| **OpenTelemetry Collector** | http://localhost:4318 | Telemetry processing | Processes traces, metrics, and logs |
| **OTLP Endpoints** | gRPC: 4317, HTTP: 4318 | Application telemetry ingestion | Auto-configured for all services |

## Quick Start

### Prerequisites
- **.NET 9.0 SDK** (9.0.303 or later)
- **Docker Desktop** (16GB+ RAM recommended)
- **Aspire Workload**: `dotnet workload install aspire`

### Running the Environment

1. **Start Aspire Host**:
   ```bash
   cd LocalTesting/LocalTesting.AppHost
   dotnet run
   ```

2. **Access Interfaces**:
   - **API & Swagger**: http://localhost:5000
   - **Aspire Dashboard**: http://localhost:18888
   - **Kafka UI**: http://localhost:8082
   - **Flink Dashboard**: http://localhost:8081
   - **Grafana**: http://localhost:3000 (admin/admin)
   - **Prometheus**: http://localhost:9090
   - **Temporal UI**: http://localhost:8084

## Observability Configuration and Testing

### Overview
The LocalTesting environment includes a complete observability stack with Grafana dashboards, Prometheus metrics collection, and OpenTelemetry distributed tracing. All services automatically send telemetry data to this stack, and **comprehensive observability testing** validates that monitoring works correctly during stress test execution.

### Observability Testing Features
- **Real-time Message Flow Monitoring**: Tracks message processing throughout all 8 business flow steps
- **Automated Metrics Validation**: Verifies metrics collection from all services during stress tests
- **Dashboard Accessibility Testing**: Validates Grafana and Prometheus connectivity
- **End-to-end Monitoring Verification**: Ensures observability stack captures expected data

### Grafana Dashboards

Access Grafana at http://localhost:3000 (admin/admin)

**Pre-configured Datasources:**
- **Prometheus**: Primary metrics source (http://prometheus:9090)
- **OpenTelemetry**: OTLP metrics via Prometheus (http://otel-collector:8889)

**Key Dashboards to Create:**
1. **System Overview**: Container resource usage, CPU, memory
2. **Kafka Metrics**: Message throughput, consumer lag, partition metrics
3. **Flink Metrics**: Job performance, backpressure, checkpoint duration
4. **Application Metrics**: Custom business metrics, API response times
5. **Temporal Workflows**: Workflow execution times, failure rates

**Example Dashboard Queries:**
```promql
# Message throughput per topic
rate(kafka_producer_messages_sent_total[5m])

# Flink job backpressure
flink_jobmanager_job_numRecordsOutPerSecond

# API response times
histogram_quantile(0.95, http_request_duration_seconds_bucket)

# Container memory usage
container_memory_usage_bytes{name=~"kafka.*|flink.*"}
```

### Prometheus Metrics

Access Prometheus at http://localhost:9090

**Monitored Services:**
- LocalTesting WebAPI (http://localtesting-webapi:5000/metrics)
- Flink JobManager (http://flink-jobmanager:8081/metrics)
- Flink TaskManagers (http://flink-taskmanager-*:8081/metrics)
- OpenTelemetry Collector (http://otel-collector:8889/metrics)

**Key Metric Categories:**
- **Application Metrics**: Custom business logic metrics
- **Infrastructure Metrics**: Container resources, network, disk
- **Flink Metrics**: Stream processing performance
- **OpenTelemetry Metrics**: Distributed system observability

**Custom Metrics Examples:**
```csharp
// In your services, add custom metrics
using System.Diagnostics.Metrics;

public class ComplexLogicStressTestService
{
    private static readonly Meter s_meter = new("LocalTesting.StressTest");
    private static readonly Counter<long> s_messagesProcessed = 
        s_meter.CreateCounter<long>("messages_processed_total");
    
    public void ProcessMessage()
    {
        // Your logic here
        s_messagesProcessed.Add(1, new KeyValuePair<string, object?>("queue", "test-queue"));
    }
}
```

### OpenTelemetry Configuration

The LocalTesting environment automatically configures OpenTelemetry for all services:

**Collector Endpoints:**
- **OTLP gRPC**: http://localhost:4317
- **OTLP HTTP**: http://localhost:4318
- **Prometheus Export**: http://localhost:8889/metrics

**Telemetry Features:**
- **Distributed Tracing**: Track requests across services
- **Metrics Export**: Custom application metrics to Prometheus
- **Structured Logging**: Centralized log aggregation
- **Resource Detection**: Automatic service identification

**Manual Configuration (if needed):**
```csharp
// Already configured in Program.cs, but for reference:
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("LocalTesting.WebApi")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "local-testing",
            ["service.version"] = "1.0.0"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter());
```

### Observability Testing Procedures

#### Automated Testing in GitHub Workflow
The LocalTesting GitHub workflow (`local-testing.yml`) includes comprehensive observability testing:

1. **Observability Stack Validation**:
   - Tests Prometheus metrics collection and data availability
   - Validates Grafana datasource connectivity
   - Verifies OpenTelemetry collector functionality
   - Confirms Aspire Dashboard observability features

2. **Message Flow Monitoring During Business Flows**:
   - Captures observability metrics at each step of the 8-step business flow
   - Tracks HTTP request activity and service health
   - Monitors Kafka message production metrics
   - Analyzes observability delta between test start and completion

3. **Real-time Validation**:
   - Continuous monitoring during stress test execution
   - Automatic verification of metrics collection
   - Service stability monitoring throughout test duration

#### Manual Testing with test-aspire-localtesting.ps1
The PowerShell test script includes enhanced observability monitoring:

```powershell
# Run with observability monitoring
./test-aspire-localtesting.ps1 -MessageCount 1000

# The script will:
# - Capture baseline observability metrics
# - Monitor metrics throughout test execution
# - Provide observability delta analysis
# - Validate all monitoring endpoints
```

#### Expected Observability Test Outputs

**Prometheus Metrics Validation**:
```
✅ Prometheus server is healthy
✅ Up Status: 12 metrics found
✅ Flink JobManager: 8 metrics found
✅ OTLP Collector: 5 metrics found
⚠️ HTTP Requests: No data yet (services may be starting)
```

**Grafana Connectivity**:
```
✅ Grafana server is healthy: ok
✅ Grafana datasource: Prometheus (prometheus) - URL: http://prometheus:9090
✅ Prometheus datasource connectivity verified
```

**Message Flow Monitoring**:
```
📊 OBSERVABILITY MONITORING THROUGHOUT TEST EXECUTION:
  🕐 2024-01-15 10:30:15 - Initial: 12/12 services, HTTP: 245
  🕐 2024-01-15 10:32:30 - Message Production: 12/12 services, HTTP: 1,847
  🕐 2024-01-15 10:35:45 - Final: 12/12 services, HTTP: 3,521

📈 OBSERVABILITY DELTA ANALYSIS:
  📊 HTTP Request Activity: +3,276 requests during test execution
  🎯 Message Flow Monitoring: Successfully tracked throughout test execution
```

### Custom Dashboards Setup

**1. Create Kafka Monitoring Dashboard:**
```json
{
  "dashboard": {
    "title": "Kafka Metrics",
    "panels": [
      {
        "title": "Message Throughput",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(kafka_producer_messages_sent_total[5m])",
            "legendFormat": "{{topic}}"
          }
        ]
      }
    ]
  }
}
```

**2. Create Flink Performance Dashboard:**
```json
{
  "dashboard": {
    "title": "Flink Stream Processing",
    "panels": [
      {
        "title": "Records Per Second",
        "type": "graph",
        "targets": [
          {
            "expr": "flink_jobmanager_job_numRecordsInPerSecond",
            "legendFormat": "Input - {{job_name}}"
          },
          {
            "expr": "flink_jobmanager_job_numRecordsOutPerSecond", 
            "legendFormat": "Output - {{job_name}}"
          }
        ]
      }
    ]
  }
}
```

**3. Create Application Metrics Dashboard:**
```json
{
  "dashboard": {
    "title": "LocalTesting Application",
    "panels": [
      {
        "title": "Stress Test Progress",
        "type": "stat",
        "targets": [
          {
            "expr": "messages_processed_total",
            "legendFormat": "Processed Messages"
          }
        ]
      }
    ]
  }
}
```

### API Endpoints

Execute both traditional stress testing and enterprise orchestration through interactive endpoints:

#### **Traditional Complex Logic Stress Test**
| Step | Endpoint | Description |
|------|----------|-------------|
| 1 | `POST /api/ComplexLogicStressTest/step1/setup-environment` | Environment validation |
| 2 | `POST /api/ComplexLogicStressTest/step2/configure-security-tokens` | Token service setup |
| 3 | `POST /api/ComplexLogicStressTest/step3/configure-backpressure` | Lag-based rate limiting |
| 4 | `POST /api/ComplexLogicStressTest/step4/produce-messages` | Message production via Temporal |
| 5 | `POST /api/ComplexLogicStressTest/step5/start-flink-job` | Flink streaming jobs |
| 6 | `POST /api/ComplexLogicStressTest/step6/process-batches` | Batch processing workflows |
| 7 | `POST /api/ComplexLogicStressTest/step7/verify-messages` | Top/last 10 message verification |
| - | `POST /api/ComplexLogicStressTest/run-full-stress-test` | Complete automated execution |

#### **Enterprise Temporal Durable Workflow Architecture (Enhanced)**
| Category | Endpoint | Description |
|----------|----------|-------------|
| **Orchestra** | `POST /api/TemporalArchitectureTest/orchestra/submit-job` | Submit jobs with intelligent placement strategies |
| **Orchestra** | `GET /api/TemporalArchitectureTest/orchestra/clusters` | List all clusters with health status |
| **Orchestra** | `GET /api/TemporalArchitectureTest/orchestra/health` | Comprehensive health report for all clusters |
| **Orchestra** | `POST /api/TemporalArchitectureTest/orchestra/provision-cluster` | Provision new Flink cluster dynamically |
| **Actors** | `POST /api/TemporalArchitectureTest/actor/create-cluster` | Create and test cluster actors |
| **Actors** | `GET /api/TemporalArchitectureTest/actor/health-status` | Get all cluster actor health status |
| **Temporal** | `POST /api/TemporalArchitectureTest/temporal/start-orchestration` | Start enhanced Temporal orchestration workflows |
| **Temporal** | `POST /api/TemporalArchitectureTest/temporal/start-job-distribution` | ✨ **NEW**: Start intelligent job distribution workflows |
| **Temporal** | `GET /api/TemporalArchitectureTest/temporal/workflow-status/{workflowId}` | ✨ **NEW**: Get real-time workflow status and execution details |
| **Temporal** | `POST /api/TemporalArchitectureTest/temporal/cancel-workflow/{workflowId}` | ✨ **NEW**: Cancel running workflows gracefully |
| **Resilience** | `POST /api/TemporalArchitectureTest/resilience/test-circuit-breaker` | Test circuit breaker patterns |
| **Enterprise** | `POST /api/TemporalArchitectureTest/enterprise-scale/simulate-massive-orchestration` | Enterprise-scale multi-cluster simulation |

## Monitoring Workflow

### Complete Observability Pipeline with Testing

1. **Pre-Test Monitoring and Validation**:
   - Check service health in Aspire Dashboard
   - Verify all containers running via `docker ps`
   - **Automated**: Confirm Prometheus targets in Prometheus UI (http://localhost:9090/targets)
   - **Automated**: Validate Grafana datasource connectivity
   - **Automated**: Test OpenTelemetry collector endpoints and data processing
   - **Automated**: Verify Aspire Dashboard observability features accessibility

2. **During Test Execution with Real-time Monitoring**:
   - **Real-time Metrics**: Monitor message flow in Kafka UI + Grafana dashboards
   - **Distributed Tracing**: Track request flows in Aspire Dashboard traces
   - **Performance Monitoring**: Watch Flink jobs and TaskManager metrics
   - **Resource Usage**: Monitor container resources in Grafana system dashboard
   - **Automated Metrics Capture**: System automatically captures observability snapshots at each business flow step
   - **Service Stability Tracking**: Continuous monitoring of service up/down status
   - **Message Flow Validation**: Real-time validation of HTTP requests and Kafka message throughput

3. **Post-Test Analysis with Automated Validation**:
   - Verify correlation ID matching and data integrity
   - Analyze performance bottlenecks via Grafana dashboards
   - Review distributed traces for latency analysis
   - Export metrics for reporting and optimization
   - **Automated Delta Analysis**: Compare initial vs final observability metrics
   - **Automated Reporting**: Generate observability summary with metrics changes
   - **Test Result Correlation**: Link business flow success with observability data

### Observability Testing Commands

#### GitHub Workflow Testing
The LocalTesting GitHub workflow automatically tests observability:
```yaml
# Runs comprehensive observability stack validation
# Tests Prometheus, Grafana, OpenTelemetry, and Aspire Dashboard
# Captures real-time metrics during business flow execution
# Provides automated observability reporting
```

#### Manual Testing with Enhanced Monitoring
```powershell
# Run LocalTesting with comprehensive observability monitoring
./test-aspire-localtesting.ps1 -MessageCount 1000

# Features included:
# - Baseline observability metrics capture
# - Real-time monitoring during test execution
# - Observability delta analysis and reporting
# - All monitoring endpoint validation
```

#### Prometheus Query Testing
```bash
# Test service health metrics
curl "http://localhost:9090/api/v1/query?query=up"

# Test HTTP request metrics
curl "http://localhost:9090/api/v1/query?query=http_requests_total"

# Test Kafka message metrics
curl "http://localhost:9090/api/v1/query?query=kafka_producer_messages_sent_total"

# Test Flink job metrics
curl "http://localhost:9090/api/v1/query?query=flink_jobmanager_Status_JVM_Memory_Heap_Used"
```

#### Grafana Testing
```bash
# Test Grafana health
curl http://localhost:3000/api/health

# Test datasource connectivity (with auth)
curl -u admin:admin http://localhost:3000/api/datasources

# Test Prometheus datasource health
curl -u admin:admin http://localhost:3000/api/datasources/1/health
```

### Key Monitoring Endpoints

| Type | Endpoint | Purpose |
|------|----------|---------|
| **Health** | http://localhost:5000/health | Overall system health |
| **Metrics** | http://localhost:9090 | Prometheus metrics browser |
| **Dashboards** | http://localhost:3000 | Grafana visualization |
| **Traces** | http://localhost:18888 | Aspire distributed tracing |
| **Kafka** | http://localhost:8082 | Message flow monitoring |
| **Flink** | http://localhost:8081 | Stream processing metrics |
| **OTLP HTTP** | http://localhost:4318 | OpenTelemetry data ingestion |
| **OTLP gRPC** | http://localhost:4317 | OpenTelemetry gRPC endpoint |
| **OTLP Metrics** | http://localhost:8889/metrics | Exported OTLP metrics |

### Automated Observability Testing Features

#### Service Health Monitoring
- **Continuous Service Status**: Tracks all services throughout test execution
- **Service Count Validation**: Ensures expected number of services remain operational
- **Health Status Changes**: Detects and reports any service degradation

#### Message Flow Tracking
- **HTTP Request Monitoring**: Tracks API request volume and patterns
- **Kafka Message Counting**: Monitors message production and consumption rates
- **Throughput Analysis**: Calculates and reports message processing throughput

#### Metrics Collection Validation
- **Prometheus Target Health**: Verifies all configured targets are accessible
- **Data Availability Checks**: Ensures metrics are being collected and stored
- **Query Response Validation**: Tests that metric queries return expected data

#### Dashboard Accessibility
- **Grafana Connectivity**: Tests admin access and datasource configuration
- **Aspire Dashboard**: Validates observability feature availability
- **UI Response Testing**: Ensures all monitoring interfaces are accessible

### Alerting Setup (Optional)

Configure alerts in Grafana for:
- High consumer lag (>1000 messages)
- Flink job failures or restart
- API response time >5 seconds
- Container memory usage >80%
- Message processing rate drops below threshold
- **Observability Stack Issues**: Alert when Prometheus, Grafana, or OTLP collector becomes unavailable

## Troubleshooting

### Common Issues

#### Service Issues
- **Services Degraded**: Check Aspire Dashboard logs, wait 2-3 minutes for full startup
- **No Messages in Kafka**: Verify broker status and topic creation in Kafka UI
- **Flink Job Failures**: Check TaskManager resources and job logs in Flink Dashboard
- **High Consumer Lag**: Monitor backpressure configuration and rate limits

#### Observability Issues
- **Grafana No Data**: Check Prometheus targets at http://localhost:9090/targets
- **Missing Metrics**: Verify OTLP collector is receiving data at http://localhost:4318
- **Dashboard Errors**: Confirm datasource connectivity in Grafana settings
- **Slow Queries**: Check Prometheus query performance and time ranges

#### Container Issues
- **Out of Memory**: Increase Docker Desktop memory allocation (16GB+ recommended)
- **Port Conflicts**: Check for conflicting services on ports 3000, 4317, 4318, 9090
- **Mount Failures**: Verify configuration files exist in AppHost directory

### Observability Troubleshooting

**Check OpenTelemetry Configuration:**
```bash
# Verify OTLP endpoints are responding
curl http://localhost:4318/v1/traces
curl http://localhost:4318/v1/metrics

# Check collector health
curl http://localhost:8889/metrics | grep otelcol
```

**Verify Prometheus Targets:**
```bash
# Check all targets status
curl http://localhost:9090/api/v1/targets

# Query specific metrics
curl "http://localhost:9090/api/v1/query?query=up"
```

**Debug Grafana Datasources:**
```bash
# Test Prometheus connection
curl http://localhost:3000/api/datasources/proxy/1/api/v1/query?query=up
```

**Observability Testing Issues:**
- **No Metrics Data**: Check that services are running and Prometheus targets are healthy
- **Grafana Connection Issues**: Verify admin:admin credentials and datasource configuration
- **OTLP Collection Failures**: Check OpenTelemetry collector logs and endpoint accessibility
- **Missing Observability Metrics During Tests**: Ensure sufficient wait time between test steps for metrics collection
- **Service Count Mismatches**: Verify all expected containers are running and healthy

**Common Observability Fixes:**
```bash
# Restart Prometheus if targets are down
docker restart prometheus

# Clear Grafana cache if dashboards aren't loading
docker restart grafana

# Restart OTLP collector if telemetry stops flowing
docker restart otel-collector

# Check container logs for observability services
docker logs prometheus
docker logs grafana
docker logs otel-collector
```

### Resource Requirements
- **Memory**: 16GB+ for all containers (3 Kafka brokers, 3 TaskManagers, monitoring stack)
- **CPU**: 8+ cores recommended for optimal performance
- **Storage**: Adequate disk space for Kafka data retention and Prometheus metrics
- **Network**: All containers use bridge networking for localhost access

## Related Documentation
- [Complex Logic Stress Tests Documentation](../docs/wiki/Complex-Logic-Stress-Tests.md)
- [Rate Limiting Implementation Tutorial](../docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md)