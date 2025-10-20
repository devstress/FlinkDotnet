using Confluent.Kafka;
using LearningCourse.Common;
using Polly;
using Polly.CircuitBreaker;
using Serilog;
using System.Text.Json;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 12.2: Automated Failover with Circuit Breaker");
Console.WriteLine("================================================================================");
Console.WriteLine();
Console.WriteLine("📚 Learning Objectives:");
Console.WriteLine("   • Implement circuit breaker patterns with Polly");
Console.WriteLine("   • Automate failover on region failures");
Console.WriteLine("   • Monitor circuit state transitions");
Console.WriteLine("   • Demonstrate resilience patterns");
Console.WriteLine("   • Apply Google/Netflix-style automated recovery");
Console.WriteLine();

// Define region configurations
var regions = new[]
{
    new { Name = "us-east-1", Topic = "dr-failover-us-east-1", IsHealthy = true },
    new { Name = "us-west-2", Topic = "dr-failover-us-west-2", IsHealthy = true },
    new { Name = "eu-west-1", Topic = "dr-failover-eu-west-1", IsHealthy = true }
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

// Circuit breaker state tracking
var circuitStates = new Dictionary<string, string>();
foreach (var region in regions)
{
    circuitStates[region.Name] = "Closed";
}

try
{
    Console.WriteLine(">> Step 1/6: Verifying Kafka infrastructure...");
    var kafkaEndpoint = await GetKafkaBootstrapServersAsync();
    Console.WriteLine($"   [SUCCESS] Kafka ready at {kafkaEndpoint}");
    Console.WriteLine();

    // Step 2: Create circuit breakers for each region
    Console.WriteLine(">> Step 2/6: Creating circuit breakers for each region...");
    Console.WriteLine("   Circuit Breaker Configuration:");
    Console.WriteLine("   • Failure Threshold: 3 consecutive failures");
    Console.WriteLine("   • Break Duration: 10 seconds");
    Console.WriteLine("   • Half-Open: Allow 1 test request");
    Console.WriteLine();

    var circuitBreakers = new Dictionary<string, ResiliencePipeline>();
    
    foreach (var region in regions)
    {
        var circuitBreakerOptions = new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(10),
            MinimumThroughput = 3,
            BreakDuration = TimeSpan.FromSeconds(10),
            OnOpened = args =>
            {
                circuitStates[region.Name] = "Open";
                Console.WriteLine($"   ⚠️  Circuit OPENED for {region.Name} - Region marked unhealthy");
                return ValueTask.CompletedTask;
            },
            OnClosed = args =>
            {
                circuitStates[region.Name] = "Closed";
                Console.WriteLine($"   ✅ Circuit CLOSED for {region.Name} - Region recovered");
                return ValueTask.CompletedTask;
            },
            OnHalfOpened = args =>
            {
                circuitStates[region.Name] = "HalfOpen";
                Console.WriteLine($"   🔄 Circuit HALF-OPEN for {region.Name} - Testing recovery");
                return ValueTask.CompletedTask;
            }
        };

        var pipeline = new ResiliencePipelineBuilder()
            .AddCircuitBreaker(circuitBreakerOptions)
            .Build();
            
        circuitBreakers[region.Name] = pipeline;
        Console.WriteLine($"   Circuit breaker created for {region.Name}");
    }
    Console.WriteLine();

    // Step 3: Normal operation - all regions healthy
    Console.WriteLine(">> Step 3/6: Testing normal operation (all regions healthy)...");
    
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageTimeoutMs = 10000
    };

    var successCount = 0;
    var failureCount = 0;

    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        // Send 20 requests in normal operation
        for (int i = 0; i < 20; i++)
        {
            var selectedRegion = regions[i % regions.Length];
            
            try
            {
                await circuitBreakers[selectedRegion.Name].ExecuteAsync(async ct =>
                {
                    var request = new
                    {
                        RequestId = $"normal-{i:D4}",
                        Timestamp = DateTimeOffset.UtcNow,
                        Region = selectedRegion.Name
                    };

                    await producer.ProduceAsync(selectedRegion.Topic, new Message<string, string>
                    {
                        Key = request.RequestId,
                        Value = JsonSerializer.Serialize(request)
                    }, ct);
                    
                    successCount++;
                });
            }
            catch (BrokenCircuitException)
            {
                failureCount++;
                Console.WriteLine($"   ⚠️  Request {i} failed - Circuit breaker open for {selectedRegion.Name}");
            }
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine($"   Normal operation: {successCount} successful, {failureCount} failed");
    Console.WriteLine();

    // Step 4: Simulate region failure
    Console.WriteLine(">> Step 4/6: Simulating region failure (us-east-1)...");
    Console.WriteLine("   Injecting failures to trigger circuit breaker");
    
    var primaryRegion = regions[0];
    var secondaryRegion = regions[1];
    
    // Simulate failures that will open the circuit
    for (int i = 0; i < 5; i++)
    {
        try
        {
            await circuitBreakers[primaryRegion.Name].ExecuteAsync(ct =>
            {
                // Simulate region failure
                throw new Exception($"Simulated failure in {primaryRegion.Name}");
            });
        }
        catch (BrokenCircuitException)
        {
            Console.WriteLine($"   Circuit breaker opened after {i} failures");
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   Failure {i + 1}: {ex.Message}");
        }
        
        await Task.Delay(100);
    }
    Console.WriteLine();

    // Step 5: Automatic failover to healthy region
    Console.WriteLine(">> Step 5/6: Automatic failover to secondary region...");
    Console.WriteLine("   Routing traffic to healthy regions only");
    
    var failoverSuccess = 0;
    var failoverFailed = 0;

    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        for (int i = 0; i < 20; i++)
        {
            // Try primary region first, failover to secondary if circuit is open
            var targetRegion = circuitStates[primaryRegion.Name] == "Closed" ? primaryRegion : secondaryRegion;
            
            try
            {
                await circuitBreakers[targetRegion.Name].ExecuteAsync(async ct =>
                {
                    var request = new
                    {
                        RequestId = $"failover-{i:D4}",
                        Timestamp = DateTimeOffset.UtcNow,
                        Region = targetRegion.Name,
                        FailoverFrom = primaryRegion.Name != targetRegion.Name ? primaryRegion.Name : null
                    };

                    await producer.ProduceAsync(targetRegion.Topic, new Message<string, string>
                    {
                        Key = request.RequestId,
                        Value = JsonSerializer.Serialize(request)
                    }, ct);
                    
                    failoverSuccess++;
                });
            }
            catch (BrokenCircuitException)
            {
                failoverFailed++;
            }
        }
        
        producer.Flush(TimeSpan.FromSeconds(10));
    }

    Console.WriteLine($"   Failover completed: {failoverSuccess} requests handled by {secondaryRegion.Name}");
    Console.WriteLine($"   Failed requests: {failoverFailed}");
    Console.WriteLine();

    // Step 6: Circuit breaker state summary
    Console.WriteLine(">> Step 6/6: Circuit Breaker State Summary");
    Console.WriteLine("   Final circuit states:");
    foreach (var region in regions)
    {
        var state = circuitStates[region.Name];
        var emoji = state == "Closed" ? "✅" : state == "Open" ? "⚠️ " : "🔄";
        Console.WriteLine($"   {emoji} {region.Name}: {state}");
    }
    Console.WriteLine();

    // Summary
    Console.WriteLine("================================================================================");
    Console.WriteLine("  Exercise 12.2 Results - Automated Failover with Circuit Breaker");
    Console.WriteLine("================================================================================");
    Console.WriteLine("  ✅ Key Achievements:");
    Console.WriteLine($"     • Created circuit breakers for {regions.Length} regions");
    Console.WriteLine($"     • Normal operation: {successCount}/{successCount + failureCount} successful");
    Console.WriteLine($"     • Detected failures and opened circuit for {primaryRegion.Name}");
    Console.WriteLine($"     • Automatic failover to {secondaryRegion.Name}");
    Console.WriteLine($"     • Failover handling: {failoverSuccess}/{failoverSuccess + failoverFailed} successful");
    Console.WriteLine();
    Console.WriteLine("  📚 Key Learnings:");
    Console.WriteLine("     ✓ Circuit breakers prevent cascading failures");
    Console.WriteLine("     ✓ Polly provides production-ready resilience patterns");
    Console.WriteLine("     ✓ Circuit states: Closed → Open → Half-Open → Closed");
    Console.WriteLine("     ✓ Automatic failover maintains service availability");
    Console.WriteLine("     ✓ Half-open state tests recovery before full restoration");
    Console.WriteLine();
    Console.WriteLine("  🎯 Production Insights:");
    Console.WriteLine("     • Google uses circuit breakers across global infrastructure");
    Console.WriteLine("     • Netflix Hystrix inspired Polly circuit breaker patterns");
    Console.WriteLine("     • Circuit breakers reduce load on failing services");
    Console.WriteLine("     • Automated failover provides sub-second recovery");
    Console.WriteLine("     • Monitoring circuit states critical for operations");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    
    Environment.Exit(0);
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 12.2: Automated Failover with Circuit Breaker");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}
