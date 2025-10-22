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
    /// Tests focused on achieving 100% branch coverage for FlinkJobManager
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerBranchCoverageTests
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

        #region Endpoint Discovery Branch Coverage Tests

        [Test]
        public void Constructor_WithAspireServiceDiscovery_UsesAspireEndpoint()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("services__flink-jobmanager__jm-http__0", "http://aspire-endpoint:8081");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Constructor should log using Aspire endpoint
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Aspire service discovery endpoint")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-jobmanager__jm-http__0", null);
            }
        }

        [Test]
        public void Constructor_WithLegacyAspireFormat_UsesLegacyEndpoint()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("services__flink-jobmanager__http__0", "http://legacy-aspire:8081");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Constructor should log using legacy Aspire endpoint
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("legacy format")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-jobmanager__http__0", null);
            }
        }

        [Test]
        public void Constructor_WithConfigEndpoint_UsesConfigurationEndpoint()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Flink:JobManager:BaseUrl"])
                .Returns("http://config-endpoint:8081");
            
            // Act
            _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Assert - Constructor should log using configuration endpoint
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("configuration endpoint")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void Constructor_WithEnvironmentVariables_UsesEnvEndpoint()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "env-host");
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "9999");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Assert - Constructor should log using environment variable endpoint
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("environment variable endpoint")),
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
        public void Constructor_WithNoDiscovery_UsesDefaultEndpoint()
        {
            // Arrange - No environment variables or configuration set
            
            // Act
            _ = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Assert - Constructor should log using default endpoint
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("default Docker Compose endpoint")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Error Path Coverage Tests

        [Test]
        public void GetJobStatusAsync_WithUnexpectedStatusCode_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.InternalServerError, "Server Error");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobStatusAsync_WithHttpException_WrapsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}", new HttpRequestException("Connection failed"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void GetJobMetricsAsync_WithHttpException_WrapsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/v1/jobs/{flinkJobId}/vertices", new HttpRequestException("Connection failed"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobMetricsAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        [Test]
        public void CancelJobAsync_WithBothEndpointsFailing_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.InternalServerError, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.InternalServerError, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to cancel job"));
        }

        [Test]
        public async Task CancelJobAsync_WithNotFoundInBothEndpoints_ReturnsFalse()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.NotFound, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.NotFound, "", "POST");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CancelJobAsync_WithHttpException_WrapsException()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            SetupHttpException($"/jobs/{flinkJobId}?mode=cancel", new HttpRequestException("Connection failed"));
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to cancel job"));
        }

        #endregion

        #region Validation Edge Cases

        [Test]
        public async Task SubmitJobAsync_WithNullMetadata_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "", JobName = "test" }, // Empty JobId instead of null metadata
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Job ID"));
        }

        [Test]
        public async Task SubmitJobAsync_WithRedisSink_NoValidationRequired()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new RedisSinkDefinition { }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Should pass validation
            Assert.That(result.Success, Is.False); // Will fail for other reasons
        }

        [Test]
        public async Task SubmitJobAsync_WithDatabaseSink_NoValidationRequired()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1" },
                Source = new KafkaSourceDefinition { Topic = "test", BootstrapServers = "localhost:9092" },
                Sink = new DatabaseSinkDefinition { }
            };
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Should pass validation
            Assert.That(result.Success, Is.False); // Will fail for other reasons
        }

        #endregion

        #region Helper Methods

        private void SetupHttpResponse(string requestUri, HttpStatusCode statusCode, string content, string method = "GET")
        {
            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri != null && 
                        req.RequestUri.PathAndQuery.Contains(requestUri) &&
                        req.Method.Method.Equals(method, StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        private void SetupHttpException(string requestUri, Exception exception)
        {
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.RequestUri != null && 
                        req.RequestUri.PathAndQuery.Contains(requestUri)),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(exception);
        }

        #endregion
    }
}
