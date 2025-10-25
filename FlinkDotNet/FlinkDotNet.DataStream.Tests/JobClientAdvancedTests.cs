using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Flink.JobBuilder.Models;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class JobClientAdvancedTests
    {
        [SetUp]
        public void SetUp()
        {
            // Set environment variable required by FlinkJobGatewayConfiguration
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        [Test]
        public async Task TriggerSavepointAsync_WhenJsonParsingFails_ReturnsResultWithEmptyTriggerId()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/savepoints")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Invalid JSON {{{", System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            // Use reflection to inject mocked HttpClient
            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.TriggerSavepointAsync("/tmp/savepoint");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task TriggerSavepointAsync_WhenResponseIsNotSuccess_ReturnsFailureWithErrorText()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/savepoints")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    Content = new StringContent("Error: Invalid savepoint path", System.Text.Encoding.UTF8, "text/plain")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.TriggerSavepointAsync("/tmp/savepoint");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Error: Invalid savepoint path"));
        }

        [Test]
        public async Task CancelWithSavepointAsync_Success_ReturnsSuccessResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/savepoints")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"request-id\": \"abc123\"}", System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.CancelWithSavepointAsync("/tmp/savepoint");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("abc123"));
        }

        [Test]
        public async Task CancelWithSavepointAsync_WhenJsonParsingFails_ReturnsResultWithEmptyTriggerId()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/savepoints")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("Not valid JSON", System.Text.Encoding.UTF8, "text/plain")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.CancelWithSavepointAsync("/tmp/savepoint");

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
        }

        [Test]
        public async Task StopWithSavepointAsync_Success_ReturnsSuccessResult()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/stop")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.StopWithSavepointAsync("/tmp/savepoint", drain: true);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.SavepointPath, Is.EqualTo("/tmp/savepoint"));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public async Task StopWithSavepointAsync_WithNoDrain_ReturnsCorrectDrainedFlag()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/stop")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.StopWithSavepointAsync("/tmp/savepoint", drain: false);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Drained, Is.False);
        }

        [Test]
        public async Task StopWithSavepointAsync_WhenFails_ReturnsFailureWithError()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri!.ToString().Contains("/stop")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Error stopping job", System.Text.Encoding.UTF8, "text/plain")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.StopWithSavepointAsync("/tmp/savepoint");

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Error stopping job"));
        }

        [Test]
        public void JobClient_WithCustomTimeout_UsesProvidedTimeout()
        {
            // Arrange & Act
            var customTimeout = TimeSpan.FromMinutes(10);
            var jobClient = new JobClient("test-job", httpTimeout: customTimeout);

            // Assert
            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var httpClient = httpField!.GetValue(jobClient) as HttpClient;
            Assert.That(httpClient!.Timeout, Is.EqualTo(customTimeout));
        }

        [Test]
        public void JobClient_WithEnvironmentVariableTimeout_UsesEnvironmentTimeout()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_HTTP_TIMEOUT_SECONDS", "120");

            try
            {
                // Act
                var jobClient = new JobClient("test-job");

                // Assert
                var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var httpClient = httpField!.GetValue(jobClient) as HttpClient;
                Assert.That(httpClient!.Timeout, Is.EqualTo(TimeSpan.FromSeconds(120)));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_HTTP_TIMEOUT_SECONDS", null);
            }
        }

        [Test]
        public void JobClient_WithCustomGatewayConfig_UsesProvidedConfig()
        {
            // Arrange
            var config = new FlinkJobGatewayConfiguration
            {
                HttpTimeout = TimeSpan.FromSeconds(30),
                MaxRetries = 5,
                RetryDelay = TimeSpan.FromSeconds(2)
            };

            // Act
            var jobClient = new JobClient("test-job", gatewayConfig: config);

            // Assert - JobClient should be created successfully
            Assert.That(jobClient.JobName, Is.EqualTo("test-job"));
        }

        [Test]
        public void JobClient_Dispose_DisposesHttpClient()
        {
            // Arrange
            var jobClient = new JobClient("test-job");

            // Act & Assert - Should not throw
            Assert.DoesNotThrow(() => jobClient.Dispose());

            // Dispose again should also not throw (idempotent)
            Assert.DoesNotThrow(() => jobClient.Dispose());
        }

        [Test]
        public async Task TriggerSavepointAsync_WithNullSavepointPath_SendsCorrectPayload()
        {
            // Arrange
            var mockHandler = new Mock<HttpMessageHandler>();
            mockHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri!.ToString().Contains("/savepoints") &&
                        req.Content!.ReadAsStringAsync().Result.Contains("\"targetDirectory\":null")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{\"request-id\": \"xyz\"}", System.Text.Encoding.UTF8, "application/json")
                });

            var httpClient = new HttpClient(mockHandler.Object) { BaseAddress = new Uri("http://localhost:8081") };
            var jobClient = new JobClient("test-job", TimeSpan.FromSeconds(1));

            var httpField = typeof(JobClient).GetField("_flinkHttp", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            httpField!.SetValue(jobClient, httpClient);

            jobClient.JobId = "test-job-id";

            // Act
            var result = await jobClient.TriggerSavepointAsync(null);

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("xyz"));
        }
    }
}
