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
using System.Linq;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.Runtime
{
    /// <summary>
    /// Default implementation of <see cref="IResultFuture{T}"/>.
    /// Collects results from async I/O operations, backed by a <see cref="TaskCompletionSource{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of result elements</typeparam>
    public sealed class ResultFuture<T> : IResultFuture<T>
    {
        private readonly TaskCompletionSource<IEnumerable<T>> _completionSource = new();

        /// <inheritdoc/>
        public void Complete(IEnumerable<T> results)
        {
            _completionSource.TrySetResult(results == null ? [] : results.ToList());
        }

        /// <inheritdoc/>
        public void CompleteExceptionally(Exception exception)
        {
            _completionSource.TrySetException(exception);
        }

        /// <summary>
        /// Gets the task that completes when results are available.
        /// </summary>
        public Task<IEnumerable<T>> ResultTask => _completionSource.Task;

        /// <summary>
        /// Gets whether the future has been completed (successfully or with exception).
        /// </summary>
        public bool IsCompleted => _completionSource.Task.IsCompleted;
    }
}
