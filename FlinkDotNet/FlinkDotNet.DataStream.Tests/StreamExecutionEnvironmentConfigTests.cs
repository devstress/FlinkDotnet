using System;
using System.Threading;
using System.Threading.Tasks;
using FlinkDotNet.Common;
using FlinkDotNet.DataStream.State;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StreamExecutionEnvironmentConfigTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            // Set environment variable required by FlinkJobGatewayConfiguration
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");

            this._env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        [TearDown]
        public void TearDown() =>
            // Clean up environment variable
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);

        #region Parallelism FlinkConfiguration Tests

        [Test]
        public void SetParallelism_WithValidValue_SetsParallelism()
        {
            // Act
            var result = this._env.SetParallelism(4);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetParallelism(), Is.EqualTo(4));
        }

        [Test]
        public void GetParallelism_ReturnsDefaultValue()
        {
            // Act
            var parallelism = this._env.GetParallelism();

            // Assert - Default is -1 (not set)
            Assert.That(parallelism, Is.EqualTo(-1).Or.GreaterThan(0));
        }

        [Test]
        public void SetMaxParallelism_WithValidValue_SetsMaxParallelism()
        {
            // Act
            var result = this._env.SetMaxParallelism(128);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetMaxParallelism(), Is.EqualTo(128));
        }

        [Test]
        public void SetMaxParallelism_WithBoundaryValue1_Works()
        {
            // Act
            var result = this._env.SetMaxParallelism(1);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetMaxParallelism(), Is.EqualTo(1));
        }

        [Test]
        public void SetMaxParallelism_WithBoundaryValue32768_Works()
        {
            // Act
            var result = this._env.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetMaxParallelism(), Is.EqualTo(32768));
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => this._env.SetMaxParallelism(0));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithNegativeValue_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => this._env.SetMaxParallelism(-5));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithValueTooLarge_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => this._env.SetMaxParallelism(32769));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        #endregion

        #region Buffer Timeout Tests

        [Test]
        public void SetBufferTimeout_WithValidValue_SetsTimeout()
        {
            // Act
            var result = this._env.SetBufferTimeout(200);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetBufferTimeout(), Is.EqualTo(200));
        }

        [Test]
        public void GetBufferTimeout_ReturnsDefaultValue()
        {
            // Act
            var timeout = this._env.GetBufferTimeout();

            // Assert
            Assert.That(timeout, Is.EqualTo(100)); // Default is 100ms
        }

        [Test]
        public void SetBufferTimeout_WithZero_Works()
        {
            // Act
            var result = this._env.SetBufferTimeout(0);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetBufferTimeout(), Is.EqualTo(0));
        }

        #endregion

        #region Operator Chaining Tests

        [Test]
        public void DisableOperatorChaining_DisablesChaining()
        {
            // Act
            var result = this._env.DisableOperatorChaining();

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void IsChainingEnabled_ReturnsTrueByDefault()
        {
            // Act
            var isEnabled = this._env.IsChainingEnabled();

            // Assert
            Assert.That(isEnabled, Is.True);
        }

        #endregion

        #region Checkpointing Tests

        [Test]
        public void EnableCheckpointing_WithValidInterval_EnablesCheckpointing()
        {
            // Act
            var result = this._env.EnableCheckpointing(1000);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetCheckpointInterval(), Is.EqualTo(1000));
        }

        [Test]
        public void GetCheckpointInterval_ReturnsNegativeOneWhenDisabled()
        {
            // Act
            var interval = this._env.GetCheckpointInterval();

            // Assert
            Assert.That(interval, Is.EqualTo(-1));
        }

        [Test]
        public void GetCheckpointConfig_ReturnsNonNullConfig()
        {
            // Act
            var config = this._env.GetCheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
        }

        #endregion

        #region Adaptive Scheduler Tests

        [Test]
        public void EnableAdaptiveScheduler_WithTrue_EnablesScheduler()
        {
            // Act
            var result = this._env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_WithFalse_DisablesScheduler()
        {
            // Arrange
            _ = this._env.EnableAdaptiveScheduler(true);

            // Act
            var result = this._env.EnableAdaptiveScheduler(false);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableAdaptiveScheduler_WithoutParameter_EnablesScheduler()
        {
            // Act
            var result = this._env.EnableAdaptiveScheduler();

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void IsAdaptiveSchedulerEnabled_ReturnsFalseByDefault()
        {
            // Act
            var isEnabled = this._env.IsAdaptiveSchedulerEnabled();

            // Assert
            Assert.That(isEnabled, Is.False);
        }

        #endregion

        #region Reactive Mode Tests

        [Test]
        public void EnableReactiveMode_WithTrue_EnablesReactiveMode()
        {
            // Act
            var result = this._env.EnableReactiveMode(true);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_WithFalse_DisablesReactiveMode()
        {
            // Arrange
            _ = this._env.EnableReactiveMode(true);

            // Act
            var result = this._env.EnableReactiveMode(false);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_WithoutParameter_EnablesReactiveMode()
        {
            // Act
            var result = this._env.EnableReactiveMode();

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void IsReactiveModeEnabled_ReturnsFalseByDefault()
        {
            // Act
            var isEnabled = this._env.IsReactiveModeEnabled();

            // Assert
            Assert.That(isEnabled, Is.False);
        }

        #endregion

        #region Savepoint Tests

        [Test]
        public void FromSavepoint_WithValidPath_SetsSavepointPath()
        {
            // Act
            var result = this._env.FromSavepoint("/path/to/savepoint");

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetSavepointPath(), Is.EqualTo("/path/to/savepoint"));
        }

        [Test]
        public void GetSavepointPath_ReturnsNullWhenNotSet()
        {
            // Act
            var path = this._env.GetSavepointPath();

            // Assert
            Assert.That(path, Is.Null);
        }

        #endregion

        #region State Backend Tests

        [Test]
        public void SetStateBackend_WithHashMapBackend_SetsBackend()
        {
            // Arrange
            var backend = new HashMapStateBackend();

            // Act
            var result = this._env.SetStateBackend(backend);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetStateBackend(), Is.SameAs(backend));
        }

        [Test]
        public void SetStateBackend_WithRocksDBBackend_SetsBackend()
        {
            // Arrange
            var backend = new EmbeddedRocksDBStateBackend();

            // Act
            var result = this._env.SetStateBackend(backend);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
            Assert.That(this._env.GetStateBackend(), Is.SameAs(backend));
        }

        [Test]
        public void SetStateBackend_WithNull_ThrowsArgumentNullException() =>
            // Act & Assert
            _ = Assert.Throws<ArgumentNullException>(() => this._env.SetStateBackend(null!));

        [Test]
        public void GetStateBackend_ReturnsNullWhenNotSet()
        {
            // Act
            var backend = this._env.GetStateBackend();

            // Assert
            Assert.That(backend, Is.Null);
        }

        #endregion

        #region FlinkConfiguration Tests

        [Test]
        public void Configure_WithConfiguration_MergesConfiguration()
        {
            // Arrange
            var config = new FlinkConfiguration();
            _ = config.SetString("test.key", "test.value");

            // Act
            var result = this._env.Configure(config);

            // Assert
            Assert.That(result, Is.SameAs(this._env));
        }

        [Test]
        public void GetConfig_ReturnsNonNullConfig()
        {
            // Act
            var config = this._env.GetConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
        }

        #endregion

        #region ExecuteAsyncJob Error Tests

        [Test]
        public void ExecuteAsyncJob_WithoutActiveJob_ThrowsInvalidOperationException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await this._env.ExecuteAsyncJob("test-job"));

            Assert.That(ex!.Message, Does.Contain("No Flink-compatible job is defined"));
        }

        [Test]
        public void ExecuteAsyncJob_WithActiveJob_ReturnsJobClient()
        {
            // Arrange
            var stream = this._env.FromKafka("test-topic", "localhost:9092");
            _ = stream.SinkToKafka("output-topic", "localhost:9092");

            // Act
            var jobClientTask = this._env.ExecuteAsyncJob("test-job");

            // Assert
            Assert.That(jobClientTask, Is.Not.Null);
            Assert.That(jobClientTask.IsCompleted, Is.True);
        }

        #endregion

        #region Source Creation Tests

        [Test]
        public void FromCollection_WithValidCollection_CreatesDataStream()
        {
            // Arrange
            var collection = new[] { 1, 2, 3, 4, 5 };

            // Act
            var stream = this._env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithSourceFunction_CreatesDataStream()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = this._env.AddSource(sourceFunction, "Test Source");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithDefaultName_CreatesDataStream()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = this._env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        #region Kafka Source Error Tests

        [Test]
        public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                this._env.FromKafka("test-topic", null));

            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                this._env.FromKafka("test-topic", ""));

            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() =>
                this._env.FromKafka("test-topic", "   "));

            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        #endregion

        #region Helper Classes

        private class TestSourceFunction : ISourceFunction<int>
        {
            public async System.Collections.Generic.IAsyncEnumerable<int> RunAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                for (int i = 0; i < 5 && !cancellationToken.IsCancellationRequested; i++)
                {
                    yield return i;
                    await Task.Delay(10, cancellationToken);
                }
            }
        }

        #endregion
    }
}
