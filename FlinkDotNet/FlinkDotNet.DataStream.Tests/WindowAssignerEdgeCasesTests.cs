using NUnit.Framework;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using System;
using System.Linq;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for Window Assigner edge cases to achieve 100% coverage.
    /// Tests boundary conditions, negative timestamps, and special cases.
    /// </summary>
    [TestFixture]
    public class WindowAssignerEdgeCasesTests
    {
        #region TumblingEventTimeWindows Edge Cases

        [Test]
        public void TumblingEventTimeWindows_WithOffset_AssignsCorrectly()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act
            var windows = assigner.AssignWindows("test", 15000L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(15000L));
            Assert.That(windows[0].End, Is.EqualTo(25000L));
        }

        [Test]
        public void TumblingEventTimeWindows_NegativeTimestamp_HandlesCorrectly()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act - Negative timestamps should still produce windows
            var windows = assigner.AssignWindows("test", -5000L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.LessThanOrEqualTo(-5000L));
            Assert.That(windows[0].End, Is.GreaterThan(-5000L));
        }

        [Test]
        public void TumblingEventTimeWindows_TimeCharacteristic_IsEventTime()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act & Assert
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void TumblingEventTimeWindows_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act
            var result = assigner.ToString();
            
            // Assert
            Assert.That(result, Does.Contain("TumblingEventTimeWindows"));
            Assert.That(result, Does.Contain("10000ms"));
        }

        [Test]
        public void TumblingEventTimeWindows_StaticHelper_CreatesCorrectInstance()
        {
            // Arrange & Act
            var assigner1 = TumblingEventTimeWindows.Of<string>(Time.Seconds(10));
            var assigner2 = TumblingEventTimeWindows.Of<int>(Time.Seconds(5), Time.Seconds(2));
            
            // Assert
            Assert.That(assigner1, Is.Not.Null);
            Assert.That(assigner2, Is.Not.Null);
            Assert.That(assigner1.IsEventTime, Is.True);
            Assert.That(assigner2.IsEventTime, Is.True);
        }

        #endregion

        #region SlidingEventTimeWindows Edge Cases

        [Test]
        public void SlidingEventTimeWindows_WithOffset_AssignsMultipleWindows()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5), Time.Seconds(2));
            
            // Act
            var windows = assigner.AssignWindows("test", 15000L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.GreaterThanOrEqualTo(2)); // Should assign to multiple overlapping windows
            Assert.That(windows, Has.All.Matches<TimeWindow>(w => w.Start <= 15000L && w.End > 15000L));
        }

        [Test]
        public void SlidingEventTimeWindows_NegativeTimestamp_HandlesCorrectly()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act - Should handle negative timestamps and skip windows with negative bounds
            var windows = assigner.AssignWindows("test", -2000L).ToList();
            
            // Assert - Should only include windows with start >= 0 or end > 0
            Assert.That(windows, Has.All.Matches<TimeWindow>(w => w.Start >= 0 || w.End > 0));
        }

        [Test]
        public void SlidingEventTimeWindows_MinValueTimestamp_ReturnsEmpty()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act - long.MinValue timestamp should return no windows
            var windows = assigner.AssignWindows("test", long.MinValue).ToList();
            
            // Assert
            Assert.That(windows, Is.Empty);
        }

        [Test]
        public void SlidingEventTimeWindows_TimeCharacteristic_IsEventTime()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act & Assert
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void SlidingEventTimeWindows_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act
            var result = assigner.ToString();
            
            // Assert
            Assert.That(result, Does.Contain("SlidingEventTimeWindows"));
            Assert.That(result, Does.Contain("10000ms"));
            Assert.That(result, Does.Contain("5000ms"));
        }

        [Test]
        public void SlidingEventTimeWindows_StaticHelper_CreatesCorrectInstance()
        {
            // Arrange & Act
            var assigner1 = SlidingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(5));
            var assigner2 = SlidingEventTimeWindows.Of<int>(Time.Seconds(20), Time.Seconds(10), Time.Seconds(5));
            
            // Assert
            Assert.That(assigner1, Is.Not.Null);
            Assert.That(assigner2, Is.Not.Null);
            Assert.That(assigner1.IsEventTime, Is.True);
            Assert.That(assigner2.IsEventTime, Is.True);
        }

        #endregion

        #region SessionWindows Edge Cases

        [Test]
        public void SessionWindows_AssignsInitialWindow()
        {
            // Arrange
            var assigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            
            // Act
            var windows = assigner.AssignWindows("test", 10000L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
            Assert.That(windows[0].End, Is.EqualTo(15000L)); // timestamp + gap
        }

        [Test]
        public void SessionWindows_TimeCharacteristic_IsEventTime()
        {
            // Arrange
            var assigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            
            // Act & Assert
            Assert.That(assigner.IsEventTime, Is.True);
            Assert.That(assigner.CanMerge, Is.True);
        }

        [Test]
        public void SessionWindows_ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var assigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            
            // Act
            var result = assigner.ToString();
            
            // Assert
            Assert.That(result, Does.Contain("SessionWindows"));
            Assert.That(result, Does.Contain("5000ms"));
            Assert.That(result, Does.Contain("gap"));
        }

        [Test]
        public void SessionWindows_MergeWindows_EmptyList_ReturnsEmpty()
        {
            // Arrange
            var windows = Array.Empty<TimeWindow>();
            
            // Act
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert
            Assert.That(merged, Is.Empty);
        }

        [Test]
        public void SessionWindows_MergeWindows_SingleWindow_ReturnsSame()
        {
            // Arrange
            var windows = new[] { new TimeWindow(0, 5000) };
            
            // Act
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(5000));
        }

        [Test]
        public void SessionWindows_MergeWindows_OverlappingWindows_MergesThem()
        {
            // Arrange
            var windows = new[]
            {
                new TimeWindow(0, 5000),
                new TimeWindow(4000, 9000),
                new TimeWindow(8000, 13000)
            };
            
            // Act
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(13000));
        }

        [Test]
        public void SessionWindows_MergeWindows_NonOverlappingWindows_KeepsSeparate()
        {
            // Arrange
            var windows = new[]
            {
                new TimeWindow(0, 5000),
                new TimeWindow(10000, 15000),
                new TimeWindow(20000, 25000)
            };
            
            // Act
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert
            Assert.That(merged.Count, Is.EqualTo(3));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(5000));
            Assert.That(merged[1].Start, Is.EqualTo(10000));
            Assert.That(merged[1].End, Is.EqualTo(15000));
            Assert.That(merged[2].Start, Is.EqualTo(20000));
            Assert.That(merged[2].End, Is.EqualTo(25000));
        }

        [Test]
        public void SessionWindows_MergeWindows_UnsortedWindows_MergesCorrectly()
        {
            // Arrange - Intentionally unsorted
            var windows = new[]
            {
                new TimeWindow(20000, 25000),
                new TimeWindow(0, 5000),
                new TimeWindow(4000, 9000)
            };
            
            // Act - Should sort internally before merging
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(9000)); // First two windows merged
            Assert.That(merged[1].Start, Is.EqualTo(20000));
            Assert.That(merged[1].End, Is.EqualTo(25000)); // Third window separate
        }

        [Test]
        public void SessionWindows_MergeWindows_AdjacentWindows_MergesAtBoundary()
        {
            // Arrange - Windows that touch at the boundary
            var windows = new[]
            {
                new TimeWindow(0, 5000),
                new TimeWindow(5000, 10000) // Touches at 5000
            };
            
            // Act
            var merged = SessionWindows<string>.MergeWindows(windows).ToList();
            
            // Assert - Should merge because start <= end
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(10000));
        }

        [Test]
        public void SessionWindows_StaticHelper_CreatesCorrectInstance()
        {
            // Arrange & Act
            var assigner = SessionWindows.WithGap<string>(Time.Seconds(5));
            
            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
            Assert.That(assigner.CanMerge, Is.True);
        }

        #endregion

        #region Boundary Condition Tests

        [Test]
        public void TumblingEventTimeWindows_ZeroTimestamp_AssignsCorrectly()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act
            var windows = assigner.AssignWindows("test", 0L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.LessThanOrEqualTo(0));
            Assert.That(windows[0].End, Is.GreaterThan(0));
        }

        [Test]
        public void SlidingEventTimeWindows_ZeroTimestamp_AssignsCorrectly()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            
            // Act
            var windows = assigner.AssignWindows("test", 0L).ToList();
            
            // Assert
            Assert.That(windows, Is.Not.Empty);
            Assert.That(windows, Has.All.Matches<TimeWindow>(w => w.Start <= 0 && w.End > 0));
        }

        [Test]
        public void SessionWindows_ZeroTimestamp_AssignsCorrectly()
        {
            // Arrange
            var assigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            
            // Act
            var windows = assigner.AssignWindows("test", 0L).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(0L));
            Assert.That(windows[0].End, Is.EqualTo(5000L));
        }

        [Test]
        public void TumblingEventTimeWindows_LargeTimestamp_AssignsCorrectly()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            
            // Act
            var windows = assigner.AssignWindows("test", long.MaxValue / 2).ToList();
            
            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.LessThanOrEqualTo(long.MaxValue / 2));
            Assert.That(windows[0].End, Is.GreaterThan(long.MaxValue / 2));
        }

        #endregion
    }
}