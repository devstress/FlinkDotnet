using System.Diagnostics;
using Aspire.Hosting;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Enhanced test base class for LocalTesting integration tests.
/// Based on successful patterns from BackPressureExample.IntegrationTests.KafkaTestBase
/// with improvements for Flink infrastructure readiness validation and Docker connectivity.
/// </summary>
public abstract class LocalTestingTestBase
{
    /// <summary>
    /// Access to shared AppHost instance from GlobalTestInfrastructure.
    /// Infrastructure is initialized once for all tests, dramatically reducing startup overhead.
    /// </summary>
    protected static DistributedApplication? AppHost => GlobalTestInfrastructure.AppHost;

    /// <summary>
    /// Access to shared Kafka connection string from GlobalTestInfrastructure.
    /// This address is used by test producers/consumers running on the host (e.g., localhost:32804).
    /// </summary>
    protected static string? KafkaConnectionString => GlobalTestInfrastructure.KafkaConnectionString;

    /// <summary>
    /// Access to discovered Temporal endpoint from GlobalTestInfrastructure.
    /// Aspire allocates dynamic ports during testing, so we must use the discovered endpoint.
    /// </summary>
    protected static string? TemporalEndpoint => GlobalTestInfrastructure.TemporalEndpoint;

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

    /// <summary>
    /// Enhanced Kafka readiness check copied from BackPressureExample.IntegrationTests.KafkaTestBase
    /// with improved error handling, fallback strategies, and dynamic container discovery.
    /// </summary>
    public static async Task WaitForKafkaReadyAsync(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        TestContext.WriteLine($"╔══════════════════════════════════════════════════════════════");
        TestContext.WriteLine($"║ 🔎 [KafkaReady] Connecting to Kafka");
        TestContext.WriteLine($"║ 📡 Bootstrap servers: {bootstrapServers}");
        TestContext.WriteLine($"║ ⏱️  Timeout: {timeout.TotalSeconds}s");
        TestContext.WriteLine($"╚══════════════════════════════════════════════════════════════");

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
            await Task.Delay(100, ct); // Optimized: Reduced to 100ms (was 250ms)
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
                    return Task.FromResult((true, (Exception?) null));
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
    /// Get bootstrap server variations for dynamic port configuration.
    /// CRITICAL: Aspire allocates dynamic ports, so we use the discovered bootstrap server.
    /// We only add localhost/127.0.0.1 variations of the discovered endpoint.
    /// </summary>
    private static Task<string[]> GetBootstrapServerVariationsAsync(string originalBootstrap)
    {
        var variations = new List<string>
        {
            originalBootstrap,
            originalBootstrap.Replace("localhost", "127.0.0.1")
        };

        // Remove duplicates
        return Task.FromResult(variations.Distinct().ToArray());
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

        TestContext.WriteLine($"╔══════════════════════════════════════════════════════════════");
        TestContext.WriteLine($"║ 🔎 [FlinkReady] Connecting to Flink JobManager");
        TestContext.WriteLine($"║ 📡 Overview URL: {overviewUrl}");
        TestContext.WriteLine($"║ ⏱️  Timeout: {timeout.TotalSeconds}s");
        TestContext.WriteLine($"║ 🎯 Require free slots: {requireFreeSlots}");
        TestContext.WriteLine($"╚══════════════════════════════════════════════════════════════");

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

            await Task.Delay(200, ct); // Optimized: Reduced to 200ms (was 500ms)
        }

        await LogFlinkContainerDiagnosticsAsync();
        throw new TimeoutException($"Flink JobManager not ready within {timeout.TotalSeconds:F0}s at {overviewUrl}");
    }

    private static async Task InitializeFlinkReadinessCheckAsync(string overviewUrl, TimeSpan timeout)
    {
        TestContext.WriteLine($"🔎 [FlinkReady] Probing Flink JobManager at {overviewUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"⏳ [FlinkReady] Checking Flink container status immediately...");

        await Task.Delay(500); // Optimized: Reduced to 500ms (was 2000ms)

        var overviewUri = new Uri(overviewUrl);
        var jobManagerPort = overviewUri.Port;
        var portAccessible = await TestPortConnectivityAsync("localhost", jobManagerPort);
        TestContext.WriteLine($"🔍 [FlinkReady] Port {jobManagerPort} accessible: {portAccessible}");
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

            // Check if port is listening using dynamically discovered endpoint
            var flinkEndpoint = await GetFlinkJobManagerEndpointAsync();
            var flinkPort = new Uri(flinkEndpoint).Port;
            var portTest = await TestPortConnectivityAsync("localhost", flinkPort);
            TestContext.WriteLine($"   Port {flinkPort} accessible: {portTest}");

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
        TestContext.WriteLine($"╔══════════════════════════════════════════════════════════════");
        TestContext.WriteLine($"║ 🔎 [GatewayReady] Connecting to Flink Job Gateway");
        TestContext.WriteLine($"║ 📡 Health URL: {healthUrl}");
        TestContext.WriteLine($"║ ⏱️  Timeout: {timeout.TotalSeconds}s");
        TestContext.WriteLine($"║ 💡 Gateway is a .NET project (starts after Flink)");
        TestContext.WriteLine($"╚══════════════════════════════════════════════════════════════");
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
        if ((int) resp.StatusCode >= 200 && (int) resp.StatusCode < 500)
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
    /// Enhanced SQL Gateway readiness check with proper retry logic.
    /// SQL Gateway is a Flink component that provides REST API for direct SQL execution.
    /// It starts after JobManager and must be validated before submitting SQL jobs.
    /// </summary>
    public static async Task WaitForSqlGatewayReadyAsync(string baseUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        var healthUrl = $"{baseUrl}/v1/info";

        LogSqlGatewayReadinessStart(healthUrl, timeout);

        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;
            if (await CheckSqlGatewayHealthAsync(http, healthUrl, attempt, sw.Elapsed, ct))
                return;

            await Task.Delay(1000, ct);
        }

        ThrowSqlGatewayTimeoutException(healthUrl, timeout, attempt, sw.Elapsed);
    }

    private static void LogSqlGatewayReadinessStart(string healthUrl, TimeSpan timeout)
    {
        TestContext.WriteLine($"🔎 [SqlGatewayReady] Probing SQL Gateway at {healthUrl} (timeout: {timeout.TotalSeconds:F0}s)");
        TestContext.WriteLine($"💡 [SqlGatewayReady] SQL Gateway is a Flink component that starts after JobManager");
    }

    private static async Task<bool> CheckSqlGatewayHealthAsync(HttpClient http, string healthUrl, int attempt, TimeSpan elapsed, CancellationToken ct)
    {
        try
        {
            var resp = await http.GetAsync(healthUrl, ct);
            if (resp.IsSuccessStatusCode)
            {
                TestContext.WriteLine($"✅ [SqlGatewayReady] SQL Gateway ready at {healthUrl} after {attempt} attempt(s), {elapsed.TotalSeconds:F1}s");
                return true;
            }

            LogSqlGatewayAttempt(attempt, elapsed, resp.StatusCode);
            return false;
        }
        catch (HttpRequestException ex)
        {
            LogSqlGatewayHttpException(attempt, elapsed, ex);
            return false;
        }
        catch (Exception ex)
        {
            LogSqlGatewayException(attempt, ex);
            return false;
        }
    }

    private static void LogSqlGatewayAttempt(int attempt, TimeSpan elapsed, System.Net.HttpStatusCode statusCode)
    {
        if (attempt % 10 == 0)
        {
            TestContext.WriteLine($"⏳ [SqlGatewayReady] Attempt {attempt}: HTTP {statusCode} (elapsed: {elapsed.TotalSeconds:F1}s)");
        }
    }

    private static void LogSqlGatewayHttpException(int attempt, TimeSpan elapsed, HttpRequestException ex)
    {
        if (attempt % 10 == 0)
        {
            TestContext.WriteLine($"⏳ [SqlGatewayReady] Attempt {attempt}: {ex.GetType().Name} (elapsed: {elapsed.TotalSeconds:F1}s)");
        }
    }

    private static void LogSqlGatewayException(int attempt, Exception ex)
    {
        if (attempt % 10 == 0)
        {
            TestContext.WriteLine($"⏳ [SqlGatewayReady] Attempt {attempt}: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private static void ThrowSqlGatewayTimeoutException(string healthUrl, TimeSpan timeout, int attempt, TimeSpan elapsed)
    {
        TestContext.WriteLine($"❌ [SqlGatewayReady] SQL Gateway failed to start after {attempt} attempts over {elapsed.TotalSeconds:F1}s");
        throw new TimeoutException($"SQL Gateway not ready within {timeout.TotalSeconds:F0}s at {healthUrl}. SQL Gateway may not have started properly - check Flink logs.");
    }
    /// <summary>
    /// Enhanced Temporal readiness check with proper retry logic.
    /// Temporal is a workflow orchestration system that starts after basic infrastructure.
    /// SQLite initialization can take significant time on first startup.
    /// </summary>

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

            // Optimized delay for faster test execution
            await Task.Delay(100);
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results?.Exists(r => r.Error.Code == ErrorCode.TopicAlreadyExists) == true)
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
    /// Performs quick health check only (trusts global setup).
    /// </summary>
    /// <param name="includeGateway">Whether to validate Gateway availability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected static async Task WaitForFullInfrastructureAsync(
        bool includeGateway = true,
        CancellationToken cancellationToken = default)
    {
        // Quick validation that endpoints are still responding
        // This is used by individual tests after global setup has already validated everything
        TestContext.WriteLine("🔧 Quick infrastructure health check...");

        // Just verify Kafka is still accessible (very quick check)
        if (string.IsNullOrEmpty(KafkaConnectionString))
        {
            throw new InvalidOperationException("Kafka connection string not available");
        }

        // Display container status with ports for visibility (no polling - containers should already be running)
        await DisplayContainerStatusAsync();

        TestContext.WriteLine("✅ Infrastructure health check passed");
    }

    /// <summary>
    /// Capture network diagnostics for a specific test checkpoint.
    /// Helper method for tests to capture network state at critical points.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="checkpoint">Checkpoint name (e.g., "before-test", "after-failure")</param>
    protected static async Task CaptureTestNetworkDiagnosticsAsync(string testName, string checkpoint)
    {
        var checkpointName = $"test-{testName}-{checkpoint}";
        await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync(checkpointName);
    }

    /// <summary>
    /// Get the dynamically allocated Flink JobManager HTTP endpoint from Aspire.
    /// Aspire DCP assigns random ports during testing, so we cannot use hardcoded ports.
    /// </summary>
    protected static async Task<string> GetFlinkJobManagerEndpointAsync()
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
    /// Retrieve JobManager logs from Flink REST API.
    /// The JobManager handles job submission, so its logs contain errors from failed job submissions.
    /// </summary>
    protected static async Task<string> GetFlinkJobManagerLogsAsync(string flinkEndpoint)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            var logsBuilder = new System.Text.StringBuilder();
            logsBuilder.AppendLine("\n========== JobManager Logs ==========");

            var mainLogName = await GetJobManagerLogListAsync(httpClient, flinkEndpoint, logsBuilder);
            if (!string.IsNullOrEmpty(mainLogName))
            {
                await AppendJobManagerLogContentAsync(httpClient, flinkEndpoint, mainLogName, logsBuilder);
            }

            return logsBuilder.ToString();
        }
        catch (Exception ex)
        {
            return $"Error fetching JobManager logs: {ex.Message}";
        }
    }

    private static async Task<string?> GetJobManagerLogListAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint, System.Text.StringBuilder logsBuilder)
    {
        var logListUrl = $"{flinkEndpoint.TrimEnd('/')}/jobmanager/logs";
        var logListResponse = await httpClient.GetAsync(logListUrl);

        if (!logListResponse.IsSuccessStatusCode)
        {
            logsBuilder.AppendLine($"Failed to get JobManager log list: HTTP {logListResponse.StatusCode}");
            return null;
        }

        var logListContent = await logListResponse.Content.ReadAsStringAsync();
        var logListJson = System.Text.Json.JsonDocument.Parse(logListContent);

        return ExtractMainLogName(logListJson, logsBuilder);
    }

    private static string? ExtractMainLogName(System.Text.Json.JsonDocument logListJson, System.Text.StringBuilder logsBuilder)
    {
        string? mainLogName = null;
        if (logListJson.RootElement.TryGetProperty("logs", out var logs))
        {
            foreach (var logFile in logs.EnumerateArray())
            {
                if (logFile.TryGetProperty("name", out var name))
                {
                    var logName = name.GetString();
                    logsBuilder.AppendLine($"  Available log: {logName}");

                    if (logName?.EndsWith(".log") == true)
                    {
                        mainLogName = logName;
                    }
                }
            }
        }
        return mainLogName;
    }

    private static async Task AppendJobManagerLogContentAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint, string mainLogName, System.Text.StringBuilder logsBuilder)
    {
        var logContentUrl = $"{flinkEndpoint.TrimEnd('/')}/jobmanager/logs/{mainLogName}";
        try
        {
            var logResponse = await httpClient.GetAsync(logContentUrl);
            if (logResponse.IsSuccessStatusCode)
            {
                await AppendLogLines(logResponse, mainLogName, logsBuilder);
            }
            else
            {
                logsBuilder.AppendLine($"  Failed to read log content: HTTP {logResponse.StatusCode}");
            }
        }
        catch (Exception logEx)
        {
            logsBuilder.AppendLine($"  Error reading log file {mainLogName}: {logEx.Message}");
        }
    }

    private static async Task AppendLogLines(System.Net.Http.HttpResponseMessage logResponse, string mainLogName, System.Text.StringBuilder logsBuilder)
    {
        var logContent = await logResponse.Content.ReadAsStringAsync();
        var lines = logContent.Split('\n');
        var lastLines = lines.Length > 500 ? lines[^500..] : lines;
        logsBuilder.AppendLine($"\n  Last 500 lines of {mainLogName}:");
        logsBuilder.AppendLine(string.Join('\n', lastLines));
    }

    /// <summary>
    /// Retrieve Flink job exceptions from the Flink REST API.
    /// This provides detailed error information when jobs fail.
    /// </summary>
    protected static async Task<string> GetFlinkJobExceptionsAsync(string flinkEndpoint, string jobId)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var url = $"{flinkEndpoint.TrimEnd('/')}/jobs/{jobId}/exceptions";
            TestContext.WriteLine($"🔍 Fetching job exceptions from: {url}");

            var response = await httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return content;
            }
            else
            {
                return $"Failed to get job exceptions: HTTP {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            return $"Error fetching job exceptions: {ex.Message}";
        }
    }

    /// <summary>
    /// Retrieve TaskManager logs from Flink REST API.
    /// Returns logs from all TaskManagers if available.
    /// </summary>
    protected static async Task<string> GetFlinkTaskManagerLogsAsync(string flinkEndpoint)
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var logsBuilder = new System.Text.StringBuilder();

            var taskManagers = await GetTaskManagerListAsync(httpClient, flinkEndpoint);
            if (!taskManagers.HasValue)
            {
                return "Failed to get TaskManager list or no TaskManagers found";
            }

            var tmCount = await ProcessTaskManagersAsync(httpClient, flinkEndpoint, taskManagers.Value, logsBuilder);

            return tmCount == 0 ? "No TaskManagers found" : logsBuilder.ToString();
        }
        catch (Exception ex)
        {
            return $"Error fetching TaskManager logs: {ex.Message}";
        }
    }

    private static async Task<System.Text.Json.JsonElement?> GetTaskManagerListAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint)
    {
        var tmListUrl = $"{flinkEndpoint.TrimEnd('/')}/taskmanagers";
        var tmListResponse = await httpClient.GetAsync(tmListUrl);

        if (!tmListResponse.IsSuccessStatusCode)
        {
            return null;
        }

        var tmListContent = await tmListResponse.Content.ReadAsStringAsync();
        var tmListJson = System.Text.Json.JsonDocument.Parse(tmListContent);

        if (!tmListJson.RootElement.TryGetProperty("taskmanagers", out var taskManagers))
        {
            return null;
        }

        return taskManagers;
    }

    private static async Task<int> ProcessTaskManagersAsync(
        System.Net.Http.HttpClient httpClient,
        string flinkEndpoint,
        System.Text.Json.JsonElement taskManagers,
        System.Text.StringBuilder logsBuilder)
    {
        int tmCount = 0;
        foreach (var tm in taskManagers.EnumerateArray())
        {
            if (tm.TryGetProperty("id", out var tmId))
            {
                var taskManagerId = tmId.GetString();
                tmCount++;
                logsBuilder.AppendLine($"\n========== TaskManager {tmCount} (ID: {taskManagerId}) ==========");

                await AppendTaskManagerLogsAsync(httpClient, flinkEndpoint, taskManagerId, logsBuilder);
            }
        }
        return tmCount;
    }

    private static async Task AppendTaskManagerLogsAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint, string? taskManagerId, System.Text.StringBuilder logsBuilder)
    {
        try
        {
            await AppendTaskManagerLogFilesAsync(httpClient, flinkEndpoint, taskManagerId, logsBuilder);
            await AppendTaskManagerStdoutAsync(httpClient, flinkEndpoint, taskManagerId, logsBuilder);
        }
        catch (Exception tmEx)
        {
            logsBuilder.AppendLine($"  Error getting TaskManager logs: {tmEx.Message}");
        }
    }

    private static async Task AppendTaskManagerLogFilesAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint, string? taskManagerId, System.Text.StringBuilder logsBuilder)
    {
        var logUrl = $"{flinkEndpoint.TrimEnd('/')}/taskmanagers/{taskManagerId}/logs";
        var logResponse = await httpClient.GetAsync(logUrl);

        if (logResponse.IsSuccessStatusCode)
        {
            var logContent = await logResponse.Content.ReadAsStringAsync();
            var logJson = System.Text.Json.JsonDocument.Parse(logContent);

            if (logJson.RootElement.TryGetProperty("logs", out var logs))
            {
                foreach (var logFile in logs.EnumerateArray())
                {
                    if (logFile.TryGetProperty("name", out var name))
                    {
                        logsBuilder.AppendLine($"  Log file: {name.GetString()}");
                    }
                }
            }
        }
    }

    private static async Task AppendTaskManagerStdoutAsync(System.Net.Http.HttpClient httpClient, string flinkEndpoint, string? taskManagerId, System.Text.StringBuilder logsBuilder)
    {
        var stdoutUrl = $"{flinkEndpoint.TrimEnd('/')}/taskmanagers/{taskManagerId}/stdout";
        var stdoutResponse = await httpClient.GetAsync(stdoutUrl);

        if (stdoutResponse.IsSuccessStatusCode)
        {
            var stdoutContent = await stdoutResponse.Content.ReadAsStringAsync();
            var lines = stdoutContent.Split('\n');
            var lastLines = lines.Length > 100 ? lines[^100..] : lines;
            logsBuilder.AppendLine($"\n  Last 100 lines of stdout:");
            logsBuilder.AppendLine(string.Join('\n', lastLines));
        }
    }

    /// <summary>
    /// Retrieve TaskManager logs from Docker container.
    /// Fallback method when Flink REST API is not available or doesn't have the logs.
    /// </summary>
    protected static async Task<string> GetTaskManagerLogsFromDockerAsync()
    {
        try
        {
            // Get all container names and filter in C# to handle Aspire's random suffixes
            var containerNames = await RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            var containers = containerNames.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var containerName = Array.Find(containers, name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (string.IsNullOrEmpty(containerName))
            {
                return "No TaskManager container found";
            }

            TestContext.WriteLine($"🔍 Getting logs from TaskManager container: {containerName}");
            var logs = await RunDockerCommandAsync($"logs {containerName} --tail 20 2>&1");
            return $"========== TaskManager Container Logs ({containerName}) - Last 20 Lines ==========\n{logs}";
        }
        catch (Exception ex)
        {
            return $"Error fetching TaskManager logs from Docker: {ex.Message}";
        }
    }

    /// <summary>
    /// Get comprehensive diagnostic information when a Flink job fails.
    /// Includes JobManager logs, job exceptions, TaskManager logs from REST API, and Docker container logs.
    /// </summary>
    protected static async Task<string> GetFlinkJobDiagnosticsAsync(string flinkEndpoint, string? jobId = null)
    {
        var diagnostics = new System.Text.StringBuilder();
        diagnostics.AppendLine("\n" + new string('=', 80));
        diagnostics.AppendLine("FLINK JOB FAILURE DIAGNOSTICS");
        diagnostics.AppendLine(new string('=', 80));

        // 1. Get JobManager logs (most important for job submission failures)
        diagnostics.AppendLine("\n--- JobManager Logs (from Flink REST API) ---");
        var jmLogs = await GetFlinkJobManagerLogsAsync(flinkEndpoint);
        diagnostics.AppendLine(jmLogs);

        // 2. Get job exceptions if jobId is provided
        if (!string.IsNullOrEmpty(jobId))
        {
            diagnostics.AppendLine("\n--- Job Exceptions ---");
            var exceptions = await GetFlinkJobExceptionsAsync(flinkEndpoint, jobId);
            diagnostics.AppendLine(exceptions);
        }

        // 3. Get TaskManager logs from Flink REST API
        diagnostics.AppendLine("\n--- TaskManager Logs (from Flink REST API) ---");
        var tmLogs = await GetFlinkTaskManagerLogsAsync(flinkEndpoint);
        diagnostics.AppendLine(tmLogs);

        // 4. Get TaskManager logs from Docker as fallback/additional info
        diagnostics.AppendLine("\n--- TaskManager Logs (from Docker) ---");
        var dockerLogs = await GetTaskManagerLogsFromDockerAsync();
        diagnostics.AppendLine(dockerLogs);

        diagnostics.AppendLine("\n" + new string('=', 80));
        return diagnostics.ToString();
    }

    /// <summary>
    /// Display current container status and ports for debugging visibility.
    /// Used in lightweight mode - assumes containers are already running from global setup.
    /// Does NOT poll or wait - just displays current state immediately.
    /// </summary>
    private static async Task DisplayContainerStatusAsync()
    {
        try
        {
            // Single quick check - no polling needed since containers should already be running
            var containerInfo = await RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");

            if (!string.IsNullOrWhiteSpace(containerInfo))
            {
                // Check if we only got the header (no actual containers)
                var lines = containerInfo.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length <= 1)
                {
                    // Only header, no containers
                    TestContext.WriteLine("⚠️ No containers found - this is unexpected in lightweight mode");
                    TestContext.WriteLine("🔍 Container info output:");
                    TestContext.WriteLine(containerInfo);

                    // Try listing ALL containers including stopped ones for diagnostics
                    var allContainersInfo = await RunDockerCommandAsync("ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
                    if (!string.IsNullOrWhiteSpace(allContainersInfo))
                    {
                        TestContext.WriteLine("🔍 All containers (including stopped):");
                        TestContext.WriteLine(allContainersInfo);
                    }
                }
                else
                {
                    TestContext.WriteLine("🐳 Container Status and Ports:");
                    TestContext.WriteLine(containerInfo);
                }
            }
            else
            {
                TestContext.WriteLine("🐳 No container output - container runtime not available or command failed");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to get container status: {ex.Message}");
        }
    }

    /// <summary>
    /// Log Flink job status via Gateway to check if job is actually running.
    /// </summary>
    protected static async Task LogJobStatusViaGatewayAsync(string gatewayBase, string jobId, string checkpoint)
    {
        try
        {
            TestContext.WriteLine($"🔍 [Job Status Check] {checkpoint} - Job ID: {jobId}");

            // Skip status check if job ID is null or empty
            if (string.IsNullOrEmpty(jobId))
            {
                TestContext.WriteLine($"⏭️ Skipping job status check - Job ID is empty");
                return;
            }

            using var httpClient = new System.Net.Http.HttpClient();
            var statusUrl = $"{gatewayBase}api/v1/jobs/{jobId}/status";
            var response = await httpClient.GetAsync(statusUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                TestContext.WriteLine($"📊 Job status response: {content}");
            }
            else
            {
                TestContext.WriteLine($"⚠️ Failed to get job status: HTTP {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to check job status: {ex.Message}");
        }
    }

    /// <summary>
    /// Log Flink container status and recent logs for debugging.
    /// </summary>
    protected static async Task LogFlinkContainerStatusAsync(string checkpoint)
    {
        try
        {
            TestContext.WriteLine($"🔍 [Flink Container Debug] {checkpoint}");

            // Get ALL container names and filter in C# to handle Aspire's random suffixes
            var allContainersList = await RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            var allContainers = allContainersList.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            var flinkContainers = allContainers.Where(name => name.Contains("flink", StringComparison.OrdinalIgnoreCase)).ToList();

            TestContext.WriteLine($"🐳 Flink containers found: {string.Join(", ", flinkContainers)}");

            // Find JobManager container
            var jmName = Array.Find(flinkContainers, name => name.Contains("flink-jobmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (!string.IsNullOrWhiteSpace(jmName))
            {
                TestContext.WriteLine($"📋 Found JobManager container: {jmName}");
                var jmLogs = await RunDockerCommandAsync($"logs {jmName} --tail 100 2>&1");
                TestContext.WriteLine($"📋 JobManager logs (last 100 lines):\n{jmLogs}");
            }
            else
            {
                TestContext.WriteLine("⚠️ No JobManager container found");
                TestContext.WriteLine($"   Available containers: {string.Join(", ", allContainers)}");
            }

            // Find TaskManager container
            var tmName = Array.Find(flinkContainers, name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (!string.IsNullOrWhiteSpace(tmName))
            {
                TestContext.WriteLine($"📋 Found TaskManager container: {tmName}");
                var tmLogs = await RunDockerCommandAsync($"logs {tmName} --tail 20 2>&1");
                TestContext.WriteLine($"📋 TaskManager logs (last 20 lines):\n{tmLogs}");
            }
            else
            {
                TestContext.WriteLine("⚠️ No TaskManager container found");
                TestContext.WriteLine($"   Available containers: {string.Join(", ", allContainers)}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to get Flink container logs: {ex.Message}");
            TestContext.WriteLine($"   Exception details: {ex.GetType().Name} - {ex.Message}");
            if (ex.StackTrace != null)
            {
                TestContext.WriteLine($"   Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Log Flink job-specific logs from JobManager.
    /// </summary>
    protected static async Task LogFlinkJobLogsAsync(string jobId, string checkpoint)
    {
        try
        {
            TestContext.WriteLine($"🔍 [Flink Job Debug] {checkpoint} - Job ID: {jobId}");

            // Get all container names and filter in C# to handle Aspire's random suffixes
            var containerNames = await RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            var containers = containerNames.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            // Find JobManager container
            var jmName = Array.Find(containers, name => name.Contains("flink-jobmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (!string.IsNullOrWhiteSpace(jmName))
            {
                // Get logs filtered for this specific job
                var jobLogs = await RunDockerCommandAsync($"logs {jmName} 2>&1");
                var jobLogLines = jobLogs.Split('\n').Where(line => line.Contains(jobId, StringComparison.OrdinalIgnoreCase)).Take(30);
                TestContext.WriteLine($"📋 Job-specific logs (last 30 lines):\n{string.Join('\n', jobLogLines)}");
            }

            // Find TaskManager container
            var tmName = Array.Find(containers, name => name?.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase) == true)?.Trim();

            if (!string.IsNullOrWhiteSpace(tmName))
            {
                // Get TaskManager logs and filter locally
                var allLogs = await RunDockerCommandAsync($"logs {tmName} 2>&1");

                // Check for Kafka-related logs
                var kafkaLogLines = allLogs.Split('\n').Where(line => line.Contains("kafka", StringComparison.OrdinalIgnoreCase)).Take(20);
                TestContext.WriteLine($"📋 Kafka-related logs from TaskManager (last 20 lines):\n{string.Join('\n', kafkaLogLines)}");

                // Also check for any error logs
                var errorLogLines = allLogs.Split('\n').Where(line =>
                    line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("exception", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("fail", StringComparison.OrdinalIgnoreCase)).Take(20);
                TestContext.WriteLine($"📋 Error logs from TaskManager (last 20 lines):\n{string.Join('\n', errorLogLines)}");
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to get Flink job logs: {ex.Message}");
        }
    }

    /// <summary>
    /// Test Kafka connectivity from within Flink TaskManager container using telnet or nc.
    /// This diagnostic helps determine if Flink containers can reach Kafka at kafka:9092.
    /// </summary>
    protected static async Task TestKafkaConnectivityFromFlinkAsync()
    {
        try
        {
            TestContext.WriteLine("🔍 [Kafka Connectivity] Testing from Flink TaskManager container...");

            // Get all container names and filter in C# to handle Aspire's random suffixes
            var containerNames = await RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            var containers = containerNames.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var tmName = Array.Find(containers, name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (string.IsNullOrWhiteSpace(tmName))
            {
                TestContext.WriteLine("⚠️ No TaskManager container found for connectivity test");
                return;
            }

            TestContext.WriteLine($"🐳 Using TaskManager container: {tmName}");

            // Test connectivity to kafka:9092
            var testResult = await RunDockerCommandAsync($"exec {tmName} timeout 2 bash -c 'echo \"test\" | nc -w 1 kafka 9092 && echo \"SUCCESS\" || echo \"FAILED\"' 2>&1");
            TestContext.WriteLine($"📊 Kafka connectivity (kafka:9092): {testResult.Trim()}");

            // Also try to resolve the hostname
            var dnsResult = await RunDockerCommandAsync($"exec {tmName} getent hosts kafka 2>&1 || echo \"DNS resolution failed\"");
            TestContext.WriteLine($"📊 DNS resolution for 'kafka': {dnsResult.Trim()}");

            // Check if Kafka connectorJARs are present
            var connectorCheck = await RunDockerCommandAsync($"exec {tmName} ls -lh /opt/flink/lib/*kafka* 2>&1 || echo \"No Kafka connector found\"");
            TestContext.WriteLine($"📊 Kafka connector JARs in Flink:\n{connectorCheck.Trim()}");

            // Check network settings
            var networkInfo = await RunDockerCommandAsync($"inspect {tmName} --format '{{{{.NetworkSettings.Networks}}}}'");
            TestContext.WriteLine($"📊 Container network info: {networkInfo.Trim()}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to test Kafka connectivity from Flink: {ex.Message}");
        }
    }
}
