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
}
