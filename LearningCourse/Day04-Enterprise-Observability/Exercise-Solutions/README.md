# Day 4: Enterprise Observability - Choosing the Right Stack

## 🎯 Learning Objectives
- **Understand when you need each observability component**
- **Choose between simple Prometheus vs. full LGTM stack**
- **Learn the trade-offs: complexity vs. capabilities**
- **Implement the right observability level for your needs**
- **Avoid over-engineering your monitoring setup**

## 🤔 **The Critical Question: Do You Need All These Components?**

### **Short Answer: Usually NO!**

Most projects start simple and add complexity only when needed. Here's the decision tree:

### **Scenario 1: Simple Monitoring (90% of projects)**
```yaml
Stack: Prometheus + Grafana
When to use:
- Single team or small organization
- Metrics are sufficient for your needs
- You don't need distributed tracing
- 15-day retention is enough
- You want minimal operational overhead

Benefits:
- 2 components only (vs. 5+ in full LGTM)
- Proven, stable, well-documented
- Lower resource usage
- Easier troubleshooting
- Faster setup and maintenance
```

### **Scenario 2: Growing Complexity (Enterprise)**
```yaml
Stack: Prometheus + Grafana + Loki (PGL)
When to use:
- Multiple teams need centralized logs
- Debugging requires log correlation
- You have microservices architecture
- Compliance requires log retention

Add Loki when:
- Manual log checking becomes painful
- You need log-based alerting
- Different teams need shared log access
```

### **Scenario 3: Full Observability (Large Scale)**
```yaml
Stack: Full LGTM + OpenTelemetry
When to use:
- Complex distributed systems (100+ services)
- Need distributed tracing for debugging
- Multiple data centers or cloud regions
- Long-term metrics analysis required
- Dedicated SRE/DevOps team

Only add when:
- Prometheus retention (15 days) is insufficient
- Request tracing across services is critical
- You have resources to maintain complex stack
```

## 📊 **Observability Stack Comparison**

### **Option 1: Simple Prometheus + Grafana (Recommended Start)**
```yaml
Components: 2
- Prometheus (metrics collection + storage)
- Grafana (dashboards)

Pros:
✅ Simple setup and maintenance
✅ Well-documented and stable
✅ Low resource usage (~200MB RAM total)
✅ Covers 80% of monitoring needs
✅ Great community support
✅ Perfect for teams getting started

Cons:
❌ No centralized logs
❌ No distributed tracing
❌ Limited to 15-day retention
❌ Manual log checking required

Best for:
- Startups and small teams
- Monolithic applications
- Simple microservices (< 10 services)
- Learning observability concepts
```

### **Option 2: Prometheus + Grafana + Loki (PGL Stack)**
```yaml
Components: 3
- Prometheus (metrics)
- Grafana (dashboards)
- Loki (centralized logs)

Pros:
✅ Centralized log management
✅ Log-based alerting capabilities
✅ Moderate complexity increase
✅ Good for team collaboration
✅ Debugging becomes much easier

Cons:
❌ No distributed tracing
❌ Still limited metrics retention
❌ More components to maintain

Best for:
- Growing teams (5-50 people)
- Microservices architecture
- When manual log checking becomes painful
- Teams needing shared log access
```

### **Option 3: Full LGTM + OpenTelemetry (Maximum Capability)**
```yaml
Components: 5+
- Loki (logs)
- Grafana (dashboards)
- Tempo (distributed tracing)
- Mimir (long-term metrics)
- Prometheus (real-time metrics)
- OpenTelemetry Collector (telemetry routing)

Pros:
✅ Complete observability coverage
✅ Distributed tracing for complex debugging
✅ Long-term metrics analysis
✅ Advanced correlation capabilities
✅ Enterprise-grade scalability

Cons:
❌ High complexity (6 components)
❌ Significant resource overhead (~2GB+ RAM)
❌ Requires dedicated DevOps/SRE expertise
❌ Complex troubleshooting when things break
❌ Longer setup and configuration time

Best for:
- Large organizations (100+ services)
- Complex distributed systems
- Dedicated SRE teams
- Compliance requirements for long retention
- When simple solutions have proven insufficient
```

## 🚦 **Decision Framework**

### **Start Here: Do You Need More Than Prometheus + Grafana?**

Ask yourself these questions:

#### **Question 1: Log Management**
```
Are you manually SSH-ing to containers to check logs?
→ YES: Add Loki
→ NO: Stick with Prometheus + Grafana
```

#### **Question 2: Distributed Tracing**
```
Do you have trouble tracing requests across multiple services?
Do you have >20 microservices?
→ YES: Consider adding Tempo
→ NO: You don't need tracing yet
```

#### **Question 3: Long-term Metrics**
```
Do you need metrics older than 15 days?
Do you do capacity planning based on historical trends?
→ YES: Add Mimir for long-term storage
→ NO: Prometheus default retention is fine
```

#### **Question 4: OpenTelemetry**
```
Do you need automatic instrumentation?
Do you have many different programming languages?
Do you want vendor-neutral telemetry?
→ YES: Add OTEL Collector
→ NO: Direct Prometheus scraping is simpler
```

## 🎯 **Our FlinkDotnet Recommendation**

### **For Learning: Start Simple**
```yaml
Recommended Learning Stack:
- Prometheus + Grafana + Loki (PGL)

Why PGL for learning:
- Covers all three pillars (metrics, logs, traces later)
- Not overwhelming for beginners
- Demonstrates real-world patterns
- Can add complexity incrementally
```

### **For Production: Assess Your Needs**
```yaml
Small Team/Startup:
- Prometheus + Grafana
- Add Loki when log correlation becomes important

Growing Company:
- Prometheus + Grafana + Loki
- Consider Tempo if distributed tracing becomes critical

Enterprise:
- Full LGTM stack with OpenTelemetry
- Dedicated team to manage complexity
```

## 🏗️ Architecture Options

### **Simple Architecture (Prometheus + Grafana)**
```
Applications → Prometheus → Grafana
             ↓
           File Logs (Docker/Kubernetes)
```

### **Balanced Architecture (PGL Stack)**
```
Applications → Prometheus → Grafana ← Loki ← Container Logs
```

### **Current LocalTesting Observability Architecture** (2025 - Complete Infrastructure)

### Full Flow Observability: WebApi → Kafka → Flink → Temporal
```
┌─────────────────────────────────────────────────────────────────────┐
│                    GRAFANA (G) - Unified Dashboard                 │
│         📊 Metrics Visualization | 📋 Log Exploration               │
│                    http://localhost:18005                           │
└─────────────────────────────────────────────────────────────────────┘
                                   │
                     ┌─────────────┼─────────────┐
                     │             │             │
┌─────────────────┐  │  ┌─────────────────┐  │  ┌─────────────────┐
│   LOKI (L)      │◄─┘  │  PROMETHEUS (P) │  └─►│   ASPIRE        │
│   Logs          │     │  Metrics        │     │   Dashboard     │
│   Port: 3100    │     │  Port: 9090     │     │   Port: 18888   │
│   ✅ ACTIVE     │     │   ✅ ACTIVE     │     │   ✅ ACTIVE     │
└─────────────────┘     └─────────────────┘     └─────────────────┘
         ▲                        ▲                        ▲
         │                        │                        │
         └────────────────────────┼────────────────────────┘
                                  │
              ┌─────────────────────────────────────┐
              │    OpenTelemetry Collector (OTEL)   │
              │    🔧 FIXED: Debug Exporter + Config │
              │    HTTP: 4318 | gRPC: 4317         │
              │    Self-Monitor: 8889               │
              │    ✅ STABLE & RUNNING              │
              └─────────────────────────────────────┘
                              ▲
                              │
              ┌───────────────┼───────────────┐
              │               │               │
              ▼               ▼               ▼
┌─────────────────┐   ┌─────────────────┐   ┌─────────────────┐
│ LocalTesting    │   │     Kafka       │   │     Flink       │
│ WebApi          │──▶│   3 Brokers     │──▶│   JobManager    │
│ Port: 5000      │   │ 10 Partitions   │   │   Port: 8081    │
│                 │   │ UI: 8082        │   │ 3 TaskManagers  │
│ ✅ OBSERVABLE   │   │ ✅ MONITORED    │   │ ✅ TRACED       │
└─────────────────┘   └─────────────────┘   └─────────────────┘
          │                       │                   │
          └───────────────────────┼───────────────────┼──────┐
                                  │                   │      │
                                  ▼                   ▼      ▼
                      ┌─────────────────┐   ┌─────────────────────┐
                      │    Temporal     │   │    Redis + PostgreSQL │
                      │   Workflows     │   │   Data Persistence   │
                      │   UI: 8084      │   │   Backend Services   │
                      │ ✅ WORKFLOW OBS │   │ ✅ STORAGE METRICS   │
                      └─────────────────┘   └─────────────────────┘

🔍 COMPLETE OBSERVABILITY COVERAGE:
📊 Metrics: All services → Prometheus → Grafana
📋 Logs: All containers → Loki → Grafana
📈 Traces: Full request flow → Aspire Dashboard
🔄 Flow: WebApi → Kafka → Flink → Temporal → Response
```

## 🔄 OpenTelemetry Integration Flow (Training-Optimized)

### How OTEL Connects Everything in PGL Stack
```yaml
# Telemetry Data Flow
Applications → OTEL Collector → PGL Stack → Grafana Dashboards

# Detailed Flow:
1. Flink/Kafka/Temporal → Generate Metrics/Logs/Traces
2. OTEL Collector → Receives all telemetry via HTTP/gRPC
3. OTEL Processors → Batch, filter, enrich telemetry data
4. OTEL Exporters → Route to appropriate PGL components:
   - Logs → Loki (L)
   - Metrics → Prometheus (P)
   - Traces → Aspire Dashboard (for training simplicity)
5. Grafana (G) → Queries Prometheus and Loki for unified view
```

### OpenTelemetry Configuration Details
Our OTEL Collector acts as the central hub with these capabilities:

**Receivers:**
- `otlp` (HTTP/gRPC): Accept telemetry from applications
- `prometheus`: Scrape metrics from Flink, Kafka, Temporal

**Processors:**
- `batch`: Optimize data transfer efficiency
- `memory_limiter`: Prevent resource exhaustion
- `resource`: Add service metadata and labels

**Exporters:**
- `loki`: Send logs to Loki for centralized storage
- `prometheus`: Send metrics to Prometheus for real-time queries
- `otlp/aspire`: Send traces to Aspire Dashboard for training visibility
- `prometheus/self`: Expose OTEL's own metrics for monitoring

## 📊 LGTM Component Details

### 🅻 **Loki** - Centralized Log Aggregation
```yaml
Purpose: Store and query logs from all services
Port: 3100
Integration: Receives logs from OTEL Collector
Query Language: LogQL (similar to PromQL)
Data Sources: All container logs (Flink, Kafka, Temporal, APIs)

Key Features:
- Label-based indexing (not full-text)
- Cost-effective log storage
- Integration with Grafana for visualization
- Retention policies for log lifecycle management
```

### 🅶 **Grafana** - Unified Visualization Platform
```yaml
Purpose: Single pane of glass for all observability data
Port: 3000
Authentication: Disabled (anonymous admin access)
Data Sources:
  - Loki: http://loki:3100 (logs)
  - Tempo: http://tempo:3200 (traces)
  - Mimir: http://mimir:9009 (long-term metrics)
  - Prometheus: http://prometheus:9090 (real-time metrics)

Dashboard Types:
- Infrastructure overview
- Application performance monitoring
- Business metrics and KPIs
- Error tracking and alerting
```

### 🆃 **Tempo** - Distributed Tracing
```yaml
Purpose: Track requests across multiple services
Port: 3200 (HTTP), 9095 (gRPC), 14268 (Jaeger)
Integration: Receives traces from OTEL Collector
Query: Trace ID lookup and service dependency mapping

Trace Flow Example:
HTTP Request → API Gateway → Flink Job → Kafka → Temporal Workflow
Each step creates spans that form a complete trace
```

### 🅼 **Mimir** - Long-term Metrics Storage
```yaml
Purpose: Scalable, long-term metrics storage (Prometheus-compatible)
Port: 9009
Integration: Receives metrics from OTEL Collector via remote write
Storage: Efficient compression and retention policies
Query: PromQL via Grafana

Vs Prometheus:
- Prometheus: Short-term, local storage (15 days default)
- Mimir: Long-term, distributed storage (months/years)
```

## 🚀 Service-Specific Observability

### Apache Flink 2.1.0 Monitoring

#### Built-in Prometheus Metrics
```properties
# Flink Configuration (automatically applied)
metrics.reporter.prom.class: org.apache.flink.metrics.prometheus.PrometheusReporter
metrics.reporter.prom.port: 9249
metrics.reporter.prom.host: 0.0.0.0
```

#### Key Flink Metrics
```yaml
JobManager Metrics:
- flink_jobmanager_numRegisteredTaskManagers: Active TaskManagers
- flink_jobmanager_numRunningJobs: Currently running jobs
- flink_jobmanager_Status_JVM_Memory_Heap_Used: Memory usage

TaskManager Metrics:
- flink_taskmanager_Status_JVM_Memory_Heap_Used: TaskManager memory
- flink_taskmanager_numBytesInLocal: Network throughput
- flink_taskmanager_numRecordsOut: Processing rate
- flink_taskmanager_Status_Network_bufferPoolUsage: Backpressure indicator

AI/ML Specific Metrics (Flink 2.1.0):
- flink_model_inference_latency: ML model response time
- flink_model_accuracy_score: Real-time model performance
- flink_ai_pipeline_throughput: AI workflow processing rate
```

#### Flink Observability Setup
```yaml
Metrics Collection:
- OTEL Collector scrapes Flink containers on port 9249
- Metrics sent to both Prometheus (real-time) and Mimir (long-term)
- Grafana dashboards show job performance, resource usage

Log Collection:
- Container logs automatically forwarded to Loki
- Structured logging with JSON format
- Query logs by job ID, task, or error level

Tracing:
- Flink jobs instrumented with OpenTelemetry
- Traces show data flow through operators
- Cross-service tracing when integrating with Kafka/Temporal
```

### Apache Kafka 3.8.0 Monitoring

#### JMX Metrics Exposure
```yaml
Configuration: JMX metrics exposed via Prometheus JMX Exporter
Ports: 9308 (JMX metrics endpoint)
Scraping: OTEL Collector pulls metrics every 15 seconds
```

#### Key Kafka Metrics
```yaml
Broker Health:
- kafka_server_brokertopicmetrics_messagesin_total: Message rate
- kafka_server_brokertopicmetrics_bytesin_total: Throughput
- kafka_controller_kafkacontroller_globalpartitioncount: Partition count
- kafka_network_requestmetrics_requestspersec: Request rate

Topic Performance:
- kafka_server_brokertopicmetrics_messagesin_rate: Per-topic message rate
- kafka_server_replicamanager_underreplicatedpartitions: Replication health
- kafka_controller_stats_leaderelectionrateandtimems: Leadership stability

10-Partition Configuration:
- All topics configured with 10 partitions for consistent load distribution
- Monitoring shows balanced partition assignment across brokers
- Consumer lag tracking per partition
```

### Temporal Server Monitoring

#### Metrics Endpoint
```yaml
Port: 9090 (Temporal metrics endpoint)
Format: Prometheus-compatible metrics
Collection: OTEL Collector scrapes and forwards to LGTM stack
```

#### Key Temporal Metrics
```yaml
Workflow Execution:
- temporal_workflow_start_counter: Workflow initiation rate
- temporal_workflow_completed_counter: Successful completions
- temporal_workflow_failed_counter: Failure rate
- temporal_activity_execution_latency: Activity performance

Task Queue Performance:
- temporal_task_queue_poll_latency: Worker polling efficiency
- temporal_task_queue_dispatch_latency: Task assignment speed
- temporal_task_queue_activity_schedule_to_start_latency: Queue depth

Persistence Layer:
- temporal_persistence_requests: Database operation rate
- temporal_history_size: Workflow history growth
- temporal_service_errors: Service-level error tracking
```

## 🎮 Hands-On Exercises

### Exercise 1: Verify Complete Observability Stack (2025 - Current Infrastructure)
```bash
# 1. Start the current infrastructure with all observability components
cd LocalTesting
dotnet run --project LocalTesting.AppHost

# 2. Wait for all services to be ready (3-5 minutes for complete stack)

# 3. Verify each component with current ports and endpoints
curl http://localhost:18005/api/health   # Grafana (unified dashboard)
curl http://localhost:18006/-/healthy    # Prometheus (metrics collection)
curl http://localhost:18005/ready        # Loki (log aggregation)
curl http://localhost:18888/health      # Aspire Dashboard (orchestration)
curl http://localhost:18009/metrics     # OTEL Collector (telemetry processing)
curl http://localhost:5000/             # WebApi Swagger (API testing)

# 4. Verify complete service stack
curl http://localhost:18002             # Flink Dashboard (stream processing)
curl http://localhost:18003             # Kafka UI (message broker)
curl http://localhost:18004             # Temporal UI (workflow orchestration)
```

### Exercise 2: Explore OTEL Telemetry Pipeline
```bash
# 1. Check OTEL Collector health
curl http://localhost:18009/metrics | grep otelcol

# 2. Send test telemetry
curl -X POST http://localhost:4318/v1/metrics \
  -H "Content-Type: application/json" \
  -d '{"resourceMetrics":[{"scopeMetrics":[{"metrics":[{"name":"test_metric","gauge":{"dataPoints":[{"value":42}]}}]}]}]}'

# 3. Verify data reaches PGL stack
# - Check Prometheus: http://localhost:18006/graph?g0.expr=test_metric
# - Check Loki via Grafana data source
```

### Exercise 3: Create Comprehensive Dashboards

#### Flink AI Dashboard
```json
{
  "dashboard": {
    "title": "Flink 2.1.0 AI Processing",
    "panels": [
      {
        "title": "AI Model Inference Rate",
        "type": "graph",
        "targets": [
          {
            "expr": "rate(flink_model_inference_latency[5m])",
            "legendFormat": "{{job_name}}"
          }
        ]
      },
      {
        "title": "Processing Throughput",
        "type": "graph", 
        "targets": [
          {
            "expr": "rate(flink_taskmanager_numRecordsOut[5m])",
            "legendFormat": "{{taskmanager}}"
          }
        ]
      }
    ]
  }
}
```

#### Kafka 10-Partition Dashboard
```json
{
  "dashboard": {
    "title": "Kafka 3.8.0 - 10 Partition Setup",
    "panels": [
      {
        "title": "Messages per Partition",
        "type": "graph",
        "targets": [
          {
            "expr": "kafka_server_brokertopicmetrics_messagesin_rate",
            "legendFormat": "Partition {{partition}}"
          }
        ]
      },
      {
        "title": "Partition Balance",
        "type": "heatmap",
        "targets": [
          {
            "expr": "kafka_controller_kafkacontroller_globalpartitioncount / 10",
            "legendFormat": "Utilization"
          }
        ]
      }
    ]
  }
}
```

### Exercise 4: Complete Flow Observability - WebApi → Kafka → Flink → Temporal

#### 🔍 Full Flow Monitoring Setup

The current LocalTesting infrastructure provides comprehensive observability across the entire data processing pipeline. Here's how to monitor each component and their interactions:

#### **Step 1: End-to-End Flow Tracing**
```bash
# 1. Start a complete flow request that touches all systems
curl -X POST http://localhost:5000/api/backpressure/stress-test \
  -H "Content-Type: application/json" \
  -H "traceparent: 00-12345678901234567890123456789012-1234567890123456-01" \
  -d '{
    "messageCount": 100,
    "processingType": "complex",
    "enableTemporal": true,
    "enableFlink": true
  }'

# 2. This request flows through:
# WebApi → Kafka Message → Flink Processing → Temporal Workflow → Response
```

#### **Step 2: Monitor Each Stage with Current Infrastructure**

**WebApi Monitoring (Port 5000):**
```bash
# Check WebApi health and metrics
curl http://localhost:5000/health
curl http://localhost:5000/         # API documentation (Swagger UI at root)

# View WebApi logs in Grafana
# Navigate to: http://localhost:18005 → Explore → Loki
# Query: {container_name="localtesting-webapi"}
```

**Kafka Monitoring (Port 8082):**
```bash
# Monitor Kafka through UI
open http://localhost:18003

# Check Kafka metrics in Prometheus
open http://localhost:18006/targets  # Verify Kafka scrapers
# Query: kafka_server_brokertopicmetrics_messagesin_total

# View Kafka logs in Loki
# Query: {container_name=~"kafka-broker-.*"}
```

**Flink Monitoring (Port 8081):**
```bash
# Monitor Flink job execution
open http://localhost:18002

# Check Flink metrics in Prometheus
# Query: flink_jobmanager_numRunningJobs
# Query: flink_taskmanager_numRecordsOut

# View Flink processing logs
# Loki Query: {container_name=~"flink-.*"}
```

**Temporal Monitoring (Port 8084):**
```bash
# Monitor Temporal workflows
open http://localhost:18004

# Check Temporal metrics in Prometheus
# Query: temporal_workflow_start_counter
# Query: temporal_workflow_completed_counter

# View Temporal workflow logs
# Loki Query: {container_name=~"temporal-.*"}
```

#### **Step 3: Unified Flow Correlation**

**Cross-Service Log Correlation:**
```logql
# Find all logs for a specific request across all services
{container_name=~"localtesting-webapi|kafka-broker-.*|flink-.*|temporal-.*"}
| json
| trace_id="12345678901234567890123456789012"
| line_format "{{.timestamp}} [{{.level}}] {{.service}}: {{.message}}"
```

**Metrics Correlation Dashboard:**
```promql
# Monitor complete flow performance
sum(rate(http_requests_total{service="webapi"}[5m])) by (endpoint)  # WebApi throughput
sum(rate(kafka_server_brokertopicmetrics_messagesin_total[5m]))      # Kafka ingestion rate
sum(rate(flink_taskmanager_numRecordsOut[5m]))                       # Flink processing rate
sum(rate(temporal_workflow_completed_counter[5m]))                   # Temporal completion rate
```

#### **Step 4: End-to-End Performance Monitoring**

**Create Complete Flow Dashboard in Grafana:**
1. Navigate to http://localhost:18005
2. Create new dashboard with panels for:

```json
{
  "dashboard": {
    "title": "Complete Flow Observability - WebApi → Kafka → Flink → Temporal",
    "panels": [
      {
        "title": "Request Flow Rate",
        "type": "graph",
        "targets": [
          {"expr": "rate(http_requests_total{service=\"webapi\"}[5m])", "legendFormat": "WebApi Requests"},
          {"expr": "rate(kafka_server_brokertopicmetrics_messagesin_total[5m])", "legendFormat": "Kafka Messages"},
          {"expr": "rate(flink_taskmanager_numRecordsOut[5m])", "legendFormat": "Flink Records"},
          {"expr": "rate(temporal_workflow_completed_counter[5m])", "legendFormat": "Temporal Workflows"}
        ]
      },
      {
        "title": "End-to-End Latency",
        "type": "graph",
        "targets": [
          {"expr": "histogram_quantile(0.95, http_request_duration_seconds_bucket{service=\"webapi\"})", "legendFormat": "WebApi p95"},
          {"expr": "kafka_network_requestmetrics_requestspersec", "legendFormat": "Kafka Request Rate"},
          {"expr": "flink_taskmanager_Status_JVM_Memory_Heap_Used", "legendFormat": "Flink Memory Usage"},
          {"expr": "temporal_activity_execution_latency", "legendFormat": "Temporal Activity Latency"}
        ]
      }
    ]
  }
}
```

#### **Step 5: Error Detection and Alerting**

**Monitor Flow Health:**
```promql
# Detect bottlenecks in the flow
(
  rate(http_requests_total{service="webapi", status=~"5.."}[5m]) > 0.01  # WebApi errors
) or (
  kafka_consumer_lag_sum > 1000  # Kafka lag
) or (
  flink_taskmanager_Status_Network_bufferPoolUsage > 0.8  # Flink backpressure
) or (
  rate(temporal_workflow_failed_counter[5m]) > 0.01  # Temporal failures
)
```

#### **Step 6: Comprehensive Flow Testing**

**Test Complete Flow with Observability:**
```bash
# 1. Generate load across all components
for i in {1..10}; do
  curl -X POST http://localhost:5000/api/backpressure/stress-test \
    -H "Content-Type: application/json" \
    -d '{
      "messageCount": 50,
      "processingType": "complex",
      "enableTemporal": true,
      "enableFlink": true
    }' &
done

# 2. Monitor in real-time:
# - Grafana Dashboard: http://localhost:18005
# - Flink Jobs: http://localhost:18002
# - Kafka Topics: http://localhost:18003
# - Temporal Workflows: http://localhost:18004
# - Aspire Overview: http://localhost:18888

# 3. Check flow completion
wait  # Wait for all background requests to complete
```

This provides complete observability across the entire WebApi → Kafka → Flink → Temporal flow with real-time monitoring, correlation, and alerting capabilities.

## 🔍 Advanced LGTM Integration

### 1. **Exemplars** - Linking Metrics to Traces
```yaml
# Prometheus exemplars automatically link metrics to traces
# When viewing a metric spike in Grafana, click "View Trace" to see the exact request
Configuration:
  - Prometheus configured with exemplar storage
  - Tempo provides trace lookup
  - Grafana displays the connection
```

### 2. **Log-to-Trace Correlation**
```yaml
# Automatically correlate logs with traces
LogQL Query: {container="flink-jobmanager"} | json | trace_id != ""
Result: Click any log line to view associated trace in Tempo
```

### 3. **Metric-to-Log Drilling**
```yaml
# From Grafana metric panels, drill down to logs
Process:
  1. See high error rate in metrics
  2. Click "Explore" → Switch to Loki
  3. Pre-filtered logs for the same timeframe and service
```

## 🚨 Production Monitoring Patterns

### SRE Golden Signals
```yaml
Latency:
  - Flink: Job processing latency
  - Kafka: End-to-end message latency  
  - Temporal: Workflow execution time

Traffic:
  - Flink: Events processed per second
  - Kafka: Messages per second per partition
  - Temporal: Workflow starts per minute

Errors:
  - Flink: Job failure rate
  - Kafka: Failed requests / total requests
  - Temporal: Workflow/activity failure rate

Saturation:
  - Flink: Task slot utilization
  - Kafka: Disk usage, network bandwidth
  - Temporal: Task queue depth
```

### Alerting Rules
```yaml
groups:
  - name: flink.rules
    rules:
      - alert: FlinkJobDown
        expr: flink_jobmanager_numRunningJobs == 0
        for: 1m
        labels:
          severity: critical
        annotations:
          summary: "Flink job stopped running"
          
      - alert: FlinkHighBackpressure
        expr: flink_taskmanager_Status_Network_bufferPoolUsage > 0.9
        for: 5m
        labels:
          severity: warning
        annotations:
          summary: "High backpressure detected in Flink TaskManager"

  - name: kafka.rules
    rules:
      - alert: KafkaHighConsumerLag
        expr: kafka_consumer_lag_sum > 10000
        for: 3m
        labels:
          severity: warning
        annotations:
          summary: "Kafka consumer lag is high"
          
      - alert: KafkaPartitionImbalance
        expr: stddev_over_time(kafka_server_brokertopicmetrics_messagesin_rate[10m]) > 100
        for: 5m
        labels:
          severity: info
        annotations:
          summary: "Uneven partition distribution detected"
```

## 📊 Key Performance Indicators (KPIs)

### Business Metrics
```yaml
Data Processing KPIs:
- Events processed per hour
- End-to-end processing latency (p95, p99)
- Data freshness (time from ingestion to availability)
- Processing accuracy (% of successful transformations)

AI/ML Performance (Flink 2.1.0):
- Model inference latency (p95)
- Model prediction accuracy
- Feature engineering throughput
- Real-time model updates per hour

System Reliability:
- Overall system uptime (99.9% SLA target)
- Mean time to recovery (MTTR)
- Error rate (< 0.1% target)
- Resource utilization efficiency
```

## 🎓 Success Criteria

By the end of this exercise, you should have:

✅ **Complete PGL Stack (Training-Optimized)**
- Loki collecting logs from all services
- Grafana providing unified dashboards
- Prometheus storing real-time metrics
- Aspire Dashboard providing trace visibility

✅ **OpenTelemetry Integration**
- OTEL Collector routing telemetry to PGL components
- Correlation between metrics and logs
- Service discovery and metadata enrichment
- Training-optimized configuration

✅ **Service-Specific Monitoring**
- Flink 2.1.0 AI processing metrics
- Kafka 3.8.0 cluster health with 10-partition visibility
- Temporal workflow execution monitoring
- Comprehensive but manageable observability

✅ **Training-Focused Observability**
- SRE golden signals implementation
- Learning-appropriate alerting patterns
- Business KPI tracking
- Simplified but comprehensive monitoring

## 🔗 PGL vs Traditional Monitoring

### Traditional Stack Limitations
```yaml
Problems:
- Siloed tools (separate systems for logs, metrics, traces)
- No correlation between data types
- Multiple UIs and query languages
- Complex data export/import between tools
- Higher operational overhead
```

### PGL Stack Benefits (Training-Optimized)
```yaml
Advantages:
- Unified data model and correlation
- Single UI (Grafana) for metrics and logs
- Consistent query languages (PromQL, LogQL)
- Automatic data linking between metrics and logs
- Open source and vendor-neutral
- Balanced architecture (Prometheus + Loki + Aspire traces)
- Training-friendly complexity level
```

## 📚 Next Steps

- **Day 5**: Learn Temporal Workflows for durable execution patterns
- **Advanced**: Implement distributed tracing across microservices
- **Production**: Set up log retention, metrics aggregation policies
- **Scaling**: Configure Prometheus and Loki for production environments

## 📖 Additional Resources

- [Grafana LGTM Stack Documentation](https://grafana.com/docs/lgtm-stack/)
- [OpenTelemetry Collector Configuration](https://opentelemetry.io/docs/collector/configuration/)
- [Flink Metrics Documentation](https://nightlies.apache.org/flink/flink-docs-release-2.1/docs/ops/metrics/)
- [Kafka Monitoring Best Practices](https://kafka.apache.org/documentation/#monitoring)
- [Temporal Observability Guide](https://docs.temporal.io/production-deployment/observability)

---

**🎯 Enterprise Observability Achieved!** You now have a complete, production-ready observability stack that provides comprehensive visibility into your stream processing platform with proper correlation between metrics, logs, and traces through the unified LGTM stack powered by OpenTelemetry.


## 🛠️ **Practical Implementation: How to Simplify**

### **Simplification Option 1: Remove OTEL Collector**
If you don't need vendor-neutral telemetry or complex routing:

```yaml
# Remove from AppHost Program.cs:
var otelCollector = builder.AddContainer(...) // DELETE THIS

# Direct metrics scraping:
Prometheus scrapes Flink, Kafka, Temporal directly
Grafana connects directly to Prometheus and Loki
Benefits: -1 component, simpler debugging
```

### **Simplification Option 2: Remove Tempo (Distributed Tracing)**
If you don't have complex microservices interactions:

```yaml
# Remove from AppHost Program.cs:
var tempo = builder.AddContainer("tempo",...) // DELETE THIS

# Remove from grafana-datasources.yml:
- name: Tempo // DELETE THIS SECTION

Benefits: -1 component, less complexity
Trade-off: No request flow visualization
```

### **Simplification Option 3: Remove Mimir (Long-term Storage)**
If 15-day retention is sufficient:

```yaml
# Remove from AppHost Program.cs:
var mimir = builder.AddContainer("mimir",...) // DELETE THIS

# Keep Prometheus for all metrics
Benefits: -1 component, simpler operation
Trade-off: No historical analysis beyond 15 days
```

### **Minimal Setup: Prometheus + Grafana Only**
For the simplest monitoring setup:

```csharp
// Minimal AppHost configuration
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(9090, 9090, "prometheus")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml");

var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin");

// Result: Only 2 components to manage!
```

## 🎓 **Learning Path Recommendation**

### **Phase 1: Start Simple (Week 1)**
```yaml
Components: Prometheus + Grafana
Focus: Learn basic metrics and dashboards
Effort: Low
Value: High (covers 80% of needs)
```

### **Phase 2: Add Logs (Week 2)**
```yaml
Components: + Loki
Focus: Centralized log management
Effort: Medium
Value: High (debugging becomes much easier)
```

### **Phase 3: Consider Advanced (Month 2+)**
```yaml
Components: + Tempo + Mimir + OTEL
Focus: Advanced correlation and long-term analysis
Effort: High
Value: Depends on your complexity
```

## 📊 **Cost-Benefit Analysis**

### **Simple Stack (Prometheus + Grafana)**
```yaml
Resource Usage: ~200MB RAM, 2 containers
Setup Time: 30 minutes
Maintenance: Low (well-known components)
Capabilities: Metrics monitoring, basic alerting
Best for: 90% of projects starting with observability
```

### **Complete PGL Stack (Training-Optimized)**
```yaml
Resource Usage: ~800MB RAM, 4 containers
Setup Time: 1-2 hours (balanced complexity)
Maintenance: Medium (manageable components)
Capabilities: Comprehensive observability with correlation
Best for: Training, medium-scale systems, balanced approach
```

## 🎯 **Key Takeaways**

1. **Start Simple**: Prometheus + Grafana covers most needs
2. **Add Complexity Gradually**: Only when current solution proves insufficient
3. **Question Every Component**: Ask "What problem does this solve?"
4. **Consider Operational Overhead**: More components = more things to break
5. **Learn Incrementally**: Master each layer before adding the next

---

**🎯 Smart Observability Achieved!** You now understand how to choose the right observability stack for your needs - from simple Prometheus + Grafana for most projects to the training-optimized PGL stack for comprehensive learning, and the complete LGTM stack for enterprise-scale complexity. Remember: the best observability stack is the simplest one that meets your requirements.
