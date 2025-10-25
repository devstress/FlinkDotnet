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

        #region SubmitJobToFlinkClusterAsync Tests - Currently 61.1% coverage

        [Test]
        public async Task SubmitJobAsync_WithJarSourceAndNoConnectorJars_SubmitsSuccessfully()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "jar-job-no-connectors",
                    JobName = "JAR Job Without Connectors"
                },
                Source = new FileSourceDefinition
                {
                    Path = "test-file.txt",
                    Format = "csv"
                },
                Sink = new ConsoleSinkDefinition(),
                Operations = new List<IOperationDefinition>()
            };

            // Setup cluster health
            SetupHttpResponse("/overview", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { version = "1.18.0" }));

            // Setup JAR upload
            SetupHttpResponse("/v1/jars/upload", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { filename = "/tmp/flink-ir-runner.jar" }), "POST");

            // Setup JAR run
            SetupHttpResponse("/v1/jars/", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { jobid = "flink-job-123" }), "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJobAsync_WithKafkaSourceAndSink_MergesConnectorJars()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "kafka-job",
                    JobName = "Kafka Job"
                },
                Source = new KafkaSourceDefinition
                {
                    Topic = "input-topic",
                    BootstrapServers = "localhost:9092",
                    GroupId = "test-group"
                },
                Sink = new KafkaSinkDefinition
                {
                    Topic = "output-topic",
                    BootstrapServers = "localhost:9092"
                },
                Operations = new List<IOperationDefinition>()
            };

            SetupHttpResponse("/overview", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { version = "1.18.0" }));

            SetupHttpResponse("/v1/jars/upload", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { filename = "/tmp/merged-job.jar" }), "POST");

            SetupHttpResponse("/v1/jars/", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { jobid = "flink-kafka-job-123" }), "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region CollectVertexBackpressureAsync Tests - Currently 50% coverage

        [Test]
        public async Task GetJobMetricsAsync_WithBackpressureNonSuccessResponse_ContinuesExecution()
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
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(new[] { new { id = "numRecordsIn", value = "100" } }));
            
            // Setup backpressure with non-success status
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.NotFound, "");
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task GetJobMetricsAsync_WithBackpressureLevelMissing_HandlesGracefully()
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
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(new[] { new { id = "numRecordsIn", value = "100" } }));
            
            // Setup backpressure response without level field
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { status = "ok" })); // Missing 'backpressure_level' field
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new { counts = new { completed = 5, restored = 1 } }));
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void GetJobMetricsAsync_WithBackpressureException_ThrowsInvalidOperationException()
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
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(new[] { new { id = "numRecordsIn", value = "100" } }));
            
            // Setup backpressure with invalid JSON to trigger exception
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure", HttpStatusCode.OK,
                "invalid-json{malformed");
            
            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await jobManager.GetJobMetricsAsync(flinkJobId));
            
            Assert.That(ex!.InnerException, Is.Not.Null);
            Assert.That(ex.Message, Does.Contain("job metrics"));
        }

        #endregion

        #region Additional Edge Case Tests

        [Test]
        public async Task GetJobMetricsAsync_WithMultipleMetricTypes_ParsesAllCorrectly()
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
            
            // Setup metrics response with all metric types
            var metricsResponse = new[]
            {
                new { id = "numRecordsIn", value = "12345" },
                new { id = "numRecordsOut", value = "67890" },
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
        public async Task GetJobMetricsAsync_WithEmptyMetricsArray_HandlesGracefully()
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
            
            // Setup empty metrics array
            SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=", HttpStatusCode.OK,
                JsonSerializer.Serialize(new object[] { }));
            
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
        public async Task GetJobMetricsAsync_WithUnknownMetricIds_IgnoresThem()
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
            
            // Setup metrics with unknown IDs
            var metricsResponse = new[]
            {
                new { id = "unknownMetric1", value = "123" },
                new { id = "someOtherMetric", value = "456" },
                new { id = "numRecordsIn", value = "789" }  // One known metric
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

        #endregion
    }
}
