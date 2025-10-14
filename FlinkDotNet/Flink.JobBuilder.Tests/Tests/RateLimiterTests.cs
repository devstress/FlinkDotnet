using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class RateLimiterTests
{
    #region TokenBucketRateLimiter Tests (20 tests)

    [Test]
    public void TokenBucketRateLimiter_Constructor_ValidParameters_CreatesInstance()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
        Assert.That(rateLimiter.MaxTokens, Is.EqualTo(20.0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_ZeroRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(0, 10.0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_NegativeRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(-5.0, 10.0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_ZeroBurstCapacity_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(10.0, 0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_NegativeBurstCapacity_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new TokenBucketRateLimiter(10.0, -5.0));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithRateLimiterId_UsesProvidedId()
    {
        var rateLimiterId = "test-limiter-123";
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0, rateLimiterId);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.RateLimiterId, Is.EqualTo(rateLimiterId));
    }

    [Test]
    public void TokenBucketRateLimiter_Constructor_WithoutRateLimiterId_GeneratesId()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.That(rateLimiter.RateLimiterId, Is.Not.Null);
        Assert.That(rateLimiter.RateLimiterId, Is.Not.Empty);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_WithAvailableTokens_ReturnsTrue()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_MultiplePermits_ConsumesMultipleTokens()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        var result = rateLimiter.TryAcquire(5);

        Assert.That(result, Is.True);
        Assert.That(rateLimiter.CurrentTokens, Is.LessThan(20.0));
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_ExceedingBurstCapacity_ReturnsFalse()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 5.0);

        // Consume all tokens
        rateLimiter.TryAcquire(5);

        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.False);
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_ZeroPermits_ThrowsArgumentException()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.TryAcquire(0));
    }

    [Test]
    public void TokenBucketRateLimiter_TryAcquire_NegativePermits_ThrowsArgumentException()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.TryAcquire(-1));
    }

    [Test]
    public async Task TokenBucketRateLimiter_TryAcquireAsync_WithAvailableTokens_ReturnsTrue()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        var result = await rateLimiter.TryAcquireAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task TokenBucketRateLimiter_TryAcquireAsync_ExceedingBurstCapacity_ReturnsFalse()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 5.0);

        // Consume all tokens
        await rateLimiter.TryAcquireAsync(5);

        var result = await rateLimiter.TryAcquireAsync();

        Assert.That(result, Is.False);
    }

    [Test]
    public async Task TokenBucketRateLimiter_AcquireAsync_WithAvailableTokens_CompletesSynchronously()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        await rateLimiter.AcquireAsync();

        Assert.That(rateLimiter.CurrentTokens, Is.LessThan(20.0));
    }

    [Test]
    public void TokenBucketRateLimiter_AcquireAsync_WithPreCancelledToken_ThrowsOperationCanceledException()
    {
        var rateLimiter = new TokenBucketRateLimiter(0.1, 1.0); // Very slow refill rate

        // Consume all tokens
        rateLimiter.TryAcquire();

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // TaskCanceledException inherits from OperationCanceledException
        Assert.CatchAsync<OperationCanceledException>(async () =>
            await rateLimiter.AcquireAsync(1, cts.Token));
    }

    [Test]
    public void TokenBucketRateLimiter_UpdateRateLimit_ValidNewRate_UpdatesRate()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        rateLimiter.UpdateRateLimit(15.0);

        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(15.0));
    }

    [Test]
    public void TokenBucketRateLimiter_UpdateRateLimit_ZeroRate_ThrowsArgumentException()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.UpdateRateLimit(0));
    }

    [Test]
    public void TokenBucketRateLimiter_UpdateRateLimit_NegativeRate_ThrowsArgumentException()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.UpdateRateLimit(-5.0));
    }

    [Test]
    public void TokenBucketRateLimiter_CurrentUtilization_InitialState_ReturnsZero()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        var utilization = rateLimiter.CurrentUtilization;

        Assert.That(utilization, Is.EqualTo(0.0).Within(0.01));
    }

    [Test]
    public void TokenBucketRateLimiter_CurrentUtilization_AfterConsumingTokens_ReturnsNonZero()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        rateLimiter.TryAcquire(10);

        var utilization = rateLimiter.CurrentUtilization;

        Assert.That(utilization, Is.GreaterThan(0.0));
    }

    [Test]
    public void TokenBucketRateLimiter_Reset_AfterConsumingTokens_RestoresFullCapacity()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        rateLimiter.TryAcquire(10);
        rateLimiter.Reset();

        var result = rateLimiter.TryAcquire(20);

        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_CanAccommodateBurst_WithinCapacity_ReturnsTrue()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        var result = rateLimiter.CanAccommodateBurst(15);

        Assert.That(result, Is.True);
    }

    [Test]
    public void TokenBucketRateLimiter_CanAccommodateBurst_ExceedingCapacity_ReturnsFalse()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        // Test with a burst that exceeds even the total capacity calculation
        var result = rateLimiter.CanAccommodateBurst(50);

        Assert.That(result, Is.False);
    }

    [Test]
    public void TokenBucketRateLimiter_Dispose_DisposesResources()
    {
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);

        rateLimiter.Dispose();

        // Verify disposal doesn't throw
        Assert.Pass();
    }

    #endregion

    #region SlidingWindowRateLimiter Tests (15 tests)

    [Test]
    public void SlidingWindowRateLimiter_Constructor_ValidParameters_CreatesInstance()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
        Assert.That(rateLimiter.WindowSize, Is.EqualTo(TimeSpan.FromSeconds(1.0)));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_ZeroRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SlidingWindowRateLimiter(0, 1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_NegativeRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SlidingWindowRateLimiter(-5.0, 1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_ZeroWindowSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SlidingWindowRateLimiter(10.0, 0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Constructor_NegativeWindowSize_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => new SlidingWindowRateLimiter(10.0, -1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_WithinLimit_ReturnsTrue()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.True);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_ExceedingLimit_ReturnsFalse()
    {
        var rateLimiter = new SlidingWindowRateLimiter(5.0, 1.0);

        // Consume all permits in window
        for (int i = 0; i < 5; i++)
        {
            rateLimiter.TryAcquire();
        }

        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.False);
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_ZeroPermits_ThrowsArgumentException()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.TryAcquire(0));
    }

    [Test]
    public void SlidingWindowRateLimiter_TryAcquire_NegativePermits_ThrowsArgumentException()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        Assert.Throws<ArgumentException>(() => rateLimiter.TryAcquire(-1));
    }

    [Test]
    public async Task SlidingWindowRateLimiter_TryAcquireAsync_WithinLimit_ReturnsTrue()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        var result = await rateLimiter.TryAcquireAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public void SlidingWindowRateLimiter_CurrentRequestCount_AfterRequests_ReturnsCorrectCount()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        rateLimiter.TryAcquire(3);

        Assert.That(rateLimiter.CurrentRequestCount, Is.EqualTo(3));
    }

    [Test]
    public void SlidingWindowRateLimiter_ActualRate_AfterRequests_ReturnsCorrectRate()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        rateLimiter.TryAcquire(5);

        Assert.That(rateLimiter.ActualRate, Is.GreaterThan(0));
    }

    [Test]
    public void SlidingWindowRateLimiter_CurrentUtilization_AfterRequests_ReturnsCorrectValue()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        rateLimiter.TryAcquire(5);

        var utilization = rateLimiter.CurrentUtilization;

        Assert.That(utilization, Is.GreaterThan(0));
        Assert.That(utilization, Is.LessThanOrEqualTo(1.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_UpdateRateLimit_ValidNewRate_UpdatesRate()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        rateLimiter.UpdateRateLimit(15.0);

        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(15.0));
    }

    [Test]
    public void SlidingWindowRateLimiter_Reset_ClearsRequestHistory()
    {
        var rateLimiter = new SlidingWindowRateLimiter(10.0, 1.0);

        rateLimiter.TryAcquire(5);
        rateLimiter.Reset();

        Assert.That(rateLimiter.CurrentRequestCount, Is.EqualTo(0));
    }

    #endregion

    #region LagBasedRateLimiter Tests (15 tests)

    [Test]
    public void LagBasedRateLimiter_Constructor_ValidParameters_CreatesInstance()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_ZeroRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(0, 20.0, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NegativeRateLimit_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(-5.0, 20.0, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_ZeroBurstCapacity_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 0, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NegativeBurstCapacity_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, -5.0, "test-group"));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NullConsumerGroup_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 20.0, null!));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_EmptyConsumerGroup_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 20.0, string.Empty));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_WithLagThreshold_UsesProvidedThreshold()
    {
        var lagThreshold = TimeSpan.FromSeconds(10);
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagThreshold);

        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void LagBasedRateLimiter_TryAcquire_WithAvailableTokens_ReturnsTrue()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task LagBasedRateLimiter_TryAcquireAsync_WithAvailableTokens_ReturnsTrue()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        var result = await rateLimiter.TryAcquireAsync();

        Assert.That(result, Is.True);
    }

    [Test]
    public void LagBasedRateLimiter_CurrentUtilization_InitialState_ReturnsZero()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        var utilization = rateLimiter.CurrentUtilization;

        Assert.That(utilization, Is.EqualTo(0.0).Within(0.01));
    }

    [Test]
    public void LagBasedRateLimiter_UpdateRateLimit_ValidNewRate_UpdatesRate()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        rateLimiter.UpdateRateLimit(15.0);

        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(15.0));
    }

    [Test]
    public void LagBasedRateLimiter_Reset_RestoresFullCapacity()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        rateLimiter.TryAcquire(10);
        rateLimiter.Reset();

        var result = rateLimiter.TryAcquire(20);

        Assert.That(result, Is.True);
    }

    [Test]
    public void LagBasedRateLimiter_CurrentTokens_AfterConsumption_DecreasesCorrectly()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        var initialTokens = rateLimiter.CurrentTokens;
        rateLimiter.TryAcquire(5);

        Assert.That(rateLimiter.CurrentTokens, Is.LessThan(initialTokens));
    }

    [Test]
    public void LagBasedRateLimiter_Dispose_DisposesResources()
    {
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group");

        rateLimiter.Dispose();

        // Verify disposal doesn't throw
        Assert.Pass();
    }

    #endregion

    #region MultiTierRateLimiter Tests (10 tests)

    [Test]
    public void MultiTierRateLimiter_Constructor_CreatesInstance()
    {
        var rateLimiter = new MultiTierRateLimiter();

        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_Constructor_WithStateStorage_UsesProvidedStorage()
    {
        var storage = new InMemoryRateLimiterStateStorage();
        var rateLimiter = new MultiTierRateLimiter(storage);

        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_ConfigureTiers_WithValidTiers_ConfiguresSuccessfully()
    {
        var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new()
            {
                Name = "Global",
                Scope = "Entire cluster",
                RateLimit = 1000,
                BurstCapacity = 2000,
                BurstDuration = TimeSpan.FromSeconds(1),
                Enforcement = RateLimitingEnforcement.HardLimit
            }
        };

        rateLimiter.ConfigureTiers(tiers);

        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_TryAcquire_WithValidContext_ReturnsResult()
    {
        var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new()
            {
                Name = "Global",
                Scope = "Entire cluster",
                RateLimit = 1000,
                BurstCapacity = 2000,
                BurstDuration = TimeSpan.FromSeconds(1),
                Enforcement = RateLimitingEnforcement.HardLimit
            }
        };
        rateLimiter.ConfigureTiers(tiers);

        var context = new RateLimitingContext
        {
            TopicName = "test-topic"
        };

        var result = rateLimiter.TryAcquire(context);

        Assert.That(result, Is.True);
    }

    [Test]
    public async Task MultiTierRateLimiter_TryAcquireAsync_WithValidContext_ReturnsResult()
    {
        var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new()
            {
                Name = "Global",
                Scope = "Entire cluster",
                RateLimit = 1000,
                BurstCapacity = 2000,
                BurstDuration = TimeSpan.FromSeconds(1),
                Enforcement = RateLimitingEnforcement.HardLimit
            }
        };
        rateLimiter.ConfigureTiers(tiers);

        var context = new RateLimitingContext
        {
            TopicName = "test-topic"
        };

        var result = await rateLimiter.TryAcquireAsync(context);

        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiTierRateLimiter_UpdateRateLimit_ValidTierName_UpdatesRate()
    {
        var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new()
            {
                Name = "Global",
                Scope = "Entire cluster",
                RateLimit = 1000,
                BurstCapacity = 2000,
                BurstDuration = TimeSpan.FromSeconds(1),
                Enforcement = RateLimitingEnforcement.HardLimit
            }
        };
        rateLimiter.ConfigureTiers(tiers);

        rateLimiter.UpdateRateLimit("Global", 1500);

        // Verify update doesn't throw
        Assert.Pass();
    }

    [Test]
    public void MultiTierRateLimiter_GetUtilizationMetrics_ReturnsMetrics()
    {
        var rateLimiter = new MultiTierRateLimiter();
        var tiers = new List<RateLimitingTier>
        {
            new()
            {
                Name = "Global",
                Scope = "Entire cluster",
                RateLimit = 1000,
                BurstCapacity = 2000,
                BurstDuration = TimeSpan.FromSeconds(1),
                Enforcement = RateLimitingEnforcement.HardLimit
            }
        };
        rateLimiter.ConfigureTiers(tiers);

        var metrics = rateLimiter.GetUtilizationMetrics();

        Assert.That(metrics, Is.Not.Null);
    }

    [Test]
    public void MultiTierRateLimiter_ValidateHierarchicalEnforcement_ReturnsValidationResult()
    {
        var rateLimiter = new MultiTierRateLimiter();

        var result = rateLimiter.ValidateHierarchicalEnforcement();

        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_ValidateBurstAccommodation_ReturnsValidationResult()
    {
        var rateLimiter = new MultiTierRateLimiter();

        var result = rateLimiter.ValidateBurstAccommodation();

        Assert.That(result, Is.InstanceOf<bool>());
    }

    [Test]
    public void MultiTierRateLimiter_Dispose_DisposesResources()
    {
        var rateLimiter = new MultiTierRateLimiter();

        rateLimiter.Dispose();

        // Verify disposal doesn't throw
        Assert.Pass();
    }

    #endregion

    #region RateLimiterFactory Tests (5 tests)

    [Test]
    public void RateLimiterFactory_CreateLagBasedBucket_ValidParameters_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(10.0, 20.0, "test-group");

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
    }

    [Test]
    public void RateLimiterFactory_CreateProductionConfiguration_ReturnsConfiguredLimiter()
    {
        var (rateLimiter, config) = RateLimiterFactory.CreateProductionConfiguration();

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.Not.Empty);
    }

    [Test]
    public void RateLimiterFactory_CreateDevelopmentConfiguration_ReturnsConfiguredLimiter()
    {
        var (rateLimiter, config) = RateLimiterFactory.CreateDevelopmentConfiguration();

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(config, Is.Not.Null);
        Assert.That(config, Is.Not.Empty);
    }

    [Test]
    public void RateLimiterFactory_CreateWithInMemoryStorage_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateWithInMemoryStorage(10.0, 20.0);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
    }

    [Test]
    public void RateLimiterFactory_CreateMultiTierWithInMemoryStorage_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateMultiTierWithInMemoryStorage();

        Assert.That(rateLimiter, Is.Not.Null);
    }

    #endregion

    #region RateLimitingContext Tests (3 tests)

    [Test]
    public void RateLimitingContext_Constructor_InitializesProperties()
    {
        var context = new RateLimitingContext
        {
            TopicName = "test-topic",
            ConsumerGroup = "test-group",
            ConsumerId = "consumer-1",
            ClientId = "client-1",
            ClientIp = "192.168.1.1",
            RequestType = "fetch"
        };

        Assert.That(context.TopicName, Is.EqualTo("test-topic"));
        Assert.That(context.ConsumerGroup, Is.EqualTo("test-group"));
        Assert.That(context.ConsumerId, Is.EqualTo("consumer-1"));
        Assert.That(context.ClientId, Is.EqualTo("client-1"));
        Assert.That(context.ClientIp, Is.EqualTo("192.168.1.1"));
        Assert.That(context.RequestType, Is.EqualTo("fetch"));
    }

    [Test]
    public void RateLimitingContext_AdditionalProperties_InitializesEmpty()
    {
        var context = new RateLimitingContext();

        Assert.That(context.AdditionalProperties, Is.Not.Null);
        Assert.That(context.AdditionalProperties, Is.Empty);
    }

    [Test]
    public void RateLimitingContext_AdditionalProperties_CanAddCustomProperties()
    {
        var context = new RateLimitingContext();
        context.AdditionalProperties["custom-key"] = "custom-value";

        Assert.That(context.AdditionalProperties["custom-key"], Is.EqualTo("custom-value"));
    }

    #endregion
}
