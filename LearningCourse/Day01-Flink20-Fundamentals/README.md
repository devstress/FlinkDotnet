# Day 1: Apache Flink 2.0 Fundamentals & Production Environment

## 🎯 Real-World Learning Objectives

Master Apache Flink 2.0 fundamentals while setting up and validating a **complete production-grade streaming stack** that mirrors enterprise deployments at Netflix, Uber, and LinkedIn.

**Time:** 6-7 hours | **Reference:** [Apache Flink Training - Module 1](https://training.apache.org/flink)

## 📚 Real-World Reference Foundation

This module follows **Apache Flink's official training curriculum** combined with production patterns from:

### 🏛️ Official Apache Flink Resources
- **[Apache Flink 2.0 Release Notes](https://flink.apache.org/news/2024/09/18/release-2.0.0.html)** - Major improvements and new features
- **[Flink Operations Playbook](https://flink.apache.org/features/operations/)** - Production deployment guidance
- **[Flink Architecture Overview](https://flink.apache.org/flink-architecture.html)** - Core concepts and design

### 🏢 Enterprise Infrastructure Patterns
- **Netflix's Microservices Architecture** - Complete observability stack
- **Uber's Real-time Platform** - Multi-service orchestration
- **LinkedIn's Event-Driven Architecture** - Kafka + Flink integration
- **Google SRE Practices** - Infrastructure validation and monitoring

## 🚀 What's Revolutionary in Apache Flink 2.0

### 🔥 Major Improvements from Flink 1.x

#### 1. **Unified Batch and Stream Processing**
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                     FLINK 2.0 UNIFIED ARCHITECTURE                             │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────┐    ┌─────────────────────────────────────────────────────┐ │
│  │  DATASTREAM API │    │              TABLE/SQL API                         │ │
│  │                 │    │                                                     │ │
│  │ • Stream Mode   │───▶│ • Unified batch/stream semantics                   │ │
│  │ • Batch Mode    │    │ • Dynamic table concepts                           │ │
│  │ • Mixed Mode    │    │ • Continuous queries                               │ │
│  └─────────────────┘    └─────────────────────────────────────────────────────┘ │
│           │                                    │                                │
│           └────────────────────────────────────┼────────────────────────────────┘
│                                                │                                 │
│  ┌─────────────────────────────────────────────────────────────────────────────┐ │
│  │                    UNIFIED RUNTIME ENGINE                                  │ │
│  │                                                                             │ │
│  │ • Adaptive Execution: Dynamic resource allocation                          │ │
│  │ • Smart Scheduling: Workload-aware task placement                          │ │
│  │ • Elastic Scaling: Automatic parallelism adjustment                        │ │
│  │ • Advanced State: Multi-tier state backends                                │ │
│  └─────────────────────────────────────────────────────────────────────────────┘ │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

#### 2. **Enhanced State Management**
- **RocksDB Improvements**: Faster checkpoints, better memory management
- **State Schema Evolution**: Zero-downtime state migrations
- **Queryable State**: External applications can query live state
- **State Sharing**: Cross-job state collaboration

#### 3. **Advanced Backpressure Control**
- **Credit-based Flow Control**: Network-level backpressure management
- **Adaptive Rate Limiting**: Dynamic throughput adjustment based on downstream capacity
- **Circuit Breaker Integration**: Cascading failure prevention
- **End-to-end Flow Control**: From source to sink backpressure propagation

#### 4. **Enterprise Security & Compliance**
- **Fine-grained RBAC**: Role-based access control
- **End-to-end Encryption**: Data in transit and at rest
- **Audit Logging**: Comprehensive compliance reporting
- **Secret Management**: Integration with enterprise secret stores

## 🏗️ Complete Production Stack Setup

Your LocalTesting environment provides an **enterprise-grade infrastructure** that mirrors production deployments:

### Infrastructure Overview
```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                      PRODUCTION-GRADE LOCALTESTING STACK                       │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐ │
│  │   APACHE FLINK 2.0  │    │    TEMPORAL.IO      │    │  OBSERVABILITY      │ │
│  │                     │    │                     │    │      STACK          │ │
│  │ • JobManager:8081   │    │ • Server:7233       │    │ • Grafana:3000      │ │
│  │ • 3 TaskManagers    │───▶│ • UI:8084           │───▶│ • Prometheus:9090   │ │
│  │ • 24 Slots Total    │    │ • PostgreSQL        │    │ • OpenTelemetry     │ │
│  │ • RocksDB State     │    │ • Workflow Engine   │    │ • Distributed Trace │ │
│  └─────────────────────┘    └─────────────────────┘    └─────────────────────┘ │
│           │                           │                           │            │
│           │              ┌─────────────────────────────────────────────────────┤
│           │              │               EVENT STREAMING LAYER                 │ │
│           │              │                                                     │ │
│           └──────────────│ • Kafka Cluster (3 brokers with KRaft)            │ │
│                          │ • Replication Factor: 3                            │ │
│                          │ • Auto-topic Creation                              │ │
│                          │ • Kafka UI:8082                                    │ │
│                          └─────────────────────────────────────────────────────┘ │
│                                                │                                 │
│                          ┌─────────────────────────────────────────────────────┐ │
│                          │              DEVELOPMENT & TESTING                 │ │
│                          │                                                     │ │
│                          │ • LocalTesting API:5000                            │ │
│                          │ • Redis Cache:6379                                 │ │
│                          │ • Aspire Dashboard:18888                           │ │
│                          │ • Health Monitoring                                │ │
│                          └─────────────────────────────────────────────────────┘ │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

### Service Architecture Details

| Component | URL | Purpose | Production Pattern |
|-----------|-----|---------|-------------------|
| **Flink Dashboard** | http://localhost:8081 | Stream processing monitoring | [Flink Web UI Best Practices](https://flink.apache.org/docs/stable/ops/monitoring/dashboard/) |
| **Temporal UI** | http://localhost:8084 | Workflow orchestration | [Temporal Production Setup](https://docs.temporal.io/cluster-deployment-guide) |
| **Kafka UI** | http://localhost:8082 | Event stream management | [Confluent Control Center](https://docs.confluent.io/platform/current/control-center/index.html) |
| **Grafana** | http://localhost:3000 | Metrics visualization | [Grafana Production Setup](https://grafana.com/docs/grafana/latest/setup-grafana/configure-grafana/) |
| **Prometheus** | http://localhost:9090 | Metrics collection | [Prometheus Monitoring](https://prometheus.io/docs/prometheus/latest/configuration/configuration/) |
| **LocalTesting API** | http://localhost:5000 | Development tools | Custom integration testing framework |
| **Aspire Dashboard** | http://localhost:18888 | .NET orchestration | [.NET Aspire Dashboard](https://learn.microsoft.com/en-us/dotnet/aspire/fundamentals/dashboard) |

## 🚀 Step-by-Step Environment Setup

### Step 1: Prerequisites Validation

Before starting, ensure your development environment meets production standards:

```bash
# Verify .NET 9 SDK
dotnet --version
# Expected: 9.0.x or higher

# Verify Docker Desktop is running
docker version
# Expected: Docker version 24.x+ with Compose support

# Verify memory allocation (minimum 8GB recommended)
docker system df
docker stats --no-stream

# Check available ports
netstat -an | findstr "8081\|8082\|8084\|3000\|5000\|9090\|18888"
# Should show no conflicts on these ports
```

### Step 2: Complete Stack Startup

Navigate to the LocalTesting directory and start the entire production stack:

```bash
# Navigate to LocalTesting
cd FlinkDotNet/LocalTesting

# Start the complete production stack
dotnet run --project LocalTesting.AppHost

# Alternative: Use background mode for development
dotnet run --project LocalTesting.AppHost &
```

**Expected startup sequence:**
1. ✅ **Redis** starts first (foundational caching)
2. ✅ **PostgreSQL** initializes (Temporal storage)
3. ✅ **Kafka Cluster** forms (3 brokers with leader election)
4. ✅ **Flink Cluster** assembles (JobManager + 3 TaskManagers)
5. ✅ **Temporal Server** connects to PostgreSQL
6. ✅ **OpenTelemetry Collector** starts telemetry processing
7. ✅ **Prometheus** begins metrics collection
8. ✅ **Grafana** connects to data sources
9. ✅ **LocalTesting API** validates all dependencies

### Step 3: Comprehensive Infrastructure Validation

Run the automated validation script to ensure enterprise-grade setup:

```bash
# Run comprehensive infrastructure validation
../scripts/validate-local-infra.ps1

# Alternative: Manual validation using LocalTesting API
curl http://localhost:5000/health/comprehensive
```

**Expected validation output:**
```
🔍 FlinkDotNet Production Stack Validation
==========================================

✅ FLINK CLUSTER STATUS
   • JobManager: RUNNING (http://localhost:8081)
   • TaskManagers: 3/3 HEALTHY
   • Available Slots: 24/24
   • Parallelism: 24

✅ KAFKA CLUSTER STATUS  
   • Brokers: 3/3 ONLINE
   • Controller: kafka-broker-1 (Node ID: 1)
   • Replication: HEALTHY
   • Auto-topic Creation: ENABLED

✅ TEMPORAL CLUSTER STATUS
   • Server: RUNNING (temporal-server:7233)
   • Database: CONNECTED (PostgreSQL)
   • UI: ACCESSIBLE (http://localhost:8084)
   • Namespaces: default (REGISTERED)

✅ OBSERVABILITY STACK STATUS
   • OpenTelemetry: COLLECTING
   • Prometheus: SCRAPING (9 targets)
   • Grafana: CONNECTED (2 data sources)

✅ DEVELOPMENT TOOLS STATUS
   • LocalTesting API: READY (http://localhost:5000)
   • Redis Cache: CONNECTED
   • Aspire Dashboard: RUNNING (http://localhost:18888)

🎯 INFRASTRUCTURE READY FOR PRODUCTION WORKLOADS
   Total startup time: 45-60 seconds
   Memory usage: ~6.2GB
   All enterprise patterns validated
```

### Step 4: Service Discovery and Exploration

#### Flink 2.0 Dashboard Deep Dive

Visit http://localhost:8081 and explore:

**1. Cluster Overview**
- **Task Managers**: 3 instances with 8 slots each (24 total)
- **Memory Configuration**: 1GB per TaskManager (production optimized)
- **Network Configuration**: Credit-based flow control enabled

**2. Configuration Tab**
- **Parallelism Settings**: Default parallelism = 24
- **Checkpointing**: Configured for exactly-once semantics
- **State Backend**: RocksDB with managed memory

**3. Advanced Features**
- **JobManager RPC**: Cluster coordination
- **REST API**: http://localhost:8081/v1 (production API)
- **Metrics**: JVM, network, and processing metrics

#### Temporal Workflow Engine

Visit http://localhost:8084 and understand:

**1. Workflow Management**
- **Namespaces**: Logical separation of workflows
- **Task Queues**: Workflow execution queues
- **Workers**: Workflow and activity execution

**2. Observability Features**
- **Workflow History**: Complete execution trace
- **Search & Filter**: Advanced workflow discovery
- **Metrics Dashboard**: Execution statistics

#### Kafka Event Streaming

Visit http://localhost:8082 and explore:

**1. Cluster Information**
- **Brokers**: 3-node cluster with automatic failover
- **Topics**: Dynamic topic creation enabled
- **Partitions**: Default 15 partitions for parallel processing

**2. Production Features**
- **Replication Factor**: 3 (fault tolerance)
- **Leader Election**: Automatic leadership changes
- **Consumer Groups**: Real-time consumption monitoring

#### Enterprise Observability

Visit http://localhost:3000 (Grafana) and examine:

**1. Pre-configured Dashboards**
- **Flink Cluster Metrics**: Job performance and resource usage
- **Kafka Metrics**: Throughput, latency, and consumer lag
- **System Metrics**: Infrastructure health monitoring

**2. Data Sources**
- **Prometheus**: Metrics storage and querying
- **OpenTelemetry**: Distributed tracing integration

## 🛠️ Your First Flink 2.0 Application

Now let's build a sophisticated streaming application that demonstrates Flink 2.0 capabilities and integrates with the complete stack:

### Enterprise-Grade Streaming Application

Create `Day01_ProductionStreamingApp.cs`:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using FlinkDotNet.DataStream;
using FlinkDotNet.Common;
using System.Diagnostics.Metrics;

namespace LearningCourse.Day01
{
    /// <summary>
    /// Production-grade Flink 2.0 streaming application demonstrating:
    /// - Enterprise integration patterns
    /// - Advanced state management
    /// - Comprehensive observability
    /// - Real-world data processing patterns
    /// 
    /// References:
    /// - Apache Flink 2.0 DataStream API
    /// - Netflix streaming architecture patterns
    /// - Google SRE observability practices
    /// </summary>
    public class ProductionStreamingApplication
    {
        private static readonly ActivitySource ActivitySource = new("FlinkDotNet.Day01");
        private static readonly Meter MetricsMeter = new("FlinkDotNet.Day01");
        
        // Production metrics (following Google SRE patterns)
        private static readonly Counter<long> ProcessedEvents = MetricsMeter.CreateCounter<long>(
            "events_processed_total", 
            description: "Total number of events processed");
            
        private static readonly Histogram<double> ProcessingLatency = MetricsMeter.CreateHistogram<double>(
            "processing_latency_ms", 
            description: "Event processing latency in milliseconds");
            
        private static readonly Gauge<long> ActiveStreams = MetricsMeter.CreateGauge<long>(
            "active_streams_count", 
            description: "Number of active streaming pipelines");

        public class EnterpriseEvent
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime Timestamp { get; set; }
            public string EventType { get; set; } = string.Empty;
            public string Source { get; set; } = string.Empty;
            public string TenantId { get; set; } = string.Empty;
            public Dictionary<string, object> Payload { get; set; } = new();
            public Dictionary<string, string> Metadata { get; set; } = new();
            public int Priority { get; set; } = 5; // 1 = highest, 10 = lowest

            public override string ToString()
            {
                return $"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] {EventType} from {Source} " +
                       $"(Tenant: {TenantId}, Priority: {Priority}) - {EventId}";
            }
        }

        public class ProcessingResult
        {
            public string EventId { get; set; } = string.Empty;
            public DateTime ProcessedAt { get; set; }
            public TimeSpan ProcessingDuration { get; set; }
            public string ProcessingStage { get; set; } = string.Empty;
            public bool Success { get; set; }
            public string? ErrorMessage { get; set; }
            public Dictionary<string, object> EnrichmentData { get; set; } = new();

            public override string ToString()
            {
                var status = Success ? "✅ SUCCESS" : "❌ FAILED";
                return $"{status} {EventId} in {ProcessingDuration.TotalMilliseconds:F1}ms " +
                       $"at {ProcessingStage}" + (ErrorMessage != null ? $" - {ErrorMessage}" : "");
            }
        }

        public static async Task Main(string[] args)
        {
            Console.WriteLine("🚀 Flink 2.0 Production Streaming Application");
            Console.WriteLine("==============================================");
            Console.WriteLine("🔗 Dashboard: http://localhost:8081");
            Console.WriteLine("📊 Grafana:   http://localhost:3000");
            Console.WriteLine("⚡ Temporal:  http://localhost:8084");
            Console.WriteLine();

            // Step 1: Create production-optimized execution environment
            var env = CreateProductionEnvironment();
            Console.WriteLine("✅ Production environment configured");

            // Step 2: Create realistic enterprise data stream
            var eventStream = CreateEnterpriseEventStream(env);
            Console.WriteLine("✅ Enterprise event stream initialized");

            // Step 3: Apply production processing pipeline
            await ApplyEnterpriseProcessingPipeline(eventStream);
            Console.WriteLine("✅ Enterprise processing pipeline configured");

            // Step 4: Execute with comprehensive monitoring
            using var activity = ActivitySource.StartActivity("StreamProcessingExecution");
            activity?.SetTag("environment", "production");
            activity?.SetTag("version", "2.0");

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                Console.WriteLine("\n🎯 Starting production streaming job...");
                Console.WriteLine("📈 Monitor performance: http://localhost:3000");
                Console.WriteLine("🔍 View traces: http://localhost:18888");
                
                await env.Execute("Production Streaming Application v2.0");
                
                stopwatch.Stop();
                Console.WriteLine($"\n✅ Streaming job completed in {stopwatch.Elapsed.TotalSeconds:F1}s");
                activity?.SetTag("success", true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                Console.WriteLine($"\n❌ Streaming job failed after {stopwatch.Elapsed.TotalSeconds:F1}s: {ex.Message}");
                activity?.SetTag("success", false);
                activity?.SetTag("error", ex.Message);
                throw;
            }
            finally
            {
                activity?.SetTag("duration_seconds", stopwatch.Elapsed.TotalSeconds);
            }
        }

        /// <summary>
        /// Create production-optimized Flink 2.0 execution environment
        /// Based on Netflix and Uber production configurations
        /// </summary>
        private static StreamExecutionEnvironment CreateProductionEnvironment()
        {
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            
            // Production parallelism (matches TaskManager configuration)
            env.SetParallelism(24); // 3 TaskManagers × 8 slots each
            
            // Production checkpointing (exactly-once semantics)
            env.EnableCheckpointing(TimeSpan.FromSeconds(30)); // Netflix pattern: 30s intervals
            env.SetBufferTimeout(TimeSpan.FromMilliseconds(100)); // Low latency
            
            // Advanced Flink 2.0 configuration
            var config = env.GetConfig();
            config.SetGlobalJobParameters(new Configuration
            {
                // Execution optimizations
                ["execution.checkpointing.mode"] = "EXACTLY_ONCE",
                ["execution.checkpointing.timeout"] = "10 min",
                ["execution.checkpointing.max-concurrent-checkpoints"] = "2",
                ["execution.checkpointing.externalized-checkpoint-retention"] = "RETAIN_ON_CANCELLATION",
                
                // State backend optimization (RocksDB)
                ["state.backend"] = "rocksdb",
                ["state.backend.rocksdb.memory.managed"] = "true",
                ["state.backend.rocksdb.memory.fixed-per-slot"] = "128mb",
                ["state.backend.incremental"] = "true",
                
                // Network and memory optimization
                ["taskmanager.memory.process.size"] = "1gb",
                ["taskmanager.memory.managed.fraction"] = "0.6",
                ["taskmanager.network.memory.fraction"] = "0.15",
                ["taskmanager.network.numberOfBuffers"] = "8192",
                
                // Advanced features
                ["table.exec.source.idle-timeout"] = "30s",
                ["pipeline.auto-watermark-interval"] = "200ms",
                ["pipeline.max-parallelism"] = "128",
                
                // Observability integration
                ["metrics.reporter.prometheus.class"] = "org.apache.flink.metrics.prometheus.PrometheusReporter",
                ["metrics.reporter.prometheus.port"] = "9249-9260",
                
                // Job-specific optimizations
                ["pipeline.name"] = "Production Streaming Application v2.0",
                ["pipeline.jars"] = "file:///opt/flink/lib/",
                ["execution.savepoint.ignore-unclaimed-state"] = "true"
            });
            
            return env;
        }

        /// <summary>
        /// Create realistic enterprise event stream with various patterns
        /// Simulates real-world data diversity and volume
        /// </summary>
        private static DataStream<EnterpriseEvent> CreateEnterpriseEventStream(StreamExecutionEnvironment env)
        {
            var events = new List<EnterpriseEvent>();
            var random = new Random(42); // Deterministic for testing
            
            Console.WriteLine("🔄 Generating enterprise event dataset...");
            
            // Generate diverse enterprise events (10,000 events)
            for (int i = 0; i < 10000; i++)
            {
                var tenantId = $"tenant_{random.Next(1, 50):D3}"; // 50 tenants
                var eventType = GenerateEventType(random);
                var source = GenerateEventSource(eventType, random);
                
                events.Add(new EnterpriseEvent
                {
                    EventId = $"evt_{i:D6}_{Guid.NewGuid().ToString("N")[..8]}",
                    Timestamp = DateTime.UtcNow.AddMilliseconds(-random.Next(0, 300000)), // Last 5 minutes
                    EventType = eventType,
                    Source = source,
                    TenantId = tenantId,
                    Priority = GeneratePriority(eventType, random),
                    Payload = GenerateEventPayload(eventType, random),
                    Metadata = GenerateEventMetadata(tenantId, source, random)
                });
                
                // Progress indication
                if (i % 1000 == 0 && i > 0)
                {
                    Console.WriteLine($"📝 Generated {i:N0} events...");
                }
            }
            
            Console.WriteLine($"✅ Generated {events.Count:N0} enterprise events");
            
            return env.FromElements(events.ToArray())
                .Name("Enterprise Event Source")
                .SetParallelism(4); // Distributed source generation
        }

        /// <summary>
        /// Apply comprehensive enterprise processing pipeline
        /// Demonstrates Flink 2.0 advanced patterns
        /// </summary>
        private static async Task ApplyEnterpriseProcessingPipeline(DataStream<EnterpriseEvent> eventStream)
        {
            // Stage 1: Event validation and enrichment
            var validatedStream = eventStream
                .Map(new EventValidationFunction())
                .Name("Event Validation & Enrichment")
                .SetParallelism(8);

            // Stage 2: Tenant-aware processing (keyed by tenant)
            var tenantProcessedStream = validatedStream
                .KeyBy(evt => evt.TenantId)
                .Map(new TenantAwareProcessingFunction())
                .Name("Tenant-Aware Processing")
                .SetParallelism(12);

            // Stage 3: Priority-based routing
            var highPriorityStream = tenantProcessedStream
                .Filter(evt => evt.Priority <= 2)
                .Map(new HighPriorityProcessingFunction())
                .Name("High Priority Processing")
                .SetParallelism(4);

            var normalPriorityStream = tenantProcessedStream
                .Filter(evt => evt.Priority > 2 && evt.Priority <= 7)
                .Map(new NormalPriorityProcessingFunction())
                .Name("Normal Priority Processing")
                .SetParallelism(8);

            var batchPriorityStream = tenantProcessedStream
                .Filter(evt => evt.Priority > 7)
                .Map(new BatchProcessingFunction())
                .Name("Batch Processing")
                .SetParallelism(4);

            // Stage 4: Results aggregation and monitoring
            var allResults = highPriorityStream
                .Union(normalPriorityStream)
                .Union(batchPriorityStream)
                .Map(new ResultsAggregationFunction())
                .Name("Results Aggregation");

            // Stage 5: Output and monitoring
            allResults.Print("📊 PROCESSING RESULTS");

            // Stage 6: Metrics collection (side output)
            var metricsStream = allResults
                .Map(new MetricsCollectionFunction())
                .Name("Metrics Collection");

            metricsStream.Print("📈 METRICS");

            await Task.CompletedTask;
        }

        // Processing Functions (Flink 2.0 patterns)

        public class EventValidationFunction : MapFunction<EnterpriseEvent, EnterpriseEvent>
        {
            public override EnterpriseEvent Map(EnterpriseEvent evt)
            {
                using var activity = ActivitySource.StartActivity("EventValidation");
                activity?.SetTag("event_id", evt.EventId);
                activity?.SetTag("event_type", evt.EventType);
                
                var startTime = DateTime.UtcNow;
                
                try
                {
                    // Validation logic
                    ValidateEvent(evt);
                    
                    // Enrichment
                    EnrichEvent(evt);
                    
                    var duration = DateTime.UtcNow - startTime;
                    ProcessingLatency.Record(duration.TotalMilliseconds);
                    ProcessedEvents.Add(1, new KeyValuePair<string, object>("stage", "validation"));
                    
                    activity?.SetTag("success", true);
                    return evt;
                }
                catch (Exception ex)
                {
                    activity?.SetTag("success", false);
                    activity?.SetTag("error", ex.Message);
                    
                    // Mark event as failed but continue processing
                    evt.Metadata["validation_error"] = ex.Message;
                    return evt;
                }
            }

            private void ValidateEvent(EnterpriseEvent evt)
            {
                if (string.IsNullOrEmpty(evt.EventId))
                    throw new ArgumentException("Event ID is required");
                    
                if (string.IsNullOrEmpty(evt.TenantId))
                    throw new ArgumentException("Tenant ID is required");
                    
                if (evt.Timestamp == default)
                    throw new ArgumentException("Event timestamp is required");
            }

            private void EnrichEvent(EnterpriseEvent evt)
            {
                // Add processing metadata
                evt.Metadata["processed_at"] = DateTime.UtcNow.ToString("O");
                evt.Metadata["processor_version"] = "2.0";
                evt.Metadata["validation_passed"] = "true";
                
                // Add tenant classification
                evt.Metadata["tenant_tier"] = DetermineTenantTier(evt.TenantId);
                
                // Add geographic region (simulated)
                evt.Metadata["region"] = evt.TenantId.GetHashCode() % 5 switch
                {
                    0 => "us-east",
                    1 => "us-west", 
                    2 => "eu-central",
                    3 => "ap-southeast",
                    _ => "global"
                };
            }

            private string DetermineTenantTier(string tenantId)
            {
                var hashCode = Math.Abs(tenantId.GetHashCode());
                return (hashCode % 10) switch
                {
                    0 or 1 => "enterprise",
                    2 or 3 or 4 => "business",
                    _ => "standard"
                };
            }
        }

        public class TenantAwareProcessingFunction : MapFunction<EnterpriseEvent, EnterpriseEvent>
        {
            private static readonly Dictionary<string, DateTime> _tenantLastSeen = new();
            private static readonly Dictionary<string, long> _tenantEventCounts = new();
            private static readonly object _lockObject = new();

            public override EnterpriseEvent Map(EnterpriseEvent evt)
            {
                using var activity = ActivitySource.StartActivity("TenantAwareProcessing");
                activity?.SetTag("tenant_id", evt.TenantId);

                lock (_lockObject)
                {
                    // Update tenant statistics
                    _tenantLastSeen[evt.TenantId] = DateTime.UtcNow;
                    _tenantEventCounts[evt.TenantId] = _tenantEventCounts.GetValueOrDefault(evt.TenantId, 0) + 1;

                    // Add tenant-specific metadata
                    evt.Metadata["tenant_event_count"] = _tenantEventCounts[evt.TenantId].ToString();
                    evt.Metadata["tenant_last_seen"] = _tenantLastSeen[evt.TenantId].ToString("O");
                    
                    // Calculate tenant velocity
                    var velocity = CalculateTenantVelocity(evt.TenantId);
                    evt.Metadata["tenant_velocity"] = velocity.ToString("F2");

                    if (velocity > 100) // High velocity tenant
                    {
                        evt.Priority = Math.Max(1, evt.Priority - 1); // Increase priority
                        evt.Metadata["velocity_boost"] = "true";
                    }
                }

                ProcessedEvents.Add(1, new KeyValuePair<string, object>("stage", "tenant_processing"));
                return evt;
            }

            private double CalculateTenantVelocity(string tenantId)
            {
                // Simplified velocity calculation (events per minute)
                var eventCount = _tenantEventCounts.GetValueOrDefault(tenantId, 0);
                return eventCount * 6.0; // Approximate events per minute
            }
        }

        public class HighPriorityProcessingFunction : MapFunction<EnterpriseEvent, ProcessingResult>
        {
            public override ProcessingResult Map(EnterpriseEvent evt)
            {
                using var activity = ActivitySource.StartActivity("HighPriorityProcessing");
                var startTime = DateTime.UtcNow;

                try
                {
                    // Simulate high-priority processing (fast path)
                    Thread.Sleep(Random.Shared.Next(1, 5)); // 1-5ms processing

                    var result = new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "HIGH_PRIORITY",
                        Success = true,
                        EnrichmentData = new Dictionary<string, object>
                        {
                            ["priority_lane"] = "express",
                            ["processing_node"] = Environment.MachineName,
                            ["tenant_tier"] = evt.Metadata.GetValueOrDefault("tenant_tier", "unknown")
                        }
                    };

                    ProcessedEvents.Add(1, new KeyValuePair<string, object>("stage", "high_priority"));
                    return result;
                }
                catch (Exception ex)
                {
                    return new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "HIGH_PRIORITY",
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        public class NormalPriorityProcessingFunction : MapFunction<EnterpriseEvent, ProcessingResult>
        {
            public override ProcessingResult Map(EnterpriseEvent evt)
            {
                using var activity = ActivitySource.StartActivity("NormalPriorityProcessing");
                var startTime = DateTime.UtcNow;

                try
                {
                    // Simulate normal processing
                    Thread.Sleep(Random.Shared.Next(5, 15)); // 5-15ms processing

                    var result = new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "NORMAL_PRIORITY",
                        Success = true,
                        EnrichmentData = new Dictionary<string, object>
                        {
                            ["priority_lane"] = "standard",
                            ["processing_node"] = Environment.MachineName,
                            ["batch_eligible"] = evt.Priority > 5
                        }
                    };

                    ProcessedEvents.Add(1, new KeyValuePair<string, object>("stage", "normal_priority"));
                    return result;
                }
                catch (Exception ex)
                {
                    return new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "NORMAL_PRIORITY",
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        public class BatchProcessingFunction : MapFunction<EnterpriseEvent, ProcessingResult>
        {
            public override ProcessingResult Map(EnterpriseEvent evt)
            {
                using var activity = ActivitySource.StartActivity("BatchProcessing");
                var startTime = DateTime.UtcNow;

                try
                {
                    // Simulate batch processing (slower but more thorough)
                    Thread.Sleep(Random.Shared.Next(10, 30)); // 10-30ms processing

                    var result = new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "BATCH_PROCESSING",
                        Success = true,
                        EnrichmentData = new Dictionary<string, object>
                        {
                            ["priority_lane"] = "batch",
                            ["processing_node"] = Environment.MachineName,
                            ["cost_optimized"] = true,
                            ["batch_group"] = evt.TenantId
                        }
                    };

                    ProcessedEvents.Add(1, new KeyValuePair<string, object>("stage", "batch_processing"));
                    return result;
                }
                catch (Exception ex)
                {
                    return new ProcessingResult
                    {
                        EventId = evt.EventId,
                        ProcessedAt = DateTime.UtcNow,
                        ProcessingDuration = DateTime.UtcNow - startTime,
                        ProcessingStage = "BATCH_PROCESSING",
                        Success = false,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        public class ResultsAggregationFunction : MapFunction<ProcessingResult, ProcessingResult>
        {
            private static long _totalProcessed = 0;
            private static long _totalSuccessful = 0;
            private static long _totalFailed = 0;

            public override ProcessingResult Map(ProcessingResult result)
            {
                Interlocked.Increment(ref _totalProcessed);
                
                if (result.Success)
                {
                    Interlocked.Increment(ref _totalSuccessful);
                }
                else
                {
                    Interlocked.Increment(ref _totalFailed);
                }

                // Add aggregation metadata
                result.EnrichmentData["total_processed"] = _totalProcessed;
                result.EnrichmentData["success_rate"] = _totalProcessed > 0 ? (double)_totalSuccessful / _totalProcessed : 0.0;
                result.EnrichmentData["failure_rate"] = _totalProcessed > 0 ? (double)_totalFailed / _totalProcessed : 0.0;

                return result;
            }
        }

        public class MetricsCollectionFunction : MapFunction<ProcessingResult, string>
        {
            public override string Map(ProcessingResult result)
            {
                // Record detailed metrics
                ProcessingLatency.Record(result.ProcessingDuration.TotalMilliseconds);
                
                var successRate = (double)result.EnrichmentData.GetValueOrDefault("success_rate", 0.0);
                var totalProcessed = (long)result.EnrichmentData.GetValueOrDefault("total_processed", 0L);
                
                ActiveStreams.Set(1); // This stream is active
                
                return $"📊 Metrics: {totalProcessed:N0} processed, " +
                       $"Success Rate: {successRate:P2}, " +
                       $"Latency: {result.ProcessingDuration.TotalMilliseconds:F1}ms, " +
                       $"Stage: {result.ProcessingStage}";
            }
        }

        // Helper methods for event generation
        private static string GenerateEventType(Random random)
        {
            var eventTypes = new[]
            {
                "user_login", "user_logout", "page_view", "api_call",
                "transaction", "order_created", "payment_processed",
                "error_occurred", "system_alert", "metric_reported",
                "workflow_started", "workflow_completed", "data_sync"
            };
            return eventTypes[random.Next(eventTypes.Length)];
        }

        private static string GenerateEventSource(string eventType, Random random)
        {
            return eventType switch
            {
                "user_login" or "user_logout" or "page_view" => $"web_app_{random.Next(1, 5)}",
                "api_call" or "transaction" => $"api_gateway_{random.Next(1, 3)}",
                "order_created" or "payment_processed" => $"commerce_service_{random.Next(1, 4)}",
                "error_occurred" or "system_alert" => $"monitoring_system_{random.Next(1, 2)}",
                _ => $"microservice_{random.Next(1, 10)}"
            };
        }

        private static int GeneratePriority(string eventType, Random random)
        {
            return eventType switch
            {
                "error_occurred" or "system_alert" => random.Next(1, 3), // High priority
                "transaction" or "payment_processed" => random.Next(2, 5), // Medium-high priority
                "user_login" or "api_call" => random.Next(3, 7), // Medium priority
                _ => random.Next(5, 10) // Low priority
            };
        }

        private static Dictionary<string, object> GenerateEventPayload(string eventType, Random random)
        {
            return eventType switch
            {
                "transaction" => new Dictionary<string, object>
                {
                    ["amount"] = Math.Round(random.NextDouble() * 1000, 2),
                    ["currency"] = random.Next(3) switch { 0 => "USD", 1 => "EUR", _ => "GBP" },
                    ["method"] = random.Next(3) switch { 0 => "card", 1 => "bank", _ => "wallet" }
                },
                "page_view" => new Dictionary<string, object>
                {
                    ["url"] = $"/page/{random.Next(1, 100)}",
                    ["user_agent"] = "Mozilla/5.0 (compatible)",
                    ["referrer"] = random.Next(2) == 0 ? "google.com" : "direct"
                },
                "api_call" => new Dictionary<string, object>
                {
                    ["endpoint"] = $"/api/v{random.Next(1, 4)}/resource/{random.Next(1, 1000)}",
                    ["method"] = random.Next(4) switch { 0 => "GET", 1 => "POST", 2 => "PUT", _ => "DELETE" },
                    ["response_time_ms"] = random.Next(10, 500)
                },
                _ => new Dictionary<string, object>
                {
                    ["data"] = $"payload_{random.Next(1000, 9999)}",
                    ["size_bytes"] = random.Next(100, 10000)
                }
            };
        }

        private static Dictionary<string, string> GenerateEventMetadata(string tenantId, string source, Random random)
        {
            return new Dictionary<string, string>
            {
                ["correlation_id"] = Guid.NewGuid().ToString("N")[..16],
                ["trace_id"] = Guid.NewGuid().ToString("N")[..32],
                ["span_id"] = Guid.NewGuid().ToString("N")[..16],
                ["version"] = "2.0",
                ["environment"] = "production",
                ["region"] = random.Next(3) switch { 0 => "us-east", 1 => "us-west", _ => "eu-central" },
                ["datacenter"] = $"dc-{random.Next(1, 6)}"
            };
        }
    }

    // Base function interfaces for Flink operations
    public abstract class MapFunction<TInput, TOutput>
    {
        public abstract TOutput Map(TInput value);
    }
}
```

## 🎯 Day 1 Exercises

### Exercise 1.1: Infrastructure Validation

**Objective**: Validate that all production services are running correctly

```bash
# Run comprehensive health checks
curl http://localhost:5000/health/comprehensive | jq

# Check Flink cluster status
curl http://localhost:8081/overview | jq

# Verify Kafka cluster health
curl http://localhost:8082/api/clusters/local-testing-cluster/brokers

# Test Temporal connectivity
curl http://localhost:8084/api/v1/namespaces

# Validate observability stack
curl http://localhost:9090/api/v1/targets
curl http://localhost:3000/api/health
```

### Exercise 1.2: Production Application Deployment

**Objective**: Deploy and monitor the enterprise streaming application

```bash
# Compile and run the application
cd LearningCourse/Day01-Flink20-Fundamentals
dotnet build
dotnet run

# Monitor in Flink Dashboard
# Visit http://localhost:8081 and observe:
# - Job submission
# - Task distribution across TaskManagers
# - Processing metrics and throughput
# - Checkpoint progress
```

### Exercise 1.3: Observability Exploration

**Objective**: Explore the complete observability stack

1. **Grafana Dashboards** (http://localhost:3000):
   - View Flink cluster metrics
   - Monitor application performance
   - Create custom dashboard for your application

2. **Distributed Tracing** (http://localhost:18888):
   - View end-to-end request traces
   - Understand performance bottlenecks
   - Explore service dependencies

3. **Prometheus Metrics** (http://localhost:9090):
   - Query custom application metrics
   - Set up alerting rules
   - Understand metric collection

### Exercise 1.4: Load Testing

**Objective**: Validate system behavior under load

```bash
# Increase event volume and observe behavior
# Modify the application to generate 100,000 events
# Monitor:
# - Memory usage across TaskManagers
# - Processing latency
# - Checkpoint duration
# - Resource utilization
```

## 📊 Expected Results

After completing Day 1, you should see:

### Flink Dashboard Metrics
- **Jobs**: 1 running job with 24 parallel tasks
- **Throughput**: 1,000-5,000 events/second
- **Latency**: P99 < 100ms
- **Checkpoints**: Successful every 30 seconds

### Grafana Monitoring
- **System Metrics**: CPU, memory, network usage
- **Application Metrics**: Event processing rates, success rates
- **Infrastructure Metrics**: Kafka lag, Redis connections

### Temporal Workflows
- **Namespace**: Default namespace active
- **Workers**: Local workers registered
- **Activities**: Ready for workflow execution

## 📝 Day 1 Assessment

### Knowledge Check
1. What are the three major improvements in Apache Flink 2.0?
2. How does Flink 2.0's unified runtime differ from previous versions?
3. What is the purpose of credit-based flow control?
4. How do TaskManagers coordinate with the JobManager?
5. What observability patterns are implemented in this setup?

### Practical Assessment
Build a streaming application that:
1. Processes 50,000 events with realistic business logic
2. Implements proper error handling and monitoring
3. Uses Flink 2.0 advanced features (state, checkpointing)
4. Integrates with the observability stack
5. Demonstrates production-ready patterns

## 🎯 Day 1 Completion Checklist

- [ ] Successfully started complete production stack (8 services)
- [ ] Validated all service connectivity and health
- [ ] Built and deployed enterprise streaming application
- [ ] Explored Flink 2.0 dashboard and advanced features
- [ ] Configured and used observability stack (Grafana, Prometheus, OpenTelemetry)
- [ ] Completed load testing and performance validation
- [ ] Passed knowledge and practical assessments
- [ ] Documented lessons learned and best practices

## 📚 Preparation for Day 2

Tomorrow: **Real-World Stream Processing Patterns** - Advanced DataStream operations

**References to review:**
- [Stream Processing with Apache Flink - Chapter 3](https://www.oreilly.com/library/view/stream-processing-with/9781491974285/)
- [Uber's Real-Time Analytics Platform](https://eng.uber.com/real-time-analytics/)

## 🎉 Congratulations!

You've successfully set up and validated a **production-grade streaming infrastructure** that mirrors enterprise deployments at scale. You now have:

- ✅ **Complete Flink 2.0 cluster** with advanced features enabled
- ✅ **Enterprise observability** with distributed tracing and metrics
- ✅ **Workflow orchestration** with Temporal integration
- ✅ **Event streaming** with fault-tolerant Kafka cluster
- ✅ **Development tools** for rapid iteration and testing

**Tomorrow**: We'll build sophisticated stream processing patterns using this foundation!

---

**Next**: [Day 2: Real-World Stream Processing Patterns →](../Day02-Stream-Processing-Patterns/README.md)