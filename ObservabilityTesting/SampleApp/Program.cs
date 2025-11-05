using System;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using FlinkDotNet.DataStream;
using Serilog;

namespace SampleApp
{
    /// <summary>
    /// SampleApp: Demonstrates FlinkDotNet JobGateway discovery and job submission
    /// Based on Exercise 1 from LearningCourse Day01
    /// </summary>
    public static class Program
    {
        private const string DefaultInputTopic = "sample_input";
        private const string DefaultOutputTopic = "sample_output";
        private const string ConsumerGroup = "sample-app";
        private const string Separator = "================================================================================";

        /// <summary>
        /// Environment variables for service discovery and topic configuration
        /// </summary>
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";

        /// <summary>
        /// Use kafka:9093 for container-to-container communication (PLAINTEXT_INTERNAL listener).
        /// This matches LocalTesting and LearningCourse Day 01 configuration.
        /// Port 9092 is for host access, port 9093 is for container-to-container.
        /// </summary>
        private static string KafkaFlinkBootstrapServers => "kafka:9093";

        private static string FlinkJobGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

        private static string InputTopic =>
            Environment.GetEnvironmentVariable("SAMPLE_APP_INPUT_TOPIC") ?? DefaultInputTopic;

        private static string OutputTopic =>
            Environment.GetEnvironmentVariable("SAMPLE_APP_OUTPUT_TOPIC") ?? DefaultOutputTopic;

        public static async Task Main()
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine(Separator);
            Console.WriteLine("  SampleApp: FlinkDotNet JobGateway Job Submission Demo");
            Console.WriteLine(Separator);
            Console.WriteLine();
            Console.WriteLine($"  FlinkDotNet JobGateway URL: {FlinkJobGatewayUrl}");
            Console.WriteLine($"  Kafka Bootstrap Servers: {KafkaBootstrapServers}");
            Console.WriteLine($"  Input Topic: {InputTopic}");
            Console.WriteLine($"  Output Topic: {OutputTopic}");
            Console.WriteLine();
            Console.WriteLine(Separator);
            Console.WriteLine();

            try
            {
                string jobId = await RunSampleJobAsync();
                Console.WriteLine($"Completed with Job ID: {jobId}");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing sample job");
                Console.WriteLine($"ERROR: {ex.Message}");
                Environment.ExitCode = 1;
                // Rethrow with context
                throw new InvalidOperationException("SampleApp execution failed. See log for details.", ex);
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        /// <summary>
        /// Public method for integration tests to run SampleApp and get the job ID.
        /// Accepts optional topic names for test isolation.
        /// </summary>
        public static async Task<string> RunAsync(string? inputTopic = null, string? outputTopic = null)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
                // Override environment variables if topics are provided
                if (inputTopic != null)
                {
                    Environment.SetEnvironmentVariable("SAMPLE_APP_INPUT_TOPIC", inputTopic);
                }
                if (outputTopic != null)
                {
                    Environment.SetEnvironmentVariable("SAMPLE_APP_OUTPUT_TOPIC", outputTopic);
                }

                return await RunSampleJobAsync();
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }

        private static async Task<string> RunSampleJobAsync()
        {
            Console.WriteLine(">> Step 1/3: Creating Kafka topics...");
            await CreateTopicsAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 2/3: Submitting Flink job via FlinkDotNet JobGateway...");
            string jobId = await SubmitJobToGatewayAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 3/3: Producing test messages...");
            await ProduceMessagesAsync();
            Console.WriteLine();

            Console.WriteLine(Separator);
            Console.WriteLine("  SAMPLEAPP COMPLETED SUCCESSFULLY!");
            Console.WriteLine($"  Job ID: {jobId}");
            Console.WriteLine(Separator);

            return jobId;
        }

        private static async Task<string> SubmitJobToGatewayAsync()
        {
            Console.WriteLine("   Creating job definition...");

            StreamExecutionEnvironment environment = StreamExecutionEnvironment.GetExecutionEnvironment();

            DataStream<string> stringInputStream = environment.FromKafka(
                topic: InputTopic,
                bootstrapServers: KafkaFlinkBootstrapServers,
                groupId: ConsumerGroup,
                startingOffsets: "earliest"
            );

            stringInputStream
                .Map(new UppercaseMapper())
                .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);

            Console.WriteLine($"   Submitting job to FlinkDotNet JobGateway at {FlinkJobGatewayUrl}...");

            // Use ExecuteAsync which internally submits to the JobGateway
            // This approach works with all FlinkDotnet package versions
            IJobClient jobClient = await environment.ExecuteAsync("sample-uppercase-job");

            string? jobId = jobClient.GetJobId();

            if (string.IsNullOrEmpty(jobId))
            {
                throw new InvalidOperationException("Job submission succeeded but no JobId returned");
            }

            Console.WriteLine($"   [SUCCESS] Job submitted with ID: {jobId}");
            return jobId;
        }

        private static async Task ProduceMessagesAsync()
        {
            ProducerConfig producerConfig = new()
            {
                BootstrapServers = KafkaBootstrapServers,
                EnableIdempotence = true,
                Acks = Acks.All
            };

            using IProducer<string, string> producer = new ProducerBuilder<string, string>(producerConfig).Build();

            const int messageCount = 20;
            Console.WriteLine($"   Producing {messageCount} lowercase messages...");

            for (int i = 0; i < messageCount; i++)
            {
                Message<string, string> message = new()
                {
                    Key = $"key-{i}",
                    Value = $"message {i}"
                };

                await producer.ProduceAsync(InputTopic, message);

                if (i % 5 == 0 || i == messageCount - 1)
                {
                    Console.WriteLine($"   [{i + 1:D2}/{messageCount}] Sent: \"{message.Value}\"");
                }

                await Task.Delay(100);
            }

            producer.Flush(TimeSpan.FromSeconds(10));
            Console.WriteLine($"   [SUCCESS] All {messageCount} messages produced");
        }

        private static async Task CreateTopicsAsync()
        {
            AdminClientConfig adminConfig = new()
            {
                BootstrapServers = KafkaBootstrapServers
            };
            using IAdminClient admin = new AdminClientBuilder(adminConfig).Build();

            TopicSpecification[] topicsToCreate =
            [
                new TopicSpecification { Name = InputTopic, NumPartitions = 2, ReplicationFactor = 1 },
                new TopicSpecification { Name = OutputTopic, NumPartitions = 2, ReplicationFactor = 1 }
            ];

            try
            {
                await admin.CreateTopicsAsync(topicsToCreate);
                Console.WriteLine($"   [SUCCESS] Topics created: {InputTopic}, {OutputTopic}");
            }
            catch (CreateTopicsException ex)
            {
                System.Collections.Generic.List<CreateTopicReport> errors = [.. ex.Results.Where(r => r.Error.Code != ErrorCode.TopicAlreadyExists)];
                if (!errors.Any())
                {
                    Console.WriteLine("   [SUCCESS] Topics already exist");
                }
            }
        }
    }

    public class UppercaseMapper : IMapFunction<string, string>
    {
        public string Map(string value) => value.ToUpperInvariant();
    }
}
