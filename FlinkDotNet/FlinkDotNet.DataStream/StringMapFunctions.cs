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

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Map function that converts strings to uppercase using ToUpperInvariant().
    /// This is automatically translated to Flink IR "upper" expression.
    /// </summary>
    public class ToUpperInvariantMapFunction : IMapFunction<string, string>
    {
        /// <summary>
        /// Converts the input string to uppercase using culture-invariant rules.
        /// </summary>
        public string Map(string value) => value?.ToUpperInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Map function that converts strings to lowercase using ToLowerInvariant().
    /// This is automatically translated to Flink IR "lower" expression.
    /// </summary>
    public class ToLowerInvariantMapFunction : IMapFunction<string, string>
    {
        /// <summary>
        /// Converts the input string to lowercase using culture-invariant rules.
        /// </summary>
        public string Map(string value) => value?.ToLowerInvariant() ?? string.Empty;
    }

    /// <summary>
    /// Map function that trims whitespace from both ends of strings.
    /// This is automatically translated to Flink IR "trim" expression.
    /// </summary>
    public class TrimMapFunction : IMapFunction<string, string>
    {
        /// <summary>
        /// Removes all leading and trailing white-space characters from the input string.
        /// </summary>
        public string Map(string value) => value?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Map function that trims whitespace from the start of strings.
    /// This is automatically translated to Flink IR "ltrim" expression.
    /// </summary>
    public class TrimStartMapFunction : IMapFunction<string, string>
    {
        /// <summary>
        /// Removes all leading white-space characters from the input string.
        /// </summary>
        public string Map(string value) => value?.TrimStart() ?? string.Empty;
    }

    /// <summary>
    /// Map function that trims whitespace from the end of strings.
    /// This is automatically translated to Flink IR "rtrim" expression.
    /// </summary>
    public class TrimEndMapFunction : IMapFunction<string, string>
    {
        /// <summary>
        /// Removes all trailing white-space characters from the input string.
        /// </summary>
        public string Map(string value) => value?.TrimEnd() ?? string.Empty;
    }
}
