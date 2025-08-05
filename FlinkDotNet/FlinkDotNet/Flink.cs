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

// Main entry point classes - expose the same API as Python Flink
using FlinkDotNet.Common;
using FlinkDotNet.DataStream;

// Backward compatibility - re-export FlinkJobBuilder
using Flink.JobBuilder;

namespace FlinkDotNet
{
    /// <summary>
    /// Main FlinkDotNet API providing both Python-compatible and backward-compatible interfaces.
    /// This provides the unified entry point matching the structure of PyFlink.
    /// </summary>
    public static class Flink
    {
        /// <summary>
        /// Gets the StreamExecutionEnvironment for DataStream API.
        /// This matches the Python pattern: StreamExecutionEnvironment.get_execution_environment()
        /// </summary>
        /// <param name="configuration">Optional configuration</param>
        /// <returns>StreamExecutionEnvironment instance</returns>
        public static StreamExecutionEnvironment GetExecutionEnvironment(Configuration? configuration = null)
        {
            return StreamExecutionEnvironment.GetExecutionEnvironment(configuration);
        }

        /// <summary>
        /// Creates a new Configuration object.
        /// This matches the Python pattern: Configuration()
        /// </summary>
        /// <returns>New Configuration instance</returns>
        public static Configuration CreateConfiguration()
        {
            return new Configuration();
        }

        /// <summary>
        /// Backward compatibility: Access to the original FlinkJobBuilder API.
        /// This preserves existing functionality while providing the new Python-aligned structure.
        /// </summary>
        public static class JobBuilder
        {
            /// <summary>
            /// Create a Kafka source for the streaming job (backward compatibility).
            /// </summary>
            /// <param name="topic">Kafka topic name</param>
            /// <param name="bootstrapServers">Kafka bootstrap servers</param>
            /// <returns>FlinkJobBuilder for method chaining</returns>
            public static FlinkJobBuilder FromKafka(string topic, string? bootstrapServers = null)
            {
                return FlinkJobBuilder.FromKafka(topic, bootstrapServers);
            }

            /// <summary>
            /// Create an HTTP source for REST API polling (backward compatibility).
            /// </summary>
            /// <param name="url">HTTP URL to poll</param>
            /// <param name="method">HTTP method</param>
            /// <param name="intervalSeconds">Polling interval</param>
            /// <returns>FlinkJobBuilder for method chaining</returns>
            public static FlinkJobBuilder FromHttp(string url, string method = "GET", int intervalSeconds = 60)
            {
                return FlinkJobBuilder.FromHttp(url, method, intervalSeconds);
            }

            /// <summary>
            /// Create a database source for polling queries (backward compatibility).
            /// </summary>
            /// <param name="connectionString">Database connection string</param>
            /// <param name="query">SQL query</param>
            /// <param name="pollingIntervalSeconds">Polling interval</param>
            /// <returns>FlinkJobBuilder for method chaining</returns>
            public static FlinkJobBuilder FromDatabase(string connectionString, string query, int pollingIntervalSeconds = 30)
            {
                return FlinkJobBuilder.FromDatabase(connectionString, query, pollingIntervalSeconds);
            }
        }
    }
}