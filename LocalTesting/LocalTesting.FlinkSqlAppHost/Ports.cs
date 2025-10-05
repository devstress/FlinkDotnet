namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    public const int JobManagerHostPort = 8081; // Host REST/UI port
    public const int JobManagerRpcPort = 8081;  // Container REST/UI port
    public const int SqlGatewayHostPort = 8083; // SQL Gateway REST API port
    public const int GatewayHostPort = 8080;    // Gateway HTTP port
    public const int KafkaPort = 9093;          // Kafka external listener for host (Aspire will map to dynamic port)
    
    // Kafka connection string for containers within Docker network
    // Used by Flink jobs running inside containers to reach Kafka
    // CRITICAL: Kafka dual listener configuration:
    // - PLAINTEXT (port 9092): Internal access for containers (kafka:9092)
    // - PLAINTEXT_HOST (port 9093): External access from host (localhost:9093 -> dynamic port)
    public const string KafkaContainerBootstrap = "kafka:9092";
}
