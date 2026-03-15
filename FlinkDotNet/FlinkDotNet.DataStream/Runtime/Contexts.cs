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

namespace FlinkDotNet.DataStream.Runtime
{
    /// <summary>
    /// Default implementation of <see cref="IProcessContext"/>.
    /// Provides access to timestamps, timers, and watermarks during process function execution.
    /// </summary>
    public class ProcessContext : IProcessContext
    {
        private readonly SortedSet<long> _eventTimeTimers = [];
        private readonly SortedSet<long> _processingTimeTimers = [];

        /// <inheritdoc/>
        public long Timestamp { get; set; }

        /// <inheritdoc/>
        public long CurrentProcessingTime { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        /// <inheritdoc/>
        public long CurrentWatermark { get; set; } = long.MinValue;

        /// <inheritdoc/>
        public void RegisterEventTimeTimer(long timestamp)
        {
            _eventTimeTimers.Add(timestamp);
        }

        /// <inheritdoc/>
        public void RegisterProcessingTimeTimer(long timestamp)
        {
            _processingTimeTimers.Add(timestamp);
        }

        /// <inheritdoc/>
        public void DeleteEventTimeTimer(long timestamp)
        {
            _eventTimeTimers.Remove(timestamp);
        }

        /// <inheritdoc/>
        public void DeleteProcessingTimeTimer(long timestamp)
        {
            _processingTimeTimers.Remove(timestamp);
        }

        /// <summary>
        /// Gets the registered event time timers.
        /// </summary>
        public IReadOnlyCollection<long> EventTimeTimers => _eventTimeTimers;

        /// <summary>
        /// Gets the registered processing time timers.
        /// </summary>
        public IReadOnlyCollection<long> ProcessingTimeTimers => _processingTimeTimers;
    }

    /// <summary>
    /// Default implementation of <see cref="IKeyedProcessContext{TKey}"/>.
    /// Extends <see cref="ProcessContext"/> with key access for keyed process functions.
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public class KeyedProcessContext<TKey> : ProcessContext, IKeyedProcessContext<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="KeyedProcessContext{TKey}"/>.
        /// </summary>
        /// <param name="currentKey">The current key</param>
        public KeyedProcessContext(TKey currentKey)
        {
            CurrentKey = currentKey;
        }

        /// <inheritdoc/>
        public TKey CurrentKey { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IOnTimerContext"/>.
    /// Provides context for timer callbacks including the time domain of the firing timer.
    /// </summary>
    public class OnTimerContext : ProcessContext, IOnTimerContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="OnTimerContext"/>.
        /// </summary>
        /// <param name="timeDomain">The time domain of the firing timer</param>
        public OnTimerContext(TimeDomain timeDomain)
        {
            TimeDomain = timeDomain;
        }

        /// <inheritdoc/>
        public TimeDomain TimeDomain { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IKeyedOnTimerContext{TKey}"/>.
    /// Provides context for keyed timer callbacks.
    /// </summary>
    /// <typeparam name="TKey">The type of the key</typeparam>
    public class KeyedOnTimerContext<TKey> : OnTimerContext, IKeyedOnTimerContext<TKey>
    {
        /// <summary>
        /// Initializes a new instance of <see cref="KeyedOnTimerContext{TKey}"/>.
        /// </summary>
        /// <param name="timeDomain">The time domain of the firing timer</param>
        /// <param name="currentKey">The current key</param>
        public KeyedOnTimerContext(TimeDomain timeDomain, TKey currentKey) : base(timeDomain)
        {
            CurrentKey = currentKey;
        }

        /// <inheritdoc/>
        public TKey CurrentKey { get; }
    }

    /// <summary>
    /// Default implementation of <see cref="IWindowContext"/>.
    /// Provides context holding window metadata during window function execution.
    /// </summary>
    public class WindowContext : IWindowContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="WindowContext"/>.
        /// </summary>
        /// <param name="windowStart">The start timestamp of the window</param>
        /// <param name="windowEnd">The end timestamp of the window</param>
        public WindowContext(long windowStart, long windowEnd)
        {
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            CurrentProcessingTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        /// <inheritdoc/>
        public long WindowStart { get; }

        /// <inheritdoc/>
        public long WindowEnd { get; }

        /// <inheritdoc/>
        public long CurrentProcessingTime { get; set; }

        /// <inheritdoc/>
        public long CurrentWatermark { get; set; } = long.MinValue;
    }
}
