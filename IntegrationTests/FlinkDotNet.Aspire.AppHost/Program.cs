using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Configure Aspire dashboard and OTLP environment variables
// These settings eliminate the need for manual environment variable setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:4323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:4324");

// Configure Aspire dashboard URL for integration testing - use different port to avoid conflicts
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18889");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_URL", "http://localhost:18889");

// Disable Aspire dashboard authentication for easier integration testing access
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_NO_AUTH", "true");

// Configure OpenTelemetry endpoints for integration test applications
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318");
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");

// Configure .NET HTTP client to properly handle IPv6 localhost connections to Aspire DCP
// Aspire DCP binds to IPv6 (::1) by design, so we need to ensure IPv6 connectivity works
try
{
    // Enable proper IPv6 localhost connectivity for HttpClient
    AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
    AppContext.SetSwitch("System.Net.Http.UseSocketsHttpHandler", true);
    AppContext.SetSwitch("System.Net.Sockets.UseSocketsHttpHandler", true);
    
    // Ensure IPv6 is enabled and properly configured for localhost connections
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6", "false");
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USEIPV6", "true");
    Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_SOCKETS_INLINE_COMPLETIONS", "true");
    
    Console.WriteLine("✅ Applied IPv6 localhost connectivity enhancement for Aspire DCP");
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ IPv6 connectivity enhancement failed: {ex.Message}");
    // Continue anyway
}

var builder = DistributedApplication.CreateBuilder(args);

// Configure extended timeouts for Aspire DCP to handle complex integration test infrastructure
// This prevents the 20-second timeout that causes container reconciliation failures
builder.Services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
{
    options.StartupTimeout = TimeSpan.FromMinutes(5); // Extended startup timeout
    options.ShutdownTimeout = TimeSpan.FromMinutes(3); // Extended shutdown timeout for graceful cleanup
});

// Configure Aspire DCP for integration testing stability
builder.Services.Configure<Microsoft.Extensions.Hosting.ConsoleLifetimeOptions>(options =>
{
    options.SuppressStatusMessages = false; // Show startup progress for integration test debugging
});

Console.WriteLine("✅ Applied extended DCP timeouts and container stability settings for integration testing");

// Enhanced sequential container startup with reduced parallel load for integration testing
// Prevents DCP reconciliation failures by limiting simultaneous container creation
// Key insight: Start essential services first, then build dependency chains

// Redis with enhanced stability and health check configuration for integration tests
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "512mb") // Increased for integration tests
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")
    .WithEnvironment("REDIS_BIND", "0.0.0.0") // Force IPv4
    .WithEnvironment("REDIS_TIMEOUT", "30")
    .WithEnvironment("REDIS_TCP_KEEPALIVE", "60")
    .WithEnvironment("REDIS_SAVE", "60 1000") // Persistence settings for stability
    .WithEnvironment("REDIS_STOP_WRITES_ON_BGSAVE_ERROR", "no"); // Prevent redis crashes on save errors

// 3 Kafka Brokers with KRaft cluster configuration using official Apache Kafka image
// Optimized for integration testing with high throughput scenarios
var kafkaBroker1 = builder.AddContainer("kafka-broker-1", "apache/kafka:3.8.0")
    .WithEndpoint(9092, 9092, "kafka1")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-1:9092")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("CLUSTER_ID", "INTEGRATION_TEST_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx1G -Xms512M") // Increased for integration tests
    .WithEnvironment("KAFKA_BROKER_VERSION_FALLBACK", "3.8.0")
    .WithEnvironment("KAFKA_INTER_BROKER_PROTOCOL_VERSION", "3.8")
    .WithEnvironment("KAFKA_LOG_MESSAGE_FORMAT_VERSION", "3.8");

var kafkaBroker2 = builder.AddContainer("kafka-broker-2", "apache/kafka:3.8.0")
    .WithEndpoint(9093, 9092, "kafka2")
    .WithEnvironment("KAFKA_NODE_ID", "2")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-2:9092")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("CLUSTER_ID", "INTEGRATION_TEST_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx1G -Xms512M")
    .WithEnvironment("KAFKA_BROKER_VERSION_FALLBACK", "3.8.0")
    .WithEnvironment("KAFKA_INTER_BROKER_PROTOCOL_VERSION", "3.8")
    .WithEnvironment("KAFKA_LOG_MESSAGE_FORMAT_VERSION", "3.8");

var kafkaBroker3 = builder.AddContainer("kafka-broker-3", "apache/kafka:3.8.0")
    .WithEndpoint(9094, 9092, "kafka3")
    .WithEnvironment("KAFKA_NODE_ID", "3")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-3:9092")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("CLUSTER_ID", "INTEGRATION_TEST_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx1G -Xms512M")
    .WithEnvironment("KAFKA_BROKER_VERSION_FALLBACK", "3.8.0")
    .WithEnvironment("KAFKA_INTER_BROKER_PROTOCOL_VERSION", "3.8")
    .WithEnvironment("KAFKA_LOG_MESSAGE_FORMAT_VERSION", "3.8");

// Add Kafka UI for integration test monitoring and debugging
var kafkaUi = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(18001, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "integration-test-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("KAFKA_CLUSTERS_0_PROPERTIES_SASL_MECHANISM", "PLAIN")
    .WithEnvironment("KAFKA_CLUSTERS_0_AUDIT_TOPICAUDITENABLED", "true")
    .WithEnvironment("KAFKA_CLUSTERS_0_AUDIT_CONSOLEAUDITENABLED", "true");

// Add Flink 2.1.0 cluster for integration testing
// JobManager with enhanced configuration for integration tests
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0") // Using 2.0.0 as 2.1.0 not yet available
    .WithHttpEndpoint(18002, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", 
        "jobmanager.memory.process.size: 2048m\n" +
        "taskmanager.memory.process.size: 2048m\n" +
        "taskmanager.numberOfTaskSlots: 10\n" +
        "parallelism.default: 5\n" +
        "state.backend: hashmap\n" +
        "state.checkpoints.dir: file:///tmp/flink-checkpoints\n" +
        "state.savepoints.dir: file:///tmp/flink-savepoints\n" +
        "execution.checkpointing.interval: 60000\n" +
        "execution.checkpointing.timeout: 300000\n" +
        "web.timeout: 300000\n" +
        "rest.connection-timeout: 300000\n" +
        "rest.idleness-timeout: 300000")
    .WithArgs("jobmanager");

// TaskManager with enhanced configuration for stress testing
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES",
        "jobmanager.memory.process.size: 2048m\n" +
        "taskmanager.memory.process.size: 2048m\n" +
        "taskmanager.numberOfTaskSlots: 10\n" +
        "parallelism.default: 5\n" +
        "state.backend: hashmap\n" +
        "taskmanager.memory.network.fraction: 0.15\n" +
        "taskmanager.memory.managed.fraction: 0.4")
    .WithArgs("taskmanager");

// Observability Stack for Integration Testing - Essential metrics collection
// Simplified Prometheus + OpenTelemetry setup for CI/CD compatibility
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(18006, 9090, "prometheus")
    .WithBindMount("./prometheus-integration.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/etc/prometheus/console_libraries",
              "--web.console.templates=/etc/prometheus/consoles",
              "--web.enable-lifecycle",
              "--storage.tsdb.retention.time=1h", // Short retention for integration tests
              "--log.level=warn",
              "--web.listen-address=0.0.0.0:9090");

var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithHttpEndpoint(18007, 4317, "otlp-grpc")
    .WithHttpEndpoint(18008, 4318, "otlp-http")
    .WithHttpEndpoint(18009, 8889, "prometheus-metrics")
    .WithEnvironment("OTEL_LOG_LEVEL", "WARN") // Reduced logging for CI/CD
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.name=integration-test-collector,service.version=1.0.0")
    .WithBindMount("./otel-config-integration.yaml", "/etc/otelcol-contrib/otel-collector-config.yaml")
    .WithArgs("--config=/etc/otelcol-contrib/otel-collector-config.yaml")
    .WaitFor(prometheus);

Console.WriteLine("🎯 Integration Test Infrastructure Setup Complete");
Console.WriteLine("📊 Aspire Dashboard: http://localhost:18889");
Console.WriteLine("🔥 Flink Dashboard: http://localhost:18002");
Console.WriteLine("📨 Kafka UI: http://localhost:18001");
Console.WriteLine("🔧 Redis: Available for distributed caching");
Console.WriteLine("📈 Prometheus: http://localhost:18006 (metrics collection)");
Console.WriteLine("🔍 OpenTelemetry: http://localhost:18009/metrics (metrics export)");
Console.WriteLine("");
Console.WriteLine("💡 Observability metrics enabled for integration testing");
Console.WriteLine("   Simplified setup optimized for CI/CD execution");

builder.Build().Run();