namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    public const int JobManagerHostPort = 8081; // Host REST/UI port
    public const int SqlGatewayHostPort = 8083; // SQL Gateway REST API port
    public const int GatewayHostPort = 8086;    // Gateway HTTP port
    
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
    
    // Temporal Server ports
    // CRITICAL: Temporal dual port configuration:
    // - Port 7233: gRPC frontend for workflow/activity execution
    //   * Used by Temporalio SDK clients to connect
    //   * Primary interface for workflow submission and queries
    // - Port 8088: HTTP UI for workflow monitoring
    //   * Web-based dashboard for observability
    //   * Displays workflow history, status, and execution details
    public const int TemporalGrpcPort = 7233;   // gRPC frontend port
    public const int TemporalUIPort = 8088;     // HTTP UI port
    public const string TemporalHostAddress = "localhost:7233";  // For SDK clients
    
    // LearningCourse Infrastructure ports (only deployed when LEARNINGCOURSE=true)
    // Redis - State management and caching for Day15 Capstone Project
    public const int RedisHostPort = 6379;      // Redis default port
    public const string RedisHostAddress = "localhost:6379";  // For SDK clients
    
    // Observability Stack - Monitoring and metrics
    // Note: Port 9090 is in Windows excluded port range (9038-9137)
    // Ports 9250-9252 are used by Flink metrics (JobManager, TaskManager, SQL Gateway)
    // Using 9253 for Prometheus to avoid conflicts
    public const int PrometheusHostPort = 9253;  // Prometheus metrics collection
    public const int GrafanaHostPort = 3000;     // Grafana visualization dashboard
}
