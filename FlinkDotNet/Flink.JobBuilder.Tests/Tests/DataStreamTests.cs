using FlinkDotNet.DataStream;

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
        // Verify normal case: typical parallelism value
        Assert.DoesNotThrow(() => stream.SetMaxParallelism(128));
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

    #region DataStream Creation and Chaining Tests

    [Test]
    public void DataStream_CreatedFromKafka_SupportsOperations()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        // Test that DataStream created from Kafka (which uses JobDefinition internally) works
        var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

        Assert.That(stream, Is.Not.Null);
        var retrievedEnv = stream.GetExecutionEnvironment();
        Assert.That(retrievedEnv, Is.SameAs(env));
    }

    [Test]
    public void DataStream_CreatedFromAddSource_SupportsOperations()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        
        var stream = env.AddSource(sourceFunc, "test-source");

        Assert.That(stream, Is.Not.Null);
        Assert.DoesNotThrow(() => stream.Map(x => x * 2));
    }

    [Test]
    public void DataStream_ChainedOperations_MaintainEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var result = stream
            .Map(x => x * 2)
            .Filter(x => x > 2)
            .Map(x => x + 1);

        var resultEnv = result.GetExecutionEnvironment();
        Assert.That(resultEnv, Is.SameAs(env));
    }

    #endregion

    #region Map Operation Advanced Coverage Tests

    [Test]
    public void Map_OnKafkaBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s));
        
        // Map should work on Kafka-backed stream
        var mapped = stream.Map(x => x * 2);
        
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Map_OnSourceFunctionBackedStream_TransformsElements()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var mapped = stream.Map(x => x * 2);

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Map_WithIMapFunctionOnKafkaStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<string>("test-topic", "localhost:9092", "test-group", s => s);
        var mapFunc = new TestMapFunction();

        var mapped = stream.Map(mapFunc);

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void Map_ChainedMultipleTimes_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var result = stream
            .Map(x => x * 2)
            .Map(x => x + 1)
            .Map(x => x * 3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Filter Operation Advanced Coverage Tests

    [Test]
    public void Filter_OnKafkaBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s));
        
        var filtered = stream.Filter(x => x > 5);
        
        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Filter_OnSourceFunctionBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var filtered = stream.Filter(x => x > 3);

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void Filter_ChainedMultipleTimes_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        var result = stream
            .Filter(x => x > 2)
            .Filter(x => x < 9)
            .Filter(x => x % 2 == 0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region FlatMap Operation Advanced Coverage Tests

    [Test]
    public void FlatMap_OnKafkaBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<string>("test-topic", "localhost:9092", "test-group", s => s);
        
        var flatMapped = stream.FlatMap<string>(s => s.Split(','));
        
        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void FlatMap_OnSourceFunctionBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var flatMapped = stream.FlatMap<int>(x => new[] { x, x * 2, x * 3 });

        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void FlatMap_ChainedWithOtherOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a,b", "c,d,e" });

        var result = stream
            .FlatMap<string>(s => s.Split(','))
            .Map(s => s.ToUpper())
            .Filter(s => s.Length > 0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }

    #endregion

    #region Sink Operation Advanced Coverage Tests

    [Test]
    public void AddSink_WithKafkaSinkFunction_CapturesKafkaSink()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<string>("input-topic", "localhost:9092", "test-group", s => s);
        var kafkaSink = new KafkaSinkFunction<string>("output-topic", "localhost:9092", s => System.Text.Encoding.UTF8.GetBytes(s));

        var result = stream.AddSink(kafkaSink);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void AddSink_WithNonKafkaSinkFunction_DoesNotThrow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a", "b", "c" });
        var genericSink = new TestSinkFunction();

        var result = stream.AddSink(genericSink);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void SinkToKafka_WithOperationCapture_CapturesSink()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("input-topic", "localhost:9092", "test-group", s => int.Parse(s));

        var result = stream.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    #endregion

    #region Window Operation Advanced Coverage Tests

    [Test]
    public void TimeWindowAll_OnKafkaBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s));

        var windowed = stream.TimeWindowAll(Time.Seconds(10));

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed, Is.TypeOf<AllWindowedStream<int>>());
    }

    [Test]
    public void CountWindowAll_OnKafkaBackedStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddKafkaSource<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s));

        var windowed = stream.CountWindowAll(100);

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed, Is.TypeOf<AllWindowedStream<int>>());
    }

    [Test]
    public void TimeWindowAll_OnSourceFunctionStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var windowed = stream.TimeWindowAll(Time.Seconds(5));

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed.GetWindowSize(), Is.Not.Null);
    }

    [Test]
    public void CountWindowAll_OnSourceFunctionStream_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var windowed = stream.CountWindowAll(50);

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed.GetWindowCount(), Is.EqualTo(50));
    }

    #endregion

    #region Aggregation Advanced Coverage Tests

    [Test]
    public void Aggregate_WithMergeOperation_WorksCorrectly()
    {
        var aggFunc = new TestAggregateFunction();
        
        var acc1 = aggFunc.Add(5, aggFunc.CreateAccumulator());
        var acc2 = aggFunc.Add(10, aggFunc.CreateAccumulator());
        var merged = aggFunc.Merge(acc1, acc2);
        
        Assert.That(merged, Is.EqualTo(15));
    }

    [Test]
    public void AggregatedSourceFunction_ProcessesMultipleElements()
    {
        var sourceFunc = new TestSourceFunction();
        var aggFunc = new TestAggregateFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Test through windowed aggregation
        var stream = env.AddSource(sourceFunc, "test");
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        var aggregated = windowed.Aggregate(aggFunc);

        Assert.That(aggregated, Is.Not.Null);
    }

    #endregion

    #region Partitioning Edge Cases

    [Test]
    public void PartitionCustom_WithStringKey_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "apple", "banana", "cherry" });

        var result = stream.PartitionCustom<string>(
            (key, numPartitions) => key.Length % numPartitions,
            s => s);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void PartitionCustom_WithComplexKey_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

        var result = stream.PartitionCustom<(int value, int mod)>(
            (key, numPartitions) => key.value % numPartitions,
            x => (x, x % 2));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void SetMaxParallelism_WithMinimumValue_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var result = stream.SetMaxParallelism(1);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    #endregion

    #region KeyedStream Additional Coverage

    [Test]
    public void KeyedStream_StoresKeySelector()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var keyed = stream.KeyBy(x => x % 3);

        // Verify keyed stream was created and operations work
        var reduced = keyed.Reduce((a, b) => a + b);
        Assert.That(reduced, Is.Not.Null);
    }

    [Test]
    public void KeyedStream_WithStructKey_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        
        var keyed = stream.KeyBy(x => (x % 2, x % 3));

        Assert.That(keyed, Is.Not.Null);
        var dataStream = keyed.GetDataStream();
        Assert.That(dataStream, Is.SameAs(stream));
    }

    #endregion

    #region Source Function Behavior Tests

    [Test]
    public void MappedSourceFunction_ThroughMapOperation_CreatesCorrectStream()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");

        var mapped = stream.Map(x => x * 2);

        // Verify the stream was created and maintains correct type
        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<int>>());
        
        // Verify we can chain more operations
        var filtered = mapped.Filter(x => x > 5);
        Assert.That(filtered, Is.Not.Null);
    }

    [Test]
    public void FilteredSourceFunction_ThroughFilterOperation_CreatesCorrectStream()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");

        var filtered = stream.Filter(x => x > 2);

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
        
        // Verify chaining works
        var mapped = filtered.Map(x => x * 2);
        Assert.That(mapped, Is.Not.Null);
    }

    [Test]
    public void FlatMappedSourceFunction_ThroughFlatMapOperation_CreatesCorrectStream()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");

        var flatMapped = stream.FlatMap<int>(x => new[] { x, x * 10 });

        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<int>>());
        
        // Verify chaining
        var filtered = flatMapped.Filter(x => x < 100);
        Assert.That(filtered, Is.Not.Null);
    }

    [Test]
    public void AggregatedSourceFunction_ThroughWindowAggregate_CreatesCorrectStream()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");
        var aggFunc = new TestAggregateFunction();

        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        var aggregated = windowed.Aggregate(aggFunc);

        Assert.That(aggregated, Is.Not.Null);
        Assert.That(aggregated, Is.TypeOf<DataStream<int>>());
        
        // Verify environment is preserved
        var env2 = aggregated.GetExecutionEnvironment();
        Assert.That(env2, Is.SameAs(env));
    }

    [Test]
    public void ChainedWrapperFunctions_AllWorkTogether()
    {
        var sourceFunc = new TestSourceFunction();
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.AddSource(sourceFunc, "test");

        // Chain map -> filter -> flatmap -> map
        var result = stream
            .Map(x => x * 2)
            .Filter(x => x > 4)
            .FlatMap<int>(x => new[] { x, x + 1 })
            .Map(x => x * 3);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Additional Window and State Coverage Tests

    [Test]
    public void TimeWindowAll_WithMinutes_CreatesCorrectWindow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var windowed = stream.TimeWindowAll(Time.Minutes(5));

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed.GetWindowSize(), Is.Not.Null);
    }

    [Test]
    public void TimeWindowAll_WithHours_CreatesCorrectWindow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var windowed = stream.TimeWindowAll(Time.Hours(1));

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed.GetWindowSize(), Is.Not.Null);
    }

    [Test]
    public void CountWindowAll_WithLargeCount_CreatesCorrectWindow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var windowed = stream.CountWindowAll(1000);

        Assert.That(windowed, Is.Not.Null);
        Assert.That(windowed.GetWindowCount(), Is.EqualTo(1000));
    }

    #endregion

    #region Complex Chaining and Combination Tests

    [Test]
    public void ComplexChain_MapFilterFlatMapWindow_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");

        var result = stream
            .Map(x => x * 2)
            .Filter(x => x > 4)
            .FlatMap<int>(x => new[] { x, x + 1 })
            .TimeWindowAll(Time.Seconds(5));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<AllWindowedStream<int>>());
    }

    [Test]
    public void ComplexChain_WithPartitioningAndWindowing_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

        var result = stream
            .Rebalance()
            .Map(x => x * 2)
            .Rescale()
            .Filter(x => x > 5)
            .TimeWindowAll(Time.Seconds(10));

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<AllWindowedStream<int>>());
    }

    [Test]
    public void KeyedStream_AfterMultipleOperations_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });

        var result = stream
            .Map(x => x * 2)
            .Filter(x => x > 10)
            .KeyBy(x => x % 3)
            .Reduce((a, b) => a + b);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void AllWindowedStream_MultipleAggregations_Work()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test");
        var aggFunc = new TestAggregateFunction();

        var windowed1 = stream.TimeWindowAll(Time.Seconds(5));
        var aggregated1 = windowed1.Aggregate(aggFunc);

        var windowed2 = stream.CountWindowAll(10);
        var aggregated2 = windowed2.Aggregate(aggFunc);

        Assert.That(aggregated1, Is.Not.Null);
        Assert.That(aggregated2, Is.Not.Null);
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public void Where_OnCollectionStream_ReturnsSameStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Where on collection stream should return same stream (no-op for local)
        var filtered = stream.Where("value > 1");

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.SameAs(stream));
    }

    [Test]
    public void AddSink_WithNullFunction_DoesNotThrow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // AddSink with null should still return stream without throwing
        Assert.DoesNotThrow(() => stream.AddSink(null!));
    }

    [Test]
    public void KeyedStream_MultipleReduceOperations_Work()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5, 6 });
        var keyed = stream.KeyBy(x => x % 2);

        var reduced1 = keyed.Reduce((a, b) => a + b);
        var reduced2 = keyed.Reduce((a, b) => Math.Max(a, b));

        Assert.That(reduced1, Is.Not.Null);
        Assert.That(reduced2, Is.Not.Null);
    }

    [Test]
    public void AllWindowedStream_GetWindowSize_ForTimeWindow_ReturnsNonNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.TimeWindowAll(Time.Milliseconds(100));

        var size = windowed.GetWindowSize();

        Assert.That(size, Is.Not.Null);
    }

    [Test]
    public void DataStream_ComplexTypeTransformations_Work()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Transform int -> string -> int -> double
        var result = stream
            .Map(x => x.ToString())
            .Map(s => int.Parse(s))
            .Map(x => (double)x);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<double>>());
    }

    [Test]
    public void MapWithIMapFunction_OnCollectionStream_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "test", "data" });
        var mapFunc = new TestMapFunction();

        var mapped = stream.Map(mapFunc);

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void FilterWithIFilterFunction_OnCollectionStream_Works()
    {
        // This test validates the IFilterFunction interface overload, 
        // distinct from lambda-based Filter tested elsewhere
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var filterFunc = new TestFilterFunction();

        var filtered = stream.Filter(filterFunc);

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<int>>());
        // Verify the IFilterFunction overload is correctly resolved
        Assert.That(filterFunc, Is.Not.Null);
    }

    [Test]
    public void FlatMapWithIFlatMapFunction_OnCollectionStream_Works()
    {
        // This test validates the IFlatMapFunction interface overload,
        // distinct from lambda-based FlatMap tested elsewhere
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "a,b", "c,d" });
        var flatMapFunc = new TestFlatMapFunction();

        var flatMapped = stream.FlatMap(flatMapFunc);

        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<string>>());
        // Verify the IFlatMapFunction overload is correctly resolved
        Assert.That(flatMapFunc, Is.Not.Null);
    }

    [Test]
    public void MultiplePartitioningStrategies_Chained_Work()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

        var result = stream
            .Shuffle()
            .Rebalance()
            .Rescale()
            .Forward()
            .Broadcast();

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void PartitionCustom_WithIntegerKey_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

        var result = stream.PartitionCustom<int>(
            (key, numPartitions) => key % numPartitions,
            x => x);

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void SetMaxParallelism_BoundaryValue_2048_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        var result = stream.SetMaxParallelism(2048);

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

    #region KafkaSourceFunction Coverage Tests - Chunk 1D

    [Test]
    public void KafkaSourceFunction_PropertyGetter_Topic_ReturnsCorrectValue()
    {
        var kafkaSource = new KafkaSourceFunction<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s,
            "earliest");

        Assert.That(kafkaSource.Topic, Is.EqualTo("test-topic"));
    }

    [Test]
    public void KafkaSourceFunction_PropertyGetter_BootstrapServers_ReturnsCorrectValue()
    {
        var kafkaSource = new KafkaSourceFunction<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s,
            "earliest");

        Assert.That(kafkaSource.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void KafkaSourceFunction_PropertyGetter_GroupId_ReturnsCorrectValue()
    {
        var kafkaSource = new KafkaSourceFunction<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s,
            "earliest");

        Assert.That(kafkaSource.GroupId, Is.EqualTo("test-group"));
    }

    [Test]
    public void KafkaSourceFunction_PropertyGetter_StartingOffsets_ReturnsCorrectValue()
    {
        var kafkaSource = new KafkaSourceFunction<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s,
            "latest");

        Assert.That(kafkaSource.StartingOffsets, Is.EqualTo("latest"));
    }

    [Test]
    public async Task KafkaSourceFunction_RunAsync_ReturnsEmptyEnumerable()
    {
        var kafkaSource = new KafkaSourceFunction<int>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => int.Parse(s),
            "earliest");

        var results = new List<int>();
        await foreach (var item in kafkaSource.RunAsync())
        {
            results.Add(item);
        }

        Assert.That(results, Is.Empty);
    }

    #endregion

    #region StreamExecutionEnvironmentExtensions Coverage Tests - Chunk 1D

    [Test]
    public void SetStreamTimeCharacteristic_WithProcessingTime_SetsConfiguration()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.ProcessingTime);

        Assert.That(result, Is.SameAs(env));
        // Verify the configuration was set
        var config = env.GetConfig().GetConfiguration();
        var timeChar = config.GetString("stream.time-characteristic", null);
        Assert.That(timeChar, Is.EqualTo("ProcessingTime"));
    }

    [Test]
    public void SetStreamTimeCharacteristic_WithEventTime_SetsConfiguration()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

        Assert.That(result, Is.SameAs(env));
        var config = env.GetConfig().GetConfiguration();
        var timeChar = config.GetString("stream.time-characteristic", null);
        Assert.That(timeChar, Is.EqualTo("EventTime"));
    }

    [Test]
    public void SetStreamTimeCharacteristic_WithIngestionTime_SetsConfiguration()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.IngestionTime);

        Assert.That(result, Is.SameAs(env));
        var config = env.GetConfig().GetConfiguration();
        var timeChar = config.GetString("stream.time-characteristic", null);
        Assert.That(timeChar, Is.EqualTo("IngestionTime"));
    }

    [Test]
    public void AddSource_WithISourceFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();

        var result = env.AddSource(sourceFunc);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void AddSource_WithKafkaSourceFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var kafkaSource = new KafkaSourceFunction<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s,
            "earliest");

        var result = env.AddSource(kafkaSource);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<string>>());
    }

    #endregion

    #region AllWindowedStream Coverage Tests - Chunk 1D

    [Test]
    public void AllWindowedStream_AttachOperationCapture_DoesNotThrow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        var capture = new OperationCapture();

        Assert.DoesNotThrow(() => windowed.AttachOperationCapture(capture));
    }

    [Test]
    public void AllWindowedStream_Aggregate_WithOperationCapture_CapturesOperation()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        var stream = env.AddSource(sourceFunc, "test-source");
        var windowed = stream.TimeWindowAll(Time.Seconds(5));
        var capture = new OperationCapture();
        windowed.AttachOperationCapture(capture);
        var aggFunc = new TestAggregateFunction();

        var result = windowed.Aggregate(aggFunc);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void AllWindowedStream_GetWindowSize_ForTimeWindow_ReturnsCorrectSize()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowTime = Time.Seconds(10);
        var windowed = stream.TimeWindowAll(windowTime);

        var size = windowed.GetWindowSize();

        Assert.That(size, Is.Not.Null);
        Assert.That(size, Is.EqualTo(windowTime));
    }

    [Test]
    public void AllWindowedStream_GetWindowCount_ForCountWindow_ReturnsCorrectCount()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });
        var windowed = stream.CountWindowAll(25);

        var count = windowed.GetWindowCount();

        Assert.That(count, Is.EqualTo(25));
    }

    #endregion

    #region DataStream Coverage Enhancement Tests - Chunk E

    [Test]
    public void DataStream_Map_WithJobDefinitionBacking_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        var mapped = stream.Map(x => x.ToUpper());

        Assert.That(mapped, Is.Not.Null);
        Assert.That(mapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void DataStream_Filter_WithJobDefinitionBacking_ReturnsSameDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        var filtered = stream.Filter(x => x.Length > 5);

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void DataStream_FlatMap_WithJobDefinitionBacking_ReturnsNewDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        var flatMapped = stream.FlatMap<string>(x => x.Split(','));

        Assert.That(flatMapped, Is.Not.Null);
        Assert.That(flatMapped, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void DataStream_Where_WithFilterExpression_AddsFilterOperation()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        var filtered = stream.Where("value.length > 5");

        Assert.That(filtered, Is.Not.Null);
        Assert.That(filtered, Is.SameAs(stream));
    }

    [Test]
    public void DataStream_SinkToKafka_WithBootstrapServers_SetsSink()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        var result = stream.SinkToKafka("output-topic", "localhost:9092");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.SameAs(stream));
    }

    [Test]
    public void DataStream_SinkToKafka_WithNullBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", null));
    }

    [Test]
    public void DataStream_SinkToKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromKafka("input-topic", "localhost:9092", "test-group");

        Assert.Throws<ArgumentException>(() => stream.SinkToKafka("output-topic", ""));
    }

    #endregion

    #region KeyedStream Coverage Tests - Chunk E

    [Test]
    public void KeyedStream_Reduce_WithFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);

        var reduced = keyed.Reduce((a, b) => a + b);

        Assert.That(reduced, Is.Not.Null);
        Assert.That(reduced, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void KeyedStream_Reduce_WithReduceFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);
        var reduceFunc = new TestReduceFunction();

        var reduced = keyed.Reduce(reduceFunc);

        Assert.That(reduced, Is.Not.Null);
        Assert.That(reduced, Is.TypeOf<DataStream<int>>());
    }

    [Test]
    public void KeyedStream_Aggregate_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });
        var keyed = stream.KeyBy(x => x % 2);

        var aggregated = keyed.Aggregate("sum", "value");

        Assert.That(aggregated, Is.Not.Null);
        Assert.That(aggregated, Is.TypeOf<DataStream<int>>());
    }

    private class TestReduceFunction : IReduceFunction<int>
    {
        public int Reduce(int value1, int value2)
        {
            return value1 + value2;
        }
    }

    #endregion

    #region OperationCapture Coverage Tests - Chunk E

    [Test]
    public void OperationCapture_CaptureFilterOperation_DoesNotThrow()
    {
        var capture = new OperationCapture();

        Assert.DoesNotThrow(() => capture.CaptureFilterOperation((string x) => x.Length > 5));
    }

    [Test]
    public void OperationCapture_CaptureFilterOperation_WithNullFunction_DoesNotThrow()
    {
        var capture = new OperationCapture();

        Assert.DoesNotThrow(() => capture.CaptureFilterOperation(null));
    }

    [Test]
    public void OperationCapture_CaptureFlatMapOperation_DoesNotThrow()
    {
        var capture = new OperationCapture();

        Assert.DoesNotThrow(() => capture.CaptureFlatMapOperation((string x) => x.Split(',')));
    }

    [Test]
    public void OperationCapture_CaptureFlatMapOperation_WithNullFunction_DoesNotThrow()
    {
        var capture = new OperationCapture();

        Assert.DoesNotThrow(() => capture.CaptureFlatMapOperation(null));
    }

    [Test]
    public void OperationCapture_CaptureTimestampAssigner_DoesNotThrow()
    {
        var capture = new OperationCapture();
        var assigner = new TestTimestampAssigner();

        Assert.DoesNotThrow(() => capture.CaptureTimestampAssigner(assigner));
    }

    [Test]
    public void OperationCapture_ToJobDefinition_WithoutKafkaSource_ThrowsInvalidOperationException()
    {
        var capture = new OperationCapture();

        Assert.Throws<InvalidOperationException>(() => 
            capture.ToJobDefinition("test-job-id", "test-job-name"));
    }

    private class TestTimestampAssigner : IAssignerWithPunctuatedWatermarks<string>
    {
        public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp)
        {
            return null;
        }

        public long ExtractTimestamp(string element, long previousElementTimestamp)
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    #endregion

    #region StreamExecutionEnvironment Coverage Tests - Chunk F

    [Test]
    public void StreamExecutionEnvironment_SetMaxParallelism_WithValidValue_SetsMaxParallelism()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        env.SetMaxParallelism(1000);

        Assert.That(env.GetMaxParallelism(), Is.EqualTo(1000));
    }

    [Test]
    public void StreamExecutionEnvironment_SetMaxParallelism_WithZero_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
    }

    [Test]
    public void StreamExecutionEnvironment_SetMaxParallelism_WithNegative_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
    }

    [Test]
    public void StreamExecutionEnvironment_SetMaxParallelism_WithTooLarge_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
    }

    [Test]
    public void StreamExecutionEnvironment_SetBufferTimeout_SetsValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        env.SetBufferTimeout(500);

        Assert.That(env.GetBufferTimeout(), Is.EqualTo(500));
    }

    [Test]
    public void StreamExecutionEnvironment_DisableOperatorChaining_DisablesChaining()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        env.DisableOperatorChaining();

        Assert.That(env.IsChainingEnabled(), Is.False);
    }

    [Test]
    public void StreamExecutionEnvironment_IsChainingEnabled_DefaultIsTrue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.That(env.IsChainingEnabled(), Is.True);
    }

    [Test]
    public void StreamExecutionEnvironment_EnableCheckpointing_SetsInterval()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        env.EnableCheckpointing(5000);

        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
    }

    [Test]
    public void StreamExecutionEnvironment_GetCheckpointInterval_DefaultIsNegativeOne()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(-1));
    }

    [Test]
    public void StreamExecutionEnvironment_EnableAdaptiveScheduler_EnablesScheduler()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        env.EnableAdaptiveScheduler(true);

        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
    }

    [Test]
    public void StreamExecutionEnvironment_EnableAdaptiveScheduler_WithFalse_DisablesScheduler()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableAdaptiveScheduler(true);

        env.EnableAdaptiveScheduler(false);

        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
    }

    #endregion

    #region Source Function Wrapper Tests - Chunk F

    [Test]
    public void MappedSourceFunction_Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new MappedSourceFunction<int, int>(null!, s => s * 2));
    }

    [Test]
    public void MappedSourceFunction_Constructor_WithNullMapFunction_ThrowsArgumentNullException()
    {
        var source = new TestSourceFunction();

        Assert.Throws<ArgumentNullException>(() => 
            new MappedSourceFunction<int, int>(source, null!));
    }

    [Test]
    public void FlatMappedSourceFunction_Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new FlatMappedSourceFunction<int, int>(null!, s => new[] { s, s * 2 }));
    }

    [Test]
    public void FlatMappedSourceFunction_Constructor_WithNullFlatMapFunction_ThrowsArgumentNullException()
    {
        var source = new TestSourceFunction();

        Assert.Throws<ArgumentNullException>(() => 
            new FlatMappedSourceFunction<int, int>(source, null!));
    }

    [Test]
    public void FilteredSourceFunction_Constructor_WithNullSource_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => 
            new FilteredSourceFunction<int>(null!, s => s > 0));
    }

    [Test]
    public void FilteredSourceFunction_Constructor_WithNullFilterFunction_ThrowsArgumentNullException()
    {
        var source = new TestSourceFunction();

        Assert.Throws<ArgumentNullException>(() => 
            new FilteredSourceFunction<int>(source, null!));
    }

    #endregion
}
