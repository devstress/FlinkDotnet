using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Flink.JobBuilder.Models;
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
        private const string InputTopic = "sample_input";
        private const string OutputTopic = "sample_output";
        private const string ConsumerGroup = "sample-app";
        private const string Separator = "================================================================================";

        /// <summary>
        /// Environment variables for service discovery
        /// </summary>
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";

        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";

        private static string FlinkJobGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

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
        /// Public method for integration tests to run SampleApp and get the job ID
        /// </summary>
        public static async Task<string> RunAsync()
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            try
            {
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

            JobDefinition jobDefinition = environment.GetJobDefinition("sample-uppercase-job");

            Console.WriteLine($"   Submitting job to FlinkDotNet JobGateway at {FlinkJobGatewayUrl}...");

            using HttpClient httpClient = new()
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            // Ensure URL doesn't have double slashes when combining
            string gatewayBaseUrl = FlinkJobGatewayUrl.TrimEnd('/');
            HttpResponseMessage response = await httpClient.PostAsJsonAsync($"{gatewayBaseUrl}/api/v1/jobs/submit", jobDefinition);

            if (!response.IsSuccessStatusCode)
            {
                string errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Job submission failed: {response.StatusCode} - {errorContent}");
            }

            JobSubmissionResponse? result = await response.Content.ReadFromJsonAsync<JobSubmissionResponse>();

            if (result?.JobId == null)
            {
                throw new InvalidOperationException("Job submission succeeded but no JobId returned");
            }

            Console.WriteLine($"   [SUCCESS] Job submitted with ID: {result.JobId}");
            return result.JobId;
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

    public class JobSubmissionResponse
    {
        public string? JobId
        {
            get; set;
        }
        public string? Status
        {
            get; set;
        }
    }
}
