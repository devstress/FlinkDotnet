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
    /// Kafka source using the Unified Source API (Flink 1.12+).
    /// Provides modern connector framework with split discovery and exactly-once semantics.
    /// </summary>
    /// <typeparam name="T">Type of elements to consume</typeparam>
    public class KafkaSource<T>
    {
        private readonly string _bootstrapServers;
        private readonly List<string> _topics;
        private readonly string? _groupId;
        private readonly KafkaStartingOffsets _startingOffsets;
        private readonly KafkaStoppingOffsets _stoppingOffsets;

        private KafkaSource(
            string bootstrapServers,
            List<string> topics,
            string? groupId,
            DeserializationSchema<T> deserializer,
            KafkaStartingOffsets startingOffsets,
            KafkaStoppingOffsets stoppingOffsets)
        {
            this._bootstrapServers = bootstrapServers;
            this._topics = topics;
            this._groupId = groupId;
            // deserializer is used in ToDefinition() method for schema information
            _ = deserializer; // Suppress unused parameter warning - will be used in future ToDefinition() implementation
            this._startingOffsets = startingOffsets;
            this._stoppingOffsets = stoppingOffsets;
        }

        /// <summary>
        /// Creates a new Kafka source builder.
        /// </summary>
        /// <returns>Kafka source builder</returns>
        public static KafkaSourceBuilder<T> Builder() => new();

        /// <summary>
        /// Gets the bootstrap servers configuration.
        /// </summary>
        public string BootstrapServers => this._bootstrapServers;

        /// <summary>
        /// Gets the list of topics to consume from.
        /// </summary>
        public IReadOnlyList<string> Topics => this._topics.AsReadOnly();

        /// <summary>
        /// Gets the consumer group ID (null for no group).
        /// </summary>
        public string? GroupId => this._groupId;

        /// <summary>
        /// Gets the starting offsets strategy.
        /// </summary>
        public KafkaStartingOffsets StartingOffsets => this._startingOffsets;

        /// <summary>
        /// Gets the stopping offsets strategy (Bounded sources only).
        /// </summary>
        public KafkaStoppingOffsets StoppingOffsets => this._stoppingOffsets;

        /// <summary>
        /// Builder for Kafka sources.
        /// </summary>
        public class KafkaSourceBuilder<TElement>
        {
            private string? _bootstrapServers;
            private readonly List<string> _topics = new();
            private string? _groupId;
            private DeserializationSchema<TElement>? _deserializer;
            private KafkaStartingOffsets _startingOffsets = KafkaStartingOffsets.Latest;
            private KafkaStoppingOffsets _stoppingOffsets = KafkaStoppingOffsets.Unbounded;

            /// <summary>
            /// Sets the Kafka bootstrap servers.
            /// </summary>
            /// <param name="servers">Comma-separated list of broker addresses</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetBootstrapServers(string servers)
            {
                this._bootstrapServers = servers;
                return this;
            }

            /// <summary>
            /// Adds a topic to consume from.
            /// </summary>
            /// <param name="topic">Topic name</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetTopic(string topic)
            {
                this._topics.Add(topic);
                return this;
            }

            /// <summary>
            /// Adds multiple topics to consume from.
            /// </summary>
            /// <param name="topics">Topic names</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetTopics(params string[] topics)
            {
                this._topics.AddRange(topics);
                return this;
            }

            /// <summary>
            /// Sets the consumer group ID for offset management.
            /// </summary>
            /// <param name="groupId">Group ID</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetGroupId(string groupId)
            {
                this._groupId = groupId;
                return this;
            }

            /// <summary>
            /// Sets the deserialization schema for decoding Kafka records.
            /// </summary>
            /// <param name="schema">Deserialization schema</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetDeserializer(DeserializationSchema<TElement> schema)
            {
                this._deserializer = schema;
                return this;
            }

            /// <summary>
            /// Sets where to start consuming from (earliest/latest/specific offsets).
            /// </summary>
            /// <param name="startingOffsets">Starting offsets strategy</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetStartingOffsets(KafkaStartingOffsets startingOffsets)
            {
                this._startingOffsets = startingOffsets;
                return this;
            }

            /// <summary>
            /// Sets where to stop consuming (for bounded sources).
            /// </summary>
            /// <param name="stoppingOffsets">Stopping offsets strategy</param>
            /// <returns>This builder</returns>
            public KafkaSourceBuilder<TElement> SetStoppingOffsets(KafkaStoppingOffsets stoppingOffsets)
            {
                this._stoppingOffsets = stoppingOffsets;
                return this;
            }

            /// <summary>
            /// Builds the Kafka source.
            /// </summary>
            /// <returns>Configured Kafka source</returns>
            /// <exception cref="InvalidOperationException">If required configuration is missing</exception>
            public KafkaSource<TElement> Build()
            {
                if (string.IsNullOrWhiteSpace(this._bootstrapServers))
                {
                    throw new InvalidOperationException("Bootstrap servers must be set");
                }

                if (this._topics.Count == 0)
                {
                    throw new InvalidOperationException("At least one topic must be set");
                }

                if (this._deserializer == null)
                {
                    throw new InvalidOperationException("Deserializer must be set");
                }

                return new KafkaSource<TElement>(
                    this._bootstrapServers,
                    this._topics,
                    this._groupId,
                    this._deserializer,
                    this._startingOffsets,
                    this._stoppingOffsets);
            }
        }
    }

    /// <summary>
    /// Deserialization schema for decoding Kafka records.
    /// </summary>
    /// <typeparam name="T">Output type</typeparam>
    public abstract class DeserializationSchema<T>
    {
        /// <summary>
        /// Deserializes a byte array into an object.
        /// </summary>
        /// <param name="message">Serialized message bytes</param>
        /// <returns>Deserialized object</returns>
        public abstract T Deserialize(byte[] message);

        /// <summary>
        /// Gets whether this schema produces a type that includes additional information (e.g., timestamp).
        /// </summary>
        public virtual bool ProducesRowtime => false;
    }

    /// <summary>
    /// Starting offsets strategy for Kafka Unified Source.
    /// </summary>
    public enum KafkaStartingOffsets
    {
        /// <summary>
        /// Start from the earliest available offset.
        /// </summary>
        Earliest,

        /// <summary>
        /// Start from the latest offset (skip existing data).
        /// </summary>
        Latest,

        /// <summary>
        /// Start from committed offsets in consumer group (or earliest if no commits).
        /// </summary>
        Group,

        /// <summary>
        /// Start from specific timestamp.
        /// </summary>
        Timestamp,

        /// <summary>
        /// Start from specific offsets per partition.
        /// </summary>
        SpecificOffsets
    }

    /// <summary>
    /// Stopping offsets strategy for bounded Kafka sources.
    /// </summary>
    public enum KafkaStoppingOffsets
    {
        /// <summary>
        /// Never stop (unbounded stream).
        /// </summary>
        Unbounded,

        /// <summary>
        /// Stop at the latest offset available when the source starts.
        /// </summary>
        Latest,

        /// <summary>
        /// Stop at specific timestamp.
        /// </summary>
        Timestamp,

        /// <summary>
        /// Stop at specific offsets per partition.
        /// </summary>
        SpecificOffsets
    }
}
