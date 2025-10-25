using System;
using FlinkDotNet.DataStream.Window;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Branch coverage tests for TimeWindow class to achieve 100% branch coverage
/// </summary>
[TestFixture]
public class TimeWindowBranchCoverageTests
{
    #region Constructor Tests

    [Test]
    public void TimeWindow_Constructor_WithStartGreaterThanEnd_ThrowsArgumentException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentException>(() => new TimeWindow(100, 50));

    [Test]
    public void TimeWindow_Constructor_WithStartEqualToEnd_DoesNotThrow() =>
        // Arrange & Act & Assert
        Assert.DoesNotThrow(() => new TimeWindow(100, 100));

    [Test]
    public void TimeWindow_Constructor_WithValidRange_CreatesWindow()
    {
        // Arrange & Act
        var window = new TimeWindow(0, 1000);

        // Assert
        Assert.That(window.Start, Is.EqualTo(0));
        Assert.That(window.End, Is.EqualTo(1000));
    }

    #endregion

    #region Intersects Tests

    [Test]
    public void TimeWindow_Intersects_WithOverlappingWindow_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(50, 150);

        // Act
        var result = window1.Intersects(window2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TimeWindow_Intersects_WithNonOverlappingWindow_ReturnsFalse()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(100, 200);

        // Act
        var result = window1.Intersects(window2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TimeWindow_Intersects_WithCompletelyContainedWindow_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(0, 200);
        var window2 = new TimeWindow(50, 150);

        // Act
        var result = window1.Intersects(window2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TimeWindow_Intersects_WithWindowsReversed_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(50, 150);
        var window2 = new TimeWindow(0, 200);

        // Act
        var result = window1.Intersects(window2);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region MergeWindows Tests

    [Test]
    public void TimeWindow_MergeWindows_WithEmptyArray_ThrowsArgumentException() =>
        // Arrange & Act & Assert
        _ = Assert.Throws<ArgumentException>(() => TimeWindow.MergeWindows());

    [Test]
    public void TimeWindow_MergeWindows_WithSingleWindow_ReturnsCoveringWindow()
    {
        // Arrange
        var window = new TimeWindow(100, 200);

        // Act
        var result = TimeWindow.MergeWindows(window);

        // Assert
        Assert.That(result.Start, Is.EqualTo(100));
        Assert.That(result.End, Is.EqualTo(200));
    }

    [Test]
    public void TimeWindow_MergeWindows_WithMultipleWindows_ReturnsCoveringWindow()
    {
        // Arrange
        var window1 = new TimeWindow(100, 200);
        var window2 = new TimeWindow(150, 250);
        var window3 = new TimeWindow(50, 175);

        // Act
        var result = TimeWindow.MergeWindows(window1, window2, window3);

        // Assert
        Assert.That(result.Start, Is.EqualTo(50));
        Assert.That(result.End, Is.EqualTo(250));
    }

    [Test]
    public void TimeWindow_MergeWindows_WithNonOverlappingWindows_ReturnsSpanningWindow()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(200, 300);

        // Act
        var result = TimeWindow.MergeWindows(window1, window2);

        // Assert
        Assert.That(result.Start, Is.EqualTo(0));
        Assert.That(result.End, Is.EqualTo(300));
    }

    #endregion

    #region Equals Tests

    [Test]
    public void TimeWindow_Equals_WithNull_ReturnsFalse()
    {
        // Arrange
        var window = new TimeWindow(0, 100);

        // Act
        var result = window.Equals(null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TimeWindow_Equals_WithNonTimeWindowObject_ReturnsFalse()
    {
        // Arrange
        var window = new TimeWindow(0, 100);
        var other = new object();

        // Act
        var result = window.Equals(other);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TimeWindow_Equals_WithSameWindow_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(0, 100);

        // Act
        var result = window1.Equals(window2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TimeWindow_Equals_WithDifferentStart_ReturnsFalse()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(50, 100);

        // Act
        var result = window1.Equals(window2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TimeWindow_Equals_WithDifferentEnd_ReturnsFalse()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(0, 150);

        // Act
        var result = window1.Equals(window2);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Additional Coverage Tests

    [Test]
    public void TimeWindow_MaxTimestamp_ReturnsEndMinusOne()
    {
        // Arrange
        var window = new TimeWindow(0, 1000);

        // Act
        var result = window.MaxTimestamp();

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void TimeWindow_Cover_WithOverlappingWindow_ReturnsMinimalCoveringWindow()
    {
        // Arrange
        var window1 = new TimeWindow(100, 200);
        var window2 = new TimeWindow(150, 250);

        // Act
        var result = window1.Cover(window2);

        // Assert
        Assert.That(result.Start, Is.EqualTo(100));
        Assert.That(result.End, Is.EqualTo(250));
    }

    [Test]
    public void TimeWindow_Cover_WithSmallerWindow_ReturnsLargerWindow()
    {
        // Arrange
        var window1 = new TimeWindow(0, 300);
        var window2 = new TimeWindow(100, 200);

        // Act
        var result = window1.Cover(window2);

        // Assert
        Assert.That(result.Start, Is.EqualTo(0));
        Assert.That(result.End, Is.EqualTo(300));
    }

    [Test]
    public void TimeWindow_GetWindowStartWithOffset_WithZeroOffset_CalculatesCorrectly()
    {
        // Arrange
        long timestamp = 5000;
        long offset = 0;
        long windowSize = 1000;

        // Act
        var window = TimeWindow.GetWindowStartWithOffset(timestamp, offset, windowSize);

        // Assert
        Assert.That(window.Start, Is.EqualTo(5000));
        Assert.That(window.End, Is.EqualTo(6000));
    }

    [Test]
    public void TimeWindow_GetWindowStartWithOffset_WithNonZeroOffset_CalculatesCorrectly()
    {
        // Arrange
        long timestamp = 5500;
        long offset = 100;
        long windowSize = 1000;

        // Act
        var window = TimeWindow.GetWindowStartWithOffset(timestamp, offset, windowSize);

        // Assert
        Assert.That(window.End - window.Start, Is.EqualTo(windowSize));
    }

    [Test]
    public void TimeWindow_GetHashCode_ForEqualWindows_ReturnsEqualHashes()
    {
        // Arrange
        var window1 = new TimeWindow(0, 100);
        var window2 = new TimeWindow(0, 100);

        // Act
        var hash1 = window1.GetHashCode();
        var hash2 = window2.GetHashCode();

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void TimeWindow_ToString_ReturnsCorrectFormat()
    {
        // Arrange
        var window = new TimeWindow(100, 200);

        // Act
        var result = window.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("TimeWindow[100, 200)"));
    }

    #endregion
}
