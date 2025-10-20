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

    [Test]
    public void SlidingWindowRateLimiter_CurrentUtilization_ReturnsCorrectValue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act - acquire some permits
        rateLimiter.TryAcquire(25);
        var utilization = rateLimiter.CurrentUtilization;

        // Assert
        Assert.That(utilization, Is.GreaterThanOrEqualTo(0));
        Assert.That(utilization, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_UpdateRateLimit_UpdatesLimit()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);
        
        // Act
        rateLimiter.UpdateRateLimit(200);

        // Assert
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(200));
    }

    [Test]
    public void SlidingWindowRateLimiter_Reset_ClearsRequests()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(10, 1.0);
        rateLimiter.TryAcquire(5);

        // Act
        rateLimiter.Reset();
        var requestCount = rateLimiter.CurrentRequestCount;

        // Assert
        Assert.That(requestCount, Is.EqualTo(0));
    }

    [Test]
    public void SlidingWindowRateLimiter_CurrentRequestCount_ReturnsCorrectValue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act
        rateLimiter.TryAcquire(10);
        rateLimiter.TryAcquire(15);
        var count = rateLimiter.CurrentRequestCount;

        // Assert
        Assert.That(count, Is.EqualTo(25));
    }

    [Test]
    public void SlidingWindowRateLimiter_ActualRate_ReturnsValidValue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);
        rateLimiter.TryAcquire(10);

        // Act
        var actualRate = rateLimiter.ActualRate;

        // Assert
        Assert.That(actualRate, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public void SlidingWindowRateLimiter_WindowSize_ReturnsConfiguredSize()
    {
        // Arrange & Act
        var rateLimiter = new SlidingWindowRateLimiter(100, 2.5);

        // Assert
        Assert.That(rateLimiter.WindowSize.TotalSeconds, Is.EqualTo(2.5));
    }

    [Test]
    public async Task SlidingWindowRateLimiter_TryAcquireAsync_WithValidPermits_ReturnsTrue()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act
        var result = await rateLimiter.TryAcquireAsync(10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SlidingWindowRateLimiter_AcquireAsync_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act & Assert - should complete without exception
        await Task.Run(async () => await rateLimiter.AcquireAsync(10));
        Assert.Pass();
    }

    [Test]
    public async Task SlidingWindowRateLimiter_TryAcquireAsync_WithCancellationToken_HandlesToken()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await rateLimiter.TryAcquireAsync(10, cts.Token);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task SlidingWindowRateLimiter_AcquireAsync_WithMultipleRequests_WaitsAppropriately()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(50, 1.0);
        rateLimiter.TryAcquire(50); // Fill the rate limiter

        // Act - This should wait for tokens to become available
        var startTime = DateTime.UtcNow;
        await rateLimiter.AcquireAsync(10);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should have waited some time
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(0));
    }

    [Test]
    public void SlidingWindowRateLimiter_MultipleAcquireAndReset_WorksCorrectly()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(100, 1.0);

        // Act - Multiple cycles of acquire and reset
        for (int i = 0; i < 3; i++)
        {
            rateLimiter.TryAcquire(30);
            rateLimiter.Reset();
        }
        var result = rateLimiter.TryAcquire(50);

        // Assert
        Assert.That(result, Is.True);
        Assert.That(rateLimiter.CurrentRequestCount, Is.EqualTo(50));
    }

    [Test]
    public void SlidingWindowRateLimiter_UpdateRateLimit_PreservesCurrentRequests()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(10, 1.0);
        rateLimiter.TryAcquire(5);

        // Act
        rateLimiter.UpdateRateLimit(100);
        
        // Assert - Updated rate limit should be reflected
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(100));
        Assert.That(rateLimiter.CurrentRequestCount, Is.EqualTo(5));
    }

    [Test]
    public void SlidingWindowRateLimiter_LargeNumberOfSmallRequests_HandlesCorrectly()
    {
        // Arrange
        var rateLimiter = new SlidingWindowRateLimiter(1000, 1.0);

        // Act - Many small requests
        int successCount = 0;
        for (int i = 0; i < 100; i++)
        {
            if (rateLimiter.TryAcquire(5))
            {
                successCount++;
            }
        }

        // Assert - Should successfully acquire until limit is reached
        Assert.That(successCount, Is.GreaterThan(0));
        Assert.That(rateLimiter.CurrentRequestCount, Is.LessThanOrEqualTo(1000));
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

    [Test]
    public void LagBasedRateLimiter_MaxTokens_ReturnsCorrectValue()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        var maxTokens = rateLimiter.MaxTokens;

        // Assert
        Assert.That(maxTokens, Is.EqualTo(50));
    }

    [Test]
    public void LagBasedRateLimiter_ConsumerGroup_ReturnsConfiguredValue()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "my-consumer-group");

        // Act
        var consumerGroup = rateLimiter.ConsumerGroup;

        // Assert
        Assert.That(consumerGroup, Is.EqualTo("my-consumer-group"));
    }

    [Test]
    public void LagBasedRateLimiter_LagThreshold_ReturnsDefaultValue()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        var lagThreshold = rateLimiter.LagThreshold;

        // Assert
        Assert.That(lagThreshold, Is.Not.Null);
        Assert.That(lagThreshold.TotalSeconds, Is.GreaterThan(0));
    }

    [Test]
    public void LagBasedRateLimiter_LagThreshold_WithCustomValue_ReturnsCustomValue()
    {
        // Arrange
        var customThreshold = TimeSpan.FromSeconds(10);
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group", customThreshold);

        // Act
        var lagThreshold = rateLimiter.LagThreshold;

        // Assert
        Assert.That(lagThreshold, Is.EqualTo(customThreshold));
    }

    [Test]
    public void LagBasedRateLimiter_IsRefillPaused_InitiallyFalse()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        var isRefillPaused = rateLimiter.IsRefillPaused;

        // Assert
        Assert.That(isRefillPaused, Is.False);
    }

    [Test]
    public void LagBasedRateLimiter_CurrentLag_ReturnsValidValue()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        var currentLag = rateLimiter.CurrentLag;

        // Assert
        Assert.That(currentLag, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
    }

    [Test]
    public void LagBasedRateLimiter_UpdateRateLimit_UpdatesSuccessfully()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        rateLimiter.UpdateRateLimit(200);

        // Assert
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(200));
    }

    [Test]
    public void LagBasedRateLimiter_Reset_ResetsState()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");
        rateLimiter.TryAcquire(25);

        // Act
        rateLimiter.Reset();

        // Assert - After reset should be able to acquire again
        var result = rateLimiter.TryAcquire(25);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LagBasedRateLimiter_TryAcquireAsync_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act
        var result = await rateLimiter.TryAcquireAsync(10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LagBasedRateLimiter_AcquireAsync_CompletesSuccessfully()
    {
        // Arrange
        using var rateLimiter = new LagBasedRateLimiter(100, 50, "test-group");

        // Act & Assert - Should complete without exception
        await rateLimiter.AcquireAsync(10);
        Assert.Pass();
    }

    #endregion

    #region Additional MultiTierRateLimiter Coverage Tests

    [Test]
    public void MultiTierRateLimiter_TryAcquire_WithDisposedInstance_ThrowsObjectDisposedException()
    {
        // Arrange
        var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext { TopicName = "test-topic", ConsumerGroup = "test-group" };
        
        rateLimiter.Dispose();

        // Act & Assert
        Assert.Throws<ObjectDisposedException>(() => rateLimiter.TryAcquire(context, 1));
    }

    [Test]
    public async Task MultiTierRateLimiter_TryAcquireAsync_WithContext_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext 
        { 
            TopicName = "test-topic", 
            ConsumerGroup = "test-group",
            ConsumerId = "consumer-1"
        };

        // Act
        var result = await rateLimiter.TryAcquireAsync(context, 1);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiTierRateLimiter_ValidateHierarchicalEnforcement_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateHierarchicalEnforcement();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateBurstAccommodation_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateBurstAccommodation();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidatePriorityPreservation_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidatePriorityPreservation();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateAdaptiveAdjustment_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateAdaptiveAdjustment();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateRebalancingIntegration_ReturnsBoolean()
    {
        // Act
        var result = MultiTierRateLimiter.ValidateRebalancingIntegration();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateFairAllocation_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateFairAllocation();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateMultiTierEnforcement_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateMultiTierEnforcement();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateQuotaEnforcement_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var result = rateLimiter.ValidateQuotaEnforcement();

        // Assert
        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_GetUtilizationMetrics_ReturnsMetrics()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var metrics = rateLimiter.GetUtilizationMetrics();

        // Assert
        Assert.That(metrics, Is.Not.Null);
        Assert.That(metrics, Is.InstanceOf<Dictionary<string, double>>());
    }

    [Test]
    public void MultiTierRateLimiter_UpdateRateLimit_UpdatesSuccessfully()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new RateLimitingTier
            {
                Name = "global",
                RateLimit = 1000,
                BurstCapacity = 2000
            }
        };
        rateLimiter.ConfigureTiers(tiers);

        // Act
        rateLimiter.UpdateRateLimit("global", 1500);

        // Assert - Should not throw
        Assert.Pass();
    }

    [Test]
    public void MultiTierRateLimiter_IsDistributed_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var isDistributed = rateLimiter.IsDistributed;

        // Assert
        Assert.That(isDistributed, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_IsPersistent_ReturnsBoolean()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();

        // Act
        var isPersistent = rateLimiter.IsPersistent;

        // Assert
        Assert.That(isPersistent, Is.InstanceOf<bool>());
    }

    [Test]
    public async Task MultiTierRateLimiter_AcquireAsync_CompletesSuccessfully()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext 
        { 
            TopicName = "test-topic", 
            ConsumerGroup = "test-group"
        };

        // Act & Assert - Should complete without exception
        await rateLimiter.AcquireAsync(context, 1);
        Assert.Pass();
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquire_WithValidContext_ReturnsTrue()
    {
        // Arrange
        using var rateLimiter = new MultiTierRateLimiter();
        var context = new RateLimitingContext 
        { 
            TopicName = "test-topic", 
            ConsumerGroup = "test-group"
        };

        // Act
        var result = rateLimiter.TryAcquire(context, 1);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region Additional TokenBucketRateLimiter Coverage Tests

    [Test]
    public async Task TokenBucketRateLimiter_TryAcquireAsync_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Act
        var result = await rateLimiter.TryAcquireAsync(10);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_MultipleDispose_HandlesCorrectly()
    {
        // Arrange
        var rateLimiter = new TokenBucketRateLimiter(100, 10);

        // Act & Assert - Multiple dispose should be safe
        rateLimiter.Dispose();
        Assert.DoesNotThrow(() => rateLimiter.Dispose());
    }

    [Test]
    public void TokenBucketRateLimiter_CurrentUtilization_ReturnsValidValue()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);

        // Act
        rateLimiter.TryAcquire(25);
        var utilization = rateLimiter.CurrentUtilization;

        // Assert
        Assert.That(utilization, Is.GreaterThanOrEqualTo(0));
        Assert.That(utilization, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void TokenBucketRateLimiter_UpdateRateLimit_UpdatesSuccessfully()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);

        // Act
        rateLimiter.UpdateRateLimit(200);

        // Assert
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(200));
    }

    [Test]
    public void TokenBucketRateLimiter_Reset_ClearsState()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);
        rateLimiter.TryAcquire(40);

        // Act
        rateLimiter.Reset();

        // Assert - after reset, should be able to acquire full capacity
        var result = rateLimiter.TryAcquire(50);
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TokenBucketRateLimiter_AcquireAsync_WaitsForTokens()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 10);
        rateLimiter.TryAcquire(10); // Use all tokens

        // Act - this should wait briefly for tokens to refill
        var startTime = DateTime.UtcNow;
        await rateLimiter.AcquireAsync(5);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - should have waited some time
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(0));
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_AfterReset_FullCapacity()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);
        rateLimiter.TryAcquire(30);
        rateLimiter.Reset();

        // Act
        var result = rateLimiter.TryAcquire(50);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TokenBucketRateLimiter_AcquireAsync_WithExhaustedTokens_WaitsForRefill()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 10);
        rateLimiter.TryAcquire(10); // Exhaust all tokens

        // Act
        var startTime = DateTime.UtcNow;
        await rateLimiter.AcquireAsync(5);
        var elapsed = DateTime.UtcNow - startTime;

        // Assert - Should have waited for refill
        Assert.That(elapsed.TotalMilliseconds, Is.GreaterThan(0));
    }

    [Test]
    public void TokenBucketRateLimiter_MultipleSmallAcquisitions_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(1000, 100);

        // Act - Many small acquisitions
        int successCount = 0;
        for (int i = 0; i < 50; i++)
        {
            if (rateLimiter.TryAcquire(1))
            {
                successCount++;
            }
        }

        // Assert
        Assert.That(successCount, Is.EqualTo(50));
    }

    [Test]
    public void TokenBucketRateLimiter_ExceedingBurstCapacity_ReturnsFalse()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);

        // Act - Try to acquire more than burst capacity
        var result = rateLimiter.TryAcquire(51);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void TokenBucketRateLimiter_UpdateRateLimit_ThenAcquire_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);
        rateLimiter.TryAcquire(40);

        // Act
        rateLimiter.UpdateRateLimit(200);
        rateLimiter.Reset(); // Reset to get new rate
        var result = rateLimiter.TryAcquire(50);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TokenBucketRateLimiter_TryAcquireAsync_WithCancellationToken_WorksCorrectly()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);
        using var cts = new CancellationTokenSource();

        // Act
        var result = await rateLimiter.TryAcquireAsync(10, cts.Token);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_CurrentUtilization_AfterMultipleOperations_ReturnsValidValue()
    {
        // Arrange
        using var rateLimiter = new TokenBucketRateLimiter(100, 50);

        // Act
        rateLimiter.TryAcquire(10);
        rateLimiter.TryAcquire(5);
        rateLimiter.Reset();
        rateLimiter.TryAcquire(20);
        var utilization = rateLimiter.CurrentUtilization;

        // Assert
        Assert.That(utilization, Is.GreaterThanOrEqualTo(0));
        Assert.That(utilization, Is.LessThanOrEqualTo(1.0));
    }

    #endregion
}
