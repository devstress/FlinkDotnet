using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Serilog;
using FlinkDotNet.DataStream;

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

        // Environment variables for service discovery
        private static string KafkaBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:9093";
        
        private static string KafkaFlinkBootstrapServers =>
            Environment.GetEnvironmentVariable("KAFKA_FLINK_BOOTSTRAP_SERVERS") ?? "kafka:9092";
        
        private static string FlinkJobGatewayUrl =>
            Environment.GetEnvironmentVariable("FLINK_JOB_GATEWAY_URL") ?? "http://localhost:8086";

        public static async Task<string> Main(string[] args)
        {
            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .CreateLogger();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  SampleApp: FlinkDotNet JobGateway Job Submission Demo");
            Console.WriteLine("================================================================================");
            Console.WriteLine();
            Console.WriteLine($"  FlinkDotNet JobGateway URL: {FlinkJobGatewayUrl}");
            Console.WriteLine($"  Kafka Bootstrap Servers: {KafkaBootstrapServers}");
            Console.WriteLine();
            Console.WriteLine("================================================================================");
            Console.WriteLine();

            try
            {
                var jobId = await RunSampleJobAsync();
                return jobId;
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

        private static async Task<string> RunSampleJobAsync()
        {
            Console.WriteLine(">> Step 1/3: Creating Kafka topics...");
            await CreateTopicsAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 2/3: Submitting Flink job via FlinkDotNet JobGateway...");
            var jobId = await SubmitJobToGatewayAsync();
            Console.WriteLine();

            Console.WriteLine(">> Step 3/3: Producing test messages...");
            await ProduceMessagesAsync();
            Console.WriteLine();

            Console.WriteLine("================================================================================");
            Console.WriteLine("  SAMPLEAPP COMPLETED SUCCESSFULLY!");
            Console.WriteLine($"  Job ID: {jobId}");
            Console.WriteLine("================================================================================");
            
            return jobId;
        }

        private static async Task<string> SubmitJobToGatewayAsync()
        {
            Console.WriteLine("   Creating job definition...");
            
            var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

            var stringInputStream = environment.FromKafka(
                topic: InputTopic,
                bootstrapServers: KafkaFlinkBootstrapServers,
                groupId: ConsumerGroup,
                startingOffsets: "earliest"
            );

            stringInputStream
                .Map(new UppercaseMapper())
                .SinkToKafka(OutputTopic, KafkaFlinkBootstrapServers);

            var jobDefinition = environment.GetJobDefinition("sample-uppercase-job");

            Console.WriteLine($"   Submitting job to FlinkDotNet JobGateway at {FlinkJobGatewayUrl}...");
            
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var response = await httpClient.PostAsJsonAsync($"{FlinkJobGatewayUrl}/api/v1/jobs/submit", jobDefinition);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new HttpRequestException($"Job submission failed: {response.StatusCode} - {errorContent}");
            }

            var result = await response.Content.ReadFromJsonAsync<JobSubmissionResponse>();
            
            if (result?.JobId == null)
            {
                throw new InvalidOperationException("Job submission succeeded but no JobId returned");
            }

            Console.WriteLine($"   [SUCCESS] Job submitted with ID: {result.JobId}");
            return result.JobId;
        }

        private static async Task ProduceMessagesAsync()
        {
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = KafkaBootstrapServers,
                EnableIdempotence = true,
                Acks = Acks.All
            };

            using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

            const int messageCount = 20;
            Console.WriteLine($"   Producing {messageCount} lowercase messages...");

            for (int i = 0; i < messageCount; i++)
            {
                var message = new Message<string, string>
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
            var adminConfig = new AdminClientConfig { BootstrapServers = KafkaBootstrapServers };
            using var admin = new AdminClientBuilder(adminConfig).Build();

            var topicsToCreate = new[]
            {
                new TopicSpecification { Name = InputTopic, NumPartitions = 2, ReplicationFactor = 1 },
                new TopicSpecification { Name = OutputTopic, NumPartitions = 2, ReplicationFactor = 1 }
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
                    Console.WriteLine($"   [SUCCESS] Topics already exist");
                }
            }
        }
    }

    public class UppercaseMapper : IMapFunction<string, string>
    {
        public string Map(string value)
        {
            return value.ToUpperInvariant();
        }
    }

    public class JobSubmissionResponse
    {
        public string? JobId { get; set; }
        public string? Status { get; set; }
    }
}
