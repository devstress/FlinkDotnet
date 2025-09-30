public static class Ports
{
    public const int JobManagerHostPort = 8081; // Host REST/UI port
    public const int JobManagerRpcPort = 8081;  // Container REST/UI port
    public const int GatewayHostPort = 8080;    // Gateway HTTP port
    public const int KafkaPort = 9092;          // Kafka external listener for host
}
