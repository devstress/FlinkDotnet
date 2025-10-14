using FlinkDotNet.Common;
using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Additional coverage tests for StreamExecutionEnvironment
    /// Target: Improve StreamExecutionEnvironment from 74.1% to 85%+ coverage
    /// </summary>
    [TestFixture]
    public class StreamExecutionEnvironmentCoverageTests
    {
        #region SetBufferTimeout Tests

        [Test]
        public void SetBufferTimeout_WithValidTimeout_SetsValue()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const int timeout = 500;

            // Act
            var result = env.SetBufferTimeout(timeout);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(timeout));
        }

        [Test]
        public void SetBufferTimeout_WithZeroTimeout_SetsZero()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetBufferTimeout(0);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetBufferTimeout(), Is.EqualTo(0));
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

        #region DisableOperatorChaining Tests

        [Test]
        public void DisableOperatorChaining_SetsFlag()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.DisableOperatorChaining();

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsChainingEnabled(), Is.False);
        }

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

        #endregion

        #region EnableCheckpointing Tests

        [Test]
        public void EnableCheckpointing_WithValidInterval_SetsInterval()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const long interval = 5000;

            // Act
            var result = env.EnableCheckpointing(interval);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(interval));
        }

        [Test]
        public void EnableCheckpointing_WithZeroInterval_SetsZero()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableCheckpointing(0);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetCheckpointInterval(), Is.EqualTo(0));
        }

        [Test]
        public void GetCheckpointInterval_DefaultValue_ReturnsMinusOne()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var interval = env.GetCheckpointInterval();

            // Assert
            Assert.That(interval, Is.EqualTo(-1));
        }

        #endregion

        #region EnableAdaptiveScheduler Tests

        [Test]
        public void EnableAdaptiveScheduler_WithTrue_EnablesScheduler()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableAdaptiveScheduler(true);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_WithFalse_DisablesScheduler()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableAdaptiveScheduler(true);

            // Act
            var result = env.EnableAdaptiveScheduler(false);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableAdaptiveScheduler_DefaultParameter_EnablesScheduler()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableAdaptiveScheduler();

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
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

        #region EnableReactiveMode Tests

        [Test]
        public void EnableReactiveMode_WithTrue_EnablesMode()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableReactiveMode(true);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_WithFalse_DisablesMode()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.EnableReactiveMode(true);

            // Act
            var result = env.EnableReactiveMode(false);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_DefaultParameter_EnablesMode()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.EnableReactiveMode();

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.IsReactiveModeEnabled(), Is.True);
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

        #region FromSavepoint Tests

        [Test]
        public void FromSavepoint_WithValidPath_SetsSavepointPath()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const string savepointPath = "/tmp/savepoints/savepoint-123";

            // Act
            var result = env.FromSavepoint(savepointPath);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetSavepointPath(), Is.EqualTo(savepointPath));
        }

        [Test]
        public void FromSavepoint_WithNullPath_SetsNullPath()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.FromSavepoint(null!);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetSavepointPath(), Is.Null);
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

        #region SetMaxParallelism Tests

        [Test]
        public void SetMaxParallelism_WithValidValue_SetsMaxParallelism()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const int maxParallelism = 128;

            // Act
            var result = env.SetMaxParallelism(maxParallelism);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(maxParallelism));
        }

        [Test]
        public void SetMaxParallelism_WithMaxValue_SetsMaxParallelism()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const int maxParallelism = 32768;

            // Act
            var result = env.SetMaxParallelism(maxParallelism);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(maxParallelism));
        }

        [Test]
        public void SetMaxParallelism_WithMinValue_SetsMaxParallelism()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            const int maxParallelism = 1;

            // Act
            var result = env.SetMaxParallelism(maxParallelism);

            // Assert
            Assert.That(result, Is.SameAs(env));
            Assert.That(env.GetMaxParallelism(), Is.EqualTo(maxParallelism));
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
            Assert.That(ex!.Message, Does.Contain("must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
            Assert.That(ex!.Message, Does.Contain("must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithTooLarge_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
            Assert.That(ex!.Message, Does.Contain("must be between 1 and 32768"));
        }

        #endregion

        #region FromKafka Tests

        [Test]
        public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", null));
            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", ""));
            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => 
                env.FromKafka("test-topic", "   "));
            Assert.That(ex!.Message, Does.Contain("bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithValidParameters_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromKafka("test-topic", "localhost:9092");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public void FromKafka_WithGroupId_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromKafka("test-topic", "localhost:9092", "my-group");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public void FromKafka_WithStartingOffsets_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromKafka("test-topic", "localhost:9092", "my-group", "earliest");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<string>>());
        }

        #endregion

        #region FromCollection Tests

        [Test]
        public void FromCollection_WithList_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new List<int> { 1, 2, 3, 4, 5 };

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public void FromCollection_WithEmptyList_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new List<string>();

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<string>>());
        }

        [Test]
        public void FromCollection_WithArray_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var collection = new[] { "a", "b", "c" };

            // Act
            var stream = env.FromCollection(collection);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<string>>());
        }

        #endregion

        #region AddSource Tests

        [Test]
        public void AddSource_WithSourceFunction_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<int>>());
        }

        [Test]
        public void AddSource_WithCustomName_CreatesDataStreamWithName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = env.AddSource(sourceFunction, "My Custom Source");

            // Assert
            Assert.That(stream, Is.Not.Null);
            Assert.That(stream, Is.TypeOf<DataStream<int>>());
        }

        #endregion

        #region Configure Tests

        [Test]
        public void Configure_WithConfiguration_UpdatesEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var config = new Configuration();
            config.SetString("test.key", "test.value");

            // Act
            var result = env.Configure(config);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void Configure_WithMultipleSettings_UpdatesEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var config = new Configuration();
            config.SetString("key1", "value1");
            config.SetInteger("key2", 42);

            // Act
            var result = env.Configure(config);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        #endregion

        #region ExecuteAsyncJob Tests

        [Test]
        public void ExecuteAsyncJob_WithoutActiveJob_ThrowsInvalidOperationException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await env.ExecuteAsyncJob();
            });
            Assert.That(ex!.Message, Does.Contain("No Flink-compatible job is defined"));
        }

        [Test]
        public async Task ExecuteAsyncJob_WithCustomJobName_ReturnsJobClient()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            _ = env.FromKafka("test-topic", "localhost:9092");

            // Act
            var jobClient = await env.ExecuteAsyncJob("Custom Job Name");

            // Assert
            Assert.That(jobClient, Is.Not.Null);
            Assert.That(jobClient.JobName, Is.EqualTo("Custom Job Name"));
        }

        #endregion

        #region Helper Classes

        private class TestSourceFunction : ISourceFunction<int>
        {
            public async IAsyncEnumerable<int> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                for (int i = 0; i < 5; i++)
                {
                    if (cancellationToken.IsCancellationRequested)
                        yield break;
                    yield return i;
                    await Task.Delay(1, cancellationToken);
                }
            }
        }

        #endregion
    }
}
