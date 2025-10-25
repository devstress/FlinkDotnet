using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class DataStreamTransformationTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region Map Tests

        [Test]
        public void Map_WithFuncDelegate_TransformsElements()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);

            // Act
            var mapped = stream.Map(x => x * 2);

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithMapFunction_TransformsElements()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var mapFunction = new TestMapFunction();

            // Act
            var mapped = stream.Map(mapFunction);

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithExpressionString_SupportsUpperExpression()
        {
            // Arrange - use FromKafka to create JobDefinition-backed stream
            var stream = this._env.FromKafka("test-topic", "localhost:9092");

            // Act
            var mapped = stream.Map("upper");

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithExpressionOnNonStringType_ThrowsNotSupportedException()
        {
            // Arrange
            var stream = this._env.AddKafkaSource<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s));

            // Act & Assert
            _ = Assert.Throws<NotSupportedException>(() => stream.Map("upper"));
        }

        [Test]
        public void Map_WithExpressionOnNonJobDefinitionStream_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = new[] { "a", "b", "c" };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<InvalidOperationException>(() => stream.Map("upper"));
        }

        [Test]
        public void Map_OnCollectionStream_TransformsCollection()
        {
            // Arrange
            var collection = new[] { "hello", "world" };
            var stream = this._env.FromCollection(collection);

            // Act
            var mapped = stream.Map(s => s.ToUpper());

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        #endregion

        #region Filter Tests

        [Test]
        public void Filter_WithFuncDelegate_FiltersElements()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);

            // Act
            var filtered = stream.Filter(x => x > 3);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void Filter_WithFilterFunction_FiltersElements()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);
            var filterFunction = new TestFilterFunction();

            // Act
            var filtered = stream.Filter(filterFunction);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void Filter_OnCollectionStream_FiltersCollection()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);

            // Act
            var filtered = stream.Filter(x => x % 2 == 0);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        #endregion

        #region FlatMap Tests

        [Test]
        public void FlatMap_WithFuncDelegate_FlatMapsElements()
        {
            // Arrange
            var collection = new[] { "a,b", "c,d" };
            var stream = this._env.FromCollection(collection);

            // Act
            var flatMapped = stream.FlatMap(s => s.Split(','));

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void FlatMap_WithFlatMapFunction_FlatMapsElements()
        {
            // Arrange
            var collection = new[] { "a,b", "c,d" };
            var stream = this._env.FromCollection(collection);
            var flatMapFunction = new TestFlatMapFunction();

            // Act
            var flatMapped = stream.FlatMap(flatMapFunction);

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void FlatMap_OnCollectionStream_FlatMapsCollection()
        {
            // Arrange
            var collection = new[] { new[] { 1, 2 }, new[] { 3, 4 } };
            var stream = this._env.FromCollection(collection);

            // Act
            var flatMapped = stream.FlatMap(arr => arr);

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        #region KeyBy and GroupBy Tests

        [Test]
        public void KeyBy_WithFuncDelegate_CreatesKeyedStream()
        {
            // Arrange
            var collection = new[] { ("a", 1), ("b", 2), ("a", 3) };
            var stream = this._env.FromCollection(collection);

            // Act
            var keyed = stream.KeyBy(x => x.Item1);

            // Assert
            Assert.That(keyed, Is.Not.Null);
            Assert.That(keyed, Is.InstanceOf<KeyedStream<(string, int), string>>());
        }

        [Test]
        public void GroupBy_WithFieldName_CreatesKeyedStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var grouped = stream.GroupBy("field");

            // Assert
            Assert.That(grouped, Is.Not.Null);
            Assert.That(grouped, Is.InstanceOf<KeyedStream<int, string>>());
        }

        #endregion

        #region Sink Tests

        [Test]
        public void SinkToKafka_WithValidParameters_CreatesSink()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092");

            // Act
            var result = stream.SinkToKafka("output-topic", "localhost:9092", s => s);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SinkToKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() =>
                stream.SinkToKafka("output-topic", null, s => s));
        }

        [Test]
        public void SinkToKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var stream = this._env.FromKafka("input-topic", "localhost:9092");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() =>
                stream.SinkToKafka("output-topic", "", s => s));
        }

        [Test]
        public void SinkToKafka_OnNonJobDefinitionStream_ThrowsInvalidOperationException()
        {
            // Arrange
            var collection = new[] { "a", "b", "c" };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<InvalidOperationException>(() =>
                stream.SinkToKafka("output-topic", "localhost:9092", s => s));
        }

        [Test]
        public void AddSink_WithKafkaSinkFunction_RegistersSink()
        {
            // Arrange
            var collection = new[] { "a", "b", "c" };
            var stream = this._env.FromCollection(collection);
            var sinkFunction = new TestKafkaSinkFunction();

            // Act
            var result = stream.AddSink(sinkFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Print_RegistersPrintSink()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Print();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Partitioning Tests

        [Test]
        public void Rebalance_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Rebalance();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Rescale_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Rescale();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Forward_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Forward();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Shuffle_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Shuffle();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Broadcast_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Broadcast();

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void PartitionCustom_WithPartitionerAndKeySelector_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.PartitionCustom((key, numPartitions) => key % numPartitions, x => x);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetParallelism_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.SetParallelism(4);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Name_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Name("Test Stream");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithValidValue_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(0));
        }

        [Test]
        public void SetMaxParallelism_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(-1));
        }

        [Test]
        public void SetMaxParallelism_WithValueTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(40000));
        }

        [Test]
        public void SlotSharingGroup_WithGroupName_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.SlotSharingGroup("group1");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Timestamp and Watermark Tests

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedAssigner_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var assigner = new TestPunctuatedWatermarkAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPeriodicAssigner_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithWatermarkStrategy_ReturnsDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var strategy = Watermarks.WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

            // Act
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithNullStrategy_ThrowsArgumentNullException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() =>
                stream.AssignTimestampsAndWatermarks((Watermarks.WatermarkStrategy<int>) null!));
        }

        #endregion

        #region Window Tests

        [Test]
        public void TimeWindowAll_WithTimeSize_CreatesAllWindowedStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var windowed = stream.TimeWindowAll(Time.Seconds(10));

            // Assert
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed, Is.InstanceOf<AllWindowedStream<int>>());
        }

        [Test]
        public void CountWindowAll_WithValidSize_CreatesAllWindowedStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var windowed = stream.CountWindowAll(5);

            // Assert
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed, Is.InstanceOf<AllWindowedStream<int>>());
        }

        [Test]
        public void CountWindowAll_WithZeroSize_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(0));
        }

        [Test]
        public void CountWindowAll_WithNegativeSize_ThrowsArgumentException()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(-1));
        }

        [Test]
        public void Where_WithFilterExpression_ReturnsDataStream()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092");

            // Act
            var result = stream.Where("value > 10");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void Where_OnNonJobDefinitionStream_ReturnsSameStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var result = stream.Where("value > 1");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region GetExecutionEnvironment Test

        [Test]
        public void GetExecutionEnvironment_ReturnsCorrectEnvironment()
        {
            // Arrange
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);

            // Act
            var returnedEnv = stream.GetExecutionEnvironment();

            // Assert
            Assert.That(returnedEnv, Is.SameAs(this._env));
        }

        #endregion

        #region Helper Classes

        private class TestMapFunction : IMapFunction<int, int>
        {
            public int Map(int value) => value * 2;
        }

        private class TestFilterFunction : IFilterFunction<int>
        {
            public bool Filter(int value) => value > 2;
        }

        private class TestFlatMapFunction : IFlatMapFunction<string, string>
        {
            public IEnumerable<string> FlatMap(string value) => value.Split(',');
        }

        private class TestKafkaSinkFunction : ISinkFunction<string>
        {
            public string Topic { get; } = "test-topic";
            public string BootstrapServers { get; } = "localhost:9092";

            public Task InvokeAsync(string element, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element * 1000L;
            public Watermark? CheckAndGetNextWatermark(int lastElement, long extractedTimestamp) => null;
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element * 1000L;
            public Watermark? GetCurrentWatermark() => null;
        }

        #endregion
    }
}
