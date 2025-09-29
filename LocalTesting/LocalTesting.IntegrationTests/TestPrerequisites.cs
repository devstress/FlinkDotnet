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





