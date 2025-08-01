using Aspire.Hosting;
using InfinityFlow.Aspire.Temporal;

var builder = DistributedApplication.CreateBuilder(args);

// Redis with IPv4 configuration
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")
    .WithEnvironment("REDIS_BIND", "0.0.0.0"); // Force IPv4

// Kafka broker with IPv4 configuration
var kafkaBroker = builder.AddContainer("kafka-broker", "bitnami/kafka:latest")
    .WithEndpoint(9092, 9092, "kafka")
    .WithEnvironment("KAFKA_ENABLE_KRAFT", "yes")
    .WithEnvironment("KAFKA_CFG_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_CFG_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_CFG_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CFG_ADVERTISED_LISTENERS", "PLAINTEXT://kafka-broker:9092")
    .WithEnvironment("KAFKA_CFG_NODE_ID", "1")
    .WithEnvironment("KAFKA_CFG_CONTROLLER_QUORUM_VOTERS", "1@kafka-broker:9093")
    .WithEnvironment("KAFKA_KRAFT_CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_CFG_BROKER_ID", "1")
    .WithEnvironment("KAFKA_CFG_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_CFG_NUM_PARTITIONS", "10")
    .WithEnvironment("KAFKA_CFG_DEFAULT_REPLICATION_FACTOR", "1")
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx512M -Xms256M");

// Kafka UI with IPv4
var kafkaUI = builder.AddContainer("kafka-ui", "provectuslabs/kafka-ui:latest")
    .WithHttpEndpoint(8082, 8080, "kafka-ui")
    .WithEnvironment("KAFKA_CLUSTERS_0_NAME", "local-testing-cluster")
    .WithEnvironment("KAFKA_CLUSTERS_0_BOOTSTRAPSERVERS", "kafka-broker:9092")
    .WithEnvironment("DYNAMIC_CONFIG_ENABLED", "true")
    .WithEnvironment("AUTH_TYPE", "disabled");

// Flink JobManager with IPv4 and proper memory configuration
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.0.0")
    .WithHttpEndpoint(8081, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        jobmanager.memory.process.size: 1024m
        jobmanager.memory.flink.size: 768m
        jobmanager.memory.jvm-overhead.max: 256m
        jobmanager.memory.jvm-overhead.min: 192m
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 2
        parallelism.default: 2
        rest.bind-address: 0.0.0.0
        """)
    .WithArgs("jobmanager");

// Flink TaskManager with IPv4
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.0.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 2
        """)
    .WithArgs("taskmanager");

// Temporal server using InfinityFlow.Aspire.Temporal package - simplified configuration
var temporal = await builder.AddTemporalServerContainer("temporal", config => config
    .WithLogLevel(LogLevel.Info)
    .WithLogFormat(LogFormat.Json)
    .WithNamespace("default"));

// Grafana with IPv4
var grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
    .WithHttpEndpoint(3000, 3000, "grafana")
    .WithEnvironment("GF_SECURITY_ADMIN_PASSWORD", "admin")
    .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
    .WithEnvironment("GF_SERVER_HTTP_ADDR", "0.0.0.0"); // Force IPv4

// LocalTesting Web API with IPv4 configuration
var localTestingApi = builder.AddProject("localtesting-webapi", "../LocalTesting.WebApi/LocalTesting.WebApi.csproj")
    .WithReference(redis)
    .WithReference(temporal)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka-broker:9092")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:5000") // Force IPv4
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();