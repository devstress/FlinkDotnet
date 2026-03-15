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
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.State
{
    /// <summary>
    /// In-memory implementation of <see cref="IReducingState{T}"/>.
    /// Applies a reduce function to incoming elements, keeping a single aggregated value.
    /// Suitable for testing and lightweight processing scenarios.
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    public sealed class InMemoryReducingState<T> : IReducingState<T>
    {
        private readonly IReduceFunction<T> _reduceFunction;
        private T _value = default!;
        private bool _hasValue;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryReducingState{T}"/>.
        /// </summary>
        /// <param name="reduceFunction">The reduce function to apply</param>
        public InMemoryReducingState(IReduceFunction<T> reduceFunction)
        {
            _reduceFunction = reduceFunction ?? throw new ArgumentNullException(nameof(reduceFunction));
        }

        /// <inheritdoc/>
        public Task<T> GetAsync()
        {
            return Task.FromResult(_hasValue ? _value : default!);
        }

        /// <inheritdoc/>
        public Task AddAsync(T value)
        {
            if (!_hasValue)
            {
                _value = value;
                _hasValue = true;
            }
            else
            {
                _value = _reduceFunction.Reduce(_value, value);
            }
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
