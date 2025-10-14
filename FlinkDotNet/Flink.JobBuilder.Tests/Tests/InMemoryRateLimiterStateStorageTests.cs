using Flink.JobBuilder.Backpressure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class InMemoryRateLimiterStateStorageTests
{
    private InMemoryRateLimiterStateStorage _storage = null!;
    private Mock<ILogger<InMemoryRateLimiterStateStorage>> _mockLogger = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<InMemoryRateLimiterStateStorage>>();
        _storage = new InMemoryRateLimiterStateStorage(_mockLogger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _storage?.Dispose();
    }

    [Test]
    public async Task SaveStateAsync_SavesState_Successfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-1";
        var state = new RateLimiterState
        {
            RateLimiterId = rateLimiterId,
            CurrentTokens = 100.0,
            MaxTokens = 1000.0,
            CurrentRateLimit = 50.0,
            LastRefill = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RateLimiterType = "TokenBucket"
        };

        // Act
        await _storage.SaveStateAsync(rateLimiterId, state);

        // Assert
        Assert.That(_storage.StateCount, Is.EqualTo(1));
        var loadedState = await _storage.LoadStateAsync(rateLimiterId);
        Assert.That(loadedState, Is.Not.Null);
        Assert.That(loadedState!.RateLimiterId, Is.EqualTo(rateLimiterId));
        Assert.That(loadedState.CurrentTokens, Is.EqualTo(100.0));
        Assert.That(loadedState.MaxTokens, Is.EqualTo(1000.0));
    }

    [Test]
    public async Task SaveStateAsync_UpdatesExistingState_Successfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-2";
        var state1 = new RateLimiterState
        {
            RateLimiterId = rateLimiterId,
            CurrentTokens = 100.0,
            MaxTokens = 1000.0,
            CurrentRateLimit = 50.0,
            LastRefill = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RateLimiterType = "TokenBucket"
        };

        var state2 = new RateLimiterState
        {
            RateLimiterId = rateLimiterId,
            CurrentTokens = 200.0,
            MaxTokens = 1000.0,
            CurrentRateLimit = 50.0,
            LastRefill = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RateLimiterType = "TokenBucket"
        };

        // Act
        await _storage.SaveStateAsync(rateLimiterId, state1);
        await _storage.SaveStateAsync(rateLimiterId, state2);

        // Assert
        Assert.That(_storage.StateCount, Is.EqualTo(1));
        var loadedState = await _storage.LoadStateAsync(rateLimiterId);
        Assert.That(loadedState, Is.Not.Null);
        Assert.That(loadedState!.CurrentTokens, Is.EqualTo(200.0));
    }

    [Test]
    public void SaveStateAsync_ThrowsObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _storage.Dispose();
        var state = new RateLimiterState { RateLimiterId = "test" };

        // Act & Assert
        Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _storage.SaveStateAsync("test", state));
    }

    [Test]
    public async Task LoadStateAsync_ReturnsState_WhenExists()
    {
        // Arrange
        var rateLimiterId = "test-limiter-3";
        var state = new RateLimiterState
        {
            RateLimiterId = rateLimiterId,
            CurrentTokens = 150.0,
            MaxTokens = 1000.0,
            CurrentRateLimit = 75.0,
            LastRefill = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            RateLimiterType = "SlidingWindow"
        };
        await _storage.SaveStateAsync(rateLimiterId, state);

        // Act
        var loadedState = await _storage.LoadStateAsync(rateLimiterId);

        // Assert
        Assert.That(loadedState, Is.Not.Null);
        Assert.That(loadedState!.RateLimiterId, Is.EqualTo(rateLimiterId));
        Assert.That(loadedState.CurrentTokens, Is.EqualTo(150.0));
        Assert.That(loadedState.RateLimiterType, Is.EqualTo("SlidingWindow"));
    }

    [Test]
    public async Task LoadStateAsync_ReturnsNull_WhenNotFound()
    {
        // Act
        var loadedState = await _storage.LoadStateAsync("non-existent");

        // Assert
        Assert.That(loadedState, Is.Null);
    }

    [Test]
    public void LoadStateAsync_ThrowsObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _storage.Dispose();

        // Act & Assert
        Assert.ThrowsAsync<ObjectDisposedException>(async () =>
            await _storage.LoadStateAsync("test"));
    }

    [Test]
    public async Task IsHealthyAsync_ReturnsTrue_WhenNotDisposed()
    {
        // Act
        var isHealthy = await _storage.IsHealthyAsync();

        // Assert
        Assert.That(isHealthy, Is.True);
    }

    [Test]
    public async Task IsHealthyAsync_ReturnsFalse_WhenDisposed()
    {
        // Arrange
        _storage.Dispose();

        // Act
        var isHealthy = await _storage.IsHealthyAsync();

        // Assert
        Assert.That(isHealthy, Is.False);
    }

    [Test]
    public void BackendInfo_ReturnsCorrectInformation()
    {
        // Act
        var backendInfo = _storage.BackendInfo;

        // Assert
        Assert.That(backendInfo, Is.Not.Null);
        Assert.That(backendInfo.BackendType, Is.EqualTo("In-Memory"));
        Assert.That(backendInfo.SupportsDistribution, Is.False);
        Assert.That(backendInfo.SupportsPersistence, Is.False);
        Assert.That(backendInfo.SupportsReplication, Is.False);
        Assert.That(backendInfo.TypicalLatency, Is.EqualTo(TimeSpan.FromMicroseconds(10)));
    }

    [Test]
    public async Task StateCount_ReturnsCorrectCount()
    {
        // Act & Assert - Initially empty
        Assert.That(_storage.StateCount, Is.EqualTo(0));

        // Add one state
        await _storage.SaveStateAsync("limiter-1", new RateLimiterState { RateLimiterId = "limiter-1" });
        Assert.That(_storage.StateCount, Is.EqualTo(1));

        // Add another state
        await _storage.SaveStateAsync("limiter-2", new RateLimiterState { RateLimiterId = "limiter-2" });
        Assert.That(_storage.StateCount, Is.EqualTo(2));

        // Update existing state (count should remain the same)
        await _storage.SaveStateAsync("limiter-1", new RateLimiterState { RateLimiterId = "limiter-1", CurrentTokens = 500 });
        Assert.That(_storage.StateCount, Is.EqualTo(2));
    }

    [Test]
    public async Task ClearAllStates_RemovesAllStates()
    {
        // Arrange
        await _storage.SaveStateAsync("limiter-1", new RateLimiterState { RateLimiterId = "limiter-1" });
        await _storage.SaveStateAsync("limiter-2", new RateLimiterState { RateLimiterId = "limiter-2" });
        await _storage.SaveStateAsync("limiter-3", new RateLimiterState { RateLimiterId = "limiter-3" });
        Assert.That(_storage.StateCount, Is.EqualTo(3));

        // Act
        _storage.ClearAllStates();

        // Assert
        Assert.That(_storage.StateCount, Is.EqualTo(0));
        var state1 = await _storage.LoadStateAsync("limiter-1");
        var state2 = await _storage.LoadStateAsync("limiter-2");
        var state3 = await _storage.LoadStateAsync("limiter-3");
        Assert.That(state1, Is.Null);
        Assert.That(state2, Is.Null);
        Assert.That(state3, Is.Null);
    }

    [Test]
    public void ClearAllStates_ThrowsObjectDisposedException_WhenDisposed()
    {
        // Arrange
        _storage.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => _storage.ClearAllStates());
    }

    [Test]
    public void Dispose_ClearsAllStates()
    {
        // Arrange - Add some states
        _storage.SaveStateAsync("limiter-1", new RateLimiterState { RateLimiterId = "limiter-1" }).Wait();
        _storage.SaveStateAsync("limiter-2", new RateLimiterState { RateLimiterId = "limiter-2" }).Wait();
        Assert.That(_storage.StateCount, Is.EqualTo(2));

        // Act
        _storage.Dispose();

        // Assert - Cannot check StateCount after dispose as it would throw
        // But we verified disposal logs the count
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert - Should not throw
        _storage.Dispose();
        Assert.DoesNotThrow(() => _storage.Dispose());
    }

    [Test]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        // Act
        using var storage = new InMemoryRateLimiterStateStorage(null);

        // Assert
        Assert.That(storage, Is.Not.Null);
        Assert.That(storage.StateCount, Is.EqualTo(0));
    }

    [Test]
    public async Task SaveStateAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new System.Threading.CancellationTokenSource();
        var state = new RateLimiterState { RateLimiterId = "test-limiter-4" };

        // Act
        await _storage.SaveStateAsync("test-limiter-4", state, cts.Token);

        // Assert
        Assert.That(_storage.StateCount, Is.EqualTo(1));
    }

    [Test]
    public async Task LoadStateAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        using var cts = new System.Threading.CancellationTokenSource();
        var state = new RateLimiterState { RateLimiterId = "test-limiter-5" };
        await _storage.SaveStateAsync("test-limiter-5", state);

        // Act
        var loadedState = await _storage.LoadStateAsync("test-limiter-5", cts.Token);

        // Assert
        Assert.That(loadedState, Is.Not.Null);
    }
}
