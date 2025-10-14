using System.Net;
using System.Text.Json;
using FlinkDotNet.ClusterManager.Actors;
using FlinkDotNet.ClusterManager.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.ClusterManager.Tests.Actors;

/// <summary>
/// Comprehensive tests for FlinkClusterActor covering all methods and scenarios.
/// </summary>
[TestFixture]
public class FlinkClusterActorTests
{
    private Mock<ILogger<FlinkClusterActor>> _mockLogger = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private ClusterConfiguration _configuration = null!;
    private const string TestClusterId = "test-cluster-1";

    [SetUp]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<FlinkClusterActor>>();
        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_mockHttpMessageHandler.Object);
        _configuration = new ClusterConfiguration
        {
            Name = "test-cluster",
            TaskSlots = 4,
            TaskManagers = 2,
            FlinkVersion = "1.18.0",
            RetryBaseDelayMs = 0 // No delays in unit tests
        };
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidParameters_CreatesInstance()
    {
        // Act
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Assert
        Assert.That(actor.ClusterId, Is.EqualTo(TestClusterId));
    }

    [Test]
    public void Constructor_WithNullClusterId_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkClusterActor(null!, _configuration, _httpClient, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkClusterActor(TestClusterId, null!, _httpClient, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WithNullHttpClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkClusterActor(TestClusterId, _configuration, null!, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkClusterActor(TestClusterId, _configuration, _httpClient, null!));
    }

    #endregion

    #region GetStatusAsync Tests

    [Test]
    public async Task GetStatusAsync_WithSuccessfulResponse_ReturnsHealthyStatus()
    {
        // Arrange
        var flinkOverview = new
        {
            SlotsTotal = 8,
            SlotsAvailable = 5,
            JobsRunning = 3,
            FlinkVersion = "1.18.0"
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(flinkOverview));
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var status = await actor.GetStatusAsync();

        // Assert
        Assert.That(status.ClusterId, Is.EqualTo(TestClusterId));
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Healthy));
        Assert.That(status.TotalSlots, Is.EqualTo(8));
        Assert.That(status.AvailableSlots, Is.EqualTo(5));
        Assert.That(status.RunningJobs, Is.EqualTo(3));
        Assert.That(status.Version, Is.EqualTo("1.18.0"));
    }

    [Test]
    public async Task GetStatusAsync_WithFailedResponse_ReturnsCriticalStatus()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "");
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var status = await actor.GetStatusAsync();

        // Assert
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Critical));
    }

    [Test]
    public async Task GetStatusAsync_WithException_ReturnsOfflineStatus()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var status = await actor.GetStatusAsync();

        // Assert
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Offline));
    }

    [Test]
    public async Task GetStatusAsync_WithCancellation_HandlesGracefully()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new TaskCanceledException("Operation canceled"));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var status = await actor.GetStatusAsync(cts.Token);

        // Assert - Actor handles cancellation gracefully and returns offline status
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Offline));
    }

    [Test]
    public async Task GetStatusAsync_UpdatesLastHealthCheck()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            SlotsTotal = 8,
            SlotsAvailable = 5,
            JobsRunning = 3,
            FlinkVersion = "1.18.0"
        }));
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var before = DateTime.UtcNow;

        // Act
        var status = await actor.GetStatusAsync();

        // Assert
        var after = DateTime.UtcNow;
        Assert.That(status.LastHealthCheck, Is.GreaterThanOrEqualTo(before));
        Assert.That(status.LastHealthCheck, Is.LessThanOrEqualTo(after));
    }

    #endregion

    #region SubmitJobAsync Tests

    [Test]
    public async Task SubmitJobAsync_WithHealthyClusterAndSufficientSlots_SucceedsSubmission()
    {
        // Arrange
        var sequence = new Queue<HttpResponseMessage>();

        // First call for status check - success
        sequence.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                SlotsTotal = 8,
                SlotsAvailable = 5,
                JobsRunning = 3,
                FlinkVersion = "1.18.0"
            }))
        });

        // Second call for job submission - success
        sequence.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { JobId = "flink-job-123" }))
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => sequence.Dequeue());

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition
        {
            JobId = "job-1",
            JobName = "Test Job",
            JobGraph = "graph-data",
            Parallelism = 4
        };

        // Act
        var result = await actor.SubmitJobAsync(job);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.JobId, Is.EqualTo("job-1"));
        Assert.That(result.ClusterId, Is.EqualTo(TestClusterId));
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-job-123"));
        Assert.That(result.PlacementInfo.ClusterId, Is.EqualTo(TestClusterId));
        Assert.That(result.PlacementInfo.AssignedSlots, Is.EqualTo(4));
    }

    [Test]
    public async Task SubmitJobAsync_WithUnhealthyCluster_ReturnsFailureResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "");
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition
        {
            JobId = "job-1",
            Parallelism = 4
        };

        // Act
        var result = await actor.SubmitJobAsync(job);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
    }

    [Test]
    public async Task SubmitJobAsync_WithInsufficientSlots_ReturnsFailureResult()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            SlotsTotal = 8,
            SlotsAvailable = 2,
            JobsRunning = 6,
            FlinkVersion = "1.18.0"
        }));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition
        {
            JobId = "job-1",
            Parallelism = 4
        };

        // Act
        var result = await actor.SubmitJobAsync(job);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Insufficient slots"));
    }

    [Test]
    public async Task SubmitJobAsync_WithFlinkApiFailure_ReturnsFailureResult()
    {
        // Arrange
        // First call for status check - success
        var statusResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                SlotsTotal = 8,
                SlotsAvailable = 5,
                JobsRunning = 3,
                FlinkVersion = "1.18.0"
            }))
        };

        // Multiple calls for job submission retries - all failures
        _mockHttpMessageHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(statusResponse)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid job graph") })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid job graph") })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid job graph") })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.BadRequest) { Content = new StringContent("Invalid job graph") });

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition
        {
            JobId = "job-1",
            Parallelism = 4
        };

        // Act
        var result = await actor.SubmitJobAsync(job);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Flink API error"));
    }

    [Test]
    public async Task SubmitJobAsync_WithException_ReturnsFailureResult()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network failure"));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition
        {
            JobId = "job-1",
            Parallelism = 4
        };

        // Act
        var result = await actor.SubmitJobAsync(job);

        // Assert
        Assert.That(result.Success, Is.False);
        // The error could be from status check failure, so check for offline or network error
        Assert.That(result.ErrorMessage, Does.Contain("not healthy").Or.Contain("Network failure"));
    }

    [Test]
    public async Task SubmitJobAsync_LogsInformation()
    {
        // Arrange
        var sequence = new Queue<HttpResponseMessage>();

        // First call for status check
        sequence.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new
            {
                SlotsTotal = 8,
                SlotsAvailable = 5,
                JobsRunning = 3,
                FlinkVersion = "1.18.0"
            }))
        });

        // Second call for job submission
        sequence.Enqueue(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(new { JobId = "flink-job-123" }))
        });

        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => sequence.Dequeue());

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        var job = new FlinkJobDefinition { JobId = "job-1", Parallelism = 2 };

        // Act
        await actor.SubmitJobAsync(job);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Submitting job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region ScaleAsync Tests

    [Test]
    public async Task ScaleAsync_WithValidParallelism_ReturnsTrue()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var result = await actor.ScaleAsync(8);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task ScaleAsync_LogsInformation()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        await actor.ScaleAsync(8);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Scaling cluster")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ScaleAsync_WithCancellation_ReturnsFalse()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var result = await actor.ScaleAsync(8, cts.Token);

        // Assert - The implementation catches exceptions and returns false
        Assert.That(result, Is.False);
    }

    #endregion

    #region RestartAsync Tests

    [Test]
    public void RestartAsync_WithSuccessfulRestart_Completes()
    {
        // Arrange
        _configuration = new ClusterConfiguration
        {
            Properties = new Dictionary<string, string> { ["restart.delay.seconds"] = "0" }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            SlotsTotal = 8,
            SlotsAvailable = 5,
            JobsRunning = 3,
            FlinkVersion = "1.18.0"
        }));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await actor.RestartAsync());
    }

    [Test]
    public async Task RestartAsync_LogsInformation()
    {
        // Arrange
        _configuration = new ClusterConfiguration
        {
            Properties = new Dictionary<string, string> { ["restart.delay.seconds"] = "0" }
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            SlotsTotal = 8,
            SlotsAvailable = 5,
            JobsRunning = 3,
            FlinkVersion = "1.18.0"
        }));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        await actor.RestartAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Restarting cluster")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public void RestartAsync_WithCancellation_ThrowsInvalidOperationException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await actor.RestartAsync(cts.Token));
        Assert.That(ex!.Message, Does.Contain("Cluster restart failed"));
    }

    #endregion

    #region ShutdownAsync Tests

    [Test]
    public void ShutdownAsync_Completes()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await actor.ShutdownAsync());
    }

    [Test]
    public async Task ShutdownAsync_LogsInformation()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        await actor.ShutdownAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Shutting down cluster")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public void ShutdownAsync_WithoutHealthMonitoring_CompletesSuccessfully()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert - Should complete without throwing
        Assert.DoesNotThrowAsync(async () => await actor.ShutdownAsync());

        // Verify shutdown was logged
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("shut down cluster")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region StartHealthMonitoringAsync Tests

    [Test]
    public void StartHealthMonitoringAsync_Starts()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await actor.StartHealthMonitoringAsync());
    }

    [Test]
    public async Task StartHealthMonitoringAsync_WithAlreadyRunningMonitoring_LogsWarning()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);
        await actor.StartHealthMonitoringAsync();

        // Act
        await actor.StartHealthMonitoringAsync();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Health monitoring already running")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task StartHealthMonitoringAsync_LogsStartMessage()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(new
        {
            SlotsTotal = 8,
            SlotsAvailable = 5,
            JobsRunning = 3,
            FlinkVersion = "1.18.0"
        }));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        await actor.StartHealthMonitoringAsync();
        await Task.Delay(1); // Minimal delay to allow async log to complete

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting health monitoring")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    #endregion

    #region GetMetricsAsync Tests

    [Test]
    public async Task GetMetricsAsync_WithSuccessfulResponse_ReturnsMetrics()
    {
        // Arrange
        var flinkMetrics = new
        {
            CpuUtilization = 0.75,
            MemoryUtilization = 0.65,
            ProcessedRecords = 1000000L,
            Throughput = 5000.5,
            BackpressureRatio = 0.15
        };

        SetupHttpResponse(HttpStatusCode.OK, JsonSerializer.Serialize(flinkMetrics));
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var metrics = await actor.GetMetricsAsync();

        // Assert
        Assert.That(metrics.ClusterId, Is.EqualTo(TestClusterId));
        Assert.That(metrics.CpuUtilization, Is.EqualTo(0.75));
        Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.65));
        Assert.That(metrics.ProcessedRecords, Is.EqualTo(1000000));
        Assert.That(metrics.Throughput, Is.EqualTo(5000.5));
        Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.15));
    }

    [Test]
    public async Task GetMetricsAsync_WithFailedResponse_ReturnsEmptyMetrics()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.InternalServerError, "");
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var metrics = await actor.GetMetricsAsync();

        // Assert
        Assert.That(metrics.ClusterId, Is.EqualTo(TestClusterId));
        Assert.That(metrics.CpuUtilization, Is.EqualTo(0.0));
        Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.0));
    }

    [Test]
    public async Task GetMetricsAsync_WithException_ReturnsEmptyMetrics()
    {
        // Arrange
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act
        var metrics = await actor.GetMetricsAsync();

        // Assert
        Assert.That(metrics.ClusterId, Is.EqualTo(TestClusterId));
    }

    #endregion

    #region Dispose Tests

    [Test]
    public void Dispose_DisposesResources()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => actor.Dispose());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var actor = new FlinkClusterActor(TestClusterId, _configuration, _httpClient, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            actor.Dispose();
            actor.Dispose();
        });
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content)
            });
    }

    #endregion
}
