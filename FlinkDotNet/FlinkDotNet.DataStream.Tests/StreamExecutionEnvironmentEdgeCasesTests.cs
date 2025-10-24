using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StreamExecutionEnvironmentEdgeCasesTests
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
        public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() =>
                env.FromKafka("test-topic", null, "test-group"));
            
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
            Assert.That(ex.Message, Does.Contain("Kafka bootstrap servers must be provided"));
        }

        [Test]
        public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() =>
                env.FromKafka("test-topic", "", "test-group"));
            
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() =>
                env.FromKafka("test-topic", "   ", "test-group"));
            
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void FromKafka_WithNullGroupId_UsesDefaultGroup()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromKafka("test-topic", "localhost:9092", null);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddKafkaSource_WithDeserializer_CreatesStream()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            System.Func<string, string> deserializer = s => s.ToUpper();

            // Act
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", deserializer);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() => env.SetMaxParallelism(0));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() => env.SetMaxParallelism(-1));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithValueAbove32768_ThrowsArgumentException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<System.ArgumentException>(() => env.SetMaxParallelism(32769));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithBoundaryValue1_Succeeds()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(1);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(1));
        }

        [Test]
        public void SetMaxParallelism_WithBoundaryValue32768_Succeeds()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(32768));
        }

        [Test]
        public void ExecuteAsyncJob_WithoutActiveJob_ThrowsInvalidOperationException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.ThrowsAsync<System.InvalidOperationException>(async () =>
                await env.ExecuteAsyncJob("test-job"));
            
            Assert.That(ex!.Message, Does.Contain("No Flink-compatible job is defined"));
        }

        [Test]
        public void ExecuteAsyncJob_WithActiveJob_ReturnsJobClient()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act
            var task = env.ExecuteAsyncJob("test-job");

            // Assert
            Assert.That(task, Is.Not.Null);
            Assert.That(task.Result, Is.InstanceOf<FlinkDotNet.DataStream.JobClient>());
            Assert.That(task.Result.JobName, Is.EqualTo("test-job"));
        }

        [Test]
        public void Configure_WithConfiguration_AddsToExecutionConfig()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            var config = new FlinkDotNet.Common.Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var result = env.Configure(config);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void SetBufferTimeout_SetsCorrectValue()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.SetBufferTimeout(500);

            // Assert
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(500));
        }

        [Test]
        public void DisableOperatorChaining_DisablesChaining()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.DisableOperatorChaining();

            // Assert
            Assert.That(env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void EnableCheckpointing_SetsInterval()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableCheckpointing(5000);

            // Assert
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        }

        [Test]
        public void EnableAdaptiveScheduler_EnablesScheduler()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_WithFalse_DisablesScheduler()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableAdaptiveScheduler(true);

            // Act
            env.EnableAdaptiveScheduler(false);

            // Assert
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_EnablesMode()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableReactiveMode(true);

            // Assert
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_WithFalse_DisablesMode()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableReactiveMode(true);

            // Act
            env.EnableReactiveMode(false);

            // Assert
            Assert.That(env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void FromSavepoint_SetsSavepointPath()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            var path = "/tmp/savepoint";

            // Act
            env.FromSavepoint(path);

            // Assert
            Assert.That(env.GetSavepointPath(), Is.EqualTo(path));
        }

        [Test]
        public void SetStateBackend_WithNullBackend_ThrowsArgumentNullException()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() => env.SetStateBackend(null!));
        }

        [Test]
        public void GetCheckpointConfig_ReturnsNonNull()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var config = env.GetCheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
        }

        [Test]
        public void FromCollection_WithElements_CreatesStream()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "a", "b", "c" };

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithSourceFunction_CreatesStream()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction, "Test Source");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithDefaultSourceName_UsesDefault()
        {
            // Arrange
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void GetExecutionEnvironment_WithConfiguration_CreatesEnvironment()
        {
            // Arrange
            var config = new FlinkDotNet.Common.Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment(config);

            // Assert
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void GetExecutionEnvironment_WithNullConfiguration_CreatesEnvironment()
        {
            // Act
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment(null);

            // Assert
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void ChainedConfiguration_WorksCorrectly()
        {
            // Arrange & Act
            var env = FlinkDotNet.DataStream.StreamExecutionEnvironment.GetExecutionEnvironment()
                .SetParallelism(4)
                .SetMaxParallelism(100)
                .SetBufferTimeout(200)
                .DisableOperatorChaining()
                .EnableCheckpointing(10000)
                .EnableAdaptiveScheduler()
                .EnableReactiveMode()
                .FromSavepoint("/tmp/savepoint");

            // Assert
            Assert.That(env.GetParallelism(), Is.EqualTo(4));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(100));
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(200));
            Assert.That(env.IsChainingEnabled(), Is.False);
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(10000));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
            Assert.That(env.GetSavepointPath(), Is.EqualTo("/tmp/savepoint"));
        }

        // Helper class for testing
        private class TestSourceFunction : FlinkDotNet.DataStream.ISourceFunction<string>
        {
            public async IAsyncEnumerable<string> RunAsync([EnumeratorCancellation] System.Threading.CancellationToken cancellationToken = default)
            {
                yield return "test";
                await System.Threading.Tasks.Task.CompletedTask;
            }
        }
    }
}