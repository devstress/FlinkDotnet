// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.TaskManager.Operators;
using FluentAssertions;

namespace FlinkDotNet.TaskManager.Tests;

public class OperatorTests
{
    [Fact]
    public void StreamRecord_Constructor_SetsValueAndTimestamp()
    {
        // Arrange
        int value = 42;
        long timestamp = 123456;

        // Act
        StreamRecord<int> record = new(value, timestamp);

        // Assert
        record.Value.Should().Be(value);
        record.Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public void StreamRecord_DefaultTimestamp_IsZero()
    {
        // Arrange & Act
        StreamRecord<string> record = new("test");

        // Assert
        record.Value.Should().Be("test");
        record.Timestamp.Should().Be(0);
    }

    [Fact]
    public async Task MapOperator_TransformsRecords()
    {
        // Arrange
        MapOperator<int, string> mapOp = new(x => $"Value: {x}");
        TestOutputCollector<string> output = new();

        // Act
        await mapOp.OpenAsync();
        await mapOp.ProcessRecordAsync(new StreamRecord<int>(42, 100), output);
        await mapOp.CloseAsync();

        // Assert
        output.CollectedRecords.Should().HaveCount(1);
        output.CollectedRecords[0].Value.Should().Be("Value: 42");
        output.CollectedRecords[0].Timestamp.Should().Be(100);
    }

    [Fact]
    public void MapOperator_WithNullFunction_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new MapOperator<int, string>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FilterOperator_EmitsMatchingRecords()
    {
        // Arrange
        FilterOperator<int> filterOp = new(x => x > 10);
        TestOutputCollector<int> output = new();

        // Act
        await filterOp.OpenAsync();
        await filterOp.ProcessRecordAsync(new StreamRecord<int>(5), output);
        await filterOp.ProcessRecordAsync(new StreamRecord<int>(15), output);
        await filterOp.ProcessRecordAsync(new StreamRecord<int>(20), output);
        await filterOp.CloseAsync();

        // Assert
        output.CollectedRecords.Should().HaveCount(2);
        output.CollectedRecords[0].Value.Should().Be(15);
        output.CollectedRecords[1].Value.Should().Be(20);
    }

    [Fact]
    public void FilterOperator_WithNullPredicate_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new FilterOperator<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CollectionSourceOperator_EmitsAllItems()
    {
        // Arrange
        List<int> source = new() { 1, 2, 3, 4, 5 };
        CollectionSourceOperator<int> sourceOp = new(source);
        TestOutputCollector<int> output = new();

        // Act
        await sourceOp.OpenAsync();
        await sourceOp.ProcessRecordAsync(new StreamRecord<object>(new object()), output);
        await sourceOp.CloseAsync();

        // Assert
        output.CollectedRecords.Should().HaveCount(5);
        output.CollectedRecords.Select(r => r.Value).Should().BeEquivalentTo(new[] { 1, 2, 3, 4, 5 });
    }

    [Fact]
    public void CollectionSourceOperator_WithNullCollection_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new CollectionSourceOperator<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task CollectionSinkOperator_CollectsAllRecords()
    {
        // Arrange
        List<int> results = new();
        CollectionSinkOperator<int> sinkOp = new(results);
        TestOutputCollector<object> output = new();

        // Act
        await sinkOp.OpenAsync();
        await sinkOp.ProcessRecordAsync(new StreamRecord<int>(10), output);
        await sinkOp.ProcessRecordAsync(new StreamRecord<int>(20), output);
        await sinkOp.ProcessRecordAsync(new StreamRecord<int>(30), output);
        await sinkOp.CloseAsync();

        // Assert
        results.Should().HaveCount(3);
        results.Should().BeEquivalentTo(new[] { 10, 20, 30 });
    }

    [Fact]
    public void CollectionSinkOperator_WithNullList_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Action act = () => new CollectionSinkOperator<int>(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CollectionSinkOperator_GetResults_ReturnsReadOnlyList()
    {
        // Arrange
        List<int> results = new();
        CollectionSinkOperator<int> sinkOp = new(results);

        // Act
        IReadOnlyList<int> readOnlyResults = sinkOp.GetResults();

        // Assert
        readOnlyResults.Should().NotBeNull();
        readOnlyResults.Should().BeEmpty();
    }

    [Fact]
    public async Task OperatorPipeline_SourceMapFilterSink_ProcessesCorrectly()
    {
        // Arrange - Create a pipeline: Source -> Map -> Filter -> Sink
        List<int> source = new() { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        List<string> results = new();

        CollectionSourceOperator<int> sourceOp = new(source);
        MapOperator<int, int> mapOp = new(x => x * 2);  // Double each value
        FilterOperator<int> filterOp = new(x => x > 10); // Keep values > 10
        MapOperator<int, string> mapToStringOp = new(x => $"Result: {x}");
        CollectionSinkOperator<string> sinkOp = new(results);

        // Create collectors
        TestOutputCollector<int> sourceOutput = new();
        TestOutputCollector<int> mapOutput = new();
        TestOutputCollector<int> filterOutput = new();
        TestOutputCollector<string> mapStringOutput = new();
        TestOutputCollector<object> sinkOutput = new();

        // Act - Execute pipeline
        await sourceOp.OpenAsync();
        await mapOp.OpenAsync();
        await filterOp.OpenAsync();
        await mapToStringOp.OpenAsync();
        await sinkOp.OpenAsync();

        // Source -> Map
        await sourceOp.ProcessRecordAsync(new StreamRecord<object>(new object()), sourceOutput);
        foreach (StreamRecord<int> record in sourceOutput.CollectedRecords)
        {
            await mapOp.ProcessRecordAsync(record, mapOutput);
        }

        // Map -> Filter
        foreach (StreamRecord<int> record in mapOutput.CollectedRecords)
        {
            await filterOp.ProcessRecordAsync(record, filterOutput);
        }

        // Filter -> MapToString
        foreach (StreamRecord<int> record in filterOutput.CollectedRecords)
        {
            await mapToStringOp.ProcessRecordAsync(record, mapStringOutput);
        }

        // MapToString -> Sink
        foreach (StreamRecord<string> record in mapStringOutput.CollectedRecords)
        {
            await sinkOp.ProcessRecordAsync(record, sinkOutput);
        }

        await sinkOp.CloseAsync();
        await mapToStringOp.CloseAsync();
        await filterOp.CloseAsync();
        await mapOp.CloseAsync();
        await sourceOp.CloseAsync();

        // Assert
        // Input: 1,2,3,4,5,6,7,8,9,10 -> Map(*2): 2,4,6,8,10,12,14,16,18,20
        // -> Filter(>10): 12,14,16,18,20 -> MapToString: "Result: 12", etc.
        results.Should().HaveCount(5);
        results.Should().Contain("Result: 12");
        results.Should().Contain("Result: 14");
        results.Should().Contain("Result: 16");
        results.Should().Contain("Result: 18");
        results.Should().Contain("Result: 20");
    }
}

/// <summary>
/// Test output collector that captures emitted records
/// </summary>
internal class TestOutputCollector<T> : IOutputCollector<T>
{
    public List<StreamRecord<T>> CollectedRecords { get; } = new();

    public Task CollectAsync(StreamRecord<T> record, CancellationToken cancellationToken = default)
    {
        CollectedRecords.Add(record);
        return Task.CompletedTask;
    }
}
