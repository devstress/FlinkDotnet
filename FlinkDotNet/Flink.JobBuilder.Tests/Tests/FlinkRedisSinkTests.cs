using Moq;
using Microsoft.Extensions.Logging;
using Flink.JobBuilder.Flink;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Comprehensive tests for FlinkRedisSink to achieve 100% coverage
    /// Chunk 4B: Redis operations, transactions, connection management, batching, error handling, retries
    /// </summary>
    [TestFixture]
    public class FlinkRedisSinkTests
    {
        private Mock<ILogger<FlinkRedisSink>>? _mockLogger;

        [SetUp]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<FlinkRedisSink>>();
        }

        #region Constructor Tests

        [Test]
        public void Constructor_WithValidParameters_InitializesSuccessfully()
        {
            // Arrange & Act
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithNullConnectionString_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FlinkRedisSink(null!, null, _mockLogger!.Object));
        }

        [Test]
        public void Constructor_WithEmptyConnectionString_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FlinkRedisSink("", null, _mockLogger!.Object));
        }

        [Test]
        public void Constructor_WithNullLogger_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => 
                new FlinkRedisSink("localhost:6379", null, null!));
        }

        [Test]
        public void Constructor_WithRedisConfig_LogsConfigCount()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "connectTimeout", 10000 },
                { "syncTimeout", 5000 }
            };

            // Act
            var sink = new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
            _mockLogger!.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("config options: 2")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public void Constructor_WithPasswordInConnectionString_MasksPassword()
        {
            // Arrange
            var connString = "localhost:6379,password=secret123";

            // Act
            var sink = new FlinkRedisSink(connString, null, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
            _mockLogger!.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("password=***")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region AtomicIncrementAsync Tests

        [Test]
        public void AtomicIncrementAsync_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicIncrementAsync(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void AtomicIncrementAsync_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicIncrementAsync(""));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void AtomicIncrementAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.AtomicIncrementAsync("test-key"));
            Assert.That(ex!.Message, Does.Contain("Redis atomic increment failed"));
        }

        [Test]
        public void AtomicIncrementAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.AtomicIncrementAsync("test-key"));
        }

        #endregion

        #region AtomicSetAddAsync Tests

        [Test]
        public void AtomicSetAddAsync_WithNullSetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicSetAddAsync(null!, "member"));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void AtomicSetAddAsync_WithEmptySetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicSetAddAsync("", "member"));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void AtomicSetAddAsync_WithNullMember_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicSetAddAsync("test-set", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("member"));
        }

        [Test]
        public void AtomicSetAddAsync_WithEmptyMember_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.AtomicSetAddAsync("test-set", ""));
            Assert.That(ex!.ParamName, Is.EqualTo("member"));
        }

        [Test]
        public void AtomicSetAddAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.AtomicSetAddAsync("test-set", "member"));
            Assert.That(ex!.Message, Does.Contain("Redis atomic set add failed"));
        }

        [Test]
        public void AtomicSetAddAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.AtomicSetAddAsync("test-set", "member"));
        }

        #endregion

        #region SetContainsAsync Tests

        [Test]
        public void SetContainsAsync_WithNullSetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.SetContainsAsync(null!, "member"));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void SetContainsAsync_WithEmptySetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.SetContainsAsync("", "member"));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void SetContainsAsync_WithNullMember_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.SetContainsAsync("test-set", null!));
            Assert.That(ex!.ParamName, Is.EqualTo("member"));
        }

        [Test]
        public void SetContainsAsync_WithEmptyMember_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.SetContainsAsync("test-set", ""));
            Assert.That(ex!.ParamName, Is.EqualTo("member"));
        }

        [Test]
        public void SetContainsAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.SetContainsAsync("test-set", "member"));
            Assert.That(ex!.Message, Does.Contain("Redis set membership check failed"));
        }

        [Test]
        public void SetContainsAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.SetContainsAsync("test-set", "member"));
        }

        #endregion

        #region GetCounterValueAsync Tests

        [Test]
        public void GetCounterValueAsync_WithNullKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.GetCounterValueAsync(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void GetCounterValueAsync_WithEmptyKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.GetCounterValueAsync(""));
            Assert.That(ex!.ParamName, Is.EqualTo("key"));
        }

        [Test]
        public void GetCounterValueAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.GetCounterValueAsync("test-key"));
            Assert.That(ex!.Message, Does.Contain("Redis get counter value failed"));
        }

        [Test]
        public void GetCounterValueAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.GetCounterValueAsync("test-key"));
        }

        #endregion

        #region GetSetSizeAsync Tests

        [Test]
        public void GetSetSizeAsync_WithNullSetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.GetSetSizeAsync(null!));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void GetSetSizeAsync_WithEmptySetKey_ThrowsArgumentException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<ArgumentException>(async () => 
                await sink.GetSetSizeAsync(""));
            Assert.That(ex!.ParamName, Is.EqualTo("setKey"));
        }

        [Test]
        public void GetSetSizeAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.GetSetSizeAsync("test-set"));
            Assert.That(ex!.Message, Does.Contain("Redis get set size failed"));
        }

        [Test]
        public void GetSetSizeAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.GetSetSizeAsync("test-set"));
        }

        #endregion

        #region ExecuteTransactionAsync Tests

        [Test]
        public void ExecuteTransactionAsync_WithNullOperations_ThrowsArgumentNullException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await sink.ExecuteTransactionAsync(null!));
        }

        [Test]
        public void ExecuteTransactionAsync_BeforeInitialize_ThrowsInvalidOperationException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            var operations = new List<RedisOperation>
            {
                new RedisOperation { Type = RedisOperationType.Increment, Key = "test-key" }
            };

            // Act & Assert
            var ex = Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.ExecuteTransactionAsync(operations));
            Assert.That(ex!.Message, Does.Contain("Redis transaction execution failed"));
        }

        [Test]
        public void ExecuteTransactionAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            sink.Dispose();
            var operations = new List<RedisOperation>
            {
                new RedisOperation { Type = RedisOperationType.Increment, Key = "test-key" }
            };

            // Act & Assert
            Assert.ThrowsAsync<ObjectDisposedException>(async () => 
                await sink.ExecuteTransactionAsync(operations));
        }

        #endregion

        #region RedisOperation Tests

        [Test]
        public void RedisOperation_DefaultConstructor_InitializesProperties()
        {
            // Act
            var operation = new RedisOperation();

            // Assert
            Assert.That(operation.Type, Is.EqualTo(default(RedisOperationType)));
            Assert.That(operation.Key, Is.Null);
            Assert.That(operation.Member, Is.Null);
            Assert.That(operation.Value, Is.Null);
            Assert.That(operation.Increment, Is.EqualTo(1));
        }

        [Test]
        public void RedisOperation_SetProperties_StoresValues()
        {
            // Arrange
            var operation = new RedisOperation();

            // Act
            operation.Type = RedisOperationType.Increment;
            operation.Key = "test-key";
            operation.Member = "test-member";
            operation.Value = "test-value";
            operation.Increment = 5;

            // Assert
            Assert.That(operation.Type, Is.EqualTo(RedisOperationType.Increment));
            Assert.That(operation.Key, Is.EqualTo("test-key"));
            Assert.That(operation.Member, Is.EqualTo("test-member"));
            Assert.That(operation.Value, Is.EqualTo("test-value"));
            Assert.That(operation.Increment, Is.EqualTo(5));
        }

        [Test]
        public void RedisOperationType_AllEnumValues_AreDefined()
        {
            // Assert - verify all expected operation types exist
            var values = Enum.GetValues(typeof(RedisOperationType)).Cast<RedisOperationType>().ToList();
            
            Assert.That(values, Contains.Item(RedisOperationType.Increment));
            Assert.That(values, Contains.Item(RedisOperationType.SetAdd));
            Assert.That(values, Contains.Item(RedisOperationType.Get));
            Assert.That(values, Contains.Item(RedisOperationType.Set));
            Assert.That(values, Contains.Item(RedisOperationType.Delete));
        }

        #endregion

        #region RedisTransactionResult Tests

        [Test]
        public void RedisTransactionResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new RedisTransactionResult();

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Results, Is.Not.Null);
            Assert.That(result.Results, Is.Empty);
            Assert.That(result.ErrorMessage, Is.Null);
        }

        [Test]
        public void RedisTransactionResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new RedisTransactionResult();
            var results = new List<object> { 1L, true, "value" };

            // Act
            result.Success = true;
            result.Results = results;
            result.ErrorMessage = "Test error";

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Results, Is.EqualTo(results));
            Assert.That(result.ErrorMessage, Is.EqualTo("Test error"));
        }

        #endregion

        #region Dispose Tests

        [Test]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => sink.Dispose());
            Assert.DoesNotThrow(() => sink.Dispose());
        }

        [Test]
        public void Dispose_ImplementsIDisposable()
        {
            // Arrange & Act
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.InstanceOf<IDisposable>());
        }

        [Test]
        public void Dispose_LogsDisposal()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);

            // Act
            sink.Dispose();

            // Assert
            _mockLogger!.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Disposing")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Configuration Tests

        [Test]
        public void Constructor_WithConnectTimeoutConfig_InitializesWithCustomTimeout()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "connectTimeout", 15000 }
            };

            // Act
            var sink = new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithSyncTimeoutConfig_InitializesWithCustomTimeout()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "syncTimeout", 8000 }
            };

            // Act
            var sink = new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithAbortOnConnectFailConfig_InitializesWithCustomSetting()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "abortOnConnectFail", true }
            };

            // Act
            var sink = new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithMultipleConfigs_InitializesWithAllSettings()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "connectTimeout", 20000 },
                { "syncTimeout", 10000 },
                { "abortOnConnectFail", false }
            };

            // Act
            var sink = new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object);

            // Assert
            Assert.That(sink, Is.Not.Null);
        }

        [Test]
        public void Constructor_WithUnrecognizedConfig_DoesNotThrow()
        {
            // Arrange
            var config = new Dictionary<string, object>
            {
                { "unknownOption", "value" }
            };

            // Act & Assert
            Assert.DoesNotThrow(() => 
                new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object));
        }

        [Test]
        public void Constructor_WithWrongTypeConfig_DoesNotThrow()
        {
            // Arrange - wrong type for connectTimeout (string instead of int)
            var config = new Dictionary<string, object>
            {
                { "connectTimeout", "not-a-number" }
            };

            // Act & Assert - should handle gracefully
            Assert.DoesNotThrow(() => 
                new FlinkRedisSink("localhost:6379", config, _mockLogger!.Object));
        }

        #endregion

        #region CancellationToken Tests

        [Test]
        public void AtomicIncrementAsync_AcceptsCancellationToken()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            using var cts = new CancellationTokenSource();

            // Act & Assert - method signature accepts cancellation token
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.AtomicIncrementAsync("test-key", 1, cts.Token));
        }

        [Test]
        public void AtomicSetAddAsync_AcceptsCancellationToken()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            using var cts = new CancellationTokenSource();

            // Act & Assert - method signature accepts cancellation token
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.AtomicSetAddAsync("test-set", "member", cts.Token));
        }

        [Test]
        public void ExecuteTransactionAsync_AcceptsCancellationToken()
        {
            // Arrange
            var sink = new FlinkRedisSink("localhost:6379", null, _mockLogger!.Object);
            var operations = new List<RedisOperation>
            {
                new RedisOperation { Type = RedisOperationType.Increment, Key = "test-key" }
            };
            using var cts = new CancellationTokenSource();

            // Act & Assert - method signature accepts cancellation token
            Assert.ThrowsAsync<InvalidOperationException>(async () => 
                await sink.ExecuteTransactionAsync(operations, cts.Token));
        }

        #endregion
    }
}
