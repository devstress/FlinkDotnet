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
    
    // Job tracking methods
    public void RecordJobSubmitted(string mode)
    {
        _jobsSubmittedTotal.WithLabels(mode).Inc();
        _jobsRunning.Inc();
    }
    
    public void RecordJobSucceeded()
    {
        _jobsSucceededTotal.Inc();
        _jobsRunning.Dec();
    }
    
    public void RecordJobFailed(string errorType)
    {
        _jobsFailedTotal.WithLabels(errorType).Inc();
        _jobsRunning.Dec();
    }
    
    // Request tracking methods
    public void RecordRequest(string endpoint, string method, int statusCode)
    {
        _requestsTotal.WithLabels(endpoint, method, statusCode.ToString()).Inc();
    }
    
    public IDisposable MeasureRequestDuration(string endpoint, string method)
    {
        return _requestDuration.WithLabels(endpoint, method).NewTimer();
    }
}