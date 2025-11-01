using NUnit.Framework;

namespace ObservabilityTesting.IntegrationTests;

[TestFixture]
public class DebugInfrastructureTest
{
    [Test]
    public void Debug_VerifyGlobalInfrastructureIsInitialized()
    {
        TestContext.WriteLine($"AppHost: {(GlobalTestInfrastructure.AppHost != null ? "✅ Initialized" : "❌ NULL")}");
        TestContext.WriteLine($"KafkaConnectionString: {GlobalTestInfrastructure.KafkaConnectionString ?? "❌ NULL"}");
        TestContext.WriteLine($"KafkaFlinkBootstrapServers: {GlobalTestInfrastructure.KafkaFlinkBootstrapServers ?? "❌ NULL"}");
        TestContext.WriteLine($"KafkaEndpoint: {GlobalTestInfrastructure.KafkaEndpoint ?? "❌ NULL"}");
        
        Assert.That(GlobalTestInfrastructure.AppHost, Is.Not.Null, "AppHost should be initialized by GlobalSetUp");
        Assert.That(GlobalTestInfrastructure.KafkaConnectionString, Is.Not.Null.And.Not.Empty, "KafkaConnectionString should be set");
    }
}
