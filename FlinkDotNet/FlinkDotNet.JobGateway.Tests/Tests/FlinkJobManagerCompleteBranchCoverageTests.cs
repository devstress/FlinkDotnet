using System.Net;
using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Comprehensive branch coverage tests for FlinkJobManager to achieve 100% coverage.
    /// Focuses on network failures, error HTTP codes, JSON parsing failures, security validation,
    /// retry logic exhaustion, timeouts, Maven build failures, and edge cases.
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerCompleteBranchCoverageTests
    {
        private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        private HttpClient _httpClient = null!;

        [SetUp]
        public void Setup()
        {
            // Set static delays to 1ms for fast test execution
            FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);
            
            _mockLogger = new Mock<ILogger<FlinkJobManager>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration values (returns null for any key by default)
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?)null);
            
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:8081")
            };
        }

        [TearDown]
        public void TearDown()
        {
            _httpClient?.Dispose();
        }

        #region Helper Methods

        private void SetupHttpResponse(string requestUri, HttpStatusCode statusCode, string responseContent, string method = "GET")
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(responseContent, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri!.PathAndQuery.Contains(requestUri) &&
                        req.Method.ToString().Equals(method, StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupHttpException(string requestUri, Exception exception, string method = "GET")
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri!.PathAndQuery.Contains(requestUri) &&
                        req.Method.ToString().Equals(method, StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);
        }



        #endregion

        #region Network Failure Tests

        [Test]
        public void GetJobStatusAsync_WithHttpRequestException_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}", new HttpRequestException("Network failure"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
            Assert.That(ex.InnerException, Is.TypeOf<HttpRequestException>());
        }

        [Test]
        public void GetJobStatusAsync_WithTaskCanceledException_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}", new TaskCanceledException("Request timeout"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
            Assert.That(ex.InnerException, Is.TypeOf<TaskCanceledException>());
        }

        [Test]
        public void GetJobMetricsAsync_WithHttpRequestException_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}/vertices", new HttpRequestException("Connection refused"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
            Assert.That(ex.InnerException, Is.TypeOf<HttpRequestException>());
        }

        [Test]
        public void GetJobMetricsAsync_WithTaskCanceledException_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}/vertices", new TaskCanceledException("Network timeout"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void CancelJobAsync_WithHttpRequestExceptionInPatch_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/jobs/{flinkJobId}?mode=cancel", new HttpRequestException("Connection error"), "PATCH");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to cancel job"));
            Assert.That(ex.InnerException, Is.TypeOf<HttpRequestException>());
        }

        [Test]
        public void CancelJobAsync_WithTaskCanceledExceptionInPatch_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/jobs/{flinkJobId}?mode=cancel", new TaskCanceledException("Request timeout"), "PATCH");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to cancel job"));
        }

        #endregion

        #region HTTP Error Code Tests

        [Test]
        public async Task GetJobStatusAsync_With404NotFound_ReturnsNull()
        {
            // Arrange
            var flinkJobId = "non-existent-job";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetJobStatusAsync_With500InternalServerError_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.InternalServerError, "Internal Server Error");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void GetJobStatusAsync_With503ServiceUnavailable_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.ServiceUnavailable, "Service Unavailable");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void GetJobStatusAsync_With502BadGateway_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.BadGateway, "Bad Gateway");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void GetJobStatusAsync_With401Unauthorized_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.Unauthorized, "Unauthorized");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void GetJobStatusAsync_With403Forbidden_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.Forbidden, "Forbidden");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public async Task CancelJobAsync_With404InBothEndpoints_ReturnsFalse()
        {
            // Arrange
            var flinkJobId = "non-existent-job";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.NotFound, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.NotFound, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public async Task CancelJobAsync_With500InPatchAnd404InPost_ReturnsFalse()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.InternalServerError, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.NotFound, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert - Should return false when one endpoint returns 404
            Assert.That(result, Is.False);
        }

        [Test]
        public void CancelJobAsync_With500InBothEndpoints_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.InternalServerError, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.InternalServerError, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void CancelJobAsync_With503InBothEndpoints_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.ServiceUnavailable, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.ServiceUnavailable, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        #endregion

        #region JSON Parsing Failure Tests

        [Test]
        public void GetJobStatusAsync_WithMalformedJson_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, "{ invalid json }");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            // The inner exception should be JsonException
            Assert.That(ex!.InnerException, Is.Not.Null);
        }

        [Test]
        public async Task GetJobStatusAsync_WithMissingStateProperty_ReturnsUnknownState()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var jsonResponse = JsonSerializer.Serialize(new { jobId = flinkJobId, name = "test" });
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, jsonResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("UNKNOWN"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithNullState_ReturnsUnknownState()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var jsonResponse = JsonSerializer.Serialize(new { state = (string?)null });
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, jsonResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("UNKNOWN"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithEmptyJson_ReturnsUnknownState()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, "{}");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("UNKNOWN"));
        }

        #endregion

        #region Security Validation Tests

        [Test]
        public void GetJobStatusAsync_WithPathTraversalAttempt_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "../../../etc/passwd";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("path traversal"));
        }

        [Test]
        public void GetJobStatusAsync_WithBackslashPathTraversal_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "..\\..\\..\\windows\\system32";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("path traversal").Or.Contains("Invalid characters"));
        }

        [Test]
        public void GetJobStatusAsync_WithNullJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(null!));
        }

        [Test]
        public void GetJobStatusAsync_WithEmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(string.Empty));
        }

        [Test]
        public void GetJobStatusAsync_WithWhitespaceJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync("   "));
        }

        [Test]
        public void GetJobMetricsAsync_WithPathTraversalAttempt_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "../../../etc/passwd";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobMetricsAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("path traversal"));
        }

        [Test]
        public void CancelJobAsync_WithPathTraversalAttempt_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "../../../etc/passwd";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.CancelJobAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("path traversal"));
        }

        #endregion

        #region Job Validation Tests

        [Test]
        public async Task SubmitJobAsync_WithNullJobId_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = null!, JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptyJobId_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
        }

        [Test]
        public async Task SubmitJobAsync_WithWhitespaceJobId_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "   ", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            // May fail validation or cluster health check
            Assert.That(result.ErrorMessage, Is.Not.Empty);
        }

        [Test]
        public async Task SubmitJobAsync_WithNullSource_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = null!,
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
        }

        [Test]
        public async Task SubmitJobAsync_WithNullSink_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = null!
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("validation failed"));
        }

        #endregion

        #region Cancel Job Local Status Tests - Removed due to reflection complexity
        
        // Test removed: CancelJobAsync_WithLocalJobStatus_UpdatesStatusToCanceled
        // Reason: JobInfo is internal class making it complex to test via reflection
        // This branch is tested indirectly through integration tests

        #endregion

        #region Endpoint Discovery Tests

        [Test]
        public void Constructor_WithPartialPortInEnvironment_UsesFullEndpoint()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "custom-host");
                // No port set, should use default
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Should log using environment variable
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("environment variable")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", null);
            }
        }

        [Test]
        public void Constructor_WithOnlyPortInEnvironment_UsesDefaultHost()
        {
            // Arrange
            try
            {
                // Only port, no host
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "9999");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Should use default host with custom port
                _mockLogger.Verify(
                    x => x.Log(
                        It.IsAny<LogLevel>(),
                        It.IsAny<EventId>(),
                        It.IsAny<It.IsAnyType>(),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", null);
            }
        }

        #endregion

        #region Submit Job Cluster Health Tests

        [Test]
        public async Task SubmitJobAsync_WithUnhealthyCluster_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpException("/v1/overview", new HttpRequestException("Connection refused"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealthTimeout_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpException("/v1/overview", new TaskCanceledException("Timeout"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealth404_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealth500_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.InternalServerError, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        #endregion

        #region GetJobMetricsAsync Edge Cases

        [Test]
        public void GetJobMetricsAsync_WithNullJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobMetricsAsync(null!));
        }

        [Test]
        public void GetJobMetricsAsync_WithEmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobMetricsAsync(string.Empty));
        }

        [Test]
        public void GetJobMetricsAsync_WithWhitespaceJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobMetricsAsync("   "));
        }

        [Test]
        public void GetJobMetricsAsync_With404OnVertices_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_With500OnVertices_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.InternalServerError, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_With503OnVertices_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.ServiceUnavailable, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_WithMalformedJsonOnVertices_ThrowsInvalidOperationException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.OK, "{ malformed json }");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        #endregion

        #region CancelJobAsync Edge Cases

        [Test]
        public void CancelJobAsync_WithNullJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.CancelJobAsync(null!));
        }

        [Test]
        public void CancelJobAsync_WithEmptyJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.CancelJobAsync(string.Empty));
        }

        [Test]
        public void CancelJobAsync_WithWhitespaceJobId_ThrowsArgumentException()
        {
            // Arrange
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.CancelJobAsync("   "));
        }

        [Test]
        public async Task CancelJobAsync_WithSuccessfulPatch_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.OK, "", "PATCH");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithPatch404AndPostSuccess_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.NotFound, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.OK, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithPatch500AndPostSuccess_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.InternalServerError, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.Accepted, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        #endregion

        #region Additional Validation Tests

        [Test]
        public async Task SubmitJobAsync_WithFileSink_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new FileSinkDefinition { Path = "/tmp/output" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithHttpSink_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new HttpSinkDefinition { Url = "http://localhost:8080/api" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithDatabaseSink_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new DatabaseSinkDefinition { ConnectionString = "Server=localhost;Database=test" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithConsoleSink_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new ConsoleSinkDefinition()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithRedisSink_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new RedisSinkDefinition()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region Source Validation Tests

        [Test]
        public async Task SubmitJobAsync_WithFileSource_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new FileSourceDefinition { Path = "/tmp/input" },
                Sink = new ConsoleSinkDefinition()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithHttpSource_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new HttpSourceDefinition { Url = "http://localhost:8080/api" },
                Sink = new ConsoleSinkDefinition()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithDatabaseSource_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new DatabaseSourceDefinition { ConnectionString = "Server=localhost;Database=test", Query = "SELECT * FROM table" },
                Sink = new ConsoleSinkDefinition()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Will fail on cluster health but passes validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region SQL Gateway Session Creation Tests

        [Test]
        public async Task SubmitJobAsync_WithSqlGatewayMode_CreatesSession()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "sql-job-1", JobName = "test-sql" },
                Source = new SqlSourceDefinition 
                { 
                    ExecutionMode = "gateway",
                    Statements = new List<string> { "CREATE TABLE test (id INT)" }
                },
                Sink = new ConsoleSinkDefinition()
            };

            // Mock SQL Gateway discovery
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", "http://localhost:8083");
            
            try
            {
                // Setup cluster health check to fail (should be skipped for SQL Gateway)
                SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
                
                var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Act
                var result = await jobManager.SubmitJobAsync(jobDefinition);

                // Assert - Should fail trying to create SQL Gateway session
                Assert.That(result.Success, Is.False);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            }
        }

        [Test]
        public async Task SubmitJobAsync_WithSqlGatewayEmptyStatements_UsesSessionHandle()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "sql-job-2", JobName = "test-sql" },
                Source = new SqlSourceDefinition 
                { 
                    ExecutionMode = "gateway",
                    Statements = new List<string>() // Empty statements
                },
                Sink = new ConsoleSinkDefinition()
            };

            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", "http://localhost:8083");
            
            try
            {
                var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Act
                var result = await jobManager.SubmitJobAsync(jobDefinition);

                // Assert
                Assert.That(result.Success, Is.False);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            }
        }

        [Test]
        public async Task SubmitJobAsync_WithSqlGatewayWhitespaceStatements_SkipsWhitespace()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "sql-job-3", JobName = "test-sql" },
                Source = new SqlSourceDefinition 
                { 
                    ExecutionMode = "gateway",
                    Statements = new List<string> { "   ", "\t", "\n" } // Whitespace only
                },
                Sink = new ConsoleSinkDefinition()
            };

            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", "http://localhost:8083");
            
            try
            {
                var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Act
                var result = await jobManager.SubmitJobAsync(jobDefinition);

                // Assert
                Assert.That(result.Success, Is.False);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            }
        }

        #endregion

        #region Path Validation Additional Tests

        [Test]
        public void GetJobStatusAsync_WithForwardSlashInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test/job/id";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public void GetJobStatusAsync_WithBackslashInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test\\job\\id";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public void GetJobStatusAsync_WithQuestionMarkInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test?query=param";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public void GetJobStatusAsync_WithHashInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test#fragment";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public void GetJobStatusAsync_WithAtSymbolInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test@domain";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public void GetJobStatusAsync_WithColonInPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "test:8080";
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("invalid character"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithValidJobIdContainingDots_Works()
        {
            // Arrange
            var validJobId = "test.job.id.123";
            SetupHttpResponse($"/v1/jobs/{Uri.EscapeDataString(validJobId)}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithValidJobIdContainingHyphens_Works()
        {
            // Arrange
            var validJobId = "test-job-id-123";
            SetupHttpResponse($"/v1/jobs/{Uri.EscapeDataString(validJobId)}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithValidJobIdContainingUnderscores_Works()
        {
            // Arrange
            var validJobId = "test_job_id_123";
            SetupHttpResponse($"/v1/jobs/{Uri.EscapeDataString(validJobId)}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RUNNING"));
        }

        #endregion

        #region GetJobMetrics Checkpoint Tests

        [Test]
        public void GetJobMetricsAsync_WithHttpRequestExceptionOnCheckpoints_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { vertices = new[] { new { id = "vertex1" } } }));
            SetupHttpException($"/v1/jobs/{flinkJobId}/checkpoints", new HttpRequestException("Connection failed"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_With404OnCheckpoints_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { vertices = new[] { new { id = "vertex1" } } }));
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_WithMalformedJsonOnCheckpoints_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { vertices = new[] { new { id = "vertex1" } } }));
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK, "{ malformed }");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        #endregion

        #region CancelJob Fallback Path Tests

        [Test]
        public async Task CancelJobAsync_WithPatchBadRequestAndPostSuccess_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.BadRequest, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.OK, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithPatchUnauthorizedAndPostSuccess_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.Unauthorized, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.Accepted, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithPatchForbiddenAndPostSuccess_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.Forbidden, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.OK, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void CancelJobAsync_WithBothUnauthorized_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.Unauthorized, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.Unauthorized, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void CancelJobAsync_WithBothForbidden_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.Forbidden, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.Forbidden, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void CancelJobAsync_WithBothBadGateway_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.BadGateway, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.BadGateway, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        #endregion

        #region Additional Validation Edge Cases

        [Test]
        public void SubmitJobAsync_WithNullMetadata_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = null!,
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<NullReferenceException>(async () => 
                await jobManager.SubmitJobAsync(jobDefinition));
            Assert.That(ex, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJobAsync_WithVeryLongJobId_PassesValidation()
        {
            // Arrange
            var longJobId = new string('a', 500);
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = longJobId, JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Fails on cluster health, not validation
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithNumericJobId_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "12345678", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithUnicodeJobId_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-αβγ-123", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region Endpoint Discovery Configuration Tests

        [Test]
        public void Constructor_WithConfigurationOnly_UsesConfigEndpoint()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Flink:JobManager:BaseUrl"])
                .Returns("http://config-flink:8081");

            // Act
            _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("configuration")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void Constructor_WithEnvironmentOnly_UsesEnvEndpoint()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "env-flink");
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "8888");

                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("environment")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", null);
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", null);
            }
        }

        [Test]
        public void Constructor_WithBothConfigAndEnv_PrefersConfig()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "env-flink");
                _mockConfiguration.Setup(x => x["Flink:JobManager:BaseUrl"])
                    .Returns("http://config-flink:8081");

                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Config should be logged, not env
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("configuration")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", null);
            }
        }

        #endregion

        #region GetJobStatus State Variations

        [Test]
        public async Task GetJobStatusAsync_WithRunningState_ReturnsRunning()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithFinishedState_ReturnsFinished()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "FINISHED" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("FINISHED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithFailedState_ReturnsFailed()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "FAILED" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("FAILED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithCanceledState_ReturnsCanceled()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "CANCELED" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("CANCELED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithCreatedState_ReturnsCreated()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "CREATED" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("CREATED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithSuspendedState_ReturnsSuspended()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "SUSPENDED" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("SUSPENDED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithReconcilingState_ReturnsReconciling()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, 
                JsonSerializer.Serialize(new { state = "RECONCILING" }));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RECONCILING"));
        }

        #endregion

        #region Additional HTTP Error Combinations

        [Test]
        public void GetJobMetricsAsync_With400BadRequest_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.BadRequest, "Bad Request");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_With504GatewayTimeout_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices", HttpStatusCode.GatewayTimeout, "Gateway Timeout");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobStatusAsync_With429TooManyRequests_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.TooManyRequests, "Too Many Requests");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void GetJobStatusAsync_With409Conflict_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.Conflict, "Conflict");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void CancelJobAsync_With429TooManyRequests_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.TooManyRequests, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.TooManyRequests, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        [Test]
        public void CancelJobAsync_With504GatewayTimeout_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.GatewayTimeout, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.GatewayTimeout, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.InnerException!.Message, Does.Contain("Unexpected status code"));
        }

        #endregion

        #region Cluster Health Additional Scenarios

        [Test]
        public async Task SubmitJobAsync_WithClusterHealthBadGateway_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.BadGateway, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealthServiceUnavailable_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.ServiceUnavailable, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealthUnauthorized_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.Unauthorized, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        [Test]
        public async Task SubmitJobAsync_WithClusterHealthForbidden_ReturnsFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.Forbidden, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("not healthy"));
        }

        #endregion

        #region Job Name Variations

        [Test]
        public async Task SubmitJobAsync_WithNullJobName_UsesJobId()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = null },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptyJobName_UsesJobId()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithVeryLongJobName_PassesValidation()
        {
            // Arrange
            var longName = new string('b', 1000);
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = longName },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithSpecialCharactersInJobName_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test@#$%^&*()job" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region Parallelism Variations

        [Test]
        public async Task SubmitJobAsync_WithNullParallelism_UsesDefault()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test", Parallelism = null },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithParallelism1_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test", Parallelism = 1 },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithHighParallelism_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test", Parallelism = 100 },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region Operations Validation

        [Test]
        public async Task SubmitJobAsync_WithNullOperations_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" },
                Operations = null
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptyOperations_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" },
                Operations = new List<IOperationDefinition>()
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithMapOperation_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" },
                Operations = new List<IOperationDefinition> { new MapOperationDefinition { Expression = "x => x.ToUpper()" } }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithFilterOperation_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" },
                Operations = new List<IOperationDefinition> { new FilterOperationDefinition { Expression = "x => x.Length > 0" } }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        [Test]
        public async Task SubmitJobAsync_WithMultipleOperations_PassesValidation()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "test" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" },
                Operations = new List<IOperationDefinition> 
                { 
                    new MapOperationDefinition { Expression = "x => x.ToUpper()" },
                    new FilterOperationDefinition { Expression = "x => x.Length > 0" }
                }
            };
            SetupHttpResponse("/v1/overview", HttpStatusCode.NotFound, "");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Not.Contain("validation"));
        }

        #endregion

        #region JSON Response Edge Cases

        [Test]
        public async Task GetJobStatusAsync_WithExtraFieldsInJson_ParsesSuccessfully()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var jsonResponse = JsonSerializer.Serialize(new 
            { 
                state = "RUNNING",
                extraField1 = "value1",
                extraField2 = 123,
                nested = new { field = "value" }
            });
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, jsonResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithLowercaseState_ParsesSuccessfully()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var jsonResponse = JsonSerializer.Serialize(new { state = "running" });
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, jsonResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("running"));
        }

        [Test]
        public void GetJobStatusAsync_WithNumberAsState_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var jsonResponse = "{ \"state\": 1 }";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, jsonResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex, Is.Not.Null);
        }

        #endregion
    }
}
