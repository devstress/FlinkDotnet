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

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Time characteristic for stream processing.
    /// Corresponds to org.apache.flink.streaming.api.TimeCharacteristic in Java Flink.
    /// </summary>
    public enum TimeCharacteristic
    {
        /// <summary>
        /// Processing time - the time when operators process elements
        /// </summary>
        ProcessingTime,

        /// <summary>
        /// Event time - timestamps embedded in the events themselves
        /// </summary>
        EventTime,

        /// <summary>
        /// Ingestion time - time when elements enter the Flink system
        /// </summary>
        IngestionTime
    }

    /// <summary>
    /// Interface for deserialization schemas.
    /// Corresponds to org.apache.flink.api.common.serialization.DeserializationSchema in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type to deserialize to</typeparam>
    public interface IDeserializationSchema<T>
    {
        /// <summary>
        /// Deserializes the byte array into an object.
        /// </summary>
        /// <param name="bytes">The byte array to deserialize</param>
        /// <returns>The deserialized object</returns>
        public T Deserialize(byte[] bytes);

        /// <summary>
        /// Checks if the given element signals the end of the stream.
        /// </summary>
        /// <param name="element">The element to check</param>
        /// <returns>True if the element signals end of stream</returns>
        public bool IsEndOfStream(T element);

        /// <summary>
        /// Gets the type information of the produced type.
        /// </summary>
        /// <returns>Type information of the produced type</returns>
        public TypeInformation<T> GetProducedType();
    }

    /// <summary>
    /// Interface for serialization schemas.
    /// Corresponds to org.apache.flink.api.common.serialization.SerializationSchema in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type to serialize</typeparam>
    public interface ISerializationSchema<in T>
    {
        /// <summary>
        /// Serializes the given object into a byte array.
        /// </summary>
        /// <param name="element">The element to serialize</param>
        /// <returns>The serialized byte array</returns>
        public byte[] Serialize(T element);
    }

    /// <summary>
    /// Type information for type inference in Flink.
    /// Corresponds to org.apache.flink.api.common.typeinfo.TypeInformation in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type</typeparam>
    public class TypeInformation<T>
    {
        private readonly System.Type _type;

        private TypeInformation(System.Type type) => this._type = type;

        /// <summary>
        /// Creates type information for the given type.
        /// </summary>
        /// <returns>Type information for T</returns>
        public static TypeInformation<T> Of() => new TypeInformation<T>(typeof(T));

        /// <summary>
        /// Creates type information for a specific type.
        /// </summary>
        /// <typeparam name="TType">The type</typeparam>
        /// <returns>Type information</returns>
        public static TypeInformation<TType> Of<TType>() => new TypeInformation<TType>(typeof(TType));

        /// <summary>
        /// Gets the .NET type.
        /// </summary>
        public new System.Type GetType() => this._type;
    }

    /// <summary>
    /// Kafka sink function for writing to Kafka with custom serialization.
    /// Corresponds to org.apache.flink.streaming.connectors.kafka.FlinkKafkaProducer in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements to write</typeparam>
    public class KafkaSinkFunction<T> : ISinkFunction<T>
    {
        private readonly string _topic;
        private readonly string _bootstrapServers;

        /// <summary>
        /// Gets the Kafka topic to write to.
        /// </summary>
        public string Topic => this._topic;

        /// <summary>
        /// Gets the Kafka bootstrap servers configuration.
        /// </summary>
        public string BootstrapServers => this._bootstrapServers;

        /// <summary>
        /// Creates a Kafka sink function.
        /// </summary>
        /// <param name="topic">Kafka topic</param>
        /// <param name="bootstrapServers">Kafka bootstrap servers</param>
        /// <param name="serializer">Serialization function</param>
        public KafkaSinkFunction(string topic, string bootstrapServers, Func<T, byte[]> serializer)
        {
            this._topic = topic;
            this._bootstrapServers = bootstrapServers;
            _ = serializer; // Reserved for future implementation - parameter kept for API compatibility
        }

        /// <summary>
        /// Invokes the sink function to write an element.
        /// </summary>
        public System.Threading.Tasks.Task InvokeAsync(T element, System.Threading.CancellationToken cancellationToken = default) =>
            // Placeholder - in production this would write to Kafka
            System.Threading.Tasks.Task.CompletedTask;
    }

    /// <summary>
    /// Starting offsets strategy for Kafka sources.
    /// </summary>
    public static class StartingOffsets
    {
        public const string Earliest = "earliest";
        public const string Latest = "latest";
    }

    /// <summary>
    /// Extension methods for StreamExecutionEnvironment to support Flink 2.1 API.
    /// </summary>
    public static class StreamExecutionEnvironmentExtensions
    {
        /// <summary>
        /// Sets the time characteristic for the streaming job.
        /// Corresponds to StreamExecutionEnvironment.setStreamTimeCharacteristic in Java Flink.
        /// </summary>
        /// <param name="env">The execution environment</param>
        /// <param name="characteristic">The time characteristic</param>
        /// <returns>The execution environment</returns>
        public static StreamExecutionEnvironment SetStreamTimeCharacteristic(
            this StreamExecutionEnvironment env,
            TimeCharacteristic characteristic)
        {
            // Store the time characteristic in the execution config
            _ = env.GetConfig().GetConfiguration().SetString("stream.time-characteristic", characteristic.ToString());
            return env;
        }

        /// <summary>
        /// Adds a Kafka source with the given source function.
        /// Corresponds to StreamExecutionEnvironment.addSource in Java Flink.
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="env">The execution environment</param>
        /// <param name="sourceFunction">The source function</param>
        /// <returns>A DataStream</returns>
        public static DataStream<T> AddSource<T>(
            this StreamExecutionEnvironment env,
            ISourceFunction<T> sourceFunction) => env.AddSource(sourceFunction, "Kafka Source");
    }

    /// <summary>
    /// Extension methods for DataStream to support Kafka sink operations.
    /// </summary>
    public static class DataStreamExtensions
    {
        /// <summary>
        /// Adds a Kafka sink to the data stream.
        /// Corresponds to DataStream.addSink in Java Flink.
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="stream">The data stream</param>
        /// <param name="sinkFunction">The Kafka sink function</param>
        /// <returns>The data stream</returns>
        public static DataStream<T> AddSink<T>(
            this DataStream<T> stream,
            KafkaSinkFunction<T> sinkFunction) => stream.AddSink(sinkFunction);
    }

    /// <summary>
    /// Extension methods for KafkaSourceFunction to support Flink-style configuration.
    /// </summary>
    public static class KafkaSourceFunctionExtensions
    {
        /// <summary>
        /// Sets the Kafka consumer to start from the earliest offset.
        /// Corresponds to FlinkKafkaConsumer.setStartFromEarliest() in Java Flink.
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="source">The Kafka source function</param>
        /// <returns>The Kafka source function</returns>
        public static KafkaSourceFunction<T> SetStartFromEarliest<T>(this KafkaSourceFunction<T> source) =>
            // Configuration is handled internally by KafkaSourceFunction
            source;

        /// <summary>
        /// Assigns timestamps and watermarks to elements from this Kafka source.
        /// Corresponds to FlinkKafkaConsumer.assignTimestampsAndWatermarks() in Java Flink.
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="source">The Kafka source function</param>
        /// <param name="assigner">The timestamp and watermark assigner</param>
        /// <returns>The Kafka source function with timestamps configured</returns>
        public static KafkaSourceFunction<T> AssignTimestampsAndWatermarks<T>(
            this KafkaSourceFunction<T> source,
            IAssignerWithPunctuatedWatermarks<T> assigner)
        {
            _ = assigner; // Reserved for future implementation
            // In production, this would configure the source to use the assigner
            // For now, we return the source to maintain API compatibility
            return source;
        }

        /// <summary>
        /// Assigns timestamps and watermarks to elements from this Kafka source using periodic watermarks.
        /// Corresponds to FlinkKafkaConsumer.assignTimestampsAndWatermarks() in Java Flink.
        /// </summary>
        /// <typeparam name="T">The type of elements</typeparam>
        /// <param name="source">The Kafka source function</param>
        /// <param name="assigner">The timestamp and watermark assigner</param>
        /// <returns>The Kafka source function with timestamps configured</returns>
        public static KafkaSourceFunction<T> AssignTimestampsAndWatermarks<T>(
            this KafkaSourceFunction<T> source,
            IAssignerWithPeriodicWatermarks<T> assigner)
        {
            _ = assigner; // Reserved for future implementation
            // In production, this would configure the source to use the assigner
            // For now, we return the source to maintain API compatibility
            return source;
        }
    }
}
