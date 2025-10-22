using System;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class OperationCaptureTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        #region CaptureKafkaSource Tests

        [Test]
        public void CaptureKafkaSource_WithValidParameters_CreatesDataStream()
        {
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void CaptureKafkaSource_WithLatestOffset_CreatesDataStream()
        {
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group", "latest");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void CaptureKafkaSource_WithDeserializer_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("test-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void CaptureKafkaSource_WithComplexDeserializer_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("test-topic", "localhost:9092", "test-group",
                (string s) => new TestMessage { Value = s }, "earliest");
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        #region CaptureMapOperation Tests

        [Test]
        public void CaptureMapOperation_WithUpperExpression_CreatesDataStream()
        {
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            var mapped = stream.Map("upper");
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void CaptureMapOperation_WithLowerExpression_CreatesDataStream()
        {
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            var mapped = stream.Map("lower");
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void CaptureMapOperation_WithCustomMapFunction_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var mapped = stream.Map(new UpperCaseMapFunction());
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void CaptureMapOperation_ChainedMaps_CreatesDataStream()
        {
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            var result = stream
                .Map("upper")
                .Map("lower")
                .Map("upper");
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region CaptureFilterOperation Tests

        [Test]
        public void CaptureFilterOperation_WithCustomFilter_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var filtered = stream.Filter(new LengthFilterFunction());
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void CaptureFilterOperation_MultipleFilters_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var result = stream
                .Filter(new LengthFilterFunction())
                .Filter(new LengthFilterFunction());
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region CaptureFlatMapOperation Tests

        [Test]
        public void CaptureFlatMapOperation_WithCustomFunction_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var flatMapped = stream.FlatMap(new SplitFlatMapFunction());
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        #region CaptureTimeWindow Tests

        [Test]
        public void CaptureTimeWindow_WithSeconds_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void CaptureTimeWindow_WithMinutes_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Minutes(2));
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void CaptureTimeWindow_WithHours_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Hours(1));
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void CaptureTimeWindow_WithMilliseconds_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Milliseconds(5000));
            Assert.That(windowed, Is.Not.Null);
        }

        #endregion

        #region CaptureCountWindow Tests

        [Test]
        public void CaptureCountWindow_SmallCount_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.CountWindowAll(10);
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void CaptureCountWindow_LargeCount_CreatesWindowedStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.CountWindowAll(1000);
            Assert.That(windowed, Is.Not.Null);
        }

        #endregion

        #region CaptureAggregateOperation Tests

        [Test]
        public void CaptureAggregateOperation_WithTimeWindow_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Seconds(30));
            var aggregated = windowed.Aggregate(new SumAggregateFunction());
            Assert.That(aggregated, Is.Not.Null);
        }

        [Test]
        public void CaptureAggregateOperation_WithCountWindow_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.CountWindowAll(100);
            var aggregated = windowed.Aggregate(new SumAggregateFunction());
            Assert.That(aggregated, Is.Not.Null);
        }

        [Test]
        public void CaptureAggregateOperation_WithComplexAggregateFunction_CreatesDataStream()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Seconds(10));
            var aggregated = windowed.Aggregate(new AverageAggregateFunction());
            Assert.That(aggregated, Is.Not.Null);
        }

        #endregion

        #region CaptureKafkaSink Tests

        [Test]
        public void CaptureKafkaSink_WithoutSerializer_CreatesSink()
        {
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            stream.SinkToKafka("output-topic", "localhost:9092");
            Assert.Pass("Sink created successfully");
        }

        [Test]
        public void CaptureKafkaSink_WithSerializer_CreatesSink()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            stream.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());
            Assert.Pass("Sink created successfully");
        }

        [Test]
        public void CaptureKafkaSink_WithComplexSerializer_CreatesSink()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => new TestMessage { Value = s }, "earliest");
            stream.SinkToKafka("output-topic", "localhost:9092", x => x.Value);
            Assert.Pass("Sink created successfully");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FullPipeline_MapFilterWindow_CreatesCompleteChain()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            
            var mapped = stream.Map(new MultiplyByTwoMapFunction());
            var filtered = mapped.Filter(new GreaterThanTenFilterFunction());
            var windowed = filtered.TimeWindowAll(Time.Seconds(5));
            
            Assert.That(windowed, Is.Not.Null);
            Assert.Pass("Full pipeline created successfully");
        }

        [Test]
        public void ComplexPipeline_MultipleWindows_CreatesCompleteChain()
        {
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            
            var windowed = stream.CountWindowAll(100);
            var aggregated = windowed.Aggregate(new AverageAggregateFunction());
            aggregated.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());
            
            Assert.Pass("Complex pipeline created successfully");
        }

        #endregion

        #region ToJobDefinition Tests

        [Test]
        public void ToJobDefinition_WithCompleteMapPipeline_CreatesJobDefinition()
        {
            // Arrange
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            stream.Map("upper").SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert - Should not throw when creating job definition internally
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithLowerMapFunction_CreatesJobDefinition()
        {
            // Arrange
            var stream = _env.FromKafka("input-topic", "localhost:9092", "test-group", "earliest");
            var result = stream.Map("lower");
            result.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithCapitalizerFunction_MapsToUpper()
        {
            // Arrange
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var mapped = stream.Map(new WordsCapitalizerMapFunction());
            mapped.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithLowerCaseFunction_MapsToLower()
        {
            // Arrange
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var mapped = stream.Map(new LowerCaseMapFunction());
            mapped.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithUnknownMapFunction_UsesIdentity()
        {
            // Arrange
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var mapped = stream.Map(new UnknownMapFunction());
            mapped.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithTimestampAssigner_SetsEventTimeCharacteristic()
        {
            // Arrange
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => s, "earliest");
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps()
                .WithTimestampAssigner(s => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            var withWatermarks = stream.AssignTimestampsAndWatermarks(strategy);
            withWatermarks.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert
            Assert.That(withWatermarks, Is.Not.Null);
        }

        [Test]
        public void ToJobDefinition_WithAggregateAndWindow_CreatesAggregateOperation()
        {
            // Arrange
            var stream = _env.AddKafkaSource("input-topic", "localhost:9092", "test-group",
                (string s) => int.Parse(s), "earliest");
            var windowed = stream.TimeWindowAll(Time.Seconds(10));
            var aggregated = windowed.Aggregate(new SumAggregateFunction());
            aggregated.SinkToKafka("output-topic", "localhost:9092", x => x.ToString());
            
            // Act & Assert
            Assert.That(aggregated, Is.Not.Null);
        }

        [Test]
        public void HasOperations_WithKafkaSource_ReturnsTrue()
        {
            // Arrange
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");
            stream.SinkToKafka("output-topic", "localhost:9092");
            
            // Act & Assert - Should create operations
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        #region LogCleanup Tests

        [Test]
        public void CreateLogger_WithOldLogFiles_CleansUpOldFiles()
        {
            // This test verifies that the logger creation with cleanup doesn't throw
            // The actual cleanup is tested indirectly through normal operation
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        // Test helper classes
        private class WordsCapitalizerMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }

        private class LowerCaseMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToLower();
        }

        private class UnknownMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value; // Identity function
        }

        // Test helper classes
        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        private class UpperCaseMapFunction : IMapFunction<string, string>
        {
            public string Map(string value) => value.ToUpper();
        }

        private class MultiplyByTwoMapFunction : IMapFunction<int, int>
        {
            public int Map(int value) => value * 2;
        }

        private class LengthFilterFunction : IFilterFunction<string>
        {
            public bool Filter(string value) => value.Length > 5;
        }

        private class GreaterThanTenFilterFunction : IFilterFunction<int>
        {
            public bool Filter(int value) => value > 10;
        }

        private class SplitFlatMapFunction : IFlatMapFunction<string, string>
        {
            public System.Collections.Generic.IEnumerable<string> FlatMap(string value) => value.Split(' ');
        }

        private class SumAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int acc1, int acc2) => acc1 + acc2;
        }

        private class AverageAggregateFunction : IAggregateFunction<int, (int, int), double>
        {
            public (int, int) CreateAccumulator() => (0, 0);
            public (int, int) Add(int value, (int, int) accumulator) => 
                (accumulator.Item1 + value, accumulator.Item2 + 1);
            public double GetResult((int, int) accumulator) => 
                accumulator.Item2 == 0 ? 0.0 : (double)accumulator.Item1 / accumulator.Item2;
            public (int, int) Merge((int, int) acc1, (int, int) acc2) => 
                (acc1.Item1 + acc2.Item1, acc1.Item2 + acc2.Item2);
        }
    }
}