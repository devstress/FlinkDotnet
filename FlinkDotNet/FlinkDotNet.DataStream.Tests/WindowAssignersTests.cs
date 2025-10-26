using System;
using System.Linq;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class WindowAssignersTests
    {
        #region TumblingEventTimeWindows Tests

        [Test]
        public void TumblingEventTimeWindows_Of_ShouldCreateAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);

            // Act
            var assigner = TumblingEventTimeWindows<string>.Of(size);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
            // Verify ToString includes window information
            Assert.That(assigner.ToString(), Does.Contain("TumblingEventTimeWindows"));
        }

        [Test]
        public void TumblingEventTimeWindows_OfWithOffset_ShouldCreateAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);
            var offset = Time.Seconds(2);

            // Act
            var assigner = TumblingEventTimeWindows<string>.Of(size, offset);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_ShouldAssignToSingleWindow()
        {
            // Arrange
            var size = Time.Seconds(10);
            var assigner = TumblingEventTimeWindows<string>.Of(size);
            var timestamp = 15000L; // 15 seconds

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000L)); // Window starts at 10s
            Assert.That(windows[0].End, Is.EqualTo(20000L));   // Window ends at 20s
        }

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_MultipleTimestamps_ShouldAssignToCorrectWindows()
        {
            // Arrange
            var size = Time.Seconds(5);
            var assigner = TumblingEventTimeWindows<string>.Of(size);

            // Act
            var window1 = assigner.AssignWindows("test1", 2000L).First();  // 2s -> [0-5s)
            var window2 = assigner.AssignWindows("test2", 7000L).First();  // 7s -> [5-10s)
            var window3 = assigner.AssignWindows("test3", 12000L).First(); // 12s -> [10-15s)

            // Assert
            Assert.That(window1.Start, Is.EqualTo(0L));
            Assert.That(window1.End, Is.EqualTo(5000L));

            Assert.That(window2.Start, Is.EqualTo(5000L));
            Assert.That(window2.End, Is.EqualTo(10000L));

            Assert.That(window3.Start, Is.EqualTo(10000L));
            Assert.That(window3.End, Is.EqualTo(15000L));
        }

        [Test]
        public void TumblingEventTimeWindows_ToString_ShouldReturnDescription()
        {
            // Arrange
            var size = Time.Seconds(10);
            var assigner = TumblingEventTimeWindows<string>.Of(size);

            // Act
            var description = assigner.ToString();

            // Assert
            Assert.That(description, Does.Contain("TumblingEventTimeWindows"));
            Assert.That(description, Does.Contain("10000"));
        }

        #endregion

        #region SlidingEventTimeWindows Tests

        [Test]
        public void SlidingEventTimeWindows_Of_ShouldCreateAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);

            // Act
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
            // Verify ToString includes window information
            Assert.That(assigner.ToString(), Does.Contain("SlidingEventTimeWindows"));
        }

        [Test]
        public void SlidingEventTimeWindows_OfWithOffset_ShouldCreateAssigner()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var offset = Time.Seconds(2);

            // Act
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide, offset);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_ShouldAssignToMultipleWindows()
        {
            // Arrange
            var size = Time.Seconds(10); // 10 second windows
            var slide = Time.Seconds(5); // 5 second slide
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide);
            var timestamp = 12000L; // 12 seconds

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert - element should belong to 2 overlapping windows
            Assert.That(windows.Count, Is.EqualTo(2));

            // First window: [10s-20s)
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
            Assert.That(windows[0].End, Is.EqualTo(20000L));

            // Second window: [5s-15s)
            Assert.That(windows[1].Start, Is.EqualTo(5000L));
            Assert.That(windows[1].End, Is.EqualTo(15000L));
        }

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_WithLongBeforeMinValue_ShouldNotAssign()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide);
            var timestamp = long.MinValue;

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert - no windows should be assigned for MinValue timestamp
            Assert.That(windows.Count, Is.EqualTo(0));
        }

        [Test]
        public void SlidingEventTimeWindows_ToString_ShouldReturnDescription()
        {
            // Arrange
            var size = Time.Seconds(10);
            var slide = Time.Seconds(5);
            var assigner = SlidingEventTimeWindows<string>.Of(size, slide);

            // Act
            var description = assigner.ToString();

            // Assert
            Assert.That(description, Does.Contain("SlidingEventTimeWindows"));
            Assert.That(description, Does.Contain("10000"));
            Assert.That(description, Does.Contain("5000"));
        }

        #endregion

        #region SessionWindows Tests

        [Test]
        public void SessionWindows_WithGap_ShouldCreateAssigner()
        {
            // Arrange
            var gap = Time.Seconds(5);

            // Act
            var assigner = SessionWindows<string>.WithGap(gap);

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner.IsEventTime, Is.True);
            Assert.That(assigner.CanMerge, Is.True);
            // Verify ToString includes window information
            Assert.That(assigner.ToString(), Does.Contain("SessionWindows"));
        }

        [Test]
        public void SessionWindows_AssignWindows_ShouldCreateInitialWindow()
        {
            // Arrange
            var gap = Time.Seconds(5);
            var assigner = SessionWindows<string>.WithGap(gap);
            var timestamp = 10000L;

            // Act
            var windows = assigner.AssignWindows("test", timestamp).ToList();

            // Assert
            Assert.That(windows.Count, Is.EqualTo(1));
            Assert.That(windows[0].Start, Is.EqualTo(10000L));
            Assert.That(windows[0].End, Is.EqualTo(15000L)); // timestamp + gap
        }

        [Test]
        public void SessionWindows_MergeWindows_NoOverlap_ShouldKeepSeparate()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(10000, 15000);
            var windows = new[] { window1, window2 };

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert - no overlap, should remain separate
            Assert.That(merged.Count, Is.EqualTo(2));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithOverlap_ShouldMerge()
        {
            // Arrange
            var window1 = new TimeWindow(0, 10000);
            var window2 = new TimeWindow(5000, 15000);
            var windows = new[] { window1, window2 };

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert - windows overlap, should merge into one
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[0].End, Is.EqualTo(15000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_MultipleOverlaps_ShouldMergeAll()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(4000, 10000);
            var window3 = new TimeWindow(9000, 15000);
            var windows = new[] { window1, window2, window3 };

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert - all windows overlap transitively, should merge into one
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[0].End, Is.EqualTo(15000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_PartialOverlaps_ShouldMergeCorrectly()
        {
            // Arrange
            var window1 = new TimeWindow(0, 5000);
            var window2 = new TimeWindow(4000, 10000);
            var window3 = new TimeWindow(20000, 25000); // Separate session
            var windows = new[] { window1, window2, window3 };

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert - first two merge, third stays separate
            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[0].End, Is.EqualTo(10000L));
            Assert.That(merged[1].Start, Is.EqualTo(20000L));
            Assert.That(merged[1].End, Is.EqualTo(25000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_EmptyList_ShouldReturnEmpty()
        {
            // Arrange
            var windows = new TimeWindow[0];

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void SessionWindows_MergeWindows_SingleWindow_ShouldReturnSame()
        {
            // Arrange
            var window = new TimeWindow(0, 5000);
            var windows = new[] { window };

            // Act
            var merged = SessionWindows.MergeWindows(windows).ToList();

            // Assert
            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[0].End, Is.EqualTo(5000L));
        }

        [Test]
        public void SessionWindows_ToString_ShouldReturnDescription()
        {
            // Arrange
            var gap = Time.Seconds(5);
            var assigner = SessionWindows<string>.WithGap(gap);

            // Act
            var description = assigner.ToString();

            // Assert
            Assert.That(description, Does.Contain("SessionWindows"));
            Assert.That(description, Does.Contain("5000"));
            Assert.That(description, Does.Contain("gap"));
        }

        #region Static Window Assigners Tests

        [Test]
        public void StaticTumblingEventTimeWindows_Of_ShouldCreateAssigner()
        {
            // Act
            var assigner = TumblingEventTimeWindows.Of<string>(Time.Seconds(10));

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner, Is.InstanceOf<TumblingEventTimeWindows<string>>());
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void StaticTumblingEventTimeWindows_OfWithOffset_ShouldCreateAssigner()
        {
            // Act
            var assigner = TumblingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(2));

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner, Is.InstanceOf<TumblingEventTimeWindows<string>>());
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void StaticSlidingEventTimeWindows_Of_ShouldCreateAssigner()
        {
            // Act
            var assigner = SlidingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(5));

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner, Is.InstanceOf<SlidingEventTimeWindows<string>>());
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void StaticSlidingEventTimeWindows_OfWithOffset_ShouldCreateAssigner()
        {
            // Act
            var assigner = SlidingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(5), Time.Seconds(2));

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner, Is.InstanceOf<SlidingEventTimeWindows<string>>());
            Assert.That(assigner.IsEventTime, Is.True);
        }

        [Test]
        public void StaticSessionWindows_WithGap_ShouldCreateAssigner()
        {
            // Act
            var assigner = SessionWindows.WithGap<string>(Time.Seconds(5));

            // Assert
            Assert.That(assigner, Is.Not.Null);
            Assert.That(assigner, Is.InstanceOf<SessionWindows<string>>());
            Assert.That(assigner.IsEventTime, Is.True);
            Assert.That(assigner.CanMerge, Is.True);
        }

        [Test]
        public void StaticWindowAssigners_DifferentTypes_ShouldCreateCorrectGenericInstances()
        {
            // Act
            var stringTumbling = TumblingEventTimeWindows.Of<string>(Time.Seconds(10));
            var intTumbling = TumblingEventTimeWindows.Of<int>(Time.Seconds(10));
            var stringSliding = SlidingEventTimeWindows.Of<string>(Time.Seconds(10), Time.Seconds(5));
            var intSession = SessionWindows.WithGap<int>(Time.Seconds(5));

            // Assert
            Assert.That(stringTumbling, Is.InstanceOf<TumblingEventTimeWindows<string>>());
            Assert.That(intTumbling, Is.InstanceOf<TumblingEventTimeWindows<int>>());
            Assert.That(stringSliding, Is.InstanceOf<SlidingEventTimeWindows<string>>());
            Assert.That(intSession, Is.InstanceOf<SessionWindows<int>>());
        }

        #endregion
        #endregion
    }
}
