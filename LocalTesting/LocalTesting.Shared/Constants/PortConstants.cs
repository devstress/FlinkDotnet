namespace LocalTesting.Shared.Constants;

/// <summary>
/// Centralized port constants for all LocalTesting services
/// All ports are in the 13000+ range to eliminate magic numbers and ensure consistency
/// </summary>
public static class PortConstants
{
    // ========================================
    // CORE WEB API SERVICE PORTS (13001-13050)
    // ========================================
    
    /// <summary>WebAPI internal port - primary service endpoint</summary>
    public const int WebApiInternal = 13001;
    
    /// <summary>WebAPI external port - Aspire exposed endpoint</summary>
    public const int WebApiExternal = 18000;
    
    // ========================================
    // INFRASTRUCTURE SERVICE PORTS (13051-13150)
    // ========================================
    
    /// <summary>Redis internal port</summary>
    public const int RedisInternal = 13051;
    
    /// <summary>Redis external port</summary>
    public const int RedisExternal = 18051;
    
    /// <summary>Kafka broker internal port</summary>
    public const int KafkaInternal = 13052;
    
    /// <summary>Kafka broker external port</summary>
    public const int KafkaExternal = 18052;
    
    /// <summary>Kafka controller internal port</summary>
    public const int KafkaControllerInternal = 13053;
    
    /// <summary>Kafka JMX metrics port</summary>
    public const int KafkaJmxInternal = 13054;
    
    /// <summary>Kafka JMX exporter external port</summary>
    public const int KafkaJmxExternal = 18054;
    
    // ========================================
    // FLINK SERVICE PORTS (13151-13200)
    // ========================================
    
    /// <summary>Flink JobManager RPC internal port</summary>
    public const int FlinkJobManagerRpcInternal = 13151;
    
    /// <summary>Flink JobManager Web UI internal port</summary>
    public const int FlinkJobManagerWebInternal = 13152;
    
    /// <summary>Flink JobManager Web UI external port</summary>
    public const int FlinkJobManagerWebExternal = 18002;
    
    /// <summary>Flink TaskManager data exchange internal port</summary>
    public const int FlinkTaskManagerDataInternal = 13153;
    
    /// <summary>Flink TaskManager RPC internal port</summary>
    public const int FlinkTaskManagerRpcInternal = 13154;
    
    /// <summary>Flink SQL Gateway internal port</summary>
    public const int FlinkSqlGatewayInternal = 13155;
    
    /// <summary>Flink SQL Gateway external port</summary>
    public const int FlinkSqlGatewayExternal = 18055;
    
    // ========================================
    // TEMPORAL SERVICE PORTS (13201-13250)
    // ========================================
    
    /// <summary>Temporal server internal port</summary>
    public const int TemporalServerInternal = 13201;
    
    /// <summary>Temporal server external port</summary>
    public const int TemporalServerExternal = 18003;
    
    /// <summary>Temporal metrics internal port</summary>
    public const int TemporalMetricsInternal = 13202;
    
    /// <summary>Temporal metrics external port</summary>
    public const int TemporalMetricsExternal = 18203;
    
    // ========================================
    // OBSERVABILITY SERVICE PORTS (13251-13350)
    // ========================================
    
    /// <summary>Prometheus internal port</summary>
    public const int PrometheusInternal = 13251;
    
    /// <summary>Prometheus external port</summary>
    public const int PrometheusExternal = 18006;
    
    /// <summary>Grafana internal port</summary>
    public const int GrafanaInternal = 13252;
    
    /// <summary>Grafana external port</summary>
    public const int GrafanaExternal = 18010;
    
    /// <summary>Loki internal port</summary>
    public const int LokiInternal = 13253;
    
    /// <summary>Loki external port</summary>
    public const int LokiExternal = 18005;
    
    /// <summary>Loki gRPC internal port</summary>
    public const int LokiGrpcInternal = 13254;
    
    // ========================================
    // ASPIRE DASHBOARD PORTS (13301-13350)
    // ========================================
    
    /// <summary>Aspire dashboard main UI port</summary>
    public const int AspireDashboard = 18888;
    
    /// <summary>Aspire OTLP endpoint port</summary>
    public const int AspireOtlpEndpoint = 13323;
    
    /// <summary>Aspire OTLP HTTP endpoint port</summary>
    public const int AspireOtlpHttpEndpoint = 13324;
    
    // ========================================
    // SIMULATION/TESTING PORTS (13351-13400)
    // ========================================
    
    /// <summary>TaskManager simulation port 1</summary>
    public const int TaskManagerSim1 = 13351;
    
    /// <summary>TaskManager simulation port 2</summary>
    public const int TaskManagerSim2 = 13352;
    
    /// <summary>TaskManager simulation port 3</summary>
    public const int TaskManagerSim3 = 13353;
    
    // ========================================
    // PORT BUILDING HELPER METHODS
    // ========================================
    
    /// <summary>Build Redis connection string with proper port</summary>
    public static string RedisConnectionString(string host = "localhost") 
        => $"{host}:{RedisInternal}";
    
    /// <summary>Build Kafka bootstrap servers with proper port</summary>
    public static string KafkaBootstrapServers(string host = "kafka") 
        => $"{host}:{KafkaInternal}";
    
    /// <summary>Build Flink JobManager URL with proper port</summary>
    public static string FlinkJobManagerUrl(string host = "flink-jobmanager") 
        => $"http://{host}:{FlinkJobManagerWebInternal}";
    
    /// <summary>Build Flink SQL Gateway URL with proper port</summary>
    public static string FlinkSqlGatewayUrl(string host = "localhost") 
        => $"http://{host}:{FlinkSqlGatewayInternal}";
    
    /// <summary>Build Temporal server URL with proper port</summary>
    public static string TemporalServerUrl(string host = "temporal-server") 
        => $"{host}:{TemporalServerInternal}";
    
    /// <summary>Build Prometheus URL with proper port</summary>
    public static string PrometheusUrl(string host = "prometheus") 
        => $"http://{host}:{PrometheusInternal}";
    
    /// <summary>Build Grafana URL with proper port</summary>
    public static string GrafanaUrl(string host = "grafana") 
        => $"http://{host}:{GrafanaInternal}";
    
    /// <summary>Build Loki URL with proper port</summary>
    public static string LokiUrl(string host = "loki") 
        => $"http://{host}:{LokiInternal}";
    
    /// <summary>Build TaskManager address for simulation</summary>
    public static string TaskManagerAddress(string host, int simNumber) => simNumber switch
    {
        1 => $"{host}:{TaskManagerSim1}",
        2 => $"{host}:{TaskManagerSim2}", 
        3 => $"{host}:{TaskManagerSim3}",
        _ => $"{host}:{TaskManagerSim1}"
    };
    
    /// <summary>Build WebAPI metrics endpoint URL</summary>
    public static string WebApiMetricsUrl(string host = "localhost") 
        => $"http://{host}:{WebApiInternal}/metrics";
    
    /// <summary>Build Aspire OTLP endpoint URL</summary>
    public static string AspireOtlpEndpointUrl(string host = "localhost") 
        => $"http://{host}:{AspireOtlpEndpoint}";
    
    /// <summary>Build Aspire OTLP HTTP endpoint URL</summary>
    public static string AspireOtlpHttpEndpointUrl(string host = "localhost") 
        => $"http://{host}:{AspireOtlpHttpEndpoint}";
}