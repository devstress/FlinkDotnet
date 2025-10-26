namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    /// <summary>
    /// Host REST/UI port
    /// </summary>
    public const int JobManagerHostPort = 8081;

    /// <summary>
    /// SQL Gateway REST API port
    /// </summary>
    public const int SqlGatewayHostPort = 8083;

    /// <summary>
    /// Gateway HTTP port
    /// </summary>
    public const int GatewayHostPort = 8080;

    /// <summary>
    /// Kafka FIXED port configuration (no dynamic allocation).
    /// </summary>
    /// <remarks>
    /// CRITICAL: Kafka dual listener setup with FIXED ports:
    /// - PLAINTEXT (port 9092): Internal container-to-container communication
    ///   * Used by Flink TaskManager to connect: kafka:9092
    ///   * Advertised listener: kafka:9092 (keeps containers on container network)
    /// - PLAINTEXT_HOST (port 9093): External host machine access
    ///   * Used by tests and external clients: localhost:9093
    ///   * Advertised listener: localhost:9093 (accessible from host)
    /// This ensures TaskManager always connects through kafka:9092 without dynamic port issues.
    /// </remarks>
    public const int KafkaInternalPort = 9092;

    /// <summary>
    /// Container network port
    /// </summary>
    public const int KafkaExternalPort = 9093;

    /// <summary>
    /// For Flink containers
    /// </summary>
    public const string KafkaContainerBootstrap = "kafka:9092";

    /// <summary>
    /// For tests/external access
    /// </summary>
    public const string KafkaHostBootstrap = "localhost:9093";

    /// <summary>
    /// Temporal Server ports.
    /// </summary>
    /// <remarks>
    /// CRITICAL: Temporal dual port configuration:
    /// - Port 7233: gRPC frontend for workflow/activity execution
    ///   * Used by Temporalio SDK clients to connect
    ///   * Primary interface for workflow submission and queries
    /// - Port 8088: HTTP UI for workflow monitoring
    ///   * Web-based dashboard for observability
    ///   * Displays workflow history, status, and execution details
    /// </remarks>
    public const int TemporalGrpcPort = 7233;

    /// <summary>
    /// gRPC frontend port
    /// </summary>
    public const int TemporalUIPort = 8088;

    /// <summary>
    /// For SDK clients
    /// </summary>
    public const string TemporalHostAddress = "localhost:7233";

    /// <summary>
    /// LearningCourse Infrastructure ports (only deployed when LEARNINGCOURSE=true).
    /// Redis - State management and caching for Day15 Capstone Project.
    /// </summary>
    public const int RedisHostPort = 6379;

    /// <summary>
    /// For SDK clients
    /// </summary>
    public const string RedisHostAddress = "localhost:6379";

    /// <summary>
    /// Observability Stack - Monitoring and metrics.
    /// </summary>
    /// <remarks>
    /// Note: Port 9090 is in Windows excluded port range (9038-9137)
    /// Ports 9250-9252 are used by Flink metrics (JobManager, TaskManager, SQL Gateway)
    /// Using 9253 for Prometheus to avoid conflicts.
    /// </remarks>
    public const int PrometheusHostPort = 9253;

    /// <summary>
    /// Grafana visualization dashboard
    /// </summary>
    public const int GrafanaHostPort = 3000;
}
