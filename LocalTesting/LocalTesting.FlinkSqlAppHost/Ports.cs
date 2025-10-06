namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    public const int JobManagerHostPort = 8081; // Host REST/UI port
    public const int JobManagerRpcPort = 8081;  // Container REST/UI port
    public const int SqlGatewayHostPort = 8083; // SQL Gateway REST API port
    public const int GatewayHostPort = 8080;    // Gateway HTTP port
    
    // Kafka FIXED port configuration (no dynamic allocation)
    // CRITICAL: Kafka dual listener setup with FIXED ports:
    // - PLAINTEXT (port 9092): Internal container-to-container communication
    //   * Used by Flink TaskManager to connect: kafka:9092
    //   * Advertised listener: kafka:9092 (keeps containers on container network)
    // - PLAINTEXT_HOST (port 9093): External host machine access
    //   * Used by tests and external clients: localhost:9093
    //   * Advertised listener: localhost:9093 (accessible from host)
    // This ensures TaskManager always connects through kafka:9092 without dynamic port issues
    public const int KafkaInternalPort = 9092;  // Container network port
    public const int KafkaExternalPort = 9093;  // Host machine port
    public const string KafkaContainerBootstrap = "kafka:9092";  // For Flink containers
    public const string KafkaHostBootstrap = "localhost:9093";   // For tests/external access
}
