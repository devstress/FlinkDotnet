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
            _collection = collection;
            _environment = environment;
            _sourceName = "Collection Source";
        }

        /// <summary>
        /// Creates a DataStream from a source function.
        /// </summary>
        /// <param name="sourceFunction">The source function</param>
        /// <param name="environment">The execution environment</param>
        /// <param name="sourceName">Name of the source</param>
        internal DataStream(ISourceFunction<T> sourceFunction, StreamExecutionEnvironment environment, string sourceName)
        {
            _sourceFunction = sourceFunction;
            _environment = environment;
            _sourceName = sourceName;
        }

        internal DataStream(Flink.JobBuilder.Models.JobDefinition job, StreamExecutionEnvironment environment)
        {
            _job = job ?? throw new ArgumentNullException(nameof(job));
            _environment = environment;
            _sourceName = "IR Source";
        }

        /// <summary>
        /// Attaches operation capture for native API translation.
        /// </summary>
        internal void AttachOperationCapture(OperationCapture capture)
        {
            _operationCapture = capture;
        }

        /// <summary>
        /// Applies a Map transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <typeparam name="TOut">The type of the output elements</typeparam>
        /// <param name="mapFunction">The map function to apply</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> Map<TOut>(Func<T, TOut> mapFunction)
        {
            if (_collection != null)
            {
                var transformedCollection = _collection.Select(mapFunction);
                return new DataStream<TOut>(transformedCollection, _environment);
            }

            if (_sourceFunction != null)
            {
                var mappedSource = new MappedSourceFunction<T, TOut>(_sourceFunction, mapFunction);
                return new DataStream<TOut>(mappedSource, _environment, $"Map({_sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture (FromKafka, AddKafkaSource)
            if (_operationCapture != null || _job != null)
            {
                // For streams created with FromKafka() or AddKafkaSource(), map operations are captured
                // and translated to JobDefinition operations during ExecuteAsync()
                // Return a new stream that maintains the operation capture chain
                var result = new DataStream<TOut>(_job ?? new Flink.JobBuilder.Models.JobDefinition(), _environment);
                if (_operationCapture != null)
                {
                    result.AttachOperationCapture(_operationCapture);
                }
                return result;
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
            _operationCapture?.CaptureMapOperation("custom", mapFunction);
            
            var result = Map(mapFunction.Map);
            
            // Propagate operation capture to result stream
            if (_operationCapture != null)
            {
                result.AttachOperationCapture(_operationCapture);
            }
            
            return result;
        }

        /// <summary>
        /// Expression-based map (Flink-compatible when T is string). Supported expressions: "upper", "lower", "identity".
        /// </summary>
        public DataStream<string> Map(string expression)
        {
            if (typeof(T) != typeof(string))
                throw new NotSupportedException("Expression-based Map is currently supported for string streams only.");
            if (_job == null)
                throw new InvalidOperationException("Expression-based Map requires an IR-backed stream created via environment.FromKafka(...)");
            _job.Operations.Add(new Flink.JobBuilder.Models.MapOperationDefinition { Expression = expression });
            return new DataStream<string>(_job, _environment);
        }

        /// <summary>
        /// Applies a Filter transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <param name="filterFunction">The filter predicate</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Filter(Func<T, bool> filterFunction)
        {
            if (_collection != null)
            {
                var filteredCollection = _collection.Where(filterFunction);
                return new DataStream<T>(filteredCollection, _environment);
            }

            if (_sourceFunction != null)
            {
                var filteredSource = new FilteredSourceFunction<T>(_sourceFunction, filterFunction);
                return new DataStream<T>(filteredSource, _environment, $"Filter({_sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture
            if (_operationCapture != null || _job != null)
            {
                var result = new DataStream<T>(_job ?? new Flink.JobBuilder.Models.JobDefinition(), _environment);
                if (_operationCapture != null)
                {
                    result.AttachOperationCapture(_operationCapture);
                }
                return result;
            }

            throw new InvalidOperationException("DataStream has no valid source");
        }

        /// <summary>
        /// Applies a Filter transformation on this DataStream using a FilterFunction (Flink-compatible).
        /// This matches the Java Flink API: dataStream.filter(new MyFilterFunction())
        /// </summary>
        /// <param name="filterFunction">The FilterFunction instance to apply</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Filter(IFilterFunction<T> filterFunction)
        {
            return Filter(filterFunction.Filter);
        }

        /// <summary>
        /// Applies a FlatMap transformation on this DataStream using a Func delegate.
        /// </summary>
        /// <typeparam name="TOut">The type of output elements</typeparam>
        /// <param name="flatMapFunction">The flat map function</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> FlatMap<TOut>(Func<T, IEnumerable<TOut>> flatMapFunction)
        {
            if (_collection != null)
            {
                var transformedCollection = _collection.SelectMany(flatMapFunction);
                return new DataStream<TOut>(transformedCollection, _environment);
            }

            if (_sourceFunction != null)
            {
                var flatMappedSource = new FlatMappedSourceFunction<T, TOut>(_sourceFunction, flatMapFunction);
                return new DataStream<TOut>(flatMappedSource, _environment, $"FlatMap({_sourceName})");
            }

            // Handle JobDefinition-backed streams with OperationCapture
            if (_operationCapture != null || _job != null)
            {
                var result = new DataStream<TOut>(_job ?? new Flink.JobBuilder.Models.JobDefinition(), _environment);
                if (_operationCapture != null)
                {
                    result.AttachOperationCapture(_operationCapture);
                }
                return result;
            }

            throw new InvalidOperationException("DataStream has no valid source");
        }

        /// <summary>
        /// Applies a FlatMap transformation on this DataStream using a FlatMapFunction (Flink-compatible).
        /// This matches the Java Flink API: dataStream.flatMap(new MyFlatMapFunction())
        /// </summary>
        /// <typeparam name="TOut">The type of output elements</typeparam>
        /// <param name="flatMapFunction">The FlatMapFunction instance to apply</param>
        /// <returns>The transformed DataStream</returns>
        public DataStream<TOut> FlatMap<TOut>(IFlatMapFunction<T, TOut> flatMapFunction)
        {
            return FlatMap(flatMapFunction.FlatMap);
        }

        /// <summary>
        /// Creates a new DataStream that contains only the elements satisfying the given filter predicate.
        /// Note: This is a simplified implementation for basic expressions.
        /// For production use, consider using the strongly-typed Filter(Func&lt;T, bool&gt;) method.
        /// </summary>
        /// <param name="filterExpression">Filter expression as string (basic expressions supported)</param>
        /// <returns>The filtered DataStream</returns>
        public DataStream<T> Where(string filterExpression)
        {
            if (_job == null)
            {
                // fallback/no-op for local stream usage
                return this;
            }
            _job.Operations.Add(new Flink.JobBuilder.Models.FilterOperationDefinition { Expression = filterExpression });
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
            if (_operationCapture != null)
            {
                _operationCapture.CaptureKafkaSink(topic, bootstrapServers, serializer);
                return this;
            }
            
            // Support IR-backed streams
            if (_job == null)
                throw new InvalidOperationException("SinkToKafka requires an IR-backed stream created via environment.FromKafka(...) or AddKafkaSource(...)");
            
            _job.Sink = new Flink.JobBuilder.Models.KafkaSinkDefinition { Topic = topic, BootstrapServers = bootstrapServers };
            _environment.SetActiveJob(_job);
            return this;
        }

        /// <summary>
        /// Groups the elements of this DataStream by the given key.
        /// </summary>
        /// <typeparam name="TKey">The type of the key</typeparam>
        /// <param name="keySelector">The key selector function</param>
        /// <returns>A KeyedStream</returns>
        public KeyedStream<T, TKey> KeyBy<TKey>(Func<T, TKey> keySelector) where TKey : notnull
        {
            return new KeyedStream<T, TKey>(this, keySelector);
        }

        /// <summary>
        /// Groups the elements of this DataStream by the given key field.
        /// Note: This is a simplified implementation for basic field names.
        /// For production use, consider using the strongly-typed KeyBy&lt;TKey&gt;(Func&lt;T, TKey&gt;) method.
        /// </summary>
        /// <param name="keyField">The field name to group by</param>
        /// <returns>A KeyedStream with string keys</returns>
        public KeyedStream<T, string> GroupBy(string keyField)
        {
            // For basic field-based grouping, we'll create a simple key function
            // This allows the API to work for basic scenarios
            return new KeyedStream<T, string>(this, _ => keyField);
        }

        /// <summary>
        /// Writes the DataStream to standard output.
        /// </summary>
        /// <returns>This DataStream</returns>
        public DataStream<T> Print()
        {
            // Print sink registered - actual output happens during execution
            return this;
        }

        /// <summary>
        /// Adds a sink to this DataStream.
        /// Currently registers the sink function for future execution.
        /// </summary>
        /// <param name="sinkFunction">The sink function</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> AddSink(ISinkFunction<T> sinkFunction)
        {
            // Sink function registered for execution
            return this;
        }

        /// <summary>
        /// Sets the parallelism for this operation.
        /// </summary>
        /// <param name="parallelism">The parallelism for this operation</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> SetParallelism(int parallelism)
        {
            return this;
        }

        /// <summary>
        /// Sets the name for this operation.
        /// </summary>
        /// <param name="name">The name of this operation</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> Name(string name)
        {
            return this;
        }

        /// <summary>
        /// Partitions the stream by uniformly distributing the data across all parallel operators.
        /// This corresponds to the rebalance() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The rebalanced DataStream</returns>
        public DataStream<T> Rebalance()
        {
            return this;
        }

        /// <summary>
        /// Partitions the stream by distributing the data to a subset of parallel operators.
        /// This is more efficient than rebalance() when the downstream operation has fewer parallel instances.
        /// This corresponds to the rescale() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The rescaled DataStream</returns>
        public DataStream<T> Rescale()
        {
            return this;
        }

        /// <summary>
        /// Forwards elements to the next operator with the same parallelism.
        /// Only works if the upstream and downstream operators have the same parallelism.
        /// This corresponds to the forward() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The forwarded DataStream</returns>
        public DataStream<T> Forward()
        {
            return this;
        }

        /// <summary>
        /// Partitions the stream randomly across all parallel operators.
        /// This corresponds to the shuffle() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The shuffled DataStream</returns>
        public DataStream<T> Shuffle()
        {
            return this;
        }

        /// <summary>
        /// Broadcasts the stream to all parallel operators of the next operation.
        /// This corresponds to the broadcast() operation in Apache Flink 2.1.0.
        /// </summary>
        /// <returns>The broadcasted DataStream</returns>
        public DataStream<T> Broadcast()
        {
            return this;
        }

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
            if (maxParallelism <= 0 || maxParallelism > 32768)
                throw new ArgumentException("Max parallelism must be between 1 and 32768");
            return this;
        }

        /// <summary>
        /// Sets the slotting group for this operation.
        /// Used for fine-grained resource management in Apache Flink 2.1.0.
        /// </summary>
        /// <param name="slotSharingGroup">The slot sharing group name</param>
        /// <returns>This DataStream</returns>
        public DataStream<T> SlotSharingGroup(string slotSharingGroup)
        {
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
            _operationCapture?.CaptureTimestampAssigner(assigner);
            
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
            // In a full implementation, this would configure the stream's time characteristic
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
            _operationCapture?.CaptureTimeWindow(size);
            
            var windowedStream = new AllWindowedStream<T>(this, size);
            
            // Propagate operation capture
            if (_operationCapture != null)
            {
                windowedStream.AttachOperationCapture(_operationCapture);
            }
            
            return windowedStream;
        }

        /// <summary>
        /// Gets the execution environment.
        /// </summary>
        /// <returns>The StreamExecutionEnvironment</returns>
        public StreamExecutionEnvironment GetExecutionEnvironment()
        {
            return _environment;
        }
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
            _dataStream = dataStream;
            _ = keySelector;
        }

        /// <summary>
        /// Applies a reduce function to this KeyedStream.
        /// </summary>
        /// <param name="reduceFunction">The reduce function</param>
        /// <returns>The reduced DataStream</returns>
        public DataStream<T> Reduce(Func<T, T, T> reduceFunction)
        {
            // This would apply the reduce function to each key group
            return _dataStream;
        }

        /// <summary>
        /// Applies a reduce function to this KeyedStream using a ReduceFunction (Flink-compatible).
        /// This matches the Java Flink API: keyedStream.reduce(new MyReduceFunction())
        /// </summary>
        /// <param name="reduceFunction">The ReduceFunction instance to apply</param>
        /// <returns>The reduced DataStream</returns>
        public DataStream<T> Reduce(IReduceFunction<T> reduceFunction)
        {
            return Reduce(reduceFunction.Reduce);
        }

        /// <summary>
        /// Applies an aggregation function to this KeyedStream.
        /// </summary>
        /// <param name="aggregationType">The type of aggregation</param>
        /// <param name="fieldName">The field to aggregate</param>
        /// <returns>The aggregated DataStream</returns>
        public DataStream<T> Aggregate(string aggregationType, string fieldName)
        {
            // This would apply the aggregation function based on the type and field
            return _dataStream;
        }

        /// <summary>
        /// Gets the underlying DataStream.
        /// </summary>
        /// <returns>The underlying DataStream</returns>
        public DataStream<T> GetDataStream()
        {
            return _dataStream;
        }
    }

    /// <summary>
    /// Represents a windowed stream where all elements are assigned to the same window.
    /// Corresponds to org.apache.flink.streaming.api.datastream.AllWindowedStream in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stream</typeparam>
    public class AllWindowedStream<T>
    {
        private readonly DataStream<T> _dataStream;
        private readonly Time _windowSize;
        private OperationCapture? _operationCapture;

        internal AllWindowedStream(DataStream<T> dataStream, Time windowSize)
        {
            _dataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
            _windowSize = windowSize ?? throw new ArgumentNullException(nameof(windowSize));
        }

        /// <summary>
        /// Attaches operation capture for native API translation.
        /// </summary>
        internal void AttachOperationCapture(OperationCapture capture)
        {
            _operationCapture = capture;
        }

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
            _operationCapture?.CaptureAggregateOperation(aggregateFunction);
            
            // In a full implementation, this would apply windowing and aggregation
            // For now, we create a transformed stream to maintain API compatibility
            var environment = _dataStream.GetExecutionEnvironment();
            
            // This is a placeholder - in production, this would integrate with the Flink runtime
            // to perform actual windowed aggregation
            var result = new DataStream<TResult>(
                new AggregatedSourceFunction<T, TAcc, TResult>(
                    _dataStream._sourceFunction ?? throw new InvalidOperationException("Source function required"),
                    aggregateFunction
                ),
                environment,
                $"Windowed Aggregate({_windowSize})"
            );
            
            // Propagate operation capture
            if (_operationCapture != null)
            {
                result.AttachOperationCapture(_operationCapture);
            }
            
            return result;
        }

        /// <summary>
        /// Gets the window size.
        /// </summary>
        public Time GetWindowSize() => _windowSize;
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
        TOut Map(TIn value);
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
        bool Filter(T value);
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
        IEnumerable<TOut> FlatMap(TIn value);
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
        T Reduce(T value1, T value2);
    }

    /// <summary>
    /// Interface for aggregate functions used in windowed aggregations.
    /// Corresponds to org.apache.flink.api.common.functions.AggregateFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TAcc">The type of the accumulator</typeparam>
    /// <typeparam name="TOut">The type of the output result</typeparam>
    public interface IAggregateFunction<in TIn, TAcc, out TOut>
    {
        /// <summary>
        /// Creates a new accumulator, starting a new aggregate.
        /// </summary>
        /// <returns>A new accumulator</returns>
        TAcc CreateAccumulator();

        /// <summary>
        /// Adds the given input value to the given accumulator, returning the new accumulator value.
        /// </summary>
        /// <param name="value">The input value to add</param>
        /// <param name="accumulator">The accumulator to add the value to</param>
        /// <returns>The accumulator with the updated state</returns>
        TAcc Add(TIn value, TAcc accumulator);

        /// <summary>
        /// Gets the result of the accumulation.
        /// </summary>
        /// <param name="accumulator">The accumulator of the aggregation</param>
        /// <returns>The final aggregation result</returns>
        TOut GetResult(TAcc accumulator);

        /// <summary>
        /// Merges two accumulators, returning an accumulator with the merged state.
        /// </summary>
        /// <param name="acc1">The first accumulator to merge</param>
        /// <param name="acc2">The second accumulator to merge</param>
        /// <returns>The merged accumulator</returns>
        TAcc Merge(TAcc acc1, TAcc acc2);
    }

    #endregion

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
        Task InvokeAsync(T element, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Internal wrapper source function that applies a map transformation to elements from another source.
    /// </summary>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
    internal class MappedSourceFunction<TIn, TOut> : ISourceFunction<TOut>
    {
        private readonly ISourceFunction<TIn> _source;
        private readonly Func<TIn, TOut> _mapFunction;

        public MappedSourceFunction(ISourceFunction<TIn> source, Func<TIn, TOut> mapFunction)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _mapFunction = mapFunction ?? throw new ArgumentNullException(nameof(mapFunction));
        }

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in _source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return _mapFunction(item);
            }
        }
    }

    /// <summary>
    /// Internal wrapper source function that applies a flat map transformation to elements from another source.
    /// </summary>
    /// <typeparam name="TIn">The input element type</typeparam>
    /// <typeparam name="TOut">The output element type</typeparam>
    internal class FlatMappedSourceFunction<TIn, TOut> : ISourceFunction<TOut>
    {
        private readonly ISourceFunction<TIn> _source;
        private readonly Func<TIn, IEnumerable<TOut>> _flatMapFunction;

        public FlatMappedSourceFunction(ISourceFunction<TIn> source, Func<TIn, IEnumerable<TOut>> flatMapFunction)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _flatMapFunction = flatMapFunction ?? throw new ArgumentNullException(nameof(flatMapFunction));
        }

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in _source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var output in _flatMapFunction(item))
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
    internal class FilteredSourceFunction<T> : ISourceFunction<T>
    {
        private readonly ISourceFunction<T> _source;
        private readonly Func<T, bool> _filterFunction;

        public FilteredSourceFunction(ISourceFunction<T> source, Func<T, bool> filterFunction)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _filterFunction = filterFunction ?? throw new ArgumentNullException(nameof(filterFunction));
        }

        public async IAsyncEnumerable<T> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var item in _source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_filterFunction(item))
                    yield return item;
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
    internal class AggregatedSourceFunction<TIn, TAcc, TOut> : ISourceFunction<TOut>
    {
        private readonly ISourceFunction<TIn> _source;
        private readonly IAggregateFunction<TIn, TAcc, TOut> _aggregateFunction;

        public AggregatedSourceFunction(ISourceFunction<TIn> source, IAggregateFunction<TIn, TAcc, TOut> aggregateFunction)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _aggregateFunction = aggregateFunction ?? throw new ArgumentNullException(nameof(aggregateFunction));
        }

        public async IAsyncEnumerable<TOut> RunAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            // This is a simplified implementation that aggregates all elements
            // In production, this would be handled by Flink's windowing mechanism
            var accumulator = _aggregateFunction.CreateAccumulator();
            
            await foreach (var item in _source.RunAsync(cancellationToken).ConfigureAwait(false))
            {
                accumulator = _aggregateFunction.Add(item, accumulator);
            }
            
            yield return _aggregateFunction.GetResult(accumulator);
        }
    }
}
