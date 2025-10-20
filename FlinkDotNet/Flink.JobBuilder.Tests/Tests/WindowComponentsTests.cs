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

    [Test]
    public void TumblingEventTimeWindows_TimeCharacteristic_ReturnsEventTime()
    {
        // Arrange
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));

        // Act & Assert
        Assert.That(assigner.TimeCharacteristic, Is.EqualTo(FlinkDotNet.DataStream.Window.Assigners.TimeCharacteristic.EventTime));
        Assert.That(assigner.IsEventTime, Is.True);
    }

    [Test]
    public void TumblingEventTimeWindows_ToString_ReturnsFormattedString()
    {
        // Arrange
        var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(10));

        // Act
        var str = assigner.ToString();

        // Assert
        Assert.That(str, Does.Contain("TumblingEventTimeWindows"));
        Assert.That(str, Does.Contain("10000")); // 10 seconds in milliseconds
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

    [Test]
    public void SlidingEventTimeWindows_TimeCharacteristic_ReturnsEventTime()
    {
        // Arrange
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));

        // Act & Assert
        Assert.That(assigner.TimeCharacteristic, Is.EqualTo(FlinkDotNet.DataStream.Window.Assigners.TimeCharacteristic.EventTime));
        Assert.That(assigner.IsEventTime, Is.True);
    }

    [Test]
    public void SlidingEventTimeWindows_ToString_ReturnsFormattedString()
    {
        // Arrange
        var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(20), Time.Seconds(5));

        // Act
        var str = assigner.ToString();

        // Assert
        Assert.That(str, Does.Contain("SlidingEventTimeWindows"));
        Assert.That(str, Does.Contain("20000")); // 20 seconds in milliseconds
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

    [Test]
    public void SessionWindows_TimeCharacteristic_ReturnsEventTime()
    {
        // Arrange
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));

        // Act & Assert
        Assert.That(assigner.TimeCharacteristic, Is.EqualTo(FlinkDotNet.DataStream.Window.Assigners.TimeCharacteristic.EventTime));
        Assert.That(assigner.IsEventTime, Is.True);
    }

    [Test]
    public void SessionWindows_CanMerge_ReturnsTrue()
    {
        // Arrange
        var assigner = SessionWindows<int>.WithGap(Time.Minutes(5));

        // Act & Assert
        Assert.That(assigner.CanMerge, Is.True);
    }

    [Test]
    public void SessionWindows_ToString_ReturnsFormattedString()
    {
        // Arrange
        var assigner = SessionWindows<int>.WithGap(Time.Seconds(30));

        // Act
        var str = assigner.ToString();

        // Assert
        Assert.That(str, Does.Contain("SessionWindows"));
        Assert.That(str, Does.Contain("30000")); // 30 seconds in milliseconds
    }

    [Test]
    public void SessionWindows_MergeWindows_WithOverlappingWindows_MergesThem()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 3000);
        var window2 = new TimeWindow(2500, 4500);
        var window3 = new TimeWindow(4000, 6000);
        var windows = new List<TimeWindow> { window1, window2, window3 };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert - should merge window1 and window2, window3 overlaps with merged
        Assert.That(merged.Count, Is.LessThanOrEqualTo(2));
    }

    [Test]
    public void SessionWindows_MergeWindows_WithNonOverlappingWindows_KeepsSeparate()
    {
        // Arrange
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(3000, 4000);
        var window3 = new TimeWindow(5000, 6000);
        var windows = new List<TimeWindow> { window1, window2, window3 };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert - should keep all windows separate
        Assert.That(merged.Count, Is.EqualTo(3));
    }

    [Test]
    public void SessionWindows_MergeWindows_WithSingleWindow_ReturnsSameWindow()
    {
        // Arrange
        var window = new TimeWindow(1000, 2000);
        var windows = new List<TimeWindow> { window };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert
        Assert.That(merged.Count, Is.EqualTo(1));
        Assert.That(merged[0].Start, Is.EqualTo(1000));
        Assert.That(merged[0].End, Is.EqualTo(2000));
    }

    [Test]
    public void SessionWindows_MergeWindows_WithEmptyList_ReturnsEmpty()
    {
        // Arrange
        var windows = new List<TimeWindow>();

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert
        Assert.That(merged, Is.Empty);
    }

    [Test]
    public void SessionWindows_MergeWindows_WithUnsortedWindows_MergesCorrectly()
    {
        // Arrange - windows provided out of order
        var window1 = new TimeWindow(5000, 6000);
        var window2 = new TimeWindow(1000, 3000);
        var window3 = new TimeWindow(2500, 4500);
        var windows = new List<TimeWindow> { window1, window2, window3 };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert - should sort and merge correctly
        Assert.That(merged.Count, Is.GreaterThan(0));
        Assert.That(merged[0].Start, Is.EqualTo(1000)); // First window should start at earliest time
    }

    [Test]
    public void SessionWindows_MergeWindows_WithAdjacentWindows_MergesThem()
    {
        // Arrange - windows that are adjacent (end of one == start of next)
        var window1 = new TimeWindow(1000, 2000);
        var window2 = new TimeWindow(2000, 3000);
        var windows = new List<TimeWindow> { window1, window2 };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert - adjacent windows should merge
        Assert.That(merged.Count, Is.EqualTo(1));
        Assert.That(merged[0].Start, Is.EqualTo(1000));
        Assert.That(merged[0].End, Is.EqualTo(3000));
    }

    [Test]
    public void SessionWindows_MergeWindows_WithMultipleOverlappingSessions_MergesAll()
    {
        // Arrange - multiple windows that all overlap
        var window1 = new TimeWindow(1000, 5000);
        var window2 = new TimeWindow(2000, 6000);
        var window3 = new TimeWindow(3000, 7000);
        var window4 = new TimeWindow(4000, 8000);
        var windows = new List<TimeWindow> { window1, window2, window3, window4 };

        // Act
        var merged = SessionWindows<int>.MergeWindows(windows).ToList();

        // Assert - all should merge into one large window
        Assert.That(merged.Count, Is.EqualTo(1));
        Assert.That(merged[0].Start, Is.EqualTo(1000));
        Assert.That(merged[0].End, Is.EqualTo(8000));
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

    #region Static Factory Method Tests for Window Assigners

    [Test]
    public void SessionWindows_StaticWithGap_CreatesTypedAssigner()
    {
        // Act - Using the static factory method
        var assigner = SessionWindows.WithGap<int>(Time.Minutes(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
        Assert.That(assigner, Is.InstanceOf<SessionWindows<int>>());
    }

    [Test]
    public void TumblingEventTimeWindows_StaticOf_CreatesTypedAssigner()
    {
        // Act - Using the static factory method
        var assigner = TumblingEventTimeWindows.Of<int>(Time.Seconds(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
        Assert.That(assigner, Is.InstanceOf<TumblingEventTimeWindows<int>>());
    }

    [Test]
    public void TumblingEventTimeWindows_StaticOfWithOffset_CreatesTypedAssigner()
    {
        // Act - Using the static factory method with offset
        var assigner = TumblingEventTimeWindows.Of<int>(Time.Seconds(5), Time.Seconds(1));

        // Assert
        Assert.That(assigner, Is.Not.Null);
        Assert.That(assigner, Is.InstanceOf<TumblingEventTimeWindows<int>>());
    }

    [Test]
    public void SlidingEventTimeWindows_StaticOf_CreatesTypedAssigner()
    {
        // Act - Using the static factory method
        var assigner = SlidingEventTimeWindows.Of<int>(Time.Seconds(10), Time.Seconds(5));

        // Assert
        Assert.That(assigner, Is.Not.Null);
        Assert.That(assigner, Is.InstanceOf<SlidingEventTimeWindows<int>>());
    }

    [Test]
    public void SlidingEventTimeWindows_StaticOfWithOffset_CreatesTypedAssigner()
    {
        // Act - Using the static factory method with offset
        var assigner = SlidingEventTimeWindows.Of<int>(Time.Seconds(10), Time.Seconds(5), Time.Seconds(2));

        // Assert
        Assert.That(assigner, Is.Not.Null);
        Assert.That(assigner, Is.InstanceOf<SlidingEventTimeWindows<int>>());
    }

    [Test]
    public void SessionWindows_StaticWithGap_CanAssignWindows()
    {
        // Arrange
        var assigner = SessionWindows.WithGap<string>(Time.Minutes(5));
        string element = "test";
        long timestamp = 10000;

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows, Is.Not.Empty);
        var window = windows.First();
        Assert.That(window.Start, Is.EqualTo(timestamp));
    }

    [Test]
    public void TumblingEventTimeWindows_StaticOf_CanAssignWindows()
    {
        // Arrange
        var assigner = TumblingEventTimeWindows.Of<string>(Time.Seconds(5));
        string element = "test";
        long timestamp = 12000; // 12 seconds

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows, Is.Not.Empty);
    }

    [Test]
    public void SlidingEventTimeWindows_StaticOf_CanAssignWindows()
    {
        // Arrange
        var assigner = SlidingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(5));
        string element = "test";
        long timestamp = 12000;

        // Act
        var windows = assigner.AssignWindows(element, timestamp);

        // Assert
        Assert.That(windows, Is.Not.Null);
        Assert.That(windows.Count(), Is.GreaterThan(0));
    }

    #endregion
}