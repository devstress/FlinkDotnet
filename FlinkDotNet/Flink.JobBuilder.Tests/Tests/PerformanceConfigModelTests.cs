using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Unit tests for Performance and Format configuration models (Flink 2.1+).
/// Tests BatchingConfig and StateBackendConfig to achieve 100% code coverage.
/// </summary>
[TestFixture]
public class PerformanceConfigModelTests
{
    #region BatchingConfig Tests

    [Test]
    public void BatchingConfig_DefaultConstructor_AllPropertiesNull()
    {
        var config = new BatchingConfig();

        Assert.That(config.MaxBatchSize, Is.Null);
        Assert.That(config.MaxBatchSizeInBytes, Is.Null);
        Assert.That(config.MaxTimeInBufferMs, Is.Null);
        Assert.That(config.MaxInFlightRequests, Is.Null);
        Assert.That(config.MaxBufferedRequests, Is.Null);
    }

    [Test]
    public void BatchingConfig_SetMaxBatchSize_ReturnsValue()
    {
        var config = new BatchingConfig { MaxBatchSize = 1000 };

        Assert.That(config.MaxBatchSize, Is.EqualTo(1000));
    }

    [Test]
    public void BatchingConfig_SetMaxBatchSizeInBytes_ReturnsValue()
    {
        var config = new BatchingConfig { MaxBatchSizeInBytes = 5242880 }; // 5MB

        Assert.That(config.MaxBatchSizeInBytes, Is.EqualTo(5242880));
    }

    [Test]
    public void BatchingConfig_SetMaxTimeInBufferMs_ReturnsValue()
    {
        var config = new BatchingConfig { MaxTimeInBufferMs = 1000 };

        Assert.That(config.MaxTimeInBufferMs, Is.EqualTo(1000));
    }

    [Test]
    public void BatchingConfig_SetMaxInFlightRequests_ReturnsValue()
    {
        var config = new BatchingConfig { MaxInFlightRequests = 50 };

        Assert.That(config.MaxInFlightRequests, Is.EqualTo(50));
    }

    [Test]
    public void BatchingConfig_SetMaxBufferedRequests_ReturnsValue()
    {
        var config = new BatchingConfig { MaxBufferedRequests = 10000 };

        Assert.That(config.MaxBufferedRequests, Is.EqualTo(10000));
    }

    [Test]
    public void BatchingConfig_SetAllProperties_ReturnsAllValues()
    {
        var config = new BatchingConfig
        {
            MaxBatchSize = 1000,
            MaxBatchSizeInBytes = 5242880,
            MaxTimeInBufferMs = 1000,
            MaxInFlightRequests = 50,
            MaxBufferedRequests = 10000
        };

        Assert.That(config.MaxBatchSize, Is.EqualTo(1000));
        Assert.That(config.MaxBatchSizeInBytes, Is.EqualTo(5242880));
        Assert.That(config.MaxTimeInBufferMs, Is.EqualTo(1000));
        Assert.That(config.MaxInFlightRequests, Is.EqualTo(50));
        Assert.That(config.MaxBufferedRequests, Is.EqualTo(10000));
    }

    [Test]
    public void BatchingConfig_SizeBased_OnlySetsSizeProperties()
    {
        var config = new BatchingConfig
        {
            MaxBatchSize = 2000,
            MaxBatchSizeInBytes = 10485760 // 10MB
        };

        Assert.That(config.MaxBatchSize, Is.EqualTo(2000));
        Assert.That(config.MaxBatchSizeInBytes, Is.EqualTo(10485760));
        Assert.That(config.MaxTimeInBufferMs, Is.Null);
        Assert.That(config.MaxInFlightRequests, Is.Null);
        Assert.That(config.MaxBufferedRequests, Is.Null);
    }

    [Test]
    public void BatchingConfig_TimeBased_OnlySetsTimeProperty()
    {
        var config = new BatchingConfig
        {
            MaxTimeInBufferMs = 500
        };

        Assert.That(config.MaxTimeInBufferMs, Is.EqualTo(500));
        Assert.That(config.MaxBatchSize, Is.Null);
        Assert.That(config.MaxBatchSizeInBytes, Is.Null);
        Assert.That(config.MaxInFlightRequests, Is.Null);
        Assert.That(config.MaxBufferedRequests, Is.Null);
    }

    #endregion

    #region StateBackendConfig Tests

    [Test]
    public void StateBackendConfig_DefaultConstructor_HasDefaultValues()
    {
        var config = new StateBackendConfig();

        Assert.That(config.Type, Is.EqualTo("rocksdb"));
        Assert.That(config.CheckpointDir, Is.Null);
        Assert.That(config.IncrementalCheckpoints, Is.Null);
        Assert.That(config.PredefinedProfile, Is.Null);
        Assert.That(config.DbOptions, Is.Null);
        Assert.That(config.ColumnFamilyOptions, Is.Null);
    }

    [Test]
    public void StateBackendConfig_SetType_ReturnsValue()
    {
        var config = new StateBackendConfig { Type = "hashmap" };

        Assert.That(config.Type, Is.EqualTo("hashmap"));
    }

    [Test]
    public void StateBackendConfig_SetCheckpointDir_ReturnsValue()
    {
        var config = new StateBackendConfig { CheckpointDir = "s3://bucket/checkpoints" };

        Assert.That(config.CheckpointDir, Is.EqualTo("s3://bucket/checkpoints"));
    }

    [Test]
    public void StateBackendConfig_SetIncrementalCheckpoints_ReturnsValue()
    {
        var config = new StateBackendConfig { IncrementalCheckpoints = true };

        Assert.That(config.IncrementalCheckpoints, Is.True);
    }

    [Test]
    public void StateBackendConfig_SetPredefinedProfile_ReturnsValue()
    {
        var config = new StateBackendConfig { PredefinedProfile = "flash_ssd_optimized" };

        Assert.That(config.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));
    }

    [Test]
    public void StateBackendConfig_SetDbOptions_ReturnsValue()
    {
        var dbOptions = new Dictionary<string, object>
        {
            { "maxBackgroundJobs", 8 },
            { "compactionStyle", "level" }
        };
        var config = new StateBackendConfig { DbOptions = dbOptions };

        Assert.That(config.DbOptions, Is.Not.Null);
        Assert.That(config.DbOptions, Is.EqualTo(dbOptions));
        Assert.That(config.DbOptions!.ContainsKey("maxBackgroundJobs"), Is.True);
        Assert.That(config.DbOptions["maxBackgroundJobs"], Is.EqualTo(8));
    }

    [Test]
    public void StateBackendConfig_SetColumnFamilyOptions_ReturnsValue()
    {
        var cfOptions = new Dictionary<string, object>
        {
            { "blockCacheSize", 268435456L },
            { "writeBufferSize", 67108864L }
        };
        var config = new StateBackendConfig { ColumnFamilyOptions = cfOptions };

        Assert.That(config.ColumnFamilyOptions, Is.Not.Null);
        Assert.That(config.ColumnFamilyOptions, Is.EqualTo(cfOptions));
        Assert.That(config.ColumnFamilyOptions!.ContainsKey("blockCacheSize"), Is.True);
        Assert.That(config.ColumnFamilyOptions["blockCacheSize"], Is.EqualTo(268435456L));
    }

    [Test]
    public void StateBackendConfig_SetAllProperties_ReturnsAllValues()
    {
        var dbOptions = new Dictionary<string, object>
        {
            { "maxBackgroundJobs", 8 },
            { "maxOpenFiles", -1 },
            { "compactionStyle", "level" }
        };
        var cfOptions = new Dictionary<string, object>
        {
            { "blockCacheSize", 268435456L },
            { "writeBufferSize", 67108864L }
        };

        var config = new StateBackendConfig
        {
            Type = "rocksdb",
            CheckpointDir = "s3://production/checkpoints",
            IncrementalCheckpoints = true,
            PredefinedProfile = "flash_ssd_optimized",
            DbOptions = dbOptions,
            ColumnFamilyOptions = cfOptions
        };

        Assert.That(config.Type, Is.EqualTo("rocksdb"));
        Assert.That(config.CheckpointDir, Is.EqualTo("s3://production/checkpoints"));
        Assert.That(config.IncrementalCheckpoints, Is.True);
        Assert.That(config.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));
        Assert.That(config.DbOptions, Is.Not.Null);
        Assert.That(config.DbOptions, Has.Count.EqualTo(3));
        Assert.That(config.ColumnFamilyOptions, Is.Not.Null);
        Assert.That(config.ColumnFamilyOptions, Has.Count.EqualTo(2));
    }

    [Test]
    public void StateBackendConfig_RocksDbWithFlashSsdProfile_ValidConfiguration()
    {
        var config = new StateBackendConfig
        {
            Type = "rocksdb",
            CheckpointDir = "s3://bucket/checkpoints",
            IncrementalCheckpoints = true,
            PredefinedProfile = "flash_ssd_optimized"
        };

        Assert.That(config.Type, Is.EqualTo("rocksdb"));
        Assert.That(config.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));
        Assert.That(config.IncrementalCheckpoints, Is.True);
    }

    [Test]
    public void StateBackendConfig_RocksDbWithSpinningDiskProfile_ValidConfiguration()
    {
        var config = new StateBackendConfig
        {
            Type = "rocksdb",
            CheckpointDir = "hdfs://namenode/checkpoints",
            PredefinedProfile = "spinning_disk_optimized"
        };

        Assert.That(config.Type, Is.EqualTo("rocksdb"));
        Assert.That(config.PredefinedProfile, Is.EqualTo("spinning_disk_optimized"));
    }

    [Test]
    public void StateBackendConfig_HashMapBackend_NoRocksDbOptions()
    {
        var config = new StateBackendConfig
        {
            Type = "hashmap"
        };

        Assert.That(config.Type, Is.EqualTo("hashmap"));
        Assert.That(config.DbOptions, Is.Null);
        Assert.That(config.ColumnFamilyOptions, Is.Null);
        Assert.That(config.PredefinedProfile, Is.Null);
    }

    [Test]
    public void StateBackendConfig_FilesystemBackend_OnlyCheckpointDir()
    {
        var config = new StateBackendConfig
        {
            Type = "filesystem",
            CheckpointDir = "file:///tmp/checkpoints"
        };

        Assert.That(config.Type, Is.EqualTo("filesystem"));
        Assert.That(config.CheckpointDir, Is.EqualTo("file:///tmp/checkpoints"));
    }

    #endregion

    #region Integration with SinkWriterConfig Tests

    [Test]
    public void SinkWriterConfig_WithBatchingConfig_StoresBatchingSettings()
    {
        var batchingConfig = new BatchingConfig
        {
            MaxBatchSize = 1000,
            MaxTimeInBufferMs = 1000
        };

        var writerConfig = new SinkWriterConfig
        {
            ClassName = "TestWriter",
            BatchingConfig = batchingConfig
        };

        Assert.That(writerConfig.BatchingConfig, Is.Not.Null);
        Assert.That(writerConfig.BatchingConfig, Is.EqualTo(batchingConfig));
        Assert.That(writerConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(1000));
    }

    [Test]
    public void SinkWriterConfig_WithoutBatchingConfig_BatchingConfigIsNull()
    {
        var writerConfig = new SinkWriterConfig
        {
            ClassName = "TestWriter"
        };

        Assert.That(writerConfig.BatchingConfig, Is.Null);
    }

    #endregion

    #region Integration with JobMetadata Tests

    [Test]
    public void JobMetadata_WithStateBackendConfig_StoresStateBackendSettings()
    {
        var stateBackendConfig = new StateBackendConfig
        {
            Type = "rocksdb",
            CheckpointDir = "s3://bucket/checkpoints",
            IncrementalCheckpoints = true
        };

        var metadata = new JobMetadata
        {
            StateBackendConfig = stateBackendConfig
        };

        Assert.That(metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(metadata.StateBackendConfig, Is.EqualTo(stateBackendConfig));
        Assert.That(metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));
    }

    [Test]
    public void JobMetadata_WithoutStateBackendConfig_StateBackendConfigIsNull()
    {
        var metadata = new JobMetadata { };

        Assert.That(metadata.StateBackendConfig, Is.Null);
    }

    #endregion

    #region Complete Job Definition Tests

    [Test]
    public void JobDefinition_WithBatchingAndStateBackend_StoresBothConfigs()
    {
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 1000,
                        MaxBatchSizeInBytes = 5242880
                    }
                }
            },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://bucket/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized"
                }
            }
        };

        Assert.That(job.Metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(job.Metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));

        var sink = job.Sink as UnifiedSinkV2Definition;
        Assert.That(sink, Is.Not.Null);
        Assert.That(sink!.WriterConfig.BatchingConfig, Is.Not.Null);
        Assert.That(sink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(1000));
    }

    #endregion

    #region ExecutionPlanConfig Tests

    [Test]
    public void ExecutionPlanConfig_DefaultConstructor_HasDefaultValues()
    {
        var config = new ExecutionPlanConfig();

        Assert.That(config.Format, Is.EqualTo("json"));
        Assert.That(config.EnableCompression, Is.Null);
        Assert.That(config.Properties, Is.Null);
    }

    [Test]
    public void ExecutionPlanConfig_SetFormat_ReturnsValue()
    {
        var config = new ExecutionPlanConfig { Format = "smile" };

        Assert.That(config.Format, Is.EqualTo("smile"));
    }

    [Test]
    public void ExecutionPlanConfig_SetEnableCompression_ReturnsValue()
    {
        var config = new ExecutionPlanConfig { EnableCompression = true };

        Assert.That(config.EnableCompression, Is.True);
    }

    [Test]
    public void ExecutionPlanConfig_SetProperties_ReturnsValue()
    {
        var properties = new Dictionary<string, object>
        {
            { "bufferSize", 8192 },
            { "encoding", "utf-8" }
        };
        var config = new ExecutionPlanConfig { Properties = properties };

        Assert.That(config.Properties, Is.Not.Null);
        Assert.That(config.Properties, Is.EqualTo(properties));
        Assert.That(config.Properties!.ContainsKey("bufferSize"), Is.True);
    }

    [Test]
    public void ExecutionPlanConfig_SetAllProperties_ReturnsAllValues()
    {
        var properties = new Dictionary<string, object>
        {
            { "maxSize", 1024000 }
        };
        var config = new ExecutionPlanConfig
        {
            Format = "smile",
            EnableCompression = true,
            Properties = properties
        };

        Assert.That(config.Format, Is.EqualTo("smile"));
        Assert.That(config.EnableCompression, Is.True);
        Assert.That(config.Properties, Is.Not.Null);
        Assert.That(config.Properties, Has.Count.EqualTo(1));
    }

    [Test]
    public void ExecutionPlanConfig_JsonFormat_ValidConfiguration()
    {
        var config = new ExecutionPlanConfig
        {
            Format = "json",
            EnableCompression = false
        };

        Assert.That(config.Format, Is.EqualTo("json"));
        Assert.That(config.EnableCompression, Is.False);
    }

    [Test]
    public void ExecutionPlanConfig_SmileFormat_ValidConfiguration()
    {
        var config = new ExecutionPlanConfig
        {
            Format = "smile",
            EnableCompression = true
        };

        Assert.That(config.Format, Is.EqualTo("smile"));
        Assert.That(config.EnableCompression, Is.True);
    }

    #endregion

    #region OptimizerConfig Tests

    [Test]
    public void OptimizerConfig_DefaultConstructor_AllPropertiesNull()
    {
        var config = new OptimizerConfig();

        Assert.That(config.EnableMultiJoinOptimization, Is.Null);
        Assert.That(config.JoinReorderingStrategy, Is.Null);
        Assert.That(config.EnableJoinPredicatePushdown, Is.Null);
        Assert.That(config.EnableFilterPushdown, Is.Null);
        Assert.That(config.Properties, Is.Null);
    }

    [Test]
    public void OptimizerConfig_SetEnableMultiJoinOptimization_ReturnsValue()
    {
        var config = new OptimizerConfig { EnableMultiJoinOptimization = true };

        Assert.That(config.EnableMultiJoinOptimization, Is.True);
    }

    [Test]
    public void OptimizerConfig_SetJoinReorderingStrategy_ReturnsValue()
    {
        var config = new OptimizerConfig { JoinReorderingStrategy = "bushy" };

        Assert.That(config.JoinReorderingStrategy, Is.EqualTo("bushy"));
    }

    [Test]
    public void OptimizerConfig_SetEnableJoinPredicatePushdown_ReturnsValue()
    {
        var config = new OptimizerConfig { EnableJoinPredicatePushdown = true };

        Assert.That(config.EnableJoinPredicatePushdown, Is.True);
    }

    [Test]
    public void OptimizerConfig_SetEnableFilterPushdown_ReturnsValue()
    {
        var config = new OptimizerConfig { EnableFilterPushdown = true };

        Assert.That(config.EnableFilterPushdown, Is.True);
    }

    [Test]
    public void OptimizerConfig_SetProperties_ReturnsValue()
    {
        var properties = new Dictionary<string, object>
        {
            { "maxJoinDepth", 5 },
            { "estimatorType", "legacy" }
        };
        var config = new OptimizerConfig { Properties = properties };

        Assert.That(config.Properties, Is.Not.Null);
        Assert.That(config.Properties, Is.EqualTo(properties));
        Assert.That(config.Properties!.ContainsKey("maxJoinDepth"), Is.True);
    }

    [Test]
    public void OptimizerConfig_SetAllProperties_ReturnsAllValues()
    {
        var properties = new Dictionary<string, object>
        {
            { "hint1", "value1" },
            { "hint2", "value2" }
        };
        var config = new OptimizerConfig
        {
            EnableMultiJoinOptimization = true,
            JoinReorderingStrategy = "cost_based",
            EnableJoinPredicatePushdown = true,
            EnableFilterPushdown = true,
            Properties = properties
        };

        Assert.That(config.EnableMultiJoinOptimization, Is.True);
        Assert.That(config.JoinReorderingStrategy, Is.EqualTo("cost_based"));
        Assert.That(config.EnableJoinPredicatePushdown, Is.True);
        Assert.That(config.EnableFilterPushdown, Is.True);
        Assert.That(config.Properties, Is.Not.Null);
        Assert.That(config.Properties, Has.Count.EqualTo(2));
    }

    [Test]
    public void OptimizerConfig_BushyStrategy_ValidConfiguration()
    {
        var config = new OptimizerConfig
        {
            EnableMultiJoinOptimization = true,
            JoinReorderingStrategy = "bushy"
        };

        Assert.That(config.EnableMultiJoinOptimization, Is.True);
        Assert.That(config.JoinReorderingStrategy, Is.EqualTo("bushy"));
    }

    [Test]
    public void OptimizerConfig_LeftDeepStrategy_ValidConfiguration()
    {
        var config = new OptimizerConfig
        {
            JoinReorderingStrategy = "left_deep",
            EnableJoinPredicatePushdown = true
        };

        Assert.That(config.JoinReorderingStrategy, Is.EqualTo("left_deep"));
        Assert.That(config.EnableJoinPredicatePushdown, Is.True);
    }

    [Test]
    public void OptimizerConfig_AllOptimizationsEnabled_ValidConfiguration()
    {
        var config = new OptimizerConfig
        {
            EnableMultiJoinOptimization = true,
            EnableJoinPredicatePushdown = true,
            EnableFilterPushdown = true
        };

        Assert.That(config.EnableMultiJoinOptimization, Is.True);
        Assert.That(config.EnableJoinPredicatePushdown, Is.True);
        Assert.That(config.EnableFilterPushdown, Is.True);
    }

    #endregion

    #region Integration with JobMetadata Tests (New Features)

    [Test]
    public void JobMetadata_WithExecutionPlanConfig_StoresExecutionPlanSettings()
    {
        var planConfig = new ExecutionPlanConfig
        {
            Format = "smile",
            EnableCompression = true
        };

        var metadata = new JobMetadata
        {
            ExecutionPlanConfig = planConfig
        };

        Assert.That(metadata.ExecutionPlanConfig, Is.Not.Null);
        Assert.That(metadata.ExecutionPlanConfig, Is.EqualTo(planConfig));
        Assert.That(metadata.ExecutionPlanConfig!.Format, Is.EqualTo("smile"));
    }

    [Test]
    public void JobMetadata_WithOptimizerConfig_StoresOptimizerSettings()
    {
        var optimizerConfig = new OptimizerConfig
        {
            EnableMultiJoinOptimization = true,
            JoinReorderingStrategy = "bushy"
        };

        var metadata = new JobMetadata
        {
            OptimizerConfig = optimizerConfig
        };

        Assert.That(metadata.OptimizerConfig, Is.Not.Null);
        Assert.That(metadata.OptimizerConfig, Is.EqualTo(optimizerConfig));
        Assert.That(metadata.OptimizerConfig!.EnableMultiJoinOptimization, Is.True);
    }

    [Test]
    public void JobMetadata_WithAllPerformanceConfigs_StoresAllSettings()
    {
        var metadata = new JobMetadata
        {
            StateBackendConfig = new StateBackendConfig
            {
                Type = "rocksdb",
                CheckpointDir = "s3://bucket/checkpoints"
            },
            ExecutionPlanConfig = new ExecutionPlanConfig
            {
                Format = "smile",
                EnableCompression = true
            },
            OptimizerConfig = new OptimizerConfig
            {
                EnableMultiJoinOptimization = true,
                JoinReorderingStrategy = "cost_based"
            }
        };

        Assert.That(metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(metadata.ExecutionPlanConfig, Is.Not.Null);
        Assert.That(metadata.OptimizerConfig, Is.Not.Null);
        Assert.That(metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(metadata.ExecutionPlanConfig!.Format, Is.EqualTo("smile"));
        Assert.That(metadata.OptimizerConfig!.EnableMultiJoinOptimization, Is.True);
    }

    #endregion

    #region Complete Job Definition Tests (All 4 Features)

    [Test]
    public void JobDefinition_WithAll4PerformanceFeatures_StoresAllConfigs()
    {
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 1000,
                        MaxBatchSizeInBytes = 5242880,
                        MaxTimeInBufferMs = 1000
                    }
                }
            },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://production/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized"
                },
                ExecutionPlanConfig = new ExecutionPlanConfig
                {
                    Format = "smile",
                    EnableCompression = true
                },
                OptimizerConfig = new OptimizerConfig
                {
                    EnableMultiJoinOptimization = true,
                    JoinReorderingStrategy = "bushy",
                    EnableJoinPredicatePushdown = true,
                    EnableFilterPushdown = true
                }
            }
        };

        // Assert: All 4 performance features configured
        Assert.That(job.Metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(job.Metadata.ExecutionPlanConfig, Is.Not.Null);
        Assert.That(job.Metadata.OptimizerConfig, Is.Not.Null);

        var sink = job.Sink as UnifiedSinkV2Definition;
        Assert.That(sink!.WriterConfig.BatchingConfig, Is.Not.Null);

        // Feature 1: Custom Async Sink Batching
        Assert.That(sink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(1000));

        // Feature 2: Enhanced State Backend Configuration
        Assert.That(job.Metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(job.Metadata.StateBackendConfig.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));

        // Feature 3: Smile Format for Compiled Plans
        Assert.That(job.Metadata.ExecutionPlanConfig!.Format, Is.EqualTo("smile"));
        Assert.That(job.Metadata.ExecutionPlanConfig.EnableCompression, Is.True);

        // Feature 4: MultiJoin Optimization Configuration
        Assert.That(job.Metadata.OptimizerConfig!.EnableMultiJoinOptimization, Is.True);
        Assert.That(job.Metadata.OptimizerConfig.JoinReorderingStrategy, Is.EqualTo("bushy"));
    }

    #endregion
}
