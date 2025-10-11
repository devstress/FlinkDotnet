using System.Diagnostics;
using LearningCourse.Common;
using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Simple test base for LearningCourse integration tests.
/// Starts LocalTesting AppHost as a standalone process and runs actual exercise code against it.
/// Much simpler than creating duplicate AppHost infrastructure.
/// </summary>
public abstract class LearningCourseTestBase
{
    private static Process? _appHostProcess;
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
    /// Start LocalTesting AppHost once for all tests
    /// </summary>
    [OneTimeSetUp]
    public static async Task GlobalSetUp()
    {
        TestContext.WriteLine("🚀 Starting LocalTesting AppHost...");
        TestContext.WriteLine($"📁 AppHost path: {AppHostPath}");
        
        // DO NOT set KAFKA_BOOTSTRAP_SERVERS here!
        // REASON: Environment variables set here are inherited by AppHost process,
        // which then passes them to all Docker containers including Flink containers.
        // This causes Kafka client inside Flink TaskManager to use localhost:9093 instead of kafka:9092.
        //
        // SOLUTION: Set KAFKA_BOOTSTRAP_SERVERS only in ExecuteExerciseAsync() so it's ONLY
        // available to exercise processes, NOT to AppHost or Flink containers.
        TestContext.WriteLine($"✅ NOT setting KAFKA_BOOTSTRAP_SERVERS globally to prevent Docker inheritance");

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

        _appHostProcess = Process.Start(psi);
        
        if (_appHostProcess == null)
        {
            throw new InvalidOperationException("Failed to start AppHost process");
        }

        // Capture output for diagnostics
        _appHostProcess.OutputDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost] {e.Data}");
            }
        };
        _appHostProcess.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                TestContext.WriteLine($"[AppHost Error] {e.Data}");
            }
        };
        
        _appHostProcess.BeginOutputReadLine();
        _appHostProcess.BeginErrorReadLine();

        TestContext.WriteLine("✅ AppHost process started, waiting for infrastructure to be ready...");
        TestContext.WriteLine($"⏱️ Waiting {AppHostStartupTimeout.TotalSeconds} seconds for infrastructure startup...");
        
        // Wait for infrastructure to be ready
        await Task.Delay(AppHostStartupTimeout);
        
        TestContext.WriteLine("✅ Infrastructure startup time elapsed");
        
        // Discover Kafka container IP for Flink jobs
        // Docker bridge network doesn't support DNS between containers, so we need actual IP
        TestContext.WriteLine("🔍 Discovering Kafka container IP for Flink jobs...");
        try
        {
            KafkaFlinkBootstrapServers = await DockerInfrastructure.GetKafkaContainerIpAsync();
            TestContext.WriteLine($"✅ Kafka container IP for Flink: {KafkaFlinkBootstrapServers}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to discover Kafka container IP: {ex.Message}");
            throw;
        }
        
        // Discover Kafka host endpoint for exercise producers/consumers
        TestContext.WriteLine("🔍 Discovering Kafka host endpoint for exercises...");
        try
        {
            KafkaHostBootstrapServers = await DockerInfrastructure.GetKafkaHostEndpointAsync();
            TestContext.WriteLine($"✅ Kafka host endpoint for exercises: {KafkaHostBootstrapServers}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to discover Kafka host endpoint: {ex.Message}");
            throw;
        }
        
        TestContext.WriteLine("✅ All infrastructure ready, tests can proceed");
    }

    /// <summary>
    /// Stop LocalTesting AppHost after all tests complete.
    /// Force kills the process and manually cleans up containers.
    /// </summary>
    [OneTimeTearDown]
    public static void GlobalTearDown()
    {
        TestContext.WriteLine("🛑 Stopping LocalTesting AppHost...");
        
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
    /// Execute an exercise program and capture its output
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
        
        TestContext.WriteLine($"🔧 Setting KAFKA_BOOTSTRAP_SERVERS={KafkaHostBootstrapServers} for exercise (host access)");
        TestContext.WriteLine($"🔧 Setting KAFKA_FLINK_BOOTSTRAP_SERVERS={KafkaFlinkBootstrapServers} for Flink jobs (container access)");

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