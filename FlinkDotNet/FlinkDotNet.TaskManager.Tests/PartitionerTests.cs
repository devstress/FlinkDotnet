// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.TaskManager.Operators;
using FlinkDotNet.TaskManager.Partitioning;
using FluentAssertions;

namespace FlinkDotNet.TaskManager.Tests;

public class PartitionerTests
{
    [Fact]
    public void ForwardPartitioner_AlwaysReturnsZero()
    {
        // Arrange
        ForwardPartitioner<int> partitioner = new();
        StreamRecord<int> record = new(42);

        // Act & Assert
        partitioner.SelectChannel(record, 1).Should().Be(0);
        partitioner.SelectChannel(record, 4).Should().Be(0);
        partitioner.SelectChannel(record, 10).Should().Be(0);
    }

    [Fact]
    public void HashPartitioner_WithSameKey_ReturnsSameChannel()
    {
        // Arrange
        HashPartitioner<string> partitioner = new(s => s.Substring(0, 1)); // Hash by first character
        int numberOfChannels = 4;

        // Act
        int channel1 = partitioner.SelectChannel(new StreamRecord<string>("apple"), numberOfChannels);
        int channel2 = partitioner.SelectChannel(new StreamRecord<string>("apricot"), numberOfChannels);
        int channel3 = partitioner.SelectChannel(new StreamRecord<string>("avocado"), numberOfChannels);

        // Assert - All start with 'a', should go to same channel
        channel1.Should().Be(channel2);
        channel2.Should().Be(channel3);
    }

    [Fact]
    public void HashPartitioner_WithDifferentKeys_DistributesAcrossChannels()
    {
        // Arrange
        HashPartitioner<int> partitioner = new(x => x); // Hash by value
        int numberOfChannels = 4;
        HashSet<int> channels = new();

        // Act - Hash many different values
        for (int i = 0; i < 100; i++)
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            channels.Add(channel);
        }

        // Assert - Should use multiple channels (statistical distribution)
        channels.Count.Should().BeGreaterThan(1);
        channels.Should().OnlyContain(ch => ch >= 0 && ch < numberOfChannels);
    }

    [Fact]
    public void HashPartitioner_WithNullKeySelector_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new HashPartitioner<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HashPartitioner_WithZeroChannels_ThrowsArgumentException()
    {
        // Arrange
        HashPartitioner<int> partitioner = new(x => x);
        StreamRecord<int> record = new(42);

        // Act
        Action act = () => partitioner.SelectChannel(record, 0);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void RebalancePartitioner_DistributesInRoundRobin()
    {
        // Arrange
        RebalancePartitioner<int> partitioner = new();
        int numberOfChannels = 4;
        List<int> channels = new();

        // Act - Get channels for 12 records
        for (int i = 0; i < 12; i++)
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            channels.Add(channel);
        }

        // Assert - Should cycle through 0,1,2,3,0,1,2,3,0,1,2,3
        channels.Should().Equal(0, 1, 2, 3, 0, 1, 2, 3, 0, 1, 2, 3);
    }

    [Fact]
    public void RebalancePartitioner_WithZeroChannels_ThrowsArgumentException()
    {
        // Arrange
        RebalancePartitioner<int> partitioner = new();
        StreamRecord<int> record = new(42);

        // Act
        Action act = () => partitioner.SelectChannel(record, 0);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*positive*");
    }

    [Fact]
    public void RebalancePartitioner_IsThreadSafe()
    {
        // Arrange
        RebalancePartitioner<int> partitioner = new();
        int numberOfChannels = 4;
        List<int> allChannels = new();
        object lockObj = new();

        // Act - Select channels from multiple threads
        Parallel.For(0, 100, i =>
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            lock (lockObj)
            {
                allChannels.Add(channel);
            }
        });

        // Assert - All channels should be valid
        allChannels.Should().HaveCount(100);
        allChannels.Should().OnlyContain(ch => ch >= 0 && ch < numberOfChannels);
    }

    [Fact]
    public void BroadcastPartitioner_ReturnsSpecialValue()
    {
        // Arrange
        BroadcastPartitioner<int> partitioner = new();
        StreamRecord<int> record = new(42);

        // Act
        int channel = partitioner.SelectChannel(record, 4);

        // Assert
        channel.Should().Be(-1); // Special broadcast marker
        partitioner.IsBroadcast.Should().BeTrue();
    }

    [Fact]
    public void RescalePartitioner_DistributesInRoundRobin()
    {
        // Arrange
        RescalePartitioner<int> partitioner = new();
        int numberOfChannels = 3;
        List<int> channels = new();

        // Act
        for (int i = 0; i < 9; i++)
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            channels.Add(channel);
        }

        // Assert - Should cycle through 0,1,2,0,1,2,0,1,2
        channels.Should().Equal(0, 1, 2, 0, 1, 2, 0, 1, 2);
    }

    [Fact]
    public void ShufflePartitioner_DistributesRandomly()
    {
        // Arrange
        ShufflePartitioner<int> partitioner = new();
        int numberOfChannels = 4;
        Dictionary<int, int> channelCounts = new();

        // Act - Shuffle 1000 records
        for (int i = 0; i < 1000; i++)
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            channelCounts.TryGetValue(channel, out int count);
            channelCounts[channel] = count + 1;
        }

        // Assert - Should use all channels with reasonable distribution
        channelCounts.Keys.Should().HaveCount(numberOfChannels);
        channelCounts.Values.Should().OnlyContain(count => count > 150 && count < 350); // Rough balance check
    }

    [Fact]
    public void ShufflePartitioner_IsThreadSafe()
    {
        // Arrange
        ShufflePartitioner<int> partitioner = new();
        int numberOfChannels = 4;
        List<int> allChannels = new();
        object lockObj = new();

        // Act - Select channels from multiple threads
        Parallel.For(0, 100, i =>
        {
            int channel = partitioner.SelectChannel(new StreamRecord<int>(i), numberOfChannels);
            lock (lockObj)
            {
                allChannels.Add(channel);
            }
        });

        // Assert - All channels should be valid
        allChannels.Should().HaveCount(100);
        allChannels.Should().OnlyContain(ch => ch >= 0 && ch < numberOfChannels);
    }

    [Fact]
    public void AllPartitioners_ReturnValidChannelIndices()
    {
        // Arrange
        int numberOfChannels = 5;
        StreamRecord<int> record = new(42);
        IPartitioner<int>[] partitioners = new IPartitioner<int>[]
        {
            new ForwardPartitioner<int>(),
            new HashPartitioner<int>(x => x),
            new RebalancePartitioner<int>(),
            new RescalePartitioner<int>(),
            new ShufflePartitioner<int>()
        };

        // Act & Assert
        foreach (IPartitioner<int> partitioner in partitioners)
        {
            int channel = partitioner.SelectChannel(record, numberOfChannels);
            if (partitioner is not BroadcastPartitioner<int>)
            {
                channel.Should().BeInRange(0, numberOfChannels - 1);
            }
        }
    }
}
