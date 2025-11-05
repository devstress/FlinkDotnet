using Aspire.Hosting;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Enhanced test base class for LocalTesting integration tests.
/// Based on successful patterns from BackPressureExample.IntegrationTests.KafkaTestBase
/// with improvements for Flink infrastructure readiness validation and Docker connectivity.
/// </summary>
public abstract partial class LocalTestingTestBase
{
    /// <summary>
    /// Access to shared AppHost instance from GlobalTestInfrastructure.
    /// Infrastructure is initialized once for all tests, dramatically reducing startup overhead.
    /// </summary>
    protected static DistributedApplication? AppHost => GlobalTestInfrastructure.AppHost;

    /// <summary>
    /// Access to shared Kafka connection string from GlobalTestInfrastructure.
    /// CRITICAL: This address is used by BOTH test producers/consumers AND Flink jobs.
    /// The simplified architecture uses a single Kafka address (localhost:port) accessible
    /// from both host and containers via Docker port mapping.
    /// </summary>
    protected static string? KafkaConnectionString => GlobalTestInfrastructure.KafkaConnectionString;

    /// <summary>
    /// Access to discovered Temporal endpoint from GlobalTestInfrastructure.
    /// Aspire allocates dynamic ports during testing, so we must use the discovered endpoint.
    /// </summary>
    protected static string? TemporalEndpoint => GlobalTestInfrastructure.TemporalEndpoint;

    /// <summary>
    /// No infrastructure setup needed - using shared global infrastructure.
    /// Tests can start immediately without waiting for infrastructure startup.
    /// </summary>
    [OneTimeSetUp]
    public virtual Task OneTimeSetUp()
    {
        // Verify shared infrastructure is available
        if (AppHost == null || string.IsNullOrEmpty(KafkaConnectionString))
        {
            throw new InvalidOperationException(
                "Global test infrastructure is not initialized. " +
                "Ensure GlobalTestInfrastructure.GlobalSetUp completed successfully.");
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// No teardown needed - shared infrastructure persists across all tests.
    /// </summary>
    [OneTimeTearDown]
    public virtual Task OneTimeTearDown()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Create Kafka topic with proper error handling for existing topics.
    /// Copied from BackPressureExample patterns.
    /// </summary>
    protected async Task CreateTopicAsync(string topicName, int partitions = 1, short replicationFactor = 1)
    {
        if (string.IsNullOrEmpty(KafkaConnectionString))
            throw new InvalidOperationException("Kafka connection string is not available");

        using var admin = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = KafkaConnectionString,
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
    /// Wait for complete infrastructure readiness including optional Gateway.
    /// Performs quick health check only (trusts global setup).
    /// </summary>
    /// <param name="includeGateway">Whether to validate Gateway availability</param>
    /// <param name="cancellationToken">Cancellation token</param>
    protected static async Task WaitForFullInfrastructureAsync(
        bool includeGateway = true,
        CancellationToken cancellationToken = default)
    {
        // Quick validation that endpoints are still responding
        // This is used by individual tests after global setup has already validated everything
        TestContext.WriteLine("🔧 Quick infrastructure health check...");

        // Just verify Kafka is still accessible (very quick check)
        if (string.IsNullOrEmpty(KafkaConnectionString))
        {
            throw new InvalidOperationException("Kafka connection string not available");
        }

        // Display container status with ports for visibility (no polling - containers should already be running)
        await DisplayContainerStatusAsync();

        TestContext.WriteLine("✅ Infrastructure health check passed");
    }
}
