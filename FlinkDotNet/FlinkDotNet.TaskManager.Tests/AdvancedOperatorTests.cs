// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.TaskManager.Operators;
using FluentAssertions;

namespace FlinkDotNet.TaskManager.Tests;

/// <summary>
/// Tests for advanced stream operators: FlatMap, KeyedReduce, CountWindow, KeyedAggregate.
/// </summary>
public class AdvancedOperatorTests
{
    // ─────────────────────────────────────────────────────────
    // FlatMapOperator Tests
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void FlatMapOperator_Constructor_ThrowsOnNullFunction()
    {
        Action act = () => new FlatMapOperator<string, string>(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task FlatMapOperator_SplitsStringByWords()
    {
        // Arrange
        FlatMapOperator<string, string> op = new(s => s.Split(' '));
        TestOutputCollector<string> output = new();

        // Act
        await op.OpenAsync();
        await op.ProcessRecordAsync(new StreamRecord<string>("hello world"), output);
        await op.CloseAsync();

        // Assert
        output.CollectedRecords.Should().HaveCount(2);
        output.CollectedRecords[0].Value.Should().Be("hello");
        output.CollectedRecords[1].Value.Should().Be("world");
    }

    [Fact]
    public async Task FlatMapOperator_EmptyResult_EmitsNothing()
    {
        // Arrange - function that always returns empty
        FlatMapOperator<string, string> op = new(_ => []);
        TestOutputCollector<string> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<string>("ignore me"), output);

        // Assert
        output.CollectedRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task FlatMapOperator_MultipleInputRecords_EmitsAllOutputs()
    {
        // Arrange - expand each integer into a range [0..n)
        FlatMapOperator<int, int> op = new(n => Enumerable.Range(0, n));
        TestOutputCollector<int> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<int>(3), output);
        await op.ProcessRecordAsync(new StreamRecord<int>(2), output);

        // Assert: 3 + 2 = 5 output records
        output.CollectedRecords.Should().HaveCount(5);
        output.CollectedRecords.Select(r => r.Value).Should().Equal(0, 1, 2, 0, 1);
    }

    [Fact]
    public async Task FlatMapOperator_PreservesTimestamp()
    {
        // Arrange
        FlatMapOperator<string, char> op = new(s => s.ToCharArray());
        TestOutputCollector<char> output = new();

        long timestamp = 999L;

        // Act
        await op.ProcessRecordAsync(new StreamRecord<string>("ab", timestamp), output);

        // Assert
        output.CollectedRecords.Should().HaveCount(2);
        output.CollectedRecords[0].Timestamp.Should().Be(timestamp);
        output.CollectedRecords[1].Timestamp.Should().Be(timestamp);
    }

    [Fact]
    public async Task FlatMapOperator_RespectsCancellation()
    {
        // Arrange - returns many items; cancellation should stop iteration
        FlatMapOperator<int, int> op = new(n => Enumerable.Range(0, n));
        TestOutputCollector<int> output = new();
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert - should throw OperationCanceledException
        await Assert.ThrowsAsync<OperationCanceledException>(
            () => op.ProcessRecordAsync(new StreamRecord<int>(1000), output, cts.Token));
    }

    // ─────────────────────────────────────────────────────────
    // KeyedReduceOperator Tests
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void KeyedReduceOperator_Constructor_ThrowsOnNullKeySelector()
    {
        Action act = () => new KeyedReduceOperator<int, string>(null!, (a, b) => a + b);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void KeyedReduceOperator_Constructor_ThrowsOnNullReduceFunction()
    {
        Action act = () => new KeyedReduceOperator<int, string>(x => x.ToString(), null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task KeyedReduceOperator_SumsByKey()
    {
        // Arrange - key by first character, sum lengths
        KeyedReduceOperator<string, char> op = new(s => s[0], (a, b) => a + b);
        TestOutputCollector<string> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<string>("aa"), output);   // key='a', state="aa"
        await op.ProcessRecordAsync(new StreamRecord<string>("ab"), output);   // key='a', state="aa"+"ab"="aaab"
        await op.ProcessRecordAsync(new StreamRecord<string>("bb"), output);   // key='b', state="bb"

        // Assert - last emitted per key
        output.CollectedRecords[0].Value.Should().Be("aa");
        output.CollectedRecords[1].Value.Should().Be("aaab");
        output.CollectedRecords[2].Value.Should().Be("bb");
    }

    [Fact]
    public async Task KeyedReduceOperator_AccumulatesPerKey_IsolatedState()
    {
        // Arrange - integer sum per key
        KeyedReduceOperator<int, string> op = new(x => x % 2 == 0 ? "even" : "odd", (a, b) => a + b);
        TestOutputCollector<int> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<int>(2), output);   // even: 2
        await op.ProcessRecordAsync(new StreamRecord<int>(4), output);   // even: 6
        await op.ProcessRecordAsync(new StreamRecord<int>(1), output);   // odd:  1
        await op.ProcessRecordAsync(new StreamRecord<int>(3), output);   // odd:  4

        // Assert
        output.CollectedRecords[0].Value.Should().Be(2);
        output.CollectedRecords[1].Value.Should().Be(6);
        output.CollectedRecords[2].Value.Should().Be(1);
        output.CollectedRecords[3].Value.Should().Be(4);

        // State should reflect final values
        op.TryGetState("even", out int evenState).Should().BeTrue();
        evenState.Should().Be(6);
        op.TryGetState("odd", out int oddState).Should().BeTrue();
        oddState.Should().Be(4);
    }

    [Fact]
    public async Task KeyedReduceOperator_GetAllState_ReturnsAllKeys()
    {
        // Arrange
        KeyedReduceOperator<string, string> op = new(s => s, (a, b) => a + b);
        TestOutputCollector<string> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<string>("x"), output);
        await op.ProcessRecordAsync(new StreamRecord<string>("y"), output);
        await op.ProcessRecordAsync(new StreamRecord<string>("x"), output);

        // Assert
        IReadOnlyDictionary<string, string> allState = op.GetAllState();
        allState.Should().HaveCount(2);
        allState["x"].Should().Be("xx");
        allState["y"].Should().Be("y");
    }

    // ─────────────────────────────────────────────────────────
    // CountWindowOperator Tests
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void CountWindowOperator_Constructor_ThrowsOnInvalidSize()
    {
        Action act1 = () => new CountWindowOperator<int>(0);
        Action act2 = () => new CountWindowOperator<int>(-5);
        act1.Should().Throw<ArgumentOutOfRangeException>();
        act2.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public async Task CountWindowOperator_EmitsWhenWindowFull()
    {
        // Arrange
        CountWindowOperator<int> op = new(3);
        TestOutputCollector<IReadOnlyList<int>> output = new();

        // Act - add exactly one full window
        await op.ProcessRecordAsync(new StreamRecord<int>(1), output);
        await op.ProcessRecordAsync(new StreamRecord<int>(2), output);
        await op.ProcessRecordAsync(new StreamRecord<int>(3), output);

        // Assert - one window emitted
        output.CollectedRecords.Should().HaveCount(1);
        output.CollectedRecords[0].Value.Should().Equal(1, 2, 3);
    }

    [Fact]
    public async Task CountWindowOperator_DoesNotEmitBeforeWindowFull()
    {
        // Arrange
        CountWindowOperator<int> op = new(5);
        TestOutputCollector<IReadOnlyList<int>> output = new();

        // Act - only two records, window size = 5
        await op.ProcessRecordAsync(new StreamRecord<int>(10), output);
        await op.ProcessRecordAsync(new StreamRecord<int>(20), output);

        // Assert - no window emitted yet
        output.CollectedRecords.Should().BeEmpty();
        op.BufferedCount.Should().Be(2);
    }

    [Fact]
    public async Task CountWindowOperator_EmitsMultipleWindows()
    {
        // Arrange
        CountWindowOperator<string> op = new(2);
        TestOutputCollector<IReadOnlyList<string>> output = new();

        // Act - six records → three windows
        foreach (string s in new[] { "a", "b", "c", "d", "e", "f" })
        {
            await op.ProcessRecordAsync(new StreamRecord<string>(s), output);
        }

        // Assert
        output.CollectedRecords.Should().HaveCount(3);
        output.CollectedRecords[0].Value.Should().Equal("a", "b");
        output.CollectedRecords[1].Value.Should().Equal("c", "d");
        output.CollectedRecords[2].Value.Should().Equal("e", "f");
    }

    [Fact]
    public async Task CountWindowOperator_ResetsAfterEmitting()
    {
        // Arrange
        CountWindowOperator<int> op = new(2);
        TestOutputCollector<IReadOnlyList<int>> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<int>(1), output); // buffered
        await op.ProcessRecordAsync(new StreamRecord<int>(2), output); // window emitted, reset
        await op.ProcessRecordAsync(new StreamRecord<int>(3), output); // buffered

        // Assert
        output.CollectedRecords.Should().HaveCount(1);
        op.BufferedCount.Should().Be(1); // only record 3 buffered
    }

    // ─────────────────────────────────────────────────────────
    // KeyedAggregateOperator Tests
    // ─────────────────────────────────────────────────────────

    [Fact]
    public void KeyedAggregateOperator_Constructor_ThrowsOnNullArguments()
    {
        Func<int, string> keySelector = x => x.ToString();
        Func<long> createAcc = () => 0L;
        Func<long, int, long> add = (acc, val) => acc + val;
        Func<long, long> getResult = acc => acc;

        Action act1 = () => new KeyedAggregateOperator<int, long, long, string>(null!, createAcc, add, getResult);
        Action act2 = () => new KeyedAggregateOperator<int, long, long, string>(keySelector, null!, add, getResult);
        Action act3 = () => new KeyedAggregateOperator<int, long, long, string>(keySelector, createAcc, null!, getResult);
        Action act4 = () => new KeyedAggregateOperator<int, long, long, string>(keySelector, createAcc, add, null!);

        act1.Should().Throw<ArgumentNullException>();
        act2.Should().Throw<ArgumentNullException>();
        act3.Should().Throw<ArgumentNullException>();
        act4.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task KeyedAggregateOperator_CountsPerKey()
    {
        // Arrange - count occurrences per category
        KeyedAggregateOperator<string, int, int, string> op = new(
            keySelector: s => s,
            createAccumulator: () => 0,
            add: (acc, _) => acc + 1,
            getResult: acc => acc);
        TestOutputCollector<int> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<string>("cat"), output);
        await op.ProcessRecordAsync(new StreamRecord<string>("dog"), output);
        await op.ProcessRecordAsync(new StreamRecord<string>("cat"), output);
        await op.ProcessRecordAsync(new StreamRecord<string>("cat"), output);

        // Assert - emits cumulative count after each record
        output.CollectedRecords[0].Value.Should().Be(1); // cat: 1
        output.CollectedRecords[1].Value.Should().Be(1); // dog: 1
        output.CollectedRecords[2].Value.Should().Be(2); // cat: 2
        output.CollectedRecords[3].Value.Should().Be(3); // cat: 3
    }

    [Fact]
    public async Task KeyedAggregateOperator_SumsPerKey()
    {
        // Arrange - sum values per parity key; TOut=int so getResult casts long accumulator to int
        KeyedAggregateOperator<int, long, int, int> op = new(
            keySelector: x => x % 2 == 0 ? 0 : 1,
            createAccumulator: () => 0L,
            add: (acc, val) => acc + val,
            getResult: acc => (int)acc);
        TestOutputCollector<int> output = new();

        // Act
        await op.ProcessRecordAsync(new StreamRecord<int>(2), output);  // even: 2
        await op.ProcessRecordAsync(new StreamRecord<int>(3), output);  // odd:  3
        await op.ProcessRecordAsync(new StreamRecord<int>(4), output);  // even: 6
        await op.ProcessRecordAsync(new StreamRecord<int>(5), output);  // odd:  8

        // Assert
        output.CollectedRecords[0].Value.Should().Be(2);
        output.CollectedRecords[1].Value.Should().Be(3);
        output.CollectedRecords[2].Value.Should().Be(6);
        output.CollectedRecords[3].Value.Should().Be(8);

        // Verify final accumulator state
        op.TryGetAccumulator(0, out long evenAcc).Should().BeTrue();
        evenAcc.Should().Be(6L);
    }

    // ─────────────────────────────────────────────────────────
    // ListOutputCollector Tests
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task ListOutputCollector_CollectsRecords()
    {
        // Arrange
        ListOutputCollector<int> collector = new();

        // Act
        await collector.CollectAsync(new StreamRecord<int>(1, 100));
        await collector.CollectAsync(new StreamRecord<int>(2, 200));

        // Assert
        collector.Records.Should().HaveCount(2);
        collector.Values.Should().Equal(1, 2);
        collector.Records[0].Timestamp.Should().Be(100);
    }

    // ─────────────────────────────────────────────────────────
    // Full pipeline tests combining advanced operators
    // ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Pipeline_FlatMapThenFilter_ProducesCorrectResults()
    {
        // Arrange - split sentences, keep words longer than 3 chars
        FlatMapOperator<string, string> flatMap = new(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        FilterOperator<string> filter = new(s => s.Length > 3);
        TestOutputCollector<string> flatMapOutput = new();
        TestOutputCollector<string> filterOutput = new();

        string[] input = ["hi there world", "a bb ccc dddd"];

        // Act
        await flatMap.OpenAsync();
        foreach (string sentence in input)
        {
            await flatMap.ProcessRecordAsync(new StreamRecord<string>(sentence), flatMapOutput);
        }
        await flatMap.CloseAsync();

        await filter.OpenAsync();
        foreach (StreamRecord<string> word in flatMapOutput.CollectedRecords)
        {
            await filter.ProcessRecordAsync(word, filterOutput);
        }
        await filter.CloseAsync();

        // Assert
        filterOutput.CollectedRecords.Select(r => r.Value)
            .Should().Equal("there", "world", "dddd");
    }

    [Fact]
    public async Task Pipeline_FlatMapThenKeyedReduce_WordCount()
    {
        // Arrange - classic word count
        FlatMapOperator<string, string> flatMap = new(s => s.Split(' ', StringSplitOptions.RemoveEmptyEntries));
        KeyedReduceOperator<int, string> reduce = new(_ => "count", (a, b) => a + b);
        TestOutputCollector<string> flatMapOutput = new();
        TestOutputCollector<int> reduceOutput = new();

        // Act - two sentences
        await flatMap.ProcessRecordAsync(new StreamRecord<string>("the cat sat"), flatMapOutput);
        await flatMap.ProcessRecordAsync(new StreamRecord<string>("the dog ran"), flatMapOutput);

        // The flatMap produced 6 words; reduce them all by emitting '1' per word
        KeyedReduceOperator<int, string> wordCountReduce = new(
            _ => "total",
            (a, b) => a + b);
        TestOutputCollector<int> countOutput = new();
        foreach (StreamRecord<string> word in flatMapOutput.CollectedRecords)
        {
            await wordCountReduce.ProcessRecordAsync(new StreamRecord<int>(1, word.Timestamp), countOutput);
        }

        // Assert - running count increases by 1 each time
        countOutput.CollectedRecords.Select(r => r.Value).Should().Equal(1, 2, 3, 4, 5, 6);
    }
}
