using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class TimeTests
    {
        #region Time.Milliseconds Tests

        [Test]
        public void Milliseconds_WithValidValue_CreatesTime()
        {
            // Arrange
            long milliseconds = 1000;

            // Act
            var time = Time.Milliseconds(milliseconds);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(milliseconds));
        }

        [Test]
        public void milliseconds_LowercaseAlias_CreatesTime()
        {
            // Arrange
            long milliseconds = 500;

            // Act
            var time = Time.milliseconds(milliseconds);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(milliseconds));
        }

        #endregion

        #region Time.Seconds Tests

        [Test]
        public void Seconds_WithValidValue_CreatesTime()
        {
            // Arrange
            long seconds = 5;

            // Act
            var time = Time.Seconds(seconds);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(5000));
        }

        [Test]
        public void seconds_LowercaseAlias_CreatesTime()
        {
            // Arrange
            long seconds = 10;

            // Act
            var time = Time.seconds(seconds);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(10000));
        }

        #endregion

        #region Time.Minutes Tests

        [Test]
        public void Minutes_WithValidValue_CreatesTime()
        {
            // Arrange
            long minutes = 2;

            // Act
            var time = Time.Minutes(minutes);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(120000)); // 2 * 60 * 1000
        }

        [Test]
        public void minutes_LowercaseAlias_CreatesTime()
        {
            // Arrange
            long minutes = 3;

            // Act
            var time = Time.minutes(minutes);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(180000)); // 3 * 60 * 1000
        }

        #endregion

        #region Time.Hours Tests

        [Test]
        public void Hours_WithValidValue_CreatesTime()
        {
            // Arrange
            long hours = 1;

            // Act
            var time = Time.Hours(hours);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000)); // 1 * 60 * 60 * 1000
        }

        [Test]
        public void hours_LowercaseAlias_CreatesTime()
        {
            // Arrange
            long hours = 2;

            // Act
            var time = Time.hours(hours);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(7200000)); // 2 * 60 * 60 * 1000
        }

        #endregion

        #region Time.Days Tests

        [Test]
        public void Days_WithValidValue_CreatesTime()
        {
            // Arrange
            long days = 1;

            // Act
            var time = Time.Days(days);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000)); // 1 * 24 * 60 * 60 * 1000
        }

        [Test]
        public void days_LowercaseAlias_CreatesTime()
        {
            // Arrange
            long days = 2;

            // Act
            var time = Time.days(days);

            // Assert
            Assert.That(time, Is.Not.Null);
            Assert.That(time.ToMilliseconds(), Is.EqualTo(172800000)); // 2 * 24 * 60 * 60 * 1000
        }

        #endregion

        #region Time.ToString Tests

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            var time = Time.Milliseconds(5000);

            // Act
            var result = time.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("5000ms"));
        }

        [Test]
        public void ToString_WithZeroMilliseconds_ReturnsZero()
        {
            // Arrange
            var time = Time.Milliseconds(0);

            // Act
            var result = time.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("0ms"));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Milliseconds_WithZero_CreatesTime()
        {
            // Act
            var time = Time.Milliseconds(0);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(0));
        }

        [Test]
        public void Milliseconds_WithLargeValue_CreatesTime()
        {
            // Arrange
            long largeValue = long.MaxValue;

            // Act
            var time = Time.Milliseconds(largeValue);

            // Assert
            Assert.That(time.ToMilliseconds(), Is.EqualTo(largeValue));
        }

        [Test]
        public void Seconds_WithLargeValue_HandlesOverflow()
        {
            // Arrange - This will overflow but should still work
            long largeSeconds = long.MaxValue / 1000;

            // Act
            var time = Time.Seconds(largeSeconds);

            // Assert
            Assert.That(time, Is.Not.Null);
        }

        #endregion
    }

    [TestFixture]
    public class WatermarkTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidTimestamp_CreatesWatermark()
        {
            // Arrange
            long timestamp = 1234567890;

            // Act
            var watermark = new Watermark(timestamp);

            // Assert
            Assert.That(watermark, Is.Not.Null);
            Assert.That(watermark.GetTimestamp(), Is.EqualTo(timestamp));
        }

        [Test]
        public void Constructor_WithZeroTimestamp_CreatesWatermark()
        {
            // Act
            var watermark = new Watermark(0);

            // Assert
            Assert.That(watermark.GetTimestamp(), Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNegativeTimestamp_CreatesWatermark()
        {
            // Arrange
            long negativeTimestamp = -1000;

            // Act
            var watermark = new Watermark(negativeTimestamp);

            // Assert
            Assert.That(watermark.GetTimestamp(), Is.EqualTo(negativeTimestamp));
        }

        #endregion

        #region GetTimestamp Tests

        [Test]
        public void GetTimestamp_ReturnsCorrectValue()
        {
            // Arrange
            long expectedTimestamp = 9876543210;
            var watermark = new Watermark(expectedTimestamp);

            // Act
            var timestamp = watermark.GetTimestamp();

            // Assert
            Assert.That(timestamp, Is.EqualTo(expectedTimestamp));
        }

        #endregion

        #region ToString Tests

        [Test]
        public void ToString_ReturnsCorrectFormat()
        {
            // Arrange
            long timestamp = 12345;
            var watermark = new Watermark(timestamp);

            // Act
            var result = watermark.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Watermark(12345)"));
        }

        [Test]
        public void ToString_WithZeroTimestamp_ReturnsCorrectFormat()
        {
            // Arrange
            var watermark = new Watermark(0);

            // Act
            var result = watermark.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Watermark(0)"));
        }

        [Test]
        public void ToString_WithNegativeTimestamp_ReturnsCorrectFormat()
        {
            // Arrange
            var watermark = new Watermark(-5000);

            // Act
            var result = watermark.ToString();

            // Assert
            Assert.That(result, Is.EqualTo("Watermark(-5000)"));
        }

        #endregion

        #region Multiple Watermark Tests

        [Test]
        public void MultipleWatermarks_WithDifferentTimestamps_AreIndependent()
        {
            // Arrange
            var watermark1 = new Watermark(1000);
            var watermark2 = new Watermark(2000);

            // Act & Assert
            Assert.That(watermark1.GetTimestamp(), Is.EqualTo(1000));
            Assert.That(watermark2.GetTimestamp(), Is.EqualTo(2000));
            Assert.That(watermark1.GetTimestamp(), Is.Not.EqualTo(watermark2.GetTimestamp()));
        }

        #endregion
    }
}
