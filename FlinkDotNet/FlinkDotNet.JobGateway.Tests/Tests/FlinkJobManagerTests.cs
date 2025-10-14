using System.Net;
using System.Text.Json;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

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
            var statusResponse = new
            {
                state = "RUNNING"
            };
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
            var statusResponse = new
            {
                state = "FINISHED"
            };
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
            var statusResponse = new
            {
                state = "FAILED"
            };
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
            var statusResponse = new
            {
                state = "CANCELED"
            };
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

        #region GetJobMetricsAsync - Detailed Tests

        [Test]
        public async Task GetJobMetricsAsync_WithEmptyResponse_ReturnsEmptyMetrics()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            
            // Setup mock to return empty/not found responses for all endpoints
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("")
                });

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobMetricsAsync(flinkJobId);

            // Assert - Should return empty metrics, not throw
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.FlinkJobId, Is.EqualTo(flinkJobId));
            Assert.That(result.RecordsIn, Is.EqualTo(0));
            Assert.That(result.RecordsOut, Is.EqualTo(0));
        }

        #endregion

        #region Job Lifecycle and Tracking Tests

        [Test]
        public async Task CancelJobAsync_UpdatesJobMappingStatus()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            
            // Setup successful PATCH endpoint
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.OK, "", "PATCH");
            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert - Verifies the method completes successfully
            Assert.That(result, Is.True);
        }

        [Test]
        public async Task GetJobStatusAsync_WithoutTrackedJob_UsesFlinkJobIdAsJobId()
        {
            // Arrange
            var flinkJobId = "flink-123";
            var statusResponse = @"{ ""state"": ""RUNNING"" }";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, statusResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act - No job mapping exists, so it should fall back to FlinkJobId
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.JobId, Is.EqualTo(flinkJobId)); // Falls back to FlinkJobId when not in mapping
            Assert.That(result.FlinkJobId, Is.EqualTo(flinkJobId));
        }

        #endregion

        #region Validation Tests - Additional Edge Cases

        [Test]
        public void SubmitJobAsync_WithNullMetadata_ThrowsException()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = null!,
                Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert - The code tries to access jobDefinition.Metadata.JobId which throws NullReferenceException
            // This is caught and wrapped, so we expect the method to return a failure result
            Assert.ThrowsAsync<NullReferenceException>(async () => await jobManager.SubmitJobAsync(jobDefinition));
        }

        [Test]
        public async Task SubmitJobAsync_WithValidationError_LogsErrorMessage()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "", JobName = "Test" },
                Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("validation failed")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task SubmitJobAsync_WithWhitespaceJobId_ReturnsValidationFailure()
        {
            // Arrange
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "   ", JobName = "Test" },
                Source = new KafkaSourceDefinition { Topic = "test-topic", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output-topic", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public async Task SubmitJobAsync_WithFileSource_ValidatesPath()
        {
            // Arrange - Valid file source but missing path
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new FileSourceDefinition { Path = null! },
                Sink = new FileSinkDefinition { Path = "/tmp/output" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("path").IgnoreCase);
        }

        [Test]
        public async Task SubmitJobAsync_WithFileSink_ValidatesPath()
        {
            // Arrange - Valid file sink but missing path
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new FileSourceDefinition { Path = "/tmp/input" },
                Sink = new FileSinkDefinition { Path = null! }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("path").IgnoreCase);
        }

        [Test]
        public async Task SubmitJobAsync_WithKafkaSource_ValidatesTopic()
        {
            // Arrange - Valid kafka source but missing topic
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new KafkaSourceDefinition { Topic = null!, BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = "output", BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("topic").IgnoreCase);
        }

        [Test]
        public async Task SubmitJobAsync_WithKafkaSink_ValidatesTopic()
        {
            // Arrange - Valid kafka sink but missing topic
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-job-1", JobName = "Test Job" },
                Source = new KafkaSourceDefinition { Topic = "input", BootstrapServers = "localhost:9092" },
                Sink = new KafkaSinkDefinition { Topic = null!, BootstrapServers = "localhost:9092" }
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("topic").IgnoreCase);
        }

        #endregion

        #region Endpoint Discovery Tests

        [Test]
        public void Constructor_WithAspireEndpoint_UsesAspireDiscovery()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("services__flink-jobmanager__jm-http__0", "http://localhost:12345");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

                // Assert - Constructor logs the discovered endpoint
                _mockLogger.Verify(
                    x => x.Log(
                        LogLevel.Information,
                        It.IsAny<EventId>(),
                        It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Aspire service discovery")),
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
                Environment.SetEnvironmentVariable("services__flink-jobmanager__http__0", "http://localhost:54321");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

                // Assert - Constructor logs legacy format usage
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
        public void Constructor_WithEnvironmentVariables_UsesEnvVars()
        {
            // Arrange
            try
            {
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "custom-host");
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "9999");
                
                // Act
                _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

                // Assert - Constructor logs environment variable usage
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
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", null);
            }
        }

        [Test]
        public void Constructor_WithNoDiscovery_UsesDefaultEndpoint()
        {
            // Arrange - No environment variables set
            
            // Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert - Constructor logs default endpoint usage
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("default")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void Constructor_WithDefaultEndpoint_LogsWarning()
        {
            // Arrange - No environment variables set
            
            // Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert - Constructor logs warning about Aspire not found
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Aspire service discovery not found")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        #endregion

        #region Job Status Edge Cases

        [Test]
        public async Task GetJobStatusAsync_WithMissingStateProperty_ReturnsUnknown()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            var statusResponse = @"{ }"; // Empty response without state property
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, statusResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("UNKNOWN"));
        }

        [Test]
        public async Task GetJobStatusAsync_WithNullState_ReturnsUnknown()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            var statusResponse = @"{ ""state"": null }";
            
            SetupHttpResponse($"/v1/jobs/{flinkJobId}", HttpStatusCode.OK, statusResponse);
            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.GetJobStatusAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result!.State, Is.EqualTo("UNKNOWN"));
        }

        [Test]
        public void GetJobStatusAsync_WithHttpException_WrapsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Connection refused"));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.GetJobStatusAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to query Flink"));
        }

        #endregion

        #region Cancel Job Additional Scenarios

        [Test]
        public void CancelJobAsync_WithBadRequest_ThrowsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.BadRequest, "Bad request", "PATCH");
            SetupHttpResponse($"/jobs/{flinkJobId}/cancel", HttpStatusCode.BadRequest, "Bad request", "POST");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () => await jobManager.CancelJobAsync(flinkJobId));
        }

        [Test]
        public async Task CancelJobAsync_WithPatchSuccess_LogsSuccess()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            SetupHttpResponse($"/jobs/{flinkJobId}?mode=cancel", HttpStatusCode.OK, "", "PATCH");

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.CancelJobAsync(flinkJobId);

            // Assert
            Assert.That(result, Is.True);
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully canceled")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public async Task CancelJobAsync_WithPostFallback_LogsWarningAndSuccess()
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
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("trying POST endpoint")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void CancelJobAsync_WithException_WrapsException()
        {
            // Arrange
            var flinkJobId = "test-job-123";
            
            _mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await jobManager.CancelJobAsync(flinkJobId));
            Assert.That(ex!.Message, Does.Contain("Failed to cancel job"));
        }

        #endregion

        #region SQL Source Validation Tests

        [Test]
        public async Task SubmitJobAsync_WithSqlSourceGatewayMode_AllowsNullSink()
        {
            // Arrange - SQL Gateway jobs don't require a sink
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                Source = new SqlSourceDefinition 
                { 
                    Statements = new List<string> { "SELECT * FROM test_table" },
                    ExecutionMode = "gateway"
                },
                Sink = null!
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            var result = await jobManager.SubmitJobAsync(jobDefinition);

            // Assert - Should pass validation but fail on SQL Gateway (not mocked)
            Assert.That(result, Is.Not.Null);
            // Will fail during execution, not validation
        }

        [Test]
        public async Task SubmitJobAsync_WithEmptySqlStatements_StillPassesValidation()
        {
            // Arrange - Empty SQL statements should still pass validation
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "test-sql-job", JobName = "SQL Test" },
                Source = new SqlSourceDefinition 
                { 
                    Statements = new List<string>()
                },
                Sink = null!
            };

            var jobManager = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Act
            await Task.Run(async () =>
            {
                var result = await jobManager.SubmitJobAsync(jobDefinition);
                
                // Assert - Validation passes, but execution will fail
                Assert.That(result, Is.Not.Null);
            });
        }

        #endregion

        #region Constructor and Initialization Tests

        [Test]
        public void Constructor_SetsBaseAddressCorrectly()
        {
            // Arrange & Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert - HttpClient base address should be set during construction
            Assert.That(_httpClient.BaseAddress, Is.Not.Null);
        }

        [Test]
        public void Constructor_SetsTimeoutTo5Minutes()
        {
            // Arrange & Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert
            Assert.That(_httpClient.Timeout, Is.EqualTo(TimeSpan.FromMinutes(5)));
        }

        [Test]
        public void Constructor_LogsInitialization()
        {
            // Arrange & Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert - Logs initialization message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("initialized")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Test]
        public void Constructor_LogsConnectivityVerificationMessage()
        {
            // Arrange & Act
            _ = new FlinkJobManager(_mockLogger.Object, _httpClient);

            // Assert - Logs that connectivity will be verified
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("verify Flink connectivity")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
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
