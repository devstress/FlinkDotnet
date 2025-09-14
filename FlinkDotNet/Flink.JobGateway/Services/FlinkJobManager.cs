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

            // Encode IR as base64
            var irJson = JsonSerializer.Serialize(jobDefinition, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var irBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(irJson));

            // Submit job via Flink REST API using IR Runner jar
            var flinkJobId = await SubmitJobToFlinkClusterAsync(irBase64, jobDefinition);
            
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
                using var doc = JsonDocument.Parse(jsonResponse);
                var root = doc.RootElement;
                var state = root.TryGetProperty("state", out var stateProp) ? stateProp.GetString() ?? "UNKNOWN" : "UNKNOWN";
                var jobMapping = _jobMapping.TryGetValue(flinkJobId, out var jobInfo) ? jobInfo : null;

                return new JobStatus
                {
                    JobId = jobMapping?.JobId ?? flinkJobId,
                    FlinkJobId = flinkJobId,
                    State = state
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
            // Aggregate vertex metrics
            long recordsIn = 0, recordsOut = 0;
            int parallelism = 0, checkpoints = 0;
            DateTime? lastCheckpoint = null;
            string backpressureLevel = "UNKNOWN";

            var verticesResp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices");
            if (!verticesResp.IsSuccessStatusCode)
            {
                _logger.LogWarning("Vertices lookup failed: {Status}", verticesResp.StatusCode);
                return new JobMetrics { FlinkJobId = flinkJobId };
            }
            var verticesJson = await verticesResp.Content.ReadAsStringAsync();
            using var vdoc = JsonDocument.Parse(verticesJson);
            if (!vdoc.RootElement.TryGetProperty("vertices", out var vertsEl) || vertsEl.ValueKind != JsonValueKind.Array)
                return new JobMetrics { FlinkJobId = flinkJobId };

            foreach (var v in vertsEl.EnumerateArray())
            {
                if (!v.TryGetProperty("id", out var idEl)) continue;
                var vid = idEl.GetString();
                if (string.IsNullOrEmpty(vid)) continue;
                var mresp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices/{vid}/metrics?get=numRecordsIn,numRecordsOut,parallelism");
                if (!mresp.IsSuccessStatusCode) continue;
                var marr = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync());
                foreach (var m in marr ?? new())
                {
                    if (m.Id.Equals("numRecordsIn", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vi)) recordsIn += vi;
                    if (m.Id.Equals("numRecordsOut", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vo)) recordsOut += vo;
                    if (m.Id.Equals("parallelism", StringComparison.OrdinalIgnoreCase) && int.TryParse(m.Value, out var p)) parallelism = Math.Max(parallelism, p);
                }

                // Backpressure level (best-effort)
                try
                {
                    var bp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices/{vid}/backpressure");
                    if (bp.IsSuccessStatusCode)
                    {
                        var bpStr = await bp.Content.ReadAsStringAsync();
                        using var bdoc = JsonDocument.Parse(bpStr);
                        var root = bdoc.RootElement;
                        string? lvl = null;
                        if (root.TryGetProperty("backpressureLevel", out var lvlEl)) lvl = lvlEl.GetString();
                        else if (root.TryGetProperty("backpressure-level", out var lvlEl2)) lvl = lvlEl2.GetString();
                        if (!string.IsNullOrEmpty(lvl))
                        {
                            // choose worst level across vertices
                            backpressureLevel = WorstBackpressure(backpressureLevel, lvl!);
                        }
                    }
                }
                catch { /* ignore */ }
            }

            // Checkpoints (best-effort)
            try
            {
                var cps = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/checkpoints");
                if (cps.IsSuccessStatusCode)
                {
                    var cpsJson = await cps.Content.ReadAsStringAsync();
                    using var cdoc = JsonDocument.Parse(cpsJson);
                    var root = cdoc.RootElement;
                    if (root.TryGetProperty("counts", out var counts) && counts.TryGetProperty("completed", out var completedEl) && completedEl.TryGetInt32(out var c))
                        checkpoints = c;
                    if (root.TryGetProperty("latest", out var latest))
                    {
                        // Try a few known timestamp fields
                        if (latest.TryGetProperty("completed", out var comp) && comp.TryGetProperty("end_time", out var endTime) && endTime.ValueKind == JsonValueKind.Number)
                        {
                            var ms = endTime.GetInt64();
                            lastCheckpoint = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                        }
                            else if (comp.TryGetProperty("trigger_timestamp", out var ts) && ts.ValueKind == JsonValueKind.Number)
                            {
                                var ms = ts.GetInt64();
                                lastCheckpoint = DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse checkpoints for job {FlinkJobId}", flinkJobId);
            }

            return new JobMetrics
            {
                FlinkJobId = flinkJobId,
                RecordsIn = recordsIn,
                RecordsOut = recordsOut,
                Parallelism = parallelism,
                Checkpoints = checkpoints,
                LastCheckpoint = lastCheckpoint,
                CustomMetrics = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    ["backpressureLevel"] = backpressureLevel
                }
            };
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

    private async Task<string> SubmitJobToFlinkClusterAsync(string irBase64, JobDefinition jobDefinition)
    {
        try
        {
            var jarId = await EnsureRunnerJarAsync();

            var runRequest = new
            {
                entryClass = "com.flink.jobgateway.FlinkJobRunner",
                programArgsList = new[] { "--irBase64", irBase64 },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1
            };

            var json = JsonSerializer.Serialize(runRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation("Running Flink IR Runner jar {JarId} with IR (base64 length={Length})", jarId, irBase64.Length);
            var response = await _httpClient.PostAsync($"/v1/jars/{jarId}/run", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Flink run failed: {response.StatusCode} - {err}");
            }

            var runContent = await response.Content.ReadAsStringAsync();
            var run = JsonSerializer.Deserialize<FlinkRunResponse>(runContent);
            if (string.IsNullOrEmpty(run?.JobId))
                throw new InvalidOperationException("Flink did not return a jobId");
            return run.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job to Flink cluster");
            throw new InvalidOperationException($"Failed to submit job to Flink cluster: {ex.Message}", ex);
        }
    }

    private async Task<string> EnsureRunnerJarAsync()
    {
        var jarPath = Environment.GetEnvironmentVariable("FLINK_RUNNER_JAR_PATH");
        if (string.IsNullOrEmpty(jarPath))
        {
            var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
            jarPath = repoRoot != null
                ? Path.Combine(repoRoot, "FlinkIRRunner", "target", "flink-ir-runner.jar")
                : Path.Combine(Environment.CurrentDirectory, "FlinkIRRunner", "target", "flink-ir-runner.jar");
        }

        if (!File.Exists(jarPath))
        {
            _logger.LogWarning("Runner jar not found at {Path}. Attempting to build via scripts/build_runner.ps1", jarPath);
            try
            {
                var repoRoot = FindRepoRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
                var buildScript = Path.Combine(repoRoot, "scripts", "build_runner.ps1");
                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "pwsh",
                    Arguments = $"-NoLogo -File \"{buildScript}\"",
                    WorkingDirectory = repoRoot,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
                using var proc = System.Diagnostics.Process.Start(psi)!;
                var stdOut = await proc.StandardOutput.ReadToEndAsync();
                var stdErr = await proc.StandardError.ReadToEndAsync();
                await proc.WaitForExitAsync();
                _logger.LogInformation("Runner build stdout: {Out}\nstderr: {Err}", stdOut, stdErr);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build runner jar automatically");
            }
        }

        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Runner jar not found at {jarPath}. Set FLINK_RUNNER_JAR_PATH env var.");
        }

        // Upload jar
        using var form = new MultipartFormDataContent();
        await using var fs = File.OpenRead(jarPath);
        var fileName = Path.GetFileName(jarPath);
        form.Add(new StreamContent(fs), "jarfile", fileName);
        var uploadResp = await _httpClient.PostAsync("/v1/jars/upload", form);
        if (!uploadResp.IsSuccessStatusCode)
        {
            var err = await uploadResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Jar upload failed: {uploadResp.StatusCode} - {err}");
        }

        // Find jarId by listing jars
        var listResp = await _httpClient.GetAsync("/v1/jars");
        listResp.EnsureSuccessStatusCode();
        var listJson = await listResp.Content.ReadAsStringAsync();
        var jars = JsonSerializer.Deserialize<FlinkJarsList>(listJson);
        var jar = jars?.Files?.OrderByDescending(f => f.Uploaded).FirstOrDefault(f => string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase));
        if (jar == null || string.IsNullOrEmpty(jar.Id))
            throw new InvalidOperationException("Uploaded jar not found in Flink jar list");
        return jar.Id;
    }

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var scripts = Path.Combine(dir.FullName, "scripts", "build_runner.ps1");
            var pom = Path.Combine(dir.FullName, "FlinkIRRunner", "pom.xml");
            if (File.Exists(scripts) && File.Exists(pom))
                return dir.FullName;
            dir = dir.Parent;
        }
        return null;
    }

    // Note: legacy placeholder converters removed; IR is executed by the Runner jar.

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
    private sealed class FlinkRunResponse { public string JobId { get; set; } = string.Empty; }

    // Removed unused response types from previous placeholder implementation.

    private sealed class FlinkJarsList
    {
        public List<FlinkJarFile> Files { get; set; } = new();
    }

    private sealed class FlinkJarFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public long Uploaded { get; init; }
    }

    private sealed class FlinkMetricEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
    }

    private static string WorstBackpressure(string current, string candidate)
    {
        static int Rank(string s) => s?.ToLowerInvariant() switch
        {
            "high" => 3,
            "ok" => 1,
            "low" => 2,
            "none" => 0,
            _ => 0
        };
        return Rank(candidate) >= Rank(current) ? candidate : current;
    }
}
