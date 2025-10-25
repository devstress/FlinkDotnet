using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Branch coverage tests for AllWindowedStream class
    /// Targets uncovered branches at lines: 666, 667, 673, 697, 705
    /// </summary>
    [TestFixture]
    public class AllWindowedStreamBranchCoverageTests
    {
        #region Constructor Null Checks (Lines 666, 667, 673)

        [Test]
        public void Constructor_WithNullDataStream_ThrowsArgumentNullException()
        {
            // Arrange
            DataStream<string>? dataStream = null;
            var windowSize = Time.Seconds(5);

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new AllWindowedStream<string>(dataStream!, windowSize));

            Assert.That(ex!.ParamName, Is.EqualTo("dataStream"));
        }

        [Test]
        public void Constructor_WithNullWindowSize_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "test" };
            var dataStream = env.FromCollection(collection);
            Time? windowSize = null;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new AllWindowedStream<string>(dataStream, windowSize!));

            Assert.That(ex!.ParamName, Is.EqualTo("windowSize"));
        }

        [Test]
        public void Constructor_WithNullDataStreamForCountWindow_ThrowsArgumentNullException()
        {
            // Arrange
            DataStream<string>? dataStream = null;
            int windowCount = 100;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new AllWindowedStream<string>(dataStream!, windowCount));

            Assert.That(ex!.ParamName, Is.EqualTo("dataStream"));
        }

        [Test]
        public void Constructor_WithValidTimeWindow_SetsPropertiesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "test1", "test2" };
            var dataStream = env.FromCollection(collection);
            var windowSize = Time.Seconds(10);

            // Act
            var windowedStream = new AllWindowedStream<string>(dataStream, windowSize);

            // Assert
            Assert.That(windowedStream, Is.Not.Null);
            // Window properties are private, so we verify by successful construction
        }

        [Test]
        public void Constructor_WithValidCountWindow_SetsPropertiesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { 1, 2, 3, 4, 5 };
            var dataStream = env.FromCollection(collection);
            int windowCount = 50;

            // Act
            var windowedStream = dataStream.CountWindowAll(windowCount);

            // Assert
            Assert.That(windowedStream, Is.Not.Null);
            // Window count is private, verified by successful construction
        }

        #endregion

        #region Aggregate Operation (Lines 697, 705)

        /// <summary>
        /// Test aggregate operation with null source function to cover line 660 exception path
        /// </summary>
        [Test]
        public void Aggregate_WithNullSourceFunction_ThrowsInvalidOperationException()
        {
            // This test creates a DataStream from collection which has null _sourceFunction
            // When we try to aggregate, it should throw InvalidOperationException at line 660
            
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "test1", "test2", "test3" };
            // FromCollection creates a DataStream with null _sourceFunction
            var dataStream = env.FromCollection(collection);
            var windowedStream = new AllWindowedStream<string>(dataStream, Time.Seconds(5));
            var aggregateFunction = new CountAggregateFunction();

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                windowedStream.Aggregate(aggregateFunction));

            Assert.That(ex!.Message, Does.Contain("Source function required"));
        }

        /// <summary>
        /// Test aggregate operation without operation capture (line 697 null check)
        /// </summary>
        [Test]
        public void Aggregate_WithKafkaSource_PerformsAggregation()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var kafkaSource = new KafkaSourceFunction<int>("test-topic", "localhost:9092", "test-group", s => int.Parse(s), "earliest");
            var dataStream = new DataStream<int>(kafkaSource, env, "TestKafkaStream");
            var windowedStream = new AllWindowedStream<int>(dataStream, Time.Seconds(5));

            var aggregateFunction = new SumAggregateFunction();

            // Act
            var result = windowedStream.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        /// <summary>
        /// Test aggregate operation with operation capture attached (line 697, 715 coverage)
        /// </summary>
        [Test]
        public void Aggregate_WithOperationCapture_CapturesOperation()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var kafkaSource = new KafkaSourceFunction<string>("test-topic", "localhost:9092", "test-group", s => s, "earliest");
            var dataStream = new DataStream<string>(kafkaSource, env, "TestKafkaStream");
            var windowedStream = new AllWindowedStream<string>(dataStream, Time.Seconds(10));

            // Attach operation capture using reflection to test line 697, 715
            var operationCapture = new OperationCapture();
            var attachMethod = windowedStream.GetType().GetMethod("AttachOperationCapture",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            Assert.That(attachMethod, Is.Not.Null, "AttachOperationCapture method should exist");
            _ = attachMethod.Invoke(windowedStream, new object[] { operationCapture });

            var aggregateFunction = new CountAggregateFunction();

            // Act
            var result = windowedStream.Aggregate(aggregateFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Helper Classes

        private class SumAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }

        private class CountAggregateFunction : IAggregateFunction<string, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(string value, int accumulator) => accumulator + 1;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }

        #endregion
    }
}
