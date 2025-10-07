using System.Diagnostics;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

internal static class TestPrerequisites
{
    private static bool? _dockerAvailable;

    internal static void EnsureDockerAvailable()
    {
        _dockerAvailable ??= ProbeDocker();

        if (_dockerAvailable != true)
        {
            Assert.That(_dockerAvailable, Is.True, "Docker CLI is not available or not responsive. Ensure Docker Desktop is running before executing LocalTesting integration tests.");
        }
    }

    internal static bool ProbeFlinkGatewayBuildable()
    {
        // IMPORTANT: Do NOT use cached value - always re-check to detect newly built JARs
        // The previous caching caused tests to fail even after JARs were built
        
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        TestContext.WriteLine($"ProbeFlinkGatewayBuildable - BaseDirectory: {AppContext.BaseDirectory}");
        TestContext.WriteLine($"ProbeFlinkGatewayBuildable - RepoRoot: {repoRoot}");
        
        var gatewayProj = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "Flink.JobGateway.csproj");
        
        if (!ValidateGatewayProjectExists(gatewayProj))
        {
            return false;
        }

        try
        {
            var runnerJarExists = CheckRunnerJarExists(repoRoot);
            return runnerJarExists;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Flink.JobGateway build probe threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    private static bool ValidateGatewayProjectExists(string gatewayProj)
    {
        if (File.Exists(gatewayProj))
        {
            return true;
        }
        
        TestContext.WriteLine($"Flink.JobGateway project not found at {gatewayProj}");
        return false;
    }

    private static bool CheckRunnerJarExists(string repoRoot)
    {
        TestContext.WriteLine($"Checking for Runner JAR with repoRoot: {repoRoot}");
        
        var candidateNames = new[] { "flink-ir-runner-java17.jar" };
        var candidateDirs = new[]
        {
            // Check Gateway build output directories first (where MSBuild copies JARs)
            Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Release", "net9.0"),
            Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "bin", "Debug", "net9.0"),
            // Then check Maven build locations
            Path.Combine(repoRoot, "FlinkIRRunner", "target"),
            Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "FlinkIRRunner", "target")
        };

        foreach (var dir in candidateDirs)
        {
            TestContext.WriteLine($"Checking directory: {dir}");
            TestContext.WriteLine($"Directory exists: {Directory.Exists(dir)}");
            
            foreach (var name in candidateNames)
            {
                var full = Path.Combine(dir, name);
                TestContext.WriteLine($"Checking file: {full}");
                if (File.Exists(full))
                {
                    TestContext.WriteLine($"✓ Found Flink IR Runner JAR: {full}");
                    return true;
                }
            }
        }
        
        TestContext.WriteLine("✗ Flink IR Runner JAR not found in any expected location.");
        return false;
    }

    private static bool ProbeDocker()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "docker",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("info");
            psi.ArgumentList.Add("--format");
            psi.ArgumentList.Add("{{json .ServerVersion}}");

            using var process = Process.Start(psi);
            if (process == null)
            {
                return false;
            }

            if (!process.WaitForExit(1000))
            {
                try
                {
                    process.Kill(entireProcessTree: true);
                }
                catch (InvalidOperationException)
                {
                    // Process already exited
                }
                return false;
            }

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                TestContext.WriteLine($"Docker probe failed with exit code {process.ExitCode}: {error}");
                return false;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(output) || string.Equals(output, "null", System.StringComparison.OrdinalIgnoreCase))
            {
                TestContext.WriteLine("Docker probe returned an unexpected payload.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Docker probe threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}











