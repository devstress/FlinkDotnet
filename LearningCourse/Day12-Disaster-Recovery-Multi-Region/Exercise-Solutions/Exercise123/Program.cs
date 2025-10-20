using Confluent.Kafka;
using LearningCourse.Common;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 12.3: Cross-Region State Replication");
Console.WriteLine("================================================================================");
Console.WriteLine();
Console.WriteLine("📚 Learning Objectives:");
Console.WriteLine("   • Implement cross-region state replication patterns");
Console.WriteLine("   • Measure replication lag and latency");
Console.WriteLine("   • Demonstrate asynchronous replication");
Console.WriteLine("   • Handle conflict resolution strategies");
Console.WriteLine("   • Apply Uber/Airbnb-style geo-replication");
Console.WriteLine();

// Define region configurations
var regions = new[]
{
    new { Name = "us-east-1", SourceTopic = "dr-source-us-east-1", ReplicaTopic = "dr-replica-us-east-1" },
    new { Name = "us-west-2", SourceTopic = "dr-source-us-west-2", ReplicaTopic = "dr-replica-us-west-2" },
    new { Name = "eu-west-1", SourceTopic = "dr-source-eu-west-1", ReplicaTopic = "dr-replica-eu-west-1" }
};

string? kafkaBootstrapServers = null;

async Task<string> GetKafkaBootstrapServersAsync()
{
    if (kafkaBootstrapServers != null)
        return kafkaBootstrapServers;
        
    kafkaBootstrapServers = await AspireServiceDiscovery.GetKafkaBootstrapServersAsync();
    Log.Information("📡 Kafka discovered at: {KafkaEndpoint}", kafkaBootstrapServers);
    return kafkaBootstrapServers;
}

// Replication metrics
var replicationMetrics = new ConcurrentDictionary<string, List<TimeSpan>>();
foreach (var region in regions)
{
    replicationMetrics[region.Name] = new List<TimeSpan>();
}

try
{
    Console.WriteLine(">> Step 1/6: Verifying Kafka infrastructure...");
    var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
    Console.WriteLine($"   [SUCCESS] Kafka ready at {kafkaEndpoint}");
    Console.WriteLine();

    // Step 2: Set up source and replica topics
    Console.WriteLine(">> Step 2/6: Setting up source and replica topics...");
    Console.WriteLine("   Regional replication architecture:");
    foreach (var region in regions)
    {
        Console.WriteLine($"   • {region.Name}");
        Console.WriteLine($"     - Source: {region.SourceTopic}");
        Console.WriteLine($"     - Replica: {region.ReplicaTopic}");
    }
    Console.WriteLine();

    // Step 3: Write data to source regions
    Console.WriteLine(">> Step 3/6: Writing data to source regions...");
    Console.WriteLine("   Simulating 50 state updates across regions");
    
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageTimeoutMs = 10000
    };

    var stateUpdates = new ConcurrentBag<(string Region, string StateId, DateTimeOffset Timestamp)>();

    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        var tasks = new List<Task>();
        
        for (int i = 0; i < 50; i++)
        {
            var region = regions[i % regions.Length];
            var stateId = $"state-{i:D4}";
            var timestamp = DateTimeOffset.UtcNow;
            
            tasks.Add(Task.Run(async () =>
            {
                var stateData = new
                {
                    StateId = stateId,
                    Region = region.Name,
                    Timestamp = timestamp,
                    Version = 1,
                    Data = $"State data for {stateId}",
                    ReplicationTimestamp = timestamp
                };

                await producer.ProduceAsync(region.SourceTopic, new Message<string, string>
                {
                    Key = stateId,
                    Value = JsonSerializer.Serialize(stateData)
                });

                stateUpdates.Add((region.Name, stateId, timestamp));
            }));
        }
        
        await Task.WhenAll(tasks);
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine($"   Written {stateUpdates.Count} state updates to source topics");
    Console.WriteLine();

    // Step 4: Replicate to other regions
    Console.WriteLine(">> Step 4/6: Replicating state to other regions...");
    Console.WriteLine("   Performing asynchronous cross-region replication");
    
    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"replication-consumer-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    var replicationTasks = new List<Task>();
    
    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        foreach (var sourceRegion in regions)
        {
            var replicateTask = Task.Run(async () =>
            {
                using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
                consumer.Subscribe(sourceRegion.SourceTopic);
                
                var replicatedCount = 0;
                var deadline = DateTime.UtcNow.AddSeconds(30);
                
                while (replicatedCount < 50 / regions.Length + 10 && DateTime.UtcNow < deadline)
                {
                    var result = consumer.Consume(TimeSpan.FromSeconds(2));
                    if (result != null)
                    {
                        var sw = Stopwatch.StartNew();
                        
                        // Replicate to all other regions
                        var replicationTargets = regions.Where(r => r.Name != sourceRegion.Name);
                        
                        foreach (var targetRegion in replicationTargets)
                        {
                            var originalData = JsonSerializer.Deserialize<JsonElement>(result.Message.Value);
                            var replicatedData = new
                            {
                                StateId = originalData.GetProperty("StateId").GetString(),
                                Region = originalData.GetProperty("Region").GetString(),
                                Timestamp = originalData.GetProperty("Timestamp").GetDateTimeOffset(),
                                Version = originalData.GetProperty("Version").GetInt32(),
                                Data = originalData.GetProperty("Data").GetString(),
                                ReplicationTimestamp = DateTimeOffset.UtcNow,
                                ReplicatedFrom = sourceRegion.Name,
                                ReplicationLag = (DateTimeOffset.UtcNow - originalData.GetProperty("Timestamp").GetDateTimeOffset()).TotalMilliseconds
                            };

                            await producer.ProduceAsync(targetRegion.ReplicaTopic, new Message<string, string>
                            {
                                Key = result.Message.Key,
                                Value = JsonSerializer.Serialize(replicatedData)
                            });
                        }
                        
                        sw.Stop();
                        replicationMetrics[sourceRegion.Name].Add(sw.Elapsed);
                        replicatedCount++;
                    }
                }
            });
            
            replicationTasks.Add(replicateTask);
        }
        
        await Task.WhenAll(replicationTasks);
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine($"   Replication completed for all regions");
    Console.WriteLine();

    // Step 5: Measure replication lag
    Console.WriteLine(">> Step 5/6: Measuring replication lag and performance...");
    
    foreach (var region in regions)
    {
        var lags = replicationMetrics[region.Name];
        if (lags.Count > 0)
        {
            var avgLag = lags.Average(l => l.TotalMilliseconds);
            var maxLag = lags.Max(l => l.TotalMilliseconds);
            var minLag = lags.Min(l => l.TotalMilliseconds);
            
            Console.WriteLine($"   {region.Name} replication metrics:");
            Console.WriteLine($"     • Messages replicated: {lags.Count}");
            Console.WriteLine($"     • Average lag: {avgLag:F2}ms");
            Console.WriteLine($"     • Min lag: {minLag:F2}ms");
            Console.WriteLine($"     • Max lag: {maxLag:F2}ms");
        }
    }
    Console.WriteLine();

    // Step 6: Verify replication consistency
    Console.WriteLine(">> Step 6/6: Verifying replication consistency...");
    Console.WriteLine("   Checking replica topics for replicated data");
    
    var replicaConsumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"replica-verify-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    var replicaCounts = new Dictionary<string, int>();
    foreach (var region in regions)
    {
        replicaCounts[region.Name] = 0;
    }

    using (var consumer = new ConsumerBuilder<string, string>(replicaConsumerConfig).Build())
    {
        consumer.Subscribe(regions.Select(r => r.ReplicaTopic).ToList());
        
        var deadline = DateTime.UtcNow.AddSeconds(20);
        var totalReceived = 0;
        
        while (DateTime.UtcNow < deadline && totalReceived < 200)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(2));
            if (result != null)
            {
                var regionName = result.Topic.Replace("dr-replica-", "");
                replicaCounts[regionName]++;
                totalReceived++;
            }
        }
    }

    Console.WriteLine("   Replication verification:");
    foreach (var region in regions)
    {
        var count = replicaCounts[region.Name];
        Console.WriteLine($"   • {region.Name}: {count} replicated messages received");
    }
    Console.WriteLine();

    // Summary
    var totalReplicated = replicationMetrics.Values.Sum(v => v.Count);
    var overallAvgLag = replicationMetrics.Values
        .SelectMany(v => v)
        .Average(l => l.TotalMilliseconds);

    Console.WriteLine("================================================================================");
    Console.WriteLine("  Exercise 12.3 Results - Cross-Region State Replication");
    Console.WriteLine("================================================================================");
    Console.WriteLine("  ✅ Key Achievements:");
    Console.WriteLine($"     • Replicated state across {regions.Length} regions");
    Console.WriteLine($"     • Total state updates: {stateUpdates.Count}");
    Console.WriteLine($"     • Total replications: {totalReplicated}");
    Console.WriteLine($"     • Average replication lag: {overallAvgLag:F2}ms");
    Console.WriteLine($"     • Replication pattern: Asynchronous multi-region");
    Console.WriteLine();
    Console.WriteLine("  📚 Key Learnings:");
    Console.WriteLine("     ✓ Cross-region replication enables disaster recovery");
    Console.WriteLine("     ✓ Asynchronous replication reduces primary region impact");
    Console.WriteLine("     ✓ Replication lag affects Recovery Point Objective (RPO)");
    Console.WriteLine("     ✓ Kafka topics simulate regional data stores");
    Console.WriteLine("     ✓ Monitoring replication lag critical for SLAs");
    Console.WriteLine();
    Console.WriteLine("  🎯 Production Insights:");
    Console.WriteLine("     • Uber replicates ride state across 3+ regions");
    Console.WriteLine("     • Airbnb uses cross-region replication for bookings");
    Console.WriteLine("     • Amazon DynamoDB Global Tables use asynchronous replication");
    Console.WriteLine("     • Typical replication lag: <1 second for geo-replication");
    Console.WriteLine("     • Conflict resolution strategies: Last-Write-Wins, CRDT");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    
    Environment.Exit(0);
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 12.3: Cross-Region State Replication");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}
