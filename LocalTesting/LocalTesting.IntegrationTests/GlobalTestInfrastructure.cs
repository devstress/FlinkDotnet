using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LocalTesting.FlinkSqlAppHost;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Assembly-level test infrastructure setup for LocalTesting integration tests.
/// Initializes infrastructure ONCE for all tests to dramatically reduce startup overhead.
/// Infrastructure includes: Docker, Kafka, Flink JobManager, Flink TaskManager, and Gateway.
/// 
/// ARCHITECTURE: Starts AppHost as a real process (not DistributedApplicationTestingBuilder)
/// because the testing builder is designed for config validation, not integration testing.
/// Real process ensures actual containers are started via Aspire DCP in both local and CI.
/// </summary>
[SetUpFixture]
public class GlobalTestInfrastructure
{
    private static readonly TimeSpan KafkaReadyTimeout = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan FlinkReadyTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan GatewayReadyTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan AppHostStartupTimeout = TimeSpan.FromSeconds(120);

    private static Process? _appHostProcess;
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
            // Validate Docker environment
            await ValidateDockerEnvironmentAsync();

            // Find repository root
            var repoRoot = FindRepositoryRoot(Environment.CurrentDirectory);
            if (repoRoot == null)
            {
                throw new InvalidOperationException("Could not find repository root (global.json not found)");
            }
            TestContext.WriteLine($"✅ Repository root: {repoRoot}");

            // Start AppHost as a real process (not testing builder)
            // This ensures real containers are started via Aspire DCP
            TestContext.WriteLine("🔧 Starting AppHost as real process...");
            _appHostProcess = StartAppHostProcess(repoRoot);
            TestContext.WriteLine("✅ AppHost process started");

            // Wait for AppHost to initialize and start containers
            await WaitForAppHostReadyAsync(AppHostStartupTimeout);
            TestContext.WriteLine("✅ AppHost is running and containers are starting");

            // Discover Kafka endpoint from running containers
            KafkaConnectionString = await DiscoverKafkaEndpointAsync();
            TestContext.WriteLine($"🔗 Kafka connection string: {KafkaConnectionString}");

            // Enhanced Kafka readiness check
            await LocalTestingTestBase.WaitForKafkaReadyAsync(KafkaConnectionString!, KafkaReadyTimeout, default);
            TestContext.WriteLine("✅ Kafka is fully operational");

            // Get Flink endpoint and wait for readiness
            var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
            TestContext.WriteLine($"🔍 Flink JobManager endpoint: {flinkEndpoint}");
            await LocalTestingTestBase.WaitForFlinkReadyAsync($"{flinkEndpoint}v1/overview", FlinkReadyTimeout, default);
            TestContext.WriteLine("✅ Flink JobManager and TaskManager are ready");

            // Wait for Gateway
            TestContext.WriteLine("⏳ Waiting for Gateway to start...");
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
            
            // Cleanup on failure
            if (_appHostProcess != null && !_appHostProcess.HasExited)
            {
                _appHostProcess.Kill();
                await _appHostProcess.WaitForExitAsync();
            }
            
            throw;
        }
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine("🌍 GLOBAL TEST INFRASTRUCTURE TEARDOWN START");
        TestContext.WriteLine("🌍 ========================================");

        if (_appHostProcess != null)
        {
            try
            {
                if (!_appHostProcess.HasExited)
                {
                    TestContext.WriteLine("🛑 Stopping AppHost process...");
                    _appHostProcess.Kill();
                    await _appHostProcess.WaitForExitAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(30));
                    TestContext.WriteLine("✅ AppHost process stopped");
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ AppHost stop warning: {ex.Message}");
            }

            try
            {
                _appHostProcess.Dispose();
                TestContext.WriteLine("✅ AppHost process disposed");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ AppHost dispose warning: {ex.Message}");
            }

            _appHostProcess = null;
        }

        // Give containers time to cleanup
        await Task.Delay(TimeSpan.FromSeconds(5));

        TestContext.WriteLine("🌍 ========================================");
        TestContext.WriteLine("🌍 GLOBAL INFRASTRUCTURE TEARDOWN COMPLETE");
        TestContext.WriteLine("🌍 ========================================");
    }

    private static Process StartAppHostProcess(string repoRoot)
    {
        var appHostProjectPath = Path.Combine(repoRoot, "LocalTesting", "LocalTesting.FlinkSqlAppHost");
        
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = "run --no-build --configuration Release",
                WorkingDirectory = appHostProjectPath,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };

        // Capture output for diagnostics
        process.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost] {e.Data}");
            }
        };
        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost ERROR] {e.Data}");
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        return process;
    }

    private static async Task WaitForAppHostReadyAsync(TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow.Add(timeout);
        var attempt = 0;

        TestContext.WriteLine("⏳ Waiting for AppHost to start containers...");

        while (DateTime.UtcNow < deadline)
        {
            attempt++;

            if (_appHostProcess?.HasExited == true)
            {
                throw new InvalidOperationException($"AppHost process exited unexpectedly with code {_appHostProcess.ExitCode}");
            }

            // Check if any containers have started (indicates AppHost is working)
            var containers = await RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            if (!string.IsNullOrWhiteSpace(containers) && containers.Contains("kafka"))
            {
                TestContext.WriteLine($"✅ AppHost has started containers after {attempt} attempts");
                return;
            }

            if (attempt % 10 == 0)
            {
                TestContext.WriteLine($"⏳ Attempt {attempt}: Still waiting for containers...");
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"AppHost did not start containers within {timeout.TotalSeconds:F0}s");
    }

    private static async Task<string> DiscoverKafkaEndpointAsync()
    {
        // Discover Kafka endpoint from running container
        var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Ports}}\"");
        
        if (string.IsNullOrWhiteSpace(kafkaContainers))
        {
            throw new InvalidOperationException("Kafka container not found");
        }

        // Parse port mapping (e.g., "0.0.0.0:9092->9092/tcp")
        var match = System.Text.RegularExpressions.Regex.Match(kafkaContainers, @"127\.0\.0\.1:(\d+)->9092|0\.0\.0\.0:(\d+)->9092");
        if (match.Success)
        {
            var port = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
            return $"localhost:{port}";
        }

        // Fallback to configured port
        return $"localhost:{Ports.KafkaPort}";
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