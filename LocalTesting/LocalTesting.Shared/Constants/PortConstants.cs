namespace LocalTesting.Shared.Constants;

/// <summary>
/// Centralized port constants for all LocalTesting services
/// Internal ports use standard defaults for inter-container communication
/// External ports use 13000+ range for Aspire exposure and external access
/// </summary>
public static class PortConstants
{
    // ========================================
    // CORE WEB API SERVICE PORTS
    // ========================================
    
    /// <summary>WebAPI internal port - standard ASP.NET Core default</summary>
    public const int WebApiInternal = 8080;
    
    /// <summary>WebAPI external port - Aspire exposed endpoint</summary>
    public const int WebApiExternal = 13001;
    
    // ========================================
    // INFRASTRUCTURE SERVICE PORTS
    // ========================================
    
    /// <summary>Redis internal port - standard Redis default</summary>
    public const int RedisInternal = 6379;
    
    /// <summary>Redis external port</summary>
    public const int RedisExternal = 13051;
    
    /// <summary>Kafka broker internal port - standard Kafka default</summary>
    public const int KafkaInternal = 9092;
    
    /// <summary>Kafka broker external port</summary>
    public const int KafkaExternal = 13052;
    
    /// <summary>Kafka controller internal port - standard KRaft controller default</summary>
    public const int KafkaControllerInternal = 9093;
    
    /// <summary>Kafka JMX metrics internal port - standard JMX default</summary>
    public const int KafkaJmxInternal = 9999;
    
    /// <summary>Kafka JMX exporter external port</summary>
    public const int KafkaJmxExternal = 13054;
    
    // ========================================
    // FLINK SERVICE PORTS
    // ========================================
    
    /// <summary>Flink JobManager RPC internal port - standard Flink default</summary>
    public const int FlinkJobManagerRpcInternal = 6123;
    
    /// <summary>Flink JobManager Web UI internal port - standard Flink default</summary>
    public const int FlinkJobManagerWebInternal = 8081;
    
    /// <summary>Flink JobManager Web UI external port</summary>
    public const int FlinkJobManagerWebExternal = 13081;
    
    /// <summary>Flink TaskManager data exchange internal port - standard Flink default</summary>
    public const int FlinkTaskManagerDataInternal = 6121;
    
    /// <summary>Flink TaskManager RPC internal port - standard Flink default</summary>
    public const int FlinkTaskManagerRpcInternal = 6122;
    
    /// <summary>Flink SQL Gateway internal port - standard Flink default</summary>
    public const int FlinkSqlGatewayInternal = 8083;
    
    /// <summary>Flink SQL Gateway external port</summary>
    public const int FlinkSqlGatewayExternal = 13083;
    
    // ========================================
    // TEMPORAL SERVICE PORTS
    // ========================================
    
    /// <summary>Temporal server internal port - standard Temporal default</summary>
    public const int TemporalServerInternal = 7233;
    
    /// <summary>Temporal server external port</summary>
    public const int TemporalServerExternal = 13233;
    
    /// <summary>Temporal metrics internal port - standard Temporal metrics default</summary>
    public const int TemporalMetricsInternal = 8234;
    
    /// <summary>Temporal metrics external port</summary>
    public const int TemporalMetricsExternal = 13090;
    
    // ========================================
    // OBSERVABILITY SERVICE PORTS
    // ========================================
    
    /// <summary>Prometheus internal port - standard Prometheus default</summary>
    public const int PrometheusInternal = 9090;
    
    /// <summary>Prometheus external port</summary>
    public const int PrometheusExternal = 13090;
    
    /// <summary>Grafana internal port - standard Grafana default</summary>
    public const int GrafanaInternal = 3000;
    
    /// <summary>Grafana external port</summary>
    public const int GrafanaExternal = 13000;
    
    /// <summary>Loki internal port - standard Loki default</summary>
    public const int LokiInternal = 3100;
    
    /// <summary>Loki external port</summary>
    public const int LokiExternal = 13100;
    
    /// <summary>Loki gRPC internal port - standard Loki gRPC default</summary>
    public const int LokiGrpcInternal = 9095;
    
    // ========================================
    // ASPIRE DASHBOARD PORTS
    // ========================================
    
    /// <summary>Aspire dashboard main UI port</summary>
    public const int AspireDashboard = 18888;
    
    /// <summary>Aspire OTLP endpoint port</summary>
    public const int AspireOtlpEndpoint = 13323;
    
    /// <summary>Aspire OTLP HTTP endpoint port</summary>
    public const int AspireOtlpHttpEndpoint = 13324;
    
    // ========================================
    // PORT BUILDING HELPER METHODS
    // ========================================
    
    /// <summary>Build Redis connection string with proper internal port</summary>
    public static string RedisConnectionString(string host = "localhost") 
        => $"{host}:{RedisInternal}";
    
    /// <summary>Build Kafka bootstrap servers with proper internal port</summary>
    public static string KafkaBootstrapServers(string host = "kafka") 
        => $"{host}:{KafkaInternal}";
    
    /// <summary>Build Flink JobManager URL with proper internal port</summary>
    public static string FlinkJobManagerUrl(string host = "flink-jobmanager") 
        => $"http://{host}:{FlinkJobManagerWebInternal}";
    
    /// <summary>Build Flink SQL Gateway URL with proper internal port</summary>
    public static string FlinkSqlGatewayUrl(string host = "localhost") 
        => $"http://{host}:{FlinkSqlGatewayInternal}";
    
    /// <summary>Build Temporal server URL with proper internal port</summary>
    public static string TemporalServerUrl(string host = "temporal-server") 
        => $"{host}:{TemporalServerInternal}";
    
    /// <summary>Build Prometheus URL with proper internal port</summary>
    public static string PrometheusUrl(string host = "prometheus") 
        => $"http://{host}:{PrometheusInternal}";
    
    /// <summary>Build Grafana URL with proper internal port</summary>
    public static string GrafanaUrl(string host = "grafana") 
        => $"http://{host}:{GrafanaInternal}";
    
    /// <summary>Build Loki URL with proper internal port</summary>
    public static string LokiUrl(string host = "loki") 
        => $"http://{host}:{LokiInternal}";
    
    /// <summary>Build WebAPI metrics endpoint URL with proper external port for external access</summary>
    public static string WebApiMetricsUrl(string host = "localhost") 
        => $"http://{host}:{WebApiExternal}/metrics";
    
    /// <summary>Build TaskManager address for simulation (fallback for compatibility)</summary>
    public static string TaskManagerAddress(string host, int simNumber) 
        => $"{host}:6122"; // Standard Flink TaskManager RPC port for all simulations
    
    /// <summary>Build Aspire OTLP endpoint URL</summary>
    public static string AspireOtlpEndpointUrl(string host = "localhost") 
        => $"http://{host}:{AspireOtlpEndpoint}";
    
    /// <summary>Build Aspire OTLP HTTP endpoint URL</summary>
    public static string AspireOtlpHttpEndpointUrl(string host = "localhost") 
        => $"http://{host}:{AspireOtlpHttpEndpoint}";
}