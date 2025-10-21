using FlinkDotNet.DataStream;
using FlinkDotNet.Common;
using FlinkDotNet.DataStream.State;
using FlinkDotNet.DataStream.Checkpoint;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StreamExecutionEnvironmentTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void Setup()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        #region Factory and Constructor Tests

        [Test]
        public void GetExecutionEnvironment_WithoutConfiguration_ReturnsEnvironment()
        {
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void GetExecutionEnvironment_WithConfiguration_ReturnsEnvironment()
        {
            var config = new Configuration();
            config.SetString("test.key", "test.value");
            var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);
            Assert.That(env, Is.Not.Null);
        }

        [Test]
        public void GetConfig_ReturnsExecutionConfig()
        {
            var config = _env.GetConfig();
            Assert.That(config, Is.Not.Null);
            Assert.That(config, Is.InstanceOf<ExecutionConfig>());
        }

        #endregion

        #region Parallelism Tests

        [Test]
        public void SetParallelism_ValidValue_SetsParallelism()
        {
            _env.SetParallelism(4);
            Assert.That(_env.GetParallelism(), Is.EqualTo(4));
        }

        [Test]
        public void SetParallelism_ReturnsThis()
        {
            var result = _env.SetParallelism(2);
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void SetParallelism_ChainableCalls()
        {
            var result = _env.SetParallelism(2).SetParallelism(4);
            Assert.That(_env.GetParallelism(), Is.EqualTo(4));
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void GetParallelism_DefaultValue_ReturnsNegativeOne()
        {
            var parallelism = _env.GetParallelism();
            Assert.That(parallelism, Is.EqualTo(-1));
        }

        [Test]
        public void SetMaxParallelism_ValidValue_SetsMaxParallelism()
        {
            _env.SetMaxParallelism(1000);
            Assert.That(_env.GetMaxParallelism(), Is.EqualTo(1000));
        }

        [Test]
        public void SetMaxParallelism_MaximumValue_SetsMaxParallelism()
        {
            _env.SetMaxParallelism(32768);
            Assert.That(_env.GetMaxParallelism(), Is.EqualTo(32768));
        }

        [Test]
        public void SetMaxParallelism_ZeroValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.SetMaxParallelism(0));
        }

        [Test]
        public void SetMaxParallelism_NegativeValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.SetMaxParallelism(-1));
        }

        [Test]
        public void SetMaxParallelism_TooLargeValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.SetMaxParallelism(32769));
        }

        [Test]
        public void SetMaxParallelism_ReturnsThis()
        {
            var result = _env.SetMaxParallelism(100);
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void GetMaxParallelism_DefaultValue_ReturnsNegativeOne()
        {
            var maxParallelism = _env.GetMaxParallelism();
            Assert.That(maxParallelism, Is.EqualTo(-1));
        }

        #endregion

        #region Buffer Timeout Tests

        [Test]
        public void SetBufferTimeout_ValidValue_SetsTimeout()
        {
            _env.SetBufferTimeout(500);
            Assert.That(_env.GetBufferTimeout(), Is.EqualTo(500));
        }

        [Test]
        public void SetBufferTimeout_ReturnsThis()
        {
            var result = _env.SetBufferTimeout(200);
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void GetBufferTimeout_DefaultValue_Returns100()
        {
            Assert.That(_env.GetBufferTimeout(), Is.EqualTo(100));
        }

        [Test]
        public void SetBufferTimeout_ZeroValue_SetsTimeout()
        {
            _env.SetBufferTimeout(0);
            Assert.That(_env.GetBufferTimeout(), Is.EqualTo(0));
        }

        #endregion

        #region Operator Chaining Tests

        [Test]
        public void IsChainingEnabled_Default_ReturnsTrue()
        {
            Assert.That(_env.IsChainingEnabled(), Is.True);
        }

        [Test]
        public void DisableOperatorChaining_DisablesChaining()
        {
            _env.DisableOperatorChaining();
            Assert.That(_env.IsChainingEnabled(), Is.False);
        }

        [Test]
        public void DisableOperatorChaining_ReturnsThis()
        {
            var result = _env.DisableOperatorChaining();
            Assert.That(result, Is.SameAs(_env));
        }

        #endregion

        #region Checkpointing Tests

        [Test]
        public void EnableCheckpointing_SetsInterval()
        {
            _env.EnableCheckpointing(5000);
            Assert.That(_env.GetCheckpointInterval(), Is.EqualTo(5000));
        }

        [Test]
        public void EnableCheckpointing_ReturnsThis()
        {
            var result = _env.EnableCheckpointing(3000);
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void GetCheckpointInterval_Default_ReturnsNegativeOne()
        {
            Assert.That(_env.GetCheckpointInterval(), Is.EqualTo(-1));
        }

        [Test]
        public void GetCheckpointConfig_ReturnsConfig()
        {
            var config = _env.GetCheckpointConfig();
            Assert.That(config, Is.Not.Null);
            Assert.That(config, Is.InstanceOf<CheckpointConfig>());
        }

        #endregion

        #region Adaptive Scheduler Tests

        [Test]
        public void IsAdaptiveSchedulerEnabled_Default_ReturnsFalse()
        {
            Assert.That(_env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableAdaptiveScheduler_EnablesScheduler()
        {
            _env.EnableAdaptiveScheduler(true);
            Assert.That(_env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_DefaultParameter_EnablesScheduler()
        {
            _env.EnableAdaptiveScheduler();
            Assert.That(_env.IsAdaptiveSchedulerEnabled(), Is.True);
        }

        [Test]
        public void EnableAdaptiveScheduler_False_DisablesScheduler()
        {
            _env.EnableAdaptiveScheduler(true);
            _env.EnableAdaptiveScheduler(false);
            Assert.That(_env.IsAdaptiveSchedulerEnabled(), Is.False);
        }

        [Test]
        public void EnableAdaptiveScheduler_ReturnsThis()
        {
            var result = _env.EnableAdaptiveScheduler();
            Assert.That(result, Is.SameAs(_env));
        }

        #endregion

        #region Reactive Mode Tests

        [Test]
        public void IsReactiveModeEnabled_Default_ReturnsFalse()
        {
            Assert.That(_env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_EnablesMode()
        {
            _env.EnableReactiveMode(true);
            Assert.That(_env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_DefaultParameter_EnablesMode()
        {
            _env.EnableReactiveMode();
            Assert.That(_env.IsReactiveModeEnabled(), Is.True);
        }

        [Test]
        public void EnableReactiveMode_False_DisablesMode()
        {
            _env.EnableReactiveMode(true);
            _env.EnableReactiveMode(false);
            Assert.That(_env.IsReactiveModeEnabled(), Is.False);
        }

        [Test]
        public void EnableReactiveMode_ReturnsThis()
        {
            var result = _env.EnableReactiveMode();
            Assert.That(result, Is.SameAs(_env));
        }

        #endregion

        #region Savepoint Tests

        [Test]
        public void GetSavepointPath_Default_ReturnsNull()
        {
            Assert.That(_env.GetSavepointPath(), Is.Null);
        }

        [Test]
        public void FromSavepoint_SetsSavepointPath()
        {
            var path = "/path/to/savepoint";
            _env.FromSavepoint(path);
            Assert.That(_env.GetSavepointPath(), Is.EqualTo(path));
        }

        [Test]
        public void FromSavepoint_ReturnsThis()
        {
            var result = _env.FromSavepoint("/test/path");
            Assert.That(result, Is.SameAs(_env));
        }

        #endregion

        #region State Backend Tests

        [Test]
        public void GetStateBackend_Default_ReturnsNull()
        {
            Assert.That(_env.GetStateBackend(), Is.Null);
        }

        [Test]
        public void SetStateBackend_HashMapStateBackend_SetsBackend()
        {
            var backend = new HashMapStateBackend();
            _env.SetStateBackend(backend);
            Assert.That(_env.GetStateBackend(), Is.SameAs(backend));
        }

        [Test]
        public void SetStateBackend_EmbeddedRocksDBStateBackend_SetsBackend()
        {
            var backend = new EmbeddedRocksDBStateBackend();
            _env.SetStateBackend(backend);
            Assert.That(_env.GetStateBackend(), Is.SameAs(backend));
        }

        [Test]
        public void SetStateBackend_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => _env.SetStateBackend(null!));
        }

        [Test]
        public void SetStateBackend_ReturnsThis()
        {
            var backend = new HashMapStateBackend();
            var result = _env.SetStateBackend(backend);
            Assert.That(result, Is.SameAs(_env));
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Configure_WithConfiguration_ReturnsThis()
        {
            var config = new Configuration();
            config.SetString("custom.key", "custom.value");
            var result = _env.Configure(config);
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void Configure_UpdatesExecutionConfig()
        {
            var config = new Configuration();
            config.SetString("test.property", "test.value");
            _env.Configure(config);
            var execConfig = _env.GetConfig();
            Assert.That(execConfig.GetConfiguration().GetString("test.property", null), Is.EqualTo("test.value"));
        }

        #endregion

        #region Source Tests

        [Test]
        public void FromKafka_WithValidParameters_ReturnsDataStream()
        {
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void FromKafka_NullBootstrapServers_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.FromKafka("test-topic", null, "test-group"));
        }

        [Test]
        public void FromKafka_EmptyBootstrapServers_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.FromKafka("test-topic", "", "test-group"));
        }

        [Test]
        public void FromKafka_WhitespaceBootstrapServers_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => _env.FromKafka("test-topic", "   ", "test-group"));
        }

        [Test]
        public void FromKafka_WithDefaultStartingOffsets_ReturnsDataStream()
        {
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void FromKafka_WithEarliestOffset_ReturnsDataStream()
        {
            var stream = _env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddKafkaSource_WithDeserializer_ReturnsDataStream()
        {
            var stream = _env.AddKafkaSource("test-topic", "localhost:9092", "test-group", 
                (string s) => int.Parse(s));
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void FromCollection_WithValidCollection_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = _env.FromCollection(collection);
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void FromCollection_WithEmptyCollection_ReturnsDataStream()
        {
            var collection = Array.Empty<int>();
            var stream = _env.FromCollection(collection);
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithValidSourceFunction_ReturnsDataStream()
        {
            var source = new TestSourceFunction();
            var stream = _env.AddSource(source);
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithCustomName_ReturnsDataStream()
        {
            var source = new TestSourceFunction();
            var stream = _env.AddSource(source, "Custom Test Source");
            Assert.That(stream, Is.Not.Null);
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_AllConfigurations_WorksTogether()
        {
            var result = _env
                .SetParallelism(4)
                .SetMaxParallelism(128)
                .SetBufferTimeout(200)
                .DisableOperatorChaining()
                .EnableCheckpointing(5000)
                .EnableAdaptiveScheduler()
                .EnableReactiveMode()
                .FromSavepoint("/test/savepoint")
                .SetStateBackend(new HashMapStateBackend());

            Assert.That(result, Is.SameAs(_env));
            Assert.That(_env.GetParallelism(), Is.EqualTo(4));
            Assert.That(_env.GetMaxParallelism(), Is.EqualTo(128));
            Assert.That(_env.GetBufferTimeout(), Is.EqualTo(200));
            Assert.That(_env.IsChainingEnabled(), Is.False);
            Assert.That(_env.GetCheckpointInterval(), Is.EqualTo(5000));
            Assert.That(_env.IsAdaptiveSchedulerEnabled(), Is.True);
            Assert.That(_env.IsReactiveModeEnabled(), Is.True);
            Assert.That(_env.GetSavepointPath(), Is.EqualTo("/test/savepoint"));
            Assert.That(_env.GetStateBackend(), Is.Not.Null);
        }

        #endregion

        // Test helper class
        private class TestSourceFunction : ISourceFunction<int>
        {
            public async IAsyncEnumerable<int> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                for (int i = 0; i < 5; i++)
                {
                    yield return i;
                    await Task.Delay(10, cancellationToken);
                }
            }
        }
    }
}