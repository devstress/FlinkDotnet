using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LearningCourse.Common;
using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Simple test base for LearningCourse integration tests.
/// Starts LocalTesting AppHost as a standalone process and runs actual exercise code against it.
/// Much simpler than creating duplicate AppHost infrastructure.
///
/// Exercises now self-manage their job lifecycle using IJobClient pattern.
/// No test infrastructure job cleanup needed.
/// </summary>
public abstract class LearningCourseTestBase
{
    private static Process? _appHostProcess;
    private static bool _isSetupComplete = false;
    private static readonly TimeSpan AppHostStartupTimeout = TimeSpan.FromSeconds(90);
    private static readonly string AppHostPath = Path.Combine(
        FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root"),
        "LocalTesting", "LocalTesting.FlinkSqlAppHost");
    private static StreamWriter? _debugLogWriter;
    
    /// <summary>
    /// Kafka container IP for Flink jobs (e.g., "172.17.0.2:9093").
    /// Docker bridge network doesn't support DNS, so we use actual IP address.
    /// </summary>
    public static string? KafkaFlinkBootstrapServers { get; private set; }
    
    /// <summary>
    /// Kafka host endpoint for exercise producers/consumers (e.g., "localhost:43175").
    /// Dynamically allocated host port mapped to Kafka's container port.
    /// </summary>
    public static string? KafkaHostBootstrapServers { get; private set; }
    
    /// <summary>
    /// Temporal gRPC endpoint for workflow execution (e.g., "localhost:43210").
    /// Dynamically allocated host port mapped to Temporal's container port 7233.
    /// </summary>
    public static string? TemporalHostEndpoint { get; private set; }
    
    /// <summary>
    /// Redis endpoint for state management (e.g., "localhost:43211").
    /// Dynamically allocated host port mapped to Redis's container port 6379.
    /// Only available when LEARNINGCOURSE=true.
    /// </summary>
    public static string? RedisHostEndpoint { get; private set; }
    
    /// <summary>
    /// Prometheus metrics endpoint (e.g., "http://localhost:43212").
    /// Dynamically allocated host port mapped to Prometheus's container port 9090.
    /// Only available when LEARNINGCOURSE=true.
    /// </summary>
    public static string? PrometheusHostEndpoint { get; private set; }
    
    /// <summary>
    /// Grafana dashboard endpoint (e.g., "http://localhost:43213").
    /// Dynamically allocated host port mapped to Grafana's container port 3000.
    /// Only available when LEARNINGCOURSE=true.
    /// </summary>
    public static string? GrafanaHostEndpoint { get; private set; }

    /// <summary>
    /// Start LocalTesting AppHost once for all tests.
    /// Called by each test assembly's SetUpFixture.
    /// Idempotent - safe to call multiple times (will only setup once).
    /// </summary>
    public static async Task GlobalSetUp()
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] GlobalSetUp called");
        
        // Kill any orphaned processes from previous test runs FIRST
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Cleaning up orphaned processes...");
        KillOrphanedJobGatewayProcesses();
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Orphaned process cleanup complete");
        
        // Dispose any existing debug log writer first to release file locks
        if (_debugLogWriter != null)
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Disposing existing debug log writer...");
            _debugLogWriter.Dispose();
            _debugLogWriter = null;
        }
        
        // Initialize debug log file with FileShare.ReadWrite to allow multiple test processes to write
        var repoRoot = FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root");
        var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
        
        // Clean up old test logs before starting new test run
        if (Directory.Exists(testLogsDir))
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Cleaning up old test logs...");
            try
            {
                Directory.Delete(testLogsDir, recursive: true);
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Old logs deleted successfully");
            }
            catch (IOException ex)
            {
                // If deletion fails due to file locks, just log and continue
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Warning: Could not delete old logs (files may be in use): {ex.Message}");
            }
        }
        
        Directory.CreateDirectory(testLogsDir);
        
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Creating debug log file...");
        var debugLogPath = Path.Combine(testLogsDir, $"TestInfrastructure.Debug.log.{DateTime.UtcNow:yyyyMMdd}");
        var fileStream = new FileStream(debugLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _debugLogWriter = new StreamWriter(fileStream) { AutoFlush = true };
        
        LogDebug("[SETUP] Starting infrastructure setup");
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Debug log initialized");
        
        // Set LEARNINGCOURSE=true to enable Redis and Observability infrastructure
        Environment.SetEnvironmentVariable("LEARNINGCOURSE", "true");
        TestContext.WriteLine("📚 Set LEARNINGCOURSE=true for Redis and Observability infrastructure");
        
        TestContext.WriteLine("🚀 Starting LocalTesting AppHost...");
        TestContext.WriteLine($"📁 AppHost path: {AppHostPath}");
    
        // DO NOT set KAFKA_BOOTSTRAP_SERVERS here!
        TestContext.WriteLine($"✅ NOT setting KAFKA_BOOTSTRAP_SERVERS globally to prevent Docker inheritance");

        _appHostProcess = StartAppHostProcess();
        var appHostStartTime = DateTime.UtcNow;
        
        TestContext.WriteLine("✅ AppHost process started, polling for infrastructure readiness...");
        
        try
        {
            await WaitForInfrastructureReadyAsync(appHostStartTime);
            
            _isSetupComplete = true;
            
            TestContext.WriteLine("✅ All infrastructure ready, tests can proceed");
        }
        catch (TimeoutException ex)
        {
            TestContext.WriteLine($"❌ Infrastructure setup failed: {ex.Message}");
            TestContext.WriteLine("🛑 Running teardown to clean up containers due to setup failure...");
            
            try
            {
                GlobalTearDown();
            }
            catch (Exception teardownEx)
            {
                TestContext.WriteLine($"⚠️ Error during teardown after setup failure: {teardownEx.Message}");
            }
            
            throw;
        }
    }
    
    /// <summary>
    /// Start AppHost process with output capture to both console and log file
    /// </summary>
    private static Process StartAppHostProcess()
    {
        // Create Aspire log file
        var repoRoot = FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root");
        var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
        Directory.CreateDirectory(testLogsDir);
        var aspireLogPath = Path.Combine(testLogsDir, $"Aspire.AppHost.log.{DateTime.UtcNow:yyyyMMdd}");
        
        // Create log writer with FileShare.ReadWrite for concurrent access
        var aspireLogStream = new FileStream(aspireLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        var aspireLogWriter = new StreamWriter(aspireLogStream) { AutoFlush = true };
        
        aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] === Aspire AppHost Starting ===");
        aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] Log file: {aspireLogPath}");
        aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] LEARNINGCOURSE=true (enabling Redis and Observability)");
        
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = "run --no-restore --no-build --configuration Release",
            WorkingDirectory = AppHostPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        // CRITICAL: Pass LEARNINGCOURSE environment variable to AppHost process
        // AppHost checks this to enable Redis and Observability infrastructure
        psi.Environment["LEARNINGCOURSE"] = "true";

        var process = Process.Start(psi);
        
        if (process == null)
        {
            aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [ERROR] Failed to start AppHost process");
            aspireLogWriter.Dispose();
            throw new InvalidOperationException("Failed to start AppHost process");
        }

        aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] AppHost process started with PID {process.Id}");

        // Capture output for diagnostics - write to both console and log file
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost] {e.Data}");
                aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [OUT] {e.Data}");
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost Error] {e.Data}");
                aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [ERR] {e.Data}");
            }
        };
        
        // Clean up log writer when process exits
        process.Exited += (sender, e) =>
        {
            aspireLogWriter.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] === Aspire AppHost Exited ===");
            aspireLogWriter.Dispose();
        };
        process.EnableRaisingEvents = true;
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        TestContext.WriteLine($"📝 Aspire AppHost log: {aspireLogPath}");
        
        return process;
    }
    
    /// <summary>
    /// Smart polling: Check if containers are actually ready instead of blind wait
    /// Ensures BOTH Kafka and Flink are ready before proceeding
    /// OPTIMIZED: Faster polling (200ms) and parallel health checks for minimal wait time
    /// </summary>
    /// <param name="appHostStartTime">The time when AppHost process was started</param>
    private static async Task WaitForInfrastructureReadyAsync(DateTime appHostStartTime)
    {
        LogDebug("[SETUP] Starting infrastructure readiness polling loop...");
        TestContext.WriteLine("[SETUP] Starting OPTIMIZED infrastructure readiness polling (200ms intervals)...");
        
        var stopwatch = Stopwatch.StartNew();
        var maxWait = AppHostStartupTimeout;
        var pollInterval = TimeSpan.FromMilliseconds(200);  // OPTIMIZED: 200ms instead of 500ms for faster detection
        bool dockerPsLogged = false;  // Track if we've logged docker ps 30 seconds after AppHost start
        
        string? kafkaFlinkIp = null;
        string? kafkaHostEndpoint = null;
        string? temporalEndpoint = null;
        string? redisEndpoint = null;
        string? prometheusEndpoint = null;
        string? grafanaEndpoint = null;
        bool flinkReady = false;
        bool temporalReady = false;
        
        // Check if LearningCourse mode is enabled for optional infrastructure
        var isLearningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
        
        int iteration = 0;
        while (stopwatch.Elapsed < maxWait)
        {
            iteration++;
            LogDebug($"[POLL] Iteration {iteration} at {stopwatch.Elapsed.TotalSeconds:F1}s: FlinkReady={flinkReady}, TemporalReady={temporalReady}");
            
            // Log docker ps 30 seconds after AppHost started (once only)
            var timeSinceAppHostStart = (DateTime.UtcNow - appHostStartTime).TotalSeconds;
            if (!dockerPsLogged && timeSinceAppHostStart >= 30)
            {
                dockerPsLogged = true;
                TestContext.WriteLine($"🐳 === DOCKER PS (30 seconds after AppHost start) ===");
                LogDebug($"[DOCKER-PS] Logging docker ps at {timeSinceAppHostStart:F1}s after AppHost start");
                await DockerInfrastructure.LogDockerPsAsync("30 seconds after AppHost start", _debugLogWriter);
            }
            
            // OPTIMIZED: Run endpoint discovery and health checks in parallel for faster detection
            var discoveryTask = TryDiscoverEndpointsAsync(
                kafkaFlinkIp, kafkaHostEndpoint, temporalEndpoint,
                redisEndpoint, prometheusEndpoint, grafanaEndpoint,
                isLearningCourse, stopwatch, iteration);
            
            var flinkHealthTask = (kafkaFlinkIp != null && kafkaHostEndpoint != null && !flinkReady)
                ? IsFlinkHealthyAsync()
                : Task.FromResult(flinkReady);
            
            var temporalHealthTask = (temporalEndpoint != null && !temporalReady)
                ? IsTemporalHealthyAsync(temporalEndpoint)
                : Task.FromResult(temporalReady);
            
            // Wait for all checks in parallel
            await Task.WhenAll(discoveryTask, flinkHealthTask, temporalHealthTask);
            
            // Update discovered endpoints
            var discovered = await discoveryTask;
            kafkaFlinkIp = discovered.flinkIp ?? kafkaFlinkIp;
            kafkaHostEndpoint = discovered.hostEndpoint ?? kafkaHostEndpoint;
            temporalEndpoint = discovered.temporal ?? temporalEndpoint;
            redisEndpoint = discovered.redis ?? redisEndpoint;
            prometheusEndpoint = discovered.prometheus ?? prometheusEndpoint;
            grafanaEndpoint = discovered.grafana ?? grafanaEndpoint;
            
            // Update health check results
            var newFlinkReady = await flinkHealthTask;
            if (newFlinkReady && !flinkReady)
            {
                flinkReady = true;
                TestContext.WriteLine($"✅ Flink cluster is healthy (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
            }
            
            var newTemporalReady = await temporalHealthTask;
            if (newTemporalReady && !temporalReady)
            {
                temporalReady = true;
                LogDebug($"[POLL] Temporal READY after {stopwatch.Elapsed.TotalSeconds:F1}s");
                TestContext.WriteLine($"✅ Temporal server is healthy and namespace ready (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
            }
            else if (temporalEndpoint != null && !temporalReady && iteration % 5 == 1)
            {
                // Log every 5th iteration to reduce noise
                LogDebug($"[POLL] Temporal NOT ready yet after {stopwatch.Elapsed.TotalSeconds:F1}s");
                TestContext.WriteLine($"⏳ Temporal not ready yet (after {stopwatch.Elapsed.TotalSeconds:F1}s), will retry...");
            }
            
            // Core infrastructure must be ready: Kafka, Flink, and Temporal
            // LearningCourse infrastructure (Redis, Prometheus, Grafana) is optional
            var coreReady = kafkaFlinkIp != null && kafkaHostEndpoint != null && temporalEndpoint != null && flinkReady && temporalReady;
            var learningCourseReady = !isLearningCourse || (redisEndpoint != null && prometheusEndpoint != null && grafanaEndpoint != null);
            
            if (coreReady && learningCourseReady)
            {
                KafkaFlinkBootstrapServers = kafkaFlinkIp;
                KafkaHostBootstrapServers = kafkaHostEndpoint;
                TemporalHostEndpoint = temporalEndpoint;
                RedisHostEndpoint = redisEndpoint;
                PrometheusHostEndpoint = prometheusEndpoint;
                GrafanaHostEndpoint = grafanaEndpoint;
                
                var savedTime = (maxWait - stopwatch.Elapsed).TotalSeconds;
                TestContext.WriteLine($"✅ All infrastructure ready after {stopwatch.Elapsed.TotalSeconds:F1}s (saved {savedTime:F1}s with optimized polling)");
                if (isLearningCourse)
                {
                    TestContext.WriteLine($"   📚 LearningCourse infrastructure: Redis={redisEndpoint}, Prometheus={prometheusEndpoint}, Grafana={grafanaEndpoint}");
                }
                
                // Log optimization metrics
                LogDebug($"[OPTIMIZATION] Poll interval: 200ms, Total iterations: {iteration}, Time saved: {savedTime:F1}s");
                return;
            }
            
            await Task.Delay(pollInterval);
        }
        
        throw new TimeoutException(
            $"Infrastructure not ready within {maxWait.TotalSeconds}s. " +
            $"KafkaFlinkIp: {KafkaFlinkBootstrapServers ?? "null"}, " +
            $"KafkaHostEndpoint: {KafkaHostBootstrapServers ?? "null"}, " +
            $"TemporalEndpoint: {TemporalHostEndpoint ?? "null"}, " +
            $"FlinkReady: {flinkReady}, " +
            $"TemporalReady: {temporalReady}" +
            (isLearningCourse ? $", Redis: {RedisHostEndpoint ?? "null"}, Prometheus: {PrometheusHostEndpoint ?? "null"}, Grafana: {GrafanaHostEndpoint ?? "null"}" : ""));
    }
    
    /// <summary>
    /// Check if Flink JobManager is healthy and ready to accept jobs
    /// OPTIMIZED: Reduced timeout to 1 second for faster failure detection
    /// </summary>
    private static async Task<bool> IsFlinkHealthyAsync()
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(1) };  // OPTIMIZED: 1s timeout
            var response = await httpClient.GetAsync("http://localhost:8080/api/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Check if Temporal server is healthy and ready to accept workflow connections.
    /// Verifies both TCP connectivity AND that the "default" namespace is created and ready.
    /// This ensures Temporal has completed initialization including namespace registration.
    /// </summary>
    private static async Task<bool> IsTemporalHealthyAsync(string temporalEndpoint)
    {
        LogDebug($"[TEMPORAL-HEALTH] Checking Temporal health at {temporalEndpoint}");
        TestContext.WriteLine($"🔍 [Temporal Health] Starting check for endpoint: {temporalEndpoint}");
        
        try
        {
            // Step 1: TCP connectivity check (fast pre-check)
            LogDebug($"[TEMPORAL-HEALTH] Step 1: TCP connectivity test");
            TestContext.WriteLine($"🔍 [Temporal Health] Step 1: TCP connectivity test");
            var parts = temporalEndpoint.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[1], out var port))
            {
                LogDebug($"[TEMPORAL-HEALTH] Invalid endpoint format: {temporalEndpoint}");
                TestContext.WriteLine($"❌ [Temporal Health] Invalid endpoint format: {temporalEndpoint}");
                return false;
            }
            
            var host = parts[0];
            
            // Verify TCP connection to Temporal gRPC port
            // OPTIMIZED: Reduced timeout to 1 second for faster detection
            using var tcpClient = new System.Net.Sockets.TcpClient();
            var connectTask = tcpClient.ConnectAsync(host, port);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(1));  // OPTIMIZED: 1s timeout
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask != connectTask || !tcpClient.Connected)
            {
                LogDebug($"[TEMPORAL-HEALTH] TCP connection failed to {host}:{port}");
                TestContext.WriteLine($"❌ [Temporal Health] TCP connection failed to {host}:{port}");
                return false; // TCP connection failed
            }
            
            LogDebug($"[TEMPORAL-HEALTH] TCP result: SUCCESS to {host}:{port}");
            TestContext.WriteLine($"✅ [Temporal Health] TCP connection successful to {host}:{port}");
            
            // Step 2: Namespace verification (verify "default" namespace exists)
            // This ensures Temporal auto-setup has completed namespace creation
            LogDebug($"[TEMPORAL-HEALTH] Step 2: Namespace verification");
            TestContext.WriteLine($"🔍 [Temporal Health] Step 2: Namespace verification for 'default'");
            var client = await Temporalio.Client.TemporalClient.ConnectAsync(
                new Temporalio.Client.TemporalClientConnectOptions
                {
                    TargetHost = temporalEndpoint,
                    Namespace = "default"
                });
            
            // If we successfully connected with namespace "default", it exists and is ready
            // Note: TemporalClient doesn't implement IDisposable, so no using statement needed
            LogDebug($"[TEMPORAL-HEALTH] Namespace verification SUCCESS");
            TestContext.WriteLine($"✅ [Temporal Health] Successfully connected with namespace 'default' - Temporal is READY");
            return true;
        }
        catch (Temporalio.Exceptions.RpcException ex) when (ex.Message.Contains("Namespace default is not found"))
        {
            // Namespace doesn't exist yet - Temporal still initializing
            LogDebug($"[TEMPORAL-HEALTH] Namespace verification FAILED: {ex.Message}");
            TestContext.WriteLine($"⏳ [Temporal Health] Namespace 'default' not found yet: {ex.Message}");
            return false;
        }
        catch (Exception ex)
        {
            // Other connection errors
            LogDebug($"[TEMPORAL-HEALTH] Connection error: {ex.GetType().Name}: {ex.Message}");
            TestContext.WriteLine($"❌ [Temporal Health] Connection error: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Try to discover all infrastructure endpoints with logging
    /// </summary>
    private static async Task<(
        string? flinkIp,
        string? hostEndpoint,
        string? temporal,
        string? redis,
        string? prometheus,
        string? grafana)> TryDiscoverEndpointsAsync(
        string? currentFlinkIp,
        string? currentHostEndpoint,
        string? currentTemporal,
        string? currentRedis,
        string? currentPrometheus,
        string? currentGrafana,
        bool isLearningCourse,
        Stopwatch stopwatch,
        int iteration)
    {
        try
        {
            string? flinkIp = currentFlinkIp;
            string? hostEndpoint = currentHostEndpoint;
            string? temporal = currentTemporal;
            string? redis = currentRedis;
            string? prometheus = currentPrometheus;
            string? grafana = currentGrafana;
            
            // Kafka discovery (always required)
            if (flinkIp == null)
            {
                try
                {
                    flinkIp = await DockerInfrastructure.GetKafkaContainerIpAsync();
                    if (flinkIp != null)
                    {
                        LogDebug($"[DISCOVERY] Kafka container IP discovered: {flinkIp}");
                        TestContext.WriteLine($"✅ Kafka container IP discovered: {flinkIp} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                        // Log docker ps after successful Kafka IP discovery
                        await DockerInfrastructure.LogDockerPsAsync("After Kafka IP Discovery", _debugLogWriter);
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"[DISCOVERY] Failed to get Kafka container IP: {ex.Message}");
                    if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                    {
                        TestContext.WriteLine($"⚠️ Kafka container IP discovery failed (iteration {iteration}): {ex.Message}");
                    }
                }
            }
            
            if (hostEndpoint == null)
            {
                try
                {
                    hostEndpoint = await DockerInfrastructure.GetKafkaHostEndpointAsync();
                    if (hostEndpoint != null)
                    {
                        LogDebug($"[DISCOVERY] Kafka host endpoint discovered: {hostEndpoint}");
                        TestContext.WriteLine($"✅ Kafka host endpoint discovered: {hostEndpoint} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"[DISCOVERY] Failed to get Kafka host endpoint: {ex.Message}");
                    if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                    {
                        TestContext.WriteLine($"⚠️ Kafka host endpoint discovery failed (iteration {iteration}): {ex.Message}");
                    }
                }
            }
            
            // Temporal discovery (always required)
            if (temporal == null)
            {
                try
                {
                    temporal = await DockerInfrastructure.GetTemporalHostEndpointAsync();
                    if (temporal != null)
                    {
                        LogDebug($"[DISCOVERY] Temporal endpoint discovered: {temporal}");
                        TestContext.WriteLine($"✅ Temporal endpoint discovered: {temporal} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                    }
                }
                catch (Exception ex)
                {
                    LogDebug($"[DISCOVERY] Failed to get Temporal endpoint: {ex.Message}");
                    if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                    {
                        TestContext.WriteLine($"⚠️ Temporal endpoint discovery failed (iteration {iteration}): {ex.Message}");
                    }
                }
            }
            
            // LearningCourse infrastructure discovery (only when LEARNINGCOURSE=true)
            if (isLearningCourse)
            {
                if (redis == null)
                {
                    try
                    {
                        redis = await DockerInfrastructure.GetRedisHostEndpointAsync();
                        if (redis != null)
                        {
                            LogDebug($"[DISCOVERY] Redis endpoint discovered: {redis}");
                            TestContext.WriteLine($"✅ Redis endpoint discovered: {redis} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"[DISCOVERY] Failed to get Redis endpoint: {ex.Message}");
                        if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                        {
                            TestContext.WriteLine($"⚠️ Redis endpoint discovery failed (iteration {iteration}): {ex.Message}");
                        }
                    }
                }
                
                if (prometheus == null)
                {
                    try
                    {
                        prometheus = await DockerInfrastructure.GetPrometheusHostEndpointAsync();
                        if (prometheus != null)
                        {
                            LogDebug($"[DISCOVERY] Prometheus endpoint discovered: {prometheus}");
                            TestContext.WriteLine($"✅ Prometheus endpoint discovered: {prometheus} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"[DISCOVERY] Failed to get Prometheus endpoint: {ex.Message}");
                        if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                        {
                            TestContext.WriteLine($"⚠️ Prometheus endpoint discovery failed (iteration {iteration}): {ex.Message}");
                        }
                    }
                }
                
                if (grafana == null)
                {
                    try
                    {
                        grafana = await DockerInfrastructure.GetGrafanaHostEndpointAsync();
                        if (grafana != null)
                        {
                            LogDebug($"[DISCOVERY] Grafana endpoint discovered: {grafana}");
                            TestContext.WriteLine($"✅ Grafana endpoint discovered: {grafana} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                        }
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"[DISCOVERY] Failed to get Grafana endpoint: {ex.Message}");
                        if (iteration % 10 == 1) // Log every 10th iteration to reduce noise
                        {
                            TestContext.WriteLine($"⚠️ Grafana endpoint discovery failed (iteration {iteration}): {ex.Message}");
                        }
                    }
                }
            }
            
            return (flinkIp, hostEndpoint, temporal, redis, prometheus, grafana);
        }
        catch (Exception ex)
        {
            LogDebug($"[DISCOVERY] Unexpected error in TryDiscoverEndpointsAsync: {ex}");
            TestContext.WriteLine($"❌ Unexpected error in endpoint discovery: {ex.Message}");
            return (null, null, null, null, null, null);
        }
    }

    /// <summary>
    /// Kill any orphaned processes from previous test runs.
    /// This prevents "address already in use" errors and infrastructure startup failures.
    /// Kills both JobGateway (port 8080) and AppHost processes.
    /// </summary>
    private static void KillOrphanedJobGatewayProcesses()
    {
        try
        {
            // Kill both JobGateway and AppHost processes to ensure clean state
            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -Command \"" +
                    "Get-Process -Name 'FlinkDotNet.JobGateway','LocalTesting.FlinkSqlAppHost' -ErrorAction SilentlyContinue | " +
                    "ForEach-Object { Stop-Process -Id $_.Id -Force }\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = Process.Start(psi);
            if (process != null)
            {
                process.WaitForExit(TimeSpan.FromSeconds(5));
                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();
                
                if (!string.IsNullOrWhiteSpace(output))
                {
                    TestContext.WriteLine($"   ✅ Killed orphaned processes: {output}");
                }
                if (!string.IsNullOrWhiteSpace(error) && !error.Contains("Cannot find a process"))
                {
                    TestContext.WriteLine($"   ⚠️ Error during cleanup: {error}");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️ Error killing orphaned processes: {ex.Message}");
            // Don't throw - cleanup is best effort
        }
    }
    
    /// <summary>
    /// Stop LocalTesting AppHost after all tests complete.
    /// Force kills the process and manually cleans up containers.
    /// Called by each test assembly's SetUpFixture.
    /// Idempotent - safe to call multiple times (will only teardown once).
    /// </summary>
    public static void GlobalTearDown()
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] GlobalTearDown called");
        LogDebug("[TEARDOWN] GlobalTearDown called");
        
        try
        {
                
                LogDebug("[TEARDOWN] Starting teardown process");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Starting teardown process");
                
                TestContext.WriteLine("🛑 Stopping LocalTesting AppHost...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Stopping LocalTesting AppHost...");
                
                // Kill any orphaned JobGateway processes
                TestContext.WriteLine("🧹 Killing orphaned JobGateway processes...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Killing orphaned JobGateway processes...");
                KillOrphanedJobGatewayProcesses();
            
                // Cancel all Flink jobs BEFORE copying logs and stopping containers
                TestContext.WriteLine("🧹 Cancelling all running Flink jobs...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Cancelling Flink jobs...");
                LogDebug("[TEARDOWN] Cancelling Flink jobs...");
                CancelAllFlinkJobsSync();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Flink jobs cancelled");
                LogDebug("[TEARDOWN] Flink jobs cancelled");
                
                // Copy logs from all containers BEFORE stopping them
                TestContext.WriteLine("📋 Copying container logs...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Copying container logs...");
                LogDebug("[TEARDOWN] Copying container logs...");
                CopyFlinkLogs();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Flink logs copied");
                CopyTemporalLogs();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Temporal logs copied");
                CopyPostgreSqlLogs();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] PostgreSQL logs copied");
                LogDebug("[TEARDOWN] All logs copied");
                
                if (_appHostProcess != null && !_appHostProcess.HasExited)
                {
                    try
                    {
                        TestContext.WriteLine($"⚠️ Force killing AppHost process (PID: {_appHostProcess.Id})...");
                        _appHostProcess.Kill(entireProcessTree: true);
                        
                        // Give it 5 seconds to terminate
                        if (_appHostProcess.WaitForExit(TimeSpan.FromSeconds(5)))
                        {
                            TestContext.WriteLine("✅ AppHost process terminated");
                        }
                        else
                        {
                            TestContext.WriteLine("⚠️ Process did not terminate within 5 seconds");
                        }
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"⚠️ Error killing AppHost: {ex.Message}");
                    }
                    finally
                    {
                        _appHostProcess.Dispose();
                        _appHostProcess = null;
                    }
                }
                
                // Manually clean up containers since force kill doesn't allow Aspire to clean them up
                TestContext.WriteLine("🧹 Manually cleaning up containers...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Cleaning up containers...");
                LogDebug("[TEARDOWN] Cleaning up containers...");
                CleanupContainers();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Containers cleaned up");
                
                // Prune Docker volumes to prevent "exceeded num_locks" error
                TestContext.WriteLine("🧹 Pruning orphaned Docker volumes...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Pruning Docker volumes...");
                LogDebug("[TEARDOWN] Pruning Docker volumes...");
                PruneDockerVolumes();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Docker volumes pruned");
                
            LogDebug("[TEARDOWN] Teardown complete");
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Teardown complete");
            TestContext.WriteLine("✅ Teardown complete");
        }
        finally
        {
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Finally block - disposing resources...");
            _debugLogWriter?.Dispose();
            _debugLogWriter = null;
            Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Resources disposed, teardown finished");
        }
    }
    
    /// <summary>
    /// Copy Flink logs from Flink containers to host filesystem before containers are stopped.
    /// This includes Apache Flink logs (JobManager, TaskManager, SQL Gateway) which contain
    /// embedded FlinkJobRunner application logs since FlinkJobRunner runs as a job within Flink.
    /// </summary>
    private static void CopyFlinkLogs()
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                TestContext.WriteLine("⚠️ Could not find repository root, skipping log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            
            // Get all Flink container IDs (JobManager and TaskManager)
            var getContainersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter label=com.microsoft.developer.usvc-dev.name --filter name=flink",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var getContainersProcess = Process.Start(getContainersPsi);
            if (getContainersProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get Flink container list");
                return;
            }
            
            var containerIds = getContainersProcess.StandardOutput.ReadToEnd().Trim();
            getContainersProcess.WaitForExit();
            
            if (string.IsNullOrWhiteSpace(containerIds))
            {
                TestContext.WriteLine("⚠️ No Flink containers found");
                return;
            }
            
            var containerIdList = containerIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            TestContext.WriteLine($"📦 Found {containerIdList.Length} Flink containers");
            
            // Copy logs from each Flink container
            foreach (var containerId in containerIdList)
            {
                CopyLogsFromContainer(containerId, testLogsDir);
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error copying Flink logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Copy all logs from /opt/flink/log/ in a specific container.
    /// This includes both Apache Flink logs and FlinkJobRunner logs (both write to same directory).
    /// Renames Apache Flink logs to Flink.{component}.log.YYYYMMDD format.
    /// FlinkJobRunner logs already follow the correct naming convention.
    /// </summary>
    private static void CopyLogsFromContainer(string containerId, string testLogsDir)
    {
        try
        {
            var logFiles = GetLogFilesFromContainer(containerId);
            if (logFiles == null || logFiles.Length == 0)
            {
                TestContext.WriteLine($"   No Flink logs found in container {containerId.Substring(0, 12)}");
                return;
            }
            
            TestContext.WriteLine($"   Found {logFiles.Length} Flink log files in container {containerId.Substring(0, 12)}");
            
            var componentType = GetContainerComponentType(containerId);
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            
            foreach (var logFile in logFiles)
            {
                CopyIndividualLogFile(containerId, logFile, testLogsDir, componentType, dateStamp);
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️ Error copying logs from container {containerId.Substring(0, 12)}: {ex.Message}");
        }
    }
    
    private static string[]? GetLogFilesFromContainer(string containerId)
    {
        var checkLogsPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec {containerId} sh -c \"ls /opt/flink/log/*.log* 2>/dev/null || echo ''\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var checkProcess = Process.Start(checkLogsPsi);
        if (checkProcess == null) return null;
        
        var logFiles = checkProcess.StandardOutput.ReadToEnd().Trim();
        checkProcess.WaitForExit();
        
        return string.IsNullOrWhiteSpace(logFiles)
            ? null
            : logFiles.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }
    
    private static string GetContainerComponentType(string containerId)
    {
        var getNamePsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"inspect --format={{{{.Name}}}} {containerId}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        
        using var getNameProcess = Process.Start(getNamePsi);
        var containerName = getNameProcess?.StandardOutput.ReadToEnd().Trim().TrimStart('/') ?? "unknown";
        getNameProcess?.WaitForExit();
        
        if (containerName.Contains("jobmanager", StringComparison.OrdinalIgnoreCase))
            return "jobmanager";
        if (containerName.Contains("taskmanager", StringComparison.OrdinalIgnoreCase) ||
            containerName.Contains("taskexecutor", StringComparison.OrdinalIgnoreCase))
            return "taskmanager";
        if (containerName.Contains("sql-gateway", StringComparison.OrdinalIgnoreCase))
            return "sql-gateway";
        
        return "unknown";
    }
    
    private static void CopyIndividualLogFile(string containerId, string logFile, string testLogsDir,
        string componentType, string dateStamp)
    {
        var fileName = Path.GetFileName(logFile);
        var destName = fileName.StartsWith("FlinkIRRunner.", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"Flink.{componentType}.log.{dateStamp}";
        
        var destPath = Path.Combine(testLogsDir, destName);
        
        var copyPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"cp {containerId}:{logFile} \"{destPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var copyProcess = Process.Start(copyPsi);
        if (copyProcess == null) return;
        
        copyProcess.WaitForExit(TimeSpan.FromSeconds(5));
        
        if (copyProcess.ExitCode == 0 && File.Exists(destPath))
        {
            var fileInfo = new FileInfo(destPath);
            TestContext.WriteLine($"   ✅ Copied {fileName} as {destName} ({fileInfo.Length} bytes)");
        }
        else
        {
            var error = copyProcess.StandardError.ReadToEnd();
            TestContext.WriteLine($"   ⚠️ Failed to copy {fileName}: {error}");
        }
    }
    
    /// <summary>
    /// Copy Temporal server logs from Temporal container to host filesystem.
    /// Temporal logs help debug workflow execution issues, especially worker initialization problems.
    /// </summary>
    private static void CopyTemporalLogs()
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                TestContext.WriteLine("⚠️ Could not find repository root, skipping Temporal log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            
            // Get Temporal container ID
            var getContainerPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter label=com.microsoft.developer.usvc-dev.name --filter name=temporal",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var getContainerProcess = Process.Start(getContainerPsi);
            if (getContainerProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get Temporal container");
                return;
            }
            
            var containerId = getContainerProcess.StandardOutput.ReadToEnd().Trim();
            getContainerProcess.WaitForExit();
            
            if (string.IsNullOrWhiteSpace(containerId))
            {
                TestContext.WriteLine("⚠️ No Temporal container found");
                return;
            }
            
            TestContext.WriteLine($"📦 Found Temporal container: {containerId.Substring(0, 12)}");
            
            // Copy Temporal server logs using docker logs command
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var destPath = Path.Combine(testLogsDir, $"Temporal.server.log.{dateStamp}");
            
            var logsPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs {containerId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var logsProcess = Process.Start(logsPsi);
            if (logsProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get Temporal logs");
                return;
            }
            
            var logs = logsProcess.StandardOutput.ReadToEnd();
            var errors = logsProcess.StandardError.ReadToEnd();
            logsProcess.WaitForExit(TimeSpan.FromSeconds(10));
            
            // Combine stdout and stderr
            var allLogs = logs;
            if (!string.IsNullOrWhiteSpace(errors))
            {
                allLogs += "\n--- STDERR ---\n" + errors;
            }
            
            if (!string.IsNullOrWhiteSpace(allLogs))
            {
                File.WriteAllText(destPath, allLogs);
                var fileInfo = new FileInfo(destPath);
                TestContext.WriteLine($"   ✅ Copied Temporal logs ({fileInfo.Length} bytes)");
            }
            else
            {
                TestContext.WriteLine("   ⚠️ No Temporal logs found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error copying Temporal logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Copy PostgreSQL logs from PostgreSQL container to host filesystem.
    /// PostgreSQL is Temporal's persistence backend, logs help debug database connection issues.
    /// </summary>
    private static void CopyPostgreSqlLogs()
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                TestContext.WriteLine("⚠️ Could not find repository root, skipping PostgreSQL log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            
            // Get PostgreSQL container ID
            var getContainerPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter label=com.microsoft.developer.usvc-dev.name --filter name=postgres",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var getContainerProcess = Process.Start(getContainerPsi);
            if (getContainerProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get PostgreSQL container");
                return;
            }
            
            var containerId = getContainerProcess.StandardOutput.ReadToEnd().Trim();
            getContainerProcess.WaitForExit();
            
            if (string.IsNullOrWhiteSpace(containerId))
            {
                TestContext.WriteLine("⚠️ No PostgreSQL container found");
                return;
            }
            
            TestContext.WriteLine($"📦 Found PostgreSQL container: {containerId.Substring(0, 12)}");
            
            // Copy PostgreSQL logs using docker logs command
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var destPath = Path.Combine(testLogsDir, $"PostgreSQL.server.log.{dateStamp}");
            
            var logsPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"logs {containerId}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var logsProcess = Process.Start(logsPsi);
            if (logsProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get PostgreSQL logs");
                return;
            }
            
            var logs = logsProcess.StandardOutput.ReadToEnd();
            var errors = logsProcess.StandardError.ReadToEnd();
            logsProcess.WaitForExit(TimeSpan.FromSeconds(10));
            
            // Combine stdout and stderr
            var allLogs = logs;
            if (!string.IsNullOrWhiteSpace(errors))
            {
                allLogs += "\n--- STDERR ---\n" + errors;
            }
            
            if (!string.IsNullOrWhiteSpace(allLogs))
            {
                File.WriteAllText(destPath, allLogs);
                var fileInfo = new FileInfo(destPath);
                TestContext.WriteLine($"   ✅ Copied PostgreSQL logs ({fileInfo.Length} bytes)");
            }
            else
            {
                TestContext.WriteLine("   ⚠️ No PostgreSQL logs found");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error copying PostgreSQL logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Write network debug information to log file for troubleshooting container connectivity.
    /// Captures Docker container state, network configuration, and port mappings.
    /// </summary>
    private static void LogNetworkState(string context)
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null) return;
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            var debugLogPath = Path.Combine(testLogsDir, $"TestInfrastructure.Debug.log.{DateTime.UtcNow:yyyyMMdd}");
            
            // Use FileShare.ReadWrite to allow multiple processes to write simultaneously
            using var fileStream = new FileStream(debugLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var writer = new StreamWriter(fileStream) { AutoFlush = true };
            writer.WriteLine($"\n==================== NETWORK DEBUG: {context} @ {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff} ====================");
            
            // Get all running containers with detailed info
            var containersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --format \"table {{.ID}}\\t{{.Image}}\\t{{.Names}}\\t{{.Status}}\\t{{.Ports}}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var containersProcess = Process.Start(containersPsi);
            if (containersProcess != null)
            {
                var output = containersProcess.StandardOutput.ReadToEnd();
                containersProcess.WaitForExit(TimeSpan.FromSeconds(5));
                writer.WriteLine("\n=== RUNNING CONTAINERS ===");
                writer.WriteLine(output);
            }
            
            // Get container count for Aspire-managed containers
            var countPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter label=com.microsoft.developer.usvc-dev.name | wc -l",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var countProcess = Process.Start(countPsi);
            if (countProcess != null)
            {
                var count = countProcess.StandardOutput.ReadToEnd().Trim();
                countProcess.WaitForExit(TimeSpan.FromSeconds(5));
                writer.WriteLine($"\n=== ASPIRE CONTAINER COUNT: {count} ===");
            }
            
            // Get specific Temporal container info if exists
            var temporalPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --filter name=temporal --format \"ID={{.ID}} Status={{.Status}} Ports={{.Ports}}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var temporalProcess = Process.Start(temporalPsi);
            if (temporalProcess != null)
            {
                var temporalInfo = temporalProcess.StandardOutput.ReadToEnd().Trim();
                temporalProcess.WaitForExit(TimeSpan.FromSeconds(5));
                writer.WriteLine($"\n=== TEMPORAL CONTAINER ===");
                writer.WriteLine(string.IsNullOrEmpty(temporalInfo) ? "NO TEMPORAL CONTAINER FOUND" : temporalInfo);
            }
            
            // Get Kafka container info
            var kafkaPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps --filter name=kafka --format \"ID={{.ID}} Status={{.Status}} Ports={{.Ports}}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var kafkaProcess = Process.Start(kafkaPsi);
            if (kafkaProcess != null)
            {
                var kafkaInfo = kafkaProcess.StandardOutput.ReadToEnd().Trim();
                kafkaProcess.WaitForExit(TimeSpan.FromSeconds(5));
                writer.WriteLine($"\n=== KAFKA CONTAINER ===");
                writer.WriteLine(string.IsNullOrEmpty(kafkaInfo) ? "NO KAFKA CONTAINER FOUND" : kafkaInfo);
            }
            
            writer.WriteLine($"\n==================== END NETWORK DEBUG: {context} ====================\n");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error logging network state: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Manually clean up all Aspire-managed containers
    /// </summary>
    private static void CleanupContainers()
    {
        try
        {
            // Get all container IDs with Aspire DCP labels
            var getContainersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -aq --filter label=com.microsoft.developer.usvc-dev.name",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var getContainersProcess = Process.Start(getContainersPsi);
            if (getContainersProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get container list");
                return;
            }
            
            var containerIds = getContainersProcess.StandardOutput.ReadToEnd().Trim();
            getContainersProcess.WaitForExit();
            
            if (string.IsNullOrWhiteSpace(containerIds))
            {
                TestContext.WriteLine("✅ No containers to clean up");
                return;
            }
            
            var containerIdList = containerIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            TestContext.WriteLine($"📦 Found {containerIdList.Length} Aspire containers to clean up");
            
            // Stop all containers with timeout
            var stopPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"stop -t 5 {string.Join(" ", containerIdList)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var stopProcess = Process.Start(stopPsi);
            if (stopProcess != null)
            {
                stopProcess.WaitForExit(TimeSpan.FromSeconds(30));
                var stopOutput = stopProcess.StandardOutput.ReadToEnd();
                TestContext.WriteLine($"✅ Stopped {containerIdList.Length} containers");
                if (!string.IsNullOrWhiteSpace(stopOutput))
                {
                    TestContext.WriteLine($"   Stop output: {stopOutput.Trim()}");
                }
            }
            
            // Remove all containers forcefully
            var rmPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"rm -f {string.Join(" ", containerIdList)}",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var rmProcess = Process.Start(rmPsi);
            if (rmProcess != null)
            {
                rmProcess.WaitForExit(TimeSpan.FromSeconds(15));
                var rmOutput = rmProcess.StandardOutput.ReadToEnd();
                TestContext.WriteLine($"✅ Removed {containerIdList.Length} containers");
                if (!string.IsNullOrWhiteSpace(rmOutput))
                {
                    TestContext.WriteLine($"   Remove output: {rmOutput.Trim()}");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error cleaning up containers: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Prune orphaned Docker volumes to prevent "exceeded num_locks" error.
    /// Orphaned volumes accumulate over time and can exhaust Docker's volume lock limit (default 2048).
    /// This cleanup prevents "allocation failed; exceeded num_locks" errors.
    /// </summary>
    private static void PruneDockerVolumes()
    {
        try
        {
            var prunePsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "volume prune -f",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var pruneProcess = Process.Start(prunePsi);
            if (pruneProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to start volume prune command");
                return;
            }
            
            pruneProcess.WaitForExit(TimeSpan.FromSeconds(30));
            var output = pruneProcess.StandardOutput.ReadToEnd();
            var error = pruneProcess.StandardError.ReadToEnd();
            
            if (pruneProcess.ExitCode == 0)
            {
                // Extract reclaimed space from output (e.g., "Total reclaimed space: 36.41GB")
                var reclaimedMatch = System.Text.RegularExpressions.Regex.Match(output, @"Total reclaimed space:\s*([\d.]+\s*[KMGT]?B)");
                if (reclaimedMatch.Success)
                {
                    TestContext.WriteLine($"✅ Docker volume prune successful - Reclaimed: {reclaimedMatch.Groups[1].Value}");
                }
                else
                {
                    TestContext.WriteLine("✅ Docker volume prune successful");
                }
            }
            else
            {
                TestContext.WriteLine($"⚠️ Docker volume prune failed with exit code {pruneProcess.ExitCode}");
                if (!string.IsNullOrWhiteSpace(error))
                {
                    TestContext.WriteLine($"   Error: {error.Trim()}");
                }
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error pruning Docker volumes: {ex.Message}");
            // Don't throw - cleanup is best effort
        }
    }

    /// <summary>
    /// Find the repository root by looking for global.json
    /// </summary>
    private static string? FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Cancel all running Flink jobs synchronously (for use in GlobalTearDown)
    /// </summary>
    private static void CancelAllFlinkJobsSync()
    {
        try
        {
            var flinkGatewayUrl = Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";
            
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            
            // Get all running jobs
            var jobsResponse = httpClient.GetAsync($"{flinkGatewayUrl}/jobs").Result;
            if (!jobsResponse.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"   ⚠️ Could not get job list (this is OK if no jobs are running): {jobsResponse.StatusCode}");
                return;
            }
            
            var jobsJson = jobsResponse.Content.ReadAsStringAsync().Result;
            
            // Parse job IDs using simple string parsing
            var jobIds = new List<string>();
            var matches = System.Text.RegularExpressions.Regex.Matches(jobsJson, @"""id""\s*:\s*""([a-f0-9]{32})""");
            foreach (System.Text.RegularExpressions.Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    jobIds.Add(match.Groups[1].Value);
                }
            }
            
            if (jobIds.Count == 0)
            {
                TestContext.WriteLine("   ✅ No running Flink jobs to cancel");
                return;
            }
            
            TestContext.WriteLine($"   📋 Found {jobIds.Count} running job(s) to cancel");
            
            // Cancel each job
            var successCount = 0;
            foreach (var jobId in jobIds)
            {
                try
                {
                    var cancelResponse = httpClient.PatchAsync($"{flinkGatewayUrl}/jobs/{jobId}?mode=cancel", null).Result;
                    
                    if (cancelResponse.IsSuccessStatusCode)
                    {
                        successCount++;
                    }
                    else
                    {
                        TestContext.WriteLine($"   ⚠️ Failed to cancel job {jobId}: {cancelResponse.StatusCode}");
                    }
                }
                catch (Exception ex)
                {
                    TestContext.WriteLine($"   ⚠️ Error cancelling job {jobId}: {ex.Message}");
                }
            }
            
            TestContext.WriteLine($"   ✅ Cancelled {successCount}/{jobIds.Count} Flink jobs");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️ Error in job cancellation: {ex.Message}");
        }
    }

    /// <summary>
    /// Get the total message count for a Kafka topic across all partitions.
    /// Used for progress monitoring during exercise execution.
    /// </summary>
    private static async Task<long> GetKafkaTopicMessageCountAsync(string topicName)
    {
        try
        {
            if (string.IsNullOrEmpty(KafkaHostBootstrapServers))
            {
                throw new InvalidOperationException("Kafka bootstrap servers not discovered");
            }

            var adminConfig = new AdminClientConfig
            {
                BootstrapServers = KafkaHostBootstrapServers
            };

            using var adminClient = new AdminClientBuilder(adminConfig).Build();
            
            // Get topic metadata to find partitions
            var metadata = adminClient.GetMetadata(topicName, TimeSpan.FromSeconds(5));
            var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == topicName);
            
            if (topicMetadata == null)
            {
                return 0; // Topic doesn't exist yet
            }

            long totalMessages = 0;
            
            // Use a consumer to query watermark offsets (AdminClient doesn't have QueryWatermarkOffsets)
            var consumerConfig = new ConsumerConfig
            {
                BootstrapServers = KafkaHostBootstrapServers,
                GroupId = $"test-consumer-{Guid.NewGuid()}", // Unique group ID for monitoring
                AutoOffsetReset = AutoOffsetReset.Earliest
            };

            using var consumer = new ConsumerBuilder<byte[], byte[]>(consumerConfig).Build();
            
            // Query each partition's high watermark (total messages)
            foreach (var partition in topicMetadata.Partitions)
            {
                var topicPartition = new TopicPartition(topicName, partition.PartitionId);
                
                // Get the high watermark (end offset) for this partition using consumer
                var watermarkOffsets = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));
                
                // Message count = high watermark - low watermark
                var messageCount = watermarkOffsets.High.Value - watermarkOffsets.Low.Value;
                totalMessages += messageCount;
            }

            // Satisfy async method signature (no actual async operations needed for Confluent.Kafka sync API)
            return await Task.FromResult(totalMessages);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to get message count for topic {topicName}: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Execute an exercise program with progress monitoring based on Kafka topic message counts.
    /// Extends timeout automatically when progress is detected (messages flowing from input to output topics).
    /// Detects hangs quickly (30 seconds without progress).
    /// </summary>
    protected async Task<(int exitCode, string output, string error)> ExecuteExerciseWithProgressMonitoringAsync(
        string exercisePath,
        string inputTopic,
        string outputTopic,
        string[]? arguments = null,
        TimeSpan? baseTimeout = null)
    {
        var repoRoot = FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root");
        var fullPath = Path.Combine(repoRoot, "LearningCourse", exercisePath);

        LogDebug($"[EXERCISE-PROGRESS] Starting {exercisePath} with progress monitoring");
        LogDebug($"[EXERCISE-PROGRESS] Input topic: {inputTopic}, Output topic: {outputTopic}");
        
        // Ensure infrastructure is ready before executing exercises
        if (!_isSetupComplete)
        {
            LogDebug("[EXERCISE-PROGRESS] Infrastructure not ready, starting setup...");
            TestContext.WriteLine("⚠️ Infrastructure not ready, initializing...");
            await GlobalSetUp();
        }
        
        TestContext.WriteLine($"🏃 Executing exercise with progress monitoring: {exercisePath}");
        TestContext.WriteLine($"📊 Monitoring progress: {inputTopic} → {outputTopic}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --configuration Release {string.Join(" ", arguments ?? Array.Empty<string>())}",
            WorkingDirectory = fullPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        // Set environment variables (same as ExecuteExerciseAsync)
        if (string.IsNullOrEmpty(KafkaHostBootstrapServers) || string.IsNullOrEmpty(KafkaFlinkBootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers not discovered. Ensure GlobalSetUp ran successfully.");
        }
        
        psi.Environment["KAFKA_BOOTSTRAP_SERVERS"] = KafkaHostBootstrapServers;
        psi.Environment["KAFKA_FLINK_BOOTSTRAP_SERVERS"] = KafkaFlinkBootstrapServers;
        
        if (!string.IsNullOrEmpty(TemporalHostEndpoint))
        {
            psi.Environment["TEMPORAL_ENDPOINT"] = TemporalHostEndpoint;
        }
        
        if (!string.IsNullOrEmpty(RedisHostEndpoint))
        {
            psi.Environment["REDIS_ENDPOINT"] = RedisHostEndpoint;
        }
        
        var testLogsDir = Path.GetFullPath(Path.Combine(repoRoot, "LocalTesting", "test-logs"));
        psi.Environment["LOG_FILE_PATH"] = testLogsDir;
        
        TestContext.WriteLine($"🔧 KAFKA_BOOTSTRAP_SERVERS={KafkaHostBootstrapServers}");
        TestContext.WriteLine($"🔧 KAFKA_FLINK_BOOTSTRAP_SERVERS={KafkaFlinkBootstrapServers}");

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start exercise: {exercisePath}");
        }

        TestContext.WriteLine($"🔍 Process started with PID {process.Id}");
        
        // Capture output incrementally
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        var outputLock = new object();
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (outputLock)
                {
                    outputBuilder.AppendLine(e.Data);
                }
                TestContext.WriteLine($"[Exercise Output] {e.Data}");
            }
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (outputLock)
                {
                    errorBuilder.AppendLine(e.Data);
                }
                TestContext.WriteLine($"[Exercise Error] {e.Data}");
            }
        };
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Progress monitoring loop
        var progressTimeout = TimeSpan.FromSeconds(30); // Timeout if no progress for 30 seconds
        var lastProgressTime = DateTime.UtcNow;
        var lastProgressPercent = 0.0;
        var waitStopwatch = Stopwatch.StartNew();
        var checkInterval = TimeSpan.FromSeconds(5); // Check progress every 5 seconds
        
        TestContext.WriteLine($"📊 Progress monitoring active: 30s no-progress timeout with automatic extensions");
        
        while (!process.HasExited)
        {
            await Task.Delay(checkInterval);
            
            // Query Kafka topics for message counts
            var inputCount = await GetKafkaTopicMessageCountAsync(inputTopic);
            var outputCount = await GetKafkaTopicMessageCountAsync(outputTopic);
            
            // Calculate progress percentage
            var currentProgress = inputCount > 0 ? (outputCount * 100.0 / inputCount) : 0.0;
            
            // Check if progress has changed
            if (Math.Abs(currentProgress - lastProgressPercent) > 0.1) // Progress changed by > 0.1%
            {
                lastProgressTime = DateTime.UtcNow;
                lastProgressPercent = currentProgress;
                TestContext.WriteLine($"📈 Progress: {outputCount}/{inputCount} messages ({currentProgress:F1}%) - extending timeout");
                LogDebug($"[EXERCISE-PROGRESS] Progress detected: {currentProgress:F1}% ({outputCount}/{inputCount})");
            }
            
            var timeSinceProgress = DateTime.UtcNow - lastProgressTime;
            
            // Check for progress timeout (no progress for 30 seconds)
            if (timeSinceProgress > progressTimeout)
            {
                TestContext.WriteLine($"❌ No progress for {timeSinceProgress.TotalSeconds:F1}s (last: {lastProgressPercent:F1}%)");
                TestContext.WriteLine($"❌ Killing process after {waitStopwatch.Elapsed.TotalSeconds:F1}s total");
                process.Kill(entireProcessTree: true);
                throw new TimeoutException(
                    $"Exercise {exercisePath} timed out: no progress for {timeSinceProgress.TotalSeconds:F1}s. " +
                    $"Last progress: {lastProgressPercent:F1}% ({outputCount}/{inputCount} messages)");
            }
        }
        
        TestContext.WriteLine($"✅ Process completed after {waitStopwatch.Elapsed.TotalSeconds:F1}s");
        TestContext.WriteLine($"✅ Final progress: {lastProgressPercent:F1}%");

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        TestContext.WriteLine($"✅ Exercise completed with exit code {process.ExitCode}");
        
        // Network state already logged in OneTimeSetup - no need to log after each exercise
        // LogNetworkState($"AFTER Exercise: {exercisePath} (ExitCode: {process.ExitCode})");

        return (process.ExitCode, output, error);
    }

    /// <summary>
    /// Execute an exercise program and capture its output.
    /// Automatically parses and tracks Flink job IDs for cleanup.
    /// </summary>
    protected async Task<(int exitCode, string output, string error)> ExecuteExerciseAsync(
        string exercisePath,
        string[]? arguments = null,
        TimeSpan? timeout = null)
    {
        var repoRoot = FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root");
        var fullPath = Path.Combine(repoRoot, "LearningCourse", exercisePath);

        LogDebug($"[EXERCISE] Starting {exercisePath}");
        LogDebug($"[EXERCISE] TEMPORAL_ENDPOINT={TemporalHostEndpoint}");
        LogDebug($"[EXERCISE] Test infrastructure state: _isSetupComplete={_isSetupComplete}");
        
        // Ensure infrastructure is ready before executing exercises
        if (!_isSetupComplete)
        {
            LogDebug("[EXERCISE] Infrastructure not ready, starting setup...");
            TestContext.WriteLine("⚠️ Infrastructure not ready, initializing...");
            await GlobalSetUp();
        }
        
        TestContext.WriteLine($"🏃 Executing exercise: {exercisePath}");

        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --no-build --configuration Release {string.Join(" ", arguments ?? Array.Empty<string>())}",
            WorkingDirectory = fullPath,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        // Set Kafka environment variables for exercise process
        // Two different addresses needed because of Docker networking:
        // 1. KAFKA_BOOTSTRAP_SERVERS: For exercise's own Kafka operations (producer/consumer on host)
        // 2. KAFKA_FLINK_BOOTSTRAP_SERVERS: For Flink job configurations (container-to-container)
        //
        // Docker bridge network doesn't support DNS between containers, so Flink needs actual container IP
        if (string.IsNullOrEmpty(KafkaHostBootstrapServers) || string.IsNullOrEmpty(KafkaFlinkBootstrapServers))
        {
            throw new InvalidOperationException("Kafka bootstrap servers not discovered. Ensure GlobalSetUp ran successfully.");
        }
        
        psi.Environment["KAFKA_BOOTSTRAP_SERVERS"] = KafkaHostBootstrapServers;
        psi.Environment["KAFKA_FLINK_BOOTSTRAP_SERVERS"] = KafkaFlinkBootstrapServers;
        
        // Set TEMPORAL_ENDPOINT for Day06 Temporal workflow exercises
        if (!string.IsNullOrEmpty(TemporalHostEndpoint))
        {
            psi.Environment["TEMPORAL_ENDPOINT"] = TemporalHostEndpoint;
        }
        
        // Set REDIS_ENDPOINT for Day15 exercises that use Redis
        if (!string.IsNullOrEmpty(RedisHostEndpoint))
        {
            psi.Environment["REDIS_ENDPOINT"] = RedisHostEndpoint;
        }
        
        // Set LOG_FILE_PATH to ensure all logs go to LocalTesting/test-logs/
        // Use absolute path to ensure logs are written to the correct location
        var testLogsDir = Path.GetFullPath(Path.Combine(repoRoot, "LocalTesting", "test-logs"));
        psi.Environment["LOG_FILE_PATH"] = testLogsDir;
        
        TestContext.WriteLine($"🔧 Setting KAFKA_BOOTSTRAP_SERVERS={KafkaHostBootstrapServers} for exercise (host access)");
        TestContext.WriteLine($"🔧 Setting KAFKA_FLINK_BOOTSTRAP_SERVERS={KafkaFlinkBootstrapServers} for Flink jobs (container access)");
        if (!string.IsNullOrEmpty(TemporalHostEndpoint))
        {
            TestContext.WriteLine($"🔧 Setting TEMPORAL_ENDPOINT={TemporalHostEndpoint} for Temporal workflows");
        }
        if (!string.IsNullOrEmpty(RedisHostEndpoint))
        {
            TestContext.WriteLine($"🔧 Setting REDIS_ENDPOINT={RedisHostEndpoint} for Redis state management");
        }
        TestContext.WriteLine($"🔧 Setting LOG_FILE_PATH={testLogsDir} for centralized logging");

        // Add process startup diagnostic logging
        TestContext.WriteLine($"🔍 [DIAGNOSTIC] About to start process: dotnet run --no-build --configuration Release");
        TestContext.WriteLine($"🔍 [DIAGNOSTIC] Working directory: {fullPath}");
        TestContext.WriteLine($"🔍 [DIAGNOSTIC] Timeout: {timeout?.TotalSeconds ?? 60} seconds");
        
        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start exercise: {exercisePath}");
        }

        TestContext.WriteLine($"🔍 [DIAGNOSTIC] Process started with PID {process.Id}, waiting for output...");
        
        // Capture output incrementally to detect progress and extend timeout dynamically
        var outputBuilder = new System.Text.StringBuilder();
        var errorBuilder = new System.Text.StringBuilder();
        var lastOutputTime = DateTime.UtcNow;
        var outputLock = new object();
        
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (outputLock)
                {
                    outputBuilder.AppendLine(e.Data);
                    lastOutputTime = DateTime.UtcNow; // Update last output time on any output
                }
                TestContext.WriteLine($"[Exercise Output] {e.Data}");
            }
        };
        
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                lock (outputLock)
                {
                    errorBuilder.AppendLine(e.Data);
                    lastOutputTime = DateTime.UtcNow; // Update last output time on any error output
                }
                TestContext.WriteLine($"[Exercise Error] {e.Data}");
            }
        };
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Dynamic timeout: automatic extensions when there's progress (output produced)
        // This matches LocalTesting pattern: extends timeout when output is produced
        var noProgressTimeout = TimeSpan.FromSeconds(45); // Kill if no output for 45 seconds (increased from 20s for ML/processing-intensive exercises)
        var waitStopwatch = Stopwatch.StartNew();
        
        // Poll for process exit with dynamic timeout extension
        while (!process.HasExited)
        {
            var timeSinceLastOutput = DateTime.UtcNow - lastOutputTime;
            
            // Check if we've exceeded the no-progress timeout
            if (timeSinceLastOutput > noProgressTimeout)
            {
                TestContext.WriteLine($"❌ [DIAGNOSTIC] No output for {timeSinceLastOutput.TotalSeconds:F1}s (threshold: {noProgressTimeout.TotalSeconds}s)");
                TestContext.WriteLine($"❌ [DIAGNOSTIC] Killing process tree for PID {process.Id} after {waitStopwatch.Elapsed.TotalSeconds:F1}s total...");
                process.Kill(entireProcessTree: true);
                throw new TimeoutException($"Exercise {exercisePath} timed out after {waitStopwatch.Elapsed.TotalSeconds:F1}s with no output for {timeSinceLastOutput.TotalSeconds:F1}s");
            }
            
            // Wait a bit before checking again
            await Task.Delay(500);
        }
        
        TestContext.WriteLine($"✅ [DIAGNOSTIC] Process exited after {waitStopwatch.Elapsed.TotalSeconds:F1}s (last output: {(DateTime.UtcNow - lastOutputTime).TotalSeconds:F1}s ago)");

        var output = outputBuilder.ToString();
        var error = errorBuilder.ToString();

        TestContext.WriteLine($"✅ Exercise completed with exit code {process.ExitCode}");
        if (!string.IsNullOrEmpty(output))
        {
            TestContext.WriteLine($"📝 Output:\n{output}");
        }
        if (!string.IsNullOrEmpty(error))
        {
            TestContext.WriteLine($"⚠️ Error output:\n{error}");
        }
        
        // Network state already logged in OneTimeSetup - no need to log after each exercise
        // LogNetworkState($"AFTER Exercise: {exercisePath} (ExitCode: {process.ExitCode})");

        return (process.ExitCode, output, error);
    }
    
    /// <summary>
    /// Write debug log message to both console and file
    /// </summary>
    private static void LogDebug(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] {message}";
        Console.WriteLine(logMessage);
        _debugLogWriter?.WriteLine(logMessage);
    }
}