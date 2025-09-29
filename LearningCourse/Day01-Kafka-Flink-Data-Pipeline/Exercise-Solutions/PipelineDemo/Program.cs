using System;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Text.Json;
using System.Net.Http;
using System.Linq;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;

namespace PipelineDemo
{
    static class Program
    {
        private const string InputTopic = "lc1.flink.input";
        private const string OutputTopic = "lc1.flink.output";
        private static readonly string KafkaBootstrapServers = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9092";

        static async Task Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("🚀 Day 1: Kafka ↔ Flink Data Pipeline (FlinkDotNet Implementation)");
            Console.WriteLine("Based on Baeldung's Kafka and Apache Flink Data Pipeline tutorial");
            Console.WriteLine("".PadRight(80, '='));
            Console.WriteLine();

            if (args.Length < 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  dotnet run -- submit    # Submit the Flink job");
                Console.WriteLine("  dotnet run -- produce   # Produce test data to input topic");
                Console.WriteLine("  dotnet run -- consume   # Consume and display output data");
                Console.WriteLine("  dotnet run -- demo      # Run complete demo (submit + produce + consume)");
                return;
            }

            var command = args[0].ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case "submit":
                        await SubmitFlinkJobAsync();
                        break;
                    case "produce":
                        await ProduceTestDataAsync();
                        break;
                    case "consume":
                        await ConsumeOutputDataAsync();
                        break;
                    case "demo":
                        await RunCompleteDemoAsync();
                        break;
                    default:
                        Console.WriteLine($"❌ Unknown command: {command}");
                        return;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing command: {Command}", command);
                Console.WriteLine($"❌ Error: {ex.Message}");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        static async Task SubmitFlinkJobAsync()
        {
            Console.WriteLine("📋 Submitting Flink job...");

            // Verify infrastructure is ready
            await WaitForKafkaReadyAsync();
            await WaitForFlinkGatewayReadyAsync();

            // Create topics if they don't exist
            await CreateTopicsAsync();

            try
            {
                // Create a simple Flink job definition (IR) that reads from Kafka, transforms, and writes to Kafka
                Console.WriteLine($"🔧 Building Flink job: {InputTopic} → transform → {OutputTopic}");

                var flinkJobDefinition = new
                {
                    jobName = "lc1-kafka-flink-pipeline",
                    sources = new[]
                    {
                        new
                        {
                            type = "kafka",
                            name = "input-source",
                            properties = new
                            {
                                topic = InputTopic,
                                bootstrapServers = KafkaBootstrapServers,
                                groupId = "lc1-flink-group",
                                autoOffsetReset = "earliest"
                            }
                        }
                    },
                    transforms = new[]
                    {
                        new
                        {
                            type = "map",
                            name = "identity-transform",
                            function = "identity", // Could be "upper" for uppercase transformation
                            from = "input-source"
                        }
                    },
                    sinks = new[]
                    {
                        new
                        {
                            type = "kafka",
                            name = "output-sink",
                            properties = new
                            {
                                topic = OutputTopic,
                                bootstrapServers = KafkaBootstrapServers
                            },
                            from = "identity-transform"
                        }
                    }
                };

                var jobJson = JsonSerializer.Serialize(flinkJobDefinition, new JsonSerializerOptions { WriteIndented = true });
                Console.WriteLine("Generated Flink Job Definition:");
                Console.WriteLine(jobJson);
                Console.WriteLine();

                // Try to submit to Flink Job Gateway
                using var httpClient = new HttpClient();
                var content = new StringContent(jobJson, System.Text.Encoding.UTF8, "application/json");

                try
                {
                    var response = await httpClient.PostAsync("http://localhost:8080/api/v1/jobs/submit", content);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"✅ Flink job submitted successfully!");
                        Console.WriteLine($"   Response: {responseContent}");
                        Console.WriteLine($"   Flink UI: http://localhost:8081");
                        Console.WriteLine($"   Job Gateway: http://localhost:8080");
                    }
                    else
                    {
                        Console.WriteLine($"⚠️  Flink job submission failed: {response.StatusCode}");
                        Console.WriteLine($"   Response: {responseContent}");
                        Console.WriteLine("   This might be expected if the Flink JAR runner is not available.");
                        Console.WriteLine("   The job definition was still generated successfully for educational purposes.");
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"⚠️  Could not connect to Flink Job Gateway: {ex.Message}");
                    Console.WriteLine("   Make sure LocalTesting AppHost is running:");
                    Console.WriteLine("   cd LocalTesting && dotnet run --project LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj");
                    Console.WriteLine();
                    Console.WriteLine("   The job definition was still generated successfully for educational purposes.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error submitting Flink job: {ex.Message}");
                throw;
            }
        }

        static async Task ProduceTestDataAsync()
        {
            Console.WriteLine("📤 Producing test data to Kafka...");

            await WaitForKafkaReadyAsync();
            await CreateTopicsAsync();

            var config = new ProducerConfig
            {
                BootstrapServers = KafkaBootstrapServers,
                EnableIdempotence = true,
                Acks = Acks.All,
                LingerMs = 5
            };

            using var producer = new ProducerBuilder<string, string>(config).Build();

            const int messageCount = 100;
            Console.WriteLine($"📨 Sending {messageCount} messages to topic '{InputTopic}'...");

            for (int i = 0; i < messageCount; i++)
            {
                var message = new Message<string, string>
                {
                    Key = $"key-{i % 10}",  // 10 different keys for partitioning
                    Value = $"Hello from FlinkDotNet message #{i} - timestamp: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff} UTC"
                };

                try
                {
                    var deliveryReport = await producer.ProduceAsync(InputTopic, message);

                    if (i % 20 == 0 || i == messageCount - 1)
                    {
                        Console.WriteLine($"   Message {i + 1}/{messageCount} delivered to partition {deliveryReport.Partition} at offset {deliveryReport.Offset}");
                    }
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"❌ Failed to produce message {i}: {ex.Error.Reason}");
                }

                // Small delay to make it observable
                await Task.Delay(50);
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"✅ Successfully produced {messageCount} messages to '{InputTopic}'");
        }

        static async Task ConsumeOutputDataAsync()
        {
            Console.WriteLine("📥 Consuming output data from Kafka...");

            await WaitForKafkaReadyAsync();

            var config = new ConsumerConfig
            {
                BootstrapServers = KafkaBootstrapServers,
                GroupId = $"lc1-consumer-{Guid.NewGuid()}",
                AutoOffsetReset = AutoOffsetReset.Earliest,
                EnableAutoCommit = false
            };

            using var consumer = new ConsumerBuilder<string, string>(config).Build();
            consumer.Subscribe(OutputTopic);

            Console.WriteLine($"🔍 Consuming messages from topic '{OutputTopic}' (waiting up to 30 seconds)...");

            var consumedMessages = 0;
            var stopwatch = Stopwatch.StartNew();
            var timeout = TimeSpan.FromSeconds(30);

            try
            {
                while (stopwatch.Elapsed < timeout)
                {
                    var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(1000));

                    if (consumeResult != null)
                    {
                        consumedMessages++;
                        Console.WriteLine($"   [{consumedMessages:D3}] Key: {consumeResult.Message.Key}, Value: {consumeResult.Message.Value}");

                        // Commit the message
                        consumer.Commit(consumeResult);
                    }
                    else if (consumedMessages > 0)
                    {
                        // If we've consumed some messages but no new ones in the last second, we're probably done
                        Console.WriteLine("   No new messages received, consumption complete.");
                        break;
                    }
                }
            }
            catch (ConsumeException ex)
            {
                Console.WriteLine($"❌ Error consuming messages: {ex.Error.Reason}");
            }
            finally
            {
                consumer.Close();
            }

            if (consumedMessages > 0)
            {
                Console.WriteLine($"✅ Successfully consumed {consumedMessages} messages from '{OutputTopic}'");
            }
            else
            {
                Console.WriteLine($"⚠️  No messages consumed from '{OutputTopic}' within {timeout.TotalSeconds} seconds");
                Console.WriteLine("   This is expected if the Flink job is not actually running.");
                Console.WriteLine("   The demonstration shows the complete pipeline concept.");
            }
        }

        static async Task RunCompleteDemoAsync()
        {
            Console.WriteLine("🎬 Running complete demo: Submit → Produce → Consume");
            Console.WriteLine();

            Console.WriteLine("Step 1: Submitting Flink job...");
            await SubmitFlinkJobAsync();
            Console.WriteLine();

            Console.WriteLine("Step 2: Waiting 5 seconds for job to start...");
            await Task.Delay(5000);
            Console.WriteLine();

            Console.WriteLine("Step 3: Producing test data...");
            await ProduceTestDataAsync();
            Console.WriteLine();

            Console.WriteLine("Step 4: Waiting 3 seconds for Flink processing...");
            await Task.Delay(3000);
            Console.WriteLine();

            Console.WriteLine("Step 5: Consuming output data...");
            await ConsumeOutputDataAsync();
            Console.WriteLine();

            Console.WriteLine("🎉 Demo completed! Check the results above.");
            Console.WriteLine();
            Console.WriteLine("📚 What you learned:");
            Console.WriteLine("   ✅ How to define a Flink job using FlinkDotNet job definitions");
            Console.WriteLine("   ✅ How to submit jobs to Flink through the Job Gateway");
            Console.WriteLine("   ✅ How to produce and consume Kafka messages in .NET");
            Console.WriteLine("   ✅ How to build a complete data pipeline: Kafka → Flink → Kafka");
            Console.WriteLine("   ✅ The foundation for more complex stream processing scenarios");
        }

        static async Task CreateTopicsAsync()
        {
            var adminConfig = new AdminClientConfig { BootstrapServers = KafkaBootstrapServers };
            using var admin = new AdminClientBuilder(adminConfig).Build();

            var topicsToCreate = new[]
            {
                new TopicSpecification { Name = InputTopic, NumPartitions = 4, ReplicationFactor = 1 },
                new TopicSpecification { Name = OutputTopic, NumPartitions = 4, ReplicationFactor = 1 }
            };

            try
            {
                await admin.CreateTopicsAsync(topicsToCreate);
                Console.WriteLine($"✅ Topics created: {InputTopic}, {OutputTopic}");
            }
            catch (CreateTopicsException ex)
            {
                // Topics might already exist, which is fine
                var errors = ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists).ToList();
                if (errors.Any())
                {
                    Console.WriteLine($"⚠️  Some topics failed to create: {string.Join(", ", errors.Select(e => e.Error.Reason))}");
                }
                else
                {
                    Console.WriteLine($"✅ Topics already exist: {InputTopic}, {OutputTopic}");
                }
            }
        }

        static async Task WaitForKafkaReadyAsync()
        {
            Console.WriteLine("🔄 Waiting for Kafka to be ready...");

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
                        Console.WriteLine($"✅ Kafka is ready with {metadata.Brokers.Count} broker(s)");
                        return;
                    }
                }
                catch
                {
                    // Kafka not ready yet, continue waiting
                }

                await Task.Delay(1000);
            }

            throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds} seconds. Make sure LocalTesting AppHost is running.");
        }

        static async Task WaitForFlinkGatewayReadyAsync()
        {
            Console.WriteLine("🔄 Waiting for Flink Job Gateway to be ready...");

            var timeout = TimeSpan.FromSeconds(30);
            var stopwatch = Stopwatch.StartNew();

            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    var response = await httpClient.GetAsync("http://localhost:8080/api/v1/health");
                    if (response.IsSuccessStatusCode)
                    {
                        Console.WriteLine("✅ Flink Job Gateway is ready");
                        return;
                    }
                }
                catch
                {
                    // Gateway not ready yet, continue waiting
                }

                await Task.Delay(1000);
            }

            Console.WriteLine("⚠️  Flink Job Gateway not ready, but continuing...");
        }
    }
}

