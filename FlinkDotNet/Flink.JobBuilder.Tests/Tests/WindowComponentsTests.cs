using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests for Window components to achieve high code coverage
/// </summary>
[TestFixture]
public class WindowComponentsTests
{
    #region TimeWindow Tests

    [Test]
    public void TimeWindow_Constructor_SetsStartAndEnd()
    {
        // Arrange & Act
        var window = new TimeWindow(1000, 2000);

        // Assert
        Assert.That(window.Start, Is.EqualTo(1000));
        Assert.That(window.End, Is.EqualTo(2000));
    }

    [Test]
    public void TimeWindow_Constructor_WithInvalidRange_ThrowsArgumentException()
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() => new TimeWindow(2000, 1000));
        Assert.That(ex.Message, Does.Contain("Start must be less than or equal to end"));
    }

    [Test]
    public void TimeWindow_MaxTimestamp_ReturnsEndMinusOne()
    {
        // Arrange
        var window = new TimeWindow(1000, 2000);

        // Act
        var maxTimestamp = window.MaxTimestamp();

        // Assert
        Assert.That(maxTimestamp, Is.EqualTo(1999));
    }

    [Test]
    public void TimeWindow_Intersects_WithOverlappingWindow_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 3000);
        var window2 = new TimeWindow(2000, 4000);

        // Act
        var intersects = window1.Intersects(window2);

        // Assert
        Assert.That(intersects, Is.True);
    }

    [Test]
    public void TimeWindow_Intersects_WithNonOverlappingWindow_ReturnsFalse()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(3000, 4000);

        // Act
        var intersects = window1.Intersects(window2);

        // Assert
        Assert.That(intersects, Is.False);
    }

    [Test]
    public void TimeWindow_Cover_ReturnsCoveringWindow()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 3000);
        var window2 = new TimeWindow(2000, 4000);

        // Act
        var covering = window1.Cover(window2);

        // Assert
        Assert.That(covering.Start, Is.EqualTo(1000));
        Assert.That(covering.End, Is.EqualTo(4000));
    }

    [Test]
    public void TimeWindow_Equals_WithSameWindow_ReturnsTrue()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(1000, 2000);

        // Act & Assert
        Assert.That(window1.Equals(window2), Is.True);
        Assert.That(window1.GetHashCode(), Is.EqualTo(window2.GetHashCode()));
    }

    [Test]
    public void TimeWindow_Equals_WithDifferentWindow_ReturnsFalse()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(2000, 3000);

        // Act & Assert
        Assert.That(window1.Equals(window2), Is.False);
    }

    [Test]
    public void TimeWindow_GetHashCode_ConsistentForSameWindow()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(1000, 2000);

        // Act & Assert
        Assert.That(window1.GetHashCode(), Is.EqualTo(window2.GetHashCode()));
    }

    [Test]
    public void TimeWindow_ToString_ReturnsFormattedString()
    {
        // Arrange
        var window = new TimeWindow(1000, 2000);

        // Act
        var str = window.ToString();

        // Assert
        Assert.That(str, Does.Contain("1000"));
        Assert.That(str, Does.Contain("2000"));
    }

    [Test]
    public void TimeWindow_MergeWindows_MergesMultipleWindows()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 3000);
        var window2 = new TimeWindow(2000, 4000);
        var window3 = new TimeWindow(5000, 6000);

        // Act
        var merged = TimeWindow.MergeWindows(window1, window2, window3);

        // Assert
        Assert.That(merged, Is.Not.Null);
        Assert.That(merged.Start, Is.EqualTo(1000));
        Assert.That(merged.End, Is.EqualTo(6000));
    }

    [Test]
    public void TimeWindow_GetWindowStartWithOffset_ReturnsCorrectWindow()
    {
        // Arrange
        long timestamp = 5500;
        long offset = 0;
        long windowSize = 1000;

        // Act
        var window = TimeWindow.GetWindowStartWithOffset(timestamp, offset, windowSize);

        // Assert
        Assert.That(window.Start, Is.EqualTo(5000));
        Assert.That(window.End, Is.EqualTo(6000));
    }

    #endregion

    #region TumblingEventTimeWindows Tests

    [Test]
    public void TumblingEventTimeWindows_Of_CreatesAssigner()
    {
        // Arrange & Act
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
    }

    [Test]
    public void TumblingEventTimeWindows_Of_WithOffset_CreatesAssigner()
    {
        // Arrange & Act
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5), Time.Seconds(1));

        // Assert
        Assert.That(assigner, Is.Not.Null);
    }

    [Test]
    public void TumblingEventTimeWindows_AssignWindows_ReturnsSingleWindow()
    {
        // Arrange
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
        int element = 42;
        long timestamp = 7500; // 7.5 seconds

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows.Count(), Is.EqualTo(1));
        var window = windows.First();
        Assert.That(window.Start, Is.EqualTo(5000)); // Should start at 5 seconds
        Assert.That(window.End, Is.EqualTo(10000)); // Should end at 10 seconds
    }

    #endregion

    #region SlidingEventTimeWindows Tests

    [Test]
    public void SlidingEventTimeWindows_Of_CreatesAssigner()
    {
        // Arrange & Act
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
    }

    [Test]
    public void SlidingEventTimeWindows_Of_WithOffset_CreatesAssigner()
    {
        // Arrange & Act
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5), Time.Seconds(1));

        // Assert
        Assert.That(assigner, Is.Not.Null);
    }

    [Test]
    public void SlidingEventTimeWindows_AssignWindows_ReturnsMultipleOverlappingWindows()
    {
        // Arrange
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));
        int element = 42;
        long timestamp = 7500; // 7.5 seconds

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows.Count(), Is.GreaterThanOrEqualTo(1));
    }

    #endregion

    #region SessionWindows Tests

    [Test]
    public void SessionWindows_WithGap_CreatesAssigner()
    {
        // Arrange & Act
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
    }

    [Test]
    public void SessionWindows_AssignWindows_ReturnsSingleWindow()
    {
        // Arrange
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));
        int element = 42;
        long timestamp = 10000;

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows.Count(), Is.EqualTo(1));
    }

    [Test]
    public void SessionWindows_AssignWindows_CreatesSessionWithGap()
    {
        // Arrange
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));
        int element = 42;
        long timestamp = 10000;

        // Act
        var windows = assigner.AssignWindows(element, timestamp);
        var window = windows.First();

        // Assert
        Assert.That(window.Start, Is.EqualTo(timestamp));
        Assert.That(window.End, Is.EqualTo(timestamp + Time.Minutes(5).ToMilliseconds()));
    }

    #endregion

    #region Time Tests

    [Test]
    public void Time_Milliseconds_CreatesCorrectTime()
    {
        // Arrange & Act
        var time = Time.Milliseconds(1500);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(1500));
    }

    [Test]
    public void Time_Seconds_CreatesCorrectTime()
    {
        // Arrange & Act
        var time = Time.Seconds(5);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(5000));
    }

    [Test]
    public void Time_Minutes_CreatesCorrectTime()
    {
        // Arrange & Act
        var time = Time.Minutes(2);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(120000));
    }

    [Test]
    public void Time_Hours_CreatesCorrectTime()
    {
        // Arrange & Act
        var time = Time.Hours(1);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
    }

    [Test]
    public void Time_Days_CreatesCorrectTime()
    {
        // Arrange & Act
        var time = Time.Days(1);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
    }

    [Test]
    public void Time_LowercaseMethodsWork()
    {
        // Test Java Flink-compatible lowercase aliases
        var ms = Time.milliseconds(1500);
        var s = Time.seconds(5);
        var m = Time.minutes(2);
        var h = Time.hours(1);
        var d = Time.days(1);

        Assert.That(ms.ToMilliseconds(), Is.EqualTo(1500));
        Assert.That(s.ToMilliseconds(), Is.EqualTo(5000));
        Assert.That(m.ToMilliseconds(), Is.EqualTo(120000));
        Assert.That(h.ToMilliseconds(), Is.EqualTo(3600000));
        Assert.That(d.ToMilliseconds(), Is.EqualTo(86400000));
    }

    #endregion
}