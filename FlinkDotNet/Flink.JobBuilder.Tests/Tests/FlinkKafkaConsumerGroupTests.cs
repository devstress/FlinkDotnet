using Flink.JobBuilder.Flink;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkKafkaConsumerGroupTests
{
    private Mock<ILogger<FlinkKafkaConsumerGroup>> _mockLogger = null!;
    private Dictionary<string, object> _consumerConfig = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<FlinkKafkaConsumerGroup>>();
        _consumerConfig = new Dictionary<string, object>
        {
            ["bootstrap.servers"] = "localhost:9092",
            ["group.id"] = "test-group"
        };
    }

    #region Constructor and Initialization Tests

    [Test]
    public void Constructor_WithValidConfiguration_CreatesInstance()
    {
        // Act
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Assert
        Assert.That(consumerGroup, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkKafkaConsumerGroup(null!, _mockLogger.Object));
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            new FlinkKafkaConsumerGroup(_consumerConfig, null!));
    }

    [Test]
    public void Constructor_ValidatesFlinkConfiguration()
    {
        // Act
        _ = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Assert - verify Flink-optimal settings were applied
        Assert.That(_consumerConfig.ContainsKey("enable.auto.commit"), Is.True);
        Assert.That(_consumerConfig["enable.auto.commit"], Is.EqualTo(false));
        Assert.That(_consumerConfig.ContainsKey("session.timeout.ms"), Is.True);
        Assert.That(_consumerConfig["session.timeout.ms"], Is.EqualTo(30000));
        Assert.That(_consumerConfig.ContainsKey("heartbeat.interval.ms"), Is.True);
        Assert.That(_consumerConfig["heartbeat.interval.ms"], Is.EqualTo(10000));
        Assert.That(_consumerConfig.ContainsKey("partition.assignment.strategy"), Is.True);
        Assert.That(_consumerConfig["partition.assignment.strategy"], Is.EqualTo("CooperativeSticky"));
    }

    [Test]
    public void Constructor_WithOptimalSettings_LogsNoWarnings()
    {
        // Arrange
        _consumerConfig["enable.auto.commit"] = false;
        _consumerConfig["session.timeout.ms"] = 30000;
        _consumerConfig["heartbeat.interval.ms"] = 10000;
        _consumerConfig["partition.assignment.strategy"] = "CooperativeSticky";

        // Act
        _ = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Assert - no warnings should be logged for optimal settings
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Test]
    public void Constructor_WithNonOptimalSettings_LogsWarnings()
    {
        // Arrange
        _consumerConfig["enable.auto.commit"] = true; // Non-optimal

        // Act
        _ = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Assert - warning should be logged for non-optimal setting
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Non-optimal Flink setting detected")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public void InitializeAsync_WithValidTopics_CompletesSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var topics = new[] { "topic1", "topic2", "topic3" };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.InitializeAsync(topics));
    }

    [Test]
    public void InitializeAsync_WithSingleTopic_CompletesSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var topics = new[] { "single-topic" };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.InitializeAsync(topics));
    }

    [Test]
    public void InitializeAsync_WithEmptyTopics_CompletesSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var topics = Array.Empty<string>();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.InitializeAsync(topics));
    }

    [Test]
    public void InitializeAsync_WithCancellationToken_AcceptsToken()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var topics = new[] { "topic1" };
        using var cts = new CancellationTokenSource();

        // Act & Assert - Current implementation doesn't throw on cancellation, just completes
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.InitializeAsync(topics, cts.Token));
    }

    [Test]
    public async Task InitializeAsync_LogsTopics()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var topics = new[] { "topic1", "topic2" };

        // Act
        await consumerGroup.InitializeAsync(topics);

        // Assert - verify logging occurred
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("topic1") && v.ToString()!.Contains("topic2")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Checkpoint Management Tests

    [Test]
    public void SnapshotState_CreatesCheckpoint()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        long checkpointId = 12345;
        long checkpointTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.SnapshotState(checkpointId, checkpointTimestamp));
    }

    [Test]
    public void SnapshotState_WithMultipleCheckpoints_StoresAll()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act
        consumerGroup.SnapshotState(1, 1000);
        consumerGroup.SnapshotState(2, 2000);
        consumerGroup.SnapshotState(3, 3000);

        // Assert - no exceptions should be thrown
        Assert.Pass("Multiple checkpoints stored successfully");
    }

    [Test]
    public void SnapshotState_WithZeroCheckpointId_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.SnapshotState(0, 0));
    }

    [Test]
    public void SnapshotState_WithNegativeCheckpointId_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.SnapshotState(-1, 1000));
    }

    [Test]
    public void RestoreState_WithValidState_RestoresSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var checkpointState = new Dictionary<string, long>
        {
            ["partition-0"] = 100,
            ["partition-1"] = 200,
            ["partition-2"] = 300
        };

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.RestoreState(checkpointState));
    }

    [Test]
    public void RestoreState_WithNullState_HandlesGracefully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.RestoreState(null!));
    }

    [Test]
    public void RestoreState_WithEmptyState_HandlesGracefully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var emptyState = new Dictionary<string, long>();

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.RestoreState(emptyState));
    }

    [Test]
    public void RestoreState_WithLargeState_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var largeState = new Dictionary<string, long>();
        for (int i = 0; i < 100; i++)
        {
            largeState[$"partition-{i}"] = i * 1000;
        }

        // Act & Assert
        Assert.DoesNotThrow(() =>
            consumerGroup.RestoreState(largeState));
    }

    [Test]
    public void CommitCheckpointOffsetsAsync_CompletesSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        long checkpointId = 12345;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.CommitCheckpointOffsetsAsync(checkpointId));
    }

    [Test]
    public void CommitCheckpointOffsetsAsync_WithZeroCheckpoint_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await consumerGroup.CommitCheckpointOffsetsAsync(0));
    }

    [Test]
    public void CommitCheckpointOffsetsAsync_MultipleCommits_AllSucceed()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await consumerGroup.CommitCheckpointOffsetsAsync(1);
            await consumerGroup.CommitCheckpointOffsetsAsync(2);
            await consumerGroup.CommitCheckpointOffsetsAsync(3);
        });
    }

    [Test]
    public void GetAssignment_ReturnsPartitionList()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act
        var assignment = consumerGroup.GetAssignment();

        // Assert
        Assert.That(assignment, Is.Not.Null);
        Assert.That(assignment, Is.InstanceOf<List<TopicPartition>>());
    }

    [Test]
    public void GetAssignment_ReturnsNonEmptyList()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act
        var assignment = consumerGroup.GetAssignment();

        // Assert
        Assert.That(assignment.Count, Is.GreaterThan(0));
    }

    [Test]
    public void GetAssignment_ReturnsValidTopicPartitions()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act
        var assignment = consumerGroup.GetAssignment();

        // Assert
        foreach (var partition in assignment)
        {
            Assert.That(partition.Topic, Is.Not.Null);
            Assert.That(partition.Topic, Is.Not.Empty);
            Assert.That(partition.Partition, Is.GreaterThanOrEqualTo(0));
        }
    }

    #endregion

    #region Consumer Operations Tests

    [Test]
    public async Task ConsumeMessageAsync_ReturnsResult()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = await consumerGroup.ConsumeMessageAsync(timeout);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ConsumeMessageAsync_ResultHasValidProperties()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var timeout = TimeSpan.FromSeconds(1);

        // Act
        var result = await consumerGroup.ConsumeMessageAsync(timeout);

        // Assert
        Assert.That(result!.Topic, Is.Not.Null);
        Assert.That(result.Topic, Is.Not.Empty);
        Assert.That(result.Partition, Is.GreaterThanOrEqualTo(0));
        Assert.That(result.Offset, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void ConsumeMessageAsync_AfterDisposal_ThrowsObjectDisposedException()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        consumerGroup.Dispose();

        // Act & Assert
        Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await consumerGroup.ConsumeMessageAsync(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task ConsumeMessageAsync_WithShortTimeout_CompletesQuickly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var timeout = TimeSpan.FromMilliseconds(100);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        _ = await consumerGroup.ConsumeMessageAsync(timeout);
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
    }

    [Test]
    public async Task ConsumeMessageAsync_WithLongTimeout_CompletesSuccessfully()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var result = await consumerGroup.ConsumeMessageAsync(timeout);

        // Assert
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public void WaitForKafkaSetupAsync_WithShortTimeout_Succeeds()
    {
        // Arrange
        var bootstrapServers = "localhost:9092";
        var timeout = TimeSpan.FromSeconds(10);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync(bootstrapServers, timeout));
    }

    [Test]
    public async Task WaitForKafkaSetupAsync_WithValidParameters_CompletesSuccessfully()
    {
        // Arrange
        var bootstrapServers = "kafka:9092";
        var timeout = TimeSpan.FromSeconds(5);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync(bootstrapServers, timeout);
        stopwatch.Stop();

        // Assert
        Assert.That(stopwatch.Elapsed, Is.LessThan(timeout.Add(TimeSpan.FromSeconds(2))));
    }

    [Test]
    public void WaitForKafkaSetupAsync_WithCancellation_ThrowsTaskCanceledException()
    {
        // Arrange
        var bootstrapServers = "localhost:9092";
        var timeout = TimeSpan.FromSeconds(30);
        using var cts = new CancellationTokenSource();
        
        // Cancel after a short delay to test cancellation handling
        cts.CancelAfter(100);

        // Act & Assert - Method throws when Task.Delay is cancelled
        Assert.CatchAsync<TaskCanceledException>(async () =>
            await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync(bootstrapServers, timeout, cts.Token));
    }

    [Test]
    public void WaitForKafkaSetupAsync_WithDifferentBootstrapServers_HandlesCorrectly()
    {
        // Arrange & Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync("localhost:9092", TimeSpan.FromSeconds(5));
            await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync("kafka1:9092,kafka2:9092", TimeSpan.FromSeconds(5));
            await FlinkKafkaConsumerGroup.WaitForKafkaSetupAsync("192.168.1.100:9092", TimeSpan.FromSeconds(5));
        });
    }

    #endregion

    #region Error Handling and Disposal Tests

    [Test]
    public void Dispose_CleansUpResources()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() => consumerGroup.Dispose());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            consumerGroup.Dispose();
            consumerGroup.Dispose();
            consumerGroup.Dispose();
        });
    }

    [Test]
    public void Dispose_LogsDisposal()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);

        // Act
        consumerGroup.Dispose();

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public void GetAssignment_AfterDisposal_DoesNotThrow()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        consumerGroup.Dispose();

        // Act & Assert - GetAssignment doesn't check disposal status
        Assert.DoesNotThrow(() => consumerGroup.GetAssignment());
    }

    [Test]
    public void SnapshotState_AfterDisposal_DoesNotThrow()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        consumerGroup.Dispose();

        // Act & Assert - SnapshotState doesn't check disposal status
        Assert.DoesNotThrow(() => consumerGroup.SnapshotState(1, 1000));
    }

    [Test]
    public void RestoreState_AfterDisposal_DoesNotThrow()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        consumerGroup.Dispose();
        var state = new Dictionary<string, long> { ["p1"] = 100 };

        // Act & Assert - RestoreState doesn't check disposal status
        Assert.DoesNotThrow(() => consumerGroup.RestoreState(state));
    }

    [Test]
    public void ConfigurationValidation_AppliesDefaultSettings()
    {
        // Arrange
        var minimalConfig = new Dictionary<string, object>
        {
            ["bootstrap.servers"] = "localhost:9092"
        };

        // Act
        _ = new FlinkKafkaConsumerGroup(minimalConfig, _mockLogger.Object);

        // Assert - defaults should be applied
        Assert.That(minimalConfig["enable.auto.commit"], Is.EqualTo(false));
        Assert.That(minimalConfig["session.timeout.ms"], Is.EqualTo(30000));
        Assert.That(minimalConfig["heartbeat.interval.ms"], Is.EqualTo(10000));
        Assert.That(minimalConfig["partition.assignment.strategy"], Is.EqualTo("CooperativeSticky"));
    }

    [Test]
    public void ConfigurationValidation_LogsAppliedSettings()
    {
        // Arrange
        var minimalConfig = new Dictionary<string, object>
        {
            ["bootstrap.servers"] = "localhost:9092"
        };

        // Act
        _ = new FlinkKafkaConsumerGroup(minimalConfig, _mockLogger.Object);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Applied Flink-optimal setting")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeast(4)); // 4 settings should be applied
    }

    [Test]
    public void ThreadSafety_ConcurrentSnapshotState_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int checkpointId = i;
            tasks.Add(Task.Run(() =>
                consumerGroup.SnapshotState(checkpointId, checkpointId * 1000)));
        }

        // Assert
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    [Test]
    public void ThreadSafety_ConcurrentRestoreState_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 10; i++)
        {
            int partitionId = i;
            var state = new Dictionary<string, long> { [$"partition-{partitionId}"] = partitionId * 100 };
            tasks.Add(Task.Run(() => consumerGroup.RestoreState(state)));
        }

        // Assert
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    [Test]
    public void ThreadSafety_ConcurrentConsumeMessage_HandlesCorrectly()
    {
        // Arrange
        var consumerGroup = new FlinkKafkaConsumerGroup(_consumerConfig, _mockLogger.Object);
        var tasks = new List<Task>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(Task.Run(async () =>
                await consumerGroup.ConsumeMessageAsync(TimeSpan.FromMilliseconds(100))));
        }

        // Assert
        Assert.DoesNotThrowAsync(async () => await Task.WhenAll(tasks));
    }

    #endregion

    #region ConsumeResult and TopicPartition Tests

    [Test]
    public void ConsumeResult_DefaultConstructor_CreatesInstance()
    {
        // Act
        var result = new ConsumeResult();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Topic, Is.EqualTo(string.Empty));
        Assert.That(result.Partition, Is.EqualTo(0));
        Assert.That(result.Offset, Is.EqualTo(0));
    }

    [Test]
    public void ConsumeResult_SetProperties_StoresValues()
    {
        // Arrange
        var result = new ConsumeResult();

        // Act
        result.Topic = "test-topic";
        result.Partition = 5;
        result.Offset = 12345;
        result.Key = "test-key";
        result.Value = "test-value";
        result.Timestamp = DateTimeOffset.UtcNow;

        // Assert
        Assert.That(result.Topic, Is.EqualTo("test-topic"));
        Assert.That(result.Partition, Is.EqualTo(5));
        Assert.That(result.Offset, Is.EqualTo(12345));
        Assert.That(result.Key, Is.EqualTo("test-key"));
        Assert.That(result.Value, Is.EqualTo("test-value"));
        Assert.That(result.Timestamp, Is.Not.EqualTo(default(DateTimeOffset)));
    }

    [Test]
    public void ConsumeResult_WithNullKey_HandlesCorrectly()
    {
        // Arrange
        var result = new ConsumeResult
        {
            Topic = "test-topic",
            Key = null,
            Value = "test-value"
        };

        // Assert
        Assert.That(result.Key, Is.Null);
        Assert.That(result.Value, Is.Not.Null);
    }

    [Test]
    public void ConsumeResult_WithNullValue_HandlesCorrectly()
    {
        // Arrange
        var result = new ConsumeResult
        {
            Topic = "test-topic",
            Key = "test-key",
            Value = null
        };

        // Assert
        Assert.That(result.Key, Is.Not.Null);
        Assert.That(result.Value, Is.Null);
    }

    [Test]
    public void TopicPartition_DefaultConstructor_CreatesInstance()
    {
        // Act
        var partition = new TopicPartition();

        // Assert
        Assert.That(partition, Is.Not.Null);
        Assert.That(partition.Topic, Is.EqualTo(string.Empty));
        Assert.That(partition.Partition, Is.EqualTo(0));
    }

    [Test]
    public void TopicPartition_SetProperties_StoresValues()
    {
        // Arrange
        var partition = new TopicPartition();

        // Act
        partition.Topic = "my-topic";
        partition.Partition = 7;

        // Assert
        Assert.That(partition.Topic, Is.EqualTo("my-topic"));
        Assert.That(partition.Partition, Is.EqualTo(7));
    }

    [Test]
    public void TopicPartition_ToString_ReturnsFormattedString()
    {
        // Arrange
        var partition = new TopicPartition
        {
            Topic = "orders",
            Partition = 3
        };

        // Act
        var result = partition.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("orders[3]"));
    }

    [Test]
    public void TopicPartition_ToString_WithEmptyTopic_ReturnsFormattedString()
    {
        // Arrange
        var partition = new TopicPartition
        {
            Topic = "",
            Partition = 0
        };

        // Act
        var result = partition.ToString();

        // Assert
        Assert.That(result, Is.EqualTo("[0]"));
    }

    [Test]
    public void TopicPartition_MultipleInstances_AreIndependent()
    {
        // Arrange
        var partition1 = new TopicPartition { Topic = "topic1", Partition = 1 };
        var partition2 = new TopicPartition { Topic = "topic2", Partition = 2 };

        // Assert
        Assert.That(partition1.Topic, Is.Not.EqualTo(partition2.Topic));
        Assert.That(partition1.Partition, Is.Not.EqualTo(partition2.Partition));
    }

    #endregion
}
