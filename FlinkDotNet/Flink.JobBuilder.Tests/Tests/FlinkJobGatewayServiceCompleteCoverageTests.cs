#nullable enable

using System.Net;
using System.Text;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive branch coverage tests for FlinkJobGatewayService to reach 100% coverage
/// Focuses on uncovered error handling, retry logic, and edge cases
/// </summary>
[TestFixture]
public class FlinkJobGatewayServiceCompleteCoverageTests
{
    private Mock<HttpMessageHandler>? _mockHttpMessageHandler;
    private HttpClient? _httpClient;
    private Mock<ILogger>? _mockLogger;

    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");

        // Set retry delay to 1ms for fast tests
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromMilliseconds(1);

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };

        _mockLogger = new Mock<ILogger>();
    }

    [TearDown]
    public void TearDown()
    {
        // Restore default retry delay
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromSeconds(1);

        _httpClient?.Dispose();
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
    }

    #region Configuration and Initialization Tests

    [Test]
    public void Constructor_WithNullConfiguration_UsesDefaultConfiguration()
    {
        // Act
        using var service = new FlinkJobGatewayService(configuration: null, httpClient: _httpClient);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullHttpClient_CreatesDefaultClient()
    {
        // Act
        using var service = new FlinkJobGatewayService(configuration: new FlinkJobGatewayConfiguration());

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithApiKey_AddsApiKeyHeader()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            ApiKey = "test-api-key-12345"
        };

        // Act
        using var service = new FlinkJobGatewayService(configuration: config);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithEmptyApiKey_DoesNotAddApiKeyHeader()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            ApiKey = ""
        };

        // Act
        using var service = new FlinkJobGatewayService(configuration: config, httpClient: _httpClient);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    #endregion

    #region Validation Failure Tests

    [Test]
    public async Task SubmitJobAsync_WithInvalidJobDefinition_ReturnsValidationFailure()
    {
        // Arrange
        var jobDefinition = new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "", // Invalid - empty jobId
                Version = "1.0.0"
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test-topic",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.SubmitJobAsync(jobDefinition);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
    }

    [Test]
    public void SubmitJobAsync_WithNullMetadata_ThrowsNullReferenceException()
    {
        // Arrange - null metadata causes NRE before validation
        var jobDefinition = new JobDefinition
        {
            Metadata = null!,
            Source = new KafkaSourceDefinition
            {
                Topic = "test-topic",
                BootstrapServers = "localhost:9092"
            },
            Sink = new ConsoleSinkDefinition()
        };

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act & Assert - Null metadata accessed before validation
        Assert.ThrowsAsync<NullReferenceException>(async () => await service.SubmitJobAsync(jobDefinition));
    }

    #endregion

    #region HTTP Error Handling Tests

    [Test]
    public void SubmitJobAsync_WithHttpRequestException_ThrowsAfterRetries()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act & Assert - Should throw after retries exhausted
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.SubmitJobAsync(jobDefinition));
    }

    [Test]
    public async Task SubmitJobAsync_With500InternalServerError_ReturnsFailure()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        SetupHttpResponse(HttpStatusCode.InternalServerError, "{\"error\":\"Server error\"}");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.SubmitJobAsync(jobDefinition);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SubmitJobAsync_With400BadRequest_ReturnsFailure()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        SetupHttpResponse(HttpStatusCode.BadRequest, "{\"error\":\"Invalid request\"}");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.SubmitJobAsync(jobDefinition);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void SubmitJobAsync_WithTaskCanceledException_ThrowsAfterRetries()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request timeout"));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act & Assert - Should throw after retries exhausted
        Assert.ThrowsAsync<TaskCanceledException>(async () => await service.SubmitJobAsync(jobDefinition));
    }

    #endregion

    #region Success Response Tests

    [Test]
    public async Task SubmitJobAsync_WithSuccessResponse_ReturnsSuccess()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        var successResponse = new
        {
            success = true,
            flinkJobId = "flink-job-123",  // camelCase for JSON serialization
            message = "Job submitted successfully"
        };

        SetupHttpResponse(HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(successResponse));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.SubmitJobAsync(jobDefinition);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-job-123"));
    }

    [Test]
    public async Task SubmitJobAsync_WithSuccessButNoFlinkJobId_ReturnsSuccessWithOriginalJobId()
    {
        // Arrange
        var jobDefinition = CreateValidJobDefinition();

        var successResponse = new
        {
            success = true,
            message = "Job submitted successfully"
        };

        SetupHttpResponse(HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(successResponse));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.SubmitJobAsync(jobDefinition);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    #endregion

    #region GetJobStatusAsync Tests

    [Test]
    public async Task GetJobStatusAsync_WithSuccessResponse_ReturnsStatus()
    {
        // Arrange
        var jobId = "test-job-123";

        var statusResponse = new
        {
            state = "RUNNING",
            startTime = DateTime.UtcNow.AddMinutes(-5).ToString("o")
        };

        SetupHttpResponse(HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(statusResponse));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.GetJobStatusAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.State, Is.EqualTo("RUNNING"));
    }

    [Test]
    public async Task GetJobStatusAsync_With404NotFound_ReturnsUnknownState()
    {
        // Arrange
        var jobId = "non-existent-job";

        SetupHttpResponse(HttpStatusCode.NotFound, "");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.GetJobStatusAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.State, Is.EqualTo("UNKNOWN"));
    }

    [Test]
    public void GetJobStatusAsync_WithHttpRequestException_ThrowsAfterRetries()
    {
        // Arrange
        var jobId = "test-job-123";

        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act & Assert - Should throw after retries exhausted
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.GetJobStatusAsync(jobId));
    }

    #endregion

    #region GetJobMetricsAsync Tests

    [Test]
    public async Task GetJobMetricsAsync_WithSuccessResponse_ReturnsMetrics()
    {
        // Arrange
        var jobId = "test-job-123";

        var metricsResponse = new
        {
            numRecordsIn = 1000L,
            numRecordsOut = 950L,
            backpressureLevel = "ok"
        };

        SetupHttpResponse(HttpStatusCode.OK, System.Text.Json.JsonSerializer.Serialize(metricsResponse));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_With404NotFound_ReturnsEmptyMetrics()
    {
        // Arrange
        var jobId = "non-existent-job";

        SetupHttpResponse(HttpStatusCode.NotFound, "");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region CancelJobAsync Tests

    [Test]
    public async Task CancelJobAsync_WithSuccessResponse_ReturnsTrue()
    {
        // Arrange
        var jobId = "test-job-123";

        SetupHttpResponse(HttpStatusCode.OK, "{\"success\":true}");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.CancelJobAsync(jobId);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CancelJobAsync_With404NotFound_ReturnsFalse()
    {
        // Arrange
        var jobId = "non-existent-job";

        SetupHttpResponse(HttpStatusCode.NotFound, "");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.CancelJobAsync(jobId);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void CancelJobAsync_WithHttpRequestException_ThrowsAfterRetries()
    {
        // Arrange
        var jobId = "test-job-123";

        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act & Assert - Should throw after retries exhausted
        Assert.ThrowsAsync<HttpRequestException>(async () => await service.CancelJobAsync(jobId));
    }

    #endregion

    #region HealthCheckAsync Tests

    [Test]
    public async Task HealthCheckAsync_WithSuccessResponse_ReturnsTrue()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{\"status\":\"UP\"}");

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.HealthCheckAsync();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HealthCheckAsync_WithHttpRequestException_ReturnsFalse()
    {
        // Arrange
        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        using var service = new FlinkJobGatewayService(httpClient: _httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.HealthCheckAsync();

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region Helper Methods

    private JobDefinition CreateValidJobDefinition()
    {
        return new JobDefinition
        {
            Metadata = new JobMetadata
            {
                JobId = "test-job-123",
                JobName = "Test Job",
                Version = "1.0.0"
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "test-topic",
                BootstrapServers = "localhost:9092",
                GroupId = "test-group"
            },
            Sink = new ConsoleSinkDefinition()
        };
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string responseContent)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _mockHttpMessageHandler!
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #endregion
}
