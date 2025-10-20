using Prometheus;

namespace FlinkDotNet.JobGateway.Services;

/// <summary>
/// Service for collecting and exposing Prometheus metrics for the JobGateway.
/// Tracks job submissions, status, and request metrics.
/// </summary>
public class MetricsService
{
    // Job-related metrics
    private readonly Counter _jobsSubmittedTotal;
    private readonly Gauge _jobsRunning;
    private readonly Counter _jobsSucceededTotal;
    private readonly Counter _jobsFailedTotal;
    
    // Request-related metrics
    private readonly Counter _requestsTotal;
    private readonly Histogram _requestDuration;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsService"/> class.
    /// Configures Prometheus counters, gauges, and histograms for job and request tracking.
    /// </summary>
    public MetricsService()
    {
        // Job metrics
        _jobsSubmittedTotal = Metrics.CreateCounter(
            "flinkdotnet_gateway_jobs_submitted_total",
            "Total number of jobs submitted to the gateway",
            new CounterConfiguration
            {
                LabelNames = new[] { "mode" } // LOCAL or REMOTE
            });
        
        _jobsRunning = Metrics.CreateGauge(
            "flinkdotnet_gateway_jobs_running",
            "Current number of running jobs tracked by the gateway");
        
        _jobsSucceededTotal = Metrics.CreateCounter(
            "flinkdotnet_gateway_jobs_succeeded_total",
            "Total number of successfully completed jobs");
        
        _jobsFailedTotal = Metrics.CreateCounter(
            "flinkdotnet_gateway_jobs_failed_total",
            "Total number of failed jobs",
            new CounterConfiguration
            {
                LabelNames = new[] { "error_type" }
            });
        
        // Request metrics
        _requestsTotal = Metrics.CreateCounter(
            "flinkdotnet_gateway_requests_total",
            "Total number of API requests",
            new CounterConfiguration
            {
                LabelNames = new[] { "endpoint", "method", "status_code" }
            });
        
        _requestDuration = Metrics.CreateHistogram(
            "flinkdotnet_gateway_request_duration_seconds",
            "Duration of API requests in seconds",
            new HistogramConfiguration
            {
                LabelNames = new[] { "endpoint", "method" },
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
            });
    }
    
    /// <summary>
    /// Records a job submission and increments the running jobs counter.
    /// </summary>
    /// <param name="mode">The submission mode (LOCAL or REMOTE).</param>
    public void RecordJobSubmitted(string mode)
    {
        _jobsSubmittedTotal.WithLabels(mode).Inc();
        _jobsRunning.Inc();
    }
    
    /// <summary>
    /// Records a successful job completion and decrements the running jobs counter.
    /// </summary>
    public void RecordJobSucceeded()
    {
        _jobsSucceededTotal.Inc();
        _jobsRunning.Dec();
    }
    
    /// <summary>
    /// Records a job failure and decrements the running jobs counter.
    /// </summary>
    /// <param name="errorType">The type or category of the error that caused the failure.</param>
    public void RecordJobFailed(string errorType)
    {
        _jobsFailedTotal.WithLabels(errorType).Inc();
        _jobsRunning.Dec();
    }
    
    /// <summary>
    /// Records an API request with endpoint, method, and status code.
    /// </summary>
    /// <param name="endpoint">The API endpoint path.</param>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    public void RecordRequest(string endpoint, string method, int statusCode)
    {
        _requestsTotal.WithLabels(endpoint, method, statusCode.ToString()).Inc();
    }
    
    /// <summary>
    /// Creates a timer to measure the duration of an API request.
    /// </summary>
    /// <param name="endpoint">The API endpoint path.</param>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <returns>A disposable timer that records the duration when disposed.</returns>
    public IDisposable MeasureRequestDuration(string endpoint, string method)
    {
        return _requestDuration.WithLabels(endpoint, method).NewTimer();
    }
}