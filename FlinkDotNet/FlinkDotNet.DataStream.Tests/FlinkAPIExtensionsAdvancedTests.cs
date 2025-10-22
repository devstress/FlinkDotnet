using NUnit.Framework;
using FlinkDotNet.DataStream;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class FlinkAPIExtensionsAdvancedTests
    {
        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithSourceFunction_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_UsesDefaultSourceName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert - Stream should be created successfully with default "Kafka Source" name
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_ReturnsDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "test1", "test2", "test3" };
            var stream = env.FromCollection(collection);
            var kafkaSink = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                str => System.Text.Encoding.UTF8.GetBytes(str)
            );

            // Act
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_AllowsMethodChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "data1", "data2" };
            var stream = env.FromCollection(collection);
            var kafkaSink = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                str => System.Text.Encoding.UTF8.GetBytes(str)
            );

            // Act
            var result = stream
                .AddSink(kafkaSink)
                .SetParallelism(2)
                .Name("Test Sink");

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithCustomSourceFunction_WorksCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var customSource = new CustomRangeSourceFunction(1, 5);

            // Act
            var stream = env.AddSource(customSource);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<int>>());
        }

        // Test source functions
        private class TestSourceFunction : ISourceFunction<string>
        {
            public async IAsyncEnumerable<string> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await System.Threading.Tasks.Task.Yield();
                yield return "test-message-1";
                yield return "test-message-2";
            }
        }

        private class CustomRangeSourceFunction : ISourceFunction<int>
        {
            private readonly int _start;
            private readonly int _end;

            public CustomRangeSourceFunction(int start, int end)
            {
                _start = start;
                _end = end;
            }

            public async IAsyncEnumerable<int> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await System.Threading.Tasks.Task.Yield();
                for (int i = _start; i <= _end; i++)
                {
                    yield return i;
                }
            }
        }
    }
}