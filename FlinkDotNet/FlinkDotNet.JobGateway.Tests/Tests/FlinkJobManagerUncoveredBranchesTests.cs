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
    /// Targeted tests for uncovered branches in FlinkJobManager to reach 100% branch coverage.
    /// Focuses on the lowest coverage methods identified through coverage analysis.
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerUncoveredBranchesTests
    {
        private Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        private HttpClient _httpClient = null!;        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            this._mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Handler did not return a response message."));
            
            this._httpClient = new HttpClient(this._mockHttpMessageHandler.Object)
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
        }



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
                Timeout = TimeSpan.FromSeconds(1) // Short timeout for unmocked calls
            };
        }

