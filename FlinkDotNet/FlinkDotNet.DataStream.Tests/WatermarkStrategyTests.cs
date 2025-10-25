using System;
using FlinkDotNet.DataStream.Watermarks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class WatermarkStrategyTests
    {
        [Test]
        public void ForBoundedOutOfOrderness_CreatesStrategyWithCorrectMaxOutOfOrderness()
        {
            // Arrange
            var maxOutOfOrderness = TimeSpan.FromSeconds(5);

            // Act
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(maxOutOfOrderness);

            // Assert
            Assert.That(strategy, Is.Not.Null);
            Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(maxOutOfOrderness));
            Assert.That(strategy.IsMonotonous, Is.False);
            Assert.That(strategy.HasTimestampAssigner, Is.False);
        }

        [Test]
        public void ForMonotonousTimestamps_CreatesStrategyWithZeroDelay()
        {
            // Act
            var strategy = WatermarkStrategy<string>.ForMonotonousTimestamps();

            // Assert
            Assert.That(strategy, Is.Not.Null);
            Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(TimeSpan.Zero));
            Assert.That(strategy.IsMonotonous, Is.True);
            Assert.That(strategy.HasTimestampAssigner, Is.False);
        }

        [Test]
        public void WithTimestampAssigner_UsingFunc_SetsAssigner()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1));
            Func<TestEvent, long> assigner = e => e.Timestamp;

            // Act
            var result = strategy.WithTimestampAssigner(assigner);

            // Assert
            Assert.That(result, Is.SameAs(strategy));
            Assert.That(strategy.HasTimestampAssigner, Is.True);
        }

        [Test]
        public void WithTimestampAssigner_UsingITimestampAssigner_SetsAssigner()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForMonotonousTimestamps();
            var assigner = new TestTimestampAssigner();

            // Act
            var result = strategy.WithTimestampAssigner(assigner);

            // Assert
            Assert.That(result, Is.SameAs(strategy));
            Assert.That(strategy.HasTimestampAssigner, Is.True);
        }

        [Test]
        public void ExtractTimestamp_WithoutAssigner_ThrowsInvalidOperationException()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1));
            var element = 42;

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() =>
                strategy.ExtractTimestamp(element, -1));
            Assert.That(ex.Message, Does.Contain("No timestamp assigner configured"));
        }

        [Test]
        public void ExtractTimestamp_WithFuncAssigner_ExtractsCorrectTimestamp()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1))
                .WithTimestampAssigner(e => e.Timestamp);
            var testEvent = new TestEvent { Timestamp = 1000L };

            // Act
            var timestamp = strategy.ExtractTimestamp(testEvent, -1);

            // Assert
            Assert.That(timestamp, Is.EqualTo(1000L));
        }

        [Test]
        public void ExtractTimestamp_WithITimestampAssigner_ExtractsCorrectTimestamp()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForMonotonousTimestamps()
                .WithTimestampAssigner(new TestTimestampAssigner());
            var testEvent = new TestEvent { Timestamp = 2000L };

            // Act
            var timestamp = strategy.ExtractTimestamp(testEvent, 1500);

            // Assert
            Assert.That(timestamp, Is.EqualTo(2000L));
        }

        [Test]
        public void GetCurrentWatermark_ForMonotonousTimestamps_ReturnsCurrentTimestamp()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForMonotonousTimestamps();
            var currentMaxTimestamp = 5000L;

            // Act
            var watermark = strategy.GetCurrentWatermark(currentMaxTimestamp);

            // Assert
            Assert.That(watermark, Is.EqualTo(currentMaxTimestamp));
        }

        [Test]
        public void GetCurrentWatermark_ForBoundedOutOfOrderness_SubtractsDelay()
        {
            // Arrange
            var maxOutOfOrderness = TimeSpan.FromSeconds(3); // 3000ms
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(maxOutOfOrderness);
            var currentMaxTimestamp = 10000L;

            // Act
            var watermark = strategy.GetCurrentWatermark(currentMaxTimestamp);

            // Assert
            Assert.That(watermark, Is.EqualTo(7000L)); // 10000 - 3000
        }

        [Test]
        public void GetCurrentWatermark_WithZeroMaxOutOfOrderness_ReturnsCurrentTimestamp()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.Zero);
            var currentMaxTimestamp = 8000L;

            // Act
            var watermark = strategy.GetCurrentWatermark(currentMaxTimestamp);

            // Assert
            Assert.That(watermark, Is.EqualTo(8000L));
        }

        [Test]
        public void GetCurrentWatermark_WithLargeDelay_CalculatesCorrectly()
        {
            // Arrange
            var maxOutOfOrderness = TimeSpan.FromMinutes(5); // 300000ms
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(maxOutOfOrderness);
            var currentMaxTimestamp = 500000L;

            // Act
            var watermark = strategy.GetCurrentWatermark(currentMaxTimestamp);

            // Assert
            Assert.That(watermark, Is.EqualTo(200000L)); // 500000 - 300000
        }

        [Test]
        public void MethodChaining_WithTimestampAssigner_WorksCorrectly()
        {
            // Act
            var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(2))
                .WithTimestampAssigner(e => e.Timestamp);

            // Assert
            Assert.That(strategy.HasTimestampAssigner, Is.True);
            Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(strategy.IsMonotonous, Is.False);
        }

        [Test]
        public void MonotonousStrategy_WithAssigner_ExtractsAndCalculatesWatermark()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForMonotonousTimestamps()
                .WithTimestampAssigner(e => e.Timestamp);
            var event1 = new TestEvent { Timestamp = 1000L };
            var event2 = new TestEvent { Timestamp = 2000L };

            // Act
            var ts1 = strategy.ExtractTimestamp(event1, -1);
            var ts2 = strategy.ExtractTimestamp(event2, ts1);
            var watermark = strategy.GetCurrentWatermark(ts2);

            // Assert
            Assert.That(ts1, Is.EqualTo(1000L));
            Assert.That(ts2, Is.EqualTo(2000L));
            Assert.That(watermark, Is.EqualTo(2000L)); // Monotonous = same as timestamp
        }

        [Test]
        public void BoundedOutOfOrdernessStrategy_WithAssigner_HandlesOutOfOrderEvents()
        {
            // Arrange
            var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1))
                .WithTimestampAssigner(e => e.Timestamp);
            var event1 = new TestEvent { Timestamp = 3000L };
            var event2 = new TestEvent { Timestamp = 2500L }; // Out of order
            var event3 = new TestEvent { Timestamp = 3500L };

            // Act
            var ts1 = strategy.ExtractTimestamp(event1, -1);
            var ts2 = strategy.ExtractTimestamp(event2, ts1);
            var ts3 = strategy.ExtractTimestamp(event3, ts2);
            var watermark = strategy.GetCurrentWatermark(ts3);

            // Assert
            Assert.That(ts1, Is.EqualTo(3000L));
            Assert.That(ts2, Is.EqualTo(2500L));
            Assert.That(ts3, Is.EqualTo(3500L));
            Assert.That(watermark, Is.EqualTo(2500L)); // 3500 - 1000ms delay
        }

        [Test]
        public void IsMonotonous_ForMonotonousStrategy_ReturnsTrue()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForMonotonousTimestamps();

            // Act & Assert
            Assert.That(strategy.IsMonotonous, Is.True);
        }

        [Test]
        public void IsMonotonous_ForBoundedOutOfOrdernessStrategy_ReturnsFalse()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

            // Act & Assert
            Assert.That(strategy.IsMonotonous, Is.False);
        }

        [Test]
        public void MaxOutOfOrderness_ReturnsConfiguredValue()
        {
            // Arrange
            var expectedDelay = TimeSpan.FromMilliseconds(500);
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(expectedDelay);

            // Act & Assert
            Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(expectedDelay));
        }

        [Test]
        public void HasTimestampAssigner_BeforeAssignment_ReturnsFalse()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1));

            // Act & Assert
            Assert.That(strategy.HasTimestampAssigner, Is.False);
        }

        [Test]
        public void HasTimestampAssigner_AfterAssignment_ReturnsTrue()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1))
                .WithTimestampAssigner(i => i);

            // Act & Assert
            Assert.That(strategy.HasTimestampAssigner, Is.True);
        }

        [Test]
        public void MultipleStrategies_AreIndependent()
        {
            // Arrange
            var strategy1 = WatermarkStrategy<int>.ForMonotonousTimestamps()
                .WithTimestampAssigner(i => i * 1000);
            var strategy2 = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5))
                .WithTimestampAssigner(i => i * 2000);

            // Act
            var ts1 = strategy1.ExtractTimestamp(1, -1);
            var ts2 = strategy2.ExtractTimestamp(1, -1);
            var wm1 = strategy1.GetCurrentWatermark(1000);
            var wm2 = strategy2.GetCurrentWatermark(2000);

            // Assert
            Assert.That(ts1, Is.EqualTo(1000));
            Assert.That(ts2, Is.EqualTo(2000));
            Assert.That(wm1, Is.EqualTo(1000)); // Monotonous
            Assert.That(wm2, Is.EqualTo(-3000)); // 2000 - 5000ms
        }

        [Test]
        public void ForBoundedOutOfOrderness_WithMilliseconds_WorksCorrectly()
        {
            // Arrange
            var maxOutOfOrderness = TimeSpan.FromMilliseconds(100);
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(maxOutOfOrderness);

            // Act
            var watermark = strategy.GetCurrentWatermark(1000L);

            // Assert
            Assert.That(watermark, Is.EqualTo(900L));
        }

        [Test]
        public void WithTimestampAssigner_CanBeCalledMultipleTimes()
        {
            // Arrange
            var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1))
                .WithTimestampAssigner(i => i * 1000)
                .WithTimestampAssigner(i => i * 2000); // Override

            // Act
            var timestamp = strategy.ExtractTimestamp(5, -1);

            // Assert
            Assert.That(timestamp, Is.EqualTo(10000)); // Uses last assigner: 5 * 2000
        }

        private class TestEvent
        {
            public long Timestamp
            {
                get; set;
            }
            public string Data { get; set; } = string.Empty;
        }

        private class TestTimestampAssigner : ITimestampAssigner<TestEvent>
        {
            public long ExtractTimestamp(TestEvent element, long previousElementTimestamp) => element.Timestamp;
        }
    }
}
