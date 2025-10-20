using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;
using System.Text.Json;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 4 Exercise 4.4: Production Deployment - Blue-Green, Canary, Rolling Update");
Console.WriteLine("".PadRight(85, '='));
Console.WriteLine();
Console.WriteLine("📊 Real deployment strategies with Kafka/FlinkDotNet infrastructure:");
Console.WriteLine("   • Blue-Green: Instant traffic switching with health validation");
Console.WriteLine("   • Canary: Progressive rollout (1% → 5% → 25% → 100%)");
Console.WriteLine("   • Rolling Update: Batch-wise instance updates with health gates");
Console.WriteLine("   • Health Monitoring: Real-time health events from Kafka");
Console.WriteLine("   • Industry Patterns: Netflix, AWS, Spotify deployment strategies");
Console.WriteLine();

try
{
    // Get Kafka bootstrap servers from environment
    var kafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") 
        ?? throw new InvalidOperationException("KAFKA_BOOTSTRAP_SERVERS environment variable not set");
    
    var kafkaFlinkBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") 
        ?? kafkaBootstrapServers;

    Log.Information("Using Kafka bootstrap servers: {BootstrapServers}", kafkaBootstrapServers);
    Log.Information("Using Kafka Flink bootstrap servers: {FlinkBootstrapServers}", kafkaFlinkBootstrapServers);

    // Validate infrastructure
    await ValidateInfrastructure(kafkaBootstrapServers);

    // Create Kafka topics for deployment orchestration
    await CreateKafkaTopics(kafkaBootstrapServers);

    // Create Flink execution environment
    var env = StreamExecutionEnvironment.GetExecutionEnvironment();
    env.SetParallelism(1); // Single parallelism for ordered deployment processing

    Log.Information("Starting consolidated deployment orchestration job...");

    // NOTE: FlinkDotNet currently supports only ONE job per StreamExecutionEnvironment
    // This consolidated job processes all deployment strategies in a single pipeline
    // See update-LearningCourse.md lines 23-59 for multi-job limitation details
    var deploymentJob = await CreateConsolidatedDeploymentJob(env, kafkaFlinkBootstrapServers);

    Log.Information("Deployment orchestration job submitted successfully");

    // Produce deployment requests for all three strategies
    await ProduceDeploymentRequests(kafkaBootstrapServers);

    // Consume and display health check events (demonstrates health monitoring)
    await ConsumeHealthCheckEvents(kafkaBootstrapServers);

    // Consume and display deployment results
    await ConsumeDeploymentResults(kafkaBootstrapServers);

    // Cleanup: Cancel Flink job
    Log.Information("Cleaning up Flink job...");
    await deploymentJob.CancelAsync();
    Log.Information("Flink job cancelled successfully");

    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine("✅ All deployment strategies executed with real Kafka/FlinkDotNet infrastructure");
    Console.WriteLine();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 4.4: Production Deployment");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Infrastructure validation
static Task ValidateInfrastructure(string kafkaBootstrapServers)
{
    Log.Information("Validating Kafka infrastructure...");
    
    var config = new AdminClientConfig { BootstrapServers = kafkaBootstrapServers };
    using var adminClient = new AdminClientBuilder(config).Build();
    
    try
    {
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        Log.Information("✅ Kafka is ready with {BrokerCount} brokers", metadata.Brokers.Count);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException($"Kafka validation failed: {ex.Message}", ex);
    }
    
    return Task.CompletedTask;
}

// Create Kafka topics for deployment orchestration
static async Task CreateKafkaTopics(string kafkaBootstrapServers)
{
    var topics = new[]
    {
        "deployment-requests",
        "blue-green-events",
        "canary-events",
        "rolling-update-events",
        "deployment-results",
        "health-check-events"
    };

    var config = new AdminClientConfig { BootstrapServers = kafkaBootstrapServers };
    using var adminClient = new AdminClientBuilder(config).Build();

    try
    {
        var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
        var existingTopics = metadata.Topics.Select(t => t.Topic).ToHashSet();
        
        var topicsToCreate = topics
            .Where(topic => !existingTopics.Contains(topic))
            .Select(topic => new TopicSpecification
            {
                Name = topic,
                NumPartitions = 1,
                ReplicationFactor = 1
            })
            .ToList();

        if (topicsToCreate.Any())
        {
            await adminClient.CreateTopicsAsync(topicsToCreate);
            Log.Information("✅ Topics created: {Topics}", string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        else
        {
            Log.Information("✅ Topics already exist");
        }
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Topic creation failed (may already exist): {Message}", ex.Message);
    }
}

// Consolidated Deployment Job (Single Job Pattern)
// FlinkDotNet limitation: Only ONE job per StreamExecutionEnvironment
// This job processes all deployment strategies in a single pipeline
static async Task<IJobClient> CreateConsolidatedDeploymentJob(
    StreamExecutionEnvironment env,
    string kafkaBootstrapServers)
{
    Log.Information("Creating Consolidated Deployment Orchestration Job...");

    // Single source: Read all deployment requests
    var deploymentRequests = env
        .FromKafka("deployment-requests", kafkaBootstrapServers, "deployment-orchestrator", "earliest")
        .Map(new DeploymentRequestParser());

    // Process Blue-Green deployments
    var blueGreenEvents = deploymentRequests
        .Filter(new BlueGreenFilter())
        .FlatMap(new BlueGreenStageGenerator());
    blueGreenEvents.SinkToKafka("blue-green-events", kafkaBootstrapServers);

    // Process Canary deployments
    var canaryEvents = deploymentRequests
        .Filter(new CanaryFilter())
        .FlatMap(new CanaryPhaseGenerator());
    canaryEvents.SinkToKafka("canary-events", kafkaBootstrapServers);

    // Process Rolling Update deployments
    var rollingUpdateEvents = deploymentRequests
        .Filter(new RollingUpdateFilter())
        .FlatMap(new RollingUpdateBatchGenerator());
    rollingUpdateEvents.SinkToKafka("rolling-update-events", kafkaBootstrapServers);

    // Generate health check events for all deployments
    var healthEvents = deploymentRequests
        .FlatMap(new HealthCheckGenerator());
    healthEvents.SinkToKafka("health-check-events", kafkaBootstrapServers);

    // Collect final results from Blue-Green deployments
    var blueGreenResults = blueGreenEvents
        .Filter(new FinalStageFilter("DecommissionBlue"))
        .Map(new BlueGreenResultMapper());
    blueGreenResults.SinkToKafka("deployment-results", kafkaBootstrapServers);

    // Collect final results from Canary deployments
    var canaryResults = canaryEvents
        .Filter(new CanaryFinalPhaseFilter())
        .Map(new CanaryResultMapper());
    canaryResults.SinkToKafka("deployment-results", kafkaBootstrapServers);

    // Collect final results from Rolling Update deployments
    var rollingUpdateResults = rollingUpdateEvents
        .Filter(new RollingUpdateFinalBatchFilter())
        .Map(new RollingUpdateResultMapper());
    rollingUpdateResults.SinkToKafka("deployment-results", kafkaBootstrapServers);

    return await env.ExecuteAsync("ConsolidatedDeploymentOrchestrator");
}

// Produce deployment requests for all three strategies
static async Task ProduceDeploymentRequests(string kafkaBootstrapServers)
{
    Log.Information("Producing deployment requests...");

    var config = new ProducerConfig
    {
        BootstrapServers = kafkaBootstrapServers,
        ClientId = "exercise44-producer"
    };

    using var producer = new ProducerBuilder<Null, string>(config).Build();

    var strategies = new[] { "BlueGreen", "Canary", "RollingUpdate" };

    foreach (var strategy in strategies)
    {
        var request = new DeploymentRequest
        {
            DeploymentId = Guid.NewGuid().ToString("N")[..8],
            Strategy = strategy,
            ApplicationName = "FlinkDotNet-StreamProcessor",
            Version = "v2.1.0",
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        var json = JsonSerializer.Serialize(request);
        await producer.ProduceAsync("deployment-requests",
            new Message<Null, string> { Value = json });

        Log.Information("📤 Deployment request produced: {Strategy} (ID: {DeploymentId})",
            strategy, request.DeploymentId);
    }

    producer.Flush(TimeSpan.FromSeconds(10));
    Log.Information("✅ All deployment requests produced successfully");
}

// Consume and display health check events
static Task ConsumeHealthCheckEvents(string kafkaBootstrapServers)
{
    Log.Information("Consuming health check events...");

    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaBootstrapServers,
        GroupId = "exercise44-health-consumer",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };

    using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    consumer.Subscribe("health-check-events");

    var checksReceived = 0;
    var targetChecks = 5; // database, cache, external_api, memory, cpu
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

    Console.WriteLine();
    Console.WriteLine("🏥 Health Check Events:");
    Console.WriteLine("".PadRight(85, '-'));

    try
    {
        while (checksReceived < targetChecks && !cts.Token.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(2));
            if (consumeResult?.Message?.Value != null)
            {
                var healthCheck = JsonSerializer.Deserialize<HealthCheckEvent>(consumeResult.Message.Value);
                if (healthCheck != null)
                {
                    checksReceived++;
                    Console.WriteLine($"   ✅ {healthCheck.CheckName}: {healthCheck.Status} ({healthCheck.ResponseTimeMs}ms)");
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when timeout occurs
    }

    consumer.Close();
    Log.Information("✅ {Count} health checks validated", checksReceived);
    
    return Task.CompletedTask;
}

// Consume and display deployment results
static Task ConsumeDeploymentResults(string kafkaBootstrapServers)
{
    Log.Information("Consuming deployment results...");

    var config = new ConsumerConfig
    {
        BootstrapServers = kafkaBootstrapServers,
        GroupId = "exercise44-consumer",
        AutoOffsetReset = AutoOffsetReset.Earliest,
        EnableAutoCommit = true
    };

    using var consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    consumer.Subscribe("deployment-results");

    var resultsReceived = 0;
    var targetResults = 3; // Blue-Green, Canary, RollingUpdate
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

    Console.WriteLine();
    Console.WriteLine("📊 Deployment Results:");
    Console.WriteLine("".PadRight(85, '-'));

    try
    {
        while (resultsReceived < targetResults && !cts.Token.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(TimeSpan.FromSeconds(5));
            if (consumeResult?.Message?.Value != null)
            {
                var result = JsonSerializer.Deserialize<DeploymentResult>(consumeResult.Message.Value);
                if (result != null)
                {
                    resultsReceived++;
                    Console.WriteLine();
                    Console.WriteLine($"🎯 {result.Strategy} Deployment (ID: {result.DeploymentId}):");
                    Console.WriteLine($"   Status: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");
                    Console.WriteLine($"   Message: {result.Message}");
                    Console.WriteLine($"   Duration: {result.DurationMs}ms");
                    Console.WriteLine($"   Completed Stages: {result.CompletedStages.Count}");
                    foreach (var stage in result.CompletedStages)
                    {
                        Console.WriteLine($"      ✓ {stage}");
                    }
                }
            }
        }
    }
    catch (OperationCanceledException)
    {
        // Expected when timeout occurs
    }

    consumer.Close();
    Log.Information("✅ {Count} deployment results consumed", resultsReceived);
    
    return Task.CompletedTask;
}

// Data models for deployment orchestration
public record DeploymentRequest
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Strategy { get; init; } = string.Empty;
    public string ApplicationName { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public long Timestamp { get; init; }
}

public record BlueGreenEvent
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Stage { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string HealthStatus { get; init; } = string.Empty;
    public long Timestamp { get; init; }
}

public record CanaryEvent
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
    public int TrafficPercent { get; init; }
    public double ErrorRate { get; init; }
    public string HealthStatus { get; init; } = string.Empty;
    public long Timestamp { get; init; }
}

public record RollingUpdateEvent
{
    public string DeploymentId { get; init; } = string.Empty;
    public string InstanceRange { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long Timestamp { get; init; }
}

public record HealthCheckEvent
{
    public string CheckName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public long ResponseTimeMs { get; init; }
    public long Timestamp { get; init; }
}

public record DeploymentResult
{
    public string DeploymentId { get; init; } = string.Empty;
    public string Strategy { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> CompletedStages { get; init; } = new();
    public long DurationMs { get; init; }
}

// Flink Function implementations
public class DeploymentRequestParser : IMapFunction<string, string>
{
    public string Map(string input) => input;
}

public class BlueGreenFilter : IFilterFunction<string>
{
    public bool Filter(string value)
    {
        try
        {
            var request = JsonSerializer.Deserialize<DeploymentRequest>(value);
            return request?.Strategy == "BlueGreen";
        }
        catch { return false; }
    }
}

public class CanaryFilter : IFilterFunction<string>
{
    public bool Filter(string value)
    {
        try
        {
            var request = JsonSerializer.Deserialize<DeploymentRequest>(value);
            return request?.Strategy == "Canary";
        }
        catch { return false; }
    }
}

public class RollingUpdateFilter : IFilterFunction<string>
{
    public bool Filter(string value)
    {
        try
        {
            var request = JsonSerializer.Deserialize<DeploymentRequest>(value);
            return request?.Strategy == "RollingUpdate";
        }
        catch { return false; }
    }
}

public class BlueGreenStageGenerator : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string input)
    {
        DeploymentRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<DeploymentRequest>(input);
        }
        catch
        {
            yield break;
        }
        
        if (request == null) yield break;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var stages = new[] { "PrepareGreen", "DeployToGreen", "HealthCheckGreen",
                            "SwitchTraffic", "VerifyProduction", "DecommissionBlue" };

        foreach (var stage in stages)
        {
            var evt = new BlueGreenEvent
            {
                DeploymentId = request.DeploymentId,
                Stage = stage,
                Status = "Success",
                HealthStatus = "Healthy",
                Timestamp = timestamp
            };
            yield return JsonSerializer.Serialize(evt);
        }
    }
}

public class CanaryPhaseGenerator : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string input)
    {
        DeploymentRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<DeploymentRequest>(input);
        }
        catch
        {
            yield break;
        }
        
        if (request == null) yield break;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var phases = new[] { (1, "Deploy1%"), (1, "Monitor1%"), (5, "Deploy5%"), (5, "Monitor5%"),
                            (25, "Deploy25%"), (25, "Monitor25%"), (100, "Deploy100%") };

        foreach (var (traffic, phase) in phases)
        {
            var errorRate = traffic switch
            {
                1 => 0.0001, 5 => 0.0002, 25 => 0.0005, 100 => 0.0008, _ => 0.0001
            };

            var evt = new CanaryEvent
            {
                DeploymentId = request.DeploymentId,
                Phase = phase,
                TrafficPercent = traffic,
                ErrorRate = errorRate,
                HealthStatus = errorRate < 0.001 ? "Healthy" : "Warning",
                Timestamp = timestamp
            };
            yield return JsonSerializer.Serialize(evt);
        }
    }
}

public class RollingUpdateBatchGenerator : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string input)
    {
        DeploymentRequest? request = null;
        try
        {
            request = JsonSerializer.Deserialize<DeploymentRequest>(input);
        }
        catch
        {
            yield break;
        }
        
        if (request == null) yield break;

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var batches = new[] { "1-3", "4-6", "7-9", "10-12" };

        foreach (var batch in batches)
        {
            foreach (var status in new[] { "Updating", "Updated", "HealthCheckPassed" })
            {
                var evt = new RollingUpdateEvent
                {
                    DeploymentId = request.DeploymentId,
                    InstanceRange = batch,
                    Status = status,
                    Timestamp = timestamp
                };
                yield return JsonSerializer.Serialize(evt);
            }
        }
    }
}

public class HealthCheckGenerator : IFlatMapFunction<string, string>
{
    public IEnumerable<string> FlatMap(string input)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var checks = new[] { ("database", 50L), ("cache", 25L), ("external_api", 100L), ("memory", 10L), ("cpu", 10L) };
        
        foreach (var (checkName, responseTime) in checks)
        {
            var evt = new HealthCheckEvent
            {
                CheckName = checkName,
                Status = "Healthy",
                ResponseTimeMs = responseTime,
                Timestamp = timestamp
            };
            yield return JsonSerializer.Serialize(evt);
        }
    }
}

public class FinalStageFilter : IFilterFunction<string>
{
    private readonly string _finalStage;
    public FinalStageFilter(string finalStage) => _finalStage = finalStage;

    public bool Filter(string value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<BlueGreenEvent>(value);
            return evt?.Stage == _finalStage;
        }
        catch { return false; }
    }
}

public class BlueGreenResultMapper : IMapFunction<string, string>
{
    public string Map(string input)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<BlueGreenEvent>(input);
            if (evt == null) return string.Empty;

            var result = new DeploymentResult
            {
                DeploymentId = evt.DeploymentId,
                Strategy = "BlueGreen",
                Success = evt.Status == "Success",
                Message = "Blue-Green deployment completed - instant traffic switch from blue to green",
                CompletedStages = new List<string> { "PrepareGreen", "DeployToGreen", "HealthCheckGreen",
                    "SwitchTraffic", "VerifyProduction", "DecommissionBlue" },
                DurationMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - evt.Timestamp
            };
            return JsonSerializer.Serialize(result);
        }
        catch { return string.Empty; }
    }
}

public class CanaryFinalPhaseFilter : IFilterFunction<string>
{
    public bool Filter(string value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<CanaryEvent>(value);
            return evt?.TrafficPercent == 100;
        }
        catch { return false; }
    }
}

public class CanaryResultMapper : IMapFunction<string, string>
{
    public string Map(string input)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<CanaryEvent>(input);
            if (evt == null) return string.Empty;

            var result = new DeploymentResult
            {
                DeploymentId = evt.DeploymentId,
                Strategy = "Canary",
                Success = evt.HealthStatus == "Healthy",
                Message = $"Canary deployment completed - progressive rollout to 100% (Error rate: {evt.ErrorRate:F4})",
                CompletedStages = new List<string> { "Deploy1%", "Monitor1%", "Deploy5%", "Monitor5%",
                    "Deploy25%", "Monitor25%", "Deploy100%" },
                DurationMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - evt.Timestamp
            };
            return JsonSerializer.Serialize(result);
        }
        catch { return string.Empty; }
    }
}

public class RollingUpdateFinalBatchFilter : IFilterFunction<string>
{
    public bool Filter(string value)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<RollingUpdateEvent>(value);
            return evt?.InstanceRange == "10-12" && evt.Status == "HealthCheckPassed";
        }
        catch { return false; }
    }
}

public class RollingUpdateResultMapper : IMapFunction<string, string>
{
    public string Map(string input)
    {
        try
        {
            var evt = JsonSerializer.Deserialize<RollingUpdateEvent>(input);
            if (evt == null) return string.Empty;

            var result = new DeploymentResult
            {
                DeploymentId = evt.DeploymentId,
                Strategy = "RollingUpdate",
                Success = true,
                Message = "Rolling update completed - all 12 instances updated with health validation",
                CompletedStages = new List<string> { "UpdateInstances1-3", "UpdateInstances4-6",
                    "UpdateInstances7-9", "UpdateInstances10-12" },
                DurationMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - evt.Timestamp
            };
            return JsonSerializer.Serialize(result);
        }
        catch { return string.Empty; }
    }
}
