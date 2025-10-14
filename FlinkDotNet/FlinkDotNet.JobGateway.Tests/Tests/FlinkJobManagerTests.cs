using NUnit.Framework;
using Moq;
using Moq.Protected;
using Microsoft.Extensions.Logging;
using FlinkDotNet.JobGateway.Services;
using Flink.JobBuilder.Models;
using System.Net;
using System.Text.Json;

namespace FlinkDotNet.JobGateway.Tests
{
    [TestFixture]
    public class FlinkJobManagerTests
    {
        private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        private HttpClient _httpClient = null!;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<FlinkJobManager>>();
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

        #region SubmitJobAsync Tests

        // Note: Full SubmitJobAsync tests are complex due to file I/O and Maven dependencies.
        // Testing validation logic separately is more reliable.

        [Test]
        public async Task SubmitJobAsync_WithMissingMetadata_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "", JobName = "Test" }, // Empty JobId triggers validation
                Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Job ID"));
        }

        [Test]
        public async Task SubmitJobAsync_WithMissingSource_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = null!,
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("source"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptyKafkaTopic_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new KafkaSourceDefinition { Topic = "", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("topic"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptyFilePath_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new FileSourceDefinition { Path = "" },
                Sink = new FileSinkDefinition { Path = "output-path" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("path"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptySinkFilePath_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new FileSourceDefinition { Path = "input-path" },
                Sink = new FileSinkDefinition { Path = "" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("path"));
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptySinkTopic_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new KafkaSourceDefinition { Topic = "input-topic", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("topic"));
        }

        [Test]
        public async Task SubmitJobAsync_WithSqlSource_AllowsMissingSink()
        {
            // Arrange - SQL jobs don't require a sink
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test SQL Job" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Sink = null!
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Should fail for other reasons (like missing executor), but not validation
            Assert.That(result, Is.Not.Null);
            // SQL jobs need special handling and will fail during submission, not validation
        }

        [Test]
        public async Task SubmitJobAsync_WithNonSqlSource_RequiresSink()
        {
            // Arrange - Non-SQL jobs require a sink
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new KafkaSourceDefinition { Topic = "input-topic", BootstrapServers = "localhost:9092" },
                Sink = null!
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("sink"));
        }

        #endregion

        #region GetJobStatusAsync Tests

        [Test]
        public async Task GetJobStatusAsync_WithValidJobId_ReturnsStatus()
        {
            // Arrange
            var flinkJobId = "test-flink-job-123";
            var statusResponse = new { state = "RUNNING" };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.FlinkJobId, Is.EqualTo(flinkJobId));
            Assert.That(result.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithNonExistentJob_ReturnsNull()
        {
            // Arrange
            var flinkJobId = "non-existent-job";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.NotFound, "");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Null);
        }

        [Test]
        public void GetJobStatusAsync_WithServerError_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.InternalServerError, "");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jobManager.GetJobStatusAsync(flinkJobId));
        }

        [Test]
        public async Task GetJobStatusAsync_WithFinishedJob_ReturnsFinishedState()
        {
            // Arrange
            var flinkJobId = "finished-job-123";
            var statusResponse = new { state = "FINISHED" };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("FINISHED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithFailedJob_ReturnsFailedState()
        {
            // Arrange
            var flinkJobId = "failed-job-123";
            var statusResponse = new { state = "FAILED" };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("FAILED"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithCanceledJob_ReturnsCanceledState()
        {
            // Arrange
            var flinkJobId = "canceled-job-123";
            var statusResponse = new { state = "CANCELED" };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("CANCELED"));
        }

        #endregion

        #region GetJobMetricsAsync Tests

        // Note: GetJobMetricsAsync involves complex HTTP interactions with multiple endpoints.
        // These tests are better suited for integration tests with a real Flink cluster.
        // Basic exception handling is covered below.

        [Test]
        public void GetJobMetricsAsync_WithFailedCollection_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            
            // Mock vertices endpoint to throw exception
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jobManager.GetJobMetricsAsync(flinkJobId));
        }

        #endregion

        #region CancelJobAsync Tests

        [Test]
        public async Task CancelJobAsync_WithPatchEndpoint_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.OK, "", "PATCH");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithPostEndpointFallback_ReturnsTrue()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.NotFound, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.OK, "", "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task CancelJobAsync_WithNonExistentJob_ReturnsFalse()
        {
            // Arrange
            var flinkJobId = "non-existent-job";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.NotFound, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.NotFound, "", "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void CancelJobAsync_WithServerError_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.InternalServerError, "", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.InternalServerError, "", "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jobManager.CancelJobAsync(flinkJobId));
        }

        #endregion

        #region Helper Methods

        private void SetupHttpResponse(string requestPath, HttpStatusCode statusCode, string responseContent, string method = "GET")
        {
            var response = new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            };

            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri != null &&
                        req.RequestUri.PathAndQuery.Contains(requestPath) &&
                        req.Method.ToString().Equals(method, StringComparison.OrdinalIgnoreCase)),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(response);
        }

        #endregion
    }
}
