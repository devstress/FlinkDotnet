#nullable enable
using System.Net;
using System.Text;
using System.Text.Json;
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.JobGateway.Tests.Tests;

/// <summary>
/// Coverage improvement tests to reach 95% code coverage for FlinkJobManager.
/// Focuses on previously uncovered methods:
/// - TryExtractVertexMetricsFromJobDetails
/// - UpdateLastKnownJars
/// - FindMatchingJar
/// - ProcessCheckpointTimestamps
/// - ProcessSourceMetrics/ProcessSinkMetrics edge cases
/// </summary>
[TestFixture]
public class FlinkJobManagerCoverageImprovementTests
{
    private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
    private Mock<IConfiguration> _mockConfiguration = null!;
    private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void SetUp()
    {
        // Set static delays and timeouts to 1ms for fast test execution
        FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JarRegistrationTimeout = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JobRecoveryTimeout = TimeSpan.FromMilliseconds(1);

        this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
        this._mockConfiguration = new Mock<IConfiguration>();
        this._mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        
        // Setup default handler for unmocked HTTP requests to fail fast
        _ = this._mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Handler did not return a response message."));
        
        this._httpClient = new HttpClient(this._mockHttpMessageHandler.Object)
        {
            BaseAddress = new Uri("http://localhost:8081/"),
            Timeout = TimeSpan.FromSeconds(1)
        };

        // Setup IConfiguration to return null by default
        _ = this._mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);
    }

    [TearDown]
    public void TearDown()
    {
        // Restore default delays and timeouts
        FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromSeconds(1);
        FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromSeconds(1);
        FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromSeconds(1);
        FlinkJobManager.JarRegistrationTimeout = TimeSpan.FromSeconds(30);
        FlinkJobManager.JobRecoveryTimeout = TimeSpan.FromSeconds(30);

        this._httpClient?.Dispose();
    }

    #region TryExtractVertexMetricsFromJobDetails Coverage Tests

    [Test]
    public async Task GetJobMetricsAsync_WithVertexMetrics_ExtractsReadRecords()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-123";

        // Mock job details with vertex that has read-records metric
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-1"",
                    ""name"": ""Source"",
                    ""metrics"": {
                        ""read-records"": 1000
                    }
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);

        // Mock empty checkpoints response
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        
        // Mock vertex metrics list endpoint (returns empty list - vertex inline metrics are sufficient)
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-1/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecordsIn, Is.EqualTo(1000));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithVertexMetrics_ExtractsWriteRecords()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-456";

        // Mock job details with vertex that has write-records metric
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-2"",
                    ""name"": ""Sink"",
                    ""metrics"": {
                        ""write-records"": 2000
                    }
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-2/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecordsOut, Is.EqualTo(2000));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithVertexMetrics_ExtractsParallelism()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-789";

        // Mock job details with vertex that has parallelism
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-3"",
                    ""name"": ""Operator"",
                    ""parallelism"": 4,
                    ""metrics"": {}
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-3/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Parallelism, Is.EqualTo(4));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithVertexMetrics_CombinesMultipleMetrics()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-combined";

        // Mock job details with multiple vertices with different metrics
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-1"",
                    ""name"": ""Source"",
                    ""parallelism"": 2,
                    ""metrics"": {
                        ""read-records"": 500
                    }
                },
                {
                    ""id"": ""vertex-2"",
                    ""name"": ""Sink"",
                    ""parallelism"": 4,
                    ""metrics"": {
                        ""write-records"": 400,
                        ""read-records"": 300
                    }
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-1/metrics", @"[]");
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-2/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecordsIn, Is.EqualTo(800)); // 500 + 300
        Assert.That(result.RecordsOut, Is.EqualTo(400));
        Assert.That(result.Parallelism, Is.EqualTo(4)); // Max of 2 and 4
    }

    [Test]
    public async Task GetJobMetricsAsync_WithVertexNoMetricsProperty_ReturnsDefaults()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-no-metrics";

        // Mock job details with vertex that has no metrics property
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-1"",
                    ""name"": ""Operator""
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        
        // Mock vertex metrics list endpoint (returns empty list)
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-1/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecordsIn, Is.EqualTo(0));
        Assert.That(result.RecordsOut, Is.EqualTo(0));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithInvalidMetricValues_HandlesGracefully()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-invalid-metrics";

        // Mock job details with non-numeric metric values
        string jobDetailsJson = @"{
            ""jid"": """ + jobId + @""",
            ""name"": ""Test Job"",
            ""state"": ""RUNNING"",
            ""vertices"": [
                {
                    ""id"": ""vertex-1"",
                    ""name"": ""Source"",
                    ""metrics"": {
                        ""read-records"": ""invalid"",
                        ""write-records"": null
                    }
                }
            ]
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}", jobDetailsJson);
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/vertex-1/metrics", @"[]");

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert - should not throw and return defaults
        Assert.That(result, Is.Not.Null);
        Assert.That(result.RecordsIn, Is.EqualTo(0));
        Assert.That(result.RecordsOut, Is.EqualTo(0));
    }

    #endregion

    #region ProcessCheckpointTimestamps Coverage Tests

    [Test]
    public async Task GetJobMetricsAsync_WithCheckpointEndTime_ExtractsTimestamp()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-checkpoint";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints with end_time
        long timestamp = 1699900000000; // Nov 13, 2023
        string checkpointsJson = @"{
            ""latest"": {
                ""completed"": {
                    ""end_time"": " + timestamp + @"
                }
            }
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Not.Null);
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        Assert.That(result.LastCheckpoint, Is.EqualTo(expectedTime));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithCheckpointTriggerTimestamp_ExtractsTimestamp()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-trigger";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints with trigger_timestamp only (no end_time)
        long timestamp = 1699900000000;
        string checkpointsJson = @"{
            ""latest"": {
                ""completed"": {
                    ""trigger_timestamp"": " + timestamp + @"
                }
            }
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Not.Null);
        var expectedTime = DateTimeOffset.FromUnixTimeMilliseconds(timestamp).UtcDateTime;
        Assert.That(result.LastCheckpoint, Is.EqualTo(expectedTime));
    }

    [Test]
    public async Task GetJobMetricsAsync_WithNullCheckpointCompleted_ReturnsNullLastCheckpoint()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-null-checkpoint";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints with null completed
        string checkpointsJson = @"{
            ""latest"": {
                ""completed"": null
            }
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_WithMissingLatestProperty_ReturnsNullLastCheckpoint()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-no-latest";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints without latest property
        string checkpointsJson = @"{}";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_WithNullTimestampValue_ReturnsNullLastCheckpoint()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-null-timestamp";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints with null timestamp values
        string checkpointsJson = @"{
            ""latest"": {
                ""completed"": {
                    ""end_time"": null,
                    ""trigger_timestamp"": null
                }
            }
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_WithInvalidTimestampType_ReturnsNullLastCheckpoint()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-invalid-timestamp";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[]}");

        // Mock checkpoints with string timestamp (invalid type)
        string checkpointsJson = @"{
            ""latest"": {
                ""completed"": {
                    ""end_time"": ""not-a-number""
                }
            }
        }";

        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", checkpointsJson);

        // Act
        var result = await manager.GetJobMetricsAsync(jobId);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.LastCheckpoint, Is.Null);
    }

    #endregion

    #region ProcessSourceMetrics and ProcessSinkMetrics Edge Cases

    [Test]
    public async Task GetJobMetricsAsync_WithSourceMetricsContainingNonNumericValue_HandlesGracefully()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-source-edge";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[{""id"":""v1""}]}");
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");

        // Mock metrics list endpoint
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/v1/metrics", @"[
            {""id"":""0.Source__KafkaSource.numRecordsOut""}
        ]");

        // Mock metrics values with non-numeric value
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/v1/metrics?get=0.Source__KafkaSource.numRecordsOut", @"[
            {""id"":""0.Source__KafkaSource.numRecordsOut"",""value"":""invalid""}
        ]");

        // Act & Assert - should not throw
        var result = await manager.GetJobMetricsAsync(jobId);
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task GetJobMetricsAsync_WithSinkMetricsContainingNonNumericValue_HandlesGracefully()
    {
        // Arrange
        var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        string jobId = "test-job-sink-edge";

        SetupHttpResponse($"/v1/jobs/{jobId}", @"{""jid"":""" + jobId + @""",""state"":""RUNNING"",""vertices"":[{""id"":""v2""}]}");
        SetupHttpResponse($"/v1/jobs/{jobId}/checkpoints", @"{""latest"":{""completed"":null}}");

        // Mock metrics list endpoint
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/v2/metrics", @"[
            {""id"":""0.Sink__KafkaSink.numRecordsIn""}
        ]");

        // Mock metrics values with non-numeric value
        SetupHttpResponse($"/v1/jobs/{jobId}/vertices/v2/metrics?get=0.Sink__KafkaSink.numRecordsIn", @"[
            {""id"":""0.Sink__KafkaSink.numRecordsIn"",""value"":""NaN""}
        ]");

        // Act & Assert - should not throw
        var result = await manager.GetJobMetricsAsync(jobId);
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region Helper Methods

    private void SetupHttpResponse(string url, string responseContent, HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
        };

        _ = this._mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.RequestUri != null &&
                    req.RequestUri.PathAndQuery.StartsWith(url, StringComparison.OrdinalIgnoreCase)),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);
    }

    #endregion
}
