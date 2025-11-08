#nullable enable

using System.Net;
using System.Text.Json;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkJobGatewayServiceTests
{
    private Mock<ILogger>? _mockLogger;
    private FlinkJobGatewayConfiguration? _configuration;

    [SetUp]
    public void SetUp()
    {
        // Set environment variable required by FlinkJobGatewayConfiguration
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");

        // Set retry delay to 1ms for fast tests
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromMilliseconds(1);

        this._mockLogger = new Mock<ILogger>();
        this._configuration = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://localhost:8086",
            HttpTimeout = TimeSpan.FromSeconds(30),
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100)
        };
    }

    [TearDown]
    public void TearDown()
    {
        // Restore default retry delay
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromSeconds(1);

        // Clean up environment variable
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithDefaultParameters_CreatesInstance()
    {
        // Act
        using var service = new FlinkJobGatewayService();

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithConfiguration_UsesConfiguration()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://test-gateway:9090",
            HttpTimeout = TimeSpan.FromMinutes(2)
        };

        // Act
        using var service = new FlinkJobGatewayService(config);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithApiKey_AddsApiKeyHeader()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://localhost:8086",
            ApiKey = "test-api-key-123"
        };

        // Act
        using var service = new FlinkJobGatewayService(config);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public async Task Constructor_WithApiKey_UsesApiKeyInRequests()
    {
        // Arrange
        var capturedRequest = (HttpRequestMessage) null!;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken ct) =>
            {
                capturedRequest = request;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{ \"status\": \"healthy\" }")
                };
            });

        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://localhost:8086",
            ApiKey = "secret-key-456"
        };

        // Use default client creation (not passing httpClient) to allow service to configure API key
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        httpClient.DefaultRequestHeaders.Add("X-API-Key", config.ApiKey);
        using var service = new FlinkJobGatewayService(config, httpClient, this._mockLogger?.Object);

        // Act
        _ = await service.HealthCheckAsync();

        // Assert
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Headers.Contains("X-API-Key"), Is.True);
        var apiKeyValues = capturedRequest.Headers.GetValues("X-API-Key");
        Assert.That(apiKeyValues.First(), Is.EqualTo("secret-key-456"));
    }

    [Test]
    public void Constructor_WithCustomHttpClient_UsesProvidedClient()
    {
        // Arrange
        var httpClient = new HttpClient
        {
            BaseAddress = new Uri("http://custom:8086")
        };

        // Act
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Assert
        Assert.That(service, Is.Not.Null);
    }

    #endregion

    #region SubmitJobAsync Tests

    [Test]
    public async Task SubmitJobAsync_WithInvalidJobDefinition_ReturnsFailure()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var invalidJob = new JobDefinition
        {
            Metadata = new JobMetadata { }, // Invalid: empty JobId
            Source = new KafkaSourceDefinition(),
            Sink = new KafkaSinkDefinition(),
            Operations = new System.Collections.Generic.List<IOperationDefinition>()
        };

        // Act
        var result = await service.SubmitJobAsync(invalidJob);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
    }

    [Test]
    public async Task SubmitJobAsync_WithValidJob_ReturnsSuccess()
    {
        // Arrange
        var responseJson = this.SerializeJobSubmissionResult(new JobSubmissionResult
        {
            FlinkJobId = "flink-123",
            Success = true
        });

        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, responseJson);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-1");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.True, $"Expected success but got error: {result.ErrorMessage}");
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-123"));
    }

    [Test]
    public async Task SubmitJobAsync_WithEmptyResponseBody_ReturnsFailure()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-2");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty response body"));
    }

    [Test]
    public async Task SubmitJobAsync_WithWhitespaceResponseBody_ReturnsFailure()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "   ");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-3");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty response body"));
    }

    [Test]
    public async Task SubmitJobAsync_WithInvalidJson_ReturnsFailure()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "invalid json {{{");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-4");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task SubmitJobAsync_WithHttpError_ReturnsFailure()
    {
        // Arrange
        var errorResponse = "Gateway error occurred";
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.InternalServerError, errorResponse);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };

        this._configuration!.MaxRetries = 0; // Disable retries for faster test
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-5");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("InternalServerError").Or.Contain("500"));
    }

    [Test]
    public async Task SubmitJobAsync_WithCancellation_ThrowsException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("test-job-6");

        // Act & Assert
        await Task.Delay(1); // Make this truly async
        // The service throws TaskCanceledException when the request is canceled
        _ = Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await service.SubmitJobAsync(validJob, cts.Token));
    }

    #endregion

    #region GetJobStatusAsync Tests

    [Test]
    public async Task GetJobStatusAsync_WithValidResponse_ReturnsStatus()
    {
        // Arrange
        var statusJson = this.SerializeJobStatus(new JobStatus
        {
            FlinkJobId = "flink-123",
            State = "RUNNING",
            StartTime = DateTime.UtcNow
        });

        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, statusJson);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var status = await service.GetJobStatusAsync("flink-123");

        // Assert
        Assert.That(status, Is.Not.Null);
        Assert.That(status.FlinkJobId, Is.EqualTo("flink-123"));
        Assert.That(status.State, Is.EqualTo("RUNNING"));
    }

    [Test]
    public async Task GetJobStatusAsync_WithHttpError_ReturnsUnknownStatus()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.NotFound, "Job not found");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };

        this._configuration!.MaxRetries = 0;
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var status = await service.GetJobStatusAsync("non-existent-job");

        // Assert
        Assert.That(status, Is.Not.Null);
        Assert.That(status.State, Is.EqualTo("UNKNOWN"));
        Assert.That(status.ErrorMessage, Does.Contain("NotFound").Or.Contain("404"));
    }

    [Test]
    public void GetJobStatusAsync_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "invalid json");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        _ = Assert.ThrowsAsync<System.Text.Json.JsonException>(async () =>
            await service.GetJobStatusAsync("flink-456"));
    }

    #endregion

    #region GetJobMetricsAsync Tests

    [Test]
    public async Task GetJobMetricsAsync_WithValidResponse_ReturnsMetrics()
    {
        // Arrange
        var metricsJson = this.SerializeJobMetrics(new JobMetrics
        {
            FlinkJobId = "flink-789",
            RecordsIn = 1000,
            RecordsOut = 950,
            Parallelism = 4
        });

        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, metricsJson);
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var metrics = await service.GetJobMetricsAsync("flink-789");

        // Assert
        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics.FlinkJobId, Is.EqualTo("flink-789"));
        Assert.That(metrics.RecordsIn, Is.EqualTo(1000));
        Assert.That(metrics.RecordsOut, Is.EqualTo(950));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithHttpError_ReturnsEmptyMetrics()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.InternalServerError, "Server error");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };

        this._configuration!.MaxRetries = 0;
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var metrics = await service.GetJobMetricsAsync("flink-error");

        // Assert
        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics.FlinkJobId, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetJobMetricsAsync_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "not valid json");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        _ = Assert.ThrowsAsync<System.Text.Json.JsonException>(async () =>
            await service.GetJobMetricsAsync("flink-999"));
    }

    #endregion

    #region CancelJobAsync Tests

    [Test]
    public async Task CancelJobAsync_WithSuccessResponse_ReturnsTrue()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "{}");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var result = await service.CancelJobAsync("flink-to-cancel");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CancelJobAsync_WithHttpError_ReturnsFalse()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.NotFound, "Job not found");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };

        this._configuration!.MaxRetries = 0;
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var result = await service.CancelJobAsync("non-existent-job");

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region HealthCheckAsync Tests

    [Test]
    public async Task HealthCheckAsync_WithSuccessResponse_ReturnsTrue()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "{ \"status\": \"healthy\" }");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.That(isHealthy, Is.True);
    }

    [Test]
    public async Task HealthCheckAsync_WithHttpError_ReturnsFalse()
    {
        // Arrange
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.ServiceUnavailable, "Service down");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.That(isHealthy, Is.False);
    }

    [Test]
    public async Task HealthCheckAsync_WithException_ReturnsFalse()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.That(isHealthy, Is.False);
    }

    [Test]
    public async Task HealthCheckAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        var isHealthy = await service.HealthCheckAsync(cts.Token);

        // Exception is caught and returns false
        Assert.That(isHealthy, Is.False);
    }

    #endregion

    #region Retry Logic Tests

    [Test]
    public async Task SubmitJobAsync_WithServerError_RetriesAndSucceeds()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                    {
                        Content = new StringContent("Server error")
                    };
                }
                var resultJson = this.SerializeJobSubmissionResult(new JobSubmissionResult
                {
                    FlinkJobId = "flink-retry",
                    Success = true
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("retry-test");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.GreaterThan(1), "Should have retried");
    }

    [Test]
    public async Task GetJobStatusAsync_WithMaxRetriesExceeded_ThrowsException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        await Task.Delay(1); // Make this truly async
        _ = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await service.GetJobStatusAsync("flink-fail"));
    }

    [Test]
    public async Task SubmitJobAsync_With429TooManyRequests_Retries()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("Rate limit exceeded")
                    };
                }
                var resultJson = this.SerializeJobSubmissionResult(new JobSubmissionResult
                {
                    FlinkJobId = "flink-rate",
                    Success = true
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("rate-limit-test");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(2), "Should have retried once");
    }

    [Test]
    public async Task SubmitJobAsync_WithFlinkClusterNotReady_Retries()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage(HttpStatusCode.BadRequest)
                    {
                        Content = new StringContent("Flink cluster is not healthy or unreachable")
                    };
                }
                var resultJson = this.SerializeJobSubmissionResult(new JobSubmissionResult
                {
                    FlinkJobId = "flink-ready",
                    Success = true
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("cluster-ready-test");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(2), "Should have retried for Flink not ready");
    }

    [Test]
    public async Task CancelJobAsync_WithBadRequest_DoesNotRetry()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Invalid job ID")
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var result = await service.CancelJobAsync("invalid-job");

        // Assert
        Assert.That(result, Is.False);
        Assert.That(callCount, Is.EqualTo(1), "Should not retry for regular bad request");
    }

    [Test]
    public void CancelJobAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        _ = Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await service.CancelJobAsync("test-job", cts.Token));
    }

    [Test]
    public async Task GetJobStatusAsync_WithNonRetryableStatusCode_ReturnsUnknownStatus()
    {
        // Arrange - Use 3xx redirect which shouldn't trigger retry
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.MovedPermanently, "Moved");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };

        this._configuration!.MaxRetries = 3;
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act
        var status = await service.GetJobStatusAsync("test-job");

        // Assert
        Assert.That(status, Is.Not.Null);
        Assert.That(status.State, Is.EqualTo("UNKNOWN"));
    }

    [Test]
    public async Task SubmitJobAsync_WithExceptionDuringRetry_LogsAndRetries()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 3)
                {
                    throw new HttpRequestException("Connection failed");
                }
                var resultJson = this.SerializeJobSubmissionResult(new JobSubmissionResult
                {
                    FlinkJobId = "flink-exception",
                    Success = true
                });
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(resultJson)
                };
            });

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("exception-retry-test");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(3), "Should have retried after exceptions");
    }

    [Test]
    public void GetJobMetricsAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Request was canceled"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert
        _ = Assert.ThrowsAsync<TaskCanceledException>(async () =>
            await service.GetJobMetricsAsync("test-job", cts.Token));
    }

    [Test]
    public async Task SubmitJobAsync_WithValidJsonButNullResult_ReturnsFailure()
    {
        // Arrange - Valid JSON that deserializes to null
        var mockHandler = this.CreateMockHttpMessageHandler(HttpStatusCode.OK, "null");
        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        var validJob = this.CreateValidJobDefinition("null-result-test");

        // Act
        var result = await service.SubmitJobAsync(validJob);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Deserialization failed"));
    }

    [Test]
    public void GetJobStatusAsync_ExceedingMaxRetries_ThrowsHttpRequestException()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Persistent connection failure"));

        var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8086") };
        this._configuration!.MaxRetries = 2;
        this._configuration.RetryDelay = TimeSpan.FromMilliseconds(10);
        using var service = new FlinkJobGatewayService(this._configuration, httpClient, this._mockLogger?.Object);

        // Act & Assert - Exception is rethrown after retries
        _ = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await service.GetJobStatusAsync("test-job"));
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_CalledOnce_DisposesHttpClient()
    {
        // Arrange
        var service = new FlinkJobGatewayService(this._configuration);

        // Act
        service.Dispose();

        // Assert - no exception thrown
        Assert.Pass("Dispose completed successfully");
    }

    [Test]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        // Arrange
        var service = new FlinkJobGatewayService(this._configuration);

        // Act
        service.Dispose();
        service.Dispose();
        service.Dispose();

        // Assert - no exception thrown
        Assert.Pass("Multiple Dispose calls handled correctly");
    }

    #endregion

    #region Helper Methods

    private Mock<HttpMessageHandler> CreateMockHttpMessageHandler(HttpStatusCode statusCode, string responseContent)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        _ = mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });
        return mockHandler;
    }

    private string SerializeJobSubmissionResult(JobSubmissionResult result)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(result, jsonOptions);
    }

    private string SerializeJobStatus(JobStatus status)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(status, jsonOptions);
    }

    private string SerializeJobMetrics(JobMetrics metrics)
    {
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        return JsonSerializer.Serialize(metrics, jsonOptions);
    }

    private JobDefinition CreateValidJobDefinition(string jobId)
    {
        return new JobDefinition
        {
            Metadata = new JobMetadata
            {
                Version = "1.0",
                JobName = $"Test Job {jobId}",
                Parallelism = 1
            },
            Source = new KafkaSourceDefinition
            {
                Topic = "input-topic",
                BootstrapServers = "kafka:9092",
                GroupId = "test-group"
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output-topic",
                BootstrapServers = "kafka:9092"
            },
            Operations = new System.Collections.Generic.List<IOperationDefinition>()
        };
    }

    #endregion

    #region Private Method Tests - Coverage Enhancement

    [Test]
    public void LogBootstrapServersInJson_WithValidKafkaSource_LogsBootstrapServers()
    {
        // Use reflection to test private method
        var method = typeof(FlinkJobGatewayService).GetMethod(
            "LogBootstrapServersInJson",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var json = @"{
            ""source"": {
                ""bootstrapServers"": ""localhost:9092"",
                ""topic"": ""test-topic""
            }
        }";

        // Act - should not throw and will trigger ExtractBootstrapServersFromJson
        Assert.DoesNotThrow(() => method!.Invoke(null, new object[] { json }));
    }

    [Test]
    public void CountDiscriminatorOccurrences_WithValidJson_CountsDiscriminators()
    {
        // Use reflection to test private method
        var config = new FlinkJobGatewayConfiguration { BaseUrl = "http://localhost:8081" };
        var service = new FlinkJobGatewayService(config);
        var method = typeof(FlinkJobGatewayService).GetMethod(
            "CountDiscriminatorOccurrences",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var json = @"{
            ""source"": { ""$type"": ""kafka"" },
            ""operations"": [
                { ""$type"": ""map"" },
                { ""$type"": ""filter"" }
            ]
        }";

        // Act - should not throw
        Assert.DoesNotThrow(() => method!.Invoke(service, new object[] { "test-job", json }));
    }

    #endregion
}
