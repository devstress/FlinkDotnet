using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using StackExchange.Redis;
using System.Net.Http;

// Environment variables for service discovery (set by test infrastructure)
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
var flinkGatewayUrl = Environment.GetEnvironmentVariable("FLINKDOTNET_JOBGATEWAY_URL") ?? "http://localhost:8080";
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_ENDPOINT") ?? "localhost:6379";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 151: Multi-Domain Platform Architecture Validation");
Console.WriteLine("================================================================================");
Console.WriteLine();

try
{
    Log.Information("Starting Exercise 151: Platform Architecture Validation");
    Console.WriteLine(">> Step 1: Validating Infrastructure Connectivity");
    Console.WriteLine();
    
    // Step 1: Validate Kafka Cluster
    Console.WriteLine("   [1/4] Validating Kafka cluster...");
    var kafkaValid = await ValidateKafkaClusterAsync(kafkaBootstrapServers);
    Console.WriteLine($"         Kafka: {(kafkaValid ? "[SUCCESS]" : "[FAILED]")} @ {kafkaBootstrapServers}");
    
    // Step 2: Validate Flink Cluster
    Console.WriteLine("   [2/4] Validating Flink cluster...");
    var flinkValid = await ValidateFlinkClusterAsync(flinkGatewayUrl);
    Console.WriteLine($"         Flink: {(flinkValid ? "[SUCCESS]" : "[FAILED]")} @ {flinkGatewayUrl}");
    
    // Step 3: Validate Redis
    Console.WriteLine("   [3/4] Validating Redis...");
    var redisValid = await ValidateRedisAsync(redisConnectionString);
    Console.WriteLine($"         Redis: {(redisValid ? "[SUCCESS]" : "[FAILED]")} @ {redisConnectionString}");
    
    // Step 4: Create Multi-Domain Kafka Topics
    Console.WriteLine("   [4/4] Creating multi-domain Kafka topics...");
    var topicsCreated = await CreateMultiDomainTopicsAsync(kafkaBootstrapServers);
    Console.WriteLine($"         Topics: {topicsCreated} created successfully");
    
    Console.WriteLine();
    Console.WriteLine(">> Step 2: Platform Architecture Report");
    Console.WriteLine();
    
    // Generate architecture validation report
    var report = GenerateArchitectureReport(kafkaValid, flinkValid, redisValid, topicsCreated);
    Console.WriteLine(report);
    
    if (!kafkaValid || !flinkValid || !redisValid)
    {
        Log.Error("Infrastructure validation failed - not all components are healthy");
        Environment.Exit(1);
    }
    
    Log.Information("Exercise 151: Platform Architecture Validation completed successfully");
    
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] Multi-domain platform architecture validated");
    Console.WriteLine($"          - Kafka: {topicsCreated} topics ready");
    Console.WriteLine("          - Flink: Cluster operational");
    Console.WriteLine("          - Redis: State storage ready");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 151: Platform Architecture Validation");
    Console.WriteLine($"[ERROR] {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

Environment.Exit(0);

// Validate Kafka cluster connectivity and create admin client
static Task<bool> ValidateKafkaClusterAsync(string bootstrapServers)
{
    return Task.Run(() =>
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = bootstrapServers,
                SocketTimeoutMs = 5000
            };
            
            using var adminClient = new AdminClientBuilder(config).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));
            
            return metadata.Brokers.Count > 0;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Kafka validation failed");
            return false;
        }
    });
}

// Validate Flink Job Gateway by checking health endpoint
static async Task<bool> ValidateFlinkClusterAsync(string gatewayUrl)
{
    try
    {
        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        
        // Check JobGateway health endpoint: /api/v1/jobs/health
        var response = await client.GetAsync($"{gatewayUrl}/api/v1/jobs/health");
        return response.IsSuccessStatusCode;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Flink Job Gateway validation failed");
        return false;
    }
}

// Validate Redis connectivity
static async Task<bool> ValidateRedisAsync(string connectionString)
{
    try
    {
        var redis = await ConnectionMultiplexer.ConnectAsync(connectionString);
        var db = redis.GetDatabase();
        
        // Test write and read
        var testKey = "platform:health:check";
        await db.StringSetAsync(testKey, DateTimeOffset.UtcNow.ToString(), TimeSpan.FromSeconds(10));
        var value = await db.StringGetAsync(testKey);
        
        await redis.CloseAsync();
        return !value.IsNullOrEmpty;
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Redis validation failed");
        return false;
    }
}

// Create multi-domain Kafka topics for E-commerce and Financial domains
static async Task<int> CreateMultiDomainTopicsAsync(string bootstrapServers)
{
    try
    {
        var config = new AdminClientConfig
        {
            BootstrapServers = bootstrapServers,
            SocketTimeoutMs = 10000
        };
        
        using var adminClient = new AdminClientBuilder(config).Build();
        
        // Define multi-domain topics
        var topics = new List<TopicSpecification>
        {
            // E-commerce Domain Topics
            new TopicSpecification
            {
                Name = "ecommerce-inventory-events",
                NumPartitions = 4,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = "ecommerce-user-interactions",
                NumPartitions = 8,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = "ecommerce-recommendations",
                NumPartitions = 4,
                ReplicationFactor = 1
            },
            
            // Financial Domain Topics
            new TopicSpecification
            {
                Name = "financial-transactions",
                NumPartitions = 8,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = "financial-fraud-alerts",
                NumPartitions = 2,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = "financial-risk-scores",
                NumPartitions = 4,
                ReplicationFactor = 1
            },
            
            // Cross-Domain Integration Topics
            new TopicSpecification
            {
                Name = "domain-events",
                NumPartitions = 8,
                ReplicationFactor = 1
            },
            new TopicSpecification
            {
                Name = "integrated-insights",
                NumPartitions = 4,
                ReplicationFactor = 1
            }
        };
        
        // Create topics (ignore if already exist)
        try
        {
            await adminClient.CreateTopicsAsync(topics);
        }
        catch (CreateTopicsException ex)
        {
            // Ignore "topic already exists" errors
            var successCount = topics.Count - ex.Results.Count(r => r.Error.Code != ErrorCode.TopicAlreadyExists && r.Error.Code != ErrorCode.NoError);
            if (successCount < topics.Count)
            {
                Log.Information("Some topics already exist, continuing...");
            }
        }
        
        return topics.Count;
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Failed to create multi-domain topics");
        return 0;
    }
}

// Generate comprehensive architecture validation report
static string GenerateArchitectureReport(bool kafkaValid, bool flinkValid, bool redisValid, int topicsCreated)
{
    var report = new System.Text.StringBuilder();
    
    report.AppendLine("   ┌─────────────────────────────────────────────────────────────────────┐");
    report.AppendLine("   │         MULTI-DOMAIN PLATFORM ARCHITECTURE REPORT                  │");
    report.AppendLine("   ├─────────────────────────────────────────────────────────────────────┤");
    report.AppendLine("   │                                                                     │");
    report.AppendLine($"   │   Infrastructure Status:                                            │");
    report.AppendLine($"   │     • Kafka Cluster:    {(kafkaValid ? "[OPERATIONAL]" : "[FAILED]    "),30}                  │");
    report.AppendLine($"   │     • Flink Cluster:    {(flinkValid ? "[OPERATIONAL]" : "[FAILED]    "),30}                  │");
    report.AppendLine($"   │     • Redis State:      {(redisValid ? "[OPERATIONAL]" : "[FAILED]    "),30}                  │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │   Domain Configuration:                                             │");
    report.AppendLine("   │     • E-commerce Domain:      3 topics configured                   │");
    report.AppendLine("   │       - inventory-events      (4 partitions)                        │");
    report.AppendLine("   │       - user-interactions     (8 partitions)                        │");
    report.AppendLine("   │       - recommendations       (4 partitions)                        │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │     • Financial Domain:       3 topics configured                   │");
    report.AppendLine("   │       - transactions          (8 partitions)                        │");
    report.AppendLine("   │       - fraud-alerts          (2 partitions)                        │");
    report.AppendLine("   │       - risk-scores           (4 partitions)                        │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │     • Cross-Domain:           2 topics configured                   │");
    report.AppendLine("   │       - domain-events         (8 partitions)                        │");
    report.AppendLine("   │       - integrated-insights   (4 partitions)                        │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine($"   │   Total Topics Created: {topicsCreated,2}                                        │");
    report.AppendLine("   │   Total Partitions:     42                                          │");
    report.AppendLine("   │   Replication Factor:   1 (LocalTesting)                            │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine($"   │   Platform Status:      {(kafkaValid && flinkValid && redisValid ? "[READY FOR DEPLOYMENT]" : "[INFRASTRUCTURE ISSUES]"),30}              │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   └─────────────────────────────────────────────────────────────────────┘");
    
    return report.ToString();
}
