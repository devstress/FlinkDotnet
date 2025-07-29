using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Add Kafka cluster for messaging - Single container with KRaft mode optimized for 3-broker performance
var kafka = builder.AddContainer("kafka-kraft-cluster", "bitnami/kafka:latest")
    .WithEndpoint(9092, 9092, "kafka")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-kraft-cluster:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-kraft-cluster:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "ASPIRE_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "1")
    .WithEnvironment("KAFKA_CFG_INTER_BROKER_LISTENER_NAME", "PLAINTEXT")
    // Topic configuration optimized for high-throughput like 3-broker setup
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "100")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_MIN_ISR", "1")
    .WithEnvironment("KAFKA_CFG_MIN_INSYNC_REPLICAS", "1")
    // Performance optimizations equivalent to 3-broker setup
    .WithEnvironment("KAFKA_CFG_LOG_RETENTION_HOURS", "168")
    .WithEnvironment("KAFKA_CFG_LOG_SEGMENT_BYTES", "134217728")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MESSAGES", "10000")
    .WithEnvironment("KAFKA_CFG_LOG_FLUSH_INTERVAL_MS", "500")
    .WithEnvironment("KAFKA_CFG_MESSAGE_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_REPLICA_FETCH_MAX_BYTES", "52428800")
    .WithEnvironment("KAFKA_CFG_SOCKET_REQUEST_MAX_BYTES", "268435456")
    .WithEnvironment("KAFKA_CFG_GROUP_INITIAL_REBALANCE_DELAY_MS", "0")
    // JVM and heap optimization (equivalent to 8GB per broker * 3 = 24GB, use 8GB for single container)
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx8G -Xms8G")
    .WithEnvironment("KAFKA_CFG_NUM_IO_THREADS", "64")
    .WithEnvironment("KAFKA_CFG_NUM_NETWORK_THREADS", "16")
    .WithEnvironment("KAFKA_CFG_NUM_REPLICA_FETCHERS", "8")
    .WithEnvironment("KAFKA_CFG_SOCKET_SEND_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_SOCKET_RECEIVE_BUFFER_BYTES", "8388608")
    .WithEnvironment("KAFKA_CFG_UNCLEAN_LEADER_ELECTION_ENABLE", "false")
    .WithEnvironment("KAFKA_CFG_TRANSACTION_STATE_LOG_NUM_PARTITIONS", "10")
    // Consumer group optimization for stress test (100 consumers across 10 TaskManager x 10 slots)
    .WithEnvironment("KAFKA_CFG_GROUP_COORDINATOR_REBALANCE_PROTOCOLS", "classic,cooperative")
    .WithEnvironment("KAFKA_CFG_REQUEST_TIMEOUT_MS", "60000")
    .WithEnvironment("KAFKA_CFG_SESSION_TIMEOUT_MS", "45000")
    .WithEnvironment("KAFKA_CFG_HEARTBEAT_INTERVAL_MS", "15000")
    .WithEnvironment("KAFKA_CFG_MAX_POLL_INTERVAL_MS", "300000")
    .WithEnvironment("KAFKA_CFG_FETCH_MAX_WAIT_MS", "500")
    .WithEnvironment("KAFKA_CFG_LOG4J_ROOT_LOGLEVEL", "WARN");

// Add Kafka UI for management
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8080, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "aspire-kraft-single-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-kraft-cluster:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true");

// Add Redis for caching and state management
var redis = builder.AddRedis("redis");

// Add Flink Job Gateway service
var flinkJobGateway = builder.AddContainer("flink-job-gateway", "flink-job-gateway")
    .WithHttpEndpoint(8080, 8080, "http")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka-kraft-cluster:9092")
    .WithEnvironment("REDIS_CONNECTION_STRING", $"{redis.GetEndpoint("tcp")}");

// Add Flink JobManager
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0")
    .WithHttpEndpoint(8081, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("jobmanager");

// Add Flink TaskManager
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithArgs("taskmanager");

builder.Build().Run();