using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using System.Net.Http;

namespace FlinkDotNet.Resilience;

/// <summary>
/// Production-ready circuit breaker using Polly. Shared singleton policy to preserve state across calls.
/// </summary>
public static class CircuitBreaker
{
    private static readonly object _lock = new();
    private static AsyncCircuitBreakerPolicy? _asyncPolicy;
    private static CircuitBreakerPolicy? _syncPolicy;

    private static CircuitBreakerPolicy GetSyncPolicy()
    {
        if (_syncPolicy != null) return _syncPolicy;
        lock (_lock)
        {
            _syncPolicy ??= Policy
                .Handle<Exception>()
                .CircuitBreaker(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));
            return _syncPolicy;
        }
    }

    private static AsyncCircuitBreakerPolicy GetAsyncPolicy()
    {
        if (_asyncPolicy != null) return _asyncPolicy;
        lock (_lock)
        {
            _asyncPolicy ??= Policy
                .Handle<Exception>()
                .CircuitBreakerAsync(handledEventsAllowedBeforeBreaking: 5, durationOfBreak: TimeSpan.FromSeconds(30));
            return _asyncPolicy;
        }
    }

    public static void ExecuteWithCircuitBreaker(Action action)
        => GetSyncPolicy().Execute(action);

    public static Task ExecuteWithCircuitBreakerAsync(Func<Task> action)
        => GetAsyncPolicy().ExecuteAsync(action);
}

/// <summary>
/// Retry helpers using Polly with jittered exponential backoff.
/// </summary>
public static class RetryPolicy
{
    private static AsyncRetryPolicy? _asyncRetry;
    private static RetryPolicy? _syncRetry;

    private static TimeSpan ComputeDelay(int attempt)
    {
        var baseDelay = TimeSpan.FromMilliseconds(200);
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 200));
        var backoff = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
        var maxDelay = TimeSpan.FromSeconds(10);
        return (backoff + jitter) > maxDelay ? maxDelay : (backoff + jitter);
    }

    private static RetryPolicy GetSyncPolicy(int maxRetries)
        => Policy.Handle<Exception>().WaitAndRetry(maxRetries, attempt => ComputeDelay(attempt));

    private static AsyncRetryPolicy GetAsyncPolicy(int maxRetries)
        => Policy.Handle<Exception>().WaitAndRetryAsync(maxRetries, attempt => Task.FromResult(ComputeDelay(attempt)));

    public static void ExecuteWithRetry(Action action, int maxRetries = 3)
        => GetSyncPolicy(maxRetries).Execute(action);

    public static Task ExecuteWithRetryAsync(Func<Task> action, int maxRetries = 3)
        => GetAsyncPolicy(maxRetries).ExecuteAsync(action);
}

/// <summary>
/// Simple health checker for HTTP endpoints.
/// </summary>
public static class HealthChecker
{
    public static bool CheckHealth(string endpoint)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = http.GetAsync(endpoint).GetAwaiter().GetResult();
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public static async Task<bool> CheckHealthAsync(string endpoint, CancellationToken cancellationToken = default)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            using var resp = await http.GetAsync(endpoint, cancellationToken).ConfigureAwait(false);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }
}
