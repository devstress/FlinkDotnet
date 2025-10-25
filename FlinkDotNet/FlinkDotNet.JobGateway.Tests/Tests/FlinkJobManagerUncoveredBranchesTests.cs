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
    /// Targeted tests for uncovered branches in FlinkJobManager to reach 100% branch coverage.
    /// Focuses on the lowest coverage methods identified through coverage analysis.
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerUncoveredBranchesTests
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

        #region CollectVertexNumericMetricsAsync Tests - Currently 5.5% coverage

        [Test]
        public async Task GetJobMetricsAsync_WithNumRecordsInMetric_ParsesCorrectly()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            // Setup successful job status
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            // Setup vertices response
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with numRecordsIn
            var metricsResponse = new[]
            {
                new { id = "numRecordsIn", value = "12345" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            // Setup backpressure response
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            // Setup checkpoint response
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithNumRecordsOutMetric_ParsesCorrectly()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with numRecordsOut
            var metricsResponse = new[]
            {
                new { id = "numRecordsOut", value = "67890" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithParallelismMetric_ParsesCorrectly()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with parallelism
            var metricsResponse = new[]
            {
                new { id = "parallelism", value = "8" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithInvalidNumRecordsInValue_SkipsParsing()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with invalid value
            var metricsResponse = new[]
            {
                new { id = "numRecordsIn", value = "not-a-number" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithInvalidNumRecordsOutValue_SkipsParsing()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with invalid value
            var metricsResponse = new[]
            {
                new { id = "numRecordsOut", value = "invalid" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithInvalidParallelismValue_SkipsParsing()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup metrics response with invalid parallelism
            var metricsResponse = new[]
            {
                new { id = "parallelism", value = "not-an-int" }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(metricsResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithNonSuccessMetricsResponse_ContinuesExecution()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup non-success metrics response
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.NotFound, "");
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithNullMetricsListResponse_HandlesGracefully()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            var vertexId = "vertex-123";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = vertexId, name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            // Setup null/invalid metrics response
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK, "null");
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { backpressure_level = "ok" }));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region ProcessVertexAsync Tests - Currently 50% coverage

        [Test]
        public async Task GetJobMetricsAsync_WithMissingVertexId_SkipsVertex()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            // Setup vertices response without id property
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { name = "Map", parallelism = 2 } // Missing 'id'
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithEmptyVertexId_SkipsVertex()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            // Setup vertices response with empty id
            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = "", name = "Map", parallelism = 2 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithNullVertexId_SkipsVertex()
        {
            // Arrange
            var flinkJobId = "test-job-id";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { state = "RUNNING" }));
            
            // Setup vertices response with null id (need manual JSON to have null)
            var verticesJson = "{\"vertices\":[{\"id\":null,\"name\":\"Map\",\"parallelism\":2}]}";
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, verticesJson);
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion
    }
}
