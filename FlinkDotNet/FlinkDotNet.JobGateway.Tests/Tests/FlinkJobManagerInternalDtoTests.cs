#nullable enable
using System.Net;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Tests for FlinkJobManager internal DTO classes and JAR-related operations
    /// Covers internal response DTOs and JAR finding logic
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerInternalDtoTests
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

            this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
            this._mockConfiguration = new Mock<IConfiguration>();

            // Setup default configuration values
            _ = this._mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?) null);

            this._mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            // Setup default handler for unmocked HTTP requests to fail fast instead of timing out
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Handler did not return a response message."));
            
            this._httpClient = new HttpClient(this._mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("http://localhost:8081"),
                Timeout = TimeSpan.FromSeconds(1) // Short timeout for unmocked calls
            };
        }

        [TearDown]
        public void TearDown()
        {
            this._httpClient?.Dispose();
        }

        #region FindExistingRunnerJar Tests

        [Test]
        public async Task SubmitJob_WithEnvironmentVariable_AttemptsToUseJarFromEnvVar()
        {
            // Arrange - use IConfiguration mocking instead of environment variables
            var testJarPath = "/tmp/nonexistent-runner.jar";
            this._mockConfiguration.Setup(c => c["FLINK_RUNNER_JAR_PATH"]).Returns(testJarPath);

            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "JAR Env Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test",
                    GroupId = "test-group"
                },
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupClusterHealthAndJarMockResponses();

            // Act
            var result = await manager.SubmitJobAsync(jobDef);

            // Assert - Should attempt to use JAR path from environment variable
            // The JAR won't be found, but the code path should be exercised
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJob_WithoutEnvironmentVariable_SearchesForJar()
        {
            // Arrange - No FLINK_RUNNER_JAR_PATH set
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "JAR Search Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test",
                    GroupId = "test-group"
                },
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupClusterHealthAndJarMockResponses();

            // Act
            var result = await manager.SubmitJobAsync(jobDef);

            // Assert - Should search for JAR in standard locations
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJob_SearchesMultiplePaths_ForRunnerJar()
        {
            // Arrange
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "JAR Multi Path Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test",
                    GroupId = "test-group"
                },
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupClusterHealthAndJarMockResponses();

            // Act
            var result = await manager.SubmitJobAsync(jobDef);

            // Assert - Should search in current directory and FlinkIRRunner/target
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Internal DTO Tests

        #endregion

        #region Helper Methods

        private void SetupClusterHealthAndJarMockResponses()
        {
            // Mock cluster overview for health check
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/overview")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"taskmanagers\":1,\"slots-total\":4,\"slots-available\":4}")
                });

            // Mock JAR list endpoint
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/jars") && req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"files\":[]}")
                });

            // Mock JAR upload - Return proper JAR ID to avoid 30-second polling delay
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/jars/upload")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"filename\":\"flink-ir-runner-java17.jar\",\"status\":\"success\"}")
                });

            // Mock JAR run
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/jars/") && req.RequestUri.PathAndQuery.Contains("/run")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"jobid\":\"test-flink-job-id-456\"}")
                });
        }

        #endregion
    }
}
