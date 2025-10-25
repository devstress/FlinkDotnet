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
    /// A state backend that stores state in memory (on the Java heap).
    /// This corresponds to org.apache.flink.runtime.state.hashmap.HashMapStateBackend in Apache Flink.
    ///
    /// This state backend is suitable for:
    /// - Development and testing
    /// - Jobs with small state
    /// - Jobs that require very low latency state access
    ///
    /// Limitations:
    /// - State must fit in memory
    /// - Checkpoints are serialized and stored externally
    /// - No incremental checkpointing support
    /// </summary>
    public class HashMapStateBackend : IStateBackend
    {
        /// <summary>
        /// Creates a new HashMapStateBackend with default configuration.
        /// </summary>
        public HashMapStateBackend()
        {
        }

        /// <summary>
        /// Gets the name of this state backend.
        /// </summary>
        /// <returns>The state backend name</returns>
        public string GetName()
        {
            return "HashMapStateBackend";
        }

        /// <summary>
        /// Gets whether this state backend supports incremental checkpointing.
        /// HashMapStateBackend does not support incremental checkpointing.
        /// </summary>
        /// <returns>False, as incremental checkpointing is not supported</returns>
        public bool SupportsIncrementalCheckpointing()
        {
            return false;
        }
    }
}
