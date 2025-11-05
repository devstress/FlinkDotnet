using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Partial class containing diagnostic and logging methods for integration tests.
/// This file contains methods for capturing logs, diagnostics, and debugging information from Flink and other infrastructure.
/// </summary>
public abstract partial class LocalTestingTestBase
{
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

        var match = Regex.Match(line, @"127\.0\.0\.1:(\d+)->8081");
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
            var jmName = flinkContainers.Find(name => name.Contains("flink-jobmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

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
            var tmName = flinkContainers.Find(name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

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
            var tmName = Array.Find(containers, name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

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
