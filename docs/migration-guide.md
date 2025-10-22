# Migration Guide: JobBuilder Fluent API to DataStream API

This guide helps you migrate from the removed `Flink.JobBuilder` fluent API to the current `FlinkDotNet.DataStream` API.

## What Changed

The fluent JobBuilder API has been removed in favor of the standard Apache Flink DataStream API pattern. This provides:
- ✅ Better alignment with Apache Flink documentation and examples
- ✅ More direct access to Flink features
- ✅ Improved type safety and IntelliSense
- ✅ Standard patterns familiar to Flink developers

## Quick Migration Examples

### Basic Stream Processing

**Before (Removed JobBuilder API):**
```csharp
using Flink.JobBuilder;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddFlinkJobBuilder(config =>
{
    config.BaseUrl = "http://localhost:18000";
});

var serviceProvider = services.BuildServiceProvider();
var job = serviceProvider.CreateJobBuilder()
    .FromKafka("input-topic")
    .Map("message => message.toUpperCase()")
    .ToKafka("output-topic");

await job.Submit("SimpleJob");
```

**After (Current DataStream API):**
```csharp
using FlinkDotNet.DataStream;

var env = Flink.GetExecutionEnvironment();

var input = env.FromKafka("input-topic", "kafka:9093", "consumer-group");
var processed = input.Map(message => message.ToUpperInvariant());
processed.SinkToKafka("output-topic", "kafka:9093");

await env.ExecuteAsync("SimpleJob");
```

### Windowing and Aggregation

**Before:**
```csharp
var job = serviceProvider.CreateJobBuilder()
    .FromKafka("orders")
    .KeyBy("customerId")
    .Window(TimeWindow.Tumbling(TimeSpan.FromMinutes(5)))
    .Aggregate("SUM", "amount")
    .ToKafka("order-totals");

await job.Submit("OrderAggregation");
```

**After:**
```csharp
var env = Flink.GetExecutionEnvironment();

var orders = env.FromKafka("orders", "kafka:9093", "order-group");
var aggregated = orders
    .KeyBy(order => order.CustomerId)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(5)))
    .Reduce((a, b) => new Order { Amount = a.Amount + b.Amount });

aggregated.SinkToKafka("order-totals", "kafka:9093");
await env.ExecuteAsync("OrderAggregation");
```

### Filtering and Transformation

**Before:**
```csharp
var job = serviceProvider.CreateJobBuilder()
    .FromKafka("events")
    .Filter("amount > 100")
    .Map("event => ProcessEvent(event)")
    .ToKafka("processed-events");
```

**After:**
```csharp
var env = Flink.GetExecutionEnvironment();

var events = env.FromKafka("events", "kafka:9093", "event-group");
var processed = events
    .Filter(event => event.Amount > 100)
    .Map(event => ProcessEvent(event));

processed.SinkToKafka("processed-events", "kafka:9093");
await env.ExecuteAsync("EventProcessor");
```

### Join Operations

**Before:**
```csharp
var job = serviceProvider.CreateJobBuilder()
    .FromKafka("orders")
    .Join(
        other: serviceProvider.CreateStream().FromKafka("customers"),
        condition: "orders.customerId == customers.id",
        window: TimeWindow.Tumbling(TimeSpan.FromMinutes(1))
    )
    .ToKafka("enriched-orders");
```

**After:**
```csharp
var env = Flink.GetExecutionEnvironment();

var orders = env.FromKafka("orders", "kafka:9093", "order-group");
var customers = env.FromKafka("customers", "kafka:9093", "customer-group");

var enriched = orders
    .Join(customers)
    .Where(order => order.CustomerId)
    .EqualTo(customer => customer.Id)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Apply((order, customer) => new EnrichedOrder(order, customer));

enriched.SinkToKafka("enriched-orders", "kafka:9093");
await env.ExecuteAsync("OrderEnrichment");
```

## Key Differences

### 1. Environment Setup

**Before:** Used dependency injection with `AddFlinkJobBuilder`
```csharp
services.AddFlinkJobBuilder(config => { ... });
```

**After:** Direct environment creation
```csharp
var env = Flink.GetExecutionEnvironment();
```

### 2. Stream Creation

**Before:** JobBuilder pattern
```csharp
var job = serviceProvider.CreateJobBuilder().FromKafka("topic")
```

**After:** Direct environment methods
```csharp
var stream = env.FromKafka("topic", "bootstrap-servers", "group-id")
```

### 3. String-based Operations

**Before:** String expressions for filters and maps
```csharp
.Filter("amount > 100")
.Map("x => x.toUpperCase()")
```

**After:** Type-safe lambda expressions
```csharp
.Filter(x => x.Amount > 100)
.Map(x => x.ToUpperInvariant())
```

### 4. Job Submission

**Before:** `.Submit()` method
```csharp
await job.Submit("JobName");
```

**After:** `ExecuteAsync()` on environment
```csharp
await env.ExecuteAsync("JobName");
```

## Configuration Migration

### Kafka Configuration

**Before:** Configured via JobBuilder settings
```csharp
services.AddFlinkJobBuilder(config =>
{
    config.KafkaBootstrapServers = "kafka:9092";
    config.KafkaGroupId = "my-group";
});
```

**After:** Passed directly to source/sink methods
```csharp
var stream = env.FromKafka(
    topic: "my-topic",
    bootstrapServers: "kafka:9092",
    groupId: "my-group"
);

stream.SinkToKafka("output-topic", "kafka:9092");
```

### Checkpoint Configuration

**Before:** Via JobBuilder configuration
```csharp
config.CheckpointInterval = TimeSpan.FromSeconds(30);
```

**After:** Via environment configuration
```csharp
env.EnableCheckpointing(TimeSpan.FromSeconds(30));
env.GetCheckpointConfig().SetCheckpointTimeout(TimeSpan.FromMinutes(1));
```

## Common Patterns

### Pattern 1: Simple ETL Pipeline

```csharp
var env = Flink.GetExecutionEnvironment();

// Source
var input = env.FromKafka("raw-data", "kafka:9093", "etl-group");

// Transform
var transformed = input
    .Filter(data => data.IsValid)
    .Map(data => Transform(data));

// Sink
transformed.SinkToKafka("processed-data", "kafka:9093");

await env.ExecuteAsync("ETL-Pipeline");
```

### Pattern 2: Real-time Aggregation

```csharp
var env = Flink.GetExecutionEnvironment();

var metrics = env.FromKafka("metrics", "kafka:9093", "metrics-group");

var aggregated = metrics
    .KeyBy(m => m.MetricName)
    .Window(TumblingTimeWindow.Of(TimeSpan.FromMinutes(1)))
    .Aggregate(new MetricAggregator());

aggregated.SinkToKafka("aggregated-metrics", "kafka:9093");

await env.ExecuteAsync("MetricsAggregation");
```

### Pattern 3: Stream Branching

```csharp
var env = Flink.GetExecutionEnvironment();

var events = env.FromKafka("events", "kafka:9093", "event-group");

var splitStream = events.Split(
    new OutputSelector<Event>() {
        e => e.Priority == "high" ? new[] { "high" } : new[] { "normal" }
    }
);

var highPriority = splitStream.Select("high");
var normalPriority = splitStream.Select("normal");

highPriority.SinkToKafka("high-priority-events", "kafka:9093");
normalPriority.SinkToKafka("normal-priority-events", "kafka:9093");

await env.ExecuteAsync("EventRouter");
```

## Testing Migration

### Unit Tests

**Before:**
```csharp
var services = new ServiceCollection();
services.AddFlinkJobBuilder(config => { ... });
var provider = services.BuildServiceProvider();
var job = provider.CreateJobBuilder()...
```

**After:**
```csharp
var env = Flink.GetExecutionEnvironment();
var stream = env.FromKafka(...)...
await env.ExecuteAsync("TestJob");
```

## Troubleshooting

### Issue: Missing AddFlinkJobBuilder Extension

**Solution:** Remove dependency injection setup. Use `Flink.GetExecutionEnvironment()` directly.

### Issue: String expressions don't work

**Solution:** Replace string expressions with type-safe lambda expressions:
- `"x > 100"` → `x => x > 100`
- `"x.toUpperCase()"` → `x => x.ToUpperInvariant()`

### Issue: Submit() method not found

**Solution:** Use `await env.ExecuteAsync("JobName")` instead of `await job.Submit("JobName")`

### Issue: Kafka configuration not applied

**Solution:** Pass Kafka bootstrap servers and group ID directly to `FromKafka()` and `SinkToKafka()` methods

## Additional Resources

- [FlinkDotNet DataStream API Documentation](api-reference.md)
- [Getting Started Guide](wiki/Getting-Started.md)
- [Usage Examples](wiki/Usage-Examples.md)
- [Apache Flink DataStream API](https://nightlies.apache.org/flink/flink-docs-stable/docs/dev/datastream/overview/)

## Support

For migration assistance:
- Check existing examples in [`LocalTesting/LocalTesting.IntegrationTests/`](../LocalTesting/LocalTesting.IntegrationTests/)
- Review [LearningCourse examples](../LearningCourse/)
- Open an issue on GitHub for specific migration questions