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

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Unified Sink API v2 (Flink 1.20+) - Modern sink interface with exactly-once semantics support.
    /// This is the recommended API for implementing custom sinks, replacing the legacy ISinkFunction.
    /// </summary>
    /// <typeparam name="TInput">Type of elements to write</typeparam>
    /// <typeparam name="TCommittable">Type of committable objects for two-phase commit (use object if not needed)</typeparam>
    /// <typeparam name="TWriterState">Type of writer state for checkpointing (use object if stateless)</typeparam>
    public interface ISink<TInput, TCommittable, TWriterState>
    {
        /// <summary>
        /// Creates a new sink writer instance.
        /// </summary>
        /// <param name="context">Context providing runtime information</param>
        /// <param name="restoredState">Restored state from previous checkpoint (default value if no state to restore)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>A new sink writer instance</returns>
        Task<ISinkWriter<TInput, TCommittable, TWriterState>> CreateWriterAsync(
            SinkWriterContext context,
            TWriterState restoredState = default!,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a committer for exactly-once semantics (optional).
        /// Return null if the sink uses at-least-once semantics.
        /// </summary>
        /// <returns>Committer instance or null</returns>
        ICommitter<TCommittable>? CreateCommitter();

        /// <summary>
        /// Creates a global committer for exactly-once semantics (optional).
        /// Used for sinks requiring global coordination (e.g., file rename operations).
        /// Return null if not needed.
        /// </summary>
        /// <returns>Global committer instance or null</returns>
        IGlobalCommitter<TCommittable, TCommittable>? CreateGlobalCommitter();
    }

    /// <summary>
    /// Writer for processing elements in the Unified Sink v2 API.
    /// Writers are created per parallel instance and handle actual data writing.
    /// </summary>
    /// <typeparam name="TInput">Type of elements to write</typeparam>
    /// <typeparam name="TCommittable">Type of committable objects</typeparam>
    /// <typeparam name="TWriterState">Type of writer state for checkpointing</typeparam>
    public interface ISinkWriter<TInput, TCommittable, TWriterState> : IAsyncDisposable
    {
        /// <summary>
        /// Writes a single element to the sink.
        /// </summary>
        /// <param name="element">Element to write</param>
        /// <param name="context">Context with timestamp and watermark information</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task WriteAsync(TInput element, ElementContext context, CancellationToken cancellationToken = default);

        /// <summary>
        /// Flushes buffered data. Called before checkpoints and when the writer is closed.
        /// </summary>
        /// <param name="endOfInput">True if this is the final flush before the stream ends</param>
        /// <param name="cancellationToken">Cancellation token</param>
        Task FlushAsync(bool endOfInput, CancellationToken cancellationToken = default);

        /// <summary>
        /// Prepares commit for exactly-once semantics. Returns committables that will be committed
        /// only after the checkpoint completes successfully.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of committables or empty list if using at-least-once semantics</returns>
        Task<List<TCommittable>> PrepareCommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Snapshots the current state for checkpointing. Called during checkpoint creation.
        /// Return default value if stateless.
        /// </summary>
        /// <param name="checkpointId">Checkpoint identifier</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Current writer state or default if stateless</returns>
        Task<TWriterState> SnapshotStateAsync(long checkpointId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Committer for exactly-once semantics in Unified Sink v2.
    /// Commits the committables only after checkpoint completion.
    /// </summary>
    /// <typeparam name="TCommittable">Type of committable objects</typeparam>
    public interface ICommitter<TCommittable>
    {
        /// <summary>
        /// Commits the given committables after successful checkpoint.
        /// </summary>
        /// <param name="committables">List of committables to commit</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of committables that failed to commit (for retry)</returns>
        Task<List<TCommittable>> CommitAsync(List<TCommittable> committables, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the committer and releases resources.
        /// </summary>
        Task CloseAsync();
    }

    /// <summary>
    /// Global committer for sinks requiring global coordination (e.g., file sinks with rename operations).
    /// Receives all committables from all parallel instances and performs global commit operations.
    /// </summary>
    /// <typeparam name="TCommittable">Type of committable objects from writers</typeparam>
    /// <typeparam name="TGlobalCommittable">Type of global committable objects</typeparam>
    public interface IGlobalCommitter<TCommittable, TGlobalCommittable>
    {
        /// <summary>
        /// Combines committables from multiple writers into global committables.
        /// </summary>
        /// <param name="committables">Committables from all parallel writer instances</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Combined global committables</returns>
        Task<List<TGlobalCommittable>> CombineAsync(List<TCommittable> committables, CancellationToken cancellationToken = default);

        /// <summary>
        /// Performs the global commit operation.
        /// </summary>
        /// <param name="globalCommittables">Global committables to commit</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>List of global committables that failed (for retry)</returns>
        Task<List<TGlobalCommittable>> CommitAsync(List<TGlobalCommittable> globalCommittables, CancellationToken cancellationToken = default);

        /// <summary>
        /// Closes the global committer and releases resources.
        /// </summary>
        Task CloseAsync();
    }

    /// <summary>
    /// Context provided when creating a sink writer, containing runtime information.
    /// </summary>
    public class SinkWriterContext
    {
        /// <summary>
        /// Gets the subtask index (0-based) of this writer instance.
        /// </summary>
        public int SubtaskId { get; init; }

        /// <summary>
        /// Gets the total number of parallel writer instances.
        /// </summary>
        public int NumberOfParallelSubtasks { get; init; }

        /// <summary>
        /// Gets the attempt number for this writer (0 for first attempt, incremented on failures).
        /// </summary>
        public int AttemptNumber { get; init; }

        /// <summary>
        /// Gets custom properties from the job configuration.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; init; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// Context for each element being written, providing timestamp and watermark information.
    /// </summary>
    public class ElementContext
    {
        /// <summary>
        /// Gets the timestamp of the element (milliseconds since epoch).
        /// </summary>
        public long Timestamp { get; init; }

        /// <summary>
        /// Gets the current watermark (milliseconds since epoch).
        /// </summary>
        public long Watermark { get; init; }

        /// <summary>
        /// Gets whether this is the last element in the stream.
        /// </summary>
        public bool IsLastElement { get; init; }
    }

    /// <summary>
    /// Builder for creating Unified Sink v2 instances with a fluent API.
    /// </summary>
    /// <typeparam name="TInput">Type of elements to write</typeparam>
    /// <typeparam name="TCommittable">Type of committable objects</typeparam>
    /// <typeparam name="TWriterState">Type of writer state</typeparam>
    public class SinkBuilder<TInput, TCommittable, TWriterState>
    {
        private Func<SinkWriterContext, TWriterState, CancellationToken, Task<ISinkWriter<TInput, TCommittable, TWriterState>>>? _writerFactory;
        private Func<ICommitter<TCommittable>?>? _committerFactory;
        private Func<IGlobalCommitter<TCommittable, TCommittable>?>? _globalCommitterFactory;

        /// <summary>
        /// Sets the writer factory function.
        /// </summary>
        /// <param name="factory">Factory function to create sink writers</param>
        /// <returns>This builder for chaining</returns>
        public SinkBuilder<TInput, TCommittable, TWriterState> WithWriter(
            Func<SinkWriterContext, TWriterState, CancellationToken, Task<ISinkWriter<TInput, TCommittable, TWriterState>>> factory)
        {
            _writerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// Sets the committer factory function for exactly-once semantics.
        /// </summary>
        /// <param name="factory">Factory function to create committers</param>
        /// <returns>This builder for chaining</returns>
        public SinkBuilder<TInput, TCommittable, TWriterState> WithCommitter(
            Func<ICommitter<TCommittable>?> factory)
        {
            _committerFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// Sets the global committer factory function for global coordination.
        /// </summary>
        /// <param name="factory">Factory function to create global committers</param>
        /// <returns>This builder for chaining</returns>
        public SinkBuilder<TInput, TCommittable, TWriterState> WithGlobalCommitter(
            Func<IGlobalCommitter<TCommittable, TCommittable>?> factory)
        {
            _globalCommitterFactory = factory ?? throw new ArgumentNullException(nameof(factory));
            return this;
        }

        /// <summary>
        /// Builds the sink instance.
        /// </summary>
        /// <returns>Configured sink instance</returns>
        /// <exception cref="InvalidOperationException">If writer factory is not set</exception>
        public ISink<TInput, TCommittable, TWriterState> Build()
        {
            if (_writerFactory == null)
            {
                throw new InvalidOperationException("Writer factory must be set before building sink");
            }

            return new BuiltSink<TInput, TCommittable, TWriterState>(
                _writerFactory,
                _committerFactory,
                _globalCommitterFactory);
        }
    }

    /// <summary>
    /// Internal implementation of ISink created by SinkBuilder.
    /// </summary>
    internal class BuiltSink<TInput, TCommittable, TWriterState> : ISink<TInput, TCommittable, TWriterState>
    {
        private readonly Func<SinkWriterContext, TWriterState, CancellationToken, Task<ISinkWriter<TInput, TCommittable, TWriterState>>> _writerFactory;
        private readonly Func<ICommitter<TCommittable>?>? _committerFactory;
        private readonly Func<IGlobalCommitter<TCommittable, TCommittable>?>? _globalCommitterFactory;

        public BuiltSink(
            Func<SinkWriterContext, TWriterState, CancellationToken, Task<ISinkWriter<TInput, TCommittable, TWriterState>>> writerFactory,
            Func<ICommitter<TCommittable>?>? committerFactory,
            Func<IGlobalCommitter<TCommittable, TCommittable>?>? globalCommitterFactory)
        {
            _writerFactory = writerFactory ?? throw new ArgumentNullException(nameof(writerFactory));
            _committerFactory = committerFactory;
            _globalCommitterFactory = globalCommitterFactory;
        }

        public Task<ISinkWriter<TInput, TCommittable, TWriterState>> CreateWriterAsync(
            SinkWriterContext context,
            TWriterState? restoredState = default,
            CancellationToken cancellationToken = default)
        {
            return _writerFactory(context, restoredState, cancellationToken);
        }

        public ICommitter<TCommittable>? CreateCommitter()
        {
            return _committerFactory?.Invoke();
        }

        public IGlobalCommitter<TCommittable, TCommittable>? CreateGlobalCommitter()
        {
            return _globalCommitterFactory?.Invoke();
        }
    }
}
