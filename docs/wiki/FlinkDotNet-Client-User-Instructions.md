# FlinkDotNet Client User Instructions

This guide covers using the FlinkDotNet NuGet package to build and submit Apache Flink streaming jobs from your .NET applications.

## Overview

The FlinkDotNet client package provides a fluent C# API for building Apache Flink 2.1 streaming jobs. You write your job definitions in C#, and the client translates them to Flink's intermediate representation (IR) for execution on Flink clusters.

## Installation

### Prerequisites

- **.NET 9.0 SDK** or later
- **Apache Flink 2.1 cluster** (accessible via REST API)
- **Apache Kafka** (optional, for Kafka sources/sinks)

### Install via NuGet

```bash
# Add the FlinkDotNet package to your project
dotnet add package FlinkDotNet
```

Or via Package Manager Console:

```powershell
Install-Package FlinkDotNet
```

## Quick Start

### 1. Create a Console Application

```bash
dotnet new console -n MyFlinkStreamingApp
cd MyFlinkStreamingApp
dotnet add package FlinkDotNet
```

### 2. Write Your First Streaming Job

```csharp
using FlinkDotNet.DataStream;

class Program
{
    static async Task Main(string[] args)
    {
        // Get the Flink execution environment
        var env = Flink.GetExecutionEnvironment();
        
        // Configure environment (optional)
        env.SetParallelism(4);
        env.EnableCheckpointing(TimeSpan.FromSeconds(30));
        
        // Create a data stream from Kafka
        var orders = env.FromKafka(
            topic: "orders",
            bootstrapServers: "localhost:9092",
            groupId: "order-processor"
        );
        
        // Transform the stream
        var highValueOrders = orders
            .Filter(order => order.Amount > 1000)
            .Map(order => order.ToUpperInvariant())
            .KeyBy(order => order.CustomerId);
        
        // Write results to Kafka
        highValueOrders.SinkToKafka("high-value-orders", "localhost:9092");
        
        // Execute the job on the Flink cluster
        var jobClient = await env.ExecuteAsync("HighValueOrderProcessor");
        
        Console.WriteLine($"Job submitted successfully! Job ID: {jobClient.JobId}");
    }
}
```

### 3. Configure Connection to Flink Cluster

Create an `appsettings.json` file:

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://localhost:8081",
    "GatewayUrl": "http://localhost:8080"
  }
}
```

Or set via environment variables:

```bash
export FLINK_JOBMANAGER_URL=http://localhost:8081
export FLINK_GATEWAY_URL=http://localhost:8080
```

### 4. Run Your Application

```bash
dotnet run
```

## API Usage

### DataStream API

#### Creating Streams

```csharp
// From Kafka
var stream = env.FromKafka("topic-name", "kafka:9092", "consumer-group");

// From collection (for testing)
var stream = env.FromCollection(new[] { "item1", "item2", "item3" });
```

#### Transformations

```csharp
// Map: One-to-one transformation
var mapped = stream.Map(x => x.ToUpperInvariant());

// Filter: Select elements
var filtered = stream.Filter(x => x.Length > 5);

// FlatMap: One-to-many transformation
var flattened = stream.FlatMap(x => x.Split(' '));

// KeyBy: Partition by key
var keyed = stream.KeyBy(x => x.CustomerId);

// Reduce: Combine elements
var reduced = keyed.Reduce((a, b) => a.Amount + b.Amount);
```

#### Windowing

```csharp
// Tumbling time window
var windowed = keyed
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(5)))
    .Reduce((a, b) => new Order { Amount = a.Amount + b.Amount });

// Sliding time window
var sliding = keyed
    .Window(SlidingTimeWindow.Of(
        size: TimeSpan.FromMinutes(10),
        slide: TimeSpan.FromMinutes(2)
    ))
    .Aggregate(new SumAggregator());

// Session window
var sessions = keyed
    .Window(SessionWindow.WithGap(TimeSpan.FromMinutes(15)))
    .Process(new SessionProcessor());
```

#### Sinks

```csharp
// To Kafka
stream.SinkToKafka("output-topic", "kafka:9092");

// Print (for debugging)
stream.Print();
```

### Environment Configuration

```csharp
var env = Flink.GetExecutionEnvironment();

// Set parallelism
env.SetParallelism(8);

// Enable checkpointing
env.EnableCheckpointing(TimeSpan.FromSeconds(30));

// Set checkpoint mode
env.SetCheckpointMode(CheckpointMode.ExactlyOnce);

// Configure restart strategy
env.SetRestartStrategy(RestartStrategies.FixedDelay(
    attempts: 3,
    delay: TimeSpan.FromSeconds(10)
));

// Set time characteristic
env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);
```

## Advanced Usage

### Working with Custom Types

```csharp
public record Order(
    string OrderId,
    string CustomerId,
    decimal Amount,
    DateTime Timestamp
);

// Use with type parameter
var orders = env.FromKafka<Order>("orders", "kafka:9092", "group1");

var processed = orders
    .Filter(o => o.Amount > 100)
    .KeyBy(o => o.CustomerId)
    .Map(o => o with { Amount = o.Amount * 1.1m });
```

### State Management

```csharp
// Using keyed state
var statefulStream = keyed
    .Process(new StatefulProcessor());

public class StatefulProcessor : KeyedProcessFunction<string, Order, Result>
{
    private ValueState<decimal> totalState;
    
    public override void Open(Configuration config)
    {
        totalState = GetState(
            new ValueStateDescriptor<decimal>("total", typeof(decimal))
        );
    }
    
    public override void ProcessElement(Order value, Context ctx, Collector<Result> out)
    {
        var currentTotal = totalState.Value();
        var newTotal = currentTotal + value.Amount;
        totalState.Update(newTotal);
        
        out.Collect(new Result(value.CustomerId, newTotal));
    }
}
```

### Joins

```csharp
var orders = env.FromKafka<Order>("orders", "kafka:9092", "group1");
var customers = env.FromKafka<Customer>("customers", "kafka:9092", "group2");

var enriched = orders
    .Join(customers)
    .Where(order => order.CustomerId)
    .EqualTo(customer => customer.Id)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Apply((order, customer) => new EnrichedOrder
    {
        OrderId = order.OrderId,
        CustomerName = customer.Name,
        Amount = order.Amount
    });
```

## Configuration Options

### Application Settings

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://flink-jobmanager:8081",
    "GatewayUrl": "http://gateway:8080",
    "DefaultParallelism": 4,
    "CheckpointInterval": "00:00:30",
    "CheckpointMode": "ExactlyOnce",
    "RestartStrategy": {
      "Type": "FixedDelay",
      "Attempts": 3,
      "DelaySeconds": 10
    }
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "DefaultGroupId": "flinkdotnet-consumer",
    "SecurityProtocol": "PLAINTEXT"
  }
}
```

### Environment Variables

```bash
# Flink connection
export FLINK_JOBMANAGER_URL=http://localhost:8081
export FLINK_GATEWAY_URL=http://localhost:8080

# Kafka configuration
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092
export KAFKA_CONSUMER_GROUP=my-group

# Job configuration
export FLINK_PARALLELISM=8
export FLINK_CHECKPOINT_INTERVAL=30000
```

## Testing

### Unit Testing

```csharp
[Test]
public async Task TestStreamingJob()
{
    var env = Flink.GetExecutionEnvironment();
    
    // Use collection source for testing
    var testData = new[] { "a", "b", "c" };
    var stream = env.FromCollection(testData);
    
    var result = stream
        .Map(x => x.ToUpperInvariant())
        .Collect();
    
    Assert.AreEqual(new[] { "A", "B", "C" }, result);
}
```

### Integration Testing

```csharp
[Test]
public async Task TestKafkaIntegration()
{
    var env = Flink.GetExecutionEnvironment();
    
    var stream = env.FromKafka("test-topic", "kafka:9092", "test-group");
    var processed = stream.Map(x => x.ToUpperInvariant());
    processed.SinkToKafka("test-output", "kafka:9092");
    
    var jobClient = await env.ExecuteAsync("IntegrationTest");
    
    // Verify job started
    Assert.IsNotNull(jobClient.JobId);
}
```

## Troubleshooting

### Connection Issues

**Problem**: Cannot connect to Flink cluster

**Solution**:
```bash
# Verify Flink is accessible
curl http://localhost:8081/config

# Check network connectivity
ping flink-jobmanager

# Verify configuration
echo $FLINK_JOBMANAGER_URL
```

### Job Submission Failures

**Problem**: Job submission fails with validation errors

**Solution**:
```csharp
// Enable detailed logging
var env = Flink.GetExecutionEnvironment();
env.SetLogLevel(LogLevel.Debug);

// Validate before submission
var jobDef = env.GetJobDefinition();
var validation = JobDefinitionValidator.Validate(jobDef);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Validation error: {error}");
    }
}
```

### Serialization Issues

**Problem**: Custom types not serializing correctly

**Solution**:
```csharp
// Use records for automatic serialization
public record MyData(string Id, int Value);

// Or implement custom serializer
public class MyDataSerializer : ISerializer<MyData>
{
    public byte[] Serialize(MyData value) { /* ... */ }
    public MyData Deserialize(byte[] bytes) { /* ... */ }
}

env.RegisterSerializer(new MyDataSerializer());
```

## Performance Optimization

### Parallelism Tuning

```csharp
// Set global parallelism
env.SetParallelism(16);

// Set operator-specific parallelism
stream
    .Map(x => x.Transform())
    .SetParallelism(8)
    .Filter(x => x.IsValid)
    .SetParallelism(16);
```

### Checkpoint Configuration

```csharp
env.EnableCheckpointing(TimeSpan.FromSeconds(60));
env.SetCheckpointMode(CheckpointMode.ExactlyOnce);
env.SetCheckpointTimeout(TimeSpan.FromMinutes(10));
env.SetMinPauseBetweenCheckpoints(TimeSpan.FromSeconds(30));
env.SetMaxConcurrentCheckpoints(1);
```

### State Backend Selection

```csharp
// Use RocksDB for large state
env.SetStateBackend(StateBackend.RocksDB);

// Use HashMap for small state (faster)
env.SetStateBackend(StateBackend.HashMap);
```

## Best Practices

1. **Use records for data classes** - Better serialization and immutability
2. **Set appropriate parallelism** - Match cluster capacity
3. **Enable checkpointing** - Fault tolerance
4. **Use event time** - Correct ordering for out-of-order events
5. **Test locally first** - Use LocalTesting environment
6. **Monitor job metrics** - Use Flink Web UI
7. **Handle backpressure** - Design for throughput variations
8. **Use keyed state** - For stateful operations

## Next Steps

- **[API Reference](../api-reference.md)** - Complete API documentation
- **[Architecture Guide](../architecture-and-usecases.md)** - System design patterns
- **[Performance Tuning](../performance-benchmarks.md)** - Optimization strategies
- **[Learning Course](../../LearningCourse/README.md)** - 15-day hands-on training
- **[Troubleshooting](../troubleshooting.md)** - Common issues and solutions

## Support

- **GitHub Issues**: https://github.com/devstress/FlinkDotnet/issues
- **Discussions**: https://github.com/devstress/FlinkDotnet/discussions
- **Documentation**: https://github.com/devstress/FlinkDotnet/docs
