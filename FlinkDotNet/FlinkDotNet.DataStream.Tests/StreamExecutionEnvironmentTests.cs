using NUnit.Framework;
using FlinkDotNet.DataStream;
using FlinkDotNet.Common;
using FlinkDotNet.DataStream.State;
using FlinkDotNet.DataStream.Checkpoint;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StreamExecutionEnvironmentTests
    {
        #region Environment Creation Tests

        [Test]
        public void GetExecutionEnvironment_WithoutConfiguration_CreatesEnvironment()
        {
            // Act
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env.GetConfig(), Is.Not.Null);
        }

        [Test]
        public void GetExecutionEnvironment_WithConfiguration_CreatesEnvironmentWithConfig()
        {
            // Arrange
            var config = new Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);

            // Assert
            Assert.That(env, Is.Not.Null);
            Assert.That(env.GetConfig(), Is.Not.Null);
            Assert.That(env.GetConfig().GetConfiguration().GetString("test.key"), Is.EqualTo("test.value"));
        }

        [Test]
        public void Configure_AddsConfigurationSettings()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var config = new Configuration();
            config.SetString("additional.key", "additional.value");

            // Act
            env.Configure(config);

            // Assert
            Assert.That(env.GetConfig().GetConfiguration().GetString("additional.key"), Is.EqualTo("additional.value"));
        }

        #endregion

        #region Parallelism Tests

        [Test]
        public void SetParallelism_SetsParallelismCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.SetParallelism(4);

            // Assert
            Assert.That(env.GetParallelism(), Is.EqualTo(4));
        }

        [Test]
        public void SetParallelism_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetParallelism(4);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void GetParallelism_DefaultValue_ReturnsNegativeOne()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var parallelism = env.GetParallelism();

            // Assert - Default parallelism is -1 (unset), which means use system default
            Assert.That(parallelism, Is.EqualTo(-1));
        }

        [Test]
        public void SetMaxParallelism_WithValidValue_SetsMaxParallelism()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.SetMaxParallelism(128);

            // Assert
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
        }

        [Test]
        public void SetMaxParallelism_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
        }

        [Test]
        public void SetMaxParallelism_WithTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
        }

        [Test]
        public void SetMaxParallelism_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        #endregion

        #region Buffer Timeout Tests

        [Test]
        public void SetBufferTimeout_SetsTimeoutCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.SetBufferTimeout(200);

            // Assert
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(200));
        }

        [Test]
        public void SetBufferTimeout_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetBufferTimeout(200);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void GetBufferTimeout_DefaultValue_Returns100()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var timeout = env.GetBufferTimeout();

            // Assert
            Assert.That(timeout, Is.EqualTo(100));
        }

        #endregion

        #region Operator Chaining Tests

        [Test]
        public void IsChainingEnabled_DefaultValue_ReturnsTrue()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var enabled = env.IsChainingEnabled();

            // Assert
            Assert.That(enabled, Is.True);
        }

        [Test]
        public void DisableOperatorChaining_DisablesChainingCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.DisableOperatorChaining();

            // Assert
            Assert.That(env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void DisableOperatorChaining_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.DisableOperatorChaining();

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        #endregion

        #region Checkpointing Tests

        [Test]
        public void EnableCheckpointing_SetsIntervalCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableCheckpointing(5000);

            // Assert
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        }

        [Test]
        public void EnableCheckpointing_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableCheckpointing(5000);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void GetCheckpointInterval_DefaultValue_ReturnsNegativeOne()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var interval = env.GetCheckpointInterval();

            // Assert
            Assert.That(interval, Is.EqualTo(-1));
        }

        [Test]
        public void GetCheckpointConfig_ReturnsCheckpointConfig()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var config = env.GetCheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config, Is.InstanceOf<CheckpointConfig>());
        }

        #endregion

        #region Adaptive Scheduler Tests

        [Test]
        public void EnableAdaptiveScheduler_EnablesSchedulerCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_DefaultValue_EnablesScheduler()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableAdaptiveScheduler();

            // Assert
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_WithFalse_DisablesScheduler()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableAdaptiveScheduler(true);

            // Act
            env.EnableAdaptiveScheduler(false);

            // Assert
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableAdaptiveScheduler_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableAdaptiveScheduler();

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void IsAdaptiveSchedulerEnabled_DefaultValue_ReturnsFalse()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var enabled = env.IsAdaptiveSchedulerEnabled();

            // Assert
            Assert.That(enabled, Is.False);
        }

        #endregion

        #region Reactive Mode Tests

        [Test]
        public void EnableReactiveMode_EnablesModeCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableReactiveMode(true);

            // Assert
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_DefaultValue_EnablesMode()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            env.EnableReactiveMode();

            // Assert
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_WithFalse_DisablesMode()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableReactiveMode(true);

            // Act
            env.EnableReactiveMode(false);

            // Assert
            Assert.That(env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableReactiveMode();

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void IsReactiveModeEnabled_DefaultValue_ReturnsFalse()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var enabled = env.IsReactiveModeEnabled();

            // Assert
            Assert.That(enabled, Is.False);
        }

        #endregion

        #region Savepoint Tests

        [Test]
        public void FromSavepoint_SetsSavepointPathCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var savepointPath = "/path/to/savepoint";

            // Act
            env.FromSavepoint(savepointPath);

            // Assert
            Assert.That(env.GetSavepointPath(), Is.EqualTo(savepointPath));
        }

        [Test]
        public void FromSavepoint_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.FromSavepoint("/path/to/savepoint");

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void GetSavepointPath_DefaultValue_ReturnsNull()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var path = env.GetSavepointPath();

            // Assert
            Assert.That(path, Is.Null);
        }

        #endregion

        #region State Backend Tests

        [Test]
        public void SetStateBackend_WithValidBackend_SetsBackendCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var backend = new HashMapStateBackend();

            // Act
            env.SetStateBackend(backend);

            // Assert
            Assert.That(env.GetStateBackend(), Is.SameAs(backend));
        }

        [Test]
        public void SetStateBackend_WithRocksDB_SetsBackendCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            env.SetStateBackend(backend);

            // Assert
            Assert.That(env.GetStateBackend(), Is.InstanceOf<EmbeddedRocksDBStateBackend>());
        }

        [Test]
        public void SetStateBackend_WithNull_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => env.SetStateBackend(null!));
        }

        [Test]
        public void SetStateBackend_ReturnsEnvironment_ForChaining()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var backend = new HashMapStateBackend();

            // Act
            var result = env.SetStateBackend(backend);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void GetStateBackend_DefaultValue_ReturnsNull()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var backend = env.GetStateBackend();

            // Assert
            Assert.That(backend, Is.Null);
        }

        #endregion

        #region Source Creation Tests

        [Test]
        public void FromKafka_WithValidParameters_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group", "latest");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<string>>());
        }

        [Test]
        public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", null, "test-group"));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", "", "test-group"));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", "   ", "test-group"));
            Assert.That(ex.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void AddKafkaSource_WithDeserializer_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Func<string, int> deserializer = s => int.Parse(s);

            // Act
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", deserializer);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void AddKafkaSource_WithComplexDeserializer_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Func<string, TestMessage> deserializer = s => new TestMessage { Value = s };

            // Act
            var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", deserializer);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<TestMessage>>());
        }

        [Test]
        public void FromCollection_WithValidCollection_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void FromCollection_WithEmptyCollection_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new List<string>();

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithSourceFunction_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction, "Test Source");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.InstanceOf<DataStream<int>>());
        }

        [Test]
        public void AddSource_WithDefaultName_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_CombinesMultipleConfigurations()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var backend = new HashMapStateBackend();

            // Act
            var result = env
                .SetParallelism(4)
                .SetMaxParallelism(128)
                .SetBufferTimeout(200)
                .DisableOperatorChaining()
                .EnableCheckpointing(5000)
                .EnableAdaptiveScheduler()
                .EnableReactiveMode()
                .FromSavepoint("/path/to/savepoint")
                .SetStateBackend(backend);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetParallelism(), Is.EqualTo(4));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(200));
            Assert.That(env.IsChainingEnabled(), Is.False);
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
            Assert.That(env.GetSavepointPath(), Is.EqualTo("/path/to/savepoint"));
            Assert.That(env.GetStateBackend(), Is.SameAs(backend));
        }

        #endregion

        #region Helper Classes

        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        private class TestSourceFunction : ISourceFunction<int>
        {
            public async IAsyncEnumerable<int> RunAsync(CancellationToken cancellationToken = default)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;
                    
                    yield return i;
                    await Task.Delay(10, cancellationToken);
                }
            }
        }

        #endregion
    }
}