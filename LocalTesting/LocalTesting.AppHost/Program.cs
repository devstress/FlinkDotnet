using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Create 3 Kafka brokers with KRaft mode for production-like environment
var kafkaBroker1 = builder.AddContainer("kafka-broker-1", "bitnami/kafka:latest")
    .WithEndpoint(9092, 9092, "kafka")
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
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    // Production-grade topic configuration for 3-broker cluster
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "100")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_CFG_MIN_INSYNC_REPLICAS", "2")
    // Performance optimizations for stress testing
    .WithEnvironment("KAFKA_CFG_LOG_RETENTION_HOURS", "168")
    .WithEnvironment("KAFKA_CFG_LOG_SEGMENT_BYTES", "134217728")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MESSAGES", "10000")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MS", "500")
    .WithEnvironment("KAFKA_CFG_MESSAGE_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_REPLICA_FETCH_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_SOCKET_REQUEST_MAX_BYTES", "268435456")
    .WithEnvironment("KAFKA_CFG_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    // JVM optimization for stress testing (8GB heap per broker)
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx8G -Xms8G")
    .WithEnvironment("KAFKA_CFG_NUM_IO_THREADS", "64")
    .WithEnvironment("KAFKA_CFG_NUM_NETWORK_THREADS", "16")
    .WithEnvironment("KAFKA_CFG_NUM_REPLICA_FETCHERS", "8")
    .WithEnvironment("KAFKA_CFG_SOCKET_SEND_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_SOCKET_RECEIVE_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_UNCLEAN_LEADER_ELECTION_ENABLE", "false")
    .WithEnvironment("KAFKA_CFG_LOG4J_ROOT_LOGLEVEL", "WARN");

var kafkaBroker2 = builder.AddContainer("kafka-broker-2", "bitnami/kafka:latest")
    .WithEndpoint(9093, 9092, "kafka")
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
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    // Same configuration as broker 1
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "100")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_CFG_MIN_INSYNC_REPLICAS", "2")
    .WithEnvironment("KAFKA_CFG_LOG_RETENTION_HOURS", "168")
    .WithEnvironment("KAFKA_CFG_LOG_SEGMENT_BYTES", "134217728")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MESSAGES", "10000")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MS", "500")
    .WithEnvironment("KAFKA_CFG_MESSAGE_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_REPLICA_FETCH_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_SOCKET_REQUEST_MAX_BYTES", "268435456")
    .WithEnvironment("KAFKA_CFG_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx8G -Xms8G")
    .WithEnvironment("KAFKA_CFG_NUM_IO_THREADS", "64")
    .WithEnvironment("KAFKA_CFG_NUM_NETWORK_THREADS", "16")
    .WithEnvironment("KAFKA_CFG_NUM_REPLICA_FETCHERS", "8")
    .WithEnvironment("KAFKA_CFG_SOCKET_SEND_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_SOCKET_RECEIVE_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_UNCLEAN_LEADER_ELECTION_ENABLE", "false")
    .WithEnvironment("KAFKA_CFG_LOG4J_ROOT_LOGLEVEL", "WARN");

var kafkaBroker3 = builder.AddContainer("kafka-broker-3", "bitnami/kafka:latest")
    .WithEndpoint(9094, 9092, "kafka")
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
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    // Same configuration as broker 1 & 2
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "100")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "3")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR", "2")
    .WithEnvironment("KAFKA_CFG_MIN_INSYNC_REPLICAS", "2")
    .WithEnvironment("KAFKA_CFG_LOG_RETENTION_HOURS", "168")
    .WithEnvironment("KAFKA_CFG_LOG_SEGMENT_BYTES", "134217728")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MESSAGES", "10000")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MS", "500")
    .WithEnvironment("KAFKA_CFG_MESSAGE_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_REPLICA_FETCH_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_SOCKET_REQUEST_MAX_BYTES", "268435456")
    .WithEnvironment("KAFKA_CFG_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx8G -Xms8G")
    .WithEnvironment("KAFKA_CFG_NUM_IO_THREADS", "64")
    .WithEnvironment("KAFKA_CFG_NUM_NETWORK_THREADS", "16")
    .WithEnvironment("KAFKA_CFG_NUM_REPLICA_FETCHERS", "8")
    .WithEnvironment("KAFKA_CFG_SOCKET_SEND_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_SOCKET_RECEIVE_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_UNCLEAN_LEADER_ELECTION_ENABLE", "false")
    .WithEnvironment("KAFKA_CFG_LOG4J_ROOT_LOGLEVEL", "WARN");

// Kafka UI for management and monitoring
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8082, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-testing-3-broker-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true")
    .WithEnvironment("AUTH_TYPE", "disabled")
    .WithEnvironment("MANAGEMENT_HEALTH_LDAP_ENABLED", "false");

// Redis for caching and state management
var redis = builder.AddRedis("redis");

// Add Flink JobManager with enhanced configuration
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0")
    .WithHttpEndpoint(8081, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        jobmanager.memory.process.size: 4096m
        taskmanager.memory.process.size: 8192m
        taskmanager.numberOfTaskSlots: 10
        parallelism.default: 100
        state.backend: rocksdb
        state.backend.incremental: true
        execution.checkpointing.interval: 10000
        execution.checkpointing.mode: EXACTLY_ONCE
        table.exec.source.idle-timeout: 30s
        """)
    .WithArgs("jobmanager");

// Add 3 Flink TaskManagers for better parallelism
var flinkTaskManager1 = builder.AddContainer("flink-taskmanager-1", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 8192m
        taskmanager.numberOfTaskSlots: 10
        """)
    .WithArgs("taskmanager");

var flinkTaskManager2 = builder.AddContainer("flink-taskmanager-2", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 8192m
        taskmanager.numberOfTaskSlots: 10
        """)
    .WithArgs("taskmanager");

var flinkTaskManager3 = builder.AddContainer("flink-taskmanager-3", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 8192m
        taskmanager.numberOfTaskSlots: 10
        """)
    .WithArgs("taskmanager");

// Flink SQL Gateway for SQL dashboard functionality
var flinkSqlGateway = builder.AddContainer("flink-sql-gateway", "flink:2.0.0")
    .WithHttpEndpoint(8083, 8083, "sql-gateway")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        sql-gateway.endpoint.rest.address: 0.0.0.0
        sql-gateway.endpoint.rest.port: 8083
        """)
    .WithArgs("sql-gateway");

// OpenTelemetry Collector for observability
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithHttpEndpoint(4321, 4317, "otlp-grpc")
    .WithHttpEndpoint(4322, 4318, "otlp-http")
    .WithHttpEndpoint(8888, 8888, "metrics")
    .WithHttpEndpoint(8889, 8889, "prometheus")
    .WithBindMount("./otel-config.yaml", "/etc/otelcol-contrib/otel-collector-config.yaml", isReadOnly: true)
    .WithArgs("--config=/etc/otelcol-contrib/otel-collector-config.yaml");

// Prometheus for metrics storage
var prometheus = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(9090, 9090, "prometheus")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml", isReadOnly: true)
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml", "--storage.tsdb.path=/prometheus", "--web.console.libraries=/etc/prometheus/console_libraries", "--web.console.templates=/etc/prometheus/consoles");

// Grafana for visualization dashboards
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithBindMount("./grafana-datasources.yml", "/etc/grafana/provisioning/datasources/datasources.yml", isReadOnly: true);

// PostgreSQL for Temporal storage (defined first to establish dependency order)
var temporalPostgres = builder.AddContainer("temporal-postgres", "postgres:13")
    .WithEnvironment("POSTGRES_DB", "temporal")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PASSWORD", "temporal")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithVolume("temporal-postgres-data", "/var/lib/postgresql/data");

// Temporal Server for durable execution workflows (depends on PostgreSQL)
var temporalServer = builder.AddContainer("temporal-server", "temporalio/auto-setup:latest")
    .WithHttpEndpoint(7233, 7233, "temporal-server")
    .WithEnvironment("DB", "postgresql")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PWD", "temporal")
    .WithEnvironment("POSTGRES_SEEDS", "temporal-postgres")
    .WithEnvironment("SQL_USER", "temporal")
    .WithEnvironment("SQL_PASSWORD", "temporal")
    .WithEnvironment("DYNAMIC_CONFIG_FILE_PATH", "config/dynamicconfig/development.yaml");

// Temporal UI for workflow monitoring (depends on Temporal server)
var temporalUI = builder.AddContainer("temporal-ui", "temporalio/ui:latest")
    .WithHttpEndpoint(8084, 8080, "temporal-ui")
    .WithEnvironment("TEMPORAL_ADDRESS", "temporal-server:7233")
    .WithEnvironment("TEMPORAL_CORS_ORIGINS", "http://localhost:8084");

// LocalTesting Web API with Swagger
var localTestingApi = builder.AddProject("localtesting-webapi", "../LocalTesting.WebApi/LocalTesting.WebApi.csproj")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("FLINK_SQL_GATEWAY_URL", "http://flink-sql-gateway:8083")
    .WithEnvironment("TEMPORAL_SERVER_URL", "temporal-server:7233")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4322")
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();