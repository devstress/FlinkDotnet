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
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream
{
    #region State Interfaces

    /// <summary>
    /// Interface for partitioned single-value state.
    /// Corresponds to org.apache.flink.api.common.state.ValueState in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the state</typeparam>
    public interface IValueState<T>
    {
        /// <summary>
        /// Gets the current value of the state.
        /// </summary>
        /// <returns>The current value or default if not set</returns>
        public Task<T> ValueAsync();

        /// <summary>
        /// Updates the value of the state.
        /// </summary>
        /// <param name="value">The new value</param>
        public Task UpdateAsync(T value);

        /// <summary>
        /// Clears the state.
        /// </summary>
        public Task ClearAsync();
    }

    /// <summary>
    /// Interface for partitioned list state.
    /// Corresponds to org.apache.flink.api.common.state.ListState in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    public interface IListState<T>
    {
        /// <summary>
        /// Gets all elements in the state.
        /// </summary>
        /// <returns>An enumerable of all elements</returns>
        public Task<IEnumerable<T>> GetAsync();

        /// <summary>
        /// Adds an element to the state.
        /// </summary>
        /// <param name="value">The element to add</param>
        public Task AddAsync(T value);

        /// <summary>
        /// Adds multiple elements to the state.
        /// </summary>
        /// <param name="values">The elements to add</param>
        public Task AddAllAsync(IEnumerable<T> values);

        /// <summary>
        /// Updates the state with a new list of elements, replacing existing content.
        /// </summary>
        /// <param name="values">The new list of elements</param>
        public Task UpdateAsync(IEnumerable<T> values);

        /// <summary>
        /// Clears the state.
        /// </summary>
        public Task ClearAsync();
    }

    /// <summary>
    /// Interface for partitioned key-value map state.
    /// Corresponds to org.apache.flink.api.common.state.MapState in Java Flink.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the map</typeparam>
    /// <typeparam name="TValue">The type of values in the map</typeparam>
    public interface IMapState<TKey, TValue>
    {
        /// <summary>
        /// Gets the value associated with the given key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>The value or default if not found</returns>
        public Task<TValue> GetAsync(TKey key);

        /// <summary>
        /// Associates the specified value with the specified key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        public Task PutAsync(TKey key, TValue value);

        /// <summary>
        /// Copies all mappings from the specified map to the state.
        /// </summary>
        /// <param name="map">The map to copy from</param>
        public Task PutAllAsync(IDictionary<TKey, TValue> map);

        /// <summary>
        /// Removes the mapping for the given key.
        /// </summary>
        /// <param name="key">The key</param>
        public Task RemoveAsync(TKey key);

        /// <summary>
        /// Checks if the state contains a mapping for the given key.
        /// </summary>
        /// <param name="key">The key</param>
        /// <returns>True if the key exists</returns>
        public Task<bool> ContainsAsync(TKey key);

        /// <summary>
        /// Gets an enumerable of all entries in the state.
        /// </summary>
        /// <returns>An enumerable of key-value pairs</returns>
        public Task<IEnumerable<KeyValuePair<TKey, TValue>>> EntriesAsync();

        /// <summary>
        /// Gets an enumerable of all keys in the state.
        /// </summary>
        /// <returns>An enumerable of keys</returns>
        public Task<IEnumerable<TKey>> KeysAsync();

        /// <summary>
        /// Gets an enumerable of all values in the state.
        /// </summary>
        /// <returns>An enumerable of values</returns>
        public Task<IEnumerable<TValue>> ValuesAsync();

        /// <summary>
        /// Clears the state.
        /// </summary>
        public Task ClearAsync();

        /// <summary>
        /// Checks if the state is empty.
        /// </summary>
        /// <returns>True if empty</returns>
        public Task<bool> IsEmptyAsync();
    }

    /// <summary>
    /// Interface for partitioned reducing state.
    /// Corresponds to org.apache.flink.api.common.state.ReducingState in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    public interface IReducingState<T>
    {
        /// <summary>
        /// Gets the current reduced value.
        /// </summary>
        /// <returns>The reduced value or default if empty</returns>
        public Task<T> GetAsync();

        /// <summary>
        /// Adds a new element and applies the reduce function.
        /// </summary>
        /// <param name="value">The element to add</param>
        public Task AddAsync(T value);

        /// <summary>
        /// Clears the state.
        /// </summary>
        public Task ClearAsync();
    }

    /// <summary>
    /// Interface for partitioned aggregating state.
    /// Corresponds to org.apache.flink.api.common.state.AggregatingState in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TOut">The type of aggregated output</typeparam>
    public interface IAggregatingState<TIn, TOut>
    {
        /// <summary>
        /// Gets the current aggregated value.
        /// </summary>
        /// <returns>The aggregated value or default if empty</returns>
        public Task<TOut> GetAsync();

        /// <summary>
        /// Adds a new element and updates the aggregate.
        /// </summary>
        /// <param name="value">The element to add</param>
        public Task AddAsync(TIn value);

        /// <summary>
        /// Clears the state.
        /// </summary>
        public Task ClearAsync();
    }

    #endregion

    #region State Descriptors

    /// <summary>
    /// Base class for state descriptors.
    /// Corresponds to org.apache.flink.api.common.state.StateDescriptor in Java Flink.
    /// </summary>
    public abstract class StateDescriptor
    {
        /// <summary>
        /// Gets the name of the state.
        /// </summary>
        public string Name
        {
            get;
        }

        protected StateDescriptor(string name) => this.Name = name ?? throw new System.ArgumentNullException(nameof(name));
    }

    /// <summary>
    /// Descriptor for value state.
    /// Corresponds to org.apache.flink.api.common.state.ValueStateDescriptor in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of value stored in the state</typeparam>
    public class ValueStateDescriptor<T> : StateDescriptor
    {
        /// <summary>
        /// Gets the type information for the state value.
        /// </summary>
        public System.Type ValueType
        {
            get;
        }

        public ValueStateDescriptor(string name) : base(name) => this.ValueType = typeof(T);
    }

    /// <summary>
    /// Descriptor for list state.
    /// Corresponds to org.apache.flink.api.common.state.ListStateDescriptor in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list</typeparam>
    public class ListStateDescriptor<T> : StateDescriptor
    {
        /// <summary>
        /// Gets the type information for list elements.
        /// </summary>
        public System.Type ElementType
        {
            get;
        }

        public ListStateDescriptor(string name) : base(name) => this.ElementType = typeof(T);
    }

    /// <summary>
    /// Descriptor for map state.
    /// Corresponds to org.apache.flink.api.common.state.MapStateDescriptor in Java Flink.
    /// </summary>
    /// <typeparam name="TKey">The type of keys</typeparam>
    /// <typeparam name="TValue">The type of values</typeparam>
    public class MapStateDescriptor<TKey, TValue> : StateDescriptor
    {
        /// <summary>
        /// Gets the type information for map keys.
        /// </summary>
        public System.Type KeyType
        {
            get;
        }

        /// <summary>
        /// Gets the type information for map values.
        /// </summary>
        public System.Type ValueType
        {
            get;
        }

        public MapStateDescriptor(string name) : base(name)
        {
            this.KeyType = typeof(TKey);
            this.ValueType = typeof(TValue);
        }
    }

    /// <summary>
    /// Descriptor for reducing state.
    /// Corresponds to org.apache.flink.api.common.state.ReducingStateDescriptor in Java Flink.
    /// </summary>
    /// <typeparam name="T">The type of elements</typeparam>
    public class ReducingStateDescriptor<T> : StateDescriptor
    {
        public IReduceFunction<T> ReduceFunction
        {
            get;
        }

        public ReducingStateDescriptor(string name, IReduceFunction<T> reduceFunction)
            : base(name) =>
            this.ReduceFunction = reduceFunction ?? throw new System.ArgumentNullException(nameof(reduceFunction));
    }

    /// <summary>
    /// Descriptor for aggregating state.
    /// Corresponds to org.apache.flink.api.common.state.AggregatingStateDescriptor in Java Flink.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TAcc">The type of accumulator</typeparam>
    /// <typeparam name="TOut">The type of output</typeparam>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters - Required for Apache Flink API compatibility
    public class AggregatingStateDescriptor<TIn, TAcc, TOut> : StateDescriptor
#pragma warning restore S2436
    {
        public IAggregateFunction<TIn, TAcc, TOut> AggregateFunction
        {
            get;
        }

        public AggregatingStateDescriptor(string name, IAggregateFunction<TIn, TAcc, TOut> aggregateFunction)
            : base(name) =>
            this.AggregateFunction = aggregateFunction ?? throw new System.ArgumentNullException(nameof(aggregateFunction));
    }

    #endregion
}
