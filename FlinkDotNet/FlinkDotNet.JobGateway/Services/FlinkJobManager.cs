using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.JobGateway.Services;

/// <summary>
/// Manages Apache Flink job lifecycle including submission, status monitoring, and cancellation.
/// Note: This gateway intentionally converts exceptions into domain objects with selective rethrowing.
/// </summary>
public class FlinkJobManager : IFlinkJobManager
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

    private static string? FindExistingRunnerJar()
    {
        // Check if FLINK_RUNNER_JAR_PATH is set
        var envPath = Environment.GetEnvironmentVariable("FLINK_RUNNER_JAR_PATH");
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        // Use Java 17 JAR for Flink 2.1.0 compatibility
        var names = new[] { "flink-ir-runner-java17.jar" };
        var baseDirs = new[]
        {
            Environment.CurrentDirectory,
            Path.Combine(Environment.CurrentDirectory, FlinkIRRunnerDirectory, "target")
        };

        var searchPaths = baseDirs.SelectMany(d => names.Select(n => Path.Combine(d, n))).ToArray();

        var repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            var repoCandidates = new[]
            {
                Path.Combine(repoRoot, FlinkIRRunnerDirectory, "target"),
                repoRoot,
            };
            searchPaths = searchPaths.Concat(repoCandidates.SelectMany(d => names.Select(n => Path.Combine(d, n)))).ToArray();
        }

        return Array.Find(searchPaths, File.Exists);
    }

    private async Task<bool> CheckFlinkClusterHealthAsync()
    {
        try
        {
            var response = await this._httpClient.GetAsync("/v1/overview");
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
            this.LogSectionHeader("📡 [FlinkJobManager] Submitting job to Flink JobManager",
                ("🌐 Target", this._httpClient.BaseAddress?.ToString() ?? "unknown"));

            var jarId = await this.EnsureRunnerJarAsync();
            this._logger.LogInformation("✅ Flink runner JAR ready: {JarId}", jarId);

            // DIAGNOSTIC: Log job definition bootstrap servers before submission
            var kafkaSource = jobDefinition.Source as KafkaSourceDefinition;
            var kafkaSink = jobDefinition.Sink as KafkaSinkDefinition;
            this._logger.LogInformation("📋 Job Kafka config: Source={SourceBootstrap}, Sink={SinkBootstrap}",
                kafkaSource?.BootstrapServers ?? "null",
                kafkaSink?.BootstrapServers ?? "null");

            // DIAGNOSTIC: Log environment variables that might override job definition
            this._logger.LogInformation("🌍 Gateway environment: KAFKA_BOOTSTRAP={KafkaBootstrap}",
                Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP") ?? "not set");
            this._logger.LogWarning("⚠️ CRITICAL: Flink containers have KAFKA_BOOTSTRAP set, which will override job definition bootstrap servers in FlinkJobRunner.java!");
            this._logger.LogWarning("⚠️ FlinkJobRunner.java line 93 uses: orElse(k.bootstrapServers, System.getenv(\"KAFKA_BOOTSTRAP\"), \"kafka:9092\")");
            this._logger.LogWarning("⚠️ This means if Flink container has KAFKA_BOOTSTRAP set, it will override the kafka:9092 value from job definition!");

            var runRequest = new
            {
                entryClass = "com.flink.jobgateway.FlinkJobRunner",
                programArgsList = new[] { "--irBase64", irBase64 },
                parallelism = jobDefinition.Metadata.Parallelism ?? 1,
                jobName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId
            };

            var requestJson = JsonSerializer.Serialize(runRequest);
            this._logger.LogDebug("📤 Flink run request: {RequestJson}", requestJson);

            using var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            // Validate jarId from Flink response to prevent injection attacks
            var sanitizedJarId = ValidateAndSanitizePathSegment(jarId);

            this._logger.LogInformation("🚀 POST {Endpoint}/v1/jars/{JarId}/run", this._httpClient.BaseAddress, jarId);
            using var response = await this._httpClient.PostAsync($"/v1/jars/{sanitizedJarId}/run", content);
            this._logger.LogInformation("📥 Response: {StatusCode} {ReasonPhrase}", (int) response.StatusCode, response.ReasonPhrase);

            if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Accepted)
            {
                var err = await response.Content.ReadAsStringAsync();
                this._logger.LogError("❌ Flink job submission failed: {StatusCode} - {Error}", response.StatusCode, err);
                throw new InvalidOperationException($"Flink run failed: {response.StatusCode} - {err}");
            }

            var runContent = await response.Content.ReadAsStringAsync();
            this._logger.LogDebug("📥 Flink response body: {RunContent}", runContent);

            string? jobId = null;
            try
            {
                var run = JsonSerializer.Deserialize<FlinkRunResponse>(runContent,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                jobId = run?.JobId;
                if (jobId != null)
                {
                    this._logger.LogInformation("✅ Extracted Flink JobId from response: {JobId}", jobId);
                }
            }
            catch (JsonException ex)
            {
                this._logger.LogDebug(ex, "⚠️ Failed to deserialize Flink run response when extracting job id");
            }

            jobId ??= TryGetJobIdFromHeaders(response);

            if (string.IsNullOrEmpty(jobId))
            {
                this._logger.LogWarning("⚠️ JobId not in response, attempting recovery...");
                var targetName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
                jobId = await this.TryRecoverFlinkJobIdAsync(targetName, TimeSpan.FromSeconds(30));
            }

            if (string.IsNullOrEmpty(jobId))
            {
                var errorMsg = $"Flink JobManager did not return a job ID. This indicates the job may not have started correctly. " +
                    $"Response status: {response.StatusCode}, Response body: {runContent}";
                this._logger.LogError("❌ {ErrorMessage}", errorMsg);
                throw new InvalidOperationException(errorMsg);
            }

            this.LogSectionHeader("✅ [FlinkJobManager] Job submitted to Flink successfully",
                ("🆔 Flink JobId", jobId));

            return jobId;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit jar to Flink REST API: {Message}", ex.Message);
            throw new InvalidOperationException($"Failed to submit jar to Flink REST API for job {jobDefinition.Metadata.JobId}", ex);
        }
    }

    private async Task<string> SubmitSqlGatewayJobAsync(SqlSourceDefinition sqlSource, JobDefinition jobDefinition)
    {
        this.LogSectionHeader("📡 [FlinkJobManager] Submitting SQL job to SQL Gateway",
            ("📋 JobId", jobDefinition.Metadata.JobId));

        try
        {
            using var sqlGatewayClient = this.CreateSqlGatewayClient();
            this._logger.LogInformation("🌐 SQL Gateway client created: {BaseAddress}", sqlGatewayClient.BaseAddress);

            this._logger.LogInformation("⏳ Waiting for SQL Gateway to be ready...");
            await this.WaitForSqlGatewayReadyAsync(sqlGatewayClient);
            this._logger.LogInformation("✅ SQL Gateway is ready");

            var sessionHandle = await this.CreateSqlGatewaySessionAsync(sqlGatewayClient, jobDefinition);
            this._logger.LogInformation("✅ SQL Gateway session created: {SessionHandle}", sessionHandle);

            var lastJobId = await this.ExecuteSqlStatementsAsync(sqlGatewayClient, sessionHandle, sqlSource.Statements);

            // SQL Gateway jobs return session handle as tracking ID
            // This is expected behavior for SQL Gateway - it manages jobs within sessions
            var result = lastJobId ?? sessionHandle;

            if (string.IsNullOrEmpty(result))
            {
                this._logger.LogError("❌ SQL Gateway did not return a job ID or session handle");
                throw new InvalidOperationException("SQL Gateway did not return a job ID or session handle. This should not happen.");
            }

            this.LogSectionHeader("✅ [FlinkJobManager] SQL job submitted successfully",
                ("🆔 JobId/SessionHandle", result));

            return result;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "❌ Failed to submit SQL job via SQL Gateway: {Message}", ex.Message);
            throw new InvalidOperationException("SQL Gateway submission failed. See inner exception for details.", ex);
        }
    }

    private HttpClient CreateSqlGatewayClient()
    {
        var sqlGatewayEndpoint = this.DiscoverSqlGatewayEndpoint();
        this._logger.LogInformation("Using SQL Gateway endpoint: {Endpoint}", sqlGatewayEndpoint);

        return new HttpClient
        {
            BaseAddress = new Uri(sqlGatewayEndpoint),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    private async Task<string> CreateSqlGatewaySessionAsync(HttpClient client, JobDefinition jobDefinition)
    {
        var sessionName = jobDefinition.Metadata.JobName ?? jobDefinition.Metadata.JobId;
        this._logger.LogInformation("🚀 POST {BaseAddress}/v1/sessions (Creating session: {SessionName})", client.BaseAddress, sessionName);

        var sessionRequest = new
        {
            sessionName
        };
        var sessionJson = JsonSerializer.Serialize(sessionRequest);
        this._logger.LogDebug("📤 Request body: {SessionJson}", sessionJson);

        using var sessionContent = new StringContent(sessionJson, Encoding.UTF8, "application/json");
        using var sessionResponse = await client.PostAsync("/v1/sessions", sessionContent);

        this._logger.LogInformation("📥 Response: {StatusCode} {ReasonPhrase}", (int) sessionResponse.StatusCode, sessionResponse.ReasonPhrase);

        if (!sessionResponse.IsSuccessStatusCode)
        {
            var errorContent = await sessionResponse.Content.ReadAsStringAsync();
            this._logger.LogError("❌ SQL Gateway session creation failed: {StatusCode} - {Error}",
                sessionResponse.StatusCode, errorContent);
            throw new InvalidOperationException($"SQL Gateway session creation failed: {sessionResponse.StatusCode} - {errorContent}");
        }

        var sessionResponseContent = await sessionResponse.Content.ReadAsStringAsync();
        this._logger.LogDebug("📥 Response body: {Response}", sessionResponseContent);

        var handle = this.ExtractSessionHandle(sessionResponseContent);
        this._logger.LogInformation("✅ Session handle extracted: {Handle}", handle);

        return handle;
    }

    private string ExtractSessionHandle(string sessionResponseContent)
    {
        try
        {
            var sessionJson = JsonDocument.Parse(sessionResponseContent);
            if (sessionJson.RootElement.TryGetProperty("sessionHandle", out var handleProp))
            {
                var sessionHandle = handleProp.GetString() ?? throw new InvalidOperationException("Session handle is null");
                this._logger.LogInformation("SQL Gateway session handle: {SessionHandle}", sessionHandle);
                return sessionHandle;
            }
            throw new InvalidOperationException("Session response doesn't contain sessionHandle");
        }
        catch (JsonException ex)
        {
            this._logger.LogError(ex, "Failed to parse session response");
            throw new InvalidOperationException("Failed to parse SQL Gateway session response", ex);
        }
    }

    private async Task<string?> ExecuteSqlStatementsAsync(HttpClient client, string sessionHandle, List<string> statements)
    {
        string? lastJobId = null;

        foreach (var statement in statements)
        {
            if (string.IsNullOrWhiteSpace(statement))
            {
                continue;
            }

            lastJobId = await this.ExecuteSingleStatementAsync(client, sessionHandle, statement) ?? lastJobId;
        }

        if (string.IsNullOrEmpty(lastJobId))
        {
            this._logger.LogInformation("Using session handle as job ID: {JobId}", sessionHandle);
        }

        return lastJobId;
    }

    private async Task<string?> ExecuteSingleStatementAsync(HttpClient client, string sessionHandle, string statement)
    {
        this._logger.LogInformation("Executing SQL statement via Gateway: {Statement}",
            statement.Length > 100 ? statement.Substring(0, 100) + "..." : statement);

        var requestBody = new
        {
            statement = statement.Trim()
        };
        var jsonContent = JsonSerializer.Serialize(requestBody);
        using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Validate sessionHandle from SQL Gateway response to prevent injection attacks
        var sanitizedSessionHandle = ValidateAndSanitizePathSegment(sessionHandle);
        var statementEndpoint = $"/v1/sessions/{sanitizedSessionHandle}/statements";
        using var response = await client.PostAsync(statementEndpoint, content);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            this._logger.LogError("SQL Gateway statement execution failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new InvalidOperationException($"SQL Gateway execution failed: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        this._logger.LogDebug("SQL Gateway response: {Response}", responseContent);

        return this.ExtractJobIdFromStatementResponse(responseContent);
    }

    private string? ExtractJobIdFromStatementResponse(string responseContent)
    {
        try
        {
            var responseJson = JsonDocument.Parse(responseContent);
            if (responseJson.RootElement.TryGetProperty("operationHandle", out var opHandleProp))
            {
                var jobId = opHandleProp.GetString();
                this._logger.LogInformation("SQL Gateway returned operation handle: {OperationHandle}", jobId);
                return jobId;
            }
            if (responseJson.RootElement.TryGetProperty("statementId", out var stmtIdProp))
            {
                var jobId = stmtIdProp.GetString();
                this._logger.LogInformation("SQL Gateway returned statement ID: {StatementId}", jobId);
                return jobId;
            }
        }
        catch (JsonException ex)
        {
            this._logger.LogWarning(ex, "Could not parse SQL Gateway response for job ID");
        }
        return null;
    }


    private async Task<string> EnsureRunnerJarAsync()
    {
        var jarPath = await this.EnsureRunnerJarPathAsync();
        if (!File.Exists(jarPath))
        {
            throw new FileNotFoundException($"Runner jar not found at {jarPath}");
        }

        // Collect connector JARs and create a shaded JAR if needed
        var connectorJars = this.CollectConnectorJars();
        if (connectorJars.Any())
        {
            this._logger.LogInformation("Found {Count} connector JARs, creating shaded JAR", connectorJars.Count);
            jarPath = await this.CreateShadedJarAsync(jarPath, connectorJars);
        }

        using var form = new MultipartFormDataContent();
        await using var fs = File.OpenRead(jarPath);
        var fileName = Path.GetFileName(jarPath);
        form.Add(new StreamContent(fs), "jarfile", fileName);

        var uploadResp = await this._httpClient.PostAsync("/v1/jars/upload", form);
        if (!uploadResp.IsSuccessStatusCode)
        {
            var err = await uploadResp.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Jar upload failed: {uploadResp.StatusCode} - {err}");
        }

        var uploadPayload = await uploadResp.Content.ReadAsStringAsync();
        this._logger.LogInformation("Jar upload response payload: {Payload}", uploadPayload);

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
                        this._logger.LogInformation("Flink accepted jar upload {JarFile} as {JarId}", fileName, jarId);
                        return jarId;
                    }
                }
            }
            catch (Exception ex)
            {
                this._logger.LogDebug(ex, "Failed to parse jar upload response: {Payload}", uploadPayload);
            }
        }

        return await this.WaitForJarRegistrationAsync(fileName);
    }

    private async Task<string> WaitForJarRegistrationAsync(string fileName, TimeSpan? timeout = null)
    {
        var waitFor = timeout ?? TimeSpan.FromSeconds(30);
        var deadline = DateTime.UtcNow + waitFor;
        List<string> lastKnownJars = new();

        while (DateTime.UtcNow < deadline)
        {
            var jarId = await this.TryFindRegisteredJarAsync(fileName, lastKnownJars);
            if (jarId != null)
            {
                return jarId;
            }

            await Task.Delay(JarRegistrationPollingDelay);
        }

        return this.ThrowJarNotFoundError(fileName, waitFor, lastKnownJars);
    }

    private async Task<string?> TryFindRegisteredJarAsync(string fileName, List<string> lastKnownJars)
    {
        try
        {
            var listResp = await this._httpClient.GetAsync("/v1/jars");
            if (!listResp.IsSuccessStatusCode)
            {
                return null;
            }

            var listJson = await listResp.Content.ReadAsStringAsync();
            var jars = JsonSerializer.Deserialize<FlinkJarsList>(listJson);

            UpdateLastKnownJars(jars, lastKnownJars);
            return FindMatchingJar(jars, fileName);
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(ex, "Polling for uploaded jar {JarFile} failed; will retry", fileName);
            return null;
        }
    }

    private static void UpdateLastKnownJars(FlinkJarsList? jars, List<string> lastKnownJars)
    {
        lastKnownJars.Clear();
        if (jars?.Files == null)
        {
            return;
        }

        lastKnownJars.AddRange(jars.Files.Select(f =>
            string.IsNullOrEmpty(f.Id) ? f.Name : $"{f.Name} ({f.Id})"));
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
        this._logger.LogError("Uploaded jar {JarFile} not found; last known jars: {JarList}", fileName, jarList);
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

        this._logger.LogInformation("Searching for connector JARs in {Count} paths", searchPaths.Count);

        foreach (var searchPath in searchPaths.Distinct())
        {
            if (Directory.Exists(searchPath))
            {
                var jars = Directory.GetFiles(searchPath, "*.jar", SearchOption.TopDirectoryOnly);
                if (jars.Length > 0)
                {
                    connectorJars.AddRange(jars);
                    this._logger.LogInformation("Found {Count} connector JARs in {Path}", jars.Length, searchPath);
                }
            }
            else
            {
                this._logger.LogDebug("Connector path does not exist: {Path}", searchPath);
            }
        }

        if (connectorJars.Count == 0)
        {
            this._logger.LogWarning("No connector JARs found. SQL jobs may fail if they require Kafka/JSON connectors.");
            this._logger.LogWarning("Current directory: {Current}, AppDomain base: {AppBase}, Repo root: {RepoRoot}",
                Environment.CurrentDirectory, AppDomain.CurrentDomain.BaseDirectory, repoRoot ?? "not found");
        }

        return connectorJars.Distinct().ToList();
    }

    private async Task<string> CreateShadedJarAsync(string runnerJarPath, List<string> connectorJars)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"flink-shaded-{Guid.NewGuid():N}");
        _ = Directory.CreateDirectory(tempDir);

        try
        {
            var shadedJarPath = Path.Combine(tempDir, "flink-ir-runner-shaded.jar");
            await this.CombineJarsAsync(runnerJarPath, connectorJars, shadedJarPath);
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
                this._logger.LogDebug(cleanupEx, "Temp directory cleanup failed: {TempDir}", tempDir);
            }
            throw;
        }
    }

    private Task CombineJarsAsync(string runnerJarPath, List<string> connectorJars, string outputPath)
    {
        this._logger.LogInformation("Combining runner JAR with {Count} connector JARs into shaded JAR", connectorJars.Count);
        File.Copy(runnerJarPath, outputPath, true);

        var serviceFiles = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        using (var outputZip = ZipFile.Open(outputPath, ZipArchiveMode.Update))
        {
            var existingEntries = new HashSet<string>(
                outputZip.Entries.Select(e => e.FullName),
                StringComparer.OrdinalIgnoreCase);

            CollectServiceFilesFromRunnerJar(outputZip, serviceFiles);
            this.MergeConnectorJars(outputZip, connectorJars, existingEntries, serviceFiles);
            this.WriteMergedServiceFiles(outputZip, serviceFiles);
        }

        this._logger.LogInformation("Created shaded JAR at {Path}", outputPath);
        return Task.CompletedTask;
    }

    private static void CollectServiceFilesFromRunnerJar(ZipArchive outputZip, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach (var entry in outputZip.Entries
            .Where(e => e.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase) && !e.FullName.EndsWith('/')))
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
                    _ = lines.Add(line);
                }
            }
            serviceFiles[entry.FullName] = lines;
        }
    }

    private void MergeConnectorJars(ZipArchive outputZip, List<string> connectorJars,
        HashSet<string> existingEntries, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach (var connectorJar in connectorJars)
        {
            if (!File.Exists(connectorJar))
            {
                this._logger.LogWarning("Connector JAR not found, skipping: {Path}", connectorJar);
                continue;
            }

            this._logger.LogDebug("Merging connector JAR: {Path}", connectorJar);
            var entriesAdded = MergeConnectorJar(outputZip, connectorJar, existingEntries, serviceFiles);
            this._logger.LogInformation("Added {Count} entries from connector JAR: {Name}",
                entriesAdded, Path.GetFileName(connectorJar));
        }
    }

    private static int MergeConnectorJar(ZipArchive outputZip, string connectorJar,
        HashSet<string> existingEntries, Dictionary<string, HashSet<string>> serviceFiles)
    {
        var entriesAdded = 0;
        using var connectorZip = ZipFile.OpenRead(connectorJar);

        foreach (var entry in connectorZip.Entries)
        {
            if (entry.FullName.EndsWith('/'))
            {
                continue;
            }

            if (entry.FullName.StartsWith("META-INF/services/", StringComparison.OrdinalIgnoreCase))
            {
                MergeServiceFile(entry, serviceFiles);
                continue;
            }

            if (existingEntries.Contains(entry.FullName))
            {
                continue;
            }

            CopyZipEntry(entry, outputZip);
            _ = existingEntries.Add(entry.FullName);
            entriesAdded++;
        }

        return entriesAdded;
    }

    private static void MergeServiceFile(ZipArchiveEntry entry, Dictionary<string, HashSet<string>> serviceFiles)
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
                _ = serviceFiles[entry.FullName].Add(line);
            }
        }
    }

    private static void CopyZipEntry(ZipArchiveEntry sourceEntry, ZipArchive destinationZip)
    {
        var newEntry = destinationZip.CreateEntry(sourceEntry.FullName, CompressionLevel.Optimal);
        using var sourceStream = sourceEntry.Open();
        using var destStream = newEntry.Open();
        sourceStream.CopyTo(destStream);
    }

    private void WriteMergedServiceFiles(ZipArchive outputZip, Dictionary<string, HashSet<string>> serviceFiles)
    {
        foreach (var (servicePath, serviceLines) in serviceFiles)
        {
            var oldEntry = outputZip.Entries.FirstOrDefault(e =>
                e.FullName.Equals(servicePath, StringComparison.OrdinalIgnoreCase));
            oldEntry?.Delete();

            var newEntry = outputZip.CreateEntry(servicePath, CompressionLevel.Optimal);
            using var writer = new StreamWriter(newEntry.Open());
            foreach (var line in serviceLines.OrderBy(l => l))
            {
                writer.WriteLine(line);
            }

            this._logger.LogDebug("Merged service file {Path} with {Count} providers",
                servicePath, serviceLines.Count);
        }
    }

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var pom = Path.Combine(dir.FullName, FlinkIRRunnerDirectory, "pom.xml");
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
        {
            return null;
        }

        var sw = Stopwatch.StartNew();
        var overviewEndpoints = new[] { "/v1/jobs/overview", "/jobs/overview" };

        while (sw.Elapsed < timeout)
        {
            var jobId = await this.TryRecoverFromEndpointsAsync(jobName, overviewEndpoints);
            if (jobId != null)
            {
                return jobId;
            }

            await Task.Delay(JobRecoveryPollingDelay);
        }

        return null;
    }

    private async Task<string?> TryRecoverFromEndpointsAsync(string jobName, string[] endpoints)
    {
        foreach (var endpoint in endpoints)
        {
            var jobId = await this.TryRecoverFromSingleEndpointAsync(jobName, endpoint);
            if (jobId != null)
            {
                return jobId;
            }
        }
        return null;
    }

    private async Task<string?> TryRecoverFromSingleEndpointAsync(string jobName, string endpoint)
    {
        try
        {
            using var response = await this._httpClient.GetAsync(endpoint);
            if (!response.IsSuccessStatusCode)
            {
                this._logger.LogDebug("Jobs overview endpoint {Endpoint} returned {StatusCode}", endpoint, response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadAsStringAsync();
            this._logger.LogInformation("Jobs overview response from {Endpoint} while recovering job id: {Payload}", endpoint, payload);

            var recovered = ExtractJobIdFromOverviewPayload(payload, jobName);
            if (!string.IsNullOrEmpty(recovered))
            {
                this._logger.LogInformation("Recovered job id {FlinkJobId} for job {JobName} via {Endpoint}", recovered, jobName, endpoint);
                return recovered;
            }
        }
        catch (Exception ex)
        {
            this._logger.LogDebug(ex, "Failed to recover job id for {JobName} via {Endpoint}; will retry", jobName, endpoint);
        }
        return null;
    }

    private static string? ExtractJobIdFromOverviewPayload(string payload, string jobName)
    {
        if (string.IsNullOrWhiteSpace(payload))
        {
            return null;
        }

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
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            var match = MatchJobEntry(element, jobName);
            if (!string.IsNullOrEmpty(match))
            {
                return match;
            }

            foreach (var property in element.EnumerateObject())
            {
                var nested = ExtractJobIdFromOverviewElement(property.Value, jobName);
                if (!string.IsNullOrEmpty(nested))
                {
                    return nested;
                }
            }
        }

        return null;
    }

    private static string? MatchJobEntry(JsonElement element, string jobName)
    {
        if (!TryGetStringProperty(element, "name", out var name) && !TryGetStringProperty(element, "jobName", out name))
        {
            return null;
        }

        if (!string.Equals(name, jobName, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (!TryGetStringProperty(element, "jid", out var jobId)
            && !TryGetStringProperty(element, "jobId", out jobId)
            && !TryGetStringProperty(element, "jobid", out jobId)
            && !TryGetStringProperty(element, "id", out jobId))
        {
            return null;
        }

        return jobId;
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
            if (!string.IsNullOrEmpty(jobId))
            {
                return jobId;
            }
        }

        if (response.Headers.TryGetValues("Location", out var locations))
        {
            foreach (var value in locations)
            {
                var jobId = ExtractJobIdFromPath(value);
                if (!string.IsNullOrEmpty(jobId))
                {
                    return jobId;
                }
            }
        }

        foreach (var headerName in new[] { "X-Flink-JobID", "X-Flink-Job-Id", "Flink-Job-Id", "Flink-JobId" })
        {
            if (response.Headers.TryGetValues(headerName, out var headerValues))
            {
                var value = headerValues.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
                if (!string.IsNullOrEmpty(value))
                {
                    return value.Trim();
                }
            }
        }

        return null;

        static string? ExtractJobIdFromPath(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Split('?', 2)[0].Trim().Trim('/');
            if (string.IsNullOrEmpty(trimmed))
            {
                return null;
            }

            var segments = trimmed.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var last = segments.LastOrDefault();
            return string.IsNullOrEmpty(last) || last.Equals("jobs", StringComparison.OrdinalIgnoreCase)
                ? null
                : last;
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
        if (jobDefinition.Sink != null || isSqlJob)
        {
            return;
        }

        errors.Add("Job sink is required");
    }

    private static void ValidateSource(object? source, List<string> errors)
    {
        if (source == null)
        {
            return;
        }

        switch (source)
        {
            case KafkaSourceDefinition kafkaSource when string.IsNullOrEmpty(kafkaSource.Topic):
                errors.Add("Kafka source must specify a topic");
                break;
            case FileSourceDefinition fileSource when string.IsNullOrEmpty(fileSource.Path):
                errors.Add("File source must specify a path");
                break;
            default:
                // Source is valid or not one of the validated types
                break;
        }
    }

    private static void ValidateSink(object? sink, List<string> errors)
    {
        if (sink == null)
        {
            return;
        }

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
            default:
                // Sink is valid or not one of the validated types
                break;
        }
    }

    /// <summary>
    /// Validates and sanitizes a path segment to prevent path traversal and URL injection attacks.
    /// Only allows alphanumeric characters, hyphens, and underscores.
    /// Rejects path traversal sequences, special characters, and URL-encoded attacks.
    /// </summary>
    /// <param name="segment">The path segment to validate.</param>
    /// <param name="parameterName">
    /// The name of the parameter being validated. This is automatically populated via CallerArgumentExpression 
    /// and should not be provided by callers. It will capture the argument expression from the call site 
    /// (e.g., "flinkJobId" when called as ValidateAndSanitizePathSegment(flinkJobId)).
    /// </param>
    /// <returns>URL-encoded safe path segment.</returns>
    /// <exception cref="ArgumentException">Thrown when segment contains invalid characters or is null/empty.</exception>
    private static string ValidateAndSanitizePathSegment(string segment, [CallerArgumentExpression(nameof(segment))] string? parameterName = null)
    {
        if (string.IsNullOrWhiteSpace(segment))
        {
            throw new ArgumentException($"Path parameter '{parameterName}' cannot be null or empty.", parameterName);
        }

        // Check for path traversal sequences
        if (segment.Contains("..") || segment.Contains("./") || segment.Contains(".\\"))
        {
            throw new ArgumentException($"Path parameter '{parameterName}' contains invalid path traversal sequence: {segment}", parameterName);
        }

        // Check for invalid characters - only allow alphanumeric, hyphens, underscores, and dots
        // This prevents: /, \, ?, #, @, :, and other special characters
        // Dots are allowed for file extensions (e.g., .jar)
        var invalidChar = segment.FirstOrDefault(c => !char.IsLetterOrDigit(c) && c != '-' && c != '_' && c != '.');
        if (invalidChar != '\0')
        {
            throw new ArgumentException($"Path parameter '{parameterName}' contains invalid character '{invalidChar}'. Only alphanumeric, hyphens, underscores, and dots are allowed.", parameterName);
        }

        // Return URL-encoded segment for additional safety
        return Uri.EscapeDataString(segment);
    }

    private sealed class JobInfo
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmissionTime
        {
            get; set;
        }
        public JobDefinition JobDefinition { get; set; } = null!;
    }

    private sealed class JobValidationResult
    {
        public bool IsValid
        {
            get; set;
        }
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
        public string? Filename
        {
            get; set;
        }

        [JsonPropertyName("status")]
        public string? Status
        {
            get; set;
        }
    }

    private sealed class FlinkJarFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uploaded")]
        public long Uploaded
        {
            get; set;
        }
    }

    private sealed class FlinkMetricEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
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

    private static string? ExtractBackpressureLevel(JsonElement root)
    {
        if (root.TryGetProperty("backpressureLevel", out var lvlEl))
        {
            return lvlEl.GetString();
        }

        if (root.TryGetProperty("backpressure-level", out var lvlEl2))
        {
            return lvlEl2.GetString();
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

        public JobMetricsBuilder(string flinkJobId) => this._flinkJobId = flinkJobId;

        public void AddRecordsIn(long value) => this._recordsIn += value;
        public void AddRecordsOut(long value) => this._recordsOut += value;
        public void UpdateMaxParallelism(int value) => this._parallelism = Math.Max(this._parallelism, value);
        public void SetCheckpoints(int value) => this._checkpoints = value;
        public void SetLastCheckpoint(DateTime value) => this._lastCheckpoint = value;
        public void UpdateWorstBackpressure(string level) => this._backpressureLevel = WorstBackpressure(this._backpressureLevel, level);

        private static string WorstBackpressure(string current, string candidate)
        {
            static int Rank(string s) => s?.ToUpperInvariant() switch
            {
                "HIGH" => 3,
                "LOW" => 2,
                "OK" => 1,
                "NONE" => 0,
                _ => 0
            };

            return Rank(candidate) >= Rank(current) ? candidate : current;
        }

        public JobMetrics Build() => new()
        {
            FlinkJobId = this._flinkJobId,
            RecordsIn = this._recordsIn,
            RecordsOut = this._recordsOut,
            Parallelism = this._parallelism,
            Checkpoints = this._checkpoints,
            LastCheckpoint = this._lastCheckpoint,
            CustomMetrics = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["backpressureLevel"] = this._backpressureLevel
            }
        };
    }
}
