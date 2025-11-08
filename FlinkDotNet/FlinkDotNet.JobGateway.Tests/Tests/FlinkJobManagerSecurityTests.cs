#nullable enable
using System.Net;
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Security tests for FlinkJobManager to prevent URL path injection vulnerabilities.
    /// These tests verify that user-controlled data cannot be used to construct malicious URLs.
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerSecurityTests
    {
        private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        private HttpClient _httpClient = null!;
        private FlinkJobManager _jobManager = null!;

        [SetUp]
        public void Setup()
        {
            // Set static delays and timeouts to 1ms for fast test execution
            FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JarRegistrationTimeout = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JobRecoveryTimeout = TimeSpan.FromMilliseconds(1);

            this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
            this._mockConfiguration = new Mock<IConfiguration>();
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

            this._jobManager = new FlinkJobManager(this._mockLogger.Object, this._mockConfiguration.Object, this._httpClient);
        }

        [TearDown]
        public void TearDown()
        {
            // Restore default delays and timeouts
            FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromSeconds(1);
            FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromSeconds(1);
            FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromSeconds(1);
            FlinkJobManager.JarRegistrationTimeout = TimeSpan.FromSeconds(30);
            FlinkJobManager.JobRecoveryTimeout = TimeSpan.FromSeconds(30);

            this._httpClient?.Dispose();
        }

        #region Path Traversal Tests

        [Test]
        public void GetJobStatusAsync_WithPathTraversalInJobId_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "../../../admin/config";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithRelativePathSequence_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "job/../other-endpoint";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithBackslashPath_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "..\\..\\admin";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobMetricsAsync_WithPathTraversalInJobId_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "../../../etc/passwd";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobMetricsAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        #endregion

        #region Special Character Tests

        [Test]
        public void GetJobStatusAsync_WithForwardSlash_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "valid-job/extra-path";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithQuestionMark_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "valid-job?param=value";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithHashSymbol_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "valid-job#fragment";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithAtSymbol_ThrowsArgumentException()
        {
            // Arrange
            var maliciousJobId = "user@host";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        #endregion

        #region Null/Empty Tests

        [Test]
        public void GetJobStatusAsync_WithNullJobId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(null!));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithEmptyJobId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(""));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithWhitespaceJobId_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync("   "));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        #endregion

        #region Valid Input Tests

        [Test]
        public async Task GetJobStatusAsync_WithValidJobId_AcceptsInput()
        {
            // Arrange
            var validJobId = "abc123-def456-789ghi";
            var expectedUrl = $"/v1/jobs/{Uri.EscapeDataString(validJobId)}";

            this.SetupHttpResponse(expectedUrl, HttpStatusCode.OK, "{\"state\":\"RUNNING\"}");

            // Act
            var result = await this._jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            this.VerifyHttpRequest(expectedUrl);
        }

        [Test]
        public async Task GetJobStatusAsync_WithHyphensAndUnderscores_AcceptsInput()
        {
            // Arrange
            var validJobId = "job_123-456_test";
            var expectedUrl = $"/v1/jobs/{Uri.EscapeDataString(validJobId)}";

            this.SetupHttpResponse(expectedUrl, HttpStatusCode.OK, "{\"state\":\"RUNNING\"}");

            // Act
            var result = await this._jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            this.VerifyHttpRequest(expectedUrl);
        }

        [Test]
        public async Task GetJobStatusAsync_WithNumericJobId_AcceptsInput()
        {
            // Arrange
            var validJobId = "123456789";
            var expectedUrl = $"/v1/jobs/{Uri.EscapeDataString(validJobId)}";

            this.SetupHttpResponse(expectedUrl, HttpStatusCode.OK, "{\"state\":\"RUNNING\"}");

            // Act
            var result = await this._jobManager.GetJobStatusAsync(validJobId);

            // Assert
            Assert.That(result, Is.Not.Null);
            this.VerifyHttpRequest(expectedUrl);
        }

        #endregion

        #region URL Encoding Tests

        [Test]
        public void GetJobStatusAsync_WithUrlEncodedPathTraversal_ThrowsArgumentException()
        {
            // Arrange - URL encoded ../
            var maliciousJobId = "%2e%2e%2f%2e%2e%2fadmin";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        [Test]
        public void GetJobStatusAsync_WithDoubleEncodedPathTraversal_ThrowsArgumentException()
        {
            // Arrange - Double encoded ../
            var maliciousJobId = "%252e%252e%252f";

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () =>
                await this._jobManager.GetJobStatusAsync(maliciousJobId));
            Assert.That(ex!.Message, Does.Contain("flinkJobId"));
        }

        #endregion

        #region Helper Methods

        private void SetupHttpResponse(string requestUri, HttpStatusCode statusCode, string content)
        {
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.RequestUri != null && req.RequestUri.ToString().Contains(requestUri)),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    Content = new StringContent(content)
                });
        }

        private void VerifyHttpRequest(string expectedUri)
        {
            this._mockHttpMessageHandler
                .Protected()
                .Verify(
                    "SendAsync",
                    Times.Once(),
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.RequestUri != null && req.RequestUri.ToString().Contains(expectedUri)),
                    ItExpr.IsAny<CancellationToken>()
                );
        }

        #endregion
    }
}
