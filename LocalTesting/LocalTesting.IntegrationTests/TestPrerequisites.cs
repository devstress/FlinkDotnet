using System.Diagnostics;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

internal static class TestPrerequisites
{
    private static bool? _containerRuntimeAvailable;

    internal static void EnsureDockerAvailable()
    {
        _containerRuntimeAvailable ??= ProbeContainerRuntime();

        if (_containerRuntimeAvailable != true)
        {
            Assert.That(_containerRuntimeAvailable, Is.True,
                "Container runtime (Docker or Podman) is not available or not responsive. " +
                "Ensure Docker Desktop or Podman is running before executing LocalTesting integration tests.");
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

    private static bool ProbeContainerRuntime()
    {
        // Try Docker first
        if (ProbeRuntime("docker"))
        {
            TestContext.WriteLine("✓ Docker runtime is available");
            return true;
        }

        // Try Podman as fallback
        if (ProbeRuntime("podman"))
        {
            TestContext.WriteLine("✓ Podman runtime is available");
            return true;
        }

        TestContext.WriteLine("✗ No container runtime (Docker or Podman) is available");
        return false;
    }

    private static bool ProbeRuntime(string runtimeCommand)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = runtimeCommand,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            // Use 'version' command which works consistently for both Docker and Podman
            psi.ArgumentList.Add("version");
            psi.ArgumentList.Add("--format");
            
            // Docker uses {{.Server.Version}}, Podman uses {{.Version}}
            // Use the simpler format that works for both
            if (runtimeCommand.Equals("docker", StringComparison.OrdinalIgnoreCase))
            {
                psi.ArgumentList.Add("{{.Server.Version}}");
            }
            else // podman
            {
                psi.ArgumentList.Add("{{.Version}}");
            }

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
                TestContext.WriteLine($"{runtimeCommand} probe failed with exit code {process.ExitCode}: {error}");
                return false;
            }

            var output = process.StandardOutput.ReadToEnd().Trim();
            if (string.IsNullOrEmpty(output) || string.Equals(output, "null", StringComparison.OrdinalIgnoreCase))
            {
                TestContext.WriteLine($"{runtimeCommand} probe returned an unexpected payload.");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"{runtimeCommand} probe threw {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}











