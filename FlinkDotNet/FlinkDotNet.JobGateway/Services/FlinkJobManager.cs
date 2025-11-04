using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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

    /// <summary>
    /// Regex to extract hexadecimal characters from Flink Job IDs.
    /// Compiled for better performance with repeated use.
    /// Includes timeout to prevent ReDoS attacks.
    /// </summary>
    private static readonly Regex s_hexOnlyRegex = new("[^0-9a-fA-F]", RegexOptions.Compiled, TimeSpan.FromSeconds(1));

    private readonly ILogger<FlinkJobManager> _logger;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, JobInfo> _jobMapping = new();

    /// <summary>
    /// Cached JsonSerializerOptions to avoid creating new instances for every serialization.
    /// </summary>
    private static readonly JsonSerializerOptions s_jobDefinitionSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
    };

    private static readonly JsonSerializerOptions s_caseInsensitiveDeserializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Thread-safe delay and timeout configuration using Interlocked operations on backing fields (stored as ticks)
    /// These allow concurrent test execution and production job submissions without race conditions
    /// </summary>
    private static long s_sqlGatewayRetryDelayTicks = TimeSpan.FromSeconds(1).Ticks;
    private static long s_jarRegistrationPollingDelayTicks = TimeSpan.FromSeconds(1).Ticks;
    private static long s_jobRecoveryPollingDelayTicks = TimeSpan.FromSeconds(1).Ticks;
    private static long s_jarRegistrationTimeoutTicks = TimeSpan.FromSeconds(30).Ticks;
    private static long s_jobRecoveryTimeoutTicks = TimeSpan.FromSeconds(30).Ticks;

    /// <summary>
    /// Gets or sets the delay between SQL Gateway retry attempts.
    /// Thread-safe for parallel test execution and production job submissions.
    /// </summary>
    public static TimeSpan SqlGatewayRetryDelay
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref s_sqlGatewayRetryDelayTicks));
        set => Interlocked.Exchange(ref s_sqlGatewayRetryDelayTicks, value.Ticks);
    }

    /// <summary>
    /// Gets or sets the delay between JAR registration polling attempts.
    /// Thread-safe for parallel test execution and production job submissions.
    /// </summary>
    public static TimeSpan JarRegistrationPollingDelay
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref s_jarRegistrationPollingDelayTicks));
        set => Interlocked.Exchange(ref s_jarRegistrationPollingDelayTicks, value.Ticks);
    }

    /// <summary>
    /// Gets or sets the delay between job recovery polling attempts.
    /// Thread-safe for parallel test execution and production job submissions.
    /// </summary>
    public static TimeSpan JobRecoveryPollingDelay
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref s_jobRecoveryPollingDelayTicks));
        set => Interlocked.Exchange(ref s_jobRecoveryPollingDelayTicks, value.Ticks);
    }

    /// <summary>
    /// Gets or sets the timeout for JAR registration waiting.
    /// Thread-safe for parallel test execution and production job submissions.
    /// Default: 30 seconds. Set to 1ms in tests for fast failure.
    /// </summary>
    public static TimeSpan JarRegistrationTimeout
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref s_jarRegistrationTimeoutTicks));
        set => Interlocked.Exchange(ref s_jarRegistrationTimeoutTicks, value.Ticks);
    }

    /// <summary>
    /// Gets or sets the timeout for job recovery attempts.
    /// Thread-safe for parallel test execution and production job submissions.
    /// Default: 30 seconds. Set to 1ms in tests for fast failure.
    /// </summary>
    public static TimeSpan JobRecoveryTimeout
    {
        get => TimeSpan.FromTicks(Interlocked.Read(ref s_jobRecoveryTimeoutTicks));
        set => Interlocked.Exchange(ref s_jobRecoveryTimeoutTicks, value.Ticks);
    }

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
        string flinkBaseUrl = this.DiscoverFlinkEndpoint();

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
    private string DiscoverEndpoint(
        string configKey,
        string envHostKey,
        string envPortKey,
        int defaultPort,
        string defaultHost,
        string serviceDisplayName,
        bool logAspireWarning)
    {
        // Strategy 1: Configuration from appsettings.json or injected by infrastructure (Aspire/tests)
        string? configEndpoint = this._configuration[configKey];
        if (!string.IsNullOrEmpty(configEndpoint))
        {
            this._logger.LogInformation("Using configuration for {ServiceName}: {Endpoint}", serviceDisplayName, configEndpoint);
            return configEndpoint;
        }

        // Strategy 2: Explicit environment variables (via IConfiguration - no direct Environment access)
        // IConfiguration automatically includes environment variables, so we read them through configuration
        string? envHost = this._configuration[envHostKey];
        string? envPort = this._configuration[envPortKey];

        if (!string.IsNullOrEmpty(envHost))
        {
            int port = int.TryParse(envPort, out int p) ? p : defaultPort;
            string protocol = this.GetProtocol();
            string envEndpoint = $"{protocol}://{envHost}:{port}";
            this._logger.LogInformation("Using environment variable for {ServiceName}: {Endpoint}", serviceDisplayName, envEndpoint);
            return envEndpoint;
        }

        // Strategy 3: Default fallback for local development
        string defaultProtocol = this.GetProtocol();
        string defaultEndpoint = $"{defaultProtocol}://{defaultHost}:{defaultPort}";
        this._logger.LogInformation("Using default Docker network for {ServiceName}: {Endpoint}", serviceDisplayName, defaultEndpoint);
        if (logAspireWarning)
        {
            this._logger.LogWarning("No configuration found for {ServiceName} - using default endpoint", serviceDisplayName);
        }
        return defaultEndpoint;
    }

    /// <summary>
    /// Gets the protocol (http or https) from configuration.
    /// Defaults to http for backward compatibility.
    /// IConfiguration automatically includes environment variables.
    /// </summary>
    /// <returns>The protocol string ("http" or "https").</returns>
    private string GetProtocol()
    {
        // Check via IConfiguration (includes environment variables automatically)
        string? envProtocol = this._configuration["FLINK_PROTOCOL"];
        if (!string.IsNullOrEmpty(envProtocol))
        {
            string protocol = envProtocol.Trim().ToUpperInvariant();
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
        string? configProtocol = this._configuration["Flink:Protocol"];
        if (!string.IsNullOrEmpty(configProtocol))
        {
            string protocol = configProtocol.Trim().ToUpperInvariant();
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

    private static string NormalizeFlinkJobId(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("Flink JobId cannot be null or empty.", nameof(jobId));
        }

        string hexOnly = s_hexOnlyRegex.Replace(jobId, string.Empty);
#pragma warning disable S4040 // Lowercase normalization is required for Flink job ID compatibility
        return hexOnly.Length != 32
            ? throw new ArgumentException($"Flink JobId must contain exactly 32 hexadecimal characters (received '{jobId}').", nameof(jobId))
            : hexOnly.ToLowerInvariant();
#pragma warning restore S4040
    }

    private async Task WaitForSqlGatewayReadyAsync(HttpClient client)
    {
        const int maxRetries = 60; // 60 seconds total wait time (SQL Gateway needs time to start after JobManager)

        this._logger.LogInformation("Waiting for SQL Gateway to become ready at {BaseAddress}", client.BaseAddress);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                this._logger.LogInformation("Checking SQL Gateway availability (attempt {Attempt}/{Max})", i + 1, maxRetries);
                HttpResponseMessage response = await client.GetAsync("/v1/info");

                if (response.IsSuccessStatusCode)
                {
                    string infoContent = await response.Content.ReadAsStringAsync();
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
        string detailsText = string.Join(" | ", details.Select(d => $"{d.Label}: {d.Value}"));
        this._logger.LogInformation("╔══════════════════════════════════════════════════════════════");
        this._logger.LogInformation("║ {Title} | {Details}\n╚══════════════════════════════════════════════════════════════", title, detailsText);
    }

    /// <summary>
    /// Submits a Flink job to the cluster based on the provided job definition.
    /// </summary>
    /// <param name="jobDefinition">The job definition containing SQL or JAR source configuration.</param>
    /// <returns>A task containing the job submission result with success status and Flink job ID.</returns>
    public async Task<JobSubmissionResult> SubmitJobAsync(JobDefinition jobDefinition)
    {
        this.LogSectionHeader("🔧 [FlinkJobManager] Processing job submission",
            ("📝 Job Name", jobDefinition.Metadata.JobName ?? "Unnamed"));

        try
        {
            JobValidationResult validation = ValidateJobDefinition(jobDefinition);
            if (!validation.IsValid)
            {
                this._logger.LogError("❌ Job validation failed: {Errors}", string.Join(", ", validation.Errors));
                return JobSubmissionResult.CreateFailure(
                    $"Job validation failed: {string.Join(", ", validation.Errors)}");
            }

            // Check if this is a SQL Gateway job
            if (jobDefinition.Source is SqlSourceDefinition sqlSource &&
                sqlSource.ExecutionMode == "gateway")
            {
                this._logger.LogInformation("✅ Validation passed | 🔀 Using SQL Gateway execution mode for job {JobName}", jobDefinition.Metadata.JobName);

                // SQL Gateway jobs are submitted directly via SQL Gateway REST API
                // No need to check JobManager cluster health - SQL Gateway handles job submission
                string rawFlinkJobId = await this.SubmitSqlGatewayJobAsync(sqlSource, jobDefinition);
                string normalizedJobId = NormalizeFlinkJobId(rawFlinkJobId);
                this.TrackJob(jobDefinition, normalizedJobId);
                return JobSubmissionResult.CreateSuccess(normalizedJobId);
            }

            // Standard JAR submission flow (including TableEnvironment SQL)
            this._logger.LogInformation("✅ Validation passed | 🔄 Using standard JAR submission flow | 🔍 Probing Flink cluster health...");
            string irBase64 = this.EncodeJobDefinition(jobDefinition);

            bool clusterHealthy2 = await this.ProbeClusterHealthSafelyAsync();

            if (!clusterHealthy2)
            {
                string flinkUrl = this._httpClient.BaseAddress?.ToString() ?? "(unknown)";
                string errorMessage = $"Flink cluster is not healthy or unreachable. Cannot submit job. Please ensure Flink JobManager is running and accessible at {flinkUrl}";
                this._logger.LogError("❌ {ErrorMessage}", errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            string flinkJobId2 = await this.SubmitJobToFlinkClusterAsync(irBase64, jobDefinition);
            string normalizedClusterJobId = NormalizeFlinkJobId(flinkJobId2);
            this.TrackJob(jobDefinition, normalizedClusterJobId);

            this._logger.LogInformation("✅ Job submitted successfully to Flink cluster");
            return JobSubmissionResult.CreateSuccess(normalizedClusterJobId);
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit job {JobName}: {Message}", jobDefinition.Metadata.JobName, ex.Message);
            return JobSubmissionResult.CreateFailure(ex.Message);
        }
    }

    private string EncodeJobDefinition(JobDefinition jobDefinition)
    {
        string irJson = JsonSerializer.Serialize(jobDefinition, s_jobDefinitionSerializerOptions);

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
        KafkaSourceDefinition? kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
        KafkaSinkDefinition? kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
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

        foreach (object op in jobDefinition.Operations)
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
        string normalizedJobId = NormalizeFlinkJobId(flinkJobId);

        this._jobMapping[normalizedJobId] = new JobInfo
        {
            FlinkJobId = normalizedJobId,
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
        string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        try
        {
            HttpResponseMessage response = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using JsonDocument doc = JsonDocument.Parse(jsonResponse);
                string state = doc.RootElement.TryGetProperty("state", out JsonElement stateProp)
                    ? stateProp.GetString() ?? "UNKNOWN"
                    : "UNKNOWN";

                return new JobStatus { FlinkJobId = flinkJobId, State = state };
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            string errorContent = await response.Content.ReadAsStringAsync();
            this._logger.LogWarning("Flink job status query returned {StatusCode}: {Body}", response.StatusCode, errorContent);
            throw new InvalidOperationException(
                $"Unexpected status code querying Flink job status: {(int) response.StatusCode} {response.StatusCode}. Body: {errorContent}");
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
            JobMetricsBuilder metrics = new(flinkJobId);
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
        string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        if (this._jobMapping.TryGetValue(flinkJobId, out JobInfo? info) && info.Status.StartsWith("LOCAL", StringComparison.OrdinalIgnoreCase))
        {
            info.Status = "LOCAL-CANCELED";
            return true;
        }

        try
        {
            this._logger.LogInformation("Attempting to cancel Flink job: {FlinkJobId}", flinkJobId);

            // Try Flink 2.x style first: PATCH /jobs/{jobId}?mode=cancel
            HttpResponseMessage patchResponse = await this._httpClient.PatchAsync($"/jobs/{sanitizedJobId}?mode=cancel", null);
            if (patchResponse.IsSuccessStatusCode)
            {
                this._logger.LogInformation("Successfully canceled job {FlinkJobId} using PATCH /jobs/{{jobId}}?mode=cancel", flinkJobId);
                if (this._jobMapping.TryGetValue(flinkJobId, out JobInfo? jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                return true;
            }

            this._logger.LogDebug("PATCH cancel returned {PatchStatus}, trying POST fallback", patchResponse.StatusCode);

            // Fallback to POST /jobs/{jobId}/cancel (without /v1 prefix)
            HttpResponseMessage postResponse = await this._httpClient.PostAsync($"/jobs/{sanitizedJobId}/cancel", null);
            if (postResponse.IsSuccessStatusCode)
            {
                this._logger.LogInformation("Successfully canceled job {FlinkJobId} using POST /jobs/{{jobId}}/cancel", flinkJobId);
                if (this._jobMapping.TryGetValue(flinkJobId, out JobInfo? jobInfo))
                {
                    jobInfo.Status = "CANCELED";
                }
                return true;
            }

            this._logger.LogWarning("POST cancel also failed with {PostStatus}", postResponse.StatusCode);

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
        string? jarPath = this.FindExistingRunnerJar();
        if (jarPath != null && File.Exists(jarPath))
        {
            this._logger.LogDebug("Found existing runner jar at {Path}", jarPath);
            return jarPath;
        }

        // Build jar on demand using Maven directly
        this._logger.LogInformation("Runner jar not found, building on demand with Maven...");
        string? repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        ArgumentNullException.ThrowIfNull(repoRoot);

        string runnerDir = Path.Combine(repoRoot, FlinkIRRunnerDirectory);
        string pomFile = Path.Combine(runnerDir, "pom.xml");
        if (!File.Exists(pomFile))
        {
            throw new InvalidOperationException($"Maven pom.xml not found at {pomFile}");
        }

        try
        {
#pragma warning disable S4036 // PATH is required for Maven executable resolution - mvn command relies on PATH
            ProcessStartInfo psi = new()
            {
                FileName = "mvn",
                Arguments = "clean package -DskipTests",
                WorkingDirectory = runnerDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            // Set environment explicitly for security (don't inherit potentially unsafe PATH)
            psi.Environment.Clear();
            psi.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH") ?? "";
#pragma warning restore S4036
            psi.Environment["JAVA_HOME"] = Environment.GetEnvironmentVariable("JAVA_HOME") ?? "";
            psi.Environment["M2_HOME"] = Environment.GetEnvironmentVariable("M2_HOME") ?? "";

            this._logger.LogDebug("Starting Maven build in {WorkingDir}: mvn {Args}", runnerDir, psi.Arguments);
            Process process = Process.Start(psi) ?? throw new InvalidOperationException("Failed to start Maven process");

            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            string stdout = await outputTask;
            string stderr = await errorTask;

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
        string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        // Flink 2.x: Get vertices from job details endpoint, not from /vertices (which doesn't exist)
        HttpResponseMessage jobResp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}");
        if (!jobResp.IsSuccessStatusCode)
        {
            return;
        }

        string jobJson = await jobResp.Content.ReadAsStringAsync();
        using JsonDocument jdoc = JsonDocument.Parse(jobJson);

        if (!jdoc.RootElement.TryGetProperty("vertices", out JsonElement vertsEl) || vertsEl.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement vertex in vertsEl.EnumerateArray())
        {
            await this.ProcessVertexAsync(flinkJobId, vertex, metrics);
        }
    }

    private async Task ProcessVertexAsync(string flinkJobId, JsonElement vertex, JobMetricsBuilder metrics)
    {
        if (!vertex.TryGetProperty("id", out JsonElement idEl))
        {
            return;
        }

        string? vertexId = idEl.GetString();
        if (string.IsNullOrEmpty(vertexId))
        {
            return;
        }

        // OPTIMIZATION: Try to extract metrics from vertex object first (Flink 2.x job details include aggregated metrics)
        // This avoids the empty response issue from /metrics endpoint for completed jobs
        bool metricsExtracted = this.TryExtractVertexMetricsFromJobDetails(vertex, metrics);

        // ALTERNATIVE SOURCE: Query Prometheus for operator-level Kafka metrics
        // Prometheus scrapes detailed operator metrics that may not be in job details API
        await this.TryCollectMetricsFromPrometheusAsync(flinkJobId, metrics);

        if (!metricsExtracted)
        {
            // Final fallback: try the Flink metrics endpoint (though it may return empty for completed jobs)
            await this.CollectVertexNumericMetricsAsync(flinkJobId, vertexId, metrics);
        }

        await this.CollectVertexBackpressureAsync(flinkJobId, vertexId, metrics);
    }

    private bool TryExtractVertexMetricsFromJobDetails(JsonElement vertex, JobMetricsBuilder metrics)
    {
        // Flink 2.x job details endpoint includes aggregated metrics for each vertex
        // Format: { "vertices": [{ "id": "...", "metrics": { "read-records": 100, "write-records": 100, ... } }] }
        if (!vertex.TryGetProperty("metrics", out JsonElement metricsEl))
        {
            return false;
        }

        bool foundAnyMetrics = false;

        // Extract read-records (input to vertex = RecordsIn for source)
        if (metricsEl.TryGetProperty("read-records", out JsonElement readRecordsEl) &&
            readRecordsEl.ValueKind == JsonValueKind.Number)
        {
            long readRecords = readRecordsEl.GetInt64();
            this._logger.LogInformation("Found vertex read-records (RecordsIn): {Value}", readRecords);
            metrics.AddRecordsIn(readRecords);
            foundAnyMetrics = true;
        }

        // Extract write-records (output from vertex = RecordsOut for sink)
        if (metricsEl.TryGetProperty("write-records", out JsonElement writeRecordsEl) &&
            writeRecordsEl.ValueKind == JsonValueKind.Number)
        {
            long writeRecords = writeRecordsEl.GetInt64();
            this._logger.LogInformation("Found vertex write-records (RecordsOut): {Value}", writeRecords);
            metrics.AddRecordsOut(writeRecords);
            foundAnyMetrics = true;
        }

        // Extract parallelism
        if (vertex.TryGetProperty("parallelism", out JsonElement parallelismEl) &&
            parallelismEl.ValueKind == JsonValueKind.Number)
        {
            int parallelism = parallelismEl.GetInt32();
            this._logger.LogInformation("Found vertex parallelism: {Value}", parallelism);
            metrics.UpdateMaxParallelism(parallelism);
            foundAnyMetrics = true;
        }

        return foundAnyMetrics;
    }

    private async Task<bool> TryCollectMetricsFromPrometheusAsync(string flinkJobId, JobMetricsBuilder metrics)
    {
        try
        {
            // Get Prometheus base URL from configuration
            // In Aspire deployments, this is typically "http://prometheus:9090" (container-to-container DNS)
            string? prometheusUrl = this._configuration["Flink:Prometheus:BaseUrl"];

            if (string.IsNullOrEmpty(prometheusUrl))
            {
                this._logger.LogDebug("Prometheus URL not configured, skipping Prometheus metrics query");
                return false;
            }

            this._logger.LogInformation("Querying Prometheus at {Url} for Flink job {JobId} comprehensive metrics", prometheusUrl, flinkJobId);

            bool foundMetrics = false;

            // ===== OPERATOR METRICS (Kafka Source/Sink) =====
            // Query for Kafka source operator numRecordsOut (= RecordsIn to the job)
            string sourceRecordsQuery = $"sum(flink_taskmanager_job_task_operator_numRecordsOut{{job_id=\"{flinkJobId}\",operator_name=~\".*Source.*\"}})";
            long? recordsIn = await this.QueryPrometheusMetricAsync(prometheusUrl, sourceRecordsQuery);

            // Query for Kafka sink operator numRecordsIn (= RecordsOut from the job)
            string sinkRecordsQuery = $"sum(flink_taskmanager_job_task_operator_numRecordsIn{{job_id=\"{flinkJobId}\",operator_name=~\".*Sink.*\"}})";
            long? recordsOut = await this.QueryPrometheusMetricAsync(prometheusUrl, sinkRecordsQuery);

            // Query for Kafka source operator numBytesOut (bytes read from Kafka)
            string sourceBytesQuery = $"sum(flink_taskmanager_job_task_operator_numBytesOut{{job_id=\"{flinkJobId}\",operator_name=~\".*Source.*\"}})";
            long? bytesRead = await this.QueryPrometheusMetricAsync(prometheusUrl, sourceBytesQuery);

            // Query for Kafka sink operator numBytesIn (bytes written to Kafka)
            string sinkBytesQuery = $"sum(flink_taskmanager_job_task_operator_numBytesIn{{job_id=\"{flinkJobId}\",operator_name=~\".*Sink.*\"}})";
            long? bytesWritten = await this.QueryPrometheusMetricAsync(prometheusUrl, sinkBytesQuery);

            if (recordsIn.HasValue && recordsIn.Value > 0)
            {
                this._logger.LogInformation("Found RecordsIn from Prometheus: {Value}", recordsIn.Value);
                metrics.AddRecordsIn(recordsIn.Value);
                foundMetrics = true;
            }

            if (recordsOut.HasValue && recordsOut.Value > 0)
            {
                this._logger.LogInformation("Found RecordsOut from Prometheus: {Value}", recordsOut.Value);
                metrics.AddRecordsOut(recordsOut.Value);
                foundMetrics = true;
            }

            if (bytesRead.HasValue && bytesRead.Value > 0)
            {
                this._logger.LogInformation("Found BytesRead from Prometheus: {Value}", bytesRead.Value);
                metrics.AddBytesRead(bytesRead.Value);
                foundMetrics = true;
            }

            if (bytesWritten.HasValue && bytesWritten.Value > 0)
            {
                this._logger.LogInformation("Found BytesWritten from Prometheus: {Value}", bytesWritten.Value);
                metrics.AddBytesWritten(bytesWritten.Value);
                foundMetrics = true;
            }

            // ===== TASKMANAGER METRICS =====
            // CPU Load - average across all TaskManagers
            string tmCpuQuery = "avg(flink_taskmanager_Status_JVM_CPU_Load)";
            long? tmCpuLoad = await this.QueryPrometheusMetricAsync(prometheusUrl, tmCpuQuery);
            if (tmCpuLoad.HasValue)
            {
                metrics.AddCustomMetric("TaskManager.CPU.Load", tmCpuLoad.Value);
                this._logger.LogInformation("Found TaskManager CPU Load: {Value}%", tmCpuLoad.Value);
                foundMetrics = true;
            }

            // Heap Memory Used - sum across all TaskManagers
            string tmHeapQuery = "sum(flink_taskmanager_Status_JVM_Memory_Heap_Used)";
            long? tmHeapUsed = await this.QueryPrometheusMetricAsync(prometheusUrl, tmHeapQuery);
            if (tmHeapUsed.HasValue)
            {
                metrics.AddCustomMetric("TaskManager.Memory.Heap.Used", tmHeapUsed.Value);
                this._logger.LogInformation("Found TaskManager Heap Used: {Value} bytes", tmHeapUsed.Value);
                foundMetrics = true;
            }

            // Number of running tasks - count tasks with operator metrics
            // Note: Counts operators that have numRecordsIn metrics exposed, which represents
            // tasks that have been initialized with Flink operators (not necessarily actively processing)
            string tmTasksQuery = $"count(flink_taskmanager_job_task_operator_numRecordsIn{{job_id=\"{flinkJobId}\"}})";
            long? activeTasks = await this.QueryPrometheusMetricAsync(prometheusUrl, tmTasksQuery);
            if (activeTasks.HasValue)
            {
                metrics.AddCustomMetric("TaskManager.ActiveTasks", activeTasks.Value);
                this._logger.LogInformation("Found Active Tasks: {Value}", activeTasks.Value);
                foundMetrics = true;
            }

            // ===== JOBMANAGER METRICS =====
            // CPU Load
            string jmCpuQuery = "flink_jobmanager_Status_JVM_CPU_Load";
            long? jmCpuLoad = await this.QueryPrometheusMetricAsync(prometheusUrl, jmCpuQuery);
            if (jmCpuLoad.HasValue)
            {
                metrics.AddCustomMetric("JobManager.CPU.Load", jmCpuLoad.Value);
                this._logger.LogInformation("Found JobManager CPU Load: {Value}%", jmCpuLoad.Value);
                foundMetrics = true;
            }

            // Heap Memory Used
            string jmHeapQuery = "flink_jobmanager_Status_JVM_Memory_Heap_Used";
            long? jmHeapUsed = await this.QueryPrometheusMetricAsync(prometheusUrl, jmHeapQuery);
            if (jmHeapUsed.HasValue)
            {
                metrics.AddCustomMetric("JobManager.Memory.Heap.Used", jmHeapUsed.Value);
                this._logger.LogInformation("Found JobManager Heap Used: {Value} bytes", jmHeapUsed.Value);
                foundMetrics = true;
            }

            // Number of running jobs - count jobs by state (uptime metric only exists for running jobs)
            string jmRunningJobsQuery = "count(flink_jobmanager_job_uptime{job_state=\"RUNNING\"})";
            long? runningJobs = await this.QueryPrometheusMetricAsync(prometheusUrl, jmRunningJobsQuery);
            if (runningJobs.HasValue)
            {
                metrics.AddCustomMetric("JobManager.RunningJobs", runningJobs.Value);
                this._logger.LogInformation("Found Running Jobs: {Value}", runningJobs.Value);
                foundMetrics = true;
            }

            // ===== KAFKA TOPIC METRICS (if available via kafka-exporter) =====
            // Consumer lag - requires kafka-exporter or similar
            string kafkaLagQuery = $"sum(kafka_consumergroup_lag{{consumergroup=~\".*{flinkJobId.Substring(0, Math.Min(8, flinkJobId.Length))}.*\"}})";
            long? consumerLag = await this.QueryPrometheusMetricAsync(prometheusUrl, kafkaLagQuery);
            if (consumerLag.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Consumer.Lag", consumerLag.Value);
                this._logger.LogInformation("Found Kafka Consumer Lag: {Value}", consumerLag.Value);
                foundMetrics = true;
            }

            // Topic partition offsets (highest offset across all partitions)
            // This shows total messages available in topics
            string topicOffsetsQuery = "sum by (topic) (kafka_topic_partition_current_offset)";
            long? topicOffsets = await this.QueryPrometheusMetricAsync(prometheusUrl, topicOffsetsQuery);
            if (topicOffsets.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Topic.TotalOffsets", topicOffsets.Value);
                this._logger.LogInformation("Found Kafka Topic Total Offsets: {Value}", topicOffsets.Value);
                foundMetrics = true;
            }

            // Topic partition count (number of partitions across all topics being monitored)
            string partitionCountQuery = "count(kafka_topic_partition_current_offset)";
            long? partitionCount = await this.QueryPrometheusMetricAsync(prometheusUrl, partitionCountQuery);
            if (partitionCount.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Topic.PartitionCount", partitionCount.Value);
                this._logger.LogInformation("Found Kafka Topic Partition Count: {Value}", partitionCount.Value);
                foundMetrics = true;
            }

            // Consumer group current offset (where consumers are reading from)
            string consumerOffsetQuery = "sum by (consumergroup) (kafka_consumergroup_current_offset)";
            long? consumerOffset = await this.QueryPrometheusMetricAsync(prometheusUrl, consumerOffsetQuery);
            if (consumerOffset.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Consumer.CurrentOffset", consumerOffset.Value);
                this._logger.LogInformation("Found Kafka Consumer Current Offset: {Value}", consumerOffset.Value);
                foundMetrics = true;
            }

            // Messages in flight (difference between latest and committed offsets across all partitions)
            string messagesInFlightQuery = "sum(kafka_topic_partition_current_offset - kafka_consumergroup_current_offset)";
            long? messagesInFlight = await this.QueryPrometheusMetricAsync(prometheusUrl, messagesInFlightQuery);
            if (messagesInFlight.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Topic.MessagesInFlight", messagesInFlight.Value);
                this._logger.LogInformation("Found Kafka Messages In Flight: {Value}", messagesInFlight.Value);
                foundMetrics = true;
            }

            // Topic message rate (messages produced per second)
            string topicMessageRateQuery = "sum(rate(kafka_topic_partition_current_offset[1m]))";
            long? topicMessageRate = await this.QueryPrometheusMetricAsync(prometheusUrl, topicMessageRateQuery);
            if (topicMessageRate.HasValue)
            {
                metrics.AddCustomMetric("Kafka.Topic.MessageRate", topicMessageRate.Value);
                this._logger.LogInformation("Found Kafka Topic Message Rate: {Value} msg/sec", topicMessageRate.Value);
                foundMetrics = true;
            }

            return foundMetrics;
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Failed to query Prometheus for metrics");
            return false;
        }
    }

    private async Task<long?> QueryPrometheusMetricAsync(string prometheusUrl, string query)
    {
        try
        {
            // Prometheus HTTP API: /api/v1/query?query={promql}
            string encodedQuery = Uri.EscapeDataString(query);
            string url = $"{prometheusUrl}/api/v1/query?query={encodedQuery}";

            this._logger.LogDebug("Querying Prometheus: {Query}", query);

            using HttpClient promClient = new()
            {
                Timeout = TimeSpan.FromSeconds(10)
            };
            HttpResponseMessage response = await promClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Prometheus query failed with status {Status}", response.StatusCode);
                return null;
            }

            string json = await response.Content.ReadAsStringAsync();
            using JsonDocument doc = JsonDocument.Parse(json);

            // Prometheus response format: { "status": "success", "data": { "resultType": "vector", "result": [{ "value": [timestamp, "value"] }] } }
            if (!doc.RootElement.TryGetProperty("status", out JsonElement statusEl) ||
                statusEl.GetString() != "success")
            {
                this._logger.LogWarning("Prometheus query returned non-success status");
                return null;
            }

            if (!doc.RootElement.TryGetProperty("data", out JsonElement dataEl) ||
                !dataEl.TryGetProperty("result", out JsonElement resultEl) ||
                resultEl.GetArrayLength() == 0)
            {
                this._logger.LogDebug("Prometheus query returned no results");
                return null;
            }

            // Get the first result's value [timestamp, "value"]
            JsonElement firstResult = resultEl[0];
            if (firstResult.TryGetProperty("value", out JsonElement valueArray) &&
                valueArray.GetArrayLength() >= 2)
            {
                string valueStr = valueArray[1].GetString() ?? "0";
                if (long.TryParse(valueStr, out long value))
                {
                    return value;
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            this._logger.LogWarning(ex, "Error querying Prometheus metric: {Query}", query);
            return null;
        }
    }

    private async Task CollectVertexNumericMetricsAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);
        string sanitizedVertexId = ValidateAndSanitizePathSegment(vertexId);

        // Flink 2.x: First get all available metrics to discover operator-specific metric names
        string metricsUrl = $"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/metrics";
        this._logger.LogDebug("Fetching metrics from URL: {Url}", metricsUrl);

        HttpResponseMessage allMetricsResp = await this._httpClient.GetAsync(metricsUrl);
        if (!allMetricsResp.IsSuccessStatusCode)
        {
            this._logger.LogWarning("Failed to get metrics list for vertex {VertexId}: {StatusCode}", vertexId, allMetricsResp.StatusCode);
            return;
        }

        string allMetricsJson = await allMetricsResp.Content.ReadAsStringAsync();
        List<FlinkMetricEntry> allMetrics = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(allMetricsJson, s_caseInsensitiveDeserializerOptions) ?? [];

        this._logger.LogDebug("Found {Count} total metrics for vertex {VertexId}", allMetrics.Count, vertexId);

        // Find operator-specific metrics for Source (RecordsOut = data entering job) and Sink (RecordsIn = data leaving job)
        // Metrics are prefixed with subtask index (e.g., "0.Source__KafkaSource.numRecordsOut")
        List<string> sourceMetrics = [.. allMetrics.Where(m => m.Id.Contains("Source") && m.Id.Contains("numRecordsOut")).Select(m => m.Id)];
        List<string> sinkMetrics = [.. allMetrics.Where(m => m.Id.Contains("Sink") && m.Id.Contains("numRecordsIn")).Select(m => m.Id)];
        List<string> parallelismMetrics = [.. allMetrics.Where(m => m.Id.Contains("parallelism")).Select(m => m.Id)];

        this._logger.LogDebug("Found {SourceCount} Source metrics, {SinkCount} Sink metrics, {ParallelismCount} parallelism metrics",
            sourceMetrics.Count, sinkMetrics.Count, parallelismMetrics.Count);

        // Build query string for operator-specific metrics
        List<string> metricsToQuery = [.. sourceMetrics, .. sinkMetrics, .. parallelismMetrics];

        if (metricsToQuery.Count == 0)
        {
            // Fallback: try generic metrics (may not work in Flink 2.x)
            this._logger.LogWarning("No operator-specific metrics found for vertex {VertexId}, trying generic metrics", vertexId);
            HttpResponseMessage fallbackResp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/metrics?get=numRecordsIn,numRecordsOut,parallelism");
            if (!fallbackResp.IsSuccessStatusCode)
            {
                this._logger.LogWarning("Fallback metrics query failed for vertex {VertexId}: {StatusCode}", vertexId, fallbackResp.StatusCode);
                return;
            }

            List<FlinkMetricEntry> fallbackMetrics = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await fallbackResp.Content.ReadAsStringAsync(), s_caseInsensitiveDeserializerOptions) ?? [];
            this.ProcessMetricValues(fallbackMetrics, metrics);
            return;
        }

        // Query the specific metrics we found
        string metricsQuery = string.Join(",", metricsToQuery);
        this._logger.LogDebug("Querying {Count} metrics for vertex {VertexId}", metricsToQuery.Count, vertexId);

        HttpResponseMessage mresp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/metrics?get={metricsQuery}");
        if (!mresp.IsSuccessStatusCode)
        {
            this._logger.LogWarning("Metrics query failed for vertex {VertexId}: {StatusCode}", vertexId, mresp.StatusCode);
            return;
        }

        List<FlinkMetricEntry> metricsList = JsonSerializer.Deserialize<List<FlinkMetricEntry>>(await mresp.Content.ReadAsStringAsync(), s_caseInsensitiveDeserializerOptions) ?? [];
        this.ProcessMetricValues(metricsList, metrics);
    }

    private void ProcessMetricValues(List<FlinkMetricEntry> metricsList, JobMetricsBuilder metrics)
    {
        foreach (FlinkMetricEntry m in metricsList)
        {
            this.ProcessSourceMetrics(m, metrics);
            this.ProcessSinkMetrics(m, metrics);
            this.ProcessGenericMetrics(m, metrics);
            this.ProcessParallelismMetric(m, metrics);
        }
    }

    private void ProcessSourceMetrics(FlinkMetricEntry metric, JobMetricsBuilder metrics)
    {
        // Source operator's numRecordsOut = records entering the job (RecordsIn)
        if (metric.Id.Contains("Source") && metric.Id.Contains("numRecordsOut") && long.TryParse(metric.Value, out long sourceOut))
        {
            this._logger.LogInformation("Found Source.numRecordsOut: {Value}", sourceOut);
            metrics.AddRecordsIn(sourceOut);
        }
    }

    private void ProcessSinkMetrics(FlinkMetricEntry metric, JobMetricsBuilder metrics)
    {
        // Sink operator's numRecordsIn = records leaving the job (RecordsOut)
        if (metric.Id.Contains("Sink") && metric.Id.Contains("numRecordsIn") && long.TryParse(metric.Value, out long sinkIn))
        {
            this._logger.LogInformation("Found Sink.numRecordsIn: {Value}", sinkIn);
            metrics.AddRecordsOut(sinkIn);
        }
    }

    private void ProcessGenericMetrics(FlinkMetricEntry metric, JobMetricsBuilder metrics)
    {
        // Fallback for generic metrics (backward compatibility)
        if (metric.Id.Equals("numRecordsIn", StringComparison.OrdinalIgnoreCase) && long.TryParse(metric.Value, out long vi))
        {
            this._logger.LogInformation("Found generic numRecordsIn: {Value}", vi);
            metrics.AddRecordsIn(vi);
        }

        if (metric.Id.Equals("numRecordsOut", StringComparison.OrdinalIgnoreCase) && long.TryParse(metric.Value, out long vo))
        {
            this._logger.LogInformation("Found generic numRecordsOut: {Value}", vo);
            metrics.AddRecordsOut(vo);
        }
    }

    private void ProcessParallelismMetric(FlinkMetricEntry metric, JobMetricsBuilder metrics)
    {
        if (metric.Id.Contains("parallelism") && int.TryParse(metric.Value, out int p))
        {
            this._logger.LogDebug("Found parallelism: {Value}", p);
            metrics.UpdateMaxParallelism(p);
        }
    }

    private async Task CollectVertexBackpressureAsync(string flinkJobId, string vertexId, JobMetricsBuilder metrics)
    {
        try
        {
            string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);
            string sanitizedVertexId = ValidateAndSanitizePathSegment(vertexId);
            HttpResponseMessage bp = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/vertices/{sanitizedVertexId}/backpressure");
            if (!bp.IsSuccessStatusCode)
            {
                return;
            }

            string bpStr = await bp.Content.ReadAsStringAsync();
            using JsonDocument bdoc = JsonDocument.Parse(bpStr);
            JsonElement root = bdoc.RootElement;

            string? level = ExtractBackpressureLevel(root);
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
        string sanitizedJobId = ValidateAndSanitizePathSegment(flinkJobId);

        try
        {
            HttpResponseMessage cps = await this._httpClient.GetAsync($"/v1/jobs/{sanitizedJobId}/checkpoints");
            if (!cps.IsSuccessStatusCode)
            {
                return;
            }

            string cpsJson = await cps.Content.ReadAsStringAsync();
            using JsonDocument cdoc = JsonDocument.Parse(cpsJson);
            JsonElement root = cdoc.RootElement;

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
        if (!root.TryGetProperty("counts", out JsonElement counts))
        {
            return;
        }

        if (!counts.TryGetProperty("completed", out JsonElement completedEl))
        {
            return;
        }

        if (!completedEl.TryGetInt32(out int c))
        {
            return;
        }

        metrics.SetCheckpoints(c);
    }

    private static void ProcessCheckpointTimestamps(JsonElement root, JobMetricsBuilder metrics)
    {
        if (!root.TryGetProperty("latest", out JsonElement latest))
        {
            return;
        }

        if (!latest.TryGetProperty("completed", out JsonElement comp))
        {
            return;
        }

        DateTime? ts = ExtractTimestamp(comp, "end_time") ?? ExtractTimestamp(comp, "trigger_timestamp");
        if (ts.HasValue)
        {
            metrics.SetLastCheckpoint(ts.Value);
        }
    }

    private static DateTime? ExtractTimestamp(JsonElement element, string propertyName)
    {
        // Handle null elements - can't get properties from a null JSON element
        if (element.ValueKind == JsonValueKind.Null || element.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (!element.TryGetProperty(propertyName, out JsonElement timeEl))
        {
            return null;
        }

        // Handle null values in JSON (e.g., when checkpoints haven't been created yet)
        if (timeEl.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        if (timeEl.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        long ms = timeEl.GetInt64();
        return DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime;
    }

    private static string? ExtractBackpressureLevel(JsonElement root)
    {
#pragma warning disable IDE0046 // Simplified form would create nested ternary which violates S3358
        if (root.TryGetProperty("backpressureLevel", out JsonElement lvlEl))
        {
            return lvlEl.GetString();
        }

        return root.TryGetProperty("backpressure-level", out JsonElement lvlEl2) ? lvlEl2.GetString() : null;
#pragma warning restore IDE0046
    }
}
