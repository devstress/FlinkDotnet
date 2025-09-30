using System.Diagnostics;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

internal static class TestPrerequisites
{
    private static bool? _dockerAvailable;
    private static bool? _gatewayBuildable;

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
        if (_gatewayBuildable.HasValue)
        {
            return _gatewayBuildable.Value;
        }

        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
        var gatewayProj = Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "Flink.JobGateway.csproj");
        if (!File.Exists(gatewayProj))
        {
            TestContext.WriteLine($"Flink.JobGateway project not found at {gatewayProj}");
            _gatewayBuildable = false;
            return false;
        }

        // Verify Flink IR Runner JAR existence first; without it, gateway cannot function for tests
        bool RunnerJarExists()
        {
            var candidateNames = new[] { "flink-ir-runner.jar", "flink-ir-runner-java17.jar" };
            var candidateDirs = new[]
            {
                Path.Combine(repoRoot, "FlinkDotNet", "Flink.JobGateway", "FlinkIRRunner", "target"),
                Path.Combine(repoRoot, "FlinkIRRunner", "target")
            };
            foreach (var dir in candidateDirs)
            {
                foreach (var name in candidateNames)
                {
                    var full = Path.Combine(dir, name);
                    if (File.Exists(full))
                    {
                        TestContext.WriteLine($"Found Flink IR Runner JAR: {full}");
                        return true;
                    }
                }
            }
            TestContext.WriteLine("Flink IR Runner JAR not found in expected locations.");
            return false;
        }

        try
        {
            // Build the gateway quickly and then ensure runner jar exists
            var psi = new ProcessStartInfo
            {
                FileName = "dotnet",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add("build");
            psi.ArgumentList.Add(gatewayProj);
            psi.ArgumentList.Add("--configuration");
            psi.ArgumentList.Add("Release");
            psi.ArgumentList.Add("--nologo");
            psi.ArgumentList.Add("--verbosity");
            psi.ArgumentList.Add("quiet");

            using var process = Process.Start(psi);
            if (process == null)
            {
                _gatewayBuildable = false;
                return false;
            }

            if (!process.WaitForExit(30000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
                _gatewayBuildable = false;
                return false;
            }

            if (process.ExitCode != 0)
            {
                var error = process.StandardError.ReadToEnd();
                TestContext.WriteLine($"Flink.JobGateway build failed: {error}");
                _gatewayBuildable = false;
                return false;
            }

            // Finally, require runner JAR presence
            if (!RunnerJarExists())
            {
                _gatewayBuildable = false;
                return false;
            }

            _gatewayBuildable = true;
            return true;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"Flink.JobGateway build probe threw {ex.GetType().Name}: {ex.Message}");
            _gatewayBuildable = false;
            return false;
        }
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

            if (!process.WaitForExit(5000))
            {
                try { process.Kill(entireProcessTree: true); } catch { }
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











