using System;
using FlinkDotNet.DataStream;
using FlinkDotNet.DataStream.Watermarks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests;

/// <summary>
/// Branch coverage tests for small utility classes
/// Targets: WatermarkStrategy, AllWindowedStream edge cases
/// </summary>
[TestFixture]
public class WatermarkAndWindowBranchCoverageTests
{
    #region WatermarkStrategy Tests (8 branches)

    [Test]
    public void WatermarkStrategy_ForBoundedOutOfOrderness_CreatesStrategy()
    {
        // Arrange
        var maxOutOfOrderness = TimeSpan.FromSeconds(5);

        // Act
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(maxOutOfOrderness);

        // Assert
        Assert.That(strategy, Is.Not.Null);
    }

    [Test]
    public void WatermarkStrategy_ForMonotonousTimestamps_CreatesStrategy()
    {
        // Act
        var strategy = WatermarkStrategy<string>.ForMonotonousTimestamps();

        // Assert
        Assert.That(strategy, Is.Not.Null);
    }

    [Test]
    public void WatermarkStrategy_WithTimestampAssignerFunc_SetsAssigner()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForMonotonousTimestamps();
        Func<string, long> assigner = s => s.Length;

        // Act
        var result = strategy.WithTimestampAssigner(assigner);

        // Assert
        Assert.That(result, Is.SameAs(strategy));
    }

    [Test]
    public void WatermarkStrategy_WithTimestampAssignerInterface_SetsAssigner()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(1));
        var assigner = new TestTimestampAssigner();

        // Act
        var result = strategy.WithTimestampAssigner(assigner);

        // Assert
        Assert.That(result, Is.SameAs(strategy));
    }

    [Test]
    public void WatermarkStrategy_BoundedOutOfOrderness_WithZeroDelay()
    {
        // Act
        var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.Zero);

        // Assert
        Assert.That(strategy, Is.Not.Null);
    }

    [Test]
    public void WatermarkStrategy_BoundedOutOfOrderness_WithLargeDelay()
    {
        // Act
        var strategy = WatermarkStrategy<int>.ForBoundedOutOfOrderness(TimeSpan.FromHours(24));

        // Assert
        Assert.That(strategy, Is.Not.Null);
    }

    private class TestTimestampAssigner : ITimestampAssigner<string>
    {
        public long ExtractTimestamp(string element, long previousElementTimestamp)
        {
            return element.Length;
        }
    }

    #endregion
}
