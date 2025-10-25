using System;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Flink.JobBuilder.Extensions;

/// <summary>
/// Extension methods for configuring Flink Job Gateway services
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Flink Job Gateway configuration from appsettings.json.
    /// <para>
    /// Configuration section: "FlinkJobGateway"
    /// Example appsettings.json:
    /// {
    ///   "FlinkJobGateway": {
    ///     "BaseUrl": "http://localhost:8080/",
    ///     "HttpTimeout": "00:05:00",
    ///     "MaxRetries": 3,
    ///     "RetryDelay": "00:00:01"
    ///   }
    /// }
    /// </para>
    /// <para>
    /// Priority: appsettings.json > FLINK_JOB_GATEWAY_URL environment variable > Exception
    /// </para>
    /// </summary>
    public static IServiceCollection AddFlinkJobGatewayConfiguration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        _ = services.Configure<FlinkJobGatewayConfiguration>(options =>
        {
            // Bind from appsettings
            configuration.GetSection("FlinkJobGateway").Bind(options);

            // If BaseUrl is already configured in appsettings, use it
            if (!string.IsNullOrEmpty(options.BaseUrl))
            {
                return;
            }

            // Otherwise, try environment variable
            var envUrl = Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL");
            if (string.IsNullOrEmpty(envUrl))
            {
                return;
            }

            options.BaseUrl = envUrl;
        });

        return services;
    }

    /// <summary>
    /// Adds Flink Job Gateway service with configuration
    /// </summary>
    public static IServiceCollection AddFlinkJobGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        return services
            .AddFlinkJobGatewayConfiguration(configuration)
            .AddTransient<IFlinkJobGatewayService, FlinkJobGatewayService>();
    }
}
