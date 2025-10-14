using FlinkDotNet.Common;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class StreamExecutionEnvironmentTests
{
    #region GetExecutionEnvironment Tests
    
    [Test]
    public void GetExecutionEnvironment_ReturnsNotNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.That(env, Is.Not.Null);
    }
    
    [Test]
    public void GetExecutionEnvironment_MultipleCalls_ReturnsInstances()
    {
        var env1 = StreamExecutionEnvironment.GetExecutionEnvironment();
        var env2 = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        // Both should be valid instances (may or may not be same instance depending on implementation)
        Assert.That(env1, Is.Not.Null);
        Assert.That(env2, Is.Not.Null);
    }
    
    #endregion
    
    #region Parallelism Tests
    
    [Test]
    public void SetParallelism_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetParallelism(4);
        
        Assert.That(result, Is.SameAs(env));
    }
    
    [Test]
    public void SetParallelism_GetParallelism_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetParallelism(8);
        
        var parallelism = env.GetParallelism();
        
        Assert.That(parallelism, Is.EqualTo(8));
    }
    
    [Test]
    public void SetMaxParallelism_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetMaxParallelism(128);
        
        Assert.That(result, Is.SameAs(env));
    }
    
    [Test]
    public void SetMaxParallelism_GetMaxParallelism_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetMaxParallelism(256);
        
        var maxParallelism = env.GetMaxParallelism();
        
        Assert.That(maxParallelism, Is.EqualTo(256));
    }
    
    [Test]
    public void SetMaxParallelism_WithZero_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
    }
    
    [Test]
    public void SetMaxParallelism_WithNegative_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
    }
    
    [Test]
    public void SetMaxParallelism_WithTooLarge_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
    }
    
    [Test]
    public void SetMaxParallelism_WithMaxValue_DoesNotThrow()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.DoesNotThrow(() => env.SetMaxParallelism(32768));
    }
    
    #endregion
    
    #region Config Tests
    
    [Test]
    public void GetConfig_ReturnsExecutionConfig()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var config = env.GetConfig();
        
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.TypeOf<ExecutionConfig>());
    }
    
    [Test]
    public void GetConfig_ReturnsSameInstance()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var config1 = env.GetConfig();
        var config2 = env.GetConfig();
        
        Assert.That(config1, Is.SameAs(config2));
    }
    
    #endregion
    
    #region FromCollection Tests
    
    [Test]
    public void FromCollection_WithArray_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var data = new[] { 1, 2, 3, 4, 5 };
        
        var stream = env.FromCollection(data);
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void FromCollection_WithList_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var data = new List<string> { "a", "b", "c" };
        
        var stream = env.FromCollection(data);
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void FromCollection_WithEmptyCollection_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var data = Array.Empty<int>();
        
        var stream = env.FromCollection(data);
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }
    
    #endregion
    
    #region AddSource Tests
    
    [Test]
    public void AddSource_WithSourceFunction_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        
        var stream = env.AddSource(sourceFunc, "test-source");
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void AddSource_WithSourceFunctionDefaultName_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        
        var stream = env.AddSource(sourceFunc);
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }
    
    #endregion
    
    #region FromKafka Tests
    
    [Test]
    public void FromKafka_WithValidParams_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void FromKafka_WithDefaultGroupId_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.FromKafka("test-topic", "localhost:9092");
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<string>>());
    }
    
    [Test]
    public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", null));
    }
    
    [Test]
    public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", ""));
    }
    
    [Test]
    public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", "   "));
    }
    
    #endregion
    
    #region AddKafkaSource Tests
    
    [Test]
    public void AddKafkaSource_WithDeserializer_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", s => int.Parse(s));
        
        Assert.That(stream, Is.Not.Null);
        Assert.That(stream, Is.TypeOf<DataStream<int>>());
    }
    
    [Test]
    public void AddKafkaSource_WithCustomType_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var stream = env.AddKafkaSource("test-topic", "localhost:9092", "test-group", s => new { Value = s });
        
        Assert.That(stream, Is.Not.Null);
    }
    
    #endregion
}
