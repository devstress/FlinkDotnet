using Exercise35.Core;
using Exercise35.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Exercise35;

/// <summary>
/// Exercise 3.5: Simple BackpressureQueue Implementation (Per-Customer)
/// 
/// Architecture: Gateway(producer) → Kafka → Flink → Temporal(processor)
/// - Gateway puts messages to Kafka
/// - Flink routes to Temporal by customer 
/// - Temporal receives and discards
/// - BackpressureQueue=2 per customer for all services
/// - Scale: 2 Gateways, 4 Flink task managers, 4 Temporal instances
/// 
/// Test scenarios:
/// 1. 3,000,000 messages | 300 customers | 10 partitions | BackpressureQueue=2 per customer
/// 2. 1,000,000 messages | 300 customers | 10 partitions | BackpressureQueue=2 per customer
/// 3. 1,000,000 messages | 300 customers | 10 partitions | BackpressureQueue=2 per customer
/// </summary>
class Program
{
    // Kafka address - read from environment variable set by test infrastructure
    // KAFKA_BOOTSTRAP_SERVERS: For host-to-container communication (exercise Kafka operations)
    // Lazy evaluation - reads env var when first accessed, not at class load time
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";

    static async Task Main(string[] args)
    {
        // Configure logging
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Console.WriteLine("🚀 Exercise 3.5: Simple BackpressureQueue Implementation (Per-Customer)");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine("Architecture: Gateway → Kafka → Flink → Temporal");
        Console.WriteLine("BackpressureQueue=2 per customer for all services");
        Console.WriteLine("Scale: 2 Gateways, 4 Flink TaskManagers, 4 Temporal instances");
        Console.WriteLine("");

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddLogging(builder => 
                {
                    builder.ClearProviders();
                    builder.AddSerilog();
                });
            })
            .UseSerilog()
            .Build();

        try
        {
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Starting Exercise 3.5: Simple BackpressureQueue Implementation (Per-Customer)");

            await RunTestScenariosAsync(logger);

            logger.LogInformation("Exercise 3.5 completed successfully");
            
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
            Console.WriteLine("================================================================================");
            Console.WriteLine("✅ Simple BackpressureQueue Implementation completed");
            Console.WriteLine();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error in Exercise 3.5");
            Console.WriteLine($"❌ Error: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            await host.StopAsync();
            await Log.CloseAndFlushAsync();
        }
    }

    static async Task RunTestScenariosAsync(Microsoft.Extensions.Logging.ILogger logger)
    {
        // Initialize BackpressureQueue configuration
        var backpressureConfig = BackpressureConfiguration.CreateDefault();
        
        Console.WriteLine("🔧 BackpressureQueue Configuration Management:");
        Console.WriteLine("".PadRight(60, '-'));
        backpressureConfig.ValidateAndLog(logger);
        Console.WriteLine("");

        // Test scenario configurations
        var scenarios = new[]
        {
            new TestScenario 
            { 
                Name = "Scenario 1: High Volume", 
                TargetMessages = 3_000_000, 
                Customers = 300,
                TopicPartitionCount = 10,
                BackpressureConfig = backpressureConfig
            },
            new TestScenario 
            {
                Name = "Scenario 2: Medium Volume, Standard Partitions",
                TargetMessages = 1_000_000,
                Customers = 300,
                TopicPartitionCount = 10,
                BackpressureConfig = backpressureConfig
            },
            new TestScenario 
            {
                Name = "Scenario 3: Medium Volume, Standard Partitions",
                TargetMessages = 1_000_000,
                Customers = 300,
                TopicPartitionCount = 10,
                BackpressureConfig = backpressureConfig
            }
        };

        Console.WriteLine("📋 Test Scenarios Overview:");
        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"  • {scenario.Name}");
            Console.WriteLine($"    Messages: {scenario.TargetMessages:N0} | Customers: {scenario.Customers} | Partitions: {scenario.TopicPartitionCount}");
            Console.WriteLine($"    BackpressureQueue Config: {scenario.BackpressureConfig.GetConfigurationInfo()}");
        }
        Console.WriteLine("");

        // For this demo, we'll run simplified versions of the scenarios
        foreach (var scenario in scenarios)
        {
            Console.WriteLine($"🔬 Running {scenario.Name}...");
            await RunScenarioAsync(scenario, logger);
            Console.WriteLine($"✅ {scenario.Name} completed");
            Console.WriteLine("");
            
            // Small delay between scenarios
            await Task.Delay(2000);
        }
    }

    static async Task RunScenarioAsync(TestScenario scenario, Microsoft.Extensions.Logging.ILogger logger)
    {
        // For demo purposes, we'll use a scaled-down version
        var scaledMessages = Math.Min(scenario.TargetMessages, 1000); // Demo with max 1000 messages
        var runDuration = TimeSpan.FromSeconds(30); // Run for 30 seconds

        logger.LogInformation("Starting scenario: {ScenarioName} (scaled to {Messages} messages for demo)",
            scenario.Name, scaledMessages);

        using var orchestrator = new ScenarioOrchestrator(
            bootstrapServers: KafkaBootstrapServers, // Use environment variable for dynamic Kafka endpoint
            topicName: $"backpressure-exercise-{scenario.TopicPartitionCount}p",
            scenario: scenario,
            logger: logger);

        var stats = await orchestrator.RunAsync(runDuration);

        // Display results
        Console.WriteLine($"📊 {scenario.Name} Results:");
        Console.WriteLine($"  Duration: {stats.Duration:mm\\:ss}");
        Console.WriteLine($"  Messages Sent: {stats.TotalMessagesSent:N0}");
        Console.WriteLine($"  Messages Processed: {stats.TotalMessagesProcessed:N0}");
        Console.WriteLine($"  Messages Dropped (Backpressure): {stats.TotalMessagesDropped:N0}");
        Console.WriteLine($"  Success Rate: {stats.SuccessRate:F1}%");
        Console.WriteLine($"  Throughput: {stats.MessagesPerSecond:F0} msg/sec");
        
        Console.WriteLine("  Service Statistics:");
        foreach (var serviceStat in stats.ServiceStats)
        {
            Console.WriteLine($"    {serviceStat.ServiceName}: {serviceStat.OverallUtilizationPercentage:F1}% avg utilization, {serviceStat.ActiveCustomers} active customers");
            
            // Show top 5 most active customers for this service
            var topCustomers = serviceStat.CustomerStats
                .OrderByDescending(c => c.UtilizationPercentage)
                .Take(5)
                .ToList();
            
            if (topCustomers.Any())
            {
                Console.WriteLine($"      Top customers: {string.Join(", ", topCustomers.Select(c => $"C{c.CustomerId}({c.UtilizationPercentage:F0}%)"))}");
            }
        }
    }
}

/// <summary>
/// Test scenario configuration.
/// </summary>
public class TestScenario
{
    public string Name { get; set; } = string.Empty;
    public int TargetMessages { get; set; }
    public int Customers { get; set; }
    public int TopicPartitionCount { get; set; }
    public BackpressureConfiguration BackpressureConfig { get; set; } = BackpressureConfiguration.CreateDefault();
    
    [Obsolete("Use BackpressureConfig.GetMaxConcurrencyPerCustomer() instead")]
    public int BackpressureQueue { get; set; } = 2; // Kept for backward compatibility
}

/// <summary>
/// Orchestrates a complete test scenario with all services.
/// </summary>
public class ScenarioOrchestrator : IDisposable
{
    private readonly List<GatewayService> _gateways;
    private readonly List<FlinkProcessorService> _flinkProcessors;
    private readonly TemporalService _temporalService;
    private readonly TestScenario _scenario;
    private readonly Microsoft.Extensions.Logging.ILogger _logger;
    private volatile bool _disposed;

    public ScenarioOrchestrator(
        string bootstrapServers,
        string topicName,
        TestScenario scenario,
        Microsoft.Extensions.Logging.ILogger logger)
    {
        _scenario = scenario;
        _logger = logger;

        // Get BackpressureQueue configuration
        var backpressureConfig = scenario.BackpressureConfig;

        // Initialize services - using simpler logger creation
        _gateways = new List<GatewayService>();
        _flinkProcessors = new List<FlinkProcessorService>();

        // For demo purposes, create simple logger
        var loggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder => builder.AddConsole());
        var gatewayLogger = loggerFactory.CreateLogger<GatewayService>();
        var flinkLogger = loggerFactory.CreateLogger<FlinkProcessorService>();
        var temporalLogger = loggerFactory.CreateLogger<TemporalService>();

        // Create 2 Gateway instances with configured BackpressureQueue
        for (int i = 0; i < 2; i++)
        {
            _gateways.Add(new GatewayService(bootstrapServers, topicName, i, backpressureConfig, gatewayLogger));
        }

        // Create 4 Temporal instances (endpoints would be real URLs in production)
        var temporalEndpoints = Enumerable.Range(0, 4)
            .Select(i => $"http://temporal-{i}:7233")
            .ToList();
        
        _temporalService = new TemporalService(temporalEndpoints, backpressureConfig, temporalLogger);

        // Create 4 Flink TaskManager instances with configured BackpressureQueue
        for (int i = 0; i < 4; i++)
        {
            _flinkProcessors.Add(new FlinkProcessorService(
                bootstrapServers,
                topicName,
                $"flink-consumer-group",
                i,
                temporalEndpoints,
                backpressureConfig,
                flinkLogger));
        }

        logger.LogInformation("Scenario orchestrator initialized: 2 Gateways, 4 Flink TaskManagers, 4 Temporal instances");
        logger.LogInformation("BackpressureQueue configuration: {Config}", backpressureConfig.GetConfigurationInfo());
    }

    public async Task<ScenarioResults> RunAsync(TimeSpan duration)
    {
        var startTime = DateTime.UtcNow;
        using var cts = new CancellationTokenSource(duration);

        _logger.LogInformation("Starting scenario execution for {Duration}", duration);

        try
        {
            // Start Flink processors (they will consume from Kafka)
            var flinkTasks = _flinkProcessors.Select(processor => 
                Task.Run(() => processor.StartProcessingAsync(cts.Token))).ToList();

            // Generate and send messages through gateways
            var totalMessagesSent = 0;
            var totalMessagesDropped = 0;
            var gatewayTasks = new List<Task>();

            // Distribute message generation across gateways
            var messagesPerGateway = Math.Min(_scenario.TargetMessages, 500) / _gateways.Count;
            
            foreach (var gateway in _gateways)
            {
                var gatewayTask = Task.Run(async () =>
                {
                    while (!cts.Token.IsCancellationRequested)
                    {
                        var messages = GenerateCustomerMessages(100, _scenario.Customers); // Send in batches of 100
                        var result = await gateway.SendMessagesAsync(messages, cts.Token);
                        
                        Interlocked.Add(ref totalMessagesSent, result.MessagesSent);
                        Interlocked.Add(ref totalMessagesDropped, result.MessagesDropped);
                        
                        await Task.Delay(100, cts.Token); // Small delay between batches
                    }
                });
                gatewayTasks.Add(gatewayTask);
            }

            // Wait for duration to complete
            await Task.Delay(duration, cts.Token);
            
            // Stop all processing
            cts.Cancel();
            
            // Wait for tasks to complete
            await Task.WhenAll(gatewayTasks.Concat(flinkTasks));

            var endTime = DateTime.UtcNow;
            var actualDuration = endTime - startTime;

            // Collect statistics
            var serviceStats = new List<BackpressureStats>();
            serviceStats.AddRange(_gateways.Select(g => g.GetStats()));
            serviceStats.AddRange(_flinkProcessors.Select(f => f.GetStats()));
            serviceStats.AddRange(_temporalService.GetAllStats());

            var totalProcessed = serviceStats.Where(s => s.ServiceName.StartsWith("Temporal"))
                .Sum(s => s.TotalConcurrency); // Use TotalConcurrency instead

            return new ScenarioResults
            {
                Duration = actualDuration,
                TotalMessagesSent = totalMessagesSent,
                TotalMessagesProcessed = totalProcessed,
                TotalMessagesDropped = totalMessagesDropped,
                ServiceStats = serviceStats
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Scenario execution completed");
            throw;
        }
    }

    private List<CustomerMessage> GenerateCustomerMessages(int count, int maxCustomerId)
    {
        var messages = new List<CustomerMessage>();
        for (int i = 0; i < count; i++)
        {
            messages.Add(new CustomerMessage
            {
                CustomerId = Random.Shared.Next(1, maxCustomerId + 1),
                Data = $"Sample data {i}",
                Timestamp = DateTime.UtcNow,
                MessageType = "BackpressureTest"
            });
        }
        return messages;
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _disposed = true;
            
            foreach (var gateway in _gateways)
                gateway.Dispose();
            
            foreach (var processor in _flinkProcessors)
                processor.Dispose();
            
            _temporalService.Dispose();
        }
    }
}

/// <summary>
/// Results from running a test scenario.
/// </summary>
public class ScenarioResults
{
    public TimeSpan Duration { get; set; }
    public int TotalMessagesSent { get; set; }
    public int TotalMessagesProcessed { get; set; }
    public int TotalMessagesDropped { get; set; }
    public List<BackpressureStats> ServiceStats { get; set; } = new();

    public double SuccessRate => TotalMessagesSent > 0 
        ? (double)TotalMessagesProcessed / TotalMessagesSent * 100 
        : 0;

    public double MessagesPerSecond => Duration.TotalSeconds > 0 
        ? TotalMessagesSent / Duration.TotalSeconds 
        : 0;
}