using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.Testing;
using LocalTesting.FlinkSqlAppHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Assembly-level test infrastructure setup for LocalTesting integration tests.
/// Initializes infrastructure ONCE for all tests to dramatically reduce startup overhead.
/// Infrastructure includes: Docker, Kafka, Flink JobManager, Flink TaskManager, and Gateway.
/// </summary>
[SetUpFixture]
public class GlobalTestInfrastructure
{

    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private static string? _previousLearningCourseMode;
    private static string? _previousIsTestingMode;

    public static DistributedApplication? AppHost
    {
        get; private set;
    }
    public static string? KafkaConnectionString
    {
        get; private set;
    }
    public static string? KafkaConnectionStringFromConfig
    {
        get; private set;
    }
    public static string? KafkaContainerIpForFlink
    {
        get; private set;
    } // Kafka IP for Flink jobs (e.g., "172.17.0.2:9093")
    public static string? TemporalEndpoint
    {
        get; private set;
    } // Discovered Temporal endpoint with dynamic port

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        Console.WriteLine("🌍 ========================================");
        Console.WriteLine("🌍 GLOBAL TEST INFRASTRUCTURE SETUP START");
        Console.WriteLine("🌍 ========================================");
        Console.WriteLine($"🌍 This infrastructure will be shared across ALL test classes");
        Console.WriteLine($"🌍 Estimated startup time: 3-4 minutes (one-time cost)");

        var sw = Stopwatch.StartNew();

        try
        {
            // Save previous environment variable values for restoration
            _previousLearningCourseMode = Environment.GetEnvironmentVariable("LEARNINGCOURSE");
            _previousIsTestingMode = Environment.GetEnvironmentVariable("IS_TESTING");

            if (string.Equals(_previousLearningCourseMode, "true", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("⚠️ LEARNINGCOURSE=true detected - forcing Aspire integration-test profile.");
            }
            Environment.SetEnvironmentVariable("LEARNINGCOURSE", "false");

            // Set IS_TESTING environment variable to indicate testing mode
            Environment.SetEnvironmentVariable("IS_TESTING", "true");
            Console.WriteLine("✅ IS_TESTING=true set for test infrastructure");

            // Clean up test-logs directory from previous test runs
            CleanupTestLogsDirectory();

            // Capture initial network state before infrastructure starts
            await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync("0-before-setup");

            // Configure JAR path for Gateway
            ConfigureGatewayJarPath();

            // Validate Docker environment
            await ValidateDockerEnvironmentAsync();

            // Build and start Aspire application
            Console.WriteLine("🔧 Building Aspire ApplicationHost...");
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>();
            Console.WriteLine("🔧 Building application...");
            var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
            Console.WriteLine("🔧 Starting application...");
            await app.StartAsync().WaitAsync(DefaultTimeout);

            AppHost = app;
            Console.WriteLine("✅ Aspire ApplicationHost started");

            // Smart polling: Wait for containers to be created and port mappings to be established
            // Aspire creates containers asynchronously - use smart polling instead of fixed delays
            Console.WriteLine("⏳ Waiting for Docker/Podman containers to be created and ports to be mapped...");
            Console.WriteLine("🔍 Using optimized polling (check every 2s, max 20s)...");

            bool containersDetected = false;
            for (int attempt = 1; attempt <= 10; attempt++) // 10 attempts × 3s = 30s max
            {
                await Task.Delay(TimeSpan.FromSeconds(3));

                var containers = await RunDockerCommandAsync("ps --filter name=kafka --format \"{{.Names}}\"");
                if (!string.IsNullOrWhiteSpace(containers))
                {
                    Console.WriteLine($"✅ Kafka container detected after {attempt * 3}s");
                    containersDetected = true;

                    // Show all containers for diagnostics
                    var allContainers = await RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
                    Console.WriteLine($"🐳 All containers:\n{allContainers}");
                    break;
                }
                Console.WriteLine($"⏳ Still waiting for containers... ({attempt * 3}s elapsed)");
            }

            if (!containersDetected)
            {
                Console.WriteLine("⚠️ Containers not detected within 30s, proceeding anyway...");
                var allContainers = await RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
                Console.WriteLine($"🐳 Current containers:\n{allContainers}");
            }

            // Capture network state after containers are detected
            await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync("1-after-container-detection");

            // CRITICAL FIX: Discover Kafka container IP for Flink job configurations
            // Docker default bridge doesn't support DNS, so we need to use the actual container IP
            Console.WriteLine("🔧 Discovering Kafka container IP for Flink jobs...");
            var kafkaContainerIp = await GetKafkaContainerIpAsync();
            Console.WriteLine($"✅ Kafka container IP: {kafkaContainerIp}");

            // Store for use in tests (replaces hostname-based connection)
            KafkaContainerIpForFlink = kafkaContainerIp;

            // CRITICAL: Use Aspire's configuration system to get Kafka connection string
            // This is the proper Aspire pattern instead of hardcoding or Docker inspection
            Console.WriteLine("🔍 Getting Kafka connection string from Aspire configuration...");
            KafkaConnectionStringFromConfig = app.Services.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()
                .GetConnectionString("kafka");

            // Also discover from Docker for comparison/debugging
            var discoveredKafkaEndpoint = await GetKafkaEndpointAsync();

            // Use config value as primary, fallback to discovered if not available
            KafkaConnectionString = !string.IsNullOrEmpty(KafkaConnectionStringFromConfig)
                ? KafkaConnectionStringFromConfig
                : discoveredKafkaEndpoint;

            Console.WriteLine($"✅ Kafka connection strings:");
            Console.WriteLine($"   📡 From Aspire config: {KafkaConnectionStringFromConfig ?? "(not set)"}");
            Console.WriteLine($"   📡 From Docker discovery: {discoveredKafkaEndpoint}");
            Console.WriteLine($"   📡 Using for tests: {KafkaConnectionString}");
            Console.WriteLine($"   ℹ️  This address will be used by both test producers/consumers AND Flink jobs");

            // Get Flink endpoint and wait for readiness with retry mechanism
            var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
            Console.WriteLine($"🔍 Flink JobManager endpoint: {flinkEndpoint}");
            await RetryWaitForReadyAsync("Flink", () => LocalTestingTestBase.WaitForFlinkReadyAsync($"{flinkEndpoint}v1/overview", DefaultTimeout, default), 3, TimeSpan.FromSeconds(5));
            Console.WriteLine("✅ Flink JobManager and TaskManager are ready");

            // Wait for Gateway with retry mechanism
            Console.WriteLine("⏳ Waiting for Gateway resource to start...");
            await RetryHealthCheckAsync("flink-job-gateway", app, 3, TimeSpan.FromSeconds(5));
            Console.WriteLine("✅ Gateway resource reported healthy");

            var gatewayEndpoint = await GetGatewayEndpointAsync();
            Console.WriteLine($"🔍 Gateway endpoint: {gatewayEndpoint}");

            // Set environment variable for FlinkJobGatewayConfiguration to use discovered endpoint
            // Note: Keep trailing slash for proper URL combination in HttpClient
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", gatewayEndpoint);
            Console.WriteLine($"✅ FLINK_JOB_GATEWAY_URL set to: {gatewayEndpoint}");

            await RetryWaitForReadyAsync("Gateway", () => LocalTestingTestBase.WaitForGatewayReadyAsync($"{gatewayEndpoint}api/v1/health", DefaultTimeout, default), 3, TimeSpan.FromSeconds(5));
            Console.WriteLine("✅ Gateway is ready");

            // Wait for Temporal server resource with retry mechanism
            Console.WriteLine("⏳ Waiting for Temporal server resource to start...");
            await RetryHealthCheckAsync("temporal-server", app, 3, TimeSpan.FromSeconds(5));
            Console.WriteLine("✅ Temporal server resource reported healthy");

            // Then wait for Temporal to be fully initialized
            Console.WriteLine("⏳ Waiting for Temporal server to be fully ready...");
            Console.WriteLine("   ℹ️ Temporal with PostgreSQL requires initialization time...");

            // Give Temporal time to complete schema setup
            await Task.Delay(TimeSpan.FromSeconds(5)); // Optimized: Reduced from 10s to 5s

            // Discover actual Temporal endpoint from Docker (Aspire uses dynamic ports in testing)
            TemporalEndpoint = await GetTemporalEndpointAsync();
            Console.WriteLine($"🔍 Temporal endpoint: {TemporalEndpoint}");
            await RetryWaitForReadyAsync("Temporal", () => LocalTestingTestBase.WaitForTemporalReadyAsync(TemporalEndpoint, DefaultTimeout, default), 3, TimeSpan.FromSeconds(5));
            Console.WriteLine("✅ Temporal server is fully ready");

            // Log TaskManager status for debugging
            await LogTaskManagerStatusAsync();

            // Capture final network state after all infrastructure is ready
            await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync("2-infrastructure-ready");

            Console.WriteLine($"🌍 ========================================");
            Console.WriteLine($"🌍 GLOBAL INFRASTRUCTURE READY in {sw.Elapsed.TotalSeconds:F1}s");
            Console.WriteLine($"🌍 ========================================");
            Console.WriteLine($"🌍 Kafka connection string: {KafkaConnectionString}");
            Console.WriteLine($"🌍 Infrastructure will remain active for all tests");
            Console.WriteLine($"🌍 Tests can now run in parallel with shared infrastructure");

            // Clean up old network diagnostic logs
            NetworkDiagnostics.CleanupOldLogs();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Global infrastructure setup failed: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");

            // Capture network diagnostics on failure
            await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync("error-setup-failed");

            // Capture container diagnostics and include in exception
            var diagnostics = await GetContainerDiagnosticsAsync();

            throw new InvalidOperationException(
                $"Global infrastructure setup failed: {ex.Message}\n\n" +
                $"Container Diagnostics:\n{diagnostics}",
                ex);
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        Console.WriteLine("🌍 TEARDOWN: Cleaning up test infrastructure...");

        // CRITICAL: Capture container logs BEFORE stopping/disposing AppHost
        // Once AppHost.StopAsync() is called, containers are immediately stopped and may be removed
        await CaptureAllContainerLogsAsync();

        // Capture network state before teardown
        await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync("3-before-teardown");

        if (AppHost != null)
        {
            try
            {
                // Aggressive cleanup with minimal timeout
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

                try
                {
                    await AppHost.StopAsync(cts.Token);
                    await AppHost.DisposeAsync();
                    Console.WriteLine("✅ Infrastructure cleaned up");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("✅ Cleanup timed out - runtime will handle remaining resources");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"✅ Cleanup completed with: {ex.Message}");
            }
        }

        // Restore previous environment variable values
        Environment.SetEnvironmentVariable("LEARNINGCOURSE", _previousLearningCourseMode);
        Environment.SetEnvironmentVariable("IS_TESTING", _previousIsTestingMode);
        Console.WriteLine("✅ Environment variables restored");
    }

    /// <summary>
    /// Clean up the test-logs directory at the start of test execution.
    /// Ensures old logs from previous test runs don't accumulate.
    /// </summary>
    private static void CleanupTestLogsDirectory()
    {
        try
        {
            Console.WriteLine("🧹 Cleaning up test-logs directory...");

            var repoRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            if (repoRoot == null)
            {
                Console.WriteLine("⚠️ Cannot find repository root, skipping test-logs cleanup");
                return;
            }

            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");

            if (Directory.Exists(testLogsDir))
            {
                try
                {
                    // Delete all files and subdirectories
                    Directory.Delete(testLogsDir, recursive: true);
                    Console.WriteLine($"✅ Deleted existing test-logs directory");
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"⚠️ Could not delete some files (may be locked): {ex.Message}");
                    // Continue anyway - we'll try to clean up what we can
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"⚠️ Access denied when deleting test-logs: {ex.Message}");
                    // Continue anyway
                }
            }

            // Recreate the directory for this test run
            Directory.CreateDirectory(testLogsDir);
            Console.WriteLine($"✅ Created fresh test-logs directory: {testLogsDir}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error during test-logs cleanup: {ex.Message}");
            // Don't fail the test run if cleanup fails
        }
    }

    /// <summary>
    /// Capture logs from Flink containers only before teardown.
    /// Only captures logs from JobManager and TaskManager to improve performance.
    /// Skips containers with no log output.
    /// </summary>
    private static async Task CaptureAllContainerLogsAsync()
    {
        try
        {
            Console.WriteLine("📋 Capturing Flink container logs before teardown...");
            Console.WriteLine("   ℹ️ Only capturing JobManager and TaskManager logs for performance");

            var repoRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            if (repoRoot == null)
            {
                Console.WriteLine("⚠️ Cannot find repository root, skipping log capture");
                return;
            }

            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd");

            // PERFORMANCE OPTIMIZATION: Only capture logs from Flink JobManager and TaskManager
            // Skip Kafka, Temporal, Redis, Gateway, and other containers to reduce teardown time
            await CaptureContainerLogAsync("flink-taskmanager", Path.Combine(testLogsDir, $"Flink.TaskManager.container.log.{timestamp}"));
            await CaptureContainerLogAsync("flink-jobmanager", Path.Combine(testLogsDir, $"Flink.JobManager.container.log.{timestamp}"));

            Console.WriteLine("✅ Flink container logs captured");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error capturing container logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Capture logs from a specific container with optimized log checking.
    /// Skips containers that have no log output to improve performance.
    /// </summary>
    private static async Task CaptureContainerLogAsync(string containerNameFilter, string outputPath)
    {
        try
        {
            // Find container by name filter (including stopped containers)
            // Use --filter to match containers whose name contains the filter string
            var containerList = await RunDockerCommandAsync($"ps -a --filter \"name={containerNameFilter}\" --format \"{{{{.Names}}}}\"");
            var containers = containerList.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrEmpty(c))
                .ToList();

            if (containers.Count == 0)
            {
                Console.WriteLine($"⏭️ Skipping: No container matching '{containerNameFilter}' found");
                return;
            }

            // Take the first matching container
            var containerName = containers[0];
            Console.WriteLine($"🔍 Processing container: {containerName}");

            // PERFORMANCE OPTIMIZATION: Check if container has logs before attempting to read them
            // Use --tail 1 to quickly check if there's any output
            var logCheck = await RunDockerCommandAsync($"logs {containerName} --tail 1 2>&1");

            // Check if logs contain error about container not found
            if (logCheck.Contains("no container with name or ID", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"⏭️ Skipping: Container {containerName} was already removed");
                return;
            }

            // If log check is empty, skip full log capture
            if (string.IsNullOrWhiteSpace(logCheck))
            {
                Console.WriteLine($"⏭️ Skipping: Container {containerName} has no log output");
                return;
            }

            // Container has logs, proceed with full capture
            var logs = await RunDockerCommandAsync($"logs {containerName} 2>&1");

            if (!string.IsNullOrWhiteSpace(logs))
            {
                await File.WriteAllTextAsync(outputPath, logs);
                var lineCount = logs.Split('\n').Length;
                Console.WriteLine($"✅ Captured {lineCount} lines of logs for {containerName} → {Path.GetFileName(outputPath)}");
            }
            else
            {
                Console.WriteLine($"⏭️ Skipping: No logs available for {containerName}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Error capturing logs for {containerNameFilter}: {ex.Message}");
        }
    }

    private static void ConfigureGatewayJarPath()
    {
        var currentDir = Environment.CurrentDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null)
        {
            Console.WriteLine("⚠️ Could not find repository root - Gateway may need to build JAR at runtime");
            return;
        }

        // Try Java 17 JAR first (new naming convention)
        var releaseJarPath17 = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner-java17.jar");

        if (File.Exists(releaseJarPath17))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", releaseJarPath17);
            Console.WriteLine($"✅ Configured Gateway JAR path: {releaseJarPath17}");
            return;
        }

        var debugJarPath17 = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner-java17.jar");

        if (File.Exists(debugJarPath17))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", debugJarPath17);
            Console.WriteLine($"✅ Configured Gateway JAR path (Debug): {debugJarPath17}");
            return;
        }

        Console.WriteLine($"⚠️ Gateway JAR not found - will build on demand");
    }

    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
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

    private static async Task ValidateDockerEnvironmentAsync()
    {
        Console.WriteLine("🐳 Validating Docker environment...");

        try
        {
            var dockerInfo = await RunDockerCommandAsync("info --format \"{{.ServerVersion}}\"");
            if (string.IsNullOrWhiteSpace(dockerInfo))
            {
                throw new InvalidOperationException("Docker is not running or not accessible");
            }

            Console.WriteLine($"✅ Docker is available (version: {dockerInfo.Trim()})");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Docker validation failed: {ex.Message}");
            throw;
        }
    }


    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        // Try Docker first, then Podman if Docker fails or returns empty
        var dockerOutput = await TryRunContainerCommandAsync("docker", arguments);
        if (!string.IsNullOrWhiteSpace(dockerOutput))
        {
            return dockerOutput;
        }

        // Fallback to Podman if Docker didn't return results
        var podmanOutput = await TryRunContainerCommandAsync("podman", arguments);
        return podmanOutput ?? string.Empty;
    }

    /// <summary>
    /// Log TaskManager status and recent logs for debugging
    /// </summary>
    private static async Task LogTaskManagerStatusAsync()
    {
        try
        {
            Console.WriteLine("\n╔══════════════════════════════════════════════════════════════");
            Console.WriteLine("║ 🔍 [TaskManager] Checking TaskManager Status");
            Console.WriteLine("╚══════════════════════════════════════════════════════════════");

            // Find TaskManager container (using name filter which matches containers containing the name)
            var containerName = await RunDockerCommandAsync("ps --filter name=flink-taskmanager --format \"{{.Names}}\" | head -1");
            containerName = containerName.Trim();

            if (string.IsNullOrEmpty(containerName))
            {
                Console.WriteLine("❌ No TaskManager container found");
                return;
            }

            Console.WriteLine($"📦 TaskManager container: {containerName}");

            // Get container status
            var status = await RunDockerCommandAsync($"ps --filter \"name={containerName}\" --format \"{{{{.Status}}}}\"");
            Console.WriteLine($"📊 Container status: {status.Trim()}");

            // Get last 100 lines of TaskManager logs
            var logs = await RunDockerCommandAsync($"logs {containerName} --tail 100");

            if (!string.IsNullOrWhiteSpace(logs))
            {
                Console.WriteLine("\n📋 TaskManager Recent Logs (last 100 lines):");
                Console.WriteLine("─────────────────────────────────────────────────────────────");
                Console.WriteLine(logs);
                Console.WriteLine("─────────────────────────────────────────────────────────────");
            }
            else
            {
                Console.WriteLine("⚠️ No TaskManager logs available");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error checking TaskManager status: {ex.Message}");
        }
    }

    private static async Task<string?> TryRunContainerCommandAsync(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine($"❌ Failed to start process: {command} {arguments}");
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var errorOutput = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            Console.WriteLine($"🔍 Command: {command} {arguments}");
            Console.WriteLine($"🔍 Exit code: {process.ExitCode}");
            Console.WriteLine($"🔍 Output length: {output?.Length ?? 0}");
            Console.WriteLine($"🔍 Error output: {(string.IsNullOrWhiteSpace(errorOutput) ? "(none)" : errorOutput)}");

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            // Also return output even if exit code is non-zero, as long as we have output
            // Some docker commands return non-zero but still provide useful output
            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine($"⚠️ Command returned non-zero exit code ({process.ExitCode}) but has output, returning it anyway");
                return output;
            }

            Console.WriteLine($"⚠️ Command failed: exit code {process.ExitCode}, no output");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Exception running command {command} {arguments}: {ex.Message}");
            return null;
        }
    }

    private static async Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            var flinkContainers = await RunDockerCommandAsync("ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"");
            var lines = flinkContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                if (line.Contains("->8081/tcp"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->8081");
                    if (match.Success)
                    {
                        return $"http://localhost:{match.Groups[1].Value}/";
                    }
                }
            }

            throw new InvalidOperationException($"Could not determine Flink JobManager endpoint from Docker ports: {flinkContainers}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Flink JobManager endpoint: {ex.Message}", ex);
        }
    }

    private static Task<string> GetGatewayEndpointAsync()
    {
        // FlinkDotNet.JobGateway is a .NET Aspire project (Projects.FlinkDotNet_JobGateway),
        // NOT a Docker container. It runs directly on the host at a fixed port.
        // Do NOT attempt Docker discovery - always use the configured fixed port.
        var gatewayEndpoint = $"http://localhost:{Ports.GatewayHostPort}/";
        return Task.FromResult(gatewayEndpoint);
    }

    private static async Task<string> GetTemporalEndpointAsync()
    {
        try
        {
            var temporalContainers = await RunDockerCommandAsync("ps --filter \"name=temporal-server\" --format \"{{.Ports}}\"");
            var lines = temporalContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                // Look for port mapping to 7233 (Temporal gRPC port)
                if (line.Contains("->7233/tcp"))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->7233");
                    if (match.Success)
                    {
                        return $"localhost:{match.Groups[1].Value}";
                    }
                }
            }

            throw new InvalidOperationException($"Could not determine Temporal endpoint from Docker ports: {temporalContainers}");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Temporal endpoint: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get the dynamically allocated Kafka endpoint from Aspire.
    /// Aspire DCP assigns random ports during testing, so we must query the actual endpoint.
    /// Kafka container exposes port 9092 internally, which gets mapped to a random host port.
    /// </summary>
    private static async Task<string> GetKafkaEndpointAsync()
    {
        try
        {
            var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Kafka container port mappings: {kafkaContainers.Trim()}");

            return ExtractKafkaEndpointFromPorts(kafkaContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Kafka endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractKafkaEndpointFromPorts(string kafkaContainers)
    {
        var lines = kafkaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 9092 (Kafka's default listener port)
            // Aspire maps container port 9092 to a dynamic host port for external access
            // Format: 127.0.0.1:PORT->9092/tcp or 0.0.0.0:PORT->9092/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->9092");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Kafka port mapping: host {port} -> container 9092");
                return $"localhost:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Kafka endpoint from Docker/Podman ports: {kafkaContainers}");
    }

    /// <summary>
    /// Get Kafka container IP address for use in Flink job configurations
    /// Works with both Docker (bridge network) and Podman (podman network)
    /// </summary>
    private static async Task<string> GetKafkaContainerIpAsync()
    {
        try
        {
            var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka-\" --format \"{{.Names}}\"");
            var kafkaContainer = kafkaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if (string.IsNullOrWhiteSpace(kafkaContainer))
            {
                throw new InvalidOperationException("Kafka container not found");
            }

            // Try Docker bridge network first
            var ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{.NetworkSettings.Networks.bridge.IPAddress}}}}\"");
            var ip = ipAddress.Trim();

            // If bridge network doesn't have IP, try podman network (for Podman runtime)
            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                Console.WriteLine($"🔍 Bridge network IP not found, trying podman network...");
                ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{.NetworkSettings.Networks.podman.IPAddress}}}}\"");
                ip = ipAddress.Trim();
            }

            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                // Fallback: Get the first available network IP
                Console.WriteLine($"🔍 Specific network not found, getting first available IP...");
                ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{.IPAddress}}}}{{{{end}}}}\"");
                ip = ipAddress.Trim();
            }

            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                throw new InvalidOperationException($"Could not determine Kafka container IP from any network. Container: {kafkaContainer}");
            }

            Console.WriteLine($"✅ Kafka container IP discovered: {ip}");

            // Return IP with PLAINTEXT_INTERNAL port (9093)
            return $"{ip}:9093";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Kafka container IP: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Get container diagnostics as a string - detects Docker or Podman and captures container status
    /// </summary>
    private static async Task<string> GetContainerDiagnosticsAsync()
    {
        try
        {
            var diagnostics = new System.Text.StringBuilder();
            diagnostics.AppendLine("\n╔══════════════════════════════════════════════════════════════");
            diagnostics.AppendLine("║ 🔍 [Diagnostics] Container Status at Test Failure");
            diagnostics.AppendLine("╚══════════════════════════════════════════════════════════════");

            // Try Docker first
            var dockerContainers = await TryRunContainerCommandAsync("docker", "ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            if (!string.IsNullOrWhiteSpace(dockerContainers))
            {
                diagnostics.AppendLine("\n🐳 Docker Containers:");
                diagnostics.AppendLine("─────────────────────────────────────────────────────────────");
                diagnostics.AppendLine(dockerContainers);
                diagnostics.AppendLine("─────────────────────────────────────────────────────────────");

                // Add TaskManager logs for debugging
                await AppendTaskManagerLogsAsync(diagnostics);

                // Also write to console for immediate visibility
                Console.WriteLine(diagnostics.ToString());
                return diagnostics.ToString();
            }

            // Try Podman if Docker didn't work
            var podmanContainers = await TryRunContainerCommandAsync("podman", "ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            if (!string.IsNullOrWhiteSpace(podmanContainers))
            {
                diagnostics.AppendLine("\n🦭 Podman Containers:");
                diagnostics.AppendLine("─────────────────────────────────────────────────────────────");
                diagnostics.AppendLine(podmanContainers);
                diagnostics.AppendLine("─────────────────────────────────────────────────────────────");

                // Add TaskManager logs for debugging
                await AppendTaskManagerLogsAsync(diagnostics);

                // Also write to console for immediate visibility
                Console.WriteLine(diagnostics.ToString());
                return diagnostics.ToString();
            }

            diagnostics.AppendLine("⚠️ No container runtime (Docker/Podman) responded to 'ps -a' command");
            diagnostics.AppendLine("   This suggests the container runtime may not be running or accessible");

            // Also write to console for immediate visibility
            Console.WriteLine(diagnostics.ToString());
            return diagnostics.ToString();
        }
        catch (Exception ex)
        {
            var errorMsg = $"⚠️ Failed to get container diagnostics: {ex.Message}";
            Console.WriteLine(errorMsg);
            return errorMsg;
        }
    }

    /// <summary>
    /// Append TaskManager logs to diagnostics output
    /// </summary>
    private static async Task AppendTaskManagerLogsAsync(System.Text.StringBuilder diagnostics)
    {
        try
        {
            var containerName = await RunDockerCommandAsync("ps --filter \"name=flink-taskmanager\" --format \"{{.Names}}\" | head -1");
            containerName = containerName.Trim();

            if (string.IsNullOrEmpty(containerName))
            {
                diagnostics.AppendLine("\n⚠️ No TaskManager container found for log capture");
                return;
            }

            diagnostics.AppendLine($"\n📋 TaskManager ({containerName}) Recent Logs (last 20 lines):");
            diagnostics.AppendLine("─────────────────────────────────────────────────────────────");

            var logs = await RunDockerCommandAsync($"logs {containerName} --tail 20 2>&1");
            if (!string.IsNullOrWhiteSpace(logs))
            {
                diagnostics.AppendLine(logs);
            }
            else
            {
                diagnostics.AppendLine("⚠️ No TaskManager logs available");
            }
            diagnostics.AppendLine("─────────────────────────────────────────────────────────────");
        }
        catch (Exception ex)
        {
            diagnostics.AppendLine($"\n⚠️ Error capturing TaskManager logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Retry health check for a resource with configurable retries and delay
    /// </summary>
    private static async Task RetryHealthCheckAsync(string resourceName, DistributedApplication app, int maxRetries, TimeSpan delayBetweenRetries)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"🔄 Health check attempt {attempt}/{maxRetries} for '{resourceName}'...");

                // Wait for resource to be healthy (with a reasonable timeout per attempt)
                await app.ResourceNotifications
                    .WaitForResourceHealthyAsync(resourceName)
                    .WaitAsync(TimeSpan.FromSeconds(30));

                Console.WriteLine($"✅ '{resourceName}' became healthy on attempt {attempt}");
                return; // Success!
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"⚠️ Attempt {attempt}/{maxRetries} failed for '{resourceName}': {ex.Message}");

                if (attempt < maxRetries)
                {
                    Console.WriteLine($"⏳ Waiting {delayBetweenRetries.TotalSeconds}s before retry...");
                    await Task.Delay(delayBetweenRetries);
                }
            }
        }

        // All retries failed
        throw new InvalidOperationException(
            $"Resource '{resourceName}' failed to become healthy after {maxRetries} attempts. " +
            $"Last error: {lastException?.Message}",
            lastException);
    }

    /// <summary>
    /// Retry a readiness check operation (like WaitForKafkaReadyAsync, WaitForFlinkReadyAsync, etc.)
    /// </summary>
    private static async Task RetryWaitForReadyAsync(string serviceName, Func<Task> readyCheckFunc, int maxRetries, TimeSpan delayBetweenRetries)
    {
        Exception? lastException = null;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"🔄 Readiness check attempt {attempt}/{maxRetries} for '{serviceName}'...");
                await readyCheckFunc();
                Console.WriteLine($"✅ '{serviceName}' became ready on attempt {attempt}");
                return; // Success!
            }
            catch (Exception ex)
            {
                lastException = ex;
                Console.WriteLine($"⚠️ Attempt {attempt}/{maxRetries} failed for '{serviceName}': {ex.Message}");

                if (attempt < maxRetries)
                {
                    Console.WriteLine($"⏳ Waiting {delayBetweenRetries.TotalSeconds}s before retry...");
                    await Task.Delay(delayBetweenRetries);
                }
            }
        }

        // All retries failed
        throw new InvalidOperationException(
            $"Service '{serviceName}' failed to become ready after {maxRetries} attempts. " +
            $"Last error: {lastException?.Message}",
            lastException);
    }
}
