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
    /// Static helper class for creating SessionWindows without specifying the element type.
    /// This provides a more convenient API similar to Java Flink.
    /// </summary>
    public static class SessionWindows
    {
        /// <summary>
        /// Creates a new SessionWindows WindowAssigner that windows elements into sessions with the given gap.
        /// Elements are grouped into the same session if the time difference between them is less than the gap.
        /// </summary>
        /// <param name="sessionGap">The session gap</param>
        /// <returns>A new SessionWindows WindowAssigner</returns>
        public static SessionWindows<T> WithGap<T>(Time sessionGap)
        {
            return SessionWindows<T>.WithGap(sessionGap);
        }

        /// <summary>
        /// Merges overlapping session windows.
        /// This is a convenience method that delegates to SessionWindows&lt;T&gt;.MergeWindows.
        /// </summary>
        /// <param name="windows">The windows to merge</param>
        /// <returns>Merged windows</returns>
        public static IEnumerable<TimeWindow> MergeWindows(IEnumerable<TimeWindow> windows)
        {
            return SessionWindows<object>.MergeWindows(windows);
        }
    }
}
