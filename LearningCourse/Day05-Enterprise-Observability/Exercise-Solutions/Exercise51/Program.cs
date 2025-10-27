using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using FlinkDotNet.DataStream;

namespace Exercise51_ObservabilityDemo
{
    /// <summary>
    /// Exercise 51: Observability Demo - Message Processing
    ///
    /// This exercise demonstrates comprehensive observability by processing messages
    /// while Prometheus and Grafana track metrics in real-time.
    ///
    /// Key differences from Exercise1:
    /// - Processes 100 messages for testing observability metrics
    /// - Job is NOT cancelled automatically - allows live metric collection
    /// - Optimized for observability testing (no delays between messages)
    /// </summary>
    static class Program
    {
        // Use fixed topic names for observability testing
        private const string InputTopic = "observability_input_day05";
        private const string OutputTopic = "observability_output_day05";
        private static readonly string ConsumerGroup = $"observability-demo-{Guid.NewGuid():N}"; // Still unique to avoid offset issues
        private const int MessageCount = 1000; // Moderate count for testing observability metrics
        
        // Kafka addresses - read from environment variables set by test infrastructure
        // IMPORTANT: Both producer and Flink consumer must use the SAME Kafka address
        // Test infrastructure dynamically discovers Kafka port and sets KAFKA_BOOTSTRAP_SERVERS
        // Producer uses host address, Flink uses Docker internal address
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";
        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
        private static string FlinkGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8080";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  Exercise 51: Observability Demo - Message Processing");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("🔧 ENVIRONMENT CONFIGURATION:");
            Console.WriteLine($"   KAFKA_BOOTSTRAP_SERVERS: {KafkaBootstrapServers}");
            Console.WriteLine($"   KAFKA_FLINK_BOOTSTRAP_SERVERS: {KafkaFlinkBootstrapServers}");
            Console.WriteLine($"   FLINK_JOB_GATEWAY_URL: {FlinkGatewayUrl}");
            Console.WriteLine($"   MESSAGE_COUNT: {MessageCount:N0}");
            Console.WriteLine($"   INPUT_TOPIC: {InputTopic}");
            Console.WriteLine($"   OUTPUT_TOPIC: {OutputTopic}");
            Console.WriteLine($"   CONSUMER_GROUP: {ConsumerGroup}");
            Console.WriteLine();
            Console.WriteLine("  Purpose: Demonstrate comprehensive observability metrics");
            Console.WriteLine($"  Message Count: {MessageCount} messages");
            Console.WriteLine("  Monitoring: Prometheus + Grafana + Flink Dashboard");
            Console.WriteLine();
            Console.WriteLine("  Note: Job will continue running for metric collection");
            Console.WriteLine("        (Will be cancelled by test infrastructure)");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                Log.Information("🚀 Starting Observability Demo at {Timestamp}", DateTime.UtcNow);
                await RunObservabilityDemo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "❌ FATAL ERROR executing observability demo");
                Console.WriteLine($"❌ ERROR: {ex.Message}");
                Console.WriteLine($"❌ Stack Trace: {ex.StackTrace}");
                Environment.Exit(1);
            }
            finally
            {
                Log.Information("🛑 Shutting down Exercise51 at {Timestamp}", DateTime.UtcNow);
                await Log.CloseAndFlushAsync();
            }
        }

        static async Task RunObservabilityDemo()
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

            Console.WriteLine(">> Step 4/6: Submitting Flink job (will poll for messages)...");
            var jobClient = await SubmitJob();
            Console.WriteLine($"   Job ID: {jobClient.GetJobId()}");
            Console.WriteLine("   Job will remain running for observability metrics collection");
            Console.WriteLine("   ⏳ Waiting for job to fully start...");
            await Task.Delay(3000); // Wait for job to start (matching Day 1 timing)
            Console.WriteLine();

            Console.WriteLine($">> Step 5/6: Producing messages...");
            await ProduceMessagesAsync(); // Produce messages AFTER job is running
            await Task.Delay(2000); // Wait for Flink to process (matching Day 1 timing)
            Console.WriteLine();

            Console.WriteLine(">> Step 6/6: Monitoring processing (sampling results)...");
            MonitorProcessing();
            Console.WriteLine();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  OBSERVABILITY DEMO RUNNING!");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("  The Flink job is now processing messages.");
            Console.WriteLine("  Prometheus is collecting metrics every 15 seconds.");
            Console.WriteLine("  Grafana dashboards are visualizing the data flow.");
            Console.WriteLine();
            Console.WriteLine("  Metrics Available:");
            Console.WriteLine("    - flink_taskmanager_job_task_operator_numRecordsIn");
            Console.WriteLine("    - flink_taskmanager_job_task_operator_numRecordsOut");
            Console.WriteLine("    - flink_taskmanager_Status_JVM_Memory_Heap_Used");
            Console.WriteLine("    - kafka_server_BrokerTopicMetrics_Count");
            Console.WriteLine();
            Console.WriteLine("  Job will continue running until cancelled by test infrastructure.");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            
            // Keep process alive - DO NOT CANCEL JOB
            // Test infrastructure will cancel the job via Flink REST API
            Console.WriteLine("Press Ctrl+C to exit (job will continue running)...");
            await Task.Delay(Timeout.Infinite);
        }

        static async Task<long> GetTopicMessageCountAsync(string topic)
        {
            try
            {
                var consumerConfig = new ConsumerConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    GroupId = $"count-check-{Guid.NewGuid()}",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                    EnableAutoCommit = false,
                    BrokerAddressFamily = BrokerAddressFamily.V4,
                    SecurityProtocol = SecurityProtocol.Plaintext
                };

                using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
                
                // Use AdminClient to get topic metadata
                var adminConfig = new AdminClientConfig
                {
                    BootstrapServers = KafkaBootstrapServers,
                    BrokerAddressFamily = BrokerAddressFamily.V4,
                    SecurityProtocol = SecurityProtocol.Plaintext
                };
                
                using var admin = new AdminClientBuilder(adminConfig).Build();
                var metadata = admin.GetMetadata(topic, TimeSpan.FromSeconds(5));
                var topicMetadata = metadata.Topics.FirstOrDefault(t => t.Topic == topic);
                
                if (topicMetadata == null)
                {
                    return await Task.FromResult(0L);
                }

                long totalMessages = 0;
                foreach (var partition in topicMetadata.Partitions)
                {
                    var topicPartition = new TopicPartition(topic, partition.PartitionId);
                    var watermark = consumer.QueryWatermarkOffsets(topicPartition, TimeSpan.FromSeconds(5));
                    totalMessages += (watermark.High.Value - watermark.Low.Value);
                }

                return await Task.FromResult(totalMessages);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"   Error checking topic message count: {ex.Message}");
                return await Task.FromResult(0L);
            }
        }

        static async Task<FlinkDotNet.DataStream.IJobClient> SubmitJob()
        {
            Log.Information("📝 Creating Flink job for observability demo");
            Console.WriteLine($"   Creating Flink job for observability demo...");
            Console.WriteLine($"   - Input Topic: {InputTopic}");
            Console.WriteLine($"   - Transformation: Uppercase");
            Console.WriteLine($"   - Output Topic: {OutputTopic}");
            Console.WriteLine($"   - Flink Bootstrap Servers: {KafkaFlinkBootstrapServers}");
            Console.WriteLine($"   - Consumer Group: {ConsumerGroup}");

            Log.Information("🔧 Initializing Flink StreamExecutionEnvironment");
            var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

            Log.Information("📥 Creating Kafka source for topic {Topic}", InputTopic);
            var stringInputStream = environment.FromKafka(
                topic: InputTopic,
                bootstrapServers: KafkaFlinkBootstrapServers,
                groupId: ConsumerGroup,
                startingOffsets: "earliest"
            );

            Log.Information("🔄 Adding Map transformation (Uppercase)");
            stringInputStream
                .Map(new WordsCapitalizer())
                .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);

            Log.Information("🚀 Submitting job 'observability-demo-pipeline' to Flink cluster");
            var jobClient = await environment.ExecuteAsync("observability-demo-pipeline");
            var jobId = jobClient.GetJobId();

            Log.Information("✅ Job submitted successfully - Job ID: {JobId}", jobId);
            Console.WriteLine($"   [SUCCESS] Job submitted - Job ID: {jobId}");
            
            return jobClient;
        }

        static async Task ProduceMessagesAsync()
        {
            Log.Information("📤 Starting ASYNC message production - {MessageCount} messages to topic {Topic}", MessageCount, InputTopic);
            var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            Console.WriteLine($"   Producing {MessageCount:N0} messages ASYNCHRONOUSLY...");
            Console.WriteLine($"   Kafka Bootstrap Servers: {KafkaBootstrapServers}");
            Console.WriteLine($"   Target Topic: {InputTopic}");
            var stopwatch = Stopwatch.StartNew();

            var tasks = new List<Task>();
            for (int i = 0; i < MessageCount; i++)
            {
                var message = new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = $"message {i}"
                };

                try
                {
                    // Fire-and-forget - don't await individual messages
                    var task = producer.ProduceAsync(InputTopic, message);
                    tasks.Add(task);
                    
                    // Print progress for small message counts
                    if (MessageCount <= 20 || (i + 1) % Math.Max(1, MessageCount / 5) == 0)
                    {
                        var elapsed = stopwatch.Elapsed.TotalSeconds;
                        var rate = elapsed > 0 ? (i + 1) / elapsed : 0;
                        Log.Information("📊 Progress: {Current}/{Total} messages produced ({Rate:N0} msg/sec)", i + 1, MessageCount, rate);
                        Console.WriteLine($"   [{i + 1}/{MessageCount}] Rate: {rate:N0} msg/sec");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Log.Error("❌ Failed to produce message {MessageId}: {Error}", i, ex.Error.Reason);
                    Console.WriteLine($"   [ERROR] Failed to produce message {i}: {ex.Error.Reason}");
                }
            }

            // Flush all pending messages to ensure delivery
            Log.Information("⏳ Flushing all pending messages to Kafka (ensuring delivery before job submission)");
            Console.WriteLine($"   Flushing messages to Kafka...");
            producer.Flush(TimeSpan.FromSeconds(30));
            stopwatch.Stop();
            
            var totalRate = MessageCount / stopwatch.Elapsed.TotalSeconds;
            Log.Information("✅ Message production complete: {MessageCount} messages in {Duration:F1}s ({Rate:N0} msg/sec)",
                MessageCount, stopwatch.Elapsed.TotalSeconds, totalRate);
            Console.WriteLine($"   [SUCCESS] All {MessageCount:N0} messages produced in {stopwatch.Elapsed.TotalSeconds:F1}s ({totalRate:N0} msg/sec)");
            Console.WriteLine($"   ✅ All messages flushed to Kafka - topics are ready for consumption");
            
            // Wait a moment for Kafka to fully persist messages
            await Task.Delay(2000);
            Log.Information("⏳ Waited 2s for Kafka to persist messages - ready for Flink job submission");
        }

        static void MonitorProcessing()
        {
            var monitorGroup = $"monitor-{Guid.NewGuid()}";
            Log.Information("👀 Starting output monitoring - Consumer Group: {ConsumerGroup}, Topic: {Topic}", monitorGroup, OutputTopic);
            
            var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, monitorGroup);
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"   Sampling output messages (will check first 100)...");
            Console.WriteLine($"   Output Topic: {OutputTopic}");
            Console.WriteLine($"   Monitoring for 30 seconds...");

            var consumedCount = 0;
            var capitalizedCount = 0;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                while (stopwatch.Elapsed < TimeSpan.FromSeconds(30) && consumedCount < 100)
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (result != null)
                    {
                        consumedCount++;
                        
                        if (result.Message.Value == result.Message.Value.ToUpperInvariant())
                        {
                            capitalizedCount++;
                        }

                        if (consumedCount % 10 == 0)
                        {
                            Log.Information("📥 Sampled {Count} output messages ({Capitalized} capitalized)", consumedCount, capitalizedCount);
                            Console.WriteLine($"   Sampled: {consumedCount} messages ({capitalizedCount} capitalized)");
                        }
                    }
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error("❌ Consumption error: {Error}", ex.Error.Reason);
                Console.WriteLine($"   [ERROR] Consumption error: {ex.Error.Reason}");
            }
            finally
            {
                consumer.Close();
            }

            if (consumedCount > 0)
            {
                Log.Information("✅ Monitoring complete: {Capitalized}/{Total} messages verified as capitalized", capitalizedCount, consumedCount);
                Console.WriteLine($"   [SUCCESS] Verified processing: {capitalizedCount}/{consumedCount} messages capitalized");
                Console.WriteLine($"   Note: Job continues processing remaining {MessageCount - consumedCount:N0} messages");
                
            }
            else
            {
                Log.Warning("⚠️ No messages consumed from output topic - job may still be starting");
                Console.WriteLine($"   [WARNING] No messages consumed yet - job may still be starting");
            }
        }

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

        public static ProducerConfig CreateProducerConfig(string kafkaAddress)
        {
            return new ProducerConfig
            {
                BootstrapServers = kafkaAddress,
                EnableIdempotence = true,
                Acks = Acks.All,
                LingerMs = 5,
                BrokerAddressFamily = BrokerAddressFamily.V4,
                SecurityProtocol = SecurityProtocol.Plaintext,
                // Optimize for throughput
                BatchSize = 16384,
                CompressionType = CompressionType.Snappy
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

            // Delete topics first to ensure clean state (prevent message accumulation between test runs)
            var topicsToDelete = new[] { InputTopic, OutputTopic };
            try
            {
                await admin.DeleteTopicsAsync(topicsToDelete);
                Console.WriteLine($"   [INFO] Deleted existing topics: {InputTopic}, {OutputTopic}");
                // Wait for topic deletion to complete
                await Task.Delay(2000);
            }
            catch (DeleteTopicsException ex)
            {
                // Topics don't exist - this is fine
                var existErrors = ex.Results.Where(r => r.Error.Code != ErrorCode.UnknownTopicOrPart).ToList();
                if (existErrors.Any())
                {
                    Console.WriteLine($"   [WARNING] Some topics failed to delete: {string.Join(", ", existErrors.Select(e => e.Error.Reason))}");
                }
            }

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
            var attemptCount = 0;

            Log.Information("⏳ Waiting for Kafka to become ready at {BootstrapServers}", KafkaBootstrapServers);

            while (stopwatch.Elapsed < timeout)
            {
                attemptCount++;
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
                        Log.Information("✅ Kafka ready after {Elapsed:F1}s - {BrokerCount} broker(s) found",
                            stopwatch.Elapsed.TotalSeconds, metadata.Brokers.Count);
                        Console.WriteLine($"   [SUCCESS] Kafka ready ({metadata.Brokers.Count} broker(s)) after {stopwatch.Elapsed.TotalSeconds:F1}s");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Log.Debug("Kafka connection attempt {Attempt} failed: {Error}", attemptCount, ex.Message);
                }

                Console.WriteLine($"   [RETRY {attemptCount}] Waiting for Kafka... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed)");
                await Task.Delay(retryDelay);
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            Log.Error("❌ Kafka not ready within {Timeout}s timeout", timeout.TotalSeconds);
            throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds}s");
        }

        static async Task WaitForFlinkHealthyAsync()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();
            var retryDelay = 1000;
            var attemptCount = 0;

            Log.Information("⏳ Waiting for Flink cluster to become healthy at {FlinkUrl}", FlinkGatewayUrl);

            while (stopwatch.Elapsed < timeout)
            {
                attemptCount++;
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/api/v1/health");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        Log.Information("✅ Flink cluster healthy after {Elapsed:F1}s - Status: {StatusCode}",
                            stopwatch.Elapsed.TotalSeconds, response.StatusCode);
                        Console.WriteLine($"   [SUCCESS] Flink cluster healthy after {stopwatch.Elapsed.TotalSeconds:F1}s");
                        return;
                    }
                    
                    Log.Debug("Flink health check attempt {Attempt} returned {StatusCode}", attemptCount, response.StatusCode);
                }
                catch (Exception ex)
                {
                    Log.Debug("Flink connection attempt {Attempt} failed: {Error}", attemptCount, ex.Message);
                }

                Console.WriteLine($"   [RETRY {attemptCount}] Waiting for Flink... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed)");
                await Task.Delay(retryDelay);
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            Log.Error("❌ Flink not healthy within {Timeout}s timeout", timeout.TotalSeconds);
            throw new TimeoutException($"Flink not healthy within {timeout.TotalSeconds}s");
        }

    }

    public class WordsCapitalizer : FlinkDotNet.DataStream.IMapFunction<string, string>
    {
        private static int _processedCount = 0;
        
        public string Map(string s)
        {
            var transformed = s.ToUpperInvariant();
            _processedCount++;
            
            // Log every 1000th message for monitoring
            if (_processedCount % 1000 == 0)
            {
                Log.Debug("🔄 Transformed {Count} messages", _processedCount);
            }
            
            return transformed;
        }
    }
}