# Day 5: Enterprise Observability - Understanding the Observability Stack Evolution

## 🎯 Learning Objectives

By the end of this lesson, you will understand:
- Why observability starts with ElasticSearch and file-based logging
- When to introduce OpenTelemetry (OTel) for distributed tracing
- When Grafana + Prometheus becomes necessary for metrics
- How to configure Prometheus exporters for Kafka, Flink JobManager, and Flink TaskManager
- Why this course focuses on Grafana + Prometheus for distributed systems
- When to add OpenTelemetry for message-level tracking in streaming pipelines

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

## 🎬 Hands-On Exercise

### Exercise 5.1: Complete Observability Stack Integration

**Objective:** Understand and validate the complete Prometheus-based observability stack with Grafana visualization

**What you'll learn:**
- Prometheus metrics collection from Kafka, Flink JobManager, and TaskManager
- Grafana dashboard navigation and data source configuration
- PromQL queries for system health and throughput monitoring
- Real-time message flow tracking through the pipeline
- Kafka broker topic metrics validation

**Location:** [`LearningCourse/Day05-Enterprise-Observability/Exercise-Solutions/Exercise51/`](./Exercise-Solutions/Exercise51/)

**Run the exercise:**
```bash
cd LearningCourse/Day05-Enterprise-Observability/Exercise-Solutions/Exercise51
dotnet run
```

**What the exercise does:**
1. Produces 1,000 messages to Kafka topic `observability_input_day05`
2. Flink processes messages and writes to `observability_output_day05`
3. All components expose Prometheus metrics:
   - Kafka JMX Exporter: Dynamic port (discovered via Docker)
   - Flink JobManager: Port 9250
   - Flink TaskManager: Port 9251
4. Prometheus scrapes all metrics every 15 seconds
5. Grafana visualizes metrics in pre-configured dashboards

**Test validation:**
```bash
cd LearningCourse
dotnet test IntegrationTests.sln --filter "FullyQualifiedName~Day05Tests"
```

**Available tests:**
1. **Day05GrafanaDashboardTest** - Validates Grafana UI, dashboards, and Prometheus connectivity
2. **Day05PrometheusMetricsTest** - Validates Prometheus metrics collection and Kafka topic metrics

**Key metrics to explore:**

```promql
# System health
up

# Flink input throughput
flink_taskmanager_job_task_operator_numRecordsIn

# Flink output throughput
flink_taskmanager_job_task_operator_numRecordsOut

# Kafka broker topic metrics (note: lowercase with _total suffix)
kafka_server_brokertopicmetrics_messagesinpersec_count_total{topic="observability_input_day05"}

# Message processing rate (messages per second)
rate(flink_taskmanager_job_task_operator_numRecordsOut[1m])

# Cluster health
flink_jobmanager_numRegisteredTaskManagers
```

**Access the observability stack:**
- **Grafana Dashboard**: http://localhost:3000 (anonymous access enabled)
- **Prometheus UI**: http://localhost:9090
- **Flink Dashboard**: http://localhost:8081

---

## 🔭 When to Add OpenTelemetry for Message Tracking

### Understanding the Need for Message-Level Tracing

While Prometheus metrics provide excellent infrastructure-level observability (throughput, latency, resource usage), there are scenarios where you need **message-level tracking** to understand the journey of individual messages through your distributed system.

### The Gap: What Prometheus Cannot Tell You

**Current Observability (Prometheus metrics):**
```promql
# Prometheus tells you AGGREGATE information:
kafka_server_brokertopicmetrics_messagesinpersec_count_total  # ✅ 1000 messages/sec
flink_taskmanager_job_task_operator_numRecordsIn              # ✅ 950 records processed
flink_taskmanager_job_task_operator_numRecordsOut             # ✅ 900 records output

# But it CANNOT tell you INDIVIDUAL message information:
# ❌ Which specific messages were processed?
# ❌ Why were 50 messages dropped (1000 input - 950 processed)?
# ❌ Where exactly did those 50 messages fail?
# ❌ How long did Order #12345 take from Kafka → Flink → Output?
# ❌ What was the processing path for message ID abc-123?
```

### Business Scenario: "Where Is My Order?"

**Customer Support Request:**
> "Customer called: Order #12345 was placed 2 hours ago but shows as 'Processing'. Where is it stuck?"

**With Prometheus only:**
```
Support: "System shows 1000 orders/hour throughput - looks normal"
Customer: "But where is MY order #12345?"
Support: "I can't track individual orders, only aggregate metrics"
```

**With OpenTelemetry tracing:**
```
Support: "Let me search for Order #12345 in Jaeger..."
Trace shows:
├─ 10:00:00.100 - Received in Kafka (topic: orders)
├─ 10:00:00.150 - Flink picked up message
├─ 10:00:00.200 - Validation started
├─ 10:00:00.250 - ❌ Validation FAILED: Missing required field "email"
└─ 10:00:00.251 - Sent to dead-letter queue

Support: "Your order failed validation due to missing email. Please update and resubmit."
```

---

### When OpenTelemetry Becomes Essential

#### Use Case 1: Debugging Message Loss in Streaming Pipelines

**Problem Statement:**
```
Pipeline: Kafka (Producer) → Flink (Transform) → Kafka (Output)
Expected: 10,000 messages
Actual: 9,500 messages in output

Where did 500 messages go? 🤔
```

**Without OTel (Prometheus metrics only):**
```promql
# You see the numbers don't add up:
kafka_in_total: 10000
flink_processed_total: 9800
kafka_out_total: 9500

# But you cannot identify:
# - Which 500 messages were lost?
# - At which stage (Kafka → Flink OR Flink → Output)?
# - Why were they lost (validation, timeout, error)?
```

**With OTel (Distributed tracing):**
```csharp
// Producer side - Start trace
using var activity = activitySource.StartActivity("ProduceMessage", ActivityKind.Producer);
var messageId = Guid.NewGuid().ToString();
activity?.SetTag("message.id", messageId);
activity?.SetTag("order.id", orderId);
activity?.SetTag("topic", "orders_input");

// Propagate trace context via Kafka headers
var headers = new Headers
{
    { "traceparent", Encoding.UTF8.GetBytes(Activity.Current.Id) }
};

await producer.ProduceAsync("orders_input", new Message<string, string>
{
    Key = orderId,
    Value = jsonMessage,
    Headers = headers
});

// Flink processing - Continue trace
var traceParent = kafkaRecord.Headers.First(h => h.Key == "traceparent");
using var activity = activitySource.StartActivity("ProcessMessage", 
    ActivityKind.Consumer, traceParent);

activity?.SetTag("message.id", messageId);
activity?.SetTag("processing.step", "validation");

try 
{
    ValidateMessage(message);
    activity?.SetTag("validation.result", "success");
}
catch (ValidationException ex)
{
    activity?.SetTag("validation.result", "failed");
    activity?.SetTag("validation.error", ex.Message);
    activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
    // Message goes to dead-letter queue
}

// Now in Jaeger you can:
// 1. Search for all failed messages: tags="validation.result:failed"
// 2. See which 500 messages failed validation
// 3. Group by error type to find common issues
// 4. Track full message journey from producer to dead-letter queue
```

**Jaeger Query Results:**
```
Found 500 traces with validation.result=failed:
- 350 messages: Missing required field "email"
- 100 messages: Invalid phone number format
- 50 messages: Duplicate order ID

Action: Fix validation logic for email field (70% of failures)
```

---

#### Use Case 2: Performance Analysis Per Message Type

**Business Question:**
> "Why do VIP customer orders take 5 seconds while regular orders take 100ms?"

**Prometheus aggregate metrics:**
```promql
# Average latency
avg(flink_processing_latency_seconds) = 0.5s

# P99 latency
histogram_quantile(0.99, flink_processing_latency_bucket) = 5s

# But which specific orders are slow? What's different about them?
```

**With OpenTelemetry (per-message breakdown):**
```csharp
using var activity = activitySource.StartActivity("ProcessOrder");
activity?.SetTag("order.id", orderId);
activity?.SetTag("customer.tier", customer.Tier); // "VIP", "Regular", "Premium"
activity?.SetTag("order.amount", order.TotalAmount);
activity?.SetTag("order.item_count", order.Items.Count);

// Child spans for each processing step
using (var dbActivity = activitySource.StartActivity("DatabaseLookup"))
{
    var customerHistory = await _db.GetCustomerHistory(customerId);
    dbActivity?.SetTag("history.order_count", customerHistory.Count);
}

using (var validationActivity = activitySource.StartActivity("VIPValidation"))
{
    if (customer.Tier == "VIP")
    {
        // VIP validation calls external fraud detection API
        await _fraudService.CheckOrder(order); // ← This takes 5 seconds!
        validationActivity?.SetTag("fraud_check.duration_ms", 5000);
    }
}

using (var outputActivity = activitySource.StartActivity("WriteToKafka"))
{
    await _kafkaProducer.ProduceAsync(output);
}
```

**Jaeger Analysis:**
```
Filter traces by:
- customer.tier = "VIP"
- duration > 4s

Results show:
├─ Total duration: 5.1s
│  ├─ DatabaseLookup: 50ms
│  ├─ VIPValidation: 5000ms ← BOTTLENECK!
│  │  └─ External fraud API call: 4950ms
│  └─ WriteToKafka: 50ms

Root cause: VIP fraud detection API has 5s timeout
Solution: Implement async fraud checking with callback
Expected improvement: VIP orders from 5s → 200ms
```

---

#### Use Case 3: Cross-System Message Journey Tracking

**Complex Architecture:**
```
HTTP API → Kafka → Flink Transform → Kafka → Consumer Service → Database → Notification Service
```

**Problem:** Customer complaint: "Order placed 30 minutes ago, no confirmation email"

**Without OTel:**
```
API Team: "We received the order and sent to Kafka" ✅
Kafka Team: "Message is in topic, Flink consumed it" ✅
Flink Team: "We processed it and sent to output topic" ✅
Consumer Team: "We saved to database successfully" ✅
Notification Team: "We haven't received any notification request" ❌

Where did it fail? 🤔 Manual correlation across 5 teams...
```

**With OpenTelemetry (Single trace ID across all systems):**
```csharp
// API receives order - Start trace
[HttpPost("orders")]
public async Task<IActionResult> CreateOrder([FromBody] Order order)
{
    using var activity = activitySource.StartActivity("ReceiveOrder", ActivityKind.Server);
    var traceId = activity?.TraceId.ToString();
    
    activity?.SetTag("order.id", order.Id);
    activity?.SetTag("customer.email", order.Email);
    
    // Propagate trace context to Kafka
    var headers = new Headers
    {
        { "traceparent", Encoding.UTF8.GetBytes(Activity.Current.Id) }
    };
    
    await _kafkaProducer.ProduceAsync("orders_input", new Message<string, string>
    {
        Key = order.Id,
        Value = JsonSerializer.Serialize(order),
        Headers = headers
    });
    
    return Ok(new { orderId = order.Id, traceId });
}

// Flink extracts and continues trace
var parentContext = ExtractTraceContext(kafkaRecord.Headers);
using var activity = activitySource.StartActivity("TransformOrder",
    ActivityKind.Consumer, parentContext);

// Consumer service continues the same trace
using var activity = activitySource.StartActivity("SaveOrder",
    ActivityKind.Client, parentContext);
await _repository.SaveAsync(order);

// Check: Should notification be triggered?
if (order.NotifyCustomer)
{
    using var notifyActivity = activitySource.StartActivity("TriggerNotification");
    await _notificationQueue.EnqueueAsync(order.Id);
}
else
{
    activity?.AddEvent(new ActivityEvent("NotificationSkipped", 
        tags: new ActivityTagsCollection { { "reason", "customer_opted_out" } }));
}
```

**Jaeger Investigation:**
```
Search: order.id = "12345"

Complete trace shows:
├─ 10:00:00.000 - API: ReceiveOrder (200ms)
├─ 10:00:00.200 - Kafka: MessageProduced
├─ 10:00:00.250 - Flink: TransformOrder (100ms)
│  └─ Event: "ValidatedSuccessfully"
├─ 10:00:00.350 - Kafka: OutputMessageProduced
├─ 10:00:00.400 - Consumer: SaveOrder (50ms)
│  └─ Event: "NotificationSkipped" ← FOUND IT!
│     └─ Reason: "customer_opted_out"
└─ Total: 400ms (no notification service involved)

Resolution: Customer opted out of email notifications in profile settings
Action: Update UI to show "Notifications disabled" message
```

---

### OpenTelemetry Integration Architecture for Streaming

```
┌────────────────────────────────────────────────────────────────────┐
│                   OpenTelemetry Instrumentation                    │
│                                                                    │
│  Producer → Kafka → Flink → Kafka → Consumer                      │
│     ↓         ↓       ↓        ↓         ↓                        │
│  [Span]   [Context] [Span]  [Context]  [Span]                    │
│     │         │       │        │         │                        │
│     └─────────┴───────┴────────┴─────────┘                        │
│                       │                                            │
│              Trace ID: abc-123-def-456                            │
│                       ↓                                            │
│              OTel Collector                                        │
│                 ├─→ Traces → Jaeger (message journey)            │
│                 ├─→ Metrics → Prometheus (aggregates)            │
│                 └─→ Logs → ElasticSearch (debug info)            │
└────────────────────────────────────────────────────────────────────┘
```

---

### Implementation Guide: Adding OTel to Exercise 5.1

**Step 1: Add OpenTelemetry NuGet Packages**
```xml
<ItemGroup>
  <PackageReference Include="OpenTelemetry" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Extensions.Hosting" Version="1.7.0" />
  <PackageReference Include="OpenTelemetry.Instrumentation.Http" Version="1.7.0" />
</ItemGroup>
```

**Step 2: Configure OpenTelemetry TracerProvider**
```csharp
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Exercise51.MessageProcessing")
    .SetResourceBuilder(ResourceBuilder.CreateDefault()
        .AddService("exercise51-producer", serviceVersion: "1.0.0"))
    .AddOtlpExporter(options =>
    {
        options.Endpoint = new Uri("http://localhost:4317"); // OTel Collector gRPC endpoint
    })
    .Build();

var activitySource = new ActivitySource("Exercise51.MessageProcessing");
```

**Step 3: Instrument Message Production**
```csharp
for (int i = 0; i < messageCount; i++)
{
    // Start a new trace span for each message
    using var activity = activitySource.StartActivity("ProduceMessage", ActivityKind.Producer);
    
    var messageId = Guid.NewGuid().ToString();
    var orderId = $"ORDER-{i:D6}";
    
    // Add tags for filtering and analysis
    activity?.SetTag("message.id", messageId);
    activity?.SetTag("order.id", orderId);
    activity?.SetTag("topic", inputTopic);
    activity?.SetTag("partition", i % 3);
    activity?.SetTag("message.index", i);
    
    var message = new ObservabilityMessage
    {
        MessageId = messageId,
        OrderId = orderId,
        Timestamp = DateTime.UtcNow,
        Data = $"Order data {i}"
    };
    
    var json = JsonSerializer.Serialize(message);
    
    // Propagate trace context via Kafka headers (W3C Trace Context standard)
    var headers = new Headers();
    if (Activity.Current != null)
    {
        headers.Add("traceparent", Encoding.UTF8.GetBytes(Activity.Current.Id));
        if (!string.IsNullOrEmpty(Activity.Current.TraceStateString))
        {
            headers.Add("tracestate", Encoding.UTF8.GetBytes(Activity.Current.TraceStateString));
        }
    }
    
    var result = await producer.ProduceAsync(inputTopic, new Message<string, string>
    {
        Key = orderId,
        Value = json,
        Headers = headers
    });
    
    // Record Kafka metadata in span
    activity?.SetTag("kafka.partition", result.Partition.Value);
    activity?.SetTag("kafka.offset", result.Offset.Value);
    activity?.SetTag("kafka.timestamp", result.Timestamp.UtcDateTime);
    activity?.AddEvent(new ActivityEvent("MessageProduced", 
        tags: new ActivityTagsCollection 
        { 
            { "kafka.topic", result.Topic },
            { "kafka.partition", result.Partition.Value }
        }));
    
    if (i % 100 == 0)
    {
        Console.WriteLine($"Produced {i} messages (trace: {activity?.TraceId})");
    }
}
```

**Step 4: Extract Trace Context in Flink (Conceptual)**
```csharp
// Note: This would require custom Flink function implementation
public class TracingMapFunction : RichMapFunction<string, string>
{
    private ActivitySource _activitySource;
    
    public override void Open(Configuration parameters)
    {
        _activitySource = new ActivitySource("Exercise51.FlinkProcessor");
    }
    
    public override string Map(string value)
    {
        // Parse Kafka record to extract headers
        var kafkaRecord = ParseKafkaRecord(value);
        
        // Extract trace context from Kafka headers
        var traceparent = kafkaRecord.Headers.FirstOrDefault(h => h.Key == "traceparent")?.Value;
        ActivityContext parentContext = default;
        
        if (traceparent != null)
        {
            ActivityContext.TryParse(Encoding.UTF8.GetString(traceparent), null, out parentContext);
        }
        
        // Start child span continuing the trace
        using var activity = _activitySource.StartActivity("ProcessMessage", 
            ActivityKind.Consumer, parentContext);
        
        activity?.SetTag("message.id", kafkaRecord.MessageId);
        activity?.SetTag("processing.step", "transformation");
        
        try
        {
            // Process message
            var transformed = Transform(kafkaRecord);
            
            activity?.SetTag("processing.result", "success");
            activity?.SetTag("output.size_bytes", transformed.Length);
            
            return transformed;
        }
        catch (Exception ex)
        {
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.RecordException(ex);
            throw;
        }
    }
}
```

**Step 5: Deploy OpenTelemetry Collector**
```yaml
# otel-collector-config.yaml
receivers:
  otlp:
    protocols:
      grpc:
        endpoint: 0.0.0.0:4317
      http:
        endpoint: 0.0.0.0:4318

processors:
  batch:
    timeout: 10s
    send_batch_size: 1024

exporters:
  jaeger:
    endpoint: jaeger:14250
    tls:
      insecure: true
  prometheus:
    endpoint: 0.0.0.0:8889
  logging:
    loglevel: debug

service:
  pipelines:
    traces:
      receivers: [otlp]
      processors: [batch]
      exporters: [jaeger, logging]
    metrics:
      receivers: [otlp]
      processors: [batch]
      exporters: [prometheus]
```

**Step 6: Query Traces in Jaeger**
```bash
# Access Jaeger UI
http://localhost:16686

# Search queries:
# 1. Find all messages for a specific order
Service: exercise51-producer
Tags: order.id="ORDER-000123"

# 2. Find slow message processing (> 1 second)
Service: exercise51-producer
Min Duration: 1s

# 3. Find failed message processing
Service: exercise51-producer
Tags: error=true

# 4. Find messages by topic
Service: exercise51-producer
Tags: topic="observability_input_day05"

# 5. View trace timeline
Click on trace to see:
├─ ProduceMessage (50ms)
│  ├─ Kafka write (30ms)
│  └─ Serialization (20ms)
├─ ProcessMessage (100ms) ← Flink processing
│  ├─ Validation (20ms)
│  ├─ Transformation (50ms)
│  └─ Enrichment (30ms)
└─ OutputMessage (30ms)
Total: 180ms end-to-end
```

---

### Cost-Benefit Analysis: When to Add OTel

**Benefits:**
- ✅ **Individual message debugging**: Track specific orders/transactions
- ✅ **Message loss investigation**: Identify exactly which messages failed and why
- ✅ **Per-message latency analysis**: Find slow messages and their characteristics
- ✅ **Cross-system correlation**: Single trace ID across entire pipeline
- ✅ **Customer support**: Answer "where is my order?" questions instantly
- ✅ **Error pattern detection**: Group failures by error type and message attributes

**Costs:**
- ❌ **Development overhead**: Instrumentation code in every component
- ❌ **Performance impact**: 1-5% CPU overhead for span creation
- ❌ **Storage costs**: Traces require 10-100x more storage than metrics
- ❌ **Infrastructure complexity**: OTel Collector, Jaeger, trace storage
- ❌ **Operational overhead**: Trace retention policies, sampling strategies
- ❌ **Learning curve**: Teams need to understand distributed tracing concepts

**Decision Matrix:**

| Your Situation | Recommendation | Reason |
|----------------|----------------|---------|
| **Simple Kafka → Flink pipeline** | ⛔ **Use Prometheus only** | Aggregate metrics sufficient for infrastructure |
| **Need to debug specific failed messages** | ✅ **Add OpenTelemetry** | Per-message visibility essential |
| **Customer support needs "where is my order?"** | ✅ **Add OpenTelemetry** | Business requirement for tracking |
| **5+ interconnected microservices** | ✅ **Add OpenTelemetry** | Cross-service debugging required |
| **Performance SLAs per customer tier** | ✅ **Add OpenTelemetry** | Need per-message latency analysis |
| **High throughput (>100k msgs/sec)** | ⚠️ **Use 1-10% sampling** | Full tracing too expensive |
| **Compliance/audit requirements** | ✅ **Add OpenTelemetry** | Message journey audit trail needed |

---

### Hybrid Approach: Prometheus + Selective OTel Tracing

**Best Practice for Production:**
```csharp
// Use head-based sampling: trace only a percentage of messages
var tracerProvider = Sdk.CreateTracerProviderBuilder()
    .AddSource("Exercise51.MessageProcessing")
    .SetSampler(new TraceIdRatioBasedSampler(0.01)) // Sample 1% of traces
    .AddOtlpExporter()
    .Build();

// OR: Trace only high-value/error cases
bool shouldTrace = 
    order.Amount > 10000 ||                    // High-value orders
    customer.Tier == "VIP" ||                   // VIP customers
    message.RetryCount > 0 ||                   // Retry attempts
    IsDebugMode();                              // Debug sessions

if (shouldTrace || activity?.Recorded == true)
{
    using var activity = activitySource.StartActivity("ProcessOrder");
    // ... detailed tracing
}
```

**Result:**
- **Prometheus**: 100% of messages → aggregate metrics (cost-effective)
- **OpenTelemetry**: 1-10% of messages → detailed traces (targeted debugging)
- **Best of both worlds**: Infrastructure monitoring + message-level debugging

---

### Summary: Prometheus vs OpenTelemetry

| Aspect | Prometheus Metrics | OpenTelemetry Traces |
|--------|-------------------|---------------------|
| **Purpose** | Infrastructure health, aggregate performance | Individual message journey tracking |
| **Granularity** | Aggregate (all messages) | Per-message (specific messages) |
| **Questions Answered** | "How many?" "How fast?" "How much?" | "Which one?" "Where?" "Why?" |
| **Storage Cost** | Low (time-series points) | High (full trace spans) |
| **Performance Overhead** | Minimal (pull-based scraping) | Moderate (span creation per message) |
| **Use in This Course** | ✅ Primary observability | ⚠️ Add when needed |
| **Production Recommendation** | Always enable | Enable selectively with sampling |

**For Exercise 5.1 (Current State):**
- ✅ **Prometheus**: Sufficient for learning infrastructure observability
- ⛔ **OpenTelemetry**: Not implemented (can be added as advanced exercise)

**When to Revisit:**
- Customer support requires message-level tracking
- Debugging production issues requires per-message analysis
- Business requires audit trail of message processing
- Multiple teams need to correlate incidents across services

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

**4. Kafka JMX metrics not showing**
```bash
# Find Kafka exporter container port (dynamic)
docker ps | grep kafka-exporter

# Test JMX exporter endpoint
curl http://localhost:<port>/metrics

# Check for BrokerTopicMetrics (only appear after messages are produced)
curl http://localhost:<port>/metrics | grep -i brokertopicmetrics

# Inspect JMX exporter logs
docker logs <kafka-exporter-container-id>
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

### OpenTelemetry
- [OpenTelemetry .NET Getting Started](https://opentelemetry.io/docs/instrumentation/net/getting-started/)
- [W3C Trace Context Specification](https://www.w3.org/TR/trace-context/)
- [Jaeger Tracing](https://www.jaegertracing.io/docs/latest/)

---

## ✅ Summary

You've learned:
1. ✅ Why observability starts with ElasticSearch for distributed logs
2. ✅ When to add OpenTelemetry for distributed tracing and message-level tracking
3. ✅ When Grafana + Prometheus becomes essential for metrics
4. ✅ How to configure Prometheus exporters for Kafka, Flink JobManager, and TaskManager
5. ✅ Why this course focuses on Grafana + Prometheus for distributed systems
6. ✅ How to navigate Grafana dashboards and query Prometheus metrics
7. ✅ How to track aggregate message flow through Flink using metrics
8. ✅ When to add OpenTelemetry for individual message journey tracking
9. ✅ Cost-benefit analysis of OpenTelemetry in streaming architectures
10. ✅ Hybrid approach: Prometheus for aggregates + selective OTel tracing

**Key Takeaway:**
> Start with Prometheus for infrastructure observability. Add OpenTelemetry when message-level tracking becomes a business requirement, compliance need, or debugging bottleneck. Use sampling (1-10%) in high-throughput production systems.

**Next Steps:**
- Day 6: Temporal Workflows - Durable orchestration patterns
- Day 7: Advanced Windows and Joins - Complex stream processing
- Day 8: Stress Testing - Performance validation under load


## Running Exercises Manually

The exercises can be run manually outside of the integration tests. This requires starting the infrastructure and setting environment variables that are normally discovered automatically by the test framework.

### Step 1: Start Infrastructure

From the repository root, start the LocalTesting infrastructure in LearningCourse mode:

```bash
# Linux/macOS
cd LocalTesting
./run-learningcourse.sh

# Windows (PowerShell)
cd LocalTesting
$env:LEARNINGCOURSE="true"
dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release
```

This starts:
- Apache Flink cluster (JobManager + TaskManager + SQL Gateway)
- Apache Kafka with JMX metrics
- FlinkDotNet Gateway (port 8086)
- Temporal workflow server (optional, for Day06+)
- Redis (for state management)
- Prometheus (metrics collection)
- Grafana (metrics visualization)

Wait approximately 60 seconds for all containers to be ready.

### Step 2: Discover Service Endpoints

The infrastructure uses dynamic port allocation. You need to discover the actual ports assigned:

1. **Open Aspire Dashboard**: The AppHost will display a URL like `http://localhost:15000`
2. **Find Kafka Port**: Look for "kafka" service, note the host port (e.g., `localhost:32785`)
3. **Find Flink JobManager Port**: Look for "flink-jobmanager-jm-http" service, note the port (e.g., `localhost:32787`)

### Step 3: Set Environment Variables

Before running an exercise, set these environment variables:

```bash
# Linux/macOS
export KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"  # Replace XXXXX with discovered Kafka host port
export KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"  # Fixed container-to-container address
export FLINK_JOB_GATEWAY_URL="http://localhost:8086/"  # Fixed JobGateway port
export FLINK_JOBMANAGER_URL="http://localhost:YYYYY"  # Replace YYYYY with discovered Flink port

# Windows (PowerShell)
$env:KAFKA_BOOTSTRAP_SERVERS="localhost:XXXXX"
$env:KAFKA_FLINK_BOOTSTRAP_SERVERS="kafka:9093"
$env:FLINK_JOB_GATEWAY_URL="http://localhost:8086/"
$env:FLINK_JOBMANAGER_URL="http://localhost:YYYYY"
```

**Optional environment variables** (depending on the exercise):
```bash
# For Day06 Temporal exercises
export TEMPORAL_ENDPOINT="localhost:ZZZZZ"  # Replace with discovered Temporal port

# For exercises using Redis
export REDIS_ENDPOINT="localhost:WWWWW"  # Replace with discovered Redis port
```

### Step 4: Run Exercise

Navigate to the exercise directory and run:

```bash
cd Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/Exercise1-StringCapitalize
dotnet run --configuration Release
```

### Environment Variable Reference

| Variable | Purpose | Example Value |
|----------|---------|---------------|
| `KAFKA_BOOTSTRAP_SERVERS` | Kafka address for producer/consumer on host | `localhost:32785` |
| `KAFKA_FLINK_BOOTSTRAP_SERVERS` | Kafka address for Flink jobs (container-to-container) | `kafka:9093` |
| `FLINK_JOB_GATEWAY_URL` | FlinkDotNet Gateway endpoint for job submission | `http://localhost:8086/` |
| `FLINK_JOBMANAGER_URL` | Flink JobManager REST API for health checks | `http://localhost:32787` |
| `TEMPORAL_ENDPOINT` | Temporal server endpoint (Day06+) | `localhost:32789` |
| `REDIS_ENDPOINT` | Redis endpoint for state management | `localhost:32783` |

### Why Dynamic Ports?

The test infrastructure uses .NET Aspire which assigns dynamic ports to avoid conflicts. This is why you need to discover ports from the Aspire Dashboard rather than using hardcoded values.

### Alternative: Use Integration Tests

For automated testing with automatic port discovery, use the integration test framework:

```bash
# Run all Day01 tests
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Day01Tests"
```

The integration tests automatically:
- Start the infrastructure
- Discover service endpoints
- Set environment variables
- Run exercises
- Validate results
- Clean up resources

