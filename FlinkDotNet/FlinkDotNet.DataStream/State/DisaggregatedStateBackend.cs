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
    /// Storage type for disaggregated state backend.
    /// Corresponds to org.apache.flink.runtime.state.storage.StateStorageType in Apache Flink 2.0.
    /// </summary>
    public enum DisaggregatedStorageType
    {
        /// <summary>
        /// Store state in S3-compatible object storage.
        /// Optimized for cloud deployments with high scalability.
        /// </summary>
        S3,

        /// <summary>
        /// Store state in HDFS (Hadoop Distributed File System).
        /// Suitable for on-premise deployments.
        /// </summary>
        HDFS,

        /// <summary>
        /// Store state in Azure Blob Storage.
        /// Optimized for Azure cloud deployments.
        /// </summary>
        AZURE_BLOB,

        /// <summary>
        /// Store state in Google Cloud Storage.
        /// Optimized for Google Cloud deployments.
        /// </summary>
        GCS
    }

    /// <summary>
    /// A state backend that uses remote/disaggregated storage as primary state storage.
    /// This is the new default state backend introduced in Apache Flink 2.0.
    /// <para>
    /// Key features:
    /// - Decouples state storage from compute resources
    /// - Enables massive scalability (handles hundreds of TB of state)
    /// - Optimized for cloud-native and Kubernetes environments
    /// - Faster recovery and job rescaling
    /// - Reduces resource spikes during state operations
    /// </para>
    /// <para>
    /// This state backend is suitable for:
    /// - Cloud-native deployments (AWS, Azure, GCP)
    /// - Very large state (hundreds of TB)
    /// - Dynamic scaling requirements
    /// - Kubernetes-based Flink clusters
    /// </para>
    /// <para>
    /// Corresponds to org.apache.flink.runtime.state.disaggregated.DisaggregatedStateBackend
    /// in Apache Flink 2.0 and later.
    /// </para>
    /// </summary>
    public class DisaggregatedStateBackend : IStateBackend
    {
        private DisaggregatedStorageType _storageType = DisaggregatedStorageType.S3;
        private string? _storagePath;
        private bool _enableIncrementalCheckpointing = true;
        private bool _enableStateCompression = true;
        private int _asyncCompactionThreads = 4;

        /// <summary>
        /// Creates a new DisaggregatedStateBackend with default configuration (S3 storage).
        /// </summary>
        public DisaggregatedStateBackend()
        {
        }

        /// <summary>
        /// Creates a new DisaggregatedStateBackend with specified storage type.
        /// </summary>
        /// <param name="storageType">The type of remote storage to use</param>
        public DisaggregatedStateBackend(DisaggregatedStorageType storageType) => this._storageType = storageType;

        /// <summary>
        /// Creates a new DisaggregatedStateBackend with specified storage type and path.
        /// </summary>
        /// <param name="storageType">The type of remote storage to use</param>
        /// <param name="storagePath">The storage path (e.g., s3://bucket/path, hdfs://namenode/path)</param>
        public DisaggregatedStateBackend(DisaggregatedStorageType storageType, string storagePath)
        {
            this._storageType = storageType;
            this.SetStoragePath(storagePath);
        }

        /// <summary>
        /// Sets the storage type for the disaggregated state backend.
        /// </summary>
        /// <param name="storageType">The storage type to use</param>
        /// <returns>This DisaggregatedStateBackend instance for method chaining</returns>
        public DisaggregatedStateBackend SetStorageType(DisaggregatedStorageType storageType)
        {
            this._storageType = storageType;
            return this;
        }

        /// <summary>
        /// Gets the configured storage type.
        /// </summary>
        /// <returns>The storage type</returns>
        public DisaggregatedStorageType GetStorageType() => this._storageType;

        /// <summary>
        /// Sets the remote storage path where state data will be stored.
        /// Format depends on storage type:
        /// - S3: s3://bucket-name/path/to/state
        /// - HDFS: hdfs://namenode:port/path/to/state
        /// - Azure Blob: wasbs://container@account.blob.core.windows.net/path
        /// - GCS: gs://bucket-name/path/to/state
        /// </summary>
        /// <param name="path">The remote storage path</param>
        /// <returns>This DisaggregatedStateBackend instance for method chaining</returns>
        public DisaggregatedStateBackend SetStoragePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new System.ArgumentException("Storage path cannot be null or empty", nameof(path));
            }
            this._storagePath = path;
            return this;
        }

        /// <summary>
        /// Gets the configured remote storage path.
        /// </summary>
        /// <returns>The storage path, or null if not set</returns>
        public string? GetStoragePath() => this._storagePath;

        /// <summary>
        /// Enables or disables incremental checkpointing.
        /// Incremental checkpointing is highly recommended for disaggregated state
        /// as it minimizes data transfer to remote storage.
        /// </summary>
        /// <param name="enabled">Whether to enable incremental checkpointing</param>
        /// <returns>This DisaggregatedStateBackend instance for method chaining</returns>
        public DisaggregatedStateBackend EnableIncrementalCheckpointing(bool enabled = true)
        {
            this._enableIncrementalCheckpointing = enabled;
            return this;
        }

        /// <summary>
        /// Gets whether incremental checkpointing is enabled.
        /// </summary>
        /// <returns>True if incremental checkpointing is enabled</returns>
        public bool IsIncrementalCheckpointingEnabled() => this._enableIncrementalCheckpointing;

        /// <summary>
        /// Enables or disables state compression before storing in remote storage.
        /// Compression reduces storage costs and network bandwidth but adds CPU overhead.
        /// </summary>
        /// <param name="enabled">Whether to enable state compression</param>
        /// <returns>This DisaggregatedStateBackend instance for method chaining</returns>
        public DisaggregatedStateBackend EnableStateCompression(bool enabled = true)
        {
            this._enableStateCompression = enabled;
            return this;
        }

        /// <summary>
        /// Gets whether state compression is enabled.
        /// </summary>
        /// <returns>True if state compression is enabled</returns>
        public bool IsStateCompressionEnabled() => this._enableStateCompression;

        /// <summary>
        /// Sets the number of threads used for asynchronous state compaction.
        /// Higher values can improve throughput but consume more CPU.
        /// </summary>
        /// <param name="threads">Number of compaction threads (must be positive)</param>
        /// <returns>This DisaggregatedStateBackend instance for method chaining</returns>
        public DisaggregatedStateBackend SetAsyncCompactionThreads(int threads)
        {
            if (threads <= 0)
            {
                throw new System.ArgumentException($"Async compaction {nameof(threads)} must be positive", nameof(threads));
            }
            this._asyncCompactionThreads = threads;
            return this;
        }

        /// <summary>
        /// Gets the number of threads used for asynchronous state compaction.
        /// </summary>
        /// <returns>The number of compaction threads</returns>
        public int GetAsyncCompactionThreads() => this._asyncCompactionThreads;

        /// <summary>
        /// Gets the name of this state backend.
        /// </summary>
        /// <returns>The state backend name</returns>
        public string GetName() => "DisaggregatedStateBackend";

        /// <summary>
        /// Gets whether this state backend supports incremental checkpointing.
        /// Disaggregated state backend fully supports incremental checkpointing.
        /// </summary>
        /// <returns>True, as incremental checkpointing is supported</returns>
        public bool SupportsIncrementalCheckpointing() => true;
    }
}
