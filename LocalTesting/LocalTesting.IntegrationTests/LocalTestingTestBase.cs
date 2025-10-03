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
public abstract class LocalTestingTestBase
{
    // Optimized timeouts for faster test execution (used in WaitForFullInfrastructureAsync)
    private static readonly TimeSpan FlinkReadyTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan GatewayReadyTimeout = TimeSpan.FromSeconds(45);

    /// <summary>
    /// Access to shared AppHost instance from GlobalTestInfrastructure.
    /// Infrastructure is initialized once for all tests, dramatically reducing startup overhead.
    /// </summary>
    protected static DistributedApplication? AppHost => GlobalTestInfrastructure.AppHost;
    
    /// <summary>
    /// Access to shared Kafka connection string from GlobalTestInfrastructure.
    /// </summary>
    protected static string? KafkaConnectionString => GlobalTestInfrastructure.KafkaConnectionString;
    
    /// <summary>
    /// Kafka connection string for use by Flink jobs running inside containers.
    /// CRITICAL: Aspire's Kafka has TWO internal listeners:
    /// - PLAINTEXT_HOST on port 9092: for external access from host machine
    /// - PLAINTEXT_INTERNAL on port 9093: for container-to-container communication
    /// Flink containers must use "kafka:9093" to reach Kafka's PLAINTEXT_INTERNAL listener.
    /// See: https://github.com/dotnet/aspire/blob/main/src/Aspire.Hosting.Kafka/KafkaBuilderExtensions.cs
    /// </summary>
    protected static string KafkaContainerConnectionString => GlobalTestInfrastructure.KafkaContainerConnectionString;

    /// <summary>
    /// No infrastructure setup needed - using shared global infrastructure.
    /// Tests can start immediately without waiting for infrastructure startup.
    /// </summary>
    [OneTimeSetUp]
    public virtual Task OneTimeSetUp()
    {
        // Verify shared infrastructure is available
        if (AppHost == null || string.IsNullOrEmpty(KafkaConnectionString))
        {
            throw new InvalidOperationException(
                "Global test infrastructure is not initialized. " +
                "Ensure GlobalTestInfrastructure.GlobalSetUp completed successfully.");
        }

        TestContext.WriteLine($"✅ Test class using shared infrastructure (Kafka: {KafkaConnectionString})");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No teardown needed - shared infrastructure persists across all tests.
    /// </summary>
    [OneTimeTearDown]
    public virtual Task OneTimeTearDown()
    {
        TestContext.WriteLine("✅ Test class completed (shared infrastructure remains active)");
        return Task.CompletedTask;
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
    public static async Task WaitForKafkaReadyAsync(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
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
                return;
            
            lastException = exception;
            await LogKafkaAttemptDiagnosticsAsync(attempt, bootstrapVariations, lastException);
            await Task.Delay(500, ct); // Optimized: Reduced from 1000ms to 500ms
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
            return;

        TestContext.WriteLine($"🔍 Container port mappings: {portMappings.Trim()}");
        
        ProcessPortMappingLines(portMappings, endpoints);
    }

    private static void ProcessPortMappingLines(string portMappings, List<string> endpoints)
    {
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
        if (!parts.Contains(":"))
            return;

        endpoints.Add(parts.Replace("0.0.0.0:", "localhost:"));
        endpoints.Add(parts.Replace("0.0.0.0:", "127.0.0.1:"));
    }

    private static async Task DiscoverContainerIPEndpointsAsync(List<string> endpoints)
    {
        var containerNames = await RunDockerCommandAsync(
            "ps --filter \"name=kafka\" --format \"{{.Names}}\""
        );
        
        if (string.IsNullOrWhiteSpace(containerNames))
            return;

        await ProcessContainerNamesAsync(containerNames, endpoints);
    }

    private static async Task ProcessContainerNamesAsync(string containerNames, List<string> endpoints)
    {
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
    /// <param name="overviewUrl">Flink overview API endpoint</param>
    /// <param name="timeout">Maximum time to wait</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="requireFreeSlots">If true, requires at least one free task slot. Use true for initial setup, false for per-test checks.</param>
    public static async Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct, bool requireFreeSlots = true)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        
        await InitializeFlinkReadinessCheckAsync(overviewUrl, timeout);
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            if (await CheckFlinkJobManagerAsync(http, overviewUrl, attempt, ct, requireFreeSlots))
            {
                var slotsMessage = requireFreeSlots ? " with available slots" : "";
                TestContext.WriteLine($"✅ [FlinkReady] JobManager with TaskManagers ready{slotsMessage} at {overviewUrl} after {attempt} attempt(s), {sw.Elapsed.TotalSeconds:F1}s");
                return;
            }
            
            await Task.Delay(1000, ct); // Optimized: Reduced from 2000ms to 1000ms
        }
        
        await LogFlinkContainerDiagnosticsAsync();
        throw new TimeoutException($"Flink JobManager not ready within {timeout.TotalSeconds:F0}s at {overviewUrl}");
    }

    private static async Task InitializeFlinkReadinessCheckAsync(string overviewUrl, TimeSpan timeout)
    {
        TestContext.WriteLine($"🔎 [FlinkReady] Probing Flink JobManager at {overviewUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"⏳ [FlinkReady] Waiting initial 10 seconds for Flink container to initialize...");
        
        await Task.Delay(10000);
        
        var portAccessible = await TestPortConnectivityAsync("localhost", Ports.JobManagerHostPort);
        TestContext.WriteLine($"🔍 [FlinkReady] Port {Ports.JobManagerHostPort} accessible: {portAccessible}");
    }
    
    /// <summary>
    /// Check if Flink JobManager is ready with TaskManagers and available task slots.
    /// Enhanced to verify task slots are available before allowing job submission.
    /// </summary>
    /// <param name="http">HTTP client to use for requests</param>
    /// <param name="overviewUrl">Flink overview API URL</param>
    /// <param name="attempt">Attempt number for logging</param>
    /// <param name="ct">Cancellation token</param>
    /// <param name="requireFreeSlots">If true, requires at least one free task slot</param>
    private static async Task<bool> CheckFlinkJobManagerAsync(HttpClient http, string overviewUrl, int attempt, CancellationToken ct, bool requireFreeSlots)
    {
        try
        {
            // First check overview endpoint to verify TaskManagers are registered
            var resp = await http.GetAsync(overviewUrl, ct);
            if (resp.IsSuccessStatusCode)
            {
                var content = await resp.Content.ReadAsStringAsync(ct);
                if (!ValidateFlinkResponse(content, attempt))
                {
                    return false;
                }
                
                // TaskManagers are registered - check slots only if required
                if (requireFreeSlots)
                {
                    var baseUrl = overviewUrl.Replace("/v1/overview", "");
                    return await CheckTaskManagerSlotsAsync(http, baseUrl, attempt, ct);
                }
                
                // Slots not required, just TaskManager registration is enough
                return true;
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
    /// Check if TaskManagers have available task slots for job submission.
    /// Queries /v1/taskmanagers endpoint to verify at least one free slot exists.
    /// </summary>
    private static async Task<bool> CheckTaskManagerSlotsAsync(HttpClient http, string baseUrl, int attempt, CancellationToken ct)
    {
        try
        {
            var taskManagersUrl = $"{baseUrl}/v1/taskmanagers";
            var resp = await http.GetAsync(taskManagersUrl, ct);
            
            if (!resp.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: TaskManagers endpoint returned {resp.StatusCode}");
                return false;
            }
            
            var content = await resp.Content.ReadAsStringAsync(ct);
            
            // Parse JSON to check for available slots
            // Expected format: {"taskmanagers":[{"id":"...","slotsNumber":2,"freeSlots":2,...}]}
            if (string.IsNullOrWhiteSpace(content) || !content.Contains("taskmanagers"))
            {
                TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: TaskManagers response missing 'taskmanagers' field");
                return false;
            }
            
            // Simple JSON parsing to check for freeSlots > 0
            // Look for "freeSlots": pattern followed by a number greater than 0
            var freeSlotsMatch = System.Text.RegularExpressions.Regex.Match(content, @"""freeSlots""\s*:\s*(\d+)");
            if (freeSlotsMatch.Success)
            {
                var freeSlots = int.Parse(freeSlotsMatch.Groups[1].Value);
                if (freeSlots > 0)
                {
                    TestContext.WriteLine($"✅ [FlinkReady] Attempt {attempt}: TaskManagers ready with {freeSlots} free slot(s)");
                    return true;
                }
                else
                {
                    TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: TaskManagers registered but no free slots available yet");
                    return false;
                }
            }
            
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: Could not parse freeSlots from TaskManagers response");
            return false;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⏳ [FlinkReady] Attempt {attempt}: TaskManager slot check failed - {ex.GetType().Name}: {ex.Message}");
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
    public static async Task WaitForGatewayReadyAsync(string healthUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        
        LogGatewayReadinessStart(healthUrl, timeout);
        
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            if (await CheckGatewayHealthAsync(http, healthUrl, attempt, sw.Elapsed, ct))
                return;
            
            await Task.Delay(1000, ct);
        }
        
        ThrowGatewayTimeoutException(healthUrl, timeout, attempt, sw.Elapsed);
    }

    private static void LogGatewayReadinessStart(string healthUrl, TimeSpan timeout)
    {
        TestContext.WriteLine($"🔎 [GatewayReady] Probing Gateway at {healthUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"💡 [GatewayReady] Gateway is a .NET project that starts after Flink, may need 30-60s");
    }

    private static async Task<bool> CheckGatewayHealthAsync(
        HttpClient http,
        string healthUrl,
        int attempt,
        TimeSpan elapsed,
        CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync(healthUrl, ct);
            return HandleGatewayResponse(resp, healthUrl, attempt, elapsed);
        }
        catch (HttpRequestException ex)
        {
            LogGatewayException(ex, attempt, elapsed, isHttpException: true);
            return false;
        }
        catch (Exception ex)
        {
            LogGatewayException(ex, attempt, elapsed, isHttpException: false);
            return false;
        }
    }

    private static bool HandleGatewayResponse(HttpResponseMessage resp, string healthUrl, int attempt, TimeSpan elapsed)
    {
        if ((int)resp.StatusCode >= 200 && (int)resp.StatusCode < 500)
        {
            TestContext.WriteLine($"✅ [GatewayReady] Gateway ready at {healthUrl} after {attempt} attempt(s), {elapsed.TotalSeconds:F1}s");
            return true;
        }

        if (attempt % 10 == 0)
        {
            TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: HTTP {resp.StatusCode} (elapsed: {elapsed.TotalSeconds:F1}s)");
        }

        return false;
    }

    private static void LogGatewayException(Exception ex, int attempt, TimeSpan elapsed, bool isHttpException)
    {
        if (attempt % 10 != 0)
            return;

        if (isHttpException)
        {
            TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: {ex.GetType().Name} (elapsed: {elapsed.TotalSeconds:F1}s)");
        }
        else
        {
            TestContext.WriteLine($"⏳ [GatewayReady] Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void ThrowGatewayTimeoutException(string healthUrl, TimeSpan timeout, int attempt, TimeSpan elapsed)
    {
        TestContext.WriteLine($"❌ [GatewayReady] Gateway failed to start after {attempt} attempts over {elapsed.TotalSeconds:F1}s");
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
    protected static async Task WaitForFullInfrastructureAsync(bool includeGateway = true, CancellationToken cancellationToken = default)
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
        // For per-test validation, we don't require free slots since previous jobs may still be running
        // Free slots are only required during initial global infrastructure setup
        await WaitForFlinkReadyAsync($"{flinkJobManagerEndpoint}v1/overview", FlinkReadyTimeout, cancellationToken, requireFreeSlots: false);
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
            var flinkContainers = await RunDockerCommandAsync("ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"");
            TestContext.WriteLine($"🔍 Flink JobManager port mappings: {flinkContainers.Trim()}");
            
            return ExtractFlinkEndpointFromPorts(flinkContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Flink JobManager endpoint: {ex.Message}", ex);
        }
    }

    private static string ExtractFlinkEndpointFromPorts(string flinkContainers)
    {
        var lines = flinkContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var endpoint = TryExtractPortFromLine(line);
            if (endpoint != null)
                return endpoint;
        }

        throw new InvalidOperationException($"Could not determine Flink JobManager endpoint from Docker ports: {flinkContainers}");
    }

    private static string? TryExtractPortFromLine(string line)
    {
        if (!line.Contains("->8081/tcp"))
            return null;

        var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->8081");
        return match.Success ? $"http://localhost:{match.Groups[1].Value}/" : null;
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
            var gatewayContainers = await RunDockerCommandAsync("ps --filter \"name=gateway\" --format \"{{.Ports}}\"");
            
            if (!string.IsNullOrWhiteSpace(gatewayContainers))
            {
                var endpoint = TryExtractGatewayContainerEndpoint(gatewayContainers);
                if (endpoint != null)
                    return endpoint;
            }
            
            return GetDefaultGatewayEndpoint();
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Gateway endpoint discovery failed: {ex.Message}, using configured port {Ports.GatewayHostPort}");
            return $"http://localhost:{Ports.GatewayHostPort}/";
        }
    }

    private static string? TryExtractGatewayContainerEndpoint(string gatewayContainers)
    {
        TestContext.WriteLine($"🔍 Gateway container port mappings: {gatewayContainers.Trim()}");
        
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
        return null;
    }

    private static string GetDefaultGatewayEndpoint()
    {
        TestContext.WriteLine($"ℹ️ Gateway running as .NET project (not containerized), using configured port {Ports.GatewayHostPort}");
        TestContext.WriteLine($"💡 Gateway may take 15-30 seconds to start after Flink is ready");
        return $"http://localhost:{Ports.GatewayHostPort}/";
    }
}
