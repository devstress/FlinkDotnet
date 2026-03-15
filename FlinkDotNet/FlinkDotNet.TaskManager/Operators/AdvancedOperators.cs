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

using System.Collections.Concurrent;

namespace FlinkDotNet.TaskManager.Operators;

/// <summary>
/// FlatMap operator that transforms each input record into zero or more output records.
/// Equivalent to Apache Flink's FlatMapFunction.
/// </summary>
public class FlatMapOperator<TIn, TOut> : AbstractOperator<TIn, TOut>
{
    private readonly Func<TIn, IEnumerable<TOut>> _flatMapFunction;

    public FlatMapOperator(Func<TIn, IEnumerable<TOut>> flatMapFunction)
    {
        _flatMapFunction = flatMapFunction ?? throw new ArgumentNullException(nameof(flatMapFunction));
    }

    public override async Task ProcessRecordAsync(StreamRecord<TIn> record, IOutputCollector<TOut> output, CancellationToken cancellationToken = default)
    {
        IEnumerable<TOut> results = _flatMapFunction(record.Value);
        foreach (TOut item in results)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await output.CollectAsync(new StreamRecord<TOut>(item, record.Timestamp), cancellationToken);
        }
    }
}

/// <summary>
/// Keyed reduce operator that combines records with the same key using a reduce function.
/// Maintains in-memory state per key. Emits a result record each time a new record is processed.
/// Equivalent to Apache Flink's ReduceFunction applied on a KeyedStream.
/// </summary>
public class KeyedReduceOperator<T, TKey> : AbstractOperator<T, T>
    where TKey : notnull
{
    private readonly Func<T, TKey> _keySelector;
    private readonly Func<T, T, T> _reduceFunction;
    private readonly ConcurrentDictionary<TKey, T> _state = new();

    public KeyedReduceOperator(Func<T, TKey> keySelector, Func<T, T, T> reduceFunction)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _reduceFunction = reduceFunction ?? throw new ArgumentNullException(nameof(reduceFunction));
    }

    public override async Task ProcessRecordAsync(StreamRecord<T> record, IOutputCollector<T> output, CancellationToken cancellationToken = default)
    {
        TKey key = _keySelector(record.Value);
        T reduced = _state.AddOrUpdate(key, record.Value, (_, existing) => _reduceFunction(existing, record.Value));
        await output.CollectAsync(new StreamRecord<T>(reduced, record.Timestamp), cancellationToken);
    }

    /// <summary>
    /// Get the current accumulated state for a key (for testing/inspection).
    /// </summary>
    public bool TryGetState(TKey key, out T? value) => _state.TryGetValue(key, out value);

    /// <summary>
    /// Get all current state entries.
    /// </summary>
    public IReadOnlyDictionary<TKey, T> GetAllState() => _state;
}

/// <summary>
/// Tumbling count window operator that buffers records and emits when the window is full.
/// When the window reaches its count, it emits all records and resets.
/// Equivalent to a simplified version of Apache Flink's CountWindow.
/// </summary>
public class CountWindowOperator<T> : AbstractOperator<T, IReadOnlyList<T>>
{
    private readonly int _windowSize;
    private readonly List<T> _buffer = new();
    private long _windowOpenTimestamp;

    public CountWindowOperator(int windowSize)
    {
        if (windowSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be greater than zero.");
        _windowSize = windowSize;
        _windowOpenTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }

    public override async Task ProcessRecordAsync(StreamRecord<T> record, IOutputCollector<IReadOnlyList<T>> output, CancellationToken cancellationToken = default)
    {
        _buffer.Add(record.Value);

        if (_buffer.Count >= _windowSize)
        {
            // Snapshot the buffer before clearing so the emitted window is immutable.
            // The window record's timestamp is set to when this window was opened.
            IReadOnlyList<T> window = new List<T>(_buffer).AsReadOnly();
            await output.CollectAsync(new StreamRecord<IReadOnlyList<T>>(window, _windowOpenTimestamp), cancellationToken);
            _buffer.Clear();
            // Update the open timestamp for the next window
            _windowOpenTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    /// <summary>
    /// Discards any buffered records that have not yet filled a complete window.
    /// Partial windows are not emitted on close; use <see cref="BufferedCount"/> to
    /// inspect remaining records before closing if needed.
    /// </summary>
    public override Task CloseAsync(CancellationToken cancellationToken = default)
    {
        // Partial windows are intentionally discarded - count-based windows only emit
        // full windows. Callers can read BufferedCount before closing if needed.
        _buffer.Clear();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Current number of records buffered in the open window.
    /// </summary>
    public int BufferedCount => _buffer.Count;
}

/// <summary>
/// Aggregate operator that applies an aggregation function over a keyed stream.
/// Accumulates state per key and emits the current aggregated value after each record.
/// Equivalent to Apache Flink's AggregateFunction applied on a KeyedStream.
/// </summary>
/// <typeparam name="TIn">Input record type</typeparam>
/// <typeparam name="TAcc">Accumulator type</typeparam>
/// <typeparam name="TOut">Output type</typeparam>
/// <typeparam name="TKey">Key type</typeparam>
#pragma warning disable S2436 // Four generic parameters are required to express Input/Accumulator/Output/Key separately
public class KeyedAggregateOperator<TIn, TAcc, TOut, TKey> : AbstractOperator<TIn, TOut>
#pragma warning restore S2436
    where TKey : notnull
{
    private readonly Func<TIn, TKey> _keySelector;
    private readonly Func<TAcc> _createAccumulator;
    private readonly Func<TAcc, TIn, TAcc> _add;
    private readonly Func<TAcc, TOut> _getResult;
    private readonly ConcurrentDictionary<TKey, TAcc> _accumulators = new();

    public KeyedAggregateOperator(
        Func<TIn, TKey> keySelector,
        Func<TAcc> createAccumulator,
        Func<TAcc, TIn, TAcc> add,
        Func<TAcc, TOut> getResult)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
        _createAccumulator = createAccumulator ?? throw new ArgumentNullException(nameof(createAccumulator));
        _add = add ?? throw new ArgumentNullException(nameof(add));
        _getResult = getResult ?? throw new ArgumentNullException(nameof(getResult));
    }

    public override async Task ProcessRecordAsync(StreamRecord<TIn> record, IOutputCollector<TOut> output, CancellationToken cancellationToken = default)
    {
        TKey key = _keySelector(record.Value);
        TAcc acc = _accumulators.GetOrAdd(key, _ => _createAccumulator());

        // Apply the add function and update the accumulator
        TAcc newAcc = _add(acc, record.Value);
        _accumulators[key] = newAcc;

        TOut result = _getResult(newAcc);
        await output.CollectAsync(new StreamRecord<TOut>(result, record.Timestamp), cancellationToken);
    }

    /// <summary>
    /// Get the current accumulator state for a key (for testing/inspection).
    /// </summary>
    public bool TryGetAccumulator(TKey key, out TAcc? accumulator) => _accumulators.TryGetValue(key, out accumulator);
}

/// <summary>
/// Pass-through output collector that delegates all records to a downstream collector.
/// Useful for composing collector chains (e.g., union streams, logging wrappers).
/// </summary>
public class DelegatingOutputCollector<T> : IOutputCollector<T>
{
    private readonly IOutputCollector<T> _downstream;

    public DelegatingOutputCollector(IOutputCollector<T> downstream)
    {
        _downstream = downstream ?? throw new ArgumentNullException(nameof(downstream));
    }

    public Task CollectAsync(StreamRecord<T> record, CancellationToken cancellationToken = default)
    {
        return _downstream.CollectAsync(record, cancellationToken);
    }
}

/// <summary>
/// Pass-through output collector that collects records into a list for testing.
/// </summary>
public class ListOutputCollector<T> : IOutputCollector<T>
{
    private readonly List<StreamRecord<T>> _records = new();

    public IReadOnlyList<StreamRecord<T>> Records => _records.AsReadOnly();

    public IReadOnlyList<T> Values => _records.Select(r => r.Value).ToList().AsReadOnly();

    public Task CollectAsync(StreamRecord<T> record, CancellationToken cancellationToken = default)
    {
        _records.Add(record);
        return Task.CompletedTask;
    }
}
