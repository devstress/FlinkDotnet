using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests for rate limiter classes to improve coverage
/// Target: MultiTierRateLimiter (49.4%), SlidingWindowRateLimiter (67.8%), TokenBucketRateLimiter (81%)
/// </summary>
[TestFixture]
public class RateLimiterCoverageTests
{
    #region MultiTierRateLimiter Tests

    [Test]
    public void MultiTierRateLimiter_Constructor_InitializesWithInMemoryStorage()
    {
        // Act
        using var rateLimiter = new MultiTierRateLimiter();

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_Constructor_AcceptsCustomStorage()
    {
        // Arrange
        var storage = new InMemoryRateLimiterStateStorage();

        // Act
        using var rateLimiter = new MultiTierRateLimiter(storage);

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_ConfigureTiers_AcceptsEmptyList()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>();

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.ConfigureTiers(tiers));
    }

    [Test]
    public void MultiTierRateLimiter_ConfigureTiers_AcceptsSingleTier()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new RateLimitingTier { Name = "Global", RateLimit = 1000, BurstCapacity = 100 }
        };

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.ConfigureTiers(tiers));
    }

    [Test]
    public void MultiTierRateLimiter_ConfigureTiers_AcceptsMultipleTiers()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new RateLimitingTier { Name = "Global", RateLimit = 1000, BurstCapacity = 100 },
            new RateLimitingTier { Name = "Topic", RateLimit = 500, BurstCapacity = 50 },
            new RateLimitingTier { Name = "Consumer", RateLimit = 100, BurstCapacity = 10 }
        };

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.ConfigureTiers(tiers));
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquire_WithDefaultTiers_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext
        {
            ClientId = "test-client",
            TopicName = "test-topic",
            ConsumerGroup = "test-group"
        };

        // Act
        var result = rateLimiter.TryAcquire(context, 1);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquire_MultipleRequests_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext
        {
            ClientId = "test-client",
            TopicName = "test-topic"
        };

        // Act - Make multiple requests
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(rateLimiter.TryAcquire(context, 1));
        }

        // Assert - Should all succeed with default configuration
        Assert.That(results, Has.All.True);
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquireAsync_WithDefaultTiers_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext
        {
            ClientId = "test-client",
            TopicName = "test-topic"
        };

        // Act
        var result = rateLimiter.TryAcquireAsync(context, 1).GetAwaiter().GetResult();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquireAsync_WithMultiplePermits_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext
        {
            ClientId = "test-client",
            TopicName = "test-topic"
        };

        // Act
        var result = rateLimiter.TryAcquireAsync(context, 5).GetAwaiter().GetResult();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiTierRateLimiter_Dispose_DoesNotThrow()
    {
        // Arrange
        var rateLimiter = new MultiTierRateLimiter();

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.Dispose());
    }

    [Test]
    public void MultiTierRateLimiter_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var rateLimiter = new MultiTierRateLimiter();

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            rateLimiter.Dispose();
            rateLimiter.Dispose();
        });
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquire_AfterDispose_ThrowsObjectDisposedException()
    {
        // Arrange
        var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext { ClientId = "test" };
        rateLimiter.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => rateLimiter.TryAcquire(context, 1));
    }

    #endregion

    #region SlidingWindowRateLimiter Tests

    [Test]
    public void SlidingWindowRateLimiter_Constructor_InitializesCorrectly()
    {
        // Act
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_WithZeroRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SlidingWindowRateLimiter(0, 1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_WithNegativeRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SlidingWindowRateLimiter(-1, 1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_WithZeroWindow_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => 
            new SlidingWindowRateLimiter(100, 0));
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_InitialRequest_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act
        var result = rateLimiter.TryAcquire(1);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_WithinLimit_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(10, 1.0);

        // Act - Acquire less than limit
        var results = new List<bool>();
        for (int i = 0; i < 5; i++)
        {
            results.Add(rateLimiter.TryAcquire(1));
        }

        // Assert
        Assert.That(results, Has.All.True);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_ExceedingLimit_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(5, 10.0);

        // Act - Acquire up to limit
        for (int i = 0; i < 50; i++)
        {
            rateLimiter.TryAcquire(1);
        }

        // Try to exceed
        var exceedResult = rateLimiter.TryAcquire(1);

        // Assert
        Assert.That(exceedResult, Is.False);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_MultiplePermits_WorksCorrectly()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act
        var result = rateLimiter.TryAcquire(10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_ExceedingPermitsInOneRequest_ReturnsFalse()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(10, 1.0);

        // Act
        var result = rateLimiter.TryAcquire(11);

        // Assert
        Assert.That(result, Is.False);
    }

    #endregion

    #region TokenBucketRateLimiter Tests

    [Test]
    public void TokenBucketRateLimiter_Constructor_InitializesCorrectly()
    {
        // Act
        using var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithZeroRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(0, 10));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithNegativeRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(-1, 10));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithZeroBurstCapacity_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(100, 0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithNegativeBurstCapacity_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(100, -1));
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_InitialRequest_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Act
        var result = rateLimiter.TryAcquire(1);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_WithinCapacity_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(10, 5);

        // Act - Acquire less than capacity
        var results = new List<bool>();
        for (int i = 0; i < 3; i++)
        {
            results.Add(rateLimiter.TryAcquire(1));
        }

        // Assert
        Assert.That(results, Has.All.True);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_ExceedingCapacity_ReturnsFalse()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(1, 5);

        // Act - Consume all tokens
        for (int i = 0; i < 5; i++)
        {
            rateLimiter.TryAcquire(1);
        }

        // Try to exceed
        var exceedResult = rateLimiter.TryAcquire(1);

        // Assert
        Assert.That(exceedResult, Is.False);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_MultipleTokens_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 100);

        // Act
        var result = rateLimiter.TryAcquire(10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_MoreThanCapacity_ReturnsFalse()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(10, 10);

        // Act
        var result = rateLimiter.TryAcquire(11);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TokenBucketRateLimiter_Dispose_DoesNotThrow()
    {
        // Arrange
        var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.Dispose());
    }

    [Test]
    public void TokenBucketRateLimiter_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            rateLimiter.Dispose();
            rateLimiter.Dispose();
        });
    }

    #endregion

    #region LagBasedRateLimiter Tests

    [Test]
    public void LagBasedRateLimiter_Constructor_InitializesCorrectly()
    {
        // Act
        using var rateLimiter = new LagBasedRateLimiter(100, 10, "test-group");

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_WithZeroRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LagBasedRateLimiter(0, 10, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_WithNegativeRate_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => new LagBasedRateLimiter(-1, 10, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_TryAcquire_WithZeroLag_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 10, "test-group");

        // Act
        var result = rateLimiter.TryAcquire(1);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void LagBasedRateLimiter_Dispose_DoesNotThrow()
    {
        // Arrange
        var rateLimiter = new LagBasedRateLimiter(100, 10, "test-group");

        // Act & Assert
        Assert.DoesNotThrow(() => rateLimiter.Dispose());
    }

    #endregion
}
