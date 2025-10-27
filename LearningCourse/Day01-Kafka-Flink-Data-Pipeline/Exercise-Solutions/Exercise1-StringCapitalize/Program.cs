using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using FlinkDotNet.DataStream;

namespace Exercise1_StringCapitalize
{
    /// <summary>
    /// Exercise 1: String Stream Processing (Section 6 from Baeldung Tutorial)
    ///
    /// Reference: https://www.baeldung.com/kafka-flink-data-pipeline (Section 6)
    ///
    /// This exercise demonstrates:
    /// - Kafka String Consumer configuration
    /// - Kafka String Producer configuration  
    /// - Flink stream processing with uppercase transformation
    /// - End-to-end data pipeline: Kafka → Flink → Kafka
    ///
    /// The Flink job reads from flink_input, capitalizes strings, and writes to flink_output.
    /// </summary>
    static class Program
    {
        private const string InputTopic = "flink_input";
        private const string OutputTopic = "flink_output";
        private const string ConsumerGroup = "baeldung";
        
        // Kafka addresses - read from environment variables set by test infrastructure
        // KAFKA_BOOTSTRAP_SERVERS: For host-to-container communication (producer/consumer operations from this exercise)
        // KAFKA_FLINK_BOOTSTRAP_SERVERS: For container-to-container communication (Flink job Kafka connectivity)
        // Lazy evaluation - reads env var when first accessed, not at class load time
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
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
            Console.WriteLine("  Exercise 1: String Stream Processing (Capitalize)");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine("  Reference: Section 6 of Baeldung Tutorial");
            Console.WriteLine("  https://www.baeldung.com/kafka-flink-data-pipeline");
            Console.WriteLine();
            Console.WriteLine("  This exercise demonstrates:");
            Console.WriteLine("  - Creating Kafka consumers and producers");
            Console.WriteLine("  - Submitting Flink jobs for stream transformation");
            Console.WriteLine("  - Capitalizing strings using Flink map operation");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                await RunCapitalizeDemo();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing capitalize demo");
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.Exit(1);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Main demo following Baeldung Section 6: Capitalize() method
        /// Steps: Submit Flink job → Produce data → Consume results
        /// </summary>
        static async Task RunCapitalizeDemo()
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

                Console.WriteLine(">> Step 4/6: Submitting Flink capitalize job...");
                jobClient = await SubmitCapitalizeJob();
                await Task.Delay(3000); // Wait for job to start
                Console.WriteLine();

                Console.WriteLine(">> Step 5/6: Producing lowercase messages to input topic...");
                await ProduceMessages();
                await Task.Delay(2000); // Wait for Flink to process
                Console.WriteLine();

                Console.WriteLine(">> Step 6/6: Consuming capitalized results from output topic...");
                await ConsumeResults();
                Console.WriteLine();

                Console.WriteLine("================================================================================");
                Console.WriteLine("  EXERCISE 1 COMPLETED!");
                Console.WriteLine("================================================================================");
                Console.WriteLine();
                Console.WriteLine("What you learned (Baeldung Section 6):");
                Console.WriteLine("  [OK] Created Kafka consumer configuration (CreateConsumerConfig)");
                Console.WriteLine("  [OK] Created Kafka producer configuration (CreateProducerConfig)");
                Console.WriteLine("  [OK] Submitted Flink job with map transformation (uppercase)");
                Console.WriteLine("  [OK] Verified end-to-end pipeline: Input -> Transform -> Output");
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
        /// Submit Flink job with uppercase transformation (Baeldung Section 6)
        ///
        /// Baeldung Java API:
        ///   StreamExecutionEnvironment environment = StreamExecutionEnvironment.getExecutionEnvironment();
        ///   DataStream String stringInputStream = environment.addSource(flinkKafkaConsumer);
        ///   stringInputStream
        ///     .map(new WordsCapitalizer())
        ///     .addSink(flinkKafkaProducer);
        ///
        /// FlinkDotNet equivalent (exact match):
        ///   var environment = StreamExecutionEnvironment.GetExecutionEnvironment();
        ///   var stringInputStream = environment.FromKafka(topic, servers, groupId);
        ///   stringInputStream
        ///     .Map(new WordsCapitalizer())  // Same as Java: new WordsCapitalizer()
        ///     .SinkToKafka(outputTopic, servers);
        ///   await environment.ExecuteAsync("string-capitalize-pipeline");
        /// </summary>
        static async Task<FlinkDotNet.DataStream.IJobClient> SubmitCapitalizeJob()
        {
            Console.WriteLine($"   Creating Flink job using native FlinkDotNet API...");
            Console.WriteLine($"   - Input Topic: {InputTopic}");
            Console.WriteLine($"   - Transformation: Uppercase (WordsCapitalizer)");
            Console.WriteLine($"   - Output Topic: {OutputTopic}");

            // Baeldung Section 6: public static void capitalize()
            var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Create Kafka source (equivalent to FlinkKafkaConsumer)
            // Note: Use kafka:9092 for Flink job (internal Docker network)
            // NOT localhost:9093 which is for host machine access
            var stringInputStream = environment.FromKafka(
                topic: InputTopic,
                bootstrapServers: KafkaFlinkBootstrapServers,  // Use container IP discovered by test infrastructure
                groupId: ConsumerGroup,
                startingOffsets: "earliest"
            );

            // Apply map transformation and add Kafka sink
            // Using WordsCapitalizer class - exact match to Baeldung Java API
            stringInputStream
                .Map(new WordsCapitalizer())
                .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);  // Use container IP discovered by test infrastructure

            // Execute the job and get JobClient for lifecycle management
            var jobClient = await environment.ExecuteAsync("string-capitalize-pipeline");

            Console.WriteLine($"   [SUCCESS] Flink job submitted successfully");
            Console.WriteLine($"   JobId: {jobClient.GetJobId()}");
            
            return jobClient;
        }

        /// <summary>
        /// Produce test messages (lowercase) to input topic
        /// Following Baeldung Section 6 example
        /// </summary>
        static async Task ProduceMessages()
        {
            var producerConfig = CreateProducerConfig(KafkaBootstrapServers);
            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            const int messageCount = 50;
            Console.WriteLine($"   Producing {messageCount} lowercase messages...");

            for (int i = 0; i < messageCount; i++)
            {
                var message = new Message<string, string>
                {
                    Key = $"key-{i}",
                    Value = $"message {i}"  // Lowercase input
                };

                try
                {
                    var deliveryReport = await producer.ProduceAsync(InputTopic, message);
                    
                    if (i % 10 == 0 || i == messageCount - 1)
                    {
                        Console.WriteLine($"   [{i + 1:D3}/{messageCount}] Sent: \"{message.Value}\" -> Partition {deliveryReport.Partition}");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"   [ERROR] Failed to produce message {i}: {ex.Error.Reason}");
                }

                await Task.Delay(50); // Small delay for observability
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"   [SUCCESS] All {messageCount} messages produced to '{InputTopic}'");
        }

        /// <summary>
        /// Consume capitalized results from output topic
        /// Verifies Flink transformation worked (lowercase -> UPPERCASE)
        /// </summary>
        static async Task ConsumeResults()
        {
            var consumerConfig = CreateConsumerConfig(KafkaBootstrapServers, $"consumer-{Guid.NewGuid()}");
            using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"   Consuming from '{OutputTopic}' (max 30 seconds)...");

            var (consumedMessages, capitalizedCount, partitionCounts) = ConsumeMessagesFromKafka(consumer);

            await ValidateConsumptionResults(consumedMessages, capitalizedCount, partitionCounts);
        }

        /// <summary>
        /// Consume messages from Kafka and track statistics
        /// </summary>
        static (int consumedMessages, int capitalizedCount, Dictionary<int, int> partitionCounts)
            ConsumeMessagesFromKafka(IConsumer<string, string> consumer)
        {
            var consumedMessages = 0;
            var capitalizedCount = 0;
            var partitionCounts = new Dictionary<int, int>();
            var allMessages = new List<(int partition, string value)>();
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(60);

            try
            {
                while (stopwatch.Elapsed < timeout && consumedMessages < 50)
                {
                    var result = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (result != null)
                    {
                        ProcessConsumedMessage(result, ref consumedMessages, ref capitalizedCount, partitionCounts, allMessages);
                        consumer.Commit(result);
                    }
                    else if (consumedMessages > 0)
                    {
                        Console.WriteLine("   No new messages - consumption complete");
                        break;
                    }
                }
                
                // Print first 5 and last 5 messages after consumption completes
                PrintMessageSummary(allMessages);
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

            return (consumedMessages, capitalizedCount, partitionCounts);
        }

        /// <summary>
        /// Process a single consumed message and update statistics
        /// </summary>
        static void ProcessConsumedMessage(
            ConsumeResult<string, string> result,
            ref int consumedMessages,
            ref int capitalizedCount,
            Dictionary<int, int> partitionCounts,
            List<(int partition, string value)> allMessages)
        {
            consumedMessages++;
            var partition = result.Partition.Value;
            
            // Print partition header when we encounter a new partition
            if (!partitionCounts.ContainsKey(partition))
            {
                partitionCounts[partition] = 0;
                Console.WriteLine($"   --- Partition {partition} ---");
            }
            partitionCounts[partition]++;
            
            // Store all messages for later display
            allMessages.Add((partition, result.Message.Value));
            
            // Verify capitalization
            bool isUppercase = result.Message.Value == result.Message.Value.ToUpperInvariant();
            if (isUppercase)
            {
                capitalizedCount++;
            }
        }
        
        /// <summary>
        /// Print first 5 and last 5 messages summary
        /// </summary>
        static void PrintMessageSummary(List<(int partition, string value)> allMessages)
        {
            if (allMessages.Count == 0)
                return;
                
            Console.WriteLine();
            Console.WriteLine("   Message Summary (First 5 and Last 5):");
            Console.WriteLine("   " + new string('-', 60));
            
            // Group by partition for display
            var byPartition = allMessages.GroupBy(m => m.partition).OrderBy(g => g.Key);
            
            foreach (var partitionGroup in byPartition)
            {
                var messages = partitionGroup.ToList();
                var partition = partitionGroup.Key;
                
                Console.WriteLine($"   Partition {partition}: {messages.Count} messages");
                
                // Show first 5
                var first5 = messages.Take(5).ToList();
                for (int i = 0; i < first5.Count; i++)
                {
                    Console.WriteLine($"     [{i + 1:D2}] {first5[i].value}");
                }
                
                // Show "..." if there are more than 10 messages
                if (messages.Count > 10)
                {
                    Console.WriteLine($"     ... ({messages.Count - 10} more messages) ...");
                }
                
                // Show last 5 (if different from first 5)
                if (messages.Count > 5)
                {
                    var last5 = messages.Skip(Math.Max(0, messages.Count - 5)).ToList();
                    for (int i = 0; i < last5.Count; i++)
                    {
                        Console.WriteLine($"     [{messages.Count - last5.Count + i + 1:D2}] {last5[i].value}");
                    }
                }
            }
            
            Console.WriteLine("   " + new string('-', 60));
        }

        /// <summary>
        /// Validate consumption results and throw errors if validation fails
        /// </summary>
        static async Task ValidateConsumptionResults(
            int consumedMessages,
            int capitalizedCount,
            Dictionary<int, int> partitionCounts)
        {
            if (consumedMessages > 0)
            {
                Console.WriteLine($"   [SUCCESS] Consumed {consumedMessages}/50 messages ({capitalizedCount} capitalized)");
                Console.WriteLine($"   Partition distribution: {string.Join(", ", partitionCounts.OrderBy(kv => kv.Key).Select(kv => $"P{kv.Key}={kv.Value}"))}");
                
                if (consumedMessages < 50)
                {
                    Console.WriteLine($"   [WARNING] Only {consumedMessages}/50 messages received - some messages may still be processing");
                }
                
                if (capitalizedCount != consumedMessages)
                {
                    throw new InvalidOperationException($"Capitalization validation failed: {capitalizedCount}/{consumedMessages} messages were capitalized");
                }
            }
            else
            {
                await HandleNoMessagesConsumed();
            }
        }

        /// <summary>
        /// Handle the case when no messages were consumed
        /// </summary>
        static async Task HandleNoMessagesConsumed()
        {
            Console.WriteLine($"   [ERROR] No messages consumed - Flink job may not be running");
            Console.WriteLine($"   Checking Flink TaskManager logs for diagnostics...");
            Console.WriteLine();
            
            await PrintTaskManagerLogsAsync();
            
            Console.WriteLine();
            throw new InvalidOperationException("No messages consumed from output topic. Flink job may not be processing data correctly.");
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
                
                // Exponential backoff: 1s, 2s, 3s, 4s, 5s (max)
                retryDelay = Math.Min(retryDelay + 1000, 5000);
            }

            throw new TimeoutException(
                $"Flink cluster not healthy within {timeout.TotalSeconds} seconds. " +
                $"Verify Flink JobManager is running and accessible at {FlinkJobManagerUrl}");
        }
    }

    /// <summary>
    /// WordsCapitalizer MapFunction (Baeldung Section 6)
    /// Implements MapFunction&lt;String, String&gt; to uppercase strings
    ///
    /// Java equivalent:
    /// public class WordsCapitalizer implements MapFunction&lt;String, String&gt; {
    ///     @Override
    ///     public String map(String s) {
    ///         return s.toUpperCase();
    ///     }
    /// }
    /// </summary>
    public class WordsCapitalizer : FlinkDotNet.DataStream.IMapFunction<string, string>
    {
        public string Map(string s)
        {
            return s.ToUpperInvariant();
        }
    }
}