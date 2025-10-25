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

using System.Collections.Generic;

namespace FlinkDotNet.DataStream.Window.Functions
{
    /// <summary>
    /// Interface for processing all elements in a window, with access to window metadata.
    /// Corresponds to org.apache.flink.streaming.api.functions.windowing.ProcessWindowFunction in Java Flink.
    /// </summary>
    /// <typeparam name="TInput">The type of input elements</typeparam>
    /// <typeparam name="TOutput">The type of output elements</typeparam>
    /// <typeparam name="TKey">The type of the key</typeparam>
    /// <typeparam name="TWindow">The type of window (used for type safety and API consistency with Flink Java API)</typeparam>
#pragma warning disable S2326 // TWindow is intentionally included for type safety and API consistency with Flink's Java API
    public interface IProcessWindowFunction<TInput, TOutput, TKey, TWindow>
#pragma warning restore S2326
        where TWindow : IWindow
    {
        /// <summary>
        /// Processes all elements of a window and returns zero, one, or more output elements.
        /// </summary>
        /// <param name="key">The key for which this window is evaluated</param>
        /// <param name="context">The context in which the window is being evaluated</param>
        /// <param name="elements">The elements in the window</param>
        /// <returns>The output elements</returns>
        public IEnumerable<TOutput> Process(TKey key, IProcessWindowContext context, IEnumerable<TInput> elements);

        /// <summary>
        /// Context that provides information about the window being processed.
        /// </summary>
        public interface IProcessWindowContext
        {
            /// <summary>
            /// Gets the window that is being processed.
            /// </summary>
            public IWindow Window
            {
                get;
            }

            /// <summary>
            /// Gets the current processing time.
            /// </summary>
            public long CurrentProcessingTime
            {
                get;
            }

            /// <summary>
            /// Gets the current event time watermark.
            /// </summary>
            public long CurrentWatermark
            {
                get;
            }
        }
    }
}
