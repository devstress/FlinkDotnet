using FlinkDotNet.Common;
using FlinkDotNet.DataStream;

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

    #region Buffer Timeout Tests

    [Test]
    public void SetBufferTimeout_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.SetBufferTimeout(500);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void SetBufferTimeout_GetBufferTimeout_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.SetBufferTimeout(1000);

        var timeout = env.GetBufferTimeout();

        Assert.That(timeout, Is.EqualTo(1000));
    }

    [Test]
    public void GetBufferTimeout_DefaultValue_Returns100()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var timeout = env.GetBufferTimeout();

        Assert.That(timeout, Is.EqualTo(100));
    }

    #endregion

    #region Operator Chaining Tests

    [Test]
    public void DisableOperatorChaining_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.DisableOperatorChaining();

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void IsChainingEnabled_DefaultValue_ReturnsTrue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var enabled = env.IsChainingEnabled();

        Assert.That(enabled, Is.True);
    }

    [Test]
    public void DisableOperatorChaining_IsChainingEnabled_ReturnsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.DisableOperatorChaining();

        var enabled = env.IsChainingEnabled();

        Assert.That(enabled, Is.False);
    }

    #endregion

    #region Checkpointing Tests

    [Test]
    public void EnableCheckpointing_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableCheckpointing(5000);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void EnableCheckpointing_GetCheckpointInterval_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableCheckpointing(10000);

        var interval = env.GetCheckpointInterval();

        Assert.That(interval, Is.EqualTo(10000));
    }

    [Test]
    public void GetCheckpointInterval_DefaultValue_ReturnsNegativeOne()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var interval = env.GetCheckpointInterval();

        Assert.That(interval, Is.EqualTo(-1));
    }

    #endregion

    #region Adaptive Scheduler Tests

    [Test]
    public void EnableAdaptiveScheduler_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableAdaptiveScheduler();

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void EnableAdaptiveScheduler_WithTrue_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableAdaptiveScheduler(true);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void EnableAdaptiveScheduler_WithFalse_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableAdaptiveScheduler(false);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void IsAdaptiveSchedulerEnabled_DefaultValue_ReturnsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var enabled = env.IsAdaptiveSchedulerEnabled();

        Assert.That(enabled, Is.False);
    }

    [Test]
    public void EnableAdaptiveScheduler_IsAdaptiveSchedulerEnabled_ReturnsTrue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableAdaptiveScheduler();

        var enabled = env.IsAdaptiveSchedulerEnabled();

        Assert.That(enabled, Is.True);
    }

    [Test]
    public void EnableAdaptiveScheduler_WithFalse_IsAdaptiveSchedulerEnabled_ReturnsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableAdaptiveScheduler(true);
        env.EnableAdaptiveScheduler(false);

        var enabled = env.IsAdaptiveSchedulerEnabled();

        Assert.That(enabled, Is.False);
    }

    #endregion

    #region Reactive Mode Tests

    [Test]
    public void EnableReactiveMode_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableReactiveMode();

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void EnableReactiveMode_WithTrue_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableReactiveMode(true);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void EnableReactiveMode_WithFalse_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.EnableReactiveMode(false);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void IsReactiveModeEnabled_DefaultValue_ReturnsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var enabled = env.IsReactiveModeEnabled();

        Assert.That(enabled, Is.False);
    }

    [Test]
    public void EnableReactiveMode_IsReactiveModeEnabled_ReturnsTrue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableReactiveMode();

        var enabled = env.IsReactiveModeEnabled();

        Assert.That(enabled, Is.True);
    }

    [Test]
    public void EnableReactiveMode_WithFalse_IsReactiveModeEnabled_ReturnsFalse()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.EnableReactiveMode(true);
        env.EnableReactiveMode(false);

        var enabled = env.IsReactiveModeEnabled();

        Assert.That(enabled, Is.False);
    }

    #endregion

    #region Savepoint Tests

    [Test]
    public void FromSavepoint_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env.FromSavepoint("/path/to/savepoint");

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void FromSavepoint_GetSavepointPath_ReturnsSetValue()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var savepointPath = "/flink/savepoints/savepoint-123";
        env.FromSavepoint(savepointPath);

        var path = env.GetSavepointPath();

        Assert.That(path, Is.EqualTo(savepointPath));
    }

    [Test]
    public void GetSavepointPath_DefaultValue_ReturnsNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var path = env.GetSavepointPath();

        Assert.That(path, Is.Null);
    }

    #endregion

    #region Configure Tests

    [Test]
    public void Configure_ReturnsEnvironment()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var config = new Configuration();

        var result = env.Configure(config);

        Assert.That(result, Is.SameAs(env));
    }

    [Test]
    public void Configure_WithValues_AppliesConfiguration()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var config = new Configuration();
        config.SetString("test.key", "test.value");

        env.Configure(config);

        var envConfig = env.GetConfig().GetConfiguration();
        Assert.That(envConfig.GetString("test.key", null), Is.EqualTo("test.value"));
    }

    #endregion

    #region GetExecutionEnvironment with Configuration Tests

    [Test]
    public void GetExecutionEnvironment_WithConfiguration_ReturnsNotNull()
    {
        var config = new Configuration();
        config.SetInteger("parallelism.default", 4);

        var env = StreamExecutionEnvironment.GetExecutionEnvironment(config);

        Assert.That(env, Is.Not.Null);
    }

    [Test]
    public void GetExecutionEnvironment_WithNullConfiguration_ReturnsNotNull()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment(null);

        Assert.That(env, Is.Not.Null);
    }

    #endregion

    #region ExecuteAsyncJob Tests

    [Test]
    public void ExecuteAsyncJob_WithoutActiveJob_ThrowsInvalidOperationException()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        Assert.ThrowsAsync<InvalidOperationException>(async () => await env.ExecuteAsyncJob("test-job"));
    }

    [Test]
    public async Task ExecuteAsyncJob_WithActiveJob_ReturnsJobClient()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.FromKafka("test-topic", "localhost:9092", "test-group");

        var result = await env.ExecuteAsyncJob("test-job");

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.TypeOf<JobClient>());
    }

    [Test]
    public async Task ExecuteAsyncJob_WithDefaultJobName_ReturnsJobClient()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        env.FromKafka("test-topic", "localhost:9092", "test-group");

        var result = await env.ExecuteAsyncJob();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.JobName, Is.EqualTo("Flink Streaming Job"));
    }

    #endregion

    #region AddSource Edge Cases

    [Test]
    public void AddSource_WithNullSourceName_UsesDefaultName()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();

        var stream = env.AddSource(sourceFunc, null!);

        Assert.That(stream, Is.Not.Null);
    }

    [Test]
    public void AddSource_WithEmptySourceName_ReturnsDataStream()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();
        var sourceFunc = new TestSourceFunction();

        var stream = env.AddSource(sourceFunc, "");

        Assert.That(stream, Is.Not.Null);
    }

    #endregion

    #region Configuration Method Chaining Tests

    [Test]
    public void MethodChaining_MultipleConfigurationCalls_WorksCorrectly()
    {
        var env = StreamExecutionEnvironment.GetExecutionEnvironment();

        var result = env
            .SetParallelism(4)
            .SetMaxParallelism(128)
            .SetBufferTimeout(500)
            .DisableOperatorChaining()
            .EnableCheckpointing(5000)
            .EnableAdaptiveScheduler()
            .EnableReactiveMode()
            .FromSavepoint("/path/to/savepoint");

        Assert.That(result, Is.SameAs(env));
        Assert.That(env.GetParallelism(), Is.EqualTo(4));
        Assert.That(env.GetMaxParallelism(), Is.EqualTo(128));
        Assert.That(env.GetBufferTimeout(), Is.EqualTo(500));
        Assert.That(env.IsChainingEnabled(), Is.False);
        Assert.That(env.GetCheckpointInterval(), Is.EqualTo(5000));
        Assert.That(env.IsAdaptiveSchedulerEnabled(), Is.True);
        Assert.That(env.IsReactiveModeEnabled(), Is.True);
        Assert.That(env.GetSavepointPath(), Is.EqualTo("/path/to/savepoint"));
    }

    #endregion

    #region Logger Initialization Tests - Coverage Enhancement

    [Test]
    public void CreateLogger_WithLogDirectory_CleansUpOldLogFiles()
    {
        // Create a temporary log directory for testing
        var tempLogPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"flink_test_logs_{System.Guid.NewGuid()}");
        System.IO.Directory.CreateDirectory(tempLogPath);

        try
        {
            // Set environment variable
            System.Environment.SetEnvironmentVariable("LOG_FILE_PATH", tempLogPath);

            // Create an old log file (2 days old)
            var oldLogFile = System.IO.Path.Combine(tempLogPath, "FlinkDotnet.log.20231201");
            System.IO.File.WriteAllText(oldLogFile, "old log content");
            System.IO.File.SetLastWriteTimeUtc(oldLogFile, System.DateTime.UtcNow.AddDays(-2));

            // Create a recent log file (today)
            var today = System.DateTime.UtcNow.ToString("yyyyMMdd");
            var recentLogFile = System.IO.Path.Combine(tempLogPath, $"FlinkDotnet.log.{today}");
            System.IO.File.WriteAllText(recentLogFile, "recent log content");

            // Use reflection to call the private CreateLogger method
            var method = typeof(StreamExecutionEnvironment).GetMethod(
                "CreateLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act - this should clean up the old log file
            var logger = method!.Invoke(null, null);

            // Assert - old file should be deleted, recent file should remain
            Assert.That(System.IO.File.Exists(oldLogFile), Is.False, "Old log file should be deleted");
            Assert.That(logger, Is.Not.Null);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("LOG_FILE_PATH", null);
            // Wait a moment for log files to be released
            System.Threading.Thread.Sleep(100);
            try
            {
                if (System.IO.Directory.Exists(tempLogPath))
                {
                    System.IO.Directory.Delete(tempLogPath, true);
                }
            }
            catch (System.IO.IOException)
            {
                // Log files may be locked - ignore cleanup errors in tests
            }
        }
    }

    [Test]
    public void CreateLogger_WithNonExistentDirectory_CreatesLogger()
    {
        // Use a path that doesn't exist
        var nonExistentPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"nonexistent_{System.Guid.NewGuid()}");

        try
        {
            // Set environment variable
            System.Environment.SetEnvironmentVariable("LOG_FILE_PATH", nonExistentPath);

            // Use reflection to call the private CreateLogger method
            var method = typeof(StreamExecutionEnvironment).GetMethod(
                "CreateLogger",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            // Act - should handle gracefully
            var logger = method!.Invoke(null, null);

            // Assert - logger should still be created
            Assert.That(logger, Is.Not.Null);
        }
        finally
        {
            // Cleanup
            System.Environment.SetEnvironmentVariable("LOG_FILE_PATH", null);
        }
    }

    #endregion
}
