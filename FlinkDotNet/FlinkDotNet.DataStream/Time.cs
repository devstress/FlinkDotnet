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

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Time unit for windowing operations.
    /// Corresponds to org.apache.flink.streaming.api.windowing.time.Time in Java Flink.
    /// </summary>
    public class Time
    {
        private readonly long _milliseconds;

        private Time(long milliseconds)
        {
            _milliseconds = milliseconds;
        }

        /// <summary>
        /// Gets the time duration in milliseconds.
        /// </summary>
        public long ToMilliseconds() => _milliseconds;

        /// <summary>
        /// Creates a time duration in milliseconds.
        /// </summary>
        public static Time Milliseconds(long milliseconds) => new Time(milliseconds);

        /// <summary>
        /// Creates a time duration in seconds.
        /// </summary>
        public static Time Seconds(long seconds) => new Time(seconds * 1000);

        /// <summary>
        /// Creates a time duration in minutes.
        /// </summary>
        public static Time Minutes(long minutes) => new Time(minutes * 60 * 1000);

        /// <summary>
        /// Creates a time duration in hours.
        /// </summary>
        public static Time Hours(long hours) => new Time(hours * 60 * 60 * 1000);

        /// <summary>
        /// Creates a time duration in days.
        /// </summary>
        public static Time Days(long days) => new Time(days * 24 * 60 * 60 * 1000);

        public override string ToString() => $"{_milliseconds}ms";
    }

    /// <summary>
    /// Watermark for event time processing.
    /// Corresponds to org.apache.flink.streaming.api.watermark.Watermark in Java Flink.
    /// </summary>
    public class Watermark
    {
        private readonly long _timestamp;

        public Watermark(long timestamp)
        {
            _timestamp = timestamp;
        }

        /// <summary>
        /// Gets the timestamp of this watermark in milliseconds.
        /// </summary>
        public long GetTimestamp() => _timestamp;

        public override string ToString() => $"Watermark({_timestamp})";
    }

    /// <summary>
    /// Interface for assigning timestamps and watermarks.
    /// Corresponds to org.apache.flink.streaming.api.functions.timestamps.BoundedOutOfOrdernessTimestampExtractor in Java Flink.
    /// </summary>
    public interface ITimestampAssigner<in T>
    {
        /// <summary>
        /// Extracts the timestamp from the given element.
        /// </summary>
        /// <param name="element">The element to extract the timestamp from</param>
        /// <param name="previousElementTimestamp">The previous timestamp</param>
        /// <returns>The timestamp in milliseconds</returns>
        long ExtractTimestamp(T element, long previousElementTimestamp);
    }

    /// <summary>
    /// Interface for generating watermarks with punctuated watermarks.
    /// Corresponds to org.apache.flink.streaming.api.functions.AssignerWithPunctuatedWatermarks in Java Flink.
    /// </summary>
    public interface IAssignerWithPunctuatedWatermarks<in T> : ITimestampAssigner<T>
    {
        /// <summary>
        /// Checks if a new watermark should be emitted.
        /// </summary>
        /// <param name="lastElement">The last processed element</param>
        /// <param name="extractedTimestamp">The extracted timestamp</param>
        /// <returns>The watermark or null if no watermark should be emitted</returns>
        Watermark? CheckAndGetNextWatermark(T lastElement, long extractedTimestamp);
    }

    /// <summary>
    /// Interface for generating watermarks with periodic watermarks.
    /// Corresponds to org.apache.flink.streaming.api.functions.AssignerWithPeriodicWatermarks in Java Flink.
    /// </summary>
    public interface IAssignerWithPeriodicWatermarks<in T> : ITimestampAssigner<T>
    {
        /// <summary>
        /// Returns the current watermark.
        /// </summary>
        /// <returns>The current watermark or null if no watermark should be emitted</returns>
        Watermark? GetCurrentWatermark();
    }
}