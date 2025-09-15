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
            var metrics = new JobMetricsBuilder(flinkJobId);
            
            await CollectVertexMetricsAsync(flinkJobId, metrics);
            await CollectCheckpointMetricsAsync(flinkJobId, metrics);
            
            return metrics.Build();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to query Flink 2.1.0 cluster for job metrics: {FlinkJobId}", flinkJobId);
            return null;
        }
    }

    private async Task CollectVertexMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        var verticesResp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices");
        if (!verticesResp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Vertices lookup failed: {Status}", verticesResp.StatusCode);
            return;
        }

        var verticesJson = await verticesResp.Content.ReadAsStringAsync();
        using var vdoc = JsonDocument.Parse(verticesJson);
        if (!vdoc.RootElement.TryGetProperty("vertices", out var vertsEl) || vertsEl.ValueKind != JsonValueKind.Array)
            return;

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

        var marr = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync());
        foreach (var m in marr ?? new())
        {
            if (m.Id.Equals("numRecordsIn", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vi))
                metrics.AddRecordsIn(vi);
            if (m.Id.Equals("numRecordsOut", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vo))
                metrics.AddRecordsOut(vo);
            if (m.Id.Equals("parallelism", StringComparison.OrdinalIgnoreCase) && int.TryParse(m.Value, out var p))
                metrics.UpdateMaxParallelism(p);
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
            
            string? level = ExtractBackpressureLevel(root);
            if (!string.IsNullOrEmpty(level))
                metrics.UpdateWorstBackpressure(level);
        }
        catch 
        { 
            // Backpressure collection is best-effort - failures are non-fatal
        }
    }

    private static string? ExtractBackpressureLevel(JsonElement root)
    {
        if (root.TryGetProperty("backpressureLevel", out var lvlEl)) 
            return lvlEl.GetString();
        if (root.TryGetProperty("backpressure-level", out var lvlEl2)) 
            return lvlEl2.GetString();
        return null;
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
            _logger.LogDebug(ex, "Failed to parse checkpoints for job {FlinkJobId}", flinkJobId);
        }
    }

    private static void ProcessCheckpointCounts(JsonElement root, JobMetricsBuilder metrics)
    {
        if (root.TryGetProperty("counts", out var counts) && counts.TryGetProperty("completed", out var completedEl) && completedEl.TryGetInt32(out var c))
            metrics.SetCheckpoints(c);
    }

    private static void ProcessCheckpointTimestamps(JsonElement root, JobMetricsBuilder metrics)
    {
        if (!root.TryGetProperty("latest", out var latest)) return;

        if (latest.TryGetProperty("completed", out var comp))
        {
            var timestamp = ExtractTimestamp(comp, "end_time") ?? ExtractTimestamp(comp, "trigger_timestamp");
            if (timestamp.HasValue)
                metrics.SetLastCheckpoint(timestamp.Value);
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

    private sealed class JobMetricsBuilder
    {
        private readonly string _flinkJobId;
        private long _recordsIn;
        private long _recordsOut;
        private int _parallelism;
        private int _checkpoints;
        private DateTime? _lastCheckpoint;
        private string _backpressureLevel = "UNKNOWN";

        public JobMetricsBuilder(string flinkJobId)
        {
            _flinkJobId = flinkJobId;
        }

        public void AddRecordsIn(long value) => _recordsIn += value;
        public void AddRecordsOut(long value) => _recordsOut += value;
        public void UpdateMaxParallelism(int value) => _parallelism = Math.Max(_parallelism, value);
        public void SetCheckpoints(int value) => _checkpoints = value;
        public void SetLastCheckpoint(DateTime value) => _lastCheckpoint = value;
        public void UpdateWorstBackpressure(string level) => _backpressureLevel = WorstBackpressure(_backpressureLevel, level);

        /// <summary>Determines the worst backpressure level between current and candidate</summary>
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

        public JobMetrics Build()
        {
            return new JobMetrics
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
            _logger.LogWarning("Runner jar not found at {Path}. Attempting to build FlinkIRRunner project", jarPath);
            var repoRoot = FindRepoRoot(Environment.CurrentDirectory) ?? Environment.CurrentDirectory;
            try
            {
                await EnsureJavaAndMavenAsync(repoRoot);
                await BuildFlinkIRRunnerAsync(repoRoot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to build runner jar automatically");
                // Fall back to PowerShell script if direct Maven build fails
                try
                {
                    await BuildViaScriptAsync(repoRoot);
                }
                catch (Exception scriptEx)
                {
                    _logger.LogError(scriptEx, "PowerShell script fallback also failed");
                }
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

    private async Task EnsureJavaAndMavenAsync(string repoRoot)
    {
        // Check Java installation
        if (!await IsCommandAvailableAsync("java"))
        {
            throw new InvalidOperationException("Java is not installed or not in PATH. Java 17+ is required for Flink components.");
        }
        
        var javaVersion = await GetCommandOutputAsync("java", "-version");
        _logger.LogInformation("Java version check: {Version}", javaVersion);
        
        // Check/Install Maven
        if (!await IsCommandAvailableAsync("mvn"))
        {
            _logger.LogInformation("Maven not found in PATH. Using repository tools directory.");
            var toolsDir = Path.Combine(repoRoot, "tools");
            var mvnPath = await EnsureMavenInToolsAsync(toolsDir);
            Environment.SetEnvironmentVariable("PATH", $"{Path.GetDirectoryName(mvnPath)}{Path.PathSeparator}{Environment.GetEnvironmentVariable("PATH")}");
        }
        
        var mvnVersion = await GetCommandOutputAsync("mvn", "-version");
        _logger.LogInformation("Maven version check: {Version}", mvnVersion);
    }

    private async Task<string> EnsureMavenInToolsAsync(string toolsDir)
    {
        const string mavenVersion = "3.9.8";
        var mvnHome = Path.Combine(toolsDir, $"apache-maven-{mavenVersion}");
        var mvnBin = Path.Combine(mvnHome, "bin", "mvn");
        
        // Check if mvn.cmd exists on Windows
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            mvnBin += ".cmd";
        }
        
        if (File.Exists(mvnBin))
        {
            return mvnBin;
        }
        
        // Download and extract Maven
        _logger.LogInformation("Downloading Maven {Version} to {ToolsDir}", mavenVersion, toolsDir);
        Directory.CreateDirectory(toolsDir);
        
        var zipUrl = $"https://archive.apache.org/dist/maven/maven-3/{mavenVersion}/binaries/apache-maven-{mavenVersion}-bin.zip";
        var zipPath = Path.Combine(toolsDir, $"apache-maven-{mavenVersion}-bin.zip");
        
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(zipUrl);
            response.EnsureSuccessStatusCode();
            await using var fileStream = File.Create(zipPath);
            await response.Content.CopyToAsync(fileStream);
        }
        
        System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, toolsDir);
        File.Delete(zipPath);
        
        return mvnBin;
    }

    private async Task BuildFlinkIRRunnerAsync(string repoRoot)
    {
        var flinkRunnerDir = Path.Combine(repoRoot, "FlinkIRRunner");
        if (!Directory.Exists(flinkRunnerDir))
        {
            throw new DirectoryNotFoundException($"FlinkIRRunner directory not found at {flinkRunnerDir}");
        }
        
        _logger.LogInformation("Building FlinkIRRunner using Maven in {Directory}", flinkRunnerDir);
        
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = "mvn",
            Arguments = "-q -DskipTests package",
            WorkingDirectory = flinkRunnerDir,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };
        
        using var proc = System.Diagnostics.Process.Start(psi)!;
        var stdOut = await proc.StandardOutput.ReadToEndAsync();
        var stdErr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"Maven build failed with exit code {proc.ExitCode}. stdout: {stdOut}, stderr: {stdErr}");
        }
        
        _logger.LogInformation("FlinkIRRunner built successfully. stdout: {Out}", stdOut);
    }

    private async Task BuildViaScriptAsync(string repoRoot)
    {
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
        
        if (proc.ExitCode != 0)
        {
            throw new InvalidOperationException($"PowerShell build script failed with exit code {proc.ExitCode}. stdout: {stdOut}, stderr: {stdErr}");
        }
        
        _logger.LogInformation("PowerShell build script completed. stdout: {Out}", stdOut);
    }

    private async Task<bool> IsCommandAvailableAsync(string command)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = Environment.OSVersion.Platform == PlatformID.Win32NT ? "where" : "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            
            using var proc = System.Diagnostics.Process.Start(psi);
            await proc!.WaitForExitAsync();
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private async Task<string> GetCommandOutputAsync(string command, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            
            using var proc = System.Diagnostics.Process.Start(psi);
            var output = await proc!.StandardOutput.ReadToEndAsync();
            var error = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();
            
            return string.IsNullOrEmpty(output) ? error : output;
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
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
        /// <summary>Upload timestamp from Flink API - populated by JSON deserialization</summary>
        public long Uploaded { get; init; } = 0;
    }

    private sealed class FlinkMetricEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
    }
}
