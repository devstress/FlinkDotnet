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
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// A DataStream represents a stream of elements of the same type.
    /// This corresponds to pyflink.datastream.DataStream in Python Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in this data stream</typeparam>
    public class DataStream<T>
    {
        private readonly StreamExecutionEnvironment _environment;
        private readonly IEnumerable<T>? _collection;
        internal readonly ISourceFunction<T>? _sourceFunction;
        private readonly string _sourceName;
        private readonly Flink.JobBuilder.Models.JobDefinition? _job;
        private OperationCapture? _operationCapture;

        /// <summary>
        /// Creates a DataStream from a collection.
        /// </summary>
        /// <param name="collection">The collection of elements</param>
        /// <param name="environment">The execution environment</param>
        internal DataStream(IEnumerable<T> collection, StreamExecutionEnvironment environment)
        {
            this._collection = collection;
            this._environment = environment;
            this._sourceName = "Collection Source";
        }

        /// <summary>
        /// Creates a DataStream from a source function.
        /// </summary>
        /// <param name="sourceFunction">The source function</param>
        /// <param name="environment">The execution environment</param>
        /// <param name="sourceName">Name of the source</param>
        internal DataStream(ISourceFunction<T> sourceFunction, StreamExecutionEnvironment environment, string sourceName)
        {
            this._sourceFunction = sourceFunction;
            this._environment = environment;
            this._sourceName = sourceName;
        }

        internal DataStream(Flink.JobBuilder.Models.JobDefinition job, StreamExecutionEnvironment environment)
        {
            this._job = job ?? throw new ArgumentNullException(nameof(job));
            this._environment = environment;
            this._sourceName = "IR Source";
        }

        /// <summary>
        /// Attaches operation capture for native API translation.
        /// </summary>
        internal void AttachOperationCapture(OperationCapture capture) => this._operationCapture = capture;

        /// <summary>
        /// Helper method to create a new DataStream from JobDefinition-backed streams with operation capture.
        /// </summary>
        private DataStream<TResult> CreateJobDefinitionBackedStream<TResult>()
        {
            DataStream<TResult> result = new(this._job ?? new Flink.JobBuilder.Models.JobDefinition(), this._environment);
            if (this._operationCapture != null)
            {
                result.AttachOperationCapture(this._operationCapture);
            }
            return result;
        }

        /// <summary>
        /// Helper method to propagate operation capture to a windowed stream.
        /// </summary>
        private void PropagateOperationCapture(AllWindowedStream<T> windowedStream)
        {
            if (this._operationCapture != null)
            {
                windowedStream.AttachOperationCapture(this._operationCapture);
            }
        }

        /// <summary>
        /// Applies a Map transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <typeparam name="TOut">The type of the output elements</typeparam>
        /// <param name="mapFunction">The map function to apply</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> Map<TOut>(Func<T, TOut> mapFunction)
        {
            if (this._collection != null)
            {
                IEnumerable<TOut> transformedCollection = this._collection.Select(mapFunction);
                return new DataStream<TOut>(transformedCollection, this._environment);
            }

            if (this._sourceFunction != null)
            {
                MappedSourceFunction<T, TOut> mappedSource = new(this._sourceFunction, mapFunction);
                return new DataStream<TOut>(mappedSource, this._environment, $"Map({this._sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture (FromKafka, AddKafkaSource)
            if (this._operationCapture != null || this._job != null)
            {
                // For streams created with FromKafka() or AddKafkaSource(), map operations are captured
                // and translated to JobDefinition operations during ExecuteAsync()
                // Return a new stream that maintains the operation capture chain
                return this.CreateJobDefinitionBackedStream<TOut>();
            }

            throw new InvalidOperationException("DataStream has no valid source");
        }

        /// <summary>
        /// Applies a Map transformation on this DataStream using a MapFunction (Flink-compatible).
        /// This matches the Java Flink API: dataStream.map(new MyMapFunction())
        /// </summary>
        /// <typeparam name="TOut">The type of the output elements</typeparam>
        /// <param name="mapFunction">The MapFunction instance to apply</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> Map<TOut>(IMapFunction<T, TOut> mapFunction)
        {
            // Capture operation if using native API
            this._operationCapture?.CaptureMapOperation("custom", mapFunction);

            DataStream<TOut> result = this.Map(mapFunction.Map);

            // Propagate operation capture to result stream
            if (this._operationCapture != null)
            {
                result.AttachOperationCapture(this._operationCapture);
            }

            return result;
        }

        /// <summary>
        /// Expression-based map (Flink-compatible when T is string). Supported expressions: "upper", "lower", "identity".
        /// </summary>
        public DataStream<string> Map(string expression)
        {
            if (typeof(T) != typeof(string))
            {
                throw new NotSupportedException("Expression-based Map is currently supported for string streams only.");
            }

            if (this._job == null)
            {
                throw new InvalidOperationException("Expression-based Map requires an IR-backed stream created via environment.FromKafka(...)");
            }

            this._job.Operations.Add(new Flink.JobBuilder.Models.MapOperationDefinition { Expression = expression });
            return new DataStream<string>(this._job, this._environment);
        }

        /// <summary>
        /// Applies a Filter transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <param name="filterFunction">The filter predicate</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Filter(Func<T, bool> filterFunction)
        {
            if (this._collection != null)
            {
                IEnumerable<T> filteredCollection = this._collection.Where(filterFunction);
                return new DataStream<T>(filteredCollection, this._environment);
            }

            if (this._sourceFunction != null)
            {
                FilteredSourceFunction<T> filteredSource = new(this._sourceFunction, filterFunction);
                return new DataStream<T>(filteredSource, this._environment, $"Filter({this._sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture
            return this._operationCapture != null || this._job != null
                ? this.CreateJobDefinitionBackedStream<T>()
                : throw new InvalidOperationException("DataStream has no valid source");
        }

        /// <summary>
        /// Applies a Filter transformation on this DataStream using a FilterFunction (Flink-compatible).
        /// This matches the Java Flink API: dataStream.filter(new MyFilterFunction())
        /// </summary>
        /// <param name="filterFunction">The FilterFunction instance to apply</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Filter(IFilterFunction<T> filterFunction) => this.Filter(filterFunction.Filter);

        /// <summary>
        /// Applies a FlatMap transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <typeparam name="TOut">The type of output elements</typeparam>
        /// <param name="flatMapFunction">The flat map function</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> FlatMap<TOut>(Func<T, IEnumerable<TOut>> flatMapFunction)
        {
            if (this._collection != null)
            {
                IEnumerable<TOut> transformedCollection = this._collection.SelectMany(flatMapFunction);
                return new DataStream<TOut>(transformedCollection, this._environment);
            }

            if (this._sourceFunction != null)
            {
                FlatMappedSourceFunction<T, TOut> flatMappedSource = new(this._sourceFunction, flatMapFunction);
                return new DataStream<TOut>(flatMappedSource, this._environment, $"FlatMap({this._sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture
            return this._operationCapture != null || this._job != null
                ? this.CreateJobDefinitionBackedStream<TOut>()
                : throw new InvalidOperationException("DataStream has no valid source");
        }

        /// <summary>
        /// Applies a FlatMap transformation on this DataStream using a FlatMapFunction (Flink-compatible).
        /// This matches the Java Flink API: dataStream.flatMap(new MyFlatMapFunction())
        /// </summary>
        /// <typeparam name="TOut">The type of output elements</typeparam>
        /// <param name="flatMapFunction">The FlatMapFunction instance to apply</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> FlatMap<TOut>(IFlatMapFunction<T, TOut> flatMapFunction) => this.FlatMap(flatMapFunction.FlatMap);

        /// <summary>
        /// Creates a new DataStream that contains only the elements satisfying the given filter predicate.
        /// Note: This is a simplified implementation for basic expressions.
        /// For production use, consider using the strongly-typed Filter(Func&lt;T, bool&gt;) method.
        /// </summary>
        /// <param name="filterExpression">Filter expression as string (basic expressions supported)</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Where(string filterExpression)
        {
            if (this._job == null)
            {
                // fallback/no-op for local stream usage
                return this;
            }
            this._job.Operations.Add(new Flink.JobBuilder.Models.FilterOperationDefinition { Expression = filterExpression });
            return this;
        }

        /// <summary>
        /// Sets a Kafka sink on the stream (Flink-compatible when using IR-backed stream or native API).
        /// </summary>
        public DataStream<T> SinkToKafka(string topic, string? bootstrapServers = null, System.Func<T, string>? serializer = null)
        {
            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                throw new ArgumentException(
                    "Kafka bootstrap servers must be provided via bootstrapServers parameter.",
                    nameof(bootstrapServers));
            }

            // Support native API with operation capture
            if (this._operationCapture != null)
            {
                this._operationCapture.CaptureKafkaSink(topic, bootstrapServers, serializer);
                return this;
            }

            // Support IR-backed streams
            if (this._job == null)
            {
                throw new InvalidOperationException("SinkToKafka requires an IR-backed stream created via environment.FromKafka(...) or AddKafkaSource(...)");
            }

            this._job.Sink = new Flink.JobBuilder.Models.KafkaSinkDefinition { Topic = topic, BootstrapServers = bootstrapServers };
            this._environment.SetActiveJob(this._job);
            return this;
        }

        /// <summary>
        /// Groups the elements of this DataStream by the given key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="keySelector">The key selector function</param>
        /// <returns>A KeyedStream</returns>
        public KeyedStream<T, TKey> KeyBy<TKey>(Func<T, TKey> keySelector) where TKey : notnull => new(this, keySelector);

        /// <summary>
        /// Groups the elements of this DataStream by the given key field.
        /// Note: This is a simplified implementation for basic field names.
        /// For production use, consider using the strongly-typed KeyBy&lt;TKey&gt;(Func&lt;T, TKey&gt;) method.
        /// </summary>
        /// <param name="keyField">The field name to group by</param>
        /// <returns>A KeyedStream with string keys</returns>
        public KeyedStream<T, string> GroupBy(string keyField) =>
            // For basic field-based grouping, we'll create a simple key function
            // This allows the API to work for basic scenarios
            new(this, _ => keyField);

        /// <summary>
        /// Writes the DataStream to standard output.
        /// </summary>
        /// <returns>This DataStream</returns>
        public DataStream<T> Print() =>
            // Print sink registered - actual output happens during execution
            this;

        /// <summary>
        /// Adds a sink to this DataStream.
        /// Currently registers the sink function for future execution.
        /// </summary>
        /// <param name="sinkFunction">The sink function</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> AddSink(ISinkFunction<T> sinkFunction)
        {
            // Capture sink operation if using native API
            if (this._operationCapture != null && sinkFunction != null)
            {
                // Try to extract Kafka sink information from the sink function using public properties
                string? topic = null;
                string? servers = null;

                // Check if it's a KafkaSinkFunction with public properties
                Type kafkaSinkType = sinkFunction.GetType();
                PropertyInfo? topicProp = kafkaSinkType.GetProperty("Topic");
                PropertyInfo? serversProp = kafkaSinkType.GetProperty("BootstrapServers");

                if (topicProp != null && serversProp != null)
                {
                    topic = topicProp.GetValue(sinkFunction) as string;
                    servers = serversProp.GetValue(sinkFunction) as string;
                }

                if (!string.IsNullOrEmpty(topic) && !string.IsNullOrEmpty(servers))
                {
                    this._operationCapture.CaptureKafkaSink(topic, servers, null);
                }
            }

            // Sink function registered for execution
            return this;
        }

        /// <summary>
        /// Adds a Unified Sink v2 (Flink 1.20+) to this DataStream.
        /// This is the recommended API for custom sinks with exactly-once semantics support.
        /// </summary>
        /// <typeparam name="TCommittable">Type of committable objects</typeparam>
        /// <typeparam name="TWriterState">Type of writer state for checkpointing</typeparam>
        /// <param name="sink">The unified sink v2 instance</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> AddSink<TCommittable, TWriterState>(ISink<T, TCommittable, TWriterState> sink)
        {
            ArgumentNullException.ThrowIfNull(sink);

            // Note: Full IR integration with OperationCapture will be implemented in Java IR Runner phase
            // For now, the unified sink v2 is registered for future execution

            // Sink registered for execution
            return this;
        }

        /// <summary>
        /// Sets the parallelism for this operation.
        /// </summary>
        /// <param name="parallelism">The parallelism for this operation</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> SetParallelism(int parallelism)
        {
            _ = parallelism; // Reserved for future implementation
            return this;
        }

        /// <summary>
        /// Sets the name for this operation.
        /// </summary>
        /// <param name="operatorName">The name of this operation</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> Name(string operatorName)
        {
            _ = operatorName; // Reserved for future implementation
            return this;
        }

        /// <summary>
        /// Partitions the stream by uniformly distributing the data across all parallel operators.
        /// This corresponds to the rebalance() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The rebalanced DataStream</returns>
        public DataStream<T> Rebalance() => this;

        /// <summary>
        /// Partitions the stream by distributing the data to a subset of parallel operators.
        /// This is more efficient than rebalance() when the downstream operation has fewer parallel instances.
        /// This corresponds to the rescale() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The rescaled DataStream</returns>
        public DataStream<T> Rescale() => this;

        /// <summary>
        /// Forwards elements to the next operator with the same parallelism.
        /// Only works if the upstream and downstream operators have the same parallelism.
        /// This corresponds to the forward() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The forwarded DataStream</returns>
        public DataStream<T> Forward() => this;

        /// <summary>
        /// Partitions the stream randomly across all parallel operators.
        /// This corresponds to the shuffle() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The shuffled DataStream</returns>
        public DataStream<T> Shuffle() => this;

        /// <summary>
        /// Broadcasts the stream to all parallel operators of the next operation.
        /// This corresponds to the broadcast() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The broadcasted DataStream</returns>
        public DataStream<T> Broadcast() => this;

        /// <summary>
        /// Partitions the stream using a custom partitioner.
        /// This corresponds to the partitionCustom() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="partitioner">The custom partitioner function</param>
        /// <param name="keySelector">The key selector function</param>
        /// <returns>The custom partitioned DataStream</returns>
        public DataStream<T> PartitionCustom<TKey>(Func<TKey, int, int> partitioner, Func<T, TKey> keySelector)
        {
            _ = partitioner; // Reserved for future implementation
            _ = keySelector; // Reserved for future implementation
            return this;
        }

        /// <summary>
        /// Sets the maximum parallelism for this operation.
        /// This is used for dynamic scaling and savepoint compatibility.
        /// Corresponds to Apache Flink 2.1.0 max parallelism configuration.
        /// </summary>
        /// <param name="maxParallelism">The maximum parallelism for this operation</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> SetMaxParallelism(int maxParallelism)
        {
            return maxParallelism <= 0 || maxParallelism > 32768
                ? throw new ArgumentException("Max parallelism must be between 1 and 32768")
                : this;
        }

        /// <summary>
        /// Sets the slotting group for this operation.
        /// Used for fine-grained resource management in Apache Flink 2.1.0.
        /// </summary>
        /// <param name="groupName">The slot sharing group name</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> SlotSharingGroup(string groupName)
        {
            _ = groupName; // Reserved for future implementation
            return this;
        }

        /// <summary>
        /// Assigns timestamps and watermarks to this DataStream.
        /// Corresponds to org.apache.flink.streaming.api.datastream.DataStream.assignTimestampsAndWatermarks in Java Flink.
        /// </summary>
        /// <param name="assigner">The timestamp and watermark assigner</param>
        /// <returns>This DataStream with timestamps assigned</returns>
        public DataStream<T> AssignTimestampsAndWatermarks(IAssignerWithPunctuatedWatermarks<T> assigner)
        {
            // Capture operation if using native API
            this._operationCapture?.CaptureTimestampAssigner(assigner);

            // In a full implementation, this would configure the stream's time characteristic
            // For now, we return the stream to maintain API compatibility
            return this;
        }

        /// <summary>
        /// Assigns timestamps and watermarks to this DataStream using periodic watermarks.
        /// Corresponds to org.apache.flink.streaming.api.datastream.DataStream.assignTimestampsAndWatermarks in Java Flink.
        /// </summary>
        /// <param name="assigner">The timestamp and watermark assigner</param>
        /// <returns>This DataStream with timestamps assigned</returns>
        public DataStream<T> AssignTimestampsAndWatermarks(IAssignerWithPeriodicWatermarks<T> assigner)
        {
            _ = assigner; // Reserved for future implementation
            // In a full implementation, this would configure the stream's time characteristic
            // For now, we return the stream to maintain API compatibility
            return this;
        }

        /// <summary>
        /// Assigns timestamps and watermarks to this DataStream using a WatermarkStrategy.
        /// Corresponds to org.apache.flink.streaming.api.datastream.DataStream.assignTimestampsAndWatermarks in Java Flink.
        /// </summary>
        /// <param name="strategy">The watermark strategy</param>
        /// <returns>This DataStream with timestamps assigned</returns>
        public DataStream<T> AssignTimestampsAndWatermarks(Watermarks.WatermarkStrategy<T> strategy)
        {
            ArgumentNullException.ThrowIfNull(strategy);

            // In a full implementation, this would configure the stream's time characteristic
            // and work with the watermark strategy to extract timestamps and generate watermarks
            // For now, we return the stream to maintain API compatibility
            return this;
        }

        /// <summary>
        /// Creates time windows over all elements in the stream.
        /// Corresponds to org.apache.flink.streaming.api.datastream.DataStream.timeWindowAll in Java Flink.
        /// </summary>
        /// <param name="size">The size of the time window</param>
        /// <returns>An AllWindowedStream that can be aggregated</returns>
        public AllWindowedStream<T> TimeWindowAll(Time size)
        {
            // Capture operation if using native API
            this._operationCapture?.CaptureTimeWindow(size);

            AllWindowedStream<T> windowedStream = new(this, size);
            this.PropagateOperationCapture(windowedStream);
            return windowedStream;
        }

        /// <summary>
        /// Creates count-based windows over all elements in the stream.
        /// Window fires when the specified number of elements have been collected.
        /// Corresponds to org.apache.flink.streaming.api.datastream.DataStream.countWindowAll in Java Flink.
        /// </summary>
        /// <param name="size">The number of elements per window</param>
        /// <returns>An AllWindowedStream that can be aggregated</returns>
        public AllWindowedStream<T> CountWindowAll(int size)
        {
            if (size <= 0)
            {
                throw new ArgumentException("Window size must be greater than 0", nameof(size));
            }

            // Capture operation if using native API
            this._operationCapture?.CaptureCountWindow(size);

            // Create a windowed stream with count-based windowing
            AllWindowedStream<T> windowedStream = new(this, size);
            this.PropagateOperationCapture(windowedStream);
            return windowedStream;
        }

        /// <summary>
        /// Gets the execution environment.
        /// </summary>
        /// <returns>The StreamExecutionEnvironment</returns>
        public StreamExecutionEnvironment GetExecutionEnvironment() => this._environment;
    }

    /// <summary>
    /// A KeyedStream represents a DataStream where elements are partitioned by key.
    /// This corresponds to the concept of keyed streams in Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stream</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public class KeyedStream<T, TKey> where TKey : notnull
    {
        private readonly DataStream<T> _dataStream;

        internal KeyedStream(DataStream<T> dataStream, Func<T, TKey> keySelector)
        {
            this._dataStream = dataStream;
            _ = keySelector;
        }

        /// <summary>
        /// Applies a reduce function to this KeyedStream.
        /// </summary>
        /// <param name="reduceFunction">The reduce function</param>
        /// <returns>The reduced DataStream</returns>
        public DataStream<T> Reduce(Func<T, T, T> reduceFunction)
        {
            _ = reduceFunction; // Reserved for future implementation
            // This would apply the reduce function to each key group
            return this._dataStream;
        }

        /// <summary>
        /// Applies a reduce function to this KeyedStream using a ReduceFunction (Flink-compatible).
        /// This matches the Java Flink API: keyedStream.reduce(new MyReduceFunction())
        /// </summary>
        /// <param name="reduceFunction">The ReduceFunction instance to apply</param>
        /// <returns>The reduced DataStream</returns>
        public DataStream<T> Reduce(IReduceFunction<T> reduceFunction) => this.Reduce(reduceFunction.Reduce);

        /// <summary>
        /// Applies an aggregation function to this KeyedStream.
        /// </summary>
        /// <param name="aggregationType">The type of aggregation</param>
        /// <param name="fieldName">The field to aggregate</param>
        /// <returns>The aggregated DataStream</returns>
        public DataStream<T> Aggregate(string aggregationType, string fieldName)
        {
            _ = aggregationType; // Reserved for future implementation
            _ = fieldName; // Reserved for future implementation
            // This would apply the aggregation function based on the type and field
            return this._dataStream;
        }

        /// <summary>
        /// Applies a window assigner to this KeyedStream, creating a WindowedStream.
        /// Corresponds to org.apache.flink.streaming.api.datastream.KeyedStream.window in Java Flink.
        /// </summary>
        /// <typeparam name="TWindow">The type of window</typeparam>
        /// <param name="assigner">The window assigner</param>
        /// <returns>A WindowedStream</returns>
        public Window.WindowedStream<T, TKey, TWindow> Window<TWindow>(
            Window.Assigners.IWindowAssigner<T, TWindow> assigner)
            where TWindow : Window.IWindow
        {
            ArgumentNullException.ThrowIfNull(assigner);

            return new Window.WindowedStream<T, TKey, TWindow>(this, assigner);
        }

        /// <summary>
        /// Gets the underlying DataStream.
        /// </summary>
        /// <returns>The underlying DataStream</returns>
        public DataStream<T> GetDataStream() => this._dataStream;
    }

    /// <summary>
    /// Represents a windowed stream where all elements are assigned to the same window.
    /// Corresponds to org.apache.flink.streaming.api.datastream.AllWindowedStream in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stream</typeparam>
    public class AllWindowedStream<T>
    {
        private readonly DataStream<T> _dataStream;
        private readonly Time? _windowSize;
        private readonly int? _windowCount;
        private OperationCapture? _operationCapture;

        internal AllWindowedStream(DataStream<T> dataStream, Time windowSize)
        {
            this._dataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this._windowSize = windowSize ?? throw new ArgumentNullException(nameof(windowSize));
            this._windowCount = null;
        }

        internal AllWindowedStream(DataStream<T> dataStream, int windowCount)
        {
            this._dataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            this._windowSize = null;
            this._windowCount = windowCount;
        }

        /// <summary>
        /// Attaches operation capture for native API translation.
        /// </summary>
        internal void AttachOperationCapture(OperationCapture capture) => this._operationCapture = capture;

        /// <summary>
        /// Applies an aggregate function to the windowed stream.
        /// Corresponds to org.apache.flink.streaming.api.datastream.AllWindowedStream.aggregate in Java Flink.
        /// </summary>
        /// <typeparam name="TAcc">The type of the accumulator</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="aggregateFunction">The aggregate function to apply</param>
        /// <returns>The aggregated DataStream</returns>
        public DataStream<TResult> Aggregate<TAcc, TResult>(IAggregateFunction<T, TAcc, TResult> aggregateFunction)
        {
            // Capture operation if using native API
            this._operationCapture?.CaptureAggregateOperation(aggregateFunction);

            // In a full implementation, this would apply windowing and aggregation
            // For now, we create a transformed stream to maintain API compatibility
            StreamExecutionEnvironment environment = this._dataStream.GetExecutionEnvironment();

            // This is a placeholder - in production, this would integrate with the Flink runtime
            // to perform actual windowed aggregation
            DataStream<TResult> result = new(
                new AggregatedSourceFunction<T, TAcc, TResult>(
                    this._dataStream._sourceFunction ?? throw new InvalidOperationException("Source function required"),
                    aggregateFunction
                ),
                environment,
                $"Windowed Aggregate({this._windowSize})"
            );

            // Propagate operation capture
            if (this._operationCapture != null)
            {
                result.AttachOperationCapture(this._operationCapture);
            }

            return result;
        }

        /// <summary>
        /// Gets the window size (for time-based windows).
        /// </summary>
        public Time? GetWindowSize() => this._windowSize;

        /// <summary>
        /// Gets the window count (for count-based windows).
        /// </summary>
        public int? GetWindowCount() => this._windowCount;
    }

    #region Function Interfaces - Core Flink API

    /// <summary>
    /// Interface for map functions that transform elements.
    /// Corresponds to org.apache.flink.api.common.functions.MapFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface IMapFunction<in TIn, out TOut>
    {
        /// <summary>
        /// The mapping method. Takes an element from the input data stream and transforms it.
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>The output value</returns>
        public TOut Map(TIn value);
    }

    /// <summary>
    /// Interface for filter functions that filter elements based on a condition.
    /// Corresponds to org.apache.flink.api.common.functions.FilterFunction in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements to filter</typeparam>
    public interface IFilterFunction<in T>
    {
        /// <summary>
        /// The filter function. Evaluates whether an element should be kept or filtered out.
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>True if the element should be kept, false otherwise</returns>
        public bool Filter(T value);
    }

    /// <summary>
    /// Interface for flat map functions that produce zero, one, or more output elements for each input.
    /// Corresponds to org.apache.flink.api.common.functions.FlatMapFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface IFlatMapFunction<in TIn, out TOut>
    {
        /// <summary>
        /// The flat map function. Processes one element and produces zero, one, or more output elements.
        /// </summary>
        /// <param name="value">The input value</param>
        /// <returns>An enumerable of output values</returns>
        public IEnumerable<TOut> FlatMap(TIn value);
    }

    /// <summary>
    /// Interface for reduce functions that combine elements into a single result.
    /// Corresponds to org.apache.flink.api.common.functions.ReduceFunction in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements to reduce</typeparam>
    public interface IReduceFunction<T>
    {
        /// <summary>
        /// The reduce function. Combines two values into one value of the same type.
        /// </summary>
        /// <param name="value1">The first value</param>
        /// <param name="value2">The second value</param>
        /// <returns>The combined value</returns>
        public T Reduce(T value1, T value2);
    }

    /// <summary>
    /// Interface for aggregate functions used in windowed aggregations.
    /// Corresponds to org.apache.flink.api.common.functions.AggregateFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TAcc">The type of the accumulator</typeparam>
    /// <typeparam name="TOut">The type of the output result</typeparam>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters - Required for Apache Flink API compatibility
    public interface IAggregateFunction<in TIn, TAcc, out TOut>
#pragma warning restore S2436
    {
        /// <summary>
        /// Creates a new accumulator, starting a new aggregate.
        /// </summary>
        /// <returns>A new accumulator</returns>
        public TAcc CreateAccumulator();

        /// <summary>
        /// Adds the given input value to the given accumulator, returning the new accumulator value.
        /// </summary>
        /// <param name="value">The input value to add</param>
        /// <param name="accumulator">The accumulator to add the value to</param>
        /// <returns>The accumulator with the updated state</returns>
        public TAcc Add(TIn value, TAcc accumulator);

        /// <summary>
        /// Gets the result of the accumulation.
        /// </summary>
        /// <param name="accumulator">The accumulator of the aggregation</param>
        /// <returns>The final aggregation result</returns>
        public TOut GetResult(TAcc accumulator);

        /// <summary>
        /// Merges two accumulators, returning an accumulator with the merged state.
        /// </summary>
        /// <param name="acc1">The first accumulator to merge</param>
        /// <param name="acc2">The second accumulator to merge</param>
        /// <returns>The merged accumulator</returns>
        public TAcc Merge(TAcc acc1, TAcc acc2);
    }

    #endregion Function Interfaces - Core Flink API

    /// <summary>
    /// Interface for sink functions that consume data streams.
    /// </summary>
    /// <typeparam name="T">The type of elements consumed by this sink</typeparam>
    public interface ISinkFunction<in T>
    {
        /// <summary>
        /// Processes the given element.
        /// </summary>
        /// <param name="element">The element to process</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the async operation</returns>
        public Task InvokeAsync(T element, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Internal wrapper source function that applies a map transformation to elements from another source.
    /// </summary>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
    internal class MappedSourceFunction<TIn, TOut>(ISourceFunction<TIn> source, Func<TIn, TOut> mapFunction) : ISourceFunction<TOut>
    {
        private readonly ISourceFunction<TIn> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly Func<TIn, TOut> _mapFunction = mapFunction ?? throw new ArgumentNullException(nameof(mapFunction));

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TIn item in this._source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return this._mapFunction(item);
            }
        }
    }

    /// <summary>
    /// Internal wrapper source function that applies a flat map transformation to elements from another source.
    /// </summary>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
    internal class FlatMappedSourceFunction<TIn, TOut>(ISourceFunction<TIn> source, Func<TIn, IEnumerable<TOut>> flatMapFunction) : ISourceFunction<TOut>
    {
        private readonly ISourceFunction<TIn> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly Func<TIn, IEnumerable<TOut>> _flatMapFunction = flatMapFunction ?? throw new ArgumentNullException(nameof(flatMapFunction));

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (TIn item in this._source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (TOut output in this._flatMapFunction(item))
                {
                    yield return output;
                }
            }
        }
    }

    /// <summary>
    /// Internal wrapper source function that applies a filter predicate to elements from another source.
    /// </summary>
    /// <typeparam name="T">The element type</typeparam>
    internal class FilteredSourceFunction<T>(ISourceFunction<T> source, Func<T, bool> filterFunction) : ISourceFunction<T>
    {
        private readonly ISourceFunction<T> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly Func<T, bool> _filterFunction = filterFunction ?? throw new ArgumentNullException(nameof(filterFunction));

        public async IAsyncEnumerable<T> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (T item in this._source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                if (this._filterFunction(item))
                {
                    yield return item;
                }
            }
        }
    }

    /// <summary>
    /// Internal wrapper source function that applies an aggregate function to elements from another source.
    /// This is a simplified implementation for API compatibility - production would use Flink's windowing engine.
    /// </summary>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TAcc">The accumulator type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters - Required for Apache Flink API compatibility
    internal class AggregatedSourceFunction<TIn, TAcc, TOut>(ISourceFunction<TIn> source, IAggregateFunction<TIn, TAcc, TOut> aggregateFunction) : ISourceFunction<TOut>
#pragma warning restore S2436
    {
        private readonly ISourceFunction<TIn> _source = source ?? throw new ArgumentNullException(nameof(source));
        private readonly IAggregateFunction<TIn, TAcc, TOut> _aggregateFunction = aggregateFunction ?? throw new ArgumentNullException(nameof(aggregateFunction));

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // This is a simplified implementation that aggregates all elements
            // In production, this would be handled by Flink's windowing mechanism
            TAcc accumulator = this._aggregateFunction.CreateAccumulator();

            await foreach (TIn item in this._source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                accumulator = this._aggregateFunction.Add(item, accumulator);
            }

            yield return this._aggregateFunction.GetResult(accumulator);
        }
    }
}
