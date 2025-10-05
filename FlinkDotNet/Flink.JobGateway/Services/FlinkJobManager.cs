using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flink.JobBuilder.Models;

namespace Flink.JobGateway.Services;

[SuppressMessage("Reliability", "S2139", Justification = "Gateway converts exceptions into domain objects; selective rethrowing is intentional.")]
public class FlinkJobManager : IFlinkJobManager
{
    private readonly ILogger<FlinkJobManager> _logger;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, JobInfo> _jobMapping = new();

    public FlinkJobManager(ILogger<FlinkJobManager> logger, HttpClient httpClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        // Try multiple Flink endpoint discovery strategies
        // NOTE: This discovery happens at Gateway startup time, which may be BEFORE
        // the Flink container is fully ready in Aspire DCP testing mode.
        // The Gateway will use this endpoint, but if Flink is not ready, operations
        // will gracefully fall back to local mode.
        var flinkBaseUrl = DiscoverFlinkEndpoint();
        
        _httpClient.BaseAddress = new Uri(flinkBaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _logger.LogInformation("Flink Job Gateway initialized with target cluster: {FlinkBaseUrl}", flinkBaseUrl);
        _logger.LogInformation("Gateway will verify Flink connectivity when jobs are submitted");
    }

    /// <summary>
    /// Discover Flink endpoint using multiple strategies for maximum compatibility.
    /// Priority: Aspire service discovery > Environment variables > Default fallback
    /// </summary>
    private string DiscoverFlinkEndpoint()
    {
        // Strategy 1: Aspire service discovery (injected by .WithReference())
        // Format: services__flink-jobmanager__http__0 = "http://localhost:63624"
        var aspireEndpoint = Environment.GetEnvironmentVariable("services__flink-jobmanager__http__0");
        if (!string.IsNullOrEmpty(aspireEndpoint))
        {
            _logger.LogInformation("Using Aspire service discovery endpoint: {Endpoint}", aspireEndpoint);
            return aspireEndpoint;
        }

        // Strategy 2: Explicit environment variables (Docker Compose)
        var envHost = Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST");
        var envPort = Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT");
        
        if (!string.IsNullOrEmpty(envHost))
        {
            var port = int.TryParse(envPort, out var p) ? p : 8081;
            var envEndpoint = $"http://{envHost}:{port}";
            _logger.LogInformation("Using environment variable endpoint: {Endpoint}", envEndpoint);
            return envEndpoint;
        }

        // Strategy 3: Default fallback for Docker Compose with standard ports
        var defaultEndpoint = "http://flink-jobmanager:8081";
        _logger.LogInformation("Using default Docker Compose endpoint: {Endpoint}", defaultEndpoint);
        return defaultEndpoint;
    }

    /// <summary>
    /// Discover Flink SQL Gateway endpoint using multiple strategies for maximum compatibility.
    /// SQL Gateway now runs in the same container as JobManager but on port 8083.
    /// Priority: Aspire service discovery > Environment variables > Default fallback
    /// </summary>
    private string DiscoverSqlGatewayEndpoint()
    {
        // Strategy 1: Aspire service discovery (injected by .WithReference())
        // Format: services__flink-jobmanager__sql-gateway__0 = "http://localhost:xxxxx"
        var aspireEndpoint = Environment.GetEnvironmentVariable("services__flink-jobmanager__sql-gateway__0");
        if (!string.IsNullOrEmpty(aspireEndpoint))
        {
            _logger.LogInformation("Using Aspire service discovery for SQL Gateway: {Endpoint}", aspireEndpoint);
            return aspireEndpoint;
        }

        // Strategy 2: Explicit environment variables
        var envHost = Environment.GetEnvironmentVariable("FLINK_SQL_GATEWAY_HOST");
        var envPort = Environment.GetEnvironmentVariable("FLINK_SQL_GATEWAY_PORT");
        
        if (!string.IsNullOrEmpty(envHost))
        {
            var port = int.TryParse(envPort, out var p) ? p : 8083;
            var envEndpoint = $"http://{envHost}:{port}";
            _logger.LogInformation("Using environment variable for SQL Gateway: {Endpoint}", envEndpoint);
            return envEndpoint;
        }

        // Strategy 3: Default fallback - SQL Gateway in same container as JobManager
        var defaultEndpoint = "http://flink-jobmanager:8083";
        _logger.LogInformation("Using default SQL Gateway endpoint: {Endpoint}", defaultEndpoint);
        return defaultEndpoint;
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        _logger.LogInformation("Submitting job: {JobId}", jobDefinition.Metadata.JobId);
        try
        {
            var validation = ValidateJobDefinition(jobDefinition);
            if (!validation.IsValid)
            {
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId,
                    $"Job validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Check if this is a SQL Gateway job
            if (jobDefinition.Source is SqlSourceDefinition sqlSource && 
                sqlSource.ExecutionMode == "gateway")
            {
                _logger.LogInformation("Detected SQL Gateway execution mode for job {JobId}", jobDefinition.Metadata.JobId);
                var clusterHealthy = await ProbeClusterHealthSafelyAsync();
                
                if (!clusterHealthy)
                {
                    return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId,
                        "SQL Gateway mode requires Flink cluster to be available. Cluster is not healthy.");
                }
                
                var flinkJobId = await SubmitSqlGatewayJobAsync(sqlSource, jobDefinition);
                TrackJob(jobDefinition, flinkJobId, true);
                return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId);
            }

            // Standard JAR submission flow (including TableEnvironment SQL)
            var irBase64 = EncodeJobDefinition(jobDefinition);
            var clusterHealthy2 = await ProbeClusterHealthSafelyAsync();
            var flinkJobId2 = clusterHealthy2
                ? await SubmitJobToFlinkClusterAsync(irBase64, jobDefinition)
                : await RunLocalAsync(irBase64, jobDefinition);

            TrackJob(jobDefinition, flinkJobId2, clusterHealthy2);

            if (!clusterHealthy2 && _jobMapping[flinkJobId2].Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
            {
                return new JobSubmissionResult
                {
                    JobId = jobDefinition.Metadata.JobId,
                    FlinkJobId = flinkJobId2,
                    Success = true,
                    SubmittedAt = DateTime.UtcNow,
                    Metadata = new Dictionary<string, string> { ["mode"] = "local" }
                };
            }

            return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit job {JobId}", jobDefinition.Metadata.JobId);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, ex.Message);
        }
    }

    private static string EncodeJobDefinition(JobDefinition jobDefinition)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
        var irJson = JsonSerializer.Serialize(jobDefinition, serializerOptions);
        
        LogJobDefinitionDiagnostics(irJson, jobDefinition);
        
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(irJson));
    }

    private static void LogJobDefinitionDiagnostics(string irJson, JobDefinition jobDefinition)
    {
        Console.WriteLine("[DIAGNOSTIC] ════════════════════════════════════════════════════════════");
        Console.WriteLine("[DIAGNOSTIC] Complete Job Definition JSON:");
        Console.WriteLine(irJson);
        Console.WriteLine("[DIAGNOSTIC] ════════════════════════════════════════════════════════════");
        
        LogKafkaConfiguration(jobDefinition);
        LogOperations(jobDefinition);
    }

    private static void LogKafkaConfiguration(JobDefinition jobDefinition)
    {
        var kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
        var kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
        Console.WriteLine($"[DIAGNOSTIC] Source BootstrapServers: {kafkaSource?.BootstrapServers ?? "null"}");
        Console.WriteLine($"[DIAGNOSTIC] Sink BootstrapServers: {kafkaSink?.BootstrapServers ?? "null"}");
        Console.WriteLine($"[DIAGNOSTIC] Operations Count: {jobDefinition.Operations?.Count ?? 0}");
    }

    private static void LogOperations(JobDefinition jobDefinition)
    {
        if (jobDefinition.Operations == null || jobDefinition.Operations.Count == 0)
            return;

        foreach (var op in jobDefinition.Operations)
        {
            if (op is MapOperationDefinition mapOp)
            {
                Console.WriteLine($"[DIAGNOSTIC] Map Operation - Expression: '{mapOp.Expression}'");
            }
        }
    }

    private async Task<bool> ProbeClusterHealthSafelyAsync()
    {
        try
        {
            return await CheckFlinkClusterHealthAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cluster health probe failed; falling back to local mode.");
            return false;
        }
    }

    private void TrackJob(JobDefinition jobDefinition, string flinkJobId, bool clusterHealthy)
    {
        _jobMapping[flinkJobId] = new JobInfo
        {
            JobId = jobDefinition.Metadata.JobId,
            FlinkJobId = flinkJobId,
            Status = clusterHealthy ? "RUNNING" : "LOCAL-RUNNING",
            SubmissionTime = DateTime.UtcNow,
            JobDefinition = jobDefinition
        };
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

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            throw new InvalidOperationException($"Unexpected status code querying Flink job status: {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
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

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }

            throw new InvalidOperationException($"Unexpected status code canceling Flink job: {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to cancel job in Flink 2.1.0 cluster: {flinkJobId}", ex);
        }
    }

    private async Task<string> RunLocalAsync(string irBase64, JobDefinition jobDefinition)
    {
        var jarPath = await EnsureRunnerJarPathAsync();
        ValidateJarExists(jarPath);

        var id = $"local-{Guid.NewGuid():N}";
        var bootstrap = GetBootstrapServers(jobDefinition);
        var proc = StartLocalRunnerProcess(jarPath, irBase64, bootstrap);

        MonitorLocalRunnerAsync(proc, id);

        LogLocalRunnerStart(proc, bootstrap, jarPath, jobDefinition.Metadata.JobId, irBase64);
        return id;
    }

    private static void ValidateJarExists(string jarPath)
    {
        if (!File.Exists(jarPath))
        {
            throw new InvalidOperationException(
                $"Runner JAR not found at {jarPath}. The JAR should be built automatically during Gateway build. " +
                "Please ensure Java and Maven are installed and the build completed successfully.");
        }
    }

    private static string GetBootstrapServers(JobDefinition jobDefinition)
    {
        if (jobDefinition.Source is KafkaSourceDefinition ks && !string.IsNullOrWhiteSpace(ks.BootstrapServers))
            return ks.BootstrapServers;
        
        if (jobDefinition.Sink is KafkaSinkDefinition ksd && !string.IsNullOrWhiteSpace(ksd.BootstrapServers))
            return ksd.BootstrapServers;

        return Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "localhost:9092";
    }

    private static Process StartLocalRunnerProcess(string jarPath, string irBase64, string bootstrap)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "java",
            Arguments = $"-jar \"{jarPath}\" --irBase64 {irBase64}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        psi.Environment["KAFKA_BOOTSTRAP"] = bootstrap;

        Process? proc;
        try
        {
            proc = Process.Start(psi);
        }
        catch (Exception startEx)
        {
            throw new InvalidOperationException(
                "Failed to start Java process for local execution. Ensure Java is installed and in PATH.", startEx);
        }

        if (proc == null)
        {
            throw new InvalidOperationException("Java process returned null - failed to start local runner.");
        }

        return proc;
    }

    private void MonitorLocalRunnerAsync(Process proc, string id)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                await CaptureProcessOutputAsync(proc, id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local runner output capture failed for {JobId}", id);
                UpdateJobStatus(id, "LOCAL-ERROR");
            }
        });
    }

    private async Task CaptureProcessOutputAsync(Process proc, string id)
    {
        var stdout = await proc.StandardOutput.ReadToEndAsync();
        var stderr = await proc.StandardError.ReadToEndAsync();
        await proc.WaitForExitAsync();
        
        LogProcessOutput(stdout, stderr, id);
        UpdateJobStatusBasedOnExitCode(proc.ExitCode, id);
    }

    private void LogProcessOutput(string stdout, string stderr, string id)
    {
        if (!string.IsNullOrWhiteSpace(stdout))
            _logger.LogInformation("[local-runner:{JobId}] STDOUT:\n{Out}", id, stdout);
        
        if (!string.IsNullOrWhiteSpace(stderr))
            _logger.LogWarning("[local-runner:{JobId}] STDERR:\n{Err}", id, stderr);
    }

    private void UpdateJobStatusBasedOnExitCode(int exitCode, string id)
    {
        if (exitCode != 0)
        {
            _logger.LogError("[local-runner:{JobId}] Process exited with code {ExitCode}", id, exitCode);
            UpdateJobStatus(id, $"LOCAL-FAILED (exit code {exitCode})");
        }
        else
        {
            _logger.LogInformation("[local-runner:{JobId}] Process completed successfully", id);
            UpdateJobStatus(id, "LOCAL-COMPLETED");
        }
    }

    private void UpdateJobStatus(string id, string status)
    {
        if (_jobMapping.TryGetValue(id, out var jobInfo))
        {
            jobInfo.Status = status;
        }
    }

    private void LogLocalRunnerStart(Process proc, string bootstrap, string jarPath, string jobId, string irBase64)
    {
        _logger.LogInformation(
            "Started local runner (PID={Pid}, bootstrap={Bootstrap}, jarPath={JarPath}) for job {JobId}",
            proc.Id, bootstrap, jarPath, jobId);
        
        var prefix = irBase64.Length > 50 ? irBase64.Substring(0, 50) : irBase64;
        _logger.LogInformation("Local runner command: java -jar \"{JarPath}\" --irBase64 {IrBase64Prefix}...",
            jarPath, prefix);
    }

    private async Task<string> EnsureRunnerJarPathAsync()
    {
        // First try to find existing jar in working directory or repo structure
        var jarPath = FindExistingRunnerJar();
        if (jarPath != null && File.Exists(jarPath))
        {
            _logger.LogDebug("Found existing runner jar at {Path}", jarPath);
            return jarPath;
        }

        // Build jar on demand using Maven directly
        _logger.LogInformation("Runner jar not found, building on demand with Maven...");
        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot == null)
        {
            throw new InvalidOperationException("Could not locate repository root for Maven build");
        }

        var runnerDir = Path.Combine(repoRoot, "FlinkIRRunner");
        var pomFile = Path.Combine(runnerDir, "pom.xml");
        if (!File.Exists(pomFile))
        {
            throw new InvalidOperationException($"Maven pom.xml not found at {pomFile}");
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "mvn",
                Arguments = "clean package -DskipTests",
                WorkingDirectory = runnerDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            _logger.LogDebug("Starting Maven build in {WorkingDir}: mvn {Args}", runnerDir, psi.Arguments);
            var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Maven process");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var stdout = await outputTask;
            var stderr = await errorTask;

            if (process.ExitCode != 0)
            {
                _logger.LogError("Maven build failed with exit code {ExitCode}\nSTDOUT:\n{Stdout}\nSTDERR:\n{Stderr}",
                    process.ExitCode, stdout, stderr);
                throw new InvalidOperationException($"Maven build failed with exit code {process.ExitCode}");
            }

            _logger.LogDebug("Maven build completed successfully");
            jarPath = Path.Combine(runnerDir, "target", "flink-ir-runner.jar");
            if (!File.Exists(jarPath))
            {
                throw new InvalidOperationException($"Maven build completed but jar not found at expected path: {jarPath}");
            }

            return jarPath;
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to build runner jar with Maven", ex);
        }
    }

    private static string? FindExistingRunnerJar()
    {
        // Check if FLINK_RUNNER_JAR_PATH is set
        var envPath = Environment.GetEnvironmentVariable("FLINK_RUNNER_JAR_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Prioritize Java 17 JAR since Flink 2.1.0 runs on Java 17
        // Even if built with JDK 25, we must use Java 17-compatible JAR for Flink submission
        var names = new[] { "flink-ir-runner-java17.jar", "flink-ir-runner.jar", "flink-ir-runner-java25.jar" };
        var baseDirs = new[]
        {
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, "FlinkIRRunner", "target")
        };

        var searchPaths = baseDirs.SelectMany(d => names.Select(n => Path.Combine(d, n))).ToArray();

        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            var repoCandidates = new[]
            {
                Path.Combine(repoRoot, "FlinkIRRunner", "target"),
                repoRoot,
            };
            searchPaths = searchPaths.Concat(repoCandidates.SelectMany(d => names.Select(n => Path.Combine(d, n)))).ToArray();
        }

        return searchPaths.FirstOrDefault(File.Exists);
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

            // DIAGNOSTIC: Log job definition bootstrap servers before submission
            var kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
            var kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
            _logger.LogInformation("[DEBUG] Job definition before Flink submission: Source bootstrap={SourceBootstrap}, Sink bootstrap={SinkBootstrap}",
                kafkaSource?.BootstrapServers ?? "null",
                kafkaSink?.BootstrapServers ?? "null");

            var runRequest = new
            {
                entryClass = "com.flink.jobgateway.FlinkJobRunner",
                programArgsList = new[] { "--irBase64", irBase64 },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                jobName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId
            };

            var requestJson = JsonSerializer.Serialize(runRequest);
            _logger.LogInformation("[DEBUG] Flink run request JSON: {RequestJson}", requestJson);
            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");
            using var response = await _httpClient.PostAsync($"/v1/jars/{jarId}/run", content);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted)
            {
                var err = await response.Content.ReadAsStringAsync();
                _logger.LogError("Flink job submission failed with {StatusCode}. Full error response: {Error}", 
                    response.StatusCode, err);
                throw new InvalidOperationException($"Flink run failed: {response.StatusCode} - {err}");
            }

            var runContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Flink run response: {RunContent}", runContent);

            string? jobId = null;
            try
            {
                var run = JsonSerializer.Deserialize<FlinkRunResponse>(runContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                jobId = run?.JobId;
            }
            catch (JsonException ex)
            {
                _logger.LogDebug(ex, "Failed to deserialize Flink run response when extracting job id");
            }

            jobId ??= TryGetJobIdFromHeaders(response);

            if (string.IsNullOrEmpty(jobId))
            {
                var targetName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
                jobId = await TryRecoverFlinkJobIdAsync(targetName, TimeSpan.FromSeconds(30));
            }

            if (string.IsNullOrEmpty(jobId))
            {
                throw new InvalidOperationException($"Flink did not return a jobId. Response: {runContent}");
            }

            return jobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit jar to Flink REST API");
            throw;
        }
    }

    private async Task<string> SubmitSqlGatewayJobAsync(SqlSourceDefinition sqlSource, JobDefinition jobDefinition)
    {
        _logger.LogInformation("Submitting SQL job via Flink SQL Gateway for job {JobId}", jobDefinition.Metadata.JobId);
        
        try
        {
            // Create dedicated HttpClient for SQL Gateway (different endpoint from JobManager)
            var sqlGatewayEndpoint = DiscoverSqlGatewayEndpoint();
            using var sqlGatewayClient = new HttpClient
            {
                BaseAddress = new Uri(sqlGatewayEndpoint),
                Timeout = TimeSpan.FromMinutes(5)
            };
            
            _logger.LogInformation("Using SQL Gateway endpoint: {Endpoint}", sqlGatewayEndpoint);
            
            // Create a session first (optional, but recommended for statement management)
            var sessionName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
            _logger.LogInformation("Creating SQL Gateway session: {SessionName}", sessionName);
            
            // Note: Flink 2.1.0 SQL Gateway endpoint is /v1/sessions for session management
            // For now, we'll submit statements directly without session management
            // This is a simplified implementation - production would use sessions
            
            // Execute each SQL statement via SQL Gateway
            string? lastJobId = null;
            foreach (var statement in sqlSource.Statements)
            {
                if (string.IsNullOrWhiteSpace(statement))
                    continue;
                    
                _logger.LogInformation("Executing SQL statement via Gateway: {Statement}", 
                    statement.Length > 100 ? statement.Substring(0, 100) + "..." : statement);
                
                var requestBody = new
                {
                    statement = statement.Trim()
                };
                
                var jsonContent = JsonSerializer.Serialize(requestBody);
                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                
                // Submit statement to SQL Gateway
                using var response = await sqlGatewayClient.PostAsync("/v1/statements", content);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("SQL Gateway statement execution failed: {StatusCode} - {Error}", 
                        response.StatusCode, errorContent);
                    throw new InvalidOperationException($"SQL Gateway execution failed: {response.StatusCode} - {errorContent}");
                }
                
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogDebug("SQL Gateway response: {Response}", responseContent);
                
                // Try to extract job ID from response if this is an INSERT statement
                if (statement.Trim().ToUpperInvariant().StartsWith("INSERT"))
                {
                    try
                    {
                        var responseJson = JsonDocument.Parse(responseContent);
                        if (responseJson.RootElement.TryGetProperty("jobId", out var jobIdProp))
                        {
                            lastJobId = jobIdProp.GetString();
                            _logger.LogInformation("SQL Gateway returned job ID: {JobId}", lastJobId);
                        }
                        else if (responseJson.RootElement.TryGetProperty("statementId", out var stmtIdProp))
                        {
                            // Some versions return statementId instead
                            lastJobId = stmtIdProp.GetString();
                            _logger.LogInformation("SQL Gateway returned statement ID: {StatementId}", lastJobId);
                        }
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogWarning(ex, "Could not parse SQL Gateway response for job ID");
                    }
                }
            }
            
            // If no job ID was extracted, generate a synthetic one
            if (string.IsNullOrEmpty(lastJobId))
            {
                lastJobId = $"sql-gateway-{Guid.NewGuid():N}";
                _logger.LogInformation("No job ID returned from SQL Gateway, using synthetic ID: {JobId}", lastJobId);
            }
            
            return lastJobId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit SQL job via SQL Gateway");
            throw new InvalidOperationException("SQL Gateway submission failed. See inner exception for details.", ex);
        }
    }


    private async Task<string> EnsureRunnerJarAsync()
    {
        var jarPath = await EnsureRunnerJarPathAsync();
        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Runner jar not found at {jarPath}");
        }

        // Collect connector JARs and create a shaded JAR if needed
        var connectorJars = CollectConnectorJars();
        if (connectorJars.Any())
        {
            _logger.LogInformation("Found {Count} connector JARs, creating shaded JAR", connectorJars.Count);
            jarPath = await CreateShadedJarAsync(jarPath, connectorJars);
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

        var uploadPayload = await uploadResp.Content.ReadAsStringAsync();
        _logger.LogInformation("Jar upload response payload: {Payload}", uploadPayload);

        if (!string.IsNullOrWhiteSpace(uploadPayload))
        {
            try
            {
                var uploadInfo = JsonSerializer.Deserialize<FlinkJarUploadResponse>(uploadPayload);
                var jarId = uploadInfo?.Filename;
                if (!string.IsNullOrEmpty(jarId))
                {
                    jarId = Path.GetFileName(jarId.Replace('\\', '/'));
                    if (!string.IsNullOrEmpty(jarId))
                    {
                        _logger.LogInformation("Flink accepted jar upload {JarFile} as {JarId}", fileName, jarId);
                        return jarId;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to parse jar upload response: {Payload}", uploadPayload);
            }
        }

        return await WaitForJarRegistrationAsync(fileName);
    }

    private async Task<string> WaitForJarRegistrationAsync(string fileName, TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + waitFor;
        var delay = TimeSpan.FromMilliseconds(500);
        List<string> lastKnownJars = new();

        while (DateTime.UtcNow < deadline)
        {
            var jarId = await TryFindRegisteredJarAsync(fileName, lastKnownJars);
            if (jarId != null)
                return jarId;

            await Task.Delay(delay);
        }

        return ThrowJarNotFoundError(fileName, waitFor, lastKnownJars);
    }

    private async Task<string?> TryFindRegisteredJarAsync(string fileName, List<string> lastKnownJars)
    {
        try
        {
            var listResp = await _httpClient.GetAsync("/v1/jars");
            if (!listResp.IsSuccessStatusCode)
                return null;

            var listJson = await listResp.Content.ReadAsStringAsync();
            var jars = JsonSerializer.Deserialize<FlinkJarsList>(listJson);
            
            UpdateLastKnownJars(jars, lastKnownJars);
            return FindMatchingJar(jars, fileName);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Polling for uploaded jar {JarFile} failed; will retry", fileName);
            return null;
        }
    }

    private static void UpdateLastKnownJars(FlinkJarsList? jars, List<string> lastKnownJars)
    {
        lastKnownJars.Clear();
        if (jars?.Files != null)
        {
            lastKnownJars.AddRange(jars.Files.Select(f =>
                string.IsNullOrEmpty(f.Id) ? f.Name : $"{f.Name} ({f.Id})"));
        }
    }

    private static string? FindMatchingJar(FlinkJarsList? jars, string fileName)
    {
        var jar = jars?.Files?
            .OrderByDescending(f => f.Uploaded)
            .FirstOrDefault(f => string.Equals(f.Name, fileName, StringComparison.OrdinalIgnoreCase));

        return string.IsNullOrEmpty(jar?.Id) ? null : jar.Id;
    }

    private string ThrowJarNotFoundError(string fileName, TimeSpan waitFor, List<string> lastKnownJars)
    {
        var jarList = string.Join(", ", lastKnownJars);
        _logger.LogError("Uploaded jar {JarFile} not found; last known jars: {JarList}", fileName, jarList);
        throw new InvalidOperationException(
            $"Uploaded jar '{fileName}' not found in Flink jar list within {waitFor.TotalSeconds:F0}s. Last seen: {jarList}");
    }

    private List<string> CollectConnectorJars()
    {
        var connectorJars = new List<string>();
        var searchPaths = new List<string>();

        var connectorPath = Environment.GetEnvironmentVariable("FLINK_CONNECTOR_PATH");
        if (!string.IsNullOrEmpty(connectorPath))
        {
            searchPaths.Add(connectorPath);
        }

        // Standard container lib
        searchPaths.Add("/opt/flink/lib");

        // Try multiple strategies to find the connector JARs
        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            searchPaths.Add(Path.Combine(repoRoot, "LocalTesting", "connectors", "flink", "lib"));
        }

        // Also try from AppDomain base directory (works better in Aspire scenarios)
        var appRoot = FindRepoRoot(AppDomain.CurrentDomain.BaseDirectory);
        if (appRoot != null && appRoot != repoRoot)
        {
            searchPaths.Add(Path.Combine(appRoot, "LocalTesting", "connectors", "flink", "lib"));
        }

        _logger.LogInformation("Searching for connector JARs in {Count} paths", searchPaths.Count);

        foreach (var searchPath in searchPaths.Distinct())
        {
            if (Directory.Exists(searchPath))
            {
                var jars = Directory.GetFiles(searchPath, "*.jar", SearchOption.TopDirectoryOnly);
                if (jars.Length > 0)
                {
                    connectorJars.AddRange(jars);
                    _logger.LogInformation("Found {Count} connector JARs in {Path}", jars.Length, searchPath);
                }
            }
            else
            {
                _logger.LogDebug("Connector path does not exist: {Path}", searchPath);
            }
        }

        if (connectorJars.Count == 0)
        {
            _logger.LogWarning("No connector JARs found. SQL jobs may fail if they require Kafka/JSON connectors.");
            _logger.LogWarning("Current directory: {Current}, AppDomain base: {AppBase}, Repo root: {RepoRoot}",
                Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory, repoRoot ?? "not found");
        }

        return connectorJars.Distinct().ToList();
    }

    private async Task<string> CreateShadedJarAsync(string runnerJarPath, List<string> connectorJars)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flink-shaded-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var shadedJarPath = Path.Combine(tempDir, "flink-ir-runner-shaded.jar");
            await CombineJarsAsync(runnerJarPath, connectorJars, shadedJarPath);
            return shadedJarPath;
        }
        catch
        {
            try
            {
                Directory.Delete(tempDir, true);
            }
            catch (Exception cleanupEx)
            {
                // Non-fatal cleanup failure; continue throwing original error
                _logger.LogDebug(cleanupEx, "Temp directory cleanup failed: {TempDir}", tempDir);
            }
            throw;
        }
    }

    private Task CombineJarsAsync(string runnerJarPath, List<string> connectorJars, string outputPath)
    {
        _logger.LogInformation("Combining runner JAR with {Count} connector JARs into shaded JAR", connectorJars.Count);
        
        // Copy runner JAR as base
        File.Copy(runnerJarPath, outputPath, true);

        // JARs are ZIP files - use ZipArchive to merge connector JARs into the runner JAR
        // Special handling: META-INF/services files must be merged, not replaced
        var serviceFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        
        using (var outputZip = ZipFile.Open(outputPath, ZipArchiveMode.Update))
        {
            var existingEntries = new HashSet<string>(
                outputZip.Entries.Select(e => e.FullName),
                StringComparer.OrdinalIgnoreCase);

            // First, collect all META-INF/services entries from runner JAR
            foreach (var entry in outputZip.Entries.ToList())
            {
                if (entry.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase) &&
                    !entry.FullName.EndsWith('/'))
                {
                    using var stream = entry.Open();
                    using var reader = new StreamReader(stream);
                    var lines = new HashSet<string>();
                    string? line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        var trimmed = line.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                        {
                            lines.Add(line); // Keep original format including comments
                        }
                    }
                    serviceFiles[entry.FullName] = lines;
                }
            }

            foreach (var connectorJar in connectorJars)
            {
                if (!File.Exists(connectorJar))
                {
                    _logger.LogWarning("Connector JAR not found, skipping: {Path}", connectorJar);
                    continue;
                }

                _logger.LogDebug("Merging connector JAR: {Path}", connectorJar);
                var entriesAdded = 0;

                using (var connectorZip = ZipFile.OpenRead(connectorJar))
                {
                    foreach (var entry in connectorZip.Entries)
                    {
                        // Skip directories (they're created automatically)
                        if (entry.FullName.EndsWith('/'))
                        {
                            continue;
                        }

                        // Special handling for META-INF/services files - merge them
                        if (entry.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase))
                        {
                            using var stream = entry.Open();
                            using var reader = new StreamReader(stream);
                            
                            if (!serviceFiles.ContainsKey(entry.FullName))
                            {
                                serviceFiles[entry.FullName] = new HashSet<string>();
                            }
                            
                            string? line;
                            while ((line = reader.ReadLine()) != null)
                            {
                                var trimmed = line.Trim();
                                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith('#'))
                                {
                                    serviceFiles[entry.FullName].Add(line);
                                }
                            }
                            continue;
                        }

                        // Skip duplicate entries (keep first occurrence from runner JAR)
                        if (existingEntries.Contains(entry.FullName))
                        {
                            continue;
                        }

                        // Copy entry to output JAR
                        var newEntry = outputZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                        using (var sourceStream = entry.Open())
                        using (var destStream = newEntry.Open())
                        {
                            sourceStream.CopyTo(destStream);
                        }

                        existingEntries.Add(entry.FullName);
                        entriesAdded++;
                    }
                }

                _logger.LogInformation("Added {Count} entries from connector JAR: {Name}", 
                    entriesAdded, Path.GetFileName(connectorJar));
            }

            // Now write merged META-INF/services files
            foreach (var (servicePath, serviceLines) in serviceFiles)
            {
                // Remove old entry if it exists
                var oldEntry = outputZip.Entries.FirstOrDefault(e => 
                    e.FullName.Equals(servicePath, StringComparison.OrdinalIgnoreCase));
                oldEntry?.Delete();

                // Create new merged entry
                var newEntry = outputZip.CreateEntry(servicePath, CompressionLevel.Optimal);
                using var writer = new StreamWriter(newEntry.Open());
                foreach (var line in serviceLines.OrderBy(l => l))
                {
                    writer.WriteLine(line);
                }
                
                _logger.LogDebug("Merged service file {Path} with {Count} providers", 
                    servicePath, serviceLines.Count);
            }
        }

        _logger.LogInformation("Created shaded JAR at {Path}", outputPath);
        return Task.CompletedTask;
    }

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var pom = Path.Combine(dir.FullName, "FlinkIRRunner", "pom.xml");
            var globalJson = Path.Combine(dir.FullName, "global.json");
            if (File.Exists(pom) && File.Exists(globalJson))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    private async Task<string?> TryRecoverFlinkJobIdAsync(string? jobName, TimeSpan timeout)
    {
        if (string.IsNullOrWhiteSpace(jobName))
            return null;

        var sw = Stopwatch.StartNew();
        var overviewEndpoints = new[] { "/v1/jobs/overview", "/jobs/overview" };

        while (sw.Elapsed < timeout)
        {
            var jobId = await TryRecoverFromEndpointsAsync(jobName, overviewEndpoints);
            if (jobId != null)
                return jobId;

            await Task.Delay(TimeSpan.FromSeconds(1));
        }

        return null;
    }

    private async Task<string?> TryRecoverFromEndpointsAsync(string jobName, string[] endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var jobId = await TryRecoverFromSingleEndpointAsync(jobName, endpoint);
            if (jobId != null)
                return jobId;
        }
        return null;
    }

    private async Task<string?> TryRecoverFromSingleEndpointAsync(string jobName, string endpoint)
    {
        try
        {
            using var response = await _httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Jobs overview endpoint {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Jobs overview response from {Endpoint} while recovering job id: {Payload}", endpoint, payload);

            var recovered = ExtractJobIdFromOverviewPayload(payload, jobName);
            if (!string.IsNullOrEmpty(recovered))
            {
                _logger.LogInformation("Recovered job id {FlinkJobId} for job {JobName} via {Endpoint}", recovered, jobName, endpoint);
                return recovered;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to recover job id for {JobName} via {Endpoint}; will retry", jobName, endpoint);
        }
        return null;
    }

    private static string? ExtractJobIdFromOverviewPayload(string payload, string jobName)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;

        try
        {
            using var document = JsonDocument.Parse(payload);
            return ExtractJobIdFromOverviewElement(document.RootElement, jobName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ExtractJobIdFromOverviewElement(JsonElement element, string jobName)
    {
        if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var child in element.EnumerateArray())
            {
                var nested = ExtractJobIdFromOverviewElement(child, jobName);
                if (!string.IsNullOrEmpty(nested)) return nested;
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            var match = MatchJobEntry(element, jobName);
            if (!string.IsNullOrEmpty(match)) return match;

            foreach (var property in element.EnumerateObject())
            {
                var nested = ExtractJobIdFromOverviewElement(property.Value, jobName);
                if (!string.IsNullOrEmpty(nested)) return nested;
            }
        }

        return null;
    }

    private static string? MatchJobEntry(JsonElement element, string jobName)
    {
        if ((TryGetStringProperty(element, "name", out var name) || TryGetStringProperty(element, "jobName", out name))
            && string.Equals(name, jobName, StringComparison.OrdinalIgnoreCase)
            && (TryGetStringProperty(element, "jid", out var jobId)
                || TryGetStringProperty(element, "jobId", out jobId)
                || TryGetStringProperty(element, "jobid", out jobId)
                || TryGetStringProperty(element, "id", out jobId)))
        {
            return jobId;
        }

        return null;
    }

    private static bool TryGetStringProperty(JsonElement element, string propertyName, out string? value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase)
                && property.Value.ValueKind == JsonValueKind.String)
            {
                value = property.Value.GetString();
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? TryGetJobIdFromHeaders(HttpResponseMessage response)
    {
        if (response.Headers.Location is Uri location)
        {
            var jobId = ExtractJobIdFromPath(location.ToString());
            if (!string.IsNullOrEmpty(jobId)) return jobId;
        }

        if (response.Headers.TryGetValues("Location", out var locations))
        {
            foreach (var value in locations)
            {
                var jobId = ExtractJobIdFromPath(value);
                if (!string.IsNullOrEmpty(jobId)) return jobId;
            }
        }

        foreach (var headerName in new[] { "X-Flink-JobID", "X-Flink-Job-Id", "Flink-Job-Id", "Flink-JobId" })
        {
            if (response.Headers.TryGetValues(headerName, out var headerValues))
            {
                var value = headerValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                if (!string.IsNullOrEmpty(value)) return value.Trim();
            }
        }

        return null;

        static string? ExtractJobIdFromPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Split('?', 2)[0].Trim().Trim('/');
            if (string.IsNullOrEmpty(trimmed)) return null;

            var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.LastOrDefault();
            if (string.IsNullOrEmpty(last) || last.Equals("jobs", StringComparison.OrdinalIgnoreCase)) return null;

            return last;
        }
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

    private sealed class FlinkRunResponse
    {
        public string JobId { get; set; } = string.Empty;
    }

    private sealed class FlinkJarsList
    {
        public List<FlinkJarFile> Files { get; set; } = new();
    }

    private sealed class FlinkJarUploadResponse
    {
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private sealed class FlinkJarFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uploaded")]
        public long Uploaded { get; set; }
    }

    private sealed class FlinkMetricEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
    }

    // ---------------- Metrics helpers ----------------

    private async Task CollectVertexMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        var verticesResp = await _httpClient.GetAsync($"/v1/jobs/{flinkJobId}/vertices");
        if (!verticesResp.IsSuccessStatusCode) return;

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

        var metricsList = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync())
            ?? new List<FlinkMetricEntry>();

        foreach (var m in metricsList)
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

            var level = ExtractBackpressureLevel(root);
            if (!string.IsNullOrEmpty(level))
                metrics.UpdateWorstBackpressure(level);
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
        if (root.TryGetProperty("counts", out var counts)
            && counts.TryGetProperty("completed", out var completedEl)
            && completedEl.TryGetInt32(out var c))
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
            if (ts.HasValue)
                metrics.SetLastCheckpoint(ts.Value);
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
        if (root.TryGetProperty("backpressureLevel", out var lvlEl))
            return lvlEl.GetString();
        if (root.TryGetProperty("backpressure-level", out var lvlEl2))
            return lvlEl2.GetString();
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