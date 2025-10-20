namespace Flink.JobBuilder.Tests.Tests
{
    [TestFixture]
    public class CheckpointConfigTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_CreatesInstanceWithDefaultValues()
        {
            // Act
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.GetCheckpointStorage(), Is.Null);
            Assert.That(config.GetCheckpointStoragePath(), Is.Null);
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(600000)); // 10 minutes default
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(int.MaxValue));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        #endregion

        #region SetCheckpointStorage(string) Tests

        [Test]
        public void SetCheckpointStorage_WithValidPath_SetsStorageAndPath()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var path = "file:///tmp/checkpoints";

            // Act
            var result = config.SetCheckpointStorage(path);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(path));
            Assert.That(config.GetCheckpointStorage(), Is.Not.Null);
            Assert.That(config.GetCheckpointStorage(), Is.InstanceOf<FlinkDotNet.DataStream.Checkpoint.FileSystemCheckpointStorage>());
        }

        [Test]
        public void SetCheckpointStorage_WithLocalPath_CreatesFileSystemStorage()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var path = "C:/checkpoints";

            // Act
            config.SetCheckpointStorage(path);

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(path));
            Assert.That(config.GetCheckpointStorage()!.GetCheckpointPath(), Is.EqualTo(path));
        }

        [Test]
        public void SetCheckpointStorage_WithHdfsPath_CreatesFileSystemStorage()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var path = "hdfs://namenode:9000/checkpoints";

            // Act
            config.SetCheckpointStorage(path);

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(path));
        }

        [Test]
        public void SetCheckpointStorage_WithS3Path_CreatesFileSystemStorage()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var path = "s3://my-bucket/checkpoints";

            // Act
            config.SetCheckpointStorage(path);

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(path));
        }

        [Test]
        public void SetCheckpointStorage_WithNullPath_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage((string)null!));
        }

        [Test]
        public void SetCheckpointStorage_WithEmptyPath_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage(string.Empty));
        }

        [Test]
        public void SetCheckpointStorage_WithWhitespacePath_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetCheckpointStorage("   "));
        }

        [Test]
        public void SetCheckpointStorage_CalledTwice_OverwritesPreviousStorage()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var firstPath = "file:///tmp/checkpoints1";
            var secondPath = "file:///tmp/checkpoints2";

            // Act
            config.SetCheckpointStorage(firstPath);
            config.SetCheckpointStorage(secondPath);

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo(secondPath));
        }

        #endregion

        #region SetCheckpointStorage(FlinkDotNet.DataStream.Checkpoint.ICheckpointStorage) Tests

        [Test]
        public void SetCheckpointStorage_WithValidStorage_SetsStorageAndPath()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var storage = new FlinkDotNet.DataStream.Checkpoint.FileSystemCheckpointStorage("file:///tmp/checkpoints");

            // Act
            var result = config.SetCheckpointStorage(storage);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetCheckpointStorage(), Is.SameAs(storage));
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("file:///tmp/checkpoints"));
        }

        [Test]
        public void SetCheckpointStorage_WithNullStorage_ThrowsArgumentNullException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => config.SetCheckpointStorage((FlinkDotNet.DataStream.Checkpoint.ICheckpointStorage)null!));
        }

        #endregion

        #region SetCheckpointTimeout Tests

        [Test]
        public void SetCheckpointTimeout_WithValidTimeout_SetsTimeout()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var timeout = 300000L; // 5 minutes

            // Act
            var result = config.SetCheckpointTimeout(timeout);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(timeout));
        }

        [Test]
        public void SetCheckpointTimeout_WithMinimumValue_SetsTimeout()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.SetCheckpointTimeout(1);

            // Assert
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(1));
        }

        [Test]
        public void SetCheckpointTimeout_WithLargeValue_SetsTimeout()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var timeout = 3600000L; // 1 hour

            // Act
            config.SetCheckpointTimeout(timeout);

            // Assert
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(timeout));
        }

        [Test]
        public void SetCheckpointTimeout_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetCheckpointTimeout(0));
        }

        [Test]
        public void SetCheckpointTimeout_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetCheckpointTimeout(-1000));
        }

        #endregion

        #region SetMinPauseBetweenCheckpoints Tests

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithValidPause_SetsPause()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var pause = 5000L;

            // Act
            var result = config.SetMinPauseBetweenCheckpoints(pause);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(pause));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithZero_SetsPause()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.SetMinPauseBetweenCheckpoints(0);

            // Assert
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithLargeValue_SetsPause()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var pause = 60000L; // 1 minute

            // Act
            config.SetMinPauseBetweenCheckpoints(pause);

            // Assert
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(pause));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetMinPauseBetweenCheckpoints(-1000));
        }

        #endregion

        #region SetMaxConcurrentCheckpoints Tests

        [Test]
        public void SetMaxConcurrentCheckpoints_WithValidValue_SetsMaxConcurrent()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var maxConcurrent = 3;

            // Act
            var result = config.SetMaxConcurrentCheckpoints(maxConcurrent);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(maxConcurrent));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithOne_SetsMaxConcurrent()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.SetMaxConcurrentCheckpoints(1);

            // Assert
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithLargeValue_SetsMaxConcurrent()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var maxConcurrent = 100;

            // Act
            config.SetMaxConcurrentCheckpoints(maxConcurrent);

            // Assert
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(maxConcurrent));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetMaxConcurrentCheckpoints(0));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetMaxConcurrentCheckpoints(-5));
        }

        #endregion

        #region SetTolerableCheckpointFailureNumber Tests

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithValidValue_SetsTolerableFailures()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            var tolerableFailures = 5;

            // Act
            var result = config.SetTolerableCheckpointFailureNumber(tolerableFailures);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(tolerableFailures));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithZero_SetsTolerableFailures()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.SetTolerableCheckpointFailureNumber(0);

            // Assert
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(0));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithMaxValue_SetsTolerableFailures()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.SetTolerableCheckpointFailureNumber(int.MaxValue);

            // Assert
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act & Assert
            Assert.Throws<ArgumentException>(() => config.SetTolerableCheckpointFailureNumber(-1));
        }

        #endregion

        #region Externalized Checkpoints Tests

        [Test]
        public void EnableExternalizedCheckpoints_WithDeleteOnCancellation_EnablesExternalizedCheckpoints()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            var result = config.EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        [Test]
        public void EnableExternalizedCheckpoints_WithRetainOnCancellation_EnablesExternalizedCheckpoints()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            config.EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        [Test]
        public void DisableExternalizedCheckpoints_DisablesExternalizedCheckpoints()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            config.EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Act
            var result = config.DisableExternalizedCheckpoints();

            // Assert
            Assert.That(result, Is.SameAs(config)); // Method chaining
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
        }

        [Test]
        public void EnableExternalizedCheckpoints_CalledTwice_UpdatesCleanupBehavior()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();
            config.EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Act
            config.EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_AllSetters_ReturnsCorrectInstance()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            var result = config
                .SetCheckpointStorage("file:///tmp/checkpoints")
                .SetCheckpointTimeout(300000)
                .SetMinPauseBetweenCheckpoints(5000)
                .SetMaxConcurrentCheckpoints(2)
                .SetTolerableCheckpointFailureNumber(3)
                .EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(config));
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("file:///tmp/checkpoints"));
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(300000));
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(5000));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(2));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(3));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        [Test]
        public void MethodChaining_ComplexConfiguration_AppliesAllSettings()
        {
            // Arrange & Act
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig()
                .SetCheckpointStorage("hdfs://namenode:9000/checkpoints")
                .SetCheckpointTimeout(600000)
                .SetMinPauseBetweenCheckpoints(10000)
                .SetMaxConcurrentCheckpoints(3)
                .SetTolerableCheckpointFailureNumber(5)
                .EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Assert
            Assert.That(config.GetCheckpointStoragePath(), Is.EqualTo("hdfs://namenode:9000/checkpoints"));
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(600000));
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(10000));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(3));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(5));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
        }

        #endregion

        #region Edge Case Tests

        [Test]
        public void GetCheckpointStorage_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            var storage = config.GetCheckpointStorage();

            // Assert
            Assert.That(storage, Is.Null);
        }

        [Test]
        public void GetCheckpointStoragePath_WhenNotSet_ReturnsNull()
        {
            // Arrange
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig();

            // Act
            var path = config.GetCheckpointStoragePath();

            // Assert
            Assert.That(path, Is.Null);
        }

        [Test]
        public void Configuration_WithMinimalSettings_IsValid()
        {
            // Arrange & Act
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig()
                .SetCheckpointStorage("file:///tmp/checkpoints");

            // Assert
            Assert.That(config.GetCheckpointStorage(), Is.Not.Null);
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(600000)); // Default
        }

        [Test]
        public void Configuration_WithMaximalSettings_IsValid()
        {
            // Arrange & Act
            var config = new FlinkDotNet.DataStream.Checkpoint.CheckpointConfig()
                .SetCheckpointStorage("s3://bucket/checkpoints")
                .SetCheckpointTimeout(1800000)
                .SetMinPauseBetweenCheckpoints(20000)
                .SetMaxConcurrentCheckpoints(5)
                .SetTolerableCheckpointFailureNumber(10)
                .EnableExternalizedCheckpoints(FlinkDotNet.DataStream.Checkpoint.ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(config.GetCheckpointStorage(), Is.Not.Null);
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(1800000));
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(20000));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(5));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(10));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.True);
        }

        #endregion
    }
}