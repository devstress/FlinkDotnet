using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.JobGateway.Services;

/// <summary>
/// Manages Apache Flink job lifecycle including submission, status monitoring, and cancellation.
/// Note: This gateway intentionally converts exceptions into domain objects with selective rethrowing.
/// </summary>
public partial class FlinkJobManager : IFlinkJobManager
{
    private const string ProtocolHttps = "HTTPS";
    private const string ProtocolHttp = "HTTP";
    private const string FlinkIRRunnerDirectory = "FlinkIRRunner";

    private readonly ILogger<FlinkJobManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, JobInfo> _jobMapping = new();

    /// <summary>
    /// Gets or sets the delay between SQL Gateway retry attempts.
    /// Static field for testability (can be set to 1ms in tests).
    /// </summary>
    public static TimeSpan SqlGatewayRetryDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the delay between JAR registration polling attempts.
    /// Static field for testability (can be set to 1ms in tests).
    /// </summary>
    public static TimeSpan JarRegistrationPollingDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Gets or sets the delay between job recovery polling attempts.
    /// Static field for testability (can be set to 1ms in tests).
    /// </summary>
    public static TimeSpan JobRecoveryPollingDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FlinkJobManager"/> class.
    /// </summary>
    /// <param name="logger">Logger for tracking job management operations.</param>
    /// <param name="configuration">Configuration for reading Flink endpoints.</param>
    /// <param name="httpClient">HTTP client configured for Flink REST API communication.</param>
    public FlinkJobManager(ILogger<FlinkJobManager> logger, IConfiguration configuration, HttpClient httpClient)
    {
        this._logger = logger;
        this._configuration = configuration;
        this._httpClient = httpClient;

        // Try multiple Flink endpoint discovery strategies
        // NOTE: This discovery happens at Gateway startup time, which may be BEFORE
        // the Flink container is fully ready in Aspire DCP testing mode.
        // The Gateway will verify Flink connectivity when jobs are submitted.
        var flinkBaseUrl = this.DiscoverFlinkEndpoint();

        this._httpClient.BaseAddress = new Uri(flinkBaseUrl);
        this._httpClient.Timeout = TimeSpan.FromMinutes(5);
        this._logger.LogInformation("Flink Job Gateway initialized with target cluster: {FlinkBaseUrl}", flinkBaseUrl);
        this._logger.LogInformation("Gateway will verify Flink connectivity when jobs are submitted");
    }

    /// <summary>
    /// Discover Flink endpoint using multiple strategies for maximum compatibility.
    /// Priority: Aspire service discovery > appsettings.json configuration > Environment variables > Default fallback
    /// </summary>
    private string DiscoverFlinkEndpoint()
    {
        return this.DiscoverEndpoint(
            serviceName: "flink-jobmanager",
            primaryEndpointName: "jm-http",
            legacyEndpointName: "http",
            configKey: "Flink:JobManager:BaseUrl",
            envHostKey: "FLINK_CLUSTER_HOST",
            envPortKey: "FLINK_CLUSTER_PORT",
            defaultPort: 8081,
            defaultHost: "flink-jobmanager",
            serviceDisplayName: "Flink JobManager",
            logAspireWarning: true
        );
    }

    /// <summary>
    /// Discover Flink SQL Gateway endpoint using multiple strategies for maximum compatibility.
    /// Priority: Aspire service discovery > appsettings.json configuration > Environment variables > Default fallback
    /// SQL Gateway runs on port 8083 (separate from JobManager REST API on 8081)
    /// </summary>
    private string DiscoverSqlGatewayEndpoint()
    {
        return this.DiscoverEndpoint(
            serviceName: "flink-sql-gateway",
            primaryEndpointName: "sg-http",
            legacyEndpointName: "http",
            configKey: "Flink:SqlGateway:BaseUrl",
            envHostKey: "FLINK_SQL_GATEWAY_HOST",
            envPortKey: "FLINK_SQL_GATEWAY_PORT",
            defaultPort: 8083,
            defaultHost: "flink-sql-gateway",
            serviceDisplayName: "SQL Gateway",
            logAspireWarning: true
        );
    }

    /// <summary>
    /// Generic endpoint discovery using multiple strategies.
    /// Reduces code duplication between Flink and SQL Gateway endpoint discovery.
    /// </summary>
    /// <remarks>
    /// This method has 10 parameters to eliminate 98 lines of code duplication.
    /// The trade-off is justified as it consolidates endpoint discovery logic.
    /// </remarks>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "S107:Methods should not have too many parameters",
        Justification = "Generic method eliminates 98 lines of duplication between DiscoverFlinkEndpoint and DiscoverSqlGatewayEndpoint")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Parameters reserved for future Aspire integration")]
    private string DiscoverEndpoint(
        string serviceName,
        string primaryEndpointName,
        string legacyEndpointName,
        string configKey,
        string envHostKey,
        string envPortKey,
        int defaultPort,
        string defaultHost,
        string serviceDisplayName,
        bool logAspireWarning)
    {
        // Strategy 1: Configuration from appsettings.json or injected by infrastructure (Aspire/tests)
        var configEndpoint = this._configuration[configKey];
        if (!string.IsNullOrEmpty(configEndpoint))
        {
            this._logger.LogInformation("Using configuration for {ServiceName}: {Endpoint}", serviceDisplayName, configEndpoint);
            return configEndpoint;
        }

        // Strategy 2: Explicit environment variables (generic, non-Aspire specific)
        var envHost = Environment.GetEnvironmentVariable(envHostKey);
        var envPort = Environment.GetEnvironmentVariable(envPortKey);

        if (!string.IsNullOrEmpty(envHost))
        {
            var port = int.TryParse(envPort, out var p) ? p : defaultPort;
            var protocol = this.GetProtocol();
            var envEndpoint = $"{protocol}://{envHost}:{port}";
            this._logger.LogInformation("Using environment variable for {ServiceName}: {Endpoint}", serviceDisplayName, envEndpoint);
            return envEndpoint;
        }

        // Strategy 3: Default fallback for local development
        var defaultProtocol = this.GetProtocol();
        var defaultEndpoint = $"{defaultProtocol}://{defaultHost}:{defaultPort}";
        this._logger.LogInformation("Using default Docker network for {ServiceName}: {Endpoint}", serviceDisplayName, defaultEndpoint);
        if (logAspireWarning)
        {
            this._logger.LogWarning("No configuration found for {ServiceName} - using default endpoint", serviceDisplayName);
        }
        return defaultEndpoint;
    }

    /// <summary>
    /// Gets the protocol (http or https) from configuration or environment variable.
    /// Defaults to http for backward compatibility.
    /// </summary>
    /// <returns>The protocol string ("http" or "https").</returns>
    private string GetProtocol()
    {
        // Check environment variable first
        var envProtocol = Environment.GetEnvironmentVariable("FLINK_PROTOCOL");
        if (!string.IsNullOrEmpty(envProtocol))
        {
            var protocol = envProtocol.Trim().ToUpperInvariant();
            if (protocol == ProtocolHttps)
            {
                this._logger.LogInformation("Using HTTPS protocol from FLINK_PROTOCOL environment variable");
                return "https";
            }
            if (protocol != ProtocolHttp)
            {
                this._logger.LogWarning("Invalid FLINK_PROTOCOL value '{Protocol}', defaulting to http", envProtocol);
            }
        }

        // Check configuration
        var configProtocol = this._configuration["Flink:Protocol"];
        if (!string.IsNullOrEmpty(configProtocol))
        {
            var protocol = configProtocol.Trim().ToUpperInvariant();
            if (protocol == ProtocolHttps)
            {
                this._logger.LogInformation("Using HTTPS protocol from configuration");
                return "https";
            }
            if (protocol != "http")
            {
                this._logger.LogWarning("Invalid Flink:Protocol configuration value '{Protocol}', defaulting to http", configProtocol);
            }
        }

        // Default to http
        return "http";
    }

    private async Task WaitForSqlGatewayReadyAsync(HttpClient client)
    {
        var maxRetries = 60; // 60 seconds total wait time (SQL Gateway needs time to start after JobManager)

        this._logger.LogInformation("Waiting for SQL Gateway to become ready at {BaseAddress}", client.BaseAddress);

        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                this._logger.LogInformation("Checking SQL Gateway availability (attempt {Attempt}/{Max})", i + 1, maxRetries);
                var response = await client.GetAsync("/v1/info");

                if (response.IsSuccessStatusCode)
                {
                    var infoContent = await response.Content.ReadAsStringAsync();
                    this._logger.LogInformation("SQL Gateway is ready and responding: {Info}", infoContent);
                    return;
                }

                this._logger.LogWarning("SQL Gateway returned {StatusCode}, retrying...", response.StatusCode);
            }
            catch (HttpRequestException ex)
            {
                this._logger.LogWarning(ex, "SQL Gateway not yet available (attempt {Attempt}/{Max})", i + 1, maxRetries);
            }
            catch (TaskCanceledException ex)
            {
                this._logger.LogWarning(ex, "SQL Gateway request timed out (attempt {Attempt}/{Max})", i + 1, maxRetries);
            }

            await Task.Delay(SqlGatewayRetryDelay);
        }

        throw new InvalidOperationException($"SQL Gateway at {client.BaseAddress} did not become ready after {maxRetries} seconds");
    }

    /// <summary>
    /// Logs a section header with box drawing for better visibility.
    /// </summary>
    private void LogSectionHeader(string title, params (string Label, string Value)[] details)
    {
        this._logger.LogInformation("╔══════════════════════════════════════════════════════════════");
        this._logger.LogInformation("║ {Title}", title);
        foreach (var (label, value) in details)
        {
            this._logger.LogInformation("║ {Label}: {Value}", label, value);
        }
        this._logger.LogInformation("╚══════════════════════════════════════════════════════════════");
    }

    /// <summary>
    /// Submits a Flink job to the cluster based on the provided job definition.
    /// </summary>
    /// <param name="jobDefinition">The job definition containing SQL or JAR source configuration.</param>
    /// <returns>A task containing the job submission result with success status and Flink job ID.</returns>
    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        this.LogSectionHeader("🔧 [FlinkJobManager] Processing job submission",
            ("📋 JobId", jobDefinition.Metadata.JobId),
            ("📝 Job Name", jobDefinition.Metadata.JobName ?? "Unnamed"));

        try
        {
            var validation = this.ValidateJobDefinition(jobDefinition);
            if (!validation.IsValid)
            {
                this._logger.LogError("❌ Job validation failed: {Errors}", string.Join(", ", validation.Errors));
                return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId,
                    $"Job validation failed: {string.Join(", ", validation.Errors)}");
            }
            this._logger.LogInformation("✅ Job definition validated successfully");

            // Check if this is a SQL Gateway job
            if (jobDefinition.Source is SqlSourceDefinition sqlSource &&
                sqlSource.ExecutionMode == "gateway")
            {
                this._logger.LogInformation("🔀 Detected SQL Gateway execution mode for job {JobId}", jobDefinition.Metadata.JobId);

                // SQL Gateway jobs are submitted directly via SQL Gateway REST API
                // No need to check JobManager cluster health - SQL Gateway handles job submission
                var flinkJobId = await this.SubmitSqlGatewayJobAsync(sqlSource, jobDefinition);
                this.TrackJob(jobDefinition, flinkJobId);
                return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId);
            }

            // Standard JAR submission flow (including TableEnvironment SQL)
            this._logger.LogInformation("🔄 Using standard JAR submission flow");
            var irBase64 = this.EncodeJobDefinition(jobDefinition);

            this._logger.LogInformation("🔍 Probing Flink cluster health...");
            var clusterHealthy2 = await this.ProbeClusterHealthSafelyAsync();

            if (!clusterHealthy2)
            {
                var flinkUrl = this._httpClient.BaseAddress?.ToString() ?? "(unknown)";
                var errorMessage = $"Flink cluster is not healthy or unreachable. Cannot submit job. Please ensure Flink JobManager is running and accessible at {flinkUrl}";
                this._logger.LogError("❌ {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var flinkJobId2 = await this.SubmitJobToFlinkClusterAsync(irBase64, jobDefinition);
            this.TrackJob(jobDefinition, flinkJobId2);

            this._logger.LogInformation("✅ Job submitted successfully to Flink cluster");
            return JobSubmissionResult.CreateSuccess(jobDefinition.Metadata.JobId, flinkJobId2);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit job {JobId}: {Message}", jobDefinition.Metadata.JobId, ex.Message);
            return JobSubmissionResult.CreateFailure(jobDefinition.Metadata.JobId, ex.Message);
        }
    }

    private string EncodeJobDefinition(JobDefinition jobDefinition)
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
        };
        var irJson = JsonSerializer.Serialize(jobDefinition, serializerOptions);

        this.LogJobDefinitionDiagnostics(irJson, jobDefinition);

        return Convert.ToBase64String(Encoding.UTF8.GetBytes(irJson));
    }

    private void LogJobDefinitionDiagnostics(string irJson, JobDefinition jobDefinition)
    {
        this._logger.LogDebug("════════════════════════════════════════════════════════════");
        this._logger.LogDebug("Complete Job Definition JSON:");
        this._logger.LogDebug("{IrJson}", irJson);
        this._logger.LogDebug("════════════════════════════════════════════════════════════");

        this.LogKafkaConfiguration(jobDefinition);
        this.LogOperations(jobDefinition);
    }

    private void LogKafkaConfiguration(JobDefinition jobDefinition)
    {
        var kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
        var kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
        this._logger.LogDebug("Source BootstrapServers: {SourceBootstrapServers}", kafkaSource?.BootstrapServers ?? "null");
        this._logger.LogDebug("Sink BootstrapServers: {SinkBootstrapServers}", kafkaSink?.BootstrapServers ?? "null");
        this._logger.LogDebug("Operations Count: {OperationsCount}", jobDefinition.Operations?.Count ?? 0);
    }

    private void LogOperations(JobDefinition jobDefinition)
    {
        if (jobDefinition.Operations == null || jobDefinition.Operations.Count == 0)
        {
            return;
        }

        foreach (var op in jobDefinition.Operations)
        {
            if (op is MapOperationDefinition mapOp)
            {
                this._logger.LogDebug("Map Operation - Expression: '{Expression}'", mapOp.Expression);
            }
        }
    }

    private async Task<bool> ProbeClusterHealthSafelyAsync()
    {
        try
        {
            return await this.CheckFlinkClusterHealthAsync();
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Cluster health probe failed; falling back to local mode.");
            return false;
        }
    }

    private void TrackJob(JobDefinition jobDefinition, string flinkJobId)
    {
        this._jobMapping[flinkJobId] = new JobInfo
        {
            JobId = jobDefinition.Metadata.JobId,
            FlinkJobId = flinkJobId,
            Status = "RUNNING",  // Jobs only submitted to healthy clusters
            SubmissionTime = DateTime.UtcNow,
            JobDefinition = jobDefinition
        };
    }

    /// <summary>
    /// Retrieves the current status of a Flink job.
    /// </summary>
    /// <param name="flinkJobId">The Flink job identifier.</param>
    /// <returns>A task containing the job status, or null if the job is not found.</returns>
    public async Task<JobStatus?> GetJobStatusAsync(string flinkJobId)
    {
        this._logger.LogDebug("Query status for {FlinkJobId}", flinkJobId);

        // Validate input before attempting HTTP call to prevent injection attacks
        var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        try
        {
            var response = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                var state = doc.RootElement.TryGetProperty("state", out var stateProp)
                    ? stateProp.GetString() ?? "UNKNOWN"
                    : "UNKNOWN";

                // Try to get JobId from mapping, fallback to FlinkJobId
                var jobId = this._jobMapping.TryGetValue(flinkJobId, out var info) ? info.JobId : flinkJobId;
                return new JobStatus { JobId = jobId, FlinkJobId = flinkJobId, State = state };
            }

            return response.StatusCode == HttpStatusCode.NotFound
                ? null
                : throw new InvalidOperationException($"Unexpected status code querying Flink job status: {(int)response.StatusCode} {response.StatusCode}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to query Flink 2.1.0 cluster for job status: {flinkJobId}", ex);
        }
    }

    /// <summary>
    /// Retrieves metrics for a running Flink job including vertex and checkpoint statistics.
    /// </summary>
    /// <param name="flinkJobId">The Flink job identifier.</param>
    /// <returns>A task containing the job metrics, or null if metrics cannot be retrieved.</returns>
    public async Task<JobMetrics?> GetJobMetricsAsync(string flinkJobId)
    {
        // Validate input before attempting HTTP calls to prevent injection attacks
        _ = ValidateAndSanitizePathSegment(flinkJobId);

        try
        {
            var metrics = new JobMetricsBuilder(flinkJobId);
            await this.CollectVertexMetricsAsync(flinkJobId, metrics);
            await this.CollectCheckpointMetricsAsync(flinkJobId, metrics);
            return metrics.Build();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to query Flink 2.1.0 cluster for job metrics: {flinkJobId}", ex);
        }
    }

    /// <summary>
    /// Cancels a running Flink job.
    /// </summary>
    /// <param name="flinkJobId">The Flink job identifier to cancel.</param>
    /// <returns>A task containing true if the job was successfully cancelled, false otherwise.</returns>
    public async Task<bool> CancelJobAsync(string flinkJobId)
    {
        // Validate input before attempting HTTP calls to prevent injection attacks
        var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        if (this._jobMapping.TryGetValue(flinkJobId, out var info) && info.Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            info.Status = "LOCAL-CANCELED";
            return true;
        }

        try
        {
            this._logger.LogInformation("Attempting to cancel Flink job: {FlinkJobId}", flinkJobId);

            // Try Flink 2.x style first: PATCH /jobs/{jobId}?mode=cancel
            var patchResponse = await this._httpClient.PatchAsync($"/jobs/{sanitizedJobId}?mode=cancel", null);
            if (patchResponse.IsSuccessStatusCode)
            {
                this._logger.LogInformation("Successfully canceled job {FlinkJobId} using PATCH /jobs/{{jobId}}?mode=cancel", flinkJobId);
                if (this._jobMapping.TryGetValue(flinkJobId, out var jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                return true;
            }

            this._logger.LogWarning("PATCH /jobs/{{jobId}}?mode=cancel returned {StatusCode}, trying POST endpoint", patchResponse.StatusCode);

            // Fallback to POST /jobs/{jobId}/cancel (without /v1 prefix)
            var postResponse = await this._httpClient.PostAsync($"/jobs/{sanitizedJobId}/cancel", null);
            if (postResponse.IsSuccessStatusCode)
            {
                this._logger.LogInformation("Successfully canceled job {FlinkJobId} using POST /jobs/{{jobId}}/cancel", flinkJobId);
                if (this._jobMapping.TryGetValue(flinkJobId, out var jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                return true;
            }

            this._logger.LogWarning("Both cancel attempts failed. PATCH: {PatchStatus}, POST: {PostStatus}",
                patchResponse.StatusCode, postResponse.StatusCode);

            if (postResponse.StatusCode == HttpStatusCode.NotFound || patchResponse.StatusCode == HttpStatusCode.NotFound)
            {
                this._logger.LogError("Job {FlinkJobId} not found in Flink cluster during cancellation", flinkJobId);
                return false;
            }

            throw new InvalidOperationException($"Unexpected status code canceling Flink job: PATCH={patchResponse.StatusCode}, POST={postResponse.StatusCode}");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to cancel job {FlinkJobId} in Flink 2.1.0 cluster", flinkJobId);
            throw new InvalidOperationException($"Failed to cancel job in Flink 2.1.0 cluster: {flinkJobId}", ex);
        }
    }

    private async Task<string> EnsureRunnerJarPathAsync()
    {
        // First try to find existing jar in working directory or repo structure
        var jarPath = FindExistingRunnerJar();
        if (jarPath != null && File.Exists(jarPath))
        {
            this._logger.LogDebug("Found existing runner jar at {Path}", jarPath);
            return jarPath;
        }

        // Build jar on demand using Maven directly
        this._logger.LogInformation("Runner jar not found, building on demand with Maven...");
        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot == null)
        {
            throw new InvalidOperationException("Could not locate repository root for Maven build");
        }

        var runnerDir = Path.Combine(repoRoot, FlinkIRRunnerDirectory);
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

            this._logger.LogDebug("Starting Maven build in {WorkingDir}: mvn {Args}", runnerDir, psi.Arguments);
            var process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Maven process");

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            var stdout = await outputTask;
            var stderr = await errorTask;

            if (process.ExitCode != 0)
            {
                this._logger.LogError("Maven build failed with exit code {ExitCode}\nSTDOUT:\n{Stdout}\nSTDERR:\n{Stderr}",
                    process.ExitCode, stdout, stderr);
                throw new InvalidOperationException($"Maven build failed with exit code {process.ExitCode}");
            }

            this._logger.LogDebug("Maven build completed successfully");
            jarPath = Path.Combine(runnerDir, "target", "flink-ir-runner-java17.jar");
            return File.Exists(jarPath)
                ? jarPath
                : throw new InvalidOperationException($"Maven build completed but jar not found at expected path: {jarPath}");
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            throw new InvalidOperationException("Failed to build runner jar with Maven", ex);
        }
    }



    // ---------------- Metrics helpers ----------------

    private async Task CollectVertexMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);
        var verticesResp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices");
        if (!verticesResp.IsSuccessStatusCode)
        {
            return;
        }

        var verticesJson = await verticesResp.Content.ReadAsStringAsync();
        using var vdoc = JsonDocument.Parse(verticesJson);

        if (!vdoc.RootElement.TryGetProperty("vertices", out var vertsEl) || vertsEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var vertex in vertsEl.EnumerateArray())
        {
            await this.ProcessVertexAsync(flinkJobId, vertex, metrics);
        }
    }

    private async Task ProcessVertexAsync(string flinkJobId, JsonElement vertex, JobMetricsBuilder metrics)
    {
        if (!vertex.TryGetProperty("id", out var idEl))
        {
            return;
        }

        var vertexId = idEl.GetString();
        if (string.IsNullOrEmpty(vertexId))
        {
            return;
        }

        await this.CollectVertexNumericMetricsAsync(flinkJobId, vertexId, metrics);
        await this.CollectVertexBackpressureAsync(flinkJobId, vertexId, metrics);
    }

    private async Task CollectVertexNumericMetricsAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);
        var sanitizedVertexId = ValidateAndSanitizePathSegment(vertexId);
        var mresp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/metrics?get=numRecordsIn,numRecordsOut,parallelism");
        if (!mresp.IsSuccessStatusCode)
        {
            return;
        }

        var metricsList = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync())
            ?? new List<FlinkMetricEntry>();

        foreach (var m in metricsList)
        {
            if (m.Id.Equals("numRecordsIn", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vi))
            {
                metrics.AddRecordsIn(vi);
            }

            if (m.Id.Equals("numRecordsOut", StringComparison.OrdinalIgnoreCase) && long.TryParse(m.Value, out var vo))
            {
                metrics.AddRecordsOut(vo);
            }

            if (m.Id.Equals("parallelism", StringComparison.OrdinalIgnoreCase) && int.TryParse(m.Value, out var p))
            {
                metrics.UpdateMaxParallelism(p);
            }
        }
    }

    private async Task CollectVertexBackpressureAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        try
        {
            var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);
            var sanitizedVertexId = ValidateAndSanitizePathSegment(vertexId);
            var bp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/backpressure");
            if (!bp.IsSuccessStatusCode)
            {
                return;
            }

            var bpStr = await bp.Content.ReadAsStringAsync();
            using var bdoc = JsonDocument.Parse(bpStr);
            var root = bdoc.RootElement;

            var level = ExtractBackpressureLevel(root);
            if (!string.IsNullOrEmpty(level))
            {
                metrics.UpdateWorstBackpressure(level);
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to collect backpressure data for job {flinkJobId}, vertex {vertexId}", ex);
        }
    }

    private async Task CollectCheckpointMetricsAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        var sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        try
        {
            var cps = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/checkpoints");
            if (!cps.IsSuccessStatusCode)
            {
                return;
            }

            var cpsJson = await cps.Content.ReadAsStringAsync();
            using var cdoc = JsonDocument.Parse(cpsJson);
            var root = cdoc.RootElement;

            ProcessCheckpointCounts(root, metrics);
            ProcessCheckpointTimestamps(root, metrics);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to collect checkpoint data for job {flinkJobId}", ex);
        }
    }

    private static void ProcessCheckpointCounts(JsonElement root, JobMetricsBuilder metrics)
    {
        if (!root.TryGetProperty("counts", out var counts))
        {
            return;
        }

        if (!counts.TryGetProperty("completed", out var completedEl))
        {
            return;
        }

        if (!completedEl.TryGetInt32(out var c))
        {
            return;
        }

        metrics.SetCheckpoints(c);
    }

    private static void ProcessCheckpointTimestamps(JsonElement root, JobMetricsBuilder metrics)
    {
        if (!root.TryGetProperty("latest", out var latest))
        {
            return;
        }

        if (!latest.TryGetProperty("completed", out var comp))
        {
            return;
        }

        var ts = ExtractTimestamp(comp, "end_time") ?? ExtractTimestamp(comp, "trigger_timestamp");
        if (ts.HasValue)
        {
            metrics.SetLastCheckpoint(ts.Value);
        }
    }

    private static DateTime? ExtractTimestamp(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var timeEl))
        {
            return null;
        }

        if (timeEl.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        var ms = timeEl.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    }

    private static string? ExtractBackpressureLevel(JsonElement root) =>
        root.TryGetProperty("backpressureLevel", out var lvlEl) ? lvlEl.GetString() :
        root.TryGetProperty("backpressure-level", out var lvlEl2) ? lvlEl2.GetString() :
        null;

}
