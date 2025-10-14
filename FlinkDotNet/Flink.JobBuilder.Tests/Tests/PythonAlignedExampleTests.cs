namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class PythonAlignedExampleTests
{
    #region PythonLikeUsage Tests

    [Test]
    public void PythonLikeUsage_CreatesExecutionEnvironment()
    {
        // This test validates that the example code structure is correct
        // We can't fully execute it without infrastructure, but we can verify it compiles
        Assert.DoesNotThrow(() =>
        {
            // The example should not throw during setup
            var env = FlinkDotNet.Flink.GetExecutionEnvironment();
            Assert.That(env, Is.Not.Null);
        });
    }

    [Test]
    public void ConfigurationExample_CreatesConfiguration()
    {
        // Verify the configuration example pattern works
        Assert.DoesNotThrow(() =>
        {
            var config = FlinkDotNet.Flink.CreateConfiguration();
            config.SetString("parallelism.default", "8");
            config.SetInteger("buffer.timeout", 100);

            Assert.That(config.GetString("parallelism.default"), Is.EqualTo("8"));
            Assert.That(config.GetInteger("buffer.timeout"), Is.EqualTo(100));
        });
    }

    [Test]
    public void ModularStructureExample_CreatesConfigurationAndExecutionConfig()
    {
        // Verify the modular structure example pattern works
        Assert.DoesNotThrow(() =>
        {
            var config = new FlinkDotNet.Common.Configuration();
            var execConfig = new FlinkDotNet.Common.ExecutionConfig(config);
            execConfig.SetParallelism(4);

            Assert.That(execConfig.Parallelism, Is.EqualTo(4));
        });
    }

    [Test]
    public void ModularStructureExample_CreatesExecutionEnvironmentWithConfiguration()
    {
        // Verify execution environment creation with configuration
        Assert.DoesNotThrow(() =>
        {
            var config = new FlinkDotNet.Common.Configuration();
            config.SetInteger("parallelism.default", 8);
            
            var env = FlinkDotNet.Flink.GetExecutionEnvironment(config);
            
            Assert.That(env, Is.Not.Null);
        });
    }

    #endregion

    #region BackwardCompatibilityExample Tests

    [Test]
    public void BackwardCompatibilityExample_UsesFlinkJobBuilder()
    {
        // Verify backward compatibility pattern compiles and creates job
        Assert.DoesNotThrow(() =>
        {
            var job = FlinkDotNet.Flink.JobBuilder
                .FromKafka("orders")
                .Where("Amount > 100")
                .GroupBy("Region")
                .Aggregate("SUM", "Amount")
                .ToKafka("high-value-orders");

            Assert.That(job, Is.Not.Null);
            Assert.That(job.BuildJobDefinition(), Is.Not.Null);
        });
    }

    #endregion
}
