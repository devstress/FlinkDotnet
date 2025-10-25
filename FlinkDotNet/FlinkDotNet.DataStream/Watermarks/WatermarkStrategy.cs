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

namespace FlinkDotNet.DataStream.Watermarks
{
    /// <summary>
    /// The WatermarkStrategy defines how to generate watermarks in the system.
    /// Corresponds to org.apache.flink.api.common.eventtime.WatermarkStrategy in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the stream</typeparam>
    public sealed class WatermarkStrategy<T>
    {
        private System.Func<T, long>? _timestampAssigner;

        /// <summary>
        /// Gets whether this strategy is for monotonous timestamps.
        /// </summary>
        public bool IsMonotonous { get; }

        /// <summary>
        /// Gets the maximum out-of-orderness allowed.
        /// </summary>
        public System.TimeSpan MaxOutOfOrderness { get; }

        private WatermarkStrategy(System.TimeSpan maxOutOfOrderness, bool isMonotonous)
        {
            this.MaxOutOfOrderness = maxOutOfOrderness;
            this.IsMonotonous = isMonotonous;
        }

        /// <summary>
        /// Creates a watermark strategy for situations where events arrive out of order,
        /// but you can bound the maximum amount of time they are out of order.
        /// </summary>
        /// <param name="maxOutOfOrderness">The maximum out-of-orderness</param>
        /// <returns>A new WatermarkStrategy</returns>
        public static WatermarkStrategy<T> ForBoundedOutOfOrderness(System.TimeSpan maxOutOfOrderness) => new WatermarkStrategy<T>(maxOutOfOrderness, false);

        /// <summary>
        /// Creates a watermark strategy for situations where timestamps are monotonously ascending
        /// (events always arrive in order).
        /// </summary>
        /// <returns>A new WatermarkStrategy</returns>
        public static WatermarkStrategy<T> ForMonotonousTimestamps() => new WatermarkStrategy<T>(System.TimeSpan.Zero, true);

        /// <summary>
        /// Sets the timestamp assigner function.
        /// </summary>
        /// <param name="assigner">Function that extracts the timestamp from an element</param>
        /// <returns>This WatermarkStrategy</returns>
        public WatermarkStrategy<T> WithTimestampAssigner(System.Func<T, long> assigner)
        {
            this._timestampAssigner = assigner;
            return this;
        }

        /// <summary>
        /// Sets the timestamp assigner using an ITimestampAssigner.
        /// </summary>
        /// <param name="assigner">The timestamp assigner</param>
        /// <returns>This WatermarkStrategy</returns>
        public WatermarkStrategy<T> WithTimestampAssigner(ITimestampAssigner<T> assigner)
        {
            this._timestampAssigner = (element) => assigner.ExtractTimestamp(element, -1);
            return this;
        }

        /// <summary>
        /// Extracts the timestamp from the given element.
        /// </summary>
        /// <param name="element">The element to extract timestamp from</param>
        /// <param name="previousTimestamp">The previous element's timestamp</param>
        /// <returns>The timestamp in milliseconds</returns>
        public long ExtractTimestamp(T element, long previousTimestamp)
        {
            _ = previousTimestamp; // Reserved for future implementation
            if (this._timestampAssigner == null)
            {
                throw new System.InvalidOperationException(
                    "No timestamp assigner configured. Call WithTimestampAssigner() first.");
            }

            return this._timestampAssigner(element);
        }

        /// <summary>
        /// Calculates the current watermark based on the maximum observed timestamp.
        /// </summary>
        /// <param name="currentMaxTimestamp">The maximum timestamp seen so far</param>
        /// <returns>The watermark timestamp</returns>
        public long GetCurrentWatermark(long currentMaxTimestamp)
        {
            if (this.IsMonotonous)
            {
                // For monotonous timestamps, watermark = current timestamp
                return currentMaxTimestamp;
            }
            else
            {
                // For bounded out-of-orderness, watermark = max timestamp - allowed delay
                return currentMaxTimestamp - (long) this.MaxOutOfOrderness.TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets whether a timestamp assigner has been configured.
        /// </summary>
        public bool HasTimestampAssigner => this._timestampAssigner != null;
    }
}
