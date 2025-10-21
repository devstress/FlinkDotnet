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
        
        // CRITICAL: Reset all static endpoint properties to null
        // This ensures fresh discovery for each test run when containers restart with new ports
        KafkaFlinkBootstrapServers = null;
        KafkaHostBootstrapServers = null;
        TemporalHostEndpoint = null;
        RedisHostEndpoint = null;
        PrometheusHostEndpoint = null;
        GrafanaHostEndpoint = null;
        _isSetupComplete = false;
        
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [SETUP] Static endpoint properties reset for fresh discovery");
        
        // Kill any orphaned processes from previous test runs
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
                await GlobalTearDownAsync();
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
        bool kafkaReady = false;
        bool temporalReady = false;
        
        // Check if LearningCourse mode is enabled for optional infrastructure
        var isLearningCourse = Environment.GetEnvironmentVariable("LEARNINGCOURSE")?.ToLower() == "true";
        
        int iteration = 0;
        while (stopwatch.Elapsed < maxWait)
        {
            iteration++;
            LogDebug($"[POLL] Iteration {iteration} at {stopwatch.Elapsed.TotalSeconds:F1}s: KafkaReady={kafkaReady}, FlinkReady={flinkReady}, TemporalReady={temporalReady}");
            
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
            
            var kafkaHealthTask = (kafkaHostEndpoint != null && !kafkaReady)
                ? IsKafkaHealthyAsync(kafkaHostEndpoint)
                : Task.FromResult(kafkaReady);
            
            var flinkHealthTask = (kafkaFlinkIp != null && kafkaHostEndpoint != null && !flinkReady)
                ? IsFlinkHealthyAsync()
                : Task.FromResult(flinkReady);
            
            var temporalHealthTask = (temporalEndpoint != null && !temporalReady)
                ? IsTemporalHealthyAsync(temporalEndpoint)
                : Task.FromResult(temporalReady);
            
            // Wait for all checks in parallel
            await Task.WhenAll(discoveryTask, kafkaHealthTask, flinkHealthTask, temporalHealthTask);
            
            // Update discovered endpoints
            var discovered = await discoveryTask;
            kafkaFlinkIp = discovered.flinkIp ?? kafkaFlinkIp;
            kafkaHostEndpoint = discovered.hostEndpoint ?? kafkaHostEndpoint;
            temporalEndpoint = discovered.temporal ?? temporalEndpoint;
            redisEndpoint = discovered.redis ?? redisEndpoint;
            prometheusEndpoint = discovered.prometheus ?? prometheusEndpoint;
            grafanaEndpoint = discovered.grafana ?? grafanaEndpoint;
            
            // Update health check results
            var newKafkaReady = await kafkaHealthTask;
            if (newKafkaReady && !kafkaReady)
            {
                kafkaReady = true;
                TestContext.WriteLine($"✅ Kafka is healthy and ready (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
            }
            
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
            
            // Core infrastructure must be ready: Kafka (healthy) and Flink
            // Temporal is optional (not all tests need it - e.g., Day05 Prometheus metrics test)
            // LearningCourse infrastructure: Redis, Prometheus, and Grafana are ALL REQUIRED
            // Day 05 observability tests specifically need Prometheus/Grafana endpoints
            var coreReady = kafkaFlinkIp != null && kafkaHostEndpoint != null && kafkaReady && flinkReady;
            var learningCourseReady = !isLearningCourse ||
                (redisEndpoint != null && prometheusEndpoint != null && grafanaEndpoint != null);
            
            if (coreReady && learningCourseReady)
            {
                KafkaFlinkBootstrapServers = kafkaFlinkIp;
                KafkaHostBootstrapServers = kafkaHostEndpoint;
                TemporalHostEndpoint = temporalEndpoint;
                RedisHostEndpoint = redisEndpoint;
                PrometheusHostEndpoint = prometheusEndpoint;
                GrafanaHostEndpoint = grafanaEndpoint;
                
                var savedTime = (maxWait - stopwatch.Elapsed).TotalSeconds;
                TestContext.WriteLine($"✅ All required infrastructure ready after {stopwatch.Elapsed.TotalSeconds:F1}s (saved {savedTime:F1}s with optimized polling)");
                if (isLearningCourse)
                {
                    TestContext.WriteLine($"   📚 LearningCourse infrastructure verified:");
                    TestContext.WriteLine($"      • Redis: {redisEndpoint} ✓");
                    TestContext.WriteLine($"      • Prometheus: {prometheusEndpoint} ✓");
                    TestContext.WriteLine($"      • Grafana: {grafanaEndpoint} ✓");
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
            $"KafkaReady: {kafkaReady}, " +
            $"FlinkReady: {flinkReady}" +
            (isLearningCourse ? $", Redis: {RedisHostEndpoint ?? "null"} (REQUIRED), Prometheus: {PrometheusHostEndpoint ?? "null"} (REQUIRED), Grafana: {GrafanaHostEndpoint ?? "null"} (REQUIRED)" : "") +
            $" (Temporal is optional: {TemporalHostEndpoint ?? "not discovered"}, TemporalReady: {temporalReady})");
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
    /// Check if Kafka is healthy and ready to accept connections.
    /// Verifies both broker connectivity and topic metadata availability.
    /// Implements 4 retries with 5-second delays for robust validation.
    /// </summary>
    private static async Task<bool> IsKafkaHealthyAsync(string kafkaHostEndpoint)
    {
        const int maxRetries = 4;
        const int retryDelaySeconds = 5;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                LogDebug($"[KAFKA-HEALTH] Checking Kafka health at {kafkaHostEndpoint} (attempt {attempt + 1}/{maxRetries})");
                TestContext.WriteLine($"🔍 [Kafka Health] Attempt {attempt + 1}/{maxRetries}: Checking endpoint {kafkaHostEndpoint}");
                
                var config = new AdminClientConfig
                {
                    BootstrapServers = kafkaHostEndpoint,
                    SocketTimeoutMs = 5000
                };
                
                using var adminClient = new AdminClientBuilder(config).Build();
                
                // Try to get cluster metadata - this validates Kafka is ready
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
                
                if (metadata != null && metadata.Brokers.Count > 0)
                {
                    LogDebug($"[KAFKA-HEALTH] Kafka is healthy: {metadata.Brokers.Count} broker(s) available");
                    TestContext.WriteLine($"✅ [Kafka Health] Kafka is READY: {metadata.Brokers.Count} broker(s) available");
                    return true;
                }
                
                LogDebug($"[KAFKA-HEALTH] Kafka metadata available but no brokers found (attempt {attempt + 1}/{maxRetries})");
                TestContext.WriteLine($"⚠️ [Kafka Health] No brokers found (attempt {attempt + 1}/{maxRetries})");
            }
            catch (Exception ex)
            {
                LogDebug($"[KAFKA-HEALTH] Kafka connection error (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                TestContext.WriteLine($"⚠️ [Kafka Health] Connection error (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
            }
            
            // Retry after delay (except on last attempt)
            if (attempt < maxRetries - 1)
            {
                LogDebug($"[KAFKA-HEALTH] Retrying Kafka health check in {retryDelaySeconds}s...");
                TestContext.WriteLine($"⏳ [Kafka Health] Retrying in {retryDelaySeconds}s...");
                await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
            }
        }
        
        LogDebug($"[KAFKA-HEALTH] Kafka health check failed after {maxRetries} attempts");
        TestContext.WriteLine($"❌ [Kafka Health] Failed after {maxRetries} attempts");
        return false;
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
    /// Helper method to discover endpoints with retry logic.
    /// Implements 4 retries with 5-second delays between attempts.
    /// </summary>
    private static async Task<string?> DiscoverWithRetryAsync(
        Func<Task<string?>> discoveryFunc,
        string endpointName,
        Stopwatch stopwatch,
        int iteration,
        Func<string?, Task>? onSuccessCallback = null)
    {
        const int maxRetries = 4;
        const int retryDelaySeconds = 5;
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var result = await discoveryFunc();
                if (result != null)
                {
                    LogDebug($"[DISCOVERY] {endpointName} discovered: {result} (attempt {attempt + 1}/{maxRetries})");
                    TestContext.WriteLine($"✅ {endpointName} discovered: {result} (after {stopwatch.Elapsed.TotalSeconds:F1}s, attempt {attempt + 1}/{maxRetries})");
                    
                    // Execute success callback if provided
                    if (onSuccessCallback != null)
                    {
                        await onSuccessCallback(result);
                    }
                    
                    return result;
                }
                
                // Result was null, retry after delay (except on last attempt)
                if (attempt < maxRetries - 1)
                {
                    LogDebug($"[DISCOVERY] {endpointName} not found (attempt {attempt + 1}/{maxRetries}), retrying in {retryDelaySeconds}s...");
                    if (iteration % 5 == 1) // Log every 5th iteration to reduce noise
                    {
                        TestContext.WriteLine($"⏳ {endpointName} not found yet (attempt {attempt + 1}/{maxRetries}), retrying in {retryDelaySeconds}s...");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
            }
            catch (Exception ex)
            {
                LogDebug($"[DISCOVERY] Failed to get {endpointName} (attempt {attempt + 1}/{maxRetries}): {ex.Message}");
                
                // Only retry if not the last attempt
                if (attempt < maxRetries - 1)
                {
                    if (iteration % 5 == 1) // Log every 5th iteration to reduce noise
                    {
                        TestContext.WriteLine($"⚠️ {endpointName} discovery error (attempt {attempt + 1}/{maxRetries}): {ex.Message}, retrying in {retryDelaySeconds}s...");
                    }
                    await Task.Delay(TimeSpan.FromSeconds(retryDelaySeconds));
                }
                else
                {
                    // Log final failure
                    if (iteration % 10 == 1)
                    {
                        TestContext.WriteLine($"❌ {endpointName} discovery failed after {maxRetries} attempts: {ex.Message}");
                    }
                }
            }
        }
        
        // All retries exhausted
        LogDebug($"[DISCOVERY] {endpointName} not discovered after {maxRetries} attempts");
        return null;
    }
    
    /// <summary>
    /// Try to discover all infrastructure endpoints with logging and retry logic.
    /// Implements 4 retries with 5-second delays between attempts for robust discovery.
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
            
            // Kafka discovery (always required) - with retry logic
            if (flinkIp == null)
            {
                flinkIp = await DiscoverWithRetryAsync(
                    async () => await DockerInfrastructure.GetKafkaContainerIpAsync(),
                    "Kafka container IP",
                    stopwatch,
                    iteration,
                    async (result) => {
                        if (result != null)
                        {
                            // Log docker ps after successful Kafka IP discovery
                            await DockerInfrastructure.LogDockerPsAsync("After Kafka IP Discovery", _debugLogWriter);
                        }
                    });
            }
            
            if (hostEndpoint == null)
            {
                hostEndpoint = await DiscoverWithRetryAsync(
                    async () => await DockerInfrastructure.GetKafkaHostEndpointAsync(),
                    "Kafka host endpoint",
                    stopwatch,
                    iteration);
            }
            
            // Temporal discovery (always required) - with retry logic
            if (temporal == null)
            {
                temporal = await DiscoverWithRetryAsync(
                    async () => await DockerInfrastructure.GetTemporalHostEndpointAsync(),
                    "Temporal endpoint",
                    stopwatch,
                    iteration);
            }
            
            // LearningCourse infrastructure discovery (only when LEARNINGCOURSE=true) - with retry logic
            if (isLearningCourse)
            {
                if (redis == null)
                {
                    redis = await DiscoverWithRetryAsync(
                        async () => await DockerInfrastructure.GetRedisHostEndpointAsync(),
                        "Redis endpoint",
                        stopwatch,
                        iteration);
                }
                
                if (prometheus == null)
                {
                    prometheus = await DiscoverWithRetryAsync(
                        async () => await DockerInfrastructure.GetPrometheusHostEndpointAsync(),
                        "Prometheus endpoint",
                        stopwatch,
                        iteration);
                }
                
                if (grafana == null)
                {
                    grafana = await DiscoverWithRetryAsync(
                        async () => await DockerInfrastructure.GetGrafanaHostEndpointAsync(),
                        "Grafana endpoint",
                        stopwatch,
                        iteration);
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
    public static async Task GlobalTearDownAsync()
    {
        Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] GlobalTearDown called");
        LogDebug("[TEARDOWN] GlobalTearDown called");
        
        try
        {
                // CRITICAL: Only run teardown if setup completed successfully
                // GlobalTearDown might be called during cleanup of failed setup
                if (!_isSetupComplete && _appHostProcess == null)
                {
                    Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Setup never completed, skipping teardown");
                    LogDebug("[TEARDOWN] Setup never completed, skipping teardown");
                    return;
                }
                
                LogDebug("[TEARDOWN] Starting teardown process");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Starting teardown process");
                
                TestContext.WriteLine("🛑 Stopping LocalTesting AppHost...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Stopping LocalTesting AppHost...");
                
                // Kill any orphaned JobGateway processes
                TestContext.WriteLine("🧹 Killing orphaned JobGateway processes...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Killing orphaned JobGateway processes...");
                KillOrphanedJobGatewayProcesses();
            
                // CRITICAL ORDER: Copy logs FIRST before any other teardown operations
                // AppHost can exit during Flink job cancellation, so copy logs immediately
                
                // Step 1: Copy CRITICAL container logs FIRST while everything is still running
                // Copy only Flink logs (TaskManager, JobManager) to avoid temporal-server timeout
                TestContext.WriteLine("📋 Copying critical Flink container logs immediately (before any cleanup)...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Copying critical Flink logs FIRST...");
                LogDebug("[TEARDOWN] CRITICAL: Copying Flink logs BEFORE any teardown operations (AppHost can exit during job cancellation)");
                
                await CopyCriticalFlinkLogsAsync();
                
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Critical Flink logs copied");
                LogDebug("[TEARDOWN] Critical Flink logs copied successfully");
                
                // Step 2: Cancel Flink jobs (AppHost may exit during this operation)
                TestContext.WriteLine("🧹 Cancelling all running Flink jobs...");
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Cancelling Flink jobs...");
                LogDebug("[TEARDOWN] Cancelling Flink jobs...");
                CancelAllFlinkJobsSync();
                Console.WriteLine($"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss.fff}] [TEARDOWN] Flink jobs cancelled");
                LogDebug("[TEARDOWN] Flink jobs cancelled");
                
                // Step 3: Kill AppHost process if it's still running
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
    /// Copy logs from all running Aspire containers using 'docker logs' command.
    /// CRITICAL: This must be called BEFORE stopping/removing containers.
    /// Copies logs from: Kafka, Flink (JobManager, TaskManager, SQL Gateway), Temporal, PostgreSQL, Redis
    /// Uses parallel processing to prevent containers being removed during sequential log capture.
    /// </summary>
    private static async Task CopyAllContainerLogsAsync()
    {
        try
        {
            LogDebug("[LOG-COPY] CopyAllContainerLogs called");
            TestContext.WriteLine("📋 Starting container log collection...");
            
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                LogDebug("[LOG-COPY] Could not find repository root");
                TestContext.WriteLine("⚠️ Could not find repository root, skipping log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            LogDebug($"[LOG-COPY] Test logs directory: {testLogsDir}");
            
            // Get all Aspire-managed containers (including stopped ones with -a flag)
            // CRITICAL: Use 'docker ps -a' to include stopped containers during teardown
            LogDebug("[LOG-COPY] Querying Docker for Aspire containers (including stopped)");
            var getContainersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -a --filter label=com.microsoft.developer.usvc-dev.name --format \"{{.ID}}|{{.Names}}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var getContainersProcess = Process.Start(getContainersPsi);
            if (getContainersProcess == null)
            {
                LogDebug("[LOG-COPY] Failed to start docker ps process");
                TestContext.WriteLine("⚠️ Failed to get container list");
                return;
            }
            
            var containerInfo = getContainersProcess.StandardOutput.ReadToEnd().Trim();
            var containerError = getContainersProcess.StandardError.ReadToEnd().Trim();
            getContainersProcess.WaitForExit();
            
            if (!string.IsNullOrWhiteSpace(containerError))
            {
                LogDebug($"[LOG-COPY] Docker ps stderr: {containerError}");
            }
            
            LogDebug($"[LOG-COPY] Docker ps output: {containerInfo}");
            
            if (string.IsNullOrWhiteSpace(containerInfo))
            {
                LogDebug("[LOG-COPY] No containers found in docker ps output");
                TestContext.WriteLine("⚠️ No containers found to copy logs from");
                return;
            }
            
            var containers = containerInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            LogDebug($"[LOG-COPY] Found {containers.Length} container(s): {string.Join(", ", containers)}");
            TestContext.WriteLine($"📦 Found {containers.Length} container(s) to copy logs from");
            
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            
            // CRITICAL: Capture logs in PARALLEL to prevent containers being removed during sequential processing
            // Sequential processing can take 60+ seconds, causing containers to be removed before we get to them
            var logCaptureTasks = new List<Task<(bool success, string containerName, string logFileName)>>();
            
            foreach (var container in containers)
            {
                var parts = container.Split('|');
                if (parts.Length != 2) continue;
                
                var containerId = parts[0];
                var containerName = parts[1];
                
                // Determine log file name based on container type
                string logFileName;
                if (containerName.Contains("kafka", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Kafka.container.log.{dateStamp}";
                else if (containerName.Contains("flink-jobmanager", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.jobmanager.container.log.{dateStamp}";
                else if (containerName.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.taskmanager.container.log.{dateStamp}";
                else if (containerName.Contains("flink-sql-gateway", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.sql-gateway.container.log.{dateStamp}";
                else if (containerName.Contains("temporal-server", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Temporal.server.container.log.{dateStamp}";
                else if (containerName.Contains("temporal-postgres", StringComparison.OrdinalIgnoreCase) ||
                         containerName.Contains("postgres", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"PostgreSQL.container.log.{dateStamp}";
                else if (containerName.Contains("redis", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Redis.container.log.{dateStamp}";
                else
                    logFileName = $"{containerName}.container.log.{dateStamp}";
                
                var logFilePath = Path.Combine(testLogsDir, logFileName);
                
                // Capture logs in parallel to prevent containers being removed during processing
                var captureTask = Task.Run(async () =>
                {
                    try
                    {
                        LogDebug($"[LOG-COPY] Processing container {containerName} (ID: {containerId}) -> {logFileName}");
                        
                        // Use docker logs to capture stdout and stderr
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
                            LogDebug($"[LOG-COPY] Failed to start docker logs process for {containerName}");
                            return (false, containerName, logFileName);
                        }
                        
                        // Read output with timeout
                        string stdout = "";
                        string stderr = "";
                        
                        var readTask = Task.Run(() =>
                        {
                            stdout = logsProcess.StandardOutput.ReadToEnd();
                            stderr = logsProcess.StandardError.ReadToEnd();
                        });
                        
                        // Temporal server has extremely large logs - use 180 second timeout
                        // Other containers use 60 second timeout
                        var timeout = containerName.Contains("temporal-server", StringComparison.OrdinalIgnoreCase)
                            ? TimeSpan.FromSeconds(180)
                            : TimeSpan.FromSeconds(60);
                        
                        var completedTask = await Task.WhenAny(readTask, Task.Delay(timeout));
                        if (completedTask != readTask)
                        {
                            LogDebug($"[LOG-COPY] Timeout ({timeout.TotalSeconds}s) reading logs from {containerName}");
                            try { logsProcess.Kill(); } catch { }
                            return (false, containerName, logFileName);
                        }
                        
                        LogDebug($"[LOG-COPY] docker logs collected: stdout={stdout?.Length ?? 0}, stderr={stderr?.Length ?? 0}");
                        
                        // Combine stdout and stderr
                        var allLogs = stdout;
                        if (!string.IsNullOrWhiteSpace(stderr))
                        {
                            allLogs += "\n\n=== STDERR ===\n" + stderr;
                        }
                        
                        if (!string.IsNullOrWhiteSpace(allLogs))
                        {
                            File.WriteAllText(logFilePath, allLogs);
                            var fileInfo = new FileInfo(logFilePath);
                            LogDebug($"[LOG-COPY] Wrote {fileInfo.Length} bytes to {logFilePath}");
                            return (true, containerName, logFileName);
                        }
                        
                        LogDebug($"[LOG-COPY] No logs content for {containerName}");
                        return (false, containerName, logFileName);
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"[LOG-COPY] Error capturing logs from {containerName}: {ex.Message}");
                        return (false, containerName, logFileName);
                    }
                });
                
                logCaptureTasks.Add(captureTask);
            }
            
            // Wait for all log captures to complete
            var results = await Task.WhenAll(logCaptureTasks);
            
            var copiedCount = 0;
            var failedCount = 0;
            foreach (var (success, containerName, logFileName) in results)
            {
                if (success)
                {
                    var fileInfo = new FileInfo(Path.Combine(testLogsDir, logFileName));
                    TestContext.WriteLine($"   ✅ Copied {containerName} logs ({fileInfo.Length} bytes) to {logFileName}");
                    copiedCount++;
                }
                else
                {
                    TestContext.WriteLine($"   ⚠️ Failed to copy logs from {containerName}");
                    failedCount++;
                }
            }
            
            LogDebug($"[LOG-COPY] Summary: {copiedCount} successful, {failedCount} failed out of {containers.Length} total");
            TestContext.WriteLine($"✅ Copied logs from {copiedCount}/{containers.Length} container(s) ({failedCount} failed)");
        }
        catch (Exception ex)
        {
            LogDebug($"[LOG-COPY] Exception in CopyAllContainerLogs: {ex}");
            TestContext.WriteLine($"⚠️ Error copying container logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Write network debug information to log file for troubleshooting container connectivity.
    /// Captures Docker container state, network configuration, and port mappings.
    /// <summary>
    /// Copy logs from CRITICAL Flink containers only (JobManager, TaskManager, SQL Gateway).
    /// Also copies kafka-exporter logs for debugging JMX metrics issues.
    /// This is faster than copying all container logs and avoids temporal-server timeout.
    /// CRITICAL: This must be called BEFORE stopping/removing containers.
    /// </summary>
    private static async Task CopyCriticalFlinkLogsAsync()
    {
        try
        {
            LogDebug("[FLINK-LOG-COPY] CopyCriticalFlinkLogsAsync called");
            TestContext.WriteLine("📋 Starting critical Flink container log collection...");
            
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                LogDebug("[FLINK-LOG-COPY] Could not find repository root");
                TestContext.WriteLine("⚠️ Could not find repository root, skipping log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            LogDebug($"[FLINK-LOG-COPY] Test logs directory: {testLogsDir}");
            
            // Get Flink containers and kafka-exporter (for JMX metrics debugging)
            LogDebug("[FLINK-LOG-COPY] Querying Docker for Flink and kafka-exporter containers");
            var getContainersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -a --filter label=com.microsoft.developer.usvc-dev.name --format \"{{.ID}}|{{.Names}}\" | findstr /i \"flink kafka-exporter\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var getContainersProcess = Process.Start(getContainersPsi);
            if (getContainersProcess == null)
            {
                LogDebug("[FLINK-LOG-COPY] Failed to start docker ps process");
                TestContext.WriteLine("⚠️ Failed to get Flink container list");
                return;
            }
            
            var containerInfo = getContainersProcess.StandardOutput.ReadToEnd().Trim();
            var containerError = getContainersProcess.StandardError.ReadToEnd().Trim();
            getContainersProcess.WaitForExit();
            
            if (!string.IsNullOrWhiteSpace(containerError))
            {
                LogDebug($"[FLINK-LOG-COPY] Docker ps stderr: {containerError}");
            }
            
            LogDebug($"[FLINK-LOG-COPY] Docker ps output: {containerInfo}");
            
            if (string.IsNullOrWhiteSpace(containerInfo))
            {
                LogDebug("[FLINK-LOG-COPY] No Flink containers found");
                TestContext.WriteLine("⚠️ No Flink containers found to copy logs from");
                return;
            }
            
            var containers = containerInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            LogDebug($"[FLINK-LOG-COPY] Found {containers.Length} critical container(s): {string.Join(", ", containers)}");
            TestContext.WriteLine($"📦 Found {containers.Length} critical container(s) to copy logs from (Flink + kafka-exporter)");
            
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            
            // Capture logs in PARALLEL to prevent containers being removed during processing
            var logCaptureTasks = new List<Task<(bool success, string containerName, string logFileName)>>();
            
            foreach (var container in containers)
            {
                var parts = container.Split('|');
                if (parts.Length != 2) continue;
                
                var containerId = parts[0];
                var containerName = parts[1];
                
                // Determine log file name based on container type
                string logFileName;
                if (containerName.Contains("flink-jobmanager", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.jobmanager.container.log.{dateStamp}";
                else if (containerName.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.taskmanager.container.log.{dateStamp}";
                else if (containerName.Contains("flink-sql-gateway", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Flink.sql-gateway.container.log.{dateStamp}";
                else if (containerName.Contains("kafka-exporter", StringComparison.OrdinalIgnoreCase))
                    logFileName = $"Kafka.exporter.container.log.{dateStamp}";
                else
                    continue; // Skip non-critical containers
                
                var logFilePath = Path.Combine(testLogsDir, logFileName);
                
                // Capture logs in parallel
                var captureTask = Task.Run(async () =>
                {
                    try
                    {
                        LogDebug($"[FLINK-LOG-COPY] Processing container {containerName} (ID: {containerId}) -> {logFileName}");
                        
                        // Use docker logs to capture stdout and stderr
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
                            LogDebug($"[FLINK-LOG-COPY] Failed to start docker logs process for {containerName}");
                            return (false, containerName, logFileName);
                        }
                        
                        // Read output with timeout (Flink logs are much smaller than Temporal)
                        string stdout = "";
                        string stderr = "";
                        
                        var readTask = Task.Run(() =>
                        {
                            stdout = logsProcess.StandardOutput.ReadToEnd();
                            stderr = logsProcess.StandardError.ReadToEnd();
                        });
                        
                        // 10 second timeout for Flink logs (they're small)
                        var timeout = TimeSpan.FromSeconds(10);
                        
                        var completedTask = await Task.WhenAny(readTask, Task.Delay(timeout));
                        if (completedTask != readTask)
                        {
                            LogDebug($"[FLINK-LOG-COPY] Timeout ({timeout.TotalSeconds}s) reading logs from {containerName}");
                            try { logsProcess.Kill(); } catch { }
                            return (false, containerName, logFileName);
                        }
                        
                        LogDebug($"[FLINK-LOG-COPY] docker logs collected: stdout={stdout?.Length ?? 0}, stderr={stderr?.Length ?? 0}");
                        
                        // Combine stdout and stderr
                        var allLogs = stdout;
                        if (!string.IsNullOrWhiteSpace(stderr))
                        {
                            allLogs += "\n\n=== STDERR ===\n" + stderr;
                        }
                        
                        if (!string.IsNullOrWhiteSpace(allLogs))
                        {
                            File.WriteAllText(logFilePath, allLogs);
                            var fileInfo = new FileInfo(logFilePath);
                            LogDebug($"[FLINK-LOG-COPY] Wrote {fileInfo.Length} bytes to {logFilePath}");
                            return (true, containerName, logFileName);
                        }
                        
                        LogDebug($"[FLINK-LOG-COPY] No logs content for {containerName}");
                        return (false, containerName, logFileName);
                    }
                    catch (Exception ex)
                    {
                        LogDebug($"[FLINK-LOG-COPY] Error capturing logs from {containerName}: {ex.Message}");
                        return (false, containerName, logFileName);
                    }
                });
                
                logCaptureTasks.Add(captureTask);
            }
            
            // Wait for all log captures to complete
            var results = await Task.WhenAll(logCaptureTasks);
            
            var copiedCount = 0;
            var failedCount = 0;
            foreach (var (success, containerName, logFileName) in results)
            {
                if (success)
                {
                    var fileInfo = new FileInfo(Path.Combine(testLogsDir, logFileName));
                    TestContext.WriteLine($"   ✅ Copied {containerName} logs ({fileInfo.Length} bytes) to {logFileName}");
                    copiedCount++;
                }
                else
                {
                    TestContext.WriteLine($"   ⚠️ Failed to copy logs from {containerName}");
                    failedCount++;
                }
            }
            
            LogDebug($"[FLINK-LOG-COPY] Summary: {copiedCount} successful, {failedCount} failed out of {containers.Length} total");
            TestContext.WriteLine($"✅ Copied critical logs from {copiedCount}/{containers.Length} container(s) ({failedCount} failed)");
        }
        catch (Exception ex)
        {
            LogDebug($"[FLINK-LOG-COPY] Exception in CopyCriticalFlinkLogsAsync: {ex}");
            TestContext.WriteLine($"⚠️ Error copying Flink container logs: {ex.Message}");
        }
    }
    
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
        
        // Set FLINK_GATEWAY_URL for exercises that submit Flink jobs directly (always use localhost:8080)
        psi.Environment["FLINK_GATEWAY_URL"] = "http://localhost:8080";
        
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