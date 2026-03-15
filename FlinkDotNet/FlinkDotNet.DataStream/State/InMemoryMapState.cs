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
using System.Linq;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.State
{
    /// <summary>
    /// In-memory implementation of <see cref="IMapState{TKey, TValue}"/>.
    /// Stores key-value pairs per keyed partition in memory.
    /// Suitable for testing and lightweight processing scenarios.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the map</typeparam>
    /// <typeparam name="TValue">The type of values in the map</typeparam>
    public sealed class InMemoryMapState<TKey, TValue> : IMapState<TKey, TValue> where TKey : notnull
    {
        private readonly Dictionary<TKey, TValue> _map = [];

        /// <inheritdoc/>
        public Task<TValue> GetAsync(TKey key)
        {
            _map.TryGetValue(key, out var value);
            return Task.FromResult(value!);
        }

        /// <inheritdoc/>
        public Task PutAsync(TKey key, TValue value)
        {
            _map[key] = value;
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task PutAllAsync(IDictionary<TKey, TValue> map)
        {
            foreach (var kvp in map)
            {
                _map[kvp.Key] = kvp.Value;
            }
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task RemoveAsync(TKey key)
        {
            _map.Remove(key);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> ContainsAsync(TKey key)
        {
            return Task.FromResult(_map.ContainsKey(key));
        }

        /// <inheritdoc/>
        public Task<IEnumerable<KeyValuePair<TKey, TValue>>> EntriesAsync()
        {
            return Task.FromResult<IEnumerable<KeyValuePair<TKey, TValue>>>(_map.ToList());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TKey>> KeysAsync()
        {
            return Task.FromResult<IEnumerable<TKey>>(_map.Keys.ToList());
        }

        /// <inheritdoc/>
        public Task<IEnumerable<TValue>> ValuesAsync()
        {
            return Task.FromResult<IEnumerable<TValue>>(_map.Values.ToList());
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            _map.Clear();
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task<bool> IsEmptyAsync()
        {
            return Task.FromResult(_map.Count == 0);
        }
    }
}
