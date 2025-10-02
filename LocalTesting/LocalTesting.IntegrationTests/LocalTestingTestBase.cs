using System.Diagnostics;
using Aspire.Hosting.Testing;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using LocalTesting.FlinkSqlAppHost;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Enhanced test base class for LocalTesting integration tests.
/// Based on successful patterns from BackPressureExample.IntegrationTests.KafkaTestBase
/// with improvements for Flink infrastructure readiness validation and Docker connectivity.
/// </summary>
public abstract class LocalTestingTestBase : IAsyncDisposable
{
    // Reduced timeout based on BackPressureExample success patterns
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan KafkaReadyTimeout = TimeSpan.FromSeconds(45); // Increased slightly for Docker startup
    private static readonly TimeSpan FlinkReadyTimeout = TimeSpan.FromSeconds(90); // Increased for Flink JobManager initialization (typically needs 30-45s)
    private static readonly TimeSpan GatewayReadyTimeout = TimeSpan.FromSeconds(60); // Increased timeout for Gateway .NET project startup

    protected DistributedApplication? AppHost { get; private set; }
    protected string? KafkaConnectionString { get; private set; }
    
    /// <summary>
    /// Kafka connection string for use by Flink jobs running inside containers.
    /// CRITICAL: Aspire's Kafka has TWO internal listeners:
    /// - PLAINTEXT_HOST on port 9092: for external access from host machine
    /// - PLAINTEXT_INTERNAL on port 9093: for container-to-container communication
    /// Flink containers must use "kafka:9093" to reach Kafka's PLAINTEXT_INTERNAL listener.
    /// See: https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Kafka/KafkaBuilderExtensions.cs
    /// </summary>
    protected static string KafkaContainerConnectionString => Ports.KafkaContainerBootstrap;

    [OneTimeSetUp]
    public virtual async Task OneTimeSetUp()
    {
        var cancellationToken = TestContext.CurrentContext.CancellationToken;
        
        TestContext.WriteLine("🔧 Starting LocalTesting infrastructure setup...");
        
        // Configure JAR path for Gateway to use Release build output
        ConfigureGatewayJarPath();
        
        // Validate Docker environment first
        await ValidateDockerEnvironmentAsync();
        
        // Build and start Aspire application with proper timeout handling
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(cancellationToken);
        var app = await appHost.BuildAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);
        await app.StartAsync(cancellationToken).WaitAsync(DefaultTimeout, cancellationToken);

        AppHost = app;

        // Wait for Kafka first (foundation component)
        TestContext.WriteLine("⏳ Waiting for Kafka resource to be healthy...");
        await app.ResourceNotifications
            .WaitForResourceHealthyAsync("kafka", cancellationToken)
            .WaitAsync(DefaultTimeout, cancellationToken);
        TestContext.WriteLine("✅ Kafka resource reported healthy");

        // Get connection string from Aspire (AddKafka properly exposes this)
        KafkaConnectionString = await app.GetConnectionStringAsync("kafka");
        TestContext.WriteLine($"🔗 Kafka connection string: {KafkaConnectionString}");

        // Additional Docker container validation
        await ValidateKafkaContainerAsync();

        // Enhanced Kafka readiness check (copied from BackPressureExample)
        await WaitForKafkaReadyAsync(KafkaConnectionString!, KafkaReadyTimeout, cancellationToken);
        TestContext.WriteLine("✅ Kafka is fully operational and ready for testing");
    }

    [OneTimeTearDown]
    public virtual async Task OneTimeTearDown()
    {
        try
        {
            await DisposeAsync();
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Cleanup warning: {ex.Message}");
        }
    }

    public async ValueTask DisposeAsync()
    {
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

        TestContext.WriteLine("✅ LocalTesting infrastructure cleanup completed");
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Configure the FLINK_RUNNER_JAR_PATH environment variable to point to the JAR
    /// built during Gateway compilation. This ensures the Gateway can find the JAR
    /// without needing to rebuild it at runtime.
    /// </summary>
    private static void ConfigureGatewayJarPath()
    {
        // Find repository root by looking for global.json
        var currentDir = Environment.CurrentDirectory;
        var repoRoot = FindRepositoryRoot(currentDir);
        
        if (repoRoot == null)
        {
            TestContext.WriteLine("⚠️ Could not find repository root - Gateway may need to build JAR at runtime");
            return;
        }
        
        // Check for JAR in Release build output (preferred for tests)
        var releaseJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0", "flink-ir-runner.jar");
        
        if (File.Exists(releaseJarPath))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", releaseJarPath);
            TestContext.WriteLine($"✅ Configured Gateway JAR path: {releaseJarPath}");
            return;
        }
        
        // Fallback to Debug build output
        var debugJarPath = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0", "flink-ir-runner.jar");
        
        if (File.Exists(debugJarPath))
        {
            Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", debugJarPath);
            TestContext.WriteLine($"✅ Configured Gateway JAR path (Debug): {debugJarPath}");
            return;
        }
        
        // If neither exists, Gateway will try to build on demand
        TestContext.WriteLine($"⚠️ Gateway JAR not found at {releaseJarPath} or {debugJarPath}");
        TestContext.WriteLine("⚠️ Gateway will attempt to build JAR on demand (requires Maven and Java)");
    }
    
    /// <summary>
    /// Find repository root by looking for global.json file.
    /// </summary>
    private static string? FindRepositoryRoot(string startPath)
    {
        var dir = new DirectoryInfo(startPath);
        while (dir != null)
        {
            var globalJsonPath = Path.Combine(dir.FullName, "global.json");
            if (File.Exists(globalJsonPath))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }

    /// <summary>
    /// Validate Docker environment is ready for container operations.
    /// </summary>
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
            throw new InvalidOperationException("Docker environment is not ready for testing. Please start Docker Desktop and ensure it's running properly.", ex);
        }
    }

    /// <summary>
    /// Validate that Kafka container is running and accessible.
    /// </summary>
    private static async Task ValidateKafkaContainerAsync()
    {
        TestContext.WriteLine("🔍 Validating Kafka container status...");
        
        try
        {
            // Wait a moment for containers to fully start
            await Task.Delay(5000);
            
            // Get detailed container information
            var containerInfo = await GetKafkaContainerDetailsAsync();
            if (!string.IsNullOrEmpty(containerInfo))
            {
                TestContext.WriteLine($"✅ Kafka container details: {containerInfo}");
            }
            
            // Check for running Kafka containers
            var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Names}} {{.Status}}\"");
            
            if (string.IsNullOrWhiteSpace(kafkaContainers))
            {
                TestContext.WriteLine("⚠️ No Kafka containers found running. Listing all containers:");
                var allContainers = await RunDockerCommandAsync("ps --format \"{{.Names}} {{.Status}}\"");
                TestContext.WriteLine($"All containers: {allContainers}");
            }
            else
            {
                TestContext.WriteLine($"✅ Kafka container status: {kafkaContainers.Trim()}");
            }

            // Check container networking
            await ValidateContainerNetworkingAsync();
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Container validation warning: {ex.Message}");
            // Don't fail here - continue with connectivity tests
        }
    }

    /// <summary>
    /// Get detailed information about Kafka containers including network configuration.
    /// </summary>
    private static async Task<string> GetKafkaContainerDetailsAsync()
    {
        try
        {
            // Get container details with network information
            var containerDetails = await RunDockerCommandAsync(
                "ps --filter \"name=kafka\" --format \"{{.Names}} {{.Ports}} {{.Networks}}\" --no-trunc"
            );
            
            if (!string.IsNullOrWhiteSpace(containerDetails))
            {
                return containerDetails.Trim();
            }

            // Try alternative container discovery
            var allContainers = await RunDockerCommandAsync(
                "ps --format \"{{.Names}} {{.Ports}} {{.Networks}}\" --no-trunc"
            );
            
            TestContext.WriteLine($"🔍 All container details: {allContainers}");
            return "No Kafka containers found";
        }
        catch (Exception ex)
        {
            return $"Could not get container details: {ex.Message}";
        }
    }

    /// <summary>
    /// Validate Docker container networking for Kafka connectivity.
    /// </summary>
    private static async Task ValidateContainerNetworkingAsync()
    {
        try
        {
            // List Docker networks
            var networks = await RunDockerCommandAsync("network ls --format \"{{.Name}}\"");
            TestContext.WriteLine($"🌐 Available Docker networks: {networks.Replace('\n', ' ').Trim()}");
            
            // Test port connectivity
            var portTest = await TestPortConnectivityAsync("127.0.0.1", Ports.KafkaPort);
            if (portTest)
            {
                TestContext.WriteLine($"✅ Port {Ports.KafkaPort} is accessible on localhost");
            }
            else
            {
                TestContext.WriteLine($"⚠️ Port {Ports.KafkaPort} is not yet accessible - containers may still be starting");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Network validation warning: {ex.Message}");
        }
    }

    /// <summary>
    /// Test if a specific port is accessible.
    /// </summary>
    private static async Task<bool> TestPortConnectivityAsync(string host, int port)
    {
        try
        {
            using var client = new System.Net.Sockets.TcpClient();
            await client.ConnectAsync(host, port);
            return client.Connected;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Run a Docker command and return the output.
    /// </summary>
    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        
        await process.WaitForExitAsync();
        
        if (process.ExitCode != 0 && !string.IsNullOrWhiteSpace(error))
        {
            throw new InvalidOperationException($"Docker command failed: {error}");
        }

        return output;
    }

    /// <summary>
    /// Enhanced Kafka readiness check copied from BackPressureExample.IntegrationTests.KafkaTestBase
    /// with improved error handling, fallback strategies, and dynamic container discovery.
    /// </summary>
    protected static async Task WaitForKafkaReadyAsync(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        TestContext.WriteLine($"🔎 [KafkaReady] Probing broker metadata at {bootstrapServers}");
        
        var bootstrapVariations = await GetBootstrapServerVariationsAsync(bootstrapServers);
        TestContext.WriteLine($"🔗 [KafkaReady] Will try connection variations: {string.Join(", ", bootstrapVariations)}");

        Exception? lastException = null;
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            
            var (connected, exception) = await TryConnectToKafkaAsync(bootstrapVariations, attempt, sw.Elapsed);
            if (connected)
            {
                return;
            }
            
            lastException = exception;
            await LogKafkaAttemptDiagnosticsAsync(attempt, bootstrapVariations, lastException);
            await Task.Delay(1000, ct);
        }
        
        throw await CreateKafkaTimeoutExceptionAsync(timeout, bootstrapVariations, lastException);
    }

    private static Task<(bool connected, Exception? exception)> TryConnectToKafkaAsync(string[] bootstrapVariations, int attempt, TimeSpan elapsed)
    {
        Exception? lastException = null;
        
        foreach (var bootstrap in bootstrapVariations)
        {
            try
            {
                using var admin = CreateKafkaAdminClient(bootstrap);
                var md = admin.GetMetadata(TimeSpan.FromSeconds(2));
                
                if (md?.Brokers?.Count > 0)
                {
                    TestContext.WriteLine($"✅ [KafkaReady] Metadata OK (brokers={md.Brokers.Count}) using {bootstrap} after {attempt} attempt(s), {elapsed.TotalSeconds:F1}s");
                    return Task.FromResult((true, (Exception?)null));
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }
        }
        return Task.FromResult((false, lastException));
    }

    private static IAdminClient CreateKafkaAdminClient(string bootstrap)
    {
        return new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = bootstrap,
            SocketTimeoutMs = 3000,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext,
            ApiVersionRequest = true,
            LogConnectionClose = false,
            AllowAutoCreateTopics = true
        })
        .SetLogHandler((_, _) => { /* Suppress logs during readiness */ })
        .SetErrorHandler((_, _) => { /* Suppress errors during readiness */ })
        .Build();
    }

    private static async Task LogKafkaAttemptDiagnosticsAsync(int attempt, string[] bootstrapVariations, Exception? lastException)
    {
        if (attempt % 10 == 0)
        {
            TestContext.WriteLine($"⏳ [KafkaReady] Attempt {attempt} - detailed diagnostics:");
            await LogDetailedDiagnosticsAsync(bootstrapVariations, lastException);
        }
        else if (attempt % 5 == 0)
        {
            TestContext.WriteLine($"⏳ [KafkaReady] Attempt {attempt} - trying multiple connection methods...");
            if (lastException != null)
            {
                TestContext.WriteLine($"    Last error: {lastException.GetType().Name} - {lastException.Message}");
            }
        }
    }

    private static async Task<TimeoutException> CreateKafkaTimeoutExceptionAsync(TimeSpan timeout, string[] bootstrapVariations, Exception? lastException)
    {
        var containerStatus = await GetKafkaContainerDetailsAsync();
        return new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s. " +
                                  $"Bootstrap servers tried: {string.Join(", ", bootstrapVariations)}. " +
                                  $"Last error: {lastException?.Message}. " +
                                  $"Container diagnostics: {containerStatus}");
    }

    /// <summary>
    /// Get comprehensive bootstrap server variations including dynamic container discovery.
    /// </summary>
    private static async Task<string[]> GetBootstrapServerVariationsAsync(string originalBootstrap)
    {
        var variations = new List<string> { originalBootstrap };
        
        try
        {
            // Standard localhost variations
            variations.Add(originalBootstrap.Replace("localhost", "127.0.0.1"));
            variations.Add($"127.0.0.1:{Ports.KafkaPort}");
            
            // Try to discover actual container IP and ports
            var containerPorts = await DiscoverKafkaContainerEndpointsAsync();
            variations.AddRange(containerPorts);
            
            // Remove duplicates
            return variations.Distinct().ToArray();
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Could not discover container endpoints: {ex.Message}");
            return variations.Distinct().ToArray();
        }
    }

    /// <summary>
    /// Discover actual Kafka container endpoints through Docker inspection.
    /// </summary>
    private static async Task<List<string>> DiscoverKafkaContainerEndpointsAsync()
    {
        var endpoints = new List<string>();
        
        try
        {
            await DiscoverPortMappingEndpointsAsync(endpoints);
            await DiscoverContainerIPEndpointsAsync(endpoints);
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Container endpoint discovery failed: {ex.Message}");
        }
        
        return endpoints;
    }

    private static async Task DiscoverPortMappingEndpointsAsync(List<string> endpoints)
    {
        var portMappings = await RunDockerCommandAsync(
            "ps --filter \"name=kafka\" --format \"{{.Ports}}\" --no-trunc"
        );
        
        if (string.IsNullOrWhiteSpace(portMappings))
        {
            return;
        }

        TestContext.WriteLine($"🔍 Container port mappings: {portMappings.Trim()}");
        
        var lines = portMappings.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains("9092") && line.Contains("->"))
            {
                ParsePortMapping(line, endpoints);
            }
        }
    }

    private static void ParsePortMapping(string line, List<string> endpoints)
    {
        var parts = line.Split("->")[0].Trim();
        if (parts.Contains(":"))
        {
            var endpoint = parts.Replace("0.0.0.0:", "localhost:");
            endpoints.Add(endpoint);
            endpoints.Add(parts.Replace("0.0.0.0:", "127.0.0.1:"));
        }
    }

    private static async Task DiscoverContainerIPEndpointsAsync(List<string> endpoints)
    {
        var containerNames = await RunDockerCommandAsync(
            "ps --filter \"name=kafka\" --format \"{{.Names}}\""
        );
        
        if (string.IsNullOrWhiteSpace(containerNames))
        {
            return;
        }

        var names = containerNames.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var name in names)
        {
            await TryAddContainerIPEndpointAsync(name.Trim(), endpoints);
        }
    }

    private static async Task TryAddContainerIPEndpointAsync(string containerName, List<string> endpoints)
    {
        try
        {
            var ipAddress = await RunDockerCommandAsync(
                $"inspect {containerName} --format \"{{{{.NetworkSettings.IPAddress}}}}\""
            );
            
            if (!string.IsNullOrWhiteSpace(ipAddress) && ipAddress.Trim() != "")
            {
                endpoints.Add($"{ipAddress.Trim()}:9092");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Could not inspect container {containerName}: {ex.Message}");
        }
    }

    /// <summary>
    /// Log detailed diagnostics for Kafka connectivity troubleshooting.
    /// </summary>
    private static async Task LogDetailedDiagnosticsAsync(string[] bootstrapVariations, Exception? lastException)
    {
        try
        {
            TestContext.WriteLine("🔍 Detailed connectivity diagnostics:");
            
            // Test each endpoint manually
            foreach (var endpoint in bootstrapVariations.Take(3)) // Test first 3 to avoid spam
            {
                var parts = endpoint.Split(':');
                if (parts.Length == 2 && int.TryParse(parts[1], out var port))
                {
                    var reachable = await TestPortConnectivityAsync(parts[0], port);
                    TestContext.WriteLine($"   {endpoint}: {(reachable ? "✅ Reachable" : "❌ Not reachable")}");
                }
            }
            
            // Container status
            var containers = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Names}}: {{.Status}} - {{.Ports}}\"");
            TestContext.WriteLine($"   Container Status: {containers.Trim()}");
            
            // Network information
            var networks = await RunDockerCommandAsync("network ls --format \"{{.Name}}: {{.Driver}}\"");
            TestContext.WriteLine($"   Networks: {networks.Replace('\n', ' ').Trim()}");
            
            if (lastException != null)
            {
                TestContext.WriteLine($"   Last Exception: {lastException.GetType().Name}: {lastException.Message}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Could not gather detailed diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Enhanced Flink readiness check with proper API validation and TaskManager status checking.
    /// Improved from original LocalTesting tests with better error handling.
    /// </summary>
    protected static async Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        
        TestContext.WriteLine($"🔎 [FlinkReady] Probing Flink JobManager at {overviewUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"⏳ [FlinkReady] Waiting initial 10 seconds for Flink container to initialize...");
        
        // Give Flink time to start - JobManager typically needs 20-30 seconds
        await Task.Delay(10000, ct);
        
        // First check container port accessibility
        var portAccessible = await TestPortConnectivityAsync("localhost", Ports.JobManagerHostPort);
        TestContext.WriteLine($"🔍 [FlinkReady] Port {Ports.JobManagerHostPort} accessible: {portAccessible}");
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            var success = await CheckFlinkJobManagerAsync(http, overviewUrl, attempt, ct);
            if (success)
            {
                TestContext.WriteLine($"✅ [FlinkReady] JobManager with TaskManagers ready at {overviewUrl} after {attempt} attempt(s), {sw.Elapsed.TotalSeconds:F1}s");
                return;
            }
            
            await Task.Delay(2000, ct); // 2-second interval for Flink checks
        }
        
        // Final diagnostics before throwing
        await LogFlinkContainerDiagnosticsAsync();
        throw new TimeoutException($"Flink JobManager not ready within {timeout.TotalSeconds:F0}s at {overviewUrl}");
    }
    
    /// <summary>
    /// Check if Flink JobManager is ready with TaskManagers.
    /// </summary>
    private static async Task<bool> CheckFlinkJobManagerAsync(HttpClient http, string overviewUrl, int attempt, CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync(overviewUrl, ct);
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync(ct);
                return ValidateFlinkResponse(content, attempt);
            }
            
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: HTTP {resp.StatusCode}");
            return false;
        }
        catch (HttpRequestException ex)
        {
            await HandleFlinkHttpExceptionAsync(ex, attempt);
            return false;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt} failed: {ex.GetType().Name} - {ex.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// Validate Flink JobManager response content.
    /// </summary>
    private static bool ValidateFlinkResponse(string content, int attempt)
    {
        if (!string.IsNullOrEmpty(content) && content.Contains("taskmanagers"))
        {
            return true;
        }
        
        if (!string.IsNullOrEmpty(content))
        {
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: JobManager responding but TaskManagers not ready yet");
        }
        
        return false;
    }
    
    /// <summary>
    /// Handle HTTP exceptions during Flink readiness checks.
    /// </summary>
    private static async Task HandleFlinkHttpExceptionAsync(HttpRequestException ex, int attempt)
    {
        if (ex.InnerException is System.Net.Sockets.SocketException socketEx)
        {
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: Connection refused (SocketError: {socketEx.SocketErrorCode}) - Flink process still starting");
        }
        else
        {
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
        }
        
        // Log detailed diagnostics every 10 attempts
        if (attempt % 10 == 0)
        {
            await LogFlinkContainerDiagnosticsAsync();
        }
    }
    
    /// <summary>
    /// Log detailed Flink container diagnostics for troubleshooting.
    /// </summary>
    private static async Task LogFlinkContainerDiagnosticsAsync()
    {
        try
        {
            TestContext.WriteLine("🔍 [FlinkReady] Container diagnostics:");
            
            // Check Flink containers
            var flinkContainers = await RunDockerCommandAsync("ps --filter \"name=flink\" --format \"{{.Names}}: {{.Status}} - {{.Ports}}\"");
            TestContext.WriteLine($"   Flink Containers: {flinkContainers.Trim()}");
            
            // Check if port is listening
            var portTest = await TestPortConnectivityAsync("localhost", Ports.JobManagerHostPort);
            TestContext.WriteLine($"   Port {Ports.JobManagerHostPort} accessible: {portTest}");
            
            // Try to get container logs
            var jobManagerLogs = await RunDockerCommandAsync("logs --tail 20 flink-jobmanager 2>&1 || echo 'Could not get logs'");
            TestContext.WriteLine($"   JobManager logs (last 20 lines): {jobManagerLogs.Trim()}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Could not gather Flink diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Enhanced Gateway readiness check with proper retry logic.
    /// Gateway is a .NET project that starts after Flink, so it may need additional time.
    /// Based on patterns from BackPressureExample with LocalTesting-specific endpoints.
    /// </summary>
    protected static async Task WaitForGatewayReadyAsync(string healthUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        
        TestContext.WriteLine($"🔎 [GatewayReady] Probing Gateway at {healthUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"💡 [GatewayReady] Gateway is a .NET project that starts after Flink, may need 30-60s");
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            try
            {
                var resp = await http.GetAsync(healthUrl, ct);
                if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
                {
                    TestContext.WriteLine($"✅ [GatewayReady] Gateway ready at {healthUrl} after {attempt} attempt(s), {sw.Elapsed.TotalSeconds:F1}s");
                    return;
                }
                else
                {
                    if (attempt % 10 == 0)
                    {
                        TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: HTTP {resp.StatusCode} (elapsed: {sw.Elapsed.TotalSeconds:F1}s)");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                // Connection refused is normal during Gateway startup
                if (attempt % 10 == 0)
                {
                    TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: {ex.GetType().Name} (elapsed: {sw.Elapsed.TotalSeconds:F1}s)");
                }
            }
            catch (Exception ex)
            {
                if (attempt % 10 == 0)
                {
                    TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
                }
            }
            
            await Task.Delay(1000, ct); // 1-second interval for Gateway checks (increased from 500ms)
        }
        
        TestContext.WriteLine($"❌ [GatewayReady] Gateway failed to start after {attempt} attempts over {sw.Elapsed.TotalSeconds:F1}s");
        throw new TimeoutException($"Gateway not ready within {timeout.TotalSeconds:F0}s at {healthUrl}. Gateway may not have started properly - check Aspire logs.");
    }

    /// <summary>
    /// Create Kafka topic with proper error handling for existing topics.
    /// Copied from BackPressureExample patterns.
    /// </summary>
    protected async Task CreateTopicAsync(string topicName, int partitions = 1, short replicationFactor = 1)
    {
        if (string.IsNullOrEmpty(KafkaConnectionString))
            throw new InvalidOperationException("Kafka connection string is not available");

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = KafkaConnectionString,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { /* Suppress logs */ })
        .SetErrorHandler((_, _) => { /* Suppress errors */ })
        .Build();

        try
        {
            var topicSpec = new TopicSpecification
            {
                Name = topicName,
                NumPartitions = partitions,
                ReplicationFactor = replicationFactor,
                Configs = new Dictionary<string, string>
                {
                    ["min.insync.replicas"] = "1",
                    ["unclean.leader.election.enable"] = "true"
                }
            };

            await admin.CreateTopicsAsync(new[] { topicSpec });
            TestContext.WriteLine($"✅ Topic '{topicName}' created successfully");
            
            // Small delay to ensure topic is fully ready
            await Task.Delay(1000);
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results?.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists) == true)
            {
                TestContext.WriteLine($"ℹ️ Topic '{topicName}' already exists");
            }
            else
            {
                TestContext.WriteLine($"❌ Error creating topic '{topicName}': {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Wait for complete infrastructure readiness including optional Gateway.
    /// Provides centralized infrastructure validation for complex test scenarios.
    /// </summary>
    protected async Task WaitForFullInfrastructureAsync(bool includeGateway = true, CancellationToken cancellationToken = default)
    {
        TestContext.WriteLine("🔧 Validating complete infrastructure readiness...");

        // Kafka is already validated in OneTimeSetUp, but double-check if needed
        if (string.IsNullOrEmpty(KafkaConnectionString))
        {
            throw new InvalidOperationException("Kafka connection string not available - OneTimeSetUp may have failed");
        }

        if (AppHost == null)
        {
            throw new InvalidOperationException("AppHost is not available - OneTimeSetUp may have failed");
        }

        // Get the dynamically allocated Flink JobManager endpoint from Aspire
        // Aspire DCP assigns random ports during testing, so we must query the actual endpoint
        var flinkJobManagerEndpoint = await GetFlinkJobManagerEndpointAsync();
        TestContext.WriteLine($"🔍 Discovered Flink JobManager endpoint: {flinkJobManagerEndpoint}");

        // Wait for Flink JobManager and TaskManager
        await WaitForFlinkReadyAsync($"{flinkJobManagerEndpoint}v1/overview", FlinkReadyTimeout, cancellationToken);
        TestContext.WriteLine("✅ Flink JobManager and TaskManager are ready");

        // Wait for Gateway if included
        if (includeGateway)
        {
            // CRITICAL: Aspire testing framework does NOT automatically start .NET project resources
            // We must explicitly wait for the Gateway resource to become healthy
            TestContext.WriteLine("⏳ Waiting for Gateway resource to start (Aspire project resources require explicit activation)...");
            await AppHost.ResourceNotifications
                .WaitForResourceHealthyAsync("flink-job-gateway", cancellationToken)
                .WaitAsync(GatewayReadyTimeout, cancellationToken);
            TestContext.WriteLine("✅ Gateway resource reported healthy by Aspire");
            
            // Now verify Gateway HTTP endpoint is responding
            var gatewayEndpoint = await GetGatewayEndpointAsync();
            TestContext.WriteLine($"🔍 Discovered Gateway endpoint: {gatewayEndpoint}");
            await WaitForGatewayReadyAsync($"{gatewayEndpoint}api/v1/health", GatewayReadyTimeout, cancellationToken);
            TestContext.WriteLine("✅ Flink Job Gateway is ready");
        }

        TestContext.WriteLine("✅ Complete infrastructure is ready for testing");
    }

    /// <summary>
    /// Get the dynamically allocated Flink JobManager HTTP endpoint from Aspire.
    /// Aspire DCP assigns random ports during testing, so we cannot use hardcoded ports.
    /// </summary>
    private static async Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            // Discover port from Docker - Aspire DCP assigns random ports
            var flinkContainers = await RunDockerCommandAsync("ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"");
            TestContext.WriteLine($"🔍 Flink JobManager port mappings: {flinkContainers.Trim()}");
            
            // Parse port mapping: 127.0.0.1:XXXXX->8081/tcp
            var lines = flinkContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.Contains("->8081/tcp"))
                {
                    // Extract the host port
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->8081");
                    if (match.Success)
                    {
                        var hostPort = match.Groups[1].Value;
                        return $"http://localhost:{hostPort}/";
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

    /// <summary>
    /// Get the dynamically allocated Gateway HTTP endpoint from Aspire.
    /// The Gateway is a .NET project (not a container), and Aspire DCP may assign random ports during testing.
    /// We check Docker first (for containerized scenarios), then check process listening ports, then fallback to configured port.
    /// </summary>
    private static async Task<string> GetGatewayEndpointAsync()
    {
        try
        {
            // Try to discover port from Docker - check for Gateway container (rare case)
            var gatewayContainers = await RunDockerCommandAsync("ps --filter \"name=gateway\" --format \"{{.Ports}}\"");
            
            if (!string.IsNullOrWhiteSpace(gatewayContainers))
            {
                TestContext.WriteLine($"🔍 Gateway container port mappings: {gatewayContainers.Trim()}");
                
                // Parse port mapping: 127.0.0.1:XXXXX->8080/tcp or similar patterns
                var lines = gatewayContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->(\d+)/tcp");
                    if (match.Success)
                    {
                        var hostPort = match.Groups[1].Value;
                        var containerPort = match.Groups[2].Value;
                        TestContext.WriteLine($"🔍 Found Gateway container port mapping: host {hostPort} -> container {containerPort}");
                        return $"http://localhost:{hostPort}/";
                    }
                }
            }
            
            // Gateway is added as .AddProject(), so it runs as a .NET process, not a Docker container
            // In Aspire testing mode, the project may use a dynamically assigned port or the configured port
            // For now, we use the configured port since Aspire's WithHttpEndpoint should respect it
            TestContext.WriteLine($"ℹ️ Gateway running as .NET project (not containerized), using configured port {Ports.GatewayHostPort}");
            TestContext.WriteLine($"💡 Gateway may take 15-30 seconds to start after Flink is ready");
            return $"http://localhost:{Ports.GatewayHostPort}/";
        }
        catch (Exception ex)
        {
            // Fallback to configured port if discovery fails
            TestContext.WriteLine($"⚠️ Gateway endpoint discovery failed: {ex.Message}, using configured port {Ports.GatewayHostPort}");
            return $"http://localhost:{Ports.GatewayHostPort}/";
        }
    }
}
