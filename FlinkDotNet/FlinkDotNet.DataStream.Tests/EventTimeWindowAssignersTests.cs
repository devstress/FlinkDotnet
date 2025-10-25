using System;
using System.Linq;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class TumblingEventTimeWindowsTests
    {
        [Test]
        public void Of_WithSize_CreatesWindowAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);

            // Act
            var assigner = TumblingEventTimeWindows<int>.Of(size);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void Of_WithSizeAndOffset_CreatesWindowAssigner()
        {
            // Arrange
            var size = Time.Seconds(5);
            var offset = Time.Seconds(1);

            // Act
            var assigner = TumblingEventTimeWindows<string>.Of(size, offset);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void AssignWindows_AssignsSingleWindow()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(10));
            var element = 42;
            var timestamp = 15000L; // 15 seconds

            // Act
            var windows = assigner.AssignWindows(element, timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000L)); // Window [10s, 20s)
            Assert.That(windows[0].End, Is.EqualTo(20000L));
        }

        [Test]
        public void AssignWindows_WithOffset_AlignsCorrectly()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(2));
            var timestamp = 15000L;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            // With 2s offset, windows are [2s, 12s), [12s, 22s), etc.
            Assert.That(windows[0].Start, Is.EqualTo(12000L));
            Assert.That(windows[0].End, Is.EqualTo(22000L));
        }

        [Test]
        public void AssignWindows_MultipleElementsSameWindow()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(10));

            // Act
            var windows1 = assigner.AssignWindows(1, 11000L).ToList();
            var windows2 = assigner.AssignWindows(2, 15000L).ToList();
            var windows3 = assigner.AssignWindows(3, 19000L).ToList();

            // Assert - All should be in window [10s, 20s)
            Assert.That(windows1[0].Start, Is.EqualTo(10000L));
            Assert.That(windows2[0].Start, Is.EqualTo(10000L));
            Assert.That(windows3[0].Start, Is.EqualTo(10000L));
        }

        [Test]
        public void AssignWindows_ElementsInDifferentWindows()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));

            // Act
            var windows1 = assigner.AssignWindows(1, 3000L).ToList();
            var windows2 = assigner.AssignWindows(2, 7000L).ToList();

            // Assert
            Assert.That(windows1[0].Start, Is.EqualTo(0L));
            Assert.That(windows1[0].End, Is.EqualTo(5000L));
            Assert.That(windows2[0].Start, Is.EqualTo(5000L));
            Assert.That(windows2[0].End, Is.EqualTo(10000L));
        }

        [Test]
        public void ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(30));

            // Act
            var result = assigner.ToString();

            // Assert
            Assert.That(result, Does.Contain("TumblingEventTimeWindows"));
            Assert.That(result, Does.Contain("30000"));
        }
    }

    [TestFixture]
    public class SlidingEventTimeWindowsTests
    {
        [Test]
        public void Of_WithSizeAndSlide_CreatesWindowAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);

            // Act
            var assigner = SlidingEventTimeWindows<int>.Of(size, slide);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void Of_WithSizeSlideAndOffset_CreatesWindowAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var offset = Time.Seconds(1);

            // Act
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide, offset);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void AssignWindows_AssignsMultipleOverlappingWindows()
        {
            // Arrange - Window size 10s, slide 5s
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));
            var timestamp = 12000L; // 12 seconds

            // Act
            var windows = assigner.AssignWindows(42, timestamp).ToList();

            // Assert - Should be in windows [5s, 15s) and [10s, 20s)
            Assert.That(windows, Has.Count.EqualTo(2));
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
            Assert.That(windows[0].End, Is.EqualTo(20000L));
            Assert.That(windows[1].Start, Is.EqualTo(5000L));
            Assert.That(windows[1].End, Is.EqualTo(15000L));
        }

        [Test]
        public void AssignWindows_WithNoOverlap_AssignsSingleWindow()
        {
            // Arrange - Window size equals slide (tumbling)
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(10));
            var timestamp = 15000L;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
        }

        [Test]
        public void AssignWindows_WithSmallSlide_AssignsManyWindows()
        {
            // Arrange - Large window, small slide creates many overlapping windows
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(2));
            var timestamp = 10000L;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert - Should span multiple windows
            Assert.That(windows.Count, Is.GreaterThan(1));
            // All windows should contain the timestamp
            foreach (var window in windows)
            {
                Assert.That(timestamp, Is.GreaterThanOrEqualTo(window.Start));
                Assert.That(timestamp, Is.LessThan(window.End));
            }
        }

        [Test]
        public void AssignWindows_WithOffset_AlignsCorrectly()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<int>.Of(
                Time.Seconds(10), 
                Time.Seconds(5), 
                Time.Seconds(2));
            var timestamp = 12000L;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows, Is.Not.Empty);
            // Verify windows are offset correctly
            foreach (var window in windows)
            {
                var windowStart = window.Start;
                Assert.That((windowStart - 2000) % 5000, Is.EqualTo(0)); // Aligned with offset
            }
        }

        [Test]
        public void AssignWindows_WithMinValueTimestamp_ReturnsEmpty()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));
            var timestamp = long.MinValue;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows, Is.Empty);
        }

        [Test]
        public void AssignWindows_ElementInWindowBoundary()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(10), Time.Seconds(5));
            var timestamp = 10000L; // Exactly at window boundary

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows, Is.Not.Empty);
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
        }

        [Test]
        public void ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var assigner = SlidingEventTimeWindows<int>.Of(Time.Seconds(30), Time.Seconds(10));

            // Act
            var result = assigner.ToString();

            // Assert
            Assert.That(result, Does.Contain("SlidingEventTimeWindows"));
            Assert.That(result, Does.Contain("30000"));
            Assert.That(result, Does.Contain("10000"));
        }
    }

    [TestFixture]
    public class SessionWindowsTests
    {
        [Test]
        public void WithGap_CreatesWindowAssigner()
        {
            // Arrange
            var gap = Time.Seconds(5);

            // Act
            var assigner = SessionWindows<int>.WithGap(gap);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
            Assert.That(assigner.CanMerge, Is.True);
        }

        [Test]
        public void AssignWindows_CreatesInitialWindowWithGap()
        {
            // Arrange
            var assigner = SessionWindows<int>.WithGap(Time.Seconds(5));
            var element = 42;
            var timestamp = 10000L;

            // Act
            var windows = assigner.AssignWindows(element, timestamp).ToList();

            // Assert
            Assert.That(windows, Has.Count.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
            Assert.That(windows[0].End, Is.EqualTo(15000L)); // timestamp + gap
        }

        [Test]
        public void AssignWindows_DifferentTimestamps_CreatesSeparateWindows()
        {
            // Arrange
            var assigner = SessionWindows<int>.WithGap(Time.Seconds(3));

            // Act
            var windows1 = assigner.AssignWindows(1, 5000L).ToList();
            var windows2 = assigner.AssignWindows(2, 10000L).ToList();

            // Assert
            Assert.That(windows1[0].Start, Is.EqualTo(5000L));
            Assert.That(windows1[0].End, Is.EqualTo(8000L));
            Assert.That(windows2[0].Start, Is.EqualTo(10000L));
            Assert.That(windows2[0].End, Is.EqualTo(13000L));
        }

        [Test]
        public void MergeWindows_WithOverlappingWindows_MergesThem()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(3000, 8000);
            var window3 = new TimeWindow(7000, 12000);

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window1, window2, window3 }).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(12000));
        }

        [Test]
        public void MergeWindows_WithNonOverlappingWindows_KeepsSeparate()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(10000, 15000);
            var window3 = new TimeWindow(20000, 25000);

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window1, window2, window3 }).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(3));
        }

        [Test]
        public void MergeWindows_WithPartialOverlap_MergesCorrectly()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(4000, 9000); // Overlaps with window1
            var window3 = new TimeWindow(15000, 20000); // No overlap

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window1, window2, window3 }).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(2));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(9000));
            Assert.That(merged[1].Start, Is.EqualTo(15000));
            Assert.That(merged[1].End, Is.EqualTo(20000));
        }

        [Test]
        public void MergeWindows_WithUnsortedWindows_MergesCorrectly()
        {
            // Arrange - Provide windows in random order
            var window1 = new TimeWindow(10000, 15000);
            var window2 = new TimeWindow(0, 5000);
            var window3 = new TimeWindow(4000, 11000);

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window1, window2, window3 }).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(15000));
        }

        [Test]
        public void MergeWindows_WithEmptyList_ReturnsEmpty()
        {
            // Act
            var merged = SessionWindows<int>.MergeWindows(Array.Empty<TimeWindow>()).ToList();

            // Assert
            Assert.That(merged, Is.Empty);
        }

        [Test]
        public void MergeWindows_WithSingleWindow_ReturnsSameWindow()
        {
            // Arrange
            var window = new TimeWindow(5000, 10000);

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window }).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(1));
            Assert.That(merged[0], Is.EqualTo(window));
        }

        [Test]
        public void MergeWindows_WithAdjacentWindows_MergesIfTouching()
        {
            // Arrange - Windows that exactly touch at the boundary
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(5000, 10000);

            // Act
            var merged = SessionWindows<int>.MergeWindows(new[] { window1, window2 }).ToList();

            // Assert - Should merge because window2.Start == window1.End
            Assert.That(merged, Has.Count.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(10000));
        }

        [Test]
        public void MergeWindows_ComplexScenario_MergesCorrectly()
        {
            // Arrange - Mix of overlapping and non-overlapping windows
            var windows = new[]
            {
                new TimeWindow(0, 3000),
                new TimeWindow(2000, 5000),     // Overlaps with first
                new TimeWindow(4000, 7000),     // Overlaps with second
                new TimeWindow(10000, 13000),   // Gap - no overlap
                new TimeWindow(12000, 15000),   // Overlaps with fourth
                new TimeWindow(20000, 25000)    // Gap - no overlap
            };

            // Act
            var merged = SessionWindows<int>.MergeWindows(windows).ToList();

            // Assert
            Assert.That(merged, Has.Count.EqualTo(3));
            Assert.That(merged[0].Start, Is.EqualTo(0));
            Assert.That(merged[0].End, Is.EqualTo(7000));
            Assert.That(merged[1].Start, Is.EqualTo(10000));
            Assert.That(merged[1].End, Is.EqualTo(15000));
            Assert.That(merged[2].Start, Is.EqualTo(20000));
            Assert.That(merged[2].End, Is.EqualTo(25000));
        }

        [Test]
        public void CanMerge_ReturnsTrue()
        {
            // Arrange
            var assigner = SessionWindows<int>.WithGap(Time.Seconds(5));

            // Act & Assert
            Assert.That(assigner.CanMerge, Is.True);
        }

        [Test]
        public void ToString_ReturnsDescriptiveString()
        {
            // Arrange
            var assigner = SessionWindows<int>.WithGap(Time.Seconds(10));

            // Act
            var result = assigner.ToString();

            // Assert
            Assert.That(result, Does.Contain("SessionWindows"));
            Assert.That(result, Does.Contain("10000"));
            Assert.That(result, Does.Contain("gap"));
        }

        [Test]
        public void SessionWindows_LargeGap_CreatesLargeWindow()
        {
            // Arrange
            var assigner = SessionWindows<int>.WithGap(Time.Minutes(10));
            var timestamp = 5000L;

            // Act
            var windows = assigner.AssignWindows(0, timestamp).ToList();

            // Assert
            Assert.That(windows[0].Start, Is.EqualTo(5000L));
            Assert.That(windows[0].End, Is.EqualTo(605000L)); // 5s + 10min
        }
    }
}