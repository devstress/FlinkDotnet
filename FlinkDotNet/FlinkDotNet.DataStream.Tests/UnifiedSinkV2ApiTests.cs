//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for Unified Sink API v2 (Flink 1.20+) interfaces and builder.
    /// These tests validate the C# API layer for the modern sink pattern.
    /// </summary>
    [TestFixture]
    public class UnifiedSinkV2ApiTests
    {
        // Test implementations for the Unified Sink v2 API

        private class TestSinkWriter : ISinkWriter<string, string, int>
        {
            public List<string> WrittenElements { get; } = [];
            public List<string> Committables { get; } = [];
            public int State { get; set; }
            public bool FlushCalled { get; set; }
            public bool DisposeCalled { get; set; }

            public Task WriteAsync(string element, ElementContext context, CancellationToken cancellationToken = default)
            {
                this.WrittenElements.Add(element);
                return Task.CompletedTask;
            }

            public Task FlushAsync(bool endOfInput, CancellationToken cancellationToken = default)
            {
                this.FlushCalled = true;
                return Task.CompletedTask;
            }

            public Task<List<string>> PrepareCommitAsync(CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.Committables);
            }

            public Task<int> SnapshotStateAsync(long checkpointId, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(this.State);
            }

            public ValueTask DisposeAsync()
            {
                this.DisposeCalled = true;
                return ValueTask.CompletedTask;
            }
        }

        private class TestCommitter : ICommitter<string>
        {
            public List<string> CommittedItems { get; } = [];
            public bool CloseCalled { get; set; }

            public Task<List<string>> CommitAsync(List<string> committables, CancellationToken cancellationToken = default)
            {
                this.CommittedItems.AddRange(committables);
                return Task.FromResult(new List<string>()); // No failures
            }

            public Task CloseAsync()
            {
                this.CloseCalled = true;
                return Task.CompletedTask;
            }
        }

        private class TestGlobalCommitter : IGlobalCommitter<string, string>
        {
            public List<string> CombinedItems { get; } = [];
            public List<string> CommittedItems { get; } = [];
            public bool CloseCalled { get; set; }

            public Task<List<string>> CombineAsync(List<string> committables, CancellationToken cancellationToken = default)
            {
                this.CombinedItems.AddRange(committables);
                return Task.FromResult(committables);
            }

            public Task<List<string>> CommitAsync(List<string> globalCommittables, CancellationToken cancellationToken = default)
            {
                this.CommittedItems.AddRange(globalCommittables);
                return Task.FromResult(new List<string>()); // No failures
            }

            public Task CloseAsync()
            {
                this.CloseCalled = true;
                return Task.CompletedTask;
            }
        }

        [Test]
        public async Task SinkWriter_WriteAsync_StoresElements()
        {
            // Arrange
            TestSinkWriter writer = new();
            ElementContext context = new() { Timestamp = 1000, Watermark = 900 };

            // Act
            await writer.WriteAsync("element1", context);
            await writer.WriteAsync("element2", context);

            // Assert
            Assert.That(writer.WrittenElements, Has.Count.EqualTo(2));
            Assert.That(writer.WrittenElements[0], Is.EqualTo("element1"));
            Assert.That(writer.WrittenElements[1], Is.EqualTo("element2"));
        }

        [Test]
        public async Task SinkWriter_FlushAsync_SetsFlagCorrectly()
        {
            // Arrange
            TestSinkWriter writer = new();

            // Act
            await writer.FlushAsync(false);

            // Assert
            Assert.That(writer.FlushCalled, Is.True);
        }

        [Test]
        public async Task SinkWriter_PrepareCommitAsync_ReturnsCommittables()
        {
            // Arrange
            TestSinkWriter writer = new();
            writer.Committables.Add("commit1");
            writer.Committables.Add("commit2");

            // Act
            List<string> result = await writer.PrepareCommitAsync();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Contains.Item("commit1"));
            Assert.That(result, Contains.Item("commit2"));
        }

        [Test]
        public async Task SinkWriter_SnapshotStateAsync_ReturnsState()
        {
            // Arrange
            TestSinkWriter writer = new() { State = 42 };

            // Act
            int state = await writer.SnapshotStateAsync(1000);

            // Assert
            Assert.That(state, Is.EqualTo(42));
        }

        [Test]
        public async Task SinkWriter_DisposeAsync_SetsFlagCorrectly()
        {
            // Arrange
            TestSinkWriter writer = new();

            // Act
            await writer.DisposeAsync();

            // Assert
            Assert.That(writer.DisposeCalled, Is.True);
        }

        [Test]
        public async Task Committer_CommitAsync_StoresCommittedItems()
        {
            // Arrange
            TestCommitter committer = new();
            List<string> committables = ["item1", "item2", "item3"];

            // Act
            List<string> failures = await committer.CommitAsync(committables);

            // Assert
            Assert.That(committer.CommittedItems, Has.Count.EqualTo(3));
            Assert.That(failures, Is.Empty);
        }

        [Test]
        public async Task Committer_CloseAsync_SetsFlagCorrectly()
        {
            // Arrange
            TestCommitter committer = new();

            // Act
            await committer.CloseAsync();

            // Assert
            Assert.That(committer.CloseCalled, Is.True);
        }

        [Test]
        public async Task GlobalCommitter_CombineAsync_StoresCommittables()
        {
            // Arrange
            TestGlobalCommitter committer = new();
            List<string> committables = ["item1", "item2"];

            // Act
            List<string> result = await committer.CombineAsync(committables);

            // Assert
            Assert.That(committer.CombinedItems, Has.Count.EqualTo(2));
            Assert.That(result, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GlobalCommitter_CommitAsync_StoresCommittedItems()
        {
            // Arrange
            TestGlobalCommitter committer = new();
            List<string> globalCommittables = ["global1", "global2"];

            // Act
            List<string> failures = await committer.CommitAsync(globalCommittables);

            // Assert
            Assert.That(committer.CommittedItems, Has.Count.EqualTo(2));
            Assert.That(failures, Is.Empty);
        }

        [Test]
        public async Task GlobalCommitter_CloseAsync_SetsFlagCorrectly()
        {
            // Arrange
            TestGlobalCommitter committer = new();

            // Act
            await committer.CloseAsync();

            // Assert
            Assert.That(committer.CloseCalled, Is.True);
        }

        [Test]
        public void SinkWriterContext_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            SinkWriterContext context = new()
            {
                SubtaskId = 2,
                NumberOfParallelSubtasks = 8,
                AttemptNumber = 1,
                Properties = new Dictionary<string, string> { { "key", "value" } }
            };

            // Assert
            Assert.That(context.SubtaskId, Is.EqualTo(2));
            Assert.That(context.NumberOfParallelSubtasks, Is.EqualTo(8));
            Assert.That(context.AttemptNumber, Is.EqualTo(1));
            Assert.That(context.Properties["key"], Is.EqualTo("value"));
        }

        [Test]
        public void ElementContext_InitializesPropertiesCorrectly()
        {
            // Arrange & Act
            ElementContext context = new()
            {
                Timestamp = 1234567890,
                Watermark = 1234567800,
                IsLastElement = true
            };

            // Assert
            Assert.That(context.Timestamp, Is.EqualTo(1234567890));
            Assert.That(context.Watermark, Is.EqualTo(1234567800));
            Assert.That(context.IsLastElement, Is.True);
        }

        [Test]
        public async Task SinkBuilder_WithWriter_BuildsSink()
        {
            // Arrange
            SinkBuilder<string, string, int> builder = new();
            bool writerCreated = false;

            // Act
            ISink<string, string, int> sink = builder
                .WithWriter((context, state, ct) =>
                {
                    writerCreated = true;
                    return Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter());
                })
                .Build();

            // Create writer to verify factory is called
            await sink.CreateWriterAsync(new SinkWriterContext());

            // Assert
            Assert.That(sink, Is.Not.Null);
            Assert.That(writerCreated, Is.True);
        }

        [Test]
        public void SinkBuilder_WithoutWriter_ThrowsException()
        {
            // Arrange
            SinkBuilder<string, string, int> builder = new();

            // Act & Assert
            InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex?.Message, Does.Contain("Writer factory must be set"));
        }

        [Test]
        public async Task SinkBuilder_WithCommitter_CreatesCommitter()
        {
            // Arrange
            bool committerCreated = false;

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter()))
                .WithCommitter(() =>
                {
                    committerCreated = true;
                    return new TestCommitter();
                })
                .Build();

            // Act
            ICommitter<string>? committer = sink.CreateCommitter();

            // Assert
            Assert.That(committer, Is.Not.Null);
            Assert.That(committerCreated, Is.True);
        }

        [Test]
        public void SinkBuilder_WithGlobalCommitter_CreatesGlobalCommitter()
        {
            // Arrange
            bool globalCommitterCreated = false;

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter()))
                .WithGlobalCommitter(() =>
                {
                    globalCommitterCreated = true;
                    return new TestGlobalCommitter();
                })
                .Build();

            // Act
            IGlobalCommitter<string, string>? globalCommitter = sink.CreateGlobalCommitter();

            // Assert
            Assert.That(globalCommitter, Is.Not.Null);
            Assert.That(globalCommitterCreated, Is.True);
        }

        [Test]
        public async Task SinkBuilder_FullWorkflow_CreatesAllComponents()
        {
            // Arrange
            TestSinkWriter writer = new();
            TestCommitter committer = new();
            TestGlobalCommitter globalCommitter = new();

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(writer))
                .WithCommitter(() => committer)
                .WithGlobalCommitter(() => globalCommitter)
                .Build();

            // Act
            ISinkWriter<string, string, int> createdWriter = await sink.CreateWriterAsync(new SinkWriterContext());
            ICommitter<string>? createdCommitter = sink.CreateCommitter();
            IGlobalCommitter<string, string>? createdGlobalCommitter = sink.CreateGlobalCommitter();

            // Assert
            Assert.That(createdWriter, Is.SameAs(writer));
            Assert.That(createdCommitter, Is.SameAs(committer));
            Assert.That(createdGlobalCommitter, Is.SameAs(globalCommitter));
        }

        [Test]
        public async Task DataStream_AddSink_WithUnifiedSinkV2_ReturnsDataStream()
        {
            // Arrange
            StreamExecutionEnvironment env = StreamExecutionEnvironment.GetExecutionEnvironment();
            DataStream<string> stream = env.FromCollection(new[] { "test" });
            
            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) => Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter()))
                .Build();

            // Act
            DataStream<string> result = stream.AddSink(sink);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.SameAs(stream)); // Fluent API returns same stream
        }

        [Test]
        public void DataStream_AddSink_WithNullSink_ThrowsArgumentNullException()
        {
            // Arrange
            StreamExecutionEnvironment env = StreamExecutionEnvironment.GetExecutionEnvironment();
            DataStream<string> stream = env.FromCollection(new[] { "test" });

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => stream.AddSink<string, int>(null!));
        }

        [Test]
        public async Task BuiltSink_CreateWriterAsync_WithRestoredState_PassesStateToFactory()
        {
            // Arrange
            int receivedState = -1;
            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) =>
                {
                    receivedState = state;
                    return Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter { State = state });
                })
                .Build();

            // Act
            await sink.CreateWriterAsync(new SinkWriterContext(), restoredState: 99);

            // Assert
            Assert.That(receivedState, Is.EqualTo(99));
        }

        [Test]
        public async Task BuiltSink_CreateWriterAsync_PassesCancellationToken()
        {
            // Arrange
            CancellationTokenSource cts = new();
            CancellationToken receivedToken = default;

            ISink<string, string, int> sink = new SinkBuilder<string, string, int>()
                .WithWriter((context, state, ct) =>
                {
                    receivedToken = ct;
                    return Task.FromResult<ISinkWriter<string, string, int>>(new TestSinkWriter());
                })
                .Build();

            // Act
            await sink.CreateWriterAsync(new SinkWriterContext(), cancellationToken: cts.Token);

            // Assert
            Assert.That(receivedToken, Is.EqualTo(cts.Token));
        }

        [Test]
        public void SinkBuilder_WithWriter_ThrowsOnNullFactory()
        {
            // Arrange
            SinkBuilder<string, string, int> builder = new();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithWriter(null!));
        }

        [Test]
        public void SinkBuilder_WithCommitter_ThrowsOnNullFactory()
        {
            // Arrange
            SinkBuilder<string, string, int> builder = new();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithCommitter(null!));
        }

        [Test]
        public void SinkBuilder_WithGlobalCommitter_ThrowsOnNullFactory()
        {
            // Arrange
            SinkBuilder<string, string, int> builder = new();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => builder.WithGlobalCommitter(null!));
        }
    }
}
