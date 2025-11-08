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

namespace FlinkDotNet.TaskManager.Operators;

/// <summary>
/// Represents a record in a data stream with value and timestamp.
/// </summary>
/// <typeparam name="T">Type of the record value</typeparam>
public class StreamRecord<T>
{
    /// <summary>
    /// The record value
    /// </summary>
    public T Value
    {
        get; set;
    }

    /// <summary>
    /// Event timestamp
    /// </summary>
    public long Timestamp
    {
        get; set;
    }

    /// <summary>
    /// Create a stream record
    /// </summary>
    public StreamRecord(T value, long timestamp = 0)
    {
        Value = value;
        Timestamp = timestamp;
    }
}

/// <summary>
/// Base interface for all stream operators.
/// </summary>
/// <typeparam name="TIn">Input record type</typeparam>
/// <typeparam name="TOut">Output record type</typeparam>
public interface IOperator<TIn, TOut>
{
    /// <summary>
    /// Initialize the operator (called once before processing)
    /// </summary>
    Task OpenAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a single input record
    /// </summary>
    /// <param name="record">Input record to process</param>
    /// <param name="output">Output collector for emitting results</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ProcessRecordAsync(StreamRecord<TIn> record, IOutputCollector<TOut> output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalize the operator (called once after all records processed)
    /// </summary>
    Task CloseAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Output collector for emitting processed records.
/// </summary>
/// <typeparam name="T">Output record type</typeparam>
public interface IOutputCollector<T>
{
    /// <summary>
    /// Emit a record to downstream operators
    /// </summary>
    Task CollectAsync(StreamRecord<T> record, CancellationToken cancellationToken = default);
}

/// <summary>
/// Abstract base class for operators with common functionality.
/// </summary>
public abstract class AbstractOperator<TIn, TOut> : IOperator<TIn, TOut>
{
    /// <summary>
    /// Initialize the operator
    /// </summary>
    public virtual Task OpenAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Process a single record
    /// </summary>
    public abstract Task ProcessRecordAsync(StreamRecord<TIn> record, IOutputCollector<TOut> output, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finalize the operator
    /// </summary>
    public virtual Task CloseAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
