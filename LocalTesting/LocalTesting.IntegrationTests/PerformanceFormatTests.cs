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

    #region Test 2: Async Sink Batching Configuration

    /// <summary>
    /// Test 2: Validates BatchingConfig in async sinks including:
    /// - Size-based batching (MaxBatchSize, MaxBatchSizeInBytes)
    /// - Time-based batching (MaxTimeInBufferMs)
    /// - In-flight and buffered request limits
    /// - Integration with UnifiedSinkV2Definition
    /// - JSON serialization
    /// </summary>
    [Test]
    public void Test2_AsyncSinkBatching_ValidatesConfiguration()
    {
        // Part A: Size-based batching
        JobDefinition sizeBatchingJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "output" },
                        { "bootstrapServers", "kafka:9092" }
                    },
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 1000,
                        MaxBatchSizeInBytes = 5 * 1024 * 1024, // 5MB
                        MaxInFlightRequests = 50,
                        MaxBufferedRequests = 10000
                    }
                },
                Semantics = "exactly-once"
            },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Part B: Time-based batching
        JobDefinition timeBatchingJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "output" },
                        { "bootstrapServers", "kafka:9092" }
                    },
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 100,
                        MaxTimeInBufferMs = 1000, // 1 second
                        MaxInFlightRequests = 10
                    }
                }
            },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Part C: No batching config (defaults)
        JobDefinition noBatchingJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "output" },
                        { "bootstrapServers", "kafka:9092" }
                    }
                    // No BatchingConfig - should work fine
                }
            },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Act: Serialize and deserialize
        JobDefinition[] jobs = [sizeBatchingJob, timeBatchingJob, noBatchingJob];
        List<JobDefinition> deserializedJobs = new List<JobDefinition>();

        foreach (JobDefinition? job in jobs)
        {
            string json = JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true });
            JobDefinition? deserialized = JsonSerializer.Deserialize<JobDefinition>(json);
            Assert.That(deserialized, Is.Not.Null);
            deserializedJobs.Add(deserialized!);
        }

        // Assert: Size-based batching
        UnifiedSinkV2Definition? sizeSink = deserializedJobs[0].Sink as UnifiedSinkV2Definition;
        Assert.That(sizeSink, Is.Not.Null);
        Assert.That(sizeSink!.WriterConfig.BatchingConfig, Is.Not.Null);
        Assert.That(sizeSink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(1000));
        Assert.That(sizeSink.WriterConfig.BatchingConfig.MaxBatchSizeInBytes, Is.EqualTo(5 * 1024 * 1024));
        Assert.That(sizeSink.WriterConfig.BatchingConfig.MaxInFlightRequests, Is.EqualTo(50));
        Assert.That(sizeSink.WriterConfig.BatchingConfig.MaxBufferedRequests, Is.EqualTo(10000));

        // Assert: Time-based batching
        UnifiedSinkV2Definition? timeSink = deserializedJobs[1].Sink as UnifiedSinkV2Definition;
        Assert.That(timeSink, Is.Not.Null);
        Assert.That(timeSink!.WriterConfig.BatchingConfig, Is.Not.Null);
        Assert.That(timeSink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(100));
        Assert.That(timeSink.WriterConfig.BatchingConfig.MaxTimeInBufferMs, Is.EqualTo(1000));

        // Assert: No batching config (backward compatibility)
        UnifiedSinkV2Definition? noSink = deserializedJobs[2].Sink as UnifiedSinkV2Definition;
        Assert.That(noSink, Is.Not.Null);
        Assert.That(noSink!.WriterConfig.BatchingConfig, Is.Null, "Batching config should be optional");
    }

    #endregion

    #region Test 3: All 4 Performance &amp; Format Features

    /// <summary>
    /// Test 3: Validates ALL 4 Performance &amp; Format features including:
    /// - Feature 1: Custom Async Sink Batching
    /// - Feature 2: Enhanced State Backend Configuration  
    /// - Feature 3: Smile Format for Compiled Plans
    /// - Feature 4: MultiJoin Optimization Configuration
    /// - Complete job definition with all performance features
    /// - Backward compatibility (optional configs)
    /// </summary>
    [Test]
    public void Test3_CombinedOptimizations_ValidatesAll4PerformanceFeatures()
    {
        // Part A: Complete job with all performance features
        JobDefinition optimizedJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "events",
                BootstrapServers = "kafka:9092",
                GroupId = "processor",
                StartingOffsets = "latest"
            },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "x.Length > 0" },
                new MapOperationDefinition { Expression = "x.ToUpper()" }
            },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "processed-events" },
                        { "bootstrapServers", "kafka:9092" },
                        { "compressionType", "gzip" }
                    },
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 1000,
                        MaxBatchSizeInBytes = 5 * 1024 * 1024,
                        MaxTimeInBufferMs = 1000,
                        MaxInFlightRequests = 50,
                        MaxBufferedRequests = 10000
                    }
                },
                CommitterConfig = new SinkCommitterConfig
                {
                    Enabled = true,
                    ClassName = "KafkaCommitter"
                },
                Semantics = "exactly-once",
                Stateful = true
            },
            Metadata = new JobMetadata
            {
                JobName = "High-Performance Event Processor",
                Version = "1.0",
                Parallelism = 8,
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://production/checkpoints",
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
                        { "blockCacheSize", 512 * 1024 * 1024L }, // 512MB
                        { "writeBufferSize", 128 * 1024 * 1024L }  // 128MB
                    }
                },
                // Feature 3: Smile Format for Compiled Plans
                ExecutionPlanConfig = new ExecutionPlanConfig
                {
                    Format = "smile",
                    EnableCompression = true,
                    Properties = new Dictionary<string, object>
                    {
                        { "bufferSize", 8192 }
                    }
                },
                // Feature 4: MultiJoin Optimization Configuration
                OptimizerConfig = new OptimizerConfig
                {
                    EnableMultiJoinOptimization = true,
                    JoinReorderingStrategy = "bushy",
                    EnableJoinPredicatePushdown = true,
                    EnableFilterPushdown = true,
                    Properties = new Dictionary<string, object>
                    {
                        { "maxJoinDepth", 5 }
                    }
                }
            }
        };

        // Part B: Job without performance configs (backward compatibility)
        JobDefinition standardJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "kafka:9092" },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Act: Serialize and validate
        string optimizedJson = JsonSerializer.Serialize(optimizedJob, new JsonSerializerOptions { WriteIndented = true });
        string standardJson = JsonSerializer.Serialize(standardJob, new JsonSerializerOptions { WriteIndented = true });

        JobDefinition? optimizedDeserialized = JsonSerializer.Deserialize<JobDefinition>(optimizedJson);
        JobDefinition? standardDeserialized = JsonSerializer.Deserialize<JobDefinition>(standardJson);

        // Assert: Optimized job has ALL 4 features
        Assert.That(optimizedDeserialized, Is.Not.Null);
        Assert.That(optimizedDeserialized!.Metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(optimizedDeserialized.Metadata.ExecutionPlanConfig, Is.Not.Null);
        Assert.That(optimizedDeserialized.Metadata.OptimizerConfig, Is.Not.Null);

        // Feature 2: State Backend Configuration
        StateBackendConfig? stateConfig = optimizedDeserialized.Metadata.StateBackendConfig;
        Assert.That(stateConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(stateConfig.IncrementalCheckpoints, Is.True);
        Assert.That(stateConfig.PredefinedProfile, Is.EqualTo("flash_ssd_optimized"));

        // Feature 1: Async Sink Batching
        UnifiedSinkV2Definition? sink = optimizedDeserialized.Sink as UnifiedSinkV2Definition;
        Assert.That(sink, Is.Not.Null);
        Assert.That(sink!.WriterConfig.BatchingConfig, Is.Not.Null);
        Assert.That(sink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(1000));
        Assert.That(sink.Semantics, Is.EqualTo("exactly-once"));

        // Feature 3: Execution Plan Config (Smile Format)
        ExecutionPlanConfig? planConfig = optimizedDeserialized.Metadata.ExecutionPlanConfig;
        Assert.That(planConfig!.Format, Is.EqualTo("smile"));
        Assert.That(planConfig.EnableCompression, Is.True);
        Assert.That(planConfig.Properties, Is.Not.Null);

        // Feature 4: Optimizer Config (MultiJoin Optimization)
        OptimizerConfig? optimizerConfig = optimizedDeserialized.Metadata.OptimizerConfig;
        Assert.That(optimizerConfig!.EnableMultiJoinOptimization, Is.True);
        Assert.That(optimizerConfig.JoinReorderingStrategy, Is.EqualTo("bushy"));
        Assert.That(optimizerConfig.EnableJoinPredicatePushdown, Is.True);
        Assert.That(optimizerConfig.EnableFilterPushdown, Is.True);

        // Assert: Standard job works without performance configs
        Assert.That(standardDeserialized, Is.Not.Null);
        Assert.That(standardDeserialized!.Metadata.StateBackendConfig, Is.Null, "Config should be optional");
        Assert.That(standardDeserialized.Metadata.ExecutionPlanConfig, Is.Null, "Config should be optional");
        Assert.That(standardDeserialized.Metadata.OptimizerConfig, Is.Null, "Config should be optional");

        // Assert: JSON contains ALL 4 performance feature keywords
        Assert.That(optimizedJson, Does.Contain("StateBackendConfig"));
        Assert.That(optimizedJson, Does.Contain("BatchingConfig"));
        Assert.That(optimizedJson, Does.Contain("ExecutionPlanConfig"));
        Assert.That(optimizedJson, Does.Contain("OptimizerConfig"));
        Assert.That(optimizedJson, Does.Contain("flash_ssd_optimized"));
        Assert.That(optimizedJson, Does.Contain("smile"));
        Assert.That(optimizedJson, Does.Contain("bushy"));
        Assert.That(optimizedJson, Does.Contain("MaxBatchSize"));
    }

    #endregion

    #region Test 4: Edge Cases and Validation

    /// <summary>
    /// Test 4: Validates edge cases and error handling including:
    /// - Null/missing configurations (optional features)
    /// - Empty dictionaries for DbOptions and ColumnFamilyOptions
    /// - Different state backend types (rocksdb, hashmap, filesystem)
    /// - Mixing configurations (state backend without batching, etc.)
    /// </summary>
    [Test]
    public void Test4_EdgeCases_ValidatesOptionalConfigsAndDefaults()
    {
        // Part A: State backend types
        JobDefinition rocksDbJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://bucket/checkpoints"
                }
            }
        };

        JobDefinition hashmapJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "hashmap"
                    // No checkpoint dir needed for hashmap
                }
            }
        };

        JobDefinition filesystemJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "filesystem",
                    CheckpointDir = "file:///tmp/checkpoints"
                }
            }
        };

        // Part B: Empty options dictionaries
        JobDefinition emptyOptionsJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://bucket/checkpoints",
                    DbOptions = new Dictionary<string, object>(), // Empty but not null
                    ColumnFamilyOptions = new Dictionary<string, object>() // Empty but not null
                }
            }
        };

        // Part C: State backend without batching
        JobDefinition stateOnlyJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter"
                    // No BatchingConfig
                }
            },
            Metadata = new JobMetadata
            {
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://bucket/checkpoints"
                }
            }
        };

        // Part D: Batching without state backend
        JobDefinition batchingOnlyJob = new JobDefinition
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
                        MaxBatchSize = 500
                    }
                }
            },
            Metadata = new JobMetadata
            {                // No StateBackendConfig
            }
        };

        // Act: Serialize and deserialize all jobs
        JobDefinition[] jobs = [rocksDbJob, hashmapJob, filesystemJob, emptyOptionsJob, stateOnlyJob, batchingOnlyJob];
        List<JobDefinition> deserializedJobs = new List<JobDefinition>();

        foreach (JobDefinition? job in jobs)
        {
            string json = JsonSerializer.Serialize(job);
            JobDefinition? deserialized = JsonSerializer.Deserialize<JobDefinition>(json);
            Assert.That(deserialized, Is.Not.Null, $"Job {job.Metadata.JobName} should deserialize");
            deserializedJobs.Add(deserialized!);
        }

        // Assert: Different state backend types
        Assert.That(deserializedJobs[0].Metadata.StateBackendConfig!.Type, Is.EqualTo("rocksdb"));
        Assert.That(deserializedJobs[1].Metadata.StateBackendConfig!.Type, Is.EqualTo("hashmap"));
        Assert.That(deserializedJobs[2].Metadata.StateBackendConfig!.Type, Is.EqualTo("filesystem"));

        // Assert: Empty dictionaries are preserved
        Assert.That(deserializedJobs[3].Metadata.StateBackendConfig!.DbOptions, Is.Not.Null);
        Assert.That(deserializedJobs[3].Metadata.StateBackendConfig!.DbOptions, Is.Empty);
        Assert.That(deserializedJobs[3].Metadata.StateBackendConfig!.ColumnFamilyOptions, Is.Not.Null);
        Assert.That(deserializedJobs[3].Metadata.StateBackendConfig!.ColumnFamilyOptions, Is.Empty);

        // Assert: State backend without batching
        Assert.That(deserializedJobs[4].Metadata.StateBackendConfig, Is.Not.Null);
        UnifiedSinkV2Definition? stateSink = deserializedJobs[4].Sink as UnifiedSinkV2Definition;
        Assert.That(stateSink!.WriterConfig.BatchingConfig, Is.Null);

        // Assert: Batching without state backend
        Assert.That(deserializedJobs[5].Metadata.StateBackendConfig, Is.Null);
        UnifiedSinkV2Definition? batchSink = deserializedJobs[5].Sink as UnifiedSinkV2Definition;
        Assert.That(batchSink!.WriterConfig.BatchingConfig, Is.Not.Null);
        Assert.That(batchSink.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(500));
    }

    #endregion

    #region Test 5: Real-World Production Scenarios

    /// <summary>
    /// Test 5: Validates realistic production scenarios including:
    /// - High-throughput event processing pipeline
    /// - Low-latency stream processing
    /// - Multi-stage pipeline with complex operations
    /// - Complete IR validation with all features enabled
    /// </summary>
    [Test]
    public void Test5_ProductionScenarios_ValidatesRealWorldConfigurations()
    {
        // Part A: High-Throughput Scenario (maximize throughput, tolerate higher latency)
        JobDefinition highThroughputJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "high-volume-events",
                BootstrapServers = "kafka:9092",
                GroupId = "throughput-processor",
                Properties = new Dictionary<string, string>
                {
                    { "fetch.min.bytes", "1048576" }, // 1MB
                    { "fetch.max.wait.ms", "500" }
                }
            },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "x != null && x.Length > 0" },
                new MapOperationDefinition { Expression = "x.Trim().ToLower()" }
            },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "processed-high-volume" },
                        { "bootstrapServers", "kafka:9092" },
                        { "compressionType", "snappy" },
                        { "lingerMs", 100 },
                        { "batchSize", 1_048_576 } // 1MB
                    },
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 5000,
                        MaxBatchSizeInBytes = 10 * 1024 * 1024, // 10MB
                        MaxTimeInBufferMs = 2000, // 2 seconds
                        MaxInFlightRequests = 100,
                        MaxBufferedRequests = 50000
                    }
                },
                Semantics = "at-least-once", // Favor throughput over exactly-once
                Stateful = false
            },
            Metadata = new JobMetadata
            {
                JobName = "High-Throughput Event Processor",
                Version = "1.0",
                Parallelism = 16,
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://production/high-throughput/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized",
                    DbOptions = new Dictionary<string, object>
                    {
                        { "maxBackgroundJobs", 16 },
                        { "maxOpenFiles", -1 },
                        { "compactionStyle", "level" }
                    },
                    ColumnFamilyOptions = new Dictionary<string, object>
                    {
                        { "blockCacheSize", 1024 * 1024 * 1024L }, // 1GB
                        { "writeBufferSize", 256 * 1024 * 1024L }   // 256MB
                    }
                }
            }
        };

        // Part B: Low-Latency Scenario (minimize latency, small batches)
        JobDefinition lowLatencyJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "realtime-events",
                BootstrapServers = "kafka:9092",
                GroupId = "latency-processor"
            },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "realtime-output" },
                        { "bootstrapServers", "kafka:9092" },
                        { "lingerMs", 0 } // Immediate send
                    },
                    BatchingConfig = new BatchingConfig
                    {
                        MaxBatchSize = 10,
                        MaxBatchSizeInBytes = 10240, // 10KB
                        MaxTimeInBufferMs = 100, // 100ms
                        MaxInFlightRequests = 5
                    }
                },
                Semantics = "exactly-once",
                Stateful = true,
                CommitterConfig = new SinkCommitterConfig { Enabled = true }
            },
            Metadata = new JobMetadata
            {
                JobName = "Low-Latency Stream Processor",
                Version = "1.0",
                Parallelism = 4,
                StateBackendConfig = new StateBackendConfig
                {
                    Type = "rocksdb",
                    CheckpointDir = "s3://production/low-latency/checkpoints",
                    IncrementalCheckpoints = true,
                    PredefinedProfile = "flash_ssd_optimized",
                    DbOptions = new Dictionary<string, object>
                    {
                        { "maxBackgroundJobs", 4 },
                        { "compactionStyle", "level" }
                    },
                    ColumnFamilyOptions = new Dictionary<string, object>
                    {
                        { "blockCacheSize", 128 * 1024 * 1024L } // 128MB (smaller for low-latency)
                    }
                }
            }
        };

        // Act: Serialize and validate both scenarios
        string highThroughputJson = JsonSerializer.Serialize(highThroughputJob, new JsonSerializerOptions { WriteIndented = true });
        string lowLatencyJson = JsonSerializer.Serialize(lowLatencyJob, new JsonSerializerOptions { WriteIndented = true });

        JobDefinition? highThroughputDeserialized = JsonSerializer.Deserialize<JobDefinition>(highThroughputJson);
        JobDefinition? lowLatencyDeserialized = JsonSerializer.Deserialize<JobDefinition>(lowLatencyJson);

        // Assert: High-throughput configuration
        Assert.That(highThroughputDeserialized, Is.Not.Null);
        UnifiedSinkV2Definition? htSink = highThroughputDeserialized!.Sink as UnifiedSinkV2Definition;
        Assert.That(htSink!.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(5000));
        Assert.That(htSink.WriterConfig.BatchingConfig.MaxBatchSizeInBytes, Is.EqualTo(10 * 1024 * 1024));
        Assert.That(htSink.Semantics, Is.EqualTo("at-least-once"));
        Assert.That(highThroughputDeserialized.Metadata.Parallelism, Is.EqualTo(16));
        Assert.That(highThroughputDeserialized.Metadata.StateBackendConfig, Is.Not.Null);
        Assert.That(highThroughputDeserialized.Metadata.StateBackendConfig!.ColumnFamilyOptions, Is.Not.Null);
        Assert.That(highThroughputDeserialized.Metadata.StateBackendConfig.ColumnFamilyOptions!.ContainsKey("blockCacheSize"), Is.True);

        // Assert: Low-latency configuration
        Assert.That(lowLatencyDeserialized, Is.Not.Null);
        UnifiedSinkV2Definition? ltSink = lowLatencyDeserialized!.Sink as UnifiedSinkV2Definition;
        Assert.That(ltSink!.WriterConfig.BatchingConfig!.MaxBatchSize, Is.EqualTo(10));
        Assert.That(ltSink.WriterConfig.BatchingConfig.MaxTimeInBufferMs, Is.EqualTo(100));
        Assert.That(ltSink.Semantics, Is.EqualTo("exactly-once"));
        Assert.That(lowLatencyDeserialized.Metadata.Parallelism, Is.EqualTo(4));

        // Assert: JSON contains production-relevant keywords
        Assert.That(highThroughputJson, Does.Contain("high-throughput"));
        Assert.That(highThroughputJson, Does.Contain("at-least-once"));
        Assert.That(lowLatencyJson, Does.Contain("low-latency"));
        Assert.That(lowLatencyJson, Does.Contain("exactly-once"));

        // Assert: Complete IR validation
        Assert.That(highThroughputDeserialized.Source, Is.InstanceOf<KafkaSourceDefinition>());
        Assert.That(highThroughputDeserialized.Operations, Has.Count.EqualTo(2));
        Assert.That(lowLatencyDeserialized.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());
    }

    #endregion
}
