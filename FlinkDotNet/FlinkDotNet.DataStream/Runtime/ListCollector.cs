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

using System.Collections.Generic;

namespace FlinkDotNet.DataStream.Runtime
{
    /// <summary>
    /// Default implementation of <see cref="ICollector{T}"/>.
    /// Collects emitted elements into a list for downstream processing.
    /// </summary>
    /// <typeparam name="T">The type of elements to collect</typeparam>
    public sealed class ListCollector<T> : ICollector<T>
    {
        private readonly List<T> _elements = [];

        /// <inheritdoc/>
        public void Collect(T element)
        {
            _elements.Add(element);
        }

        /// <summary>
        /// Gets the collected elements.
        /// </summary>
        public IReadOnlyList<T> Elements => _elements;

        /// <summary>
        /// Clears all collected elements.
        /// </summary>
        public void Clear()
        {
            _elements.Clear();
        }
    }
}
