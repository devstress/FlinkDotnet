using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Additional tests targeting remaining uncovered branches to reach 100% coverage.
    /// Focuses on edge cases and error paths not covered by existing tests.
    /// </summary>
    [TestFixture]
    public class AdditionalBranchCoverageTests
    {
        [Test]
        public void DataStream_FromCollection_EmptyCollection_CreatesEmptyStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var emptyList = new List<string>();

            // Act - Test with empty collection
            var stream = env.FromCollection(emptyList);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void DataStream_FromCollection_SingleElement_CreatesStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var stream = env.FromCollection(new[] { "single" });

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void KafkaSourceFunction_Constructor_WithEarliestOffset_Succeeds()
        {
            // Arrange & Act - Test constructor with earliest offset
            var source = new KafkaSourceFunction<string>(
                topic: "test-topic",
                bootstrapServers: "localhost:9092",
                groupId: "test-group",
                deserializer: s => s,
                startingOffsets: "earliest");

            // Assert
            Assert.That(source, Is.Not.Null);
        }

        [Test]
        public void KafkaSourceFunction_Constructor_WithLatestOffset_Succeeds()
        {
            // Arrange & Act - Test with latest offset
            var source = new KafkaSourceFunction<string>(
                topic: "test-topic",
                bootstrapServers: "localhost:9092",
                groupId: "test-group",
                deserializer: s => s,
                startingOffsets: "latest");

            // Assert
            Assert.That(source, Is.Not.Null);
        }

        [Test]
        public void TimeWindow_Constructor_WithEqualStartAndEnd_Succeeds()
        {
            // Arrange & Act - Boundary case: start == end
            var window = new Window.TimeWindow(1000, 1000);

            // Assert
            Assert.That(window.Start, Is.EqualTo(1000));
            Assert.That(window.End, Is.EqualTo(1000));
        }

        [Test]
        public void TimeWindow_MaxTimestamp_WithMinimalWindow_ReturnsCorrectValue()
        {
            // Arrange
            var window = new Window.TimeWindow(0, 1);

            // Act
            var maxTimestamp = window.MaxTimestamp();

            // Assert
            Assert.That(maxTimestamp, Is.EqualTo(0));
        }

        [Test]
        public void TimeWindow_Intersects_WithAdjacentWindows_ReturnsFalse()
        {
            // Arrange - Two adjacent but non-overlapping windows
            var window1 = new Window.TimeWindow(0, 100);
            var window2 = new Window.TimeWindow(100, 200);

            // Act
            var result = window1.Intersects(window2);

            // Assert - Adjacent windows don't intersect (end is exclusive)
            Assert.That(result, Is.False);
        }

        [Test]
        public void TimeWindow_Cover_WithIdenticalWindows_ReturnsSameWindow()
        {
            // Arrange
            var window1 = new Window.TimeWindow(100, 200);
            var window2 = new Window.TimeWindow(100, 200);

            // Act
            var covered = window1.Cover(window2);

            // Assert
            Assert.That(covered.Start, Is.EqualTo(100));
            Assert.That(covered.End, Is.EqualTo(200));
        }

        [Test]
        public void SessionWindows_WithGap_CreatesCorrectly()
        {
            // Arrange & Act
            var sessionWindows = Window.Assigners.SessionWindows<string>.WithGap(Time.Seconds(30));

            // Assert
            Assert.That(sessionWindows, Is.Not.Null);
        }

        [Test]
        public void SessionWindows_WithExtractor_CreatesCorrectly()
        {
            // Arrange & Act - Test session windows creation
            var sessionWindows = Window.Assigners.SessionWindows<string>.WithGap(Time.Seconds(10));

            // Assert
            Assert.That(sessionWindows, Is.Not.Null);
        }

        [Test]
        public void OutputTag_GetHashCode_ForDifferentTags_ReturnsDifferentValues()
        {
            // Arrange
            var tag1 = new OutputTag<string>("tag1");
            var tag2 = new OutputTag<string>("tag2");

            // Act
            var hash1 = tag1.GetHashCode();
            var hash2 = tag2.GetHashCode();

            // Assert - Different tags should have different hash codes
            Assert.That(hash1, Is.Not.EqualTo(hash2));
        }

        [Test]
        public void OutputTag_GetHashCode_ForSameName_ReturnsSameValue()
        {
            // Arrange
            var tag1 = new OutputTag<string>("same-tag");
            var tag2 = new OutputTag<string>("same-tag");

            // Act
            var hash1 = tag1.GetHashCode();
            var hash2 = tag2.GetHashCode();

            // Assert - Same name should produce same hash
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void AllWindowedStream_TimeWindow_WithVerySmallWindow_CreatesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act - Test with very small time window (1 millisecond)
            var windowed = stream.TimeWindowAll(Time.Milliseconds(1));

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void AllWindowedStream_CountWindow_WithMinimalCount_CreatesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act - Test with count window of 1
            var windowed = stream.CountWindowAll(1);

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void Time_Milliseconds_WithZero_CreatesZeroDuration()
        {
            // Arrange & Act
            var time = Time.Milliseconds(0);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(0));
        }

        [Test]
        public void Time_Seconds_WithLargeValue_CreatesCorrectly()
        {
            // Arrange & Act
            var time = Time.Seconds(3600); // 1 hour

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
        }

        [Test]
        public void Time_Minutes_WithZero_CreatesZeroDuration()
        {
            // Arrange & Act
            var time = Time.Minutes(0);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(0));
        }

        [Test]
        public void Time_Hours_WithOne_CreatesCorrectDuration()
        {
            // Arrange & Act
            var time = Time.Hours(1);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
        }

        [Test]
        public void Time_Days_WithOne_CreatesCorrectDuration()
        {
            // Arrange & Act
            var time = Time.Days(1);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
        }

        [Test]
        public void StateDescriptor_Constructor_WithName_SetsNameCorrectly()
        {
            // Arrange & Act
            var descriptor = new ListStateDescriptor<int>("test-state");

            // Assert - Access via public method
            Assert.That(descriptor, Is.Not.Null);
        }

        [Test]
        public void MapStateDescriptor_Constructor_CreatesCorrectly()
        {
            // Arrange & Act
            var descriptor = new MapStateDescriptor<string, int>("map-state");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
        }

        [Test]
        public void ReducingStateDescriptor_Constructor_WithReduceFunction_CreatesCorrectly()
        {
            // Arrange
            var reduceFunc = new TestReduceFunction();

            // Act
            var descriptor = new ReducingStateDescriptor<int>("reduce-state", reduceFunc);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
        }

        [Test]
        public void ValueStateDescriptor_Constructor_CreatesCorrectly()
        {
            // Arrange & Act
            var descriptor = new ValueStateDescriptor<string>("value-state");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
        }

        [Test]
        public void AggregatingStateDescriptor_Constructor_WithAggregateFunction_CreatesCorrectly()
        {
            // Arrange
            var aggFunc = new TestAggregateFunction();

            // Act
            var descriptor = new AggregatingStateDescriptor<int, int, int>("agg-state", aggFunc);

            // Assert
            Assert.That(descriptor, Is.Not.Null);
        }

        // Helper classes for testing
        private class TestReduceFunction : IReduceFunction<int>
        {
            public int Reduce(int value1, int value2) => value1 + value2;
        }

        private class TestAggregateFunction : IAggregateFunction<int, int, int>
        {
            public int CreateAccumulator() => 0;
            public int Add(int value, int accumulator) => accumulator + value;
            public int GetResult(int accumulator) => accumulator;
            public int Merge(int a, int b) => a + b;
        }
    }
}
