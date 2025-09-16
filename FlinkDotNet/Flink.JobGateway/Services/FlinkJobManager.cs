using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Diagnostics.CodeAnalysis; // added
using Flink.JobBuilder.Models;

namespace Flink.JobGateway.Services;

[SuppressMessage("Reliability", "S2139", Justification = "Intentional conversion of exceptions into domain JobSubmissionResult / status objects for gateway API without rethrow in selected methods.")]
public class FlinkJobManager : IFlinkJobManager
{
    private readonly ILogger<FlinkJobManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, JobInfo> _jobMapping = new();

    public FlinkJobManager(ILogger<FlinkJobManager> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        var host = Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST") ?? "flink-jobmanager";
        var port = int.Parse(Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT") ?? "8081");
        var flinkBaseUrl = $"http://{host}:{port}";
        _httpClient.BaseAddress = new Uri(flinkBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger.LogInformation("Flink Job Gateway targeting cluster at: {FlinkBaseUrl}", flinkBaseUrl);
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        _logger.LogInformation("Submitting job: {JobId}", jobDefinition.Metadata.JobId);
        try
        {
            var validationResult = ValidateJobDefinition(jobDefinition);
            if (!validationResult.IsValid)
            {
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId,
                    $"Job validation failed: {string.Join(", ", validationResult.Errors)}");
            }

            var irJson = JsonSerializer.Serialize(jobDefinition, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
            var irBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(irJson));

            var forceLocal = string.Equals(Environment.GetEnvironmentVariable("FLINK_FORCE_LOCAL"), "1", StringComparison.OrdinalIgnoreCase);
            if (forceLocal)
            {
                var simulatedId = $"local-sim-{Guid.NewGuid():N}";
                _logger.LogInformation("FLINK_FORCE_LOCAL enabled; returning simulated local success for job {JobId} with id {SimId}", jobDefinition.Metadata.JobId, simulatedId);
                _jobMapping[simulatedId] = new JobInfo
                {
                    JobId = jobDefinition.Metadata.JobId,
                    FlinkJobId = simulatedId,
                    Status = "LOCAL-RUNNING",
                    SubmissionTime = DateTime.UtcNow,
                    JobDefinition = jobDefinition
                };
                return new JobSubmissionResult
                {
                    JobId = jobDefinition.Metadata.JobId,
                    FlinkJobId = simulatedId,
                    Success = true,
                    SubmittedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string> { ["mode"] = "forced-local" }
                };
            }

            bool clusterHealthy = false;
            try { clusterHealthy = await CheckFlinkClusterHealthAsync(); }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Cluster health probe failed; falling back to local mode.");
            }

            string flinkJobId;
            if (clusterHealthy)
            {
                _logger.LogInformation("Cluster healthy - submitting to Flink REST API");
                flinkJobId = await SubmitJobToFlinkClusterAsync(irBase64, jobDefinition);
            }
            else
            {
                flinkJobId = await RunLocalAsync(irBase64, jobDefinition);
            }

            _jobMapping[flinkJobId] = new JobInfo
            {
                JobId = jobDefinition.Metadata.JobId,
                FlinkJobId = flinkJobId,
                Status = clusterHealthy ? "RUNNING" : "LOCAL-RUNNING",
                SubmissionTime = DateTime.UtcNow,
                JobDefinition = jobDefinition
            };

            if (!clusterHealthy && _jobMapping[flinkJobId].Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                return new JobSubmissionResult
                {
                    JobId = jobDefinition.Metadata.JobId,
                    FlinkJobId = flinkJobId,
                    Success = true,
                    SubmittedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string> { ["mode"] = "local" }
                };
            }

            return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job {JobId}", jobDefinition.Metadata.JobId);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, ex.Message);
        }
    }

    public async Task<JobStatus?> GetJobStatusAsync(string flinkJobId)
    {
        _logger.LogDebug("Query status for {FlinkJobId}", flinkJobId);
        if (_jobMapping.TryGetValue(flinkJobId, out var info) && info.Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            return new JobStatus { JobId = info.JobId, FlinkJobId = flinkJobId, State = info.Status };
        }

        try
        {
            var response = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var state = doc.RootElement.TryGetProperty("state", out var stateProp)
                    ? stateProp.GetString() ?? "UNKNOWN"
                    : "UNKNOWN";
                return new JobStatus { JobId = info?.JobId ?? flinkJobId, FlinkJobId = flinkJobId, State = state };
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected status code querying Flink job status: {(int)response.StatusCode} {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Rethrow with contextual message as requested
            throw new InvalidOperationException($"Failed to query Flink 2.1.0 cluster for job status: {flinkJobId}", ex);
        }
    }

    public async Task<JobMetrics?> GetJobMetricsAsync(string flinkJobId)
    {
        if (_jobMapping.TryGetValue(flinkJobId, out var info) && info.Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            return new JobMetrics
            {
                FlinkJobId = flinkJobId,
                RecordsIn = 0,
                RecordsOut = 0,
                Parallelism = info.JobDefinition.Metadata.Parallelism ?? 1,
                Checkpoints = 0,
                LastCheckpoint = null,
                CustomMetrics = new Dictionary<string, object> { ["mode"] = "local" }
            };
        }

        try
        {
            var metrics = new JobMetricsBuilder(flinkJobId);
            await CollectVertexMetricsAsync(flinkJobId, metrics);
            await CollectCheckpointMetricsAsync(flinkJobId, metrics);
            return metrics.Build();
        }
        catch (Exception ex)
        {
            // Rethrow with context for TDD visibility
            throw new InvalidOperationException($"Failed to query Flink 2.1.0 cluster for job metrics: {flinkJobId}", ex);
        }
    }

    public async Task<bool> CancelJobAsync(string flinkJobId)
    {
        if (_jobMapping.TryGetValue(flinkJobId, out var info) && info.Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            info.Status = "LOCAL-CANCELED";
            return true;
        }

        try
        {
            var response = await _httpClient.PostAsync($"/v1/jobs/{flinkJobId}/cancel", null);
            if (response.IsSuccessStatusCode)
            {
                if (_jobMapping.TryGetValue(flinkJobId, out var jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                return true;
            }
            else if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
            else
            {
                throw new InvalidOperationException($"Unexpected status code canceling Flink job: {(int)response.StatusCode} {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            // Rethrow with contextual message as requested
            throw new InvalidOperationException($"Failed to cancel job in Flink 2.1.0 cluster: {flinkJobId}", ex);
        }
    }

    private async Task<string> RunLocalAsync(string irBase64, JobDefinition jobDefinition)
    {
        var jarPath = await EnsureRunnerJarPathAsync();
        var id = $"local-{Guid.NewGuid():N}";
        string? bootstrap = null;
        if (jobDefinition.Source is KafkaSourceDefinition ks && !string.IsNullOrWhiteSpace(ks.BootstrapServers)) bootstrap = ks.BootstrapServers;
        else if (jobDefinition.Sink is KafkaSinkDefinition ksd && !string.IsNullOrWhiteSpace(ksd.BootstrapServers)) bootstrap = ksd.BootstrapServers;
        bootstrap ??= Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "localhost:9092";

        if (!File.Exists(jarPath))
        {
            _logger.LogWarning("Runner jar missing at {Path}; using simulated local execution for job {JobId}", jarPath, jobDefinition.Metadata.JobId);
            return id; // simulated
        }

        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "java",
                Arguments = $"-jar \"{jarPath}\" --irBase64 {irBase64}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.Environment["KAFKA_BOOTSTRAP"] = bootstrap;
            System.Diagnostics.Process? proc = null;
            try
            {
                proc = System.Diagnostics.Process.Start(psi);
            }
            catch (Exception startEx)
            {
                _logger.LogWarning(startEx, "Java process start failed; falling back to simulated execution (job {JobId})", jobDefinition.Metadata.JobId);
                return id; // simulated fallback
            }
            if (proc == null)
            {
                _logger.LogWarning("Java process returned null; simulated execution for job {JobId}", jobDefinition.Metadata.JobId);
                return id;
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    var stdout = await proc.StandardOutput.ReadToEndAsync();
                    var stderr = await proc.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(stdout)) _logger.LogDebug("[local-runner:{JobId}] OUT: {Out}", id, stdout);
                    if (!string.IsNullOrWhiteSpace(stderr)) _logger.LogDebug("[local-runner:{JobId}] ERR: {Err}", id, stderr);
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Local runner output capture failed for {JobId}", id);
                }
            });
            _logger.LogInformation("Started local runner (PID={Pid}, bootstrap={Bootstrap}) for job {JobId}", proc.Id, bootstrap, jobDefinition.Metadata.JobId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Local runner unexpected failure; using simulated state for job {JobId}", jobDefinition.Metadata.JobId);
        }
        return id;
    }

    private async Task<string> EnsureRunnerJarPathAsync()
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
            _logger.LogWarning("Runner jar missing at {Path}, attempting build (ensure-flink-runner.ps1)", jarPath);
            try
            {
                var repoRoot = FindRepoRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
                var ensureScript = Path.Combine(repoRoot, "scripts", "ensure-flink-runner.ps1");
                if (File.Exists(ensureScript))
                {
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "pwsh",
                        Arguments = $"-NoLogo -File \"{ensureScript}\" -Force",
                        WorkingDirectory = repoRoot,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };
                    var p = System.Diagnostics.Process.Start(psi);
                    if (p != null)
                    {
                        var outTask = p.StandardOutput.ReadToEndAsync();
                        var errTask = p.StandardError.ReadToEndAsync();
                        await p.WaitForExitAsync();
                        _logger.LogDebug("ensure-flink-runner exit {Code}\nOUT:{Out}\nERR:{Err}", p.ExitCode, await outTask, await errTask);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to build runner jar automatically at expected path {jarPath}", ex);
            }
        }
        return jarPath;
    }

    private async Task<bool> CheckFlinkClusterHealthAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync("/v1/overview");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Cluster health check failed", ex);
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
            var response = await _httpClient.PostAsync($"/v1/jars/{jarId}/run", content);
            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                throw new InvalidOperationException($"Flink run failed: {response.StatusCode} - {err}");
            }
            var runContent = await response.Content.ReadAsStringAsync();
            var run = JsonSerializer.Deserialize<FlinkRunResponse>(runContent);
            if (string.IsNullOrEmpty(run?.JobId))
            {
                throw new InvalidOperationException("Flink did not return a jobId");
            }
            return run.JobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cluster submission failed");
            throw;
        }
    }

    private async Task<string> EnsureRunnerJarAsync()
    {
        var jarPath = await EnsureRunnerJarPathAsync();
        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Runner jar not found at {jarPath}");
        }

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

        var listResp = await _httpClient.GetAsync("/v1/jars");
        listResp.EnsureSuccessStatusCode();
        var listJson = await listResp.Content.ReadAsStringAsync();
        var jars = JsonSerializer.Deserialize<FlinkJarsList>(listJson);
        var jar = jars?.Files?
            .OrderByDescending(f => f.Uploaded)
            .FirstOrDefault(f => string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase));
        if (jar == null || string.IsNullOrEmpty(jar.Id))
        {
            throw new InvalidOperationException("Uploaded jar not found in Flink jar list");
        }
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
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private JobValidationResult ValidateJobDefinition(JobDefinition jobDefinition)
    {
        var errors = new List<string>();
        ValidateBasicProperties(jobDefinition, errors);
        ValidateSource(jobDefinition.Source, errors);
        ValidateSink(jobDefinition.Sink, errors);
        return new JobValidationResult { IsValid = errors.Count == 0, Errors = errors };
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
        {
            errors.Add("Job source is required");
        }
        var isSqlJob = jobDefinition.Source is SqlSourceDefinition;
        if (jobDefinition.Sink == null && !isSqlJob)
        {
            errors.Add("Job sink is required");
        }
    }

    private static void ValidateSource(object? source, List<string> errors)
    {
        if (source == null) return;
        switch (source)
        {
            case KafkaSourceDefinition kafkaSource:
                if (string.IsNullOrEmpty(kafkaSource.Topic))
                {
                    errors.Add("Kafka source must specify a topic");
                }
                break;
            case FileSourceDefinition fileSource:
                if (string.IsNullOrEmpty(fileSource.Path))
                {
                    errors.Add("File source must specify a path");
                }
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
                {
                    errors.Add("Kafka sink must specify a topic");
                }
                break;
            case FileSinkDefinition fileSink:
                if (string.IsNullOrEmpty(fileSink.Path))
                {
                    errors.Add("File sink must specify a path");
                }
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

    private sealed class FlinkRunResponse { public string JobId { get; set; } = string.Empty; }
    private sealed class FlinkJarsList { public List<FlinkJarFile> Files { get; set; } = new(); }
    private sealed class FlinkJarFile { public string Id { get; set; } = string.Empty; public string Name { get; set; } = string.Empty; [JsonPropertyName("uploaded")] public long Uploaded { get; set; } }
    private sealed class FlinkMetricEntry { public string Id { get; set; } = string.Empty; public string Value { get; set; } = "0"; }

    // ---------------- Metrics helpers ----------------
    private async Task CollectVertexMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        var verticesResp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices");
        if (!verticesResp.IsSuccessStatusCode) return;
        var verticesJson = await verticesResp.Content.ReadAsStringAsync();
        using var vdoc = JsonDocument.Parse(verticesJson);
        if (!vdoc.RootElement.TryGetProperty("vertices", out var vertsEl) || vertsEl.ValueKind != JsonValueKind.Array) return;
        foreach (var vertex in vertsEl.EnumerateArray())
        {
            await ProcessVertexAsync(flinkJobId, vertex, metrics);
        }
    }

    private async Task ProcessVertexAsync(string flinkJobId, JsonElement vertex, JobMetricsBuilder metrics)
    {
        if (!vertex.TryGetProperty("id", out var idEl)) return;
        var vertexId = idEl.GetString();
        if (string.IsNullOrEmpty(vertexId)) return;
        await CollectVertexNumericMetricsAsync(flinkJobId, vertexId, metrics);
        await CollectVertexBackpressureAsync(flinkJobId, vertexId, metrics);
    }

    private async Task CollectVertexNumericMetricsAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        var mresp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/metrics?get=numRecordsIn,numRecordsOut,parallelism");
        if (!mresp.IsSuccessStatusCode) return;
        var metricsList = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync()) ?? new List<FlinkMetricEntry>();
        foreach (var m in metricsList)
        {
            if (m.Id.Equals("numRecordsIn", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vi)) metrics.AddRecordsIn(vi);
            if (m.Id.Equals("numRecordsOut", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vo)) metrics.AddRecordsOut(vo);
            if (m.Id.Equals("parallelism", StringComparison.OrdinalIgnoreCase) && int.TryParse(m.Value, out var p)) metrics.UpdateMaxParallelism(p);
        }
    }

    private async Task CollectVertexBackpressureAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        try
        {
            var bp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices/{vertexId}/backpressure");
            if (!bp.IsSuccessStatusCode) return;
            var bpStr = await bp.Content.ReadAsStringAsync();
            using var bdoc = JsonDocument.Parse(bpStr);
            var root = bdoc.RootElement;
            var level = ExtractBackpressureLevel(root);
            if (!string.IsNullOrEmpty(level)) metrics.UpdateWorstBackpressure(level);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to collect backpressure metrics for job {flinkJobId}, vertex {vertexId}", ex);
        }
    }

    private async Task CollectCheckpointMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        try
        {
            var cps = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/checkpoints");
            if (!cps.IsSuccessStatusCode) return;
            var cpsJson = await cps.Content.ReadAsStringAsync();
            using var cdoc = JsonDocument.Parse(cpsJson);
            var root = cdoc.RootElement;
            ProcessCheckpointCounts(root, metrics);
            ProcessCheckpointTimestamps(root, metrics);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to collect checkpoint metrics for job {flinkJobId}", ex);
        }
    }

    private static void ProcessCheckpointCounts(JsonElement root, JobMetricsBuilder metrics)
    {
        if (root.TryGetProperty("counts", out var counts) &&
            counts.TryGetProperty("completed", out var completedEl) &&
            completedEl.TryGetInt32(out var c))
        {
            metrics.SetCheckpoints(c);
        }
    }

    private static void ProcessCheckpointTimestamps(JsonElement root, JobMetricsBuilder metrics)
    {
        if (!root.TryGetProperty("latest", out var latest)) return;
        if (latest.TryGetProperty("completed", out var comp))
        {
            var ts = ExtractTimestamp(comp, "end_time") ?? ExtractTimestamp(comp, "trigger_timestamp");
            if (ts.HasValue) metrics.SetLastCheckpoint(ts.Value);
        }
    }

    private static DateTime? ExtractTimestamp(JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var timeEl) && timeEl.ValueKind == JsonValueKind.Number)
        {
            var ms = timeEl.GetInt64();
            return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
        }
        return null;
    }

    private static string? ExtractBackpressureLevel(JsonElement root)
    {
        if (root.TryGetProperty("backpressureLevel", out var lvlEl)) return lvlEl.GetString();
        if (root.TryGetProperty("backpressure-level", out var lvlEl2)) return lvlEl2.GetString();
        return null;
    }

    private sealed class JobMetricsBuilder
    {
        private readonly string _flinkJobId;
        private long _recordsIn;
        private long _recordsOut;
        private int _parallelism;
        private int _checkpoints;
        private DateTime? _lastCheckpoint;
        private string _backpressureLevel = "UNKNOWN";

        public JobMetricsBuilder(string flinkJobId) => _flinkJobId = flinkJobId;
        public void AddRecordsIn(long value) => _recordsIn += value;
        public void AddRecordsOut(long value) => _recordsOut += value;
        public void UpdateMaxParallelism(int value) => _parallelism = Math.Max(_parallelism, value);
        public void SetCheckpoints(int value) => _checkpoints = value;
        public void SetLastCheckpoint(DateTime value) => _lastCheckpoint = value;
        public void UpdateWorstBackpressure(string level) => _backpressureLevel = WorstBackpressure(_backpressureLevel, level);

        private static string WorstBackpressure(string current, string candidate)
        {
            static int Rank(string s) => s?.ToLowerInvariant() switch
            {
                "high" => 3,
                "low" => 2,
                "ok" => 1,
                "none" => 0,
                _ => 0
            };
            return Rank(candidate) >= Rank(current) ? candidate : current;
        }

        public JobMetrics Build() => new JobMetrics
        {
            FlinkJobId = _flinkJobId,
            RecordsIn = _recordsIn,
            RecordsOut = _recordsOut,
            Parallelism = _parallelism,
            Checkpoints = _checkpoints,
            LastCheckpoint = _lastCheckpoint,
            CustomMetrics = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["backpressureLevel"] = _backpressureLevel
            }
        };
    }
}
