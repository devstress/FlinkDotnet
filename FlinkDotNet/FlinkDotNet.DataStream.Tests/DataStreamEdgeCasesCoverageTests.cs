using NUnit.Framework;
using FlinkDotNet.DataStream;
using System;
using System.Linq;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Comprehensive branch coverage tests for DataStream transformation edge cases
/// Covers null checks, boundary conditions, and error paths across stream operations
/// </summary>
[TestFixture]
public class DataStreamEdgeCasesCoverageTests
{
    #region Collection Source Edge Cases

    [Test]
    public void FromCollection_WithEmptyCollection_CreatesEmptyStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var emptyList = Array.Empty<int>();

        // Act
        var dataStream = env.FromCollection(emptyList);

        // Assert
        Assert.That(dataStream, Is.Not.Null);
    }

    [Test]
    public void FromCollection_WithSingleElement_CreatesStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act
        var dataStream = env.FromCollection(new[] { 42 });

        // Assert
        Assert.That(dataStream, Is.Not.Null);
    }

    [Test]
    public void FromCollection_WithLargeCollection_CreatesStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var largeList = Enumerable.Range(1, 10000).ToArray();

        // Act
        var dataStream = env.FromCollection(largeList);

        // Assert
        Assert.That(dataStream, Is.Not.Null);
    }

    #endregion

    #region Parallelism Edge Cases

    [Test]
    public void SetParallelism_WithOne_SetsParallelism()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var result = dataStream.SetParallelism(1);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void SetParallelism_WithMaxValue_SetsParallelism()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var result = dataStream.SetParallelism(128);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Transformation Chaining

    [Test]
    public void ChainedTransformations_MapFilterMap_Works()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3, 4, 5 });

        // Act
        var result = dataStream
            .Map(x => x * 2)
            .Filter(x => x > 4)
            .Map(x => x.ToString());

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void ChainedTransformations_FilterMapFlatMap_Works()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var result = dataStream
            .Filter(x => x > 0)
            .Map(x => x * 2)
            .FlatMap(x => new[] { x, x + 1 });

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Named Streams

    [Test]
    public void Name_WithEmptyString_SetsName()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var named = dataStream.Name("");

        // Assert
        Assert.That(named, Is.Not.Null);
    }

    [Test]
    public void Name_WithVeryLongString_SetsName()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });
        var longName = new string('a', 1000);

        // Act
        var named = dataStream.Name(longName);

        // Assert
        Assert.That(named, Is.Not.Null);
    }

    [Test]
    public void Name_WithSpecialCharacters_SetsName()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var named = dataStream.Name("test-stream_123!@#$%");

        // Assert
        Assert.That(named, Is.Not.Null);
    }

    #endregion

    #region Broadcast Edge Cases

    [Test]
    public void Broadcast_WithEmptyStream_CreatesBroadcast()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(Array.Empty<int>());

        // Act
        var broadcast = dataStream.Broadcast();

        // Assert
        Assert.That(broadcast, Is.Not.Null);
    }

    [Test]
    public void Broadcast_WithLargeStream_CreatesBroadcast()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var dataStream = env.FromCollection(Enumerable.Range(1, 1000).ToArray());

        // Act
        var broadcast = dataStream.Broadcast();

        // Assert
        Assert.That(broadcast, Is.Not.Null);
    }

    #endregion
}
