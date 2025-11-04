using Confluent.Kafka;
using Confluent.Kafka.Admin;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Kafka topic management and connectivity testing utilities.
/// </summary>
internal static class KafkaHelpers
{
    /// <summary>
    /// Create Kafka topic with proper error handling for existing topics.
    /// Copied from BackPressureExample patterns.
    /// </summary>
    public static async Task CreateTopicAsync(string kafkaConnectionString, string topicName, int partitions = 1, short replicationFactor = 1)
    {
        if (string.IsNullOrEmpty(kafkaConnectionString))
            throw new InvalidOperationException("Kafka connection string is not available");

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = kafkaConnectionString,
            BrokerAddressFamily = BrokerAddressFamily.V4,
            SecurityProtocol = SecurityProtocol.Plaintext
        })
        .SetLogHandler((_, _) => { /* Suppress logs */ })
        .SetErrorHandler((_, _) => { /* Suppress errors */ })
        .Build();

        try
        {
            var topicSpec = new TopicSpecification
            {
                Name = topicName,
                NumPartitions = partitions,
                ReplicationFactor = replicationFactor,
                Configs = new Dictionary<string, string>
                {
                    ["min.insync.replicas"] = "1",
                    ["unclean.leader.election.enable"] = "true"
                }
            };

            await admin.CreateTopicsAsync(new[] { topicSpec });
            TestContext.WriteLine($"✅ Topic '{topicName}' created successfully");

            // Optimized delay for faster test execution
            await Task.Delay(100);
        }
        catch (CreateTopicsException ex)
        {
            if (ex.Results?.Exists(r => r.Error.Code == ErrorCode.TopicAlreadyExists) == true)
            {
                TestContext.WriteLine($"ℹ️ Topic '{topicName}' already exists");
            }
            else
            {
                TestContext.WriteLine($"❌ Error creating topic '{topicName}': {ex.Message}");
                throw;
            }
        }
    }

    /// <summary>
    /// Test Kafka connectivity from within Flink TaskManager container using telnet or nc.
    /// This diagnostic helps determine if Flink containers can reach Kafka at kafka:9092.
    /// </summary>
    public static async Task TestKafkaConnectivityFromFlinkAsync()
    {
        try
        {
            TestContext.WriteLine("🔍 [Kafka Connectivity] Testing from Flink TaskManager container...");

            // Get all container names and filter in C# to handle Aspire's random suffixes
            var containerNames = await DockerUtilities.RunDockerCommandAsync("ps --format \"{{.Names}}\"");
            var containers = containerNames.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var tmName = Array.Find(containers, name => name.Contains("flink-taskmanager", StringComparison.OrdinalIgnoreCase))?.Trim();

            if (string.IsNullOrWhiteSpace(tmName))
            {
                TestContext.WriteLine("⚠️ No TaskManager container found for connectivity test");
                return;
            }

            TestContext.WriteLine($"🐳 Using TaskManager container: {tmName}");

            // Test connectivity to kafka:9092
            var testResult = await DockerUtilities.RunDockerCommandAsync($"exec {tmName} timeout 2 bash -c 'echo \"test\" | nc -w 1 kafka 9092 && echo \"SUCCESS\" || echo \"FAILED\"' 2>&1");
            TestContext.WriteLine($"📊 Kafka connectivity (kafka:9092): {testResult.Trim()}");

            // Also try to resolve the hostname
            var dnsResult = await DockerUtilities.RunDockerCommandAsync($"exec {tmName} getent hosts kafka 2>&1 || echo \"DNS resolution failed\"");
            TestContext.WriteLine($"📊 DNS resolution for 'kafka': {dnsResult.Trim()}");

            // Check if Kafka connector JARs are present
            var connectorCheck = await DockerUtilities.RunDockerCommandAsync($"exec {tmName} ls -lh /opt/flink/lib/*kafka* 2>&1 || echo \"No Kafka connector found\"");
            TestContext.WriteLine($"📊 Kafka connector JARs in Flink:\n{connectorCheck.Trim()}");

            // Check network settings
            var networkInfo = await DockerUtilities.RunDockerCommandAsync($"inspect {tmName} --format '{{{{.NetworkSettings.Networks}}}}'");
            TestContext.WriteLine($"📊 Container network info: {networkInfo.Trim()}");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Failed to test Kafka connectivity from Flink: {ex.Message}");
        }
    }
}
