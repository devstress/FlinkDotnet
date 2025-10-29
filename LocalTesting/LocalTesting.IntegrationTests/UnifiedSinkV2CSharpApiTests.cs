using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests
{
    /// <summary>
    /// Integration tests for Unified Sink API v2 C# user-facing API.
    /// Tests validate that the new ISink, ISinkWriter, ICommitter, and SinkBuilder APIs work correctly.
    /// </summary>
    [TestFixture]
    public class UnifiedSinkV2CSharpApiTests
    {
        // Test sink implementation for integration testing
        private class TestIntegrationSink : ISink<string, string, int>
        {
            private readonly List<string> _writtenElements = [];
            private readonly List<string> _committedElements = [];

            public List<string> WrittenElements => this._writtenElements;
            public List<string> CommittedElements => this._committedElements;

            public Task<ISinkWriter<string, string, int>> CreateWriterAsync(
                SinkWriterContext context,
                int restoredState = default,
                CancellationToken cancellationToken = default)
            {
                return Task.FromResult<ISinkWriter<string, string, int>>(
                    new TestIntegrationWriter(this._writtenElements, restoredState));
            }

            public ICommitter<string>? CreateCommitter()
            {
                return new TestIntegrationCommitter(this._committedElements);
            }

            public IGlobalCommitter<string, string>? CreateGlobalCommitter()
            {
                return null; // Not using global committer for this test
            }
        }

        private class TestIntegrationWriter : ISinkWriter<string, string, int>
        {
            private readonly List<string> _elements;
            private readonly List<string> _pendingCommits = [];
            private int _state;

            public TestIntegrationWriter(List<string> elements, int initialState)
            {
                this._elements = elements;
                this._state = initialState;
            }

            public Task WriteAsync(string element, ElementContext context, CancellationToken cancellationToken = default)
            {
                this._elements.Add(element);
                this._pendingCommits.Add(element);
                return Task.CompletedTask;
            }

            public Task FlushAsync(bool endOfInput, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }

            public Task<List<string>> PrepareCommitAsync(CancellationToken cancellationToken = default)
            {
                List<string> result = [.. this._pendingCommits];
                this._pendingCommits.Clear();
                return Task.FromResult(result);
            }

            public Task<int> SnapshotStateAsync(long checkpointId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this._state++);
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }
        }

        private class TestIntegrationCommitter : ICommitter<string>
        {
            private readonly List<string> _committedElements;

            public TestIntegrationCommitter(List<string> committedElements)
            {
                this._committedElements = committedElements;
            }

            public Task<List<string>> CommitAsync(List<string> committables, CancellationToken cancellationToken = default)
            {
                this._committedElements.AddRange(committables);
                return Task.FromResult(new List<string>()); // No failures
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task UnifiedSinkV2_EndToEnd_WritesAndCommitsElements()
        {
            // Arrange
            TestIntegrationSink sink = new();
            
            SinkWriterContext context = new()
            {
                SubtaskId = 0,
                NumberOfParallelSubtasks = 1,
                AttemptNumber = 0
            };

            ElementContext elementContext = new()
            {
                Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                Watermark = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - 1000
            };

            // Act - Create writer and write elements
            ISinkWriter<string, string, int> writer = await sink.CreateWriterAsync(context);
            await writer.WriteAsync("element1", elementContext);
            await writer.WriteAsync("element2", elementContext);
            await writer.WriteAsync("element3", elementContext);

            // Flush and prepare commit
            await writer.FlushAsync(false);
            List<string> committables = await writer.PrepareCommitAsync();

            // Create committer and commit
            ICommitter<string>? committer = sink.CreateCommitter();
            Assert.That(committer, Is.Not.Null, "Committer should be created");
            
            await committer!.CommitAsync(committables);

            // Assert
            Assert.That(sink.WrittenElements, Has.Count.EqualTo(3), "Should have 3 written elements");
            Assert.That(sink.CommittedElements, Has.Count.EqualTo(3), "Should have 3 committed elements");
            Assert.That(sink.WrittenElements, Contains.Item("element1"));
            Assert.That(sink.WrittenElements, Contains.Item("element2"));
            Assert.That(sink.WrittenElements, Contains.Item("element3"));
        }

        [Test]
        public async Task UnifiedSinkV2_StateSnapshot_PreservesState()
        {
            // Arrange
            TestIntegrationSink sink = new();
            SinkWriterContext context = new() { SubtaskId = 0, NumberOfParallelSubtasks = 1 };

            // Act
            ISinkWriter<string, string, int> writer = await sink.CreateWriterAsync(context, restoredState: 10);
            
            int state1 = await writer.SnapshotStateAsync(1);
            int state2 = await writer.SnapshotStateAsync(2);
            int state3 = await writer.SnapshotStateAsync(3);

            // Assert - State should increment from initial value of 10
            Assert.That(state1, Is.EqualTo(10));
            Assert.That(state2, Is.EqualTo(11));
            Assert.That(state3, Is.EqualTo(12));
        }

        [Test]
        public async Task UnifiedSinkV2_SinkBuilder_CreatesWorkingSink()
        {
            // Arrange
            List<string> writtenElements = [];
            List<string> committedElements = [];

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((ctx, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(
                    new TestIntegrationWriter(writtenElements, state)))
                .WithCommitter(() => new TestIntegrationCommitter(committedElements))
                .Build();

            SinkWriterContext context = new() { SubtaskId = 0, NumberOfParallelSubtasks = 1 };
            ElementContext elemContext = new() { Timestamp = 1000, Watermark = 900 };

            // Act
            ISinkWriter<string, string, int> writer = await sink.CreateWriterAsync(context);
            await writer.WriteAsync("test1", elemContext);
            await writer.WriteAsync("test2", elemContext);
            
            List<string> committables = await writer.PrepareCommitAsync();
            ICommitter<string>? committer = sink.CreateCommitter();
            await committer!.CommitAsync(committables);

            // Assert
            Assert.That(writtenElements, Has.Count.EqualTo(2));
            Assert.That(committedElements, Has.Count.EqualTo(2));
        }

        [Test]
        public void UnifiedSinkV2_DataStreamIntegration_AcceptsSink()
        {
            // Arrange
            StreamExecutionEnvironment env = StreamExecutionEnvironment.GetExecutionEnvironment();
            DataStream<string> stream = env.FromCollection(new[] { "test1", "test2", "test3" });

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((ctx, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(
                    new TestIntegrationWriter([], state)))
                .Build();

            // Act
            DataStream<string> result = stream.AddSink(sink);

            // Assert
            Assert.That(result, Is.Not.Null, "AddSink should return a DataStream");
            Assert.That(result, Is.SameAs(stream), "Fluent API should return same stream");
        }

        [Test]
        public async Task UnifiedSinkV2_MultipleWritersParallel_IndependentState()
        {
            // Arrange - Simulate parallel subtasks
            TestIntegrationSink sink = new();
            
            SinkWriterContext context1 = new() { SubtaskId = 0, NumberOfParallelSubtasks = 2 };
            SinkWriterContext context2 = new() { SubtaskId = 1, NumberOfParallelSubtasks = 2 };

            // Act - Create two independent writers
            ISinkWriter<string, string, int> writer1 = await sink.CreateWriterAsync(context1, restoredState: 0);
            ISinkWriter<string, string, int> writer2 = await sink.CreateWriterAsync(context2, restoredState: 100);

            int state1 = await writer1.SnapshotStateAsync(1);
            int state2 = await writer2.SnapshotStateAsync(1);

            // Assert - Each writer should have independent state
            Assert.That(state1, Is.EqualTo(0), "Writer 1 state should start at 0");
            Assert.That(state2, Is.EqualTo(100), "Writer 2 state should start at 100");
        }

        [Test]
        public async Task UnifiedSinkV2_CommitterRetry_HandlesFailures()
        {
            // Arrange - Committer that fails first time but succeeds on retry
            List<string> committedElements = [];
            int commitAttempts = 0;

            ICommitter<string> committer = new RetryableCommitter(committedElements, attempts => commitAttempts = attempts);

            // Act - First commit attempt (will fail half the elements)
            List<string> failures1 = await committer.CommitAsync(["item1", "item2", "item3"]);

            // Retry failed items
            List<string> failures2 = await committer.CommitAsync(failures1);

            // Assert
            Assert.That(commitAttempts, Is.EqualTo(2), "Should have attempted commit twice");
            Assert.That(failures2, Is.Empty, "All items should succeed on retry");
            Assert.That(committedElements, Has.Count.GreaterThan(0), "Some elements should be committed");
        }

        // Helper class for retry test
        private class RetryableCommitter : ICommitter<string>
        {
            private readonly List<string> _committedElements;
            private int _attempts;
            private readonly Action<int> _reportAttempt;

            public RetryableCommitter(List<string> committedElements, Action<int> reportAttempt)
            {
                this._committedElements = committedElements;
                this._reportAttempt = reportAttempt;
            }

            public Task<List<string>> CommitAsync(List<string> committables, CancellationToken cancellationToken = default)
            {
                this._attempts++;
                this._reportAttempt(this._attempts);

                if (this._attempts == 1)
                {
                    // First attempt - commit half, fail half
                    int halfSize = committables.Count / 2;
                    this._committedElements.AddRange(committables.GetRange(0, halfSize));
                    return Task.FromResult(committables.GetRange(halfSize, committables.Count - halfSize));
                }
                else
                {
                    // Subsequent attempts - commit all
                    this._committedElements.AddRange(committables);
                    return Task.FromResult(new List<string>());
                }
            }

            public Task CloseAsync()
            {
                return Task.CompletedTask;
            }
        }
    }
}
