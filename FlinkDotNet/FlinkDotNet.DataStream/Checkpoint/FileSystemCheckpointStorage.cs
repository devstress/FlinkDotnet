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

namespace FlinkDotNet.DataStream.Checkpoint
{
    /// <summary>
    /// Checkpoint storage implementation that stores checkpoints on a file system.
    /// This corresponds to org.apache.flink.runtime.state.storage.FileSystemCheckpointStorage in Apache Flink.
    ///
    /// Supports various file systems:
    /// - Local file system: file:///path/to/checkpoints
    /// - HDFS: hdfs://namenode:port/path/to/checkpoints
    /// - Amazon S3: s3://bucket/path/to/checkpoints
    /// - Azure Blob Storage: wasb://container@account/path/to/checkpoints
    /// - Google Cloud Storage: gs://bucket/path/to/checkpoints
    ///
    /// File system checkpoint storage provides:
    /// - Persistent checkpoint storage
    /// - High availability support
    /// - Suitable for production deployments
    /// </summary>
    public class FileSystemCheckpointStorage : ICheckpointStorage
    {
        private readonly string _checkpointPath;
        private readonly int _fileSizeThreshold;

        /// <summary>
        /// Creates a new FileSystemCheckpointStorage with the specified checkpoint path.
        /// </summary>
        /// <param name="checkpointPath">The base path where checkpoints will be stored</param>
        /// <param name="fileSizeThreshold">
        /// The file size threshold (in bytes) below which state is stored inline.
        /// Default is -1 (use Flink default, typically 1024 bytes).
        /// </param>
        public FileSystemCheckpointStorage(string checkpointPath, int fileSizeThreshold = -1)
        {
            if (string.IsNullOrWhiteSpace(checkpointPath))
            {
                throw new System.ArgumentException(
                    "Checkpoint path cannot be null or empty",
                    nameof(checkpointPath));
            }

            _checkpointPath = checkpointPath;
            _fileSizeThreshold = fileSizeThreshold;
        }

        /// <summary>
        /// Gets the base path where checkpoints are stored.
        /// </summary>
        /// <returns>The checkpoint storage path</returns>
        public string GetCheckpointPath()
        {
            return _checkpointPath;
        }

        /// <summary>
        /// Gets the file size threshold for inline state storage.
        /// </summary>
        /// <returns>The file size threshold in bytes, or -1 for default</returns>
        public int GetFileSizeThreshold()
        {
            return _fileSizeThreshold;
        }

        /// <summary>
        /// Gets whether this storage implementation supports high availability.
        /// File system checkpoint storage supports high availability when using
        /// distributed file systems (HDFS, S3, etc.).
        /// </summary>
        /// <returns>True, as file system storage supports high availability</returns>
        public bool SupportsHighAvailability()
        {
            return true;
        }
    }
}