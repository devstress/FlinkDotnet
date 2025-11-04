using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Flink endpoint discovery utilities for dynamic port allocation.
/// </summary>
internal static class FlinkEndpointDiscovery
{
    /// <summary>
    /// Get the dynamically allocated Flink JobManager HTTP endpoint from Aspire.
    /// Aspire DCP assigns random ports during testing, so we cannot use hardcoded ports.
    /// </summary>
    public static async Task<string> GetFlinkJobManagerEndpointAsync()
    {
        try
        {
            var flinkContainers = await DockerUtilities.RunDockerCommandAsync("ps --filter \"name=flink-jobmanager\" --format \"{{.Ports}}\"");
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

        var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->8081");
        return match.Success ? $"http://localhost:{match.Groups[1].Value}/" : null;
    }
}
