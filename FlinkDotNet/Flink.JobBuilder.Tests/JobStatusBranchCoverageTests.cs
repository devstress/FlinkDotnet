using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Tests for JobStatus class to achieve 100% branch coverage
/// Focuses on Duration property with all possible combinations of StartTime and EndTime
/// </summary>
[TestFixture]
public class JobStatusBranchCoverageTests
{
    #region Duration Property Tests

    [Test]
    public void Duration_WhenBothStartAndEndTimeSet_CalculatesDifference()
    {
        // Arrange
        var status = new JobStatus
        {
            JobId = "test-job",
            State = "FINISHED",
            StartTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            EndTime = new DateTime(2025, 1, 1, 11, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var duration = status.Duration;

        // Assert
        Assert.That(duration, Is.Not.Null);
        Assert.That(duration!.Value.TotalHours, Is.EqualTo(1.5).Within(0.01));
    }

    [Test]
    public void Duration_WhenOnlyStartTimeSet_ReturnsNull()
    {
        // Arrange
        var status = new JobStatus
        {
            JobId = "test-job",
            State = "RUNNING",
            StartTime = new DateTime(2025, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            EndTime = null
        };

        // Act
        var duration = status.Duration;

        // Assert
        Assert.That(duration, Is.Null);
    }

    [Test]
    public void Duration_WhenOnlyEndTimeSet_ReturnsNull()
    {
        // Arrange
        var status = new JobStatus
        {
            JobId = "test-job",
            State = "FAILED",
            StartTime = null,
            EndTime = new DateTime(2025, 1, 1, 11, 30, 0, DateTimeKind.Utc)
        };

        // Act
        var duration = status.Duration;

        // Assert
        Assert.That(duration, Is.Null);
    }

    [Test]
    public void Duration_WhenBothTimesNull_ReturnsNull()
    {
        // Arrange
        var status = new JobStatus
        {
            JobId = "test-job",
            State = "CREATED",
            StartTime = null,
            EndTime = null
        };

        // Act
        var duration = status.Duration;

        // Assert
        Assert.That(duration, Is.Null);
    }

    [Test]
    public void JobStatus_WithErrorMessage_StoresValue()
    {
        // Arrange & Act
        var status = new JobStatus
        {
            JobId = "test-job",
            State = "FAILED",
            ErrorMessage = "Connection timeout"
        };

        // Assert
        Assert.That(status.ErrorMessage, Is.EqualTo("Connection timeout"));
    }

    [Test]
    public void JobStatus_WithMetrics_StoresValue()
    {
        // Arrange
        var metrics = new JobMetrics
        {
            FlinkJobId = "flink-123",
            RecordsIn = 1000,
            RecordsOut = 950
        };

        // Act
        var status = new JobStatus
        {
            JobId = "test-job",
            Metrics = metrics
        };

        // Assert
        Assert.That(status.Metrics, Is.Not.Null);
        Assert.That(status.Metrics.FlinkJobId, Is.EqualTo("flink-123"));
        Assert.That(status.Metrics.RecordsIn, Is.EqualTo(1000));
    }

    #endregion
}
