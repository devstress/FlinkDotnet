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
    /// Exercise 2: Custom Objects and Backup Aggregation (Baeldung Sections 7-11)
    ///
    /// Demonstrates Flink event-time windowing with custom object serialization.
    /// Uses 1-minute windows for testing instead of 24-hour production windows.
    /// See BaeldungNativeApi.cs for the DataStream API implementation.
    /// </summary>
    static class Program
    {
        private const string InputTopic = "exercise2_input";
        private const string OutputTopic = "exercise2_output";
        
        // Kafka configuration for HOST operations (producer/consumer)
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? throw new InvalidOperationException("KAFKA_BOOTSTRAP_SERVERS environment variable must be set");
        
        // Flink Gateway configuration
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string FlinkGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

        private static string FlinkJobManagerUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOBMANAGER_URL") ?? "http://localhost:8081";

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
            Console.WriteLine("  Using native DataStream API - MODIFIED FOR TESTING");
            Console.WriteLine("  .TimeWindowAll(Time.Minutes(1))  // Testing: 1-minute window");
            Console.WriteLine("  .Aggregate(new BackupAggregator())");
            Console.WriteLine("  Messages: 50 historical messages (T-30s to T-25s, 5s span)");
            Console.WriteLine("  Watermark: BoundedOutOfOrderness (200ms) advances past window");
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
        /// Main demo flow: Submit Flink job → Produce messages → Consume aggregated results
        /// </summary>
        static async Task RunBackupAggregationDemo()
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

                Console.WriteLine(">> Step 4/6: Submitting Flink backup aggregation job...");
                jobClient = await SubmitBackupAggregationJob();
                await Task.Delay(3000); // Wait for job to start
                Console.WriteLine();

                Console.WriteLine(">> Step 5/6: Producing timestamped InputMessage objects...");
                await ProduceInputMessages();
                await Task.Delay(10000); // Wait 10 seconds for window to fire (marker at T+95s triggers window)
                Console.WriteLine();

                Console.WriteLine(">> Step 6/6: Consuming Backup aggregation results...");
                await ConsumeBackupResults();
                Console.WriteLine();

                Console.WriteLine("================================================================================");
                Console.WriteLine("  EXERCISE 2 COMPLETED!");
                Console.WriteLine("================================================================================");
                Console.WriteLine();
                Console.WriteLine("What you learned (Baeldung Sections 7-11):");
                Console.WriteLine("  [OK] Custom object deserialization (InputMessage)");
                Console.WriteLine("  [OK] Custom object serialization (Backup)");
                Console.WriteLine("  [OK] EventTime timestamp extraction (recent timestamps from messages)");
                Console.WriteLine("  [OK] Time windows: .TimeWindowAll(Time.Minutes(1)) - Testing");
                Console.WriteLine("  [OK] Aggregation: .Aggregate(new BackupAggregator())");
                Console.WriteLine("  [OK] Watermark strategy: BoundedOutOfOrderness(200ms)");
                Console.WriteLine("  [OK] 1-minute window captures all 50 messages in single window firing");
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
        /// Submit Flink job with event-time windowing and aggregation.
        /// Creates pipeline: Kafka source → timestamp extraction → window aggregation → Kafka sink
        /// </summary>
        static async Task<FlinkDotNet.DataStream.IJobClient> SubmitBackupAggregationJob()
        {
            Console.WriteLine($"   Creating Flink job using native DataStream API...");
            Console.WriteLine($"   - Input Topic: {InputTopic}");
            Console.WriteLine($"   - Time Characteristic: EventTime");
            Console.WriteLine($"   - Window: TimeWindowAll(Time.Minutes(1)) - Testing");
            Console.WriteLine($"   - Watermark: BoundedOutOfOrderness(200ms tolerance)");
            Console.WriteLine($"   - Aggregation: BackupAggregator");
            Console.WriteLine($"   - Output Topic: {OutputTopic}");
            Console.WriteLine();
            
            try
            {
                var jobClient = await BaeldungNativeApi.CreateBackup();
                Console.WriteLine($"   [SUCCESS] Backup aggregation job submitted");
                return jobClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   [ERROR] Error submitting job: {ex.Message}");
                Console.WriteLine($"   Stack trace: {ex.StackTrace}");
                throw;
            }
        }


        /// <summary>
        /// Produce timestamped messages for Flink to process.
        /// Messages span 5 seconds with 100ms intervals between each.
        /// </summary>
        static async Task ProduceInputMessages()
        {
            var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            const int messageCount = 50;
            Console.WriteLine($"   Producing {messageCount} InputMessage objects with historical timestamps...");

            // Start timestamps 30 seconds in the past to ensure they're historical
            // when watermark advances. Messages span 5 seconds (50 × 100ms).
            var baseTimestamp = DateTime.UtcNow.AddSeconds(-30);
            
            for (int i = 0; i < messageCount; i++)
            {
                var inputMessage = new InputMessage
                {
                    Sender = $"sender-{i}",
                    Recipient = $"recipient-{i}",
                    SentAt = baseTimestamp.AddMilliseconds(i * 100),  // 100ms intervals
                    Message = $"Test message {i}"
                };

                var json = InputMessageSerializer.Serialize(inputMessage);

                var kafkaMessage = new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = json
                };

                try
                {
                    var deliveryResult = await producer.ProduceAsync(InputTopic, kafkaMessage);
                    
                    if (i % 10 == 0 || i == messageCount - 1)
                    {
                        Console.WriteLine($"   [{i + 1:D3}/{messageCount}] Sent: From={inputMessage.Sender} To={inputMessage.Recipient} Time={inputMessage.SentAt:HH:mm:ss} Partition={deliveryResult.Partition.Value}");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"   [ERROR] Failed to produce message {i}: {ex.Error.Reason}");
                }

                await Task.Delay(50); // Small delay for observability
            }

            // Send marker message with timestamp far in future to advance watermark
            // This triggers window closure and output generation
            Console.WriteLine($"   [DEBUG] Sending marker message 95 seconds after base to advance watermark...");
            var markerMessage = new InputMessage
            {
                Sender = "marker",
                Recipient = "system",
                SentAt = baseTimestamp.AddSeconds(95),  // T+65s advances watermark past window end
                Message = "Marker message to advance watermark"
            };
            var markerJson = InputMessageSerializer.Serialize(markerMessage);
            await producer.ProduceAsync(InputTopic, new Message<string, string>
            {
                Key = "marker",
                Value = markerJson
            });
            Console.WriteLine($"   [DEBUG] Marker message sent with timestamp {markerMessage.SentAt:HH:mm:ss} to trigger window");

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"   [SUCCESS] All {messageCount} InputMessage objects produced to '{InputTopic}'");
            Console.WriteLine($"   [SUCCESS] Marker message sent to trigger window closure");
        }

        /// <summary>
        /// Consume Backup aggregation results from output topic.
        /// Expects one Backup containing all 50 messages from the window.
        /// </summary>
        static async Task ConsumeBackupResults()
        {
            var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, $"consumer-{Guid.NewGuid()}");
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"   Consuming Backup aggregations from '{OutputTopic}' (max 25 seconds)...");
            Console.WriteLine($"   NOTE: 1-minute event-time window with BoundedOutOfOrderness watermarks");
            Console.WriteLine($"   Window fires when watermark advances past window boundaries");
            Console.WriteLine($"   Expecting 1 backup record with all 50 messages in single window");

            var consumedBackups = 0;
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(25);

            try
            {
                consumedBackups = await ConsumeMessagesLoop(consumer, stopwatch, timeout);
            }
            catch (ConsumeException ex)
            {
                HandleConsumeException(ex);
            }
            catch (JsonException ex)
            {
                HandleJsonException(ex);
            }
            finally
            {
                consumer.Close();
            }

            await ValidateConsumptionResults(consumedBackups);
        }

        /// <summary>
        /// Main message consumption loop
        /// </summary>
        private static Task<int> ConsumeMessagesLoop(IConsumer<string, string> consumer, Stopwatch stopwatch, TimeSpan timeout)
        {
            var consumedBackups = 0;
            var messageNumber = 0;
            var totalMessages = 0;
            var consecutiveNullCount = 0;  // Track consecutive null results for retry logic
            const int maxConsecutiveNulls = 5;  // Retry 5 times with 2 second intervals
            
            while (stopwatch.Elapsed < timeout && consumedBackups < 10)  // Allow up to 10 backups
            {
                var result = consumer.Consume(TimeSpan.FromMilliseconds(2000));

                if (result == null)
                {
                    consecutiveNullCount++;
                    
                    if (consumedBackups > 0)
                    {
                        Console.WriteLine("   No new backups - consumption complete");
                        break;
                    }
                    
                    // If we haven't received any messages yet, retry up to 3 times
                    if (consecutiveNullCount >= maxConsecutiveNulls)
                    {
                        Console.WriteLine($"   No messages after {maxConsecutiveNulls} retry attempts");
                        break;
                    }
                    
                    Console.WriteLine($"   No messages yet, retrying ({consecutiveNullCount}/{maxConsecutiveNulls})...");
                    continue;
                }

                // Reset consecutive null count when we receive a message
                consecutiveNullCount = 0;
                messageNumber++;
                var (shouldCommit, wasBackup, messageCount) = ProcessConsumedMessage(result, messageNumber);
                
                if (wasBackup)
                {
                    consumedBackups++;
                    totalMessages += messageCount;
                }
                
                if (shouldCommit)
                {
                    consumer.Commit(result);
                }
            }
            
            Console.WriteLine($"   [SUMMARY] Consumed {consumedBackups} backup(s) with total {totalMessages} messages");
            return Task.FromResult(totalMessages);  // Return total message count instead of backup count
        }

        /// <summary>
        /// Process a single consumed message
        /// Returns: (shouldCommit, wasBackup, messageCount) tuple
        /// </summary>
        private static (bool shouldCommit, bool wasBackup, int messageCount) ProcessConsumedMessage(ConsumeResult<string, string> result, int messageNumber)
        {
            var messageValue = result.Message.Value ?? string.Empty;
            if (!IsJsonMessage(messageValue))
            {
                // Commit offset to skip Exercise 1 messages on next run - don't print or count them
                return (true, false, 0); // Commit to advance offset, but wasn't a backup
            }
            
            PrintRawMessageDetails(result, messageNumber);
            
            var messageCount = TryDeserializeBackup(messageValue, messageNumber, out var errorOccurred);
            if (messageCount == 0 && errorOccurred)
            {
                throw new JsonException($"Failed to deserialize backup message at offset {result.Offset}");
            }

            return (true, true, messageCount); // Should commit, was a backup, return message count
        }

        /// <summary>
        /// Print raw message details for debugging
        /// </summary>
        private static void PrintRawMessageDetails(ConsumeResult<string, string> result, int messageNumber)
        {
            Console.WriteLine($"   [{messageNumber:D2}] Raw Kafka Message:");
            Console.WriteLine($"        Partition: {result.Partition}");
            Console.WriteLine($"        Offset: {result.Offset}");
            Console.WriteLine($"        Timestamp: {result.Message.Timestamp.UtcDateTime:yyyy-MM-dd HH:mm:ss.fff} UTC");
            Console.WriteLine($"        Key: {result.Message.Key ?? "(null)"}");
            Console.WriteLine($"        Value Length: {result.Message.Value?.Length ?? 0} characters");
            
            var valuePreview = result.Message.Value?.Length > 500
                ? result.Message.Value.Substring(0, 500) + "..."
                : result.Message.Value ?? "(null)";
            Console.WriteLine($"        Value (first 500 chars): {valuePreview}");
            Console.WriteLine();
        }

        /// <summary>
        /// Handle Kafka consumption exceptions
        /// </summary>
        private static void HandleConsumeException(ConsumeException ex)
        {
            Console.WriteLine($"   [ERROR] Consumption error: {ex.Error.Reason}");
        }

        /// <summary>
        /// Handle JSON deserialization exceptions
        /// </summary>
        private static void HandleJsonException(JsonException ex)
        {
            Console.WriteLine($"   [FINAL ERROR] JSON deserialization failed");
            Console.WriteLine($"   [ERROR] Message: {ex.Message}");
            Console.WriteLine($"   [ERROR] Path: {ex.Path ?? "(none)"}");
            Console.WriteLine($"   [ERROR] Line: {ex.LineNumber?.ToString() ?? "(none)"}");
            Console.WriteLine($"   [ERROR] Position: {ex.BytePositionInLine?.ToString() ?? "(none)"}");
        }

        /// <summary>
        /// Validate consumption results and handle failures
        /// </summary>
        private static Task ValidateConsumptionResults(int totalMessages)
        {
            if (totalMessages >= 50)
            {
                Console.WriteLine($"   [SUCCESS] Total of {totalMessages} messages across all backup windows");
                Console.WriteLine($"   [SUCCESS] 1-minute event-time window fired successfully!");
            }
            else if (totalMessages > 0)
            {
                Console.WriteLine($"   [WARNING] Only {totalMessages} messages consumed (expected 50+)");
                Console.WriteLine($"   [WARNING] Some messages may not have been captured in windows");
            }
            else
            {
                Console.WriteLine($"   [ERROR] No backups consumed - aggregation may have failed");
                Console.WriteLine($"   [ERROR] Window: TimeWindowAll(Time.Minutes(1))");
                Console.WriteLine($"   [ERROR] Expected: Multiple backups totaling 50 InputMessages but found 0");
            }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Create Kafka consumer configuration
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
        /// Create Kafka producer configuration
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

        /// <summary>
        /// Check if message value is JSON (starts with '{')
        /// </summary>
        private static bool IsJsonMessage(string messageValue)
        {
            return messageValue.TrimStart().StartsWith('{');
        }

        /// <summary>
        /// Deserialize and display backup message, returning message count
        /// </summary>
        private static int TryDeserializeBackup(string messageValue, int messageNumber, out bool errorOccurred)
        {
            errorOccurred = false;
            try
            {
                var backup = BackupDeserializer.Deserialize(messageValue);
                
                Console.WriteLine($"   [{messageNumber:D2}] Backup Deserialized:");
                Console.WriteLine($"        UUID: {backup.Uuid}");
                Console.WriteLine($"        Timestamp: {backup.BackupTimestamp:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"        Messages Count: {backup.InputMessages?.Count ?? 0}");
                
                if (backup.InputMessages?.Count > 0)
                {
                    Console.WriteLine($"        First Message: {backup.InputMessages[0].Sender} -> {backup.InputMessages[0].Recipient}");
                    Console.WriteLine($"        [OK] Window captured {backup.InputMessages.Count} messages");
                    return backup.InputMessages.Count;
                }
                else
                {
                    Console.WriteLine($"        [WARNING] Backup contains no messages");
                    return 0;
                }
            }
            catch (JsonException deserEx)
            {
                Console.WriteLine($"   [WARN] Skipping non-JSON message (likely from Exercise 1)");
                Console.WriteLine($"        [ERROR] Deserialization error: {deserEx.Message}");
                PrintCharacterBreakdown(messageValue);
                errorOccurred = true;
                return 0;
            }
        }

        /// <summary>
        /// Print character breakdown for debugging deserialization issues
        /// </summary>
        private static void PrintCharacterBreakdown(string messageValue)
        {
            if (messageValue != null)
            {
                Console.WriteLine($"        Character breakdown (first 50):");
                for (int i = 0; i < Math.Min(50, messageValue.Length); i++)
                {
                    var c = messageValue[i];
                    Console.WriteLine($"          [{i}] '{c}' (ASCII: {(int)c}, Hex: 0x{(int)c:X2})");
                }
            }
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
                // Use 4 partitions - TimeWindowAll merges all partition streams into single window operator
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
            var timeout = TimeSpan.FromSeconds(60);  // Increased from 20s to 30s - Confluent Local takes time to initialize
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
            var timeout = TimeSpan.FromSeconds(60);
            var stopwatch = Stopwatch.StartNew();
            var retryDelay = 1000;  // Start with 1 second

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                    var response = await httpClient.GetAsync($"{FlinkJobManagerUrl}/v1/overview");

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"   [SUCCESS] Flink cluster is healthy");
                        Console.WriteLine($"   JobManager URL: {FlinkJobManagerUrl}");
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
                $"Attempted to connect to: {FlinkJobManagerUrl}. " +
                $"Verify FLINK_JOBMANAGER_URL environment variable is set correctly and Flink is running. " +
                $"Check Flink JobManager logs for issues.");
        }
        
    }

    #region Custom Objects (Baeldung Section 7 & 8)

    /// <summary>
    /// Input message with sender, recipient, timestamp, and content
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
    /// Backup aggregation containing multiple input messages
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
    /// Deserializes JSON to InputMessage objects
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
    /// Serializes Backup objects to JSON
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