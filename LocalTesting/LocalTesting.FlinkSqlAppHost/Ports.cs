namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    public const int JobManagerHostPort = 8081; // Host REST/UI port
    public const int JobManagerRpcPort = 8081;  // Container REST/UI port
    public const int SqlGatewayHostPort = 8083; // SQL Gateway REST API port
    public const int GatewayHostPort = 8080;    // Gateway HTTP port
    public const int KafkaPort = 9093;          // Kafka external listener for host (hardcoded)
    
    // Kafka connection string for containers within Docker network
    // Used by Flink jobs running inside containers to reach Kafka
    // CRITICAL: Aspire's Kafka uses port 9093 for PLAINTEXT_INTERNAL listener (container-to-container)
    // Port 9092 is PLAINTEXT_HOST listener (external access from host machine)
    // Kafka container address for jobs running inside Flink containers
    // CRITICAL: Aspire's Kafka uses port 9093 for PLAINTEXT_INTERNAL listener (container-to-container)
    // Port 9092 is PLAINTEXT_HOST listener (external access from host machine)
    public const string KafkaContainerBootstrap = "kafka:9093";
}
