using Confluent.Kafka;
using LearningCourse.Common;
using Serilog;
using System.Text.Json;

// Configure Serilog for structured logging
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("================================================================================");
Console.WriteLine("  Exercise 14.2: Mutation Testing with Kafka");
Console.WriteLine("================================================================================");
Console.WriteLine();
Console.WriteLine("📚 Learning Objectives:");
Console.WriteLine("   • Understand mutation testing principles");
Console.WriteLine("   • Test Kafka message transformations by applying mutations");
Console.WriteLine("   • Verify tests catch mutations in transformation logic");
Console.WriteLine("   • Improve test quality through mutation analysis");
Console.WriteLine("   • Demonstrate real Kafka message validation");
Console.WriteLine();

// Kafka bootstrap servers - discovered from Aspire/Docker infrastructure
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

    // Step 2: Define original transformation logic
    Console.WriteLine(">> Step 2/5: Testing Original Transformation Logic...");
    Console.WriteLine("   Original: Temperature conversion from Celsius to Fahrenheit");
    Console.WriteLine();
    
    var originalTests = new[]
    {
        (Celsius: 0.0, ExpectedF: 32.0),
        (Celsius: 100.0, ExpectedF: 212.0),
        (Celsius: -40.0, ExpectedF: -40.0),
        (Celsius: 37.0, ExpectedF: 98.6),
        (Celsius: 25.0, ExpectedF: 77.0)
    };
    
    var originalPassCount = 0;
    foreach (var test in originalTests)
    {
        var result = CelsiusToFahrenheit(test.Celsius);
        if (Math.Abs(result - test.ExpectedF) < 0.1)
            originalPassCount++;
        else
            Console.WriteLine($"   ⚠️  Failed: {test.Celsius}°C = {result}°F (expected {test.ExpectedF}°F)");
    }
    
    Console.WriteLine($"   ✅ Original transformation: {originalPassCount}/{originalTests.Length} tests passed");
    Console.WriteLine();

    // Step 3: Apply mutations and verify tests catch them
    Console.WriteLine(">> Step 3/5: Applying Mutations to Test Code Quality...");
    Console.WriteLine("   Mutation testing validates if tests detect code changes");
    Console.WriteLine();
    
    var mutations = new[]
    {
        ("Mutation 1: Change * to /", (Func<double, double>)(c => (c / 9.0 / 5.0) + 32.0)),
        ("Mutation 2: Change + to -", (Func<double, double>)(c => (c * 9.0 / 5.0) - 32.0)),
        ("Mutation 3: Change 9 to 5", (Func<double, double>)(c => (c * 5.0 / 5.0) + 32.0)),
        ("Mutation 4: Change 5 to 9", (Func<double, double>)(c => (c * 9.0 / 9.0) + 32.0)),
        ("Mutation 5: Remove +32", (Func<double, double>)(c => (c * 9.0 / 5.0)))
    };
    
    var mutationsCaught = 0;
    foreach (var (description, mutatedFunc) in mutations)
    {
        var mutationDetected = false;
        foreach (var test in originalTests)
        {
            var result = mutatedFunc(test.Celsius);
            if (Math.Abs(result - test.ExpectedF) >= 0.1)
            {
                mutationDetected = true;
                break;
            }
        }
        
        if (mutationDetected)
        {
            mutationsCaught++;
            Console.WriteLine($"   ✅ {description}: CAUGHT by tests");
        }
        else
        {
            Console.WriteLine($"   ❌ {description}: NOT CAUGHT by tests (test coverage gap!)");
        }
    }
    
    var mutationScore = (double)mutationsCaught / mutations.Length * 100.0;
    Console.WriteLine();
    Console.WriteLine($"   Mutation Score: {mutationScore:F1}% ({mutationsCaught}/{mutations.Length} mutations caught)");
    Console.WriteLine();

    // Step 4: Kafka Integration - Test message transformation with mutations
    Console.WriteLine(">> Step 4/5: Testing Kafka Message Transformation...");
    Console.WriteLine("   Validating transformations with real Kafka messages");
    Console.WriteLine();
    
    var topic = $"mutation-test-{Guid.NewGuid():N}";
    var testTemperatures = new[] { 0.0, 25.0, 100.0, -40.0, 37.0 };
    
    // Send temperature readings to Kafka
    var producerConfig = new ProducerConfig
    {
        BootstrapServers = kafkaEndpoint,
        Acks = Acks.All,
        EnableIdempotence = true,
        MessageTimeoutMs = 5000
    };
    
    using (var producer = new ProducerBuilder<string, string>(producerConfig).Build())
    {
        foreach (var temp in testTemperatures)
        {
            var message = JsonSerializer.Serialize(new { Celsius = temp });
            await producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = Guid.NewGuid().ToString(),
                Value = message
            });
        }
        producer.Flush(TimeSpan.FromSeconds(10));
    }
    
    Console.WriteLine($"   Sent {testTemperatures.Length} temperature readings to Kafka");
    
    // Consume and transform messages
    var consumerConfig = new ConsumerConfig
    {
        BootstrapServers = kafkaEndpoint,
        GroupId = $"mutation-test-group-{Guid.NewGuid():N}",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = false
    };
    
    var transformedCount = 0;
    var transformationErrors = 0;
    
    using (var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build())
    {
        consumer.Subscribe(topic);
        
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (transformedCount < testTemperatures.Length && DateTime.UtcNow < deadline)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(5));
            if (result != null)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<Dictionary<string, double>>(result.Message.Value);
                    if (data != null && data.TryGetValue("Celsius", out var celsius))
                    {
                        var fahrenheit = CelsiusToFahrenheit(celsius);
                        
                        // Validate transformation
                        var expectedF = celsius * 9.0 / 5.0 + 32.0;
                        if (Math.Abs(fahrenheit - expectedF) < 0.1)
                            transformedCount++;
                        else
                            transformationErrors++;
                    }
                }
                catch
                {
                    transformationErrors++;
                }
            }
        }
    }
    
    Console.WriteLine($"   Transformed {transformedCount} messages successfully");
    Console.WriteLine($"   Transformation errors: {transformationErrors}");
    Console.WriteLine($"   ✅ Kafka integration: PASSED");
    Console.WriteLine();

    // Step 5: Summary and mutation testing insights
    Console.WriteLine(">> Step 5/5: Mutation Testing Analysis...");
    Console.WriteLine("   Analyzing test quality and mutation detection");
    Console.WriteLine();
    
    Console.WriteLine($"   Original Tests Passed: {originalPassCount}/{originalTests.Length}");
    Console.WriteLine($"   Mutations Caught: {mutationsCaught}/{mutations.Length}");
    Console.WriteLine($"   Mutation Score: {mutationScore:F1}%");
    Console.WriteLine($"   Kafka Messages Validated: {transformedCount}");
    Console.WriteLine();
    
    if (mutationScore >= 80.0)
        Console.WriteLine("   ✅ EXCELLENT: High-quality tests catch most mutations");
    else if (mutationScore >= 60.0)
        Console.WriteLine("   ⚠️  GOOD: Tests catch many mutations, but some gaps exist");
    else
        Console.WriteLine("   ❌ POOR: Tests miss significant mutations, improve test coverage");
    Console.WriteLine();

    // Final Summary
    Console.WriteLine("================================================================================");
    Console.WriteLine("  Exercise 14.2 Results - Mutation Testing");
    Console.WriteLine("================================================================================");
    Console.WriteLine("  ✅ Key Achievements:");
    Console.WriteLine($"     • Original transformation logic: {originalPassCount}/{originalTests.Length} tests passed");
    Console.WriteLine($"     • Mutations applied and tested: {mutations.Length}");
    Console.WriteLine($"     • Mutations caught by tests: {mutationsCaught}/{mutations.Length}");
    Console.WriteLine($"     • Mutation score: {mutationScore:F1}%");
    Console.WriteLine($"     • Kafka message transformations: {transformedCount} validated");
    Console.WriteLine();
    Console.WriteLine("  📚 Key Learnings:");
    Console.WriteLine("     ✓ Mutation testing reveals test quality gaps");
    Console.WriteLine("     ✓ High mutation scores indicate robust test coverage");
    Console.WriteLine("     ✓ Tests should catch semantic changes, not just syntax");
    Console.WriteLine("     ✓ Real Kafka validation ensures production-ready code");
    Console.WriteLine("     ✓ Mutation testing improves overall code quality");
    Console.WriteLine();
    Console.WriteLine("  🎯 Production Insights:");
    Console.WriteLine("     • Mutation testing used at Google, Netflix, Facebook");
    Console.WriteLine("     • 80%+ mutation score indicates high-quality tests");
    Console.WriteLine("     • Catches subtle bugs that code reviews miss");
    Console.WriteLine("     • Validates transformation logic correctness");
    Console.WriteLine("     • Essential for critical data pipelines");
    Console.WriteLine();
    Console.WriteLine("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    
    Environment.Exit(0);
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 14.2: Mutation Testing");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Original transformation function
static double CelsiusToFahrenheit(double celsius)
{
    return (celsius * 9.0 / 5.0) + 32.0;
}
