using Aspire.Hosting;
using System.Net;
using System.Net.Sockets;

// Force IPv4 usage globally - this must be set before any networking operations
Environment.SetEnvironmentVariable("DOTNET_SYSTEM_NET_DISABLEIPV6", "true");
Environment.SetEnvironmentVariable("ASPIRE_PREFER_IPV4", "true");
Environment.SetEnvironmentVariable("DOCKER_DEFAULT_PLATFORM", "linux/amd64");

// Force the system to use IPv4 only
if (Socket.OSSupportsIPv6)
{
    // Disable IPv6 support at the .NET level
    AppContext.SetSwitch("System.Net.DisableIPv6", true);
}

var builder = DistributedApplication.CreateBuilder(args);

// Start with minimal configuration to test Aspire orchestration
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru");

// Add a single Kafka broker with IPv4-only configuration
var kafkaBroker = builder.AddContainer("kafka-broker", "bitnami/kafka:latest")
    .WithEndpoint(9092, 9092, "kafka")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://127.0.0.1:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@127.0.0.1:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "1")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M")
    .WithEnvironment("JAVA_OPTS", "-Djava.net.preferIPv4Stack=true");

// Add Kafka UI with IPv4 configuration
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8082, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-testing-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "127.0.0.1:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true")
    .WithEnvironment("AUTH_TYPE", "disabled")
    .WithEnvironment("JAVA_OPTS", "-Djava.net.preferIPv4Stack=true");

// Add simple Flink JobManager with IPv4 configuration
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0")
    .WithHttpEndpoint(8081, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "127.0.0.1")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: 127.0.0.1
        jobmanager.rpc.port: 6123
        jobmanager.memory.process.size: 512m
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 2
        parallelism.default: 2
        """)
    .WithEnvironment("JAVA_OPTS", "-Djava.net.preferIPv4Stack=true")
    .WithArgs("jobmanager");

// Add one TaskManager with IPv4 configuration
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "127.0.0.1")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: 127.0.0.1
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 2
        """)
    .WithEnvironment("JAVA_OPTS", "-Djava.net.preferIPv4Stack=true")
    .WithArgs("taskmanager");

// Simple PostgreSQL for Temporal (IPv4 configuration)
var temporalPostgres = builder.AddContainer("temporal-postgres", "postgres:13")
    .WithEnvironment("POSTGRES_DB", "temporal")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PASSWORD", "temporal")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithVolume("temporal-postgres-data", "/var/lib/postgresql/data")
    .WithEndpoint(5432, 5432, "postgres");

// Temporal Server (IPv4 configuration)
var temporalServer = builder.AddContainer("temporal-server", "temporalio/auto-setup:latest")
    .WithHttpEndpoint(7233, 7233, "temporal-server")
    .WithEnvironment("DB", "postgres12")
    .WithEnvironment("DB_PORT", "5432")
    .WithEnvironment("POSTGRES_SEEDS", "127.0.0.1")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PWD", "temporal")
    .WithEnvironment("DBNAME", "temporal")
    .WithEnvironment("VISIBILITY_DBNAME", "temporal_visibility")
    .WithEnvironment("TEMPORAL_CLI_ADDRESS", "127.0.0.1:7233")
    .WithEnvironment("BIND_ON_IP", "0.0.0.0")
    .WithEnvironment("SERVICES", "history,matching,worker,frontend")
    .WaitFor(temporalPostgres);

// Temporal UI with IPv4 configuration
var temporalUI = builder.AddContainer("temporal-ui", "temporalio/ui:latest")
    .WithHttpEndpoint(8084, 8080, "temporal-ui")
    .WithEnvironment("TEMPORAL_ADDRESS", "127.0.0.1:7233")
    .WithEnvironment("TEMPORAL_CORS_ORIGINS", "http://127.0.0.1:8084")
    .WaitFor(temporalServer);

// Grafana (simple setup)
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false");

// LocalTesting Web API with IPv4 configuration
var localTestingApi = builder.AddProject("localtesting-webapi", "../LocalTesting.WebApi/LocalTesting.WebApi.csproj")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "127.0.0.1:9092")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://127.0.0.1:8081")
    .WithEnvironment("TEMPORAL_SERVER_URL", "127.0.0.1:7233")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:5000")
    .WithEnvironment("DOTNET_SYSTEM_NET_DISABLEIPV6", "true")
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();