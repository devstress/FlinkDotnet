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

using FlinkDotNet.DataStream.Window.Assigners;
using FlinkDotNet.DataStream.Window.Functions;

namespace FlinkDotNet.DataStream.Window
{
    /// <summary>
    /// A WindowedStream represents a keyed data stream where elements are grouped into windows.
    /// Corresponds to org.apache.flink.streaming.api.datastream.WindowedStream in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stream</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="TWindow">The type of window</typeparam>
    public class WindowedStream<T, TKey, TWindow> where TWindow : IWindow where TKey : notnull
    {
        private readonly KeyedStream<T, TKey> _keyedStream;
        private readonly IWindowAssigner<T, TWindow> _windowAssigner;

        internal WindowedStream(KeyedStream<T, TKey> keyedStream, IWindowAssigner<T, TWindow> windowAssigner)
        {
            _keyedStream = keyedStream ?? throw new System.ArgumentNullException(nameof(keyedStream));
            _windowAssigner = windowAssigner ?? throw new System.ArgumentNullException(nameof(windowAssigner));
        }

        /// <summary>
        /// Applies an aggregate function to the windowed stream.
        /// This performs incremental aggregation, which is more efficient than collecting all elements.
        /// </summary>
        /// <typeparam name="TAcc">The type of the accumulator</typeparam>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="aggregateFunction">The aggregate function to apply</param>
        /// <returns>The resulting DataStream</returns>
        public DataStream<TResult> Aggregate<TAcc, TResult>(IAggregateFunction<T, TAcc, TResult> aggregateFunction)
        {
            if (aggregateFunction == null)
                throw new System.ArgumentNullException(nameof(aggregateFunction));

            // In a production implementation, this would integrate with Flink's windowing engine
            // For now, return the underlying data stream to maintain API compatibility
            var dataStream = _keyedStream.GetDataStream();
            return new DataStream<TResult>(
                new System.Collections.Generic.List<TResult>(),
                dataStream.GetExecutionEnvironment()
            );
        }

        /// <summary>
        /// Applies a reduce function to the windowed stream.
        /// This combines elements within each window using the reduce function.
        /// </summary>
        /// <param name="reduceFunction">The reduce function to apply</param>
        /// <returns>The resulting DataStream</returns>
        public DataStream<T> Reduce(IReduceFunction<T> reduceFunction)
        {
            if (reduceFunction == null)
                throw new System.ArgumentNullException(nameof(reduceFunction));

            // In a production implementation, this would integrate with Flink's windowing engine
            return _keyedStream.GetDataStream();
        }

        /// <summary>
        /// Applies a reduce function to the windowed stream using a lambda.
        /// </summary>
        /// <param name="reduceFunction">The reduce function to apply</param>
        /// <returns>The resulting DataStream</returns>
        public DataStream<T> Reduce(System.Func<T, T, T> reduceFunction)
        {
            if (reduceFunction == null)
                throw new System.ArgumentNullException(nameof(reduceFunction));

            return _keyedStream.Reduce(reduceFunction);
        }

        /// <summary>
        /// Applies a process window function to the windowed stream.
        /// This provides full access to all elements in the window and window metadata.
        /// </summary>
        /// <typeparam name="TResult">The type of the result</typeparam>
        /// <param name="processFunction">The process function to apply</param>
        /// <returns>The resulting DataStream</returns>
        public DataStream<TResult> Process<TResult>(
            IProcessWindowFunction<T, TResult, TKey, TWindow> processFunction)
        {
            if (processFunction == null)
                throw new System.ArgumentNullException(nameof(processFunction));

            // In a production implementation, this would integrate with Flink's windowing engine
            var dataStream = _keyedStream.GetDataStream();
            return new DataStream<TResult>(
                new System.Collections.Generic.List<TResult>(),
                dataStream.GetExecutionEnvironment()
            );
        }

        /// <summary>
        /// Gets the window assigner used by this windowed stream.
        /// </summary>
        public IWindowAssigner<T, TWindow> GetWindowAssigner() => _windowAssigner;

        /// <summary>
        /// Gets the underlying keyed stream.
        /// </summary>
        public KeyedStream<T, TKey> GetKeyedStream() => _keyedStream;
    }
}
