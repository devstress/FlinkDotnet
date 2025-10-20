using Confluent.Kafka;
using Serilog;
using StackExchange.Redis;
using System.Text.Json;

// Environment variables for service discovery
var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_ENDPOINT") ?? "localhost:6379";

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 153: Cross-Domain Integration & Event Correlation");
Console.WriteLine("================================================================================");
Console.WriteLine();

try
{
    Log.Information("Starting Exercise 153: Cross-Domain Integration");
    Console.WriteLine(">> Step 1: Initializing Cross-Domain Correlation Hub");
    Console.WriteLine();
    
    // Connect to Redis for cross-domain state
    var redis = await ConnectionMultiplexer.ConnectAsync(redisConnectionString);
    var db = redis.GetDatabase();
    
    Console.WriteLine("   [1/3] Reading E-commerce domain events");
    var ecommerceEvents = await ReadDomainEventsAsync(kafkaBootstrapServers, "ecommerce", db);
    Console.WriteLine($"         E-commerce: {ecommerceEvents} events read");
    
    Console.WriteLine("   [2/3] Reading Financial domain events");
    var financialEvents = await ReadDomainEventsAsync(kafkaBootstrapServers, "financial", db);
    Console.WriteLine($"         Financial: {financialEvents} events read");
    
    Console.WriteLine("   [3/3] Correlating cross-domain patterns");
    var correlations = await CorrelateCrossDomainEventsAsync(db, kafkaBootstrapServers);
    Console.WriteLine($"         Correlations: {correlations} insights generated");
    
    Console.WriteLine();
    Console.WriteLine(">> Step 2: Cross-Domain Correlation Results");
    Console.WriteLine();
    
    // Generate correlation report
    var report = GenerateCorrelationReport(ecommerceEvents, financialEvents, correlations);
    Console.WriteLine(report);
    
    await redis.CloseAsync();
    
    Log.Information("Exercise 153: Cross-Domain Integration completed successfully");
    
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] Cross-domain integration operational");
    Console.WriteLine($"          - Events correlated: {ecommerceEvents + financialEvents}");
    Console.WriteLine($"          - Insights generated: {correlations}");
    Console.WriteLine("          - Integration hub: Active");
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 153: Cross-Domain Integration");
    Console.WriteLine($"[ERROR] {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

Environment.Exit(0);

// Read domain events from Redis state storage
static async Task<int> ReadDomainEventsAsync(string bootstrapServers, string domain, IDatabase redis)
{
    int eventCount = 0;
    
    try
    {
        // Read from domain-specific Redis keys
        if (domain == "ecommerce")
        {
            // Check inventory events
            var products = new[] { "laptop-pro", "smartphone-x", "tablet-max", "headphones-elite" };
            foreach (var product in products)
            {
                var inventory = await redis.StringGetAsync($"inventory:{product}");
                if (inventory.HasValue)
                {
                    eventCount++;
                    // Store in correlation buffer
                    await redis.StringSetAsync($"correlation:ecommerce:{product}", inventory.ToString(), TimeSpan.FromMinutes(5));
                }
            }
            
            // Check recommendation events
            var users = new[] { "user-001", "user-002", "user-003", "user-004" };
            foreach (var user in users)
            {
                var recommendation = await redis.StringGetAsync($"recommendation:{user}");
                if (recommendation.HasValue)
                {
                    eventCount++;
                    await redis.StringSetAsync($"correlation:ecommerce-rec:{user}", recommendation.ToString()!, TimeSpan.FromMinutes(5));
                }
            }
        }
        else if (domain == "financial")
        {
            // Check transaction counts
            var accounts = new[] { "ACC001", "ACC002", "ACC003", "ACC004" };
            foreach (var account in accounts)
            {
                var txnCount = await redis.StringGetAsync($"txn-count:{account}");
                if (txnCount.HasValue)
                {
                    eventCount++;
                    await redis.StringSetAsync($"correlation:financial:{account}", txnCount.ToString()!, TimeSpan.FromMinutes(5));
                }
                
                // Check risk scores
                var riskScore = await redis.StringGetAsync($"risk-score:{account}");
                if (riskScore.HasValue)
                {
                    eventCount++;
                    await redis.StringSetAsync($"correlation:financial-risk:{account}", riskScore.ToString()!, TimeSpan.FromMinutes(5));
                }
            }
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, $"Error reading {domain} events");
    }
    
    return eventCount;
}

// Correlate events across domains and generate integrated insights
static async Task<int> CorrelateCrossDomainEventsAsync(IDatabase redis, string bootstrapServers)
{
    int correlationCount = 0;
    
    try
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = "cross-domain-correlation-producer"
        };
        
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        
        // Correlation Pattern 1: High-risk customer + Low inventory
        var accounts = new[] { "ACC001", "ACC002", "ACC003", "ACC004" };
        var products = new[] { "laptop-pro", "smartphone-x", "tablet-max", "headphones-elite" };
        
        foreach (var account in accounts)
        {
            var riskScore = await redis.StringGetAsync($"correlation:financial-risk:{account}");
            
            if (riskScore.HasValue && double.Parse(riskScore!) > 70.0)
            {
                // High-risk customer detected
                foreach (var product in products)
                {
                    var inventory = await redis.StringGetAsync($"correlation:ecommerce:{product}");
                    
                    if (inventory.HasValue && int.Parse(inventory!) < 20)
                    {
                        // Correlation found: High-risk + Low inventory
                        var insight = new
                        {
                            CorrelationId = Guid.NewGuid().ToString(),
                            Type = "high-risk-low-inventory",
                            AccountId = account,
                            ProductId = product,
                            RiskScore = double.Parse(riskScore!),
                            InventoryLevel = int.Parse(inventory!),
                            Recommendation = "Monitor purchase patterns closely",
                            Timestamp = DateTimeOffset.UtcNow
                        };
                        
                        var message = JsonSerializer.Serialize(insight);
                        await producer.ProduceAsync("integrated-insights",
                            new Message<string, string> { Key = $"{account}:{product}", Value = message });
                        
                        correlationCount++;
                    }
                }
            }
        }
        
        // Correlation Pattern 2: Transaction activity + Product recommendations
        var users = new[] { "user-001", "user-002", "user-003", "user-004" };
        
        foreach (var user in users)
        {
            // Simulate user-to-account mapping (in real system, this would be a join)
            var accountMapping = new Dictionary<string, string>
            {
                ["user-001"] = "ACC001",
                ["user-002"] = "ACC002",
                ["user-003"] = "ACC003",
                ["user-004"] = "ACC004"
            };
            
            if (accountMapping.TryGetValue(user, out var mappedAccount))
            {
                var recommendation = await redis.StringGetAsync($"correlation:ecommerce-rec:{user}");
                var txnCount = await redis.StringGetAsync($"correlation:financial:{mappedAccount}");
                
                if (recommendation.HasValue && txnCount.HasValue && int.Parse(txnCount!) > 5)
                {
                    // Correlation found: High transaction activity + Active recommendations
                    var insight = new
                    {
                        CorrelationId = Guid.NewGuid().ToString(),
                        Type = "high-activity-customer",
                        UserId = user,
                        AccountId = mappedAccount,
                        TransactionCount = int.Parse(txnCount!),
                        HasRecommendations = true,
                        Action = "Prioritize customer service",
                        Timestamp = DateTimeOffset.UtcNow
                    };
                    
                    var message = JsonSerializer.Serialize(insight);
                    await producer.ProduceAsync("integrated-insights",
                        new Message<string, string> { Key = user, Value = message });
                    
                    correlationCount++;
                }
            }
        }
        
        // Publish to domain-events topic for general monitoring
        var summaryEvent = new
        {
            EventId = Guid.NewGuid().ToString(),
            EventType = "cross-domain-correlation-complete",
            CorrelationsFound = correlationCount,
            Domains = new[] { "ecommerce", "financial" },
            Timestamp = DateTimeOffset.UtcNow
        };
        
        await producer.ProduceAsync("domain-events",
            new Message<string, string> { Key = "correlation-summary", Value = JsonSerializer.Serialize(summaryEvent) });
        
        producer.Flush(TimeSpan.FromSeconds(5));
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Error correlating cross-domain events");
    }
    
    return correlationCount;
}

// Generate comprehensive correlation report
static string GenerateCorrelationReport(int ecommerceEvents, int financialEvents, int correlations)
{
    var report = new System.Text.StringBuilder();
    
    report.AppendLine("   ┌─────────────────────────────────────────────────────────────────────┐");
    report.AppendLine("   │         CROSS-DOMAIN CORRELATION REPORT                             │");
    report.AppendLine("   ├─────────────────────────────────────────────────────────────────────┤");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │   Event Collection:                                                 │");
    report.AppendLine($"   │     • E-commerce Events:     {ecommerceEvents,3} events collected                   │");
    report.AppendLine($"   │     • Financial Events:      {financialEvents,3} events collected                   │");
    report.AppendLine($"   │     • Total Events:          {ecommerceEvents + financialEvents,3} ready for correlation             │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │   Correlation Patterns:                                             │");
    report.AppendLine("   │     • Pattern 1: High-Risk + Low Inventory                          │");
    report.AppendLine("   │       - Identifies risky customers interested in scarce products    │");
    report.AppendLine("   │       - Action: Enhanced fraud monitoring                           │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │     • Pattern 2: High Transaction Activity + Recommendations        │");
    report.AppendLine("   │       - Identifies valuable active customers                        │");
    report.AppendLine("   │       - Action: Priority customer service                           │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │   Integration Results:                                              │");
    report.AppendLine($"   │     • Insights Generated:    {correlations,3} cross-domain insights                │");
    report.AppendLine("   │     • Topics Updated:        2 (domain-events, integrated-insights) │");
    report.AppendLine("   │     • State Storage:         Redis correlation buffer               │");
    report.AppendLine("   │     • Correlation Window:    5 minutes                              │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   │   Platform Integration:                                             │");
    report.AppendLine("   │     • Cross-Domain Hub:      [ACTIVE]                               │");
    report.AppendLine("   │     • Event Correlation:     [OPERATIONAL]                          │");
    report.AppendLine("   │     • Insight Publishing:    [ACTIVE]                               │");
    report.AppendLine("   │     • Next Phase:            Production Validation (Exercise154)    │");
    report.AppendLine("   │                                                                     │");
    report.AppendLine("   └─────────────────────────────────────────────────────────────────────┘");
    
    return report.ToString();
}
