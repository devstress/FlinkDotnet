using System.Diagnostics;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Docker/Podman container utilities for test infrastructure.
/// </summary>
internal static class DockerUtilities
{
    /// <summary>
    /// Get detailed information about Kafka containers including network configuration.
    /// </summary>
    public static async Task<string> GetKafkaContainerDetailsAsync()
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
    public static async Task<bool> TestPortConnectivityAsync(string host, int port)
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
    public static async Task<string> RunDockerCommandAsync(string arguments)
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
}
