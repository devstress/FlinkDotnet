namespace LearningCourse.Common;

/// <summary>
/// Aspire service discovery for exercises running standalone.
/// Automatically discovers dynamic ports from running Aspire/Docker containers.
/// </summary>
public static class AspireServiceDiscovery
{
    /// <summary>
    /// Get Kafka bootstrap servers for host-to-container communication.
    /// Checks environment variable first (set by tests), then discovers from Docker.
    /// </summary>
    public static async Task<string> GetKafkaBootstrapServersAsync()
    {
        // First check if test infrastructure already set it
        var envValue = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS");
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Discover from Docker/Aspire
        try
        {
            var endpoint = await DockerInfrastructure.GetKafkaHostEndpointAsync();
            Console.WriteLine($"[Aspire Discovery] Kafka Bootstrap Servers: {endpoint}");
            return endpoint;
        }
        catch
        {
            // Fallback to default if discovery fails
            Console.WriteLine("[Aspire Discovery] Using fallback: localhost:9093");
            return "localhost:9093";
        }
    }

    /// <summary>
    /// Get Kafka bootstrap servers for Flink job configurations (container-to-container).
    /// Checks environment variable first (set by tests), then discovers from Docker.
    /// </summary>
    public static async Task<string> GetKafkaFlinkBootstrapServersAsync()
    {
        // First check if test infrastructure already set it
        var envValue = Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS");
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Discover from Docker/Aspire
        try
        {
            var endpoint = await DockerInfrastructure.GetKafkaContainerIpAsync();
            Console.WriteLine($"[Aspire Discovery] Kafka Flink Bootstrap Servers: {endpoint}");
            return endpoint;
        }
        catch
        {
            // Fallback to default if discovery fails
            Console.WriteLine("[Aspire Discovery] Using fallback: kafka:9092");
            return "kafka:9092";
        }
    }

    /// <summary>
    /// Get Temporal gRPC endpoint for workflow execution.
    /// Checks environment variable first (set by tests), then discovers from Docker.
    /// </summary>
    public static async Task<string> GetTemporalEndpointAsync()
    {
        // First check if test infrastructure already set it
        var envValue = Environment.GetEnvironmentVariable("TEMPORAL_ENDPOINT");
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Discover from Docker/Aspire
        try
        {
            var endpoint = await DockerInfrastructure.GetTemporalHostEndpointAsync();
            Console.WriteLine($"[Aspire Discovery] Temporal Endpoint: {endpoint}");
            return endpoint;
        }
        catch
        {
            // Fallback to default if discovery fails
            Console.WriteLine("[Aspire Discovery] Using fallback: localhost:7233");
            return "localhost:7233";
        }
    }

    /// <summary>
    /// Get Flink Gateway URL for job submission.
    /// Checks environment variable first (set by tests), then uses default.
    /// </summary>
    public static string GetFlinkGatewayUrl()
    {
        var envValue = Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL");
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        // Flink Gateway typically uses fixed port 8080
        return "http://localhost:8080";
    }
}