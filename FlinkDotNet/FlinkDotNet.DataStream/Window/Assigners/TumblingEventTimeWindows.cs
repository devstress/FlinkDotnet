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
    /// A WindowAssigner that assigns elements to fixed-size, non-overlapping windows based on event time.
    /// Corresponds to org.apache.flink.streaming.api.windowing.assigners.TumblingEventTimeWindows in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements being windowed</typeparam>
    public sealed class TumblingEventTimeWindows<T> : IWindowAssigner<T, TimeWindow>
    {
        private readonly long _size;
        private readonly long _offset;

        private TumblingEventTimeWindows(long size, long offset)
        {
            this._size = size;
            this._offset = offset;
        }

        /// <summary>
        /// Creates a new TumblingEventTimeWindows WindowAssigner that assigns elements to windows of the given size.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <returns>A new TumblingEventTimeWindows WindowAssigner</returns>
        public static TumblingEventTimeWindows<T> Of(Time size) => new TumblingEventTimeWindows<T>(size.ToMilliseconds(), 0);

        /// <summary>
        /// Creates a new TumblingEventTimeWindows WindowAssigner that assigns elements to windows of the given size with an offset.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <param name="offset">The offset which window start would be shifted by</param>
        /// <returns>A new TumblingEventTimeWindows WindowAssigner</returns>
        public static TumblingEventTimeWindows<T> Of(Time size, Time offset) => new TumblingEventTimeWindows<T>(size.ToMilliseconds(), offset.ToMilliseconds());

        /// <summary>
        /// Assigns the element to a single tumbling window based on its timestamp.
        /// </summary>
        public IEnumerable<TimeWindow> AssignWindows(T element, long timestamp)
        {
            var start = this.GetWindowStart(timestamp);
            yield return new TimeWindow(start, start + this._size);
        }

        private long GetWindowStart(long timestamp) => timestamp - ((timestamp - this._offset + this._size) % this._size);

        /// <summary>
        /// Gets the time characteristic (Event Time) of this window assigner.
        /// </summary>
        public TimeCharacteristic TimeCharacteristic => TimeCharacteristic.EventTime;

        /// <summary>
        /// Returns true indicating this is an event time window assigner.
        /// </summary>
        public bool IsEventTime => true;

        public override string ToString() => $"TumblingEventTimeWindows({this._size}ms)";
    }
}
