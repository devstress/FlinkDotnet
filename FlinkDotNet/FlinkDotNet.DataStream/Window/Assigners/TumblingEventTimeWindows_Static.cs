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

namespace FlinkDotNet.DataStream.Window.Assigners
{
    /// <summary>
    /// Static helper class for creating TumblingEventTimeWindows without specifying the element type.
    /// This provides a more convenient API similar to Java Flink.
    /// </summary>
    public static class TumblingEventTimeWindows
    {
        /// <summary>
        /// Creates a new TumblingEventTimeWindows WindowAssigner that assigns elements to windows of the given size.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <returns>A new TumblingEventTimeWindows WindowAssigner</returns>
        public static TumblingEventTimeWindows<T> Of<T>(Time size)
        {
            return TumblingEventTimeWindows<T>.Of(size);
        }

        /// <summary>
        /// Creates a new TumblingEventTimeWindows WindowAssigner that assigns elements to windows of the given size with an offset.
        /// </summary>
        /// <param name="size">The size of the window</param>
        /// <param name="offset">The offset which window start would be shifted by</param>
        /// <returns>A new TumblingEventTimeWindows WindowAssigner</returns>
        public static TumblingEventTimeWindows<T> Of<T>(Time size, Time offset)
        {
            return TumblingEventTimeWindows<T>.Of(size, offset);
        }
    }
}
