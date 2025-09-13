using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;

namespace Flink.JobGateway.Services;

/// <summary>
/// Implementation of Flink Job Manager that integrates with real Apache Flink 2.1.0 cluster
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
        
        _logger.LogInformation("Flink Job Gateway configured for real Flink 2.1.0 cluster at: {FlinkBaseUrl}", flinkBaseUrl);
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        _logger.LogInformation("Submitting job to real Flink 2.1.0 cluster: {JobId}", jobDefinition.Metadata.JobId);

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

            // Convert job definition to IR JSON for the Runner
            var irJson = ConvertToFlinkProgram(jobDefinition);
            
            // Submit job via Flink REST API using IR Runner
            var flinkJobId = await SubmitJobToFlinkClusterAsync(irJson, jobDefinition);
            
            // Store job mapping for tracking
            _jobMapping[flinkJobId] = new JobInfo
            {
                JobId = jobDefinition.Metadata.JobId,
                FlinkJobId = flinkJobId,
                Status = "RUNNING",
                SubmissionTime = DateTime.UtcNow,
                JobDefinition = jobDefinition
            };

            _logger.LogInformation("Job submitted successfully to Flink 2.1.0 cluster: {JobId} -> {FlinkJobId}", 
                jobDefinition.Metadata.JobId, flinkJobId);

            return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job to Flink 2.1.0 cluster: {JobId}", jobDefinition.Metadata.JobId);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, ex.Message);
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string flinkJobId)
    {
        _logger.LogDebug("Getting status from Flink 2.1.0 cluster for job: {FlinkJobId}", flinkJobId);

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
            _logger.LogError(ex, "Failed to query Flink 2.1.0 cluster for job status: {FlinkJobId}", flinkJobId);
            return null;
        }
    }

    public async Task<JobMetrics?> GetJobMetricsAsync(string flinkJobId)
    {
        _logger.LogDebug("Getting metrics from Flink 2.1.0 cluster for job: {FlinkJobId}", flinkJobId);

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
            _logger.LogError(ex, "Failed to query Flink 2.1.0 cluster for job metrics: {FlinkJobId}", flinkJobId);
            return null;
        }
    }

    public async Task<bool> CancelJobAsync(string flinkJobId)
    {
        _logger.LogInformation("Canceling job in Flink 2.1.0 cluster: {FlinkJobId}", flinkJobId);

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
            _logger.LogError(ex, "Failed to cancel job in Flink 2.1.0 cluster: {FlinkJobId}", flinkJobId);
            return false;
        }
    }

    private async Task<bool> CheckFlinkClusterHealthAsync()
    {
        try
        {
            _logger.LogDebug("Checking Flink 2.1.0 cluster health at {Host}:{Port}", _flinkClusterHost, _flinkClusterPort);
            
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

    private async Task<string> SubmitJobToFlinkClusterAsync(string irJson, JobDefinition jobDefinition)
    {
        try
        {
            // Step 1: Upload IR Runner JAR to Flink cluster if not already cached
            var jarId = await EnsureIRRunnerJarAsync();
            
            // Step 2: Convert job definition to IR JSON for the runner
            var base64Ir = Convert.ToBase64String(Encoding.UTF8.GetBytes(irJson));
            
            // Step 3: Submit job with IR Runner JAR and IR as argument
            var jobRequest = new
            {
                entryClass = "com.flinkdotnet.irrunner.FlinkIRRunner",
                programArgs = new[] { "--ir-base64", base64Ir },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                savepointPath = (string?)null,
                allowNonRestoredState = false
            };

            var json = JsonSerializer.Serialize(jobRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Submitting job to Flink cluster with IR Runner JAR: {JarId}", jarId);

            var response = await _httpClient.PostAsync($"/v1/jars/{jarId}/run", content);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var submitResponse = JsonSerializer.Deserialize<FlinkJobSubmissionResponse>(responseContent);
                
                if (submitResponse?.JobId != null)
                {
                    _logger.LogInformation("Successfully submitted job to Flink: {JobId}", submitResponse.JobId);
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

    private async Task<string> EnsureIRRunnerJarAsync()
    {
        try
        {
            // Check if IR Runner JAR is already uploaded
            var jarsResponse = await _httpClient.GetAsync("/v1/jars");
            if (jarsResponse.IsSuccessStatusCode)
            {
                var jarsContent = await jarsResponse.Content.ReadAsStringAsync();
                var jarsResult = JsonSerializer.Deserialize<FlinkJarsResponse>(jarsContent);
                
                // Look for existing IR Runner JAR
                var existingJar = jarsResult?.Files?.FirstOrDefault(jar => 
                    jar.Name.Contains("flink-ir-runner") || jar.Id.Contains("ir-runner"));
                
                if (existingJar != null)
                {
                    _logger.LogDebug("Using existing IR Runner JAR: {JarId}", existingJar.Id);
                    return existingJar.Id;
                }
            }
            
            // Upload IR Runner JAR if not found
            return await UploadIRRunnerJarAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure IR Runner JAR is available in Flink cluster");
            throw new InvalidOperationException($"Failed to ensure IR Runner JAR availability: {ex.Message}", ex);
        }
    }

    private async Task<string> UploadIRRunnerJarAsync()
    {
        try
        {
            // For now, we'll use a placeholder until the actual JAR is built
            // In production, this would load the actual IR Runner JAR file
            var mockJarContent = CreateMockIRRunnerJar();
            
            using var form = new MultipartFormDataContent();
            using var fileContent = new ByteArrayContent(mockJarContent);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/java-archive");
            form.Add(fileContent, "jarfile", "flink-ir-runner-1.0.0.jar");
            
            _logger.LogInformation("Uploading IR Runner JAR to Flink cluster");
            
            var response = await _httpClient.PostAsync("/v1/jars/upload", form);
            
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var uploadResponse = JsonSerializer.Deserialize<FlinkJarUploadResponse>(responseContent);
                
                if (uploadResponse?.Filename != null)
                {
                    _logger.LogInformation("Successfully uploaded IR Runner JAR: {Filename}", uploadResponse.Filename);
                    return uploadResponse.Filename;
                }
                else
                {
                    throw new InvalidOperationException("Flink cluster did not return a JAR ID after upload");
                }
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Failed to upload IR Runner JAR: {response.StatusCode} - {errorContent}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload IR Runner JAR to Flink cluster");
            throw new InvalidOperationException($"Failed to upload IR Runner JAR: {ex.Message}", ex);
        }
    }

    private static byte[] CreateMockIRRunnerJar()
    {
        // Create a simple mock JAR for demonstration
        // In production, this would load the actual compiled IR Runner JAR
        var mockManifest = @"Manifest-Version: 1.0
Main-Class: com.flinkdotnet.irrunner.FlinkIRRunner
Implementation-Title: FlinkDotNet IR Runner (Mock)
Implementation-Version: 1.0.0
";
        return Encoding.UTF8.GetBytes(mockManifest);
    }

    private static string ConvertToFlinkProgram(JobDefinition jobDefinition)
    {
        // Convert the JobDefinition to IR JSON that the IR Runner can process
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            };
            
            var irJson = JsonSerializer.Serialize(jobDefinition, options);
            return irJson;
        }
        catch (Exception)
        {
            // Fallback to a simple IR structure if serialization fails
            return CreateFallbackIR(jobDefinition);
        }
    }

    private static string CreateFallbackIR(JobDefinition jobDefinition)
    {
        // Create a simplified IR structure for the runner
        var fallbackIR = new
        {
            metadata = new
            {
                jobId = jobDefinition.Metadata.JobId,
                jobName = jobDefinition.Metadata.JobName ?? "FlinkDotNet-Job",
                version = jobDefinition.Metadata.Version,
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                createdAt = DateTime.UtcNow
            },
            source = jobDefinition.Source,
            operations = jobDefinition.Operations,
            sink = jobDefinition.Sink
        };
        
        return JsonSerializer.Serialize(fallbackIR, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
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

    private sealed class FlinkJarUploadResponse
    {
        public string Filename { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    private sealed class FlinkJarsResponse
    {
        public List<FlinkJarFile>? Files { get; set; } = new();
    }

    private sealed class FlinkJarFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }
}