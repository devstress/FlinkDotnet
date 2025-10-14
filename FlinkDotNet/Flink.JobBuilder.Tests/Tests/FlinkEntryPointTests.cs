using FlinkDotNet.Common;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkEntryPointTests
{
    #region Flink Entry Point Tests

    [Test]
    public void Flink_GetExecutionEnvironment_ReturnsEnvironment()
    {
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();

        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void Flink_GetExecutionEnvironment_WithConfiguration_ReturnsEnvironment()
    {
        var config = new Configuration();
        config.SetString("test.key", "test.value");

        var env = FlinkDotNet.Flink.GetExecutionEnvironment(config);

        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void Flink_GetExecutionEnvironment_WithNullConfiguration_ReturnsEnvironment()
    {
        var env = FlinkDotNet.Flink.GetExecutionEnvironment(null);

        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void Flink_CreateConfiguration_ReturnsNewConfiguration()
    {
        var config = FlinkDotNet.Flink.CreateConfiguration();

        Assert.That(config, Is.Not.Null);
    }

    [Test]
    public void Flink_CreateConfiguration_ReturnsEmptyConfiguration()
    {
        var config = FlinkDotNet.Flink.CreateConfiguration();

        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void Flink_CreateConfiguration_ReturnsIndependentInstances()
    {
        var config1 = FlinkDotNet.Flink.CreateConfiguration();
        var config2 = FlinkDotNet.Flink.CreateConfiguration();

        config1.SetString("key", "value1");
        config2.SetString("key", "value2");

        Assert.That(config1.GetString("key"), Is.EqualTo("value1"));
        Assert.That(config2.GetString("key"), Is.EqualTo("value2"));
    }

    #endregion

    #region JobBuilder Static Methods Tests

    [Test]
    public void Flink_JobBuilder_FromKafka_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromKafka("test-topic", "localhost:9092");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Flink_JobBuilder_FromKafka_WithNullBootstrap_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromKafka("test-topic", null);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Flink_JobBuilder_FromHttp_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromHttp("http://api.example.com/data");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Flink_JobBuilder_FromHttp_WithMethodAndInterval_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromHttp("http://api.example.com/data", "POST", 30);

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Flink_JobBuilder_FromDatabase_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromDatabase(
            "Server=localhost;Database=test",
            "SELECT * FROM users");

        Assert.That(builder, Is.Not.Null);
    }

    [Test]
    public void Flink_JobBuilder_FromDatabase_WithPollingInterval_ReturnsJobBuilder()
    {
        var builder = FlinkDotNet.Flink.JobBuilder.FromDatabase(
            "Server=localhost;Database=test",
            "SELECT * FROM users",
            60);

        Assert.That(builder, Is.Not.Null);
    }

    #endregion
}
