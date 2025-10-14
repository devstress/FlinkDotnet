using System.Net;
using System.Text.Json;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkJobGatewayServiceTests
{
    private static HttpClient CreateHttpClient(HttpMessageHandler handler)
    {
        return new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:8080")
        };
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithDefaultParameters_CreatesService()
    {
        using var service = new FlinkJobGatewayService();
        
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithConfiguration_UsesProvidedConfiguration()
    {
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://custom:8080",
            HttpTimeout = TimeSpan.FromSeconds(60),
            MaxRetries = 5
        };

        using var service = new FlinkJobGatewayService(config);
        
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithHttpClient_UsesProvidedClient()
    {
        var httpClient = new HttpClient();
        
        using var service = new FlinkJobGatewayService(null, httpClient);
        
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithLogger_UsesProvidedLogger()
    {
        var mockLogger = new Mock<ILogger>();
        
        using var service = new FlinkJobGatewayService(null, null, mockLogger.Object);
        
        Assert.That(service, Is.Not.Null);
    }

    #endregion

    #region SubmitJobAsync - Validation Tests

    [Test]
    public async Task SubmitJobAsync_WithInvalidJob_ReturnsValidationFailure()
    {
        using var service = new FlinkJobGatewayService();
        
        var invalidJob = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "", Version = "" },
            Source = null!,
            Sink = null
        };

        var result = await service.SubmitJobAsync(invalidJob);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
    }

    [Test]
    public async Task SubmitJobAsync_WithMissingSource_ReturnsValidationFailure()
    {
        using var service = new FlinkJobGatewayService();
        
        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = null!,
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("source is required"));
    }

    #endregion

    #region SubmitJobAsync - Success Tests

    [Test]
    public async Task SubmitJobAsync_WithSuccessResponse_ReturnsSuccess()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var responseJson = JsonSerializer.Serialize(new JobSubmissionResult
        {
            JobId = "job-123",
            FlinkJobId = "flink-456",
            Success = true,
            SubmittedAt = DateTime.UtcNow
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.True);
        Assert.That(result.JobId, Is.EqualTo("job-123"));
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-456"));
    }

    [Test]
    public async Task SubmitJobAsync_WithEmptyResponseBody_ReturnsFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("")
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("empty response body"));
    }

    #endregion

    #region SubmitJobAsync - HTTP Failure Tests

    [Test]
    public async Task SubmitJobAsync_WithHttpError_ReturnsFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Server error")
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("InternalServerError").Or.Contains("500"));
    }

    [Test]
    public async Task SubmitJobAsync_WithBadRequest_ReturnsFailure()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("Bad request")
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("BadRequest").Or.Contains("400"));
    }

    #endregion

    #region GetJobStatusAsync Tests

    [Test]
    public async Task GetJobStatusAsync_WithSuccessResponse_ReturnsStatus()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var jobStatus = new JobStatus
        {
            JobId = "job-123",
            FlinkJobId = "flink-123",
            State = "RUNNING"
        };
        var responseJson = JsonSerializer.Serialize(jobStatus, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.GetJobStatusAsync("flink-123");

        Assert.That(result.FlinkJobId, Is.EqualTo("flink-123"));
        Assert.That(result.State, Is.EqualTo("RUNNING"));
    }

    [Test]
    public async Task GetJobStatusAsync_WithHttpError_ReturnsUnknownState()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound,
                Content = new StringContent("Not found")
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var result = await service.GetJobStatusAsync("flink-123");

        Assert.That(result.FlinkJobId, Is.EqualTo("flink-123"));
        Assert.That(result.State, Is.EqualTo("UNKNOWN"));
        Assert.That(result.ErrorMessage, Does.Contain("NotFound").Or.Contains("404"));
    }

    #endregion

    #region GetJobMetricsAsync Tests

    [Test]
    public async Task GetJobMetricsAsync_WithSuccessResponse_ReturnsMetrics()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        var responseJson = JsonSerializer.Serialize(new JobMetrics());

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.GetJobMetricsAsync("flink-123");

        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_WithHttpError_ReturnsEmptyMetrics()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError,
                Content = new StringContent("Error")
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var result = await service.GetJobMetricsAsync("flink-123");

        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region CancelJobAsync Tests

    [Test]
    public async Task CancelJobAsync_WithSuccessResponse_ReturnsTrue()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.CancelJobAsync("flink-123");

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task CancelJobAsync_WithHttpError_ReturnsFalse()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var result = await service.CancelJobAsync("flink-123");

        Assert.That(result, Is.False);
    }

    #endregion

    #region HealthCheckAsync Tests

    [Test]
    public async Task HealthCheckAsync_WithSuccessResponse_ReturnsTrue()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.HealthCheckAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task HealthCheckAsync_WithHttpError_ReturnsFalse()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.ServiceUnavailable
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.HealthCheckAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task HealthCheckAsync_WithException_ReturnsFalse()
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient);

        var result = await service.HealthCheckAsync();

        Assert.That(result, Is.False);
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_WhenCalled_DisposesResources()
    {
        var service = new FlinkJobGatewayService();
        
        Assert.DoesNotThrow(() => service.Dispose());
    }

    [Test]
    public void Dispose_WhenCalledMultipleTimes_DoesNotThrow()
    {
        var service = new FlinkJobGatewayService();
        
        service.Dispose();
        Assert.DoesNotThrow(() => service.Dispose());
    }

    #endregion

    #region Retry Logic Tests

    [Test]
    public async Task SubmitJobAsync_WithServerError_RetriesRequest()
    {
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.InternalServerError,
                        Content = new StringContent("Server error")
                    };
                }
                
                var responseJson = JsonSerializer.Serialize(new JobSubmissionResult
                {
                    JobId = "job-123",
                    FlinkJobId = "flink-456",
                    Success = true
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration 
        { 
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SubmitJobAsync_WithTooManyRequestsError_RetriesRequest()
    {
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.TooManyRequests,
                        Content = new StringContent("Too many requests")
                    };
                }
                
                var responseJson = JsonSerializer.Serialize(new JobSubmissionResult
                {
                    JobId = "job-123",
                    FlinkJobId = "flink-456",
                    Success = true
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration 
        { 
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(2));
    }

    [Test]
    public async Task SubmitJobAsync_WithFlinkClusterNotReady_RetriesRequest()
    {
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount < 2)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.BadRequest,
                        Content = new StringContent("Flink cluster is not healthy or unreachable")
                    };
                }
                
                var responseJson = JsonSerializer.Serialize(new JobSubmissionResult
                {
                    JobId = "job-123",
                    FlinkJobId = "flink-456",
                    Success = true
                }, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson)
                };
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration 
        { 
            MaxRetries = 2,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        var result = await service.SubmitJobAsync(job);

        Assert.That(result.Success, Is.True);
        Assert.That(callCount, Is.EqualTo(2));
    }

    #endregion

    #region CancellationToken Tests

    [Test]
    public void SubmitJobAsync_WithCancellationToken_PropagatesToken()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException());

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration { MaxRetries = 0 };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        Assert.ThrowsAsync<TaskCanceledException>(async () => 
            await service.SubmitJobAsync(job, cts.Token));
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task SubmitJobAsync_WithLogger_LogsMessages()
    {
        var mockLogger = new Mock<ILogger>();
        var mockHandler = new Mock<HttpMessageHandler>();
        var responseJson = JsonSerializer.Serialize(new JobSubmissionResult
        {
            JobId = "job-123",
            FlinkJobId = "flink-456",
            Success = true
        });

        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(responseJson)
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        using var service = new FlinkJobGatewayService(null, httpClient, mockLogger.Object);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        await service.SubmitJobAsync(job);

        // Verify logging occurred
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task SubmitJobAsync_WithValidationFailure_LogsWarning()
    {
        var mockLogger = new Mock<ILogger>();
        using var service = new FlinkJobGatewayService(null, null, mockLogger.Object);

        var invalidJob = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "", Version = "" },
            Source = null!,
            Sink = null
        };

        await service.SubmitJobAsync(invalidJob);

        // Verify warning was logged
        mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Configuration Tests

    [Test]
    public async Task SubmitJobAsync_WithCustomRetryConfiguration_RespectsSettings()
    {
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Server error")
                };
            });

        var httpClient = CreateHttpClient(mockHandler.Object);
        var config = new FlinkJobGatewayConfiguration 
        { 
            MaxRetries = 3,
            RetryDelay = TimeSpan.FromMilliseconds(10)
        };
        using var service = new FlinkJobGatewayService(config, httpClient);

        var job = new JobDefinition
        {
            Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
            Source = new KafkaSourceDefinition { Topic = "input" },
            Sink = new KafkaSinkDefinition { Topic = "output" }
        };

        try
        {
            await service.SubmitJobAsync(job);
        }
        catch
        {
            // Expected to fail after retries
        }

        // Should have attempted initial call + 3 retries = 4 total
        Assert.That(callCount, Is.EqualTo(4));
    }

    #endregion
}
