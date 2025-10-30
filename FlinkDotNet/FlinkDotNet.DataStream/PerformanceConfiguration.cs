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

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// State backend configuration for Flink (Flink 2.1+).
    /// Provides advanced tuning for state management and checkpointing.
    /// </summary>
    public class StateBackendConfiguration
    {
        /// <summary>
        /// Gets the state backend type.
        /// </summary>
        public StateBackendType Backend { get; }

        /// <summary>
        /// Gets the checkpoint storage URI.
        /// </summary>
        public string? CheckpointStorageUri { get; }

        /// <summary>
        /// Gets RocksDB-specific configuration options.
        /// </summary>
        public RocksDBOptions? RocksDBOptions { get; }

        /// <summary>
        /// Gets whether incremental checkpoints are enabled.
        /// </summary>
        public bool IncrementalCheckpoints { get; }

        /// <summary>
        /// Gets additional state backend properties.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        private StateBackendConfiguration(
            StateBackendType backend,
            string? checkpointStorageUri,
            RocksDBOptions? rocksDBOptions,
            bool incrementalCheckpoints,
            Dictionary<string, string> properties)
        {
            this.Backend = backend;
            this.CheckpointStorageUri = checkpointStorageUri;
            this.RocksDBOptions = rocksDBOptions;
            this.IncrementalCheckpoints = incrementalCheckpoints;
            this.Properties = properties;
        }

        /// <summary>
        /// Creates a new state backend configuration builder.
        /// </summary>
        /// <returns>Configuration builder</returns>
        public static StateBackendConfigurationBuilder Builder() => new();

        /// <summary>
        /// Builder for state backend configuration.
        /// </summary>
        public class StateBackendConfigurationBuilder
        {
            private StateBackendType _backend = StateBackendType.HashMapStateBackend;
            private string? _checkpointStorageUri;
            private RocksDBOptions? _rocksDBOptions;
            private bool _incrementalCheckpoints;
            private readonly Dictionary<string, string> _properties = new();

            /// <summary>
            /// Sets the state backend type.
            /// </summary>
            /// <param name="backend">Backend type</param>
            /// <returns>This builder</returns>
            public StateBackendConfigurationBuilder SetBackend(StateBackendType backend)
            {
                this._backend = backend;
                return this;
            }

            /// <summary>
            /// Sets the checkpoint storage URI.
            /// </summary>
            /// <param name="uri">Storage URI (e.g., "file:///checkpoints", "s3://bucket/checkpoints")</param>
            /// <returns>This builder</returns>
            public StateBackendConfigurationBuilder SetCheckpointStorageUri(string uri)
            {
                this._checkpointStorageUri = uri;
                return this;
            }

            /// <summary>
            /// Sets RocksDB-specific options.
            /// </summary>
            /// <param name="options">RocksDB configuration</param>
            /// <returns>This builder</returns>
            public StateBackendConfigurationBuilder SetRocksDBOptions(RocksDBOptions options)
            {
                this._rocksDBOptions = options;
                return this;
            }

            /// <summary>
            /// Enables or disables incremental checkpoints.
            /// </summary>
            /// <param name="enabled">Whether to enable incremental checkpoints</param>
            /// <returns>This builder</returns>
            public StateBackendConfigurationBuilder SetIncrementalCheckpoints(bool enabled)
            {
                this._incrementalCheckpoints = enabled;
                return this;
            }

            /// <summary>
            /// Adds a custom property.
            /// </summary>
            /// <param name="key">Property key</param>
            /// <param name="value">Property value</param>
            /// <returns>This builder</returns>
            public StateBackendConfigurationBuilder AddProperty(string key, string value)
            {
                this._properties[key] = value;
                return this;
            }

            /// <summary>
            /// Builds the state backend configuration.
            /// </summary>
            /// <returns>Configured state backend</returns>
            public StateBackendConfiguration Build()
            {
                return new StateBackendConfiguration(
                    this._backend,
                    this._checkpointStorageUri,
                    this._rocksDBOptions,
                    this._incrementalCheckpoints,
                    this._properties);
            }
        }
    }

    /// <summary>
    /// State backend types supported by Flink.
    /// </summary>
    public enum StateBackendType
    {
        /// <summary>
        /// HashMap state backend (in-memory, heap-based).
        /// </summary>
        HashMapStateBackend,

        /// <summary>
        /// EmbeddedRocksDB state backend (disk-based with RocksDB).
        /// </summary>
        EmbeddedRocksDBStateBackend
    }

    /// <summary>
    /// RocksDB-specific configuration options for state backend.
    /// </summary>
    public class RocksDBOptions
    {
        /// <summary>
        /// Gets the maximum background jobs for RocksDB.
        /// </summary>
        public int? MaxBackgroundJobs { get; init; }

        /// <summary>
        /// Gets the maximum write buffer number.
        /// </summary>
        public int? MaxWriteBufferNumber { get; init; }

        /// <summary>
        /// Gets the write buffer size in bytes.
        /// </summary>
        public long? WriteBufferSize { get; init; }

        /// <summary>
        /// Gets the block cache size in bytes.
        /// </summary>
        public long? BlockCacheSize { get; init; }

        /// <summary>
        /// Gets whether to use bloom filters.
        /// </summary>
        public bool? UseBloomFilter { get; init; }

        /// <summary>
        /// Gets the compaction style.
        /// </summary>
        public string? CompactionStyle { get; init; }

        /// <summary>
        /// Gets additional RocksDB properties.
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Configuration for Smile format in compiled plans (Flink 2.1+).
    /// Smile is a binary JSON format for efficient plan serialization.
    /// </summary>
    public class SmileFormatConfiguration
    {
        /// <summary>
        /// Gets whether Smile format is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets the compression level (0-9).
        /// </summary>
        public int CompressionLevel { get; }

        /// <summary>
        /// Gets whether to use shared string values.
        /// </summary>
        public bool UseSharedStringValues { get; }

        /// <summary>
        /// Gets additional Smile format properties.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        private SmileFormatConfiguration(
            bool enabled,
            int compressionLevel,
            bool useSharedStringValues,
            Dictionary<string, string> properties)
        {
            this.Enabled = enabled;
            this.CompressionLevel = compressionLevel;
            this.UseSharedStringValues = useSharedStringValues;
            this.Properties = properties;
        }

        /// <summary>
        /// Creates a new Smile format configuration builder.
        /// </summary>
        /// <returns>Configuration builder</returns>
        public static SmileFormatConfigurationBuilder Builder() => new();

        /// <summary>
        /// Builder for Smile format configuration.
        /// </summary>
        public class SmileFormatConfigurationBuilder
        {
            private bool _enabled = true;
            private int _compressionLevel = 6;
            private bool _useSharedStringValues = true;
            private readonly Dictionary<string, string> _properties = new();

            /// <summary>
            /// Enables or disables Smile format.
            /// </summary>
            /// <param name="enabled">Whether to enable Smile format</param>
            /// <returns>This builder</returns>
            public SmileFormatConfigurationBuilder SetEnabled(bool enabled)
            {
                this._enabled = enabled;
                return this;
            }

            /// <summary>
            /// Sets the compression level (0-9).
            /// </summary>
            /// <param name="level">Compression level</param>
            /// <returns>This builder</returns>
            /// <exception cref="ArgumentOutOfRangeException">If level is not between 0 and 9</exception>
            public SmileFormatConfigurationBuilder SetCompressionLevel(int level)
            {
                if (level < 0 || level > 9)
                {
                    throw new ArgumentOutOfRangeException(nameof(level), "Compression level must be between 0 and 9");
                }

                this._compressionLevel = level;
                return this;
            }

            /// <summary>
            /// Sets whether to use shared string values.
            /// </summary>
            /// <param name="use">Whether to use shared strings</param>
            /// <returns>This builder</returns>
            public SmileFormatConfigurationBuilder SetUseSharedStringValues(bool use)
            {
                this._useSharedStringValues = use;
                return this;
            }

            /// <summary>
            /// Adds a custom property.
            /// </summary>
            /// <param name="key">Property key</param>
            /// <param name="value">Property value</param>
            /// <returns>This builder</returns>
            public SmileFormatConfigurationBuilder AddProperty(string key, string value)
            {
                this._properties[key] = value;
                return this;
            }

            /// <summary>
            /// Builds the Smile format configuration.
            /// </summary>
            /// <returns>Configured Smile format</returns>
            public SmileFormatConfiguration Build()
            {
                return new SmileFormatConfiguration(
                    this._enabled,
                    this._compressionLevel,
                    this._useSharedStringValues,
                    this._properties);
            }
        }
    }

    /// <summary>
    /// Configuration for multi-join optimization (Flink 2.1+).
    /// Provides hints for optimizing complex join operations.
    /// </summary>
    public class MultiJoinOptimizationConfiguration
    {
        /// <summary>
        /// Gets whether multi-join optimization is enabled.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Gets the join reorder strategy.
        /// </summary>
        public JoinReorderStrategy ReorderStrategy { get; }

        /// <summary>
        /// Gets the maximum number of joins to optimize together.
        /// </summary>
        public int MaxJoinsToOptimize { get; }

        /// <summary>
        /// Gets whether to use cost-based optimization.
        /// </summary>
        public bool UseCostBasedOptimization { get; }

        /// <summary>
        /// Gets additional optimization properties.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        private MultiJoinOptimizationConfiguration(
            bool enabled,
            JoinReorderStrategy reorderStrategy,
            int maxJoinsToOptimize,
            bool useCostBasedOptimization,
            Dictionary<string, string> properties)
        {
            this.Enabled = enabled;
            this.ReorderStrategy = reorderStrategy;
            this.MaxJoinsToOptimize = maxJoinsToOptimize;
            this.UseCostBasedOptimization = useCostBasedOptimization;
            this.Properties = properties;
        }

        /// <summary>
        /// Creates a new multi-join optimization configuration builder.
        /// </summary>
        /// <returns>Configuration builder</returns>
        public static MultiJoinOptimizationConfigurationBuilder Builder() => new();

        /// <summary>
        /// Builder for multi-join optimization configuration.
        /// </summary>
        public class MultiJoinOptimizationConfigurationBuilder
        {
            private bool _enabled = true;
            private JoinReorderStrategy _reorderStrategy = JoinReorderStrategy.LeftDeep;
            private int _maxJoinsToOptimize = 10;
            private bool _useCostBasedOptimization = true;
            private readonly Dictionary<string, string> _properties = new();

            /// <summary>
            /// Enables or disables multi-join optimization.
            /// </summary>
            /// <param name="enabled">Whether to enable optimization</param>
            /// <returns>This builder</returns>
            public MultiJoinOptimizationConfigurationBuilder SetEnabled(bool enabled)
            {
                this._enabled = enabled;
                return this;
            }

            /// <summary>
            /// Sets the join reorder strategy.
            /// </summary>
            /// <param name="strategy">Reorder strategy</param>
            /// <returns>This builder</returns>
            public MultiJoinOptimizationConfigurationBuilder SetReorderStrategy(JoinReorderStrategy strategy)
            {
                this._reorderStrategy = strategy;
                return this;
            }

            /// <summary>
            /// Sets the maximum number of joins to optimize together.
            /// </summary>
            /// <param name="max">Maximum joins</param>
            /// <returns>This builder</returns>
            /// <exception cref="ArgumentOutOfRangeException">If max is less than 2</exception>
            public MultiJoinOptimizationConfigurationBuilder SetMaxJoinsToOptimize(int max)
            {
                if (max < 2)
                {
                    throw new ArgumentOutOfRangeException(nameof(max), "Max joins must be at least 2");
                }

                this._maxJoinsToOptimize = max;
                return this;
            }

            /// <summary>
            /// Sets whether to use cost-based optimization.
            /// </summary>
            /// <param name="use">Whether to use cost-based optimization</param>
            /// <returns>This builder</returns>
            public MultiJoinOptimizationConfigurationBuilder SetUseCostBasedOptimization(bool use)
            {
                this._useCostBasedOptimization = use;
                return this;
            }

            /// <summary>
            /// Adds a custom property.
            /// </summary>
            /// <param name="key">Property key</param>
            /// <param name="value">Property value</param>
            /// <returns>This builder</returns>
            public MultiJoinOptimizationConfigurationBuilder AddProperty(string key, string value)
            {
                this._properties[key] = value;
                return this;
            }

            /// <summary>
            /// Builds the multi-join optimization configuration.
            /// </summary>
            /// <returns>Configured multi-join optimization</returns>
            public MultiJoinOptimizationConfiguration Build()
            {
                return new MultiJoinOptimizationConfiguration(
                    this._enabled,
                    this._reorderStrategy,
                    this._maxJoinsToOptimize,
                    this._useCostBasedOptimization,
                    this._properties);
            }
        }
    }

    /// <summary>
    /// Join reorder strategies for multi-join optimization.
    /// </summary>
    public enum JoinReorderStrategy
    {
        /// <summary>
        /// No reordering (use original join order).
        /// </summary>
        None,

        /// <summary>
        /// Left-deep tree strategy (linear join sequence).
        /// </summary>
        LeftDeep,

        /// <summary>
        /// Bushy tree strategy (balanced join tree).
        /// </summary>
        Bushy,

        /// <summary>
        /// Dynamic programming approach (optimal for small number of joins).
        /// </summary>
        DynamicProgramming
    }
}
