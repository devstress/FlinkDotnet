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

namespace FlinkDotNet.DataStream.Window
{
    /// <summary>
    /// A window that represents a time interval from Start (inclusive) to End (exclusive).
    /// Corresponds to org.apache.flink.streaming.api.windowing.windows.TimeWindow in Java Flink.
    /// </summary>
    public class TimeWindow : IWindow
    {
        /// <summary>
        /// Gets the starting timestamp of the window (inclusive).
        /// </summary>
        public long Start { get; }

        /// <summary>
        /// Gets the ending timestamp of the window (exclusive).
        /// </summary>
        public long End { get; }

        /// <summary>
        /// Creates a new TimeWindow.
        /// </summary>
        /// <param name="start">The starting timestamp (inclusive)</param>
        /// <param name="end">The ending timestamp (exclusive)</param>
        public TimeWindow(long start, long end)
        {
            if (start > end)
                throw new ArgumentException("Start must be less than or equal to end");

            Start = start;
            End = end;
        }

        /// <summary>
        /// Gets the largest timestamp that still belongs to this window.
        /// </summary>
        /// <returns>The largest timestamp that still belongs to this window (end - 1)</returns>
        public long MaxTimestamp() => End - 1;

        /// <summary>
        /// Checks if this window intersects with another window.
        /// </summary>
        /// <param name="other">The other window to check</param>
        /// <returns>True if the windows intersect, false otherwise</returns>
        public bool Intersects(TimeWindow other)
        {
            return Start < other.End && End > other.Start;
        }

        /// <summary>
        /// Returns the minimal window that covers both this window and the given window.
        /// </summary>
        /// <param name="other">The other window to cover</param>
        /// <returns>The minimal window covering both windows</returns>
        public TimeWindow Cover(TimeWindow other)
        {
            return new TimeWindow(Math.Min(Start, other.Start), Math.Max(End, other.End));
        }

        /// <summary>
        /// Gets the window that results from merging this window with the given windows.
        /// </summary>
        /// <param name="windows">The windows to merge with</param>
        /// <returns>The merged window</returns>
        public static TimeWindow MergeWindows(params TimeWindow[] windows)
        {
            if (windows.Length == 0)
                throw new ArgumentException("Cannot merge empty window collection");

            long minStart = long.MaxValue;
            long maxEnd = long.MinValue;

            foreach (var window in windows)
            {
                minStart = Math.Min(minStart, window.Start);
                maxEnd = Math.Max(maxEnd, window.End);
            }

            return new TimeWindow(minStart, maxEnd);
        }

        /// <summary>
        /// Gets the window for the given timestamp with the specified window size.
        /// </summary>
        /// <param name="timestamp">The timestamp</param>
        /// <param name="offset">The offset to apply to the window start</param>
        /// <param name="windowSize">The size of the window in milliseconds</param>
        /// <returns>The window that contains the timestamp</returns>
        public static TimeWindow GetWindowStartWithOffset(long timestamp, long offset, long windowSize)
        {
            long start = timestamp - (timestamp - offset + windowSize) % windowSize;
            return new TimeWindow(start, start + windowSize);
        }

        public override bool Equals(object? obj)
        {
            if (obj is not TimeWindow other)
                return false;

            return Start == other.Start && End == other.End;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Start, End);
        }

        public override string ToString()
        {
            return $"TimeWindow[{Start}, {End})";
        }
    }
}