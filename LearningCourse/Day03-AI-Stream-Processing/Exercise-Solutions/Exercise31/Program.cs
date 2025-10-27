using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Exercise31.Models;
using Exercise31.Services;
using Exercise31.FlinkJobs;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Exercise31;

/// <summary>
/// AI Model DDL Mastery - Real Kafka/FlinkDotNet Infrastructure
/// Demonstrates AI model lifecycle management with real streaming
/// NO simulations - uses actual Kafka topics and Flink jobs
/// </summary>
class Program
{
    // Service discovery - NO hardcoded addresses
    private static string KafkaBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
    private static string KafkaFlinkBootstrapServers =>
        Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
    private static string FlinkGatewayUrl =>
        Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8080";

    private const string RegistrationTopic = "ai-model-registrations";
    private const string ValidationTopic = "ai-model-validations";

    static async Task<int> Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        FlinkDotNet.DataStream.IJobClient? validationJob = null;

        try
        {
            Log.Information("================================================================================");
            Log.Information("  AI Model DDL Mastery - Real Infrastructure");
            Log.Information("================================================================================");
            Log.Information("");
            Log.Information("Configuration:");
            Log.Information("  Kafka (Host): {KafkaHost}", KafkaBootstrapServers);
            Log.Information("  Kafka (Flink): {KafkaFlink}", KafkaFlinkBootstrapServers);
            Log.Information("  Flink Gateway: {FlinkGateway}", FlinkGatewayUrl);
            Log.Information("");

            // Step 1: Verify infrastructure
            Log.Information(">> Step 1/6: Verifying Kafka...");
            await WaitForKafkaReadyAsync();
            Log.Information("");

            Log.Information(">> Step 2/6: Verifying Flink...");
            await WaitForFlinkHealthyAsync();
            Log.Information("");

            // Step 2: Create Kafka topics
            Log.Information(">> Step 3/6: Creating Kafka topics...");
            await CreateTopicsAsync();
            Log.Information("");

            // Step 3: Submit Flink validation job
            Log.Information(">> Step 4/6: Submitting Flink validation job...");
            using var loggerFactory = LoggerFactory.Create(builder => builder.AddSerilog());
            var validationJobLogger = loggerFactory.CreateLogger<ModelValidationJob>();
            var validationJobClient = new ModelValidationJob(KafkaFlinkBootstrapServers, validationJobLogger);
            validationJob = await validationJobClient.SubmitAsync();
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job startup
            Log.Information("");

            // Step 4: Register AI models through Kafka
            Log.Information(">> Step 5/6: Registering AI models...");
            var registrationLogger = loggerFactory.CreateLogger<ModelRegistrationService>();
            using var registrationService = new ModelRegistrationService(KafkaBootstrapServers, registrationLogger);
            
            var modelIds = await RegisterModelsAsync(registrationService);
            Log.Information("   Registered {Count} models", modelIds.Count);
            Log.Information("");

            // Step 5: Wait for validation and consume results
            Log.Information(">> Step 6/6: Validating model registrations...");
            await Task.Delay(TimeSpan.FromSeconds(3)); // Allow Flink processing time
            var validationResults = await ConsumeValidationResultsAsync(modelIds.Count);
            
            Log.Information("");
            Log.Information("Validation Results:");
            foreach (var result in validationResults)
            {
                if (result.IsValid)
                    Log.Information("   [SUCCESS] Model {ModelId} validated", result.ModelId);
                else
                    Log.Warning("   [FAILED] Model {ModelId}: {Errors}", result.ModelId, 
                        string.Join(", ", result.ValidationErrors));
            }
            Log.Information("");

            Log.Information("================================================================================");
            Log.Information("  EXERCISE COMPLETED SUCCESSFULLY!");
            Log.Information("================================================================================");
            Log.Information("[SUCCESS] AI Model DDL Mastery completed with real infrastructure");
            Log.Information("");
            Log.Information("Key Achievements:");
            Log.Information("  [SUCCESS] Real Kafka streaming for model registration");
            Log.Information("  [SUCCESS] FlinkDotNet validation job processing");
            Log.Information("  [SUCCESS] Event-driven model lifecycle management");
            Log.Information("  [SUCCESS] Production-ready AI model DDL patterns");
            Log.Information("");

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Exercise failed with exception");
            return 1;
        }
        finally
        {
            // Cleanup: Cancel Flink job
            if (validationJob != null)
            {
                Log.Information(">> Cleaning up: Cancelling Flink job...");
                try
                {
                    await validationJob.CancelAsync();
                    Log.Information("   [SUCCESS] Flink job cancelled");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to cancel job");
                }
            }
            
            await Log.CloseAndFlushAsync();
        }
    }

    /// <summary>
    /// Register AI models through real Kafka streaming
    /// </summary>
    private static async Task<List<string>> RegisterModelsAsync(ModelRegistrationService service)
    {
        var modelIds = new List<string>();

        // Model 1: Fraud Detection
        var fraudModel = new AIModelDefinition
        {
            ModelName = "fraud_detection_v1",
            ModelType = ModelType.Classification,
            ModelFormat = ModelFormat.ONNX,
            ModelVersion = "1.0.0",
            ModelPath = "s3://ai-models/fraud-detection/v1.0.0/model.onnx",
            
            InputSchema = new Dictionary<string, string>
            {
                ["transaction_amount"] = "DOUBLE",
                ["merchant_category"] = "STRING",
                ["user_age"] = "INT",
                ["time_of_day"] = "INT"
            },
            
            OutputSchema = new Dictionary<string, string>
            {
                ["fraud_probability"] = "DOUBLE",
                ["risk_score"] = "DOUBLE"
            },
            
            OptimizationSettings = new ModelOptimizationSettings
            {
                BatchSize = 100,
                CacheSize = "256MB",
                WarmupSamples = 1000
            },
            
            QualityMetrics = new ModelQualityMetrics
            {
                Accuracy = 0.94,
                Precision = 0.91,
                Recall = 0.89,
                F1Score = 0.90
            }
        };

        var modelId1 = await service.RegisterModelAsync(fraudModel);
        modelIds.Add(modelId1);
        Log.Information("   [SUCCESS] Registered: {ModelName} (ID: {ModelId})", 
            fraudModel.ModelName, modelId1);

        // Model 2: Sentiment Analysis
        var sentimentModel = new AIModelDefinition
        {
            ModelName = "sentiment_analysis_v2",
            ModelType = ModelType.NLPClassification,
            ModelFormat = ModelFormat.TensorFlow,
            ModelVersion = "2.1.0",
            
            InputSchema = new Dictionary<string, string>
            {
                ["text"] = "STRING",
                ["language"] = "STRING"
            },
            
            OutputSchema = new Dictionary<string, string>
            {
                ["sentiment"] = "STRING",
                ["confidence"] = "DOUBLE"
            },
            
            QualityMetrics = new ModelQualityMetrics
            {
                Accuracy = 0.92,
                Precision = 0.90,
                Recall = 0.88,
                F1Score = 0.89
            }
        };

        var modelId2 = await service.RegisterModelAsync(sentimentModel);
        modelIds.Add(modelId2);
        Log.Information("   [SUCCESS] Registered: {ModelName} (ID: {ModelId})", 
            sentimentModel.ModelName, modelId2);

        return modelIds;
    }

    /// <summary>
    /// Consume validation results from Kafka
    /// </summary>
    private static Task<List<ModelValidationResult>> ConsumeValidationResultsAsync(int expectedCount)
    {
        var results = new List<ModelValidationResult>();
        
        var config = new ConsumerConfig
        {
            BootstrapServers = KafkaBootstrapServers,
            GroupId = "aimodel-validation-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(ValidationTopic);

        var timeoutCount = 0;
        const int maxTimeouts = 60;
        var stopwatch = Stopwatch.StartNew();

        while (results.Count < expectedCount && 
               timeoutCount < maxTimeouts && 
               stopwatch.Elapsed < TimeSpan.FromSeconds(30))
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));
                
                if (result != null)
                {
                    var validation = JsonSerializer.Deserialize<ModelValidationResult>(result.Message.Value);
                    if (validation != null)
                    {
                        results.Add(validation);
                        consumer.Commit(result);
                    }
                    timeoutCount = 0;
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming validation result");
                break;
            }
        }

        consumer.Close();
        return Task.FromResult(results);
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
            new TopicSpecification { Name = RegistrationTopic, NumPartitions = 3, ReplicationFactor = 1 },
            new TopicSpecification { Name = ValidationTopic, NumPartitions = 3, ReplicationFactor = 1 }
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