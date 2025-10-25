using System;
using FlinkDotNet.DataStream.Checkpoint;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class FileSystemCheckpointStorageTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidPath_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("/tmp/checkpoints");

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("/tmp/checkpoints"));
            Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(-1)); // Default
        }

        [Test]
        public void Constructor_WithPathAndThreshold_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("/checkpoints", 2048);

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("/checkpoints"));
            Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(2048));
        }

        [Test]
        public void Constructor_WithHdfsPath_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("hdfs://namenode:8020/flink/checkpoints");

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("hdfs://namenode:8020/flink/checkpoints"));
        }

        [Test]
        public void Constructor_WithS3Path_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("s3://my-bucket/checkpoints");

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("s3://my-bucket/checkpoints"));
        }

        [Test]
        public void Constructor_WithAzurePath_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("wasb://container@account/checkpoints");

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("wasb://container@account/checkpoints"));
        }

        [Test]
        public void Constructor_WithGcsPath_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("gs://my-bucket/checkpoints");

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo("gs://my-bucket/checkpoints"));
        }

        [Test]
        public void Constructor_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new FileSystemCheckpointStorage(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("checkpointPath"));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void Constructor_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new FileSystemCheckpointStorage(string.Empty));
            Assert.That(ex!.ParamName, Is.EqualTo("checkpointPath"));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void Constructor_WithWhitespacePath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => new FileSystemCheckpointStorage("   "));
            Assert.That(ex!.ParamName, Is.EqualTo("checkpointPath"));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void Constructor_WithZeroThreshold_CreatesInstance()
        {
            // Arrange & Act
            var storage = new FileSystemCheckpointStorage("/checkpoints", 0);

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNegativeThreshold_CreatesInstance()
        {
            // Arrange & Act - Negative threshold means use default
            var storage = new FileSystemCheckpointStorage("/checkpoints", -100);

            // Assert
            Assert.That(storage, Is.Not.Null);
            Assert.That(storage.GetFileSizeThreshold(), Is.EqualTo(-100));
        }

        #endregion

        #region GetCheckpointPath Tests

        [Test]
        public void GetCheckpointPath_ReturnsConstructorPath()
        {
            // Arrange
            var expectedPath = "/var/flink/checkpoints";
            var storage = new FileSystemCheckpointStorage(expectedPath);

            // Act
            var actualPath = storage.GetCheckpointPath();

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GetCheckpointPath_WithComplexPath_ReturnsExactPath()
        {
            // Arrange
            var expectedPath = "hdfs://namenode:8020/user/flink/checkpoints/job-123";
            var storage = new FileSystemCheckpointStorage(expectedPath);

            // Act
            var actualPath = storage.GetCheckpointPath();

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        [Test]
        public void GetCheckpointPath_WithTrailingSlash_ReturnsWithSlash()
        {
            // Arrange
            var expectedPath = "/checkpoints/";
            var storage = new FileSystemCheckpointStorage(expectedPath);

            // Act
            var actualPath = storage.GetCheckpointPath();

            // Assert
            Assert.That(actualPath, Is.EqualTo(expectedPath));
        }

        #endregion

        #region GetFileSizeThreshold Tests

        [Test]
        public void GetFileSizeThreshold_WithDefaultValue_ReturnsNegativeOne()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("/checkpoints");

            // Act
            var threshold = storage.GetFileSizeThreshold();

            // Assert
            Assert.That(threshold, Is.EqualTo(-1));
        }

        [Test]
        public void GetFileSizeThreshold_WithCustomValue_ReturnsCustomValue()
        {
            // Arrange
            var expectedThreshold = 4096;
            var storage = new FileSystemCheckpointStorage("/checkpoints", expectedThreshold);

            // Act
            var threshold = storage.GetFileSizeThreshold();

            // Assert
            Assert.That(threshold, Is.EqualTo(expectedThreshold));
        }

        [Test]
        public void GetFileSizeThreshold_WithLargeValue_ReturnsLargeValue()
        {
            // Arrange
            var expectedThreshold = 1024 * 1024 * 10; // 10 MB
            var storage = new FileSystemCheckpointStorage("/checkpoints", expectedThreshold);

            // Act
            var threshold = storage.GetFileSizeThreshold();

            // Assert
            Assert.That(threshold, Is.EqualTo(expectedThreshold));
        }

        [Test]
        public void GetFileSizeThreshold_WithZero_ReturnsZero()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("/checkpoints", 0);

            // Act
            var threshold = storage.GetFileSizeThreshold();

            // Assert
            Assert.That(threshold, Is.EqualTo(0));
        }

        #endregion

        #region SupportsHighAvailability Tests

        [Test]
        public void SupportsHighAvailability_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("/checkpoints");

            // Act
            var supportsHA = storage.SupportsHighAvailability();

            // Assert
            Assert.That(supportsHA, Is.True);
        }

        [Test]
        public void SupportsHighAvailability_WithHdfsPath_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("hdfs://namenode/checkpoints");

            // Act
            var supportsHA = storage.SupportsHighAvailability();

            // Assert
            Assert.That(supportsHA, Is.True);
        }

        [Test]
        public void SupportsHighAvailability_WithS3Path_ReturnsTrue()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("s3://bucket/checkpoints");

            // Act
            var supportsHA = storage.SupportsHighAvailability();

            // Assert
            Assert.That(supportsHA, Is.True);
        }

        [Test]
        public void SupportsHighAvailability_WithLocalPath_ReturnsTrue()
        {
            // Arrange - Even local paths return true as per implementation
            var storage = new FileSystemCheckpointStorage("file:///tmp/checkpoints");

            // Act
            var supportsHA = storage.SupportsHighAvailability();

            // Assert
            Assert.That(supportsHA, Is.True);
        }

        #endregion

        #region Integration Tests

        [Test]
        public void FileSystemCheckpointStorage_WithMultiplePaths_MaintainsIndependentState()
        {
            // Arrange
            var storage1 = new FileSystemCheckpointStorage("/path1", 1024);
            var storage2 = new FileSystemCheckpointStorage("/path2", 2048);

            // Act & Assert
            Assert.That(storage1.GetCheckpointPath(), Is.EqualTo("/path1"));
            Assert.That(storage1.GetFileSizeThreshold(), Is.EqualTo(1024));

            Assert.That(storage2.GetCheckpointPath(), Is.EqualTo("/path2"));
            Assert.That(storage2.GetFileSizeThreshold(), Is.EqualTo(2048));
        }

        [Test]
        public void FileSystemCheckpointStorage_UsedWithCheckpointConfig_WorksCorrectly()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("s3://my-bucket/checkpoints", 2048);
            var config = new CheckpointConfig();

            // Act
            config.SetCheckpointStorage(storage);

            // Assert
            Assert.That(config.GetCheckpointStorage(), Is.SameAs(storage));
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("s3://my-bucket/checkpoints"));
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void Constructor_WithVeryLongPath_CreatesInstance()
        {
            // Arrange
            var longPath = new string('a', 500);

            // Act
            var storage = new FileSystemCheckpointStorage(longPath);

            // Assert
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo(longPath));
        }

        [Test]
        public void Constructor_WithSpecialCharactersInPath_CreatesInstance()
        {
            // Arrange
            var specialPath = "/checkpoints/job-$123/checkpoint-@456";

            // Act
            var storage = new FileSystemCheckpointStorage(specialPath);

            // Assert
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo(specialPath));
        }

        [Test]
        public void Constructor_WithUnicodeCharactersInPath_CreatesInstance()
        {
            // Arrange
            var unicodePath = "/checkpoints/用户/数据";

            // Act
            var storage = new FileSystemCheckpointStorage(unicodePath);

            // Assert
            Assert.That(storage.GetCheckpointPath(), Is.EqualTo(unicodePath));
        }

        #endregion
    }
}
