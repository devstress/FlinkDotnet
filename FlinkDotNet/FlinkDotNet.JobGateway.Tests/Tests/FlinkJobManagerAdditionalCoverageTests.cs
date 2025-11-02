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
    /// Additional branch coverage tests for FlinkJobManager uncovered methods
    /// Focuses on SQL Gateway, JAR operations, and error paths
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerAdditionalCoverageTests
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

            // Clean up environment variables
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__http__0", null);
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST", null);
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT", null);
        }

        #region SQL Gateway Endpoint Discovery Tests

        [Test]
        public async Task SqlGateway_WithConfigurationEndpoint_UsesConfigEndpoint()
        {
            // Arrange
            _ = this._mockConfiguration.Setup(x => x["Flink:SqlGateway:BaseUrl"]).Returns("http://config-sql-gateway:8083");

            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "SQL Test" },
                Source = new SqlSourceDefinition
                {
                    Statements = new List<string> { "SELECT 1" },
                    ExecutionMode = "gateway"
                },
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupSqlGatewayMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert
            this._mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using configuration for SQL Gateway")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task SqlGateway_WithEnvironmentVariables_UsesEnvVars()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST", "env-sql-gateway");
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT", "9999");

            try
            {
                var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

                var jobDef = new JobDefinition
                {
                    Metadata = new JobMetadata { JobName = "SQL Test" },
                    Source = new SqlSourceDefinition
                    {
                        Statements = new List<string> { "SELECT 1" },
                        ExecutionMode = "gateway"
                    },
                    Sink = new ConsoleSinkDefinition()
                };

                this.SetupSqlGatewayMockResponses();

                // Act
                _ = await manager.SubmitJobAsync(jobDef);

                // Assert
                this._mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using environment variable for SQL Gateway")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST", null);
                Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT", null);
            }
        }

        [Test]
        public async Task SqlGateway_WithNoConfiguration_UsesDefaultEndpoint()
        {
            // Arrange
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "SQL Test" },
                Source = new SqlSourceDefinition
                {
                    Statements = new List<string> { "SELECT 1" },
                    ExecutionMode = "gateway"
                },
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupSqlGatewayMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert
            this._mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Using default Docker network for SQL Gateway")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region LogOperations and Job Definition Tests

        [Test]
        public async Task SubmitJob_WithMapOperation_LogsMapExpression()
        {
            // Arrange
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "Map Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test-topic",
                    GroupId = "test-group"
                },
                Operations = new List<IOperationDefinition>
                {
                    new MapOperationDefinition
                    {
                        Expression = "x => x.ToUpper()"
                    }
                },
                Sink = new ConsoleSinkDefinition()
            };

            // Setup mock for cluster health check
            this.SetupClusterHealthMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert - Should log map operation expression at Debug level
            this._mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Map Operation") && v.ToString()!.Contains("x => x.ToUpper()")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task SubmitJob_WithEmptyOperations_DoesNotLogOperations()
        {
            // Arrange
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "No Ops Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test-topic",
                    GroupId = "test-group"
                },
                Operations = new List<IOperationDefinition>(), // Empty operations
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupClusterHealthAndJarMockResponses();

            // Act
            var result = await manager.SubmitJobAsync(jobDef);

            // Assert - Should not log map operations since there are none
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJob_WithNullOperations_DoesNotLogOperations()
        {
            // Arrange
            var manager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobName = "Null Ops Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test-topic",
                    GroupId = "test-group"
                },
                Operations = null, // Null operations
                Sink = new ConsoleSinkDefinition()
            };

            this.SetupClusterHealthAndJarMockResponses();

            // Act
            var result = await manager.SubmitJobAsync(jobDef);

            // Assert - Should handle null operations gracefully
            Assert.That(result, Is.Not.Null);
        }

        #endregion

        #region Helper Methods

        private void SetupClusterHealthMockResponses()
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

            // Mock JAR upload
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/jars/upload")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"filename\":\"/tmp/test-jar.jar\",\"status\":\"success\"}")
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
                    Content = new StringContent("{\"jobid\":\"test-flink-job-id-123\"}")
                });
        }

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

            // Mock JAR list endpoint - Return uploaded JAR to avoid 30-second polling delay
            // This MUST come after /jars/upload to avoid matching that endpoint
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => 
                        req.Method == HttpMethod.Get && 
                        req.RequestUri!.PathAndQuery.Contains("/jars") &&
                        !req.RequestUri.PathAndQuery.Contains("/upload")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"files\":[{\"id\":\"flink-ir-runner-java17.jar\",\"name\":\"flink-ir-runner-java17.jar\",\"uploaded\":1234567890}]}")
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
                    Content = new StringContent("{\"jobid\":\"test-flink-job-id-123\"}")
                });
        }

        private void SetupSqlGatewayMockResponses()
        {
            // Mock SQL Gateway info endpoint
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/v1/info")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"productName\":\"Apache Flink SQL Gateway\"}")
                });

            // Mock SQL Gateway session creation
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/v1/sessions") && req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"sessionHandle\":\"test-session-handle-123\"}")
                });

            // Mock SQL Gateway statement execution
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/statements") && req.Method == HttpMethod.Post),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"operationHandle\":\"test-operation-handle-456\"}")
                });

            // Mock operation status check
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.PathAndQuery.Contains("/operations/") && req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"status\":\"FINISHED\",\"jobID\":\"test-flink-job-id-789\"}")
                });
        }

        #endregion
    }
}
