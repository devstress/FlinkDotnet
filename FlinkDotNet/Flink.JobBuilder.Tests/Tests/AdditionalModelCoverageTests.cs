namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Additional tests to improve model coverage
/// </summary>
[TestFixture]
public class AdditionalModelCoverageTests
{
    [Test]
    public void JobSubmissionResult_CreateSuccess_SetsCorrectProperties()
    {
        // Arrange
        var jobId = "job-123";
        var flinkJobId = "flink-456";

        // Act
        var result = Models.JobSubmissionResult.CreateSuccess(jobId, flinkJobId);

        // Assert
        Assert.That(result.JobId, Is.EqualTo(jobId));
        Assert.That(result.FlinkJobId, Is.EqualTo(flinkJobId));
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public void JobSubmissionResult_CreateFailure_SetsCorrectProperties()
    {
        // Arrange
        var jobId = "job-123";
        var errorMessage = "Test error";

        // Act
        var result = Models.JobSubmissionResult.CreateFailure(jobId, errorMessage);

        // Assert
        Assert.That(result.JobId, Is.EqualTo(jobId));
        Assert.That(result.Success, Is.False);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo(errorMessage));
    }

    [Test]
    public void JobSubmissionResult_Metadata_CanBeModified()
    {
        // Arrange
        var result = new Models.JobSubmissionResult();

        // Act
        result.Metadata["key1"] = "value1";
        result.Metadata["key2"] = "value2";

        // Assert
        Assert.That(result.Metadata.Count, Is.EqualTo(2));
        Assert.That(result.Metadata["key1"], Is.EqualTo("value1"));
    }

    [Test]
    public void JobMetrics_CustomMetrics_CanBeModified()
    {
        // Arrange
        var metrics = new Models.JobMetrics();

        // Act
        metrics.CustomMetrics["latency"] = 100.5;
        metrics.CustomMetrics["throughput"] = "1000 req/s";

        // Assert
        Assert.That(metrics.CustomMetrics.Count, Is.EqualTo(2));
        Assert.That(metrics.CustomMetrics["latency"], Is.EqualTo(100.5));
    }

    [Test]
    public void JobMetrics_AllNumericProperties_CanBeSet()
    {
        // Act
        var metrics = new Models.JobMetrics
        {
            RecordsIn = 1000,
            RecordsOut = 900,
            Parallelism = 4,
            Checkpoints = 5,
            RecordsRead = 2000,
            RecordsWritten = 1800,
            BytesRead = 50000,
            BytesWritten = 45000
        };

        // Assert
        Assert.That(metrics.RecordsIn, Is.EqualTo(1000));
        Assert.That(metrics.RecordsOut, Is.EqualTo(900));
        Assert.That(metrics.Parallelism, Is.EqualTo(4));
        Assert.That(metrics.BytesRead, Is.EqualTo(50000));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_AllProperties_CanBeSet()
    {
        // Arrange
        var timeout = System.TimeSpan.FromSeconds(30);

        // Act
        var config = new Models.FlinkJobGatewayConfiguration
        {
            BaseUrl = "https://custom-host:9090",
            ApiKey = "test-api-key",
            HttpTimeout = timeout,
            UseHttps = true,
            MaxRetries = 5
        };

        // Assert
        Assert.That(config.BaseUrl, Is.EqualTo("https://custom-host:9090"));
        Assert.That(config.ApiKey, Is.EqualTo("test-api-key"));
        Assert.That(config.HttpTimeout, Is.EqualTo(timeout));
        Assert.That(config.UseHttps, Is.True);
        Assert.That(config.MaxRetries, Is.EqualTo(5));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_WithZeroRetries_IsValid()
    {
        // Act
        var config = new Models.FlinkJobGatewayConfiguration
        {
            MaxRetries = 0
        };

        // Assert
        Assert.That(config.MaxRetries, Is.EqualTo(0));
    }

    [Test]
    public void SavepointResult_AllProperties_CanBeSet()
    {
        // Act
        var result = new FlinkDotNet.DataStream.SavepointResult
        {
            SavepointPath = "/tmp/savepoint-123",
            Success = true,
            TriggerId = "trigger-456",
            Error = "No error"
        };

        // Assert
        Assert.That(result.SavepointPath, Is.EqualTo("/tmp/savepoint-123"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
    }

    [Test]
    public void StopWithSavepointResult_AllProperties_CanBeSet()
    {
        // Act
        var result = new FlinkDotNet.DataStream.StopWithSavepointResult
        {
            SavepointPath = "/tmp/savepoint-789",
            Success = true,
            TriggerId = "trigger-xyz",
            Drained = true
        };

        // Assert
        Assert.That(result.SavepointPath, Is.EqualTo("/tmp/savepoint-789"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Drained, Is.True);
    }
}
