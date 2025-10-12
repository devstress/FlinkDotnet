# Apache Flink 2.1 Features in FlinkDotNet

FlinkDotNet provides comprehensive support for Apache Flink 2.1 features through native C# APIs.

## Dynamic Scaling and Resource Management

### Adaptive Scheduler

Automatically optimizes parallelism based on workload characteristics.

```csharp
var env = Flink.GetExecutionEnvironment()
    .EnableAdaptiveScheduler()
    .SetMaxParallelism(128);
```

### Reactive Mode

Elastic scaling that adapts to available cluster resources.

```csharp
var env = Flink.GetExecutionEnvironment()
    .EnableReactiveMode()
    .SetParallelism(8);  // Initial parallelism
```

### Savepoint-based Scaling

Scale jobs using savepoints for state consistency.

```csharp
// Execute job and get JobClient
var jobClient = await env.ExecuteAsyncJob("my-job");

// Create savepoint for scaling
var savepointResult = await jobClient.TriggerSavepointAsync("/savepoints/scaling");

// Stop with savepoint
var stopResult = await jobClient.StopWithSavepointAsync(
    savepointPath: savepointResult.SavepointPath,
    drain: true
);

// Restart with new parallelism
var scaledEnv = Flink.GetExecutionEnvironment()
    .FromSavepoint(stopResult.SavepointPath)
    .SetParallelism(16)  // New parallelism
    .SetMaxParallelism(256);

await scaledEnv.ExecuteAsyncJob("scaled-job");
```

## Advanced Partitioning Strategies

### Rebalance
Uniform distribution across all operators.

```csharp
stream.Map(x => x * 2)
    .Rebalance()  // Round-robin distribution
    .SetParallelism(8)
```

### Rescale
Efficient distribution to subset of operators.

```csharp
stream.Map(x => x * 3)
    .Rescale()   // Local round-robin
    .Filter(x => x > 10)
```

### Forward
Direct one-to-one forwarding.

```csharp
stream.Forward()   // Same parallelism required
    .Map(x => x + 1)
```

### Shuffle
Random distribution.

```csharp
stream.Shuffle()   // Random partitioning
    .Map(x => x * 2)
```

### Broadcast
Send to all parallel instances.

```csharp
stream.Broadcast() // All instances receive all elements
    .Map(x => x + 10)
```

### Custom Partitioning

```csharp
stream.PartitionCustom(
    (key, numPartitions) => key % numPartitions,  // Partitioner
    x => x.GetHashCode()                          // Key selector
)
```

## Fine-Grained Resource Management

### Slot Sharing Groups

```csharp
// Heavy operations in dedicated slots
stream1.Map(new HeavyProcessor())
    .SetParallelism(4)
    .SlotSharingGroup("heavy-processing");

// Light operations share slots
stream2.Map(new LightProcessor())
    .SetParallelism(8)
    .SlotSharingGroup("light-processing");
```

### Resource Profiles

```csharp
env.SetParallelism(8)
   .SetMaxParallelism(128)
   .EnableSlotSharing();  // Resource optimization
```

## Enhanced Checkpointing

### Checkpoint Configuration

```csharp
env.EnableCheckpointing(5000);  // Every 5 seconds

env.GetCheckpointConfig()
    .SetCheckpointingMode(CheckpointingMode.EXACTLY_ONCE)
    .SetMinPauseBetweenCheckpoints(1000)
    .SetCheckpointTimeout(60000)
    .SetMaxConcurrentCheckpoints(1)
    .EnableExternalizedCheckpoints(
        ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);
```

### Savepoint Operations

```csharp
// Trigger savepoint
var savepoint = await jobClient.TriggerSavepointAsync("/savepoints/manual");

// Cancel with savepoint
var cancelResult = await jobClient.CancelWithSavepointAsync();

// Dispose savepoint
await jobClient.DisposeSavepointAsync("/savepoints/old");
```

## Advanced Restart Strategies

### Exponential Delay

```csharp
env.SetRestartStrategy(RestartStrategies.ExponentialDelayRestart(
    maxAttempts: 10,
    initialDelay: Time.Seconds(1),
    maxDelay: Time.Minutes(5),
    multiplier: 2.0
));
```

### Fixed Delay

```csharp
env.SetRestartStrategy(RestartStrategies.FixedDelayRestart(
    restartAttempts: 3,
    delayBetweenAttempts: Time.Seconds(10)
));
```

### Failure Rate

```csharp
env.SetRestartStrategy(RestartStrategies.FailureRateRestart(
    maxFailuresPerInterval: 5,
    failureInterval: Time.Minutes(5),
    delayBetweenAttempts: Time.Seconds(10)
));
```

## Watermark Strategies

### Bounded Out-of-Orderness

```csharp
stream.AssignTimestampsAndWatermarks(
    WatermarkStrategy
        .ForBoundedOutOfOrderness<Event>(Duration.OfSeconds(5))
        .WithTimestampAssigner(e => e.Timestamp.ToUnixTimeMilliseconds())
        .WithIdleness(Duration.OfSeconds(60))  // Handle idle sources
);
```

### Monotonous Timestamps

```csharp
stream.AssignTimestampsAndWatermarks(
    WatermarkStrategy
        .ForMonotonousTimestamps<Event>()
        .WithTimestampAssigner(e => e.Timestamp)
);
```

### Custom Watermark Generator

```csharp
public class CustomWatermarkGenerator : IWatermarkGenerator<Event>
{
    private long maxTimestamp = long.MinValue;
    private readonly long outOfOrdernessMillis = 5000;
    
    public void OnEvent(Event event, long eventTimestamp, IWatermarkOutput output)
    {
        maxTimestamp = Math.Max(maxTimestamp, eventTimestamp);
    }
    
    public void OnPeriodicEmit(IWatermarkOutput output)
    {
        output.EmitWatermark(new Watermark(maxTimestamp - outOfOrdernessMillis));
    }
}

stream.AssignTimestampsAndWatermarks(
    WatermarkStrategy.ForGenerator(() => new CustomWatermarkGenerator())
        .WithTimestampAssigner(e => e.Timestamp)
);
```

## Job Monitoring and Control

### JobClient Operations

```csharp
// Get job status
var status = await jobClient.GetJobStatusAsync();
Console.WriteLine($"State: {status.State}");
Console.WriteLine($"Parallelism: {status.Parallelism}/{status.MaxParallelism}");

// Get execution plan
var plan = await jobClient.GetExecutionPlanAsync();

// Cancel job
await jobClient.CancelAsync();

// Stop job gracefully
await jobClient.StopAsync();
```

### Metrics Access

```csharp
// Get job metrics
var metrics = await jobClient.GetMetricsAsync();
foreach (var metric in metrics)
{
    Console.WriteLine($"{metric.Name}: {metric.Value}");
}
```

## Performance Optimizations

### Object Reuse

```csharp
env.GetConfig().EnableObjectReuse();  // Reduce GC pressure
```

### Operator Chaining

```csharp
// Disable chaining for specific operator
stream.Map(new HeavyOperation())
    .DisableChaining();

// Start new chain
stream.Map(new Operation1())
    .StartNewChain()
    .Map(new Operation2());
```

### Buffer Timeout

```csharp
env.SetBufferTimeout(100);  // Milliseconds
```

## API Mapping: Java Flink → FlinkDotNet

| Java Flink API | FlinkDotNet C# API | Notes |
|----------------|-------------------|-------|
| `StreamExecutionEnvironment.getExecutionEnvironment()` | `Flink.GetExecutionEnvironment()` | Static method |
| `env.setParallelism(8)` | `env.SetParallelism(8)` | Fluent API |
| `stream.map(new MyMapper())` | `stream.Map(new MyMapper())` | IMapFunction<TIn, TOut> |
| `stream.filter(new MyFilter())` | `stream.Filter(new MyFilter())` | IFilterFunction<T> |
| `stream.keyBy(e -> e.key)` | `stream.KeyBy(e => e.Key)` | Lambda expressions |
| `stream.timeWindowAll(Time.hours(24))` | `stream.TimeWindowAll(Time.Hours(24))` | Time helper class |
| `env.execute("job-name")` | `await env.ExecuteAsync("job-name")` | Async by default |
| `env.executeAsync("job-name")` | `await env.ExecuteAsyncJob("job-name")` | Returns JobClient |

## Complete Example

```csharp
using FlinkDotNet.DataStream;

var env = Flink.GetExecutionEnvironment();

// Configure Flink 2.1 features
env.SetParallelism(8)
   .SetMaxParallelism(128)
   .EnableAdaptiveScheduler()      // Flink 2.1 adaptive scheduler
   .EnableReactiveMode()           // Flink 2.1 reactive mode
   .EnableCheckpointing(5000)
   .SetRestartStrategy("exponential-delay");

// Create stream with timestamp assignment
var events = env.FromKafka("events", "kafka:9093", "my-group")
    .AssignTimestampsAndWatermarks(
        WatermarkStrategy
            .ForBoundedOutOfOrderness<Event>(Duration.OfSeconds(5))
            .WithTimestampAssigner(e => e.Timestamp)
    );

// Process with dynamic partitioning
var processed = events
    .Map(new EnrichEvent())
    .SetParallelism(4)
    .SlotSharingGroup("enrichment")
    .Rebalance()                    // Flink 2.1 rebalancing
    .Filter(new ValidateEvent())
    .SetParallelism(8)
    .KeyBy(e => e.CustomerId)
    .TimeWindow(Time.Minutes(5))
    .Aggregate(new AggregateEvents());

// Sink with exactly-once guarantees
processed.SinkToKafka("results", "kafka:9093");

// Execute with monitoring
var jobClient = await env.ExecuteAsyncJob("flink21-example");

// Monitor execution
var status = await jobClient.GetJobStatusAsync();
Console.WriteLine($"Job running with parallelism: {status.Parallelism}");
```

---

## See Also

- [API Reference](api-reference.md) - Complete DataStream API
- [Architecture Guide](architecture-and-usecases.md) - System design
- [Performance Benchmarks](performance-benchmarks.md) - Throughput metrics