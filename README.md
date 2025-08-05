# FlinkDotNet

**FlinkDotNet** is a comprehensive solution that enables .NET developers to build and submit streaming jobs to Apache Flink clusters using a fluent C# DSL.

## Quick Start

```csharp
using FlinkDotNet;
using FlinkDotNet.DataStream;

var env = Flink.GetExecutionEnvironment();
env.SetParallelism(4);

var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

dataStream
    .Map(x => x * 2)
    .Filter(x => x > 5)
    .Print();

await env.ExecuteAsync("My Job");
```

## FlinkJobBuilder API

FlinkDotNet provides a fluent C# DSL for building streaming jobs:

```csharp
var job = Flink.JobBuilder
    .FromKafka("orders")
    .Where("Amount > 100")
    .GroupBy("Region")
    .Aggregate("SUM", "Amount")
    .ToKafka("high-value-orders");

await job.Submit("Processing Job");
```

This generates IR and submits to the Flink Job Gateway:

```json
{
  "source": { "type": "kafka", "topic": "orders" },
  "operations": [
    { "type": "filter", "expression": "Amount > 100" },
    { "type": "groupBy", "key": "Region" },
    { "type": "aggregate", "aggregationType": "SUM", "field": "Amount" }
  ],
  "sink": { "type": "kafka", "topic": "high-value-orders" }
}
```

## Architecture

FlinkDotNet provides a complete integration solution for Apache Flink:

- **.NET SDK (FlinkDotNet.DataStream)**: Streaming API for .NET developers
- **Legacy SDK (Flink.JobBuilder)**: Fluent C# DSL for job construction
- **Intermediate Representation (IR)**: JSON-based job definitions
- **Job Gateway**: HTTP service that bridges .NET applications with Apache Flink clusters

### FlinkDotNet.Gateway to Apache Flink Communication

The FlinkDotNet.Gateway acts as a bridge between .NET applications and Apache Flink clusters:

1. **Job Submission**: .NET applications submit job definitions via HTTP to the gateway
2. **IR Translation**: Gateway translates JSON IR to Flink JobGraph
3. **Cluster Communication**: Gateway communicates with Flink JobManager via REST API
4. **Status Monitoring**: Gateway provides job status and metrics back to .NET applications

```
┌─────────────────┐    HTTP     ┌─────────────────┐    REST     ┌─────────────────┐
│   .NET App      │─────────────▶│ FlinkDotNet     │─────────────▶│ Apache Flink    │
│                 │             │ Gateway         │             │ JobManager      │
│ FlinkJobBuilder │◀─────────────│                 │◀─────────────│                 │
└─────────────────┘   JSON IR   └─────────────────┘  JobGraph   └─────────────────┘
```

The gateway handles:
- **Authentication & Authorization**: Secure access to Flink clusters
- **Load Balancing**: Distribute jobs across multiple Flink clusters
- **Monitoring & Metrics**: Real-time job status and performance metrics
- **Error Handling**: Graceful error recovery and retry logic

## Modular Structure

```
FlinkDotNet/
├── FlinkDotNet.Common/           # Core types and configuration
│   ├── Configuration             # Configuration, ExecutionConfig
│   ├── TypeInfo                  # Types, TypeInformation  
│   └── JobManagement            # JobClient, JobExecutionResult
├── FlinkDotNet.DataStream/       # Streaming API
│   ├── StreamExecutionEnvironment # Main entry point
│   ├── DataStream                # Core streaming API
│   ├── Functions                 # User functions
│   └── Connectors               # Sources and sinks
├── FlinkDotNet.Table/           # Table API (future)
├── FlinkDotNet.Testing/         # Testing utilities
├── FlinkDotNet.Util/            # Utility classes
└── FlinkDotNet/                 # Main unified API
```

## Examples

### Basic Data Processing

```csharp
var env = Flink.GetExecutionEnvironment();

var numbers = env.FromCollection(Enumerable.Range(1, 1000));

var result = numbers
    .Filter(x => x % 2 == 0)  // Even numbers only
    .Map(x => x * x)          // Square them
    .Sum();                   // Sum the results

await env.ExecuteAsync("Even Squares");
```

### Kafka Integration

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

### Windowed Aggregations

```csharp
var job = Flink.JobBuilder
    .FromKafka("events")
    .GroupBy("userId")
    .Window("TUMBLING", 5, "MINUTES")
    .Aggregate("COUNT", "*")
    .ToKafka("user-activity");

await job.Submit("User Activity");
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
   var job = Flink.JobBuilder
       .FromKafka("source")
       .Map("value = process(data)")
       .ToKafka("destination");
       
   await job.Submit("My First Job");
   ```

## Documentation

- [API Documentation](./docs/api/)
- [Gateway Setup Guide](./docs/gateway-setup.md)
- [Integration Examples](./docs/examples/)
- [Performance Tuning](./docs/performance.md)
- [Troubleshooting](./docs/troubleshooting.md)

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests
5. Submit a pull request

See [CONTRIBUTING.md](./CONTRIBUTING.md) for detailed guidelines.

## License

MIT License - see [LICENSE](./LICENSE) file for details.