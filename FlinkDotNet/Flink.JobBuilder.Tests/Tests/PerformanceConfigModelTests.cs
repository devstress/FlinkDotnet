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
            JobId = "test-job",
            StateBackendConfig = stateBackendConfig
        };

        Assert.That(metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(metadata.StateBackendConfig, Is.EqualTo(stateBackendConfig));
        Assert.That(metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));
    }

    [Test]
    public void JobMetadata_WithoutStateBackendConfig_StateBackendConfigIsNull()
    {
        var metadata = new JobMetadata
        {
            JobId = "test-job"
        };

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
                JobId = "optimized-job",
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
}
