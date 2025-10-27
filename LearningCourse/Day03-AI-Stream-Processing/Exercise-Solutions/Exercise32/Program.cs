using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using FlinkDotNet.DataStream;

namespace Exercise32;

/// <summary>
/// Day 3 Exercise: Fraud Detection System
/// Real-time fraud detection using FlinkDotNet with real Kafka infrastructure
/// Implements patterns from financial services companies (Uber-scale fraud detection)
///
/// Reference: Apache Flink 2.1.0 AI Features
/// https://flink.apache.org/2025/07/31/apache-flink-2.1.0-ushers-in-a-new-era-of-unified-real-time-data--ai-with-comprehensive-upgrades/
/// </summary>
public class Program
{
    private const string InputTopic = "fraud_transactions";
    private const string OutputTopic = "fraud_alerts";
    private const string ConsumerGroup = "fraud-detection-group";
    
    // Kafka addresses - read from environment variables set by test infrastructure
    // KAFKA_BOOTSTRAP_SERVERS: For host-to-container communication (producer/consumer operations)
    // KAFKA_FLINK_BOOTSTRAP_SERVERS: For container-to-container communication (Flink job Kafka connectivity)
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
    
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";
        
    private static string FlinkJobManagerUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOBMANAGER_URL") ?? "http://localhost:8081";

    public static async Task Main(string[] args)
    {
        // Set console encoding to UTF-8
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateLogger();

        Console.WriteLine("================================================================================");
        Console.WriteLine("  Day 3 Exercise: Fraud Detection System");
        Console.WriteLine("================================================================================");
        Console.WriteLine();
        Console.WriteLine("  Real-time fraud detection using FlinkDotNet");
        Console.WriteLine("  Processing financial transactions with ML-based risk assessment");
        Console.WriteLine();
        Console.WriteLine("  This exercise demonstrates:");
        Console.WriteLine("  - Real-time transaction stream processing");
        Console.WriteLine("  - Fraud detection using rule-based risk scoring");
        Console.WriteLine("  - Kafka producer for transaction generation");
        Console.WriteLine("  - Flink job for real-time fraud analysis");
        Console.WriteLine("  - Kafka consumer for fraud alerts");
        Console.WriteLine();
        Console.WriteLine("================================================================================");
        Console.WriteLine();

        try
        {
            await RunFraudDetectionDemo();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing fraud detection demo");
            Console.WriteLine($"ERROR: {ex.Message}");
            Environment.Exit(1);
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Main demo following fraud detection pattern
    /// Steps: Validate infrastructure → Submit Flink job → Generate transactions → Consume fraud alerts
    /// </summary>
    static async Task RunFraudDetectionDemo()
    {
        FlinkDotNet.DataStream.IJobClient? jobClient = null;
        
        try
        {
            Console.WriteLine(">> Step 1/6: Verifying Kafka is ready...");
            await WaitForKafkaReadyAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 2/6: Verifying Flink cluster is ready...");
            await WaitForFlinkHealthyAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 3/6: Creating Kafka topics...");
            await CreateTopicsAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 4/6: Submitting Flink fraud detection job...");
            jobClient = await SubmitFraudDetectionJob();
            await Task.Delay(3000); // Wait for job to start
            Console.WriteLine();

            Console.WriteLine(">> Step 5/6: Generating transaction stream...");
            await GenerateTransactions();
            await Task.Delay(2000); // Wait for Flink to process
            Console.WriteLine();

            Console.WriteLine(">> Step 6/6: Consuming fraud alerts from output topic...");
            await ConsumeFraudAlerts();
            Console.WriteLine();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("What you learned:");
            Console.WriteLine("  [SUCCESS] Real-time transaction stream processing");
            Console.WriteLine("  [SUCCESS] Fraud detection with risk scoring");
            Console.WriteLine("  [SUCCESS] FlinkDotNet job submission and monitoring");
            Console.WriteLine("  [SUCCESS] End-to-end fraud detection pipeline");
            Console.WriteLine();
        }
        finally
        {
            // Clean up: Cancel the Flink job
            if (jobClient != null)
            {
                Console.WriteLine(">> Cleaning up: Cancelling Flink job...");
                try
                {
                    await jobClient.CancelAsync();
                    Console.WriteLine("   [SUCCESS] Flink job cancelled successfully");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"   [WARNING] Failed to cancel job: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Submit Flink fraud detection job
    /// Processes transaction stream and identifies fraudulent transactions
    /// </summary>
    static async Task<FlinkDotNet.DataStream.IJobClient> SubmitFraudDetectionJob()
    {
        Console.WriteLine($"   Creating Flink fraud detection job...");
        Console.WriteLine($"   - Input Topic: {InputTopic}");
        Console.WriteLine($"   - Transformation: Fraud Risk Scoring");
        Console.WriteLine($"   - Output Topic: {OutputTopic}");

        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Create Kafka source for transactions
        var transactionStream = environment.FromKafka(
            topic: InputTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        );

        // Apply fraud detection transformation
        transactionStream
            .Map(new FraudDetectionFunction())
            .Filter(new HighRiskFilter())
            .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);

        // Execute the job
        var jobClient = await environment.ExecuteAsync("fraud-detection-pipeline");

        Console.WriteLine($"   [SUCCESS] Flink fraud detection job submitted");
        Console.WriteLine($"   JobId: {jobClient.GetJobId()}");
        
        return jobClient;
    }

    /// <summary>
    /// Generate realistic transaction patterns for fraud detection testing
    /// </summary>
    static async Task GenerateTransactions()
    {
        var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

        const int messageCount = 20;
        Console.WriteLine($"   Generating {messageCount} transaction messages...");

        var transactions = GenerateRealisticTransactions();
        
        for (int i = 0; i < transactions.Length; i++)
        {
            var transaction = transactions[i];
            var transactionJson = System.Text.Json.JsonSerializer.Serialize(transaction);
            
            var message = new Message<string, string>
            {
                Key = $"txn-{transaction.Id}",
                Value = transactionJson
            };

            try
            {
                var deliveryReport = await producer.ProduceAsync(InputTopic, message);
                
                if (i % 5 == 0 || i == messageCount - 1)
                {
                    var riskLabel = CalculateRiskScore(transaction) >= 0.75 ? "HIGH RISK" : "Normal";
                    Console.WriteLine($"   [{i + 1:D2}/{messageCount}] Transaction ${transaction.Amount} - {riskLabel} -> Partition {deliveryReport.Partition}");
                }
            }
            catch (ProduceException<string, string> ex)
            {
                Console.WriteLine($"   [ERROR] Failed to produce transaction {i}: {ex.Error.Reason}");
            }

            await Task.Delay(50); // Small delay for observability
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Console.WriteLine($"   [SUCCESS] All {messageCount} transactions generated");
    }

    /// <summary>
    /// Generate realistic transaction patterns based on actual financial data
    /// </summary>
    private static TransactionData[] GenerateRealisticTransactions()
    {
        var transactions = new List<TransactionData>();
        
        // Pattern 1: Normal small purchases (70% of transactions)
        for (int i = 1; i <= 14; i++)
        {
            transactions.Add(new TransactionData
            {
                Id = i,
                Amount = 15m + (i * 7) % 150,
                Location = GetLocationByPattern(i),
                AccountAge = 30 + (i * 23) % 300,
                TransactionCount = 10 + (i * 5) % 40,
                TimeOfDay = 8 + (i * 2) % 14
            });
        }
        
        // Pattern 2: Medium purchases (20% of transactions)
        for (int i = 15; i <= 18; i++)
        {
            transactions.Add(new TransactionData
            {
                Id = i,
                Amount = 200m + (i * 50) % 800,
                Location = GetLocationByPattern(i),
                AccountAge = 90 + (i * 30) % 200,
                TransactionCount = 15 + (i * 3) % 25,
                TimeOfDay = 10 + (i * 3) % 12
            });
        }
        
        // Pattern 3: Suspicious/high-risk transactions (10% of transactions)
        for (int i = 19; i <= 20; i++)
        {
            transactions.Add(new TransactionData
            {
                Id = i,
                Amount = 2500m + (i * 1000) % 7500,
                Location = i % 2 == 0 ? "Unknown" : "High-Risk-Region",
                AccountAge = 1 + (i * 5) % 20,
                TransactionCount = 1 + i % 3,
                TimeOfDay = (i * 2) % 6
            });
        }
        
        return transactions.ToArray();
    }

    /// <summary>
    /// Calculate fraud risk score based on real industry patterns
    /// </summary>
    private static double CalculateRiskScore(TransactionData transaction)
    {
        double riskScore = 0.0;
        
        // Amount risk
        if (transaction.Amount > 5000) riskScore += 0.4;
        else if (transaction.Amount > 1000) riskScore += 0.2;
        else if (transaction.Amount < 5) riskScore += 0.1;
        
        // Location risk
        if (transaction.Location == "Unknown" || transaction.Location == "High-Risk-Region")
            riskScore += 0.3;
        
        // Account age risk
        if (transaction.AccountAge < 7) riskScore += 0.3;
        else if (transaction.AccountAge < 30) riskScore += 0.1;
        
        // Transaction history risk
        if (transaction.TransactionCount < 3) riskScore += 0.2;
        else if (transaction.TransactionCount < 10) riskScore += 0.1;
        
        // Time of day risk
        if (transaction.TimeOfDay >= 0 && transaction.TimeOfDay <= 6) riskScore += 0.15;
        
        return Math.Min(1.0, riskScore);
    }

    /// <summary>
    /// Get location based on realistic global transaction patterns
    /// </summary>
    private static string GetLocationByPattern(int pattern)
    {
        var locations = new[]
        {
            "New York", "London", "Tokyo", "Sydney", "Toronto",
            "San Francisco", "Singapore", "Frankfurt", "Chicago", "Los Angeles"
        };
        return locations[pattern % locations.Length];
    }

    /// <summary>
    /// Consume fraud alerts from output topic
    /// </summary>
    static async Task ConsumeFraudAlerts()
    {
        var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, $"fraud-alert-consumer-{Guid.NewGuid()}");
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(OutputTopic);

        Console.WriteLine($"   Consuming fraud alerts from '{OutputTopic}' (max 30 seconds)...");

        var consumedCount = 0;
        var highRiskCount = 0;
        var stopwatch = Stopwatch.StartNew();
        var timeout = TimeSpan.FromSeconds(60);

        try
        {
            while (stopwatch.Elapsed < timeout && consumedCount < 20)
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                if (result != null)
                {
                    consumedCount++;
                    
                    // Parse the fraud alert
                    var alertData = System.Text.Json.JsonSerializer.Deserialize<FraudAlert>(result.Message.Value);
                    
                    if (alertData != null && alertData.RiskScore >= 0.75)
                    {
                        highRiskCount++;
                        Console.WriteLine($"   [ALERT {consumedCount:D2}] Transaction {alertData.TransactionId} - Risk: {alertData.RiskScore:F3} - Amount: ${alertData.Amount}");
                    }
                    
                    consumer.Commit(result);
                }
                else if (consumedCount > 0)
                {
                    Console.WriteLine("   No new alerts - consumption complete");
                    break;
                }
            }
        }
        catch (ConsumeException ex)
        {
            Console.WriteLine($"   [ERROR] Consumption error: {ex.Error.Reason}");
            throw;
        }
        finally
        {
            consumer.Close();
        }

        Console.WriteLine($"   [SUCCESS] Consumed {consumedCount} fraud alerts ({highRiskCount} high-risk)");
    }

    static ProducerConfig CreateProducerConfig(string kafkaAddress)
    {
        return new ProducerConfig
        {
            BootstrapServers = kafkaAddress,
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
    }

    static ConsumerConfig CreateConsumerConfig(string kafkaAddress, string kafkaGroup)
    {
        return new ConsumerConfig
        {
            BootstrapServers = kafkaAddress,
            GroupId = kafkaGroup,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
    }

    static async Task CreateTopicsAsync()
    {
        var adminConfig = new AdminClientConfig 
        { 
            BootstrapServers = KafkaBootstrapServers,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        };
        
        using var admin = new AdminClientBuilder(adminConfig).Build();

        var topicsToCreate = new[]
        {
            new TopicSpecification { Name = InputTopic, NumPartitions = 4, ReplicationFactor = 1 },
            new TopicSpecification { Name = OutputTopic, NumPartitions = 4, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Console.WriteLine($"   [SUCCESS] Topics created: {InputTopic}, {OutputTopic}");
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Console.WriteLine($"   [SUCCESS] Topics already exist: {InputTopic}, {OutputTopic}");
            }
            else
            {
                Console.WriteLine($"   [WARNING] Some topics failed to create");
            }
        }
    }

    static async Task WaitForKafkaReadyAsync()
    {
        var timeout = TimeSpan.FromSeconds(60);
        var stopwatch = Stopwatch.StartNew();
        var retryDelay = 1000;

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    SocketTimeoutMs = 3000,
                    BrokerAddressFamily = BrokerAddressFamily.V4,
                    SecurityProtocol = SecurityProtocol.Plaintext
                };

                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));

                if (metadata?.Brokers?.Count > 0)
                {
                    Console.WriteLine($"   [SUCCESS] Kafka is ready with {metadata.Brokers.Count} broker(s)");
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            Console.WriteLine($"   [RETRY] Kafka not ready yet, retrying in {retryDelay/1000.0:F1}s... (elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)");
            await Task.Delay(retryDelay);
            retryDelay = Math.Min(retryDelay + 1000, 5000);
        }

        throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds} seconds");
    }

    static async Task WaitForFlinkHealthyAsync()
    {
        var timeout = TimeSpan.FromSeconds(60);
        var stopwatch = Stopwatch.StartNew();
        var retryDelay = 1000;

        while (stopwatch.Elapsed < timeout)
        {
            try
            {
                using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                var response = await httpClient.GetAsync($"{FlinkJobManagerUrl}/v1/overview");
                
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"   [SUCCESS] Flink cluster is healthy and ready");
                    return;
                }
            }
            catch
            {
                // Continue waiting
            }

            Console.WriteLine($"   [RETRY] Flink not ready yet, retrying in {retryDelay/1000.0:F1}s... (elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)");
            await Task.Delay(retryDelay);
            retryDelay = Math.Min(retryDelay + 1000, 5000);
        }

        throw new TimeoutException($"Flink cluster not healthy within {timeout.TotalSeconds} seconds");
    }
}

/// <summary>
/// Transaction data model for fraud detection
/// </summary>
public class TransactionData
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Location { get; set; } = string.Empty;
    public int AccountAge { get; set; }
    public int TransactionCount { get; set; }
    public int TimeOfDay { get; set; }
}

/// <summary>
/// Fraud alert output model
/// </summary>
public class FraudAlert
{
    public int TransactionId { get; set; }
    public decimal Amount { get; set; }
    public double RiskScore { get; set; }
    public string RiskCategory { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Fraud detection map function
/// Calculates risk scores for transactions
/// </summary>
public class FraudDetectionFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    public string Map(string transactionJson)
    {
        var transaction = System.Text.Json.JsonSerializer.Deserialize<TransactionData>(transactionJson);
        
        if (transaction == null)
            return transactionJson;

        double riskScore = CalculateRiskScore(transaction);
        string riskCategory = DetermineRiskCategory(riskScore);

        var fraudAlert = new FraudAlert
        {
            TransactionId = transaction.Id,
            Amount = transaction.Amount,
            RiskScore = riskScore,
            RiskCategory = riskCategory,
            Location = transaction.Location
        };

        return System.Text.Json.JsonSerializer.Serialize(fraudAlert);
    }

    private double CalculateRiskScore(TransactionData transaction)
    {
        double riskScore = 0.0;
        
        if (transaction.Amount > 5000) riskScore += 0.4;
        else if (transaction.Amount > 1000) riskScore += 0.2;
        
        if (transaction.Location == "Unknown" || transaction.Location == "High-Risk-Region")
            riskScore += 0.3;
        
        if (transaction.AccountAge < 7) riskScore += 0.3;
        else if (transaction.AccountAge < 30) riskScore += 0.1;
        
        if (transaction.TransactionCount < 3) riskScore += 0.2;
        
        if (transaction.TimeOfDay >= 0 && transaction.TimeOfDay <= 6) riskScore += 0.15;
        
        return Math.Min(1.0, riskScore);
    }

    private string DetermineRiskCategory(double riskScore)
    {
        if (riskScore >= 0.75) return "HIGH_RISK";
        if (riskScore >= 0.5) return "MEDIUM_RISK";
        if (riskScore >= 0.25) return "LOW_RISK";
        return "NORMAL";
    }
}

/// <summary>
/// Filter to only output high-risk transactions
/// </summary>
public class HighRiskFilter : FlinkDotNet.DataStream.IFilterFunction<string>
{
    public bool Filter(string alertJson)
    {
        var alert = System.Text.Json.JsonSerializer.Deserialize<FraudAlert>(alertJson);
        return alert != null && alert.RiskScore >= 0.75;
    }
}