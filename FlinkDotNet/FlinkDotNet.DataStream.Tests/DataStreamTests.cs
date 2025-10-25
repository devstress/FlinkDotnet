using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class DataStreamTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup() => this._env = StreamExecutionEnvironment.GetExecutionEnvironment();

        #region Map Tests

        [Test]
        public void Map_WithFunc_TransformsElements()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);
            var mapped = stream.Map(x => x * 2);
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithMapFunction_TransformsElements()
        {
            var collection = new[] { "test", "data" };
            var stream = this._env.FromCollection(collection);
            var mapFunction = new TestMapFunction();
            var mapped = stream.Map(mapFunction);
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithExpressionUpper_ReturnsDataStream()
        {
            var kafkaStream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var mapped = kafkaStream.Map("upper");
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithExpressionLower_ReturnsDataStream()
        {
            var kafkaStream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var mapped = kafkaStream.Map("lower");
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_WithExpressionIdentity_ReturnsDataStream()
        {
            var kafkaStream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var mapped = kafkaStream.Map("identity");
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Map_ExpressionOnNonStringStream_ThrowsNotSupportedException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<NotSupportedException>(() => stream.Map("upper"));
        }

        [Test]
        public void Map_ExpressionWithoutIRBackedStream_ThrowsInvalidOperationException()
        {
            var collection = new[] { "test", "data" };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<InvalidOperationException>(() => stream.Map("upper"));
        }

        #endregion

        #region Filter Tests

        [Test]
        public void Filter_WithFunc_FiltersElements()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);
            var filtered = stream.Filter(x => x > 3);
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void Filter_WithFilterFunction_FiltersElements()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);
            var filterFunction = new TestFilterFunction();
            var filtered = stream.Filter(filterFunction);
            Assert.That(filtered, Is.Not.Null);
        }

        #endregion

        #region FlatMap Tests

        [Test]
        public void FlatMap_WithFunc_FlattensElements()
        {
            var collection = new[] { "hello world", "test data" };
            var stream = this._env.FromCollection(collection);
            var flatMapped = stream.FlatMap(x => x.Split(' '));
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void FlatMap_WithFlatMapFunction_FlattensElements()
        {
            var collection = new[] { "hello world", "test data" };
            var stream = this._env.FromCollection(collection);
            var flatMapFunction = new TestFlatMapFunction();
            var flatMapped = stream.FlatMap(flatMapFunction);
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        #region Where Tests

        [Test]
        public void Where_WithExpression_ReturnsDataStream()
        {
            var kafkaStream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var filtered = kafkaStream.Where("value > 0");
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void Where_WithoutIRBackedStream_ReturnsThis()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var filtered = stream.Where("value > 0");
            Assert.That(filtered, Is.SameAs(stream));
        }

        #endregion

        #region KeyBy and GroupBy Tests

        [Test]
        public void KeyBy_WithFunc_ReturnsKeyedStream()
        {
            var collection = new[] { "apple", "banana", "apricot", "blueberry" };
            var stream = this._env.FromCollection(collection);
            var keyed = stream.KeyBy(x => x[0]);
            Assert.That(keyed, Is.Not.Null);
            Assert.That(keyed, Is.InstanceOf<KeyedStream<string, char>>());
        }

        [Test]
        public void GroupBy_WithFieldName_ReturnsKeyedStream()
        {
            var collection = new[] { "test1", "test2", "test3" };
            var stream = this._env.FromCollection(collection);
            var grouped = stream.GroupBy("field");
            Assert.That(grouped, Is.Not.Null);
            Assert.That(grouped, Is.InstanceOf<KeyedStream<string, string>>());
        }

        #endregion

        #region Sink Tests

        [Test]
        public void SinkToKafka_WithValidParameters_ReturnsDataStream()
        {
            var kafkaStream = this._env.FromKafka("input-topic", "localhost:9092", "test-group");
            var sunk = kafkaStream.SinkToKafka("output-topic", "localhost:9092");
            Assert.That(sunk, Is.Not.Null);
        }

        [Test]
        public void SinkToKafka_NullBootstrapServers_ThrowsArgumentException()
        {
            var kafkaStream = this._env.FromKafka("input-topic", "localhost:9092", "test-group");
            _ = Assert.Throws<ArgumentException>(() => kafkaStream.SinkToKafka("output-topic", null));
        }

        [Test]
        public void SinkToKafka_EmptyBootstrapServers_ThrowsArgumentException()
        {
            var kafkaStream = this._env.FromKafka("input-topic", "localhost:9092", "test-group");
            _ = Assert.Throws<ArgumentException>(() => kafkaStream.SinkToKafka("output-topic", ""));
        }

        [Test]
        public void SinkToKafka_WhitespaceBootstrapServers_ThrowsArgumentException()
        {
            var kafkaStream = this._env.FromKafka("input-topic", "localhost:9092", "test-group");
            _ = Assert.Throws<ArgumentException>(() => kafkaStream.SinkToKafka("output-topic", "   "));
        }

        [Test]
        public void SinkToKafka_WithSerializer_ReturnsDataStream()
        {
            var stream = this._env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s));
            var sunk = stream.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());
            Assert.That(sunk, Is.Not.Null);
        }

        [Test]
        public void SinkToKafka_WithoutIRBackedStream_ThrowsInvalidOperationException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<InvalidOperationException>(() => stream.SinkToKafka("output-topic", "localhost:9092"));
        }

        [Test]
        public void AddSink_WithSinkFunction_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var sinkFunction = new TestSinkFunction();
            var result = stream.AddSink(sinkFunction);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Print_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Print();
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void SetParallelism_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.SetParallelism(4);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Name_WithValidName_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Name("Test Stream");
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SetMaxParallelism_ValidValue_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.SetMaxParallelism(100);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void SetMaxParallelism_MaxValue_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.SetMaxParallelism(32768);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_ZeroValue_ThrowsArgumentException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(0));
        }

        [Test]
        public void SetMaxParallelism_NegativeValue_ThrowsArgumentException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(-1));
        }

        [Test]
        public void SetMaxParallelism_TooLargeValue_ThrowsArgumentException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentException>(() => stream.SetMaxParallelism(32769));
        }

        [Test]
        public void SlotSharingGroup_WithValidName_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.SlotSharingGroup("test-group");
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region Partitioning Tests

        [Test]
        public void Rebalance_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Rebalance();
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Rescale_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Rescale();
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Forward_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Forward();
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Shuffle_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Shuffle();
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void Broadcast_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var result = stream.Broadcast();
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void PartitionCustom_WithValidPartitioner_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = this._env.FromCollection(collection);
            var result = stream.PartitionCustom((key, numPartitions) => key % numPartitions, x => x);
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region Timestamp and Watermark Tests

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedWatermarks_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var assigner = new TestPunctuatedWatermarkAssigner();
            var result = stream.AssignTimestampsAndWatermarks(assigner);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPeriodicWatermarks_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var assigner = new TestPeriodicWatermarkAssigner();
            var result = stream.AssignTimestampsAndWatermarks(assigner);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_NullStrategy_ThrowsArgumentNullException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentNullException>(() => stream.AssignTimestampsAndWatermarks((Watermarks.WatermarkStrategy<int>) null!));
        }

        #endregion

        #region Window Tests

        [Test]
        public void TimeWindowAll_WithValidSize_ReturnsAllWindowedStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed, Is.InstanceOf<AllWindowedStream<int>>());
        }

        [Test]
        public void CountWindowAll_WithValidSize_ReturnsAllWindowedStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var windowed = stream.CountWindowAll(100);
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed, Is.InstanceOf<AllWindowedStream<int>>());
        }

        [Test]
        public void CountWindowAll_ZeroSize_ThrowsArgumentException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(0));
        }

        [Test]
        public void CountWindowAll_NegativeSize_ThrowsArgumentException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(-1));
        }

        #endregion

        #region GetExecutionEnvironment Tests

        [Test]
        public void GetExecutionEnvironment_ReturnsEnvironment()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(collection);
            var env = stream.GetExecutionEnvironment();
            Assert.That(env, Is.SameAs(this._env));
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_MapFilterPrint_WorksTogether()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var result = this._env.FromCollection(collection)
                .Map(x => x * 2)
                .Filter(x => x > 5)
                .Print();
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MethodChaining_WithConfiguration_WorksTogether()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var result = this._env.FromCollection(collection)
                .SetParallelism(4)
                .Name("Test Stream")
                .SetMaxParallelism(128)
                .SlotSharingGroup("test-group")
                .Map(x => x * 2);
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void MethodChaining_WithPartitioning_WorksTogether()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var result = this._env.FromCollection(collection)
                .Rebalance()
                .Map(x => x * 2)
                .Shuffle()
                .Filter(x => x > 5)
                .Broadcast();
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Source Function Transformation Tests

        [Test]
        public void Map_OnSourceBackedStream_CreatesTransformedStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act
            var mapped = stream.Map(x => x * 2);

            // Assert - Just verify the stream is created
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Filter_OnSourceBackedStream_CreatesFilteredStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act
            var filtered = stream.Filter(x => x > 2);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void FlatMap_OnSourceBackedStream_CreatesFlatMappedStream()
        {
            // Arrange
            var source = new StringSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act
            var flatMapped = stream.FlatMap(x => x.Split(' '));

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void MapChain_OnSourceBackedStream_CreatesChainedStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act - Chain multiple maps
            var transformed = stream
                .Map(x => x * 2)
                .Map(x => x + 10)
                .Map(x => x.ToString());

            // Assert
            Assert.That(transformed, Is.Not.Null);
        }

        [Test]
        public void FilterChain_OnSourceBackedStream_CreatesChainedFilters()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act - Chain multiple filters
            var filtered = stream
                .Filter(x => x > 1)
                .Filter(x => x < 10)
                .Filter(x => x % 2 == 0);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void MapFilterFlatMapChain_OnSourceBackedStream_CreatesMixedChain()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");

            // Act - Mix map, filter, and flatmap
            var transformed = stream
                .Map(x => x * 2)
                .Filter(x => x > 3)
                .FlatMap(x => new[] { x, x + 1 });

            // Assert
            Assert.That(transformed, Is.Not.Null);
        }

        [Test]
        public void Map_WithMapFunction_OnSourceBackedStream_CreatesTransformedStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");
            var mapFunction = new MultiplyMapFunction();

            // Act
            var mapped = stream.Map(mapFunction);

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void Filter_WithFilterFunction_OnSourceBackedStream_CreatesFilteredStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");
            var filterFunction = new GreaterThanFilterFunction();

            // Act
            var filtered = stream.Filter(filterFunction);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void FlatMap_WithFlatMapFunction_OnSourceBackedStream_CreatesFlatMappedStream()
        {
            // Arrange
            var source = new IntegerSourceFunction();
            var stream = this._env.AddSource(source, "test-source");
            var flatMapFunction = new DuplicateFlatMapFunction();

            // Act
            var flatMapped = stream.FlatMap(flatMapFunction);

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        // Test helper classes
        private class TestMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }

        private class IntegerSourceFunction : ISourceFunction<int>
        {
            public async IAsyncEnumerable<int> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                for (int i = 1; i <= 10; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    await Task.Delay(10, cancellationToken);
                    yield return i;
                }
            }
        }

        private class StringSourceFunction : ISourceFunction<string>
        {
            public async IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                var phrases = new[] { "hello world", "test data", "sample text" };
                foreach (var phrase in phrases)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        yield break;
                    }

                    await Task.Delay(10, cancellationToken);
                    yield return phrase;
                }
            }
        }

        private class TestFilterFunction : IFilterFunction<int>
        {
            public bool Filter(int value) => value > 2;
        }

        private class TestFlatMapFunction : IFlatMapFunction<string, string>
        {
            public IEnumerable<string> FlatMap(string value) => value.Split(' ');
        }

        private class TestSinkFunction : ISinkFunction<int>
        {
            public Task InvokeAsync(int element, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element;
            public Watermark? CheckAndGetNextWatermark(int lastElement, long extractedTimestamp) => null;
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element;
            public Watermark? GetCurrentWatermark() => null;
        }

        private class MultiplyMapFunction : IMapFunction<int, int>
        {
            public int Map(int value) => value * 2;
        }

        private class GreaterThanFilterFunction : IFilterFunction<int>
        {
            public bool Filter(int value) => value > 2;
        }

        private class DuplicateFlatMapFunction : IFlatMapFunction<int, int>
        {
            public IEnumerable<int> FlatMap(int value) => new[] { value, value };
        }
    }
}
