using FlinkDotNet.DataStream;
using Xunit;

namespace FlinkDotNet.DataStream.Tests
{
    public class PerformanceConfigurationTests
    {
        #region StateBackendConfiguration Tests

        [Fact]
        public void StateBackend_Builder_SetsBackendType()
        {
            var config = StateBackendConfiguration.Builder()
                .SetBackend(StateBackendType.EmbeddedRocksDBStateBackend)
                .Build();

            Assert.Equal(StateBackendType.EmbeddedRocksDBStateBackend, config.Backend);
        }

        [Fact]
        public void StateBackend_Builder_SetsCheckpointStorageUri()
        {
            var config = StateBackendConfiguration.Builder()
                .SetCheckpointStorageUri("s3://bucket/checkpoints")
                .Build();

            Assert.Equal("s3://bucket/checkpoints", config.CheckpointStorageUri);
        }

        [Fact]
        public void StateBackend_Builder_SetsIncrementalCheckpoints()
        {
            var config = StateBackendConfiguration.Builder()
                .SetIncrementalCheckpoints(true)
                .Build();

            Assert.True(config.IncrementalCheckpoints);
        }

        [Fact]
        public void StateBackend_Builder_SetsRocksDBOptions()
        {
            var rocksDBOptions = new RocksDBOptions
            {
                MaxBackgroundJobs = 4,
                BlockCacheSize = 64 * 1024 * 1024
            };

            var config = StateBackendConfiguration.Builder()
                .SetRocksDBOptions(rocksDBOptions)
                .Build();

            Assert.NotNull(config.RocksDBOptions);
            Assert.Equal(4, config.RocksDBOptions.MaxBackgroundJobs);
            Assert.Equal(64 * 1024 * 1024, config.RocksDBOptions.BlockCacheSize);
        }

        [Fact]
        public void StateBackend_Builder_AddsCustomProperty()
        {
            var config = StateBackendConfiguration.Builder()
                .AddProperty("custom.key", "custom.value")
                .Build();

            Assert.Single(config.Properties);
            Assert.Equal("custom.value", config.Properties["custom.key"]);
        }

        [Fact]
        public void StateBackend_Builder_DefaultsToHashMapBackend()
        {
            var config = StateBackendConfiguration.Builder().Build();

            Assert.Equal(StateBackendType.HashMapStateBackend, config.Backend);
        }

        [Fact]
        public void StateBackend_Builder_DefaultsToNoCheckpointUri()
        {
            var config = StateBackendConfiguration.Builder().Build();

            Assert.Null(config.CheckpointStorageUri);
        }

        [Fact]
        public void StateBackend_Builder_DefaultsToNoIncrementalCheckpoints()
        {
            var config = StateBackendConfiguration.Builder().Build();

            Assert.False(config.IncrementalCheckpoints);
        }

        [Fact]
        public void StateBackend_Builder_SupportsChaining()
        {
            var config = StateBackendConfiguration.Builder()
                .SetBackend(StateBackendType.EmbeddedRocksDBStateBackend)
                .SetCheckpointStorageUri("file:///checkpoints")
                .SetIncrementalCheckpoints(true)
                .AddProperty("prop1", "value1")
                .AddProperty("prop2", "value2")
                .Build();

            Assert.Equal(StateBackendType.EmbeddedRocksDBStateBackend, config.Backend);
            Assert.Equal("file:///checkpoints", config.CheckpointStorageUri);
            Assert.True(config.IncrementalCheckpoints);
            Assert.Equal(2, config.Properties.Count);
        }

        [Fact]
        public void StateBackendType_Enum_HasExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(StateBackendType), StateBackendType.HashMapStateBackend));
            Assert.True(Enum.IsDefined(typeof(StateBackendType), StateBackendType.EmbeddedRocksDBStateBackend));
        }

        #endregion

        #region SmileFormatConfiguration Tests

        [Fact]
        public void SmileFormat_Builder_SetsEnabled()
        {
            var config = SmileFormatConfiguration.Builder()
                .SetEnabled(true)
                .Build();

            Assert.True(config.Enabled);
        }

        [Fact]
        public void SmileFormat_Builder_SetsDisabled()
        {
            var config = SmileFormatConfiguration.Builder()
                .SetEnabled(false)
                .Build();

            Assert.False(config.Enabled);
        }

        [Fact]
        public void SmileFormat_Builder_SetsCompressionLevel()
        {
            var config = SmileFormatConfiguration.Builder()
                .SetCompressionLevel(9)
                .Build();

            Assert.Equal(9, config.CompressionLevel);
        }

        [Fact]
        public void SmileFormat_Builder_ThrowsOnInvalidCompressionLevel()
        {
            var builder = SmileFormatConfiguration.Builder();

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCompressionLevel(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetCompressionLevel(10));
        }

        [Fact]
        public void SmileFormat_Builder_SetsUseSharedStringValues()
        {
            var config = SmileFormatConfiguration.Builder()
                .SetUseSharedStringValues(false)
                .Build();

            Assert.False(config.UseSharedStringValues);
        }

        [Fact]
        public void SmileFormat_Builder_AddsCustomProperty()
        {
            var config = SmileFormatConfiguration.Builder()
                .AddProperty("encoding", "utf8")
                .Build();

            Assert.Single(config.Properties);
            Assert.Equal("utf8", config.Properties["encoding"]);
        }

        [Fact]
        public void SmileFormat_Builder_DefaultsToEnabled()
        {
            var config = SmileFormatConfiguration.Builder().Build();

            Assert.True(config.Enabled);
        }

        [Fact]
        public void SmileFormat_Builder_DefaultsToCompressionLevel6()
        {
            var config = SmileFormatConfiguration.Builder().Build();

            Assert.Equal(6, config.CompressionLevel);
        }

        [Fact]
        public void SmileFormat_Builder_DefaultsToSharedStringValues()
        {
            var config = SmileFormatConfiguration.Builder().Build();

            Assert.True(config.UseSharedStringValues);
        }

        [Fact]
        public void SmileFormat_Builder_SupportsChaining()
        {
            var config = SmileFormatConfiguration.Builder()
                .SetEnabled(true)
                .SetCompressionLevel(8)
                .SetUseSharedStringValues(false)
                .AddProperty("prop1", "value1")
                .Build();

            Assert.True(config.Enabled);
            Assert.Equal(8, config.CompressionLevel);
            Assert.False(config.UseSharedStringValues);
            Assert.Single(config.Properties);
        }

        #endregion

        #region MultiJoinOptimizationConfiguration Tests

        [Fact]
        public void MultiJoin_Builder_SetsEnabled()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetEnabled(true)
                .Build();

            Assert.True(config.Enabled);
        }

        [Fact]
        public void MultiJoin_Builder_SetsReorderStrategy()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetReorderStrategy(JoinReorderStrategy.Bushy)
                .Build();

            Assert.Equal(JoinReorderStrategy.Bushy, config.ReorderStrategy);
        }

        [Fact]
        public void MultiJoin_Builder_SetsMaxJoinsToOptimize()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetMaxJoinsToOptimize(20)
                .Build();

            Assert.Equal(20, config.MaxJoinsToOptimize);
        }

        [Fact]
        public void MultiJoin_Builder_ThrowsOnInvalidMaxJoins()
        {
            var builder = MultiJoinOptimizationConfiguration.Builder();

            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetMaxJoinsToOptimize(1));
            Assert.Throws<ArgumentOutOfRangeException>(() => builder.SetMaxJoinsToOptimize(0));
        }

        [Fact]
        public void MultiJoin_Builder_SetsCostBasedOptimization()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetUseCostBasedOptimization(false)
                .Build();

            Assert.False(config.UseCostBasedOptimization);
        }

        [Fact]
        public void MultiJoin_Builder_AddsCustomProperty()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .AddProperty("hint", "broadcast_left")
                .Build();

            Assert.Single(config.Properties);
            Assert.Equal("broadcast_left", config.Properties["hint"]);
        }

        [Fact]
        public void MultiJoin_Builder_DefaultsToEnabled()
        {
            var config = MultiJoinOptimizationConfiguration.Builder().Build();

            Assert.True(config.Enabled);
        }

        [Fact]
        public void MultiJoin_Builder_DefaultsToLeftDeepStrategy()
        {
            var config = MultiJoinOptimizationConfiguration.Builder().Build();

            Assert.Equal(JoinReorderStrategy.LeftDeep, config.ReorderStrategy);
        }

        [Fact]
        public void MultiJoin_Builder_DefaultsToMax10Joins()
        {
            var config = MultiJoinOptimizationConfiguration.Builder().Build();

            Assert.Equal(10, config.MaxJoinsToOptimize);
        }

        [Fact]
        public void MultiJoin_Builder_DefaultsToCostBasedOptimization()
        {
            var config = MultiJoinOptimizationConfiguration.Builder().Build();

            Assert.True(config.UseCostBasedOptimization);
        }

        [Fact]
        public void MultiJoin_Builder_SupportsChaining()
        {
            var config = MultiJoinOptimizationConfiguration.Builder()
                .SetEnabled(true)
                .SetReorderStrategy(JoinReorderStrategy.DynamicProgramming)
                .SetMaxJoinsToOptimize(15)
                .SetUseCostBasedOptimization(true)
                .AddProperty("prop1", "value1")
                .Build();

            Assert.True(config.Enabled);
            Assert.Equal(JoinReorderStrategy.DynamicProgramming, config.ReorderStrategy);
            Assert.Equal(15, config.MaxJoinsToOptimize);
            Assert.True(config.UseCostBasedOptimization);
            Assert.Single(config.Properties);
        }

        [Fact]
        public void JoinReorderStrategy_Enum_HasAllExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(JoinReorderStrategy), JoinReorderStrategy.None));
            Assert.True(Enum.IsDefined(typeof(JoinReorderStrategy), JoinReorderStrategy.LeftDeep));
            Assert.True(Enum.IsDefined(typeof(JoinReorderStrategy), JoinReorderStrategy.Bushy));
            Assert.True(Enum.IsDefined(typeof(JoinReorderStrategy), JoinReorderStrategy.DynamicProgramming));
        }

        #endregion

        #region RocksDBOptions Tests

        [Fact]
        public void RocksDBOptions_AllowsSettingAllProperties()
        {
            var options = new RocksDBOptions
            {
                MaxBackgroundJobs = 8,
                MaxWriteBufferNumber = 4,
                WriteBufferSize = 128 * 1024 * 1024,
                BlockCacheSize = 256 * 1024 * 1024,
                UseBloomFilter = true,
                CompactionStyle = "level",
                Properties = new Dictionary<string, string> { { "custom", "value" } }
            };

            Assert.Equal(8, options.MaxBackgroundJobs);
            Assert.Equal(4, options.MaxWriteBufferNumber);
            Assert.Equal(128 * 1024 * 1024, options.WriteBufferSize);
            Assert.Equal(256 * 1024 * 1024, options.BlockCacheSize);
            Assert.True(options.UseBloomFilter);
            Assert.Equal("level", options.CompactionStyle);
            Assert.Single(options.Properties);
        }

        #endregion
    }
}
