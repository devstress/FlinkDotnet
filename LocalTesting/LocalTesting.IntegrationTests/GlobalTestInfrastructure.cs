using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using LocalTesting.FlinkSqlAppHost;
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
    private static readonly TimeSpan KafkaReadyTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan FlinkReadyTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan GatewayReadyTimeout = TimeSpan.FromSeconds(60);

    public static DistributedApplication? AppHost { get; private set; }
    public static string? KafkaConnectionString { get; private set; }
    public static string KafkaContainerConnectionString => Ports.KafkaContainerBootstrap;

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

            // Wait for containers to be created and port mappings to be established
            // Aspire creates containers asynchronously, need significant wait time for Docker
            Console.WriteLine("⏳ Waiting for Docker containers to be created and ports to be mapped...");
            Console.WriteLine($"⏳ Waiting 5 seconds first...");
            await Task.Delay(TimeSpan.FromSeconds(5));
            
            // Check if containers exist after 5 seconds
            Console.WriteLine("🐳 Checking for containers after 5 seconds...");
            var containers = await RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            Console.WriteLine($"Containers:\n{containers}");
            
            Console.WriteLine($"⏳ Waiting additional 25 seconds for total 30s wait...");
            await Task.Delay(TimeSpan.FromSeconds(25)); // Total 30 seconds
            
            // Check again
            Console.WriteLine("🐳 Checking for containers after 30 seconds...");
            containers = await RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            Console.WriteLine($"Containers:\n{containers}");
            
            // Wait for Kafka
            Console.WriteLine("⏳ Waiting for Kafka resource to be healthy...");
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka")
                .WaitAsync(DefaultTimeout);
            Console.WriteLine("✅ Kafka resource reported healthy");

            // CRITICAL FIX: Always use Docker port discovery, never Aspire's connection string
            // Aspire's GetConnectionStringAsync() returns stale proxy ports when containers persist across runs
            // Docker port discovery gets the actual mapped port from the running container
            Console.WriteLine("🔍 Discovering Kafka external port mapping via Docker...");
            var actualKafkaPort = await DiscoverKafkaExternalPortAsync();
            if (actualKafkaPort == null)
            {
                // Capture container diagnostics to include in exception message
                var diagnostics = await GetContainerDiagnosticsAsync();
                
                throw new InvalidOperationException(
                    "Failed to discover Kafka external port via Docker. " +
                    "Ensure Kafka container is running and port 9093 is mapped. " +
                    "Check with: docker ps --filter \"name=kafka\" and docker port <container-name> 9093\n\n" +
                    $"Container Diagnostics:\n{diagnostics}");
            }

            KafkaConnectionString = $"localhost:{actualKafkaPort}";
            Console.WriteLine($"✅ Using discovered Kafka connection string: {KafkaConnectionString}");
            Console.WriteLine($"   📡 External listener: localhost:9093 (container) -> localhost:{actualKafkaPort} (host)");
            Console.WriteLine($"   📡 Internal listener: kafka:9092 (for Flink containers)");

            // Enhanced Kafka readiness check
            await LocalTestingTestBase.WaitForKafkaReadyAsync(KafkaConnectionString!, KafkaReadyTimeout, default);
            Console.WriteLine("✅ Kafka is fully operational");

            // Get Flink endpoint and wait for readiness
            var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
            Console.WriteLine($"🔍 Flink JobManager endpoint: {flinkEndpoint}");
            await LocalTestingTestBase.WaitForFlinkReadyAsync($"{flinkEndpoint}v1/overview", FlinkReadyTimeout, default);
            Console.WriteLine("✅ Flink JobManager and TaskManager are ready");

            // Wait for Gateway
            Console.WriteLine("⏳ Waiting for Gateway resource to start...");
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("flink-job-gateway")
                .WaitAsync(GatewayReadyTimeout);
            Console.WriteLine("✅ Gateway resource reported healthy");

            var gatewayEndpoint = await GetGatewayEndpointAsync();
            Console.WriteLine($"🔍 Gateway endpoint: {gatewayEndpoint}");
            await LocalTestingTestBase.WaitForGatewayReadyAsync($"{gatewayEndpoint}api/v1/health", GatewayReadyTimeout, default);
            Console.WriteLine("✅ Gateway is ready");

            // Log TaskManager status for debugging
            await LogTaskManagerStatusAsync();
            
            Console.WriteLine($"🌍 ========================================");
            Console.WriteLine($"🌍 GLOBAL INFRASTRUCTURE READY in {sw.Elapsed.TotalSeconds:F1}s");
            Console.WriteLine($"🌍 ========================================");
            Console.WriteLine($"🌍 Kafka container bootstrap: {KafkaContainerConnectionString}");
            Console.WriteLine($"🌍 Kafka external connection: {KafkaConnectionString}");
            Console.WriteLine($"🌍 Infrastructure will remain active for all tests");
            Console.WriteLine($"🌍 Tests can now run in parallel with shared infrastructure");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Global infrastructure setup failed: {ex.Message}");
            Console.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            
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
        Console.WriteLine("🌍 ========================================");
        Console.WriteLine("🌍 GLOBAL TEST INFRASTRUCTURE TEARDOWN START");
        Console.WriteLine("🌍 ========================================");

        if (AppHost != null)
        {
            try
            {
                Console.WriteLine("🔧 Stopping AppHost...");
                await AppHost.StopAsync();
                Console.WriteLine("✅ AppHost stopped");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error stopping AppHost: {ex.Message}");
            }

            try
            {
                Console.WriteLine("🔧 Disposing AppHost...");
                await AppHost.DisposeAsync();
                Console.WriteLine("✅ AppHost disposed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error disposing AppHost: {ex.Message}");
            }
        }

        Console.WriteLine("🌍 ========================================");
        Console.WriteLine("🌍 GLOBAL INFRASTRUCTURE TEARDOWN COMPLETE");
        Console.WriteLine("🌍 ========================================");
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

    private static async Task<string?> DiscoverKafkaExternalPortAsync()
    {
        Console.WriteLine("🔍 Starting Kafka port discovery...");
        
        // Retry a few times in case Docker is still starting containers
        for (int attempt = 1; attempt <= 5; attempt++)  // Increased from 3 to 5 attempts
        {
            try
            {
                var port = await TryDiscoverPortOnAttemptAsync(attempt);
                if (port != null)
                {
                    Console.WriteLine($"✅ Successfully discovered Kafka port: {port}");
                    return port;
                }

                if (attempt < 5)
                {
                    Console.WriteLine($"⚠️ Attempt {attempt}/5 failed, retrying in 3 seconds...");
                    await Task.Delay(3000); // Increased delay from 2 to 3 seconds
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ [Attempt {attempt}/5] Error discovering Kafka external port: {ex.Message}");
                if (attempt < 5)
                {
                    Console.WriteLine($"   Retrying in 3 seconds...");
                    await Task.Delay(3000);
                }
            }
        }
        
        Console.WriteLine("❌ Failed to discover Kafka port after 5 attempts");
        return null;
    }

    private static async Task<string?> TryDiscoverPortOnAttemptAsync(int attempt)
    {
        Console.WriteLine($"🔍 [Attempt {attempt}/3] Looking for Kafka container...");
        
        var kafkaContainer = await FindKafkaContainerAsync(attempt);
        if (kafkaContainer == null)
            return null;

        var portMapping = await GetPortMappingAsync(kafkaContainer, attempt);
        if (portMapping == null)
            return null;

        return ParsePortMapping(portMapping);
    }

    private static async Task<string?> FindKafkaContainerAsync(int attempt)
    {
        var containerName = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Names}}\" --no-trunc");
        if (string.IsNullOrWhiteSpace(containerName))
        {
            Console.WriteLine($"⚠️ [Attempt {attempt}/3] Kafka container not found yet");
            return null;
        }

        var kafkaContainer = containerName.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
        if (string.IsNullOrWhiteSpace(kafkaContainer))
        {
            Console.WriteLine($"⚠️ [Attempt {attempt}/3] Could not parse container name");
            return null;
        }

        Console.WriteLine($"✅ Found Kafka container: {kafkaContainer}");
        return kafkaContainer;
    }

    private static async Task<string?> GetPortMappingAsync(string kafkaContainer, int attempt)
    {
        var portMapping = await RunDockerCommandAsync($"port {kafkaContainer} 9093");
        if (string.IsNullOrWhiteSpace(portMapping))
        {
            Console.WriteLine($"⚠️ [Attempt {attempt}/3] Port 9093 not mapped yet for container {kafkaContainer}");
            return null;
        }

        Console.WriteLine($"🔍 Port mapping: {portMapping.Trim()}");
        return portMapping;
    }

    private static string? ParsePortMapping(string portMapping)
    {
        // Parse port mapping (format: "9093/tcp -> 127.0.0.1:32769")
        var parts = portMapping.Split("->", StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            Console.WriteLine($"⚠️ Could not parse port mapping: {portMapping}");
            return null;
        }

        var hostPort = parts[1].Trim();
        // Extract just the port number (format: "127.0.0.1:32769")
        var portParts = hostPort.Split(':', StringSplitOptions.TrimEntries);
        if (portParts.Length != 2)
        {
            Console.WriteLine($"⚠️ Could not parse host port: {hostPort}");
            return null;
        }

        var discoveredPort = portParts[1].Trim();
        Console.WriteLine($"✅ Discovered external port: {discoveredPort}");
        return discoveredPort;
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
            
            // Find TaskManager container
            var containerName = await RunDockerCommandAsync("ps --filter \"name=flink-taskmanager\" --format \"{{.Names}}\" | head -1");
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
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            return null;
        }
        catch
        {
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

    private static async Task<string> GetGatewayEndpointAsync()
    {
        try
        {
            var gatewayContainers = await RunDockerCommandAsync("ps --filter \"name=gateway\" --format \"{{.Ports}}\"");

            if (!string.IsNullOrWhiteSpace(gatewayContainers))
            {
                var lines = gatewayContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->(\d+)/tcp");
                    if (match.Success)
                    {
                        return $"http://localhost:{match.Groups[1].Value}/";
                    }
                }
            }

            return $"http://localhost:{Ports.GatewayHostPort}/";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Gateway endpoint discovery failed: {ex.Message}, using configured port {Ports.GatewayHostPort}");
            return $"http://localhost:{Ports.GatewayHostPort}/";
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
}