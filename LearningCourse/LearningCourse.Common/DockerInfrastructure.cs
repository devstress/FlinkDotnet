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
                return $"127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Kafka endpoint from Docker/Podman ports: {kafkaContainers}");
    }

    /// <summary>
    /// Discovers the Temporal gRPC endpoint from Docker port mappings for host-to-Temporal connections.
    /// This finds the dynamically allocated host port that maps to Temporal's gRPC port (7233).
    /// </summary>
    /// <returns>Temporal endpoint for host access (e.g., "localhost:43210")</returns>
    public static async Task<string> GetTemporalHostEndpointAsync()
    {
        try
        {
            var temporalContainers = await RunDockerCommandAsync("ps --filter \"name=temporal-server\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Temporal container port mappings: {temporalContainers.Trim()}");
            
            return ExtractTemporalEndpointFromPorts(temporalContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Temporal endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractTemporalEndpointFromPorts(string temporalContainers)
    {
        var lines = temporalContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 7233 (Temporal's gRPC port)
            // Format: 127.0.0.1:PORT->7233/tcp or 0.0.0.0:PORT->7233/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->7233");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Temporal gRPC port mapping: host {port} -> container 7233");
                return $"127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Temporal endpoint from Docker/Podman ports: {temporalContainers}");
    }

    /// <summary>
    /// Discovers the Redis endpoint from Docker port mappings for host-to-Redis connections.
    /// This finds the dynamically allocated host port that maps to Redis's port (6379).
    /// </summary>
    /// <returns>Redis endpoint for host access (e.g., "localhost:43211")</returns>
    public static async Task<string> GetRedisHostEndpointAsync()
    {
        try
        {
            var redisContainers = await RunDockerCommandAsync("ps --filter \"name=redis\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Redis container port mappings: {redisContainers.Trim()}");
            
            return ExtractRedisEndpointFromPorts(redisContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Redis endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractRedisEndpointFromPorts(string redisContainers)
    {
        var lines = redisContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 6379 (Redis's default port)
            // Format: 127.0.0.1:PORT->6379/tcp or 0.0.0.0:PORT->6379/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->6379");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Redis port mapping: host {port} -> container 6379");
                return $"127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Redis endpoint from Docker/Podman ports: {redisContainers}");
    }

    /// <summary>
    /// Discovers the Prometheus endpoint from Docker port mappings for host-to-Prometheus connections.
    /// This finds the dynamically allocated host port that maps to Prometheus's port (9090).
    /// </summary>
    /// <returns>Prometheus endpoint for host access (e.g., "localhost:43212")</returns>
    public static async Task<string> GetPrometheusHostEndpointAsync()
    {
        try
        {
            var prometheusContainers = await RunDockerCommandAsync("ps --filter \"name=prometheus\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Prometheus container port mappings: {prometheusContainers.Trim()}");
            
            return ExtractPrometheusEndpointFromPorts(prometheusContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Prometheus endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractPrometheusEndpointFromPorts(string prometheusContainers)
    {
        var lines = prometheusContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 9090 (Prometheus's HTTP port)
            // Format: 127.0.0.1:PORT->9090/tcp or 0.0.0.0:PORT->9090/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->9090");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Prometheus port mapping: host {port} -> container 9090");
                return $"http://127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Prometheus endpoint from Docker/Podman ports: {prometheusContainers}");
    }

    /// <summary>
    /// Discovers the Grafana endpoint from Docker port mappings for host-to-Grafana connections.
    /// This finds the dynamically allocated host port that maps to Grafana's port (3000).
    /// </summary>
    /// <returns>Grafana endpoint for host access (e.g., "localhost:43213")</returns>
    public static async Task<string> GetGrafanaHostEndpointAsync()
    {
        try
        {
            var grafanaContainers = await RunDockerCommandAsync("ps --filter \"name=grafana\" --format \"{{.Ports}}\"");
            Console.WriteLine($"🔍 Grafana container port mappings: {grafanaContainers.Trim()}");
            
            return ExtractGrafanaEndpointFromPorts(grafanaContainers);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to discover Grafana endpoint from Docker: {ex.Message}", ex);
        }
    }

    private static string ExtractGrafanaEndpointFromPorts(string grafanaContainers)
    {
        var lines = grafanaContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 3000 (Grafana's HTTP port)
            // Format: 127.0.0.1:PORT->3000/tcp or 0.0.0.0:PORT->3000/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->3000");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Grafana port mapping: host {port} -> container 3000");
                return $"http://127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Grafana endpoint from Docker/Podman ports: {grafanaContainers}");
    }
}