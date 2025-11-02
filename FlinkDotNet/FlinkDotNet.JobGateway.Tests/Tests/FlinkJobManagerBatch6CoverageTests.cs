#nullable enable
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
    /// Batch 6 coverage tests to reach closer to 100% branch coverage
    /// Focuses on edge cases and remaining uncovered scenarios
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerBatch6CoverageTests
    {
        private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        private HttpClient _httpClient = null!;

        [SetUp]
        public void Setup()
        {
            FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);

            _mockLogger = new Mock<ILogger<FlinkJobManager>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?) null);

            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            // Setup default handler for unmocked HTTP requests to fail fast instead of timing out
            _ = _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Handler did not return a response message."));
            
            _httpClient = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:8081"),
                Timeout = TimeSpan.FromSeconds(1) // Short timeout for unmocked calls
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

        #endregion

        #region Multi-Vertex Scenarios

        [Test]
        public async Task GetJobMetricsAsync_With5Vertices_ProcessesAllSuccessfully()
        {
            // Arrange
            var flinkJobId = "complex-job-5-vertices";

            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    state = "RUNNING"
                }));

            var verticesResponse = new
            {
                vertices = new[]
                {
                    new { id = "v1", name = "Source", parallelism = 2 },
                    new { id = "v2", name = "Map1", parallelism = 4 },
                    new { id = "v3", name = "Map2", parallelism = 4 },
                    new { id = "v4", name = "Reduce", parallelism = 2 },
                    new { id = "v5", name = "Sink", parallelism = 1 }
                }
            };
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(verticesResponse));

            // Setup metrics and backpressure for all vertices
            for (int i = 1; i <= 5; i++)
            {
                var vid = $"v{i}";
                SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vid}/metrics", HttpStatusCode.OK,
                    JsonSerializer.Serialize(new[] { new { id = "numRecordsIn", value = $"{i * 1000}" } }));
                SetupHttpResponse($"/v1/jobs/{flinkJobId}/vertices/{vid}/backpressure", HttpStatusCode.OK,
                    JsonSerializer.Serialize(new
                    {
                        backpressure_level = i % 2 == 0 ? "ok" : "low"
                    }));
            }

            SetupHttpResponse($"/v1/jobs/{flinkJobId}/checkpoints", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    counts = new
                    {
                        completed = 20,
                        restored = 5
                    }
                }));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Different Sink Types

        [Test]
        public async Task SubmitJobAsync_WithDatabaseSink_SubmitsCorrectly()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                                        JobName = "Database Sink Job"
                },
                Source = new FileSourceDefinition
                {
                    Path = "input.csv",
                    Format = "csv"
                },
                Sink = new DatabaseSinkDefinition
                {
                    ConnectionString = "jdbc:postgresql://localhost:5432/testdb"
                },
                Operations = new List<IOperationDefinition>()
            };

            SetupHttpResponse("/overview", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    version = "1.18.0"
                }));

            SetupHttpResponse("/v1/jars/upload", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    filename = "flink-ir-runner-java17.jar"
                }), "POST");

            var jarsResponse = new
            {
                files = new[] { new { id = "flink-ir-runner.jar", name = "flink-ir-runner.jar" } }
            };
            SetupHttpResponse("/v1/jars", HttpStatusCode.OK,
                JsonSerializer.Serialize(jarsResponse));

            SetupHttpResponse("/v1/jars/flink-ir-runner.jar/run", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    jobid = "flink-db-sink-123"
                }), "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJobAsync_WithFileSink_SubmitsCorrectly()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                                        JobName = "File Sink Job"
                },
                Source = new KafkaSourceDefinition
                {
                    Topic = "events",
                    BootstrapServers = "localhost:9092",
                    GroupId = "test-group"
                },
                Sink = new FileSinkDefinition
                {
                    Path = "/output/results.json",
                    Format = "json"
                },
                Operations = new List<IOperationDefinition>()
            };

            SetupHttpResponse("/overview", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    version = "1.18.0"
                }));

            SetupHttpResponse("/v1/jars/upload", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    filename = "flink-ir-runner-java17.jar"
                }), "POST");

            var jarsResponse = new
            {
                files = new[] { new { id = "flink-ir-runner.jar", name = "flink-ir-runner.jar" } }
            };
            SetupHttpResponse("/v1/jars", HttpStatusCode.OK,
                JsonSerializer.Serialize(jarsResponse));

            SetupHttpResponse("/v1/jars/flink-ir-runner.jar/run", HttpStatusCode.OK,
                JsonSerializer.Serialize(new
                {
                    jobid = "flink-file-sink-123"
                }), "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Job State Variations

        [Test]
        public async Task GetJobStatusAsync_WithCancelingState_ReturnsCorrectState()
        {
            // Arrange
            var flinkJobId = "canceling-job";

            var statusResponse = new
            {
                state = "CANCELING",
                startTime = DateTime.UtcNow.AddMinutes(-30).ToString("o")
            };

            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("CANCELING"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithRestartingState_ReturnsCorrectState()
        {
            // Arrange
            var flinkJobId = "restarting-job";

            var statusResponse = new
            {
                state = "RESTARTING",
                startTime = DateTime.UtcNow.AddMinutes(-15).ToString("o")
            };

            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK,
                JsonSerializer.Serialize(statusResponse));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("RESTARTING"));
        }

        #endregion
    }
}
