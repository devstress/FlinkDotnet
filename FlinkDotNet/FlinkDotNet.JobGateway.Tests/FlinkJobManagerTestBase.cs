#nullable enable
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using FlinkDotNet.JobGateway.Services;
using NUnit.Framework;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Base class for FlinkJobManager tests providing common setup for thread-safe parallel execution.
    /// Sets up IConfiguration mocking to avoid Environment.SetEnvironmentVariable calls.
    /// Uses OneTimeSetUp/OneTimeTearDown to create one HttpClient per test fixture for thread-safe parallel execution.
    /// </summary>
    public abstract class FlinkJobManagerTestBase
    {
        protected Mock<ILogger<FlinkJobManager>> _mockLogger = null!;
        protected Mock<IConfiguration> _mockConfiguration = null!;
        protected Mock<HttpMessageHandler> _mockHttpMessageHandler = null!;
        protected HttpClient _httpClient = null!;

        [OneTimeSetUp]
        public virtual void OneTimeSetup()
        {
            // Create HttpClient once per test fixture (class)
            // Each test class gets its own instance, avoiding disposal conflicts in parallel execution
            this._mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            
            // Setup default handler for unmocked HTTP requests to fail fast
            _ = this._mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("Handler did not return a response message."));
            
            // Don't set BaseAddress here - FlinkJobManager constructor will set it
            this._httpClient = new HttpClient(this._mockHttpMessageHandler.Object)
            {
                Timeout = TimeSpan.FromSeconds(1)
            };
        }

        [SetUp]
        public virtual void Setup()
        {
            // Set static delays to 1ms for fast test execution
            FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
            FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);

            this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
            this._mockConfiguration = new Mock<IConfiguration>();

            // Setup default configuration values (returns null for any key not explicitly mocked)
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

        [OneTimeTearDown]
        public virtual void OneTimeTearDown()
        {
            // Dispose HttpClient once when all tests in this fixture complete
            this._httpClient?.Dispose();
        }

        protected static string? FindRepoRoot(string start)
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

        /// <summary>
        /// Helper method to mock configuration values (simulates environment variables).
        /// Use this instead of Environment.SetEnvironmentVariable for thread-safe parallel tests.
        /// </summary>
        protected void MockConfigurationValue(string key, string value)
        {
            this._mockConfiguration.Setup(c => c[key]).Returns(value);
        }
    }
}
