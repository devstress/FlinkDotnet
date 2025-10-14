namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkDotNetPipelinesTests
{
    #region KafkaToKafka Tests

    [Test]
    public void KafkaToKafka_WithDefaultBootstrapServers_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka("input-topic", "output-topic");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
        Assert.That(jobDef.Source, Is.Not.Null);
        Assert.That(jobDef.Sink, Is.Not.Null);
    }

    [Test]
    public void KafkaToKafka_WithCustomBootstrapServers_CreatesJobWithServers()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
            "input-topic", 
            "output-topic", 
            "localhost:9092");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        var kafkaSource = jobDef.Source as Models.KafkaSourceDefinition;
        Assert.That(kafkaSource, Is.Not.Null);
        Assert.That(kafkaSource!.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void KafkaToKafka_WithCustomMapExpression_CreatesJobWithMapOperation()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
            "input-topic", 
            "output-topic", 
            mapExpression: "upper");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef.Operations, Is.Not.Empty);
    }

    [Test]
    public void KafkaToKafka_WithIdentityMap_CreatesJobWithIdentityOperation()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToKafka(
            "input-topic", 
            "output-topic", 
            mapExpression: "identity");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef.Operations, Is.Not.Empty);
    }

    #endregion

    #region KafkaToConsole Tests

    [Test]
    public void KafkaToConsole_WithDefaultBootstrapServers_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole("input-topic");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
        Assert.That(jobDef.Source, Is.Not.Null);
        Assert.That(jobDef.Sink, Is.Not.Null);
    }

    [Test]
    public void KafkaToConsole_WithCustomBootstrapServers_CreatesJobWithServers()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
            "input-topic", 
            "localhost:9092");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        var kafkaSource = jobDef.Source as Models.KafkaSourceDefinition;
        Assert.That(kafkaSource, Is.Not.Null);
        Assert.That(kafkaSource!.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    [Test]
    public void KafkaToConsole_WithCustomMapExpression_CreatesJobWithMapOperation()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
            "input-topic", 
            mapExpression: "lower");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef.Operations, Is.Not.Empty);
    }

    [Test]
    public void KafkaToConsole_WithIdentityMap_CreatesJobWithIdentityOperation()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.KafkaToConsole(
            "input-topic", 
            mapExpression: "identity");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef.Operations, Is.Not.Empty);
    }

    #endregion

    #region Sql Tests

    [Test]
    public void Sql_WithSingleStatement_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.Sql("SELECT * FROM table1");

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
    }

    [Test]
    public void Sql_WithMultipleStatements_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.Sql(
            "CREATE TABLE source (id INT, name STRING)",
            "CREATE TABLE sink (id INT, name STRING)",
            "INSERT INTO sink SELECT * FROM source"
        );

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
    }

    [Test]
    public void Sql_WithNullStatements_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.Sql(null!);

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
    }

    [Test]
    public void Sql_WithEmptyStatements_CreatesJob()
    {
        var job = FlinkDotNet.Pipelines.FlinkDotNet.Sql();

        Assert.That(job, Is.Not.Null);
        var jobDef = job.BuildJobDefinition();
        Assert.That(jobDef, Is.Not.Null);
    }

    #endregion
}
