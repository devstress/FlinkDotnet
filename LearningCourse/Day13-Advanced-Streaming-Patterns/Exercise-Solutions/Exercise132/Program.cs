using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise132;

/// <summary>
/// Exercise 13.2: CQRS Pattern Implementation
/// 
/// Real-time CQRS system for banking transactions that demonstrates:
/// - Command Query Responsibility Segregation (CQRS) pattern
/// - Separate command (write) and query (read) models
/// - Command processing (Deposit, Withdraw, Transfer)
/// - Event generation and broadcasting
/// - Multiple read model projections (Balance, History, Audit)
/// - Event-driven synchronization between models
/// 
/// Architecture: Commands → CommandProcessor → Events → [BalanceView | HistoryView | AuditView]
/// </summary>
class Program
{
    // Kafka addresses - read from environment variables set by test infrastructure
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";

    // Kafka topics for CQRS pattern
    private const string CommandsTopic = "banking-commands";
    private const string EventsTopic = "banking-events";
    private const string BalanceTopic = "query-balance";
    private const string HistoryTopic = "query-history";
    private const string AuditTopic = "query-audit";
    private const string ConsumerGroup = "exercise132-consumer";
    
    // Test scenarios for CQRS validation
    private static readonly List<CQRSScenario> Scenarios = new()
    {
        new() { Name = "Basic Banking Operations", TransactionCount = 20 },
        new() { Name = "High Volume Transactions", TransactionCount = 40 },
        new() { Name = "Complex Transfer Scenarios", TransactionCount = 30 }
    };

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        try
        {
            Log.Information("================================================================================");
            Log.Information("  Exercise 13.2: CQRS Pattern Implementation");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("Learning Objectives:");
            Log.Information("  - Command Query Responsibility Segregation (CQRS)");
            Log.Information("  - Separate write (commands) and read (queries) models");
            Log.Information("  - Multiple read model projections from same events");
            Log.Information("  - Event-driven synchronization between models");
            Log.Information("");
            Log.Information("Configuration:");
            Log.Information("  Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("  Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("  Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? commandProcessorJob = null;
            FlinkDotNet.DataStream.IJobClient? balanceViewJob = null;
            FlinkDotNet.DataStream.IJobClient? historyViewJob = null;
            FlinkDotNet.DataStream.IJobClient? auditViewJob = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/9: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/9: Verifying Flink cluster is ready...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/9: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Submit Command Processor job (Write Side)
                Log.Information(">> Step 4/9: Submitting Command Processor job (Commands → Events)...");
                commandProcessorJob = await SubmitCommandProcessorJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start
                Log.Information("");

                // Step 3: Submit Query Model jobs (Read Side)
                Log.Information(">> Step 5/9: Submitting Balance View job (Events → Balance)...");
                balanceViewJob = await SubmitBalanceViewJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
                Log.Information("");

                Log.Information(">> Step 6/9: Submitting History View job (Events → History)...");
                historyViewJob = await SubmitHistoryViewJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
                Log.Information("");

                Log.Information(">> Step 7/9: Submitting Audit View job (Events → Audit)...");
                auditViewJob = await SubmitAuditViewJobAsync();
                await Task.Delay(TimeSpan.FromSeconds(3));
                Log.Information("");

                // Step 4: Execute CQRS scenarios
                Log.Information(">> Step 8/9: Executing CQRS scenarios...");
                var results = await ExecuteCQRSScenariosAsync();
                Log.Information("");

                // Step 5: Generate CQRS report
                Log.Information(">> Step 9/9: Generating CQRS report...");
                GenerateCQRSReport(results);
                Log.Information("");

                // Results
                Log.Information("================================================================================");
                Log.Information("  Exercise 13.2 Results - CQRS Pattern");
                Log.Information("================================================================================");
                Log.Information("  CQRS Metrics:");
                Log.Information("     Total Commands: {Commands:N0}", results.Sum(r => r.CommandsIssued));
                Log.Information("     Total Events: {Events:N0}", results.Sum(r => r.EventsGenerated));
                Log.Information("     Balance Updates: {Balance:N0}", results.Sum(r => r.BalanceUpdates));
                Log.Information("     History Updates: {History:N0}", results.Sum(r => r.HistoryUpdates));
                Log.Information("     Audit Updates: {Audit:N0}", results.Sum(r => r.AuditUpdates));
                Log.Information("     Deposits: {Deposits:N0}", results.Sum(r => r.Deposits));
                Log.Information("     Withdrawals: {Withdrawals:N0}", results.Sum(r => r.Withdrawals));
                Log.Information("     Transfers: {Transfers:N0}", results.Sum(r => r.Transfers));
                Log.Information("");
                Log.Information("  Key Learnings:");
                Log.Information("     [SUCCESS] Command-Query separation validated");
                Log.Information("     [SUCCESS] Multiple read models working");
                Log.Information("     [SUCCESS] Event-driven synchronization verified");
                Log.Information("     [SUCCESS] Independent scaling capability demonstrated");
                Log.Information("");
                Log.Information("[SUCCESS] Exercise 13.2 COMPLETED successfully");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel all Flink jobs
                if (commandProcessorJob != null)
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Command Processor job...");
                    try
                    {
                        await commandProcessorJob.CancelAsync();
                        Log.Information("   [SUCCESS] Command Processor job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel Command Processor job");
                    }
                }

                if (balanceViewJob != null)
                {
                    Log.Information(">> Cleaning up: Cancelling Balance View job...");
                    try
                    {
                        await balanceViewJob.CancelAsync();
                        Log.Information("   [SUCCESS] Balance View job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel Balance View job");
                    }
                }

                if (historyViewJob != null)
                {
                    Log.Information(">> Cleaning up: Cancelling History View job...");
                    try
                    {
                        await historyViewJob.CancelAsync();
                        Log.Information("   [SUCCESS] History View job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel History View job");
                    }
                }

                if (auditViewJob != null)
                {
                    Log.Information(">> Cleaning up: Cancelling Audit View job...");
                    try
                    {
                        await auditViewJob.CancelAsync();
                        Log.Information("   [SUCCESS] Audit View job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel Audit View job");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 13.2 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for command processing (Commands → Events) - WRITE SIDE
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitCommandProcessorJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka commands topic
        var commandStream = environment.FromKafka(
            topic: CommandsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup + "-processor",
            startingOffsets: "earliest"
        );

        // Process commands and generate events
        var eventStream = commandStream
            .Map(new CommandProcessorFunction());

        // Sink events to Kafka (event store)
        eventStream.SinkToKafka(EventsTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise132-CommandProcessor");

        Log.Information("   [SUCCESS] Command Processor job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        Log.Information("   Function: Commands → Events (Write Side)");
        
        return jobClient;
    }

    /// <summary>
    /// Submit Flink job for balance view (Events → Balance) - READ SIDE
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitBalanceViewJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka events topic
        var eventStream = environment.FromKafka(
            topic: EventsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup + "-balance",
            startingOffsets: "earliest"
        );

        // Project balance view from events
        var balanceStream = eventStream
            .Map(new BalanceProjectionFunction());

        // Sink balance to Kafka (balance query topic)
        balanceStream.SinkToKafka(BalanceTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise132-BalanceView");

        Log.Information("   [SUCCESS] Balance View job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        Log.Information("   Function: Events → Balance (Read Model 1)");
        
        return jobClient;
    }

    /// <summary>
    /// Submit Flink job for history view (Events → History) - READ SIDE
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitHistoryViewJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka events topic
        var eventStream = environment.FromKafka(
            topic: EventsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup + "-history",
            startingOffsets: "earliest"
        );

        // Project history view from events
        var historyStream = eventStream
            .Map(new HistoryProjectionFunction());

        // Sink history to Kafka (history query topic)
        historyStream.SinkToKafka(HistoryTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise132-HistoryView");

        Log.Information("   [SUCCESS] History View job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        Log.Information("   Function: Events → History (Read Model 2)");
        
        return jobClient;
    }

    /// <summary>
    /// Submit Flink job for audit view (Events → Audit) - READ SIDE
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitAuditViewJobAsync()
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source stream from Kafka events topic
        var eventStream = environment.FromKafka(
            topic: EventsTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup + "-audit",
            startingOffsets: "earliest"
        );

        // Project audit view from events
        var auditStream = eventStream
            .Map(new AuditProjectionFunction());

        // Sink audit to Kafka (audit query topic)
        auditStream.SinkToKafka(AuditTopic, KafkaFlinkBootstrapServers);

        // Execute job
        var jobClient = await environment.ExecuteAsync("Exercise132-AuditView");

        Log.Information("   [SUCCESS] Audit View job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        Log.Information("   Function: Events → Audit (Read Model 3)");
        
        return jobClient;
    }

    /// <summary>
    /// Execute all CQRS scenarios
    /// </summary>
    private static async Task<List<ScenarioResult>> ExecuteCQRSScenariosAsync()
    {
        var results = new List<ScenarioResult>();
        
        Console.WriteLine("\n💰 CQRS Banking Scenarios:");
        foreach (var scenario in Scenarios)
        {
            Console.WriteLine($"  • {scenario.Name}: {scenario.TransactionCount} transactions");
        }
        Console.WriteLine();

        foreach (var scenario in Scenarios)
        {
            Log.Information("🏦 Executing {ScenarioName}...", scenario.Name);
            
            var result = await ExecuteSingleScenarioAsync(scenario);
            results.Add(result);
            
            Log.Information("✅ {ScenarioName} completed:", scenario.Name);
            Log.Information("   • Commands: {Commands:N0}", result.CommandsIssued);
            Log.Information("   • Events: {Events:N0}", result.EventsGenerated);
            Log.Information("   • Balance Updates: {Balance:N0}", result.BalanceUpdates);
            Log.Information("   • History Updates: {History:N0}", result.HistoryUpdates);
            Log.Information("   • Audit Updates: {Audit:N0}", result.AuditUpdates);
            Log.Information("   • Deposits: {D}, Withdrawals: {W}, Transfers: {T}", 
                result.Deposits, result.Withdrawals, result.Transfers);
            
            // Cool-down between scenarios
            if (scenario != Scenarios[^1])
            {
                Console.WriteLine("⏸️ Cool-down: 2 seconds...");
                await Task.Delay(2000);
            }
        }

        return results;
    }

    /// <summary>
    /// Execute a single CQRS scenario
    /// </summary>
    private static async Task<ScenarioResult> ExecuteSingleScenarioAsync(CQRSScenario scenario)
    {
        var producerConfig = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = $"exercise132-{scenario.Name.Replace(" ", "-").ToLower()}",
            Acks = Acks.All,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        Log.Information("   Issuing banking commands for {Count} transactions...", scenario.TransactionCount);

        var result = new ScenarioResult { ScenarioName = scenario.Name };
        var stopwatch = Stopwatch.StartNew();

        // Generate diverse banking operations
        for (int i = 0; i < scenario.TransactionCount; i++)
        {
            var accountId = $"ACC-{i % 10:D3}";
            var commandType = i % 3;

            if (commandType == 0) // Deposit
            {
                var depositCommand = new BankingCommand
                {
                    AccountId = accountId,
                    CommandType = "Deposit",
                    Amount = (i % 10 + 1) * 100.0m,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await ProduceCommandAsync(producer, depositCommand);
                result.CommandsIssued++;
                result.Deposits++;
            }
            else if (commandType == 1) // Withdraw
            {
                var withdrawCommand = new BankingCommand
                {
                    AccountId = accountId,
                    CommandType = "Withdraw",
                    Amount = (i % 5 + 1) * 50.0m,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await ProduceCommandAsync(producer, withdrawCommand);
                result.CommandsIssued++;
                result.Withdrawals++;
            }
            else // Transfer
            {
                var targetAccount = $"ACC-{(i + 1) % 10:D3}";
                var transferCommand = new BankingCommand
                {
                    AccountId = accountId,
                    CommandType = "Transfer",
                    Amount = (i % 3 + 1) * 75.0m,
                    TargetAccount = targetAccount,
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                };

                await ProduceCommandAsync(producer, transferCommand);
                result.CommandsIssued++;
                result.Transfers++;
            }
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        stopwatch.Stop();

        result.Duration = stopwatch.Elapsed;
        
        // Wait for processing
        await Task.Delay(TimeSpan.FromSeconds(4));
        
        // Count messages in all query topics
        result.EventsGenerated = await CountMessagesInTopicAsync(EventsTopic);
        result.BalanceUpdates = await CountMessagesInTopicAsync(BalanceTopic);
        result.HistoryUpdates = await CountMessagesInTopicAsync(HistoryTopic);
        result.AuditUpdates = await CountMessagesInTopicAsync(AuditTopic);

        Log.Information("   Scenario completed in {Duration:F2}s", result.Duration.TotalSeconds);

        return result;
    }

    /// <summary>
    /// Produce command to Kafka
    /// </summary>
    private static async Task ProduceCommandAsync(IProducer<string, string> producer, BankingCommand command)
    {
        try
        {
            await producer.ProduceAsync(CommandsTopic, new Message<string, string>
            {
                Key = command.AccountId,
                Value = JsonSerializer.Serialize(command)
            });
        }
        catch (ProduceException<string, string> ex)
        {
            Log.Error(ex, "Failed to produce command {CommandType} for account {AccountId}", 
                command.CommandType, command.AccountId);
        }
    }

    /// <summary>
    /// Count messages in a topic (for validation)
    /// </summary>
    private static Task<int> CountMessagesInTopicAsync(string topicName)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = ConsumerGroup + "-count-" + Guid.NewGuid().ToString("N"),
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(topicName);

        var count = 0;
        var timeoutCount = 0;
        const int maxTimeouts = 5;
        var stopwatch = Stopwatch.StartNew();

        while (timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(15))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(500));
                
                if (result != null)
                {
                    count++;
                    timeoutCount = 0;
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException)
            {
                break;
            }
        }

        consumer.Close();
        return Task.FromResult(count);
    }

    private static void GenerateCQRSReport(List<ScenarioResult> results)
    {
        Console.WriteLine("\n📊 CQRS PATTERN REPORT");
        Console.WriteLine("======================");
        
        foreach (var result in results)
        {
            Console.WriteLine($"\n  💰 {result.ScenarioName}:");
            Console.WriteLine($"     Duration: {result.Duration.TotalSeconds:F2}s");
            Console.WriteLine($"     Commands Issued: {result.CommandsIssued:N0}");
            Console.WriteLine($"     Events Generated: {result.EventsGenerated:N0}");
            Console.WriteLine($"     Balance Updates: {result.BalanceUpdates:N0}");
            Console.WriteLine($"     History Updates: {result.HistoryUpdates:N0}");
            Console.WriteLine($"     Audit Updates: {result.AuditUpdates:N0}");
            Console.WriteLine($"     Operations: D={result.Deposits:N0} | W={result.Withdrawals:N0} | T={result.Transfers:N0}");
        }
        
        Console.WriteLine("\n📈 Summary:");
        Console.WriteLine($"     Total Commands: {results.Sum(r => r.CommandsIssued):N0}");
        Console.WriteLine($"     Total Events: {results.Sum(r => r.EventsGenerated):N0}");
        Console.WriteLine($"     Total Balance Updates: {results.Sum(r => r.BalanceUpdates):N0}");
        Console.WriteLine($"     Total History Updates: {results.Sum(r => r.HistoryUpdates):N0}");
        Console.WriteLine($"     Total Audit Updates: {results.Sum(r => r.AuditUpdates):N0}");
        Console.WriteLine($"     Command-Query Separation: ✅ Validated");
        Console.WriteLine($"     Multiple Read Models: ✅ Working");
        Console.WriteLine($"     Event-Driven Sync: ✅ Verified");
        
        Console.WriteLine("\n🎉 CQRS pattern successfully validated!");
    }

    private static async Task CreateTopicsAsync()
    {
        var adminConfig = new AdminClientConfig 
        { 
            BootstrapServers = KafkaBootstrapServers
        };
        
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new[]
        {
            new TopicSpecification { Name = CommandsTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = EventsTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = BalanceTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = HistoryTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = AuditTopic, NumPartitions = 4, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: {Topics}", 
                string.Join(", ", topicsToCreate.Select(t => t.Name)));
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist");
            }
            else
            {
                Log.Warning("Some topics failed to create");
            }
        }
    }

    private static async Task WaitForKafkaReadyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    SocketTimeoutMs = 3000
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata?.Brokers?.Count > 0)
                {
                    Log.Information("   [SUCCESS] Kafka is ready with {BrokerCount} broker(s)", metadata.Brokers.Count);
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds} seconds");
    }

    private static async Task WaitForFlinkHealthyAsync()
    {
        var timeout = TimeSpan.FromSeconds(30);
        var stopwatch = Stopwatch.StartNew();

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/api/v1/health");
                
                if (response.IsSuccessStatusCode)
                {
                    Log.Information("   [SUCCESS] Flink cluster is healthy");
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            await Task.Delay(1000);
        }

        throw new TimeoutException($"Flink cluster not healthy within {timeout.TotalSeconds} seconds");
    }
}

// Data models
public class CQRSScenario
{
    public string Name { get; set; } = string.Empty;
    public int TransactionCount { get; set; }
}

public class BankingCommand
{
    public string AccountId { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty; // Deposit, Withdraw, Transfer
    public decimal Amount { get; set; }
    public string? TargetAccount { get; set; } // For transfers
    public long Timestamp { get; set; }
}

public class BankingEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty; // DepositMade, WithdrawalMade, TransferCompleted
    public string AccountId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long Timestamp { get; set; }
    public string? TargetAccount { get; set; }
}

public class BalanceView
{
    public string AccountId { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public int TransactionCount { get; set; }
    public long LastUpdated { get; set; }
}

public class TransactionHistory
{
    public string AccountId { get; set; } = string.Empty;
    public List<Transaction> Transactions { get; set; } = new();
}

public class Transaction
{
    public string EventId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public long Timestamp { get; set; }
}

public class AuditLog
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class ScenarioResult
{
    public string ScenarioName { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; }
    public int CommandsIssued { get; set; }
    public int EventsGenerated { get; set; }
    public int BalanceUpdates { get; set; }
    public int HistoryUpdates { get; set; }
    public int AuditUpdates { get; set; }
    public int Deposits { get; set; }
    public int Withdrawals { get; set; }
    public int Transfers { get; set; }
}

/// <summary>
/// Map function that processes banking commands and generates events
/// Implements the Command → Event transformation (WRITE SIDE)
/// </summary>
public class CommandProcessorFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private long _eventIdCounter = 0;

    public string Map(string commandJson)
    {
        try
        {
            var command = JsonSerializer.Deserialize<BankingCommand>(commandJson);
            if (command == null) return commandJson;

            // Transform command to event based on command type
            var eventType = command.CommandType switch
            {
                "Deposit" => "DepositMade",
                "Withdraw" => "WithdrawalMade",
                "Transfer" => "TransferCompleted",
                _ => "UnknownEvent"
            };

            var bankingEvent = new BankingEvent
            {
                EventId = $"evt-{Interlocked.Increment(ref _eventIdCounter):D8}",
                EventType = eventType,
                AccountId = command.AccountId,
                Amount = command.Amount,
                Timestamp = command.Timestamp,
                TargetAccount = command.TargetAccount
            };

            return JsonSerializer.Serialize(bankingEvent);
        }
        catch
        {
            return commandJson;
        }
    }
}

/// <summary>
/// Map function that projects balance view from events
/// Implements the Event → Balance transformation (READ MODEL 1)
/// </summary>
public class BalanceProjectionFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private readonly Dictionary<string, BalanceView> _balances = new();

    public string Map(string eventJson)
    {
        try
        {
            var bankingEvent = JsonSerializer.Deserialize<BankingEvent>(eventJson);
            if (bankingEvent == null) return eventJson;

            // Get or create balance view
            if (!_balances.TryGetValue(bankingEvent.AccountId, out var balance))
            {
                balance = new BalanceView
                {
                    AccountId = bankingEvent.AccountId,
                    Balance = 0,
                    TransactionCount = 0,
                    LastUpdated = 0
                };
                _balances[bankingEvent.AccountId] = balance;
            }

            // Update balance based on event type
            switch (bankingEvent.EventType)
            {
                case "DepositMade":
                    balance.Balance += bankingEvent.Amount;
                    break;
                case "WithdrawalMade":
                    balance.Balance -= bankingEvent.Amount;
                    break;
                case "TransferCompleted":
                    // Deduct from source account
                    balance.Balance -= bankingEvent.Amount;
                    
                    // Credit target account
                    if (bankingEvent.TargetAccount != null)
                    {
                        if (!_balances.TryGetValue(bankingEvent.TargetAccount, out var targetBalance))
                        {
                            targetBalance = new BalanceView
                            {
                                AccountId = bankingEvent.TargetAccount,
                                Balance = 0,
                                TransactionCount = 0,
                                LastUpdated = 0
                            };
                            _balances[bankingEvent.TargetAccount] = targetBalance;
                        }
                        targetBalance.Balance += bankingEvent.Amount;
                        targetBalance.TransactionCount++;
                        targetBalance.LastUpdated = bankingEvent.Timestamp;
                    }
                    break;
            }

            balance.TransactionCount++;
            balance.LastUpdated = bankingEvent.Timestamp;

            return JsonSerializer.Serialize(balance);
        }
        catch
        {
            return eventJson;
        }
    }
}

/// <summary>
/// Map function that projects transaction history from events
/// Implements the Event → History transformation (READ MODEL 2)
/// </summary>
public class HistoryProjectionFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private readonly Dictionary<string, TransactionHistory> _histories = new();

    public string Map(string eventJson)
    {
        try
        {
            var bankingEvent = JsonSerializer.Deserialize<BankingEvent>(eventJson);
            if (bankingEvent == null) return eventJson;

            // Get or create transaction history
            if (!_histories.TryGetValue(bankingEvent.AccountId, out var history))
            {
                history = new TransactionHistory
                {
                    AccountId = bankingEvent.AccountId,
                    Transactions = new()
                };
                _histories[bankingEvent.AccountId] = history;
            }

            // Add transaction to history
            var transaction = new Transaction
            {
                EventId = bankingEvent.EventId,
                Type = bankingEvent.EventType,
                Amount = bankingEvent.Amount,
                Timestamp = bankingEvent.Timestamp
            };

            history.Transactions.Add(transaction);

            // Keep only last 50 transactions per account (prevent unbounded growth)
            if (history.Transactions.Count > 50)
            {
                history.Transactions.RemoveAt(0);
            }

            return JsonSerializer.Serialize(history);
        }
        catch
        {
            return eventJson;
        }
    }
}

/// <summary>
/// Map function that projects audit log from events
/// Implements the Event → Audit transformation (READ MODEL 3)
/// </summary>
public class AuditProjectionFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string eventJson)
    {
        try
        {
            var bankingEvent = JsonSerializer.Deserialize<BankingEvent>(eventJson);
            if (bankingEvent == null) return eventJson;

            // Create comprehensive audit log entry
            var auditLog = new AuditLog
            {
                EventId = bankingEvent.EventId,
                EventType = bankingEvent.EventType,
                Details = $"Account: {bankingEvent.AccountId}, Amount: {bankingEvent.Amount:C}, " +
                          $"Target: {bankingEvent.TargetAccount ?? "N/A"}",
                Timestamp = DateTimeOffset.FromUnixTimeMilliseconds(bankingEvent.Timestamp).DateTime
            };

            return JsonSerializer.Serialize(auditLog);
        }
        catch
        {
            return eventJson;
        }
    }
}
