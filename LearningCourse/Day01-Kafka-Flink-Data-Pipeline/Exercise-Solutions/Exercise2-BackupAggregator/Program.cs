using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;

namespace Exercise2_BackupAggregator
{
    /// <summary>
    /// Exercise 2: Custom Objects and Backup Aggregation (Sections 7-11 from Baeldung Tutorial)
    ///
    /// Reference: https://www.baeldung.com/kafka-flink-data-pipeline (Sections 7-11)
    ///
    /// This exercise demonstrates:
    /// - Custom object deserialization (Section 7)
    /// - Custom object serialization (Section 8)
    /// - Timestamping messages with EventTime (Section 9)
    /// - Creating time windows (Section 10)
    /// - Aggregating backups in time windows (Section 11)
    ///
    /// NOTE: FlinkDotNet currently uses JobDefinition API for advanced features like
    /// time windows and aggregations. Future versions will add these to the DataStream API.
    /// </summary>
    static class Program
    {
        private const string InputTopic = "flink_input";
        private const string OutputTopic = "flink_output";
        private const string ConsumerGroup = "baeldung";
        
        // Kafka configuration for HOST operations (producer/consumer)
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? throw new InvalidOperationException("KAFKA_BOOTSTRAP_SERVERS environment variable must be set");
        
        // Kafka configuration for FLINK JOB submissions (JSON API)
        // Docker bridge network requires container IP, not DNS names
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS")
            ?? throw new InvalidOperationException("KAFKA_FLINK_BOOTSTRAP_SERVERS environment variable must be set");
        // Flink Gateway configuration
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string FlinkGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";
        private const string JobSubmitEndpoint = "/api/v1/jobs/submit";

        static async Task Main(string[] args)
        {
            // Set console encoding to UTF-8
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  Exercise 2: Custom Objects and Backup Aggregation");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("  Reference: Sections 7-11 of Baeldung Tutorial");
            Console.WriteLine("  https://www.baeldung.com/kafka-flink-data-pipeline");
            Console.WriteLine();
            Console.WriteLine("  This exercise demonstrates:");
            Console.WriteLine("  - Section 7: Custom object deserialization (InputMessage)");
            Console.WriteLine("  - Section 8: Custom object serialization (Backup)");
            Console.WriteLine("  - Section 9: Timestamping messages (EventTime)");
            Console.WriteLine("  - Section 10: Creating time windows (tumbling windows)");
            Console.WriteLine("  - Section 11: Aggregating backups (daily aggregation)");
            Console.WriteLine();
            Console.WriteLine("  NOTE: This exercise uses JobDefinition API for advanced features.");
            Console.WriteLine("  Future FlinkDotNet versions will add TimeWindow/Aggregate to DataStream API.");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                await RunBackupAggregationDemo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing backup aggregation demo");
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Main demo following Baeldung Section 11: CreateBackup() method
        /// Steps: Submit aggregation job → Produce timestamped messages → Consume backup results
        /// </summary>
        static async Task RunBackupAggregationDemo()
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

            Console.WriteLine(">> Step 4/6: Submitting Flink backup aggregation job...");
            await SubmitBackupAggregationJob();
            await Task.Delay(3000); // Wait for job to start
            Console.WriteLine();

            Console.WriteLine(">> Step 5/6: Producing timestamped InputMessage objects...");
            await ProduceInputMessages();
            await Task.Delay(2000); // Wait for Flink to aggregate
            Console.WriteLine();

            Console.WriteLine(">> Step 6/6: Consuming Backup aggregation results...");
            await ConsumeBackupResults();
            Console.WriteLine();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  EXERCISE 2 COMPLETED!");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("What you learned (Baeldung Sections 7-11):");
            Console.WriteLine("  [OK] Custom object deserialization (InputMessageDeserializer)");
            Console.WriteLine("  [OK] Custom object serialization (BackupSerializer)");
            Console.WriteLine("  [OK] EventTime for message timestamps");
            Console.WriteLine("  [OK] Time windows (tumbling 10-second window for testing)");
            Console.WriteLine("  [OK] Aggregation functions (collect messages into Backup)");
            Console.WriteLine();
            Console.WriteLine("  NOTE: Baeldung tutorial uses 24-hour window for production use.");
            Console.WriteLine();
        }

        /// <summary>
        /// Submit Flink job with time-windowed aggregation (Baeldung Sections 9-11)
        /// 
        /// Baeldung Java API:
        ///   environment.setStreamTimeCharacteristic(TimeCharacteristic.EventTime);
        ///   flinkKafkaConsumer.assignTimestampsAndWatermarks(new InputMessageTimestampAssigner());
        ///   DataStream<InputMessage> inputMessagesStream = environment.addSource(flinkKafkaConsumer);
        ///   inputMessagesStream
        ///     .timeWindowAll(Time.hours(24))
        ///     .aggregate(new BackupAggregator())
        ///     .addSink(flinkKafkaProducer);
        ///   environment.execute();
        /// 
        /// FlinkDotNet equivalent using JobDefinition API:
        ///   (Advanced features like timeWindowAll and aggregate require JobDefinition API)
        /// </summary>
        static async Task SubmitBackupAggregationJob()
        {
            PrintJobConfiguration();
            
            try
            {
                var flinkJobDefinition = CreateFlinkJobDefinition();
                var jobJson = JsonSerializer.Serialize(flinkJobDefinition, new JsonSerializerOptions { WriteIndented = true });
                
                await PostJobToFlinkGateway(jobJson);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [WARNING] Error submitting job: {ex.Message}");
                Console.WriteLine($"   Note: Job definition is correct - infrastructure may not be running");
            }
        }

        /// <summary>
        /// Print Flink job configuration details
        /// </summary>
        static void PrintJobConfiguration()
        {
            Console.WriteLine($"   Creating Flink job using FlinkDotNet JobDefinition API...");
            Console.WriteLine($"   - Input Topic: {InputTopic}");
            Console.WriteLine($"   - Time Characteristic: EventTime");
            Console.WriteLine($"   - Window: Tumbling 10-second window (for testing)");
            Console.WriteLine($"   - Aggregation: Collect InputMessages into Backup");
            Console.WriteLine($"   - Output Topic: {OutputTopic}");
            Console.WriteLine();
            Console.WriteLine($"   NOTE: Baeldung tutorial uses 24-hour window for production:");
            Console.WriteLine($"   inputMessagesStream");
            Console.WriteLine($"     .timeWindowAll(Time.hours(24))");
            Console.WriteLine($"     .aggregate(new BackupAggregator())");
            Console.WriteLine($"     .addSink(flinkKafkaProducer);");
            Console.WriteLine();
            Console.WriteLine($"   This exercise uses 10-second window for faster testing.");
        }

        /// <summary>
        /// Create Flink job definition object
        /// </summary>
        static object CreateFlinkJobDefinition()
        {
            return new
            {
                source = new
                {
                    type = "kafka",
                    topic = InputTopic,
                    bootstrapServers = KafkaFlinkBootstrapServers,  // Use container IP discovered by test infrastructure
                    groupId = ConsumerGroup,
                    startingOffsets = "earliest"
                },
                operations = new object[]
                {
                    new
                    {
                        type = "window",
                        windowType = "TUMBLING",  // Section 10: Time windows (tumbling)
                        size = 10,                // 10 seconds for testing (Baeldung uses 24 hours)
                        timeUnit = "SECONDS",
                        timeField = "sentAt"      // Section 9: EventTime from sentAt field
                    },
                    new
                    {
                        type = "aggregate",          // Section 11: Aggregating backups
                        aggregationType = "COLLECT", // Collect messages into Backup
                        field = "*",                 // Aggregate all fields
                        windowSeconds = 10           // 10 seconds for testing (Baeldung uses 24 hours)
                    }
                },
                sink = new
                {
                    type = "kafka",
                    topic = OutputTopic,
                    bootstrapServers = KafkaFlinkBootstrapServers  // Use container IP discovered by test infrastructure
                },
                metadata = new
                {
                    jobId = Guid.NewGuid().ToString(),
                    jobName = "backup-aggregator",
                    createdAt = DateTime.UtcNow,
                    version = "1.0",
                    properties = new Dictionary<string, string>
                    {
                        { "timeCharacteristic", "EventTime" }  // Section 9: Use event time
                    }
                }
            };
        }

        /// <summary>
        /// Post job definition to Flink Gateway
        /// </summary>
        static async Task PostJobToFlinkGateway(string jobJson)
        {
            using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(10) };
            var content = new System.Net.Http.StringContent(jobJson, System.Text.Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{FlinkGatewayUrl}{JobSubmitEndpoint}", content);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   [SUCCESS] Backup aggregation job submitted successfully");
                Console.WriteLine($"   Response: {responseContent}");
            }
            else
            {
                HandleJobSubmissionFailure(response).Wait();
            }
        }

        /// <summary>
        /// Handle job submission failure with detailed error reporting
        /// </summary>
        static async Task HandleJobSubmissionFailure(System.Net.Http.HttpResponseMessage response)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"   [ERROR] Job submission failed with status: {response.StatusCode}");
            Console.WriteLine($"   Response: {responseContent}");
            
            PrintValidationErrors(responseContent);
            
            throw new InvalidOperationException($"Flink job submission failed: {response.StatusCode} - {responseContent}");
        }

        /// <summary>
        /// Parse and print validation errors from response
        /// </summary>
        static void PrintValidationErrors(string responseContent)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                if (errorResponse != null)
                {
                    Console.WriteLine();
                    Console.WriteLine("   [ERROR] Validation errors detected:");
                    foreach (var kvp in errorResponse)
                    {
                        Console.WriteLine($"      - {kvp.Key}: {kvp.Value}");
                    }
                    Console.WriteLine();
                }
            }
            catch
            {
                // Response is not JSON or cannot be parsed - already printed above
            }
        }

        /// <summary>
        /// Produce timestamped InputMessage objects (Baeldung Section 7 & 9)
        /// Each message has sender, recipient, sentAt timestamp, and message content
        /// </summary>
        static async Task ProduceInputMessages()
        {
            var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            const int messageCount = 50;
            Console.WriteLine($"   Producing {messageCount} InputMessage objects with timestamps...");

            for (int i = 0; i < messageCount; i++)
            {
                // Section 7: InputMessage custom object
                var inputMessage = new InputMessage
                {
                    Sender = $"sender-{i}",
                    Recipient = $"recipient-{i}",
                    SentAt = DateTime.UtcNow,  // Section 9: EventTime timestamp
                    Message = $"Test message {i}"
                };

                // Section 7: Custom serialization
                var json = InputMessageSerializer.Serialize(inputMessage);

                var kafkaMessage = new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = json
                };

                try
                {
                    await producer.ProduceAsync(InputTopic, kafkaMessage);
                    
                    if (i % 10 == 0 || i == messageCount - 1)
                    {
                        Console.WriteLine($"   [{i + 1:D3}/{messageCount}] Sent: From={inputMessage.Sender} To={inputMessage.Recipient} Time={inputMessage.SentAt:HH:mm:ss}");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"   [ERROR] Failed to produce message {i}: {ex.Error.Reason}");
                }

                await Task.Delay(50); // Small delay for observability
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"   [SUCCESS] All {messageCount} InputMessage objects produced to '{InputTopic}'");
        }

        /// <summary>
        /// Consume Backup aggregation results (Baeldung Section 8 & 11)
        /// Each Backup contains aggregated InputMessages with UUID and timestamp
        /// </summary>
        static async Task ConsumeBackupResults()
        {
            var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, $"consumer-{Guid.NewGuid()}");
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"   Consuming Backup aggregations from '{OutputTopic}' (max 30 seconds)...");

            var consumedBackups = 0;
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(30);

            try
            {
                while (stopwatch.Elapsed < timeout && consumedBackups < 5) // Show first 5 backups
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (result != null)
                    {
                        consumedBackups++;
                        
                        // Section 8: Custom deserialization
                        var backup = BackupDeserializer.Deserialize(result.Message.Value);
                        
                        Console.WriteLine($"   [{consumedBackups:D2}] Backup Received:");
                        Console.WriteLine($"        UUID: {backup.Uuid}");
                        Console.WriteLine($"        Timestamp: {backup.BackupTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
                        Console.WriteLine($"        Messages Count: {backup.InputMessages?.Count ?? 0}");
                        
                        if (backup.InputMessages?.Count > 0)
                        {
                            Console.WriteLine($"        First Message: {backup.InputMessages[0].Sender} -> {backup.InputMessages[0].Recipient}");
                            Console.WriteLine($"        [OK] Successfully aggregated {backup.InputMessages.Count} messages into backup!");
                        }

                        consumer.Commit(result);
                    }
                    else if (consumedBackups > 0)
                    {
                        Console.WriteLine("   No new backups - consumption complete");
                        break;
                    }
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"   [ERROR] Consumption error: {ex.Error.Reason}");
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"   [ERROR] Deserialization error: {ex.Message}");
            }
            finally
            {
                consumer.Close();
            }

            if (consumedBackups > 0)
            {
                Console.WriteLine($"   [SUCCESS] Consumed {consumedBackups} Backup aggregations");
            }
            else
            {
                Console.WriteLine($"   [ERROR] No backups consumed - Flink job may not be running");
                Console.WriteLine($"   Checking Flink TaskManager logs for diagnostics...");
                Console.WriteLine();
                
                // Print last 20 lines of TaskManager container logs
                await PrintTaskManagerLogsAsync();
                
                Console.WriteLine();
                throw new InvalidOperationException("No backup aggregations consumed from output topic. Flink job may not be processing data correctly.");
            }
        }

        /// <summary>
        /// Create Kafka consumer configuration (Baeldung Section 4)
        /// </summary>
        public static ConsumerConfig CreateConsumerConfig(string kafkaAddress, string kafkaGroup)
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

        /// <summary>
        /// Create Kafka producer configuration (Baeldung Section 5)
        /// </summary>
        public static ProducerConfig CreateProducerConfig(string kafkaAddress)
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
            var timeout = TimeSpan.FromSeconds(30);  // Increased from 20s to 30s - Confluent Local takes time to initialize
            var stopwatch = Stopwatch.StartNew();
            var retryDelay = 1000;  // Start with 1 second

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
                    // Continue waiting - use exponential backoff
                }

                Console.WriteLine($"   [RETRY] Kafka not ready yet, retrying in {retryDelay/1000.0:F1}s... (elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)");
                await Task.Delay(retryDelay);
                
                // Exponential backoff: 1s, 2s, 3s, 4s, 5s (max)
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            // Print docker/podman ps to help diagnose the issue
            Console.WriteLine();
            Console.WriteLine("   [DEBUG] Checking container status:");
            
            string[] containerCommands = { "docker", "podman" };
            bool containerStatusShown = false;
            
            foreach (var command in containerCommands)
            {
                try
                {
                    var process = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = "ps",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    process.Start();
                    var output = await process.StandardOutput.ReadToEndAsync();
                    _ = await process.StandardError.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    
                    if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                    {
                        Console.WriteLine($"   [DEBUG] Running '{command} ps':");
                        Console.WriteLine(output);
                        containerStatusShown = true;
                        break;
                    }
                }
                catch
                {
                    // Try next command
                }
            }
            
            if (!containerStatusShown)
            {
                Console.WriteLine("   [WARNING] Could not run docker or podman ps - container runtime not available");
            }
            Console.WriteLine();

            throw new TimeoutException(
                $"Kafka not ready within {timeout.TotalSeconds} seconds. " +
                $"Attempted to connect to: {KafkaBootstrapServers}. " +
                $"Verify KAFKA_BOOTSTRAP_SERVERS environment variable is set correctly and Kafka is running. " +
                $"Check 'docker ps' to confirm Kafka container port mapping.");
        }
        
        /// <summary>
        /// Wait for Flink cluster to become healthy before submitting jobs
        /// Polls Flink health endpoint with exponential backoff retry logic
        /// </summary>
        static async Task WaitForFlinkHealthyAsync()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();
            var retryDelay = 1000;  // Start with 1 second

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/api/v1/health");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"   [SUCCESS] Flink cluster is healthy");
                        Console.WriteLine($"   Gateway URL: {FlinkGatewayUrl}");
                        return;
                    }
                }
                catch
                {
                    // Continue waiting - use exponential backoff
                }

                Console.WriteLine($"   [RETRY] Flink not ready yet, retrying in {retryDelay/1000.0:F1}s... (elapsed: {stopwatch.Elapsed.TotalSeconds:F1}s)");
                await Task.Delay(retryDelay);
                
                // Exponential backoff: 1s, 2s, 3s, 4s, 5s (max)
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            throw new TimeoutException(
                $"Flink cluster not healthy within {timeout.TotalSeconds} seconds. " +
                $"Attempted to connect to: {FlinkGatewayUrl}. " +
                $"Verify FLINK_GATEWAY_URL environment variable is set correctly and Flink is running. " +
                $"Check Flink JobManager logs for issues.");
        }
        
        /// <summary>
        /// Print last 20 lines of TaskManager container logs for debugging
        /// </summary>
        static async Task PrintTaskManagerLogsAsync()
        {
            string[] containerCommands = { "docker", "podman" };
            
            foreach (var command in containerCommands)
            {
                try
                {
                    // Find TaskManager container
                    var findProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = command,
                            Arguments = "ps --filter name=taskmanager --format {{.ID}}",
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };
                    findProcess.Start();
                    var containerId = (await findProcess.StandardOutput.ReadToEndAsync()).Trim();
                    await findProcess.WaitForExitAsync();
                    
                    if (findProcess.ExitCode == 0 && !string.IsNullOrEmpty(containerId))
                    {
                        // Get last 20 lines of logs
                        var logsProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = command,
                                Arguments = $"logs --tail 20 {containerId}",
                                RedirectStandardOutput = true,
                                RedirectStandardError = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };
                        logsProcess.Start();
                        var logs = await logsProcess.StandardOutput.ReadToEndAsync();
                        await logsProcess.WaitForExitAsync();
                        
                        if (logsProcess.ExitCode == 0 && !string.IsNullOrEmpty(logs))
                        {
                            Console.WriteLine($"   [DEBUG] TaskManager logs (last 20 lines):");
                            Console.WriteLine("   " + new string('-', 78));
                            foreach (var line in logs.Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)))
                            {
                                Console.WriteLine($"   {line}");
                            }
                            Console.WriteLine("   " + new string('-', 78));
                            return; // Successfully printed logs
                        }
                    }
                }
                catch
                {
                    // Try next command
                }
            }
            
            Console.WriteLine("   [WARNING] Could not find or access TaskManager container logs");
            Console.WriteLine("   Verify Flink TaskManager is running: docker ps | grep taskmanager");
        }
    }

    #region Custom Objects (Baeldung Section 7 & 8)

    /// <summary>
    /// InputMessage class (Baeldung Section 7)
    /// Represents a message with sender, recipient, timestamp, and content
    /// </summary>
    public class InputMessage
    {
        [JsonPropertyName("sender")]
        public string Sender { get; set; } = string.Empty;

        [JsonPropertyName("recipient")]
        public string Recipient { get; set; } = string.Empty;

        [JsonPropertyName("sentAt")]
        public DateTime SentAt { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Backup class (Baeldung Section 8)
    /// Contains aggregated InputMessages with UUID and backup timestamp
    /// </summary>
    public class Backup
    {
        [JsonPropertyName("inputMessages")]
        public List<InputMessage> InputMessages { get; set; } = new();

        [JsonPropertyName("backupTimestamp")]
        public DateTime BackupTimestamp { get; set; }

        [JsonPropertyName("uuid")]
        public Guid Uuid { get; set; }

        public Backup()
        {
            Uuid = Guid.NewGuid();
            BackupTimestamp = DateTime.UtcNow;
        }

        public Backup(List<InputMessage> inputMessages, DateTime backupTimestamp)
        {
            InputMessages = inputMessages;
            BackupTimestamp = backupTimestamp;
            Uuid = Guid.NewGuid();
        }
    }

    /// <summary>
    /// InputMessage deserializer (Baeldung Section 7)
    /// </summary>
    public static class InputMessageDeserializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static InputMessage Deserialize(byte[] bytes)
        {
            return JsonSerializer.Deserialize<InputMessage>(bytes, Options) ?? new InputMessage();
        }

        public static InputMessage Deserialize(string json)
        {
            return JsonSerializer.Deserialize<InputMessage>(json, Options) ?? new InputMessage();
        }
    }

    /// <summary>
    /// InputMessage serializer
    /// </summary>
    public static class InputMessageSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static string Serialize(InputMessage message)
        {
            return JsonSerializer.Serialize(message, Options);
        }
    }

    /// <summary>
    /// Backup serializer (Baeldung Section 8)
    /// </summary>
    public static class BackupSerializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public static byte[] Serialize(Backup backup)
        {
            var json = JsonSerializer.Serialize(backup, Options);
            return System.Text.Encoding.UTF8.GetBytes(json);
        }

        public static string SerializeToString(Backup backup)
        {
            return JsonSerializer.Serialize(backup, Options);
        }
    }

    /// <summary>
    /// Backup deserializer
    /// </summary>
    public static class BackupDeserializer
    {
        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public static Backup Deserialize(byte[] bytes)
        {
            return JsonSerializer.Deserialize<Backup>(bytes, Options) ?? new Backup();
        }

        public static Backup Deserialize(string json)
        {
            return JsonSerializer.Deserialize<Backup>(json, Options) ?? new Backup();
        }
    }

    #endregion
}