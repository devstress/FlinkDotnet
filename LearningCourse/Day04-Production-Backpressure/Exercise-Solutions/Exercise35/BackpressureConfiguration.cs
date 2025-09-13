using Microsoft.Extensions.Logging;

namespace Exercise35.Core;

/// <summary>
/// Central configuration management for BackpressureQueue settings across all services.
/// Provides configurable per-customer backpressure limits with service-specific overrides.
/// </summary>
public class BackpressureConfiguration
{
    /// <summary>
    /// Default BackpressureQueue limit per customer for all services.
    /// </summary>
    public int DefaultMaxConcurrencyPerCustomer { get; set; } = 2;

    /// <summary>
    /// Service-specific BackpressureQueue overrides per customer.
    /// Key: Service type name, Value: Max concurrency per customer for that service type.
    /// </summary>
    public Dictionary<string, int> ServiceOverrides { get; set; } = new();

    /// <summary>
    /// Gets the configured BackpressureQueue limit for a specific service type.
    /// </summary>
    /// <param name="serviceType">Service type (e.g., "Gateway", "Flink", "Temporal")</param>
    /// <returns>Max concurrency per customer for the specified service type</returns>
    public int GetMaxConcurrencyPerCustomer(string serviceType)
    {
        if (ServiceOverrides.TryGetValue(serviceType, out var overrideValue))
        {
            return overrideValue;
        }
        return DefaultMaxConcurrencyPerCustomer;
    }

    /// <summary>
    /// Sets a service-specific BackpressureQueue override.
    /// </summary>
    /// <param name="serviceType">Service type (e.g., "Gateway", "Flink", "Temporal")</param>
    /// <param name="maxConcurrencyPerCustomer">Max concurrency per customer for this service type</param>
    public void SetServiceOverride(string serviceType, int maxConcurrencyPerCustomer)
    {
        if (maxConcurrencyPerCustomer <= 0)
            throw new ArgumentException("Max concurrency per customer must be positive", nameof(maxConcurrencyPerCustomer));

        ServiceOverrides[serviceType] = maxConcurrencyPerCustomer;
    }

    /// <summary>
    /// Removes a service-specific override, falling back to default.
    /// </summary>
    /// <param name="serviceType">Service type to remove override for</param>
    public void RemoveServiceOverride(string serviceType)
    {
        ServiceOverrides.Remove(serviceType);
    }

    /// <summary>
    /// Gets all configured BackpressureQueue settings for debugging/monitoring.
    /// </summary>
    public BackpressureConfigurationInfo GetConfigurationInfo()
    {
        return new BackpressureConfigurationInfo
        {
            DefaultMaxConcurrencyPerCustomer = DefaultMaxConcurrencyPerCustomer,
            ServiceOverrides = new Dictionary<string, int>(ServiceOverrides),
            EffectiveSettings = GetEffectiveSettings()
        };
    }

    /// <summary>
    /// Gets the effective BackpressureQueue settings for all known service types.
    /// </summary>
    private Dictionary<string, int> GetEffectiveSettings()
    {
        var knownServiceTypes = new[] { "Gateway", "Flink", "Temporal" };
        var effectiveSettings = new Dictionary<string, int>();

        foreach (var serviceType in knownServiceTypes)
        {
            effectiveSettings[serviceType] = GetMaxConcurrencyPerCustomer(serviceType);
        }

        // Add any additional overrides that might not be in known types
        foreach (var kvp in ServiceOverrides)
        {
            if (!effectiveSettings.ContainsKey(kvp.Key))
            {
                effectiveSettings[kvp.Key] = kvp.Value;
            }
        }

        return effectiveSettings;
    }

    /// <summary>
    /// Validates the configuration and logs warnings for potential issues.
    /// </summary>
    public void ValidateAndLog(ILogger logger)
    {
        logger.LogInformation("BackpressureQueue Configuration:");
        logger.LogInformation("  Default per customer: {DefaultMax}", DefaultMaxConcurrencyPerCustomer);

        if (ServiceOverrides.Any())
        {
            logger.LogInformation("  Service-specific overrides:");
            foreach (var kvp in ServiceOverrides.OrderBy(x => x.Key))
            {
                logger.LogInformation("    {ServiceType}: {MaxConcurrency} per customer", kvp.Key, kvp.Value);
            }
        }
        else
        {
            logger.LogInformation("  No service-specific overrides configured");
        }

        var effectiveSettings = GetEffectiveSettings();
        logger.LogInformation("  Effective settings per service type:");
        foreach (var kvp in effectiveSettings.OrderBy(x => x.Key))
        {
            logger.LogInformation("    {ServiceType}: {MaxConcurrency} per customer", kvp.Key, kvp.Value);
        }

        // Validation warnings
        if (DefaultMaxConcurrencyPerCustomer > 10)
        {
            logger.LogWarning("Default BackpressureQueue per customer is high ({Value}). This may reduce backpressure effectiveness.", 
                DefaultMaxConcurrencyPerCustomer);
        }

        foreach (var kvp in ServiceOverrides.Where(x => x.Value > 10))
        {
            logger.LogWarning("BackpressureQueue for {ServiceType} is high ({Value}). This may reduce backpressure effectiveness.", 
                kvp.Key, kvp.Value);
        }
    }

    /// <summary>
    /// Creates a default configuration for Exercise 3.5 scenarios.
    /// </summary>
    public static BackpressureConfiguration CreateDefault()
    {
        return new BackpressureConfiguration
        {
            DefaultMaxConcurrencyPerCustomer = 2
            // No service overrides - all services use 2 per customer as specified in Exercise 3.5
        };
    }

    /// <summary>
    /// Creates a configuration for testing with different service limits.
    /// </summary>
    public static BackpressureConfiguration CreateForTesting()
    {
        var config = new BackpressureConfiguration
        {
            DefaultMaxConcurrencyPerCustomer = 2
        };

        // Example of how different services could have different limits
        // config.SetServiceOverride("Gateway", 3);    // Gateways can handle more load
        // config.SetServiceOverride("Temporal", 1);   // Temporal instances are more constrained

        return config;
    }
}

/// <summary>
/// Information about current BackpressureQueue configuration for monitoring/debugging.
/// </summary>
public class BackpressureConfigurationInfo
{
    public int DefaultMaxConcurrencyPerCustomer { get; init; }
    public Dictionary<string, int> ServiceOverrides { get; init; } = new();
    public Dictionary<string, int> EffectiveSettings { get; init; } = new();

    public override string ToString()
    {
        var lines = new List<string>
        {
            $"Default: {DefaultMaxConcurrencyPerCustomer} per customer"
        };

        if (ServiceOverrides.Any())
        {
            lines.Add("Overrides: " + string.Join(", ", 
                ServiceOverrides.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }

        lines.Add("Effective: " + string.Join(", ", 
            EffectiveSettings.Select(kvp => $"{kvp.Key}={kvp.Value}")));

        return string.Join(" | ", lines);
    }
}