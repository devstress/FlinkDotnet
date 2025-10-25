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

namespace FlinkDotNet.DataStream.State
{
    /// <summary>
    /// Predefined RocksDB configuration options optimized for different workloads.
    /// Corresponds to org.apache.flink.contrib.streaming.state.PredefinedOptions in Apache Flink.
    /// </summary>
    public enum RocksDBPredefinedOptions
    {
        /// <summary>
        /// Default RocksDB configuration with balanced settings.
        /// </summary>
        DEFAULT,

        /// <summary>
        /// Configuration optimized for spinning disk storage.
        /// Uses larger write buffers and optimized compaction settings.
        /// </summary>
        SPINNING_DISK_OPTIMIZED,

        /// <summary>
        /// Configuration optimized for flash/SSD storage.
        /// Uses smaller write buffers and more aggressive compaction.
        /// </summary>
        FLASH_SSD_OPTIMIZED,

        /// <summary>
        /// Configuration optimized for spinning disk storage with larger state.
        /// Similar to SPINNING_DISK_OPTIMIZED but with even larger buffers.
        /// </summary>
        SPINNING_DISK_OPTIMIZED_HIGH_MEM
    }

    /// <summary>
    /// A state backend that stores state in an embedded RocksDB database.
    /// This corresponds to org.apache.flink.contrib.streaming.state.EmbeddedRocksDBStateBackend in Apache Flink.
    ///
    /// This state backend is suitable for:
    /// - Production deployments
    /// - Jobs with large state (larger than available memory)
    /// - Jobs requiring high throughput checkpointing
    ///
    /// Features:
    /// - State stored off-heap in RocksDB
    /// - Supports incremental checkpointing
    /// - State can exceed available memory
    /// - Persistent across restarts with checkpointing
    /// </summary>
    public class EmbeddedRocksDBStateBackend : IStateBackend
    {
        private RocksDBPredefinedOptions _predefinedOptions = RocksDBPredefinedOptions.DEFAULT;
        private string? _dbStoragePath;
        private bool _enableIncrementalCheckpointing = true;

        /// <summary>
        /// Creates a new EmbeddedRocksDBStateBackend with default configuration.
        /// </summary>
        public EmbeddedRocksDBStateBackend()
        {
        }

        /// <summary>
        /// Creates a new EmbeddedRocksDBStateBackend with incremental checkpointing configuration.
        /// </summary>
        /// <param name="enableIncrementalCheckpointing">Whether to enable incremental checkpointing</param>
        public EmbeddedRocksDBStateBackend(bool enableIncrementalCheckpointing)
        {
            _enableIncrementalCheckpointing = enableIncrementalCheckpointing;
        }

        /// <summary>
        /// Sets the predefined RocksDB configuration options.
        /// </summary>
        /// <param name="options">The predefined options to use</param>
        /// <returns>This EmbeddedRocksDBStateBackend instance for method chaining</returns>
        public EmbeddedRocksDBStateBackend SetPredefinedOptions(RocksDBPredefinedOptions options)
        {
            _predefinedOptions = options;
            return this;
        }

        /// <summary>
        /// Gets the configured predefined RocksDB options.
        /// </summary>
        /// <returns>The predefined options</returns>
        public RocksDBPredefinedOptions GetPredefinedOptions()
        {
            return _predefinedOptions;
        }

        /// <summary>
        /// Sets the local directory path where RocksDB stores its data files.
        /// If not set, RocksDB will use the system's temp directory.
        /// </summary>
        /// <param name="path">The local directory path for RocksDB storage</param>
        /// <returns>This EmbeddedRocksDBStateBackend instance for method chaining</returns>
        public EmbeddedRocksDBStateBackend SetDbStoragePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new System.ArgumentException("RocksDB storage path cannot be null or empty", nameof(path));
            }
            _dbStoragePath = path;
            return this;
        }

        /// <summary>
        /// Gets the configured RocksDB storage path.
        /// </summary>
        /// <returns>The storage path, or null if using default</returns>
        public string? GetDbStoragePath()
        {
            return _dbStoragePath;
        }

        /// <summary>
        /// Enables or disables incremental checkpointing.
        /// Incremental checkpointing only stores state changes since the last checkpoint,
        /// which can significantly reduce checkpoint times and storage for large state.
        /// </summary>
        /// <param name="enabled">Whether to enable incremental checkpointing</param>
        /// <returns>This EmbeddedRocksDBStateBackend instance for method chaining</returns>
        public EmbeddedRocksDBStateBackend EnableIncrementalCheckpointing(bool enabled = true)
        {
            _enableIncrementalCheckpointing = enabled;
            return this;
        }

        /// <summary>
        /// Gets whether incremental checkpointing is enabled.
        /// </summary>
        /// <returns>True if incremental checkpointing is enabled</returns>
        public bool IsIncrementalCheckpointingEnabled()
        {
            return _enableIncrementalCheckpointing;
        }

        /// <summary>
        /// Gets the name of this state backend.
        /// </summary>
        /// <returns>The state backend name</returns>
        public string GetName()
        {
            return "EmbeddedRocksDBStateBackend";
        }

        /// <summary>
        /// Gets whether this state backend supports incremental checkpointing.
        /// RocksDB state backend supports incremental checkpointing.
        /// </summary>
        /// <returns>True, as incremental checkpointing is supported</returns>
        public bool SupportsIncrementalCheckpointing()
        {
            return true;
        }
    }
}