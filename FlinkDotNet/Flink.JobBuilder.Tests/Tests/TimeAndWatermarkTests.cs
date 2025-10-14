using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class TimeAndWatermarkTests
{
    #region Time Tests

    [Test]
    public void Time_Milliseconds_CreatesCorrectDuration()
    {
        // Arrange & Act
        var time = Time.Milliseconds(500);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(500));
    }

    [Test]
    public void Time_Seconds_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.Seconds(5);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(5000));
    }

    [Test]
    public void Time_Minutes_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.Minutes(2);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(120000));
    }

    [Test]
    public void Time_Hours_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.Hours(1);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(3600000));
    }

    [Test]
    public void Time_Days_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.Days(1);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(86400000));
    }

    [Test]
    public void Time_LowercaseMilliseconds_CreatesCorrectDuration()
    {
        // Arrange & Act
        var time = Time.milliseconds(750);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(750));
    }

    [Test]
    public void Time_LowercaseSeconds_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.seconds(10);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(10000));
    }

    [Test]
    public void Time_LowercaseMinutes_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.minutes(3);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(180000));
    }

    [Test]
    public void Time_LowercaseHours_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.hours(2);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(7200000));
    }

    [Test]
    public void Time_LowercaseDays_ConvertsToMilliseconds()
    {
        // Arrange & Act
        var time = Time.days(2);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(172800000));
    }

    [Test]
    public void Time_ToString_ReturnsFormattedString()
    {
        // Arrange
        var time = Time.Milliseconds(1500);

        // Act
        var result = time.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("1500ms"));
    }

    [Test]
    public void Time_ZeroMilliseconds_CreatesValidTime()
    {
        // Arrange & Act
        var time = Time.Milliseconds(0);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(0));
    }

    [Test]
    public void Time_LargeValue_HandlesCorrectly()
    {
        // Arrange & Act
        var time = Time.Days(365);

        // Assert
        Assert.That(time.ToMilliseconds(), Is.EqualTo(31536000000));
    }

    #endregion

    #region Watermark Tests

    [Test]
    public void Watermark_Constructor_SetsTimestamp()
    {
        // Arrange & Act
        var watermark = new Watermark(12345);

        // Assert
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(12345));
    }

    [Test]
    public void Watermark_ToString_ReturnsFormattedString()
    {
        // Arrange
        var watermark = new Watermark(9876543210);

        // Act
        var result = watermark.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("Watermark(9876543210)"));
    }

    [Test]
    public void Watermark_ZeroTimestamp_IsValid()
    {
        // Arrange & Act
        var watermark = new Watermark(0);

        // Assert
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(0));
    }

    [Test]
    public void Watermark_NegativeTimestamp_IsValid()
    {
        // Arrange & Act
        var watermark = new Watermark(-1);

        // Assert
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(-1));
    }

    [Test]
    public void Watermark_MaxValue_HandlesCorrectly()
    {
        // Arrange & Act
        var watermark = new Watermark(long.MaxValue);

        // Assert
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(long.MaxValue));
    }

    #endregion
}
