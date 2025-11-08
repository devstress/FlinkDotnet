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
/// Source operator that emits records from a collection.
/// </summary>
public class CollectionSourceOperator<T> : AbstractOperator<object, T>
{
    private readonly IEnumerable<T> _source;

    public CollectionSourceOperator(IEnumerable<T> source)
    {
        _source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public override async Task ProcessRecordAsync(StreamRecord<object> record, IOutputCollector<T> output, CancellationToken cancellationToken = default)
    {
        // Source operator doesn't process input records, it emits from collection
        foreach (T item in _source)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await output.CollectAsync(new StreamRecord<T>(item, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()), cancellationToken);
        }
    }
}

/// <summary>
/// Map operator that transforms each record using a function.
/// </summary>
public class MapOperator<TIn, TOut> : AbstractOperator<TIn, TOut>
{
    private readonly Func<TIn, TOut> _mapFunction;

    public MapOperator(Func<TIn, TOut> mapFunction)
    {
        _mapFunction = mapFunction ?? throw new ArgumentNullException(nameof(mapFunction));
    }

    public override async Task ProcessRecordAsync(StreamRecord<TIn> record, IOutputCollector<TOut> output, CancellationToken cancellationToken = default)
    {
        TOut result = _mapFunction(record.Value);
        await output.CollectAsync(new StreamRecord<TOut>(result, record.Timestamp), cancellationToken);
    }
}

/// <summary>
/// Filter operator that only emits records matching a predicate.
/// </summary>
public class FilterOperator<T> : AbstractOperator<T, T>
{
    private readonly Func<T, bool> _filterFunction;

    public FilterOperator(Func<T, bool> filterFunction)
    {
        _filterFunction = filterFunction ?? throw new ArgumentNullException(nameof(filterFunction));
    }

    public override async Task ProcessRecordAsync(StreamRecord<T> record, IOutputCollector<T> output, CancellationToken cancellationToken = default)
    {
        if (_filterFunction(record.Value))
        {
            await output.CollectAsync(record, cancellationToken);
        }
    }
}

/// <summary>
/// Sink operator that collects records into a list.
/// </summary>
public class CollectionSinkOperator<T> : AbstractOperator<T, object>
{
    private readonly List<T> _results;

    public CollectionSinkOperator(List<T> results)
    {
        _results = results ?? throw new ArgumentNullException(nameof(results));
    }

    public IReadOnlyList<T> GetResults() => _results.AsReadOnly();

    public override Task ProcessRecordAsync(StreamRecord<T> record, IOutputCollector<object> output, CancellationToken cancellationToken = default)
    {
        _results.Add(record.Value);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Sink operator that writes records to console.
/// </summary>
public class ConsoleSinkOperator<T> : AbstractOperator<T, object>
{
    private readonly string _prefix;

    public ConsoleSinkOperator(string prefix = "")
    {
        _prefix = prefix;
    }

    public override Task ProcessRecordAsync(StreamRecord<T> record, IOutputCollector<object> output, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"{_prefix}{record.Value}");
        return Task.CompletedTask;
    }
}
