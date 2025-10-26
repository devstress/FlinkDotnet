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
            var kafkaContainers = await RunDockerCommandAsync("ps --filter name=kafka --format \"{{.Names}}\"");
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
    /// Logs the current state of all Docker containers using docker ps.
    /// Useful for debugging container discovery and connectivity issues.
    /// </summary>
    /// <param name="context">Context description for the log entry</param>
    /// <param name="logWriter">Optional StreamWriter for writing to debug log file</param>
    public static async Task LogDockerPsAsync(string context, StreamWriter? logWriter = null)
    {
        try
        {
            var header = $"\n🐳 === DOCKER PS ({context}) ===";
            var footer = $"🐳 === END DOCKER PS ({context}) ===\n";
            
            Console.WriteLine(header);
            logWriter?.WriteLine(header);
            
            var dockerPs = await RunDockerCommandAsync("ps --format \"table {{.ID}}\\t{{.Image}}\\t{{.Names}}\\t{{.Status}}\\t{{.Ports}}\"");
            
            Console.WriteLine(dockerPs);
            logWriter?.WriteLine(dockerPs);
            
            Console.WriteLine(footer);
            logWriter?.WriteLine(footer);
            
            logWriter?.Flush();  // Ensure it's written immediately
        }
        catch (Exception ex)
        {
            var errorMsg = $"⚠️ Failed to run docker ps for {context}: {ex.Message}";
            Console.WriteLine(errorMsg);
            logWriter?.WriteLine(errorMsg);
            logWriter?.Flush();
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
            var kafkaContainers = await RunDockerCommandAsync("ps --filter name=kafka --format {{.Ports}}");
            Console.WriteLine($"🔍 Kafka container port mappings: {kafkaContainers.Trim()}");
            
            // Log docker ps after discovering Kafka ports
            await LogDockerPsAsync("After Kafka Port Discovery");
            
            return ExtractKafkaEndpointFromPorts(kafkaContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if Kafka discovery fails
            await LogDockerPsAsync("Kafka Discovery Failed");
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
            var temporalContainers = await RunDockerCommandAsync("ps --filter name=temporal-server --format {{.Ports}}");
            Console.WriteLine($"🔍 Temporal container port mappings: {temporalContainers.Trim()}");
            
            // Log docker ps after discovering Temporal ports
            await LogDockerPsAsync("After Temporal Port Discovery");
            
            return ExtractTemporalEndpointFromPorts(temporalContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if Temporal discovery fails
            await LogDockerPsAsync("Temporal Discovery Failed");
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
            var redisContainers = await RunDockerCommandAsync("ps --filter name=redis --format {{.Ports}}");
            Console.WriteLine($"🔍 Redis container port mappings: {redisContainers.Trim()}");
            
            // Log docker ps after discovering Redis ports
            await LogDockerPsAsync("After Redis Port Discovery");
            
            return ExtractRedisEndpointFromPorts(redisContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if Redis discovery fails
            await LogDockerPsAsync("Redis Discovery Failed");
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
            var prometheusContainers = await RunDockerCommandAsync("ps --filter name=prometheus --format {{.Ports}}");
            Console.WriteLine($"🔍 Prometheus container port mappings: {prometheusContainers.Trim()}");
            
            // Log docker ps after discovering Prometheus ports
            await LogDockerPsAsync("After Prometheus Port Discovery");
            
            return ExtractPrometheusEndpointFromPorts(prometheusContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if Prometheus discovery fails
            await LogDockerPsAsync("Prometheus Discovery Failed");
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
            var grafanaContainers = await RunDockerCommandAsync("ps --filter name=grafana --format {{.Ports}}");
            Console.WriteLine($"🔍 Grafana container port mappings: {grafanaContainers.Trim()}");
            
            // Log docker ps after discovering Grafana ports
            await LogDockerPsAsync("After Grafana Port Discovery");
            
            return ExtractGrafanaEndpointFromPorts(grafanaContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if Grafana discovery fails
            await LogDockerPsAsync("Grafana Discovery Failed");
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

    /// <summary>
    /// Discovers the Flink REST API endpoint from Docker port mappings.
    /// This finds the dynamically allocated host port that maps to Flink JobManager's REST API port (8081).
    /// <summary>
    /// Gets the Flink Job Gateway endpoint for FlinkDotNet job submissions.
    /// JobGateway runs as a host process (not containerized) on fixed port 8080.
    /// This is the correct endpoint for exercises that submit FlinkDotNet jobs.
    /// 
    /// Note: This is different from Flink JobManager REST API (port 8081) which is for cluster management.
    /// Exercises use JobGateway (/api/v1/health, /jobs endpoints) not JobManager.
    /// </summary>
    /// <returns>Flink Job Gateway endpoint (http://localhost:8080)</returns>
    public static async Task<string> GetFlinkRestApiEndpointAsync()
    {
        try
        {
            // JobGateway runs on fixed localhost:8080 (not a Docker container)
            // No dynamic port discovery needed - it's configured in LocalTesting AppHost
            const string jobGatewayEndpoint = "http://localhost:8080";
            
            Console.WriteLine($"🔍 Using Flink Job Gateway endpoint: {jobGatewayEndpoint}");
            Console.WriteLine($"   (JobGateway runs as host process on fixed port 8080, not in Docker)");
            
            // Still log docker ps for debugging, but we're not discovering from it
            await LogDockerPsAsync("Flink Job Gateway Endpoint Configuration");
            
            return jobGatewayEndpoint;
        }
        catch (Exception ex)
        {
            await LogDockerPsAsync("Flink Job Gateway Endpoint Configuration Failed");
            throw new InvalidOperationException($"Failed to get Flink Job Gateway endpoint: {ex.Message}", ex);
        }
    }

    // NOTE: ExtractFlinkRestApiEndpointFromPorts method removed - JobGateway uses fixed port 8080, not Docker discovery
    // If you need to discover Flink JobManager REST API (port 8081) for cluster management, create a separate method


    /// <summary>
    /// Discovers the Kafka JMX Exporter endpoint from Docker port mappings for debugging metrics export.
    /// This finds the dynamically allocated host port that maps to JMX Exporter's HTTP port (5556).
    /// </summary>
    /// <returns>Kafka JMX Exporter endpoint for host access (e.g., "http://localhost:43214") or null if not found</returns>
    public static async Task<string?> GetKafkaExporterHostEndpointAsync()
    {
        try
        {
            var exporterContainers = await RunDockerCommandAsync("ps --filter name=kafka-exporter --format {{.Ports}}");
            Console.WriteLine($"🔍 Kafka JMX Exporter port mappings: {exporterContainers.Trim()}");
            
            if (string.IsNullOrWhiteSpace(exporterContainers))
            {
                Console.WriteLine("⚠️  kafka-exporter container not found");
                return null;
            }
            
            // Log docker ps after discovering exporter ports
            await LogDockerPsAsync("After Kafka Exporter Port Discovery");
            
            return ExtractKafkaExporterEndpointFromPorts(exporterContainers);
        }
        catch (Exception ex)
        {
            // Log docker ps if exporter discovery fails
            await LogDockerPsAsync("Kafka Exporter Discovery Failed");
            Console.WriteLine($"⚠️  Failed to discover Kafka JMX Exporter endpoint from Docker: {ex.Message}");
            return null;
        }
    }

    private static string ExtractKafkaExporterEndpointFromPorts(string exporterContainers)
    {
        var lines = exporterContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            // Look for port mapping to 5556 (JMX Exporter's HTTP port)
            // Format: 127.0.0.1:PORT->5556/tcp or 0.0.0.0:PORT->5556/tcp
            var match = System.Text.RegularExpressions.Regex.Match(line, @"(?:127\.0\.0\.1|0\.0\.0\.0):(\d+)->5556");
            if (match.Success)
            {
                var port = match.Groups[1].Value;
                Console.WriteLine($"🔍 Found Kafka JMX Exporter port mapping: host {port} -> container 5556");
                return $"http://127.0.0.1:{port}";
            }
        }

        throw new InvalidOperationException($"Could not determine Kafka JMX Exporter endpoint from Docker/Podman ports: {exporterContainers}");
    }
}