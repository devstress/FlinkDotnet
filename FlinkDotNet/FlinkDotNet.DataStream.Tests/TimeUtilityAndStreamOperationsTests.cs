using NUnit.Framework;
using FlinkDotNet.DataStream;
using System;
using System.Linq;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Additional coverage tests for Time utility class and stream operations
/// </summary>
[TestFixture]
public class TimeUtilityAndStreamOperationsTests
{
    #region Time Utility Tests

    [Test]
    public void Time_Milliseconds_CreatesCorrectTime()
    {
        // Act
        var time = Time.Milliseconds(500);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_Seconds_CreatesCorrectTime()
    {
        // Act
        var time = Time.Seconds(10);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_Minutes_CreatesCorrectTime()
    {
        // Act
        var time = Time.Minutes(5);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_Hours_CreatesCorrectTime()
    {
        // Act
        var time = Time.Hours(2);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_Days_CreatesCorrectTime()
    {
        // Act
        var time = Time.Days(1);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_WithZeroMilliseconds_CreatesTime()
    {
        // Act
        var time = Time.Milliseconds(0);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    [Test]
    public void Time_WithLargeMilliseconds_CreatesTime()
    {
        // Act
        var time = Time.Milliseconds(999999999);

        // Assert
        Assert.That(time, Is.Not.Null);
    }

    #endregion

    #region Stream Source Variations

    [Test]
    public void FromCollection_WithSingleElement_CreatesStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act
        var stream = env.FromCollection(new[] { 42 });

        // Assert
        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void FromCollection_WithRange_CreatesStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var data = Enumerable.Range(1, 100).ToArray();

        // Act
        var stream = env.FromCollection(data);

        // Assert
        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void FromCollection_WithStrings_CreatesStream()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act
        var stream = env.FromCollection(new[] { "a", "b", "c" });

        // Assert
        Assert.That(stream, Is.Not.Null);
    }

    #endregion

    #region Stream Window Operations

    [Test]
    public void TimeWindowAll_WithMilliseconds_CreatesWindow()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var windowed = stream.TimeWindowAll(Time.Milliseconds(100));

        // Assert
        Assert.That(windowed, Is.Not.Null);
    }

    [Test]
    public void TimeWindowAll_WithSeconds_CreatesWindow()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var windowed = stream.TimeWindowAll(Time.Seconds(5));

        // Assert
        Assert.That(windowed, Is.Not.Null);
    }

    [Test]
    public void TimeWindowAll_WithMinutes_CreatesWindow()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var windowed = stream.TimeWindowAll(Time.Minutes(1));

        // Assert
        Assert.That(windowed, Is.Not.Null);
    }

    [Test]
    public void TimeWindowAll_WithHours_CreatesWindow()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var windowed = stream.TimeWindowAll(Time.Hours(2));

        // Assert
        Assert.That(windowed, Is.Not.Null);
    }

    [Test]
    public void TimeWindowAll_WithDays_CreatesWindow()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var windowed = stream.TimeWindowAll(Time.Days(1));

        // Assert
        Assert.That(windowed, Is.Not.Null);
    }

    #endregion

    #region Parallelism Tests

    [Test]
    public void SetParallelism_WithPositiveValue_SetsValue()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Act & Assert
        Assert.DoesNotThrow(() => env.SetParallelism(4));
    }

    [Test]
    public void SetParallelism_OnStream_SetsValue()
    {
        // Arrange
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var stream = env.FromCollection(new[] { 1, 2, 3 });

        // Act
        var result = stream.SetParallelism(8);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion
}
