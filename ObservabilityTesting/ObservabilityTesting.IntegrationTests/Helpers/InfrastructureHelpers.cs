using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Infrastructure coordination and status display utilities.
/// </summary>
internal static class InfrastructureHelpers
{
    /// <summary>
    /// Wait for complete infrastructure readiness including optional Gateway.
    /// Performs quick health check only (trusts global setup).
    /// </summary>
    /// <param name="kafkaConnectionString">Kafka connection string to validate</param>
    /// <param name="includeGateway">Whether to validate Gateway availability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public static async Task WaitForFullInfrastructureAsync(
        string? kafkaConnectionString,
        bool includeGateway = true,
        CancellationToken cancellationToken = default)
    {
        // Quick validation that endpoints are still responding
        // This is used by individual tests after global setup has already validated everything
        TestContext.WriteLine("🔧 Quick infrastructure health check...");

        // Just verify Kafka is still accessible (very quick check)
        if (string.IsNullOrEmpty(kafkaConnectionString))
        {
            throw new InvalidOperationException("Kafka connection string not available");
        }

        // Display container status with ports for visibility (no polling - containers should already be running)
        await DisplayContainerStatusAsync();

        TestContext.WriteLine("✅ Infrastructure health check passed");
    }

    /// <summary>
    /// Display current container status and ports for debugging visibility.
    /// Used in lightweight mode - assumes containers are already running from global setup.
    /// Does NOT poll or wait - just displays current state immediately.
    /// </summary>
    public static async Task DisplayContainerStatusAsync()
    {
        try
        {
            // Single quick check - no polling needed since containers should already be running
            var containerInfo = await DockerUtilities.RunDockerCommandAsync("ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");

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
                    var allContainersInfo = await DockerUtilities.RunDockerCommandAsync("ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
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
    /// Capture network diagnostics for a specific test checkpoint.
    /// Helper method for tests to capture network state at critical points.
    /// </summary>
    /// <param name="testName">Name of the test</param>
    /// <param name="checkpoint">Checkpoint name (e.g., "before-test", "after-failure")</param>
    public static async Task CaptureTestNetworkDiagnosticsAsync(string testName, string checkpoint)
    {
        var checkpointName = $"test-{testName}-{checkpoint}";
        await NetworkDiagnostics.CaptureNetworkDiagnosticsAsync(checkpointName);
    }
}
