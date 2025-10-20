using FlinkDotNet.DataStream.State;
using FlinkDotNet.DataStream.Checkpoint;
using FlinkDotNet.DataStream.Watermarks;
using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests for State, Checkpoint, and Watermark components
/// </summary>
[TestFixture]
public class StateAndCheckpointTests
{
    #region EmbeddedRocksDBStateBackend Tests

    [Test]
    public void EmbeddedRocksDBStateBackend_DefaultConstructor_CreatesInstance()
    {
        // Act
        var backend = new EmbeddedRocksDBStateBackend();

        // Assert
        Assert.That(backend, Is.Not.Null);
        Assert.That(backend.GetName(), Is.EqualTo("EmbeddedRocksDBStateBackend"));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_ConstructorWithFlag_SetsIncrementalCheckpointing()
    {
        // Act
        var backend = new EmbeddedRocksDBStateBackend(enableIncrementalCheckpointing: false);

        // Assert
        Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.False);
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SetPredefinedOptions_UpdatesOptions()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act
        backend.SetPredefinedOptions(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED);

        // Assert
        Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SetPredefinedOptions_ReturnsThis()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act
        var result = backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED);

        // Assert
        Assert.That(result, Is.SameAs(backend));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SetDbStoragePath_UpdatesPath()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();
        var path = "/tmp/rocksdb";

        // Act
        backend.SetDbStoragePath(path);

        // Assert
        Assert.That(backend.GetDbStoragePath(), Is.EqualTo(path));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SetDbStoragePath_WithNullPath_ThrowsArgumentException()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => backend.SetDbStoragePath(null!));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SetDbStoragePath_WithEmptyPath_ThrowsArgumentException()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => backend.SetDbStoragePath(""));
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_EnableIncrementalCheckpointing_UpdatesFlag()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend(false);

        // Act
        backend.EnableIncrementalCheckpointing(true);

        // Assert
        Assert.That(backend.IsIncrementalCheckpointingEnabled(), Is.True);
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_SupportsIncrementalCheckpointing_ReturnsTrue()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act
        var supports = backend.SupportsIncrementalCheckpointing();

        // Assert
        Assert.That(supports, Is.True);
    }

    [Test]
    public void EmbeddedRocksDBStateBackend_AllPredefinedOptions_AreValid()
    {
        // Arrange
        var backend = new EmbeddedRocksDBStateBackend();

        // Act & Assert - Test all predefined options
        backend.SetPredefinedOptions(RocksDBPredefinedOptions.DEFAULT);
        Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.DEFAULT));

        backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED);
        Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED));

        backend.SetPredefinedOptions(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED);
        Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.FLASH_SSD_OPTIMIZED));

        backend.SetPredefinedOptions(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM);
        Assert.That(backend.GetPredefinedOptions(), Is.EqualTo(RocksDBPredefinedOptions.SPINNING_DISK_OPTIMIZED_HIGH_MEM));
    }

    #endregion

    #region HashMapStateBackend Tests

    [Test]
    public void HashMapStateBackend_Constructor_CreatesInstance()
    {
        // Act
        var backend = new HashMapStateBackend();

        // Assert
        Assert.That(backend, Is.Not.Null);
        Assert.That(backend.GetName(), Is.EqualTo("HashMapStateBackend"));
    }

    [Test]
    public void HashMapStateBackend_SupportsIncrementalCheckpointing_ReturnsFalse()
    {
        // Arrange
        var backend = new HashMapStateBackend();

        // Act
        var supports = backend.SupportsIncrementalCheckpointing();

        // Assert
        Assert.That(supports, Is.False);
    }

    #endregion

    #region FileSystemCheckpointStorage Tests

    [Test]
    public void FileSystemCheckpointStorage_Constructor_SetsCheckpointPath()
    {
        // Arrange
        var path = "file:///tmp/checkpoints";

        // Act
        var storage = new FileSystemCheckpointStorage(path);

        // Assert
        Assert.That(storage.GetCheckpointPath(), Is.EqualTo(path));
    }

    [Test]
    public void FileSystemCheckpointStorage_Constructor_WithNullPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FileSystemCheckpointStorage(null!));
    }

    [Test]
    public void FileSystemCheckpointStorage_Constructor_WithEmptyPath_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new FileSystemCheckpointStorage(""));
    }

    [Test]
    public void FileSystemCheckpointStorage_Constructor_WithFileSizeThreshold_SetsThreshold()
    {
        // Arrange
        var path = "hdfs://namenode:9000/checkpoints";
        var threshold = 2048;

        // Act
        var storage = new FileSystemCheckpointStorage(path, threshold);

        // Assert
        Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(threshold));
    }

    [Test]
    public void FileSystemCheckpointStorage_Constructor_WithoutThreshold_UsesDefault()
    {
        // Arrange
        var path = "s3://bucket/checkpoints";

        // Act
        var storage = new FileSystemCheckpointStorage(path);

        // Assert
        Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(-1));
    }

    [Test]
    public void FileSystemCheckpointStorage_SupportsHighAvailability_ReturnsTrue()
    {
        // Arrange
        var storage = new FileSystemCheckpointStorage("file:///tmp/checkpoints");

        // Act
        var supports = storage.SupportsHighAvailability();

        // Assert
        Assert.That(supports, Is.True);
    }

    [Test]
    public void FileSystemCheckpointStorage_VariousFileSystemPaths_AreAccepted()
    {
        // Test various file system URIs
        var paths = new[]
        {
            "file:///tmp/checkpoints",
            "hdfs://namenode:9000/checkpoints",
            "s3://bucket/path/to/checkpoints",
            "wasb://container@account/checkpoints",
            "gs://bucket/checkpoints"
        };

        foreach (var path in paths)
        {
            var storage = new FileSystemCheckpointStorage(path);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo(path));
        }
    }

    #endregion

    #region WatermarkStrategy Tests

    [Test]
    public void WatermarkStrategy_ForBoundedOutOfOrderness_CreatesStrategy()
    {
        // Act
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

        // Assert
        Assert.That(strategy, Is.Not.Null);
        Assert.That(strategy.IsMonotonous, Is.False);
        Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(TimeSpan.FromSeconds(5)));
    }

    [Test]
    public void WatermarkStrategy_ForMonotonousTimestamps_CreatesStrategy()
    {
        // Act
        var strategy = WatermarkStrategy<string>.ForMonotonousTimestamps();

        // Assert
        Assert.That(strategy, Is.Not.Null);
        Assert.That(strategy.IsMonotonous, Is.True);
        Assert.That(strategy.MaxOutOfOrderness, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void WatermarkStrategy_WithTimestampAssigner_SetsAssigner()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

        // Act
        strategy.WithTimestampAssigner(s => long.Parse(s));

        // Assert
        Assert.That(strategy.HasTimestampAssigner, Is.True);
    }

    [Test]
    public void WatermarkStrategy_WithTimestampAssigner_ReturnsThis()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

        // Act
        var result = strategy.WithTimestampAssigner(s => long.Parse(s));

        // Assert
        Assert.That(result, Is.SameAs(strategy));
    }

    [Test]
    public void WatermarkStrategy_ExtractTimestamp_WithAssigner_ReturnsTimestamp()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5))
            .WithTimestampAssigner(s => long.Parse(s));

        // Act
        var timestamp = strategy.ExtractTimestamp("12345", 0);

        // Assert
        Assert.That(timestamp, Is.EqualTo(12345));
    }

    [Test]
    public void WatermarkStrategy_ExtractTimestamp_WithoutAssigner_ThrowsInvalidOperationException()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => strategy.ExtractTimestamp("12345", 0));
    }

    [Test]
    public void WatermarkStrategy_GetCurrentWatermark_ForMonotonous_ReturnsMaxTimestamp()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForMonotonousTimestamps();

        // Act
        var watermark = strategy.GetCurrentWatermark(10000);

        // Assert
        Assert.That(watermark, Is.EqualTo(10000));
    }

    [Test]
    public void WatermarkStrategy_GetCurrentWatermark_ForBoundedOutOfOrderness_SubtractsDelay()
    {
        // Arrange
        var strategy = WatermarkStrategy<string>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));

        // Act
        var watermark = strategy.GetCurrentWatermark(10000);

        // Assert
        Assert.That(watermark, Is.EqualTo(5000)); // 10000 - 5000ms
    }

    [Test]
    public void WatermarkStrategy_WithITimestampAssigner_SetsAssigner()
    {
        // Arrange
        var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));
        var assigner = new TestTimestampAssigner();

        // Act
        strategy.WithTimestampAssigner(assigner);

        // Assert
        Assert.That(strategy.HasTimestampAssigner, Is.True);
    }

    [Test]
    public void WatermarkStrategy_ExtractTimestamp_WithITimestampAssigner_CallsExtractTimestamp()
    {
        // Arrange
        var strategy = WatermarkStrategy<TestEvent>.ForBoundedOutOfOrderness(TimeSpan.FromSeconds(5));
        var assigner = new TestTimestampAssigner();
        strategy.WithTimestampAssigner(assigner);

        var testEvent = new TestEvent { Timestamp = 12345 };

        // Act
        var timestamp = strategy.ExtractTimestamp(testEvent, 0);

        // Assert
        Assert.That(timestamp, Is.EqualTo(12345));
    }

    #endregion

    #region Watermark and Time Tests

    [Test]
    public void Watermark_Constructor_SetsTimestamp()
    {
        // Act
        var watermark = new Watermark(12345);

        // Assert
        Assert.That(watermark.GetTimestamp(), Is.EqualTo(12345));
    }

    [Test]
    public void Watermark_ToString_ContainsTimestamp()
    {
        // Arrange
        var watermark = new Watermark(12345);

        // Act
        var str = watermark.ToString();

        // Assert
        Assert.That(str, Does.Contain("12345"));
    }

    [Test]
    public void Time_ToString_ReturnsFormattedString()
    {
        // Arrange
        var time = Time.Milliseconds(1500);

        // Act
        var str = time.ToString();

        // Assert
        Assert.That(str, Does.Contain("1500"));
    }

    #endregion

    #region Helper Classes

    private class TestEvent
    {
        public long Timestamp { get; set; }
    }

    private class TestTimestampAssigner : ITimestampAssigner<TestEvent>
    {
        public long ExtractTimestamp(TestEvent element, long previousElementTimestamp)
        {
            return element.Timestamp;
        }
    }

    #endregion
}