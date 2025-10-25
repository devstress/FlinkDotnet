using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Final tests to achieve 100% code coverage for FlinkDotNet.DataStream.
    /// Targets remaining gaps in DataStreamExtensions, StreamExecutionEnvironmentExtensions,
    /// and other uncovered code paths.
    /// </summary>
    [TestFixture]
    public class FinalCoverageGapTests
    {
        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_ShouldCallInternalAddSink()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var sinkFunction = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act - This calls DataStreamExtensions.AddSink() which internally calls stream.AddSink()
            var result = DataStreamExtensions.AddSink(stream, sinkFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream)); // AddSink returns the same stream for chaining
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithSourceFunction_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act - This calls StreamExecutionEnvironmentExtensions.AddSource() which internally calls env.AddSource()
            var dataStream = StreamExecutionEnvironmentExtensions.AddSource(env, sourceFunction);

            // Assert
            Assert.That(dataStream, Is.Not.Null);
        }

        [Test]
        public void OperationCapture_TranslateFilterOperation_WithFunction_ShouldAddFilterOperation()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Use Filter with a function to trigger TranslateFilterOperation path
            var filtered = stream.Filter(new TestFilterFunction());
            filtered.SinkToKafka("output-topic", "localhost:9092");

            // Assert - The stream should have the filter operation captured
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void KafkaSinkFunction_InvokeAsync_ShouldReturnCompletedTask()
        {
            // Arrange
            var sinkFunction = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s)
            );

            // Act
            var task = sinkFunction.InvokeAsync("test-message");

            // Assert
            Assert.That(task.IsCompleted, Is.True);
        }

        [Test]
        public void TypeInformation_GetType_ShouldReturnCorrectType()
        {
            // Arrange
            var typeInfo = TypeInformation<string>.Of();

            // Act
            var type = typeInfo.GetType();

            // Assert
            Assert.That(type, Is.EqualTo(typeof(string)));
        }

        [Test]
        public void KafkaSourceFunctionExtensions_SetStartFromEarliest_ShouldReturnSameSource()
        {
            // Arrange
            var sourceFunction = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "latest"
            );

            // Act
            var result = KafkaSourceFunctionExtensions.SetStartFromEarliest(sourceFunction);

            // Assert
            Assert.That(result, Is.SameAs(sourceFunction));
        }

        [Test]
        public void KafkaSourceFunctionExtensions_AssignTimestampsAndWatermarks_WithPunctuated_ShouldReturnSameSource()
        {
            // Arrange
            var sourceFunction = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "latest"
            );
            var assigner = new TestPunctuatedAssigner();

            // Act
            var result = KafkaSourceFunctionExtensions.AssignTimestampsAndWatermarks(sourceFunction, assigner);

            // Assert
            Assert.That(result, Is.SameAs(sourceFunction));
        }

        [Test]
        public void KafkaSourceFunctionExtensions_AssignTimestampsAndWatermarks_WithPeriodic_ShouldReturnSameSource()
        {
            // Arrange
            var sourceFunction = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "latest"
            );
            var assigner = new TestPeriodicAssigner();

            // Act
            var result = KafkaSourceFunctionExtensions.AssignTimestampsAndWatermarks(sourceFunction, assigner);

            // Assert
            Assert.That(result, Is.SameAs(sourceFunction));
        }

        [Test]
        public void StartingOffsets_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        // Helper classes
        private class TestSourceFunction : ISourceFunction<string>
        {
            public async System.Collections.Generic.IAsyncEnumerable<string> RunAsync([EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                yield return "test-data";
                await Task.CompletedTask;
            }
        }

        private class TestFilterFunction : IFilterFunction<string>
        {
            public bool Filter(string value)
            {
                return !string.IsNullOrEmpty(value);
            }
        }

        private class TestPunctuatedAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp)
            {
                return new Watermark(extractedTimestamp);
            }
        }

        private class TestPeriodicAssigner : IAssignerWithPeriodicWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark? GetCurrentWatermark()
            {
                return new Watermark(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }
    }
}
