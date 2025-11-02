#nullable enable
using FlinkDotNet.JobGateway.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace FlinkDotNet.JobGateway.Tests.Tests;

/// <summary>
/// Final coverage tests to reach 100% branch coverage for FlinkJobManager.
/// Focuses on uncovered branches in endpoint discovery, error handling, and edge cases.
/// </summary>
[TestFixture]
public class FlinkJobManagerFinalCoverageTests
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
    public void SetUp()
    {
        // Set static delays to 1ms for fast test execution
        FlinkJobManager.SqlGatewayRetryDelay = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JarRegistrationPollingDelay = TimeSpan.FromMilliseconds(1);
        FlinkJobManager.JobRecoveryPollingDelay = TimeSpan.FromMilliseconds(1);

        this._mockLogger = new Mock<ILogger<FlinkJobManager>>();
        this._mockConfiguration = new Mock<IConfiguration>();

        // Reset all environment variables before each test
        Environment.SetEnvironmentVariable("services__flink-jobmanager__jm-http__0", null);
        Environment.SetEnvironmentVariable("services__flink-jobmanager__http__0", null);
        Environment.SetEnvironmentVariable("services__flink-sql-gateway__sg-http__0", null);
        Environment.SetEnvironmentVariable("services__flink-sql-gateway__http__0", null);
        Environment.SetEnvironmentVariable("FLINK_CLUSTER_HOST", null);
        Environment.SetEnvironmentVariable("FLINK_CLUSTER_PORT", null);
        Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST", null);
        Environment.SetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT", null);
    }

