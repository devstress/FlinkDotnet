using System.Diagnostics;

namespace LearningCourse.Common;

/// <summary>
/// Shared infrastructure utilities for LearningCourse test projects.
/// Provides Docker container discovery and connectivity helpers.
/// </summary>
public static class DockerInfrastructure
{
    /// <summary>
    /// Discovers the Kafka container IP address for Flink job configurations.
    /// Docker's default bridge network doesn't support DNS between containers,
    /// so we need to use the actual container IP address.
    /// </summary>
    /// <returns>Kafka container IP with port (e.g., "172.17.0.2:9093")</returns>
    public static async Task<string> GetKafkaContainerIpAsync()
    {
        try
        {
            var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka-\" --format \"{{.Names}}\"");
            var kafkaContainer = kafkaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            
            if (string.IsNullOrWhiteSpace(kafkaContainer))
            {
                throw new InvalidOperationException("Kafka container not found");
            }

            // Try Docker bridge network first
            var ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{.NetworkSettings.Networks.bridge.IPAddress}}}}\"");
            var ip = ipAddress.Trim();
            
            // If bridge network doesn't have IP, try podman network (for Podman runtime)
            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                Console.WriteLine($"🔍 Bridge network IP not found, trying podman network...");
                ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{.NetworkSettings.Networks.podman.IPAddress}}}}\"");
                ip = ipAddress.Trim();
            }
            
            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                // Fallback: Get the first available network IP
                Console.WriteLine($"🔍 Specific network not found, getting first available IP...");
                ipAddress = await RunDockerCommandAsync($"inspect {kafkaContainer} --format \"{{{{range .NetworkSettings.Networks}}}}{{{{.IPAddress}}}}{{{{end}}}}\"");
                ip = ipAddress.Trim();
            }
            
            if (string.IsNullOrWhiteSpace(ip) || ip == "<no value>")
            {
                throw new InvalidOperationException($"Could not determine Kafka container IP from any network. Container: {kafkaContainer}");
            }

            Console.WriteLine($"✅ Kafka container IP discovered: {ip}");
            
            // Return IP with PLAINTEXT_INTERNAL port (9093)
            return $"{ip}:9093";
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get Kafka container IP: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Executes a Docker command and returns the output.
    /// Works with both Docker and Podman (podman-docker compatibility).
    /// </summary>
    /// <param name="arguments">Docker command arguments</param>
    /// <returns>Command output</returns>
    public static async Task<string> RunDockerCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Docker command failed: {error}");
        }

        return output;
    }

    /// <summary>
    /// Discovers the Kafka endpoint from Docker port mappings for host-to-Kafka connections.
    /// This finds the dynamically allocated host port that maps to Kafka's container port.
    /// </summary>
    /// <returns>Kafka endpoint for host access (e.g., "localhost:43175")</returns>
    public static async Task<string> GetKafkaHostEndpointAsync()
    {
        try
        {
            var kafkaContainers = await RunDockerCommandAsync("ps --filter \"name=kafka\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Kafka container port mappings: {kafkaContainers.Trim()}");
            
            return ExtractKafkaEndpointFromPorts(kafkaContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Kafka endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractKafkaEndpointFromPorts(string kafkaContainers)
    {
        var lines = kafkaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 9092 (Kafka's default listener port)
            // Aspire maps container port 9092 to a dynamic host port for external access
            // Format: 127.0.0.1:PORT->9092/tcp or 0.0.0.0:PORT->9092/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->9092");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Kafka port mapping: host {port} -> container 9092");
                return $"localhost:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Kafka endpoint from Docker/Podman ports: {kafkaContainers}");
    }
}