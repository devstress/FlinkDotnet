using System.Diagnostics;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
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
        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine("🌍 GLOBAL TEST INFRASTRUCTURE SETUP START");
        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine($"🌍 This infrastructure will be shared across ALL test classes");
        TestContext.WriteLine($"🌍 Estimated startup time: 3-4 minutes (one-time cost)");

        var sw = Stopwatch.StartNew();

        try
        {
            // Configure JAR path for Gateway
            ConfigureGatewayJarPath();

            // Validate Docker environment
            await ValidateDockerEnvironmentAsync();

            // Build and start Aspire application
            TestContext.WriteLine("🔧 Building Aspire ApplicationHost...");
            var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>();
            var app = await appHost.BuildAsync().WaitAsync(DefaultTimeout);
            await app.StartAsync().WaitAsync(DefaultTimeout);

            AppHost = app;
            TestContext.WriteLine("✅ Aspire ApplicationHost started");

            // Wait for Kafka
            TestContext.WriteLine("⏳ Waiting for Kafka resource to be healthy...");
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka")
                .WaitAsync(DefaultTimeout);
            TestContext.WriteLine("✅ Kafka resource reported healthy");

            // Get Kafka connection string from Aspire
            var aspireKafkaConnectionString = await app.GetConnectionStringAsync("kafka");
            TestContext.WriteLine($"🔗 Aspire Kafka connection string: {aspireKafkaConnectionString}");
            
            // Override with hardcoded localhost:9093 for external access
            // Aspire's connection string may not respect KAFKA_CFG_ADVERTISED_LISTENERS
            // We configured Kafka to advertise PLAINTEXT_HOST://localhost:9093 for external clients
            KafkaConnectionString = "localhost:9093";
            TestContext.WriteLine($"🔗 Using Kafka connection string: {KafkaConnectionString} (external listener)");

            // Enhanced Kafka readiness check
            await LocalTestingTestBase.WaitForKafkaReadyAsync(KafkaConnectionString!, KafkaReadyTimeout, default);
            TestContext.WriteLine("✅ Kafka is fully operational");

            // Get Flink endpoint and wait for readiness
            var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
            TestContext.WriteLine($"🔍 Flink JobManager endpoint: {flinkEndpoint}");
            await LocalTestingTestBase.WaitForFlinkReadyAsync($"{flinkEndpoint}v1/overview", FlinkReadyTimeout, default);
            TestContext.WriteLine("✅ Flink JobManager and TaskManager are ready");

            // Wait for Gateway
            TestContext.WriteLine("⏳ Waiting for Gateway resource to start...");
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("flink-job-gateway")
                .WaitAsync(GatewayReadyTimeout);
            TestContext.WriteLine("✅ Gateway resource reported healthy");

            var gatewayEndpoint = await GetGatewayEndpointAsync();
            TestContext.WriteLine($"🔍 Gateway endpoint: {gatewayEndpoint}");
            await LocalTestingTestBase.WaitForGatewayReadyAsync($"{gatewayEndpoint}api/v1/health", GatewayReadyTimeout, default);
            TestContext.WriteLine("✅ Gateway is ready");

            TestContext.WriteLine($"🌍 ========================================");
            TestContext.WriteLine($"🌍 GLOBAL INFRASTRUCTURE READY in {sw.Elapsed.TotalSeconds:F1}s");
            TestContext.WriteLine($"🌍 ========================================");
            TestContext.WriteLine($"🌍 Infrastructure will remain active for all tests");
            TestContext.WriteLine($"🌍 Tests can now run in parallel with shared infrastructure");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"❌ Global infrastructure setup failed: {ex.Message}");
            TestContext.WriteLine($"❌ Stack trace: {ex.StackTrace}");
            throw;
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine("🌍 GLOBAL TEST INFRASTRUCTURE TEARDOWN START");
        TestContext.WriteLine("🌍 ========================================");

        if (AppHost != null)
        {
            try
            {
                await AppHost.StopAsync();
                TestContext.WriteLine("✅ AppHost stopped successfully");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ AppHost stop warning: {ex.Message}");
            }

            try
            {
                await AppHost.DisposeAsync();
                TestContext.WriteLine("✅ AppHost disposed successfully");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ AppHost dispose warning: {ex.Message}");
            }

            AppHost = null;
        }

        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine("🌍 GLOBAL INFRASTRUCTURE TEARDOWN COMPLETE");
        TestContext.WriteLine("🌍 ========================================");
    }

    private static void ConfigureGatewayJarPath()
    {
        var currentDir = Environment.CurrentDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);

        if (repoRoot == null)
        {
            TestContext.WriteLine("⚠️ Could not find repository root - Gateway may need to build JAR at runtime");
            return;
        }

        // Try Java 17 JAR first (new naming convention)
        var releaseJarPath17 = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner-java17.jar");

        if (File.Exists(releaseJarPath17))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", releaseJarPath17);
            TestContext.WriteLine($"✅ Configured Gateway JAR path: {releaseJarPath17}");
            return;
        }

        var debugJarPath17 = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner-java17.jar");

        if (File.Exists(debugJarPath17))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", debugJarPath17);
            TestContext.WriteLine($"✅ Configured Gateway JAR path (Debug): {debugJarPath17}");
            return;
        }

        // Fallback to old naming convention for backwards compatibility
        var releaseJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner.jar");

        if (File.Exists(releaseJarPath))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", releaseJarPath);
            TestContext.WriteLine($"✅ Configured Gateway JAR path (legacy): {releaseJarPath}");
            return;
        }

        var debugJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner.jar");

        if (File.Exists(debugJarPath))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", debugJarPath);
            TestContext.WriteLine($"✅ Configured Gateway JAR path (Debug, legacy): {debugJarPath}");
            return;
        }

        TestContext.WriteLine($"⚠️ Gateway JAR not found - will build on demand");
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
        TestContext.WriteLine("🐳 Validating Docker environment...");

        try
        {
            var dockerInfo = await RunDockerCommandAsync("info --format \"{{.ServerVersion}}\"");
            if (string.IsNullOrWhiteSpace(dockerInfo))
            {
                throw new InvalidOperationException("Docker is not accessible. Please ensure Docker Desktop is running.");
            }

            TestContext.WriteLine($"✅ Docker is running (version: {dockerInfo.Trim()})");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"❌ Docker validation failed: {ex.Message}");
            throw new InvalidOperationException("Docker environment is not ready for testing.", ex);
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
            TestContext.WriteLine($"⚠️ Gateway endpoint discovery failed: {ex.Message}, using configured port {Ports.GatewayHostPort}");
            return $"http://localhost:{Ports.GatewayHostPort}/";
        }
    }
}