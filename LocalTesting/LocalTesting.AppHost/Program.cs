// Configure Aspire dashboard and OTLP environment variables
// These settings eliminate the need for manual environment variable setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:4323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:4324");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_URL", "http://localhost:18888");
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");

// Configure OpenTelemetry endpoints for applications
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:4318");
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf");

// Force IPv4 binding to prevent IPv6 address conflicts and DCP connectivity issues
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6", "true");
Environment.SetEnvironmentVariable("ASPNETCORE_PREVENTHOSTINGSTARTUP", "false");

// Additional IPv4 enforcement for DCP API server - system-wide approach
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://127.0.0.1:18888");
Environment.SetEnvironmentVariable("DOTNET_ASPIRE_DASHBOARD_URL", "http://127.0.0.1:18888");

// Force system-wide IPv4 preference to resolve DCP connectivity issues
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_SOCKETSHTTPHANDLER_HTTP2SUPPORT", "false");
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_HTTP_USEIPV6", "false");

// DCP-specific IPv4 enforcement
Environment.SetEnvironmentVariable("DCP_API_SERVER_BIND_ADDRESS", "127.0.0.1");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_BIND_ADDRESS", "127.0.0.1");

var builder = DistributedApplication.CreateBuilder(args);

// Redis with IPv4 configuration
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")
    .WithEnvironment("REDIS_BIND", "0.0.0.0"); // Force IPv4

// 3 Kafka Brokers with KRaft cluster configuration
var kafkaBroker1 = builder.AddContainer("kafka-broker-1", "bitnami/kafka:latest")
    .WithEndpoint(9092, 9092, "kafka1")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-1:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "1")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "15")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M");

var kafkaBroker2 = builder.AddContainer("kafka-broker-2", "bitnami/kafka:latest")
    .WithEndpoint(9093, 9092, "kafka2")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-2:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "2")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "2")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "15")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M");

var kafkaBroker3 = builder.AddContainer("kafka-broker-3", "bitnami/kafka:latest")
    .WithEndpoint(9094, 9092, "kafka3")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker-3:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "3")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "3")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "15")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M");

// Kafka UI with IPv4 - connecting to all 3 brokers
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8082, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-testing-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true")
    .WithEnvironment("AUTH_TYPE", "disabled");

// Flink JobManager with working memory configuration from WI4 success pattern
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0")
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

// Flink TaskManager 1 with simplified working memory configuration
var flinkTaskManager1 = builder.AddContainer("flink-taskmanager-1", "flink:2.0.0")
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

// Flink TaskManager 2 with simplified working memory configuration
var flinkTaskManager2 = builder.AddContainer("flink-taskmanager-2", "flink:2.0.0")
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

// Flink TaskManager 3 with simplified working memory configuration
var flinkTaskManager3 = builder.AddContainer("flink-taskmanager-3", "flink:2.0.0")
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

// Prometheus for metrics collection
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(9090, 9090, "prometheus")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", 
              "--storage.tsdb.path=/prometheus", 
              "--web.console.libraries=/etc/prometheus/console_libraries",
              "--web.console.templates=/etc/prometheus/consoles",
              "--web.enable-lifecycle");

// OpenTelemetry Collector for telemetry processing
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithHttpEndpoint(4317, 4317, "otlp-grpc")
    .WithHttpEndpoint(4318, 4318, "otlp-http")
    .WithHttpEndpoint(8889, 8889, "prometheus-metrics")
    .WithBindMount("./otel-config.yaml", "/etc/otelcol-contrib/otel-collector-config.yaml")
    .WithArgs("--config=/etc/otelcol-contrib/otel-collector-config.yaml");

// Grafana with no authentication required for local testing
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
    .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithEnvironment("GF_SERVER_HTTP_ADDR", "0.0.0.0") // Force IPv4
    .WithBindMount("./grafana-datasources.yml", "/etc/grafana/provisioning/datasources/datasources.yml")
    .WaitFor(prometheus)
    .WaitFor(otelCollector);

// LocalTesting Web API with single HTTP endpoint configuration
var localTestingApi = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("TEMPORAL_SERVER_URL", "temporal-server:7233")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithHttpEndpoint(5000, name: "webapi")
    .WaitFor(flinkJobManager)
    .WaitFor(flinkTaskManager1)
    .WaitFor(flinkTaskManager2)
    .WaitFor(flinkTaskManager3)
    .WaitFor(temporalServer)
    .WaitFor(kafkaBroker1)
    .WaitFor(kafkaBroker2)
    .WaitFor(kafkaBroker3)
    .WaitFor(prometheus)
    .WaitFor(otelCollector)
    .WaitFor(grafana);

builder.Build().Run();
