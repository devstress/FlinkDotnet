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

using System;
using System.Collections.Generic;

namespace FlinkDotNet.DataStream.Runtime
{
    /// <summary>
    /// Default implementation of <see cref="ISourceOutput{T}"/>.
    /// Collects elements and watermarks emitted by source readers.
    /// </summary>
    /// <typeparam name="T">Type of elements</typeparam>
    public sealed class SourceOutput<T> : ISourceOutput<T>
    {
        private readonly List<(T Element, long Timestamp)> _elements = [];

        /// <inheritdoc/>
        public void Collect(T element)
        {
            _elements.Add((element, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
        }

        /// <inheritdoc/>
        public void Collect(T element, long timestamp)
        {
            _elements.Add((element, timestamp));
        }

        /// <inheritdoc/>
        public void EmitWatermark(long watermark)
        {
            CurrentWatermark = watermark;
        }

        /// <summary>
        /// Gets the collected elements with their timestamps.
        /// </summary>
        public IReadOnlyList<(T Element, long Timestamp)> Elements => _elements;

        /// <summary>
        /// Gets the current watermark.
        /// </summary>
        public long CurrentWatermark { get; private set; } = long.MinValue;

        /// <summary>
        /// Clears all collected elements.
        /// </summary>
        public void Clear()
        {
            _elements.Clear();
        }
    }
}
