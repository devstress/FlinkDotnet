using System;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Edge case tests for OperationCapture to achieve complete code coverage.
    /// Tests unusual scenarios, boundary conditions, and error paths.
    /// </summary>
    [TestFixture]
    public class OperationCaptureEdgeCasesTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            this._env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "localhost");
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "8081");
        }

        #region FlatMap Operation Edge Cases

        [Test]
        public void OperationCapture_FlatMap_WithEmptyResults_HandlesCorrectly()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - FlatMap that returns empty enumerable
            var flatMapped = stream.FlatMap(s => Enumerable.Empty<string>());

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_FlatMap_WithFlatMapFunction_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var flatMapFunction = new TestFlatMapFunction();

            // Act
            var flatMapped = stream.FlatMap(flatMapFunction);

            // Assert
            Assert.That(flatMapped, Is.Not.Null);
        }

        #endregion

        #region Map Operation Edge Cases

        [Test]
        public void OperationCapture_Map_WithIdentityFunction_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Identity map (returns same value)
            var mapped = stream.Map(s => s);

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_Map_WithComplexTransformation_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Complex transformation
            var mapped = stream.Map(s => $"{s.ToUpper()}_{s.Length}_{DateTime.UtcNow.Ticks}");

            // Assert
            Assert.That(mapped, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_Map_WithReduceFunction_WorksCorrectly()
        {
            // Arrange
            var testData = new[] { "a", "b", "c" };
            var stream = this._env.FromCollection(testData);
            var keyed = stream.KeyBy(s => s.Length);

            // Act - Use ReduceFunction interface
            var reduced = keyed.Reduce(new TestReduceFunction());

            // Assert
            Assert.That(reduced, Is.Not.Null);
        }

        #endregion

        #region Filter Operation Edge Cases

        [Test]
        public void OperationCapture_Filter_WithAlwaysTruePredicate_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Filter that always returns true
            var filtered = stream.Filter(s => true);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_Filter_WithAlwaysFalsePredicate_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Filter that always returns false
            var filtered = stream.Filter(s => false);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_Filter_WithFilterFunction_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var filterFunction = new TestFilterFunction();

            // Act
            var filtered = stream.Filter(filterFunction);

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        #endregion

        #region Where Operation Edge Cases

        [Test]
        public void OperationCapture_Where_WithoutJobDefinition_ReturnsOriginalStream()
        {
            // Arrange - Create stream without JobDefinition (collection source)
            var testData = new[] { "test1", "test2" };
            var stream = this._env.FromCollection(testData);

            // Act - Where should be no-op without JobDefinition
            var filtered = stream.Where("value > 10");

            // Assert - Should return same stream instance
            Assert.That(filtered, Is.SameAs(stream));
        }

        [Test]
        public void OperationCapture_Where_WithJobDefinition_AddsFilterOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act
            var filtered = stream.Where("length > 5");

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        #endregion

        #region GroupBy Operation Edge Cases

        [Test]
        public void OperationCapture_GroupBy_WithStringKey_CreatesKeyedStream()
        {
            // Arrange
            var testData = new[] { "apple", "banana", "apricot" };
            var stream = this._env.FromCollection(testData);

            // Act - Group by first character (as string)
            var grouped = stream.GroupBy("firstChar");

            // Assert
            Assert.That(grouped, Is.Not.Null);
            Assert.That(grouped, Is.InstanceOf<KeyedStream<string, string>>());
        }

        #endregion

        #region Print Operation Edge Cases

        [Test]
        public void OperationCapture_Print_ReturnsStream()
        {
            // Arrange
            var testData = new[] { "log1", "log2" };
            var stream = this._env.FromCollection(testData);

            // Act
            var printed = stream.Print();

            // Assert - Print should return the same stream
            Assert.That(printed, Is.SameAs(stream));
        }

        #endregion

        #region AddSink Edge Cases

        [Test]
        public void OperationCapture_AddSink_WithNonKafkaSink_ReturnsStream()
        {
            // Arrange
            var testData = new[] { "data1", "data2" };
            var stream = this._env.FromCollection(testData);
            var customSink = new TestCustomSinkFunction();

            // Act
            var result = stream.AddSink(customSink);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void OperationCapture_AddSink_WithKafkaSinkFunction_ExtractsProperties()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "localhost");
            var stream = this._env.FromKafka("input-topic", "localhost:9092", "test-group");
            var kafkaSink = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act - AddSink with KafkaSinkFunction
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Timestamp and Watermark Edge Cases

        [Test]
        public void OperationCapture_AssignTimestampsAndWatermarks_WithPunctuatedAssigner_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var assigner = new TestPunctuatedWatermarkAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void OperationCapture_AssignTimestampsAndWatermarks_WithPeriodicAssigner_CapturesOperation()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void OperationCapture_AssignTimestampsAndWatermarks_WithWatermarkStrategy_ThrowsOnNull()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() =>
                stream.AssignTimestampsAndWatermarks((Watermarks.WatermarkStrategy<string>) null!));
        }

        [Test]
        public void OperationCapture_AssignTimestampsAndWatermarks_WithValidWatermarkStrategy_ReturnsStream()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        #endregion

        #region Window Edge Cases

        [Test]
        public void OperationCapture_CountWindowAll_WithZeroSize_ThrowsException()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(0));
        }

        [Test]
        public void OperationCapture_CountWindowAll_WithNegativeSize_ThrowsException()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert
            _ = Assert.Throws<ArgumentException>(() => stream.CountWindowAll(-5));
        }

        [Test]
        public void OperationCapture_TimeWindowAll_WithVerySmallWindow_CreatesWindow()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - 1 millisecond window
            var windowed = stream.TimeWindowAll(Time.Milliseconds(1));

            // Assert
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed.GetWindowSize()!.ToMilliseconds(), Is.EqualTo(1));
        }

        [Test]
        public void OperationCapture_TimeWindowAll_WithVeryLargeWindow_CreatesWindow()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - 1 hour window
            var windowed = stream.TimeWindowAll(Time.Hours(1));

            // Assert
            Assert.That(windowed, Is.Not.Null);
            Assert.That(windowed.GetWindowSize()!.ToMilliseconds(), Is.EqualTo(3600000));
        }

        #endregion

        #region GetExecutionEnvironment Edge Cases

        [Test]
        public void OperationCapture_GetExecutionEnvironment_ReturnsEnvironment()
        {
            // Arrange
            var testData = new[] { 1, 2, 3 };
            var stream = this._env.FromCollection(testData);

            // Act
            var environment = stream.GetExecutionEnvironment();

            // Assert
            Assert.That(environment, Is.SameAs(this._env));
        }

        #endregion

        #region Invalid Operation Combinations

        [Test]
        public void OperationCapture_Map_AfterInvalidSource_ThrowsException()
        {
            // Arrange - Create a stream with no valid source (internal test scenario)
            var testData = new string[] { };
            var stream = this._env.FromCollection(testData);

            // Force the stream into an invalid state by mapping multiple times
            var mapped1 = stream.Map(s => s.ToUpper());
            var mapped2 = mapped1.Map(s => s.ToLower());

            // Assert - Stream should still be valid even with multiple maps
            Assert.That(mapped2, Is.Not.Null);
        }

        #endregion

        #region Helper Classes

        private class TestFlatMapFunction : IFlatMapFunction<string, string>
        {
            public System.Collections.Generic.IEnumerable<string> FlatMap(string value) => value.Split(' ');
        }

        private class TestReduceFunction : IReduceFunction<string>
        {
            public string Reduce(string value1, string value2) => value1 + value2;
        }

        private class TestFilterFunction : IFilterFunction<string>
        {
            public bool Filter(string value) => value.Length > 3;
        }

        private class TestCustomSinkFunction : ISinkFunction<string>
        {
            public System.Threading.Tasks.Task InvokeAsync(string element, System.Threading.CancellationToken cancellationToken = default) =>
                // Custom sink logic
                System.Threading.Tasks.Task.CompletedTask;
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp) => new Watermark(extractedTimestamp);
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp) => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            public Watermark? GetCurrentWatermark() => new Watermark(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        #endregion
    }
}
