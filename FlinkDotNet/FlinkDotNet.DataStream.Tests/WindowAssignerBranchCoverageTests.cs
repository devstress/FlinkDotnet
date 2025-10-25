using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests to achieve 100% branch coverage for Window Assigner classes.
    /// Targets SessionWindows, SlidingEventTimeWindows, and TumblingEventTimeWindows.
    /// </summary>
    [TestFixture]
    public class WindowAssignerBranchCoverageTests
    {
        #region SessionWindows MergeWindows Tests

        [Test]
        public void SessionWindows_MergeWindows_WithEmptyCollection_ReturnsEmptyList()
        {
            // Arrange
            var windows = new List<TimeWindow>();

            // Act
            var result = SessionWindows.MergeWindows(windows);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void SessionWindows_MergeWindows_WithSingleWindow_ReturnsSingleWindow()
        {
            // Arrange
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 2000)
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(2000));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithNonOverlappingWindows_ReturnsAllWindows()
        {
            // Arrange
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 2000),
                new TimeWindow(3000, 4000),
                new TimeWindow(5000, 6000)
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(3));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithOverlappingWindows_MergesThem()
        {
            // Arrange
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 3000),
                new TimeWindow(2000, 4000)  // Overlaps with first window
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(4000));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithMultipleOverlappingWindows_MergesAllOverlapping()
        {
            // Arrange
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 3000),
                new TimeWindow(2500, 4500),
                new TimeWindow(4000, 6000),  // Overlaps with previous
                new TimeWindow(8000, 9000)   // Does not overlap
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(6000));
            Assert.That(result[1].Start, Is.EqualTo(8000));
            Assert.That(result[1].End, Is.EqualTo(9000));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithUnsortedWindows_SortsAndMerges()
        {
            // Arrange - Windows in reverse order
            var windows = new List<TimeWindow>
            {
                new TimeWindow(5000, 6000),
                new TimeWindow(2000, 4000),
                new TimeWindow(1000, 2500)  // Overlaps with 2000-4000
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(4000));
            Assert.That(result[1].Start, Is.EqualTo(5000));
            Assert.That(result[1].End, Is.EqualTo(6000));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithAdjacentWindows_DoesNotMerge()
        {
            // Arrange - Windows touch but don't overlap
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 2000),
                new TimeWindow(2001, 3000)  // Starts after first ends
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithEdgeCase_WindowStartEqualsCurrentEnd()
        {
            // Arrange - Window starts exactly at the end of current window (should merge)
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 2000),
                new TimeWindow(2000, 3000)  // Starts at end of first
            };

            // Act
            var result = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(3000));
        }

        [Test]
        public void SessionWindows_Instance_MergeWindows_WithOverlappingWindows_MergesThem()
        {
            // Arrange
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 3000),
                new TimeWindow(2500, 4500)
            };

            // Act
            var result = SessionWindows<string>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Start, Is.EqualTo(1000));
            Assert.That(result[0].End, Is.EqualTo(4500));
        }

        #endregion

        #region SlidingEventTimeWindows Tests

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_WithValidTimestamp_AssignsCorrectWindows()
        {
            // Arrange
            var windowSize = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var assigner = SlidingEventTimeWindows<string>.Of(windowSize, slide);
            var timestamp = 15000L; // 15 seconds

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows, Is.Not.Empty);
            // Elements should be assigned to multiple overlapping windows
            Assert.That(windows.Count, Is.GreaterThan(1));
        }

        #endregion

        #region TumblingEventTimeWindows Tests

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_WithValidTimestamp_AssignsSingleWindow()
        {
            // Arrange
            var windowSize = Time.Seconds(10);
            var assigner = TumblingEventTimeWindows<string>.Of(windowSize);
            var timestamp = 15000L; // 15 seconds

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            // Window should start at 10000 (floor of 15000 / 10000 * 10000)
            Assert.That(windows[0].Start, Is.EqualTo(10000));
            Assert.That(windows[0].End, Is.EqualTo(20000));
        }

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_WithOffset_AssignsWindowWithOffset()
        {
            // Arrange
            var windowSize = Time.Seconds(10);
            var offset = Time.Seconds(2);
            var assigner = TumblingEventTimeWindows<string>.Of(windowSize, offset);
            var timestamp = 15000L; // 15 seconds

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            // With offset of 2s, windows should be [2000, 12000), [12000, 22000), etc.
            // So 15000 falls in [12000, 22000)
            Assert.That(windows[0].Start, Is.EqualTo(12000));
            Assert.That(windows[0].End, Is.EqualTo(22000));
        }

        #endregion

        #region SessionWindows Generic Instance Tests

        [Test]
        public void SessionWindows_WithGap_CreatesInstanceWithCorrectGap()
        {
            // Arrange & Act
            var sessionWindows = SessionWindows<string>.WithGap(Time.Seconds(5));

            // Assert
            Assert.That(sessionWindows, Is.Not.Null);
            Assert.That(sessionWindows.IsEventTime, Is.True);
            Assert.That(sessionWindows.CanMerge, Is.True);
        }

        [Test]
        public void SessionWindows_AssignWindows_CreatesWindowWithCorrectDuration()
        {
            // Arrange
            var gap = Time.Seconds(5);
            var sessionWindows = SessionWindows<string>.WithGap(gap);
            var timestamp = 10000L;

            // Act
            var windows = sessionWindows.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000));
            Assert.That(windows[0].End, Is.EqualTo(15000)); // 10000 + 5000
        }

        #endregion

        #region SlidingEventTimeWindows Properties Tests

        [Test]
        public void SlidingEventTimeWindows_Properties_ReturnCorrectValues()
        {
            // Arrange
            var windowSize = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var assigner = SlidingEventTimeWindows<string>.Of(windowSize, slide);

            // Act & Assert
            Assert.That(assigner.IsEventTime, Is.True);
        }

        #endregion

        #region TumblingEventTimeWindows Properties Tests

        [Test]
        public void TumblingEventTimeWindows_Properties_ReturnCorrectValues()
        {
            // Arrange
            var windowSize = Time.Seconds(10);
            var assigner = TumblingEventTimeWindows<string>.Of(windowSize);

            // Act & Assert
            Assert.That(assigner.IsEventTime, Is.True);
        }

        #endregion
    }
}
