namespace LocalTesting.FlinkSqlAppHost;

public static class Ports
{
    /// <summary>
    /// Flink JobManager REST/UI container port (host port allocated dynamically by Aspire)
    /// </summary>
    public const int JobManagerHostPort = 8081;

    /// <summary>
    /// Flink SQL Gateway REST API container port (host port allocated dynamically by Aspire)
    /// </summary>
    public const int SqlGatewayHostPort = 8083;

    /// <summary>
    /// Gateway HTTP port (Projects.FlinkDotNet_JobGateway runs on fixed host port)
    /// </summary>
    public const int GatewayHostPort = 8086;

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
    /// Temporal Server container ports (host ports allocated dynamically by Aspire).
    /// </summary>
    /// <remarks>
    /// CRITICAL: Temporal dual port configuration:
    /// - Port 7233: gRPC frontend for workflow/activity execution
    ///   * Used by Temporalio SDK clients to connect
    ///   * Primary interface for workflow submission and queries
    /// - Port 8233: HTTP UI for workflow monitoring
    ///   * Web-based dashboard for observability
    ///   * Displays workflow history, status, and execution details
    /// </remarks>
    public const int TemporalGrpcPort = 7233;

    /// <summary>
    /// gRPC frontend port
    /// </summary>
    public const int TemporalUIPort = 8233;

    public const string TemporalHostAddress = "localhost:7233";

    /// <summary>
    /// Observability Stack - Monitoring and metrics.
    /// </summary>
    /// <remarks>
    /// Note: Prometheus exposes metrics on container port 9090.
    /// </remarks>
    public const int RedisHostPort = 6379;
    public const int PrometheusHostPort = 9090;

    /// <summary>
    /// Grafana visualization dashboard
    /// </summary>
    public const int GrafanaHostPort = 3000;
}
