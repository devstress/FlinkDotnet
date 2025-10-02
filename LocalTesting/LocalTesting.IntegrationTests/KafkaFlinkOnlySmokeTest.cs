using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture, NonParallelizable]
[Category("kafka-flink-only")]
public class KafkaFlinkOnlySmokeTest : LocalTestingTestBase
{
    [Test]
    public async Task KafkaAndFlink_StartWithoutGateway_Succeeds()
    {
        TestPrerequisites.EnsureDockerAvailable();

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(3)); // Reduced from 15 minutes
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;

        try
        {
            TestContext.WriteLine("🔧 Testing Kafka + Flink infrastructure without Gateway...");

            // Infrastructure readiness is handled by base class OneTimeSetUp
            // Additional validation for Flink components (Gateway runs but we don't wait for it)
            await WaitForFullInfrastructureAsync(includeGateway: false, ct);

            TestContext.WriteLine("✅ Kafka + Flink infrastructure validated successfully");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"❌ Infrastructure validation failed: {ex.Message}");
            throw;
        }
    }
}












