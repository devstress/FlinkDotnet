using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using FlinkDotNet.DataStream;

namespace Exercise51_ObservabilityDemo
{
    /// <summary>
    /// Exercise 51: Observability Demo - High-Volume Message Processing
    ///
    /// This exercise demonstrates comprehensive observability by processing 100,000 messages
    /// while Prometheus and Grafana track metrics in real-time.
    ///
    /// Key differences from Exercise1:
    /// - Processes 100,000 messages (vs 50) for extended observation
    /// - Job is NOT cancelled automatically - allows live metric collection
    /// - Optimized for observability testing (no delays between messages)
    /// </summary>
    static class Program
    {
        private const string InputTopic = "observability_input";
        private const string OutputTopic = "observability_output";
        private const string ConsumerGroup = "observability-demo";
        private const int MessageCount = 10000; // Sufficient volume for observability testing with reasonable execution time
        
        // Kafka addresses - read from environment variables set by test infrastructure
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
        private static string FlinkGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_GATEWAY_URL") ?? "http://localhost:8080";

        static async Task Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  Exercise 51: Observability Demo - High-Volume Processing");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("  Purpose: Demonstrate comprehensive observability metrics");
            Console.WriteLine($"  Message Volume: {MessageCount:N0} messages");
            Console.WriteLine("  Monitoring: Prometheus + Grafana + Flink Dashboard");
            Console.WriteLine();
            Console.WriteLine("  Note: Job will continue running for metric collection");
            Console.WriteLine("        (Will be cancelled by test infrastructure)");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                await RunObservabilityDemo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing observability demo");
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
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

            Console.WriteLine(">> Step 4/6: Submitting Flink job...");
            var jobClient = await SubmitJob();
            Console.WriteLine($"   Job ID: {jobClient.GetJobId()}");
            Console.WriteLine("   Job will remain running for observability metrics collection");
            await Task.Delay(5000); // Wait for job to fully start
            Console.WriteLine();

            Console.WriteLine($">> Step 5/6: Producing {MessageCount:N0} messages...");
            ProduceMessages();
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

        static async Task<FlinkDotNet.DataStream.IJobClient> SubmitJob()
        {
            Console.WriteLine($"   Creating Flink job for observability demo...");
            Console.WriteLine($"   - Input Topic: {InputTopic}");
            Console.WriteLine($"   - Transformation: Uppercase");
            Console.WriteLine($"   - Output Topic: {OutputTopic}");

            var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

            var stringInputStream = environment.FromKafka(
                topic: InputTopic,
                bootstrapServers: KafkaFlinkBootstrapServers,
                groupId: ConsumerGroup,
                startingOffsets: "earliest"
            );

            stringInputStream
                .Map(new WordsCapitalizer())
                .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);

            var jobClient = await environment.ExecuteAsync("observability-demo-pipeline");

            Console.WriteLine($"   [SUCCESS] Job submitted");
            
            return jobClient;
        }

        static void ProduceMessages()
        {
            var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            Console.WriteLine($"   Producing {MessageCount:N0} messages (optimized for speed)...");
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < MessageCount; i++)
            {
                var message = new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = $"message {i}"
                };

                try
                {
                    // Fire and forget for speed (no await)
                    _ = producer.ProduceAsync(InputTopic, message);
                    
                    // Print progress every 10,000 messages
                    if ((i + 1) % 10000 == 0)
                    {
                        var elapsed = stopwatch.Elapsed.TotalSeconds;
                        var rate = (i + 1) / elapsed;
                        Console.WriteLine($"   [{i + 1:N0}/{MessageCount:N0}] Rate: {rate:N0} msg/sec");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"   [ERROR] Failed to produce message {i}: {ex.Error.Reason}");
                }
            }

            // Flush all pending messages
            producer.Flush(TimeSpan.FromSeconds(30));
            stopwatch.Stop();
            
            var totalRate = MessageCount / stopwatch.Elapsed.TotalSeconds;
            Console.WriteLine($"   [SUCCESS] All {MessageCount:N0} messages produced in {stopwatch.Elapsed.TotalSeconds:F1}s ({totalRate:N0} msg/sec)");
        }

        static void MonitorProcessing()
        {
            var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, $"monitor-{Guid.NewGuid()}");
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"   Sampling output messages (will check first 100)...");

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

                        if (consumedCount % 20 == 0)
                        {
                            Console.WriteLine($"   Sampled: {consumedCount} messages ({capitalizedCount} capitalized)");
                        }
                    }
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"   [ERROR] Consumption error: {ex.Error.Reason}");
            }
            finally
            {
                consumer.Close();
            }

            if (consumedCount > 0)
            {
                Console.WriteLine($"   [SUCCESS] Verified processing: {capitalizedCount}/{consumedCount} messages capitalized");
                Console.WriteLine($"   Note: Job continues processing remaining {MessageCount - consumedCount:N0} messages");
            }
            else
            {
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
                        Console.WriteLine($"   [SUCCESS] Kafka ready ({metadata.Brokers.Count} broker(s))");
                        return;
                    }
                }
                catch
                {
                    // Continue waiting
                }

                Console.WriteLine($"   [RETRY] Waiting for Kafka... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed)");
                await Task.Delay(retryDelay);
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            throw new TimeoutException($"Kafka not ready within {timeout.TotalSeconds}s");
        }

        static async Task WaitForFlinkHealthyAsync()
        {
            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();
            var retryDelay = 1000;

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    using var httpClient = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                    var response = await httpClient.GetAsync($"{FlinkGatewayUrl}/v1/overview");
                    
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"   [SUCCESS] Flink cluster healthy");
                        return;
                    }
                }
                catch
                {
                    // Continue waiting
                }

                Console.WriteLine($"   [RETRY] Waiting for Flink... ({stopwatch.Elapsed.TotalSeconds:F1}s elapsed)");
                await Task.Delay(retryDelay);
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            throw new TimeoutException($"Flink not healthy within {timeout.TotalSeconds}s");
        }
    }

    public class WordsCapitalizer : FlinkDotNet.DataStream.IMapFunction<string, string>
    {
        public string Map(string s)
        {
            return s.ToUpperInvariant();
        }
    }
}