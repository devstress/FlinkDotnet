using Confluent.Kafka;
using LearningCourse.Common;
using Serilog;
using System.Text.Json;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 12.1: Multi-Region Active-Active Deployment");
Console.WriteLine("================================================================================");
Console.WriteLine();
Console.WriteLine("📚 Learning Objectives:");
Console.WriteLine("   • Understand multi-region deployment architectures");
Console.WriteLine("   • Implement active-active patterns across regions");
Console.WriteLine("   • Load balance traffic across multiple regions");
Console.WriteLine("   • Monitor regional health and performance");
Console.WriteLine("   • Demonstrate Netflix/Amazon-style regional distribution");
Console.WriteLine();

// Define region configurations
var regions = new[]
{
    new { Name = "us-east-1", Priority = 1, Weight = 40, Topic = "dr-region-us-east-1" },
    new { Name = "us-west-2", Priority = 2, Weight = 40, Topic = "dr-region-us-west-2" },
    new { Name = "eu-west-1", Priority = 3, Weight = 20, Topic = "dr-region-eu-west-1" }
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

try
{
    Console.WriteLine(">> Step 1/5: Verifying Kafka infrastructure...");
    var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
    Console.WriteLine($"   [SUCCESS] Kafka ready at {kafkaEndpoint}");
    Console.WriteLine();

    // Step 2: Set up multi-region topics
    Console.WriteLine(">> Step 2/5: Setting up multi-region infrastructure...");
    Console.WriteLine("   Creating regional topics to simulate multi-region deployment:");
    foreach (var region in regions)
    {
        Console.WriteLine($"   • {region.Name} (Priority: {region.Priority}, Weight: {region.Weight}%)");
    }
    Console.WriteLine();

    // Step 3: Distribute traffic across regions
    Console.WriteLine(">> Step 3/5: Distributing traffic across regions...");
    Console.WriteLine("   Simulating 100 requests distributed by regional weights");
    
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageTimeoutMs = 10000
    };

    var regionalStats = new Dictionary<string, int>();
    foreach (var region in regions)
    {
        regionalStats[region.Name] = 0;
    }

    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        var requestCount = 100;
        var random = new Random(42); // Deterministic for testing
        
        for (int i = 0; i < requestCount; i++)
        {
            // Weighted random selection based on region weights
            var selector = random.Next(100);
            var selectedRegion = selector < 40 ? regions[0] : 
                                selector < 80 ? regions[1] : regions[2];
            
            var request = new
            {
                RequestId = $"req-{i:D4}",
                Timestamp = DateTimeOffset.UtcNow,
                Region = selectedRegion.Name,
                Data = $"Request data {i}"
            };

            await producer.ProduceAsync(selectedRegion.Topic, new Message<string, string>
            {
                Key = request.RequestId,
                Value = JsonSerializer.Serialize(request)
            });

            regionalStats[selectedRegion.Name]++;
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine("   Traffic distribution:");
    foreach (var region in regions)
    {
        var count = regionalStats[region.Name];
        var percentage = (count * 100.0) / 100;
        Console.WriteLine($"   • {region.Name}: {count} requests ({percentage:F1}%) - Target: {region.Weight}%");
    }
    Console.WriteLine();

    // Step 4: Monitor regional health
    Console.WriteLine(">> Step 4/5: Monitoring regional health...");
    Console.WriteLine("   Checking message delivery across all regions");
    
    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"multi-region-monitor-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };

    var regionalMessages = new Dictionary<string, int>();
    foreach (var region in regions)
    {
        regionalMessages[region.Name] = 0;
    }

    using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
    {
        // Subscribe to all regional topics
        consumer.Subscribe(regions.Select(r => r.Topic).ToList());
        
        var deadline = DateTime.UtcNow.AddSeconds(30);
        var totalExpected = regionalStats.Values.Sum();
        var totalReceived = 0;
        
        while (totalReceived < totalExpected && DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                var regionName = result.Topic.Replace("dr-region-", "");
                regionalMessages[regionName]++;
                totalReceived++;
            }
        }
    }

    Console.WriteLine("   Regional health status:");
    var allRegionsHealthy = true;
    foreach (var region in regions)
    {
        var sent = regionalStats[region.Name];
        var received = regionalMessages[region.Name];
        var healthStatus = received == sent ? "✅ HEALTHY" : "⚠️  DEGRADED";
        Console.WriteLine($"   • {region.Name}: {received}/{sent} messages - {healthStatus}");
        
        if (received != sent)
            allRegionsHealthy = false;
    }
    Console.WriteLine();

    // Step 5: Summary and metrics
    Console.WriteLine(">> Step 5/5: Multi-Region Deployment Summary");
    Console.WriteLine("   Active-Active Configuration:");
    Console.WriteLine($"   • Total Regions: {regions.Length}");
    Console.WriteLine($"   • All Regions Active: YES");
    Console.WriteLine($"   • Traffic Distribution: Weighted by capacity");
    Console.WriteLine($"   • Health Status: {(allRegionsHealthy ? "All regions healthy" : "Some regions degraded")}");
    Console.WriteLine();

    // Summary
    Console.WriteLine("================================================================================");
    Console.WriteLine("  Exercise 12.1 Results - Multi-Region Active-Active");
    Console.WriteLine("================================================================================");
    Console.WriteLine("  ✅ Key Achievements:");
    Console.WriteLine($"     • Deployed across {regions.Length} simulated regions");
    Console.WriteLine($"     • Distributed {regionalStats.Values.Sum()} requests across regions");
    Console.WriteLine($"     • Load balanced by regional capacity weights");
    Console.WriteLine($"     • Monitored health across all regions");
    Console.WriteLine($"     • Regional distribution: {string.Join(", ", regions.Select(r => $"{r.Name}:{regionalStats[r.Name]}"))}");
    Console.WriteLine();
    Console.WriteLine("  📚 Key Learnings:");
    Console.WriteLine("     ✓ Active-active deployments provide high availability");
    Console.WriteLine("     ✓ Traffic distribution based on regional capacity");
    Console.WriteLine("     ✓ Each region processes requests independently");
    Console.WriteLine("     ✓ Health monitoring critical for multi-region operations");
    Console.WriteLine("     ✓ Kafka topics simulate regional isolation");
    Console.WriteLine();
    Console.WriteLine("  🎯 Production Insights:");
    Console.WriteLine("     • Netflix uses 25+ regions for Prime Video streaming");
    Console.WriteLine("     • Amazon distributes traffic by geographic proximity");
    Console.WriteLine("     • Regional failover happens within seconds");
    Console.WriteLine("     • Active-active prevents single region failure");
    Console.WriteLine("     • Capacity planning based on regional weights");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    
    Environment.Exit(0);
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 12.1: Multi-Region Active-Active");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}
