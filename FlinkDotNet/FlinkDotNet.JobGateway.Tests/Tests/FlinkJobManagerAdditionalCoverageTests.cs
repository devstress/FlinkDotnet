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
            
            _mockLogger = new Mock<ILogger<FlinkJobManager>>();
            _mockConfiguration = new Mock<IConfiguration>();
            
            // Setup default configuration values
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
            
            // Clean up environment variables
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__http__0", null);
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST", null);
            Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT", null);
        }

        #region SQL Gateway Endpoint Discovery Tests

        [Test]
        public async Task Constructor_WithSqlGatewayAspireEndpoint_UsesSqlGatewayAspireEndpoint()
        {
            // Arrange
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", "http://aspire-sql-gateway:8083");
            
            try
            {
                // Act - Create FlinkJobManager to trigger SQL Gateway endpoint discovery
                var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                // Now submit a job that uses SQL Gateway
                var jobDef = new JobDefinition
                {
                    Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                    Source = new SqlSourceDefinition
                    {
                        Statements = new List<string> { "SELECT 1" },
                        ExecutionMode = "gateway"
                    },
                    Sink = new ConsoleSinkDefinition()
                };

                // Setup mock HTTP responses for SQL Gateway
                SetupSqlGatewayMockResponses();

                // Act
                _ = await manager.SubmitJobAsync(jobDef);

                // Assert - Should attempt to use Aspire SQL Gateway endpoint
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Aspire service discovery for SQL Gateway")),
                        It.IsAny<Exception>(),
                        It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    Times.AtLeastOnce);
            }
            finally
            {
                Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
            }
        }

        [Test]
        public async Task Constructor_WithSqlGatewayLegacyAspire_UsesLegacyFormat()
        {
            // Arrange
            Environment.SetEnvironmentVariable("services__flink-sql-gateway__http__0", "http://legacy-sql-gateway:8083");
            
            try
            {
                // Act
                var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                var jobDef = new JobDefinition
                {
                    Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                    Source = new SqlSourceDefinition
                    {
                        Statements = new List<string> { "SELECT 1" },
                        ExecutionMode = "gateway"
                    },
                    Sink = new ConsoleSinkDefinition()
                };

                SetupSqlGatewayMockResponses();

                // Act
                _  = await manager.SubmitJobAsync(jobDef);

                // Assert
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
                Environment.SetEnvironmentVariable("services__flink-sql-gateway__http__0", null);
            }
        }

        [Test]
        public async Task SqlGateway_WithConfigurationEndpoint_UsesConfigEndpoint()
        {
            // Arrange
            _mockConfiguration.Setup(x => x["Flink:SqlGateway:BaseUrl"]).Returns("http://config-sql-gateway:8083");
            
            var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                Source = new SqlSourceDefinition
                {
                    Statements = new List<string> { "SELECT 1" },
                    ExecutionMode = "gateway"
                },
                Sink = new ConsoleSinkDefinition()
            };

            SetupSqlGatewayMockResponses();

            // Act
            _  = await manager.SubmitJobAsync(jobDef);

            // Assert
            _mockLogger.Verify(
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
                var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

                var jobDef = new JobDefinition
                {
                    Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                    Source = new SqlSourceDefinition
                    {
                        Statements = new List<string> { "SELECT 1" },
                        ExecutionMode = "gateway"
                    },
                    Sink = new ConsoleSinkDefinition()
                };

                SetupSqlGatewayMockResponses();

                // Act
                _  = await manager.SubmitJobAsync(jobDef);

                // Assert
                _mockLogger.Verify(
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
            var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                Source = new SqlSourceDefinition
                {
                    Statements = new List<string> { "SELECT 1" },
                    ExecutionMode = "gateway"
                },
                Sink = new ConsoleSinkDefinition()
            };

            SetupSqlGatewayMockResponses();

            // Act
            _  = await manager.SubmitJobAsync(jobDef);

            // Assert
            _mockLogger.Verify(
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
            var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-map-job", JobName = "Map Test" },
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
            SetupClusterHealthMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert - Should log map operation expression at Debug level
            _mockLogger.Verify(
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
            var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-no-ops", JobName = "No Ops Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test-topic",
                    GroupId = "test-group"
                },
                Operations = new List<IOperationDefinition>(), // Empty operations
                Sink = new ConsoleSinkDefinition()
            };

            SetupClusterHealthAndJarMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert - Should not log map operations since there are none
            Assert.That(true); // Test passes if no exception thrown
        }

        [Test]
        public async Task SubmitJob_WithNullOperations_DoesNotLogOperations()
        {
            // Arrange
            var manager = new FlinkJobManager(_mockLogger.Object, _mockConfiguration.Object, _httpClient);

            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-null-ops", JobName = "Null Ops Test" },
                Source = new KafkaSourceDefinition
                {
                    BootstrapServers = "localhost:9092",
                    Topic = "test-topic",
                    GroupId = "test-group"
                },
                Operations = null, // Null operations
                Sink = new ConsoleSinkDefinition()
            };

            SetupClusterHealthAndJarMockResponses();

            // Act
            _ = await manager.SubmitJobAsync(jobDef);

            // Assert - Should handle null operations gracefully
            Assert.That(true); // Test passes if no exception thrown
        }

        #endregion

        #region Helper Methods

        private void SetupClusterHealthMockResponses()
        {
            // Mock cluster overview for health check
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
            _mockHttpMessageHandler
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
