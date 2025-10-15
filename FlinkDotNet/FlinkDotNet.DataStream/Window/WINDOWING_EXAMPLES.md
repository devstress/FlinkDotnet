# FlinkDotNet Windowing API - Usage Examples

This document provides comprehensive examples of using the windowing operators in FlinkDotNet.

## Table of Contents
1. [Tumbling Windows](#tumbling-windows)
2. [Sliding Windows](#sliding-windows)
3. [Session Windows](#session-windows)
4. [Watermarks and Late Events](#watermarks-and-late-events)
5. [Window Functions](#window-functions)

## Tumbling Windows

Tumbling windows are fixed-size, non-overlapping windows. Each element belongs to exactly one window.

### Example 1: Count Events per 10-Second Window

```csharp
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window.Assigners;
using FlinkDotNet.DataStream.Watermarks;

public class RideCountAggregator : IAggregateFunction<Ride, long, long>
{
    public long CreateAccumulator() => 0;
    public long Add(Ride ride, long accumulator) => accumulator + 1;
    public long GetResult(long accumulator) => accumulator;
    public long Merge(long acc1, long acc2) => acc1 + acc2;
}

// Usage
var env = StreamExecutionEnvironment.GetExecutionEnvironment();

env.AddKafkaSource<Ride>("rides", "localhost:9092", "consumer-group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<Ride>(TimeSpan.FromSeconds(5))
        .WithTimestampAssigner(ride => ride.Timestamp))
    .KeyBy(ride => ride.DriverId)
    .Window(TumblingEventTimeWindows.Of<Ride>(Time.Seconds(10)))
    .Aggregate(new RideCountAggregator())
    .SinkToKafka("ride-counts", "localhost:9092");

await env.ExecuteAsync("Tumbling Window Example");
```

### Example 2: Sum Values in 1-Minute Windows

```csharp
public class TransactionSumAggregator : IAggregateFunction<Transaction, decimal, decimal>
{
    public decimal CreateAccumulator() => 0m;
    public decimal Add(Transaction txn, decimal accumulator) => accumulator + txn.Amount;
    public decimal GetResult(decimal accumulator) => accumulator;
    public decimal Merge(decimal acc1, decimal acc2) => acc1 + acc2;
}

// Usage
env.AddKafkaSource<Transaction>("transactions", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForMonotonousTimestamps<Transaction>()
        .WithTimestampAssigner(txn => txn.Timestamp))
    .KeyBy(txn => txn.AccountId)
    .Window(TumblingEventTimeWindows.Of<Transaction>(Time.Minutes(1)))
    .Aggregate(new TransactionSumAggregator())
    .SinkToKafka("account-totals", "localhost:9092");
```

## Sliding Windows

Sliding windows overlap, allowing elements to belong to multiple windows.

### Example 3: 5-Minute Window, 1-Minute Slide

```csharp
public class AverageSpeedAggregator : IAggregateFunction<SpeedReading, (double sum, int count), double>
{
    public (double sum, int count) CreateAccumulator() => (0.0, 0);
    
    public (double sum, int count) Add(SpeedReading reading, (double sum, int count) acc) 
        => (acc.sum + reading.Speed, acc.count + 1);
    
    public double GetResult((double sum, int count) acc) 
        => acc.count > 0 ? acc.sum / acc.count : 0.0;
    
    public (double sum, int count) Merge((double sum, int count) acc1, (double sum, int count) acc2) 
        => (acc1.sum + acc2.sum, acc1.count + acc2.count);
}

// Usage - 5-minute window slides every 1 minute
env.AddKafkaSource<SpeedReading>("speed-readings", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<SpeedReading>(TimeSpan.FromSeconds(10))
        .WithTimestampAssigner(reading => reading.Timestamp))
    .KeyBy(reading => reading.SensorId)
    .Window(SlidingEventTimeWindows.Of<SpeedReading>(
        Time.Minutes(5),  // Window size
        Time.Minutes(1)   // Slide interval
    ))
    .Aggregate(new AverageSpeedAggregator())
    .SinkToKafka("average-speeds", "localhost:9092");
```

### Example 4: Reduce Function with Sliding Window

```csharp
public class MaxTemperatureReducer : IReduceFunction<TemperatureReading>
{
    public TemperatureReading Reduce(TemperatureReading r1, TemperatureReading r2)
        => r1.Temperature > r2.Temperature ? r1 : r2;
}

// Usage
env.AddKafkaSource<TemperatureReading>("temperatures", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<TemperatureReading>(TimeSpan.FromSeconds(3))
        .WithTimestampAssigner(reading => reading.Timestamp))
    .KeyBy(reading => reading.LocationId)
    .Window(SlidingEventTimeWindows.Of<TemperatureReading>(
        Time.Hours(1),      // 1-hour window
        Time.Minutes(15)    // Slides every 15 minutes
    ))
    .Reduce(new MaxTemperatureReducer())
    .SinkToKafka("max-temperatures", "localhost:9092");
```

## Session Windows

Session windows group events based on inactivity gaps. Windows are created dynamically based on data patterns.

### Example 5: User Session Analysis

```csharp
public class SessionAggregator : IAggregateFunction<UserEvent, UserSession, UserSession>
{
    public UserSession CreateAccumulator() 
        => new UserSession { EventCount = 0, StartTime = long.MaxValue, EndTime = long.MinValue };
    
    public UserSession Add(UserEvent evt, UserSession session)
    {
        session.EventCount++;
        session.StartTime = Math.Min(session.StartTime, evt.Timestamp);
        session.EndTime = Math.Max(session.EndTime, evt.Timestamp);
        return session;
    }
    
    public UserSession GetResult(UserSession session) => session;
    
    public UserSession Merge(UserSession s1, UserSession s2)
    {
        return new UserSession
        {
            EventCount = s1.EventCount + s2.EventCount,
            StartTime = Math.Min(s1.StartTime, s2.StartTime),
            EndTime = Math.Max(s1.EndTime, s2.EndTime)
        };
    }
}

// Usage - 30-second inactivity gap
env.AddKafkaSource<UserEvent>("user-events", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<UserEvent>(TimeSpan.FromSeconds(5))
        .WithTimestampAssigner(evt => evt.Timestamp))
    .KeyBy(evt => evt.UserId)
    .Window(SessionWindows.WithGap<UserEvent>(Time.Seconds(30)))
    .Aggregate(new SessionAggregator())
    .SinkToKafka("user-sessions", "localhost:9092");
```

## Watermarks and Late Events

Watermarks control when windows fire and handle out-of-order events.

### Example 6: Bounded Out-of-Orderness

```csharp
// Allow events up to 10 seconds late
var watermarkStrategy = WatermarkStrategy
    .ForBoundedOutOfOrderness<Event>(TimeSpan.FromSeconds(10))
    .WithTimestampAssigner(evt => evt.EventTime);

env.AddKafkaSource<Event>("events", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(watermarkStrategy)
    .KeyBy(evt => evt.Key)
    .Window(TumblingEventTimeWindows.Of<Event>(Time.Minutes(1)))
    .Aggregate(new EventCountAggregator())
    .SinkToKafka("results", "localhost:9092");
```

### Example 7: Monotonous Timestamps

```csharp
// For perfectly ordered streams
var watermarkStrategy = WatermarkStrategy
    .ForMonotonousTimestamps<LogEntry>()
    .WithTimestampAssigner(log => log.Timestamp);

env.AddKafkaSource<LogEntry>("logs", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(watermarkStrategy)
    .KeyBy(log => log.ServiceName)
    .Window(TumblingEventTimeWindows.Of<LogEntry>(Time.Seconds(5)))
    .Aggregate(new LogCountAggregator())
    .SinkToKafka("log-counts", "localhost:9092");
```

## Window Functions

### Example 8: Process Window Function (Full Window Access)

```csharp
using FlinkDotNet.DataStream.Window.Functions;

public class WindowStatisticsFunction : IProcessWindowFunction<MetricEvent, WindowStats, string, TimeWindow>
{
    public IEnumerable<WindowStats> Process(
        string key, 
        Context context, 
        IEnumerable<MetricEvent> elements)
    {
        var list = elements.ToList();
        var stats = new WindowStats
        {
            Key = key,
            WindowStart = context.Window.Start,
            WindowEnd = context.Window.End,
            Count = list.Count,
            Average = list.Average(e => e.Value),
            Min = list.Min(e => e.Value),
            Max = list.Max(e => e.Value)
        };
        
        yield return stats;
    }
}

// Usage
env.AddKafkaSource<MetricEvent>("metrics", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<MetricEvent>(TimeSpan.FromSeconds(5))
        .WithTimestampAssigner(evt => evt.Timestamp))
    .KeyBy(evt => evt.MetricName)
    .Window(TumblingEventTimeWindows.Of<MetricEvent>(Time.Minutes(5)))
    .Process(new WindowStatisticsFunction())
    .SinkToKafka("metric-stats", "localhost:9092");
```

### Example 9: Combining Incremental Aggregation with Full Window Context

```csharp
// First do incremental aggregation for efficiency
public class CountAggregator : IAggregateFunction<Event, long, long>
{
    public long CreateAccumulator() => 0;
    public long Add(Event evt, long acc) => acc + 1;
    public long GetResult(long acc) => acc;
    public long Merge(long acc1, long acc2) => acc1 + acc2;
}

// Then use process function for context
public class EnrichWithWindowInfo : IProcessWindowFunction<long, EnrichedCount, string, TimeWindow>
{
    public IEnumerable<EnrichedCount> Process(string key, Context context, IEnumerable<long> elements)
    {
        var count = elements.First();
        yield return new EnrichedCount
        {
            Key = key,
            Count = count,
            WindowStart = context.Window.Start,
            WindowEnd = context.Window.End,
            ProcessingTime = context.CurrentProcessingTime
        };
    }
}

// Usage (combine aggregate + process)
env.AddKafkaSource<Event>("events", "localhost:9092", "group")
    .AssignTimestampsAndWatermarks(WatermarkStrategy
        .ForBoundedOutOfOrderness<Event>(TimeSpan.FromSeconds(5))
        .WithTimestampAssigner(evt => evt.Timestamp))
    .KeyBy(evt => evt.Category)
    .Window(TumblingEventTimeWindows.Of<Event>(Time.Minutes(1)))
    .Aggregate(new CountAggregator())
    .SinkToKafka("enriched-counts", "localhost:9092");
```

## Complete Example: Real-Time Dashboard

```csharp
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window.Assigners;
using FlinkDotNet.DataStream.Watermarks;

public class DashboardMetricsJob
{
    public static async Task Main(string[] args)
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetParallelism(4);
        
        // Read clickstream events
        var clicks = env.AddKafkaSource<ClickEvent>(
            "clickstream", 
            "localhost:9092", 
            "dashboard-group"
        );
        
        // Assign watermarks (allow 5 seconds for late events)
        var clicksWithWatermarks = clicks
            .AssignTimestampsAndWatermarks(WatermarkStrategy
                .ForBoundedOutOfOrderness<ClickEvent>(TimeSpan.FromSeconds(5))
                .WithTimestampAssigner(click => click.Timestamp));
        
        // Calculate clicks per minute (tumbling window)
        clicksWithWatermarks
            .KeyBy(click => click.PageId)
            .Window(TumblingEventTimeWindows.Of<ClickEvent>(Time.Minutes(1)))
            .Aggregate(new ClickCountAggregator())
            .SinkToKafka("page-views-minute", "localhost:9092");
        
        // Calculate moving average (sliding window)
        clicksWithWatermarks
            .KeyBy(click => click.PageId)
            .Window(SlidingEventTimeWindows.Of<ClickEvent>(
                Time.Minutes(10),  // 10-minute window
                Time.Minutes(1)    // Update every minute
            ))
            .Aggregate(new MovingAverageAggregator())
            .SinkToKafka("page-views-moving-avg", "localhost:9092");
        
        // Detect user sessions (session window)
        clicksWithWatermarks
            .KeyBy(click => click.UserId)
            .Window(SessionWindows.WithGap<ClickEvent>(Time.Minutes(30)))
            .Aggregate(new SessionAnalysisAggregator())
            .SinkToKafka("user-sessions", "localhost:9092");
        
        await env.ExecuteAsync("Real-Time Dashboard Metrics");
    }
}
```

## API Reference

### Window Assigners
- `TumblingEventTimeWindows.Of<T>(Time size)` - Fixed non-overlapping windows
- `SlidingEventTimeWindows.Of<T>(Time size, Time slide)` - Overlapping windows
- `SessionWindows.WithGap<T>(Time gap)` - Dynamic session windows

### Watermark Strategies
- `WatermarkStrategy.ForBoundedOutOfOrderness<T>(TimeSpan)` - Handle late events
- `WatermarkStrategy.ForMonotonousTimestamps<T>()` - For ordered streams

### Window Functions
- `IAggregateFunction<TIn, TAcc, TOut>` - Incremental aggregation (most efficient)
- `IReduceFunction<T>` - Combining elements (simple aggregation)
- `IProcessWindowFunction<TIn, TOut, TKey, TWindow>` - Full window access

### Time Utilities
- `Time.Milliseconds(long)`, `Time.Seconds(long)`, `Time.Minutes(long)`
- `Time.Hours(long)`, `Time.Days(long)`