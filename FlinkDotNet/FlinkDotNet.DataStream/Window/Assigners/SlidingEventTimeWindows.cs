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

namespace FlinkDotNet.DataStream.Window.Assigners
{
    /// <summary>
    /// A WindowAssigner that assigns elements to potentially overlapping sliding windows based on event time.
    /// Corresponds to org.apache.flink.streaming.api.windowing.assigners.SlidingEventTimeWindows in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements being windowed</typeparam>
    public sealed class SlidingEventTimeWindows<T> : IWindowAssigner<T, TimeWindow>
    {
        private readonly long _size;
        private readonly long _slide;
        private readonly long _offset;

        private SlidingEventTimeWindows(long size, long slide, long offset)
        {
            this._size = size;
            this._slide = slide;
            this._offset = offset;
        }

        /// <summary>
        /// Creates a new SlidingEventTimeWindows WindowAssigner that assigns elements to windows of the given size and slide.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <param name="slide">The slide interval (how often a new window starts)</param>
        /// <returns>A new SlidingEventTimeWindows WindowAssigner</returns>
        public static SlidingEventTimeWindows<T> Of(Time size, Time slide) => new SlidingEventTimeWindows<T>(size.ToMilliseconds(), slide.ToMilliseconds(), 0);

        /// <summary>
        /// Creates a new SlidingEventTimeWindows WindowAssigner with an offset.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <param name="slide">The slide interval</param>
        /// <param name="offset">The offset which window start would be shifted by</param>
        /// <returns>A new SlidingEventTimeWindows WindowAssigner</returns>
        public static SlidingEventTimeWindows<T> Of(Time size, Time slide, Time offset) => new SlidingEventTimeWindows<T>(size.ToMilliseconds(), slide.ToMilliseconds(), offset.ToMilliseconds());

        /// <summary>
        /// Assigns the element to multiple overlapping sliding windows based on its timestamp.
        /// An element belongs to all windows that contain its timestamp.
        /// </summary>
        public IEnumerable<TimeWindow> AssignWindows(T element, long timestamp)
        {
            if (timestamp <= long.MinValue)
            {
                yield break;
            }

            // Determine the start of the first window that contains this timestamp
            var lastStart = this.GetWindowStart(timestamp);

            // Generate all windows that contain this timestamp
            for (var start = lastStart; start > timestamp - this._size; start -= this._slide)
            {
                if (start >= 0 || start + this._size > 0)
                {
                    yield return new TimeWindow(start, start + this._size);
                }
            }
        }

        private long GetWindowStart(long timestamp) => timestamp - ((timestamp - this._offset + this._slide) % this._slide);

        /// <summary>
        /// Gets the time characteristic (Event Time) of this window assigner.
        /// </summary>
        public TimeCharacteristic TimeCharacteristic => TimeCharacteristic.EventTime;

        /// <summary>
        /// Returns true indicating this is an event time window assigner.
        /// </summary>
        public bool IsEventTime => true;

        public override string ToString() => $"SlidingEventTimeWindows({this._size}ms, {this._slide}ms)";
    }
}
