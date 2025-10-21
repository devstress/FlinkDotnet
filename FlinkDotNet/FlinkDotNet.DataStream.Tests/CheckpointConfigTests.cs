using System;
using NUnit.Framework;
using FlinkDotNet.DataStream.Checkpoint;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class CheckpointConfigTests
    {
        private CheckpointConfig _checkpointConfig = null!;

        [SetUp]
        public void SetUp()
        {
            _checkpointConfig = new CheckpointConfig();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_CreatesInstanceWithDefaultValues()
        {
            // Arrange & Act
            var config = new CheckpointConfig();

            // Assert
            Assert.That(config, Is.Not.Null);
            Assert.That(config.GetCheckpointTimeout(), Is.EqualTo(600000)); // 10 minutes default
            Assert.That(config.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
            Assert.That(config.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
            Assert.That(config.GetTolerableCheckpointFailureNumber(), Is.EqualTo(int.MaxValue));
            Assert.That(config.IsExternalizedCheckpointsEnabled(), Is.False);
            Assert.That(config.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        #endregion

        #region SetCheckpointStorage(string) Tests

        [Test]
        public void SetCheckpointStorage_WithValidPath_SetsStoragePath()
        {
            // Arrange
            var path = "hdfs://namenode:8020/flink/checkpoints";

            // Act
            var result = _checkpointConfig.SetCheckpointStorage(path);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig)); // Method chaining
            Assert.That(_checkpointConfig.GetCheckpointStoragePath(), Is.EqualTo(path));
            Assert.That(_checkpointConfig.GetCheckpointStorage(), Is.Not.Null);
        }

        [Test]
        public void SetCheckpointStorage_WithNullPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetCheckpointStorage((string)null!));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
            Assert.That(ex.Message, Does.Contain("cannot be null or empty"));
        }

        [Test]
        public void SetCheckpointStorage_WithEmptyPath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetCheckpointStorage(string.Empty));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
        }

        [Test]
        public void SetCheckpointStorage_WithWhitespacePath_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetCheckpointStorage("   "));
            Assert.That(ex!.ParamName, Is.EqualTo("path"));
        }

        #endregion

        #region SetCheckpointStorage(ICheckpointStorage) Tests

        [Test]
        public void SetCheckpointStorage_WithValidStorage_SetsStorage()
        {
            // Arrange
            var storage = new FileSystemCheckpointStorage("/tmp/checkpoints");

            // Act
            var result = _checkpointConfig.SetCheckpointStorage(storage);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetCheckpointStorage(), Is.SameAs(storage));
            Assert.That(_checkpointConfig.GetCheckpointStoragePath(), Is.EqualTo("/tmp/checkpoints"));
        }

        [Test]
        public void SetCheckpointStorage_WithNullStorage_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() => _checkpointConfig.SetCheckpointStorage((ICheckpointStorage)null!));
            Assert.That(ex!.ParamName, Is.EqualTo("storage"));
        }

        #endregion

        #region GetCheckpointStorage Tests

        [Test]
        public void GetCheckpointStorage_WhenNotSet_ReturnsNull()
        {
            // Act
            var storage = _checkpointConfig.GetCheckpointStorage();

            // Assert
            Assert.That(storage, Is.Null);
        }

        [Test]
        public void GetCheckpointStorage_WhenSet_ReturnsStorage()
        {
            // Arrange
            var expectedStorage = new FileSystemCheckpointStorage("/checkpoints");
            _checkpointConfig.SetCheckpointStorage(expectedStorage);

            // Act
            var storage = _checkpointConfig.GetCheckpointStorage();

            // Assert
            Assert.That(storage, Is.SameAs(expectedStorage));
        }

        #endregion

        #region GetCheckpointStoragePath Tests

        [Test]
        public void GetCheckpointStoragePath_WhenNotSet_ReturnsNull()
        {
            // Act
            var path = _checkpointConfig.GetCheckpointStoragePath();

            // Assert
            Assert.That(path, Is.Null);
        }

        [Test]
        public void GetCheckpointStoragePath_WhenSet_ReturnsPath()
        {
            // Arrange
            var expectedPath = "s3://bucket/checkpoints";
            _checkpointConfig.SetCheckpointStorage(expectedPath);

            // Act
            var path = _checkpointConfig.GetCheckpointStoragePath();

            // Assert
            Assert.That(path, Is.EqualTo(expectedPath));
        }

        #endregion

        #region SetCheckpointTimeout Tests

        [Test]
        public void SetCheckpointTimeout_WithValidTimeout_SetsTimeout()
        {
            // Arrange
            long timeout = 300000; // 5 minutes

            // Act
            var result = _checkpointConfig.SetCheckpointTimeout(timeout);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetCheckpointTimeout(), Is.EqualTo(timeout));
        }

        [Test]
        public void SetCheckpointTimeout_WithZeroTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetCheckpointTimeout(0));
            Assert.That(ex!.ParamName, Is.EqualTo("timeoutMs"));
            Assert.That(ex.Message, Does.Contain("must be positive"));
        }

        [Test]
        public void SetCheckpointTimeout_WithNegativeTimeout_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetCheckpointTimeout(-1000));
            Assert.That(ex!.ParamName, Is.EqualTo("timeoutMs"));
        }

        [Test]
        public void SetCheckpointTimeout_SupportsMethodChaining()
        {
            // Act
            var result = _checkpointConfig
                .SetCheckpointTimeout(60000)
                .SetMinPauseBetweenCheckpoints(5000);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetCheckpointTimeout(), Is.EqualTo(60000));
            Assert.That(_checkpointConfig.GetMinPauseBetweenCheckpoints(), Is.EqualTo(5000));
        }

        #endregion

        #region GetCheckpointTimeout Tests

        [Test]
        public void GetCheckpointTimeout_ReturnsDefaultValue()
        {
            // Act
            var timeout = _checkpointConfig.GetCheckpointTimeout();

            // Assert
            Assert.That(timeout, Is.EqualTo(600000)); // 10 minutes
        }

        [Test]
        public void GetCheckpointTimeout_ReturnsSetValue()
        {
            // Arrange
            _checkpointConfig.SetCheckpointTimeout(120000);

            // Act
            var timeout = _checkpointConfig.GetCheckpointTimeout();

            // Assert
            Assert.That(timeout, Is.EqualTo(120000));
        }

        #endregion

        #region SetMinPauseBetweenCheckpoints Tests

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithValidPause_SetsPause()
        {
            // Arrange
            long pause = 10000;

            // Act
            var result = _checkpointConfig.SetMinPauseBetweenCheckpoints(pause);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetMinPauseBetweenCheckpoints(), Is.EqualTo(pause));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithZero_SetsZero()
        {
            // Act
            var result = _checkpointConfig.SetMinPauseBetweenCheckpoints(0);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetMinPauseBetweenCheckpoints(), Is.EqualTo(0));
        }

        [Test]
        public void SetMinPauseBetweenCheckpoints_WithNegativeValue_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetMinPauseBetweenCheckpoints(-1));
            Assert.That(ex!.ParamName, Is.EqualTo("pauseMs"));
            Assert.That(ex.Message, Does.Contain("must be non-negative"));
        }

        #endregion

        #region GetMinPauseBetweenCheckpoints Tests

        [Test]
        public void GetMinPauseBetweenCheckpoints_ReturnsDefaultValue()
        {
            // Act
            var pause = _checkpointConfig.GetMinPauseBetweenCheckpoints();

            // Assert
            Assert.That(pause, Is.EqualTo(0));
        }

        [Test]
        public void GetMinPauseBetweenCheckpoints_ReturnsSetValue()
        {
            // Arrange
            _checkpointConfig.SetMinPauseBetweenCheckpoints(5000);

            // Act
            var pause = _checkpointConfig.GetMinPauseBetweenCheckpoints();

            // Assert
            Assert.That(pause, Is.EqualTo(5000));
        }

        #endregion

        #region SetMaxConcurrentCheckpoints Tests

        [Test]
        public void SetMaxConcurrentCheckpoints_WithValidValue_SetsMaxConcurrent()
        {
            // Arrange
            int maxConcurrent = 3;

            // Act
            var result = _checkpointConfig.SetMaxConcurrentCheckpoints(maxConcurrent);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetMaxConcurrentCheckpoints(), Is.EqualTo(maxConcurrent));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithOne_SetsOne()
        {
            // Act
            var result = _checkpointConfig.SetMaxConcurrentCheckpoints(1);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetMaxConcurrentCheckpoints(), Is.EqualTo(1));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithZero_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetMaxConcurrentCheckpoints(0));
            Assert.That(ex!.ParamName, Is.EqualTo("maxConcurrent"));
            Assert.That(ex.Message, Does.Contain("must be at least 1"));
        }

        [Test]
        public void SetMaxConcurrentCheckpoints_WithNegative_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetMaxConcurrentCheckpoints(-1));
            Assert.That(ex!.ParamName, Is.EqualTo("maxConcurrent"));
        }

        #endregion

        #region GetMaxConcurrentCheckpoints Tests

        [Test]
        public void GetMaxConcurrentCheckpoints_ReturnsDefaultValue()
        {
            // Act
            var maxConcurrent = _checkpointConfig.GetMaxConcurrentCheckpoints();

            // Assert
            Assert.That(maxConcurrent, Is.EqualTo(1));
        }

        [Test]
        public void GetMaxConcurrentCheckpoints_ReturnsSetValue()
        {
            // Arrange
            _checkpointConfig.SetMaxConcurrentCheckpoints(5);

            // Act
            var maxConcurrent = _checkpointConfig.GetMaxConcurrentCheckpoints();

            // Assert
            Assert.That(maxConcurrent, Is.EqualTo(5));
        }

        #endregion

        #region SetTolerableCheckpointFailureNumber Tests

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithValidValue_SetsTolerableFailures()
        {
            // Arrange
            int tolerableFailures = 10;

            // Act
            var result = _checkpointConfig.SetTolerableCheckpointFailureNumber(tolerableFailures);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetTolerableCheckpointFailureNumber(), Is.EqualTo(tolerableFailures));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithZero_SetsZero()
        {
            // Act
            var result = _checkpointConfig.SetTolerableCheckpointFailureNumber(0);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetTolerableCheckpointFailureNumber(), Is.EqualTo(0));
        }

        [Test]
        public void SetTolerableCheckpointFailureNumber_WithNegative_ThrowsArgumentException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentException>(() => _checkpointConfig.SetTolerableCheckpointFailureNumber(-1));
            Assert.That(ex!.ParamName, Is.EqualTo("tolerableFailures"));
            Assert.That(ex.Message, Does.Contain("must be non-negative"));
        }

        #endregion

        #region GetTolerableCheckpointFailureNumber Tests

        [Test]
        public void GetTolerableCheckpointFailureNumber_ReturnsDefaultValue()
        {
            // Act
            var tolerableFailures = _checkpointConfig.GetTolerableCheckpointFailureNumber();

            // Assert
            Assert.That(tolerableFailures, Is.EqualTo(int.MaxValue));
        }

        [Test]
        public void GetTolerableCheckpointFailureNumber_ReturnsSetValue()
        {
            // Arrange
            _checkpointConfig.SetTolerableCheckpointFailureNumber(5);

            // Act
            var tolerableFailures = _checkpointConfig.GetTolerableCheckpointFailureNumber();

            // Assert
            Assert.That(tolerableFailures, Is.EqualTo(5));
        }

        #endregion

        #region EnableExternalizedCheckpoints Tests

        [Test]
        public void EnableExternalizedCheckpoints_WithDeleteOnCancellation_EnablesWithCorrectCleanup()
        {
            // Act
            var result = _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(_checkpointConfig.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        [Test]
        public void EnableExternalizedCheckpoints_WithRetainOnCancellation_EnablesWithCorrectCleanup()
        {
            // Act
            var result = _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(_checkpointConfig.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        #endregion

        #region DisableExternalizedCheckpoints Tests

        [Test]
        public void DisableExternalizedCheckpoints_DisablesExternalizedCheckpoints()
        {
            // Arrange
            _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Act
            var result = _checkpointConfig.DisableExternalizedCheckpoints();

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.IsExternalizedCheckpointsEnabled(), Is.False);
        }

        [Test]
        public void DisableExternalizedCheckpoints_WhenAlreadyDisabled_RemainsDisabled()
        {
            // Act
            var result = _checkpointConfig.DisableExternalizedCheckpoints();

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.IsExternalizedCheckpointsEnabled(), Is.False);
        }

        #endregion

        #region IsExternalizedCheckpointsEnabled Tests

        [Test]
        public void IsExternalizedCheckpointsEnabled_DefaultIsFalse()
        {
            // Act
            var enabled = _checkpointConfig.IsExternalizedCheckpointsEnabled();

            // Assert
            Assert.That(enabled, Is.False);
        }

        [Test]
        public void IsExternalizedCheckpointsEnabled_AfterEnable_ReturnsTrue()
        {
            // Arrange
            _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);

            // Act
            var enabled = _checkpointConfig.IsExternalizedCheckpointsEnabled();

            // Assert
            Assert.That(enabled, Is.True);
        }

        [Test]
        public void IsExternalizedCheckpointsEnabled_AfterDisable_ReturnsFalse()
        {
            // Arrange
            _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION);
            _checkpointConfig.DisableExternalizedCheckpoints();

            // Act
            var enabled = _checkpointConfig.IsExternalizedCheckpointsEnabled();

            // Assert
            Assert.That(enabled, Is.False);
        }

        #endregion

        #region GetExternalizedCheckpointCleanup Tests

        [Test]
        public void GetExternalizedCheckpointCleanup_DefaultIsDeleteOnCancellation()
        {
            // Act
            var cleanup = _checkpointConfig.GetExternalizedCheckpointCleanup();

            // Assert
            Assert.That(cleanup, Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        [Test]
        public void GetExternalizedCheckpointCleanup_ReturnsSetValue()
        {
            // Arrange
            _checkpointConfig.EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Act
            var cleanup = _checkpointConfig.GetExternalizedCheckpointCleanup();

            // Assert
            Assert.That(cleanup, Is.EqualTo(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        #endregion

        #region Method Chaining Tests

        [Test]
        public void MethodChaining_AllMethods_WorkCorrectly()
        {
            // Act
            var result = _checkpointConfig
                .SetCheckpointStorage("/checkpoints")
                .SetCheckpointTimeout(180000)
                .SetMinPauseBetweenCheckpoints(5000)
                .SetMaxConcurrentCheckpoints(2)
                .SetTolerableCheckpointFailureNumber(3)
                .EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION);

            // Assert
            Assert.That(result, Is.SameAs(_checkpointConfig));
            Assert.That(_checkpointConfig.GetCheckpointStoragePath(), Is.EqualTo("/checkpoints"));
            Assert.That(_checkpointConfig.GetCheckpointTimeout(), Is.EqualTo(180000));
            Assert.That(_checkpointConfig.GetMinPauseBetweenCheckpoints(), Is.EqualTo(5000));
            Assert.That(_checkpointConfig.GetMaxConcurrentCheckpoints(), Is.EqualTo(2));
            Assert.That(_checkpointConfig.GetTolerableCheckpointFailureNumber(), Is.EqualTo(3));
            Assert.That(_checkpointConfig.IsExternalizedCheckpointsEnabled(), Is.True);
            Assert.That(_checkpointConfig.GetExternalizedCheckpointCleanup(), Is.EqualTo(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        #endregion

        #region ExternalizedCheckpointCleanup Enum Tests

        [Test]
        public void ExternalizedCheckpointCleanup_HasDeleteOnCancellationValue()
        {
            // Assert
            Assert.That(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION, Is.EqualTo(ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION));
        }

        [Test]
        public void ExternalizedCheckpointCleanup_HasRetainOnCancellationValue()
        {
            // Assert
            Assert.That(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION, Is.EqualTo(ExternalizedCheckpointCleanup.RETAIN_ON_CANCELLATION));
        }

        #endregion
    }
}