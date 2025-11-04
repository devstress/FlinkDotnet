using Aspire.Hosting;
using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Enhanced test base class for LocalTesting integration tests.
/// Based on successful patterns from BackPressureExample.IntegrationTests.KafkaTestBase
/// with improvements for Flink infrastructure readiness validation and Docker connectivity.
/// </summary>
public abstract class LocalTestingTestBase
{
    /// <summary>
    /// Access to shared AppHost instance from GlobalTestInfrastructure.
    /// Infrastructure is initialized once for all tests, dramatically reducing startup overhead.
    /// </summary>
    protected static DistributedApplication? AppHost => GlobalTestInfrastructure.AppHost;

    /// <summary>
    /// Access to shared Kafka connection string from GlobalTestInfrastructure.
    /// This address is used by test producers/consumers running on the host (e.g., localhost:32804).
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

        TestContext.WriteLine($"✅ Test class using shared infrastructure (Kafka: {KafkaConnectionString})");
        return Task.CompletedTask;
    }

    /// <summary>
    /// No teardown needed - shared infrastructure persists across all tests.
    /// </summary>
    [OneTimeTearDown]
    public virtual Task OneTimeTearDown()
    {
        TestContext.WriteLine("✅ Test class completed (shared infrastructure remains active)");
        return Task.CompletedTask;
    }

    // ========== Delegating methods to helper classes ==========
    // These methods delegate to the helper classes for readability and maintainability

    /// <summary>
    /// Wait for Kafka to be ready. Delegates to ReadinessChecks helper.
    /// </summary>
    public static Task WaitForKafkaReadyAsync(string bootstrapServers, TimeSpan timeout, CancellationToken ct) =>
        ReadinessChecks.WaitForKafkaReadyAsync(bootstrapServers, timeout, ct);

    /// <summary>
    /// Wait for Flink JobManager to be ready. Delegates to ReadinessChecks helper.
    /// </summary>
    public static Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct, bool requireFreeSlots = true) =>
        ReadinessChecks.WaitForFlinkReadyAsync(overviewUrl, timeout, ct, requireFreeSlots);

    /// <summary>
    /// Wait for Gateway to be ready. Delegates to ReadinessChecks helper.
    /// </summary>
    public static Task WaitForGatewayReadyAsync(string healthUrl, TimeSpan timeout, CancellationToken ct) =>
        ReadinessChecks.WaitForGatewayReadyAsync(healthUrl, timeout, ct);

    /// <summary>
    /// Wait for SQL Gateway to be ready. Delegates to ReadinessChecks helper.
    /// </summary>
    public static Task WaitForSqlGatewayReadyAsync(string baseUrl, TimeSpan timeout, CancellationToken ct) =>
        ReadinessChecks.WaitForSqlGatewayReadyAsync(baseUrl, timeout, ct);

    /// <summary>
    /// Create Kafka topic. Delegates to KafkaHelpers.
    /// </summary>
    protected Task CreateTopicAsync(string topicName, int partitions = 1, short replicationFactor = 1) =>
        KafkaHelpers.CreateTopicAsync(KafkaConnectionString!, topicName, partitions, replicationFactor);

    /// <summary>
    /// Wait for full infrastructure readiness. Delegates to InfrastructureHelpers.
    /// </summary>
    protected static Task WaitForFullInfrastructureAsync(bool includeGateway = true, CancellationToken cancellationToken = default) =>
        InfrastructureHelpers.WaitForFullInfrastructureAsync(GlobalTestInfrastructure.KafkaConnectionString, includeGateway, cancellationToken);

    /// <summary>
    /// Capture test network diagnostics. Delegates to InfrastructureHelpers.
    /// </summary>
    protected static Task CaptureTestNetworkDiagnosticsAsync(string testName, string checkpoint) =>
        InfrastructureHelpers.CaptureTestNetworkDiagnosticsAsync(testName, checkpoint);

    /// <summary>
    /// Get Flink JobManager endpoint. Delegates to FlinkEndpointDiscovery.
    /// </summary>
    protected static Task<string> GetFlinkJobManagerEndpointAsync() =>
        FlinkEndpointDiscovery.GetFlinkJobManagerEndpointAsync();

    /// <summary>
    /// Get Flink JobManager logs. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task<string> GetFlinkJobManagerLogsAsync(string flinkEndpoint) =>
        FlinkDiagnostics.GetFlinkJobManagerLogsAsync(flinkEndpoint);

    /// <summary>
    /// Get Flink job exceptions. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task<string> GetFlinkJobExceptionsAsync(string flinkEndpoint, string jobId) =>
        FlinkDiagnostics.GetFlinkJobExceptionsAsync(flinkEndpoint, jobId);

    /// <summary>
    /// Get Flink TaskManager logs. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task<string> GetFlinkTaskManagerLogsAsync(string flinkEndpoint) =>
        FlinkDiagnostics.GetFlinkTaskManagerLogsAsync(flinkEndpoint);

    /// <summary>
    /// Get TaskManager logs from Docker. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task<string> GetTaskManagerLogsFromDockerAsync() =>
        FlinkDiagnostics.GetTaskManagerLogsFromDockerAsync();

    /// <summary>
    /// Get comprehensive Flink job diagnostics. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task<string> GetFlinkJobDiagnosticsAsync(string flinkEndpoint, string? jobId = null) =>
        FlinkDiagnostics.GetFlinkJobDiagnosticsAsync(flinkEndpoint, jobId);

    /// <summary>
    /// Log job status via Gateway. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task LogJobStatusViaGatewayAsync(string gatewayBase, string jobId, string checkpoint) =>
        FlinkDiagnostics.LogJobStatusViaGatewayAsync(gatewayBase, jobId, checkpoint);

    /// <summary>
    /// Log Flink container status. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task LogFlinkContainerStatusAsync(string checkpoint) =>
        FlinkDiagnostics.LogFlinkContainerStatusAsync(checkpoint);

    /// <summary>
    /// Log Flink job-specific logs. Delegates to FlinkDiagnostics.
    /// </summary>
    protected static Task LogFlinkJobLogsAsync(string jobId, string checkpoint) =>
        FlinkDiagnostics.LogFlinkJobLogsAsync(jobId, checkpoint);

    /// <summary>
    /// Test Kafka connectivity from Flink. Delegates to KafkaHelpers.
    /// </summary>
    protected static Task TestKafkaConnectivityFromFlinkAsync() =>
        KafkaHelpers.TestKafkaConnectivityFromFlinkAsync();
}
