using System.Diagnostics;
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
    private static readonly SemaphoreSlim _setupSemaphore = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan AppHostStartupTimeout = TimeSpan.FromSeconds(45);
    private static readonly string AppHostPath = Path.Combine(
        FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root"),
        "LocalTesting", "LocalTesting.FlinkSqlAppHost");
    
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
    /// Start LocalTesting AppHost once for all tests.
    /// Called by each test assembly's SetUpFixture.
    /// Idempotent - safe to call multiple times (will only setup once).
    /// </summary>
    public static async Task GlobalSetUp()
    {
        await _setupSemaphore.WaitAsync();
        try
        {
            if (_isSetupComplete)
            {
                TestContext.WriteLine("✅ Infrastructure already set up, skipping...");
                return;
            }
            
            TestContext.WriteLine("🚀 Starting LocalTesting AppHost...");
        TestContext.WriteLine($"📁 AppHost path: {AppHostPath}");
        
        // Clean up test-logs directory from previous runs
        var repoRoot = FindRepositoryRoot() ?? throw new InvalidOperationException("Could not find repository root");
        var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
        
        if (Directory.Exists(testLogsDir))
        {
            TestContext.WriteLine($"🧹 Cleaning up test logs directory: {testLogsDir}");
            try
            {
                Directory.Delete(testLogsDir, recursive: true);
                TestContext.WriteLine("✅ Test logs directory cleaned");
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"⚠️ Warning: Could not clean test logs directory: {ex.Message}");
            }
        }
        
        // Recreate the directory
        Directory.CreateDirectory(testLogsDir);
        TestContext.WriteLine($"📁 Test logs directory ready: {testLogsDir}");
        
        // DO NOT set KAFKA_BOOTSTRAP_SERVERS here!
        // REASON: Environment variables set here are inherited by AppHost process,
        // which then passes them to all Docker containers including Flink containers.
        // This causes Kafka client inside Flink TaskManager to use localhost:9093 instead of kafka:9092.
        //
        // SOLUTION: Set KAFKA_BOOTSTRAP_SERVERS only in ExecuteExerciseAsync() so it's ONLY
        // available to exercise processes, NOT to AppHost or Flink containers.
        TestContext.WriteLine($"✅ NOT setting KAFKA_BOOTSTRAP_SERVERS globally to prevent Docker inheritance");

        _appHostProcess = StartAppHostProcess();
        
        TestContext.WriteLine("✅ AppHost process started, polling for infrastructure readiness...");
        
            await WaitForInfrastructureReadyAsync();
            
            _isSetupComplete = true;
            
            TestContext.WriteLine("✅ All infrastructure ready, tests can proceed");
        }
        finally
        {
            _setupSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Start AppHost process with output capture
    /// </summary>
    private static Process StartAppHostProcess()
    {
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

        var process = Process.Start(psi);
        
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start AppHost process");
        }

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
                TestContext.WriteLine($"[AppHost Error] {e.Data}");
            }
        };
        
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        
        return process;
    }
    
    /// <summary>
    /// Smart polling: Check if containers are actually ready instead of blind wait
    /// Ensures BOTH Kafka and Flink are ready before proceeding
    /// </summary>
    private static async Task WaitForInfrastructureReadyAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var maxWait = AppHostStartupTimeout;
        var pollInterval = TimeSpan.FromMilliseconds(500);
        
        string? kafkaFlinkIp = null;
        string? kafkaHostEndpoint = null;
        bool flinkReady = false;
        
        while (stopwatch.Elapsed < maxWait)
        {
            var discovered = await TryDiscoverEndpointsAsync(kafkaFlinkIp, kafkaHostEndpoint, stopwatch);
            kafkaFlinkIp = discovered.flinkIp ?? kafkaFlinkIp;
            kafkaHostEndpoint = discovered.hostEndpoint ?? kafkaHostEndpoint;
            
            // Also check if Flink is ready (not just Kafka)
            if (kafkaFlinkIp != null && kafkaHostEndpoint != null && !flinkReady)
            {
                flinkReady = await IsFlinkHealthyAsync();
                if (flinkReady)
                {
                    TestContext.WriteLine($"✅ Flink cluster is healthy (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                }
            }
            
            // All infrastructure must be ready: Kafka AND Flink
            if (kafkaFlinkIp != null && kafkaHostEndpoint != null && flinkReady)
            {
                KafkaFlinkBootstrapServers = kafkaFlinkIp;
                KafkaHostBootstrapServers = kafkaHostEndpoint;
                TestContext.WriteLine($"✅ All infrastructure ready after {stopwatch.Elapsed.TotalSeconds:F1}s (saved {(maxWait - stopwatch.Elapsed).TotalSeconds:F1}s)");
                return;
            }
            
            await Task.Delay(pollInterval);
        }
        
        throw new TimeoutException(
            $"Infrastructure not ready within {maxWait.TotalSeconds}s. " +
            $"KafkaFlinkIp: {KafkaFlinkBootstrapServers ?? "null"}, " +
            $"KafkaHostEndpoint: {KafkaHostBootstrapServers ?? "null"}, " +
            $"FlinkReady: {flinkReady}");
    }
    
    /// <summary>
    /// Check if Flink JobManager is healthy and ready to accept jobs
    /// </summary>
    private static async Task<bool> IsFlinkHealthyAsync()
    {
        try
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await httpClient.GetAsync("http://localhost:8080/api/v1/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
    
    /// <summary>
    /// Try to discover Kafka endpoints with logging
    /// </summary>
    private static async Task<(string? flinkIp, string? hostEndpoint)> TryDiscoverEndpointsAsync(
        string? currentFlinkIp,
        string? currentHostEndpoint,
        Stopwatch stopwatch)
    {
        try
        {
            string? flinkIp = currentFlinkIp;
            string? hostEndpoint = currentHostEndpoint;
            
            if (flinkIp == null)
            {
                flinkIp = await DockerInfrastructure.GetKafkaContainerIpAsync();
                if (flinkIp != null)
                {
                    TestContext.WriteLine($"✅ Kafka container IP discovered: {flinkIp} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                }
            }
            
            if (hostEndpoint == null)
            {
                hostEndpoint = await DockerInfrastructure.GetKafkaHostEndpointAsync();
                if (hostEndpoint != null)
                {
                    TestContext.WriteLine($"✅ Kafka host endpoint discovered: {hostEndpoint} (after {stopwatch.Elapsed.TotalSeconds:F1}s)");
                }
            }
            
            return (flinkIp, hostEndpoint);
        }
        catch
        {
            return (null, null);
        }
    }

    /// <summary>
    /// Stop LocalTesting AppHost after all tests complete.
    /// Force kills the process and manually cleans up containers.
    /// Called by each test assembly's SetUpFixture.
    /// Idempotent - safe to call multiple times (will only teardown once).
    /// </summary>
    public static void GlobalTearDown()
    {
        _setupSemaphore.Wait();
        try
        {
            if (!_isSetupComplete)
            {
                TestContext.WriteLine("✅ Infrastructure already torn down, skipping...");
                return;
            }
            _isSetupComplete = false;
            
            TestContext.WriteLine("🛑 Stopping LocalTesting AppHost...");
        
        // Cancel all Flink jobs BEFORE copying logs and stopping containers
        TestContext.WriteLine("🧹 Cancelling all running Flink jobs...");
        CancelAllFlinkJobsSync();
        
        // Copy Flink logs from Flink containers BEFORE stopping them
        // Note: FlinkJobRunner logs are embedded within Flink's logging system
        TestContext.WriteLine("📋 Copying Flink logs from Flink containers...");
        CopyFlinkLogs();
        
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
            CleanupContainers();
            
            TestContext.WriteLine("✅ Teardown complete");
        }
        finally
        {
            _setupSemaphore.Release();
        }
    }
    
    /// <summary>
    /// Copy Flink logs from Flink containers to host filesystem before containers are stopped.
    /// This includes Apache Flink logs (JobManager, TaskManager, SQL Gateway) which contain
    /// embedded FlinkJobRunner application logs since FlinkJobRunner runs as a job within Flink.
    /// </summary>
    private static void CopyFlinkLogs()
    {
        try
        {
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                TestContext.WriteLine("⚠️ Could not find repository root, skipping log copy");
                return;
            }
            
            var testLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs");
            Directory.CreateDirectory(testLogsDir);
            
            // Get all Flink container IDs (JobManager and TaskManager)
            var getContainersPsi = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "ps -q --filter label=com.microsoft.developer.usvc-dev.name --filter name=flink",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };
            
            using var getContainersProcess = Process.Start(getContainersPsi);
            if (getContainersProcess == null)
            {
                TestContext.WriteLine("⚠️ Failed to get Flink container list");
                return;
            }
            
            var containerIds = getContainersProcess.StandardOutput.ReadToEnd().Trim();
            getContainersProcess.WaitForExit();
            
            if (string.IsNullOrWhiteSpace(containerIds))
            {
                TestContext.WriteLine("⚠️ No Flink containers found");
                return;
            }
            
            var containerIdList = containerIds.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            TestContext.WriteLine($"📦 Found {containerIdList.Length} Flink containers");
            
            // Copy logs from each Flink container
            foreach (var containerId in containerIdList)
            {
                CopyLogsFromContainer(containerId, testLogsDir);
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error copying Flink logs: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Copy all logs from /opt/flink/log/ in a specific container.
    /// This includes both Apache Flink logs and FlinkJobRunner logs (both write to same directory).
    /// Renames Apache Flink logs to Flink.{component}.log.YYYYMMDD format.
    /// FlinkJobRunner logs already follow the correct naming convention.
    /// </summary>
    private static void CopyLogsFromContainer(string containerId, string testLogsDir)
    {
        try
        {
            var logFiles = GetLogFilesFromContainer(containerId);
            if (logFiles == null || logFiles.Length == 0)
            {
                TestContext.WriteLine($"   No Flink logs found in container {containerId.Substring(0, 12)}");
                return;
            }
            
            TestContext.WriteLine($"   Found {logFiles.Length} Flink log files in container {containerId.Substring(0, 12)}");
            
            var componentType = GetContainerComponentType(containerId);
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            
            foreach (var logFile in logFiles)
            {
                CopyIndividualLogFile(containerId, logFile, testLogsDir, componentType, dateStamp);
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"   ⚠️ Error copying logs from container {containerId.Substring(0, 12)}: {ex.Message}");
        }
    }
    
    private static string[]? GetLogFilesFromContainer(string containerId)
    {
        var checkLogsPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec {containerId} sh -c \"ls /opt/flink/log/*.log* 2>/dev/null || echo ''\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var checkProcess = Process.Start(checkLogsPsi);
        if (checkProcess == null) return null;
        
        var logFiles = checkProcess.StandardOutput.ReadToEnd().Trim();
        checkProcess.WaitForExit();
        
        return string.IsNullOrWhiteSpace(logFiles)
            ? null
            : logFiles.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
    }
    
    private static string GetContainerComponentType(string containerId)
    {
        var getNamePsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"inspect --format={{{{.Name}}}} {containerId}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        
        using var getNameProcess = Process.Start(getNamePsi);
        var containerName = getNameProcess?.StandardOutput.ReadToEnd().Trim().TrimStart('/') ?? "unknown";
        getNameProcess?.WaitForExit();
        
        if (containerName.Contains("jobmanager", StringComparison.OrdinalIgnoreCase))
            return "jobmanager";
        if (containerName.Contains("taskmanager", StringComparison.OrdinalIgnoreCase) ||
            containerName.Contains("taskexecutor", StringComparison.OrdinalIgnoreCase))
            return "taskmanager";
        if (containerName.Contains("sql-gateway", StringComparison.OrdinalIgnoreCase))
            return "sql-gateway";
        
        return "unknown";
    }
    
    private static void CopyIndividualLogFile(string containerId, string logFile, string testLogsDir,
        string componentType, string dateStamp)
    {
        var fileName = Path.GetFileName(logFile);
        var destName = fileName.StartsWith("FlinkIRRunner.", StringComparison.OrdinalIgnoreCase)
            ? fileName
            : $"Flink.{componentType}.log.{dateStamp}";
        
        var destPath = Path.Combine(testLogsDir, destName);
        
        var copyPsi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"cp {containerId}:{logFile} \"{destPath}\"",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        
        using var copyProcess = Process.Start(copyPsi);
        if (copyProcess == null) return;
        
        copyProcess.WaitForExit(TimeSpan.FromSeconds(5));
        
        if (copyProcess.ExitCode == 0 && File.Exists(destPath))
        {
            var fileInfo = new FileInfo(destPath);
            TestContext.WriteLine($"   ✅ Copied {fileName} as {destName} ({fileInfo.Length} bytes)");
        }
        else
        {
            var error = copyProcess.StandardError.ReadToEnd();
            TestContext.WriteLine($"   ⚠️ Failed to copy {fileName}: {error}");
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
        
        // Set LOG_FILE_PATH to ensure all logs go to LocalTesting/test-logs/
        // Use absolute path to ensure logs are written to the correct location
        var testLogsDir = Path.GetFullPath(Path.Combine(repoRoot, "LocalTesting", "test-logs"));
        psi.Environment["LOG_FILE_PATH"] = testLogsDir;
        
        TestContext.WriteLine($"🔧 Setting KAFKA_BOOTSTRAP_SERVERS={KafkaHostBootstrapServers} for exercise (host access)");
        TestContext.WriteLine($"🔧 Setting KAFKA_FLINK_BOOTSTRAP_SERVERS={KafkaFlinkBootstrapServers} for Flink jobs (container access)");
        TestContext.WriteLine($"🔧 Setting LOG_FILE_PATH={testLogsDir} for centralized logging");

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException($"Failed to start exercise: {exercisePath}");
        }

        var outputTask = process.StandardOutput.ReadToEndAsync();
        var errorTask = process.StandardError.ReadToEndAsync();

        var timeoutMilliseconds = (int)(timeout ?? TimeSpan.FromMinutes(5)).TotalMilliseconds;
        if (!process.WaitForExit(timeoutMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"Exercise {exercisePath} timed out after {timeout}");
        }

        var output = await outputTask;
        var error = await errorTask;

        TestContext.WriteLine($"✅ Exercise completed with exit code {process.ExitCode}");
        if (!string.IsNullOrEmpty(output))
        {
            TestContext.WriteLine($"📝 Output:\n{output}");
        }
        if (!string.IsNullOrEmpty(error))
        {
            TestContext.WriteLine($"⚠️ Error output:\n{error}");
        }

        return (process.ExitCode, output, error);
    }
}