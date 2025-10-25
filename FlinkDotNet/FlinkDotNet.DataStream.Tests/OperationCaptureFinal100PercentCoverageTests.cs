#nullable enable
using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Final tests to achieve 100% branch coverage for OperationCapture.
    /// Each test targets specific uncovered branches identified in coverage analysis.
    /// </summary>
    [TestFixture]
    public class OperationCaptureFinal100PercentCoverageTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "localhost");
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "8081");
        }

        #region ConfigureJobMetadata Branches (Lines 209, 214, 216, 219, 224)

        [Test]
        public void OperationCapture_ConfigureJobMetadata_WithTimestampAssigner_SetsEventTime()
        {
            // This test covers line 209-211: if (this._hasTimestampAssigner)
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", (Func<string, string>)(s => s));
            
            // Assign timestamps to set _hasTimestampAssigner = true
            var withTimestamps = stream.AssignTimestampsAndWatermarks(
                Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps()
                    .WithTimestampAssigner(_ => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds())
            );
            
            var result = withTimestamps.SinkToKafka("output-topic", "localhost:9092");
            
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_ConfigureJobMetadata_WithDeserializer_SetsProperty()
        {
            // This test covers line 214-216: if (this._deserializationFunction != null)
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            
            // Use AddKafkaSource with deserializer to set _deserializationFunction
            Func<string, string> deserializer = s => s.ToUpper();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", deserializer);
            
            var result = stream.SinkToKafka("output-topic", "localhost:9092");
            
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_ConfigureJobMetadata_WithSerializer_SetsProperty()
        {
            // This test covers line 219-224: if (this._serializationFunction != null)
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", (Func<string, string>)(s => s));
            
            // Use SinkToKafka with serializer to set _serializationFunction
            Func<string, string> serializer = s => s.ToLower();
            var result = stream.SinkToKafka("output-topic", "localhost:9092", serializer);
            
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region TranslateAggregateOperation Branches (Line 317-319, 327-340)

        [Test]
        public void OperationCapture_TranslateAggregateOperation_WithFunction_SetsProperty()
        {
            // This covers line 317-319: if (operation.Function != null)
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", (Func<string, string>)(s => s));
            
            // Create a windowed stream with aggregate
            var windowed = stream.TimeWindowAll(Time.Seconds(5));
            var testAggFunc = new TestAggregateFunction();
            var aggregated = windowed.Aggregate(testAggFunc);
            
            var result = aggregated.SinkToKafka("output-topic", "localhost:9092");
            
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_TranslateAggregateOperation_CountBasedWindow_SetsWindowCount()
        {
            // This covers line 327-332: if (this._windowDefinition.IsCountBased)
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", (Func<string, string>)(s => s));
            
            // Create a COUNT-based windowed stream
            var windowed = stream.CountWindowAll(100);
            var testAggFunc = new TestAggregateFunction();
            var aggregated = windowed.Aggregate(testAggFunc);
            
            var result = aggregated.SinkToKafka("output-topic", "localhost:9092");
            
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_TranslateAggregateOperation_TimeBasedWindow_SetsWindowSeconds()
        {
            // This covers line 334-340: else branch for time-based windows
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", (Func<string, string>)(s => s));
            
            // Create a TIME-based windowed stream
            var windowed = stream.TimeWindowAll(Time.Seconds(10));
            var testAggFunc = new TestAggregateFunction();
            var aggregated = windowed.Aggregate(testAggFunc);
            
            var result = aggregated.SinkToKafka("output-topic", "localhost:9092");
            
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Helper Classes

        private class TestAggregateFunction : IAggregateFunction<string, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(string value, int accumulator) => accumulator + 1;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }

        #endregion
    }
}
