# LocalTesting - Interactive Stress Test Environment

LocalTesting provides an interactive API environment for debugging and executing Complex Logic Stress Test scenarios with real-time monitoring through Aspire dashboard and specialized UIs.

## Business Flow

The LocalTesting environment implements an 8-step business flow for comprehensive stress testing:

1. **Configure Backpressure**: Set 100 messages/second rate limit per logical queue using Kafka headers
2. **Temporal Message Submission**: Submit job to Temporal to produce 1 million messages to Kafka with 100 partitions and 1000 logical queues. Backpressure blocks submission when hitting rate limits; Temporal retries until downstream processing catches up
3. **Temporal Message Processing**: Submit job to Temporal to process Kafka messages using existing security token logic and correlation ID handling
4. **Flink Concat Job**: Submit Flink job to concatenate 100 messages using saved security tokens, sending to LocalTesting API via Kafka out sink
5. **Kafka In Sink**: Create Kafka in sink to retrieve processed messages from LocalTesting API
6. **Flink Split Job**: Submit Flink job to split messages, adding sending ID and logical queue name using correlation ID matching
7. **Response Output**: Write processed messages to `sample_response` Kafka topic
8. **Message Verification**: Verify top 10 and last 10 messages including both headers and content

## BDD Explanation

The LocalTesting environment transforms BDD (Behavior-Driven Development) test scenarios into executable API endpoints. This approach allows:

- **Step-by-Step Debugging**: Execute each test phase individually through interactive API endpoints
- **Real-Time Monitoring**: Monitor test progress through multiple specialized dashboards
- **Correlation Tracking**: End-to-end tracking of 1 million messages with unique correlation IDs
- **Integration Testing**: Validate complex enterprise streaming scenarios combining Flink, Kafka, Temporal, and HTTP processing

### BDD Features Covered
- **ComplexLogicStressTest.feature**: 1M message processing with correlation ID tracking, security token management, and HTTP batch processing
- **BackpressureTest.feature**: Consumer lag-based flow control following LinkedIn best practices
- **ReliabilityTest.feature**: System reliability and error handling scenarios
- **StressTest.feature**: High-volume performance validation

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

## Observability Configuration

### Overview
The LocalTesting environment includes a complete observability stack with Grafana dashboards, Prometheus metrics collection, and OpenTelemetry distributed tracing. All services automatically send telemetry data to this stack.

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

Execute the 8-step business flow through interactive endpoints:

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

## Monitoring Workflow

### Complete Observability Pipeline

1. **Pre-Test Monitoring**:
   - Check service health in Aspire Dashboard
   - Verify all containers running via `docker ps`
   - Confirm Prometheus targets in Prometheus UI (http://localhost:9090/targets)
   - Validate Grafana datasource connectivity

2. **During Test Execution**:
   - **Real-time Metrics**: Monitor message flow in Kafka UI + Grafana dashboards
   - **Distributed Tracing**: Track request flows in Aspire Dashboard traces
   - **Performance Monitoring**: Watch Flink jobs and TaskManager metrics
   - **Resource Usage**: Monitor container resources in Grafana system dashboard

3. **Post-Test Analysis**:
   - Verify correlation ID matching and data integrity
   - Analyze performance bottlenecks via Grafana dashboards
   - Review distributed traces for latency analysis
   - Export metrics for reporting and optimization

### Key Monitoring Endpoints

| Type | Endpoint | Purpose |
|------|----------|---------|
| **Health** | http://localhost:5000/health | Overall system health |
| **Metrics** | http://localhost:9090 | Prometheus metrics browser |
| **Dashboards** | http://localhost:3000 | Grafana visualization |
| **Traces** | http://localhost:18888 | Aspire distributed tracing |
| **Kafka** | http://localhost:8082 | Message flow monitoring |
| **Flink** | http://localhost:8081 | Stream processing metrics |

### Alerting Setup (Optional)

Configure alerts in Grafana for:
- High consumer lag (>1000 messages)
- Flink job failures or restart
- API response time >5 seconds
- Container memory usage >80%
- Message processing rate drops below threshold

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

### Resource Requirements
- **Memory**: 16GB+ for all containers (3 Kafka brokers, 3 TaskManagers, monitoring stack)
- **CPU**: 8+ cores recommended for optimal performance
- **Storage**: Adequate disk space for Kafka data retention and Prometheus metrics
- **Network**: All containers use bridge networking for localhost access

## Related Documentation
- [Complex Logic Stress Tests Documentation](../docs/wiki/Complex-Logic-Stress-Tests.md)
- [Rate Limiting Implementation Tutorial](../docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md)