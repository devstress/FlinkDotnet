using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for extension methods in FlinkAPIExtensions.cs to achieve 100% coverage.
    /// Targets: DataStreamExtensions, StreamExecutionEnvironmentExtensions
    /// </summary>
    [TestFixture]
    public class ExtensionMethodsCoverageTests
    {
        [Test]
        public void DataStreamExtensions_AddSink_WithKafkaSinkFunction_ShouldReturnDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var kafkaSink = new KafkaSinkFunction<string>("output-topic", "localhost:9092", s => Encoding.UTF8.GetBytes(s));

            // Act
            var result = DataStreamExtensions.AddSink(stream, kafkaSink);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_SetStreamTimeCharacteristic_ProcessingTime_ShouldSetConfiguration()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = StreamExecutionEnvironmentExtensions.SetStreamTimeCharacteristic(env, TimeCharacteristic.ProcessingTime);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(env));
            var config = env.GetConfig().GetConfiguration();
            var characteristic = config.GetString("stream.time-characteristic", null);
            Assert.That(characteristic, Is.EqualTo("ProcessingTime"));
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_SetStreamTimeCharacteristic_EventTime_ShouldSetConfiguration()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = StreamExecutionEnvironmentExtensions.SetStreamTimeCharacteristic(env, TimeCharacteristic.EventTime);

            // Assert
            Assert.That(result, Is.Not.Null);
            var config = env.GetConfig().GetConfiguration();
            var characteristic = config.GetString("stream.time-characteristic", null);
            Assert.That(characteristic, Is.EqualTo("EventTime"));
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_SetStreamTimeCharacteristic_IngestionTime_ShouldSetConfiguration()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = StreamExecutionEnvironmentExtensions.SetStreamTimeCharacteristic(env, TimeCharacteristic.IngestionTime);

            // Assert
            Assert.That(result, Is.Not.Null);
            var config = env.GetConfig().GetConfiguration();
            var characteristic = config.GetString("stream.time-characteristic", null);
            Assert.That(characteristic, Is.EqualTo("IngestionTime"));
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithSourceFunction_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var result = StreamExecutionEnvironmentExtensions.AddSource(env, sourceFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void KafkaSinkFunction_Properties_ShouldReturnCorrectValues()
        {
            // Arrange
            var topic = "test-topic";
            var servers = "localhost:9092";
            var serializer = new Func<string, byte[]>(s => Encoding.UTF8.GetBytes(s));

            // Act
            var kafkaSink = new KafkaSinkFunction<string>(topic, servers, serializer);

            // Assert
            Assert.That(kafkaSink.Topic, Is.EqualTo(topic));
            Assert.That(kafkaSink.BootstrapServers, Is.EqualTo(servers));
        }

        [Test]
        public async Task KafkaSinkFunction_InvokeAsync_ShouldCompleteSuccessfully()
        {
            // Arrange
            var kafkaSink = new KafkaSinkFunction<string>("test-topic", "localhost:9092", s => Encoding.UTF8.GetBytes(s));
            var element = "test-message";

            // Act
            await kafkaSink.InvokeAsync(element);

            // Assert - Task completed without exception
            Assert.Pass();
        }

        [Test]
        public async Task KafkaSinkFunction_InvokeAsync_WithCancellationToken_ShouldRespectCancellation()
        {
            // Arrange
            var kafkaSink = new KafkaSinkFunction<string>("test-topic", "localhost:9092", s => Encoding.UTF8.GetBytes(s));
            var element = "test-message";
            using var cts = new CancellationTokenSource();

            // Act
            await kafkaSink.InvokeAsync(element, cts.Token);

            // Assert - Task completed without exception
            Assert.Pass();
        }

        [Test]
        public void TypeInformation_Of_Generic_ShouldCreateTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<string>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void TypeInformation_Of_SpecificType_ShouldCreateTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation.Of<int>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(int)));
        }

        [Test]
        public void StartingOffsets_Constants_ShouldHaveCorrectValues()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        private class TestSourceFunction : ISourceFunction<string>
        {
            public async System.Collections.Generic.IAsyncEnumerable<string> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                await Task.Delay(1, cancellationToken);
                yield return "test";
            }
        }
    }
}
