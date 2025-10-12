# FlinkDotNet API Reference

Complete reference for FlinkDotNet DataStream API, compatible with Apache Flink 2.1.

## Table of Contents

1. [StreamExecutionEnvironment](#streamexecutionenvironment)
2. [DataStream Operations](#datastream-operations)
3. [Sources and Sinks](#sources-and-sinks)
4. [Windowing](#windowing)
5. [State Management](#state-management)
6. [Time and Watermarks](#time-and-watermarks)
7. [Partitioning Strategies](#partitioning-strategies)
8. [JobBuilder Fluent API](#jobbuilder-fluent-api)

## StreamExecutionEnvironment

Entry point for creating Flink DataStream jobs.

### Creating Environment

```csharp
using FlinkDotNet.DataStream;

// Get default execution environment
var env = Flink.GetExecutionEnvironment();

// Get environment with configuration
var env = Flink.GetExecutionEnvironment(new ExecutionConfig
{
    Parallelism = 4,
    MaxParallelism = 128,
    RestartStrategy = "exponential-delay"
});
```

### Configuration Methods

```csharp
// Set parallelism
env.SetParallelism(8);
env.SetMaxParallelism(128);

// Enable Apache Flink 2.1 features
env.EnableAdaptiveScheduler();     // Intelligent resource management
env.EnableReactiveMode();          // Elastic scaling
env.EnableCheckpointing(5000);     // Checkpoint every 5 seconds

// Configure time characteristic
env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

// Enable object reuse for performance
env.GetConfig().EnableObjectReuse();
```

### Job Execution

```csharp
// Execute synchronously
await env.ExecuteAsync("my-job-name");

// Execute asynchronously and get JobClient
var jobClient = await env.ExecuteAsyncJob("my-job-name");

// Restore from savepoint
var env = Flink.GetExecutionEnvironment()
    .FromSavepoint("/path/to/savepoint")
    .SetParallelism(16);
```

## DataStream Operations

### Transformation Operations

#### Map
Transform each element to another element (1-to-1).

```csharp
var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

// Using lambda
var doubled = stream.Map(x => x * 2);

// Using MapFunction
public class Doubler : IMapFunction<int, int>
{
    public int Map(int value) => value * 2;
}

var doubled = stream.Map(new Doubler());
```

#### FlatMap
Transform each element to zero or more elements (1-to-N).

```csharp
// Split strings into words
var words = sentences.FlatMap(sentence => sentence.Split(' '));

// Using FlatMapFunction
public class WordSplitter : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string value)
    {
        return value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }
}

var words = sentences.FlatMap(new WordSplitter());
```

#### Filter
Keep only elements that match a predicate.

```csharp
// Using lambda
var evens = numbers.Filter(x => x % 2 == 0);

// Using FilterFunction
public class EvenFilter : IFilterFunction<int>
{
    public bool Filter(int value) => value % 2 == 0;
}

var evens = numbers.Filter(new EvenFilter());
```

#### KeyBy
Partition stream by key for keyed operations.

```csharp
// Key by field
var keyed = orders.KeyBy(order => order.CustomerId);

// Key by computed value
var keyed = events.KeyBy(e => e.Timestamp.Date);

// Multiple keys
var keyed = data.KeyBy(d => (d.Region, d.Category));
```

### Aggregation Operations

```csharp
// Sum
var total = numbers.Sum();
var totalAmount = orders.KeyBy(o => o.CustomerId).Sum(o => o.Amount);

// Min/Max
var min = numbers.Min();
var max = numbers.Max();

// MinBy/MaxBy (return full element)
var cheapest = products.MinBy(p => p.Price);
var mostExpensive = products.MaxBy(p => p.Price);

// Reduce
var sum = numbers.Reduce((a, b) => a + b);

// Aggregate (custom accumulator)
var stats = numbers.Aggregate(
    createAccumulator: () => new Stats(),
    add: (value, acc) => acc.Add(value),
    getResult: acc => acc.Compute(),
    merge: (acc1, acc2) => acc1.Merge(acc2)
);
```

### Joining Streams

```csharp
// Join on key
var joined = stream1
    .Join(stream2)
    .Where(e1 => e1.Key)
    .EqualTo(e2 => e2.Key)
    .Window(TumblingEventTimeWindows.Of(Time.Minutes(5)))
    .Apply((e1, e2) => new { e1.Value, e2.Data });

// CoGroup (full outer join)
var coGrouped = stream1
    .CoGroup(stream2)
    .Where(e1 => e1.Key)
    .EqualTo(e2 => e2.Key)
    .Window(TumblingEventTimeWindows.Of(Time.Minutes(5)))
    .Apply((group1, group2) => ProcessGroups(group1, group2));
```

## Sources and Sinks

### Kafka Source

```csharp
// Simple Kafka source
var stream = env.FromKafka(
    topic: "input-topic",
    bootstrapServers: "kafka:9093",
    groupId: "my-consumer-group",
    startingOffsets: "earliest"  // or "latest"
);

// Kafka source with configuration
var stream = env.AddKafkaSource(
    topic: "input-topic",
    servers: "kafka:9093",
    groupId: "my-group",
    deserializer: new MyDeserializer(),
    startingOffsets: "earliest"
);

// Kafka source with multiple topics
var stream = env.FromKafka(
    topics: new[] { "topic1", "topic2", "topic3" },
    bootstrapServers: "kafka:9093",
    groupId: "multi-topic-consumer"
);
```

### Kafka Sink

```csharp
// Simple Kafka sink
stream.SinkToKafka("output-topic", "kafka:9093");

// Kafka sink with serializer
stream.AddSink(new KafkaSink<MyType>(
    topic: "output-topic",
    servers: "kafka:9093",
    serializer: new MySerializer()
));
```

### Collection Source

```csharp
// From array
var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

// From list
var stream = env.FromCollection(myList);

// From enumerable
var stream = env.FromCollection(Enumerable.Range(1, 100));
```

### File Source

```csharp
// Read text file
var lines = env.ReadTextFile("/path/to/file.txt");

// Read CSV file
var records = env.ReadCsvFile<MyRecord>("/path/to/data.csv");
```

### Custom Source

```csharp
public class CustomSource : ISourceFunction<MyType>
{
    private volatile bool isRunning = true;
    
    public void Run(ISourceContext<MyType> ctx)
    {
        while (isRunning)
        {
            var element = GenerateElement();
            ctx.Collect(element);
        }
    }
    
    public void Cancel()
    {
        isRunning = false;
    }
}

var stream = env.AddSource(new CustomSource());
```

## Windowing

### Window Types

#### Tumbling Windows
Fixed-size, non-overlapping windows.

```csharp
// Tumbling time window (event time)
stream.TimeWindow(Time.Minutes(5))
    .Aggregate(new MyAggregator());

// Tumbling count window
stream.CountWindow(100)
    .Reduce((a, b) => a + b);

// Global tumbling window (all parallelism merged)
stream.TimeWindowAll(Time.Hours(1))
    .Aggregate(new DailyAggregator());
```

#### Sliding Windows
Fixed-size, overlapping windows.

```csharp
// Sliding time window (size=10min, slide=5min)
stream.TimeWindow(Time.Minutes(10), Time.Minutes(5))
    .Aggregate(new MyAggregator());

// Sliding count window (size=100, slide=50)
stream.CountWindow(100, 50)
    .Sum();
```

#### Session Windows
Dynamic windows based on inactivity gap.

```csharp
// Session window with 30-minute gap
stream.Window(SessionWindows.WithGap(Time.Minutes(30)))
    .Aggregate(new SessionAggregator());
```

### Window Operations

```csharp
// Window with aggregate function
stream.TimeWindow(Time.Minutes(5))
    .Aggregate(new MyAggregator());

// Window with reduce function
stream.TimeWindow(Time.Minutes(5))
    .Reduce((a, b) => a + b);

// Window with process function (full window access)
stream.TimeWindow(Time.Minutes(5))
    .Process(new MyWindowProcessor());

// Allowed lateness
stream.TimeWindow(Time.Minutes(5))
    .AllowedLateness(Time.Minutes(1))
    .Aggregate(new MyAggregator());
```

## State Management

### Keyed State

```csharp
public class StatefulProcessor : IKeyedProcessFunction<string, Event, Result>
{
    private ValueState<int> counterState;
    private ListState<Event> historyState;
    
    public void Open(Configuration config)
    {
        counterState = GetRuntimeContext()
            .GetState(new ValueStateDescriptor<int>("counter"));
        
        historyState = GetRuntimeContext()
            .GetListState(new ListStateDescriptor<Event>("history"));
    }
    
    public void ProcessElement(Event value, IContext ctx, ICollector<Result> out)
    {
        // Read and update state
        int count = counterState.Value();
        counterState.Update(count + 1);
        
        // Add to list state
        historyState.Add(value);
        
        out.Collect(new Result(value, count));
    }
}
```

### State Types

```csharp
// ValueState - single value per key
var valueState = GetRuntimeContext()
    .GetState(new ValueStateDescriptor<int>("myValue"));

// ListState - list of values per key
var listState = GetRuntimeContext()
    .GetListState(new ListStateDescriptor<string>("myList"));

// MapState - key-value map per key
var mapState = GetRuntimeContext()
    .GetMapState(new MapStateDescriptor<string, int>("myMap"));

// ReducingState - single value with reduce function
var reducingState = GetRuntimeContext()
    .GetReducingState(new ReducingStateDescriptor<int>(
        "sum", (a, b) => a + b));

// AggregatingState - single value with aggregation logic
var aggState = GetRuntimeContext()
    .GetAggregatingState(new AggregatingStateDescriptor<Event, Stats, Result>(
        "stats", new MyAggregator()));
```

## Time and Watermarks

### Time Characteristics

```csharp
// Event time (from data)
env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

// Processing time (system time)
env.SetStreamTimeCharacteristic(TimeCharacteristic.ProcessingTime);

// Ingestion time (time when entering Flink)
env.SetStreamTimeCharacteristic(TimeCharacteristic.IngestionTime);
```

### Timestamp Assignment

```csharp
// Assign timestamps with bounded out-of-orderness
stream.AssignTimestampsAndWatermarks(
    WatermarkStrategy
        .ForBoundedOutOfOrderness<Event>(Duration.OfSeconds(5))
        .WithTimestampAssigner(e => e.Timestamp.ToUnixTimeMilliseconds())
);

// Assign timestamps with periodic watermarks
public class MyTimestampAssigner : IAssignerWithPeriodicWatermarks<Event>
{
    private long maxTimestamp = long.MinValue;
    
    public long ExtractTimestamp(Event element, long previousTimestamp)
    {
        maxTimestamp = Math.Max(maxTimestamp, element.Timestamp);
        return element.Timestamp;
    }
    
    public Watermark GetCurrentWatermark()
    {
        return new Watermark(maxTimestamp - 1000); // 1 second behind
    }
}

stream.AssignTimestampsAndWatermarks(new MyTimestampAssigner());
```

## Partitioning Strategies

### Rebalance
Uniformly distribute across all parallel instances.

```csharp
stream.Rebalance()  // Round-robin distribution
```

### Rescale
Distribute to subset of parallel instances (more efficient).

```csharp
stream.Rescale()  // Local round-robin
```

### Forward
Direct forwarding (requires same parallelism).

```csharp
stream.Forward()  // One-to-one forwarding
```

### Shuffle
Random distribution.

```csharp
stream.Shuffle()  // Random partitioning
```

### Broadcast
Send to all parallel instances.

```csharp
stream.Broadcast()  // All instances receive all elements
```

### Custom Partitioning

```csharp
stream.PartitionCustom(
    (key, numPartitions) => key.GetHashCode() % numPartitions,
    element => element.CustomerId
)
```

## JobBuilder Fluent API

Alternative high-level API for rapid development.

```csharp
using Flink.JobBuilder;

var job = Flink.JobBuilder
    .FromKafka("input-topic", config => {
        config.BootstrapServers = "kafka:9093";
        config.GroupId = "my-group";
    })
    .Map("value = transform(data)")
    .Where("value.IsValid")
    .GroupBy("CustomerId")
    .Window("TUMBLING", 5, "MINUTES")
    .Aggregate("SUM", "Amount")
    .ToKafka("output-topic");

// Configure and submit
await job.Configure(config => {
    config.SetParallelism(8);
    config.EnableAdaptiveScheduler();
}).Submit("my-job-name");
```

## Best Practices

### Performance

```csharp
// Enable object reuse
env.GetConfig().EnableObjectReuse();

// Set appropriate parallelism
env.SetParallelism(kafkaPartitionCount);

// Use keyed operations for stateful processing
stream.KeyBy(e => e.Key)
    .Map(new StatefulMapper());

// Configure checkpointing
env.EnableCheckpointing(30000);  // Every 30 seconds
env.GetCheckpointConfig()
    .SetMinPauseBetweenCheckpoints(10000);
```

### Resource Management

```csharp
// Slot sharing groups
stream1.Map(new HeavyOperation())
    .SlotSharingGroup("heavy-operations");

stream2.Map(new LightOperation())
    .SlotSharingGroup("light-operations");

// Set operator-specific parallelism
stream.Map(new Transformer())
    .SetParallelism(4)  // Override default
```

### Fault Tolerance

```csharp
// Configure restart strategy
env.SetRestartStrategy(RestartStrategies.ExponentialDelayRestart(
    maxAttempts: 10,
    initialDelay: Time.Seconds(1),
    maxDelay: Time.Minutes(5),
    multiplier: 2.0
));

// Exactly-once checkpointing
env.GetCheckpointConfig()
    .SetCheckpointingMode(CheckpointingMode.EXACTLY_ONCE);
```

---

## See Also

- [Architecture Guide](architecture-and-usecases.md) - System design and scaling
- [Flink 2.1 Features](flink-21-features.md) - Apache Flink 2.1 compatibility
- [Getting Started](wiki/Getting-Started.md) - Setup and first job
- [Examples](../LearningCourse/README.md) - Hands-on learning course