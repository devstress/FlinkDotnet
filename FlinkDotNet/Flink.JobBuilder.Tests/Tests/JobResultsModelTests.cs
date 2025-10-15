using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class JobResultsModelTests
{
    #region JobSubmissionResult Tests

    [Test]
    public void JobSubmissionResult_DefaultConstructor_InitializesProperties()
    {
        var result = new JobSubmissionResult();

        Assert.That(result.JobId, Is.EqualTo(string.Empty));
        Assert.That(result.FlinkJobId, Is.EqualTo(string.Empty));
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.Metadata, Is.Not.Null);
        Assert.That(result.Metadata, Is.Empty);
    }

    [Test]
    public void JobSubmissionResult_SetJobId_ReturnsValue()
    {
        var result = new JobSubmissionResult { JobId = "job-123" };

        Assert.That(result.JobId, Is.EqualTo("job-123"));
    }

    [Test]
    public void JobSubmissionResult_SetFlinkJobId_ReturnsValue()
    {
        var result = new JobSubmissionResult { FlinkJobId = "flink-456" };

        Assert.That(result.FlinkJobId, Is.EqualTo("flink-456"));
    }

    [Test]
    public void JobSubmissionResult_SetSuccess_ReturnsValue()
    {
        var result = new JobSubmissionResult { Success = true };

        Assert.That(result.Success, Is.True);
        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void JobSubmissionResult_IsSuccess_ReturnsTrueWhenSuccessful()
    {
        var result = new JobSubmissionResult { Success = true };

        Assert.That(result.IsSuccess, Is.True);
    }

    [Test]
    public void JobSubmissionResult_IsSuccess_ReturnsFalseWhenFailed()
    {
        var result = new JobSubmissionResult { Success = false };

        Assert.That(result.IsSuccess, Is.False);
    }

    [Test]
    public void JobSubmissionResult_SetErrorMessage_ReturnsValue()
    {
        var result = new JobSubmissionResult { ErrorMessage = "Connection failed" };

        Assert.That(result.ErrorMessage, Is.EqualTo("Connection failed"));
    }

    [Test]
    public void JobSubmissionResult_SetMetadata_ReturnsValue()
    {
        var metadata = new Dictionary<string, string> { { "key", "value" } };
        var result = new JobSubmissionResult { Metadata = metadata };

        Assert.That(result.Metadata, Is.EqualTo(metadata));
        Assert.That(result.Metadata["key"], Is.EqualTo("value"));
    }

    [Test]
    public void JobSubmissionResult_CreateSuccess_ReturnsSuccessfulResult()
    {
        var result = JobSubmissionResult.CreateSuccess("job-123", "flink-456");

        Assert.That(result.JobId, Is.EqualTo("job-123"));
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-456"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.SubmittedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void JobSubmissionResult_CreateSuccess_SetsSubmittedAt()
    {
        var before = DateTime.UtcNow;
        var result = JobSubmissionResult.CreateSuccess("job-123", "flink-456");
        var after = DateTime.UtcNow;

        Assert.That(result.SubmittedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(result.SubmittedAt, Is.LessThanOrEqualTo(after));
    }

    [Test]
    public void JobSubmissionResult_CreateFailure_ReturnsFailedResult()
    {
        var result = JobSubmissionResult.CreateFailure("job-123", "Connection timeout");

        Assert.That(result.JobId, Is.EqualTo("job-123"));
        Assert.That(result.Success, Is.False);
        Assert.That(result.IsSuccess, Is.False);
        Assert.That(result.ErrorMessage, Is.EqualTo("Connection timeout"));
        Assert.That(result.SubmittedAt, Is.Not.EqualTo(default(DateTime)));
    }

    [Test]
    public void JobSubmissionResult_CreateFailure_SetsSubmittedAt()
    {
        var before = DateTime.UtcNow;
        var result = JobSubmissionResult.CreateFailure("job-123", "Error");
        var after = DateTime.UtcNow;

        Assert.That(result.SubmittedAt, Is.GreaterThanOrEqualTo(before));
        Assert.That(result.SubmittedAt, Is.LessThanOrEqualTo(after));
    }

    #endregion

    #region JobExecutionResult Tests

    [Test]
    public void JobExecutionResult_DefaultConstructor_InitializesProperties()
    {
        var result = new JobExecutionResult();

        Assert.That(result.JobId, Is.EqualTo(string.Empty));
        Assert.That(result.FlinkJobId, Is.EqualTo(string.Empty));
        Assert.That(result.State, Is.EqualTo(string.Empty));
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.CompletedAt, Is.Null);
        Assert.That(result.Metrics, Is.Null);
    }

    [Test]
    public void JobExecutionResult_SetAllProperties_ReturnsValues()
    {
        var metrics = new JobMetrics { FlinkJobId = "flink-123" };
        var completedAt = DateTime.UtcNow;

        var result = new JobExecutionResult
        {
            JobId = "job-123",
            FlinkJobId = "flink-456",
            State = "FINISHED",
            Success = true,
            Error = null,
            CompletedAt = completedAt,
            Metrics = metrics
        };

        Assert.That(result.JobId, Is.EqualTo("job-123"));
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-456"));
        Assert.That(result.State, Is.EqualTo("FINISHED"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
        Assert.That(result.CompletedAt, Is.EqualTo(completedAt));
        Assert.That(result.Metrics, Is.EqualTo(metrics));
    }

    [Test]
    public void JobExecutionResult_SetError_ReturnsValue()
    {
        var result = new JobExecutionResult { Error = "Job failed" };

        Assert.That(result.Error, Is.EqualTo("Job failed"));
    }

    #endregion

    #region JobStatus Tests

    [Test]
    public void JobStatus_DefaultConstructor_InitializesProperties()
    {
        var status = new JobStatus();

        Assert.That(status.JobId, Is.EqualTo(string.Empty));
        Assert.That(status.FlinkJobId, Is.EqualTo(string.Empty));
        Assert.That(status.State, Is.EqualTo(string.Empty));
        Assert.That(status.StartTime, Is.Null);
        Assert.That(status.EndTime, Is.Null);
        Assert.That(status.Duration, Is.Null);
        Assert.That(status.ErrorMessage, Is.Null);
        Assert.That(status.Metrics, Is.Null);
    }

    [Test]
    public void JobStatus_Duration_ReturnsNullWhenNoStartTime()
    {
        var status = new JobStatus
        {
            EndTime = DateTime.UtcNow
        };

        Assert.That(status.Duration, Is.Null);
    }

    [Test]
    public void JobStatus_Duration_ReturnsNullWhenNoEndTime()
    {
        var status = new JobStatus
        {
            StartTime = DateTime.UtcNow
        };

        Assert.That(status.Duration, Is.Null);
    }

    [Test]
    public void JobStatus_Duration_CalculatesDurationWhenBothTimesSet()
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);

        var status = new JobStatus
        {
            StartTime = startTime,
            EndTime = endTime
        };

        Assert.That(status.Duration, Is.Not.Null);
        Assert.That(status.Duration!.Value.TotalMinutes, Is.EqualTo(5).Within(0.1));
    }

    [Test]
    public void JobStatus_SetAllProperties_ReturnsValues()
    {
        var startTime = DateTime.UtcNow.AddMinutes(-10);
        var endTime = DateTime.UtcNow;
        var metrics = new JobMetrics();

        var status = new JobStatus
        {
            JobId = "job-123",
            FlinkJobId = "flink-456",
            State = "RUNNING",
            StartTime = startTime,
            EndTime = endTime,
            ErrorMessage = "Warning message",
            Metrics = metrics
        };

        Assert.That(status.JobId, Is.EqualTo("job-123"));
        Assert.That(status.FlinkJobId, Is.EqualTo("flink-456"));
        Assert.That(status.State, Is.EqualTo("RUNNING"));
        Assert.That(status.StartTime, Is.EqualTo(startTime));
        Assert.That(status.EndTime, Is.EqualTo(endTime));
        Assert.That(status.ErrorMessage, Is.EqualTo("Warning message"));
        Assert.That(status.Metrics, Is.EqualTo(metrics));
    }

    #endregion

    #region JobMetrics Tests

    [Test]
    public void JobMetrics_DefaultConstructor_InitializesProperties()
    {
        var metrics = new JobMetrics();

        Assert.That(metrics.FlinkJobId, Is.EqualTo(string.Empty));
        Assert.That(metrics.Runtime, Is.Null);
        Assert.That(metrics.RecordsIn, Is.EqualTo(0));
        Assert.That(metrics.RecordsOut, Is.EqualTo(0));
        Assert.That(metrics.Parallelism, Is.EqualTo(0));
        Assert.That(metrics.Checkpoints, Is.EqualTo(0));
        Assert.That(metrics.LastCheckpoint, Is.Null);
        Assert.That(metrics.RecordsRead, Is.EqualTo(0));
        Assert.That(metrics.RecordsWritten, Is.EqualTo(0));
        Assert.That(metrics.BytesRead, Is.EqualTo(0));
        Assert.That(metrics.BytesWritten, Is.EqualTo(0));
        Assert.That(metrics.Duration, Is.Null);
        Assert.That(metrics.CustomMetrics, Is.Not.Null);
        Assert.That(metrics.CustomMetrics, Is.Empty);
    }

    [Test]
    public void JobMetrics_SetAllProperties_ReturnsValues()
    {
        var runtime = TimeSpan.FromMinutes(30);
        var duration = TimeSpan.FromMinutes(35);
        var lastCheckpoint = DateTime.UtcNow;
        var customMetrics = new Dictionary<string, object> { { "custom", "value" } };

        var metrics = new JobMetrics
        {
            FlinkJobId = "flink-123",
            Runtime = runtime,
            RecordsIn = 1000,
            RecordsOut = 950,
            Parallelism = 8,
            Checkpoints = 5,
            LastCheckpoint = lastCheckpoint,
            RecordsRead = 1000,
            RecordsWritten = 950,
            BytesRead = 1024000,
            BytesWritten = 950000,
            Duration = duration,
            CustomMetrics = customMetrics
        };

        Assert.That(metrics.FlinkJobId, Is.EqualTo("flink-123"));
        Assert.That(metrics.Runtime, Is.EqualTo(runtime));
        Assert.That(metrics.RecordsIn, Is.EqualTo(1000));
        Assert.That(metrics.RecordsOut, Is.EqualTo(950));
        Assert.That(metrics.Parallelism, Is.EqualTo(8));
        Assert.That(metrics.Checkpoints, Is.EqualTo(5));
        Assert.That(metrics.LastCheckpoint, Is.EqualTo(lastCheckpoint));
        Assert.That(metrics.RecordsRead, Is.EqualTo(1000));
        Assert.That(metrics.RecordsWritten, Is.EqualTo(950));
        Assert.That(metrics.BytesRead, Is.EqualTo(1024000));
        Assert.That(metrics.BytesWritten, Is.EqualTo(950000));
        Assert.That(metrics.Duration, Is.EqualTo(duration));
        Assert.That(metrics.CustomMetrics, Is.EqualTo(customMetrics));
    }

    [Test]
    public void JobMetrics_CustomMetrics_CanStoreMultipleValues()
    {
        var metrics = new JobMetrics();
        metrics.CustomMetrics["throughput"] = 1000.5;
        metrics.CustomMetrics["latency"] = 15;
        metrics.CustomMetrics["errors"] = true;

        Assert.That(metrics.CustomMetrics["throughput"], Is.EqualTo(1000.5));
        Assert.That(metrics.CustomMetrics["latency"], Is.EqualTo(15));
        Assert.That(metrics.CustomMetrics["errors"], Is.EqualTo(true));
    }

    #endregion

    #region FlinkJobGatewayConfiguration Tests

    [Test]
    public void FlinkJobGatewayConfiguration_DefaultConstructor_SetsDefaults()
    {
        var config = new FlinkJobGatewayConfiguration();

        Assert.That(config.BaseUrl, Is.EqualTo("http://localhost:8080"));
        Assert.That(config.ApiKey, Is.Null);
        Assert.That(config.HttpTimeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(config.UseHttps, Is.False);
        Assert.That(config.MaxRetries, Is.EqualTo(3));
        Assert.That(config.RetryDelay, Is.EqualTo(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetBaseUrl_ReturnsValue()
    {
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://flink-gateway:9090"
        };

        Assert.That(config.BaseUrl, Is.EqualTo("http://flink-gateway:9090"));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetApiKey_ReturnsValue()
    {
        var config = new FlinkJobGatewayConfiguration
        {
            ApiKey = "secret-key-123"
        };

        Assert.That(config.ApiKey, Is.EqualTo("secret-key-123"));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetHttpTimeout_ReturnsValue()
    {
        var timeout = TimeSpan.FromMinutes(10);
        var config = new FlinkJobGatewayConfiguration
        {
            HttpTimeout = timeout
        };

        Assert.That(config.HttpTimeout, Is.EqualTo(timeout));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetUseHttps_ReturnsValue()
    {
        var config = new FlinkJobGatewayConfiguration
        {
            UseHttps = true
        };

        Assert.That(config.UseHttps, Is.True);
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetMaxRetries_ReturnsValue()
    {
        var config = new FlinkJobGatewayConfiguration
        {
            MaxRetries = 5
        };

        Assert.That(config.MaxRetries, Is.EqualTo(5));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetRetryDelay_ReturnsValue()
    {
        var delay = TimeSpan.FromSeconds(2);
        var config = new FlinkJobGatewayConfiguration
        {
            RetryDelay = delay
        };

        Assert.That(config.RetryDelay, Is.EqualTo(delay));
    }

    [Test]
    public void FlinkJobGatewayConfiguration_SetAllProperties_ReturnsValues()
    {
        var timeout = TimeSpan.FromMinutes(15);
        var delay = TimeSpan.FromSeconds(3);

        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "https://flink.example.com",
            ApiKey = "api-key-789",
            HttpTimeout = timeout,
            UseHttps = true,
            MaxRetries = 10,
            RetryDelay = delay
        };

        Assert.That(config.BaseUrl, Is.EqualTo("https://flink.example.com"));
        Assert.That(config.ApiKey, Is.EqualTo("api-key-789"));
        Assert.That(config.HttpTimeout, Is.EqualTo(timeout));
        Assert.That(config.UseHttps, Is.True);
        Assert.That(config.MaxRetries, Is.EqualTo(10));
        Assert.That(config.RetryDelay, Is.EqualTo(delay));
    }

    #endregion
}
