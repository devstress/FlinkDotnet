# FlinkDotNet

**FlinkDotNet** is a comprehensive solution that enables .NET developers to build and submit streaming jobs to Apache Flink clusters using a fluent C# DSL. It now includes Temporal multi-cluster orchestration capabilities for durable storage, durable workflow, resilience, reliability, exactly one semantic, scalability to billion messages per second and 99,999% availability.

## 🚀 Netflix Architecture Integration

FlinkDotNet now implements Netflix's "Actor Workflows: Reliably orchestrating thousands of Flink clusters" architecture, enabling:

- **Massive Scale**: Orchestrate thousands of Flink clusters simultaneously
- **99.999% Availability**: Actor-based isolation prevents cascade failures  
- **Intelligent Job Placement**: Smart distribution across clusters based on capacity and health
- **Temporal Workflows**: Durable orchestration with exactly-once execution guarantees
- **Auto-scaling**: Dynamic cluster provisioning based on demand

## Multi-Scale Architecture

FlinkDotNet provides a comprehensive, multi-layered architecture supporting everything from single jobs to Netflix-scale orchestration:

### Individual Job Development
```csharp
// Modern DataStream API (Python-aligned)
var env = Flink.GetExecutionEnvironment();
env.SetParallelism(4);

var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
dataStream
    .Map(x => x * 2)
    .Filter(x => x > 5)
    .Print();

await env.ExecuteAsync("My Job");

// Legacy JobBuilder API (Backward Compatible)
var job = Flink.JobBuilder
    .FromKafka("orders")
    .Where("Amount > 100")
    .GroupBy("Region")
    .Aggregate("SUM", "Amount")
    .ToKafka("high-value-orders");

await job.Submit("Processing Job");
```

### Multi-Cluster Orchestration
```csharp
// Netflix-style Orchestra for thousands of clusters
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

// Start Temporal orchestration workflows
await orchestra.StartOrchestrationWorkflowAsync(new OrchestrationRequest
{
    TargetClusters = 1000,
    MinClusters = 10,
    MaxClusters = 5000
});
```

## Architecture Overview

FlinkDotNet provides a complete Netflix-scale integration solution with multi-layered architecture:

### Core Components

#### Job Development Layer
- **.NET SDK (FlinkDotNet.DataStream)**: Modern streaming API aligned with Python Flink
- **Legacy SDK (Flink.JobBuilder)**: Backward-compatible fluent C# DSL (still supported and recommended for many use cases)
- **Intermediate Representation (IR)**: JSON-based job definitions
- **Job Gateway**: HTTP service that bridges .NET applications with Apache Flink clusters

#### Orchestration Layer  
- **FlinkDotNet.Orchestra**: Multi-cluster job orchestration with intelligent placement strategies
- **FlinkDotNet.ClusterManager**: Actor-based cluster lifecycle management  
- **FlinkDotNet.Temporal**: Temporal.io workflow definitions for durable orchestration
- **FlinkDotNet.Resilience**: Circuit breakers, retry policies, and health checkers

### Netflix Architecture Implementation

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                           FlinkDotNet.Orchestra                             │
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
│                     Individual Job Development APIs                        │
│  ┌─────────────────────┐              ┌─────────────────────┐             │
│  │ FlinkDotNet         │              │ Flink.JobBuilder    │             │
│  │ .DataStream         │              │ (Legacy)            │             │
│  │ (Modern Python-     │              │ (Backward           │             │
│  │  aligned API)       │              │  Compatible)        │             │
│  └─────────────────────┘              └─────────────────────┘             │
└─────────────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────────────────┐
│                           Apache Flink Clusters                           │
│  ┌─────────────────┐  ┌─────────────────┐  ┌─────────────────┐           │
│  │ JobManager +    │  │ JobManager +    │  │ JobManager +    │    ...    │
│  │ TaskManagers    │  │ TaskManagers    │  │ TaskManagers    │           │
│  └─────────────────┘  └─────────────────┘  └─────────────────┘           │
└─────────────────────────────────────────────────────────────────────────────┘
```

### FlinkDotNet.Gateway to Apache Flink Communication

The FlinkDotNet.Gateway acts as a bridge between .NET applications and Apache Flink clusters, now enhanced with multi-cluster orchestration:

#### Single Cluster Communication (Traditional)
1. **Job Submission**: .NET applications submit job definitions via HTTP to the gateway
2. **IR Translation**: Gateway translates JSON IR to Flink JobGraph
3. **Cluster Communication**: Gateway communicates with Flink JobManager via REST API
4. **Status Monitoring**: Gateway provides job status and metrics back to .NET applications

#### Multi-Cluster Orchestration (Netflix Architecture)
1. **Orchestra Coordination**: FlinkOrchestra manages job distribution across thousands of clusters
2. **Actor-based Management**: Each cluster is managed by an independent ClusterActor
3. **Temporal Workflows**: Long-running orchestration processes with exactly-once guarantees
4. **Intelligent Placement**: Jobs routed to optimal clusters based on health, capacity, and locality
5. **Auto-scaling**: Dynamic cluster provisioning and decommissioning based on demand

```
┌─────────────────┐    HTTP     ┌─────────────────┐    Orchestration    ┌─────────────────┐
│   .NET App      │─────────────▶│ FlinkDotNet     │─────────────────────▶│ FlinkDotNet     │
│                 │             │ Gateway         │                     │ Orchestra       │
│ JobBuilder/     │◀─────────────│                 │◀─────────────────────│ (Multi-cluster) │
│ DataStream APIs │   JSON IR   └─────────────────┘    Job Distribution └─────────────────┘
└─────────────────┘                      │                                        │
                                         ▼                                        ▼
                              ┌─────────────────┐                        ┌─────────────────┐
                              │ Apache Flink    │                        │ ClusterManager  │
                              │ JobManager      │◀───────────────────────│ Actors          │
                              │ (Single)        │      REST APIs         │ (Thousands)     │
                              └─────────────────┘                        └─────────────────┘
```

The gateway and orchestra handle:
- **Authentication & Authorization**: Secure access to Flink clusters
- **Load Balancing**: Distribute jobs across multiple Flink clusters
- **Monitoring & Metrics**: Real-time job status and performance metrics across all clusters
- **Error Handling**: Graceful error recovery and retry logic with circuit breakers
- **Auto-scaling**: Intelligent cluster provisioning and capacity management
- **Health Aggregation**: Cross-cluster health monitoring and issue detection

## Modular Structure

```
FlinkDotNet/
├── FlinkDotNet.Common/           # Core types and configuration
│   ├── Configuration             # Configuration, ExecutionConfig
│   ├── TypeInfo                  # Types, TypeInformation  
│   └── JobManagement            # JobClient, JobExecutionResult
├── FlinkDotNet.DataStream/       # Modern streaming API (Python-aligned)
│   ├── StreamExecutionEnvironment # Main entry point
│   ├── DataStream                # Core streaming API
│   ├── Functions                 # User functions
│   └── Connectors               # Sources and sinks
├── FlinkDotNet.Orchestra/        # Multi-cluster orchestration (Netflix architecture)
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
├── Flink.JobBuilder/             # Legacy fluent API (backward compatible)
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

#### Basic Data Processing (Modern DataStream API)
```csharp
var env = Flink.GetExecutionEnvironment();

var numbers = env.FromCollection(Enumerable.Range(1, 1000));

var result = numbers
    .Filter(x => x % 2 == 0)  // Even numbers only
    .Map(x => x * x)          // Square them
    .Sum();                   // Sum the results

await env.ExecuteAsync("Even Squares");
```

#### Kafka Integration (Legacy JobBuilder - Still Recommended)
```csharp
var job = Flink.JobBuilder
    .FromKafka("input-topic", config => {
        config.BootstrapServers = "localhost:9092";
        config.GroupId = "processing-group";
    })
    .Map("processed = transform(data)")
    .Where("processed.isValid")
    .ToKafka("output-topic");

await job.Submit("Kafka Processing");
```

#### Windowed Aggregations (Legacy JobBuilder)
```csharp
var job = Flink.JobBuilder
    .FromKafka("events")
    .GroupBy("userId")
    .Window("TUMBLING", 5, "MINUTES")
    .Aggregate("COUNT", "*")
    .ToKafka("user-activity");

await job.Submit("User Activity");
```

### Multi-Cluster Orchestration (Netflix Architecture)

#### Cluster Provisioning and Management
```csharp
var orchestra = new FlinkOrchestra(logger);

// Provision a new cluster
var cluster = await orchestra.ProvisionClusterAsync(new ClusterConfiguration
{
    Name = "production-west",
    TaskSlots = 16,
    TaskManagers = 8,
    Region = "us-west-2",
    HighAvailability = true
});

// Get cluster health across all clusters
var health = await orchestra.GetClusterHealthAsync();
Console.WriteLine($"Overall Health Score: {health.OverallHealthScore:F1}%");
Console.WriteLine($"Total Clusters: {health.TotalClusters}");
Console.WriteLine($"Healthy: {health.HealthyClusters}, Critical: {health.CriticalClusters}");
```

#### Intelligent Job Submission
```csharp
// Define a job (can come from DataStream or JobBuilder APIs)
var jobDefinition = new FlinkJobDefinition
{
    JobId = "analytics-pipeline",
    JobName = "Real-time Analytics",
    JobGraph = "...", // Generated from DataStream/JobBuilder
    Parallelism = 8,
    Priority = JobPriority.High
};

// Submit with intelligent placement
var result = await orchestra.SubmitJobAsync(jobDefinition, SubmissionStrategy.BestFit);

if (result.Success)
{
    Console.WriteLine($"Job {result.JobId} submitted to cluster {result.ClusterId}");
    Console.WriteLine($"Flink Job ID: {result.FlinkJobId}");
}
```

#### Auto-scaling with Temporal Workflows
```csharp
// Start long-running orchestration workflow
var workflowId = await orchestra.StartOrchestrationWorkflowAsync(new OrchestrationRequest
{
    RequestId = "scaling-request-1",
    TargetClusters = 500,
    MinClusters = 50,
    MaxClusters = 2000,
    ScalingPolicy = "demand-based"
});

Console.WriteLine($"Started orchestration workflow: {workflowId}");

// Monitor and scale dynamically
var scalingResult = await orchestra.ScaleOrchestraAsync(targetCapacity: 750);
Console.WriteLine($"Scaled from {scalingResult.PreviousCapacity} to {scalingResult.NewCapacity} clusters");
```

## Backpressure and Rate Limiting

FlinkDotNet includes built-in backpressure support to ensure system stability:

```csharp
using Flink.JobBuilder.Backpressure;

// Configure rate limiter
var rateLimiter = new TokenBucketRateLimiter(
    rateLimit: 1000.0,      // 1000 operations per second
    burstCapacity: 2000.0   // Handle bursts up to 2000
);

// Use in your application
if (rateLimiter.TryAcquire())
{
    await ProcessMessage(message);
}
else
{
    // Handle backpressure
    await Task.Delay(100); // Wait and retry
}
```

## Testing and Reliability

FlinkDotNet includes comprehensive testing capabilities:

### Integration Tests

```csharp
[Fact]
public async Task TestStreamProcessing()
{
    var env = Flink.GetExecutionEnvironment();
    
    var testData = new[] { 1, 2, 3, 4, 5 };
    var result = env.FromCollection(testData)
        .Map(x => x * 2)
        .CollectAsync();
        
    var expected = new[] { 2, 4, 6, 8, 10 };
    Assert.Equal(expected, await result);
}
```

### Stress Testing

The project includes comprehensive stress tests that validate:
- High-throughput processing (1M+ messages)
- Backpressure handling
- Fault tolerance and recovery
- Consumer rebalancing scenarios

## Local Development with Aspire

FlinkDotNet integrates with .NET Aspire for local development:

```csharp
// LocalTesting/Program.cs
var builder = DistributedApplication.CreateBuilder(args);

var kafka = builder.AddKafka("kafka");
var flink = builder.AddContainer("flink", "flink:latest");

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

2. **Set up Apache Flink cluster**
   - Download and install Apache Flink
   - Start JobManager and TaskManager

3. **Deploy FlinkDotNet.Gateway**
   - Configure connection to your Flink cluster
   - Deploy as web service or container

4. **Build and submit your first job**
   ```csharp
   // Modern approach (DataStream API)
   var env = Flink.GetExecutionEnvironment();
   var stream = env.FromCollection(new[] { 1, 2, 3 });
   await env.ExecuteAsync("My First DataStream Job");
   
   // Legacy approach (JobBuilder - still recommended for many cases)
   var job = Flink.JobBuilder
       .FromKafka("source")
       .Map("value = process(data)")
       .ToKafka("destination");
   await job.Submit("My First JobBuilder Job");
   ```

### Netflix-Scale Multi-Cluster Setup

1. **Install additional orchestration packages**
   ```bash
   dotnet add package FlinkDotNet.Orchestra
   dotnet add package FlinkDotNet.ClusterManager
   dotnet add package FlinkDotNet.Temporal
   dotnet add package FlinkDotNet.Resilience
   ```

2. **Set up Temporal Server**
   ```bash
   # Using Docker
   docker run -p 7233:7233 -p 8233:8233 temporalio/auto-setup:latest
   ```

3. **Initialize Orchestra service**
   ```csharp
   var services = new ServiceCollection();
   services.AddLogging();
   services.AddSingleton<IFlinkOrchestra, FlinkOrchestra>();
   
   var provider = services.BuildServiceProvider();
   var orchestra = provider.GetRequiredService<IFlinkOrchestra>();
   ```

4. **Start with cluster provisioning**
   ```csharp
   // Provision your first cluster
   var cluster = await orchestra.ProvisionClusterAsync(new ClusterConfiguration
   {
       Name = "starter-cluster",
       TaskSlots = 4,
       TaskManagers = 2
   });
   
   // Check overall health
   var health = await orchestra.GetClusterHealthAsync();
   Console.WriteLine($"Health Score: {health.OverallHealthScore:F1}%");
   ```

## Documentation

### Core Documentation
- [API Documentation](./docs/api/)
- [Gateway Setup Guide](./docs/gateway-setup.md)
- [Integration Examples](./docs/examples/)
- [Performance Tuning](./docs/performance.md)
- [Troubleshooting](./docs/troubleshooting.md)

### Netflix Architecture Documentation
- [Flink vs Temporal Decision Guide](./docs/flink-vs-temporal-decision-guide.md)
- [Multi-Cluster Orchestration Patterns](./docs/orchestration-patterns.md)
- [Temporal Workflow Setup](./docs/temporal-setup.md)
- [Auto-scaling Configuration](./docs/auto-scaling.md)
- [Resilience Patterns](./docs/resilience-patterns.md)

## Frequently Asked Questions

### Do I still need JobBuilder?

**Yes! JobBuilder is still fully supported and recommended for many use cases.**

The new Netflix architecture operates at a different level:

- **JobBuilder/DataStream APIs**: Create individual job definitions 
- **Orchestra/ClusterManager**: Orchestrate where those jobs run across thousands of clusters
- **Temporal Workflows**: Coordinate long-running orchestration processes

```csharp
// JobBuilder creates the job definition
var job = Flink.JobBuilder
    .FromKafka("orders")
    .Where("Amount > 100")
    .ToKafka("high-value-orders");

// Orchestra decides which cluster runs it
var jobDef = new FlinkJobDefinition 
{ 
    JobGraph = job.ToJobGraph(), // From JobBuilder
    Parallelism = 4 
};

var result = await orchestra.SubmitJobAsync(jobDef, SubmissionStrategy.BestFit);
```

**When to use each approach:**
- **Single cluster, simple deployments**: Use DataStream or JobBuilder directly
- **Multiple clusters, Netflix-scale**: Use Orchestra + your preferred job definition API
- **Complex business workflows**: Add Temporal workflows for coordination
- **High availability requirements**: Use the full Netflix architecture stack

### Migration Path

1. **Keep existing jobs**: All JobBuilder and DataStream code continues to work
2. **Add Orchestra gradually**: Start with single cluster, add orchestration layer
3. **Scale horizontally**: Add more clusters through Orchestra as needed
4. **Add Temporal**: Implement complex workflows when business requirements demand it

The architecture is designed for **incremental adoption** - you can start simple and scale to Netflix levels as needed.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

## License

MIT License - see [LICENSE](./LICENSE) file for details.
