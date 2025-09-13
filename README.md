# FlinkDotNet

**FlinkDotNet** is a comprehensive .NET framework that enables developers to build and submit streaming jobs to Apache Flink 2.1.0 clusters using a fluent C# API. It provides extensive compatibility with Apache Flink 2.1.0 features including dynamic scaling, adaptive scheduling, reactive mode, and enterprise-scale multi-cluster orchestration.

## Runtime Architecture (IR Runner + Gateway + Temporal)

FlinkDotNet embraces a pragmatic, production‑ready model that separates authoring from execution while keeping .NET first:

- .NET DSL → IR (JobDefinition)
  - Your C# pipeline is compiled to a portable JSON IR that describes sources, operations, sinks and metadata.

- IR Runner Jar (Java/Scala, single prebuilt artifact)
  - A reusable Flink DataStream job jar that reads the IR and builds the topology at runtime (Kafka source/sink, map/filter/window/timer, etc.).
  - Ships with the right connectors/classpath so users don’t need a JVM toolchain.

- Flink Job Gateway (ASP.NET Core)
  - Submits IR to a Flink cluster by ensuring the Runner jar is uploaded and calling Flink REST to run the job, then exposes health/status/metrics endpoints to .NET clients.
  - Centralizes auth, retries, and metrics normalization, so all apps don’t re‑implement the same logic.

- Optional: Temporal Orchestration
  - Durable, auditable workflows to submit/monitor/cancel with retries, idempotency and compensations.
  - This mirrors large‑scale production patterns (e.g., organizations coordinating large numbers of Flink jobs via a workflow engine).

Why this design
- You need a JVM artifact to run inside Flink TaskManagers; the IR Runner jar avoids per‑job compilation and operational drift.
- The Gateway provides a clean boundary and a stable protocol for .NET clients.
- Temporal guarantees that submits eventually succeed or fail cleanly with a durable record.

Alternatives
- Client‑only mode (embed Runner jar in SDK and call Flink REST directly): keeps the same runner, but each app handles auth/policy and jar versioning.
- Flink SQL Gateway: no jar for a subset of pipelines; use when DSL can map cleanly to SQL.

Refer to the docs/ directory for the implementation roadmap and guides.

## 📚 Learning Course

**New to FlinkDotNet?** Visit our comprehensive [`LearningCourse/`](./LearningCourse/) to understand FlinkDotNet from fundamentals to advanced enterprise patterns. The course includes 14 days of hands-on exercises covering:

- **Day 1-2**: Flink 2.1 fundamentals and AI stream processing
- **Day 3-4**: Production backpressure and enterprise observability  
- **Day 5-6**: Temporal workflows and advanced windowing
- **Day 7-8**: Stress testing and exactly-once semantics
- **Day 9-10**: Performance optimization and security compliance
- **Day 11-12**: Disaster recovery and advanced streaming patterns
- **Day 13-14**: Chaos engineering and capstone project

Each day includes complete exercise solutions, production-ready code examples, and real-world implementation patterns. Perfect for developers transitioning to enterprise-scale stream processing with .NET.

# Why Kafka + FlinkDotNet + Temporal? Strategic Architecture Decision Guide

In today's data-driven enterprise landscape, choosing the right messaging and stream processing architecture is critical for scalability, reliability, and maintainability. This section provides a comprehensive analysis of why **Kafka + FlinkDotNet + Temporal** represents the optimal choice for modern real-time data processing at enterprise scale.

## 🏗️ Messaging Systems Comparison: When to Choose What

| **Criteria** | **Apache Kafka** | **Amazon Kinesis** | **Azure Service Bus** | **Amazon SQS** | **Azure Event Hubs** |
|--------------|------------------|-------------------|----------------------|----------------|---------------------|
| **Best Use Case** | High-throughput streaming, event sourcing | AWS-native streaming | Enterprise messaging | Simple queuing | Big data ingestion |
| **Throughput** | Millions/sec | Thousands/sec | Thousands/sec | ~3000/sec per queue | Millions/sec |
| **Message Retention** | Configurable (days to years) | 24 hours - 7 days | 14 days max | 14 days max | 1-7 days |
| **Message Ordering** | Per-partition | Per-shard | Session-based | FIFO queues only | Per-partition |
| **Multi-Region** | Self-managed | Native | Native | Native | Native |
| **Cost Model** | Infrastructure + ops | Per shard/hour | Per message + storage | Per message | Per throughput unit |
| **Ecosystem** | Rich (Kafka Connect, etc.) | AWS-specific | Azure-specific | Limited | Azure-specific |
| **Schema Evolution** | Yes (Schema Registry) | Limited | No | No | Limited |
| **Complexity** | High | Medium | Low | Very Low | Medium |

### **Decision Matrix:**

**Note:** When evaluating stream processing solutions, it's important to distinguish between Kafka (the message broker) and complete streaming solutions (Kafka + Kafka Streams or Apache Flink-based solutions).

- **Choose Kafka alone** when you need: Message queuing, event storage, basic pub/sub, data pipeline transport
- **Choose Kafka + Kafka Streams** when you need: High throughput stream processing, Java/Scala ecosystem, tight Kafka integration, stream topologies, local state management
- **Choose Kinesis** when you have: AWS-only environment, moderate throughput needs, integrated AWS services requirement  
- **Choose Service Bus** when you need: Enterprise messaging patterns, complex routing, Azure-native integration
- **Choose SQS** when you need: Simple queuing, AWS integration, low operational overhead
- **Choose Event Hubs** when you need: Azure-native big data ingestion, moderate complexity

## 🔄 Architecture Comparison: Kafka + Kafka Streams vs FlinkDotNet + Temporal

When choosing between streaming architectures, it's important to compare complete solutions. This section compares **Kafka + Kafka Streams** (the complete Kafka ecosystem) with **FlinkDotNet + Temporal** for enterprise-scale stream processing:

### **Kafka + Kafka Streams vs FlinkDotNet + Temporal Comparison:**

| **Capability** | **Kafka + Kafka Streams** | **FlinkDotNet + Temporal** |
|----------------|---------------------------|----------------------------|
| **Stream Processing** | Kafka Streams provides rich processing (windowing, joins, aggregations) | FlinkDotNet provides equivalent stream processing with Apache Flink 2.1.0 features |
| **Fault Tolerance** | At-least-once processing, exactly-once within Kafka topics | Exactly-once guarantees with Apache Flink checkpointing + Temporal workflows |
| **State Management** | Local state stores with changelog topics for fault tolerance | FlinkDotNet savepoints + Temporal durable state persistence |
| **Scaling** | Horizontal scaling via Kafka partitions, manual rebalancing | FlinkDotNet adaptive scheduler + automatic scaling with Temporal orchestration |
| **Complex Workflows** | Limited to stream processing topologies | Temporal workflows handle long-running, multi-step business processes |
| **Error Handling** | Stream-level error handling and dead letter queues | Temporal's advanced retry policies, compensation patterns, and workflow recovery |
| **Cross-System Coordination** | Limited to Kafka ecosystem, requires external orchestration | Temporal natively coordinates across Kafka + databases + APIs + external systems |
| **Language Ecosystem** | Java/Scala native, limited .NET support | Full .NET integration with C# APIs and .NET ecosystem |

### **When to Choose Each Architecture:**

**Choose Kafka + Kafka Streams when:**
- Your team has strong Java/Scala expertise
- You need tight integration with the Kafka ecosystem
- Your processing requirements fit well within stream processing topologies
- You want to minimize infrastructure complexity (single technology stack)
- Your use cases are primarily stream transformations and aggregations

**Choose FlinkDotNet + Temporal when:**
- Your team uses .NET and C# as primary languages
- You need complex, long-running business process orchestration
- You require advanced fault tolerance and workflow recovery capabilities
- You need to coordinate across multiple external systems and APIs
- You want Apache Flink 2.1.0 features like adaptive scheduling and reactive mode

### **Technical Architecture Comparison:**

```
Kafka + Kafka Streams Architecture:
Kafka (Message Broker) + Kafka Streams (Stream Processing)
    ↓                          ↓
Message Topics          →  Stream Topologies
Partitioned Data        →  Stateful Processing
At-least-once          →  Local State Stores

FlinkDotNet + Temporal Architecture:
Kafka (Data Highway) + FlinkDotNet (Processing Engine) + Temporal (Orchestration Brain)
    ↓                        ↓                              ↓
Stream Transport     →   Real-time Processing      →   Durable Coordination
Partitioned Topics   →   Windowing/Aggregations    →   Multi-step Workflows  
At-least-once       →   Exactly-once Processing   →   Workflow Guarantees
```

## 🚀 Architecture Comparison: FlinkDotNet + Temporal vs Alternative Solutions

### **vs. Traditional ESB (Enterprise Service Bus)**
- **Traditional ESB**: Monolithic, vendor lock-in, limited scalability, expensive licensing
- **Our Stack**: Microservices-friendly, open-source, elastic scaling, cloud-native

### **vs. Cloud-Native Serverless (AWS Lambda + SQS + Step Functions)**
- **Serverless**: Vendor lock-in, cold starts, limited processing time, complex local development  
- **Our Stack**: Multi-cloud, consistent performance, unlimited processing time, local development with Aspire

### **vs. Big Data Stack (Spark + Hadoop + Airflow)**
- **Big Data**: Batch-oriented, complex cluster management, high latency, Java-centric
- **Our Stack**: Stream-first with batch capability, managed scaling, low latency, .NET ecosystem

### **vs. Apache Pulsar + Apache Flink + Apache Airflow**
- **Pulsar + Flink + Airflow**: Java-centric ecosystem, complex multi-system integration, separate orchestration layer
- **Our Stack**: .NET-native APIs with unified Flink integration, simplified operations via Temporal workflows

## 🏗️ Current LocalTesting Architecture Implementation

FlinkDotNet includes a comprehensive local testing environment that demonstrates real-world patterns with optimized performance:

### **Message Processing Architecture (Current Implementation)**
```
📥 100 Logical Customer Queues (1 queue per customer)
    ↓
🔄 20 Kafka Partitions (High-throughput distribution)
  • 3-broker KRaft cluster configuration
  • Auto-partitioning with round-robin distribution
  • Enhanced producer: LZ4 compression, 128KB batches, 2GB buffers
    ↓
⚡ Flink Processing (Apache Flink 2.1.0)
  • JobManager + 3 TaskManagers (8 slots each = 24 total slots)
  • Real-time stream processing with low latency
  • Parallel job execution with dynamic scaling
    ↓
🔄 Temporal Workflows (10% Processing)
  • First 10 customers (out of 100) trigger workflows = 10% of total messages
  • Complex orchestration: Cluster management, resource allocation, scaling
  • Durable execution with exactly-once guarantees
    ↓
📤 Optimized Output Processing
  • End-to-end pipeline with full observability
  • Performance targets: 80,000+ msg/sec per partition
```

### **Full PGL + OpenTelemetry Observability Stack**
The LocalTesting environment includes enterprise-grade observability:

- **Prometheus**: Real-time metrics collection with localtesting_ namespace
- **Grafana**: Unified dashboards for metrics and logs visualization  
- **Loki**: Centralized log aggregation and querying
- **OpenTelemetry Collector**: Complete telemetry collection and export
- **Aspire Dashboard**: Distributed tracing and application insights

## 🏭 Real-World Industrial Use Cases: Multi-Business Case Reusability

The **Kafka + FlinkDotNet + Temporal** architecture excels in scenarios requiring **reusable patterns across diverse business cases** within the same enterprise infrastructure:

### **1. Financial Services: Trading & Risk Management Platform**

**Scenario**: Real-time trade processing, risk calculation, and regulatory reporting

```csharp
// Reusable pattern: Event-driven processing with orchestration
var tradingWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("trades")
    .FlinkProcess(env => env
        .FromKafka("raw-trades")  
        .Map(trade => trade.EnrichWithMarketData())
        .Rebalance()  // Dynamic scaling
        .Filter(trade => trade.PassesRiskChecks())
        .ToKafka("validated-trades"))
    .OrchestrateLongRunning(async () => {
        await settleTradeAsync();
        await updatePortfolioAsync(); 
        await generateRegulatoryReportAsync();
    });
```

**Business Cases Served by Same Architecture:**
- **Real-time Trading**: Low-latency order processing
- **Risk Management**: Continuous position monitoring  
- **Regulatory Reporting**: End-of-day compliance workflows
- **Customer Notifications**: Trade confirmations and alerts
- **Data Analytics**: Real-time dashboards and ML model feeding

### **2. E-commerce: Omnichannel Order Processing**

**Scenario**: Orders from web, mobile, in-store processed through unified pipeline

```csharp
// Current LocalTesting Implementation Pattern: 100 customer queues with 20 Kafka partitions
var orderWorkflow = Temporal.WorkflowBuilder
    .OnMultipleKafkaEvents("web-orders", "mobile-orders", "pos-orders")
    .FlinkProcess(env => env
        .UnionStreams("web-orders", "mobile-orders", "pos-orders")
        .Map(order => order.Normalize())
        .KeyBy(order => order.CustomerId % 100)  // 100 logical customer queues
        .Window(TimeWindow.Of(Time.Minutes(5)))  // Order bundling
        .Aggregate(orders => orders.Combine())
        .PartitionCustom((order, partitions) => order.CustomerId % 20, order => order.CustomerId)  // 20 Kafka partitions
        .ToKafka("unified-orders"))
    .OrchestrateLongRunning(async (order) => {
        // 10% of customers (first 10 out of 100) get enhanced workflow processing
        if (order.CustomerId % 100 < 10) {
            await inventoryCheckAsync(order);
            await paymentProcessingAsync(order);
            await fulfillmentCoordinationAsync(order);
        }
        await customerNotificationAsync(order);
    });
```

**Business Cases Served by Same Architecture:**
- **Order Processing**: Multi-channel order aggregation across 100 customer segments
- **Inventory Management**: Real-time stock updates with 20-partition distribution
- **Payment Processing**: Fraud detection with 10% enhanced workflow processing
- **Fulfillment**: Warehouse coordination using Temporal orchestration
- **Customer Experience**: Real-time order tracking with optimized throughput
- **Analytics**: Customer behavior analysis across logical customer queues

### **3. Manufacturing: IoT Smart Factory**

**Scenario**: Production line monitoring, predictive maintenance, quality control

```csharp
// Reusable pattern: IoT data processing with ML integration
var manufacturingWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("sensor-data")
    .FlinkProcess(env => env
        .FromKafka("iot-sensors")
        .KeyBy(reading => reading.MachineId)
        .Window(SlidingTimeWindow.Of(Time.Minutes(10), Time.Minutes(1)))
        .Aggregate(readings => readings.CalculateMetrics())
        .Filter(metrics => metrics.AnomalyScore > 0.8)
        .ToKafka("anomaly-alerts"))
    .OrchestrateLongRunning(async (anomaly) => {
        var prediction = await callMLModelAsync(anomaly);
        await scheduleMaintenanceAsync(prediction);
        await notifyTechniciansAsync(anomaly);
        await adjustProductionParametersAsync(prediction);
    });
```

**Business Cases Served by Same Architecture:**
- **Predictive Maintenance**: Equipment failure prediction
- **Quality Control**: Real-time defect detection
- **Production Optimization**: Throughput maximization
- **Supply Chain**: Just-in-time inventory
- **Energy Management**: Power consumption optimization
- **Compliance**: Environmental and safety monitoring

### **4. Healthcare: Patient Monitoring & Care Coordination**

**Scenario**: Continuous patient monitoring, care team coordination, emergency response

```csharp
// Reusable pattern: Critical event processing with care coordination  
var healthcareWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("patient-vitals")
    .FlinkProcess(env => env
        .FromKafka("vital-signs")
        .KeyBy(vital => vital.PatientId)
        .Map(vital => vital.AnalyzeWithAI())  // Real-time AI analysis
        .Filter(analysis => analysis.RequiresIntervention)
        .Rebalance()  // Load balancing for critical alerts
        .ToKafka("critical-alerts"))
    .OrchestrateLongRunning(async (alert) => {
        await notifyNursingStationAsync(alert);
        await escalateToPhysicianAsync(alert);
        await prepareEmergencyProtocolsAsync(alert);
        await updatePatientRecordAsync(alert);
    });
```

**Business Cases Served by Same Architecture:**
- **Patient Monitoring**: Continuous vital sign analysis
- **Emergency Response**: Critical event escalation
- **Care Coordination**: Multi-provider workflow
- **Medical Records**: Real-time documentation
- **Resource Management**: Staff and equipment allocation
- **Compliance**: HIPAA audit trails

### **5. Media & Entertainment: Real-time Content Processing**

**Scenario**: Live streaming, content moderation, audience engagement

```csharp
// Reusable pattern: Media processing with real-time engagement
var mediaWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("content-streams")
    .FlinkProcess(env => env
        .FromKafka("live-content")
        .Map(content => content.ExtractMetadata())
        .Filter(content => content.PassesModerationAsync())  // AI content moderation
        .PartitionCustom((content, partitions) => 
            content.ContentType.GetHashCode() % partitions)
        .ToKafka("moderated-content"))
    .OrchestrateLongRunning(async (content) => {
        await generateThumbnailsAsync(content);
        await createSubtitlesAsync(content);  // AI-powered
        await distributeToChannelsAsync(content);
        await trackEngagementMetricsAsync(content);
    });
```

**Business Cases Served by Same Architecture:**
- **Content Processing**: Real-time transcoding and optimization
- **Content Moderation**: AI-powered safety checks
- **Audience Engagement**: Real-time interactions and comments
- **Analytics**: Viewing patterns and recommendations
- **Monetization**: Dynamic ad insertion
- **Distribution**: Multi-platform content delivery

## 🤖 AI/LLM & GenAI Integration Patterns

### **Real-time AI Model Serving Architecture**

```csharp
// Pattern: Real-time AI inference with fallback strategies
var aiWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("inference-requests")
    .FlinkProcess(env => env
        .FromKafka("ai-requests")
        .Map(request => request.PreprocessForModel())
        .KeyBy(request => request.ModelType)  // Route by AI model
        .Rebalance()  // Distribute load across AI workers
        .ToKafka("preprocessed-requests"))
    .OrchestrateLongRunning(async (request) => {
        try {
            var result = await callPrimaryAIModelAsync(request);
            await cacheResultAsync(result);
            return result;
        } catch (ModelUnavailableException) {
            return await callFallbackModelAsync(request);
        }
    });
```

**AI/GenAI Use Cases:**
- **Document Processing**: PDF/image extraction → LLM analysis → structured data
- **Customer Support**: Real-time chat → sentiment analysis → automated responses  
- **Content Generation**: User input → GPT processing → personalized content
- **Fraud Detection**: Transaction streams → ML models → risk scoring
- **Predictive Analytics**: Historical data → AI models → future predictions

## 💼 CI/CD & DevOps Integration Benefits

The architecture provides **unified patterns for both business applications and DevOps workflows**:

### **Build Pipeline Orchestration**
```csharp
// Same patterns for CI/CD as business workflows
var buildWorkflow = Temporal.WorkflowBuilder
    .OnKafkaEvent("git-commits")
    .FlinkProcess(env => env
        .FromKafka("code-changes")
        .Filter(change => change.AffectsProduction())
        .Map(change => change.DetermineTestStrategy())
        .ToKafka("build-requests"))
    .OrchestrateLongRunning(async (buildRequest) => {
        await runTestSuiteAsync(buildRequest);
        await buildArtifactsAsync(buildRequest);
        await deployToStagingAsync(buildRequest);
        await runIntegrationTestsAsync(buildRequest);
        await deployToProductionAsync(buildRequest);
    });
```

**DevOps Benefits:**
- **Unified Architecture**: Same infrastructure for business and DevOps
- **Observability**: Consistent monitoring across all workflows
- **Scalability**: Elastic CI/CD that scales with development team
- **Reliability**: Temporal's workflow guarantees for deployments
- **Cost Efficiency**: Shared infrastructure reduces operational overhead

## 📊 Enterprise ROI & Business Impact

### **Cost Comparison (Enterprise Scale - Estimated)**

*Note: These are estimated costs based on typical enterprise deployments and may vary significantly based on specific requirements, scale, and implementation choices.*

| **Solution** | **Initial Setup** | **Annual Operations** | **3-Year TCO** | **Vendor Lock-in Risk** |
|--------------|------------------|--------------------|----------------|----------------------|
| **Our Stack** | Medium | Low (open-source) | **$2.5M+** | **Low** |
| **Full AWS** | Low | High (per-message) | $4.2M+ | High |
| **Full Azure** | Low | High (per-message) | $3.8M+ | High |
| **Traditional ESB** | High | Very High (licensing) | $6.1M+ | Very High |

### **Development Velocity Impact - Potential Benefits**

*Note: These metrics represent potential improvements and will vary based on team experience, project complexity, and implementation quality.*

- **Time to Production**: Potentially 60% faster with reusable patterns
- **Developer Onboarding**: .NET developers can be productive immediately  
- **Maintenance Overhead**: Potential 70% reduction with unified architecture
- **Bug Resolution**: Potentially faster debugging with consistent patterns

---
## 🚀 Apache Flink 2.1.0 Compatibility

FlinkDotNet implements extensive Apache Flink 2.1.0 feature support for .NET developers, including:

- **Dynamic Scaling**: Change job parallelism without stopping jobs
- **Adaptive Scheduler**: Intelligent resource management and automatic parallelism adjustment
- **Reactive Mode**: Automatic adaptation to available cluster resources
- **Advanced Partitioning**: Rebalance, rescale, forward, shuffle, broadcast, and custom partitioning
- **Savepoint-based Scaling**: Scale jobs using savepoints for state consistency
- **Fine-grained Resource Management**: Slot sharing groups and resource profiles
- **Temporal Multi-cluster Orchestration**: Enterprise-scale coordination across thousands of clusters

## 🔄 Dynamic Scaling and Rebalancing

FlinkDotNet provides comprehensive support for Apache Flink 2.1.0's dynamic scaling capabilities:

### Partitioning Strategies

```csharp
var env = Flink.GetExecutionEnvironment();

var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

// Rebalance: Uniformly distribute data across all parallel operators
var rebalanced = dataStream
    .Map(x => x * 2)
    .Rebalance()  // Apache Flink 2.1.0 rebalance operation
    .Filter(x => x > 5);

// Rescale: Distribute to subset of operators (more efficient for different parallelisms)
var rescaled = dataStream
    .Map(x => x * 3)
    .Rescale()   // Apache Flink 2.1.0 rescale operation
    .Filter(x => x > 10);

// Forward: Direct forwarding (same parallelism required)
var forwarded = dataStream
    .Forward()   // Apache Flink 2.1.0 forward partitioning
    .Map(x => x + 1);

// Shuffle: Random distribution
var shuffled = dataStream
    .Shuffle()   // Apache Flink 2.1.0 shuffle partitioning
    .Map(x => x * 2);

// Broadcast: Send to all operators
var broadcasted = dataStream
    .Broadcast() // Apache Flink 2.1.0 broadcast partitioning
    .Map(x => x + 10);

// Custom partitioning
var customPartitioned = dataStream
    .PartitionCustom(
        (key, numPartitions) => key % numPartitions,  // Custom partitioner
        x => x.GetHashCode()                          // Key selector
    );

await env.ExecuteAsync("Dynamic Partitioning Example");
```

### Parallelism and Scaling Configuration

```csharp
var env = Flink.GetExecutionEnvironment();

// Configure parallelism and scaling parameters
env.SetParallelism(8)                    // Default parallelism
   .SetMaxParallelism(128)               // Maximum parallelism for scaling
   .EnableAdaptiveScheduler()            // Apache Flink 2.1.0 adaptive scheduler
   .EnableReactiveMode();                // Apache Flink 2.1.0 reactive mode

var dataStream = env.FromCollection(data)
    .SetParallelism(4)                   // Operator-specific parallelism
    .SetMaxParallelism(64)              // Operator-specific max parallelism
    .SlotSharingGroup("data-processing") // Fine-grained resource management
    .Map(x => processData(x))
    .Rebalance()                        // Dynamic rebalancing
    .SetParallelism(8);                 // Scale specific operation

await env.ExecuteAsync("Scalable Processing Job");
```

### Savepoint-based Scaling

```csharp
// Start job from savepoint for scaling
var env = Flink.GetExecutionEnvironment()
    .FromSavepoint("/path/to/savepoint")  // Restore from savepoint
    .SetParallelism(16);                  // New parallelism

// Execute job asynchronously to get JobClient
var jobClient = await env.ExecuteAsyncJob("Scaled Job");

// Trigger savepoint for scaling
var savepointResult = await jobClient.TriggerSavepointAsync("/path/to/new/savepoint");

// Stop job with savepoint for clean scaling
var stopResult = await jobClient.StopWithSavepointAsync("/path/to/scaling/savepoint", drain: true);

// Cancel with savepoint (alternative approach)
var cancelResult = await jobClient.CancelWithSavepointAsync();

// Monitor job status during scaling
var status = await jobClient.GetJobStatusAsync();
Console.WriteLine($"Job {status.JobName}: {status.State}, Parallelism: {status.Parallelism}/{status.MaxParallelism}");
```

## Multi-Scale Architecture

FlinkDotNet provides a comprehensive, multi-layered architecture supporting everything from single jobs to enterprise-scale orchestration:

### Individual Job Development

```csharp
// Modern DataStream API (Apache Flink 2.1.0 compatible)
var env = Flink.GetExecutionEnvironment();
env.SetParallelism(4)
   .EnableAdaptiveScheduler()
   .EnableReactiveMode();

var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
dataStream
    .Map(x => x * 2)
    .Rebalance()           // Rebalance across all operators
    .Filter(x => x > 5)
    .Rescale()             // Rescale to subset
    .Print();

await env.ExecuteAsync("My Job");

// JobBuilder API (Alternative fluent approach)
var job = Flink.JobBuilder
    .FromKafka("orders")
    .Where("Amount > 100")
    .GroupBy("Region")
    .Aggregate("SUM", "Amount")
    .ToKafka("high-value-orders");

await job.Submit("Processing Job");
```

### Multi-Cluster FlinkDotNet.Orchestration

```csharp
// Enterprise-scale FlinkDotNet.Orchestration for thousands of clusters
var orchestra = new FlinkOrchestra(logger);

// Provision clusters with auto-scaling
await orchestra.ProvisionClusterAsync(new ClusterConfiguration
{
    Name = "production-cluster",
    TaskSlots = 8,
    TaskManagers = 4
});

// Submit jobs with intelligent placement
var result = await orchestra.SubmitJobAsync(jobDefinition, SubmissionStrategy.BestFit);

// Start Temporal FlinkDotNet.Orchestration workflows
await orchestra.StartOrchestrationWorkflowAsync(new OrchestrationRequest
{
    TargetClusters = 1000,
    MinClusters = 10,
    MaxClusters = 5000
});
```

## Apache Flink 2.1.0 Configuration

```csharp
var config = new ExecutionConfig()
    .SetParallelism(8)
    .SetMaxParallelism(128)
    .EnableAdaptiveScheduler()           // Apache Flink 2.1.0 intelligent scheduling
    .EnableReactiveMode()                // Apache Flink 2.1.0 elastic scaling
    .SetRestartStrategy("exponential-delay")  // Advanced fault tolerance
    .EnableSlotSharing()                 // Resource optimization
    .EnableObjectReuse()                 // Performance optimization
    .SetAutoWatermarkInterval(200);      // Event time processing

var env = Flink.GetExecutionEnvironment(config);
```

## Architecture Overview

FlinkDotNet provides a complete enterprise-scale integration solution with multi-layered architecture:

### Core Components

#### Job Development Layer
- **.NET SDK (FlinkDotNet.DataStream)**: Complete Apache Flink 2.1.0 streaming API
- **JobBuilder SDK (Flink.JobBuilder)**: Fluent C# DSL for rapid development
- **Intermediate Representation (IR)**: JSON-based job definitions
- **Job Gateway**: HTTP service that bridges .NET applications with Apache Flink clusters

#### FlinkDotNet.Orchestration Layer  
- **FlinkDotNet.Orchestration**: Multi-cluster job orchestration with intelligent placement strategies
- **FlinkDotNet.ClusterManager**: Actor-based cluster lifecycle management  
- **FlinkDotNet.Temporal**: Temporal.io workflow definitions for durable orchestration
- **FlinkDotNet.Resilience**: Circuit breakers, retry policies, and health checkers

### Apache Flink 2.1.0 Integration Architecture

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           FlinkDotNet.Orchestration                             │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │
│  │   Cluster A     │  │   Cluster B     │  │   Cluster N     │    ...    │
│  │  (Actor-based)  │  │  (Actor-based)  │  │  (Actor-based)  │           │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                        FlinkDotNet.Temporal Workflows                      │
│        ┌──────────────────────┐  ┌──────────────────────┐                │
│        │  Auto-scaling        │  │  Job Distribution    │                │
│        │  Workflows           │  │  Workflows           │                │
│        └──────────────────────┘  └──────────────────────┘                │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                     Apache Flink 2.1.0 Compatible APIs                     │
│  ┌─────────────────────┐              ┌─────────────────────┐             │
│  │ FlinkDotNet         │              │ Flink.JobBuilder    │             │
│  │ .DataStream         │              │ (Fluent DSL)        │             │
│  │ (Apache Flink 2.1.0   │              │ (Rapid              │             │
│  │  compatible API)    │              │  Development)       │             │
│  └─────────────────────┘              └─────────────────────┘             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Apache Flink 2.1.0 Clusters                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │
│  │ JobManager +    │  │ JobManager +    │  │ JobManager +    │    ...    │
│  │ TaskManagers    │  │ TaskManagers    │  │ TaskManagers    │           │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
```

### FlinkDotNet.Gateway to Apache Flink Communication

The FlinkDotNet.Gateway acts as a bridge between .NET applications and Apache Flink 2.1.0 clusters, supporting advanced scaling features:

#### Single Cluster Communication
1. **Job Submission**: .NET applications submit job definitions via HTTP to the gateway
2. **IR Translation**: Gateway translates JSON IR to Flink JobGraph
3. **Cluster Communication**: Gateway communicates with Flink JobManager via REST API
4. **Status Monitoring**: Gateway provides job status and metrics back to .NET applications

#### Multi-Cluster Orchestration (Apache Flink 2.1.0 Enhanced)
1. **Orchestra Coordination**: FlinkOrchestra manages job distribution across thousands of clusters
2. **Actor-based Management**: Each cluster is managed by an independent ClusterActor
3. **Temporal Workflows**: Long-running orchestration processes with exactly-once guarantees
4. **Intelligent Placement**: Jobs routed to optimal clusters based on health, capacity, and locality
5. **Auto-scaling**: Dynamic cluster provisioning and decommissioning based on demand
6. **Adaptive Scheduling**: Apache Flink 2.1.0 adaptive scheduler integration
7. **Reactive Scaling**: Automatic adaptation to available resources

```
┌─────────────────┐    HTTP     ┌─────────────────┐    Orchestration    ┌─────────────────┐
│   .NET App      │─────────────▶│ FlinkDotNet     │─────────────────────▶│ FlinkDotNet     │
│                 │             │ Gateway         │                     │ Orchestra       │
│ DataStream/     │◀─────────────│                 │◀─────────────────────│ (Multi-cluster) │
│ JobBuilder APIs │   JSON IR   └─────────────────┘    Job Distribution └─────────────────┘
└─────────────────┘                      │                                        │
                                         ▼                                        ▼
                              ┌─────────────────┐                        ┌─────────────────┐
                              │ Apache Flink    │                        │ ClusterManager  │
                              │ JobManager      │◀───────────────────────│ Actors          │
                              │ (Single)        │   REST APIs + Scaling  │ (Thousands)     │
                              └─────────────────┘                        └─────────────────┘
```

The gateway and orchestra handle:
- **Authentication & Authorization**: Secure access to Flink clusters
- **Load Balancing**: Distribute jobs across multiple Flink clusters
- **Monitoring & Metrics**: Real-time job status and performance metrics across all clusters
- **Error Handling**: Graceful error recovery and retry logic with circuit breakers
- **Auto-scaling**: Intelligent cluster provisioning and capacity management
- **Health Aggregation**: Cross-cluster health monitoring and issue detection
- **Dynamic Scaling**: Apache Flink 2.1.0 savepoint-based scaling workflows
- **Adaptive Scheduling**: Integration with Flink 2.1.0 adaptive scheduler
- **Reactive Mode**: Automatic parallelism adjustment based on cluster resources

## Modular Structure

```
FlinkDotNet/
├── FlinkDotNet.Common/           # Core types and configuration
│   ├── Configuration             # Configuration, ExecutionConfig with Flink 2.1.0 features
│   ├── TypeInfo                  # Types, TypeInformation  
│   └── JobManagement            # JobClient with scaling capabilities
├── FlinkDotNet.DataStream/       # Apache Flink 2.1.0 compatible streaming API
│   ├── StreamExecutionEnvironment # Main entry point with adaptive/reactive modes
│   ├── DataStream                # Core streaming API with partitioning strategies
│   ├── Functions                 # User functions
│   └── Connectors               # Sources and sinks
├── FlinkDotNet.Orchestration/        # Multi-cluster orchestration
│   ├── Services                  # FlinkOrchestra, ClusterActorBridge
│   ├── Models                    # ClusterStatus, JobSubmissionResult
│   └── Interfaces               # IFlinkOrchestra, IFlinkClusterActor
├── FlinkDotNet.ClusterManager/   # Individual cluster management
│   ├── Actors                    # FlinkClusterActor (actor-based lifecycle)
│   ├── Models                    # ClusterConfiguration, ClusterMetrics
│   └── Interfaces               # IFlinkClusterActor
├── FlinkDotNet.Temporal/         # Temporal.io workflow definitions
│   ├── Workflows                 # ClusterOrchestrationWorkflow
│   ├── Activities               # Cluster management activities
│   └── Models                   # Workflow request/response models
├── FlinkDotNet.Resilience/       # Fault tolerance patterns
│   ├── CircuitBreakers          # Prevent cascade failures
│   ├── RetryPolicies           # Exponential backoff strategies
│   └── HealthCheckers          # Cluster health validation
├── Flink.JobBuilder/             # Fluent DSL for rapid development
│   ├── FlinkJobBuilder          # Main fluent DSL
│   ├── Models                   # JobDefinition, IR models
│   └── Extensions              # Extension methods
├── FlinkDotNet.Table/           # Table API (future)
├── FlinkDotNet.Testing/         # Testing utilities
├── FlinkDotNet.Util/            # Utility classes
└── FlinkDotNet/                 # Main unified API entry point
```

## Examples

### Individual Job Development

#### Basic Data Processing with Dynamic Scaling

```csharp
var env = Flink.GetExecutionEnvironment();

// Configure Apache Flink 2.1.0 features
env.SetParallelism(4)
   .SetMaxParallelism(128)              // Enable dynamic scaling
   .EnableAdaptiveScheduler()           // Automatic parallelism adjustment
   .EnableReactiveMode()                // Adapt to cluster resources
   .EnableCheckpointing(5000);          // Checkpointing for fault tolerance

var numbers = env.FromCollection(Enumerable.Range(1, 1000));

var result = numbers
    .Filter(x => x % 2 == 0)      // Even numbers only
    .Map(x => x * x)              // Square them
    .Rebalance()                  // Apache Flink 2.1.0 rebalancing
    .SetParallelism(8)            // Scale this operation
    .Sum();                       // Sum the results

await env.ExecuteAsync("Even Squares with Dynamic Scaling");
```

#### Advanced Partitioning and Resource Management

```csharp
var env = Flink.GetExecutionEnvironment();
env.EnableAdaptiveScheduler()
   .EnableReactiveMode();

var dataStream = env.FromCollection(generateData());

// Demonstrate all Apache Flink 2.1.0 partitioning strategies
var processed = dataStream
    .Map(x => processData(x))
    .SetParallelism(4)
    .SlotSharingGroup("data-processing")    // Fine-grained resource management
    
    // Rebalance: Uniform distribution across all operators
    .Rebalance()
    .Map(x => enrichData(x))
    .SetParallelism(8)
    
    // Rescale: Efficient distribution for different parallelisms  
    .Rescale()
    .Filter(x => x.IsValid)
    .SetParallelism(4)
    
    // Forward: Direct forwarding (same parallelism)
    .Forward()
    .Map(x => finalProcessing(x))
    .SetParallelism(4)
    
    // Custom partitioning based on business logic
    .PartitionCustom(
        (key, numPartitions) => key.GetHashCode() % numPartitions,
        x => x.CustomerId
    )
    .SlotSharingGroup("customer-processing");

await env.ExecuteAsync("Advanced Partitioning Example");
```

#### Kafka Integration with Dynamic Scaling

```csharp
var job = Flink.JobBuilder
    .FromKafka("input-topic", config => {
        config.BootstrapServers = "localhost:9092";
        config.GroupId = "processing-group";
    })
    .Map("processed = transform(data)")
    .Where("processed.isValid")
    .ToKafka("output-topic");

// Configure Apache Flink 2.1.0 features for the job
await job.Configure(config => {
    config.EnableAdaptiveScheduler()
          .EnableReactiveMode()
          .SetParallelism(8)
          .SetMaxParallelism(128);
}).Submit("Kafka Processing with Auto-Scaling");
```

#### Windowed Aggregations with Reactive Scaling

```csharp
var job = Flink.JobBuilder
    .FromKafka("events")
    .GroupBy("userId")
    .Window("TUMBLING", 5, "MINUTES")
    .Aggregate("COUNT", "*")
    .ToKafka("user-activity");

await job.Configure(config => {
    config.EnableReactiveMode()          // Adapt to cluster resources
          .SetRestartStrategy("exponential-delay")  // Advanced fault tolerance
          .EnableSlotSharing();          // Resource optimization
}).Submit("User Activity with Reactive Scaling");
```

### Multi-Cluster Orchestration

#### Cluster Provisioning and Management

```csharp
var orchestra = new FlinkOrchestra(logger);

// Provision a new cluster with Apache Flink 2.1.0 features
var cluster = await orchestra.ProvisionClusterAsync(new ClusterConfiguration
{
    Name = "production-west",
    TaskSlots = 16,
    TaskManagers = 8,
    Region = "us-west-2",
    HighAvailability = true,
    AdaptiveSchedulerEnabled = true,    // Enable Apache Flink 2.1.0 adaptive scheduler
    ReactiveModeEnabled = true          // Enable reactive mode
});

// Get cluster health across all clusters
var health = await orchestra.GetClusterHealthAsync();
Console.WriteLine($"Overall Health Score: {health.OverallHealthScore:F1}%");
Console.WriteLine($"Total Clusters: {health.TotalClusters}");
Console.WriteLine($"Healthy: {health.HealthyClusters}, Critical: {health.CriticalClusters}");
```

#### Intelligent Job Submission with Scaling

```csharp
// Define a job with Apache Flink 2.1.0 configuration
var jobDefinition = new FlinkJobDefinition
{
    JobId = "analytics-pipeline",
    JobName = "Real-time Analytics",
    JobGraph = "...", // Generated from DataStream/JobBuilder
    Parallelism = 8,
    MaxParallelism = 128,               // Enable dynamic scaling
    AdaptiveSchedulerEnabled = true,    // Intelligent resource management
    ReactiveModeEnabled = true,         // Automatic adaptation
    Priority = JobPriority.High
};

// Submit with intelligent placement
var result = await orchestra.SubmitJobAsync(jobDefinition, SubmissionStrategy.BestFit);

if (result.Success)
{
    Console.WriteLine($"Job {result.JobId} submitted to cluster {result.ClusterId}");
    Console.WriteLine($"Flink Job ID: {result.FlinkJobId}");
    
    // Monitor scaling behavior
    var jobClient = result.JobClient;
    var status = await jobClient.GetJobStatusAsync();
    Console.WriteLine($"Current Parallelism: {status.Parallelism}/{status.MaxParallelism}");
}
```

#### Savepoint-based Scaling Workflows

```csharp
// Execute job with scaling capabilities
var jobClient = await env.ExecuteAsyncJob("Scalable Analytics Job");

// Monitor and scale using savepoints
var status = await jobClient.GetJobStatusAsync();
Console.WriteLine($"Initial Parallelism: {status.Parallelism}");

// Create savepoint for scaling
var savepointResult = await jobClient.TriggerSavepointAsync("/savepoints/scaling-point");
if (savepointResult.Success)
{
    Console.WriteLine($"Savepoint created at: {savepointResult.SavepointPath}");
    
    // Stop job gracefully for scaling
    var stopResult = await jobClient.StopWithSavepointAsync(savepointPath: savepointResult.SavepointPath, drain: true);
    
    if (stopResult.Success)
    {
        // Restart with new parallelism
        var scaledEnv = Flink.GetExecutionEnvironment()
            .FromSavepoint(stopResult.SavepointPath)    // Restore from savepoint
            .SetParallelism(16)                         // New parallelism
            .SetMaxParallelism(256)                     // New max parallelism
            .EnableAdaptiveScheduler()
            .EnableReactiveMode();
        
        // Re-execute with scaled configuration
        var scaledJobClient = await scaledEnv.ExecuteAsyncJob("Scaled Analytics Job");
        var scaledStatus = await scaledJobClient.GetJobStatusAsync();
        Console.WriteLine($"Scaled Parallelism: {scaledStatus.Parallelism}");
    }
}
```

#### Auto-scaling with Temporal Workflows

```csharp
// Start long-running orchestration workflow with Apache Flink 2.1.0 features
var workflowId = await orchestra.StartOrchestrationWorkflowAsync(new OrchestrationRequest
{
    RequestId = "scaling-request-1",
    TargetClusters = 500,
    MinClusters = 50,
    MaxClusters = 2000,
    ScalingPolicy = "demand-based",
    AdaptiveSchedulerEnabled = true,    // Enable intelligent scheduling across clusters
    ReactiveModeEnabled = true          // Enable reactive scaling
});

Console.WriteLine($"Started orchestration workflow: {workflowId}");

// Monitor and scale dynamically
var scalingResult = await orchestra.ScaleOrchestraAsync(targetCapacity: 750);
Console.WriteLine($"Scaled from {scalingResult.PreviousCapacity} to {scalingResult.NewCapacity} clusters");
```

## Backpressure and Rate Limiting

FlinkDotNet includes built-in backpressure support with Apache Flink 2.1.0 enhancements to ensure system stability:

```csharp
using Flink.JobBuilder.Backpressure;

// Configure rate limiter with adaptive behavior
var rateLimiter = new TokenBucketRateLimiter(
    rateLimit: 1000.0,      // 1000 operations per second
    burstCapacity: 2000.0   // Handle bursts up to 2000
);

// Use in your application with automatic backpressure handling
if (rateLimiter.TryAcquire())
{
    await ProcessMessage(message);
}
else
{
    // Apache Flink 2.1.0 handles backpressure automatically
    // This provides additional application-level control
    await Task.Delay(100); // Wait and retry
}

// Configure backpressure in execution environment
var env = Flink.GetExecutionEnvironment();
env.GetConfig()
   .SetProperty("taskmanager.network.memory.max-buffers-per-channel", "10")
   .SetProperty("taskmanager.network.memory.buffers-per-channel", "2")
   .EnableObjectReuse();  // Reduce GC pressure
```

## Testing and Reliability

FlinkDotNet includes comprehensive testing capabilities with Apache Flink 2.1.0 integration:

### Integration Tests

```csharp
[Fact]
public async Task TestStreamProcessingWithScaling()
{
    var env = Flink.GetExecutionEnvironment();
    env.EnableAdaptiveScheduler()
       .EnableReactiveMode()
       .SetMaxParallelism(128);
    
    var testData = new[] { 1, 2, 3, 4, 5 };
    var result = env.FromCollection(testData)
        .Map(x => x * 2)
        .Rebalance()                    // Test Apache Flink 2.1.0 rebalancing
        .SetParallelism(4)              // Test dynamic parallelism
        .CollectAsync();
        
    var expected = new[] { 2, 4, 6, 8, 10 };
    Assert.Equal(expected, await result);
}

[Fact]  
public async Task TestSavepointBasedScaling()
{
    var jobClient = await env.ExecuteAsyncJob("Test Scaling Job");
    
    // Test savepoint creation
    var savepointResult = await jobClient.TriggerSavepointAsync();
    Assert.True(savepointResult.Success);
    
    // Test graceful stopping with savepoint
    var stopResult = await jobClient.StopWithSavepointAsync(drain: true);
    Assert.True(stopResult.Success);
    Assert.True(stopResult.Drained);
}
```

### Stress Testing

The project includes comprehensive stress tests that validate:
- High-throughput processing (1M+ messages)
- Backpressure handling with Apache Flink 2.1.0 improvements
- Fault tolerance and recovery with adaptive scheduling
- Dynamic scaling scenarios and savepoint-based workflows
- Reactive mode adaptation to resource changes

## Local Development with Aspire

FlinkDotNet integrates with .NET Aspire for local development with Apache Flink 2.1.0 features.

**Platform-Specific Setup**: Before using Aspire locally, ensure the workload is installed:
- **Windows/macOS**: Aspire is typically included with .NET SDK (.NET 8+)
- **Linux**: Manual installation required: `dotnet workload install aspire`

```csharp
// LocalTesting/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");

// Apache Flink 2.1.0 cluster with advanced features
var flink = builder.AddContainer("flink", "flink:2.0-latest")
    .WithEnvironment("FLINK_PROPERTIES", 
        "scheduler-mode: adaptive\n" +
        "scheduler.adaptive.scaling-enabled: true\n" +
        "scheduler.adaptive.resource.wait-timeout: 60s\n" +
        "execution.checkpointing.interval: 5s\n" +
        "parallelism.default: 4\n" +
        "parallelism.default.sink: 8\n" +
        "taskmanager.numberOfTaskSlots: 8");

var gateway = builder.AddProject<Projects.FlinkDotNet_Gateway>("gateway")
    .WithReference(flink);

var testApp = builder.AddProject<Projects.TestApp>("testapp")
    .WithReference(gateway)
    .WithReference(kafka);

builder.Build().Run();
```

## 🔨 Build and Test Enforcement

### NET 9.0 Requirements

FlinkDotNet implements comprehensive build and test validation to ensure code quality and prevent build failures.

### Quick Setup
```bash
# Verify .NET 9.0 requirement
dotnet --version  # Must show 9.0.x

# Run complete validation
./scripts/validate-build-and-tests.ps1

# Quick build check (skip tests)
./scripts/validate-build-and-tests.ps1 -SkipTests
```

### Enforcement Rules
- ✅ **ALL builds MUST pass** before commits/merges
- ✅ **.NET 9.0.x** required for all development
- ✅ **Three solutions** validated: FlinkDotNet, Sample, LocalTesting
- ✅ **Automated blocking** of build failures via GitHub Actions

### Pre-Commit Validation
```bash
# Always run before committing
./scripts/pre-commit-validation.ps1
```

### Documentation
- 📖 **[Complete Guide](docs/BUILD_ENFORCEMENT.md)** - Detailed enforcement rules and troubleshooting
- 🚀 **[Quick Start](docs/BUILD_ENFORCEMENT_QUICKSTART.md)** - 2-minute developer setup guide

**Important**: Build failures are automatically blocked. Fix build errors before proceeding with any development work.

## Getting Started

### Single Job Development

1. **Clone and Build FlinkDotNet Repository**
   ```bash
   git clone https://github.com/devstress/FlinkDotnet.git
   cd FlinkDotnet
   dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
   ```

2. **Set up Apache Flink 2.1.0 cluster**
   - Download and install Apache Flink 2.1.0
   - Start JobManager and TaskManager with adaptive scheduler enabled
   - Configure reactive mode if desired

3. **Deploy FlinkDotNet.Gateway**
   - Configure connection to your Flink cluster
   - Deploy as web service or container
   - Enable Apache Flink 2.1.0 feature support

4. **Build and submit your first job with scaling capabilities**
   ```csharp
   // Apache Flink 2.1.0 compatible approach (DataStream API)
   var env = Flink.GetExecutionEnvironment();
   env.EnableAdaptiveScheduler()
      .EnableReactiveMode()
      .SetMaxParallelism(128);
      
   var stream = env.FromCollection(new[] { 1, 2, 3 })
       .Rebalance()
       .SetParallelism(4);
   await env.ExecuteAsync("My First Scaling Job");
   
   // Alternative approach (JobBuilder for rapid development)
   var job = Flink.JobBuilder
       .FromKafka("source")
       .Map("value = process(data)")
       .ToKafka("destination");
   await job.Configure(config => config.EnableAdaptiveScheduler())
            .Submit("My First JobBuilder Job");
   ```

### Enterprise-Scale Multi-Cluster Setup

1. **Use FlinkDotNet Repository (All Components Included)**
   ```bash
   # The repository contains all enterprise components:
   # - FlinkDotNet.Orchestration, ClusterManager, Temporal, Resilience
   cd FlinkDotnet
   dotnet build --configuration Release
   ```

2. **Set up Temporal Server**
   ```bash
   # Using Docker
   docker run -p 7233:7233 -p 8233:8233 temporalio/auto-setup:latest
   ```

3. **Initialize Orchestra service with Apache Flink 2.1.0 features**
   ```csharp
   var services = new ServiceCollection();
   services.AddLogging();
   services.AddSingleton<IFlinkOrchestra, FlinkOrchestra>();
   
   var provider = services.BuildServiceProvider();
   var orchestra = provider.GetRequiredService<IFlinkOrchestra>();
   ```

4. **Start with cluster provisioning and scaling**
   ```csharp
   // Provision your first cluster with Apache Flink 2.1.0 features
   var cluster = await orchestra.ProvisionClusterAsync(new ClusterConfiguration
   {
       Name = "starter-cluster",
       TaskSlots = 4,
       TaskManagers = 2,
       AdaptiveSchedulerEnabled = true,    // Enable intelligent scheduling
       ReactiveModeEnabled = true          // Enable automatic adaptation
   });
   
   // Check overall health and scaling capabilities
   var health = await orchestra.GetClusterHealthAsync();
   Console.WriteLine($"Health Score: {health.OverallHealthScore:F1}%");
   Console.WriteLine($"Adaptive Scheduler: {cluster.AdaptiveSchedulerEnabled}");
   Console.WriteLine($"Reactive Mode: {cluster.ReactiveModeEnabled}");
   ```

## Documentation

### Core Documentation
- [Getting Started Guide](./docs/wiki/Getting-Started.md)
- [Contributing Guidelines](./CONTRIBUTING.md)

### Apache Flink 2.1.0 Documentation
- See the official Flink docs for feature configuration and operations.

### Temporal Durable Workflow Architecture Documentation
- [Flink vs Temporal Decision Guide](./docs/flink-vs-temporal-decision-guide.md)
- [Local Testing Setup](./docs/local-testing-setup.md)

### Testing and Quality Assurance
- [Observability Mapping](./docs/observability.md)
- [Monitoring Best Practices](./docs/observability/monitoring-best-practices.md)

## 📊 Observability Metrics Testing

FlinkDotNet includes comprehensive observability testing that validates message-per-second metrics across all system layers with **configurable message processing** using the full LGTM observability stack.

### **Observability Tests Workflow**

🔗 **[View Observability Test Runs](../../actions/workflows/observability-tests.yml)** - Monitor real-time observability metrics test execution

The observability tests process **configurable message volumes** (100,000 for CI, 1 million for full testing) to validate:
- **Kafka Producer Metrics**: Messages-per-second rates across 20 partitions with enhanced producer configuration
- **Flink Processing Metrics**: Real-time stream processing throughput with Apache Flink 2.1.0 features  
- **Temporal Workflow Metrics**: Workflow execution rates for 10% of messages (first 10 customers out of 100)
- **End-to-End Flow Metrics**: Complete pipeline throughput through 100 logical customer queues

### **Full PGL + OpenTelemetry Stack Integration**

The observability system uses the complete PGL + OpenTelemetry stack:
- **Prometheus**: Real-time metrics with localtesting_ namespace prefix for component isolation
- **Grafana**: Unified dashboards showing metrics and logs in single view
- **Loki**: Centralized log aggregation from all LocalTesting components
- **OpenTelemetry Collector**: Telemetry collection and forwarding
- **Aspire Dashboard**: Distributed tracing across Kafka → Flink → Temporal → output pipeline

### **Live Metrics Display**

During test execution, the system displays real-time metrics:

```
📈 Kafka Producer Metrics (20 partitions):
  📤 ingress-topic-partition-0: 80,247.5 msg/sec
  📤 ingress-topic-partition-1: 80,195.2 msg/sec
  ... (18 more partitions with balanced load)
📈 Flink Processing Metrics:  
  📥 Input Rate - real-job-1: 800,000 msg/sec (from 20 partitions)
  📤 Output Rate - real-job-1: 799,500 msg/sec (high efficiency)
📈 Temporal Workflow Metrics:
  📊 Workflow Rate: 80,000 workflows/sec (10% of message volume from 10 customers)
  📊 Complex Orchestration: Cluster scaling, resource allocation, workflow coordination
📈 End-to-End Flow Metrics:
  📊 100 Customer Queues → 20 Kafka Partitions: 800,000 msg/sec
  📊 Complete Pipeline: 799,500 msg/sec (total throughput with 10% Temporal processing)
```

### **High-Throughput Validation with Current Architecture**

The message processing validates current optimized architecture:
- ✅ **100 Logical Customer Queues**: Even distribution across customer segments
- ✅ **20 Kafka Partitions**: Enhanced partition distribution for maximum throughput
- ✅ **10% Temporal Processing**: First 10 customers trigger complex workflows
- ✅ **Enhanced Producer Configuration**: LZ4 compression, 128KB batches, 2GB buffers
- ✅ **Full LGTM Observability**: Complete telemetry collection and visualization

Run locally:
```bash
  # Run observability tests (LocalTesting)
  dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj \
    --filter "Category=observability" \
    --configuration Release
```

## Frequently Asked Questions

### How does FlinkDotNet support Apache Flink 2.1.0 features?

**FlinkDotNet provides complete Apache Flink 2.1.0 compatibility** including:

- **Adaptive Scheduler**: Automatic parallelism adjustment based on workload characteristics
- **Reactive Mode**: Elastic scaling that adapts to available cluster resources
- **Dynamic Scaling**: Change job parallelism without stopping jobs using savepoints
- **Advanced Partitioning**: All Apache Flink 2.1.0 partitioning strategies (rebalance, rescale, forward, shuffle, broadcast, custom)
- **Fine-grained Resource Management**: Slot sharing groups and resource profiles
- **Enhanced Fault Tolerance**: Advanced restart strategies and checkpointing

```csharp
// Enable all Apache Flink 2.1.0 features
var env = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()     // Intelligent resource management
    .EnableReactiveMode()          // Elastic scaling
    .SetMaxParallelism(256)        // Dynamic scaling support
    .EnableCheckpointing(5000);    // Enhanced fault tolerance

var scalableStream = env.FromCollection(data)
    .Rebalance()                   // Apache Flink 2.1.0 rebalancing
    .SetParallelism(8)             // Dynamic parallelism
    .SlotSharingGroup("processing"); // Fine-grained resources
```

### What are the scaling approaches available?

**FlinkDotNet supports multiple scaling approaches:**

1. **Reactive Mode Scaling** (Automatic)
   ```csharp
   env.EnableReactiveMode(); // Automatically adapts to cluster resources
   ```

2. **Adaptive Scheduler** (Intelligent)
   ```csharp
   env.EnableAdaptiveScheduler(); // AI-driven parallelism adjustment
   ```

3. **Savepoint-based Scaling** (Manual)
   ```csharp
   var jobClient = await env.ExecuteAsyncJob("My Job");
   var savepoint = await jobClient.TriggerSavepointAsync();
   // Restart with new parallelism from savepoint
   ```

4. **Runtime Partitioning** (Dynamic)
   ```csharp
   dataStream.Rebalance()    // Redistribute uniformly
            .Rescale()       // Efficient subset distribution
            .Forward()       // Direct forwarding
            .Shuffle();      // Random distribution
   ```

### How do I choose between different APIs?

**Choose based on your use case:**

- **DataStream API**: Use for Apache Flink 2.1.0 compatibility, complex stream processing, and when you need full control over scaling and partitioning
- **JobBuilder API**: Use for rapid development, simple pipelines, and when you prefer fluent syntax
- **Orchestra API**: Use for enterprise-scale multi-cluster deployments with thousands of jobs

**Example decision matrix:**
```csharp
// Complex processing with scaling requirements
var env = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()
    .EnableReactiveMode();
var stream = env.FromCollection(data)
    .Rebalance()
    .SetParallelism(8);

// Simple pipeline with fluent syntax
var job = Flink.JobBuilder
    .FromKafka("input")
    .Map("process(data)")
    .ToKafka("output");

// Enterprise multi-cluster orchestration
var orchestra = new FlinkOrchestra(logger);
await orchestra.SubmitJobAsync(jobDef, SubmissionStrategy.BestFit);
```

### Migration Path from Earlier Versions

**FlinkDotNet maintains full compatibility** while adding Apache Flink 2.1.0 features:

1. **Keep existing code**: All existing DataStream and JobBuilder code continues to work
2. **Add Apache Flink 2.1.0 features gradually**: Enable adaptive scheduler, reactive mode, and advanced partitioning as needed
3. **Scale incrementally**: Start with single cluster, add orchestration layer when needed
4. **Optimize performance**: Use new partitioning strategies and resource management features

**Migration example:**
```csharp
// Existing code (still works)
var env = Flink.GetExecutionEnvironment();
var stream = env.FromCollection(data).Map(x => x * 2);

// Enhanced with Apache Flink 2.1.0 features
var enhancedEnv = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()     // Add intelligent scheduling
    .EnableReactiveMode()          // Add elastic scaling
    .SetMaxParallelism(128);       // Enable dynamic scaling

var enhancedStream = enhancedEnv.FromCollection(data)
    .Map(x => x * 2)
    .Rebalance()                   // Add efficient rebalancing
    .SetParallelism(8);            // Set optimal parallelism
```

The architecture is designed for **incremental adoption** - you can start with basic features and scale to enterprise levels with Apache Flink 2.1.0 capabilities as your requirements grow.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

## License

MIT License - see [LICENSE](./LICENSE) file for details.
