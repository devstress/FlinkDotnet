using System;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class PerformanceConfigurationTests
    {
        #region StateBackendConfiguration Tests

        [Test]
        public void StateBackendConfiguration_Builder_ReturnsNewBuilder()
        {
            // Act
            var builder = StateBackendConfiguration.Builder();

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.InstanceOf<StateBackendConfiguration.StateBackendConfigurationBuilder>());
        }

        [Test]
        public void StateBackendConfiguration_Build_WithDefaults_Succeeds()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var config = builder.Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Backend, Is.EqualTo(StateBackendType.HashMapStateBackend));
            Assert.That(config.CheckpointStorageUri, Is.Null);
            Assert.That(config.RocksDBOptions, Is.Null);
            Assert.That(config.IncrementalCheckpoints, Is.False);
            Assert.That(config.Properties, Is.Not.Null);
            Assert.That(config.Properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void StateBackendConfiguration_SetBackend_HashMapStateBackend_SetsBackend()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var result = builder.SetBackend(StateBackendType.HashMapStateBackend);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Backend, Is.EqualTo(StateBackendType.HashMapStateBackend));
        }

        [Test]
        public void StateBackendConfiguration_SetBackend_EmbeddedRocksDB_SetsBackend()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var result = builder.SetBackend(StateBackendType.EmbeddedRocksDBStateBackend);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Backend, Is.EqualTo(StateBackendType.EmbeddedRocksDBStateBackend));
        }

        [Test]
        public void StateBackendConfiguration_SetCheckpointStorageUri_FileUri_SetsUri()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();
            var uri = "file:///tmp/checkpoints";

            // Act
            var result = builder.SetCheckpointStorageUri(uri);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.CheckpointStorageUri, Is.EqualTo(uri));
        }

        [Test]
        public void StateBackendConfiguration_SetCheckpointStorageUri_S3Uri_SetsUri()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();
            var uri = "s3://my-bucket/checkpoints";

            // Act
            var result = builder.SetCheckpointStorageUri(uri);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.CheckpointStorageUri, Is.EqualTo(uri));
        }

        [Test]
        public void StateBackendConfiguration_SetRocksDBOptions_SetsOptions()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();
            var options = new RocksDBOptions
            {
                MaxBackgroundJobs = 4,
                WriteBufferSize = 64 * 1024 * 1024
            };

            // Act
            var result = builder.SetRocksDBOptions(options);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.RocksDBOptions, Is.SameAs(options));
            Assert.That(config.RocksDBOptions!.MaxBackgroundJobs, Is.EqualTo(4));
        }

        [Test]
        public void StateBackendConfiguration_SetIncrementalCheckpoints_True_EnablesCheckpoints()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var result = builder.SetIncrementalCheckpoints(true);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.IncrementalCheckpoints, Is.True);
        }

        [Test]
        public void StateBackendConfiguration_SetIncrementalCheckpoints_False_DisablesCheckpoints()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var result = builder.SetIncrementalCheckpoints(false);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.IncrementalCheckpoints, Is.False);
        }

        [Test]
        public void StateBackendConfiguration_AddProperty_AddsProperty()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            var result = builder.AddProperty("custom.key", "custom.value");
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Properties, Contains.Key("custom.key"));
            Assert.That(config.Properties["custom.key"], Is.EqualTo("custom.value"));
        }

        [Test]
        public void StateBackendConfiguration_AddProperty_MultipleProperties_AddsAll()
        {
            // Arrange
            var builder = StateBackendConfiguration.Builder();

            // Act
            builder.AddProperty("key1", "value1");
            builder.AddProperty("key2", "value2");
            var config = builder.Build();

            // Assert
            Assert.That(config.Properties.Count, Is.EqualTo(2));
            Assert.That(config.Properties["key1"], Is.EqualTo("value1"));
            Assert.That(config.Properties["key2"], Is.EqualTo("value2"));
        }

        [Test]
        public void StateBackendConfiguration_FluentAPI_ChainsCorrectly()
        {
            // Arrange & Act
            var config = StateBackendConfiguration.Builder()
                .SetBackend(StateBackendType.EmbeddedRocksDBStateBackend)
                .SetCheckpointStorageUri("s3://bucket/checkpoints")
                .SetIncrementalCheckpoints(true)
                .AddProperty("test", "value")
                .Build();

            // Assert
            Assert.That(config.Backend, Is.EqualTo(StateBackendType.EmbeddedRocksDBStateBackend));
            Assert.That(config.CheckpointStorageUri, Is.EqualTo("s3://bucket/checkpoints"));
            Assert.That(config.IncrementalCheckpoints, Is.True);
            Assert.That(config.Properties["test"], Is.EqualTo("value"));
        }

        #endregion

        #region RocksDBOptions Tests

        [Test]
        public void RocksDBOptions_InitializerSyntax_SetsProperties()
        {
            // Act
            var options = new RocksDBOptions
            {
                MaxBackgroundJobs = 8,
                MaxWriteBufferNumber = 4,
                WriteBufferSize = 128 * 1024 * 1024,
                BlockCacheSize = 256 * 1024 * 1024,
                UseBloomFilter = true,
                CompactionStyle = "level"
            };

            // Assert
            Assert.That(options.MaxBackgroundJobs, Is.EqualTo(8));
            Assert.That(options.MaxWriteBufferNumber, Is.EqualTo(4));
            Assert.That(options.WriteBufferSize, Is.EqualTo(128 * 1024 * 1024));
            Assert.That(options.BlockCacheSize, Is.EqualTo(256 * 1024 * 1024));
            Assert.That(options.UseBloomFilter, Is.True);
            Assert.That(options.CompactionStyle, Is.EqualTo("level"));
        }

        [Test]
        public void RocksDBOptions_Properties_DefaultsToEmptyDictionary()
        {
            // Act
            var options = new RocksDBOptions();

            // Assert
            Assert.That(options.Properties, Is.Not.Null);
            Assert.That(options.Properties.Count, Is.EqualTo(0));
        }

        #endregion

        #region SmileFormatConfiguration Tests

        [Test]
        public void SmileFormatConfiguration_Builder_ReturnsNewBuilder()
        {
            // Act
            var builder = SmileFormatConfiguration.Builder();

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.InstanceOf<SmileFormatConfiguration.SmileFormatConfigurationBuilder>());
        }

        [Test]
        public void SmileFormatConfiguration_Build_WithDefaults_Succeeds()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var config = builder.Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Enabled, Is.True);
            Assert.That(config.CompressionLevel, Is.EqualTo(6));
            Assert.That(config.UseSharedStringValues, Is.True);
            Assert.That(config.Properties, Is.Not.Null);
            Assert.That(config.Properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void SmileFormatConfiguration_SetEnabled_True_EnablesFormat()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.SetEnabled(true);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Enabled, Is.True);
        }

        [Test]
        public void SmileFormatConfiguration_SetEnabled_False_DisablesFormat()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.SetEnabled(false);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Enabled, Is.False);
        }

        [Test]
        public void SmileFormatConfiguration_SetCompressionLevel_ValidLevel_SetsLevel()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.SetCompressionLevel(3);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.CompressionLevel, Is.EqualTo(3));
        }

        [Test]
        public void SmileFormatConfiguration_SetCompressionLevel_Zero_SetsLevel()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var config = builder.SetCompressionLevel(0).Build();

            // Assert
            Assert.That(config.CompressionLevel, Is.EqualTo(0));
        }

        [Test]
        public void SmileFormatConfiguration_SetCompressionLevel_Nine_SetsLevel()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var config = builder.SetCompressionLevel(9).Build();

            // Assert
            Assert.That(config.CompressionLevel, Is.EqualTo(9));
        }

        [Test]
        public void SmileFormatConfiguration_SetCompressionLevel_NegativeLevel_ThrowsException()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCompressionLevel(-1));
            Assert.That(ex!.ParamName, Is.EqualTo("level"));
            Assert.That(ex.Message, Does.Contain("Compression level must be between 0 and 9"));
        }

        [Test]
        public void SmileFormatConfiguration_SetCompressionLevel_LevelTooHigh_ThrowsException()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCompressionLevel(10));
            Assert.That(ex!.ParamName, Is.EqualTo("level"));
            Assert.That(ex.Message, Does.Contain("Compression level must be between 0 and 9"));
        }

        [Test]
        public void SmileFormatConfiguration_SetUseSharedStringValues_True_EnablesSharing()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.SetUseSharedStringValues(true);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.UseSharedStringValues, Is.True);
        }

        [Test]
        public void SmileFormatConfiguration_SetUseSharedStringValues_False_DisablesSharing()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.SetUseSharedStringValues(false);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.UseSharedStringValues, Is.False);
        }

        [Test]
        public void SmileFormatConfiguration_AddProperty_AddsProperty()
        {
            // Arrange
            var builder = SmileFormatConfiguration.Builder();

            // Act
            var result = builder.AddProperty("custom.key", "custom.value");
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Properties, Contains.Key("custom.key"));
            Assert.That(config.Properties["custom.key"], Is.EqualTo("custom.value"));
        }

        [Test]
        public void SmileFormatConfiguration_FluentAPI_ChainsCorrectly()
        {
            // Arrange & Act
            var config = SmileFormatConfiguration.Builder()
                .SetEnabled(true)
                .SetCompressionLevel(8)
                .SetUseSharedStringValues(false)
                .AddProperty("test", "value")
                .Build();

            // Assert
            Assert.That(config.Enabled, Is.True);
            Assert.That(config.CompressionLevel, Is.EqualTo(8));
            Assert.That(config.UseSharedStringValues, Is.False);
            Assert.That(config.Properties["test"], Is.EqualTo("value"));
        }

        #endregion

        #region MultiJoinOptimizationConfiguration Tests

        [Test]
        public void MultiJoinOptimizationConfiguration_Builder_ReturnsNewBuilder()
        {
            // Act
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.InstanceOf<MultiJoinOptimizationConfiguration.MultiJoinOptimizationConfigurationBuilder>());
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_Build_WithDefaults_Succeeds()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var config = builder.Build();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.Enabled, Is.True);
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.LeftDeep));
            Assert.That(config.MaxJoinsToOptimize, Is.EqualTo(10));
            Assert.That(config.UseCostBasedOptimization, Is.True);
            Assert.That(config.Properties, Is.Not.Null);
            Assert.That(config.Properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetEnabled_True_EnablesOptimization()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetEnabled(true);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Enabled, Is.True);
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetEnabled_False_DisablesOptimization()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetEnabled(false);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Enabled, Is.False);
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetReorderStrategy_None_SetsStrategy()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetReorderStrategy(JoinReorderStrategy.None);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.None));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetReorderStrategy_LeftDeep_SetsStrategy()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var config = builder.SetReorderStrategy(JoinReorderStrategy.LeftDeep).Build();

            // Assert
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.LeftDeep));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetReorderStrategy_Bushy_SetsStrategy()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var config = builder.SetReorderStrategy(JoinReorderStrategy.Bushy).Build();

            // Assert
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.Bushy));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetReorderStrategy_DynamicProgramming_SetsStrategy()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var config = builder.SetReorderStrategy(JoinReorderStrategy.DynamicProgramming).Build();

            // Assert
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.DynamicProgramming));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetMaxJoinsToOptimize_ValidValue_SetsValue()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetMaxJoinsToOptimize(5);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.MaxJoinsToOptimize, Is.EqualTo(5));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetMaxJoinsToOptimize_MinimumValue_SetsValue()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var config = builder.SetMaxJoinsToOptimize(2).Build();

            // Assert
            Assert.That(config.MaxJoinsToOptimize, Is.EqualTo(2));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetMaxJoinsToOptimize_LessThanTwo_ThrowsException()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetMaxJoinsToOptimize(1));
            Assert.That(ex!.ParamName, Is.EqualTo("max"));
            Assert.That(ex.Message, Does.Contain("Max joins must be at least 2"));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetMaxJoinsToOptimize_Zero_ThrowsException()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act & Assert
            var ex = Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetMaxJoinsToOptimize(0));
            Assert.That(ex!.ParamName, Is.EqualTo("max"));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetUseCostBasedOptimization_True_EnablesCostBased()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetUseCostBasedOptimization(true);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.UseCostBasedOptimization, Is.True);
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_SetUseCostBasedOptimization_False_DisablesCostBased()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.SetUseCostBasedOptimization(false);
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.UseCostBasedOptimization, Is.False);
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_AddProperty_AddsProperty()
        {
            // Arrange
            var builder = MultiJoinOptimizationConfiguration.Builder();

            // Act
            var result = builder.AddProperty("custom.key", "custom.value");
            var config = result.Build();

            // Assert
            Assert.That(result, Is.SameAs(builder));
            Assert.That(config.Properties, Contains.Key("custom.key"));
            Assert.That(config.Properties["custom.key"], Is.EqualTo("custom.value"));
        }

        [Test]
        public void MultiJoinOptimizationConfiguration_FluentAPI_ChainsCorrectly()
        {
            // Arrange & Act
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetEnabled(false)
                .SetReorderStrategy(JoinReorderStrategy.Bushy)
                .SetMaxJoinsToOptimize(15)
                .SetUseCostBasedOptimization(false)
                .AddProperty("test", "value")
                .Build();

            // Assert
            Assert.That(config.Enabled, Is.False);
            Assert.That(config.ReorderStrategy, Is.EqualTo(JoinReorderStrategy.Bushy));
            Assert.That(config.MaxJoinsToOptimize, Is.EqualTo(15));
            Assert.That(config.UseCostBasedOptimization, Is.False);
            Assert.That(config.Properties["test"], Is.EqualTo("value"));
        }

        #endregion
    }
}
