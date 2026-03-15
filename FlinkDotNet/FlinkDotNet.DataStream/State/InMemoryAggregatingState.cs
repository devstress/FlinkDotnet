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
    /// In-memory implementation of <see cref="IAggregatingState{TIn, TOut}"/>.
    /// Applies an aggregate function to incoming elements, keeping an accumulator value.
    /// Suitable for testing and lightweight processing scenarios.
    /// </summary>
    /// <typeparam name="TIn">The type of input elements</typeparam>
    /// <typeparam name="TAcc">The type of the accumulator</typeparam>
    /// <typeparam name="TOut">The type of aggregated output</typeparam>
#pragma warning disable S2436 // Types and methods should not have too many generic parameters - Required for Apache Flink API compatibility
    public sealed class InMemoryAggregatingState<TIn, TAcc, TOut> : IAggregatingState<TIn, TOut>
#pragma warning restore S2436
    {
        private readonly IAggregateFunction<TIn, TAcc, TOut> _aggregateFunction;
        private TAcc _accumulator;
        private bool _hasValue;

        /// <summary>
        /// Initializes a new instance of <see cref="InMemoryAggregatingState{TIn, TAcc, TOut}"/>.
        /// </summary>
        /// <param name="aggregateFunction">The aggregate function to apply</param>
        public InMemoryAggregatingState(IAggregateFunction<TIn, TAcc, TOut> aggregateFunction)
        {
            _aggregateFunction = aggregateFunction ?? throw new ArgumentNullException(nameof(aggregateFunction));
            _accumulator = _aggregateFunction.CreateAccumulator();
        }

        /// <inheritdoc/>
        public Task<TOut> GetAsync()
        {
            return Task.FromResult(_aggregateFunction.GetResult(_accumulator));
        }

        /// <inheritdoc/>
        public Task AddAsync(TIn value)
        {
            if (!_hasValue)
            {
                _accumulator = _aggregateFunction.CreateAccumulator();
                _hasValue = true;
            }
            _accumulator = _aggregateFunction.Add(value, _accumulator);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task ClearAsync()
        {
            _accumulator = _aggregateFunction.CreateAccumulator();
            _hasValue = false;
            return Task.CompletedTask;
        }
    }
}
