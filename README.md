# FlinkDotNet

**FlinkDotNet** is a comprehensive .NET framework that enables developers to build and submit streaming jobs to Apache Flink 2.0 clusters using a fluent C# API. It provides complete compatibility with Apache Flink 2.0 features including dynamic scaling, adaptive scheduling, reactive mode, and enterprise-scale multi-cluster orchestration.

## 🚀 Apache Flink 2.0 Compatibility

FlinkDotNet implements the complete Apache Flink 2.0 feature set for .NET developers, including:

- **Dynamic Scaling**: Change job parallelism without stopping jobs
- **Adaptive Scheduler**: Intelligent resource management and automatic parallelism adjustment
- **Reactive Mode**: Automatic adaptation to available cluster resources
- **Advanced Partitioning**: Rebalance, rescale, forward, shuffle, broadcast, and custom partitioning
- **Savepoint-based Scaling**: Scale jobs using savepoints for state consistency
- **Fine-grained Resource Management**: Slot sharing groups and resource profiles
- **Temporal Multi-cluster Orchestration**: Enterprise-scale coordination across thousands of clusters

## 🔄 Dynamic Scaling and Rebalancing

FlinkDotNet provides comprehensive support for Apache Flink 2.0's dynamic scaling capabilities:

### Partitioning Strategies

```csharp
var env = Flink.GetExecutionEnvironment();

var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

// Rebalance: Uniformly distribute data across all parallel operators
var rebalanced = dataStream
    .Map(x => x * 2)
    .Rebalance()  // Apache Flink 2.0 rebalance operation
    .Filter(x => x > 5);

// Rescale: Distribute to subset of operators (more efficient for different parallelisms)
var rescaled = dataStream
    .Map(x => x * 3)
    .Rescale()   // Apache Flink 2.0 rescale operation
    .Filter(x => x > 10);

// Forward: Direct forwarding (same parallelism required)
var forwarded = dataStream
    .Forward()   // Apache Flink 2.0 forward partitioning
    .Map(x => x + 1);

// Shuffle: Random distribution
var shuffled = dataStream
    .Shuffle()   // Apache Flink 2.0 shuffle partitioning
    .Map(x => x * 2);

// Broadcast: Send to all operators
var broadcasted = dataStream
    .Broadcast() // Apache Flink 2.0 broadcast partitioning
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
   .EnableAdaptiveScheduler()            // Apache Flink 2.0 adaptive scheduler
   .EnableReactiveMode();                // Apache Flink 2.0 reactive mode

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
// Modern DataStream API (Apache Flink 2.0 compatible)
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

## Apache Flink 2.0 Configuration

```csharp
var config = new ExecutionConfig()
    .SetParallelism(8)
    .SetMaxParallelism(128)
    .EnableAdaptiveScheduler()           // Apache Flink 2.0 intelligent scheduling
    .EnableReactiveMode()                // Apache Flink 2.0 elastic scaling
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
- **.NET SDK (FlinkDotNet.DataStream)**: Complete Apache Flink 2.0 streaming API
- **JobBuilder SDK (Flink.JobBuilder)**: Fluent C# DSL for rapid development
- **Intermediate Representation (IR)**: JSON-based job definitions
- **Job Gateway**: HTTP service that bridges .NET applications with Apache Flink clusters

#### FlinkDotNet.Orchestration Layer  
- **FlinkDotNet.Orchestration**: Multi-cluster job orchestration with intelligent placement strategies
- **FlinkDotNet.ClusterManager**: Actor-based cluster lifecycle management  
- **FlinkDotNet.Temporal**: Temporal.io workflow definitions for durable orchestration
- **FlinkDotNet.Resilience**: Circuit breakers, retry policies, and health checkers

### Apache Flink 2.0 Integration Architecture

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
│                     Apache Flink 2.0 Compatible APIs                     │
│  ┌─────────────────────┐              ┌─────────────────────┐             │
│  │ FlinkDotNet         │              │ Flink.JobBuilder    │             │
│  │ .DataStream         │              │ (Fluent DSL)        │             │
│  │ (Apache Flink 2.0   │              │ (Rapid              │             │
│  │  compatible API)    │              │  Development)       │             │
│  └─────────────────────┘              └─────────────────────┘             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Apache Flink 2.0 Clusters                      │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │
│  │ JobManager +    │  │ JobManager +    │  │ JobManager +    │    ...    │
│  │ TaskManagers    │  │ TaskManagers    │  │ TaskManagers    │           │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
```

### FlinkDotNet.Gateway to Apache Flink Communication

The FlinkDotNet.Gateway acts as a bridge between .NET applications and Apache Flink 2.0 clusters, supporting advanced scaling features:

#### Single Cluster Communication
1. **Job Submission**: .NET applications submit job definitions via HTTP to the gateway
2. **IR Translation**: Gateway translates JSON IR to Flink JobGraph
3. **Cluster Communication**: Gateway communicates with Flink JobManager via REST API
4. **Status Monitoring**: Gateway provides job status and metrics back to .NET applications

#### Multi-Cluster Orchestration (Apache Flink 2.0 Enhanced)
1. **Orchestra Coordination**: FlinkOrchestra manages job distribution across thousands of clusters
2. **Actor-based Management**: Each cluster is managed by an independent ClusterActor
3. **Temporal Workflows**: Long-running orchestration processes with exactly-once guarantees
4. **Intelligent Placement**: Jobs routed to optimal clusters based on health, capacity, and locality
5. **Auto-scaling**: Dynamic cluster provisioning and decommissioning based on demand
6. **Adaptive Scheduling**: Apache Flink 2.0 adaptive scheduler integration
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
- **Dynamic Scaling**: Apache Flink 2.0 savepoint-based scaling workflows
- **Adaptive Scheduling**: Integration with Flink 2.0 adaptive scheduler
- **Reactive Mode**: Automatic parallelism adjustment based on cluster resources

## Modular Structure

```
FlinkDotNet/
├── FlinkDotNet.Common/           # Core types and configuration
│   ├── Configuration             # Configuration, ExecutionConfig with Flink 2.0 features
│   ├── TypeInfo                  # Types, TypeInformation  
│   └── JobManagement            # JobClient with scaling capabilities
├── FlinkDotNet.DataStream/       # Apache Flink 2.0 compatible streaming API
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

// Configure Apache Flink 2.0 features
env.SetParallelism(4)
   .SetMaxParallelism(128)              // Enable dynamic scaling
   .EnableAdaptiveScheduler()           // Automatic parallelism adjustment
   .EnableReactiveMode()                // Adapt to cluster resources
   .EnableCheckpointing(5000);          // Checkpointing for fault tolerance

var numbers = env.FromCollection(Enumerable.Range(1, 1000));

var result = numbers
    .Filter(x => x % 2 == 0)      // Even numbers only
    .Map(x => x * x)              // Square them
    .Rebalance()                  // Apache Flink 2.0 rebalancing
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

// Demonstrate all Apache Flink 2.0 partitioning strategies
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

// Configure Apache Flink 2.0 features for the job
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

// Provision a new cluster with Apache Flink 2.0 features
var cluster = await orchestra.ProvisionClusterAsync(new ClusterConfiguration
{
    Name = "production-west",
    TaskSlots = 16,
    TaskManagers = 8,
    Region = "us-west-2",
    HighAvailability = true,
    AdaptiveSchedulerEnabled = true,    // Enable Apache Flink 2.0 adaptive scheduler
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
// Define a job with Apache Flink 2.0 configuration
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
// Start long-running orchestration workflow with Apache Flink 2.0 features
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

FlinkDotNet includes built-in backpressure support with Apache Flink 2.0 enhancements to ensure system stability:

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
    // Apache Flink 2.0 handles backpressure automatically
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

FlinkDotNet includes comprehensive testing capabilities with Apache Flink 2.0 integration:

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
        .Rebalance()                    // Test Apache Flink 2.0 rebalancing
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
- Backpressure handling with Apache Flink 2.0 improvements
- Fault tolerance and recovery with adaptive scheduling
- Dynamic scaling scenarios and savepoint-based workflows
- Reactive mode adaptation to resource changes

## Local Development with Aspire

FlinkDotNet integrates with .NET Aspire for local development with Apache Flink 2.0 features:

```csharp
// LocalTesting/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");

// Apache Flink 2.0 cluster with advanced features
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

## Getting Started

### Single Job Development

1. **Install FlinkDotNet NuGet packages**
   ```bash
   dotnet add package FlinkDotNet
   dotnet add package FlinkDotNet.DataStream
   ```

2. **Set up Apache Flink 2.0 cluster**
   - Download and install Apache Flink 2.0
   - Start JobManager and TaskManager with adaptive scheduler enabled
   - Configure reactive mode if desired

3. **Deploy FlinkDotNet.Gateway**
   - Configure connection to your Flink cluster
   - Deploy as web service or container
   - Enable Apache Flink 2.0 feature support

4. **Build and submit your first job with scaling capabilities**
   ```csharp
   // Apache Flink 2.0 compatible approach (DataStream API)
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

1. **Install additional orchestration packages**
   ```bash
   dotnet add package FlinkDotNet.Orchestration
   dotnet add package FlinkDotNet.ClusterManager
   dotnet add package FlinkDotNet.Temporal
   dotnet add package FlinkDotNet.Resilience
   ```

2. **Set up Temporal Server**
   ```bash
   # Using Docker
   docker run -p 7233:7233 -p 8233:8233 temporalio/auto-setup:latest
   ```

3. **Initialize Orchestra service with Apache Flink 2.0 features**
   ```csharp
   var services = new ServiceCollection();
   services.AddLogging();
   services.AddSingleton<IFlinkOrchestra, FlinkOrchestra>();
   
   var provider = services.BuildServiceProvider();
   var orchestra = provider.GetRequiredService<IFlinkOrchestra>();
   ```

4. **Start with cluster provisioning and scaling**
   ```csharp
   // Provision your first cluster with Apache Flink 2.0 features
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
- [Complete Usage Example](./docs/wiki/Complete-Usage-Example.md)
- [Gateway Communication Guide](./docs/wiki/flinkdotnet-gateway-communication.md)
- [Local Development Setup](./docs/wiki/Aspire-Local-Development-Setup.md)
- [Contributing Guidelines](./CONTRIBUTING.md)

### Apache Flink 2.0 Feature Documentation
- [Dynamic Scaling and Rebalancing Guide](./docs/flink-2.0-scaling-guide.md)
- [Adaptive Scheduler Configuration](./docs/adaptive-scheduler-setup.md)
- [Reactive Mode Implementation](./docs/reactive-mode-guide.md)
- [Savepoint-based Scaling Workflows](./docs/savepoint-scaling-guide.md)
- [Fine-grained Resource Management](./docs/resource-management-guide.md)

### Temporal Durable Workflow Architecture Documentation
- [Flink vs Temporal Decision Guide](./docs/flink-vs-temporal-decision-guide.md)
- [Backpressure Complete Reference](./docs/wiki/Backpressure-Complete-Reference.md)
- [Aspire Container Architecture](./docs/wiki/Backpressure-Aspire-Container-Architecture.md)
- [Rate Limiting Implementation](./docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Local Testing Setup](./docs/local-testing-setup.md)

### Testing and Quality Assurance
- [Stress Tests Overview](./docs/wiki/Stress-Tests-Overview.md)
- [Reliability Tests Overview](./docs/wiki/Reliability-Tests-Overview.md)
- [Complex Logic Stress Tests](./docs/wiki/Complex-Logic-Stress-Tests.md)
- [Observability and Monitoring](./docs/observability/README.md)
- [Monitoring Best Practices](./docs/observability/monitoring-best-practices.md)

## Frequently Asked Questions

### How does FlinkDotNet support Apache Flink 2.0 features?

**FlinkDotNet provides complete Apache Flink 2.0 compatibility** including:

- **Adaptive Scheduler**: Automatic parallelism adjustment based on workload characteristics
- **Reactive Mode**: Elastic scaling that adapts to available cluster resources
- **Dynamic Scaling**: Change job parallelism without stopping jobs using savepoints
- **Advanced Partitioning**: All Apache Flink 2.0 partitioning strategies (rebalance, rescale, forward, shuffle, broadcast, custom)
- **Fine-grained Resource Management**: Slot sharing groups and resource profiles
- **Enhanced Fault Tolerance**: Advanced restart strategies and checkpointing

```csharp
// Enable all Apache Flink 2.0 features
var env = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()     // Intelligent resource management
    .EnableReactiveMode()          // Elastic scaling
    .SetMaxParallelism(256)        // Dynamic scaling support
    .EnableCheckpointing(5000);    // Enhanced fault tolerance

var scalableStream = env.FromCollection(data)
    .Rebalance()                   // Apache Flink 2.0 rebalancing
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

- **DataStream API**: Use for Apache Flink 2.0 compatibility, complex stream processing, and when you need full control over scaling and partitioning
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

**FlinkDotNet maintains full compatibility** while adding Apache Flink 2.0 features:

1. **Keep existing code**: All existing DataStream and JobBuilder code continues to work
2. **Add Apache Flink 2.0 features gradually**: Enable adaptive scheduler, reactive mode, and advanced partitioning as needed
3. **Scale incrementally**: Start with single cluster, add orchestration layer when needed
4. **Optimize performance**: Use new partitioning strategies and resource management features

**Migration example:**
```csharp
// Existing code (still works)
var env = Flink.GetExecutionEnvironment();
var stream = env.FromCollection(data).Map(x => x * 2);

// Enhanced with Apache Flink 2.0 features
var enhancedEnv = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()     // Add intelligent scheduling
    .EnableReactiveMode()          // Add elastic scaling
    .SetMaxParallelism(128);       // Enable dynamic scaling

var enhancedStream = enhancedEnv.FromCollection(data)
    .Map(x => x * 2)
    .Rebalance()                   // Add efficient rebalancing
    .SetParallelism(8);            // Set optimal parallelism
```

The architecture is designed for **incremental adoption** - you can start with basic features and scale to enterprise levels with Apache Flink 2.0 capabilities as your requirements grow.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

## License

MIT License - see [LICENSE](./LICENSE) file for details.
