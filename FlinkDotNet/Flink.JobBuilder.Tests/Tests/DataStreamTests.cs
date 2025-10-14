using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class DataStreamTests
{
    #region Map Transformation Tests

    [Test]
    public void Map_WithFunction_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var mapped = stream.Map(s => s.ToUpper());
        
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void Map_WithTransformation_ReturnsTypedDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "1", "2", "3" });
        
        var mapped = stream.Map(s => int.Parse(s));
        
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Map_WithExpression_ReturnsStringDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");
        
        var mapped = stream.Map("upper");
        
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }

    #endregion

    #region Filter Transformation Tests

    [Test]
    public void Filter_WithPredicate_ReturnsSameTypeDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var filtered = stream.Filter(x => x > 3);
        
        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Where_WithExpression_ReturnsSameTypeDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var filtered = stream.Where("length > 0");
        
        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<string>>());
    }

    #endregion

    #region FlatMap Transformation Tests

    [Test]
    public void FlatMap_WithFunction_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a,b", "c,d" });
        
        var flatMapped = stream.FlatMap<string>(s => s.Split(','));
        
        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void FlatMap_WithTransformation_ReturnsTypedDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        var flatMapped = stream.FlatMap<int>(x => new[] { x, x * 2 });
        
        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region KeyBy and GroupBy Tests

    [Test]
    public void KeyBy_WithKeySelector_ReturnsKeyedStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "apple", "banana", "avocado" });
        
        var keyed = stream.KeyBy(s => s[0]);
        
        Assert.That(keyed, Is.Not.Null);
        Assert.That(keyed, Is.TypeOf<KeyedStream<string, char>>());
    }

    [Test]
    public void GroupBy_WithKeyField_ReturnsKeyedStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var grouped = stream.GroupBy("field");
        
        Assert.That(grouped, Is.Not.Null);
        Assert.That(grouped, Is.TypeOf<KeyedStream<string, string>>());
    }

    #endregion

    #region Sink Tests

    [Test]
    public void Print_ReturnsSameDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Print();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void SinkToKafka_WithIRBackedStream_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");
        
        var result = stream.SinkToKafka("output-topic", "localhost:9092");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void SinkToKafka_WithSerializer_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("input-topic", "localhost:9092", "test-group", s => int.Parse(s));
        
        var result = stream.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void SetParallelism_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.SetParallelism(4);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void Name_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Name("test-stream");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void SetMaxParallelism_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.SetMaxParallelism(128);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void SlotSharingGroup_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.SlotSharingGroup("group-1");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    #endregion

    #region Partitioning Tests

    [Test]
    public void Rebalance_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Rebalance();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void Rescale_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Rescale();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void Forward_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Forward();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void Shuffle_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Shuffle();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void Broadcast_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.Broadcast();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void PartitionCustom_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.PartitionCustom<char>(
            (key, numPartitions) => key % numPartitions,
            s => s[0]);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    #endregion

    #region Window Tests

    [Test]
    public void TimeWindowAll_ReturnsAllWindowedStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        
        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed, Is.TypeOf<AllWindowedStream<int>>());
    }

    [Test]
    public void CountWindowAll_ReturnsAllWindowedStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        var windowed = stream.CountWindowAll(10);
        
        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed, Is.TypeOf<AllWindowedStream<int>>());
    }

    #endregion

    #region Environment Access Tests

    [Test]
    public void GetExecutionEnvironment_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var retrievedEnv = stream.GetExecutionEnvironment();
        
        Assert.That(retrievedEnv, Is.Not.Null);
        Assert.That(retrievedEnv, Is.SameAs(env));
    }

    #endregion

    #region Fluent API Tests

    [Test]
    public void FluentAPI_ChainingTransformations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromCollection(new[] { "apple", "banana", "avocado" })
            .Filter(s => s.Length > 4)
            .Map(s => s.ToUpper())
            .SetParallelism(2)
            .Name("test-pipeline")
            .Rebalance();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }

    #endregion
    
    #region Function Interface Tests
    
    [Test]
    public void Map_WithIMapFunctionInterface_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "hello", "world" });
        var mapFunc = new TestMapFunction();
        
        var mapped = stream.Map(mapFunc);
        
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void Filter_WithIFilterFunctionInterface_ReturnsSameTypeDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var filterFunc = new TestFilterFunction();
        
        var filtered = stream.Filter(filterFunc);
        
        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void FlatMap_WithIFlatMapFunctionInterface_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a,b", "c,d" });
        var flatMapFunc = new TestFlatMapFunction();
        
        var flatMapped = stream.FlatMap(flatMapFunc);
        
        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void AddSink_WithISinkFunctionInterface_ReturnsSameDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        var sinkFunc = new TestSinkFunction();
        
        var result = stream.AddSink(sinkFunc);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }
    
    #endregion
    
    #region Edge Case and Error Tests
    
    [Test]
    public void Map_WithExpressionOnNonStringStream_ThrowsNotSupportedException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        Assert.Throws<NotSupportedException>(() => stream.Map("upper"));
    }
    
    [Test]
    public void Map_WithExpressionOnCollectionStream_ThrowsInvalidOperationException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<InvalidOperationException>(() => stream.Map("upper"));
    }
    
    [Test]
    public void CountWindowAll_WithZeroSize_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        Assert.Throws<ArgumentException>(() => stream.CountWindowAll(0));
    }
    
    [Test]
    public void CountWindowAll_WithNegativeSize_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        Assert.Throws<ArgumentException>(() => stream.CountWindowAll(-5));
    }
    
    [Test]
    public void SetMaxParallelism_WithZero_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(0));
    }
    
    [Test]
    public void SetMaxParallelism_WithNegative_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(-1));
    }
    
    [Test]
    public void SetMaxParallelism_WithTooLarge_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(32769));
    }
    
    [Test]
    public void SetMaxParallelism_WithValidValue_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.SetMaxParallelism(128);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }
    
    [Test]
    public void SetMaxParallelism_WithMaxValue_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        var result = stream.SetMaxParallelism(32768); // Maximum allowed value
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
        // Verify boundary condition: 32768 is the maximum allowed parallelism
        Assert.DoesNotThrow(() => stream.SetMaxParallelism(32768));
    }
    
    [Test]
    public void SinkToKafka_WithoutBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", null));
    }
    
    [Test]
    public void SinkToKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", ""));
    }
    
    [Test]
    public void SinkToKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", "   "));
    }
    
    [Test]
    public void SinkToKafka_OnCollectionStream_ThrowsInvalidOperationException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        
        Assert.Throws<InvalidOperationException>(() => stream.SinkToKafka("output-topic", "localhost:9092"));
    }
    
    [Test]
    public void AssignTimestampsAndWatermarks_WithPunctuatedWatermarks_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var assigner = new TestPunctuatedWatermarkAssigner();
        
        var result = stream.AssignTimestampsAndWatermarks(assigner);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }
    
    [Test]
    public void AssignTimestampsAndWatermarks_WithPeriodicWatermarks_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var assigner = new TestPeriodicWatermarkAssigner();
        
        var result = stream.AssignTimestampsAndWatermarks(assigner);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }
    
    #endregion
}

// Test helper classes
internal class TestMapFunction : IMapFunction<string, string>
{
    public string Map(string value) => value.ToUpper();
}

internal class TestFilterFunction : IFilterFunction<int>
{
    public bool Filter(int value) => value > 2;
}

internal class TestFlatMapFunction : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string value) => value.Split(',');
}

internal class TestSinkFunction : ISinkFunction<string>
{
    public Task InvokeAsync(string element, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

internal class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<int>
{
    public long ExtractTimestamp(int element, long previousElementTimestamp) => element * 1000L;
    
    public Watermark? CheckAndGetNextWatermark(int lastElement, long extractedTimestamp)
    {
        return new Watermark(extractedTimestamp - 1000);
    }
}

internal class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<int>
{
    public long ExtractTimestamp(int element, long previousElementTimestamp) => element * 1000L;
    
    public Watermark? GetCurrentWatermark()
    {
        return new Watermark(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
    }
}

[TestFixture]
public class KeyedStreamTests
{
    #region Reduce Tests

    [Test]
    public void Reduce_WithFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var reduced = keyed.Reduce((a, b) => a + b);
        
        Assert.That(reduced, Is.Not.Null);
        Assert.That(reduced, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Aggregate Tests

    [Test]
    public void Aggregate_WithType_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var aggregated = keyed.Aggregate("SUM", "value");
        
        Assert.That(aggregated, Is.Not.Null);
        Assert.That(aggregated, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region GetDataStream Tests

    [Test]
    public void GetDataStream_ReturnsUnderlyingDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var dataStream = keyed.GetDataStream();
        
        Assert.That(dataStream, Is.Not.Null);
        Assert.That(dataStream, Is.TypeOf<DataStream<int>>());
    }

    #endregion
    
    #region IReduceFunction Tests
    
    [Test]
    public void Reduce_WithIReduceFunctionInterface_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);
        var reduceFunc = new TestReduceFunction();
        
        var reduced = keyed.Reduce(reduceFunc);
        
        Assert.That(reduced, Is.Not.Null);
        Assert.That(reduced, Is.TypeOf<DataStream<int>>());
    }
    
    #endregion
}

// More test helper classes
internal class TestReduceFunction : IReduceFunction<int>
{
    public int Reduce(int value1, int value2) => value1 + value2;
}

[TestFixture]
public class AllWindowedStreamTests
{
    #region Window Properties Tests

    [Test]
    public void GetWindowSize_ForTimeWindow_ReturnsSize()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        
        var size = windowed.GetWindowSize();
        
        Assert.That(size, Is.Not.Null);
    }

    [Test]
    public void GetWindowCount_ForCountWindow_ReturnsCount()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.CountWindowAll(10);
        
        var count = windowed.GetWindowCount();
        
        Assert.That(count, Is.EqualTo(10));
    }

    [Test]
    public void GetWindowSize_ForCountWindow_ReturnsNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.CountWindowAll(10);
        
        var size = windowed.GetWindowSize();
        
        Assert.That(size, Is.Null);
    }

    [Test]
    public void GetWindowCount_ForTimeWindow_ReturnsNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        
        var count = windowed.GetWindowCount();
        
        Assert.That(count, Is.Null);
    }

    #endregion
    
    #region Window Aggregation Tests
    
    [Test]
    public void Aggregate_WithIAggregateFunctionInterface_ReturnsAggregatedDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(new TestSourceFunction(), "test-source");
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        var aggFunc = new TestAggregateFunction();
        
        var aggregated = windowed.Aggregate(aggFunc);
        
        Assert.That(aggregated, Is.Not.Null);
        Assert.That(aggregated, Is.TypeOf<DataStream<int>>());
    }
    
    #endregion
}

// Additional test helper classes
internal class TestSourceFunction : ISourceFunction<int>
{
    public async IAsyncEnumerable<int> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= 5; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;
            yield return i;
            await Task.Delay(10, cancellationToken);
        }
    }
}

internal class TestAggregateFunction : IAggregateFunction<int, int, int>
{
    public int CreateAccumulator() => 0;
    
    public int Add(int value, int accumulator) => accumulator + value;
    
    public int GetResult(int accumulator) => accumulator;
    
    public int Merge(int acc1, int acc2) => acc1 + acc2;
}

[TestFixture]
public class TimeTests
{
    #region Time Factory Methods Tests

    [Test]
    public void Seconds_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.Seconds(5);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(5000));
    }

    [Test]
    public void Minutes_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.Minutes(2);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(120000));
    }

    [Test]
    public void Hours_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.Hours(1);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
    }

    [Test]
    public void Days_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.Days(1);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
    }

    [Test]
    public void Milliseconds_CreatesTimeWithCorrectValue()
    {
        var time = Time.Milliseconds(1000);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(1000));
    }
    
    #endregion
    
    #region Time Lowercase Aliases Tests (Java Flink Compatibility)
    
    [Test]
    public void milliseconds_CreatesTimeWithCorrectValue()
    {
        var time = Time.milliseconds(1000);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(1000));
    }
    
    [Test]
    public void seconds_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.seconds(5);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(5000));
    }
    
    [Test]
    public void minutes_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.minutes(2);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(120000));
    }
    
    [Test]
    public void hours_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.hours(1);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
    }
    
    [Test]
    public void days_CreatesTimeWithCorrectMilliseconds()
    {
        var time = Time.days(1);
        
        Assert.That(time, Is.Not.Null);
        Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
    }
    
    [Test]
    public void ToString_ReturnsFormattedString()
    {
        var time = Time.Seconds(5);
        
        var result = time.ToString();
        
        Assert.That(result, Is.EqualTo("5000ms"));
    }
    
    #endregion
}

[TestFixture]
public class WatermarkTests
{
    [Test]
    public void Constructor_SetsTimestamp()
    {
        var watermark = new Watermark(12345);
        
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(12345));
    }
    
    [Test]
    public void ToString_ReturnsFormattedString()
    {
        var watermark = new Watermark(12345);
        
        var result = watermark.ToString();
        
        Assert.That(result, Is.EqualTo("Watermark(12345)"));
    }
}

[TestFixture]
public class StateDescriptorTests
{
    #region ValueStateDescriptor Tests
    
    [Test]
    public void ValueStateDescriptor_Constructor_SetsNameAndType()
    {
        var descriptor = new ValueStateDescriptor<int>("test-state");
        
        Assert.That(descriptor.Name, Is.EqualTo("test-state"));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
    }
    
    [Test]
    public void ValueStateDescriptor_WithStringType_SetsCorrectType()
    {
        var descriptor = new ValueStateDescriptor<string>("string-state");
        
        Assert.That(descriptor.Name, Is.EqualTo("string-state"));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(string)));
    }
    
    [Test]
    public void ValueStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ValueStateDescriptor<int>(null!));
    }
    
    #endregion
    
    #region ListStateDescriptor Tests
    
    [Test]
    public void ListStateDescriptor_Constructor_SetsNameAndElementType()
    {
        var descriptor = new ListStateDescriptor<string>("list-state");
        
        Assert.That(descriptor.Name, Is.EqualTo("list-state"));
        Assert.That(descriptor.ElementType, Is.EqualTo(typeof(string)));
    }
    
    [Test]
    public void ListStateDescriptor_WithIntType_SetsCorrectType()
    {
        var descriptor = new ListStateDescriptor<int>("int-list");
        
        Assert.That(descriptor.Name, Is.EqualTo("int-list"));
        Assert.That(descriptor.ElementType, Is.EqualTo(typeof(int)));
    }
    
    [Test]
    public void ListStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ListStateDescriptor<string>(null!));
    }
    
    #endregion
    
    #region MapStateDescriptor Tests
    
    [Test]
    public void MapStateDescriptor_Constructor_SetsNameAndTypes()
    {
        var descriptor = new MapStateDescriptor<string, int>("map-state");
        
        Assert.That(descriptor.Name, Is.EqualTo("map-state"));
        Assert.That(descriptor.KeyType, Is.EqualTo(typeof(string)));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(int)));
    }
    
    [Test]
    public void MapStateDescriptor_WithComplexTypes_SetsCorrectTypes()
    {
        var descriptor = new MapStateDescriptor<int, List<string>>("complex-map");
        
        Assert.That(descriptor.Name, Is.EqualTo("complex-map"));
        Assert.That(descriptor.KeyType, Is.EqualTo(typeof(int)));
        Assert.That(descriptor.ValueType, Is.EqualTo(typeof(List<string>)));
    }
    
    [Test]
    public void MapStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new MapStateDescriptor<string, int>(null!));
    }
    
    #endregion
    
    #region ReducingStateDescriptor Tests
    
    [Test]
    public void ReducingStateDescriptor_Constructor_SetsNameAndFunction()
    {
        var reduceFunc = new TestReduceFunction();
        var descriptor = new ReducingStateDescriptor<int>("reducing-state", reduceFunc);
        
        Assert.That(descriptor.Name, Is.EqualTo("reducing-state"));
        Assert.That(descriptor.ReduceFunction, Is.SameAs(reduceFunc));
    }
    
    [Test]
    public void ReducingStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        var reduceFunc = new TestReduceFunction();
        Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>(null!, reduceFunc));
    }
    
    [Test]
    public void ReducingStateDescriptor_WithNullFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ReducingStateDescriptor<int>("test", null!));
    }
    
    #endregion
    
    #region AggregatingStateDescriptor Tests
    
    [Test]
    public void AggregatingStateDescriptor_Constructor_SetsNameAndFunction()
    {
        var aggFunc = new TestAggregateFunction();
        var descriptor = new AggregatingStateDescriptor<int, int, int>("agg-state", aggFunc);
        
        Assert.That(descriptor.Name, Is.EqualTo("agg-state"));
        Assert.That(descriptor.AggregateFunction, Is.SameAs(aggFunc));
    }
    
    [Test]
    public void AggregatingStateDescriptor_WithNullName_ThrowsArgumentNullException()
    {
        var aggFunc = new TestAggregateFunction();
        Assert.Throws<ArgumentNullException>(() => new AggregatingStateDescriptor<int, int, int>(null!, aggFunc));
    }
    
    [Test]
    public void AggregatingStateDescriptor_WithNullFunction_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new AggregatingStateDescriptor<int, int, int>("test", null!));
    }
    
    #endregion
}

[TestFixture]
public class OutputTagTests
{
    [Test]
    public void Constructor_SetsId()
    {
        var tag = new OutputTag<string>("side-output");
        
        Assert.That(tag.Id, Is.EqualTo("side-output"));
    }
    
    [Test]
    public void Constructor_WithNullId_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
    }
    
    [Test]
    public void Equals_WithSameId_ReturnsTrue()
    {
        var tag1 = new OutputTag<string>("test");
        var tag2 = new OutputTag<string>("test");
        
        Assert.That(tag1.Equals(tag2), Is.True);
    }
    
    [Test]
    public void Equals_WithDifferentId_ReturnsFalse()
    {
        var tag1 = new OutputTag<string>("test1");
        var tag2 = new OutputTag<string>("test2");
        
        Assert.That(tag1.Equals(tag2), Is.False);
    }
    
    [Test]
    public void Equals_WithDifferentType_ReturnsFalse()
    {
        var tag1 = new OutputTag<string>("test");
        var tag2 = new OutputTag<int>("test");
        
        Assert.That(tag1.Equals(tag2), Is.False);
    }
    
    [Test]
    public void Equals_WithNonOutputTag_ReturnsFalse()
    {
        var tag = new OutputTag<string>("test");
        
        Assert.That(tag.Equals("test"), Is.False);
    }
    
    [Test]
    public void Equals_WithNull_ReturnsFalse()
    {
        var tag = new OutputTag<string>("test");
        
        Assert.That(tag.Equals(null), Is.False);
    }
    
    [Test]
    public void GetHashCode_WithSameId_ReturnsSameHashCode()
    {
        var tag1 = new OutputTag<string>("test");
        var tag2 = new OutputTag<string>("test");
        
        Assert.That(tag1.GetHashCode(), Is.EqualTo(tag2.GetHashCode()));
    }
    
    [Test]
    public void GetHashCode_WithDifferentId_ReturnsDifferentHashCode()
    {
        var tag1 = new OutputTag<string>("test1");
        var tag2 = new OutputTag<string>("test2");
        
        Assert.That(tag1.GetHashCode(), Is.Not.EqualTo(tag2.GetHashCode()));
    }
}

[TestFixture]
public class InternalWrapperClassesTests
{
    #region MappedSourceFunction Tests
    
    [Test]
    public void MappedSourceFunction_TransformsElements()
    {
        var sourceFunc = new TestSourceFunction();
        // Note: Cannot directly test MappedSourceFunction as it's internal
        // But we can test it through DataStream.Map
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");
        
        var mapped = stream.Map(x => x * 2);
        
        Assert.That(mapped, Is.Not.Null);
    }
    
    #endregion
    
    #region FilteredSourceFunction Tests
    
    [Test]
    public void FilteredSourceFunction_FiltersElements()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");
        
        var filtered = stream.Filter(x => x > 3);
        
        Assert.That(filtered, Is.Not.Null);
    }
    
    #endregion
    
    #region FlatMappedSourceFunction Tests
    
    [Test]
    public void FlatMappedSourceFunction_ExpandsElements()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");
        
        var flatMapped = stream.FlatMap<int>(x => new[] { x, x * 2, x * 3 });
        
        Assert.That(flatMapped, Is.Not.Null);
    }
    
    #endregion
}

[TestFixture]
public class AdvancedDataStreamTests
{
    #region Complex Transformation Chain Tests
    
    [Test]
    public void ComplexTransformationChain_WithMultipleOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromCollection(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 })
            .Filter(x => x % 2 == 0)                    // Keep even numbers: 2, 4, 6, 8, 10
            .Map(x => x * 10)                           // Multiply by 10: 20, 40, 60, 80, 100
            .Filter(x => x > 50)                        // Keep > 50: 60, 80, 100
            .SetParallelism(2)
            .Name("complex-pipeline")
            .Rebalance();
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void ComplexTransformationChain_WithKeyBy_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromCollection(new[] { "apple", "apricot", "banana", "blueberry", "cherry" })
            .Filter(s => s.Length > 5)
            .KeyBy(s => s[0])
            .Reduce((a, b) => a.Length > b.Length ? a : b);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void ComplexTransformationChain_WithFlatMapAndKeyBy_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromCollection(new[] { "hello world", "flink streaming" })
            .FlatMap<string>(s => s.Split(' '))
            .Map(s => s.ToUpper())
            .KeyBy(s => s.Length)
            .Reduce((a, b) => $"{a},{b}");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }
    
    #endregion
    
    #region Window and Aggregation Tests
    
    [Test]
    public void TimeWindowAll_WithMultipleOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");
        
        var result = stream
            .Filter(x => x > 0)
            .TimeWindowAll(Time.Seconds(5));
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<AllWindowedStream<int>>());
    }
    
    [Test]
    public void CountWindowAll_WithMultipleOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromCollection(new[] { 1, 2, 3, 4, 5 })
            .Map(x => x * 2)
            .CountWindowAll(3);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<AllWindowedStream<int>>());
    }
    
    [Test]
    public void Aggregate_AfterTimeWindow_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");
        var aggFunc = new TestAggregateFunction();
        
        var result = stream
            .TimeWindowAll(Time.Seconds(10))
            .Aggregate(aggFunc);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void Aggregate_AfterCountWindow_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");
        var aggFunc = new TestAggregateFunction();
        
        var result = stream
            .CountWindowAll(100)
            .Aggregate(aggFunc);
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }
    
    #endregion
    
    #region Partitioning Strategy Tests
    
    [Test]
    public void MultiplePartitioningOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var result = stream
            .Rebalance()
            .Map(x => x * 2)
            .Rescale()
            .Filter(x => x > 5)
            .Forward();
        
        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public void PartitionCustom_WithMultipleOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c", "d", "e" });
        
        var result = stream
            .Map(s => s.ToUpper())
            .PartitionCustom<char>((key, numPartitions) => key % numPartitions, s => s[0])
            .Filter(s => s.Length > 0);
        
        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public void Broadcast_AfterFilter_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var result = stream
            .Filter(x => x % 2 == 0)
            .Broadcast();
        
        Assert.That(result, Is.Not.Null);
    }
    
    [Test]
    public void Shuffle_AfterMap_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var result = stream
            .Map(x => x * 2)
            .Shuffle();
        
        Assert.That(result, Is.Not.Null);
    }
    
    #endregion
    
    #region Configuration Chaining Tests
    
    [Test]
    public void MultipleConfigurationMethods_CanBeChained()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "test" });
        
        var result = stream
            .SetParallelism(4)
            .SetMaxParallelism(128)
            .Name("my-stream")
            .SlotSharingGroup("group-1");
        
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }
    
    [Test]
    public void ConfigurationMethods_ReturnSameInstance()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        
        var result1 = stream.SetParallelism(2);
        var result2 = stream.Name("test");
        var result3 = stream.SlotSharingGroup("group");
        
        Assert.That(result1, Is.SameAs(stream));
        Assert.That(result2, Is.SameAs(stream));
        Assert.That(result3, Is.SameAs(stream));
    }
    
    #endregion
    
    #region KeyedStream Advanced Tests
    
    [Test]
    public void KeyBy_WithComplexKeySelector_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "apple", "apricot", "banana", "blueberry" });
        
        var keyed = stream.KeyBy(s => (s[0], s.Length));
        
        Assert.That(keyed, Is.Not.Null);
    }
    
    [Test]
    public void Reduce_WithComplexReduceFunction_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var reduced = keyed.Reduce((a, b) => Math.Max(a, b));
        
        Assert.That(reduced, Is.Not.Null);
    }
    
    [Test]
    public void Aggregate_WithStringFieldName_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var aggregated = keyed.Aggregate("SUM", "value");
        
        Assert.That(aggregated, Is.Not.Null);
    }
    
    [Test]
    public void Aggregate_WithDifferentTypes_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var maxAgg = keyed.Aggregate("MAX", "value");
        var minAgg = keyed.Aggregate("MIN", "value");
        var avgAgg = keyed.Aggregate("AVG", "value");
        
        Assert.That(maxAgg, Is.Not.Null);
        Assert.That(minAgg, Is.Not.Null);
        Assert.That(avgAgg, Is.Not.Null);
    }
    
    [Test]
    public void GetDataStream_ReturnsOriginalStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var keyed = stream.KeyBy(x => x % 2);
        
        var dataStream = keyed.GetDataStream();
        
        Assert.That(dataStream, Is.SameAs(stream));
    }
    
    #endregion
}
