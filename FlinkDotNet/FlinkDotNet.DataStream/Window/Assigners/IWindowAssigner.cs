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
    /// Defines the time characteristic for window operations.
    /// </summary>
    public enum TimeCharacteristic
    {
        /// <summary>
        /// Event time - uses timestamps embedded in the events.
        /// </summary>
        EventTime,

        /// <summary>
        /// Processing time - uses the system time when the element is processed.
        /// </summary>
        ProcessingTime
    }

    /// <summary>
    /// A WindowAssigner assigns elements to windows based on their timestamp.
    /// Corresponds to org.apache.flink.streaming.api.windowing.assigners.WindowAssigner in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements being windowed</typeparam>
    /// <typeparam name="W">The type of Window that this assigner assigns elements to</typeparam>
    public interface IWindowAssigner<T, out W> where W : IWindow
    {
        /// <summary>
        /// Assigns the element to one or more windows based on the given timestamp.
        /// For overlapping windows (like sliding windows), an element can be assigned to multiple windows.
        /// </summary>
        /// <param name="element">The element to assign</param>
        /// <param name="timestamp">The timestamp of the element</param>
        /// <returns>A collection of windows that the element is assigned to</returns>
        public IEnumerable<W> AssignWindows(T element, long timestamp);

        /// <summary>
        /// Gets the time characteristic (event time or processing time) of this window assigner.
        /// </summary>
        public TimeCharacteristic TimeCharacteristic
        {
            get;
        }

        /// <summary>
        /// Returns true if windows created by this assigner can be merged.
        /// This is true for session windows but false for tumbling/sliding windows.
        /// </summary>
        public bool IsEventTime
        {
            get;
        }
    }
}
