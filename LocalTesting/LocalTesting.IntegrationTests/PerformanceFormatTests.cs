using System.Text.Json;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Integration tests for Performance and Format features (Flink 2.1+).
/// Covers state backend configuration and async sink batching optimizations.
/// Maximum 5 tests per Apache Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("performance-format")]
public class PerformanceFormatTests
{
    #region Test 1: State Backend Configuration and IR Serialization

    /// <summary>
    /// Test 1: Validates StateBackendConfig IR schema including:
    /// - RocksDB state backend configuration
    /// - Predefined profiles (Flash SSD, Spinning Disk)
    /// - Database options (compaction, background jobs)
    /// - Column family options (block cache, write buffers)
    /// - JSON round-trip serialization
    /// </summary>
    [Test]
    public void Test1_StateBackendConfig_ValidatesIRSchemaAndSerialization()
    {
        // Part A: RocksDB with Flash SSD profile
        JobDefinition flashSsdJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output",
                BootstrapServers = "localhost:9092"
            },
            Metadata = new JobMetadata
            {
                Version = "1.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://my-bucket/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized",
                    DbOptions = new Dictionary<string, object>
                    {
                        { "maxBackgroundJobs", 8 },
                        { "maxOpenFiles", -1 },
                        { "compactionStyle", "level" }
                    },
                    ColumnFamilyOptions = new Dictionary<string, object>
                    {
                        { "blockCacheSize", 268_435_456L }, // 256MB
                        { "writeBufferSize", 67_108_864L }  // 64MB
                    }
                }
            }
        };

        // Part B: RocksDB with Spinning Disk profile
        JobDefinition spinningDiskJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                Version = "1.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "hdfs://namenode:9000/checkpoints",
                    IncrementalCheckpoints = false,
                    PredefinedProfile = "spinning_disk_optimized",
                    DbOptions = new Dictionary<string, object>
                    {
                        { "maxBackgroundJobs", 4 },
                        { "compactionStyle", "universal" }
                    }
                }
            }
        };

        // Part C: Minimal config (defaults)
        JobDefinition minimalJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                Version = "1.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "file:///tmp/checkpoints"
                }
            }
        };

        // Act: Serialize and deserialize all three job types
        JobDefinition[] jobs = [flashSsdJob, spinningDiskJob, minimalJob];
        List<JobDefinition> deserializedJobs = new List<JobDefinition>();

        foreach (JobDefinition? job in jobs)
        {
            string json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
            JobDefinition? deserialized = JsonSerializer.Deserialize<JobDefinition>(json);
            Assert.That(deserialized, Is.Not.Null, "Deserialization should succeed");
            deserializedJobs.Add(deserialized!);
        }

        // Assert: Flash SSD config
        StateBackendConfig? flashSsdConfig = deserializedJobs[0].Metadata.StateBackendConfig;
        Assert.That(flashSsdConfig, Is.Not.Null);
        Assert.That(flashSsdConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(flashSsdConfig.CheckpointDir, Is.EqualTo("s3://my-bucket/checkpoints"));
        Assert.That(flashSsdConfig.IncrementalCheckpoints, Is.True);
        Assert.That(flashSsdConfig.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));
        Assert.That(flashSsdConfig.DbOptions, Is.Not.Null);
        Assert.That(flashSsdConfig.DbOptions!.ContainsKey("maxBackgroundJobs"), Is.True);
        Assert.That(flashSsdConfig.DbOptions.ContainsKey("compactionStyle"), Is.True);
        Assert.That(flashSsdConfig.ColumnFamilyOptions, Is.Not.Null);
        Assert.That(flashSsdConfig.ColumnFamilyOptions!.ContainsKey("blockCacheSize"), Is.True);

        // Assert: Spinning Disk config
        StateBackendConfig? spinningDiskConfig = deserializedJobs[1].Metadata.StateBackendConfig;
        Assert.That(spinningDiskConfig, Is.Not.Null);
        Assert.That(spinningDiskConfig!.PredefinedProfile, Is.EqualTo("spinning_disk_optimized"));
        Assert.That(spinningDiskConfig.IncrementalCheckpoints, Is.False);
        Assert.That(spinningDiskConfig.DbOptions, Is.Not.Null);
        Assert.That(spinningDiskConfig.DbOptions!.ContainsKey("compactionStyle"), Is.True);

        // Assert: Minimal config
        StateBackendConfig? minimalConfig = deserializedJobs[2].Metadata.StateBackendConfig;
        Assert.That(minimalConfig, Is.Not.Null);
        Assert.That(minimalConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(minimalConfig.CheckpointDir, Is.EqualTo("file:///tmp/checkpoints"));
        Assert.That(minimalConfig.PredefinedProfile, Is.Null);
        Assert.That(minimalConfig.DbOptions, Is.Null);
    }

    #endregion
}
