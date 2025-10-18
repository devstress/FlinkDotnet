# Day 5: Enterprise Observability - Understanding the Observability Stack Evolution

## 🎯 Learning Objectives

By the end of this lesson, you will understand:
- Why observability starts with ElasticSearch and file-based logging
- When to introduce OpenTelemetry (OTel) for distributed tracing
- When Grafana + Prometheus becomes necessary for metrics
- How to configure Prometheus exporters for Kafka, Flink JobManager, and Flink TaskManager
- Why this course focuses on Grafana + Prometheus for distributed systems

---

## 📊 The Observability Stack Evolution

### Stage 1: ElasticSearch + File Sink (Foundation)

**When to use:** Starting any new application, especially microservices

**Why start here:**
- **Distributed Log Aggregation**: ElasticSearch excels at collecting logs from multiple services across different machines
- **Full-Text Search**: Quickly search through millions of log entries across your entire infrastructure
- **Log Correlation**: Find related events across services using correlation IDs
- **Simple Setup**: File-based logging is easy to implement and familiar to all developers
- **Cost-Effective**: No additional infrastructure needed initially - just write to files

**What you get:**
```
Application → File Logs → Filebeat/Fluentd → ElasticSearch → Kibana
```

**Example use case:**
```csharp
// Simple file-based logging
_logger.LogInformation("Processing order {OrderId} for user {UserId}", 
    orderId, userId);
```

**Limitations:**
- ❌ No real-time metrics (only logs after events happen)
- ❌ Requires a lot of manual works for distributed tracing
- ❌ High storage costs for high-volume logs
- ❌ Difficult to extract performance metrics from logs

---

### Stage 2: OpenTelemetry (OTel) - Distributed Tracing

**When to add:** When you have multiple microservices and need to understand request flows

**Why OTel:**
- **Distributed Tracing**: Track a single request as it flows through multiple services
- **Performance Analysis**: Identify bottlenecks in your service mesh
- **Service Dependencies**: Visualize how services interact and depend on each other
- **Standardized Instrumentation**: Vendor-neutral instrumentation (works with Jaeger, Zipkin, etc.)
- **Context Propagation**: Automatically propagate trace context across service boundaries

**What you get:**
```
Service A → HTTP → Service B → gRPC → Service C
   ↓           ↓        ↓          ↓        ↓
   └─────── Trace ID: abc123 spans entire flow ────────┘
                      ↓
           OTel Collector → Jaeger/Zipkin
```

**Example instrumentation:**
```csharp
using OpenTelemetry.Trace;

public class OrderService
{
    private readonly ActivitySource _activitySource;
    
    public async Task ProcessOrder(Order order)
    {
        using var activity = _activitySource.StartActivity("ProcessOrder");
        activity?.SetTag("order.id", order.Id);
        activity?.SetTag("user.id", order.UserId);
        
        // Span automatically includes timing and nested calls
        await _paymentService.ChargeCard(order.Total);  // Creates child span
        await _inventoryService.Reserve(order.Items);    // Creates child span
    }
}
```

**When you need it:**
- Multiple microservices (3+ services)
- Complex request flows
- Cross-service debugging is difficult
- Need to understand service dependencies
- Performance optimization across services

---

### Stage 3: Grafana + Prometheus - Metrics & Monitoring

**When to add:** When you need real-time system health monitoring and alerting

**Why Grafana + Prometheus:**
- **Time-Series Metrics**: Efficient storage and querying of numeric metrics over time
- **Real-Time Monitoring**: See system health as it happens, not after the fact
- **Low Overhead**: Pull-based scraping is less resource-intensive than log shipping
- **Alerting**: Proactive notifications based on metric thresholds
- **Resource Monitoring**: CPU, memory, disk, network metrics out of the box
- **Business Metrics**: Track application-specific KPIs (orders/sec, revenue, etc.)

**What you get:**
```
Flink JobManager:9250 ─┐
Flink TaskManager:9251 ├─→ Prometheus (scrapes every 15s) → Grafana
Kafka JMX:5556 ────────┘              ↓
                               Alertmanager (alerts)
```

**When you need it:**
- Distributed systems (Kubernetes, microservices)
- Need proactive monitoring and alerts
- Performance optimization requires metrics
- SLA/SLO tracking
- Capacity planning based on trends

**Key metrics for Flink:**
```promql
# Messages processed per second
rate(flink_taskmanager_job_task_operator_numRecordsOut[1m])

# Job health
flink_jobmanager_numRegisteredTaskManagers

# Resource usage
flink_taskmanager_Status_JVM_Memory_Heap_Used
```

---

### Stage 4: Full Observability - OTel + Grafana + Prometheus

**When to add:** Enterprise production systems with complex distributed architectures

**Why the full stack:**
- **Unified Observability**: Logs + Traces + Metrics in one place
- **Correlation**: Link metrics spikes to specific traces and logs
- **Root Cause Analysis**: Faster incident resolution with complete context
- **End-to-End Visibility**: From user request to database query and back

**What you get:**
```
Applications with OTel SDK ─→ OTel Collector
                              ├─→ Traces → Jaeger
                              ├─→ Metrics → Prometheus → Grafana
                              └─→ Logs → ElasticSearch → Kibana
```

**Example correlation:**
1. Grafana alert fires: "P99 latency > 1000ms"
2. Click through to Prometheus query
3. Find time range of spike
4. Switch to Jaeger, filter traces in that time range
5. Identify slow span: `database.query: SELECT * FROM orders`
6. Check Kibana for database error logs in same time range
7. Root cause: Database connection pool exhausted

**When you need it:**
- Production systems at scale
- Multiple teams/services
- Compliance requirements (audit logs)
- Complex incident response
- Performance SLAs

---

## 🎓 Why This Course Uses Grafana + Prometheus

### Focus Areas: Distributed Systems with Flink, Kafka, and Temporal

This course focuses on **Stage 3 (Grafana + Prometheus)** for these reasons:

**1. Distributed System Focus**
- Flink, Kafka, and Temporal are inherently distributed
- Metrics are critical for understanding cluster health
- Need real-time monitoring for stream processing

**2. Kubernetes-Ready**
- Prometheus is the de facto standard for Kubernetes monitoring
- Grafana integrates seamlessly with Kubernetes dashboards
- Same stack used in production cloud environments

**3. Stream Processing Metrics**
```promql
# Critical Flink metrics we track:
flink_taskmanager_job_task_operator_numRecordsIn   # Input throughput
flink_taskmanager_job_task_operator_numRecordsOut  # Output throughput
flink_jobmanager_numRegisteredTaskManagers         # Cluster size
flink_taskmanager_Status_JVM_Memory_Heap_Used      # Resource usage
```

**4. Infrastructure Components Monitored**
- **Kafka**: Message broker health, throughput, lag
- **Flink JobManager**: Cluster coordination, job scheduling
- **Flink TaskManager**: Task execution, parallelism, backpressure
- **Temporal**: Workflow execution state (future exercises)

**5. Learning Path Efficiency**
- Prometheus exporters are built into Flink
- Kafka JMX metrics are industry standard
- Same patterns apply to production systems
- No complex setup compared to OTel instrumentation

---

## ❓ Why Don't We Need OpenTelemetry (OTel) in This Exercise?

### TL;DR: We're monitoring infrastructure, not application traces

**Short answer:** OTel is for **distributed tracing of application requests**. This exercise focuses on **infrastructure metrics** (Kafka, Flink), which are already exposed by built-in exporters.

### Detailed Explanation

#### What OTel Provides (and Why We Don't Need It Here):

**1. Distributed Request Tracing**
- **OTel purpose**: Track a single user request as it flows through multiple microservices
- **This exercise**: We're not tracking individual user requests or API calls
- **What we track instead**: Stream processing throughput and infrastructure health

Example of what OTel would trace:
```
User Request → API Gateway → Order Service → Payment Service → Inventory Service
     ↓              ↓              ↓                ↓                  ↓
     └────────── Trace ID: abc123 spans entire request flow ──────────┘
```

Example of what we actually monitor (without OTel):
```
Kafka Messages → Flink Processing → Kafka Output
       ↓                ↓                 ↓
   Throughput       Transform       Throughput
   (metrics)        (metrics)       (metrics)
```

**2. Application Instrumentation**
- **OTel purpose**: Instrument your application code to emit traces and spans
- **This exercise**: Flink and Kafka already have built-in metrics exporters
- **No custom code needed**: Infrastructure components self-report their metrics

**3. Service-to-Service Dependencies**
- **OTel purpose**: Visualize how microservices call each other and their dependencies
- **This exercise**: We have a simple pipeline architecture (Kafka → Flink → Kafka), not a complex service mesh
- **Complexity level**: 3 components in a linear pipeline vs dozens of interconnected microservices

### When You WOULD Need OTel + Prometheus Together

You'd add OTel to this stack if you had:

**Scenario 1: Multiple Microservices with Complex Interactions**
```
┌─────────────────────────────────────────────────────────┐
│  User Request Flow (requires OTel tracing)              │
└─────────────────────────────────────────────────────────┘

API Gateway → Order Service ──┬→ Payment Service
                              ├→ Inventory Service
                              ├→ Shipping Service
                              └→ Notification Service
                                        ↓
                              Each service calls 2-3 others
```

**Scenario 2: Need to Debug Cross-Service Latency**
```
Problem: API response time is 2000ms, but where's the slowdown?

With OTel Traces:
├─ API Gateway: 50ms
├─ Order Service: 100ms
│  ├─ Database query: 80ms
│  └─ Business logic: 20ms
├─ Payment Service: 1800ms ← FOUND THE BOTTLENECK!
│  ├─ External API call: 1750ms ← Third-party payment gateway is slow
│  └─ Processing: 50ms
└─ Notification Service: 50ms

Without OTel: You'd only see "API took 2000ms" with no breakdown
```

**Scenario 3: Multiple Teams/Services Need Correlation**
```
Frontend Team: "The checkout is slow!"
Backend Team: "Which service? What request ID?"
Payment Team: "We don't see any issues..."
Database Team: "Our metrics look normal..."

With OTel: Share trace ID abc123, everyone sees the same request flow
Without OTel: Each team looks at their own logs/metrics in isolation
```

### What We Actually Have in This Exercise

**Simple Linear Pipeline**:
```
Kafka (Producer) → Flink (Transform) → Kafka (Consumer)
       ↓                  ↓                   ↓
   Messages/sec      Processing Rate      Messages/sec
   (Prometheus)      (Prometheus)         (Prometheus)
```

**Infrastructure Metrics Suffice Because**:
1. **No request tracing needed**: We're processing streams, not handling individual user requests
2. **Built-in exporters**: Kafka JMX and Flink Prometheus reporters already exist
3. **Simple data flow**: Linear pipeline is easy to understand without traces
4. **Performance metrics**: Throughput and latency metrics from Prometheus are sufficient

### Real-World Example: When to Add OTel

**Current Exercise (No OTel Needed)**:
- Flink job processes Kafka messages
- Monitor: throughput, lag, processing time
- Tools: Prometheus metrics from Flink/Kafka
- Complexity: Low (3 components)

**E-commerce Platform (OTel Required)**:
- User places order → 15 microservices involved
- Monitor: which service caused the 5-second delay?
- Tools: OTel traces + Prometheus metrics + logs
- Complexity: High (50+ services, 100+ endpoints)

### Summary: This Exercise's Observability Needs

| Observability Need | Solution in This Exercise | Why OTel Not Needed |
|-------------------|---------------------------|---------------------|
| **Infrastructure Health** | Prometheus metrics (CPU, memory, JVM) | OTel doesn't monitor infrastructure |
| **Throughput Tracking** | Flink metrics (records in/out) | Built-in Flink reporter suffices |
| **Performance Monitoring** | Prometheus queries (rate, latency) | No cross-service traces needed |
| **Alerting** | Grafana + Prometheus alerts | Metrics-based alerts work fine |
| **Debugging** | Logs + metrics correlation | Single-pipeline is straightforward |

### When to Revisit OTel

Add OpenTelemetry when you:
1. ✅ Have 5+ microservices with complex interactions
2. ✅ Need to trace individual requests across services
3. ✅ Multiple teams need to correlate incidents
4. ✅ Cross-service latency debugging becomes difficult
5. ✅ Service dependency mapping is unclear

For this Flink streaming exercise, **Prometheus metrics alone provide everything we need**.

---

## 🔧 Prometheus Exporter Configuration

### Overview

We configure **3 Prometheus exporters** in this course:

1. **Kafka JMX Exporter** - Exposes Kafka broker and JVM metrics
2. **Flink JobManager Exporter** - Exposes cluster coordination metrics
3. **Flink TaskManager Exporter** - Exposes task execution metrics

---

### 1. Kafka JMX Exporter Configuration

**Location:** [`LocalTesting/jmx-exporter-kafka-config.yml`](../../LocalTesting/jmx-exporter-kafka-config.yml)

**How it works:**
```yaml
# Connect to Kafka's JMX endpoint
hostPort: kafka:9101

# Transform JMX beans to Prometheus metrics
rules:
  # Kafka Server metrics → kafka_server_*
  - pattern: kafka.server<type=(.+), name=(.+)><>Value
    name: kafka_server_$1_$2
    type: GAUGE
  
  # JVM Memory metrics → java_lang_memory_*
  - pattern: 'java.lang<type=Memory><HeapMemoryUsage>(.+)'
    name: java_lang_memory_heap_$1
    type: GAUGE
```

**Code reference:** [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs:83-108`](../../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs#L83-L108)
```csharp
// Enable JMX on Kafka
kafka = kafka
    .WithEnvironment("KAFKA_JMX_PORT", "9101")
    .WithEnvironment("KAFKA_JMX_HOSTNAME", "0.0.0.0")
    .WithEnvironment("KAFKA_JMX_OPTS",
        "-Dcom.sun.management.jmxremote " +
        "-Dcom.sun.management.jmxremote.authenticate=false " +
        "-Dcom.sun.management.jmxremote.ssl=false");

// Deploy JMX Exporter container
var kafkaExporter = builder.AddContainer("kafka-exporter", "bitnami/jmx-exporter", "latest")
    .WithBindMount(jmxConfigPath, "/opt/bitnami/jmx-exporter/exporter.yml", isReadOnly: true)
    .WithHttpEndpoint(targetPort: 5556, name: "metrics")
    .WithArgs("5556", "/opt/bitnami/jmx-exporter/exporter.yml");
```

**Prometheus scrape config:** [`LocalTesting/prometheus.yml:30-39`](../../LocalTesting/prometheus.yml#L30-L39)
```yaml
- job_name: 'kafka'
  metrics_path: '/metrics'
  static_configs:
    - targets: ['kafka-exporter:5556']
```

---

### 2. Flink JobManager Prometheus Reporter

**How it works:**
- Flink has built-in Prometheus reporter
- Configured via `FLINK_PROPERTIES` environment variable
- Exposes metrics on port 9250

**Code reference:** [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs:131-166`](../../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs#L131-L166)
```csharp
// Configure Prometheus reporter via FLINK_PROPERTIES
var jobManagerFlinkProperties =
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9250\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n";

jobManager = jobManager
    .WithEnvironment("FLINK_PROPERTIES", jobManagerFlinkProperties)
    .WithHttpEndpoint(port: 9250, targetPort: 9250, name: "jm-metrics")
    .WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
```

**Prometheus scrape config:** [`LocalTesting/prometheus.yml:12-19`](../../LocalTesting/prometheus.yml#L12-L19)
```yaml
- job_name: 'flink-jobmanager'
  metrics_path: '/metrics'
  static_configs:
    - targets: ['flink-jobmanager:9250']
```

**Key metrics exposed:**
```promql
flink_jobmanager_numRegisteredTaskManagers  # Cluster size
flink_jobmanager_numRunningJobs             # Active jobs
flink_jobmanager_job_uptime                 # Job runtime
```

---

### 3. Flink TaskManager Prometheus Reporter

**How it works:**
- Same built-in Prometheus reporter as JobManager
- Exposes task execution and resource metrics
- Exposes metrics on port 9251

**Code reference:** [`LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs:179-217`](../../LocalTesting/LocalTesting.FlinkSqlAppHost/Program.cs#L179-L217)
```csharp
// Configure Prometheus reporter via FLINK_PROPERTIES
var taskManagerFlinkProperties =
    "metrics.reporters: prom\n" +
    "metrics.reporter.prom.factory.class: org.apache.flink.metrics.prometheus.PrometheusReporterFactory\n" +
    "metrics.reporter.prom.port: 9251\n" +
    "metrics.reporter.prom.filterLabelValueCharacters: false\n";

taskManager = taskManager
    .WithEnvironment("FLINK_PROPERTIES", taskManagerFlinkProperties)
    .WithHttpEndpoint(port: 9251, targetPort: 9251, name: "tm-metrics")
    .WithBindMount(metricsJarPath, "/opt/flink/lib/flink-metrics-prometheus-2.1.0.jar", isReadOnly: true);
```

**Prometheus scrape config:** [`LocalTesting/prometheus.yml:21-28`](../../LocalTesting/prometheus.yml#L21-L28)
```yaml
- job_name: 'flink-taskmanager'
  metrics_path: '/metrics'
  static_configs:
    - targets: ['flink-taskmanager:9251']
```

**Key metrics exposed:**
```promql
flink_taskmanager_job_task_operator_numRecordsIn     # Input throughput
flink_taskmanager_job_task_operator_numRecordsOut    # Output throughput
flink_taskmanager_Status_JVM_Memory_Heap_Used        # Memory usage
flink_taskmanager_Status_JVM_CPU_Load                # CPU usage
```

---

## 🎬 Hands-On Exercises

### Exercise 1: Grafana Dashboard Exploration

**Objective:** Navigate Grafana UI and explore observability features

**What you'll learn:**
- Anonymous access configuration (no login required)
- Dashboard discovery and navigation
- Data source configuration (Prometheus)
- Flink metrics visualization
- Message flow tracking

**Video demonstration:**
- 📹 [Full UI Test Recording](./videos/GrafanaDashboard_20251018_120432.webm)
- 📸 [Screenshots](./videos/):
  - [Homepage](./videos/Grafana_01_Homepage_20251018_120345.png)
  - [Dashboards](./videos/Grafana_02_Dashboards_20251018_120347.png)
  - [Flink Dashboard](./videos/Grafana_03_FlinkDashboard_20251018_120349.png)
  - [Data Sources](./videos/Grafana_04_DataSources_20251018_120352.png)
  - [Flink Metrics](./videos/Grafana_06_FlinkDashboard_20251018_120428.png)

**Test to run:**
```bash
cd LearningCourse/LearningCourse.IntegrationTests
dotnet test --filter "FullyQualifiedName~UIVideoTest_GrafanaDashboard"
```

**What the test does:**
1. Starts Exercise 1 (message processing pipeline) in background
2. Opens Grafana in browser (anonymous access)
3. Discovers available dashboards
4. Explores Flink metrics dashboard
5. Verifies Prometheus data source connection
6. Tracks message flow through metrics
7. Integrates with Flink Dashboard for job status

---

### Exercise 2: Prometheus Metrics Tracking

**Objective:** Query Prometheus directly and track message processing

**What you'll learn:**
- Prometheus query interface (PromQL)
- System uptime verification
- Flink records IN/OUT metrics
- Throughput rate calculation
- Graph visualization
- Integration with Flink Dashboard

**Video demonstration:**
- 📹 [Full UI Test Recording](./videos/PrometheusMetrics_20251018_120452.webm)
- 📸 [Screenshots](./videos/):
  - [Homepage](./videos/Prometheus_01_Homepage_20251018_120440.png)
  - [Uptime Query](./videos/Prometheus_02_UptimeQuery_20251018_120447.png)
  - [Query Interface](./videos/Prometheus_Debug_QueryInterface_20251018_120443.png)

**Test to run:**
```bash
cd LearningCourse/LearningCourse.IntegrationTests
dotnet test --filter "FullyQualifiedName~UIVideoTest_PrometheusMetrics"
```

**What the test does:**
1. Starts Exercise 1 (message processing pipeline) in background
2. Opens Prometheus UI
3. Queries system uptime: `up`
4. Queries Flink input metrics: `flink_taskmanager_job_task_operator_numRecordsIn`
5. Switches to graph view for visualization
6. Queries Flink output metrics: `flink_taskmanager_job_task_operator_numRecordsOut`
7. Calculates throughput rate: `rate(flink_taskmanager_job_task_operator_numRecordsOut[1m])`
8. Navigates to Flink Dashboard for job verification
9. Returns to Prometheus targets page for health check

**Key PromQL queries:**
```promql
# System health
up

# Message input tracking
flink_taskmanager_job_task_operator_numRecordsIn

# Message output tracking
flink_taskmanager_job_task_operator_numRecordsOut

# Throughput rate (messages per second)
rate(flink_taskmanager_job_task_operator_numRecordsOut[1m])

# Cluster health
flink_jobmanager_numRegisteredTaskManagers
```

---

### Exercise 3: Prometheus Exporter Validation (Non-Playwright)

**Objective:** Verify all three Prometheus exporters are functioning correctly

**What you'll learn:**
- HTTP-based metrics endpoint testing
- Kafka JMX exporter validation (port 5556)
- Flink JobManager metrics validation (port 9250)
- Flink TaskManager metrics validation (port 9251)
- Prometheus scrape target verification

**Test to run:**
```bash
cd LearningCourse/LearningCourse.IntegrationTests
dotnet test --filter "FullyQualifiedName~Day05PrometheusMetricsTest"
```

**What the test does:**
1. Verifies Kafka JMX exporter responds on port 5556
2. Validates Flink JobManager metrics endpoint on port 9250
3. Validates Flink TaskManager metrics endpoint on port 9251
4. Ensures all exporters return valid Prometheus metrics format
5. Uses LocalTesting/test-logs/ for debugging if failures occur

**Metrics endpoints verified:**
- `http://localhost:5556/metrics` - Kafka JMX Exporter
- `http://localhost:9250/metrics` - Flink JobManager
- `http://localhost:9251/metrics` - Flink TaskManager

---

## 📈 Metrics Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                    Observability Stack                      │
│                                                             │
│  ┌─────────────┐      ┌──────────────┐      ┌───────────┐ │
│  │   Grafana   │◄─────┤  Prometheus  │◄─────┤  Scraper  │ │
│  │  Dashboard  │      │   (Metrics   │      │  (15s     │ │
│  │  Visualizer │      │   Storage)   │      │  interval)│ │
│  └─────────────┘      └──────────────┘      └─────┬─────┘ │
│                                                     │       │
└─────────────────────────────────────────────────────┼───────┘
                                                      │
                    ┌─────────────────────────────────┼───────────────────┐
                    │                                 ▼                   │
                    │         Metrics Exporters (HTTP /metrics)          │
                    │                                                     │
                    │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │
                    │  │    Kafka     │  │    Flink     │  │  Flink   │ │
                    │  │ JMX Exporter │  │ JobManager   │  │TaskManager│ │
                    │  │   :5556      │  │   :9250      │  │  :9251   │ │
                    │  └───────┬──────┘  └──────┬───────┘  └────┬─────┘ │
                    │          │                 │               │       │
                    └──────────┼─────────────────┼───────────────┼───────┘
                               │                 │               │
                    ┌──────────┼─────────────────┼───────────────┼───────┐
                    │          ▼                 ▼               ▼       │
                    │  ┌──────────────┐  ┌──────────────┐  ┌──────────┐ │
                    │  │    Kafka     │  │    Flink     │  │  Flink   │ │
                    │  │   (JMX:9101) │  │ JobManager   │  │TaskManager│ │
                    │  │              │  │              │  │          │ │
                    │  └──────────────┘  └──────────────┘  └──────────┘ │
                    │                                                     │
                    │           Application Infrastructure                │
                    └─────────────────────────────────────────────────────┘
```

---

## 🔍 Troubleshooting

### Common Issues

**1. Prometheus shows targets as "Down"**
```bash
# Check Prometheus targets page
http://localhost:9090/targets

# Common causes:
# - Exporters not started yet (wait 30s)
# - Port conflicts
# - Container network issues
```

**2. Grafana shows "No data" in dashboards**
```bash
# Verify Prometheus data source
http://localhost:3000/datasources

# Check Prometheus has data
http://localhost:9090/graph?g0.expr=up

# Ensure scrape interval has passed (15s minimum)
```

**3. Flink metrics not appearing**
```bash
# Check Flink Prometheus port is accessible
curl http://localhost:9250/metrics  # JobManager
curl http://localhost:9251/metrics  # TaskManager

# Verify flink-metrics-prometheus JAR is loaded
docker exec flink-jobmanager ls /opt/flink/lib/ | grep prometheus
```

---

## 📚 Further Reading

### Observability Fundamentals
- [Grafana Fundamentals](https://grafana.com/tutorials/grafana-fundamentals/)
- [Prometheus Best Practices](https://prometheus.io/docs/practices/)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/specs/otel/)

### Flink Monitoring
- [Flink Metrics System](https://nightlies.apache.org/flink/flink-docs-release-2.1/docs/ops/metrics/)
- [Flink Prometheus Reporter](https://nightlies.apache.org/flink/flink-docs-release-2.1/docs/deployment/metric_reporters/#prometheus)

### PromQL (Prometheus Query Language)
- [PromQL Tutorial](https://prometheus.io/docs/prometheus/latest/querying/basics/)
- [PromQL Cheat Sheet](https://promlabs.com/promql-cheat-sheet/)

---

## ✅ Summary

You've learned:
1. ✅ Why observability starts with ElasticSearch for distributed logs
2. ✅ When to add OpenTelemetry for distributed tracing
3. ✅ When Grafana + Prometheus becomes essential for metrics
4. ✅ How to configure Prometheus exporters for Kafka, Flink JobManager, and TaskManager
5. ✅ Why this course focuses on Grafana + Prometheus for distributed systems
6. ✅ How to navigate Grafana dashboards and query Prometheus metrics
7. ✅ How to track message flow through Flink using metrics

**Next Steps:**
- Day 6: Temporal Workflows - Durable orchestration patterns
- Day 7: Advanced Windows and Joins - Complex stream processing
- Day 8: Stress Testing - Performance validation under load
