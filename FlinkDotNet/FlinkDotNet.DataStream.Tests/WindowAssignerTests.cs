using System.Collections.Generic;
using System.Linq;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Window;
using FlinkDotNet.DataStream.Window.Assigners;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class WindowAssignerTests
    {
        #region TumblingEventTimeWindows Tests

        [Test]
        public void TumblingEventTimeWindows_Of_CreatesWindowWithCorrectSize()
        {
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));

            Assert.That(windowAssigner, Is.Not.Null);
            Assert.That(windowAssigner.IsEventTime, Is.True);
        }

        [Test]
        public void TumblingEventTimeWindows_Of_WithOffset_CreatesWindowWithOffset()
        {
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(2));

            Assert.That(windowAssigner, Is.Not.Null);
        }

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_ReturnsCorrectWindow()
        {
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));
            var timestamp = 15000L;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.EqualTo(1));
            var window = windows[0];
            Assert.That(window.Start, Is.EqualTo(10000L));
            Assert.That(window.End, Is.EqualTo(20000L));
        }

        [Test]
        public void TumblingEventTimeWindows_AssignWindows_WithOffset_ReturnsCorrectWindow()
        {
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(2));
            var timestamp = 15000L;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.EqualTo(1));
            var window = windows[0];
            Assert.That(window.Start, Is.LessThanOrEqualTo(timestamp));
            Assert.That(window.End, Is.GreaterThan(timestamp));
            Assert.That(window.End - window.Start, Is.EqualTo(10000L));
        }

        [Test]
        public void TumblingEventTimeWindows_ToString_ReturnsFormattedString()
        {
            var windowAssigner = TumblingEventTimeWindows<string>.Of(Time.Seconds(10));

            var result = windowAssigner.ToString();

            Assert.That(result, Does.Contain("TumblingEventTimeWindows"));
            Assert.That(result, Does.Contain("10000"));
        }

        #endregion

        #region SlidingEventTimeWindows Tests

        [Test]
        public void SlidingEventTimeWindows_Of_CreatesWindowWithCorrectSizeAndSlide()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));

            Assert.That(windowAssigner, Is.Not.Null);
            Assert.That(windowAssigner.IsEventTime, Is.True);
        }

        [Test]
        public void SlidingEventTimeWindows_Of_WithOffset_CreatesWindowWithOffset()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5), Time.Seconds(2));

            Assert.That(windowAssigner, Is.Not.Null);
        }

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_ReturnsMultipleOverlappingWindows()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            var timestamp = 7000L;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.EqualTo(2));
            Assert.That(windows.Any(w => w.Start == 0L && w.End == 10000L), Is.True);
            Assert.That(windows.Any(w => w.Start == 5000L && w.End == 15000L), Is.True);
        }

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_WithSmallSlide_ReturnsMoreWindows()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(2));
            var timestamp = 10000L;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.GreaterThanOrEqualTo(3));
            
            foreach (var window in windows)
            {
                Assert.That(window.Start, Is.LessThanOrEqualTo(timestamp));
                Assert.That(window.End, Is.GreaterThan(timestamp));
                Assert.That(window.End - window.Start, Is.EqualTo(10000L));
            }
        }

        [Test]
        public void SlidingEventTimeWindows_AssignWindows_WithMinValueTimestamp_ReturnsEmpty()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));
            var timestamp = long.MinValue;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.EqualTo(0));
        }

        [Test]
        public void SlidingEventTimeWindows_ToString_ReturnsFormattedString()
        {
            var windowAssigner = SlidingEventTimeWindows<string>.Of(Time.Seconds(10), Time.Seconds(5));

            var result = windowAssigner.ToString();

            Assert.That(result, Does.Contain("SlidingEventTimeWindows"));
            Assert.That(result, Does.Contain("10000"));
            Assert.That(result, Does.Contain("5000"));
        }

        #endregion

        #region SessionWindows Tests

        [Test]
        public void SessionWindows_WithGap_CreatesWindowWithCorrectGap()
        {
            var windowAssigner = SessionWindows<string>.WithGap(Time.Seconds(5));

            Assert.That(windowAssigner, Is.Not.Null);
            Assert.That(windowAssigner.IsEventTime, Is.True);
            Assert.That(windowAssigner.CanMerge, Is.True);
        }

        [Test]
        public void SessionWindows_AssignWindows_CreatesWindowStartingAtTimestamp()
        {
            var windowAssigner = SessionWindows<string>.WithGap(Time.Seconds(5));
            var timestamp = 10000L;

            var windows = windowAssigner.AssignWindows("test", timestamp).ToList();

            Assert.That(windows.Count, Is.EqualTo(1));
            var window = windows[0];
            Assert.That(window.Start, Is.EqualTo(timestamp));
            Assert.That(window.End, Is.EqualTo(timestamp + 5000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_MergesOverlappingWindows()
        {
            var windows = new List<TimeWindow>
            {
                new TimeWindow(1000, 6000),
                new TimeWindow(2000, 7000),
                new TimeWindow(10000, 15000)
            };

            var merged = SessionWindows<string>.MergeWindows(windows).ToList();

            Assert.That(merged.Count, Is.EqualTo(2));
            Assert.That(merged[0].Start, Is.EqualTo(1000L));
            Assert.That(merged[0].End, Is.EqualTo(7000L));
            Assert.That(merged[1].Start, Is.EqualTo(10000L));
            Assert.That(merged[1].End, Is.EqualTo(15000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithAdjacentWindows_MergesThem()
        {
            var windows = new List<TimeWindow>
            {
                new TimeWindow(0, 5000),
                new TimeWindow(5000, 10000),
                new TimeWindow(10000, 15000)
            };

            var merged = SessionWindows<string>.MergeWindows(windows).ToList();

            Assert.That(merged.Count, Is.EqualTo(1));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[0].End, Is.EqualTo(15000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithNonOverlappingWindows_KeepsThemSeparate()
        {
            var windows = new List<TimeWindow>
            {
                new TimeWindow(0, 5000),
                new TimeWindow(10000, 15000),
                new TimeWindow(20000, 25000)
            };

            var merged = SessionWindows<string>.MergeWindows(windows).ToList();

            Assert.That(merged.Count, Is.EqualTo(3));
            Assert.That(merged[0].Start, Is.EqualTo(0L));
            Assert.That(merged[1].Start, Is.EqualTo(10000L));
            Assert.That(merged[2].Start, Is.EqualTo(20000L));
        }

        [Test]
        public void SessionWindows_MergeWindows_WithEmptyList_ReturnsEmpty()
        {
            var windows = new List<TimeWindow>();

            var merged = SessionWindows<string>.MergeWindows(windows).ToList();

            Assert.That(merged.Count, Is.EqualTo(0));
        }

        [Test]
        public void SessionWindows_ToString_ReturnsFormattedString()
        {
            var windowAssigner = SessionWindows<string>.WithGap(Time.Seconds(5));

            var result = windowAssigner.ToString();

            Assert.That(result, Does.Contain("SessionWindows"));
            Assert.That(result, Does.Contain("5000"));
            Assert.That(result, Does.Contain("gap"));
        }

        #endregion

        #region TimeWindow Tests

        [Test]
        public void TimeWindow_Constructor_SetsStartAndEnd()
        {
            var window = new TimeWindow(1000, 5000);

            Assert.That(window.Start, Is.EqualTo(1000));
            Assert.That(window.End, Is.EqualTo(5000));
        }

        [Test]
        public void TimeWindow_MaxTimestamp_ReturnsEndMinusOne()
        {
            var window = new TimeWindow(1000, 5000);

            Assert.That(window.MaxTimestamp(), Is.EqualTo(4999));
        }

        [Test]
        public void TimeWindow_Cover_MergesWindows()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(3000, 8000);

            var covered = window1.Cover(window2);

            Assert.That(covered.Start, Is.EqualTo(1000));
            Assert.That(covered.End, Is.EqualTo(8000));
        }

        [Test]
        public void TimeWindow_Intersects_WithOverlappingWindows_ReturnsTrue()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(3000, 8000);

            Assert.That(window1.Intersects(window2), Is.True);
            Assert.That(window2.Intersects(window1), Is.True);
        }

        [Test]
        public void TimeWindow_Intersects_WithNonOverlappingWindows_ReturnsFalse()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(6000, 10000);

            Assert.That(window1.Intersects(window2), Is.False);
            Assert.That(window2.Intersects(window1), Is.False);
        }

        [Test]
        public void TimeWindow_Constructor_StartGreaterThanEnd_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => new TimeWindow(5000, 1000));
        }

        [Test]
        public void TimeWindow_Constructor_StartEqualsEnd_CreatesWindow()
        {
            var window = new TimeWindow(5000, 5000);
            Assert.That(window.Start, Is.EqualTo(5000));
            Assert.That(window.End, Is.EqualTo(5000));
        }

        [Test]
        public void TimeWindow_MergeWindows_WithMultipleWindows_ReturnsMergedWindow()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(3000, 8000);
            var window3 = new TimeWindow(6000, 10000);

            var merged = TimeWindow.MergeWindows(window1, window2, window3);

            Assert.That(merged.Start, Is.EqualTo(1000));
            Assert.That(merged.End, Is.EqualTo(10000));
        }

        [Test]
        public void TimeWindow_MergeWindows_WithSingleWindow_ReturnsSameWindow()
        {
            var window = new TimeWindow(1000, 5000);

            var merged = TimeWindow.MergeWindows(window);

            Assert.That(merged.Start, Is.EqualTo(1000));
            Assert.That(merged.End, Is.EqualTo(5000));
        }

        [Test]
        public void TimeWindow_MergeWindows_WithEmptyArray_ThrowsArgumentException()
        {
            Assert.Throws<System.ArgumentException>(() => TimeWindow.MergeWindows());
        }

        [Test]
        public void TimeWindow_GetWindowStartWithOffset_ReturnsCorrectWindow()
        {
            var window = TimeWindow.GetWindowStartWithOffset(15000, 0, 10000);

            Assert.That(window.Start, Is.EqualTo(10000));
            Assert.That(window.End, Is.EqualTo(20000));
        }

        [Test]
        public void TimeWindow_GetWindowStartWithOffset_WithOffset_ReturnsCorrectWindow()
        {
            var window = TimeWindow.GetWindowStartWithOffset(15000, 2000, 10000);

            Assert.That(window.Start, Is.LessThanOrEqualTo(15000));
            Assert.That(window.End, Is.GreaterThan(15000));
            Assert.That(window.End - window.Start, Is.EqualTo(10000));
        }

        [Test]
        public void TimeWindow_Equals_WithSameWindow_ReturnsTrue()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(1000, 5000);

            Assert.That(window1.Equals(window2), Is.True);
            Assert.That(window1.GetHashCode(), Is.EqualTo(window2.GetHashCode()));
        }

        [Test]
        public void TimeWindow_Equals_WithDifferentWindow_ReturnsFalse()
        {
            var window1 = new TimeWindow(1000, 5000);
            var window2 = new TimeWindow(2000, 6000);

            Assert.That(window1.Equals(window2), Is.False);
        }

        [Test]
        public void TimeWindow_Equals_WithNull_ReturnsFalse()
        {
            var window = new TimeWindow(1000, 5000);

            Assert.That(window.Equals(null), Is.False);
        }

        [Test]
        public void TimeWindow_Equals_WithNonTimeWindow_ReturnsFalse()
        {
            var window = new TimeWindow(1000, 5000);

            Assert.That(window.Equals("not a window"), Is.False);
        }

        [Test]
        public void TimeWindow_ToString_ReturnsFormattedString()
        {
            var window = new TimeWindow(1000, 5000);

            var result = window.ToString();

            Assert.That(result, Does.Contain("1000"));
            Assert.That(result, Does.Contain("5000"));
            Assert.That(result, Does.Contain("TimeWindow"));
        }

        #endregion

        #region WindowedStream Tests

        [Test]
        public void WindowedStream_Aggregate_WithValidFunction_ReturnsDataStream()
        {
            var collection = new[] { "a", "b", "c" };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x[0]);
            var windowed = keyed.Window(TumblingEventTimeWindows<string>.Of(Time.Seconds(5)));
            
            var result = windowed.Aggregate(new TestWindowAggregateFunction());

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void WindowedStream_Aggregate_WithNullFunction_ThrowsArgumentNullException()
        {
            var collection = new[] { "a", "b", "c" };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x[0]);
            var windowed = keyed.Window(TumblingEventTimeWindows<string>.Of(Time.Seconds(5)));

            Assert.Throws<System.ArgumentNullException>(() =>
                windowed.Aggregate<int, int>(null!));
        }

        [Test]
        public void WindowedStream_Reduce_WithIReduceFunction_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var windowed = keyed.Window(TumblingEventTimeWindows<int>.Of(Time.Seconds(5)));
            
            var result = windowed.Reduce(new TestReduceFunction());

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void WindowedStream_Reduce_WithNullIReduceFunction_ThrowsArgumentNullException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var windowed = keyed.Window(TumblingEventTimeWindows<int>.Of(Time.Seconds(5)));

            Assert.Throws<System.ArgumentNullException>(() =>
                windowed.Reduce((IReduceFunction<int>)null!));
        }

        [Test]
        public void WindowedStream_Reduce_WithFunc_ReturnsDataStream()
        {
            var collection = new[] { 1, 2, 3, 4, 5 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var windowed = keyed.Window(TumblingEventTimeWindows<int>.Of(Time.Seconds(5)));
            
            var result = windowed.Reduce((a, b) => a + b);

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void WindowedStream_Reduce_WithNullFunc_ThrowsArgumentNullException()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var windowed = keyed.Window(TumblingEventTimeWindows<int>.Of(Time.Seconds(5)));

            Assert.Throws<System.ArgumentNullException>(() =>
                windowed.Reduce((System.Func<int, int, int>)null!));
        }

        [Test]
        public void WindowedStream_GetWindowAssigner_ReturnsAssigner()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var assigner = TumblingEventTimeWindows<int>.Of(Time.Seconds(5));
            var windowed = keyed.Window(assigner);

            var result = windowed.GetWindowAssigner();

            Assert.That(result, Is.SameAs(assigner));
        }

        [Test]
        public void WindowedStream_GetKeyedStream_ReturnsKeyedStream()
        {
            var collection = new[] { 1, 2, 3 };
            var stream = StreamExecutionEnvironment.GetExecutionEnvironment().FromCollection(collection);
            var keyed = stream.KeyBy(x => x % 2);
            var windowed = keyed.Window(TumblingEventTimeWindows<int>.Of(Time.Seconds(5)));

            var result = windowed.GetKeyedStream();

            Assert.That(result, Is.SameAs(keyed));
        }

        #endregion

        // Test helper classes
        private class TestWindowAggregateFunction : IAggregateFunction<string, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(string value, int accumulator) => accumulator + 1;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }

        private class TestReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2) => value1 + value2;
        }
    }
}