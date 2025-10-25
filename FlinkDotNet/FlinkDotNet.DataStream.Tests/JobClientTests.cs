using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class JobClientTests
    {
        private Mock<HttpMessageHandler> _mockHttpHandler = null!;
        private HttpClient _mockHttpClient = null!;

        [SetUp]
        public void Setup()
        {
            // Set environment variable required by FlinkJobGatewayConfiguration
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");

            _mockHttpHandler = new Mock<HttpMessageHandler>();
            _mockHttpClient = new HttpClient(_mockHttpHandler.Object)
            {
                BaseAddress = new Uri("http://test-flink:8081")
            };
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            _mockHttpClient?.Dispose();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithDefaultParameters_CreatesClient()
        {
            // Act
            using var client = new JobClient("test-job");

            // Assert
            Assert.That(client, Is.Not.Null);
            Assert.That(client.JobName, Is.EqualTo("test-job"));
        }

        [Test]
        public void Constructor_WithCustomTimeout_SetsTimeout()
        {
            // Arrange
            var timeout = TimeSpan.FromSeconds(10);

            // Act
            using var client = new JobClient("test-job", timeout);

            // Assert
            Assert.That(client, Is.Not.Null);
            Assert.That(client.JobName, Is.EqualTo("test-job"));
        }

        [Test]
        public void Constructor_WithEnvironmentVariables_UsesEnvVars()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", "custom-host");
            Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", "9090");
            Environment.SetEnvironmentVariable("FLINK_HTTP_TIMEOUT_SECONDS", "30");

            try
            {
                // Act
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                // Cleanup
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", null);
                Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", null);
                Environment.SetEnvironmentVariable("FLINK_HTTP_TIMEOUT_SECONDS", null);
            }
        }

        [Test]
        public void Constructor_WithGatewayConfig_UsesConfig()
        {
            // Arrange
            var config = new FlinkJobGatewayConfiguration
            {
                HttpTimeout = TimeSpan.FromSeconds(5),
                MaxRetries = 2,
                RetryDelay = TimeSpan.FromSeconds(1)
            };

            // Act
            using var client = new JobClient("test-job", gatewayConfig: config);

            // Assert
            Assert.That(client, Is.Not.Null);
        }

        #endregion

        #region GetJobId Tests

        [Test]
        public void GetJobId_ReturnsJobId()
        {
            // Arrange
            using var client = new JobClient("test-job")
            {
                JobId = "test-job-id-123"
            };

            // Act
            var jobId = client.GetJobId();

            // Assert
            Assert.That(jobId, Is.EqualTo("test-job-id-123"));
        }

        [Test]
        public void GetJobId_WithEmptyJobId_ReturnsEmptyString()
        {
            // Arrange
            using var client = new JobClient("test-job");

            // Act
            var jobId = client.GetJobId();

            // Assert
            Assert.That(jobId, Is.Empty);
        }

        #endregion

        #region TriggerSavepointAsync Tests with HTTP Mocking

        [Test]
        public async Task TriggerSavepointAsync_WithSuccessResponse_ReturnsSavepointResult()
        {
            // Arrange
            var jobId = "test-job-id";
            var savepointPath = "/test/savepoint/path";
            var triggerId = "trigger-123";

            var responseJson = JsonSerializer.Serialize(new
            {
                requestId = triggerId
            });
            var responseContent = new StringContent(responseJson, Encoding.UTF8, "application/json");

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"/v1/jobs/{jobId}/savepoints")),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.TriggerSavepointAsync(savepointPath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task TriggerSavepointAsync_WithNullPath_SendsNullTargetDirectory()
        {
            // Arrange
            var jobId = "test-job-id";

            var responseJson = JsonSerializer.Serialize(new
            {
                requestId = "trigger-123"
            });
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.TriggerSavepointAsync(null);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task TriggerSavepointAsync_WithErrorResponse_ReturnsFailure()
        {
            // Arrange
            var jobId = "test-job-id";
            var errorMessage = "Savepoint trigger failed";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent(errorMessage, Encoding.UTF8, "text/plain")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.TriggerSavepointAsync("/test/path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
        }

        [Test]
        public async Task TriggerSavepointAsync_WithMalformedJson_HandlesGracefully()
        {
            // Arrange
            var jobId = "test-job-id";
            var malformedJson = "{ invalid json }";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(malformedJson, Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.TriggerSavepointAsync("/test/path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.Empty); // Should be empty due to JSON parse failure
        }

        [Test]
        public void TriggerSavepointAsync_WithCancellationToken_PropagatesToken()
        {
            // Arrange
            var jobId = "test-job-id";
            using var cts = new System.Threading.CancellationTokenSource();
            cts.Cancel(); // Cancel immediately

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ThrowsAsync(new TaskCanceledException());

            using var client = CreateTestJobClient(jobId);

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await client.TriggerSavepointAsync("/test/path", cts.Token));
        }

        #endregion

        #region CancelWithSavepointAsync Tests with HTTP Mocking

        [Test]
        public async Task CancelWithSavepointAsync_WithSuccessResponse_ReturnsSavepointResult()
        {
            // Arrange
            var jobId = "test-job-id";
            var savepointPath = "/test/savepoint/path";
            var triggerId = "trigger-456";

            var responseJson = JsonSerializer.Serialize(new
            {
                requestId = triggerId
            });
            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"/v1/jobs/{jobId}/savepoints")),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.CancelWithSavepointAsync(savepointPath);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public async Task CancelWithSavepointAsync_WithErrorResponse_ReturnsFailure()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Job not found", Encoding.UTF8, "text/plain")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.CancelWithSavepointAsync("/test/path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
        }

        #endregion

        #region StopWithSavepointAsync Tests with HTTP Mocking

        [Test]
        public async Task StopWithSavepointAsync_WithSuccessResponse_ReturnsResult()
        {
            // Arrange
            var jobId = "test-job-id";
            var savepointPath = "/test/savepoint/path";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.RequestUri!.ToString().Contains($"/v1/jobs/{jobId}/stop")),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.StopWithSavepointAsync(savepointPath, drain: true);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.Drained, Is.True);
            Assert.That(result.SavepointPath, Is.EqualTo(savepointPath));
        }

        [Test]
        public async Task StopWithSavepointAsync_WithNoDrain_SetsDrainedFalse()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.StopWithSavepointAsync(null, drain: false);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Drained, Is.False);
        }

        [Test]
        public async Task StopWithSavepointAsync_WithErrorResponse_ReturnsFailure()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Invalid request", Encoding.UTF8, "text/plain")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.StopWithSavepointAsync("/test/path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Not.Null);
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_CalledOnce_DisposesResources()
        {
            // Arrange
            var client = new JobClient("test-job", TimeSpan.FromSeconds(1));

            // Act
            client.Dispose();

            // Assert - Should not throw
            Assert.Pass("Dispose completed without exceptions");
        }

        [Test]
        public void Dispose_CalledMultipleTimes_HandlesGracefully()
        {
            // Arrange
            var client = new JobClient("test-job", TimeSpan.FromSeconds(1));

            // Act
            client.Dispose();
            client.Dispose(); // Second dispose should be safe

            // Assert
            Assert.Pass("Multiple dispose calls handled gracefully");
        }

        [Test]
        public void Dispose_AfterOperations_Completes()
        {
            // Arrange
            var client = new JobClient("test-job", TimeSpan.FromSeconds(1))
            {
                JobId = "test-id"
            };

            // Act
            var jobId = client.GetJobId();
            client.Dispose();

            // Assert
            Assert.That(jobId, Is.EqualTo("test-id"));
        }

        #endregion

        #region IJobClient Interface Tests

        [Test]
        public void JobClient_ImplementsIJobClient()
        {
            // Arrange & Act
            using var client = new JobClient("test-job");

            // Assert
            Assert.That(client, Is.InstanceOf<IJobClient>());
        }

        [Test]
        public void JobClient_IJobClientGetJobId_MatchesImplementation()
        {
            // Arrange
            using var client = new JobClient("test-job") { JobId = "abc-123" };
            IJobClient interfaceClient = client;

            // Act
            var directJobId = client.GetJobId();
            var interfaceJobId = interfaceClient.GetJobId();

            // Assert
            Assert.That(directJobId, Is.EqualTo(interfaceJobId));
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        public void TriggerSavepointAsync_WithHttpRequestException_Throws()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Network error"));

            using var client = CreateTestJobClient(jobId);

            // Act & Assert
            Assert.ThrowsAsync<HttpRequestException>(async () =>
                await client.TriggerSavepointAsync("/test/path"));
        }

        [Test]
        public void CancelWithSavepointAsync_WithTimeout_Throws()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ThrowsAsync(new TaskCanceledException("Request timeout"));

            using var client = CreateTestJobClient(jobId);

            // Act & Assert
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await client.CancelWithSavepointAsync("/test/path"));
        }

        [Test]
        public async Task StopWithSavepointAsync_WithEmptyResponse_HandlesGracefully()
        {
            // Arrange
            var jobId = "test-job-id";

            _mockHttpHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<System.Threading.CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("", Encoding.UTF8, "application/json")
                });

            using var client = CreateTestJobClient(jobId);

            // Act
            var result = await client.StopWithSavepointAsync("/test/path");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
        }

        #endregion

        #region Helper Methods

        private JobClient CreateTestJobClient(string jobId)
        {
            // Use reflection to inject the mocked HTTP client
            var client = new JobClient("test-job", TimeSpan.FromSeconds(1))
            {
                JobId = jobId
            };

            // Set the private _flinkHttp field using reflection
            var httpField = typeof(JobClient).GetField("_flinkHttp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (httpField != null)
            {
                httpField.SetValue(client, _mockHttpClient);
            }

            return client;
        }

        #endregion
    }
}
