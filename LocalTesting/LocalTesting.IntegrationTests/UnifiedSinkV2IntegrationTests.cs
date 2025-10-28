using Flink.JobBuilder.Models;
using NUnit.Framework;
using System.Text.Json;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Integration tests for Unified Sink API v2 (Flink 1.20+) functionality.
/// Tests validate JobDefinition with UnifiedSinkV2Definition can be created,
/// serialized, deserialized, and validated correctly.
/// </summary>
[TestFixture]
[Category("unified-sink-v2")]
public class UnifiedSinkV2IntegrationTests
{
    #region Serialization and Deserialization Tests

    [Test]
    public void UnifiedSinkV2_JobDefinition_SerializesCorrectly()
    {
        // Arrange
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "input-topic",
                BootstrapServers = "localhost:9092",
                GroupId = "test-group",
                StartingOffsets = "earliest"
            },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "x => x.ToUpper()" }
            },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "kafka",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "KafkaUnifiedWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "topic", "output-topic" },
                        { "bootstrapServers", "localhost:9092" }
                    }
                },
                CommitterConfig = new SinkCommitterConfig
                {
                    Enabled = true,
                    ClassName = "KafkaCommitter",
                    Properties = new Dictionary<string, object>
                    {
                        { "transactionPrefix", "flink-" }
                    }
                },
                Semantics = "exactly-once",
                Stateful = true,
                Properties = new Dictionary<string, string>
                {
                    { "compression", "gzip" }
                }
            },
            Metadata = new JobMetadata
            {
                JobId = "test-job-001",
                JobName = "Unified Sink v2 Test",
                Version = "1.0",
                CreatedAt = DateTime.UtcNow
            }
        };

        // Act
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Sink, Is.Not.Null);
        Assert.That(deserialized.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());

        var sink = deserialized.Sink as UnifiedSinkV2Definition;
        Assert.That(sink, Is.Not.Null);
        Assert.That(sink.Type, Is.EqualTo("unified_sink_v2"));
        Assert.That(sink.SinkType, Is.EqualTo("kafka"));
        Assert.That(sink.Semantics, Is.EqualTo("exactly-once"));
        Assert.That(sink.Stateful, Is.True);
        Assert.That(sink.WriterConfig.ClassName, Is.EqualTo("KafkaUnifiedWriter"));
        Assert.That(sink.CommitterConfig, Is.Not.Null);
        Assert.That(sink.CommitterConfig.Enabled, Is.True);
    }

    [Test]
    public void UnifiedSinkV2_AtLeastOnceSemantics_SerializesCorrectly()
    {
        // Arrange
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "file",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "FileWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "path", "/tmp/output" },
                        { "format", "parquet" }
                    }
                },
                Semantics = "at-least-once",
                Stateful = false
            },
            Metadata = new JobMetadata { JobId = "test-002", Version = "1.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(jobDef);
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        var sink = deserialized.Sink as UnifiedSinkV2Definition;
        Assert.That(sink, Is.Not.Null);
        Assert.That(sink.Semantics, Is.EqualTo("at-least-once"));
        Assert.That(sink.Stateful, Is.False);
        Assert.That(sink.CommitterConfig, Is.Null);
    }

    [Test]
    public void UnifiedSinkV2_CustomSink_SerializesCorrectly()
    {
        // Arrange
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "custom",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "MyCustomWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "endpoint", "https://api.example.com/ingest" },
                        { "batchSize", 100 },
                        { "timeout", 5000 }
                    }
                },
                Semantics = "at-least-once",
                Properties = new Dictionary<string, string>
                {
                    { "retryAttempts", "3" },
                    { "backoffMs", "1000" }
                }
            },
            Metadata = new JobMetadata { JobId = "test-003", Version = "1.0" }
        };

        // Act
        var json = JsonSerializer.Serialize(jobDef);
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);

        // Assert
        Assert.That(deserialized, Is.Not.Null);
        var sink = deserialized.Sink as UnifiedSinkV2Definition;
        Assert.That(sink, Is.Not.Null);
        Assert.That(sink.SinkType, Is.EqualTo("custom"));
        Assert.That(sink.WriterConfig.ClassName, Is.EqualTo("MyCustomWriter"));
        
        // JSON deserialization converts numbers to JsonElement, so we need to handle that
        Assert.That(sink.WriterConfig.Properties.ContainsKey("batchSize"), Is.True);
        Assert.That(sink.WriterConfig.Properties["endpoint"].ToString(), Is.EqualTo("https://api.example.com/ingest"));
        Assert.That(sink.Properties["retryAttempts"], Is.EqualTo("3"));
    }

    #endregion

    #region Backward Compatibility Tests

    [Test]
    public void UnifiedSinkV2_CoexistsWithLegacyKafkaSink()
    {
        // Arrange - Legacy Kafka Sink
        var legacyJobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output",
                BootstrapServers = "localhost:9092",
                Serializer = "json"
            },
            Metadata = new JobMetadata { JobId = "legacy-001", Version = "1.0" }
        };

        // Arrange - Unified Sink v2
        var unifiedJobDef = new JobDefinition
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
                        { "bootstrapServers", "localhost:9092" }
                    }
                },
                Semantics = "at-least-once"
            },
            Metadata = new JobMetadata { JobId = "unified-001", Version = "1.0" }
        };

        // Act
        var legacyJson = JsonSerializer.Serialize(legacyJobDef);
        var unifiedJson = JsonSerializer.Serialize(unifiedJobDef);

        var legacyDeserialized = JsonSerializer.Deserialize<JobDefinition>(legacyJson);
        var unifiedDeserialized = JsonSerializer.Deserialize<JobDefinition>(unifiedJson);

        // Assert - Both patterns work
        Assert.That(legacyDeserialized.Sink, Is.InstanceOf<KafkaSinkDefinition>());
        Assert.That(unifiedDeserialized.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());

        // Assert - Type discriminators are different
        var legacySink = legacyDeserialized.Sink as KafkaSinkDefinition;
        var unifiedSink = unifiedDeserialized.Sink as UnifiedSinkV2Definition;

        Assert.That(legacySink?.Type, Is.EqualTo("kafka"));
        Assert.That(unifiedSink?.Type, Is.EqualTo("unified_sink_v2"));
    }

    #endregion

    #region JobDefinition Validation Tests

    [Test]
    public void UnifiedSinkV2_WithCompleteJob_ValidatesStructure()
    {
        // Arrange
        var jobDef = new JobDefinition
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
                    }
                },
                CommitterConfig = new SinkCommitterConfig
                {
                    Enabled = true,
                    ClassName = "KafkaCommitter",
                    Properties = new Dictionary<string, object>
                    {
                        { "transactionPrefix", "flink-processor-" },
                        { "transactionTimeout", 60000 }
                    }
                },
                Semantics = "exactly-once",
                Stateful = true,
                Properties = new Dictionary<string, string>
                {
                    { "maxInFlightRequests", "1" },
                    { "acks", "all" }
                }
            },
            Metadata = new JobMetadata
            {
                JobId = "processor-job-001",
                JobName = "Event Processor with Unified Sink",
                Version = "1.0",
                CreatedAt = DateTime.UtcNow,
                Parallelism = 4
            }
        };

        // Act
        var json = JsonSerializer.Serialize(jobDef, new JsonSerializerOptions { WriteIndented = true });

        // Assert - JSON should contain all components
        Assert.That(json, Does.Contain("unified_sink_v2"));
        Assert.That(json, Does.Contain("exactly-once"));
        Assert.That(json, Does.Contain("KafkaWriter"));
        Assert.That(json, Does.Contain("KafkaCommitter"));
        Assert.That(json, Does.Contain("transactionPrefix"));

        // Deserialize and validate
        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized.Source, Is.InstanceOf<KafkaSourceDefinition>());
        Assert.That(deserialized.Operations, Has.Count.EqualTo(2));
        Assert.That(deserialized.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());
        Assert.That(deserialized.Metadata.Parallelism, Is.EqualTo(4));
    }

    [Test]
    public void UnifiedSinkV2_MultipleJobDefinitions_EachSerializesIndependently()
    {
        // Arrange - Create 3 different job definitions
        var jobs = new[]
        {
            CreateExactlyOnceKafkaJob("job1"),
            CreateAtLeastOnceFileJob("job2"),
            CreateCustomSinkJob("job3")
        };

        // Act - Serialize and deserialize each
        var deserializedJobs = jobs
            .Select(job => JsonSerializer.Serialize(job))
            .Select(json => JsonSerializer.Deserialize<JobDefinition>(json))
            .ToList();

        // Assert
        Assert.That(deserializedJobs, Has.Count.EqualTo(3));
        Assert.That(deserializedJobs[0].Sink, Is.InstanceOf<UnifiedSinkV2Definition>());
        Assert.That(deserializedJobs[1].Sink, Is.InstanceOf<UnifiedSinkV2Definition>());
        Assert.That(deserializedJobs[2].Sink, Is.InstanceOf<UnifiedSinkV2Definition>());

        var sink0 = deserializedJobs[0].Sink as UnifiedSinkV2Definition;
        var sink1 = deserializedJobs[1].Sink as UnifiedSinkV2Definition;
        var sink2 = deserializedJobs[2].Sink as UnifiedSinkV2Definition;

        Assert.That(sink0?.Semantics, Is.EqualTo("exactly-once"));
        Assert.That(sink1?.Semantics, Is.EqualTo("at-least-once"));
        Assert.That(sink2?.SinkType, Is.EqualTo("custom"));
    }

    #endregion

    #region Helper Methods

    private JobDefinition CreateExactlyOnceKafkaJob(string jobId)
    {
        return new JobDefinition
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
                        { "bootstrapServers", "localhost:9092" }
                    }
                },
                CommitterConfig = new SinkCommitterConfig { Enabled = true },
                Semantics = "exactly-once",
                Stateful = true
            },
            Metadata = new JobMetadata { JobId = jobId, Version = "1.0" }
        };
    }

    private JobDefinition CreateAtLeastOnceFileJob(string jobId)
    {
        return new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "file",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "FileWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "path", "/data/output" }
                    }
                },
                Semantics = "at-least-once",
                Stateful = false
            },
            Metadata = new JobMetadata { JobId = jobId, Version = "1.0" }
        };
    }

    private JobDefinition CreateCustomSinkJob(string jobId)
    {
        return new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new UnifiedSinkV2Definition
            {
                SinkType = "custom",
                WriterConfig = new SinkWriterConfig
                {
                    ClassName = "CustomWriter",
                    Properties = new Dictionary<string, object>
                    {
                        { "endpoint", "http://api.example.com" }
                    }
                },
                Semantics = "at-least-once"
            },
            Metadata = new JobMetadata { JobId = jobId, Version = "1.0" }
        };
    }

    #endregion
}
