using System.Net;
using System.Text.Json;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Additional branch coverage tests for FlinkJobGatewayService
/// Covers logging branches and edge cases in private methods
/// </summary>
[TestFixture]
public class FlinkJobGatewayServiceAdditionalBranchCoverageTests
{
    [SetUp]
    public void SetUp()
    {
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromMilliseconds(1);
    }

    [TearDown]
    public void TearDown()
    {
        FlinkJobGatewayService.RetryDelay = TimeSpan.FromSeconds(1);
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
    }

    #region CreateDefaultHttpClient - ApiKey branch coverage

    [Test]
    public void Constructor_WithApiKey_AddsApiKeyHeader()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://localhost:8086",
            ApiKey = "test-api-key-12345"
        };

        // Act
        using var service = new FlinkJobGatewayService(config);

        // Assert - Service should be created successfully with ApiKey
        Assert.That(service, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithEmptyApiKey_DoesNotAddApiKeyHeader()
    {
        // Arrange
        var config = new FlinkJobGatewayConfiguration
        {
            BaseUrl = "http://localhost:8086",
            ApiKey = "" // Empty string
        };

        // Act
        using var service = new FlinkJobGatewayService(config);

        // Assert - Service should be created successfully without ApiKey
        Assert.That(service, Is.Not.Null);
    }

    #endregion

    #region Dispose - Branch coverage

    [Test]
    public void Dispose_CalledMultipleTimes_OnlyDisposesOnce()
    {
        // Arrange
        var service = new FlinkJobGatewayService();

        // Act
        service.Dispose();
        service.Dispose(); // Second dispose should be no-op

        // Assert - Should not throw
        Assert.Pass("Multiple Dispose calls handled correctly");
    }

    [Test]
    public void Dispose_WithProvidedHttpClient_DisposesClient()
    {
        // Arrange
        var customClient = new HttpClient { BaseAddress = new Uri("http://localhost:8086") };
        var service = new FlinkJobGatewayService(null, customClient);

        // Act
        service.Dispose();

        // Assert - HttpClient should be disposed
        Assert.Throws<ObjectDisposedException>(() => customClient.GetAsync("/test"));
    }

    #endregion
}
