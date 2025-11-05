using System.Diagnostics;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Partial class containing infrastructure readiness validation methods.
/// This file contains all WaitFor*ReadyAsync methods for checking Kafka, Flink, Gateway, SQL Gateway, and Temporal readiness.
/// </summary>
public abstract partial class LocalTestingTestBase
{
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
    public static async Task WaitForTemporalReadyAsync(string address, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        Exception? lastException = null;

        LogTemporalReadinessStart(address, timeout);

        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            attempt++;

            var (success, exception) = await TryConnectToTemporalAsync(address, attempt, sw.Elapsed);
            if (success)
            {
                return;
            }

            lastException = exception;
            await LogTemporalConnectionAttemptAsync(attempt, sw.Elapsed, lastException);
            await Task.Delay(1000, ct);
        }

        throw CreateTemporalTimeoutException(address, timeout, attempt, sw.Elapsed, lastException);
    }

    private static void LogTemporalReadinessStart(string address, TimeSpan timeout)
    {
        TestContext.WriteLine($"╔══════════════════════════════════════════════════════════════");
        TestContext.WriteLine($"║ 🔎 [TemporalReady] Connecting to Temporal Server");
        TestContext.WriteLine($"║ 📡 Address: {address}");
        TestContext.WriteLine($"║ ⏱️  Timeout: {timeout.TotalSeconds}s");
        TestContext.WriteLine($"║ ℹ️  PostgreSQL initialization may take 30-60s on first start");
        TestContext.WriteLine($"╚══════════════════════════════════════════════════════════════");
    }

    private static async Task<(bool success, Exception? exception)> TryConnectToTemporalAsync(string address, int attempt, TimeSpan elapsed)
    {
        try
        {
            var client = await Temporalio.Client.TemporalClient.ConnectAsync(new Temporalio.Client.TemporalClientConnectOptions
            {
                TargetHost = address,
                Namespace = "default",
            });

            if (client.Connection != null)
            {
                TestContext.WriteLine($"✅ [TemporalReady] Temporal ready at {address} after {attempt} attempt(s), {elapsed.TotalSeconds:F1}s");
                return (true, null);
            }

            return (false, null);
        }
        catch (Exception ex)
        {
            return (false, ex);
        }
    }

    private static Task LogTemporalConnectionAttemptAsync(int attempt, TimeSpan elapsed, Exception? lastException)
    {
        if (attempt % 10 == 0 || (attempt % 30 == 0 && elapsed.TotalSeconds >= 30))
        {
            if (lastException != null)
            {
                TestContext.WriteLine($"⏳ [TemporalReady] Attempt {attempt} ({elapsed.TotalSeconds:F0}s elapsed): {lastException.GetType().Name}");
            }

            if (elapsed.TotalSeconds >= 30 && attempt % 30 == 0)
            {
                TestContext.WriteLine($"   💡 Temporal PostgreSQL initialization can be slow - this is normal for first startup");
            }
        }

        return Task.CompletedTask;
    }

    private static TimeoutException CreateTemporalTimeoutException(string address, TimeSpan timeout, int attempt, TimeSpan elapsed, Exception? lastException)
    {
        var errorMessage = $"Temporal not ready within {timeout.TotalSeconds:F0}s at {address}. " +
                          $"Attempted {attempt} times over {elapsed.TotalSeconds:F1}s.";

        if (lastException != null)
        {
            errorMessage += $" Last error: {lastException.GetType().Name} - {lastException.Message}";
        }

        return new TimeoutException(errorMessage);
    }

}
