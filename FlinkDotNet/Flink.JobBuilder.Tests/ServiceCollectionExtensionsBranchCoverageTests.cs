#nullable enable

using Flink.JobBuilder.Extensions;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Flink.JobBuilder.Tests;

/// <summary>
/// Tests for ServiceCollectionExtensions to achieve 100% branch coverage
/// </summary>
[TestFixture]
public class ServiceCollectionExtensionsBranchCoverageTests
{
    #region AddFlinkJobGatewayConfiguration Tests

    [Test]
    public void AddFlinkJobGatewayConfiguration_WithBaseUrlInConfig_UsesConfigValue()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "FlinkJobGateway:BaseUrl", "http://config-url:8080/" },
            { "FlinkJobGateway:HttpTimeout", "00:05:00" },
            { "FlinkJobGateway:MaxRetries", "3" },
            { "FlinkJobGateway:RetryDelay", "00:00:01" }
        });
        var configuration = configBuilder.Build();

        // Act
        _ = services.AddFlinkJobGatewayConfiguration(configuration);
        var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<FlinkJobGatewayConfiguration>>().Value;

        // Assert
        Assert.That(options.BaseUrl, Is.EqualTo("http://config-url:8080/"));
        Assert.That(options.MaxRetries, Is.EqualTo(3));
    }

    [Test]
    public void AddFlinkJobGatewayConfiguration_WithNullBaseUrlInConfig_UsesEnvironmentVariable()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // BaseUrl not set in config
            { "FlinkJobGateway:MaxRetries", "3" }
        });
        var configuration = configBuilder.Build();

        // Set environment variable
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://env-url:9090/");

        try
        {
            // Act
            _ = services.AddFlinkJobGatewayConfiguration(configuration);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<FlinkJobGatewayConfiguration>>().Value;

            // Assert
            Assert.That(options.BaseUrl, Is.EqualTo("http://env-url:9090/"));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }
    }

    [Test]
    public void AddFlinkJobGatewayConfiguration_WithEmptyBaseUrlInConfig_UsesEnvironmentVariable()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            { "FlinkJobGateway:BaseUrl", "" }, // Empty string
            { "FlinkJobGateway:MaxRetries", "3" }
        });
        var configuration = configBuilder.Build();

        // Set environment variable
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://env-url-2:7070/");

        try
        {
            // Act
            _ = services.AddFlinkJobGatewayConfiguration(configuration);
            var provider = services.BuildServiceProvider();
            var options = provider.GetRequiredService<IOptions<FlinkJobGatewayConfiguration>>().Value;

            // Assert
            Assert.That(options.BaseUrl, Is.EqualTo("http://env-url-2:7070/"));
        }
        finally
        {
            // Cleanup
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }
    }

    [Test]
    public void AddFlinkJobGatewayConfiguration_WithNoBaseUrlAndNoEnvironment_StillRegisters()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            // No BaseUrl
            { "FlinkJobGateway:MaxRetries", "3" }
        });
        var configuration = configBuilder.Build();

        // Ensure no environment variable
        Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);

        // Act
        _ = services.AddFlinkJobGatewayConfiguration(configuration);
        var provider = services.BuildServiceProvider();

        // Assert - Configuration is registered, but will throw when accessing BaseUrl
        var options = provider.GetService<IOptions<FlinkJobGatewayConfiguration>>();
        Assert.That(options, Is.Not.Null);
        // Don't access BaseUrl as it will throw - just verify configuration was registered
    }

    [Test]
    public void AddFlinkJobGatewayConfiguration_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddFlinkJobGatewayConfiguration(configuration);

        // Assert
        Assert.That(result, Is.SameAs(services)); // Method chaining
    }

    #endregion

    #region AddFlinkJobGateway Tests

    [Test]
    public void AddFlinkJobGateway_RegistersConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "FlinkJobGateway:BaseUrl", "http://test:8080/" }
        }!);
        var configuration = configBuilder.Build();

        // Act
        _ = services.AddFlinkJobGateway(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var options = provider.GetService<IOptions<FlinkJobGatewayConfiguration>>();
        Assert.That(options, Is.Not.Null);
        Assert.That(options!.Value.BaseUrl, Is.EqualTo("http://test:8080/"));
    }

    [Test]
    public void AddFlinkJobGateway_RegistersService()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddHttpClient(); // FlinkJobGatewayService needs HttpClient
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "FlinkJobGateway:BaseUrl", "http://test:8080/" }
        }!);
        var configuration = configBuilder.Build();

        // Act
        _ = services.AddFlinkJobGateway(configuration);
        var provider = services.BuildServiceProvider();

        // Assert
        var service = provider.GetService<IFlinkJobGatewayService>();
        Assert.That(service, Is.Not.Null);
        Assert.That(service, Is.InstanceOf<FlinkJobGatewayService>());
    }

    [Test]
    public void AddFlinkJobGateway_ReturnsServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddFlinkJobGateway(configuration);

        // Assert
        Assert.That(result, Is.SameAs(services)); // Method chaining
    }

    [Test]
    public void AddFlinkJobGateway_RegistersServiceAsTransient()
    {
        // Arrange
        var services = new ServiceCollection();
        _ = services.AddHttpClient();
        var configBuilder = new ConfigurationBuilder();
        _ = configBuilder.AddInMemoryCollection(new Dictionary<string, string>
        {
            { "FlinkJobGateway:BaseUrl", "http://test:8080/" }
        }!);
        var configuration = configBuilder.Build();

        // Act
        _ = services.AddFlinkJobGateway(configuration);
        var provider = services.BuildServiceProvider();

        // Assert - Get two instances to verify they are different (Transient)
        var service1 = provider.GetService<IFlinkJobGatewayService>();
        var service2 = provider.GetService<IFlinkJobGatewayService>();
        Assert.That(service1, Is.Not.SameAs(service2)); // Different instances = Transient
    }

    #endregion
}
