using System;
using FlinkDotNet.DataStream.Checkpoint;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests for CheckpointConfig covering all properties, methods, and edge cases.
    /// Tests checkpoint storage, timeout configuration, concurrent checkpoint limits, and externalized checkpoints.
    /// </summary>
    [TestFixture]
    public class CheckpointConfigTests
    {
        [Test]
        public void Constructor_CreatesWithDefaultValues()
        {
            // Act
            var config = new CheckpointConfig();

            // Assert
            Assert.That(config.GetCheckpointStorage(), Is.Null);
            Assert.That(config.GetCheckpointStoragePath(), Is.Null);
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(600000)); // 10 minutes default
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(int.MaxValue));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        #region SetCheckpointStorage Tests

        [Test]
        public void SetCheckpointStorage_WithValidPath_SetsStorageAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();
            var path = "file:///tmp/checkpoints";

            // Act
            var result = config.SetCheckpointStorage(path);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetCheckpointStorage(), Is.Not.Null);
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(path));
            Assert.That(config.GetCheckpointStorage(), Is.InstanceOf<FileSystemCheckpointStorage>());
        }

        [Test]
        public void SetCheckpointStorage_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage((string)null!));
            Assert.That(ex!.Message, Does.Contain("Checkpoint storage path cannot be null or empty"));
            Assert.That(ex.ParamName, Is.EqualTo("path"));
        }

        [Test]
        public void SetCheckpointStorage_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage(""));
            Assert.That(ex!.Message, Does.Contain("Checkpoint storage path cannot be null or empty"));
        }

        [Test]
        public void SetCheckpointStorage_WithWhitespacePath_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage("   "));
            Assert.That(ex!.Message, Does.Contain("Checkpoint storage path cannot be null or empty"));
        }

        [Test]
        public void SetCheckpointStorage_WithStorageInterface_SetsStorageAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();
            var storage = new FileSystemCheckpointStorage("hdfs:///checkpoints");

            // Act
            var result = config.SetCheckpointStorage(storage);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetCheckpointStorage(), Is.SameAs(storage));
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("hdfs:///checkpoints"));
        }

        [Test]
        public void SetCheckpointStorage_WithNullStorage_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => config.SetCheckpointStorage((ICheckpointStorage)null!));
            Assert.That(ex!.ParamName, Is.EqualTo("storage"));
        }

        #endregion

        #region SetCheckpointTimeout Tests

        [Test]
        public void SetCheckpointTimeout_WithValidTimeout_SetsTimeoutAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.SetCheckpointTimeout(30000); // 30 seconds

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(30000));
        }

        [Test]
        public void SetCheckpointTimeout_WithMinimalPositiveTimeout_SetsTimeout()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            config.SetCheckpointTimeout(1);

            // Assert
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(1));
        }

        [Test]
        public void SetCheckpointTimeout_WithZeroTimeout_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetCheckpointTimeout(0));
            Assert.That(ex!.Message, Does.Contain("Checkpoint timeout must be positive"));
            Assert.That(ex.ParamName, Is.EqualTo("timeoutMs"));
        }

        [Test]
        public void SetCheckpointTimeout_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetCheckpointTimeout(-1));
            Assert.That(ex!.Message, Does.Contain("Checkpoint timeout must be positive"));
        }

        #endregion

        #region SetMinPauseBetweenCheckpoints Tests

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithValidPause_SetsPauseAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.SetMinPauseBetweenCheckpoints(5000); // 5 seconds

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(5000));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithZeroPause_SetsPause()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            config.SetMinPauseBetweenCheckpoints(0);

            // Assert
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithNegativePause_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetMinPauseBetweenCheckpoints(-1));
            Assert.That(ex!.Message, Does.Contain("Minimum pause must be non-negative"));
            Assert.That(ex.ParamName, Is.EqualTo("pauseMs"));
        }

        #endregion

        #region SetMaxConcurrentCheckpoints Tests

        [Test]
        public void SetMaxConcurrentCheckpoints_WithValidCount_SetsCountAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.SetMaxConcurrentCheckpoints(3);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(3));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithOne_SetsCount()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            config.SetMaxConcurrentCheckpoints(1);

            // Assert
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetMaxConcurrentCheckpoints(0));
            Assert.That(ex!.Message, Does.Contain("Max concurrent checkpoints must be at least 1"));
            Assert.That(ex.ParamName, Is.EqualTo("maxConcurrent"));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetMaxConcurrentCheckpoints(-1));
            Assert.That(ex!.Message, Does.Contain("Max concurrent checkpoints must be at least 1"));
        }

        #endregion

        #region SetTolerableCheckpointFailureNumber Tests

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithValidCount_SetsCountAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.SetTolerableCheckpointFailureNumber(5);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(5));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithZero_SetsCount()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            config.SetTolerableCheckpointFailureNumber(0);

            // Assert
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(0));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithNegative_ThrowsArgumentException()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => config.SetTolerableCheckpointFailureNumber(-1));
            Assert.That(ex!.Message, Does.Contain("Tolerable failures must be non-negative"));
            Assert.That(ex.ParamName, Is.EqualTo("tolerableFailures"));
        }

        #endregion

        #region Externalized Checkpoints Tests

        [Test]
        public void EnableExternalizedCheckpoints_WithDeleteOnCancellation_EnablesAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        [Test]
        public void EnableExternalizedCheckpoints_WithRetainOnCancellation_EnablesAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var result = config.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        [Test]
        public void DisableExternalizedCheckpoints_DisablesAndReturnsThis()
        {
            // Arrange
            var config = new CheckpointConfig();
            config.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Act
            var result = config.DisableExternalizedCheckpoints();

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
        }

        [Test]
        public void DisableExternalizedCheckpoints_WhenAlreadyDisabled_RemainsDisabled()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            config.DisableExternalizedCheckpoints();

            // Assert
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_AllSettersReturnThis()
        {
            // Arrange
            var config = new CheckpointConfig();
            var storage = new FileSystemCheckpointStorage("file:///tmp/checkpoints");

            // Act
            var result = config
                .SetCheckpointStorage(storage)
                .SetCheckpointTimeout(30000)
                .SetMinPauseBetweenCheckpoints(5000)
                .SetMaxConcurrentCheckpoints(2)
                .SetTolerableCheckpointFailureNumber(10)
                .EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(30000));
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(5000));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(2));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(10));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
        }

        #endregion

        #region Edge Cases and Integration Tests

        [Test]
        public void SetCheckpointStorage_OverwritesPreviousStorage()
        {
            // Arrange
            var config = new CheckpointConfig();
            var storage1 = new FileSystemCheckpointStorage("file:///tmp/checkpoints1");
            var storage2 = new FileSystemCheckpointStorage("file:///tmp/checkpoints2");

            // Act
            config.SetCheckpointStorage(storage1);
            config.SetCheckpointStorage(storage2);

            // Assert
            Assert.That(config.GetCheckpointStorage(), Is.SameAs(storage2));
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("file:///tmp/checkpoints2"));
        }

        [Test]
        public void SetCheckpointStorage_PathOverwritesStorageInterface()
        {
            // Arrange
            var config = new CheckpointConfig();
            var storage = new FileSystemCheckpointStorage("hdfs:///checkpoints");

            // Act
            config.SetCheckpointStorage(storage);
            config.SetCheckpointStorage("s3://bucket/checkpoints");

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("s3://bucket/checkpoints"));
            Assert.That(config.GetCheckpointStorage(), Is.InstanceOf<FileSystemCheckpointStorage>());
        }

        [Test]
        public void GetCheckpointStorage_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var storage = config.GetCheckpointStorage();

            // Assert
            Assert.That(storage, Is.Null);
        }

        [Test]
        public void GetCheckpointStoragePath_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var config = new CheckpointConfig();

            // Act
            var path = config.GetCheckpointStoragePath();

            // Assert
            Assert.That(path, Is.Null);
        }

        #endregion
    }
}