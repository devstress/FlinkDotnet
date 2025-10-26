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
    /// A WindowAssigner that windows elements into sessions based on a session gap.
    /// Sessions are formed by grouping consecutive elements with gaps smaller than the session gap.
    /// Corresponds to org.apache.flink.streaming.api.windowing.assigners.EventTimeSessionWindows in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements being windowed</typeparam>
    public sealed class SessionWindows<T> : IWindowAssigner<T, TimeWindow>
    {
        private readonly long _sessionGap;

        private SessionWindows(long sessionGap) => this._sessionGap = sessionGap;

        /// <summary>
        /// Creates a new SessionWindows WindowAssigner that windows elements into sessions with the given gap.
        /// Elements are grouped into the same session if the time difference between them is less than the gap.
        /// </summary>
        /// <param name="sessionGap">The session gap</param>
        /// <returns>A new SessionWindows WindowAssigner</returns>
        public static SessionWindows<T> WithGap(Time sessionGap) => new(sessionGap.ToMilliseconds());

        /// <summary>
        /// Assigns the element to a session window.
        /// For session windows, each element initially gets its own window, and windows are merged later.
        /// </summary>
        public IEnumerable<TimeWindow> AssignWindows(T element, long timestamp)
        {
            // For session windows, each element initially creates a window that starts at the timestamp
            // and ends at timestamp + session gap. Windows are merged when they overlap.
            yield return new(timestamp, timestamp + this._sessionGap);
        }

        /// <summary>
        /// Gets the time characteristic (Event Time) of this window assigner.
        /// </summary>
        public TimeCharacteristic TimeCharacteristic => TimeCharacteristic.EventTime;

        /// <summary>
        /// Returns true indicating this is an event time window assigner.
        /// </summary>
        public bool IsEventTime => true;

        /// <summary>
        /// Returns true indicating that session windows can be merged.
        /// </summary>
        public bool CanMerge => true;

        /// <summary>
        /// Merges overlapping session windows.
        /// </summary>
        /// <param name="windows">The windows to merge</param>
        /// <returns>Merged windows</returns>
        public static IEnumerable<TimeWindow> MergeWindows(IEnumerable<TimeWindow> windows)
        {
            List<TimeWindow> sortedWindows = new(windows);
            sortedWindows.Sort((w1, w2) => w1.Start.CompareTo(w2.Start));

            List<TimeWindow> mergedWindows = [];
            TimeWindow? currentWindow = null;

            foreach (TimeWindow window in sortedWindows)
            {
                if (currentWindow == null)
                {
                    currentWindow = window;
                }
                else if (window.Start <= currentWindow.End)
                {
                    // Windows overlap, merge them
                    currentWindow = currentWindow.Cover(window);
                }
                else
                {
                    // No overlap, add current window and start a new one
                    mergedWindows.Add(currentWindow);
                    currentWindow = window;
                }
            }

            if (currentWindow != null)
            {
                mergedWindows.Add(currentWindow);
            }

            return mergedWindows;
        }

        public override string ToString() => $"SessionWindows({this._sessionGap}ms gap)";
    }
}
