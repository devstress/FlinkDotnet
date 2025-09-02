// Configure Aspire dashboard and OTLP environment variables
// These settings eliminate the need for manual environment variable setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:4323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:4324");

// Configure Aspire dashboard URL - required for dashboard initialization
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_URL", "http://localhost:18888");

// Disable Aspire dashboard authentication for easier local development access
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_NO_AUTH", "true");

// Configure OpenTelemetry endpoints for applications
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

// Redis with IPv4 configuration
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")
    .WithEnvironment("REDIS_BIND", "0.0.0.0"); // Force IPv4

// 3 Kafka Brokers with KRaft cluster configuration using official Apache Kafka image
var kafkaBroker1 = builder.AddContainer("kafka-broker-1", "apache/kafka:3.8.0")
    .WithEndpoint(9092, 9092, "kafka1")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-1:9092")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M")
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
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M")
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
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M")
    .WithEnvironment("KAFKA_BROKER_VERSION_FALLBACK", "3.8.0")
    .WithEnvironment("KAFKA_INTER_BROKER_PROTOCOL_VERSION", "3.8")
    .WithEnvironment("KAFKA_LOG_MESSAGE_FORMAT_VERSION", "3.8");

// Kafka UI with IPv4 - connecting to all 3 brokers
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8082, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-testing-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true")
    .WithEnvironment("AUTH_TYPE", "disabled");

// Flink JobManager with working memory configuration from WI4 success pattern - Updated to 2.1.0 for latest AI capabilities
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(8081, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        jobmanager.memory.process.size: 1024m
        jobmanager.memory.off-heap.size: 64m
        taskmanager.numberOfTaskSlots: 8
        parallelism.default: 24
        rest.bind-address: 0.0.0.0
        rest.port: 8081
        """)
    .WithArgs("jobmanager");

// Flink TaskManager 1 with simplified working memory configuration - Updated to 2.1.0 for latest AI capabilities
var flinkTaskManager1 = builder.AddContainer("flink-taskmanager-1", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 8
        taskmanager.host: flink-taskmanager-1
        """)
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// Flink TaskManager 2 with simplified working memory configuration - Updated to 2.1.0 for latest AI capabilities
var flinkTaskManager2 = builder.AddContainer("flink-taskmanager-2", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 8
        taskmanager.host: flink-taskmanager-2
        """)
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// Flink TaskManager 3 with simplified working memory configuration - Updated to 2.1.0 for latest AI capabilities
var flinkTaskManager3 = builder.AddContainer("flink-taskmanager-3", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 8
        taskmanager.host: flink-taskmanager-3
        """)
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// PostgreSQL for Temporal storage
var temporalPostgres = builder.AddContainer("temporal-postgres", "postgres:13")
    .WithEnvironment("POSTGRES_DB", "temporal")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PASSWORD", "temporal")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=trust")
    .WithVolume("temporal-postgres-data", "/var/lib/postgresql/data")
    .WithEndpoint(5432, 5432, "postgres");

// Temporal Server for durable execution workflows
var temporalServer = builder.AddContainer("temporal-server", "temporalio/auto-setup:latest")
    .WithHttpEndpoint(7233, 7233, "temporal-server")
    .WithEnvironment("DB", "postgres12")
    .WithEnvironment("DB_PORT", "5432")
    .WithEnvironment("POSTGRES_SEEDS", "temporal-postgres")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PWD", "temporal")
    .WithEnvironment("DBNAME", "temporal")
    .WithEnvironment("VISIBILITY_DBNAME", "temporal_visibility")
    .WithEnvironment("TEMPORAL_CLI_ADDRESS", "temporal-server:7233")
    .WithEnvironment("SERVICES", "history,matching,worker,frontend")
    .WithEnvironment("SKIP_DB_CREATE", "false")
    .WithEnvironment("SKIP_SCHEMA_SETUP", "false")
    .WithEnvironment("ENABLE_ES", "false")
    .WithEnvironment("LOG_LEVEL", "info")
    .WaitFor(temporalPostgres);

// Temporal UI for workflow monitoring
var temporalUI = builder.AddContainer("temporal-ui", "temporalio/ui:latest")
    .WithHttpEndpoint(8084, 8080, "temporal-ui")
    .WithEnvironment("TEMPORAL_ADDRESS", "temporal-server:7233")
    .WithEnvironment("TEMPORAL_CORS_ORIGINS", "http://localhost:8084")
    .WaitFor(temporalServer);

// Loki for centralized log aggregation (part of LGTM stack)
var loki = builder.AddContainer("loki", "grafana/loki:3.0.0")
    .WithHttpEndpoint(3100, 3100, "loki")
    .WithEnvironment("LOKI_ADDR", "0.0.0.0:3100")
    .WithArgs("-config.file=/etc/loki/local-config.yaml");

// Simplified observability - removing Tempo temporarily due to configuration complexity
// Focus on Loki + Grafana + Prometheus for now

// Prometheus for metrics collection (part of LGTM stack)
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(9090, 9090, "prometheus")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/etc/prometheus/console_libraries",
              "--web.console.templates=/etc/prometheus/consoles",
              "--web.enable-lifecycle");

// OpenTelemetry Collector for telemetry processing with simplified configuration
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithHttpEndpoint(4317, 4317, "otlp-grpc")
    .WithHttpEndpoint(4318, 4318, "otlp-http")
    .WithHttpEndpoint(8889, 8889, "prometheus-metrics")
    .WithEnvironment("LOKI_ENDPOINT", "http://loki:3100/loki/api/v1/push")
    .WithEnvironment("PROMETHEUS_ENDPOINT", "http://prometheus:9090/api/v1/write")
    .WaitFor(loki)
    .WaitFor(prometheus);

// Grafana with LGTM stack integration (no authentication for local testing)
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithEnvironment("GF_SERVER_HTTP_ADDR", "0.0.0.0") // Force IPv4
    .WithEnvironment("LOKI_URL", "http://loki:3100")
    .WithEnvironment("PROMETHEUS_URL", "http://prometheus:9090")
    .WithBindMount("./grafana-datasources.yml", "/etc/grafana/provisioning/datasources/datasources.yml")
    .WaitFor(loki)
    .WaitFor(prometheus)
    .WaitFor(otelCollector);

// LocalTesting Web API with LGTM observability integration
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("KAFKA_DEFAULT_PARTITIONS", "10")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("TEMPORAL_SERVER_URL", "temporal-server:7233")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithEnvironment("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://otel-collector:4317")
    .WithEnvironment("LOKI_ENDPOINT", "http://loki:3100")
    .WithEnvironment("GRAFANA_URL", "http://grafana:3000")
    .WithEnvironment("PROMETHEUS_URL", "http://prometheus:9090")
    .WithHttpEndpoint(5000, 5001, name: "webapi") // External port 5000 -> Internal port 5001
    .WaitFor(flinkJobManager)
    .WaitFor(flinkTaskManager1)
    .WaitFor(flinkTaskManager2)
    .WaitFor(flinkTaskManager3)
    .WaitFor(temporalServer)
    .WaitFor(kafkaBroker1)
    .WaitFor(kafkaBroker2)
    .WaitFor(kafkaBroker3)
    .WaitFor(loki)
    .WaitFor(prometheus)
    .WaitFor(otelCollector)
    .WaitFor(grafana);

builder.Build().Run();
