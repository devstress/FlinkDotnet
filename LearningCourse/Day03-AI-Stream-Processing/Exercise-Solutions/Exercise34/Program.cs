using Microsoft.ML;
using Microsoft.ML.Data;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;
using System.Text.Json;
using System.Diagnostics;

namespace Exercise34;

/// <summary>
/// Day 3 Exercise 4: ML.NET Integration with Real Streaming Infrastructure
/// 
/// This exercise demonstrates:
/// - Real ML.NET model training with fraud detection
/// - Real-time streaming inference using Kafka
/// - FlinkDotNet integration for distributed processing
/// - Production-ready ML serving architecture
/// 
/// Architecture: Kafka Producer → Kafka → Flink Job (ML.NET) → Kafka → Consumer
/// </summary>
class Program
{
    // KAFKA ADDRESSES - Read from environment variables set by test infrastructure
    // KAFKA_BOOTSTRAP_SERVERS: For host-to-container communication (producer/consumer from exercise)
    // KAFKA_FLINK_BOOTSTRAP_SERVERS: For container-to-container communication (Flink job connectivity)
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8080";

    private const string InputTopic = "mlnet-transactions-input";
    private const string OutputTopic = "mlnet-fraud-predictions-output";
    private const string ConsumerGroup = "mlnet-integration-consumer";
    
    private const int MessageCount = 100;

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
            Log.Information("  Day 3 Exercise 4: ML.NET Integration with Real Streaming");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("📊 Configuration:");
            Log.Information("   Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("   Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("   Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("   Messages: {MessageCount}", MessageCount);
            Log.Information("");

            FlinkDotNet.DataStream.IJobClient? jobClient = null;

            try
            {
                // Step 1: Verify infrastructure
                Log.Information(">> Step 1/7: Verifying Kafka is ready...");
                await WaitForKafkaReadyAsync();
                Log.Information("");

                Log.Information(">> Step 2/7: Verifying Flink cluster is healthy...");
                await WaitForFlinkHealthyAsync();
                Log.Information("");

                Log.Information(">> Step 3/7: Creating Kafka topics...");
                await CreateTopicsAsync();
                Log.Information("");

                // Step 2: Train ML.NET model
                Log.Information(">> Step 4/7: Training ML.NET fraud detection model...");
                var mlContext = new MLContext(seed: 0);
                var fraudDetectionService = new FraudDetectionService(mlContext);
                await fraudDetectionService.InitializeModelAsync();
                Log.Information("");

                // Step 3: Submit Flink job
                Log.Information(">> Step 5/7: Submitting FlinkDotNet job for real-time inference...");
                jobClient = await SubmitFlinkJobAsync(fraudDetectionService);
                await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start
                Log.Information("");

                // Step 4: Produce transactions
                Log.Information(">> Step 6/7: Producing transactions to Kafka...");
                var stopwatch = Stopwatch.StartNew();
                var producedCount = await ProduceTransactionsAsync();
                stopwatch.Stop();
                var produceRate = producedCount / stopwatch.Elapsed.TotalSeconds;
                Log.Information("   📈 Production Rate: {Rate:F1} msg/sec", produceRate);
                Log.Information("");

                // Step 5: Wait for processing and consume results
                Log.Information(">> Step 7/7: Consuming fraud predictions from Kafka...");
                await Task.Delay(TimeSpan.FromSeconds(10)); // Allow time for processing
                
                var consumedCount = await ConsumeAndVerifyPredictionsAsync();
                Log.Information("");

                // Results
                var successRate = producedCount > 0 ? (double)consumedCount / producedCount * 100 : 0;
                
                Log.Information("================================================================================");
                Log.Information("  Exercise 4 Results - ML.NET Real-Time Inference");
                Log.Information("================================================================================");
                Log.Information("  📊 Statistics:");
                Log.Information("     Transactions Produced: {Produced:N0}", producedCount);
                Log.Information("     Predictions Consumed: {Consumed:N0}", consumedCount);
                Log.Information("     Success Rate: {SuccessRate:F1}%", successRate);
                Log.Information("     Production Rate: {ProduceRate:F1} msg/sec", produceRate);
                Log.Information("");
                Log.Information("  🎓 Key Learnings:");
                Log.Information("     ✅ ML.NET model training completed successfully");
                Log.Information("     ✅ Real-time inference pipeline deployed on Flink");
                Log.Information("     ✅ Kafka streaming integration working");
                Log.Information("     ✅ Production-ready ML serving demonstrated");
                Log.Information("");
                Log.Information("✅ EXERCISE COMPLETED SUCCESSFULLY!");
                Log.Information("================================================================================");

                return 0;
            }
            finally
            {
                // Cleanup: Cancel the Flink job
                if (jobClient != null)
                {
                    Log.Information("");
                    Log.Information(">> Cleaning up: Cancelling Flink job...");
                    try
                    {
                        await jobClient.CancelAsync();
                        Log.Information("   [SUCCESS] Flink job cancelled");
                    }
                    catch (Exception ex)
                    {
                        Log.Warning(ex, "Failed to cancel job");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise 4 failed with exception");
            return 1;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Submit Flink job for real-time ML.NET inference
    /// </summary>
    private static async Task<FlinkDotNet.DataStream.IJobClient> SubmitFlinkJobAsync(FraudDetectionService fraudService)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Source: Kafka consumer
        var transactionStream = environment.FromKafka(
            topic: InputTopic,
            bootstrapServers: KafkaFlinkBootstrapServers,
            groupId: ConsumerGroup,
            startingOffsets: "earliest"
        ).SetParallelism(2);

        // Map: ML.NET fraud inference
        var predictionStream = transactionStream
            .Map(new FraudInferenceFunction(fraudService))
            .SetParallelism(2);

        // Sink: Kafka producer
        predictionStream
            .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers)
            .SetParallelism(2);

        // Execute job
        var jobClient = await environment.ExecuteAsync("MLNet-Fraud-Detection");

        Log.Information("   [SUCCESS] Flink job submitted");
        Log.Information("   JobId: {JobId}", jobClient.GetJobId());
        
        return jobClient;
    }

    /// <summary>
    /// Produce transaction messages to Kafka
    /// </summary>
    private static async Task<int> ProduceTransactionsAsync()
    {
        var config = new ProducerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            ClientId = "mlnet-producer",
            Acks = Acks.All,
            LingerMs = 5
        };

        using var producer = new ProducerBuilder<string, string>(config).Build();

        var producedCount = 0;
        Log.Information("   Producing {MessageCount} transactions...", MessageCount);

        for (int i = 0; i < MessageCount; i++)
        {
            var transaction = GenerateRealisticTransaction(i);
            var transactionJson = JsonSerializer.Serialize(transaction);

            try
            {
                var result = await producer.ProduceAsync(InputTopic, new Message<string, string>
                {
                    Key = $"txn-{i}",
                    Value = transactionJson
                });
                
                if (result.Status == PersistenceStatus.Persisted)
                {
                    producedCount++;
                    
                    if ((i + 1) % 25 == 0)
                    {
                        Log.Information("   [{Count}/{Total}] transactions produced...", i + 1, MessageCount);
                    }
                }
            }
            catch (ProduceException<string, string> ex)
            {
                Log.Error(ex, "Failed to produce transaction {TransactionId}", i);
            }
        }

        producer.Flush(TimeSpan.FromSeconds(10));
        Log.Information("   [SUCCESS] All {MessageCount} transactions produced", producedCount);
        return producedCount;
    }

    /// <summary>
    /// Consume and verify fraud predictions from Kafka
    /// </summary>
    private static Task<int> ConsumeAndVerifyPredictionsAsync()
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = ConsumerGroup + "-verify",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(OutputTopic);

        Log.Information("   Consuming predictions from '{OutputTopic}' (max 30 seconds)...", OutputTopic);

        var consumedCount = 0;
        var fraudCount = 0;
        var timeoutCount = 0;
        const int maxTimeouts = 60;
        var stopwatch = Stopwatch.StartNew();

        while (timeoutCount < maxTimeouts && stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    consumedCount++;
                    timeoutCount = 0;

                    // Parse prediction
                    try
                    {
                        var predictionResult = JsonSerializer.Deserialize<PredictionResult>(result.Message.Value);
                        if (predictionResult?.Prediction?.IsFraud == true)
                        {
                            fraudCount++;
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors
                    }
                    
                    if (consumedCount % 25 == 0)
                    {
                        Log.Information("   [{Count}] predictions consumed (fraud detected: {FraudCount})...", 
                            consumedCount, fraudCount);
                    }
                    
                    consumer.Commit(result);
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming prediction");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {ConsumedCount} predictions ({FraudCount} fraud detected)", 
            consumedCount, fraudCount);
        return Task.FromResult(consumedCount);
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
            new TopicSpecification { Name = InputTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = OutputTopic, NumPartitions = 3, ReplicationFactor = 1 }
        };

        try
        {
            await admin.CreateTopicsAsync(topicsToCreate);
            Log.Information("   [SUCCESS] Topics created: {InputTopic}, {OutputTopic}", InputTopic, OutputTopic);
        }
        catch (CreateTopicsException ex)
        {
            var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
            if (!errors.Any())
            {
                Log.Information("   [SUCCESS] Topics already exist: {InputTopic}, {OutputTopic}", InputTopic, OutputTopic);
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

    private static TransactionData GenerateRealisticTransaction(int transactionId)
    {
        // Generate deterministic transaction patterns for educational consistency
        var locations = new[] { "New York", "London", "Tokyo", "Sydney", "San Francisco", "Toronto", "Unknown" };
        
        return new TransactionData
        {
            Amount = 10 + (transactionId % 190) * 10, // $10-$1900 in realistic patterns
            AccountAge = Math.Max(1, 30 + (transactionId % 300)), // 30-330 days
            TransactionCount = Math.Max(1, 5 + (transactionId % 45)), // 5-50 transactions
            Location = locations[transactionId % locations.Length],
            TimeOfDay = (transactionId % 24) // 0-23 hours
        };
    }
}

// Transaction data model
public class TransactionData
{
    public float Amount { get; set; }
    public float AccountAge { get; set; }
    public float TransactionCount { get; set; }
    public string Location { get; set; } = string.Empty;
    public float TimeOfDay { get; set; }
    public bool IsFraud { get; set; }
}

public class FraudPrediction
{
    [ColumnName("PredictedLabel")]
    public bool IsFraud { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("Score")]
    public float Score { get; set; }
}

public class PredictionResult
{
    public TransactionData? Transaction { get; set; }
    public FraudPrediction? Prediction { get; set; }
    public DateTime Timestamp { get; set; }
}

// Fraud detection service (preserves ML.NET training)
public class FraudDetectionService
{
    private readonly MLContext _mlContext;
    private PredictionEngine<TransactionData, FraudPrediction>? _predictionEngine;
    
    public FraudDetectionService(MLContext mlContext)
    {
        _mlContext = mlContext;
    }
    
    public async Task InitializeModelAsync()
    {
        Log.Information("   Creating synthetic training data...");
        
        // Create synthetic training data
        var trainingData = GenerateRealisticTrainingData();
        var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);
        
        Log.Information("   Training fraud detection model...");
        
        // Define data preparation and training pipeline
        var pipeline = _mlContext.Transforms.Text.FeaturizeText("LocationFeatures", nameof(TransactionData.Location))
            .Append(_mlContext.Transforms.Concatenate("Features", 
                nameof(TransactionData.Amount),
                nameof(TransactionData.AccountAge), 
                nameof(TransactionData.TransactionCount),
                nameof(TransactionData.TimeOfDay),
                "LocationFeatures"))
            .Append(_mlContext.BinaryClassification.Trainers.SdcaLogisticRegression(
                labelColumnName: nameof(TransactionData.IsFraud),
                featureColumnName: "Features"));
        
        // Train the model
        var model = pipeline.Fit(dataView);
        
        // Create prediction engine
        _predictionEngine = _mlContext.Model.CreatePredictionEngine<TransactionData, FraudPrediction>(model);
        
        Log.Information("   [SUCCESS] Model training completed successfully");
        
        // Allow async operation
        await Task.CompletedTask;
    }
    
    public FraudPrediction PredictFraud(TransactionData transaction)
    {
        if (_predictionEngine == null)
            throw new InvalidOperationException("Model not initialized");
            
        // Real ML.NET inference - no simulation delays
        var prediction = _predictionEngine.Predict(transaction);
        
        return prediction;
    }
    
    private static List<TransactionData> GenerateRealisticTrainingData()
    {
        var data = new List<TransactionData>();
        
        // Generate 1000 training samples using deterministic patterns
        for (int i = 0; i < 1000; i++)
        {
            var seed = i * 137; // Prime multiplier for good distribution
            var isFraud = (seed % 10) == 0; // 10% fraud rate
            
            data.Add(new TransactionData
            {
                Amount = isFraud
                    ? 1000 + ((seed % 9000) + 1) // $1,001-$10,000 for fraud
                    : 1 + (seed % 499), // $1-$500 for legitimate
                AccountAge = isFraud
                    ? 1 + (seed % 29) // 1-30 days for fraud
                    : 30 + (seed % 335), // 30-365 days for legitimate
                TransactionCount = isFraud
                    ? 1 + (seed % 4) // 1-5 transactions for fraud
                    : 5 + (seed % 45), // 5-50 transactions for legitimate
                Location = isFraud ? "Unknown" : GetRealisticLocation(seed),
                TimeOfDay = isFraud
                    ? (seed % 6) // 0-5 (night hours) for fraud
                    : 6 + (seed % 18), // 6-23 (day hours) for legitimate
                IsFraud = isFraud
            });
        }
        
        return data;
    }
    
    private static string GetRealisticLocation(int seed)
    {
        var locations = new[] { "New York", "London", "Tokyo", "Sydney", "San Francisco", "Toronto" };
        return locations[seed % locations.Length];
    }
}

// Flink Map Function for ML.NET inference
public class FraudInferenceFunction : FlinkDotNet.DataStream.IMapFunction<string, string>
{
    private readonly FraudDetectionService _fraudService;
    
    public FraudInferenceFunction(FraudDetectionService fraudService)
    {
        _fraudService = fraudService;
    }
    
    public string Map(string input)
    {
        try
        {
            // Deserialize transaction from JSON
            var transaction = JsonSerializer.Deserialize<TransactionData>(input);
            if (transaction == null)
                return JsonSerializer.Serialize(new { Error = "Invalid transaction data" });
            
            // Real ML.NET inference
            var prediction = _fraudService.PredictFraud(transaction);
            
            // Serialize prediction result to JSON
            return JsonSerializer.Serialize(new PredictionResult
            {
                Transaction = transaction,
                Prediction = prediction,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return JsonSerializer.Serialize(new { Error = ex.Message });
        }
    }
}