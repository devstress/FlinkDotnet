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
    /// A state backend defines how the state of a streaming application is stored and checkpointed.
    /// This corresponds to org.apache.flink.runtime.state.StateBackend in Apache Flink.
    /// <para>
    /// Different state backends store their state in different places:
    /// - HashMapStateBackend: Stores state in memory (on-heap)
    /// - EmbeddedRocksDBStateBackend: Stores state in RocksDB (off-heap, disk-backed)
    /// </para>
    /// </summary>
    public interface IStateBackend
    {
        /// <summary>
        /// Gets the name of this state backend for configuration and logging purposes.
        /// </summary>
        /// <returns>The state backend name</returns>
        public string GetName();

        /// <summary>
        /// Gets whether this state backend supports incremental checkpointing.
        /// Incremental checkpointing only stores the delta between consecutive checkpoints,
        /// rather than the full state.
        /// </summary>
        /// <returns>True if incremental checkpointing is supported</returns>
        public bool SupportsIncrementalCheckpointing();
    }
}
