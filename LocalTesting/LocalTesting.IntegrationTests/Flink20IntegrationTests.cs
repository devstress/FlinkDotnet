using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Integration tests for Apache Flink 2.0 features.
/// Covers Disaggregated State Management architecture and related features.
/// Maximum 5 tests as per project guidelines.
/// </summary>
[TestFixture]
[Category("flink-2.0")]
public class Flink20IntegrationTests
{
    #region Test 1: Disaggregated State Backend with S3 Storage

    /// <summary>
    /// Test 1: Validates DisaggregatedStateBackend configuration with S3 storage.
    /// Tests S3-based disaggregated state backend with incremental checkpointing and compression.
    /// </summary>
    [Test]
    public void Test1_DisaggregatedStateBackend_S3Storage_ValidatesConfiguration()
    {
        // Arrange: Create job with S3 disaggregated state backend
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "orders" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "processed-orders",
                BootstrapServers = "localhost:9092"
            },
            Metadata = new JobMetadata
            {
                                Version = "2.0.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "disaggregated",
                    StorageType = "s3",
                    StoragePath = "s3://flink-state-bucket/production/checkpoints",
                    IncrementalCheckpoints = true,
                    EnableCompression = true,
                    AsyncCompactionThreads = 8
                }
            }
        };

        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(job, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        // Assert: Verify state backend configuration
        Assert.That(job.Metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(job.Metadata.StateBackendConfig.Type, Is.EqualTo("disaggregated"));
        Assert.That(job.Metadata.StateBackendConfig.StorageType, Is.EqualTo("s3"));
        Assert.That(job.Metadata.StateBackendConfig.IncrementalCheckpoints, Is.True);
        Assert.That(job.Metadata.StateBackendConfig.EnableCompression, Is.True);
        Assert.That(job.Metadata.StateBackendConfig.AsyncCompactionThreads, Is.EqualTo(8));

        // Verify JSON contains key configuration
        Assert.That(json, Does.Contain("disaggregated"));
        Assert.That(json, Does.Contain("s3://flink-state-bucket"));
    }

    #endregion

    #region Test 2: Disaggregated State Backend with HDFS Storage

    /// <summary>
    /// Test 2: Validates DisaggregatedStateBackend configuration with HDFS storage.
    /// Tests on-premise HDFS deployment with custom compaction settings.
    /// </summary>
    [Test]
    public void Test2_DisaggregatedStateBackend_HDFSStorage_ValidatesConfiguration()
    {
        // Arrange: Create job with HDFS disaggregated state backend
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "transactions" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "validated-transactions",
                BootstrapServers = "kafka:9092"
            },
            Metadata = new JobMetadata
            {
                                Version = "2.0.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "disaggregated",
                    StorageType = "hdfs",
                    StoragePath = "hdfs://namenode:9000/flink/state",
                    IncrementalCheckpoints = true,
                    EnableCompression = false,
                    AsyncCompactionThreads = 4
                }
            }
        };

        // Act: Serialize and deserialize to verify round-trip
        var json = JsonSerializer.Serialize(job);
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert: Verify configuration persists through serialization
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Metadata.StateBackendConfig.Type, Is.EqualTo("disaggregated"));
        Assert.That(deserialized.Metadata.StateBackendConfig.StorageType, Is.EqualTo("hdfs"));
        Assert.That(deserialized.Metadata.StateBackendConfig.StoragePath, Does.Contain("hdfs://"));
        Assert.That(deserialized.Metadata.StateBackendConfig.EnableCompression, Is.False);
        Assert.That(deserialized.Metadata.StateBackendConfig.AsyncCompactionThreads, Is.EqualTo(4));
    }

    #endregion

    #region Test 3: Disaggregated State Backend with Azure Blob Storage

    /// <summary>
    /// Test 3: Validates DisaggregatedStateBackend configuration with Azure Blob Storage.
    /// Tests Azure cloud deployment with state compression enabled.
    /// </summary>
    [Test]
    public void Test3_DisaggregatedStateBackend_AzureStorage_ValidatesConfiguration()
    {
        // Arrange: Create job with Azure Blob disaggregated state backend
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "events" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "processed-events",
                BootstrapServers = "kafka.azure.local:9092"
            },
            Metadata = new JobMetadata
            {
                                Version = "2.0.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "disaggregated",
                    StorageType = "azure_blob",
                    StoragePath = "wasbs://flink-state@myaccount.blob.core.windows.net/checkpoints",
                    IncrementalCheckpoints = true,
                    EnableCompression = true,
                    AsyncCompactionThreads = 6
                }
            }
        };

        // Act & Assert: Verify all Azure-specific configuration
        Assert.That(job.Metadata.StateBackendConfig.Type, Is.EqualTo("disaggregated"));
        Assert.That(job.Metadata.StateBackendConfig.StorageType, Is.EqualTo("azure_blob"));
        Assert.That(job.Metadata.StateBackendConfig.StoragePath, Does.StartWith("wasbs://"));
        Assert.That(job.Metadata.StateBackendConfig.StoragePath, Does.Contain("blob.core.windows.net"));
        Assert.That(job.Metadata.StateBackendConfig.IncrementalCheckpoints, Is.True);
        Assert.That(job.Metadata.StateBackendConfig.EnableCompression, Is.True);
    }

    #endregion

    #region Test 4: Disaggregated State Backend with GCS Storage

    /// <summary>
    /// Test 4: Validates DisaggregatedStateBackend configuration with Google Cloud Storage.
    /// Tests GCP deployment with maximum compaction threads.
    /// </summary>
    [Test]
    public void Test4_DisaggregatedStateBackend_GCSStorage_ValidatesConfiguration()
    {
        // Arrange: Create job with GCS disaggregated state backend
        var job = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "user-activity" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "analytics",
                BootstrapServers = "kafka.gcp.local:9092"
            },
            Metadata = new JobMetadata
            {
                                Version = "2.0.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "disaggregated",
                    StorageType = "gcs",
                    StoragePath = "gs://my-flink-bucket/state/production",
                    IncrementalCheckpoints = true,
                    EnableCompression = true,
                    AsyncCompactionThreads = 12
                }
            }
        };

        // Act: Serialize with different options
        var options = new JsonSerializerOptions
        {
            WriteIndented = false,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var compactJson = JsonSerializer.Serialize(job, options);

        // Assert: Verify GCS configuration
        Assert.That(job.Metadata.StateBackendConfig.Type, Is.EqualTo("disaggregated"));
        Assert.That(job.Metadata.StateBackendConfig.StorageType, Is.EqualTo("gcs"));
        Assert.That(job.Metadata.StateBackendConfig.StoragePath, Does.StartWith("gs://"));
        Assert.That(job.Metadata.StateBackendConfig.AsyncCompactionThreads, Is.EqualTo(12));
        
        // Verify compact JSON format
        Assert.That(compactJson, Does.Not.Contain("\n"));
        Assert.That(compactJson, Does.Contain("gcs"));
    }

    #endregion

    #region Test 5: State Backend Comparison - Legacy vs Disaggregated

    /// <summary>
    /// Test 5: Compares legacy RocksDB state backend with new disaggregated state backend.
    /// Validates that both configurations can coexist and serialize correctly.
    /// </summary>
    [Test]
    public void Test5_StateBackendComparison_LegacyVsDisaggregated_ValidatesCoexistence()
    {
        // Arrange: Create job with legacy RocksDB backend
        var legacyJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "legacy-input" },
            Sink = new KafkaSinkDefinition { Topic = "legacy-output" },
            Metadata = new JobMetadata
            {
                                Version = "1.20.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "file:///tmp/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized"
                }
            }
        };

        // Arrange: Create job with new disaggregated backend
        var modernJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "modern-input" },
            Sink = new KafkaSinkDefinition { Topic = "modern-output" },
            Metadata = new JobMetadata
            {
                                Version = "2.0.0",
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "disaggregated",
                    StorageType = "s3",
                    StoragePath = "s3://modern-bucket/state",
                    IncrementalCheckpoints = true,
                    EnableCompression = true,
                    AsyncCompactionThreads = 8
                }
            }
        };

        // Act: Serialize both configurations
        var legacyJson = JsonSerializer.Serialize(legacyJob);
        var modernJson = JsonSerializer.Serialize(modernJob);

        var deserializedLegacy = JsonSerializer.Deserialize<JobDefinition>(legacyJson);
        var deserializedModern = JsonSerializer.Deserialize<JobDefinition>(modernJson);

        // Assert: Verify legacy backend configuration
        Assert.That(deserializedLegacy, Is.Not.Null);
        Assert.That(deserializedLegacy!.Metadata.StateBackendConfig.Type, Is.EqualTo("rocksdb"));
        Assert.That(deserializedLegacy.Metadata.StateBackendConfig.PredefinedProfile, Is.Not.Null);
        Assert.That(deserializedLegacy.Metadata.StateBackendConfig.CheckpointDir, Does.StartWith("file://"));

        // Assert: Verify disaggregated backend configuration
        Assert.That(deserializedModern, Is.Not.Null);
        Assert.That(deserializedModern!.Metadata.StateBackendConfig.Type, Is.EqualTo("disaggregated"));
        Assert.That(deserializedModern.Metadata.StateBackendConfig.StorageType, Is.EqualTo("s3"));
        Assert.That(deserializedModern.Metadata.StateBackendConfig.EnableCompression, Is.True);

        // Assert: Verify both support incremental checkpoints
        Assert.That(deserializedLegacy.Metadata.StateBackendConfig.IncrementalCheckpoints, Is.True);
        Assert.That(deserializedModern.Metadata.StateBackendConfig.IncrementalCheckpoints, Is.True);

        // Assert: Verify configurations are independent
        Assert.That(deserializedLegacy.Metadata.StateBackendConfig.Type, 
            Is.Not.EqualTo(deserializedModern.Metadata.StateBackendConfig.Type));
    }

    #endregion
}
