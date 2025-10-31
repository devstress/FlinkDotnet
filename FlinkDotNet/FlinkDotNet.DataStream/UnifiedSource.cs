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
    /// Unified Source API (Flink 1.12+/FLIP-27) - Modern source connector framework.
    /// This is the recommended API for implementing custom sources, replacing the legacy SourceFunction.
    /// Supports both bounded and unbounded streams with split discovery and enumeration.
    /// </summary>
    /// <typeparam name="TOutput">Type of elements produced by the source</typeparam>
    /// <typeparam name="TSplit">Type of source splits (partitions/shards/segments)</typeparam>
    /// <typeparam name="TEnumState">Type of enumerator state for checkpointing</typeparam>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters - Matching Flink's native API design
    public interface ISource<TOutput, TSplit, TEnumState> where TSplit : ISourceSplit
#pragma warning restore S2436
    {
        /// <summary>
        /// Gets the boundedness of this source (bounded/unbounded).
        /// </summary>
        public Boundedness Boundedness { get; }

        /// <summary>
        /// Creates a split enumerator for discovering and assigning source splits to readers.
        /// </summary>
        /// <param name="context">Context providing runtime information</param>
        /// <param name="restoredState">Restored state from previous checkpoint (default if no state to restore)</param>
        /// <returns>A new split enumerator instance</returns>
        public ISplitEnumerator<TSplit, TEnumState> CreateEnumerator(
            SplitEnumeratorContext context,
            TEnumState restoredState = default!);

        /// <summary>
        /// Creates a split reader for reading data from assigned splits.
        /// </summary>
        /// <param name="context">Context providing runtime information</param>
        /// <returns>A new split reader instance</returns>
        public ISplitReader<TOutput, TSplit> CreateReader(SplitReaderContext context);

        /// <summary>
        /// Gets the simple state serializer for enumerator checkpointing.
        /// Can return null if the enumerator is stateless.
        /// </summary>
        /// <returns>State serializer or null</returns>
        public ISimpleVersionedSerializer<TEnumState>? GetEnumeratorCheckpointSerializer();

        /// <summary>
        /// Gets the simple state serializer for split checkpointing.
        /// </summary>
        /// <returns>State serializer for splits</returns>
        public ISimpleVersionedSerializer<TSplit> GetSplitSerializer();
    }

    /// <summary>
    /// Split enumerator responsible for discovering and assigning source splits to readers.
    /// Runs on the job manager with a single parallelism.
    /// </summary>
    /// <typeparam name="TSplit">Type of source splits</typeparam>
    /// <typeparam name="TEnumState">Type of enumerator state for checkpointing</typeparam>
    public interface ISplitEnumerator<TSplit, TEnumState> : IDisposable where TSplit : ISourceSplit
    {
        /// <summary>
        /// Starts the split enumerator. Called once after creation.
        /// </summary>
        public void Start();

        /// <summary>
        /// Handles requests for splits from readers.
        /// </summary>
        /// <param name="subtaskId">ID of the subtask requesting splits</param>
        /// <param name="hostname">Hostname where the reader is running</param>
        public void HandleSplitRequest(int subtaskId, string? hostname = null);

        /// <summary>
        /// Adds new splits back to the enumerator (e.g., when a reader fails).
        /// </summary>
        /// <param name="splits">Splits to add back</param>
        /// <param name="subtaskId">ID of the subtask that was processing these splits</param>
        public void AddSplitsBack(IList<TSplit> splits, int subtaskId);

        /// <summary>
        /// Snapshots the current state for checkpointing.
        /// </summary>
        /// <param name="checkpointId">Checkpoint ID</param>
        /// <returns>Current state snapshot</returns>
        public TEnumState SnapshotState(long checkpointId);

        /// <summary>
        /// Registers a split assignment event handler for sending splits to readers.
        /// </summary>
        /// <param name="splitAssignmentHandler">Handler for split assignments</param>
        public void RegisterSplitAssignmentHandler(Action<int, IList<TSplit>> splitAssignmentHandler);

        /// <summary>
        /// Registers a callback for no-more-splits notifications.
        /// </summary>
        /// <param name="noMoreSplitsHandler">Handler for no-more-splits events</param>
        public void RegisterNoMoreSplitsHandler(Action<int> noMoreSplitsHandler);
    }

    /// <summary>
    /// Split reader responsible for reading data from assigned source splits.
    /// Runs on task managers with parallelism matching the source's parallelism.
    /// </summary>
    /// <typeparam name="TOutput">Type of elements produced</typeparam>
    /// <typeparam name="TSplit">Type of source splits</typeparam>
    public interface ISplitReader<TOutput, TSplit> : IDisposable where TSplit : ISourceSplit
    {
        /// <summary>
        /// Assigns a new split to this reader.
        /// </summary>
        /// <param name="split">Split to read from</param>
        public void AddSplit(TSplit split);

        /// <summary>
        /// Fetches elements from assigned splits.
        /// </summary>
        /// <param name="output">Output collector for emitted elements</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the fetch operation</returns>
        public Task FetchAsync(ISourceOutput<TOutput> output, CancellationToken cancellationToken = default);

        /// <summary>
        /// Notifies the reader that no more splits will be assigned.
        /// </summary>
        public void NotifyNoMoreSplits();

        /// <summary>
        /// Gets the current state of splits being processed.
        /// </summary>
        /// <returns>List of splits with their current state</returns>
        public IList<TSplit> SnapshotState();
    }

    /// <summary>
    /// Base interface for source splits (partitions/shards/segments).
    /// </summary>
    public interface ISourceSplit
    {
        /// <summary>
        /// Gets the unique identifier for this split.
        /// </summary>
        public string SplitId { get; }
    }

    /// <summary>
    /// Output collector for source elements.
    /// </summary>
    /// <typeparam name="T">Type of elements</typeparam>
    public interface ISourceOutput<in T>
    {
        /// <summary>
        /// Collects an element with automatic timestamp extraction.
        /// </summary>
        /// <param name="element">Element to collect</param>
        public void Collect(T element);

        /// <summary>
        /// Collects an element with an explicit timestamp.
        /// </summary>
        /// <param name="element">Element to collect</param>
        /// <param name="timestamp">Timestamp in milliseconds</param>
        public void Collect(T element, long timestamp);

        /// <summary>
        /// Emits a watermark for event time processing.
        /// </summary>
        /// <param name="watermark">Watermark timestamp in milliseconds</param>
        public void EmitWatermark(long watermark);
    }

    /// <summary>
    /// Simple versioned serializer for split and state serialization.
    /// </summary>
    /// <typeparam name="T">Type to serialize</typeparam>
    public interface ISimpleVersionedSerializer<T>
    {
        /// <summary>
        /// Gets the version of this serializer.
        /// </summary>
        public int Version { get; }

        /// <summary>
        /// Serializes an object to bytes.
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Serialized bytes</returns>
        public byte[] Serialize(T obj);

        /// <summary>
        /// Deserializes an object from bytes.
        /// </summary>
        /// <param name="version">Serializer version used</param>
        /// <param name="bytes">Serialized bytes</param>
        /// <returns>Deserialized object</returns>
        public T Deserialize(int version, byte[] bytes);
    }

    /// <summary>
    /// Context for split enumerators providing runtime information.
    /// </summary>
    public class SplitEnumeratorContext
    {
        /// <summary>
        /// Gets the current parallelism of the source.
        /// </summary>
        public int CurrentParallelism { get; init; }

        /// <summary>
        /// Gets the metric group for the enumerator.
        /// </summary>
        public object? MetricGroup { get; init; }
    }

    /// <summary>
    /// Context for split readers providing runtime information.
    /// </summary>
    public class SplitReaderContext
    {
        /// <summary>
        /// Gets the index of this subtask.
        /// </summary>
        public int SubtaskIndex { get; init; }

        /// <summary>
        /// Gets the number of parallel subtasks.
        /// </summary>
        public int NumberOfParallelSubtasks { get; init; }

        /// <summary>
        /// Gets the metric group for the reader.
        /// </summary>
        public object? MetricGroup { get; init; }
    }

    /// <summary>
    /// Enum for source boundedness.
    /// </summary>
    public enum Boundedness
    {
        /// <summary>
        /// Bounded source (finite data set, e.g., file).
        /// </summary>
        Bounded,

        /// <summary>
        /// Unbounded source (infinite stream, e.g., Kafka).
        /// </summary>
        Unbounded
    }
}
