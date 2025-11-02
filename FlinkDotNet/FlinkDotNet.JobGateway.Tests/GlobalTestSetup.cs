using NUnit.Framework;

namespace FlinkDotNet.JobGateway.Tests;

/// <summary>
/// Global test setup that runs once before all tests in the assembly.
/// Sets environment variables to optimize test performance by avoiding Maven builds.
/// </summary>
[SetUpFixture]
public class GlobalTestSetup
{
    [OneTimeSetUp]
    public void GlobalSetup()
    {
        // Set FLINK_RUNNER_JAR_PATH to pre-built JAR to avoid Maven builds during tests
        // This dramatically improves test performance from 6+ minutes to under 1 minute
        string? repoRoot = FindRepoRoot(Environment.CurrentDirectory);
        if (repoRoot != null)
        {
            string jarPath = Path.Combine(repoRoot, "FlinkIRRunner", "target", "flink-ir-runner-java17.jar");
            if (File.Exists(jarPath))
            {
                Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", jarPath);
                Console.WriteLine($"[GlobalTestSetup] Set FLINK_RUNNER_JAR_PATH to {jarPath}");
            }
            else
            {
                // Try alternate location in FlinkDotNet.JobGateway project
                jarPath = Path.Combine(repoRoot, "FlinkDotNet", "FlinkDotNet.JobGateway", "flink-ir-runner-java17.jar");
                if (File.Exists(jarPath))
                {
                    Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", jarPath);
                    Console.WriteLine($"[GlobalTestSetup] Set FLINK_RUNNER_JAR_PATH to {jarPath}");
                }
                else
                {
                    Console.WriteLine($"[GlobalTestSetup] Warning: flink-ir-runner-java17.jar not found at {jarPath}");
                }
            }
        }
    }

    [OneTimeTearDown]
    public void GlobalTeardown()
    {
        // Clean up environment variable
        Environment.SetEnvironmentVariable("FLINK_RUNNER_JAR_PATH", null);
    }

    private static string? FindRepoRoot(string start)
    {
        DirectoryInfo? dir = new(start);
        while (dir != null)
        {
            string globalJson = Path.Combine(dir.FullName, "global.json");
            if (File.Exists(globalJson))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
