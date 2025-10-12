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
//  limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream
{
    #region Process Functions

    /// <summary>
    /// Interface for process functions that provide access to event time, timers, and state.
    /// Corresponds to org.apache.flink.streaming.api.functions.ProcessFunction in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface IProcessFunction<in T, out TOut>
    {
        /// <summary>
        /// Processes one element from the input stream.
        /// </summary>
        /// <param name="value">The input value</param>
        /// <param name="ctx">The context providing access to element timestamp and timers</param>
        /// <param name="out">The collector for returning result values</param>
        Task ProcessElementAsync(T value, IProcessContext ctx, ICollector<TOut> @out);

        /// <summary>
        /// Called when a timer fires.
        /// </summary>
        /// <param name="timestamp">The timestamp of the firing timer</param>
        /// <param name="ctx">The timer context</param>
        /// <param name="out">The collector for returning result values</param>
        Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<TOut> @out);
    }

    /// <summary>
    /// Interface for keyed process functions.
    /// Corresponds to org.apache.flink.streaming.api.functions.KeyedProcessFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="T">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface IKeyedProcessFunction<TKey, in T, out TOut>
    {
        /// <summary>
        /// Processes one element from the input stream.
        /// </summary>
        Task ProcessElementAsync(T value, IKeyedProcessContext<TKey> ctx, ICollector<TOut> @out);

        /// <summary>
        /// Called when a timer fires.
        /// </summary>
        Task OnTimerAsync(long timestamp, IKeyedOnTimerContext<TKey> ctx, ICollector<TOut> @out);
    }

    /// <summary>
    /// Interface for co-process functions that process two connected streams.
    /// Corresponds to org.apache.flink.streaming.api.functions.co.CoProcessFunction in Java Flink.
    /// </summary>
    /// <typeparam name="T1">The type of the first input stream</typeparam>
    /// <typeparam name="T2">The type of the second input stream</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface ICoProcessFunction<in T1, in T2, out TOut>
    {
        /// <summary>
        /// Processes an element from the first input stream.
        /// </summary>
        Task ProcessElement1Async(T1 value, IProcessContext ctx, ICollector<TOut> @out);

        /// <summary>
        /// Processes an element from the second input stream.
        /// </summary>
        Task ProcessElement2Async(T2 value, IProcessContext ctx, ICollector<TOut> @out);

        /// <summary>
        /// Called when a timer fires.
        /// </summary>
        Task OnTimerAsync(long timestamp, IOnTimerContext ctx, ICollector<TOut> @out);
    }

    /// <summary>
    /// Interface for window process functions.
    /// Corresponds to org.apache.flink.streaming.api.functions.windowing.ProcessWindowFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public interface IProcessWindowFunction<in TIn, out TOut, TKey>
    {
        /// <summary>
        /// Processes all elements in a window.
        /// </summary>
        /// <param name="key">The key for which this window is evaluated</param>
        /// <param name="elements">The elements in the window</param>
        /// <param name="ctx">The context holding window metadata</param>
        /// <param name="out">The collector for emitting results</param>
        Task ProcessAsync(TKey key, IEnumerable<TIn> elements, IWindowContext ctx, ICollector<TOut> @out);
    }

    #endregion

    #region Async I/O

    /// <summary>
    /// Interface for asynchronous I/O operations.
    /// Corresponds to org.apache.flink.streaming.api.functions.async.AsyncFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of output elements</typeparam>
    public interface IAsyncFunction<in TIn, out TOut>
    {
        /// <summary>
        /// Trigger async operation for each stream input.
        /// </summary>
        /// <param name="input">The input element</param>
        /// <param name="resultFuture">The result future to complete with async results</param>
        Task AsyncInvokeAsync(TIn input, IResultFuture<TOut> resultFuture);

        /// <summary>
        /// Timeout handling method (optional). Called when async operation times out.
        /// </summary>
        Task TimeoutAsync(TIn input, IResultFuture<TOut> resultFuture)
        {
            // Default: complete with empty collection
            resultFuture.Complete(Array.Empty<TOut>());
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Result future for async operations.
    /// </summary>
    public interface IResultFuture<in T>
    {
        /// <summary>
        /// Completes the future with a collection of results.
        /// </summary>
        void Complete(IEnumerable<T> results);

        /// <summary>
        /// Completes the future exceptionally.
        /// </summary>
        void CompleteExceptionally(Exception exception);
    }

    #endregion

    #region Context Interfaces

    /// <summary>
    /// Context for process functions.
    /// </summary>
    public interface IProcessContext
    {
        /// <summary>
        /// Gets the timestamp of the element currently being processed.
        /// </summary>
        long Timestamp { get; }

        /// <summary>
        /// Gets the current processing time.
        /// </summary>
        long CurrentProcessingTime { get; }

        /// <summary>
        /// Gets the current event time watermark.
        /// </summary>
        long CurrentWatermark { get; }

        /// <summary>
        /// Registers a timer to fire at the given timestamp.
        /// </summary>
        void RegisterEventTimeTimer(long timestamp);

        /// <summary>
        /// Registers a processing time timer.
        /// </summary>
        void RegisterProcessingTimeTimer(long timestamp);

        /// <summary>
        /// Deletes the event time timer for the given timestamp.
        /// </summary>
        void DeleteEventTimeTimer(long timestamp);

        /// <summary>
        /// Deletes the processing time timer for the given timestamp.
        /// </summary>
        void DeleteProcessingTimeTimer(long timestamp);
    }

    /// <summary>
    /// Context for keyed process functions.
    /// </summary>
    public interface IKeyedProcessContext<out TKey> : IProcessContext
    {
        /// <summary>
        /// Gets the key of the element currently being processed.
        /// </summary>
        TKey CurrentKey { get; }
    }

    /// <summary>
    /// Context provided to timer callbacks.
    /// </summary>
    public interface IOnTimerContext : IProcessContext
    {
        /// <summary>
        /// Gets the time domain of the firing timer.
        /// </summary>
        TimeDomain TimeDomain { get; }
    }

    /// <summary>
    /// Context for keyed timer callbacks.
    /// </summary>
    public interface IKeyedOnTimerContext<out TKey> : IOnTimerContext
    {
        /// <summary>
        /// Gets the key of the timer.
        /// </summary>
        TKey CurrentKey { get; }
    }

    /// <summary>
    /// Context for window functions.
    /// </summary>
    public interface IWindowContext
    {
        /// <summary>
        /// Gets the start timestamp of the window.
        /// </summary>
        long WindowStart { get; }

        /// <summary>
        /// Gets the end timestamp of the window.
        /// </summary>
        long WindowEnd { get; }

        /// <summary>
        /// Gets the current processing time.
        /// </summary>
        long CurrentProcessingTime { get; }

        /// <summary>
        /// Gets the current event time watermark.
        /// </summary>
        long CurrentWatermark { get; }
    }

    /// <summary>
    /// Collector for emitting results.
    /// </summary>
    public interface ICollector<in T>
    {
        /// <summary>
        /// Emits an element.
        /// </summary>
        void Collect(T element);
    }

    #endregion

    #region Side Outputs and Tagged Outputs

    /// <summary>
    /// Tag for identifying side outputs.
    /// Corresponds to org.apache.flink.util.OutputTag in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the side output</typeparam>
    public class OutputTag<T>
    {
        /// <summary>
        /// Gets the identifier of this output tag.
        /// </summary>
        public string Id { get; }

        public OutputTag(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public override bool Equals(object? obj)
        {
            if (obj is OutputTag<T> other)
            {
                return Id == other.Id;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// Time domain for timers.
    /// Corresponds to org.apache.flink.streaming.api.TimeDomain in Java Flink.
    /// </summary>
    public enum TimeDomain
    {
        /// <summary>
        /// Event time domain.
        /// </summary>
        EventTime,

        /// <summary>
        /// Processing time domain.
        /// </summary>
        ProcessingTime
    }

    #endregion

    #region Join and CoGroup

    /// <summary>
    /// Interface for join functions.
    /// Corresponds to org.apache.flink.api.common.functions.JoinFunction in Java Flink.
    /// </summary>
    public interface IJoinFunction<in T1, in T2, out TOut>
    {
        /// <summary>
        /// Joins two elements.
        /// </summary>
        TOut Join(T1 first, T2 second);
    }

    /// <summary>
    /// Interface for flat join functions.
    /// Corresponds to org.apache.flink.api.common.functions.FlatJoinFunction in Java Flink.
    /// </summary>
    public interface IFlatJoinFunction<in T1, in T2, out TOut>
    {
        /// <summary>
        /// Joins two elements and produces zero, one, or more result elements.
        /// </summary>
        IEnumerable<TOut> Join(T1 first, T2 second);
    }

    /// <summary>
    /// Interface for co-group functions.
    /// Corresponds to org.apache.flink.api.common.functions.CoGroupFunction in Java Flink.
    /// </summary>
    public interface ICoGroupFunction<in T1, in T2, out TOut>
    {
        /// <summary>
        /// Co-groups two groups of elements.
        /// </summary>
        IEnumerable<TOut> CoGroup(IEnumerable<T1> first, IEnumerable<T2> second);
    }

    #endregion
}