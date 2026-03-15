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

using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.State
{
    /// <summary>
    /// In-memory implementation of <see cref="IValueState{T}"/>.
    /// Stores a single value per keyed partition in memory.
    /// Suitable for testing and lightweight processing scenarios.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the state</typeparam>
    public sealed class InMemoryValueState<T> : IValueState<T>
    {
        private T _value = default!;
        private bool _hasValue;

        /// <inheritdoc/>
        public Task<T> ValueAsync()
        {
            return Task.FromResult(_hasValue ? _value : default!);
        }

        /// <inheritdoc/>
        public Task UpdateAsync(T value)
        {
            _value = value;
            _hasValue = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            _value = default!;
            _hasValue = false;
            return Task.CompletedTask;
        }
    }
}
