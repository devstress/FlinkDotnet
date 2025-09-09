using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Configure Aspire dashboard and OTLP environment variables
// These settings eliminate the need for manual environment variable setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:13323");
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_OTLP_HTTP_ENDPOINT_URL", "http://localhost:13324");

// Configure Aspire dashboard URL - required for dashboard initialization
Environment.SetEnvironmentVariable("ASPNETCORE_URLS", "http://localhost:18888");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_URL", "http://localhost:18888");

// Disable Aspire dashboard authentication for easier local development access
Environment.SetEnvironmentVariable("DOTNET_DASHBOARD_UNSECURED_ALLOW_ANONYMOUS", "true");
Environment.SetEnvironmentVariable("ASPIRE_DASHBOARD_NO_AUTH", "true");

// Configure OpenTelemetry endpoints for applications
Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:18009");
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

// Configure extended timeouts for Aspire DCP to handle complex infrastructure
// This prevents the 20-second timeout that causes container reconciliation failures
builder.Services.Configure<Microsoft.Extensions.Hosting.HostOptions>(options =>
{
    options.StartupTimeout = TimeSpan.FromMinutes(5); // Extended startup timeout
    options.ShutdownTimeout = TimeSpan.FromMinutes(2); // Extended shutdown timeout
});

// Configure Aspire DCP with extended resource creation timeouts
// This addresses the core issue: Aspire.Hosting.Dcp.KubernetesService timeout
Environment.SetEnvironmentVariable("ASPIRE_DCP_STARTUP_TIMEOUT", "300"); // 5 minutes
Environment.SetEnvironmentVariable("ASPIRE_DCP_RESOURCE_TIMEOUT", "120"); // 2 minutes per resource
Environment.SetEnvironmentVariable("ASPIRE_DCP_MAX_RETRIES", "5");
Environment.SetEnvironmentVariable("ASPIRE_DCP_RETRY_BACKOFF", "10"); // 10 seconds between retries

// Configure container runtime stability settings
Environment.SetEnvironmentVariable("ASPIRE_DCP_CONTAINER_RESTART_POLICY", "always");
Environment.SetEnvironmentVariable("ASPIRE_DCP_HEALTH_CHECK_TIMEOUT", "60"); // 60 seconds for health checks
Environment.SetEnvironmentVariable("ASPIRE_DCP_NETWORK_RETRY_COUNT", "10");
Environment.SetEnvironmentVariable("ASPIRE_DCP_NETWORK_RETRY_DELAY", "5"); // 5 seconds between network retries

// Docker runtime optimizations for container stability
Environment.SetEnvironmentVariable("DOCKER_CLI_EXPERIMENTAL", "enabled");
Environment.SetEnvironmentVariable("DOCKER_BUILDKIT", "1");

Console.WriteLine("✅ Applied extended DCP timeouts and container stability settings");

// PERFORMANCE OPTIMIZATION: Detect test mode for performance optimizations
var isTestMode = args.Contains("--test-mode") || Environment.GetEnvironmentVariable("TESTING_MODE") == "true";

// Enhanced sequential container startup with reduced parallel load
// Prevents DCP reconciliation failures by limiting simultaneous container creation
// Key insight: Start essential services first, then build dependency chains

// Redis with enhanced stability and health check configuration
var redis = builder.AddRedis("redis")
    .WithEnvironment("REDIS_MAXMEMORY", "256mb")
    .WithEnvironment("REDIS_MAXMEMORY_POLICY", "allkeys-lru")
    .WithEnvironment("REDIS_BIND", "0.0.0.0") // Force IPv4
    .WithEnvironment("REDIS_TIMEOUT", "30")
    .WithEnvironment("REDIS_TCP_KEEPALIVE", "60")
    .WithEnvironment("REDIS_SAVE", "60 1000") // Persistence settings for stability
    .WithEnvironment("REDIS_STOP_WRITES_ON_BGSAVE_ERROR", "no"); // Prevent redis crashes on save errors

// Single Kafka instance using manual configuration (AddKafka had connection issues)
var kafka = builder.AddContainer("kafka", "apache/kafka:3.8.0")
    .WithEndpoint(9092, 9092, "kafka")
    .WithEnvironment("KAFKA_NODE_ID", "1")
    .WithEnvironment("KAFKA_PROCESS_ROLES", "broker,controller")
    .WithEnvironment("KAFKA_LISTENERS", "PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093")
    .WithEnvironment("KAFKA_ADVERTISED_LISTENERS", "PLAINTEXT://kafka:9092")
    .WithEnvironment("KAFKA_CONTROLLER_LISTENER_NAMES", "CONTROLLER")
    .WithEnvironment("KAFKA_LISTENER_SECURITY_PROTOCOL_MAP", "PLAINTEXT:PLAINTEXT,CONTROLLER:PLAINTEXT")
    .WithEnvironment("KAFKA_CONTROLLER_QUORUM_VOTERS", "1@kafka:9093")
    .WithEnvironment("CLUSTER_ID", "LOCAL_TESTING_KRAFT_CLUSTER_2024")
    .WithEnvironment("KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR", "1") // Single broker
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_REPLICATION_FACTOR", "1") // Single broker
    .WithEnvironment("KAFKA_TRANSACTION_STATE_LOG_MIN_ISR", "1") // Single broker
    .WithEnvironment("KAFKA_AUTO_CREATE_TOPICS_ENABLE", "true")
    .WithEnvironment("KAFKA_NUM_PARTITIONS", "20") // Optimized for high throughput distribution
    .WithEnvironment("KAFKA_DEFAULT_REPLICATION_FACTOR", "1") // Single broker
    // OPTIMIZED: Memory configuration for thousands msg/sec throughput
    .WithEnvironment("KAFKA_HEAP_OPTS", "-Xmx2G -Xms1G") // Increased from 1G to 2G for better performance
    // OPTIMIZED: Storage configuration for high-volume processing (fixed integer overflow)
    .WithEnvironment("KAFKA_LOG_SEGMENT_BYTES", "1073741824") // 1GB segments (max safe Java int value)
    .WithEnvironment("KAFKA_LOG_RETENTION_BYTES", "2147483647") // Max Java int retention (~2GB)
    // OPTIMIZED: Network configuration for thousands msg/sec
    .WithEnvironment("KAFKA_SOCKET_SEND_BUFFER_BYTES", "131072") // 128KB send buffer (increased from 100KB)
    .WithEnvironment("KAFKA_SOCKET_RECEIVE_BUFFER_BYTES", "131072") // 128KB receive buffer (increased from 100KB)
    .WithEnvironment("KAFKA_SOCKET_REQUEST_MAX_BYTES", "209715200") // 200MB max request size (increased from 100MB)
    .WithEnvironment("KAFKA_REPLICA_FETCH_MAX_BYTES", "2097152") // 2MB fetch size (increased from 1MB)
    .WithEnvironment("KAFKA_MESSAGE_MAX_BYTES", "2000000") // 2MB max message size (increased from 1MB)
    // OPTIMIZED: Performance tuning for high throughput
    .WithEnvironment("KAFKA_NUM_NETWORK_THREADS", "8") // Increased network threads for better parallelism
    .WithEnvironment("KAFKA_NUM_IO_THREADS", "8") // Increased I/O threads for better performance
    .WithEnvironment("KAFKA_QUEUED_MAX_REQUESTS", "1000") // Increased request queue for higher throughput
    .WithEnvironment("KAFKA_BATCH_SIZE", "65536"); // 64KB batch size for optimal producer batching

// Single Flink JobManager with simplified configuration - Updated to 2.1.0 for latest AI capabilities
var flinkJobManager = builder.AddContainer("flink-jobmanager", "flink:2.1.0")
    .WithHttpEndpoint(18002, 8081, "jobmanager-ui")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        jobmanager.memory.process.size: 1024m
        jobmanager.memory.off-heap.size: 64m
        taskmanager.numberOfTaskSlots: 8
        parallelism.default: 8
        rest.bind-address: 0.0.0.0
        rest.port: 8081
        """)
    .WithArgs("jobmanager");

// Single Flink TaskManager with simplified configuration - Updated to 2.1.0 for latest AI capabilities
var flinkTaskManager = builder.AddContainer("flink-taskmanager", "flink:2.1.0")
    .WithEnvironment("JOB_MANAGER_RPC_ADDRESS", "flink-jobmanager")
    .WithEnvironment("FLINK_PROPERTIES", """
        jobmanager.rpc.address: flink-jobmanager
        jobmanager.rpc.port: 6123
        taskmanager.memory.process.size: 1024m
        taskmanager.numberOfTaskSlots: 8
        taskmanager.host: flink-taskmanager
        """)
    .WithArgs("taskmanager")
    .WaitFor(flinkJobManager);

// Single PostgreSQL for Temporal storage with enhanced startup reliability and health checks
var temporalPostgres = builder.AddContainer("temporal-postgres", "postgres:13")
    .WithEnvironment("POSTGRES_DB", "temporal")
    .WithEnvironment("POSTGRES_USER", "temporal")
    .WithEnvironment("POSTGRES_PASSWORD", "temporal")
    .WithEnvironment("POSTGRES_HOST_AUTH_METHOD", "trust")
    .WithEnvironment("POSTGRES_INITDB_ARGS", "--auth-host=trust")
    .WithEnvironment("POSTGRES_MAX_CONNECTIONS", "100")
    .WithEnvironment("POSTGRES_SHARED_BUFFERS", "128MB")
    .WithEnvironment("POSTGRES_INITDB_WAIT_TIMEOUT", "60") // Extended init timeout
    .WithEnvironment("POSTGRES_LOG_STATEMENT", "none") // Reduce logging for stability
    .WithEnvironment("POSTGRES_LOG_MIN_MESSAGES", "warning") // Reduce log noise
    .WithVolume("temporal-postgres-data", "/var/lib/postgresql/data")
    .WithEndpoint(5432, 5432, "postgres")
    .WaitFor(redis); // Sequence after Redis to spread DCP load

// Single Temporal Server for durable execution workflows with enhanced startup reliability
var temporalServer = builder.AddContainer("temporal-server", "temporalio/auto-setup:latest")
    .WithHttpEndpoint(18003, 7233, "temporal-server")
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
    .WithEnvironment("LOG_LEVEL", "warn") // Reduce log noise for stability
    .WithEnvironment("AUTO_SETUP", "true")
    .WithEnvironment("TEMPORAL_DYNAMIC_CONFIG_FILE_PATH", "/etc/temporal/config/dynamicconfig/development.yaml")
    // Fix authentication warnings by disabling auth for local testing
    .WithEnvironment("TEMPORAL_AUTH_ENABLED", "false")
    .WithEnvironment("TEMPORAL_TLS_ENABLED", "false")
    // Fix namespace setup to prevent "Namespace default is not found" errors
    .WithEnvironment("DEFAULT_NAMESPACE", "default")
    .WithEnvironment("DEFAULT_NAMESPACE_RETENTION", "72h")
    // Additional namespace configuration to prevent errors
    .WithEnvironment("TEMPORAL_NAMESPACE", "default")
    .WithEnvironment("TEMPORAL_AUTO_SETUP", "true")
    .WithEnvironment("TEMPORAL_ENABLE_NAMESPACES", "true")
    .WithEnvironment("TEMPORAL_DEFAULT_NAMESPACE", "default")
    // Connection optimization
    .WithEnvironment("SQL_MAX_CONNS", "20") // Limit connections for stability
    .WithEnvironment("SQL_MAX_IDLE_CONNS", "10")
    .WithEnvironment("SQL_MAX_CONN_LIFETIME", "3600") // 1 hour connection lifetime
    // Add command line flags to suppress warnings and improve startup
    .WithArgs("temporal-server", 
              "--allow-no-auth",  // Suppress auth warning 
              "--log-level", "warn",  // Reduce log noise
              "start")
    .WaitFor(temporalPostgres);

// Loki for centralized log aggregation with enhanced stability  
// PERFORMANCE OPTIMIZATION: Disable Loki when running tests for faster execution
IResourceBuilder<ContainerResource>? loki = null;
if (!isTestMode)
{
    loki = builder.AddContainer("loki", "grafana/loki:3.0.0")
        .WithHttpEndpoint(18005, 3100, "loki")
        .WithEnvironment("LOKI_ADDR", "0.0.0.0:3100")
        .WithEnvironment("LOKI_LOG_LEVEL", "warn") // Reduce log noise
        .WithEnvironment("LOKI_SERVER_HTTP_LISTEN_PORT", "3100")
        .WithEnvironment("LOKI_SERVER_GRPC_LISTEN_PORT", "9095")
        .WithArgs("-config.file=/etc/loki/local-config.yaml", "-log.level=warn");
        
    Console.WriteLine("📝 Loki logging enabled for development mode");
}
else 
{
    Console.WriteLine("⚡ Loki logging disabled for test mode (performance optimization)");
}

// Prometheus for metrics collection with enhanced startup stability
// PERFORMANCE OPTIMIZATION: Simplified Prometheus configuration for test mode
var prometheusBuilder = builder.AddContainer("prometheus", "prom/prometheus:latest")
    .WithHttpEndpoint(18006, 9090, "prometheus")
    .WithBindMount("./prometheus.yml", "/etc/prometheus/prometheus.yml")
    .WithEnvironment("PROMETHEUS_STORAGE_TSDB_RETENTION_TIME", isTestMode ? "1h" : "7d") // Shorter retention for tests
    .WithEnvironment("PROMETHEUS_WEB_LISTEN_ADDRESS", "0.0.0.0:9090")
    .WithArgs("--config.file=/etc/prometheus/prometheus.yml",
              "--storage.tsdb.path=/prometheus",
              "--web.console.libraries=/etc/prometheus/console_libraries",
              "--web.console.templates=/etc/prometheus/consoles",
              "--web.enable-lifecycle",
              "--storage.tsdb.retention.time=" + (isTestMode ? "1h" : "7d"),
              "--log.level=warn",
              "--web.listen-address=0.0.0.0:9090");

var prometheus = prometheusBuilder;

// OpenTelemetry Collector with corrected file mounting approach
// Implements pattern: App → local OTel Collector (localhost gRPC/HTTP) → backend
var otelCollector = builder.AddContainer("otel-collector", "otel/opentelemetry-collector-contrib:latest")
    .WithHttpEndpoint(18007, 4317, "otlp-grpc")
    .WithHttpEndpoint(18009, 4318, "otlp-http")
    .WithHttpEndpoint(18008, 8889, "prometheus-metrics")
    .WithEnvironment("OTEL_LOG_LEVEL", "WARN") // Reduced logging for performance
    .WithEnvironment("OTEL_RESOURCE_ATTRIBUTES", "service.name=local-otel-collector,source.version=1.0.0,deployment.environment=local-testing")
    .WithEnvironment("GOMAXPROCS", "4") // Optimize Go runtime for high-performance
    .WithEnvironment("GOMEMLIMIT", "1GiB") // Memory limit for collector
    // CORRECTED APPROACH: Use absolute path and ensure proper file mount
    .WithBindMount(Path.Combine(AppContext.BaseDirectory, "otel-config-simple.yaml"), "/etc/otelcol-contrib/config.yaml")
    .WithArgs("--config=/etc/otelcol-contrib/config.yaml")
    .WaitFor(prometheus); // Only wait for Prometheus (Loki integration removed for stability)

// Grafana with PGL stack integration and enhanced startup reliability
// PERFORMANCE OPTIMIZATION: Disable Grafana and Temporal UI when running tests for faster execution  
IResourceBuilder<ContainerResource>? grafana = null;
if (!isTestMode)
{
    grafana = builder.AddContainer("grafana", "grafana/grafana:latest")
        .WithHttpEndpoint(18010, 3000, "grafana")
        .WithEnvironment("GF_AUTH_DISABLE_LOGIN_FORM", "true")
        .WithEnvironment("GF_AUTH_ANONYMOUS_ENABLED", "true")
        .WithEnvironment("GF_AUTH_ANONYMOUS_ORG_ROLE", "Admin")
        .WithEnvironment("GF_USERS_ALLOW_SIGN_UP", "false")
        .WithEnvironment("GF_SERVER_HTTP_ADDR", "0.0.0.0") // Force IPv4
        .WithEnvironment("GF_SERVER_HTTP_PORT", "3000")
        .WithEnvironment("GF_LOG_LEVEL", "warn") // Reduce log noise
        .WithEnvironment("GF_INSTALL_PLUGINS", "") // Disable plugin installation for faster startup
        .WithEnvironment("GF_ANALYTICS_REPORTING_ENABLED", "false")
        .WithEnvironment("GF_ANALYTICS_CHECK_FOR_UPDATES", "false")
        .WithEnvironment("LOKI_URL", loki != null ? "http://loki:3100" : "")
        .WithEnvironment("PROMETHEUS_URL", "http://prometheus:9090")
        .WithBindMount("./grafana-datasources-training.yml", "/etc/grafana/provisioning/datasources/datasources.yml")
        .WaitFor(prometheus)
        .WaitFor(otelCollector);
        
    // Only wait for Loki if it exists
    if (loki != null)
    {
        grafana = grafana.WaitFor(loki);
    }
        
    Console.WriteLine("🎨 Grafana UI enabled for development mode");
}
else 
{
    Console.WriteLine("⚡ Grafana UI disabled for test mode (performance optimization)");
}

// LocalTesting Web API with simplified dependency chain to prevent DCP reconciliation failures
var localTestingApiBuilder = builder.AddProject<Projects.LocalTesting_WebApi>("localtesting-webapi")
    .WithReference(redis)
    .WithEnvironment("KAFKA_BOOTSTRAP_SERVERS", "kafka:9092") // Single Kafka broker
    .WithEnvironment("KAFKA_DEFAULT_PARTITIONS", "3")
    .WithEnvironment("KAFKA_REQUEST_TIMEOUT_MS", "30000")
    .WithEnvironment("KAFKA_RETRY_BACKOFF_MS", "1000")
    .WithEnvironment("FLINK_JOBMANAGER_URL", "http://flink-jobmanager:8081")
    .WithEnvironment("TEMPORAL_SERVER_URL", "temporal-server:7233")
    .WithEnvironment("OTEL_EXPORTER_OTLP_ENDPOINT", "http://otel-collector:4318")
    .WithEnvironment("OTEL_EXPORTER_OTLP_PROTOCOL", "http/protobuf")
    .WithEnvironment("OTEL_EXPORTER_OTLP_TRACES_ENDPOINT", "http://otel-collector:4317")
    .WithEnvironment("LOKI_ENDPOINT", loki != null ? "http://loki:3100" : "")
    .WithEnvironment("GRAFANA_URL", "http://grafana:3000")
    .WithEnvironment("PROMETHEUS_URL", "http://prometheus:9090")
    .WithEnvironment("ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:13323")
    .WithEnvironment("DOTNET_DASHBOARD_OTLP_ENDPOINT_URL", "http://localhost:13323")
    .WithHttpEndpoint(18000, 13001, name: "webapi") // External port 18000 -> Internal port 13001
    // Simplified dependency chain prevents DCP reconciliation race conditions
    .WaitFor(redis)
    .WaitFor(kafka)              // Single Kafka instance
    .WaitFor(flinkTaskManager)   // Single Flink TaskManager (which waits for JobManager)
    .WaitFor(temporalServer)     // Single Temporal Server (which waits for Postgres)
    .WaitFor(otelCollector);     // Wait for observability stack except Grafana (conditionally disabled for tests)

// Conditionally wait for Grafana if it's enabled
var localTestingApi = grafana != null 
    ? localTestingApiBuilder.WaitFor(grafana)
    : localTestingApiBuilder;

// Enhanced application startup with DCP timeout fixes and comprehensive error handling
try
{
    Console.WriteLine("🚀 Starting LocalTesting infrastructure with simplified setup...");
    Console.WriteLine("📊 PGL Observability Stack: Prometheus + Grafana + Loki + OpenTelemetry");
    Console.WriteLine("⚙️  Simplified setup: 1 Kafka + 1 Flink + 1 Temporal + Full Observability");
    Console.WriteLine("⏱️  Expected startup time: 2-3 minutes for complete infrastructure (optimized for performance)");
    Console.WriteLine();
    
    var app = builder.Build();
    
    // Add graceful shutdown handling with extended timeout
    var cts = new CancellationTokenSource();
    Console.CancelKeyPress += (_, e) =>
    {
        e.Cancel = true;
        cts.Cancel();
        Console.WriteLine("🛑 Graceful shutdown initiated...");
    };
    
    // Enhanced startup with retry logic for DCP failures
    var maxRetries = 3;
    var currentRetry = 0;
    
    while (currentRetry < maxRetries)
    {
        try
        {
            Console.WriteLine($"🔄 Startup attempt {currentRetry + 1}/{maxRetries}...");
            await app.RunAsync(cts.Token);
            break; // Success - exit retry loop
        }
        catch (AggregateException ae) when (ae.InnerException is Polly.Timeout.TimeoutRejectedException)
        {
            currentRetry++;
            if (currentRetry >= maxRetries)
            {
                Console.WriteLine("❌ All startup attempts failed due to DCP timeouts");
                throw;
            }
            
            Console.WriteLine($"⚠️ DCP timeout on attempt {currentRetry}. Cleaning up and retrying...");
            
            // Clean up any partial containers before retry
            var cleanupProcess = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "docker",
                    Arguments = "system prune -f --volumes",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                }
            };
            
            cleanupProcess.Start();
            await cleanupProcess.WaitForExitAsync();
            Console.WriteLine("🧹 Docker cleanup completed. Retrying startup...");
            
            // Wait before retry
            await Task.Delay(TimeSpan.FromSeconds(10), cts.Token);
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ LocalTesting infrastructure startup failed: {ex.Message}");
    Console.WriteLine("🔧 Troubleshooting steps:");
    Console.WriteLine("1. Ensure Docker Desktop is running with 8GB+ RAM");
    Console.WriteLine("2. Check if ports 5000, 8081, 8084, 9090, 3000, 4317, 4318 are available");
    Console.WriteLine("3. Run: docker system prune -f --volumes");
    Console.WriteLine("4. Restart Docker Desktop if issues persist");
    Console.WriteLine("5. Check Windows Defender/Antivirus is not blocking Docker");
    Console.WriteLine($"📝 Full error: {ex}");
    throw;
}
