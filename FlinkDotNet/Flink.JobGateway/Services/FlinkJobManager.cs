using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;

namespace Flink.JobGateway.Services;

/// <summary>
/// Implementation of Flink Job Manager that integrates with real Apache Flink 2.0 cluster
/// Uses Flink REST API to submit, monitor, and manage jobs
/// </summary>
public class FlinkJobManager : IFlinkJobManager
{
    private readonly ILogger<FlinkJobManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, JobInfo> _jobMapping = new();
    private readonly string _flinkClusterHost;
    private readonly int _flinkClusterPort;

    public FlinkJobManager(ILogger<FlinkJobManager> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Get Flink cluster configuration from environment or use defaults
        _flinkClusterHost = Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST") ?? "flink-jobmanager";
        _flinkClusterPort = int.Parse(Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT") ?? "8081");
        
        // Configure HTTP client for Flink REST API
        var flinkBaseUrl = $"http://{_flinkClusterHost}:{_flinkClusterPort}";
        _httpClient.BaseAddress = new Uri(flinkBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        
        _logger.LogInformation("Flink Job Gateway configured for real Flink 2.0 cluster at: {FlinkBaseUrl}", flinkBaseUrl);
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        _logger.LogInformation("Submitting job to real Flink 2.0 cluster: {JobId}", jobDefinition.Metadata.JobId);

        try
        {
            // Validate job definition
            var validationResult = ValidateJobDefinition(jobDefinition);
            if (!validationResult.IsValid)
            {
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, 
                    $"Job validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            // Check Flink cluster health before submission
            var isHealthy = await CheckFlinkClusterHealthAsync();
            if (!isHealthy)
            {
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, 
                    "Flink cluster is not available or unhealthy");
            }

            // Convert job definition to Flink DataStream program
            var flinkProgram = ConvertToFlinkProgram(jobDefinition);
            
            // Submit job via Flink REST API
            var flinkJobId = await SubmitJobToFlinkClusterAsync(flinkProgram, jobDefinition);
            
            // Store job mapping for tracking
            _jobMapping[flinkJobId] = new JobInfo
            {
                JobId = jobDefinition.Metadata.JobId,
                FlinkJobId = flinkJobId,
                Status = "RUNNING",
                SubmissionTime = DateTime.UtcNow,
                JobDefinition = jobDefinition
            };

            _logger.LogInformation("Job submitted successfully to Flink 2.0 cluster: {JobId} -> {FlinkJobId}", 
                jobDefinition.Metadata.JobId, flinkJobId);

            return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job to Flink 2.0 cluster: {JobId}", jobDefinition.Metadata.JobId);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, ex.Message);
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string flinkJobId)
    {
        _logger.LogDebug("Getting status from Flink 2.0 cluster for job: {FlinkJobId}", flinkJobId);

        try
        {
            // Query actual Flink cluster for job status via REST API
            var response = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var flinkJobInfo = JsonSerializer.Deserialize<FlinkJobStatusResponse>(jsonResponse);
                
                var jobMapping = _jobMapping.TryGetValue(flinkJobId, out var jobInfo) ? jobInfo : null;
                
                return new JobStatus
                {
                    JobId = jobMapping?.JobId ?? flinkJobId,
                    FlinkJobId = flinkJobId,
                    State = flinkJobInfo?.State ?? "UNKNOWN",
                    StartTime = flinkJobInfo?.StartTime ?? DateTime.UtcNow,
                    EndTime = flinkJobInfo?.EndTime
                };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Job not found in Flink cluster: {FlinkJobId}", flinkJobId);
                return null;
            }
            else
            {
                _logger.LogError("Error querying Flink cluster for job status: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Flink 2.0 cluster for job status: {FlinkJobId}", flinkJobId);
            return null;
        }
    }

    public async Task<JobMetrics?> GetJobMetricsAsync(string flinkJobId)
    {
        _logger.LogDebug("Getting metrics from Flink 2.0 cluster for job: {FlinkJobId}", flinkJobId);

        try
        {
            // Query actual Flink cluster for job metrics via REST API
            var response = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/metrics");
            
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var flinkMetrics = JsonSerializer.Deserialize<FlinkJobMetricsResponse>(jsonResponse);
                
                return new JobMetrics
                {
                    FlinkJobId = flinkJobId,
                    Runtime = TimeSpan.FromMinutes(1), // Default runtime
                    RecordsIn = flinkMetrics?.RecordsIn ?? 0,
                    RecordsOut = flinkMetrics?.RecordsOut ?? 0,
                    Parallelism = flinkMetrics?.Parallelism ?? 1,
                    Checkpoints = flinkMetrics?.Checkpoints ?? 0,
                    LastCheckpoint = flinkMetrics?.LastCheckpoint ?? DateTime.UtcNow
                };
            }
            else
            {
                _logger.LogWarning("Could not retrieve metrics from Flink cluster: {StatusCode}", response.StatusCode);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Flink 2.0 cluster for job metrics: {FlinkJobId}", flinkJobId);
            return null;
        }
    }

    public async Task<bool> CancelJobAsync(string flinkJobId)
    {
        _logger.LogInformation("Canceling job in Flink 2.0 cluster: {FlinkJobId}", flinkJobId);

        try
        {
            // Cancel job via Flink REST API
            var response = await _httpClient.PostAsync($"/v1/jobs/{flinkJobId}/cancel", null);
            
            if (response.IsSuccessStatusCode)
            {
                // Update local tracking
                if (_jobMapping.TryGetValue(flinkJobId, out var jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                
                _logger.LogInformation("Job canceled successfully in Flink cluster: {FlinkJobId}", flinkJobId);
                return true;
            }
            else
            {
                _logger.LogWarning("Failed to cancel job in Flink cluster: {FlinkJobId}, Status: {StatusCode}", 
                    flinkJobId, response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job in Flink 2.0 cluster: {FlinkJobId}", flinkJobId);
            return false;
        }
    }

    private async Task<bool> CheckFlinkClusterHealthAsync()
    {
        try
        {
            _logger.LogDebug("Checking Flink 2.0 cluster health at {Host}:{Port}", _flinkClusterHost, _flinkClusterPort);
            
            var response = await _httpClient.GetAsync("/v1/overview");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("Flink cluster health check successful: {Content}", content);
                return true;
            }
            else
            {
                _logger.LogWarning("Flink cluster health check failed: {StatusCode}", response.StatusCode);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Flink cluster health check failed");
            return false;
        }
    }

    private async Task<string> SubmitJobToFlinkClusterAsync(string flinkProgram, JobDefinition jobDefinition)
    {
        try
        {
            // Create job submission request
            var jobRequest = new
            {
                entryClass = "com.flink.jobgateway.FlinkJobRunner",
                programArgs = new[] { flinkProgram },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                savepointPath = (string?)null,
                allowNonRestoredState = false
            };

            var json = JsonSerializer.Serialize(jobRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Submitting job to Flink cluster with payload: {Json}", json);

            var response = await _httpClient.PostAsync("/v1/jars/upload", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var submitResponse = JsonSerializer.Deserialize<FlinkJobSubmissionResponse>(responseContent);
                
                if (submitResponse?.JobId != null)
                {
                    return submitResponse.JobId;
                }
                else
                {
                    throw new InvalidOperationException("Flink cluster did not return a job ID");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Flink job submission failed: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job to Flink cluster");
            throw new InvalidOperationException($"Failed to submit job to Flink cluster: {ex.Message}", ex);
        }
    }

    private static string ConvertToFlinkProgram(JobDefinition jobDefinition)
    {
        // Convert the job definition IR to actual Flink DataStream program
        // This is a simplified implementation - in production this would be much more comprehensive
        
        var programBuilder = new StringBuilder();
        programBuilder.AppendLine("// Generated Flink DataStream program");
        programBuilder.AppendLine("StreamExecutionEnvironment env = StreamExecutionEnvironment.getExecutionEnvironment();");
        
        // Convert source
        ConvertSource(jobDefinition.Source, programBuilder);
        
        // Convert operations
        foreach (var operation in jobDefinition.Operations)
        {
            ConvertOperation(operation, programBuilder);
        }
        
        // Convert sink
        ConvertSink(jobDefinition.Sink, programBuilder);
        
        programBuilder.AppendLine("env.execute();");
        
        return programBuilder.ToString();
    }

    private static void ConvertSource(object source, StringBuilder programBuilder)
    {
        switch (source)
        {
            case KafkaSourceDefinition kafkaSource:
                programBuilder.AppendLine($"DataStream<String> stream = env.addSource(new FlinkKafkaConsumer<>(\"{kafkaSource.Topic}\", new SimpleStringSchema(), props));");
                break;
            case FileSourceDefinition fileSource:
                programBuilder.AppendLine($"DataStream<String> stream = env.readTextFile(\"{fileSource.Path}\");");
                break;
            default:
                programBuilder.AppendLine("DataStream<String> stream = env.fromElements(\"sample\");");
                break;
        }
    }

    private static void ConvertOperation(IOperationDefinition operation, StringBuilder programBuilder)
    {
        switch (operation.Type.ToLowerInvariant())
        {
            case "filter":
                var filterOp = (FilterOperationDefinition)operation;
                programBuilder.AppendLine($"stream = stream.filter(x -> {filterOp.Expression});");
                break;
            case "map":
                var mapOp = (MapOperationDefinition)operation;
                programBuilder.AppendLine($"stream = stream.map(x -> {mapOp.Expression});");
                break;
            case "groupby":
                var groupOp = (GroupByOperationDefinition)operation;
                programBuilder.AppendLine($"KeyedStream<String, String> keyedStream = stream.keyBy(x -> {groupOp.Key});");
                break;
            case "aggregate":
                var aggOp = (AggregateOperationDefinition)operation;
                programBuilder.AppendLine($"stream = keyedStream.reduce((a, b) -> {aggOp.AggregationType}(a, b));");
                break;
        }
    }

    private static void ConvertSink(object sink, StringBuilder programBuilder)
    {
        switch (sink)
        {
            case KafkaSinkDefinition kafkaSink:
                programBuilder.AppendLine($"stream.addSink(new FlinkKafkaProducer<>(\"{kafkaSink.Topic}\", new SimpleStringSchema(), props));");
                break;
            case FileSinkDefinition fileSink:
                programBuilder.AppendLine($"stream.writeAsText(\"{fileSink.Path}\");");
                break;
            case ConsoleSinkDefinition:
                programBuilder.AppendLine("stream.print();");
                break;
        }
    }

    private JobValidationResult ValidateJobDefinition(JobDefinition jobDefinition)
    {
        var errors = new List<string>();

        ValidateBasicProperties(jobDefinition, errors);
        ValidateSource(jobDefinition.Source, errors);
        ValidateSink(jobDefinition.Sink, errors);

        return new JobValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }

    private static void ValidateBasicProperties(JobDefinition jobDefinition, List<string> errors)
    {
        if (jobDefinition.Metadata == null)
        {
            errors.Add("Job metadata is required");
        }
        else if (string.IsNullOrEmpty(jobDefinition.Metadata.JobId))
        {
            errors.Add("Job ID is required");
        }

        if (jobDefinition.Source == null)
            errors.Add("Job source is required");

        if (jobDefinition.Sink == null)
            errors.Add("Job sink is required");
    }

    private static void ValidateSource(object? source, List<string> errors)
    {
        if (source == null) return;

        switch (source)
        {
            case KafkaSourceDefinition kafkaSource:
                if (string.IsNullOrEmpty(kafkaSource.Topic))
                    errors.Add("Kafka source must specify a topic");
                break;
            case FileSourceDefinition fileSource:
                if (string.IsNullOrEmpty(fileSource.Path))
                    errors.Add("File source must specify a path");
                break;
        }
    }

    private static void ValidateSink(object? sink, List<string> errors)
    {
        if (sink == null) return;

        switch (sink)
        {
            case KafkaSinkDefinition kafkaSink:
                if (string.IsNullOrEmpty(kafkaSink.Topic))
                    errors.Add("Kafka sink must specify a topic");
                break;
            case FileSinkDefinition fileSink:
                if (string.IsNullOrEmpty(fileSink.Path))
                    errors.Add("File sink must specify a path");
                break;
        }
    }

    private sealed class JobInfo
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public JobDefinition JobDefinition { get; set; } = null!;
    }

    private sealed class JobValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    // Flink REST API response models
    private sealed class FlinkJobSubmissionResponse
    {
        public string JobId { get; set; } = string.Empty;
    }

    private sealed class FlinkJobStatusResponse
    {
        public string State { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.UtcNow;
        public DateTime? EndTime { get; set; } = null;
    }

    private sealed class FlinkJobMetricsResponse
    {
        public long RecordsIn { get; set; } = 0;
        public long RecordsOut { get; set; } = 0;
        public int Parallelism { get; set; } = 1;
        public int Checkpoints { get; set; } = 0;
        public DateTime? LastCheckpoint { get; set; } = null;
    }
}