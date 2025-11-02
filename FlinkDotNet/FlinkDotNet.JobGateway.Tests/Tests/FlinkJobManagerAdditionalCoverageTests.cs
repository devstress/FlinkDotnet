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

            this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
            this._mockConfiguration = new Mock<IConfiguration>();

            // Setup default configuration values
            _ = this._mockConfiguration.Setup(x => x[It.IsAny<string>()]).Returns((string?) null);
            
            // Mock FLINK_RUNNER_JAR_PATH to avoid Maven builds during tests
            string? repoRoot = FindRepoRoot(Environment.CurrentDirectory);
            if (repoRoot != null)
            {
                string jarPath = Path.Combine(repoRoot, "FlinkIRRunner", "target", "flink-ir-runner-java17.jar");
                if (!File.Exists(jarPath))
                {
                    jarPath = Path.Combine(repoRoot, "FlinkDotNet", "FlinkDotNet.JobGateway", "flink-ir-runner-java17.jar");
                }
                if (File.Exists(jarPath))
                {
                    this._mockConfiguration.Setup(c => c["FLINK_RUNNER_JAR_PATH"]).Returns(jarPath);
                }
            }
        }

        private static string? FindRepoRoot(string start)
        {
            DirectoryInfo? dir = new(start);
            while (dir != null)
            {
                string globalJson = Path.Combine(dir.FullName, "global.json");
                if (File.Exists(globalJson))
                {
                    return dir.FullName;
                }
                dir = dir.Parent;
            }
            return null;
        }

