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
    /// Checkpoint storage defines where completed checkpoints are persisted.
    /// This corresponds to org.apache.flink.runtime.state.storage.CheckpointStorage in Apache Flink.
    /// 
    /// Different checkpoint storage implementations:
    /// - FileSystemCheckpointStorage: Stores checkpoints on file system (local, HDFS, S3, etc.)
    /// - JobManagerCheckpointStorage: Stores small checkpoints in JobManager memory
    /// </summary>
    public interface ICheckpointStorage
    {
        /// <summary>
        /// Gets the base path where checkpoints are stored.
        /// </summary>
        /// <returns>The checkpoint storage path</returns>
        string GetCheckpointPath();

        /// <summary>
        /// Gets whether this storage implementation supports high availability.
        /// High availability requires persistent storage that survives JobManager failures.
        /// </summary>
        /// <returns>True if high availability is supported</returns>
        bool SupportsHighAvailability();
    }
}