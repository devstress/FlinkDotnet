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

using FlinkDotNet.TaskManager.Operators;

namespace FlinkDotNet.TaskManager.Partitioning;

/// <summary>
/// Strategy for partitioning data across downstream tasks.
/// </summary>
public interface IPartitioner<T>
{
    /// <summary>
    /// Select target subtask index for a record.
    /// </summary>
    /// <param name="record">The record to partition</param>
    /// <param name="numberOfChannels">Number of downstream channels</param>
    /// <returns>Target subtask index (0 to numberOfChannels-1)</returns>
    int SelectChannel(StreamRecord<T> record, int numberOfChannels);
}

/// <summary>
/// Forward partitioner - sends all records to the same downstream task.
/// Used for chaining operators on the same TaskManager.
/// </summary>
public class ForwardPartitioner<T> : IPartitioner<T>
{
    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        // Forward always goes to channel 0 (same subtask index)
        return 0;
    }
}

/// <summary>
/// Hash partitioner - distributes records based on key hash.
/// Ensures records with the same key go to the same downstream task.
/// </summary>
public class HashPartitioner<T> : IPartitioner<T>
{
    private readonly Func<T, object> _keySelector;

    public HashPartitioner(Func<T, object> keySelector)
    {
        _keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        if (numberOfChannels <= 0)
            throw new ArgumentException("Number of channels must be positive", nameof(numberOfChannels));

        object key = _keySelector(record.Value);
        int hashCode = key?.GetHashCode() ?? 0;

        // Ensure positive index
        return Math.Abs(hashCode % numberOfChannels);
    }
}

/// <summary>
/// Rebalance partitioner - distributes records in round-robin fashion.
/// Provides balanced load across downstream tasks.
/// </summary>
public class RebalancePartitioner<T> : IPartitioner<T>
{
    private int _nextChannel = 0;
    private readonly object _lock = new();

    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        if (numberOfChannels <= 0)
            throw new ArgumentException("Number of channels must be positive", nameof(numberOfChannels));

        lock (_lock)
        {
            int channel = _nextChannel;
            _nextChannel = (_nextChannel + 1) % numberOfChannels;
            return channel;
        }
    }
}

/// <summary>
/// Broadcast partitioner - sends each record to all downstream tasks.
/// </summary>
public class BroadcastPartitioner<T> : IPartitioner<T>
{
    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        // Broadcast is handled differently - this is just a marker
        // In actual implementation, the output collector would send to all channels
        return -1; // Special value indicating broadcast
    }

    /// <summary>
    /// Check if this is a broadcast partitioner
    /// </summary>
    public bool IsBroadcast => true;
}

/// <summary>
/// Rescale partitioner - distributes to subset of downstream tasks.
/// Similar to rebalance but only within a subset.
/// </summary>
public class RescalePartitioner<T> : IPartitioner<T>
{
    private int _nextChannel = 0;
    private readonly object _lock = new();

    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        if (numberOfChannels <= 0)
            throw new ArgumentException("Number of channels must be positive", nameof(numberOfChannels));

        lock (_lock)
        {
            int channel = _nextChannel;
            _nextChannel = (_nextChannel + 1) % numberOfChannels;
            return channel;
        }
    }
}

/// <summary>
/// Shuffle partitioner - randomly distributes records.
/// </summary>
public class ShufflePartitioner<T> : IPartitioner<T>
{
    private readonly Random _random = new();
    private readonly object _lock = new();

    public int SelectChannel(StreamRecord<T> record, int numberOfChannels)
    {
        if (numberOfChannels <= 0)
            throw new ArgumentException("Number of channels must be positive", nameof(numberOfChannels));

        lock (_lock)
        {
            return _random.Next(numberOfChannels);
        }
    }
}
