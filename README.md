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
- **SQL Gateway**: Separate Flink SQL Gateway container forwarding SQL to Flink cluster - follows Flink's recommended deployment pattern for production environments. Enables Flink SQL UI for visual query execution and catalog browsing.

Refer to the docs/ directory for the implementation roadmap and guides.

## ✅ Verified Working Solution - Integration Tests Passing

FlinkDotNet is a **production-ready, fully tested framework** with comprehensive integration tests validating the complete pipeline. Don't take our word for it - see the tests running in CI:

🔗 **[View Live Integration Test Results](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)** - 9 tests passing on every commit

### What's Validated

**✅ Complete End-to-End Pipeline:**
- Kafka message production and consumption
- Flink cluster job processing (JobManager + TaskManagers)
- FlinkDotNet Gateway job submission and monitoring
- Full data flow: Kafka → Flink → Processing → Output

**✅ All Major FlinkDotNet Features:**
- Basic transformations (map, filter, flatMap)
- Stateful processing (timers, event time)
- SQL support (native Flink SQL via TableEnvironment)
- Complex multi-step pipelines
- Aspire orchestration and container management

**✅ 10 Integration Tests Cover:**

| Test | What It Proves | Status |
|------|---------------|--------|
| **Gateway Pattern 1**: [`Uppercase`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:23) | Basic map transformation works | ✅ Passing |
| **Gateway Pattern 2**: [`Filter`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:36) | Filter operations work correctly | ✅ Passing |
| **Gateway Pattern 3**: [`SplitConcat`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:48) | FlatMap and aggregation work | ✅ Passing |
| **Gateway Pattern 4**: [`Timer`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:62) | Stateful processing with timers works | ✅ Passing |
| **Gateway Pattern 5**: [`DirectFlinkSQL`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:76) | Native Flink SQL execution works | ✅ Passing |
| **Gateway Pattern 6**: [`SqlTransform`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:90) | SQL transformation pipeline works | ✅ Passing |
| **Gateway Pattern 7**: [`Composite`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:104) | Complex multi-step operations work | ✅ Passing |
| **Native Flink**: [`Uppercase`](LocalTesting/LocalTesting.IntegrationTests/NativeFlinkAllPatternsTests.cs:29) | Aspire infrastructure works correctly | ✅ Passing |
| **Temporal Integration**: [`BizTalkStyleOrchestration`](LocalTesting/LocalTesting.IntegrationTests/TemporalIntegrationTests.cs:16) | Temporal + Kafka + FlinkDotNet integration works | ✅ Passing |
| **Infrastructure**: [`AspireValidation`](LocalTesting/LocalTesting.IntegrationTests/AspireValidationTest.cs:16) | All services are accessible | ✅ Passing |

### Run Tests Yourself

Verify FlinkDotNet works on your machine:

```bash
# Prerequisites: .NET 9.0, Docker Desktop, Aspire workload
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release
```

**Expected output**: All 10 tests pass, proving the complete pipeline works end-to-end.

For detailed test documentation, test architecture, and troubleshooting, see [LocalTesting Integration Tests Documentation](#localtesting-integration-tests-detailed-documentation).


## 🔬 DSL to IR to Flink Job: Complete Example

### 1. C# DSL (Domain-Specific Language)

```csharp
using FlinkDotNet;

// Build a streaming job using fluent C# API
var job = Flink.JobBuilder
    .FromKafka("input-topic", "kafka:9093")
    .Map("uppercase")  // Transform each message to uppercase
    .Filter("non-empty")  // Keep only non-empty messages
    .ToKafka("output-topic", "kafka:9093");

// Submit to Flink cluster
var result = await job.Submit("my-uppercase-job");
```

### 2. IR (Intermediate Representation) - JobDefinition JSON

The DSL compiles to a portable JSON IR that describes the job:

```json
{
  "metadata": {
    "jobId": "job-123",
    "jobName": "my-uppercase-job",
    "submittedAt": "2025-01-01T12:00:00Z"
  },
  "source": {
    "type": "kafka",
    "topic": "input-topic",
    "bootstrapServers": "kafka:9093",
    "groupId": "flink-consumer-group"
  },
  "operations": [
    {
      "type": "map",
      "operator": "uppercase",
      "implementation": "com.flinkdotnet.operators.UppercaseMapFunction"
    },
    {
      "type": "filter",
      "operator": "non-empty",
      "implementation": "com.flinkdotnet.operators.NonEmptyFilterFunction"
    }
  ],
  "sink": {
    "type": "kafka",
    "topic": "output-topic",
    "bootstrapServers": "kafka:9093"
  }
}
```

### 3. IR Runner JAR: Building the Flink Job

The **IR Runner JAR** (`flink-ir-runner.jar`) is a prebuilt Java artifact that:
- Reads the IR JSON at runtime
- Builds the Flink DataStream topology dynamically
- Instantiates sources, operators, and sinks
- Handles connector dependencies (Kafka, JSON, etc.)

**IR Runner execution flow:**

```java
// Simplified IR Runner logic (actual implementation in FlinkIRRunner/)
public class FlinkIRRunner {
    public static void main(String[] args) {
        // 1. Read IR from environment or base64 argument
        String irJson = System.getenv("FLINK_JOB_IR");
        JobDefinition jobDef = parseIR(irJson);
        
        // 2. Build Flink execution environment
        StreamExecutionEnvironment env = 
            StreamExecutionEnvironment.getExecutionEnvironment();
        
        // 3. Create source from IR
        DataStream<String> source = createKafkaSource(env, jobDef.source);
        
        // 4. Apply operations from IR
        for (Operation op : jobDef.operations) {
            if (op.type.equals("map")) {
                source = source.map(new UppercaseMapFunction());
            } else if (op.type.equals("filter")) {
                source = source.filter(new NonEmptyFilterFunction());
            }
        }
        
        // 5. Create sink from IR
        source.addSink(createKafkaSink(jobDef.sink));
        
        // 6. Execute job
        env.execute(jobDef.metadata.jobName);
    }
}
```

**Key benefits:**
- **No per-job compilation**: Single JAR handles all job types
- **Connector bundling**: Kafka, JSON, and other connectors included
- **Version control**: Update runner JAR without changing user code
- **Debugging**: Logs show IR interpretation and topology building

## 🌐 Flink.JobGateway Connections: How Everything Connects

### Architecture Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        .NET Application                         │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  FlinkDotNet DSL                                         │  │
│  │  var job = Flink.JobBuilder.FromKafka(...).Map(...)     │  │
│  └──────────────────┬───────────────────────────────────────┘  │
│                     │ Compiles to IR (JSON)                     │
└─────────────────────┼───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│              Flink.JobGateway (ASP.NET Core)                    │
│  ┌──────────────────────────────────────────────────────────┐  │
│  │  POST /api/jobs  (receives IR)                           │  │
│  │  1. Validate IR JobDefinition                            │  │
│  │  2. Choose execution mode:                               │  │
│  │     - TableEnvironment SQL → Submit via JM REST          │  │
│  │     - Gateway SQL → Submit via SQL Gateway REST          │  │
│  │     - DataStream → Upload IR Runner JAR + Submit         │  │
│  └──────────────────┬───────────────────────────────────────┘  │
└─────────────────────┼───────────────────────────────────────────┘
                      │
        ┌─────────────┴──────────────┐
        │                            │
        ▼                            ▼
┌───────────────────┐      ┌──────────────────────┐
│ Flink JobManager  │◄─────│ Flink SQL Gateway    │
│   (Port 8081)     │      │   (Port 8083)        │
│                   │      │  (Control-plane)     │
│ REST API:         │      │                      │
│ /jars/upload      │      │ REST API:            │
│ /jars/{id}/run    │      │ /v1/sessions         │
│ /jobs/{id}        │      │ /v1/sessions/{id}/   │
│ /jobs/{id}/cancel │      │   statements         │
└─────────┬─────────┘      └──────────┬───────────┘
          │                           │
          │  Submits jobs to cluster  │
          └───────────┬───────────────┘
                      │
          ┌───────────▼────────────┐
          │   Flink TaskManagers   │
          │   (Data Plane)         │
          │                        │
          │  - Execute operators   │
          │  - Manage state        │
          │  - Process streams     │
          │  - Connect to Kafka    │
          └────────────────────────┘
```

### Connection Details

#### 1. **Flink.JobGateway → Flink JobManager** (TableEnvironment & DataStream jobs)

```csharp
// Gateway discovers JobManager endpoint via Aspire
var jobManagerEndpoint = DiscoverFlinkJobManagerEndpoint();
// Priority: Aspire service discovery > Environment vars > Default

using var httpClient = new HttpClient { BaseAddress = new Uri(jobManagerEndpoint) };

// Upload IR Runner JAR (first time only, cached by JM)
var uploadResponse = await httpClient.PostAsync("/v1/jars/upload", jarContent);
var jarId = parseJarId(uploadResponse);

// Submit job with IR as argument
var submitPayload = new {
    entryClass = "com.flinkdotnet.FlinkIRRunner",
    programArgs = $"--ir-base64 {Base64.Encode(irJson)}",
    parallelism = 4
};
await httpClient.PostAsync($"/v1/jars/{jarId}/run", submitPayload);
```

#### 2. **Flink.JobGateway → Flink SQL Gateway** (SQL Gateway mode)

```csharp
// Gateway discovers SQL Gateway endpoint
var sqlGatewayEndpoint = DiscoverSqlGatewayEndpoint();

using var sqlClient = new HttpClient { BaseAddress = new Uri(sqlGatewayEndpoint) };

// Step 1: Create session
var sessionRequest = new { sessionName = "my-sql-job" };
var sessionResponse = await sqlClient.PostAsync("/v1/sessions", sessionRequest);
var sessionHandle = parseSessionHandle(sessionResponse);

// Step 2: Execute SQL statements
foreach (var statement in sqlStatements) {
    var stmtRequest = new { statement = statement };
    await sqlClient.PostAsync(
        $"/v1/sessions/{sessionHandle}/statements", 
        stmtRequest
    );
}
```

#### 3. **Flink SQL Gateway → Flink JobManager** (Control-plane forwarding)

SQL Gateway is configured to connect to the JobManager:

```yaml
# SQL Gateway FLINK_PROPERTIES
jobmanager.rpc.address: flink-jobmanager
rest.address: flink-jobmanager
rest.port: 8081
sql-gateway.endpoint.type: remote  # Forward to cluster
```

When SQL Gateway receives SQL statements:
1. Parses and validates SQL
2. Converts to Flink job graph
3. Submits to JobManager via REST API
4. Returns operation handle to client

### SQL Gateway Approaches: Containerized vs Embedded

FlinkDotNet supports two approaches for SQL job submission:

#### **1. Containerized SQL Gateway (Production Pattern)**

**Architecture**: Separate Flink SQL Gateway container (port 8083) forwards SQL to Flink cluster

**Benefits**:
- ✅ Official Flink component with full SQL feature support
- ✅ Enables **Flink SQL UI** - visual interface for running queries, browsing catalogs, viewing results
- ✅ Follows Flink's recommended deployment pattern for Kubernetes/production
- ✅ Stateless control-plane service - horizontally scalable
- ✅ Session management for multi-query workflows

**Trade-offs**:
- Additional container infrastructure complexity
- Container startup and health check orchestration
- Network connectivity between Gateway container and Flink cluster

**Configuration**:
```yaml
# SQL Gateway connects to Flink JobManager cluster
sql-gateway:
  container: apache/flink:2.1.0-java17
  command: ["sql-gateway.sh", "start-foreground"]
  environment:
    rest.address: flink-jobmanager
    rest.port: 8081
    sql-gateway.endpoint.type: remote
```

**Access Flink SQL UI**: Once SQL Gateway is running, access the web UI at `http://sql-gateway:8083` to visually execute SQL queries, explore catalogs, and monitor results.

**Why SQL Gateway Container**:
- ✅ Official Flink component - full SQL feature support
- ✅ Flink SQL UI enabled - visual query execution and catalog browsing
- ✅ Horizontal scaling - stateless control plane
- ✅ REST and JDBC endpoints for SQL submission
- ✅ Session management for multi-statement workflows
- ✅ Production-ready deployment pattern


## 📊 Scaling Components: JM/TM Clusters

### Component Scaling Strategies

#### 1. **Flink JobManager (JM)** - Control Plane

**Role**: Coordinates jobs, schedules tasks, maintains job graph, handles checkpoints

**Scaling approach**: 
- **Session Cluster**: Single JM for multiple jobs (shared resources)
- **Application Cluster**: One JM per application (isolated resources)
- **High Availability**: Multiple JM instances with ZooKeeper/Kubernetes HA

```yaml
# Session cluster (most common)
jobmanager:
  replicas: 1  # Single JM
  resources:
    cpu: "2"
    memory: "2Gi"
  highAvailability:
    enabled: true
    storageDir: "s3://flink-ha/checkpoints"
```

**When to scale JM**:
- ❌ Don't scale horizontally (only one active JM per cluster)
- ✅ Scale vertically for more jobs or complex job graphs
- ✅ Enable HA for production (standby JMs)

#### 2. **Flink TaskManager (TM)** - Data Plane

**Role**: Execute operators, manage state, process data streams

**Scaling approach**:
- **Horizontal**: Add more TM instances
- **Vertical**: Increase TM memory/CPU
- **Auto-scaling**: Reactive mode (Flink 2.1.0+)

```yaml
# TaskManager horizontal scaling
taskmanager:
  replicas: 5  # Start with 5 TMs
  resources:
    cpu: "4"
    memory: "8Gi"
  numberOfTaskSlots: 4  # 4 slots per TM = 20 total slots
```

**Scaling formula**:
```
Total Parallelism = TM Replicas × Task Slots per TM
Example: 5 TMs × 4 slots = 20 parallel tasks
```

**When to scale TM**:
- ✅ Job parallelism increases
- ✅ Throughput requirements grow
- ✅ State size increases (scale vertically for memory)
- ✅ Backpressure detected (add more TMs)

#### 3. **Flink.JobGateway** - Submission Service

**Role**: Submit jobs, monitor status, normalize metrics

**Scaling approach**:
- Stateless service - scale horizontally behind load balancer
- No data processing - lightweight resource requirements

```yaml
jobgateway:
  replicas: 3  # Multiple replicas for HA
  resources:
    cpu: "1"
    memory: "512Mi"
```

**When to scale**:
- ✅ High submission rate (many concurrent job submissions)
- ✅ High availability requirements
- ❌ Don't scale for data throughput (TMs handle that)

#### 4. **Flink SQL Gateway** - SQL Submission Service

**Role**: Accept SQL queries, compile to Flink jobs, forward to cluster

**Scaling approach**:
- Stateless control-plane - scale horizontally
- No data processing

```yaml
sqlgateway:
  replicas: 2  # Multiple replicas for load distribution
  resources:
    cpu: "1"
    memory: "1Gi"
  config:
    sql-gateway.endpoint.type: remote
    rest.address: flink-jobmanager
    rest.port: 8081
```

**When to scale**:
- ✅ High SQL query submission rate
- ✅ Many concurrent sessions
- ❌ Don't scale for data volume (JM/TMs handle execution)

### Complete Scaling Example

**Scenario**: Processing 1M events/sec from Kafka

```yaml
# Kafka cluster (data source)
kafka:
  brokers: 3
  partitions: 20  # High parallelism

# Flink cluster sizing
flink:
  jobmanager:
    replicas: 1  # With HA standby
    resources:
      cpu: "4"
      memory: "4Gi"
  
  taskmanager:
    replicas: 10  # Scale horizontally
    resources:
      cpu: "8"
      memory: "16Gi"
    taskSlots: 4
    # Total parallelism: 10 TMs × 4 slots = 40 parallel tasks
  
  config:
    parallelism.default: 20  # Match Kafka partitions
    state.backend: rocksdb
    state.checkpoints.dir: "s3://flink/checkpoints"

# Gateway services (control-plane)
jobgateway:
  replicas: 3
  resources:
    cpu: "1"
    memory: "512Mi"

sqlgateway:
  replicas: 2
  resources:
    cpu: "1"
    memory: "1Gi"
```

**Scaling decision tree**:
1. **Throughput issues?** → Scale TM horizontally (add replicas)
2. **State too large?** → Scale TM vertically (more memory)
3. **Job submission slow?** → Scale JobGateway/SQL Gateway
4. **Complex job graphs?** → Scale JM vertically
5. **Need HA?** → Enable JM HA with standby instances

### Monitoring Scaling Effectiveness

```csharp
// FlinkDotNet provides metrics to guide scaling decisions
var metrics = await flinkGateway.GetJobMetricsAsync(jobId);

if (metrics.Backpressure > 0.8) {
    // High backpressure → need more TM capacity
    Console.WriteLine("⚠️ Scale TaskManagers horizontally");
}

if (metrics.CheckpointDuration > TimeSpan.FromMinutes(5)) {
    // Slow checkpoints → state too large or TM underpowered
    Console.WriteLine("⚠️ Scale TaskManagers vertically (more memory)");
}

if (metrics.TaskUtilization < 0.3) {
    // Low utilization → over-provisioned
    Console.WriteLine("ℹ️ Consider scaling down TaskManagers");
}
```

## 📚 Learning Course

**New to FlinkDotNet?** Visit our comprehensive [`LearningCourse/`](./LearningCourse/) to understand FlinkDotNet from fundamentals to advanced enterprise patterns. The course includes 15 days of hands-on exercises covering:

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

## 🔄 Stream Processing: Kafka Streams vs Apache Flink

When choosing a stream processing framework, the decision isn't between Kafka (the message broker) and Flink, but rather between **Kafka Streams** and **Apache Flink** as processing engines. Both commonly use Kafka as the underlying message transport.

### **Kafka Streams vs Apache Flink Comparison:**

| **Capability** | **Kafka Streams** | **Apache Flink (via FlinkDotNet)** |
|----------------|-------------------|-------------------------------------|
| **Deployment Model** | Embedded library (runs within your application) | Standalone cluster (JobManager + TaskManagers) |
| **Language Support** | Java/Scala native | Java/Scala native + .NET via FlinkDotNet |
| **Learning Curve** | Lower - simpler operational model | Higher - requires cluster management knowledge |
| **Scale Limits** | Moderate (< 100K events/sec typically) | Massive (millions of events/sec) |
| **State Management** | Local state stores (RocksDB) per instance | Distributed state with savepoints/checkpoints |
| **Event-Time Processing** | Basic event-time windowing | Advanced event-time, watermarks, late data handling |
| **Exactly-Once Semantics** | Within Kafka ecosystem only | Across external systems (databases, APIs, etc.) |
| **Complex Event Processing** | Basic windowing and joins | Advanced CEP, pattern matching, complex joins |
| **Batch + Stream** | Streams only | Unified batch and stream processing |
| **Operational Complexity** | Low - auto-scales with Kafka partitions | Medium - requires cluster deployment |
| **Resource Efficiency** | High for small/medium scale | Optimized for large-scale workloads |
| **Fault Tolerance** | Application restart required | Automatic recovery with state restoration |

### **When to Choose Kafka Streams:**

**Ideal for:**
- **Kafka-centric environments** where all data flows through Kafka
- **Simple to moderate complexity** stream transformations and aggregations
- **Small to medium scale** (< 100K events/sec)
- **Microservices architecture** where each service embeds its own processing
- **Quick deployment** without cluster management overhead
- **Java/Scala teams** comfortable with embedded library approach

**Trade-offs:**
- Limited scalability beyond Kafka partition count
- Lacks advanced event-time processing features
- No unified batch/stream processing
- Exactly-once guarantees only within Kafka

### **When to Choose Apache Flink (via FlinkDotNet):**

**Ideal for:**
- **Large-scale processing** (> 100K events/sec, up to millions)
- **Complex event processing** requirements (CEP, pattern matching, multi-stream joins)
- **Advanced event-time handling** with watermarks and out-of-order events
- **Exactly-once guarantees** across external systems (databases, APIs, file systems)
- **Unified batch and stream** processing in the same framework
- **.NET ecosystems** leveraging C# and .NET tooling
- **Sophisticated windowing** and stateful computations
- **Production-grade fault tolerance** with savepoints and checkpointing

**Trade-offs:**
- Higher operational complexity (cluster deployment required)
- Steeper learning curve
- More infrastructure overhead for small-scale use cases

### **Decision Matrix:**

```
Scale & Complexity:
  Small scale (<10K events/sec)
  ├─ Kafka-centric → Kafka Streams
  └─ Multi-system → Consider simpler alternatives
  
  Medium scale (10K-100K events/sec)
  ├─ Simple transforms → Kafka Streams
  └─ Complex processing → Apache Flink
  
  Large scale (>100K events/sec)
  └─ Apache Flink (mandatory for performance)

Feature Requirements:
  Basic windowing & aggregations → Kafka Streams
  Advanced CEP & pattern matching → Apache Flink
  Exactly-once to external systems → Apache Flink
  Batch + Stream unified → Apache Flink
  
Language & Team:
  Java/Scala only → Either
  .NET primary → FlinkDotNet
  Embedded in app → Kafka Streams
  Centralized cluster → Apache Flink
```

### **FlinkDotNet + Temporal Architecture:**

FlinkDotNet combines Apache Flink's processing power with Temporal's orchestration capabilities:

```
Kafka (Data Highway) + FlinkDotNet (Processing Engine) + Temporal (Orchestration)
    ↓                        ↓                              ↓
Stream Transport     →   Real-time Processing      →   Durable Workflows
Partitioned Topics   →   Windowing/Aggregations    →   Multi-step Coordination  
At-least-once       →   Exactly-once Processing   →   Workflow Guarantees
```

**Benefits of the combined stack:**
- **Flink** handles large-scale, complex stream processing
- **Temporal** orchestrates long-running workflows and cross-system coordination
- **Kafka** provides reliable message transport
- **.NET** enables full-stack development in C#

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

FlinkDotNet provides a complete enterprise-scale integration solution with multi-layered architecture and enhanced code quality:

### Core Components

#### Job Development Layer
- **.NET SDK (FlinkDotNet.DataStream)**: Complete Apache Flink 2.1.0 streaming API
- **JobBuilder SDK (Flink.JobBuilder)**: Fluent C# DSL with comprehensive validation
  - *Enhanced JobDefinitionValidator*: Modular validation with improved error messages
  - *Cognitive complexity optimized*: All validation methods under 15 complexity threshold
  - *Maintainable structure*: Large methods split into focused, testable components
- **Intermediate Representation (IR)**: JSON-based job definitions with robust validation
- **Job Gateway**: HTTP service with improved error handling and metrics collection
  - *Restructured FlinkJobManager*: Builder pattern for metrics collection
  - *Enhanced fault tolerance*: Comprehensive error handling and logging
  - *Improved maintainability*: Complex methods split into focused responsibilities

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
├── Flink.JobBuilder/             # Enhanced fluent DSL
│   ├── FlinkJobBuilder          # Main fluent DSL
│   ├── Services/                # Enhanced validation and job management
│   │   └── JobDefinitionValidator # Modular validation with cognitive complexity <15
│   ├── Models                   # JobDefinition, IR models with robust validation
│   ├── Backpressure/            # Enhanced rate limiting with null-safety
│   │   └── LagBasedRateLimiter  # Improved null-safe rate limiting implementation
│   └── Extensions              # Extension methods
├── Flink.JobGateway/            # Enhanced gateway service
│   ├── Services/                # Improved job management
│   │   └── FlinkJobManager      # Restructured with builder pattern and error handling
│   └── Models                   # Enhanced result types and metrics
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

### .NET 9.0 Requirements

FlinkDotNet implements comprehensive build and test validation to ensure code quality and prevent build failures.

### Quick Setup
```bash
# Verify .NET 9.0 requirement
dotnet --version  # Must show 9.0.x

# Run complete validation
./validate-build-and-tests.ps1

# Quick build check (skip tests)
./validate-build-and-tests.ps1 -SkipTests
```

### Enforcement Rules
- ✅ **ALL builds MUST pass** before commits/merges
- ✅ **.NET 9.0.x** required for all development
- ✅ **Three solutions** validated: FlinkDotNet, Sample, LocalTesting
- ✅ **Automated blocking** of build failures via GitHub Actions

### Pre-Commit Validation
```bash
# Always run before committing
./pre-commit-validation.ps1
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

### **LocalTesting Integration Tests Workflow**

🔗 **[View LocalTesting Test Runs](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)** - Monitor real-time FlinkDotNet integration test execution

The LocalTesting integration tests validate FlinkDotNet functionality with Kafka and Flink:
- **Kafka Integration**: Producer and consumer functionality with Kafka broker
- **Flink Job Submission**: FlinkDotNet job definitions submitted through Job Gateway  
- **FlinkDotNet Pipeline**: End-to-end stream processing using FlinkDotNet API
- **Infrastructure Health**: Aspire orchestration, Flink JobManager, and Job Gateway connectivity

### **BackPressure Integration Tests Workflow**

🔗 **[View BackPressure Test Runs](https://github.com/devstress/FlinkDotnet/actions/workflows/backpressure-integration-tests.yml)** - Monitor real-time backpressure and performance test execution

The BackPressure integration tests validate high-throughput message processing capabilities:
- **Kafka Performance**: High-volume message production and consumption testing
- **System Configuration**: Optimal Kafka performance settings for containerized environments
- **Backpressure Handling**: Stress testing with configurable message volumes
- **Performance Metrics**: Throughput measurement and bottleneck identification

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

## ✅ Verifying FlinkDotNet Installation

### Running LocalTesting Integration Tests

This section provides detailed documentation for developers who want to understand the integration test architecture, run specific test categories, or troubleshoot test execution.

### Test Suite Details

The **LocalTesting** project provides **10 comprehensive integration tests** organized into four categories:

##### 🔧 Gateway Pattern Tests (7 tests)
Tests that validate FlinkDotNet job submission through the [`Flink.JobGateway`](FlinkDotNet/Flink.JobGateway/) service:

1. **[`Gateway_Pattern1_Uppercase_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:23)** - Validates basic map transformation (string → uppercase)
   - **Proves**: FlinkDotNet can submit simple transformation jobs through the Gateway
   - **Flow**: Input messages → Uppercase transformation → Output validation
   - **Expected**: 2 input messages become 2 uppercased output messages

2. **[`Gateway_Pattern2_Filter_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:36)** - Validates filtering operations
   - **Proves**: FlinkDotNet filter operations work correctly
   - **Flow**: Mixed messages (some empty) → Filter non-empty → Output validation
   - **Expected**: 5 input messages (3 non-empty) become 3 output messages

3. **[`Gateway_Pattern3_SplitConcat_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:48)** - Validates flatMap and aggregation
   - **Proves**: FlinkDotNet can handle split/concat operations
   - **Flow**: Comma-separated input → Split → Concat → Output validation
   - **Expected**: 1 input message "a,b" becomes 1 concatenated output

4. **[`Gateway_Pattern4_Timer_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:62)** - Validates stateful processing with timers
   - **Proves**: FlinkDotNet supports stateful operations and event time processing
   - **Flow**: Input messages → Timer-based processing → Output validation
   - **Expected**: 2 input messages processed with timing constraints

5. **[`Gateway_Pattern5_DirectFlinkSQL_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:76)** - Validates Flink SQL via TableEnvironment
   - **Proves**: FlinkDotNet can execute native Flink SQL through JobManager REST API
   - **Flow**: JSON input → SQL transformation → Output validation
   - **Expected**: 1 JSON message processed via SQL query

6. **[`Gateway_Pattern6_SqlTransform_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:90)** - Validates SQL transformations
   - **Proves**: FlinkDotNet SQL transformation pipeline works end-to-end
   - **Flow**: JSON input → SQL SELECT/WHERE → Output validation
   - **Expected**: 1 JSON message transformed via SQL

7. **[`Gateway_Pattern7_Composite_ShouldWork`](LocalTesting/LocalTesting.IntegrationTests/GatewayAllPatternsTests.cs:104)** - Validates complex multi-step operations
   - **Proves**: FlinkDotNet can chain multiple operations (split, filter, concat, timer)
   - **Flow**: Input → Split → Filter → Concat → Timer → Output validation
   - **Expected**: 1 complex input message produces 1 fully processed output

##### 🏗️ Native Flink Pattern Test (1 test)
Direct Apache Flink validation independent of Gateway:

8. **[`Pattern1_Uppercase_ShouldTransformMessages`](LocalTesting/LocalTesting.IntegrationTests/NativeFlinkAllPatternsTests.cs:29)** - Validates native Flink job execution
   - **Proves**: Aspire infrastructure and Flink cluster work correctly
   - **Flow**: Native Flink JAR → Direct JobManager submission → Kafka processing
   - **Expected**: 2 input messages become 2 uppercased outputs via native Flink

##### 🔄 Temporal Workflow Integration Test (1 test)
Validates enterprise workflow orchestration with Temporal:

9. **[`Temporal_BizTalkStyleOrchestration_ComplexOrderProcessing`](LocalTesting/LocalTesting.IntegrationTests/TemporalIntegrationTests.cs:16)** - Validates Temporal workflow orchestration with Kafka and FlinkDotNet
   - **Proves**: Complete integration of Temporal workflows with Kafka messaging and FlinkDotNet availability
   - **Flow**: Temporal workflow → Order processing → Kafka integration → FlinkDotNet health checks
   - **Architecture**: Demonstrates BizTalk-style orchestration using Temporal for multi-step business workflows
   - **Integration Points**:
     - **Temporal Workflows**: Durable workflow execution with exactly-once guarantees
     - **Kafka Integration**: Async message processing through Kafka topics
     - **FlinkDotNet Availability**: Health checks for JobManager and Gateway services
     - **Infrastructure Validation**: Kafka connectivity and Temporal server status
   - **Expected**: Order workflow executes successfully with all infrastructure components healthy

##### 🔍 Infrastructure Validation Test (1 test)
Validates core infrastructure components:

10. **[`AspireValidationTest`](LocalTesting/LocalTesting.IntegrationTests/AspireValidationTest.cs:16)** - Validates all service connectivity
    - **Proves**: Aspire orchestration, Kafka, Flink JobManager, and Gateway are accessible
    - **Validates**: Service health checks, port mappings, container networking
    - **Expected**: All services respond with healthy status

#### Running the Tests Locally

**Prerequisites:**
- .NET 9.0 SDK installed
- Docker Desktop running (or Podman with Docker compatibility)
- Aspire workload installed: `dotnet workload install aspire`

**Execute all integration tests:**
```bash
# From repository root
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --configuration Release
```

**Run specific test categories:**
```bash
# Gateway pattern tests only (7 tests)
dotnet test LocalTesting.IntegrationTests --filter "Category=gateway-patterns"

# Native Flink tests only (1 test)
dotnet test LocalTesting.IntegrationTests --filter "Category=native-flink-patterns"

# Temporal integration tests only (1 test)
dotnet test LocalTesting.IntegrationTests --filter "Category=temporal-integration"
```

**View test results in real-time:**
```bash
dotnet test LocalTesting.IntegrationTests --logger "console;verbosity=detailed"
```

#### GitHub Actions Integration Test Workflow

The integration tests run automatically on every push via GitHub Actions. View test execution and results:

🔗 **[LocalTesting Integration Tests Workflow](https://github.com/devstress/FlinkDotnet/actions/workflows/localtesting-integration-tests.yml)** - Monitor live test execution and historical results

**Workflow validates:**
- ✅ All 10 integration tests pass in CI environment
- ✅ Docker container orchestration via Aspire
- ✅ Kafka → Flink → FlinkDotNet complete pipeline
- ✅ Cross-platform compatibility (Ubuntu)
- ✅ Performance and reliability under CI constraints

**CI Environment specifics:**
- **Platform**: Ubuntu latest with Docker pre-installed
- **.NET Version**: 9.0.x with Aspire workload
- **Java Version**: JDK 17 (Temurin distribution)
- **Timeout**: 20 minutes for complete test suite
- **Parallelization**: Tests run in parallel with shared infrastructure (8 TaskManager slots)

#### What These Tests Prove

**✅ Complete Pipeline Validation:**
- Kafka message production and consumption works correctly
- Flink cluster (JobManager + TaskManagers) processes jobs successfully
- FlinkDotNet Gateway submits and monitors jobs correctly
- End-to-end data flow from input → transformation → output

**✅ FlinkDotNet Feature Coverage:**
- **Basic transformations**: Map, filter, flatMap operations
- **Stateful processing**: Timers, event time processing
- **SQL support**: Native Flink SQL via TableEnvironment and SQL Gateway
- **Complex pipelines**: Multi-step transformations with composite operations
- **Infrastructure**: Aspire orchestration, container networking, service discovery

**✅ Production Readiness:**
- Tests run in parallel (simulating production load)
- Shared infrastructure with 8 TaskManager slots (resource efficiency)
- Automatic cleanup and container management
- CI/CD integration for continuous validation

#### Test Architecture

```
┌─────────────────────────────────────────────────────────────┐
│              LocalTesting Integration Tests                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  GlobalTestInfrastructure (OneTimeSetUp)            │   │
│  │  • Starts Aspire AppHost once                        │   │
│  │  • Discovers Kafka/Flink endpoints dynamically       │   │
│  │  • Validates all services are healthy               │   │
│  │  • Shared across all tests (performance)            │   │
│  └──────────────────────────────────────────────────────┘   │
│                           ↓                                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  10 Parallel Integration Tests                       │   │
│  │  • Gateway Patterns (7 tests)                        │   │
│  │  • Native Flink Pattern (1 test)                     │   │
│  │  • Temporal Workflow Integration (1 test)            │   │
│  │  • Infrastructure Validation (1 test)                │   │
│  └──────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────┘
                           ↓
┌─────────────────────────────────────────────────────────────┐
│                   Aspire Infrastructure                      │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Kafka   │  │  Flink   │  │ Gateway  │  │ Aspire   │   │
│  │  Broker  │  │ Cluster  │  │ Service  │  │Dashboard │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└─────────────────────────────────────────────────────────────┘
```

#### Troubleshooting

**Docker not available:**
```bash
# Verify Docker is running
docker ps

# On Windows: Ensure Docker Desktop is started
# On Linux: sudo systemctl start docker
```

**Aspire workload missing:**
```bash
# Install Aspire workload
dotnet workload install aspire

# Verify installation
dotnet workload list
```

**Test failures:**
```bash
# Enable detailed logging
dotnet test LocalTesting.IntegrationTests --logger "console;verbosity=detailed"

# Check Aspire dashboard for service status
# http://localhost:15888 (started automatically during tests)
```

**Port conflicts:**
The tests use dynamic port assignment, but if you have conflicts:
- Kafka: Default port 9092
- Flink JobManager: Default port 8081
- Gateway: Default port 8080
- Aspire Dashboard: Default port 15888

Stop any conflicting services or containers before running tests.

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
