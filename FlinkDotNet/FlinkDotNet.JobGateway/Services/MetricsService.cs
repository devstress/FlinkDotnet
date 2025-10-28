using Prometheus;

namespace FlinkDotNet.JobGateway.Services;

/// <summary>
/// Service for collecting and exposing Prometheus metrics for the JobGateway.
/// Tracks job submissions, status, and request metrics.
/// </summary>
public class MetricsService
{
    /// <summary>
    /// Job-related metrics
    /// </summary>
    private readonly Counter _jobsSubmittedTotal;
    private readonly Gauge _jobsRunning;
    private readonly Counter _jobsSucceededTotal;
    private readonly Counter _jobsFailedTotal;

    /// <summary>
    /// Request-related metrics
    /// </summary>
    private readonly Counter _requestsTotal;
    private readonly Histogram _requestDuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsService"/> class.
    /// Configures Prometheus counters, gauges, and histograms for job and request tracking.
    /// </summary>
    public MetricsService()
    {
        // Job metrics
        this._jobsSubmittedTotal = Metrics.CreateCounter(
            "flinkdotnet_jobgateway_jobs_submitted_total",
            "Total number of jobs submitted to the gateway",
            new CounterConfiguration
            {
                LabelNames = ["mode"] // LOCAL or REMOTE
            });

        this._jobsRunning = Metrics.CreateGauge(
            "flinkdotnet_jobgateway_jobs_running",
            "Current number of running jobs tracked by the gateway");

        this._jobsSucceededTotal = Metrics.CreateCounter(
            "flinkdotnet_jobgateway_jobs_succeeded_total",
            "Total number of successfully completed jobs");

        this._jobsFailedTotal = Metrics.CreateCounter(
            "flinkdotnet_jobgateway_jobs_failed_total",
            "Total number of failed jobs",
            new CounterConfiguration
            {
                LabelNames = ["error_type"]
            });

        // Request metrics
        this._requestsTotal = Metrics.CreateCounter(
            "flinkdotnet_jobgateway_requests_total",
            "Total number of API requests",
            new CounterConfiguration
            {
                LabelNames = ["endpoint", "method", "status_code"]
            });

        this._requestDuration = Metrics.CreateHistogram(
            "flinkdotnet_jobgateway_request_duration_seconds",
            "Duration of API requests in seconds",
            new HistogramConfiguration
            {
                LabelNames = ["endpoint", "method"],
                Buckets = Histogram.ExponentialBuckets(0.001, 2, 10) // 1ms to ~1s
            });
    }

    /// <summary>
    /// Records a job submission and increments the running jobs counter.
    /// </summary>
    /// <param name="mode">The submission mode (LOCAL or REMOTE).</param>
    public void RecordJobSubmitted(string mode)
    {
        this._jobsSubmittedTotal.WithLabels(mode).Inc();
        this._jobsRunning.Inc();
    }

    /// <summary>
    /// Records a successful job completion and decrements the running jobs counter.
    /// </summary>
    public void RecordJobSucceeded()
    {
        this._jobsSucceededTotal.Inc();
        this._jobsRunning.Dec();
    }

    /// <summary>
    /// Records a job failure and decrements the running jobs counter.
    /// </summary>
    /// <param name="errorType">The type or category of the error that caused the failure.</param>
    public void RecordJobFailed(string errorType)
    {
        this._jobsFailedTotal.WithLabels(errorType).Inc();
        this._jobsRunning.Dec();
    }

    /// <summary>
    /// Records an API request with endpoint, method, and status code.
    /// </summary>
    /// <param name="endpoint">The API endpoint path.</param>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <param name="statusCode">The HTTP status code returned.</param>
    public void RecordRequest(string endpoint, string method, int statusCode) => this._requestsTotal.WithLabels(endpoint, method, statusCode.ToString()).Inc();

    /// <summary>
    /// Creates a timer to measure the duration of an API request.
    /// </summary>
    /// <param name="endpoint">The API endpoint path.</param>
    /// <param name="method">The HTTP method (GET, POST, etc.).</param>
    /// <returns>A disposable timer that records the duration when disposed.</returns>
    public IDisposable MeasureRequestDuration(string endpoint, string method) => this._requestDuration.WithLabels(endpoint, method).NewTimer();
}
