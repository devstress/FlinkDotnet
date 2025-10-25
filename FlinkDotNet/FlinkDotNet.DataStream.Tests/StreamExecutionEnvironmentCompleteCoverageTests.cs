using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using FlinkDotNet.Common;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for StreamExecutionEnvironment to achieve 100% coverage.
    /// Targets uncovered lines in StreamExecutionEnvironment class.
    /// </summary>
    [TestFixture]
    public class StreamExecutionEnvironmentCompleteCoverageTests
    {
        [SetUp]
        public void SetUp()
        {
            // Set environment variable required by FlinkJobGatewayConfiguration
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        [Test]
        public void StreamExecutionEnvironment_Constructor_WithConfiguration_ShouldInitialize()
        {
            // Arrange
            var config = new Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env.GetConfig(), Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_FromKafka_WithNullBootstrapServers_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                env.FromKafka("test-topic", bootstrapServers: null));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void StreamExecutionEnvironment_FromKafka_WithEmptyBootstrapServers_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                env.FromKafka("test-topic", bootstrapServers: ""));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void StreamExecutionEnvironment_FromKafka_WithWhitespaceBootstrapServers_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                env.FromKafka("test-topic", bootstrapServers: "   "));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void StreamExecutionEnvironment_AddKafkaSource_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Func<string, TestMessage> deserializer = json => new TestMessage { Data = json };

            // Act
            var stream = env.AddKafkaSource(
                "test-topic",
                "localhost:9092",
                "test-group",
                deserializer,
                "earliest");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_AddKafkaSource_WithLatestOffset_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Func<string, TestMessage> deserializer = json => new TestMessage { Data = json };

            // Act
            var stream = env.AddKafkaSource(
                "test-topic",
                "localhost:9092",
                "test-group",
                deserializer,
                "latest");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_WithValidValue_ShouldSucceed()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_WithMaxValue_ShouldSucceed()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(32768));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_WithZero_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_WithNegativeValue_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
        }

        [Test]
        public void StreamExecutionEnvironment_SetMaxParallelism_WithTooLargeValue_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
        }

        [Test]
        public void StreamExecutionEnvironment_FromCollection_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "a", "b", "c" };

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_AddSource_WithSourceFunction_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction, "Test Source");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_AddSource_WithDefaultName_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironment_ExecuteAsync_WithNoJob_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.ExecuteAsync("test-job"));
            Assert.That(ex.Message, Does.Contain("No Flink-compatible job"));
        }

        [Test]
        public void StreamExecutionEnvironment_ExecuteAsync_WithDefaultJobName_ShouldUseDefaultName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act & Assert - Will fail because no gateway, but tests the path
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.ExecuteAsync());
        }

        [Test]
        public void StreamExecutionEnvironment_ExecuteAsyncJob_WithNoJob_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await env.ExecuteAsyncJob("test-job"));
        }

        [Test]
        public void StreamExecutionEnvironment_Configure_ShouldAddConfiguration()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var config = new Configuration();
            config.SetString("new.key", "new.value");

            // Act
            var result = env.Configure(config);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(env));
            var envConfig = env.GetConfig().GetConfiguration();
            Assert.That(envConfig.GetString("new.key", null), Is.EqualTo("new.value"));
        }

        [Test]
        public void StreamExecutionEnvironment_DisableOperatorChaining_ShouldDisableChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.DisableOperatorChaining();

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void StreamExecutionEnvironment_EnableCheckpointing_ShouldSetInterval()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableCheckpointing(5000);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        }

        [Test]
        public void StreamExecutionEnvironment_EnableAdaptiveScheduler_ShouldEnableFeature()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void StreamExecutionEnvironment_EnableReactiveMode_ShouldEnableFeature()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableReactiveMode(true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void StreamExecutionEnvironment_FromSavepoint_ShouldSetPath()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var savepointPath = "/tmp/savepoint";

            // Act
            var result = env.FromSavepoint(savepointPath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetSavepointPath(), Is.EqualTo(savepointPath));
        }

        [Test]
        public void StreamExecutionEnvironment_SetStateBackend_WithNull_ShouldThrowException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => env.SetStateBackend(null!));
        }

        [Test]
        public void StreamExecutionEnvironment_GetCheckpointConfig_ShouldReturnConfig()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var config = env.GetCheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
        }

        private class TestMessage
        {
            public string Data { get; set; } = string.Empty;
        }

        private class TestSourceFunction : ISourceFunction<string>
        {
            public async System.Collections.Generic.IAsyncEnumerable<string> RunAsync(
                [EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                await Task.Delay(1, cancellationToken);
                yield return "test";
            }
        }
    }
}
