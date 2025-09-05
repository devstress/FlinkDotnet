# LocalTesting: A Complete Beginner's Guide to Stream Processing 🚀

> **"I just want to understand what all this code does!"** - This guide is for you.

## Table of Contents
1. [What is LocalTesting? (The Big Picture)](#what-is-localtesting-the-big-picture)
2. [Real-World Analogies](#real-world-analogies)
3. [Core Components Explained Simply](#core-components-explained-simply)
4. [The Data Journey](#the-data-journey)
5. [Code Examples Walkthrough](#code-examples-walkthrough)
6. [Understanding Sources and Sinks](#understanding-sources-and-sinks)
7. [Temporal Workflows Made Simple](#temporal-workflows-made-simple)
8. [Monitoring and Observability](#monitoring-and-observability)
9. [Common Questions](#common-questions)
10. [Getting Started](#getting-started)

---

## What is LocalTesting? (The Big Picture)

LocalTesting is like a **miniature data processing factory** running on your computer. Imagine you have a factory that:

- 📥 **Receives** thousands of orders per second
- 🔄 **Processes** each order (validates, enriches, transforms)
- 📤 **Sends** results to different destinations
- 👀 **Monitors** everything in real-time
- 🛠️ **Self-manages** when things go wrong

That's exactly what LocalTesting does, but with data instead of physical products!

### Why Does This Matter?

In the real world, companies need to process millions of events per second:
- 🏦 **Banks**: Processing credit card transactions
- 🚗 **Uber**: Matching riders with drivers
- 📱 **Netflix**: Recommending movies based on viewing patterns
- 🛒 **Amazon**: Updating inventory and recommendations

LocalTesting lets you build and test these kinds of systems on your laptop!

---

## Real-World Analogies

### 🏭 The Factory Analogy

Think of LocalTesting as a **smart factory**:

| Component | Factory Equivalent | What It Does |
|-----------|-------------------|--------------|
| **Kafka** | Conveyor Belt System | Moves messages/data between stations |
| **Flink** | Processing Stations | Transforms, filters, and analyzes data |
| **Temporal** | Factory Manager | Coordinates complex multi-step processes |
| **Sources** | Raw Material Inputs | Where data comes from (files, databases, APIs) |
| **Sinks** | Finished Goods Outputs | Where results go (databases, files, other systems) |
| **Observability** | Security Cameras & Dashboards | Monitors everything happening |

### 🍕 The Pizza Restaurant Analogy

Imagine a pizza restaurant processing orders:

1. **Orders come in** (Sources) - Phone, app, walk-ins
2. **Conveyor belt** (Kafka) - Orders flow to kitchen
3. **Kitchen stations** (Flink) - Prep, cook, package
4. **Manager** (Temporal) - Handles complex situations (large orders, complaints)
5. **Delivery** (Sinks) - Orders go to customers
6. **Monitoring** (Observability) - Track wait times, kitchen efficiency

---

## Core Components Explained Simply

### 🔀 Kafka: The Message Highway

**What it is**: A super-fast messaging system that can handle millions of messages per second.

**Real-world comparison**: Like a multi-lane highway where cars (messages) flow from one place to another.

**In LocalTesting**:
```
🏢 Producer App → [Kafka Topic] → 🏭 Consumer App
```

**Key concepts**:
- **Topics**: Like different highway routes (e.g., "user-orders", "payment-events")
- **Partitions**: Multiple lanes on the same highway for parallel processing
- **Producers**: Apps that send messages
- **Consumers**: Apps that receive and process messages

**Example**: 
```
Topic: "pizza-orders"
Message: {"orderId": 123, "pizza": "margherita", "customer": "John"}
```

### ⚡ Flink: The Smart Processor

**What it is**: A stream processing engine that analyzes data as it flows.

**Real-world comparison**: Like a smart filter/transformer on an assembly line that can:
- Filter out defective items
- Add new information
- Count and aggregate
- Detect patterns

**In LocalTesting**:
```
Kafka Messages → [Flink Processing] → Processed Results
```

**What Flink can do**:
```csharp
// Example: Filter high-value orders
var highValueOrders = orderStream
    .Filter(order => order.Amount > 100)
    .Map(order => new { order.Id, order.Amount, Priority = "HIGH" });
```

**Key concepts**:
- **DataStream**: A continuous flow of data
- **Operators**: Functions that transform data (filter, map, reduce)
- **Parallelism**: Processing multiple messages simultaneously
- **State**: Remembering information between messages

### 🔄 Temporal: The Workflow Orchestrator

**What it is**: A system that manages complex, long-running business processes.

**Real-world comparison**: Like a project manager who:
- Tracks multi-step processes
- Handles failures and retries
- Coordinates between teams
- Ensures nothing gets lost

**Example workflow**: E-commerce order processing
```
1. Validate payment ✅
2. Reserve inventory ✅  
3. Create shipment ⏳ (in progress)
4. Send confirmation 📧 (waiting)
5. Track delivery 🚚 (pending)
```

**Why it's powerful**:
- **Durable**: Survives server crashes
- **Fault-tolerant**: Automatically retries failed steps
- **Observable**: You can see exactly what's happening
- **Scalable**: Handles millions of workflows

### 📊 Observability: The Monitoring Dashboard

**What it is**: Tools that let you see what's happening inside your system.

**Components**:
- **Prometheus**: Collects metrics (like a car's dashboard)
- **Grafana**: Creates visual dashboards and charts
- **Loki**: Collects and searches log messages
- **OpenTelemetry**: Tracks requests across services

**What you can see**:
- 📈 Messages per second
- ⏱️ Processing latency
- 🚨 Error rates
- 💾 Memory usage
- 🔄 Workflow status

---

## The Data Journey

Let's follow a single message through the entire LocalTesting system:

### Step 1: Message Arrives 📨
```
A sensor reading arrives: {"sensorId": "temp_01", "temperature": 23.5, "timestamp": "2024-01-15T10:30:00Z"}
```

### Step 2: Kafka Receives and Routes 🚛
```
Message goes to topic: "sensor-readings"
Partition: 3 (based on sensorId hash)
```

### Step 3: Flink Processes 🔄
```csharp
// Real code from LocalWorkingExample.cs
var sensorStream = env.FromCollection(sensorData);

// Filter high temperature readings
var highTempReadings = sensorStream.Filter(reading => reading.Temperature > 25.0);

// Group by sensor ID
var groupedBySensor = highTempReadings.KeyBy(reading => reading.SensorId);
```

### Step 4: Temporal Workflow (if needed) 🔄
```
If temperature > 30°C:
  1. Send alert to maintenance team
  2. Check if this is recurring issue
  3. Create work order if needed
  4. Update monitoring dashboard
```

### Step 5: Results Go to Sink 📤
```
Results saved to:
- Database (for historical analysis)
- Alert system (for immediate action)
- Dashboard (for real-time monitoring)
```

### Step 6: Monitoring Captures Everything 👀
```
Metrics recorded:
- Message processing time: 2ms
- Workflow execution time: 150ms
- Memory usage: 45MB
- Success rate: 99.9%
```

---

## Code Examples Walkthrough

### Basic Flink Example

Let's look at real code from `LocalWorkingExample.cs`:

```csharp
// 1. Create Flink execution environment
var env = FlinkDotNet.Flink.GetExecutionEnvironment();
env.SetParallelism(4); // Use 4 parallel threads

// 2. Create sample data (like sensor readings)
var sensorData = new[]
{
    new { SensorId = "sensor1", Temperature = 20.5, Timestamp = DateTime.Now },
    new { SensorId = "sensor2", Temperature = 25.3, Timestamp = DateTime.Now },
    new { SensorId = "sensor1", Temperature = 30.1, Timestamp = DateTime.Now }
};

// 3. Create a data stream from the collection
var sensorStream = env.FromCollection(sensorData);

// 4. Filter out readings below 25°C
var highTempReadings = sensorStream.Filter(reading => reading.Temperature > 25.0);

// 5. Group readings by sensor ID
var groupedBySensor = highTempReadings.KeyBy(reading => reading.SensorId);

// 6. Print results to console
highTempReadings.Print();

// 7. Execute the processing pipeline
var result = await env.ExecuteAsync("Temperature Processing Example");
```

**What this does**:
1. Sets up Flink to use 4 parallel processors
2. Creates fake sensor data (in real life, this comes from Kafka)
3. Filters out normal temperatures (only high temps matter)
4. Groups readings by which sensor they came from
5. Prints the results
6. Runs the entire pipeline

### Sources: Where Data Comes From

From `JobDefinition.cs`, here are the different ways to get data:

#### Kafka Source (Most Common)
```csharp
public class KafkaSourceDefinition : ISourceDefinition
{
    public string Type => "kafka";
    public string Topic { get; set; } = "sensor-readings";        // Which topic to read from
    public string BootstrapServers { get; set; } = "localhost:9092"; // Kafka server address
    public string GroupId { get; set; } = "my-consumer-group";    // Consumer group ID
    public string StartingOffsets { get; set; } = "latest";       // Start from newest messages
}
```

#### File Source (For Batch Processing)
```csharp
public class FileSourceDefinition : ISourceDefinition
{
    public string Type => "file";
    public string Path { get; set; } = "/data/sensor-readings.json"; // File path
    public string Format { get; set; } = "json";                    // File format
}
```

#### Database Source (For Traditional Data)
```csharp
public class DatabaseSourceDefinition : ISourceDefinition
{
    public string Type => "database";
    public string ConnectionString { get; set; } = "Host=localhost;Database=sensors;"; 
    public string Query { get; set; } = "SELECT * FROM readings WHERE created_at > NOW() - INTERVAL '1 hour'";
    public int PollingIntervalSeconds { get; set; } = 30; // Check every 30 seconds
}
```

### Sinks: Where Results Go

#### Kafka Sink (Send to Another Topic)
```csharp
public class KafkaSinkDefinition : ISinkDefinition
{
    public string Type => "kafka";
    public string Topic { get; set; } = "processed-readings";      // Destination topic
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string Serializer { get; set; } = "json";              // How to format messages
}
```

#### Database Sink (Save to Database)
```csharp
public class DatabaseSinkDefinition : ISinkDefinition
{
    public string Type => "database";
    public string ConnectionString { get; set; } = "Host=localhost;Database=results;";
    public string Table { get; set; } = "processed_readings";      // Table name
    public string DatabaseType { get; set; } = "postgresql";       // Database type
}
```

#### File Sink (Save to File)
```csharp
public class FileSinkDefinition : ISinkDefinition
{
    public string Type => "file";
    public string Path { get; set; } = "/output/results.json";     // Output file path
    public string Format { get; set; } = "json";                   // Output format
}
```

---

## Understanding Sources and Sinks

### Sources: Data Input Points

Think of sources as **data faucets** - they control how data flows into your system.

**Common Source Types**:

1. **Real-time Sources** (streaming data):
   - Kafka topics
   - Message queues
   - IoT sensor streams
   - Web APIs

2. **Batch Sources** (historical data):
   - Files (CSV, JSON, Parquet)
   - Databases
   - Data warehouses

**Example Source Configuration**:
```yaml
# A Kafka source reading user events
source:
  type: kafka
  topic: user-events
  bootstrap_servers: localhost:9092
  starting_offsets: earliest  # Read all historical data
  properties:
    security.protocol: PLAINTEXT
```

### Sinks: Data Output Points

Think of sinks as **data destinations** - they control where processed data goes.

**Common Sink Types**:

1. **Real-time Sinks**:
   - Kafka topics (for downstream processing)
   - Alert systems
   - Real-time dashboards

2. **Storage Sinks**:
   - Databases (PostgreSQL, MySQL)
   - Data warehouses (Snowflake, BigQuery)
   - Files (for backup/analysis)

3. **Action Sinks**:
   - Email notifications
   - API calls
   - External services

**Example Sink Configuration**:
```yaml
# A database sink saving processed results
sink:
  type: database
  connection_string: "postgresql://user:pass@localhost:5432/results"
  table: user_behavior_summary
  batch_size: 1000  # Insert 1000 records at a time
```

### Source-to-Sink Pipeline Example

Here's a complete data pipeline:

```
📱 Mobile App Events (Source)
    ↓
🔀 Kafka Topic: "user-events"
    ↓
⚡ Flink Processing:
   - Filter active users
   - Calculate session duration
   - Detect unusual behavior
    ↓
📊 Multiple Sinks:
   - Database: Store user sessions
   - Kafka: Send alerts for unusual behavior
   - File: Backup raw events
```

**Flink Code**:
```csharp
// Source: Read from Kafka
var userEvents = env.AddSource(new KafkaSource<UserEvent>("user-events"));

// Processing: Calculate session duration
var sessions = userEvents
    .KeyBy(e => e.UserId)
    .Window(TimeWindow.of(Time.minutes(30)))
    .Aggregate(new SessionAggregator());

// Sinks: Send to multiple destinations
sessions.AddSink(new DatabaseSink("user_sessions"));     // Store in database
sessions.Filter(s => s.IsUnusual)
        .AddSink(new KafkaSink("security-alerts"));      // Send alerts
```

---

## Temporal Workflows Made Simple

### What Are Workflows?

Workflows are like **recipes for complex processes**. They define step-by-step instructions for handling business logic that spans multiple services and can take a long time.

### Real-World Workflow Examples

#### 1. E-commerce Order Processing
```
Workflow: "ProcessOrder"
Steps:
1. Validate payment method ✅
2. Check inventory availability ✅
3. Reserve items ✅
4. Charge payment card ⏳ (retrying...)
5. Create shipping label 📋 (waiting)
6. Send confirmation email 📧 (waiting)
7. Update inventory 📦 (waiting)
```

#### 2. User Onboarding
```
Workflow: "OnboardNewUser"  
Steps:
1. Send welcome email ✅
2. Wait 24 hours ⏰
3. Send tutorial email ✅  
4. Wait 3 days ⏰
5. Send feature highlight ⏳ (in progress)
6. Wait 1 week ⏰
7. Request feedback 📋 (scheduled)
```

### Temporal Workflow Interfaces

From `IClusterWorkflows.cs`, here are the main workflow types:

#### Cluster Orchestration Workflow
```csharp
public interface IClusterOrchestratorWorkflow
{
    // Manages multiple Flink clusters
    Task OrchestrateClustersAsync(OrchestrationRequest request);
}
```

**What it does**: Like a **data center manager** that:
- Starts new Flink clusters when needed
- Balances workload across clusters  
- Shuts down unused clusters to save resources
- Handles cluster failures

#### Job Distribution Workflow
```csharp
public interface IJobDistributionWorkflow
{
    // Distributes processing jobs across clusters
    Task<JobDistributionResult> DistributeJobsAsync(List<FlinkJobDefinition> jobs, SubmissionStrategy strategy);
}
```

**What it does**: Like a **smart scheduler** that:
- Takes a list of data processing jobs
- Finds the best cluster for each job
- Monitors job execution
- Redistributes jobs if clusters fail

#### Auto-Scaling Workflow
```csharp
public interface IAutoScalingWorkflow
{
    // Automatically scales clusters based on load
    Task AutoScaleClustersAsync(AutoScalingConfig config);
}
```

**What it does**: Like an **automatic capacity manager** that:
- Monitors how busy each cluster is
- Adds more processing power when busy
- Removes extra capacity when quiet
- Saves money by right-sizing resources

### Workflow Activities

Activities are the **individual steps** within a workflow. From the documentation:

#### RealActivity1 - Resource Provisioning
```csharp
[Activity("ProvisionResources")]
public static async Task ProvisionResourcesAsync(ClusterConfig config)
{
    // Creates cloud infrastructure (VMs, networks, storage)
    // Configures security groups and access policies  
    // Sets up monitoring and logging agents
}
```

#### RealActivity2 - Configuration Management  
```csharp
[Activity("ConfigureCluster")]
public static async Task ConfigureClusterAsync(ClusterConfig config)
{
    // Applies Flink cluster configurations
    // Manages application-specific settings
    // Handles configuration validation and rollback
}
```

### Workflow Benefits

**Why use workflows instead of simple code?**

1. **Durability**: Workflows survive server crashes
   ```
   ❌ Simple code: Server crash = lost progress
   ✅ Temporal: Server crash = resume from last step
   ```

2. **Visibility**: You can see exactly what's happening
   ```
   ❌ Simple code: Black box, hard to debug
   ✅ Temporal: Full execution history and current state
   ```

3. **Reliability**: Automatic retries and error handling
   ```
   ❌ Simple code: Manual retry logic, easy to miss edge cases
   ✅ Temporal: Built-in retry policies and failure handling
   ```

4. **Scalability**: Handles millions of workflows
   ```
   ❌ Simple code: Database polling, limited scale
   ✅ Temporal: Event-driven, scales horizontally
   ```

---

## Monitoring and Observability

### The Three Pillars of Observability

#### 1. 📊 Metrics (Numbers and Trends)
**Tool**: Prometheus + Grafana

**What you see**:
- Messages processed per second
- Memory and CPU usage
- Error rates and latency
- Workflow completion times

**Example Metrics**:
```
kafka_messages_per_second{topic="user-events"} = 1,250
flink_processing_latency_ms{job="real-job-1"} = 3.2
temporal_workflow_duration_seconds{workflow="ProcessOrder"} = 45.8
jvm_memory_used_bytes{service="flink-taskmanager"} = 512,000,000
```

#### 2. 📝 Logs (Detailed Messages)
**Tool**: Loki

**What you see**:
- Application debug messages
- Error stack traces
- User activity logs
- System events

**Example Logs**:
```
2024-01-15T10:30:15Z INFO [flink-taskmanager-1] Processing message for user_id=12345
2024-01-15T10:30:16Z WARN [flink-taskmanager-1] High latency detected: 150ms > 100ms threshold
2024-01-15T10:30:17Z ERROR [temporal-worker] Payment processing failed: insufficient_funds
```

#### 3. 🔍 Traces (Request Journeys)
**Tool**: OpenTelemetry

**What you see**:
- Complete request path across services
- Time spent in each component
- Where bottlenecks occur

**Example Trace**:
```
Order Processing Trace (Total: 245ms)
├── Kafka Message Received (2ms)
├── Flink Processing (15ms)
├── Temporal Workflow Started (5ms)
│   ├── Validate Payment (45ms)
│   ├── Check Inventory (30ms)
│   └── Reserve Items (25ms)
├── Database Write (80ms)
└── Response Sent (3ms)
```

### Observability URLs

When LocalTesting is running, you can access:

- **Grafana Dashboards**: http://localhost:18010
  - Visual charts and graphs
  - Real-time metrics
  - Custom dashboards

- **Prometheus Metrics**: http://localhost:18006
  - Raw metrics data
  - Query interface
  - Alert configuration

- **Temporal UI**: http://localhost:18004
  - Workflow execution history
  - Current workflow states
  - Error details and retries

- **Kafka UI**: http://localhost:18001
  - Topic management
  - Message browsing
  - Consumer group monitoring

- **Flink Dashboard**: http://localhost:18002
  - Job management
  - Task manager status
  - Checkpoint information

### What to Monitor

#### System Health
```
✅ All services running
✅ Memory usage < 80%
✅ CPU usage < 70%
✅ Disk space available
```

#### Data Flow
```
📊 Message throughput: ~99,000 msg/sec
⏱️ End-to-end latency: < 5ms
🚨 Error rate: < 0.1%
🔄 Backpressure: None
```

#### Workflows
```
⚡ Active workflows: 150
✅ Completed workflows: 50,450
❌ Failed workflows: 23 (0.05%)
⏳ Average duration: 2.3 seconds
```

---

## Common Questions

### Q: What's the difference between Kafka and Flink?

**A**: Think of them as different parts of a assembly line:

- **Kafka** = **Conveyor Belt**: Moves data between systems reliably
- **Flink** = **Processing Station**: Transforms and analyzes data as it flows

```
Data Source → [Kafka] → [Flink] → [Kafka] → Destination
             Transport  Process   Transport
```

### Q: Why do I need Temporal if I have Flink?

**A**: They solve different problems:

- **Flink** = **Fast data processing** (milliseconds, stateless operations)
- **Temporal** = **Complex business logic** (minutes/hours/days, stateful workflows)

**Example**:
- Flink: "Filter messages where temperature > 30°C" (instant)
- Temporal: "When temperature > 30°C, send alert, wait 5 minutes, check if still high, create work order, track completion" (long-running)

### Q: What are Sources and Sinks?

**A**: 
- **Sources** = **Data Inputs** (where data comes FROM)
- **Sinks** = **Data Outputs** (where results GO)

```
[Database] → Source → [Flink Processing] → Sink → [Another Database]
[Kafka]    → Source → [Flink Processing] → Sink → [File]
[File]     → Source → [Flink Processing] → Sink → [Alert System]
```

### Q: How fast is "real-time"?

**A**: In LocalTesting:
- **Kafka**: Delivers messages in microseconds
- **Flink**: Processes messages in 2-5 milliseconds
- **End-to-end**: Total pipeline latency under 10ms

For comparison:
- Human eye blink: 100-150ms
- Mouse click: ~10ms
- LocalTesting processing: 2-10ms ⚡

### Q: What happens if something crashes?

**A**: The system is designed for resilience:

- **Kafka**: Messages are stored on disk, won't lose data
- **Flink**: Automatically restarts from last checkpoint
- **Temporal**: Workflows resume exactly where they left off
- **Containers**: Aspire automatically restarts failed services

### Q: How much data can this handle?

**A**: LocalTesting performance:
- **Kafka**: ~800,000 messages/second (10 partitions × 80k each)
- **Flink**: ~99,000 messages/second processed
- **Temporal**: ~10 workflows/second (complex orchestration)

Real production systems handle millions of messages/second!

### Q: Can I modify the processing logic?

**A**: Yes! Here's how:

1. **Flink Logic**: Modify code in `LocalWorkingExample.cs`
2. **Sources/Sinks**: Update configurations in `JobDefinition.cs`
3. **Workflows**: Implement interfaces in `IClusterWorkflows.cs`
4. **Infrastructure**: Change setup in `Program.cs`

### Q: What programming knowledge do I need?

**A**: To understand:
- **Basic**: C# syntax, async/await, interfaces
- **Helpful**: LINQ operations, dependency injection
- **Advanced**: Distributed systems concepts

To modify:
- **Basic changes**: Update configurations, simple filters
- **Medium changes**: New processing logic, different sources/sinks  
- **Advanced changes**: Custom workflows, performance tuning

---

## Getting Started

### Prerequisites

1. **Install .NET 9.0 SDK**
   ```bash
   # Check version
   dotnet --version  # Should show 9.0.x
   
   # If not installed, download from:
   # https://dotnet.microsoft.com/download/dotnet/9.0
   ```

2. **Install Docker Desktop**
   - Download from: https://www.docker.com/products/docker-desktop
   - Ensure it has at least 8GB RAM allocated
   - Make sure it's running before starting LocalTesting

3. **Clone the Repository**
   ```bash
   git clone https://github.com/devstress/FlinkDotnet.git
   cd FlinkDotnet/LocalTesting
   ```

### Quick Start

1. **Build the Solution**
   ```bash
   dotnet build LocalTesting.sln
   ```

2. **Start LocalTesting**
   ```bash
   cd LocalTesting.AppHost
   dotnet run
   ```

3. **Wait for Startup** (3-5 minutes)
   - Watch the console for service startup messages
   - All components need to initialize and connect

4. **Access the Dashboards**
   ```bash
   # Open these URLs in your browser:
   
   # Aspire Dashboard (main control panel)
   http://localhost:18888
   
   # Grafana (metrics and monitoring)
   http://localhost:18010
   
   # Temporal UI (workflow monitoring)  
   http://localhost:18004
   
   # Kafka UI (message browsing)
   http://localhost:18001
   
   # Flink Dashboard (stream processing)
   http://localhost:18002
   ```

### First Experiments

#### 1. Watch Real-Time Metrics
1. Open Grafana: http://localhost:18010
2. Look for pre-built dashboards
3. Watch metrics update in real-time

#### 2. Explore Kafka Messages
1. Open Kafka UI: http://localhost:18001
2. Browse topics like "ingress-topic"
3. See messages flowing through the system

#### 3. Monitor Workflows
1. Open Temporal UI: http://localhost:18004
2. Look for running workflows
3. Click on workflows to see execution details

#### 4. Modify Processing Logic
1. Edit `IntegrationTests/FlinkJobBuilder.Sample/LocalWorkingExample.cs`
2. Change the filter condition:
   ```csharp
   // Change from > 25.0 to > 20.0
   var highTempReadings = sensorStream.Filter(reading => reading.Temperature > 20.0);
   ```
3. Rebuild and run to see the effect

### Next Steps

#### Learn More About Each Component

1. **Flink Fundamentals**
   - Read: Apache Flink documentation
   - Practice: Modify `LocalWorkingExample.cs`
   - Experiment: Try different operators (map, filter, reduce)

2. **Kafka Deep Dive**
   - Read: Kafka documentation
   - Practice: Create new topics in Kafka UI
   - Experiment: Change partition counts and see effects

3. **Temporal Workflows**
   - Read: Temporal documentation
   - Practice: Examine workflow code in `IClusterWorkflows.cs`
   - Experiment: Create simple workflows

4. **Observability**
   - Read: Prometheus and Grafana documentation
   - Practice: Create custom dashboards
   - Experiment: Add new metrics to the code

#### Advanced Challenges

1. **Add a New Source**
   - Create a file-based source
   - Process CSV data instead of Kafka messages

2. **Create Custom Processing**
   - Implement a moving average calculator
   - Add real-time anomaly detection

3. **Build a Complete Pipeline**
   - Source: Read from database
   - Process: Aggregate by time windows
   - Sink: Write to another database

4. **Design a Workflow**
   - Create a user onboarding workflow
   - Add error handling and retries

### Troubleshooting

#### Common Issues

**"Container failed to start"**
- Ensure Docker Desktop is running
- Check if ports are already in use
- Try: `docker system prune -f --volumes`

**"Connection refused" errors**  
- Wait longer for startup (can take 5+ minutes)
- Check service logs in Aspire dashboard
- Verify all containers are healthy

**".NET version not found"**
- Install .NET 9.0 SDK
- Restart terminal/IDE after installation
- Verify with: `dotnet --version`

**High memory usage**
- Allocate more RAM to Docker (8GB minimum)
- Close other resource-intensive applications
- Monitor memory usage in Grafana

#### Getting Help

1. **Check the Logs**
   - Aspire Dashboard: http://localhost:18888
   - Look for red/error status indicators
   - Click on services to see detailed logs

2. **Monitor Resource Usage**
   - Grafana Dashboard: http://localhost:18010
   - Check CPU, memory, and disk usage
   - Look for resource constraints

3. **Community Resources**
   - Apache Flink documentation
   - Temporal.io documentation
   - Apache Kafka documentation
   - Stack Overflow for specific errors

---

## Conclusion

Congratulations! 🎉 You now understand the core concepts behind LocalTesting:

- **Kafka** moves data reliably between systems
- **Flink** processes streaming data in real-time  
- **Temporal** orchestrates complex, long-running workflows
- **Sources/Sinks** define where data comes from and goes
- **Observability** tools help you monitor everything

LocalTesting gives you a complete data processing platform that handles the same challenges as systems used by Netflix, Uber, and other tech giants. Start experimenting, and you'll quickly see how powerful these tools can be!

**Remember**: This is just the beginning. Modern data processing is a vast field, but you now have a solid foundation to build upon. Happy coding! 🚀

---

## Additional Resources

- [Apache Flink Documentation](https://flink.apache.org/docs/)
- [Temporal Documentation](https://docs.temporal.io/)
- [Apache Kafka Documentation](https://kafka.apache.org/documentation/)
- [.NET Aspire Documentation](https://docs.microsoft.com/en-us/dotnet/aspire/)
- [Grafana Documentation](https://grafana.com/docs/)
- [Prometheus Documentation](https://prometheus.io/docs/)

---

*This guide was created to make complex distributed systems concepts accessible to developers of all skill levels. If you have suggestions for improvements, please contribute!*