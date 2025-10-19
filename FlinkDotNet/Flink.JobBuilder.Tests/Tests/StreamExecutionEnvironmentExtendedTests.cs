using FlinkDotNet.DataStream;
using FlinkDotNet.Common;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class StreamExecutionEnvironmentExtendedTests
{
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
    public void Configure_AppliesConfiguration()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var config = new Configuration();
        config.SetInteger("parallelism.default", 8);
        
        var result = env.Configure(config);
        
        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void GetExecutionEnvironment_WithConfiguration_AppliesConfig()
    {
        var config = new Configuration();
        config.SetInteger("parallelism.default", 16);
        
        var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);
        
        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void GetExecutionEnvironment_WithNullConfiguration_CreatesDefaultEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment(null);
        
        Assert.That(env, Is.Not.Null);
    }

    #endregion

    #region Parallelism Tests

    [Test]
    public void SetParallelism_SetsParallelism()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetParallelism(8);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetParallelism(), Is.EqualTo(8));
    }

    [Test]
    public void GetParallelism_ReturnsNegativeOneWhenNotSet()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var parallelism = env.GetParallelism();
        
        Assert.That(parallelism, Is.EqualTo(-1));
    }

    [Test]
    public void SetMaxParallelism_SetsMaxParallelism()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetMaxParallelism(16);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(16));
    }

    [Test]
    public void GetMaxParallelism_ReturnsNegativeOneWhenNotSet()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var maxParallelism = env.GetMaxParallelism();
        
        Assert.That(maxParallelism, Is.EqualTo(-1));
    }

    #endregion

    #region Buffer Timeout Tests

    [Test]
    public void SetBufferTimeout_SetsTimeout()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.SetBufferTimeout(100);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetBufferTimeout(), Is.EqualTo(100));
    }

    [Test]
    public void GetBufferTimeout_ReturnsDefaultWhenNotSet()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var timeout = env.GetBufferTimeout();
        
        // Default is 100ms
        Assert.That(timeout, Is.EqualTo(100));
    }

    #endregion

    #region Operator Chaining Tests

    [Test]
    public void DisableOperatorChaining_DisablesChaining()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.DisableOperatorChaining();
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsChainingEnabled(), Is.False);
    }

    [Test]
    public void IsChainingEnabled_ReturnsTrueByDefault()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.That(env.IsChainingEnabled(), Is.True);
    }

    #endregion

    #region Checkpointing Tests

    [Test]
    public void EnableCheckpointing_SetsCheckpointInterval()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableCheckpointing(5000);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
    }

    [Test]
    public void GetCheckpointInterval_ReturnsNegativeOneWhenNotSet()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var interval = env.GetCheckpointInterval();
        
        Assert.That(interval, Is.EqualTo(-1));
    }

    #endregion

    #region Adaptive Scheduler Tests

    [Test]
    public void EnableAdaptiveScheduler_EnablesScheduler()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableAdaptiveScheduler(true);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
    }

    [Test]
    public void EnableAdaptiveScheduler_DisablesScheduler()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableAdaptiveScheduler(true);
        
        var result = env.EnableAdaptiveScheduler(false);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
    }

    [Test]
    public void IsAdaptiveSchedulerEnabled_ReturnsFalseByDefault()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.False);
    }

    #endregion

    #region Reactive Mode Tests

    [Test]
    public void EnableReactiveMode_EnablesMode()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env.EnableReactiveMode(true);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsReactiveModeEnabled(), Is.True);
    }

    [Test]
    public void EnableReactiveMode_DisablesMode()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableReactiveMode(true);
        
        var result = env.EnableReactiveMode(false);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.IsReactiveModeEnabled(), Is.False);
    }

    [Test]
    public void IsReactiveModeEnabled_ReturnsFalseByDefault()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        Assert.That(env.IsReactiveModeEnabled(), Is.False);
    }

    #endregion

    #region Savepoint Tests

    [Test]
    public void FromSavepoint_SetsSavepointPath()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var savepointPath = "/path/to/savepoint";
        
        var result = env.FromSavepoint(savepointPath);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetSavepointPath(), Is.EqualTo(savepointPath));
    }

    [Test]
    public void GetSavepointPath_ReturnsNullWhenNotSet()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var path = env.GetSavepointPath();
        
        Assert.That(path, Is.Null);
    }

    #endregion

    #region Source Tests

    [Test]
    public void FromCollection_CreatesDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var collection = new[] { 1, 2, 3, 4, 5 };
        
        var dataStream = env.FromCollection(collection);
        
        Assert.That(dataStream, Is.Not.Null);
    }

    [Test]
    public void AddSource_CreatesDataStreamWithCustomSource()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        
        var dataStream = env.AddSource(sourceFunc, "Test Source");
        
        Assert.That(dataStream, Is.Not.Null);
    }

    [Test]
    public void AddSource_WithDefaultName_CreatesDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();
        
        var dataStream = env.AddSource(sourceFunc);
        
        Assert.That(dataStream, Is.Not.Null);
    }

    private class TestSourceFunction : ISourceFunction<string>
    {
        public async IAsyncEnumerable<string> RunAsync(CancellationToken cancellationToken = default)
        {
            yield return "test";
            await Task.CompletedTask;
        }
    }

    #endregion

    #region Fluent Configuration Tests

    [Test]
    public void FluentConfiguration_ChainsMultipleMethods()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        
        var result = env
            .SetParallelism(4)
            .SetMaxParallelism(8)
            .SetBufferTimeout(100)
            .EnableCheckpointing(5000)
            .EnableAdaptiveScheduler(true)
            .EnableReactiveMode(true);
        
        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetParallelism(), Is.EqualTo(4));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(8));
        Assert.That(env.GetBufferTimeout(), Is.EqualTo(100));
        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        Assert.That(env.IsReactiveModeEnabled(), Is.True);
    }

    #endregion
}
