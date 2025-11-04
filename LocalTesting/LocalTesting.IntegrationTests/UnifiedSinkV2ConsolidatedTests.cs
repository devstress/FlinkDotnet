using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Consolidated integration tests for Unified Sink API v2 (Flink 1.20+).
/// Combines IR schema and C# API tests into 5 comprehensive tests that maintain full coverage.
/// Maximum 5 tests per Flink version as per project guidelines.
/// </summary>
[TestFixture]
[Category("unified-sink-v2")]
public class UnifiedSinkV2ConsolidatedTests
{
    #region Test 1: Comprehensive Serialization & IR Schema

    /// <summary>
    /// Test 1: Validates complete IR schema serialization including:
    /// - Exactly-once semantics with committer
    /// - At-least-once semantics without committer
    /// - Custom sink types
    /// - JSON round-trip serialization
    /// - Multiple job definitions
    /// </summary>
    [Test]
    public void Test1_ComprehensiveSerialization_ValidatesAllSemantics()
    {
        // Part A: Exactly-Once Kafka Sink with Committer
        var exactlyOnceJob = new JobDefinition
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
                        { "transactionPrefix", "flink-" },
                        { "transactionTimeout", 60000 }
                    }
                },
                Semantics = "exactly-once",
                Stateful = true,
                Properties = new Dictionary<string, string>
                {
                    { "compression", "gzip" },
                    { "maxInFlightRequests", "1" }
                }
            },
            Metadata = new JobMetadata
            {
                                JobName = "Exactly-Once Test",
                Version = "1.0",
                Parallelism = 4
            }
        };

        // Part B: At-Least-Once File Sink (no committer)
        var atLeastOnceJob = new JobDefinition
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
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Part C: Custom Sink
        var customSinkJob = new JobDefinition
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
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Act: Serialize and deserialize all three job types
        var jobs = new[] { exactlyOnceJob, atLeastOnceJob, customSinkJob };
        var deserializedJobs = jobs
            .Select(job => JsonSerializer.Serialize(job, new JsonSerializerOptions { WriteIndented = true }))
            .Select(json => JsonSerializer.Deserialize<JobDefinition>(json))
            .ToList();

        // Assert: Exactly-Once Sink
        Assert.That(deserializedJobs[0], Is.Not.Null);
        var exactlyOnceSink = deserializedJobs[0]!.Sink as UnifiedSinkV2Definition;
        Assert.That(exactlyOnceSink, Is.Not.Null);
        Assert.That(exactlyOnceSink!.Type, Is.EqualTo("unified_sink_v2"));
        Assert.That(exactlyOnceSink.SinkType, Is.EqualTo("kafka"));
        Assert.That(exactlyOnceSink.Semantics, Is.EqualTo("exactly-once"));
        Assert.That(exactlyOnceSink.Stateful, Is.True);
        Assert.That(exactlyOnceSink.WriterConfig!.ClassName, Is.EqualTo("KafkaUnifiedWriter"));
        Assert.That(exactlyOnceSink.CommitterConfig, Is.Not.Null);
        Assert.That(exactlyOnceSink.CommitterConfig!.Enabled, Is.True);
        Assert.That(exactlyOnceSink.CommitterConfig.ClassName, Is.EqualTo("KafkaCommitter"));

        // Assert: At-Least-Once Sink
        var atLeastOnceSink = deserializedJobs[1]!.Sink as UnifiedSinkV2Definition;
        Assert.That(atLeastOnceSink, Is.Not.Null);
        Assert.That(atLeastOnceSink!.SinkType, Is.EqualTo("file"));
        Assert.That(atLeastOnceSink.Semantics, Is.EqualTo("at-least-once"));
        Assert.That(atLeastOnceSink.Stateful, Is.False);
        Assert.That(atLeastOnceSink.CommitterConfig, Is.Null);

        // Assert: Custom Sink
        var customSink = deserializedJobs[2]!.Sink as UnifiedSinkV2Definition;
        Assert.That(customSink, Is.Not.Null);
        Assert.That(customSink!.SinkType, Is.EqualTo("custom"));
        Assert.That(customSink.WriterConfig!.ClassName, Is.EqualTo("MyCustomWriter"));
        Assert.That(customSink.WriterConfig.Properties!.ContainsKey("batchSize"), Is.True);
        Assert.That(customSink.Properties!["retryAttempts"], Is.EqualTo("3"));

        // Assert: All sinks are independent
        Assert.That(deserializedJobs, Has.Count.EqualTo(3));
        var sinkTypes = deserializedJobs.Select(j => (j!.Sink as UnifiedSinkV2Definition)?.SinkType).Distinct().ToList();
        Assert.That(sinkTypes, Has.Count.EqualTo(3));
        Assert.That(sinkTypes, Contains.Item("kafka"));
        Assert.That(sinkTypes, Contains.Item("file"));
        Assert.That(sinkTypes, Contains.Item("custom"));
    }

    #endregion

    #region Test 2: C# API End-to-End Flow

    /// <summary>
    /// Test 2: Validates complete C# API workflow including:
    /// - Writer creation and element writing
    /// - Flush and prepare commit operations
    /// - Committer creation and commit
    /// - SinkBuilder fluent API
    /// - Element context handling
    /// </summary>
    [Test]
    public async Task Test2_CSharpApiEndToEnd_WritesAndCommitsSuccessfully()
    {
        // Part A: Test with direct sink implementation
        var directTestSink = new TestSink();
        var writerContext = new SinkWriterContext
        {
            SubtaskId = 0,
            NumberOfParallelSubtasks = 1,
            AttemptNumber = 0
        };
        var elementContext = new ElementContext
        {
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Watermark = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000
        };

        // Create writer and write elements
        var writer = await directTestSink.CreateWriterAsync(writerContext);
        await writer.WriteAsync("element1", elementContext);
        await writer.WriteAsync("element2", elementContext);
        await writer.WriteAsync("element3", elementContext);

        // Flush and prepare commit
        await writer.FlushAsync(false);
        var committables = await writer.PrepareCommitAsync();

        // Create committer and commit
        var committer = directTestSink.CreateCommitter();
        Assert.That(committer, Is.Not.Null);
        await committer!.CommitAsync(committables);

        // Assert: Direct implementation
        Assert.That(directTestSink.WrittenElements, Has.Count.EqualTo(3));
        Assert.That(directTestSink.CommittedElements, Has.Count.EqualTo(3));

        // Part B: Test with SinkBuilder
        var builderWrittenElements = new List<string>();
        var builderCommittedElements = new List<string>();

        var builtSink = new SinkBuilder<string, string, int>()
            .WithWriter((ctx, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(
                new TestWriter(builderWrittenElements, state)))
            .WithCommitter(() => new TestCommitter(builderCommittedElements))
            .Build();

        var builderWriter = await builtSink.CreateWriterAsync(writerContext);
        await builderWriter.WriteAsync("test1", elementContext);
        await builderWriter.WriteAsync("test2", elementContext);

        var builderCommittables = await builderWriter.PrepareCommitAsync();
        var builderCommitter = builtSink.CreateCommitter();
        await builderCommitter!.CommitAsync(builderCommittables);

        // Assert: SinkBuilder
        Assert.That(builderWrittenElements, Has.Count.EqualTo(2));
        Assert.That(builderCommittedElements, Has.Count.EqualTo(2));
        Assert.That(builderWrittenElements, Contains.Item("test1"));
        Assert.That(builderWrittenElements, Contains.Item("test2"));
    }

    #endregion

    #region Test 3: State Management & Checkpointing

    /// <summary>
    /// Test 3: Validates state management including:
    /// - State snapshot creation
    /// - State restoration
    /// - Parallel writer independence
    /// - Checkpoint coordination
    /// </summary>
    [Test]
    public async Task Test3_StateManagement_HandlesSnapshotsAndParallelism()
    {
        // Part A: State snapshot progression
        var sink = new TestSink();
        var context = new SinkWriterContext { SubtaskId = 0, NumberOfParallelSubtasks = 1 };

        var writer = await sink.CreateWriterAsync(context, restoredState: 100);

        var state1 = await writer.SnapshotStateAsync(1);
        var state2 = await writer.SnapshotStateAsync(2);
        var state3 = await writer.SnapshotStateAsync(3);

        // Assert: State increments from restored value
        Assert.That(state1, Is.EqualTo(100));
        Assert.That(state2, Is.EqualTo(101));
        Assert.That(state3, Is.EqualTo(102));

        // Part B: Parallel writers with independent state
        var parallelSink = new TestSink();

        var writer1Context = new SinkWriterContext { SubtaskId = 0, NumberOfParallelSubtasks = 2 };
        var writer2Context = new SinkWriterContext { SubtaskId = 1, NumberOfParallelSubtasks = 2 };

        var writer1 = await parallelSink.CreateWriterAsync(writer1Context, restoredState: 0);
        var writer2 = await parallelSink.CreateWriterAsync(writer2Context, restoredState: 500);

        var writer1State = await writer1.SnapshotStateAsync(1);
        var writer2State = await writer2.SnapshotStateAsync(1);

        // Assert: Each writer maintains independent state
        Assert.That(writer1State, Is.EqualTo(0));
        Assert.That(writer2State, Is.EqualTo(500));

        // Advance each writer independently
        await writer1.SnapshotStateAsync(2);
        var writer1State2 = await writer1.SnapshotStateAsync(3);
        var writer2State2 = await writer2.SnapshotStateAsync(2);

        Assert.That(writer1State2, Is.EqualTo(2)); // Advanced 3 times from 0
        Assert.That(writer2State2, Is.EqualTo(501)); // Advanced 2 times from 500
    }

    #endregion

    #region Test 4: Backward Compatibility

    /// <summary>
    /// Test 4: Validates backward compatibility including:
    /// - Coexistence with legacy KafkaSinkDefinition
    /// - Type discriminator handling
    /// - Independent serialization paths
    /// - No breaking changes to existing code
    /// </summary>
    [Test]
    public void Test4_BackwardCompatibility_CoexistsWithLegacySinks()
    {
        // Part A: Legacy Kafka Sink
        var legacyJob = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition
            {
                Topic = "legacy-output",
                BootstrapServers = "localhost:9092",
                Serializer = "json"
            },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Part B: Unified Sink v2
        var unifiedJob = new JobDefinition
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
                        { "topic", "unified-output" },
                        { "bootstrapServers", "localhost:9092" }
                    }
                },
                Semantics = "exactly-once",
                CommitterConfig = new SinkCommitterConfig { Enabled = true }
            },
            Metadata = new JobMetadata { Version = "1.0" }
        };

        // Act: Serialize both
        var legacyJson = JsonSerializer.Serialize(legacyJob);
        var unifiedJson = JsonSerializer.Serialize(unifiedJob);

        var legacyDeserialized = JsonSerializer.Deserialize<JobDefinition>(legacyJson);
        var unifiedDeserialized = JsonSerializer.Deserialize<JobDefinition>(unifiedJson);

        // Assert: Both patterns work independently
        Assert.That(legacyDeserialized!.Sink, Is.InstanceOf<KafkaSinkDefinition>());
        Assert.That(unifiedDeserialized!.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());

        // Assert: Type discriminators are different
        var legacySink = legacyDeserialized.Sink as KafkaSinkDefinition;
        var unifiedSink = unifiedDeserialized.Sink as UnifiedSinkV2Definition;

        Assert.That(legacySink?.Type, Is.EqualTo("kafka"));
        Assert.That(unifiedSink?.Type, Is.EqualTo("unified_sink_v2"));

        // Assert: Properties are preserved correctly
        Assert.That(legacySink?.Topic, Is.EqualTo("legacy-output"));
        Assert.That(unifiedSink?.Semantics, Is.EqualTo("exactly-once"));
    }

    #endregion

    #region Test 5: Advanced Features & Integration

    /// <summary>
    /// Test 5: Validates advanced features including:
    /// - DataStream API integration
    /// - Committer retry logic
    /// - Complete job validation with all components
    /// - Complex job pipelines
    /// </summary>
    [Test]
    public async Task Test5_AdvancedFeatures_IntegratesWithDataStreamAndRetries()
    {
        // Part A: DataStream Integration
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { "item1", "item2", "item3" });

        var sink = new SinkBuilder<string, string, int>()
            .WithWriter((ctx, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(
                new TestWriter(new List<string>(), state)))
            .Build();

        var result = stream.AddSink(sink);

        Assert.That(result, Is.Not.Null, "AddSink should return a DataStream");
        Assert.That(result, Is.SameAs(stream), "Fluent API should return same stream");

        // Part B: Committer Retry Logic
        var committedElements = new List<string>();
        int commitAttempts = 0;

        var retryCommitter = new RetryableCommitter(committedElements, attempts => commitAttempts = attempts);

        // First attempt (partial failure)
        var failures1 = await retryCommitter.CommitAsync(new List<string> { "item1", "item2", "item3" });
        Assert.That(failures1, Is.Not.Empty, "First attempt should have failures");
        Assert.That(commitAttempts, Is.EqualTo(1));

        // Retry failed items
        var failures2 = await retryCommitter.CommitAsync(failures1);
        Assert.That(failures2, Is.Empty, "Retry should succeed for all items");
        Assert.That(commitAttempts, Is.EqualTo(2));
        Assert.That(committedElements, Has.Count.GreaterThan(0));

        // Part C: Complete Job with All Components
        var completeJob = new JobDefinition
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
                                JobName = "Complete Event Processor",
                Version = "1.0",
                Parallelism = 4
            }
        };

        var json = JsonSerializer.Serialize(completeJob, new JsonSerializerOptions { WriteIndented = true });

        // Assert: Complete job structure
        Assert.That(json, Does.Contain("unified_sink_v2"));
        Assert.That(json, Does.Contain("exactly-once"));
        Assert.That(json, Does.Contain("KafkaWriter"));
        Assert.That(json, Does.Contain("KafkaCommitter"));

        var deserialized = JsonSerializer.Deserialize<JobDefinition>(json);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Source, Is.InstanceOf<KafkaSourceDefinition>());
        Assert.That(deserialized.Operations, Has.Count.EqualTo(2));
        Assert.That(deserialized.Sink, Is.InstanceOf<UnifiedSinkV2Definition>());
        Assert.That(deserialized.Metadata.Parallelism, Is.EqualTo(4));
    }

    #endregion

    #region Helper Classes

    private class TestSink : ISink<string, string, int>
    {
        private readonly List<string> _writtenElements = new();
        private readonly List<string> _committedElements = new();

        public List<string> WrittenElements => _writtenElements;
        public List<string> CommittedElements => _committedElements;

        public Task<ISinkWriter<string, string, int>> CreateWriterAsync(
            SinkWriterContext context,
            int restoredState = default!,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ISinkWriter<string, string, int>>(
                new TestWriter(_writtenElements, restoredState));
        }

        public ICommitter<string>? CreateCommitter()
        {
            return new TestCommitter(_committedElements);
        }

        public IGlobalCommitter<string, string>? CreateGlobalCommitter()
        {
            return null;
        }
    }

    private class TestWriter : ISinkWriter<string, string, int>
    {
        private readonly List<string> _elements;
        private readonly List<string> _pendingCommits = new();
        private int _state;

        public TestWriter(List<string> elements, int initialState)
        {
            _elements = elements;
            _state = initialState;
        }

        public Task WriteAsync(string element, ElementContext context, CancellationToken cancellationToken = default)
        {
            _elements.Add(element);
            _pendingCommits.Add(element);
            return Task.CompletedTask;
        }

        public Task FlushAsync(bool endOfInput, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<List<string>> PrepareCommitAsync(CancellationToken cancellationToken = default)
        {
            var result = new List<string>(_pendingCommits);
            _pendingCommits.Clear();
            return Task.FromResult(result);
        }

        public Task<int> SnapshotStateAsync(long checkpointId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_state++);
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }
    }

    private class TestCommitter : ICommitter<string>
    {
        private readonly List<string> _committedElements;

        public TestCommitter(List<string> committedElements)
        {
            _committedElements = committedElements;
        }

        public Task<List<string>> CommitAsync(List<string> committables, CancellationToken cancellationToken = default)
        {
            _committedElements.AddRange(committables);
            return Task.FromResult(new List<string>()); // No failures
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }
    }

    private class RetryableCommitter : ICommitter<string>
    {
        private readonly List<string> _committedElements;
        private int _attempts;
        private readonly Action<int> _reportAttempt;

        public RetryableCommitter(List<string> committedElements, Action<int> reportAttempt)
        {
            _committedElements = committedElements;
            _reportAttempt = reportAttempt;
        }

        public Task<List<string>> CommitAsync(List<string> committables, CancellationToken cancellationToken = default)
        {
            _attempts++;
            _reportAttempt(_attempts);

            if (_attempts == 1)
            {
                // First attempt - commit half, fail half
                int halfSize = committables.Count / 2;
                _committedElements.AddRange(committables.GetRange(0, halfSize));
                return Task.FromResult(committables.GetRange(halfSize, committables.Count - halfSize));
            }
            else
            {
                // Subsequent attempts - commit all
                _committedElements.AddRange(committables);
                return Task.FromResult(new List<string>());
            }
        }

        public Task CloseAsync()
        {
            return Task.CompletedTask;
        }
    }

    #endregion
}
