using LocalTesting.WebApi.Models;

namespace LocalTesting.WebApi.Services;

/// <summary>
/// Infrastructure readiness validation service interface
/// Ensures real infrastructure has executed and generated metrics before test completion
/// </summary>
public interface IInfrastructureReadinessService
{
    Task<InfrastructureStatus> ValidateInfrastructureAsync(TimeSpan timeout = default);
    Task<bool> EnsureMetricAvailabilityAsync(string[] requiredMetrics, TimeSpan timeout = default);
    Task<WarmupResult> ExecuteWarmupWorkloadAsync(WarmupRequest request);
    Task<ValidationResult> ValidatePrometheusDataAsync(ValidationCriteria criteria);
}

/// <summary>
/// Infrastructure status result
/// </summary>
public class InfrastructureStatus
{
    public bool IsReady { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, bool> ComponentStatus { get; set; } = new();
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Warmup workload request
/// </summary>
public class WarmupRequest
{
    public int MessageCount { get; set; } = 1000;
    public string Topic { get; set; } = "test-warmup";
    public int TimeoutSeconds { get; set; } = 60;
    public bool RequireMetricPropagation { get; set; } = true;
}

/// <summary>
/// Warmup execution result
/// </summary>
public class WarmupResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int MessagesProduced { get; set; }
    public double ExecutionTimeSeconds { get; set; }
    public Dictionary<string, double> GeneratedMetrics { get; set; } = new();
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Prometheus validation criteria
/// </summary>
public class ValidationCriteria
{
    public string[] RequiredMetrics { get; set; } = Array.Empty<string>();
    public TimeSpan MaxAge { get; set; } = TimeSpan.FromMinutes(5);
    public bool RequireNonZeroValues { get; set; } = true;
    public int MinimumMetricCount { get; set; } = 1;
}

/// <summary>
/// Validation result
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] MissingMetrics { get; set; } = Array.Empty<string>();
    public string[] FoundMetrics { get; set; } = Array.Empty<string>();
    public Dictionary<string, double> MetricValues { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Exception thrown when infrastructure is not ready for testing
/// </summary>
public class InfrastructureNotReadyException : Exception
{
    public InfrastructureNotReadyException(string message) : base(message) { }
    public InfrastructureNotReadyException(string message, Exception innerException) : base(message, innerException) { }
}