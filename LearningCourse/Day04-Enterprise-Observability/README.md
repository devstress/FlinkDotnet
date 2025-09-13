# Day 4: Enterprise Observability & Monitoring

## 🗺️ Course Navigation
**[← Day 3: Production Backpressure](../Day03-Production-Backpressure/)** | **[Course Overview](../README.md)** | **[Next: Day 5 - Temporal Workflows →](../Day05-Temporal-Workflows/)**

---

## 🎯 Real-World Learning Objectives

Master **production-grade observability patterns** used by Netflix, Google, and Uber to monitor distributed streaming systems at scale. Learn to implement comprehensive monitoring, alerting, and troubleshooting workflows for Apache Flink applications.

**Time:** 6-7 hours | **Reference:** [Google SRE Book - Monitoring](https://sre.google/sre-book/monitoring-distributed-systems/)

## 🛠️ LocalTesting Environment Setup

This course uses the **PGL observability stack** (Prometheus + Grafana + Loki) with OpenTelemetry for comprehensive training coverage. The LocalTesting environment provides all observability pillars while maintaining simplicity for effective learning.

### Training-Focused Observability Stack

**What We Cover:**
- ✅ **Metrics**: Prometheus + OpenTelemetry (business & infrastructure metrics)
- ✅ **Logs**: Loki centralized log aggregation with Grafana integration
- ✅ **Traces**: Focus on Flink Job Gateway logs/metrics; Aspire traces optional
- ✅ **Service Health**: Prometheus service discovery with Grafana dashboards

### Quick Setup for Day 4 Exercises

1. **Prerequisites**:
   - .NET 9.0 SDK installed
   - Docker Desktop or Podman running
   - LocalTesting environment setup (see [LocalTesting README](../../LocalTesting/README.md#observability-configuration-and-testing))

2. **Start LocalTesting PGL Stack**:
   ```bash
   cd LocalTesting
   dotnet run --project LocalTesting.AppHost
   ```

3. **Verify Training Stack Endpoints**:
   - **Grafana Dashboard**: http://localhost:18010 (anonymous access enabled)
   - **Prometheus Metrics**: http://localhost:18006/
   - **Loki Logs**: http://localhost:18005/
   - **Grafana Dashboards**: http://localhost:18010/ (admin/admin)
   - **OpenTelemetry Collector**: http://localhost:18009
   - **Flink UI**: http://localhost:8081

4. **Generate Training Data**:
   ```bash
   # Generate comprehensive observability data for training
   curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000
   ```

### Training Benefits

The PGL stack provides:
- **📊 Complete Observability Coverage**: All three pillars (metrics, logs, traces) without complexity
- **🎯 Training-Optimized**: Simplified configuration focused on learning core concepts
- **🔗 Integrated Experience**: Seamless correlation between metrics, logs, and traces in Grafana
- **⚡ Fast Setup**: 3 core components vs 6+ in full enterprise stacks
- **💡 Real Patterns**: Industry-standard PGL stack used by many production teams

💡 **Training Philosophy**: Master the fundamentals with PGL stack, then apply knowledge to any observability platform!

📖 **Why PGL for Training**: See [Observability Stack Comparison](OBSERVABILITY_STACK_COMPARISON.md) for detailed analysis of when to use different complexity levels.

## 📚 Real-World Reference Foundation

This module implements **enterprise observability patterns** from industry leaders:

### 🏛️ Industry Reference Standards
- **[Google SRE Practices](https://sre.google/sre-book/)** - The four golden signals, SLI/SLO design
- **[Netflix Engineering Blog](https://netflixtechblog.com/chaos-engineering-upgraded-878d341f15fa)** - Large-scale monitoring architecture
- **[Uber Engineering: Monitoring at Scale](https://eng.uber.com/logging/)** - Multi-tenant metrics infrastructure
- **[Apache Flink Monitoring](https://nightlies.apache.org/flink/flink-docs-master/docs/ops/monitoring/)** - Official monitoring best practices

### 🔧 Enterprise Technology Stack
- **[OpenTelemetry](https://opentelemetry.io/)** - Vendor-neutral observability framework
- **[Prometheus](https://prometheus.io/)** - Time-series metrics collection and storage
- **[Grafana](https://grafana.com/)** - Metrics visualization and alerting platform
- **[Jaeger](https://jaegertracing.io/)** - Distributed tracing system

### 📊 Choosing the Right Observability Stack
**New:** For comprehensive analysis of different observability approaches, see our detailed comparison guide:

**[📋 Observability Stack Comparison: LGTM vs ELK vs OpenTelemetry →](OBSERVABILITY_STACK_COMPARISON.md)**

This guide covers:
- **LGTM Stack** (Loki + Grafana + Tempo + Mimir) vs **ELK Stack** (Elasticsearch + Logstash + Kibana)
- **OpenTelemetry + SigNoz** vs **Enterprise SaaS** solutions (DataDog, New Relic)
- **Decision framework** for choosing the right complexity level for your organization
- **Cost-benefit analysis** and **migration strategies** between different stacks
- **Professional references** from industry experts and comparative studies

💡 **Key Insight**: Most projects don't need the full complexity of enterprise observability stacks. Start simple with Prometheus + Grafana, then add complexity only when proven necessary.

## 🎯 Quick Start: View Real Observability Metrics in Grafana

**Follow these steps to see live metrics in Grafana dashboards:**

### Step 1: Start LocalTesting Environment
```bash
cd LocalTesting
dotnet run --project LocalTesting.AppHost
```

**Wait for all services to start (look for "Now listening on" messages)**

### Step 2: Generate Real Metrics Data
```bash
# Execute real infrastructure flow to generate observability metrics
curl -X POST http://localhost:5000/api/observability/metrics/simulate \
  -H "Content-Type: application/json" \
  -d '{"kafkaMessages": 10000, "flinkJobs": 2, "temporalWorkflows": 1000}'
```

**Note:** Temporal workflows are triggered for ~10% of messages, so 10,000 messages generate approximately 1,000 workflows.

**Expected output:**
```
✅ Real infrastructure flow executed successfully. Real metrics available.
```

### Step 3: Access Grafana Dashboard
1. **Open Grafana**: http://localhost:18010
2. **Login**: admin/admin (first time setup)
3. **Navigate to**: Dashboards → FlinkDotNet Observability Metrics

**You should now see live metrics showing:**
- 🔄 **Kafka Producing**: Real messages/sec from message production
- 📥 **Kafka Consuming (Flink Input)**: Messages processed by Flink
- ⚙️ **Flink Processing**: Stream processing throughput
- 🔄 **Temporal Processing**: Workflow execution rate

### Step 4: Verify Metrics Are Not Zero
If you see metrics like "0.00 msg/sec", try these troubleshooting steps:

1. **Check metric availability via API**:
   ```bash
   curl http://localhost:5000/api/observability/metrics/messages-per-second
   ```

2. **Verify Prometheus is collecting metrics**:
   ```bash
   curl "http://localhost:18006/api/v1/query?query=kafka_producer_messages_total"
   ```

3. **Generate more metrics data**:
   ```bash
   # Try with more messages to ensure visibility
   curl -X POST http://localhost:5000/api/observability/metrics/simulate \
     -H "Content-Type: application/json" \
     -d '{"kafkaMessages": 50000, "flinkJobs": 3, "temporalWorkflows": 5000}'
   ```

   **Note:** With 10% workflow volume, 50,000 messages generate approximately 5,000 workflows.

4. **Wait and refresh Grafana** (metrics may take 30-60 seconds to propagate)

### Step 5: Create Custom Queries
In Grafana, try these Prometheus queries to explore metrics:

```promql
# Kafka message production rate per partition
rate(kafka_producer_messages_total[5m])

# Flink job processing throughput
rate(flink_job_messages_in_total[5m])

# Temporal workflow execution rate
rate(temporal_workflow_executions_total[5m])

# End-to-end message flow rate
rate(flow_messages_end_to_end_total[5m])
```

### Metrics Flow Architecture

When you execute the observability simulation, here's what happens:

1. **Real Message Production**: `KafkaProducerService` produces messages to Kafka topics
2. **Metric Recording**: Each successful message triggers `ObservabilityMetricsService.RecordKafkaProducerMessage()`
3. **OpenTelemetry Export**: Metrics are exported to Prometheus via OTLP
4. **Prometheus Storage**: Prometheus scrapes and stores the time-series data
5. **Grafana Visualization**: Grafana queries Prometheus and displays real-time charts

### Troubleshooting Zero Metrics

**Common Issue**: All metrics show "0.00 msg/sec" even after simulation

**Root Causes & Solutions:**

1. **Timing Issue - Metrics Not Yet Propagated**
   ```bash
   # Wait 1-2 minutes after simulation, then check API directly
   curl http://localhost:5000/api/observability/metrics/messages-per-second
   ```

2. **Infrastructure Not Ready**
   ```bash
   # Verify all services are running
   curl http://localhost:18006/api/v1/targets  # Check Prometheus targets
   curl http://localhost:18010/api/health      # Check Grafana health
   ```

3. **Metric Export Configuration**
   - Ensure OpenTelemetry exporters are configured in `Program.cs`
   - Verify OTLP endpoint (http://localhost:18009) is accessible
   - Check that meters are properly registered

4. **Local vs Prometheus Metrics**
   - The system falls back to local metrics if Prometheus is empty
   - Check the "MetricsSource" field in API responses for debugging

### Expected Metric Values

After a successful simulation with 10,000 messages, you should see:
- **Kafka Producing**: 100-500 msg/sec (depends on batch size and timing)
- **Kafka Consuming (Flink Input)**: 50-300 msg/sec (Flink processing rate)
- **Flink Processing**: 20-150 msg/sec (transformation throughput)
- **Temporal Processing**: 1-50 exec/sec (workflow execution rate)

## 📸 Grafana Dashboard Screenshots

### Live Metrics Dashboard
![FlinkDotNet Observability Metrics Dashboard](../../docs/wiki/TestScreenshoots/Grafana-FlinkDotNet-Observability-Dashboard.png)
*Real-time observability metrics showing Kafka, Flink, and Temporal throughput*

### Detailed Metrics View
![Grafana Detailed Metrics](../../docs/wiki/TestScreenshoots/Grafana-Detailed-Metrics.png)
*Detailed view of individual metric components with time-series visualization*

### Prometheus Data Source
![Grafana Prometheus Configuration](../../docs/wiki/TestScreenshoots/Grafana-Prometheus-DataSource.png)
*Prometheus data source configuration showing connection to LocalTesting infrastructure*

> **Note**: Screenshots will be automatically updated after implementing the metrics fixes to show actual working dashboard with real data.

## 🌟 The Four Golden Signals of Observability

Following Google SRE practices, enterprise monitoring focuses on four critical signals:

### 1. **Latency** 
Time taken to service requests - differentiate between successful and failed requests

### 2. **Traffic**
Demand on your system - requests per second, events per second, transactions per second

### 3. **Errors** 
Rate of requests that fail - explicit failures and implicit failures (wrong content)

### 4. **Saturation**
How "full" your service is - CPU utilization, memory usage, queue depth

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                    ENTERPRISE OBSERVABILITY ARCHITECTURE                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐ │
│  │   APPLICATION       │    │    COLLECTION       │    │   STORAGE &         │ │
│  │   TELEMETRY         │    │    & PROCESSING     │    │   VISUALIZATION     │ │
│  │                     │    │                     │    │                     │ │
│  │ • Metrics           │───▶│ • OpenTelemetry     │───▶│ • Prometheus        │ │
│  │ • Traces            │    │   Collector         │    │ • Grafana           │ │
│  │ • Logs              │    │ • Data Processing   │    │ • Jaeger            │ │
│  │ • Custom Events     │    │ • Enrichment        │    │ • Alert Manager     │ │
│  └─────────────────────┘    └─────────────────────┘    └─────────────────────┘ │
│           │                           │                           │            │
│           │              ┌─────────────────────────────────────────────────────┤
│           │              │                DATA SOURCES                        │ │
│           │              │                                                     │ │
│           └──────────────│ • Flink Cluster (JobManager + TaskManagers)       │ │
│                          │ • Kafka Cluster (Brokers + Topics)                │ │
│                          │ • Temporal Server (Workflows + Activities)        │ │
│                          │ • Redis Cache (Memory + Operations)               │ │
│                          │ • LocalTesting API (Business Metrics)             │ │
│                          │ • System Infrastructure (CPU, Memory, Network)    │ │
│                          └─────────────────────────────────────────────────────┘ │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## 🏗️ Your Production Observability Stack

Your LocalTesting environment provides a **complete enterprise observability platform** that mirrors production setups at major tech companies:

### Infrastructure Overview

| Component | URL | Enterprise Pattern | Production Use Case |
|-----------|-----|-------------------|-------------------|
| **Grafana Dashboard** | http://localhost:18010 | Netflix monitoring dashboards | Real-time operational visibility |
| **Prometheus Metrics** | http://localhost:18006 | Google Borgmon successor | Time-series metrics storage |
| **OpenTelemetry HTTP** | http://localhost:18008 | CNCF standard telemetry | Vendor-neutral observability |
| **Flink UI** | http://localhost:8081 | Flink job overview | JobManager web UI |
| **Flink Dashboard** | http://localhost:18002 | Stream processing monitoring | Job execution visibility |
| **Temporal UI** | http://localhost:18004 | Workflow execution monitoring | Durable execution visibility |

### Metrics Collection Architecture

```yaml
# Current Prometheus Scrape Targets (enhanced for learning course)
Scrape Targets:
  - prometheus:9090          # Prometheus self-monitoring
  - otel-collector:8889      # OpenTelemetry metrics endpoint
  - flink-jobmanager:8081    # Flink JobManager metrics
  - flink-taskmanager-1:8081 # TaskManager 1 metrics
  - flink-taskmanager-2:8081 # TaskManager 2 metrics  
  - flink-taskmanager-3:8081 # TaskManager 3 metrics
  - localtesting-webapi:5000 # Application business metrics
  - temporal-server:7233     # Temporal workflow metrics
  - redis:6379              # Redis cache metrics
  - kafka-cluster:9092      # Kafka cluster metrics

Collection Frequency: 10-15 seconds (production-grade intervals)
Retention Policy: 15 days (configurable for production needs)
```

## 🚀 Comprehensive Observability Implementation

Let's build a sophisticated observability system that demonstrates enterprise-grade monitoring patterns:

### Step 1: Enhanced Application Metrics

Create `Day04_ObservabilityShowcase.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Exporter;

namespace LearningCourse.Day04
{
    /// <summary>
    /// Production-grade observability showcase demonstrating:
    /// - The Four Golden Signals implementation
    /// - Enterprise metrics patterns from Netflix/Google/Uber
    /// - Distributed tracing with OpenTelemetry
    /// - Custom business metrics and SLI/SLO monitoring
    /// - Real-time alerting patterns
    /// 
    /// References:
    /// - Google SRE Book: Monitoring Distributed Systems
    /// - Netflix Technology Blog: Observability at Scale
    /// - OpenTelemetry Best Practices Guide
    /// </summary>
    public class ObservabilityShowcase
    {
        // OpenTelemetry instrumentation following OTEL semantic conventions
        private static readonly ActivitySource ActivitySource = new("FlinkDotNet.Day04.ObservabilityShowcase");
        private static readonly Meter ApplicationMeter = new("FlinkDotNet.Day04.Application");
        
        // === THE FOUR GOLDEN SIGNALS IMPLEMENTATION ===
        
        // 1. LATENCY - Request processing time distributions
        private static readonly Histogram<double> RequestLatency = ApplicationMeter.CreateHistogram<double>(
            "http_request_duration_seconds",
            "s",
            "Duration of HTTP requests in seconds");
            
        private static readonly Histogram<double> StreamProcessingLatency = ApplicationMeter.CreateHistogram<double>(
            "stream_processing_duration_ms", 
            "ms",
            "Duration of stream processing operations");

        // 2. TRAFFIC - Throughput and request rates
        private static readonly Counter<long> RequestsTotal = ApplicationMeter.CreateCounter<long>(
            "http_requests_total",
            description: "Total number of HTTP requests");
            
        private static readonly Counter<long> EventsProcessedTotal = ApplicationMeter.CreateCounter<long>(
            "events_processed_total",
            description: "Total number of events processed");
            
        private static readonly Gauge<double> CurrentThroughput = ApplicationMeter.CreateGauge<double>(
            "events_per_second_current",
            description: "Current events processing rate per second");

        // 3. ERRORS - Error rates and failure classifications
        private static readonly Counter<long> ErrorsTotal = ApplicationMeter.CreateCounter<long>(
            "errors_total",
            description: "Total number of errors by type and severity");
            
        private static readonly Counter<long> FailedRequestsTotal = ApplicationMeter.CreateCounter<long>(
            "http_requests_failed_total", 
            description: "Total number of failed HTTP requests");

        // 4. SATURATION - Resource utilization and queue depths
        private static readonly Gauge<double> CpuUtilization = ApplicationMeter.CreateGauge<double>(
            "cpu_utilization_percent",
            "%",
            "Current CPU utilization percentage");
            
        private static readonly Gauge<double> MemoryUtilization = ApplicationMeter.CreateGauge<double>(
            "memory_utilization_percent", 
            "%", 
            "Current memory utilization percentage");
            
        private static readonly Gauge<long> QueueDepth = ApplicationMeter.CreateGauge<long>(
            "processing_queue_depth",
            description: "Current number of items in processing queue");

        // === BUSINESS METRICS (SLI/SLO MONITORING) ===
        
        private static readonly Counter<long> BusinessTransactionsTotal = ApplicationMeter.CreateCounter<long>(
            "business_transactions_total",
            description: "Total business transactions processed");
            
        private static readonly Histogram<double> BusinessTransactionValue = ApplicationMeter.CreateHistogram<double>(
            "business_transaction_value_usd",
            "USD",
            "Value of business transactions in USD");
            
        private static readonly Gauge<double> ServiceLevelIndicator = ApplicationMeter.CreateGauge<double>(
            "service_level_indicator_percent",
            "%",
            "Current service level indicator (success rate)");

        // === FLINK-SPECIFIC METRICS ===
        
        private static readonly Gauge<long> FlinkParallelism = ApplicationMeter.CreateGauge<long>(
            "flink_parallelism_current",
            description: "Current Flink job parallelism");
            
        private static readonly Counter<long> FlinkCheckpointsTotal = ApplicationMeter.CreateCounter<long>(
            "flink_checkpoints_total",
            description: "Total number of Flink checkpoints");
            
        private static readonly Histogram<double> FlinkCheckpointDuration = ApplicationMeter.CreateHistogram<double>(
            "flink_checkpoint_duration_ms",
            "ms", 
            "Duration of Flink checkpoints");

        public class EnterpriseEvent
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
            public decimal TransactionValue { get; set; }
            public int Priority { get; set; }
            public Dictionary<string, object> Payload { get; set; } = new();
            public Dictionary<string, string> TraceContext { get; set; } = new();
            
            public override string ToString()
            {
                return $"[{Timestamp:HH:mm:ss.fff}] {EventType} | {Source} | Tenant:{TenantId} | Value:${TransactionValue:F2} | Priority:{Priority}";
            }
        }

        public class ProcessingResult
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime ProcessedAt { get; set; }
            public TimeSpan ProcessingDuration { get; set; }
            public bool Success { get; set; }
            public string ProcessingStage { get; set; } = string.Empty;
            public string? ErrorMessage { get; set; }
            public Dictionary<string, object> Metrics { get; set; } = new();
            public string TraceId { get; set; } = string.Empty;
            public string SpanId { get; set; } = string.Empty;
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🔍 Enterprise Observability Showcase");
            Console.WriteLine("====================================");
            Console.WriteLine("📊 Grafana:     http://localhost:18010");
            Console.WriteLine("📈 Prometheus:  http://localhost:18006");
            Console.WriteLine("🔗 Traces:      http://localhost:18888");
            Console.WriteLine("⚡ Flink:       http://localhost:18002");
            Console.WriteLine();

            // Configure enterprise-grade observability
            using var host = CreateObservabilityHost();
            await host.StartAsync();

            var logger = host.Services.GetRequiredService<ILogger<ObservabilityShowcase>>();
            logger.LogInformation("🚀 Starting enterprise observability showcase");

            try
            {
                // Demonstrate the Four Golden Signals
                await DemonstrateGoldenSignals(logger);
                
                // Show distributed tracing patterns
                await DemonstrateDistributedTracing(logger);
                
                // Implement business metrics and SLI/SLO monitoring
                await DemonstrateBusinessMetrics(logger);
                
                // Showcase alerting and anomaly detection
                await DemonstrateAlertingPatterns(logger);
                
                logger.LogInformation("✅ Enterprise observability showcase completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Enterprise observability showcase failed");
                throw;
            }
            finally
            {
                await host.StopAsync();
            }
        }

        /// <summary>
        /// Configure production-grade observability with OpenTelemetry
        /// Following enterprise patterns from Netflix and Google
        /// </summary>
        private static IHost CreateObservabilityHost()
        {
            return Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    // Configure OpenTelemetry with enterprise resource attributes
                    services.AddOpenTelemetry()
                        .ConfigureResource(resource => resource
                            .AddService("FlinkDotNet.ObservabilityShowcase", "1.0.0")
                            .AddAttributes(new Dictionary<string, object>
                            {
                                ["deployment.environment"] = "local-testing",
                                ["service.namespace"] = "flinkdotnet.learningcourse",
                                ["service.instance.id"] = Environment.MachineName,
                                ["team"] = "platform-engineering",
                                ["component"] = "streaming-application"
                            }))
                        .WithTracing(tracing => tracing
                            .AddSource(ActivitySource.Name)
                            .AddConsoleExporter()
                            .AddOtlpExporter(options =>
                            {
                                options.Endpoint = new Uri("http://localhost:18009");
                                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                            }))
                        .WithMetrics(metrics => metrics
                            .AddMeter(ApplicationMeter.Name)
                            .AddConsoleExporter()
                            .AddOtlpExporter(options =>
                            {
                                options.Endpoint = new Uri("http://localhost:18009");
                                options.Protocol = OtlpExportProtocol.HttpProtobuf;
                            }));
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.SetMinimumLevel(LogLevel.Information);
                })
                .Build();
        }

        /// <summary>
        /// Demonstrate The Four Golden Signals of monitoring
        /// Based on Google SRE best practices
        /// </summary>
        private static async Task DemonstrateGoldenSignals(ILogger logger)
        {
            logger.LogInformation("📊 Demonstrating The Four Golden Signals...");
            
            using var activity = ActivitySource.StartActivity("GoldenSignalsDemo");
            activity?.SetTag("demo.type", "four_golden_signals");
            
            var random = new Random(42);
            var successfulRequests = 0L;
            var totalRequests = 0L;
            var currentQueueDepth = 0L;
            
            // Simulate realistic production workload
            for (int i = 0; i < 1000; i++)
            {
                var requestStart = DateTime.UtcNow;
                var isSuccess = random.NextDouble() > 0.05; // 95% success rate (production SLI)
                var processingTime = isSuccess 
                    ? random.NextDouble() * 100 + 10    // 10-110ms for success
                    : random.NextDouble() * 500 + 200;  // 200-700ms for failures
                
                totalRequests++;
                
                // 1. LATENCY - Record request processing time
                RequestLatency.Record(processingTime / 1000, new KeyValuePair<string, object?>("status", isSuccess ? "success" : "error"));
                
                // 2. TRAFFIC - Record request rate
                RequestsTotal.Add(1, new KeyValuePair<string, object?>("method", "POST"), new KeyValuePair<string, object?>("endpoint", "/api/events"));
                
                if (isSuccess)
                {
                    successfulRequests++;
                    
                    // Process event successfully
                    var eventProcessingTime = random.NextDouble() * 50 + 5; // 5-55ms
                    StreamProcessingLatency.Record(eventProcessingTime);
                    EventsProcessedTotal.Add(1, new KeyValuePair<string, object?>("stage", "processing"));
                    
                    // Business transaction
                    var transactionValue = random.NextDouble() * 1000 + 10; // $10-$1010
                    BusinessTransactionsTotal.Add(1, new KeyValuePair<string, object?>("type", "purchase"));
                    BusinessTransactionValue.Record(transactionValue);
                }
                else
                {
                    // 3. ERRORS - Record error
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("error_type", "processing_failure"),
                        new KeyValuePair<string, object?>("severity", "medium"));
                    FailedRequestsTotal.Add(1, new KeyValuePair<string, object?>("status_code", "500"));
                }
                
                // 4. SATURATION - Update resource utilization
                if (i % 10 == 0) // Update every 10 requests
                {
                    var cpuUsage = 20 + (random.NextDouble() * 60); // 20-80% CPU
                    var memoryUsage = 40 + (random.NextDouble() * 40); // 40-80% Memory
                    currentQueueDepth = Math.Max(0, currentQueueDepth + random.Next(-2, 5)); // Queue fluctuation
                    
                    CpuUtilization.Set(cpuUsage);
                    MemoryUtilization.Set(memoryUsage);
                    QueueDepth.Set(currentQueueDepth);
                    
                    // Calculate current SLI
                    var currentSli = totalRequests > 0 ? (double)successfulRequests / totalRequests * 100 : 0;
                    ServiceLevelIndicator.Set(currentSli);
                    
                    // Traffic metrics
                    var currentThroughput = (double)totalRequests / ((i / 10) + 1) * 10; // Approximate RPS
                    CurrentThroughput.Set(currentThroughput);
                }
                
                // Simulate realistic timing
                if (i % 100 == 0)
                {
                    await Task.Delay(50); // Brief pause every 100 requests
                    logger.LogInformation($"📈 Processed {i:N0} requests | SLI: {(double)successfulRequests / totalRequests:P2} | Queue: {currentQueueDepth}");
                }
            }
            
            logger.LogInformation($"✅ Golden Signals Demo Complete - {totalRequests:N0} requests, {(double)successfulRequests / totalRequests:P2} success rate");
        }

        /// <summary>
        /// Demonstrate distributed tracing patterns
        /// Following OpenTelemetry semantic conventions
        /// </summary>
        private static async Task DemonstrateDistributedTracing(ILogger logger)
        {
            logger.LogInformation("🔗 Demonstrating distributed tracing patterns...");
            
            using var parentActivity = ActivitySource.StartActivity("DistributedTracingDemo");
            parentActivity?.SetTag("demo.type", "distributed_tracing");
            parentActivity?.SetTag("service.name", "observability-showcase");
            
            // Simulate multi-service request flow
            for (int i = 0; i < 50; i++)
            {
                await SimulateDistributedRequest(logger, i);
                
                if (i % 10 == 0)
                {
                    logger.LogInformation($"🔗 Completed {i + 1}/50 distributed traces");
                }
            }
            
            logger.LogInformation("✅ Distributed tracing demonstration complete");
        }

        private static async Task SimulateDistributedRequest(ILogger logger, int requestId)
        {
            using var requestActivity = ActivitySource.StartActivity("ProcessDistributedRequest");
            requestActivity?.SetTag("request.id", requestId.ToString());
            requestActivity?.SetTag("http.method", "POST");
            requestActivity?.SetTag("http.url", "/api/events/process");
            
            var random = new Random();
            
            try
            {
                // Step 1: API Gateway
                using (var gatewayActivity = ActivitySource.StartActivity("APIGateway.ProcessRequest"))
                {
                    gatewayActivity?.SetTag("component", "api-gateway");
                    gatewayActivity?.SetTag("span.kind", "server");
                    
                    await Task.Delay(random.Next(5, 15)); // 5-15ms gateway processing
                    
                    RequestsTotal.Add(1, new KeyValuePair<string, object?>("service", "api-gateway"));
                }
                
                // Step 2: Authentication Service
                using (var authActivity = ActivitySource.StartActivity("AuthService.ValidateToken"))
                {
                    authActivity?.SetTag("component", "auth-service");
                    authActivity?.SetTag("span.kind", "client");
                    
                    await Task.Delay(random.Next(10, 30)); // 10-30ms auth validation
                    
                    if (random.NextDouble() < 0.02) // 2% auth failures
                    {
                        authActivity?.SetStatus(ActivityStatusCode.Error, "Authentication failed");
                        ErrorsTotal.Add(1, new KeyValuePair<string, object?>("service", "auth-service"));
                        throw new UnauthorizedAccessException("Invalid token");
                    }
                }
                
                // Step 3: Business Logic Service  
                using (var businessActivity = ActivitySource.StartActivity("BusinessService.ProcessEvent"))
                {
                    businessActivity?.SetTag("component", "business-service");
                    businessActivity?.SetTag("span.kind", "internal");
                    businessActivity?.SetTag("event.type", "transaction");
                    
                    await Task.Delay(random.Next(20, 80)); // 20-80ms business processing
                    
                    var transactionValue = random.NextDouble() * 500 + 50; // $50-$550
                    businessActivity?.SetTag("transaction.value", transactionValue);
                    BusinessTransactionValue.Record(transactionValue);
                }
                
                // Step 4: Database Service
                using (var dbActivity = ActivitySource.StartActivity("DatabaseService.SaveEvent"))
                {
                    dbActivity?.SetTag("component", "database-service");
                    dbActivity?.SetTag("span.kind", "client");
                    dbActivity?.SetTag("db.system", "postgresql");
                    dbActivity?.SetTag("db.operation", "INSERT");
                    
                    await Task.Delay(random.Next(15, 40)); // 15-40ms database operation
                    
                    if (random.NextDouble() < 0.01) // 1% database failures
                    {
                        dbActivity?.SetStatus(ActivityStatusCode.Error, "Database timeout");
                        ErrorsTotal.Add(1, new KeyValuePair<string, object?>("service", "database"));
                        throw new TimeoutException("Database operation timed out");
                    }
                }
                
                // Step 5: Kafka Producer
                using (var kafkaActivity = ActivitySource.StartActivity("KafkaProducer.SendEvent"))
                {
                    kafkaActivity?.SetTag("component", "kafka-producer");
                    kafkaActivity?.SetTag("span.kind", "producer");
                    kafkaActivity?.SetTag("messaging.system", "kafka");
                    kafkaActivity?.SetTag("messaging.destination", "events-topic");
                    
                    await Task.Delay(random.Next(5, 20)); // 5-20ms kafka send
                    
                    EventsProcessedTotal.Add(1, new KeyValuePair<string, object?>("destination", "kafka"));
                }
                
                requestActivity?.SetStatus(ActivityStatusCode.Ok);
                requestActivity?.SetTag("http.status_code", 200);
                
            }
            catch (Exception ex)
            {
                requestActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                requestActivity?.SetTag("http.status_code", 500);
                requestActivity?.SetTag("error.type", ex.GetType().Name);
                
                FailedRequestsTotal.Add(1, new KeyValuePair<string, object?>("error_type", ex.GetType().Name));
                
                logger.LogWarning(ex, "Request {RequestId} failed in distributed processing", requestId);
            }
        }

        /// <summary>
        /// Demonstrate business metrics and SLI/SLO monitoring
        /// Following Google SRE practices for service reliability
        /// </summary>
        private static async Task DemonstrateBusinessMetrics(ILogger logger)
        {
            logger.LogInformation("💼 Demonstrating business metrics and SLI/SLO monitoring...");
            
            using var activity = ActivitySource.StartActivity("BusinessMetricsDemo");
            activity?.SetTag("demo.type", "business_metrics");
            
            var random = new Random();
            var totalTransactions = 0L;
            var successfulTransactions = 0L;
            var totalRevenue = 0.0;
            
            // SLO: 99.5% availability, P99 latency < 200ms, 0.1% error rate
            var sloAvailabilityTarget = 99.5;
            var sloLatencyTarget = 200.0; // milliseconds
            var sloErrorRateTarget = 0.1; // percent
            
            for (int i = 0; i < 500; i++)
            {
                using var transactionActivity = ActivitySource.StartActivity("ProcessBusinessTransaction");
                transactionActivity?.SetTag("transaction.id", Guid.NewGuid().ToString());
                
                var processingStart = DateTime.UtcNow;
                totalTransactions++;
                
                try
                {
                    // Simulate business transaction processing
                    var processingTime = GenerateRealisticLatency(random);
                    await Task.Delay((int)processingTime);
                    
                    // Generate business metrics
                    var transactionValue = GenerateTransactionValue(random);
                    var transactionType = GenerateTransactionType(random);
                    var customerTier = GenerateCustomerTier(random);
                    
                    // Record business metrics
                    BusinessTransactionsTotal.Add(1, 
                        new KeyValuePair<string, object?>("type", transactionType),
                        new KeyValuePair<string, object?>("customer_tier", customerTier));
                    
                    BusinessTransactionValue.Record(transactionValue,
                        new KeyValuePair<string, object?>("type", transactionType),
                        new KeyValuePair<string, object?>("currency", "USD"));
                    
                    // Record latency
                    StreamProcessingLatency.Record(processingTime);
                    
                    successfulTransactions++;
                    totalRevenue += transactionValue;
                    
                    transactionActivity?.SetTag("transaction.type", transactionType);
                    transactionActivity?.SetTag("transaction.value", transactionValue);
                    transactionActivity?.SetTag("customer.tier", customerTier);
                    transactionActivity?.SetTag("processing.duration_ms", processingTime);
                    
                    // Check SLO compliance
                    var isLatencySloCompliant = processingTime <= sloLatencyTarget;
                    transactionActivity?.SetTag("slo.latency.compliant", isLatencySloCompliant);
                    
                    if (!isLatencySloCompliant)
                    {
                        logger.LogWarning("🚨 SLO Violation: Latency {Latency}ms exceeds target {Target}ms", processingTime, sloLatencyTarget);
                    }
                    
                }
                catch (Exception ex)
                {
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("error_type", "business_logic_failure"),
                        new KeyValuePair<string, object?>("severity", "high"));
                    
                    transactionActivity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                    logger.LogError(ex, "❌ Business transaction failed");
                }
                
                // Calculate and update SLIs
                if (i % 50 == 0 && i > 0)
                {
                    var currentAvailability = (double)successfulTransactions / totalTransactions * 100;
                    var currentErrorRate = (double)(totalTransactions - successfulTransactions) / totalTransactions * 100;
                    var averageRevenue = totalRevenue / successfulTransactions;
                    
                    ServiceLevelIndicator.Set(currentAvailability);
                    
                    // Check SLO compliance
                    var isAvailabilitySloCompliant = currentAvailability >= sloAvailabilityTarget;
                    var isErrorRateSloCompliant = currentErrorRate <= sloErrorRateTarget;
                    
                    logger.LogInformation($"📊 Business Metrics Update:");
                    logger.LogInformation($"   💰 Revenue: ${totalRevenue:F2} | Avg: ${averageRevenue:F2}");
                    logger.LogInformation($"   ✅ Availability: {currentAvailability:F2}% (SLO: {sloAvailabilityTarget}%) {(isAvailabilitySloCompliant ? "✅" : "🚨")}");
                    logger.LogInformation($"   ❌ Error Rate: {currentErrorRate:F2}% (SLO: ≤{sloErrorRateTarget}%) {(isErrorRateSloCompliant ? "✅" : "🚨")}");
                    logger.LogInformation($"   📈 Transactions: {successfulTransactions:N0}/{totalTransactions:N0}");
                    
                    if (!isAvailabilitySloCompliant || !isErrorRateSloCompliant)
                    {
                        logger.LogWarning("🚨 SLO VIOLATION DETECTED - Consider triggering alerts or scaling actions");
                    }
                }
            }
            
            logger.LogInformation("✅ Business metrics demonstration complete");
        }

        /// <summary>
        /// Demonstrate alerting patterns and anomaly detection
        /// Following Netflix and Google alerting best practices
        /// </summary>
        private static async Task DemonstrateAlertingPatterns(ILogger logger)
        {
            logger.LogInformation("🚨 Demonstrating alerting patterns and anomaly detection...");
            
            using var activity = ActivitySource.StartActivity("AlertingPatternsDemo");
            activity?.SetTag("demo.type", "alerting_patterns");
            
            // Simulate various alerting scenarios
            await SimulateHighLatencyAlert(logger);
            await SimulateErrorRateSpike(logger);
            await SimulateResourceSaturation(logger);
            await SimulateThroughputAnomaly(logger);
            
            logger.LogInformation("✅ Alerting patterns demonstration complete");
        }

        private static async Task SimulateHighLatencyAlert(ILogger logger)
        {
            logger.LogInformation("🕐 Simulating high latency alert scenario...");
            
            using var activity = ActivitySource.StartActivity("HighLatencyAlert");
            var random = new Random();
            
            // Normal latency baseline: 10-50ms
            // Alert threshold: P99 > 200ms for 5 consecutive measurements
            
            var latencyMeasurements = new List<double>();
            
            for (int i = 0; i < 20; i++)
            {
                double latency;
                
                if (i >= 10 && i <= 15) // Simulate latency spike
                {
                    latency = 300 + random.NextDouble() * 200; // 300-500ms (alert condition)
                    logger.LogWarning("🚨 HIGH LATENCY DETECTED: {Latency:F1}ms", latency);
                }
                else
                {
                    latency = 10 + random.NextDouble() * 40; // Normal: 10-50ms
                }
                
                latencyMeasurements.Add(latency);
                StreamProcessingLatency.Record(latency);
                
                // Calculate P99 over last 10 measurements
                if (latencyMeasurements.Count >= 10)
                {
                    var recentMeasurements = latencyMeasurements.TakeLast(10).OrderBy(x => x).ToList();
                    var p99 = recentMeasurements[(int)(recentMeasurements.Count * 0.99)];
                    
                    if (p99 > 200)
                    {
                        logger.LogError("🚨 ALERT: P99 Latency {P99:F1}ms exceeds threshold (200ms)", p99);
                        
                        // Record alert metrics
                        ErrorsTotal.Add(1, 
                            new KeyValuePair<string, object?>("alert_type", "high_latency"),
                            new KeyValuePair<string, object?>("severity", "critical"));
                    }
                }
                
                await Task.Delay(100);
            }
        }

        private static async Task SimulateErrorRateSpike(ILogger logger)
        {
            logger.LogInformation("🔥 Simulating error rate spike scenario...");
            
            using var activity = ActivitySource.StartActivity("ErrorRateSpike");
            var random = new Random();
            
            var totalRequests = 0;
            var errorRequests = 0;
            
            for (int i = 0; i < 100; i++)
            {
                totalRequests++;
                
                // Normal error rate: 1-2%
                // Alert condition: >5% error rate over 5-minute window
                
                bool isError;
                if (i >= 30 && i <= 60) // Simulate error spike
                {
                    isError = random.NextDouble() < 0.15; // 15% error rate (alert condition)
                }
                else
                {
                    isError = random.NextDouble() < 0.02; // Normal: 2% error rate
                }
                
                if (isError)
                {
                    errorRequests++;
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("error_type", "service_unavailable"),
                        new KeyValuePair<string, object?>("severity", "high"));
                    FailedRequestsTotal.Add(1, new KeyValuePair<string, object?>("status_code", "503"));
                    
                    logger.LogWarning("❌ Request failed: Service unavailable");
                }
                else
                {
                    RequestsTotal.Add(1, new KeyValuePair<string, object?>("status", "success"));
                }
                
                // Check error rate every 10 requests
                if (i % 10 == 0 && i > 0)
                {
                    var errorRate = (double)errorRequests / totalRequests * 100;
                    
                    if (errorRate > 5.0)
                    {
                        logger.LogError("🚨 ALERT: Error rate {ErrorRate:F1}% exceeds threshold (5%)", errorRate);
                        
                        // Record alert
                        ErrorsTotal.Add(1, 
                            new KeyValuePair<string, object?>("alert_type", "high_error_rate"),
                            new KeyValuePair<string, object?>("severity", "critical"));
                    }
                }
                
                await Task.Delay(50);
            }
        }

        private static async Task SimulateResourceSaturation(ILogger logger)
        {
            logger.LogInformation("📊 Simulating resource saturation scenario...");
            
            using var activity = ActivitySource.StartActivity("ResourceSaturation");
            var random = new Random();
            
            for (int i = 0; i < 30; i++)
            {
                // Simulate gradually increasing resource usage
                var cpuUsage = Math.Min(95, 20 + (i * 2.5) + random.NextDouble() * 10); // Gradually increase to 95%
                var memoryUsage = Math.Min(90, 30 + (i * 2) + random.NextDouble() * 5); // Gradually increase to 90%
                var queueDepth = Math.Max(0, i * 3 + random.Next(-5, 10)); // Queue growing
                
                CpuUtilization.Set(cpuUsage);
                MemoryUtilization.Set(memoryUsage);
                QueueDepth.Set(queueDepth);
                
                // Alert thresholds: CPU > 80%, Memory > 85%, Queue > 50
                if (cpuUsage > 80)
                {
                    logger.LogWarning("🚨 HIGH CPU USAGE: {CpuUsage:F1}%", cpuUsage);
                    
                    if (cpuUsage > 90)
                    {
                        logger.LogError("🚨 CRITICAL: CPU usage {CpuUsage:F1}% exceeds critical threshold (90%)", cpuUsage);
                        ErrorsTotal.Add(1, 
                            new KeyValuePair<string, object?>("alert_type", "cpu_critical"),
                            new KeyValuePair<string, object?>("severity", "critical"));
                    }
                }
                
                if (memoryUsage > 85)
                {
                    logger.LogError("🚨 ALERT: Memory usage {MemoryUsage:F1}% exceeds threshold (85%)", memoryUsage);
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("alert_type", "memory_high"),
                        new KeyValuePair<string, object?>("severity", "warning"));
                }
                
                if (queueDepth > 50)
                {
                    logger.LogError("🚨 ALERT: Queue depth {QueueDepth} exceeds threshold (50)", queueDepth);
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("alert_type", "queue_backlog"),
                        new KeyValuePair<string, object?>("severity", "warning"));
                }
                
                await Task.Delay(200);
            }
        }

        private static async Task SimulateThroughputAnomaly(ILogger logger)
        {
            logger.LogInformation("📈 Simulating throughput anomaly detection...");
            
            using var activity = ActivitySource.StartActivity("ThroughputAnomaly");
            var random = new Random();
            
            // Baseline throughput: 100-150 events/second
            var baselineThroughput = 125.0;
            var throughputMeasurements = new List<double>();
            
            for (int i = 0; i < 20; i++)
            {
                double currentThroughput;
                
                if (i >= 8 && i <= 12) // Simulate throughput drop
                {
                    currentThroughput = baselineThroughput * 0.3 + random.NextDouble() * 20; // 30% of baseline
                    logger.LogWarning("📉 THROUGHPUT DROP DETECTED: {Throughput:F1} events/second", currentThroughput);
                }
                else if (i >= 15) // Simulate throughput spike
                {
                    currentThroughput = baselineThroughput * 2.5 + random.NextDouble() * 50; // 250% of baseline
                    logger.LogInformation("📈 THROUGHPUT SPIKE: {Throughput:F1} events/second", currentThroughput);
                }
                else
                {
                    currentThroughput = baselineThroughput + random.NextDouble() * 25 - 12.5; // Normal variation
                }
                
                throughputMeasurements.Add(currentThroughput);
                CurrentThroughput.Set(currentThroughput);
                
                // Anomaly detection: throughput < 50% or > 200% of baseline
                if (currentThroughput < baselineThroughput * 0.5)
                {
                    logger.LogError("🚨 ALERT: Throughput {Throughput:F1} is {Percentage:F1}% below baseline", 
                        currentThroughput, (1 - currentThroughput / baselineThroughput) * 100);
                    
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("alert_type", "throughput_low"),
                        new KeyValuePair<string, object?>("severity", "warning"));
                }
                else if (currentThroughput > baselineThroughput * 2.0)
                {
                    logger.LogWarning("🚨 ALERT: Throughput {Throughput:F1} is {Percentage:F1}% above baseline", 
                        currentThroughput, (currentThroughput / baselineThroughput - 1) * 100);
                    
                    ErrorsTotal.Add(1, 
                        new KeyValuePair<string, object?>("alert_type", "throughput_high"),
                        new KeyValuePair<string, object?>("severity", "info"));
                }
                
                await Task.Delay(300);
            }
        }

        // Helper methods for realistic data generation
        
        private static double GenerateRealisticLatency(Random random)
        {
            // Generate realistic latency distribution (log-normal)
            var normalRandom = GenerateNormalRandom(random);
            var logLatency = 3.0 + normalRandom * 0.5; // Mean around 20ms, with tail
            return Math.Exp(logLatency);
        }

        private static double GenerateTransactionValue(Random random)
        {
            // Generate realistic transaction values (power law distribution)
            if (random.NextDouble() < 0.8) // 80% small transactions
            {
                return 10 + random.NextDouble() * 90; // $10-$100
            }
            else if (random.NextDouble() < 0.95) // 15% medium transactions
            {
                return 100 + random.NextDouble() * 900; // $100-$1000
            }
            else // 5% large transactions
            {
                return 1000 + random.NextDouble() * 9000; // $1000-$10000
            }
        }

        private static string GenerateTransactionType(Random random)
        {
            var types = new[] { "purchase", "refund", "subscription", "donation", "transfer" };
            return types[random.Next(types.Length)];
        }

        private static string GenerateCustomerTier(Random random)
        {
            return random.NextDouble() switch
            {
                < 0.1 => "platinum",
                < 0.3 => "gold", 
                < 0.6 => "silver",
                _ => "bronze"
            };
        }

        private static double GenerateNormalRandom(Random random)
        {
            // Box-Muller transform for normal distribution
            var u1 = random.NextDouble();
            var u2 = random.NextDouble();
            return Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
        }
    }
}
```

## 🎯 Day 4 Exercises

### Exercise 4.1: Grafana Dashboard Creation with LocalTesting

**Objective**: Build comprehensive monitoring dashboards using the LocalTesting observability stack

#### Prerequisites
- LocalTesting environment running: `dotnet run --project LocalTesting.AppHost` 
- Generate observability data: `curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000`

#### Step 1: Access LocalTesting Grafana
1. **Navigate to Grafana** (http://localhost:18010)
   - Login: admin/admin (first time)
   - **Pre-configured data sources**: Prometheus (localhost:18006), OpenTelemetry

2. **Verify Data Sources**:
   ```bash
   # Test Prometheus connectivity
   curl http://localhost:18006/api/v1/query?query=up

   # Check available metrics
   curl http://localhost:18006/api/v1/label/__name__/values
   ```

#### Step 2: Create LocalTesting Dashboard
Import the following queries that work with LocalTesting metrics:

**Panel 1: LocalTesting Service Health**
```promql
# Services status from LocalTesting
up{job=~"localtesting-webapi|otel-collector|prometheus"}
```

**Panel 2: HTTP Request Rate**
```promql
# API request rate from LocalTesting WebAPI
rate(http_requests_total[5m])
```

**Panel 3: Kafka Message Flow** 
```promql
# Message production from LocalTesting stress tests
rate(kafka_producer_messages_sent_total[5m])
```

**Panel 4: Resource Utilization**
```promql
# Container resources from LocalTesting environment
container_memory_usage_bytes{name=~"localtesting.*|prometheus|grafana"}
```

#### Step 3: Configure LocalTesting Alerts
Set up alerts based on LocalTesting observability patterns:
   - API response time > 200ms
   - Service down (up == 0)
   - High message processing lag
   - Container memory usage > 80%

#### Step 4: Test with Real Data
```bash
# Generate metrics while monitoring dashboard
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 5000

# Watch dashboard update in real-time
# Navigate between Grafana (localhost:18010) and Aspire Dashboard (localhost:18888)
```

💡 **LocalTesting Integration**: This exercise uses the actual LocalTesting observability stack with real metrics from stress testing scenarios.

### Exercise 4.2: Custom Metrics Implementation with LocalTesting

**Objective**: Implement and test custom metrics using the LocalTesting observability infrastructure

#### Step 1: Add Custom Metrics to LocalTesting
Navigate to `LocalTesting/LocalTesting.WebApi` and add business metrics:

```csharp
// Add to ComplexLogicStressTestService.cs or create new MetricsService.cs

using System.Diagnostics.Metrics;

public class BusinessMetricsService
{
    private static readonly Meter BusinessMeter = new("LocalTesting.Business");
    
    // 1. Customer Experience Metrics
    private static readonly Histogram<double> CustomerSatisfactionScore = 
        BusinessMeter.CreateHistogram<double>("customer_satisfaction_score");
    
    // 2. Business Process Metrics  
    private static readonly Counter<long> OrdersProcessedTotal = 
        BusinessMeter.CreateCounter<long>("orders_processed_total");
    
    // 3. Infrastructure Cost Metrics
    private static readonly Gauge<double> InfrastructureCostPerHour = 
        BusinessMeter.CreateGauge<double>("infrastructure_cost_usd_per_hour");
    
    // 4. Team Productivity Metrics
    private static readonly Counter<long> DeploymentFrequency = 
        BusinessMeter.CreateCounter<long>("deployments_total");
        
    public void RecordBusinessMetrics()
    {
        // Called during stress test execution
        CustomerSatisfactionScore.Record(4.2, new("region", "us-west"), new("tier", "premium"));
        OrdersProcessedTotal.Add(1, new("product_type", "subscription"));
        InfrastructureCostPerHour.Set(12.50);
        DeploymentFrequency.Add(1, new("environment", "staging"));
    }
}
```

#### Step 2: Test Metrics with LocalTesting
```bash
# 1. Start LocalTesting to enable metrics collection
dotnet run --project LocalTesting.AppHost

# 2. Generate business metrics data
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000

# 3. Query your custom metrics in Prometheus
curl "http://localhost:18006/api/v1/query?query=customer_satisfaction_score"
curl "http://localhost:18006/api/v1/query?query=orders_processed_total"
```

#### Step 3: Create Business Dashboard
Add panels to your Grafana dashboard for the new metrics:
- Customer satisfaction trends
- Order processing rates
- Infrastructure cost monitoring
- Deployment frequency analysis

💡 **LocalTesting Advantage**: Your metrics are automatically collected and available in the full observability stack without additional configuration!

### Exercise 4.3: Distributed Tracing Analysis with LocalTesting

**Objective**: Use LocalTesting's distributed tracing to debug performance issues in real business flows

#### Step 1: Generate Distributed Traces
```bash
# Start LocalTesting with full observability stack
dotnet run --project LocalTesting.AppHost

# Generate rich tracing data through 8-step business flow
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 2000
```

#### Step 2: Analyze LocalTesting Traces
1. **Access Aspire Dashboard**: http://localhost:18888
2. **Navigate to Traces** section  
3. **Filter by LocalTesting services**:
   - `LocalTesting.WebApi`
   - `ComplexLogicStressTest` operations
   - Business flow step-by-step execution

#### Step 3: Trace Analysis Patterns
Use LocalTesting's actual business flows to identify:

**Performance Patterns:**
- Analyze 8-step stress test execution times
- Find bottlenecks in message production → Flink processing → batch workflows
- Correlate Temporal workflow execution with API response times

**Error Correlation:**
- Trace failures across services during stress testing  
- Identify cascade failures in multi-cluster orchestration
- Debug circuit breaker activations

**Service Dependencies:**
- Understand LocalTesting → Kafka → Flink → Temporal relationships
- Trace token validation through security service
- Follow message lifecycle from API → Queue → Processing

#### Step 4: Performance Optimization
Based on trace analysis:
1. Identify slowest operations in LocalTesting stress tests
2. Correlate with Prometheus metrics for resource usage
3. Use findings to optimize actual LocalTesting performance

💡 **Real-World Value**: These are actual production patterns from LocalTesting's enterprise orchestration - your analysis directly improves the platform!

### Exercise 4.4: LocalTesting Automated Observability Testing

**Objective**: Master LocalTesting's automated observability testing procedures for enterprise validation

#### Step 1: Understanding LocalTesting Observability Testing
LocalTesting includes **comprehensive automated observability testing** that validates:
- Prometheus metrics collection and data availability
- Grafana datasource connectivity  
- OpenTelemetry collector functionality
- Aspire Dashboard observability features
- Real-time message flow monitoring during business flows

#### Step 2: Run Automated Observability Validation
```bash
# Execute comprehensive observability testing
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000

# Watch for observability validation output:
# 🔍 OBSERVABILITY VALIDATION:
# ✅ Prometheus (12/12 services), Grafana (3/3 datasources)
# ✅ OpenTelemetry (HTTP: 18009, gRPC: 4317), Aspire (18888)
# 📊 HTTP Request Activity: +3,276 requests during test execution
# 🎯 Message Flow Monitoring: Successfully tracked throughout test execution
```

#### Step 3: Analyze Observability Delta Analysis
LocalTesting automatically captures:
- **Baseline metrics** before test execution
- **Real-time monitoring** during 8-step business flow
- **Delta analysis** comparing initial vs final observability state
- **Service stability tracking** throughout test duration

#### Step 4: Implement Custom Observability Tests
Create your own observability validation:

```bash
# Test specific Prometheus queries
curl "http://localhost:18006/api/v1/query?query=up{job='localtesting-webapi'}"

# Validate Grafana health
curl -u admin:admin http://localhost:18010/api/health

# Test OpenTelemetry endpoints
curl http://localhost:18009/v1/metrics

# Verify service discovery
curl http://localhost:18006/api/v1/targets
```

#### Step 5: Enterprise Monitoring Patterns
Study LocalTesting's implementation of:
- **Four Golden Signals** monitoring during stress tests
- **SLI/SLO tracking** with real business metrics
- **Service mesh observability** across distributed components
- **Circuit breaker monitoring** with automatic recovery

💡 **Enterprise Learning**: This exercise teaches you actual enterprise observability patterns used in LocalTesting's production-grade architecture!

### Exercise 4.5: Alert Configuration with LocalTesting

**Objective**: Set up production-ready alerting for LocalTesting observability stack

#### Step 1: LocalTesting Alert Scenarios
Create alerts based on LocalTesting's actual metrics and business flows:

1. **Prometheus Alert Rules** for LocalTesting (`alert_rules.yml`):
   ```yaml
   groups:
   - name: localtesting_alerts
     rules:
     - alert: LocalTestingAPIHighLatency
       expr: histogram_quantile(0.99, http_request_duration_seconds{job="localtesting-webapi"}) > 0.2
       for: 5m
       labels:
         severity: warning
       annotations:
         summary: "LocalTesting API high latency detected"
         description: "P99 latency is {{ $value }}ms for LocalTesting API"
   
     - alert: LocalTestingServiceDown  
       expr: up{job=~"localtesting-webapi|otel-collector"} == 0
       for: 30s
       labels:
         severity: critical
       annotations:
         summary: "LocalTesting service is down"
         description: "Service {{ $labels.job }} is not responding"
         
     - alert: LocalTestingStressTestFailures
       expr: rate(complex_logic_stress_test_failures_total[5m]) > 0.1
       for: 2m
       labels:
         severity: warning
       annotations:
         summary: "High stress test failure rate in LocalTesting"
   ```

#### Step 2: Test Alerts with LocalTesting
```bash
# Generate alert conditions during stress testing
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 10000  # High load

# Monitor alerts in Prometheus: http://localhost:18006/alerts
# Check alert manager: http://localhost:18008 (if configured)
```

#### Step 3: Business Flow Alerts
Set up alerts for LocalTesting's 8-step business flows:
- Step execution timeout (>30s per step)
- Workflow orchestration failures
- Message processing lag in Kafka
- Flink job failures or restarts

💡 **Production Pattern**: These alerts are based on actual LocalTesting enterprise patterns and can be adapted for real production use!
         severity: critical
       annotations:
         summary: "Error rate above 5%"
   ```

2. **Test Alert Conditions**: Modify the application to trigger alerts

### Exercise 4.6: SLI/SLO Implementation with LocalTesting

**Objective**: Implement Google SRE-style service level objectives using LocalTesting metrics

#### Step 1: Define LocalTesting SLIs/SLOs
Based on LocalTesting's enterprise requirements:

```csharp
// LocalTesting Service Level Objectives
public class LocalTestingSLOs
{
    // SLI: API Availability (successful requests / total requests)
    public static readonly double AvailabilitySLO = 99.9; // 99.9%
    
    // SLI: Stress Test P99 Latency 
    public static readonly double StressTestLatencySLO = 200; // 200ms
    
    // SLI: Business Flow Success Rate
    public static readonly double BusinessFlowSuccessSLO = 99.5; // 99.5%
    
    // SLI: Observability Stack Uptime
    public static readonly double ObservabilityStackSLO = 99.99; // 99.99%
    
    // Error Budget: 1 - SLO = acceptable failure rate
    public static readonly double ErrorBudget = 1 - (AvailabilitySLO / 100);
}
```

#### Step 2: Measure SLIs with LocalTesting
```promql
# LocalTesting API Availability SLI
(
  sum(rate(http_requests_total{job="localtesting-webapi",status=~"2.."}[5m])) /
  sum(rate(http_requests_total{job="localtesting-webapi"}[5m]))
) * 100

# Business Flow Success Rate SLI  
(
  sum(rate(complex_logic_stress_test_success_total[5m])) /
  sum(rate(complex_logic_stress_test_total[5m]))
) * 100

# Observability Stack Uptime SLI
avg(up{job=~"prometheus|grafana|otel-collector"}) * 100
```

#### Step 3: Create SLO Dashboard
Build Grafana dashboard panels for LocalTesting SLO tracking:
- Current SLI vs SLO targets
- Error budget consumption over time  
- SLO compliance trends
- Alert status for SLO violations

#### Step 4: Test SLO Scenarios
```bash
# Test SLO compliance during normal operations
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000

# Test SLO behavior under stress
curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 20000

# Analyze SLO impact in Grafana dashboards
```

💡 **Enterprise SRE**: This exercise teaches actual SLI/SLO implementation using LocalTesting's production-grade observability stack!

## 📊 Expected Observability Results

After completing Day 4 with LocalTesting integration, you should have:

### LocalTesting Observability Mastery
- **Complete observability stack** running with real enterprise components
- **Hands-on experience** with Grafana, Prometheus, OpenTelemetry using LocalTesting
- **Real business metrics** from 8-step stress test scenarios
- **Automated observability testing** knowledge from LocalTesting procedures

### Grafana Dashboards
- **LocalTesting Golden Signals Dashboard**: Real-time monitoring of API latency, traffic, errors, saturation
- **Business Flow Dashboard**: 8-step stress test execution monitoring with message flow tracking
- **Infrastructure Dashboard**: Flink cluster, Kafka, Temporal, Redis monitoring from LocalTesting stack
- **SLI/SLO Dashboard**: LocalTesting service reliability tracking and compliance

### Prometheus Metrics
- **Application Metrics**: 50+ custom metrics from the showcase application
- **Infrastructure Metrics**: System and container resource usage
- **Business Metrics**: Revenue, transaction values, customer tiers
- **Alert Rules**: Production-ready alerting configuration

### Distributed Tracing
- **End-to-end Traces**: Complete request flow visualization
- **Performance Analysis**: Bottleneck identification and optimization opportunities
- **Error Correlation**: Cross-service error analysis and root cause identification

---

## 🚀 Get Started with Exercises

All Day 4 exercises include complete working solutions using the LocalTesting observability stack:

### Exercise Overview and Links

**[→ Complete Exercise Solutions](Exercise-Solutions/README.md)**

- **[Exercise 4.1: Grafana Dashboard Creation](Exercise-Solutions/Exercise41/)** - Build production monitoring dashboards
- **[Exercise 4.2: Custom Metrics Implementation](Exercise-Solutions/Exercise42/)** - Implement business KPI tracking
- **[Exercise 4.3: Distributed Tracing Analysis](Exercise-Solutions/Exercise43/)** - Analyze request flows across services
- **[Exercise 4.4: Automated Observability Testing](Exercise-Solutions/Exercise44/)** - Continuous monitoring validation
- **[Exercise 4.5: Alert Configuration](Exercise-Solutions/Exercise45/)** - Production alerting and escalation
- **[Exercise 4.6: SLI/SLO Implementation](Exercise-Solutions/Exercise46/)** - Enterprise SLA monitoring

### Quick Start Instructions

1. **Start LocalTesting Environment**:
   ```bash
   cd LocalTesting
   dotnet run --project LocalTesting.AppHost
   ```

2. **Generate Observability Data**:
   ```bash
   curl -X POST http://localhost:5000/stress/complex-logic -MessageCount 1000
   ```

3. **Access Monitoring Stack**:
   - **Grafana Dashboard**: http://localhost:18010 (admin/admin)
   - **Prometheus Metrics**: http://localhost:18006
   - **Aspire Dashboard**: http://localhost:18888

**All exercises demonstrate real-world enterprise observability patterns using the complete LocalTesting production environment.**

## 📈 Performance Benchmarks

Your observability stack should achieve:

- **Metrics Ingestion**: 10,000+ metrics/second
- **Trace Processing**: 1,000+ traces/minute  
- **Dashboard Response**: <2 seconds for complex queries
- **Alert Latency**: <30 seconds from trigger to notification
- **Data Retention**: 15 days of detailed metrics

## 🎯 Day 4 Assessment

### Knowledge Check
1. What are the Four Golden Signals and why are they important?
2. How does SLI differ from SLO, and how do you calculate error budgets?
3. What is the difference between metrics, traces, and logs?
4. How do you implement effective alerting without alert fatigue?
5. What are the key components of a distributed tracing system?

### Practical Assessment
Build an observability solution that:
1. Implements all Four Golden Signals for a streaming application
2. Creates comprehensive Grafana dashboards with enterprise patterns
3. Sets up meaningful alerts with proper thresholds
4. Demonstrates distributed tracing across multiple services
5. Tracks business metrics and SLI/SLO compliance

## 🎯 Day 4 Completion Checklist

### LocalTesting Environment
- [ ] **LocalTesting observability stack** successfully running (Grafana, Prometheus, OpenTelemetry)
- [ ] **Automated observability testing** executed with `curl -X POST http://localhost:5000/stress/complex-logic`
- [ ] **All monitoring endpoints accessible**: localhost:18010, localhost:18006, localhost:18888

### Core Observability Skills  
- [ ] Successfully implemented The Four Golden Signals monitoring using LocalTesting metrics
- [ ] Created comprehensive Grafana dashboards using LocalTesting data sources
- [ ] Configured and tested production-ready alerting rules for LocalTesting services
- [ ] Implemented distributed tracing analysis using LocalTesting business flows
- [ ] Built business metrics tracking and SLI/SLO monitoring for LocalTesting scenarios

### Advanced Integration
- [ ] Analyzed performance bottlenecks in LocalTesting 8-step stress tests
- [ ] Validated alert configurations with LocalTesting stress testing scenarios  
- [ ] Used LocalTesting's automated observability validation features
- [ ] Documented observability patterns from LocalTesting enterprise architecture

💡 **LocalTesting Mastery**: You've learned enterprise observability using a real production-grade platform that demonstrates patterns used by major tech companies!

## 📚 Preparation for Day 5

Tomorrow: **Temporal Workflow Orchestration** - Durable execution patterns

**References to review:**
- [Temporal Patterns and Best Practices](https://docs.temporal.io/dev-guide)
- [Microservices Patterns - Saga Orchestration](https://microservices.io/patterns/data/saga.html)

## 🎉 Congratulations!

You've mastered **enterprise-grade observability** that matches production deployments at Netflix, Google, and Uber! You now have:

- ✅ **Complete monitoring stack** with Four Golden Signals implementation
- ✅ **Production dashboards** for operational visibility and business intelligence  
- ✅ **Distributed tracing** for performance analysis and debugging
- ✅ **Intelligent alerting** based on SLI/SLO principles
- ✅ **Business metrics tracking** for data-driven decisions
- ✅ **Performance optimization** insights from comprehensive telemetry

**Tomorrow**: We'll orchestrate complex workflows with Temporal's durable execution engine!

---

**Next**: [Day 5: Temporal Workflow Orchestration →](../Day05-Temporal-Workflows/README.md)
---

## 🗺️ Course Navigation
**[← Day 3: Production Backpressure](../Day03-Production-Backpressure/)** | **[Course Overview](../README.md)** | **[Next: Day 5 - Temporal Workflows →](../Day05-Temporal-Workflows/)**

**Course Progress**: Day 4 of 14 Complete ✅
