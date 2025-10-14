using FlinkDotNet.Common;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class StreamExecutionEnvironmentTests
{
    #region Factory Methods Tests

    [Test]
    public void GetExecutionEnvironment_ReturnsInstance()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void GetExecutionEnvironment_WithConfiguration_ReturnsInstance()
    {
        var config = new Configuration()
            .SetString("jobmanager.memory.process.size", "1024m");
        
        var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);
        
        Assert.That(env, Is.Not.Null);
        Assert.That(env.GetConfig(), Is.Not.Null);
    }

    [Test]
    public void GetExecutionEnvironment_WithNullConfiguration_ReturnsInstance()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment(null);
        
        Assert.That(env, Is.Not.Null);
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void GetConfig_ReturnsExecutionConfig()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var config = env.GetConfig();
        
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.TypeOf<ExecutionConfig>());
    }

    [Test]
    public void SetParallelism_SetsValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetParallelism(4);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetParallelism(), Is.EqualTo(4));
    }

    [Test]
    public void SetParallelism_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetParallelism(2);
        
        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void GetParallelism_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetParallelism(8);
        
        var parallelism = env.GetParallelism();
        
        Assert.That(parallelism, Is.EqualTo(8));
    }

    [Test]
    public void SetMaxParallelism_WithValidValue_SetsValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetMaxParallelism(128);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
    }

    [Test]
    public void SetMaxParallelism_WithZero_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
    }

    [Test]
    public void SetMaxParallelism_WithNegative_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
    }

    [Test]
    public void SetMaxParallelism_WithTooLarge_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
    }

    [Test]
    public void SetMaxParallelism_WithMaxValue_Works()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetMaxParallelism(32768);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(32768));
    }

    [Test]
    public void GetMaxParallelism_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetMaxParallelism(256);
        
        var maxParallelism = env.GetMaxParallelism();
        
        Assert.That(maxParallelism, Is.EqualTo(256));
    }

    #endregion

    #region Buffer Timeout Tests

    [Test]
    public void SetBufferTimeout_SetsValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetBufferTimeout(200);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetBufferTimeout(), Is.EqualTo(200));
    }

    [Test]
    public void SetBufferTimeout_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetBufferTimeout(50);
        
        Assert.That(result, Is.SameAs(env));
    }

    #endregion

    #region Operator Chaining Tests

    [Test]
    public void DisableOperatorChaining_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.DisableOperatorChaining();
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsChainingEnabled(), Is.False);
    }

    [Test]
    public void IsChainingEnabled_DefaultIsTrue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var isEnabled = env.IsChainingEnabled();
        
        Assert.That(isEnabled, Is.True);
    }

    #endregion

    #region Checkpointing Tests

    [Test]
    public void EnableCheckpointing_WithInterval_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableCheckpointing(5000);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
    }

    [Test]
    public void GetCheckpointInterval_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableCheckpointing(3000);
        
        var interval = env.GetCheckpointInterval();
        
        Assert.That(interval, Is.EqualTo(3000));
    }

    #endregion

    #region Scheduler Tests

    [Test]
    public void EnableAdaptiveScheduler_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableAdaptiveScheduler();
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
    }

    [Test]
    public void IsAdaptiveSchedulerEnabled_DefaultIsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var isEnabled = env.IsAdaptiveSchedulerEnabled();
        
        Assert.That(isEnabled, Is.False);
    }

    [Test]
    public void EnableReactiveMode_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableReactiveMode();
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsReactiveModeEnabled(), Is.True);
    }

    [Test]
    public void IsReactiveModeEnabled_DefaultIsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var isEnabled = env.IsReactiveModeEnabled();
        
        Assert.That(isEnabled, Is.False);
    }

    #endregion

    #region Savepoint Tests

    [Test]
    public void FromSavepoint_WithPath_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.FromSavepoint("/path/to/savepoint");
        
        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void GetSavepointPath_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.FromSavepoint("/path/to/savepoint");
        
        var path = env.GetSavepointPath();
        
        Assert.That(path, Is.EqualTo("/path/to/savepoint"));
    }

    [Test]
    public void GetSavepointPath_BeforeSet_ReturnsNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var path = env.GetSavepointPath();
        
        Assert.That(path, Is.Null);
    }

    #endregion

    #region Kafka Source Tests

    [Test]
    public void FromKafka_WithValidParameters_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void FromKafka_WithNullBootstrapServers_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => 
            env.FromKafka("test-topic", null));
    }

    [Test]
    public void FromKafka_WithEmptyBootstrapServers_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => 
            env.FromKafka("test-topic", ""));
    }

    [Test]
    public void FromKafka_WithWhitespaceBootstrapServers_ThrowsException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => 
            env.FromKafka("test-topic", "   "));
    }

    [Test]
    public void FromKafka_WithDefaultGroupId_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.FromKafka("test-topic", "localhost:9092");
        
        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void FromKafka_WithCustomStartingOffset_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");
        
        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void AddKafkaSource_WithDeserializer_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.AddKafkaSource<string>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => s.ToUpper());
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void AddKafkaSource_WithCustomType_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.AddKafkaSource<int>(
            "test-topic",
            "localhost:9092",
            "test-group",
            s => int.Parse(s));
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }

    #endregion

    #region Fluent API Tests

    [Test]
    public void FluentAPI_ChainingMethods_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env
            .SetParallelism(4)
            .SetMaxParallelism(128)
            .SetBufferTimeout(100)
            .DisableOperatorChaining()
            .EnableCheckpointing(5000)
            .EnableAdaptiveScheduler()
            .EnableReactiveMode()
            .FromSavepoint("/path/to/savepoint");
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetParallelism(), Is.EqualTo(4));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
        Assert.That(env.GetBufferTimeout(), Is.EqualTo(100));
        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        Assert.That(env.IsChainingEnabled(), Is.False);
    }

    #endregion

    #region Additional Tests

    [Test]
    public void GetBufferTimeout_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetBufferTimeout(150);
        
        var timeout = env.GetBufferTimeout();
        
        Assert.That(timeout, Is.EqualTo(150));
    }

    [Test]
    public void FromCollection_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var data = new[] { "a", "b", "c" };
        
        var stream = env.FromCollection(data);
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }

    [Test]
    public void Configure_WithConfiguration_ReturnsThis()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var config = new Configuration().SetString("test.key", "test.value");
        
        var result = env.Configure(config);
        
        Assert.That(result, Is.SameAs(env));
    }

    #endregion
}
