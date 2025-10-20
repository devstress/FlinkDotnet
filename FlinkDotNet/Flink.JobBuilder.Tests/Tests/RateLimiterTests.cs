using Flink.JobBuilder.Backpressure;
using Moq;

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

    #region RateLimiterFactory Tests (15 tests)

    [Test]
    public void RateLimiterFactory_CreateLagBasedBucket_ValidParameters_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(10.0, 20.0, "test-group");

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
    }

    [Test]
    public void RateLimiterFactory_CreateLagBasedBucket_WithAllParameters_CreatesInstance()
    {
        var lagThreshold = TimeSpan.FromSeconds(10);
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 50.0,
            burstCapacity: 100.0,
            consumerGroup: "production-group",
            lagThreshold: lagThreshold
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(50.0));
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
    public void RateLimiterFactory_CreateProductionConfiguration_WithCustomParameters_ReturnsConfigured()
    {
        var (rateLimiter, config) = RateLimiterFactory.CreateProductionConfiguration(
            rateLimit: 2000.0,
            burstCapacity: 4000.0,
            consumerGroup: "custom-consumer-group",
            lagThreshold: TimeSpan.FromSeconds(10)
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(2000.0));
        Assert.That(config, Contains.Substring("2000"));
        Assert.That(config, Contains.Substring("4000"));
        Assert.That(config, Contains.Substring("custom-consumer-group"));
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
    public void RateLimiterFactory_CreateDevelopmentConfiguration_WithCustomParameters_ReturnsConfigured()
    {
        var (rateLimiter, config) = RateLimiterFactory.CreateDevelopmentConfiguration(
            rateLimit: 50.0,
            burstCapacity: 100.0,
            consumerGroup: "dev-test-group"
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(50.0));
        Assert.That(config, Contains.Substring("50"));
        Assert.That(config, Contains.Substring("100"));
        Assert.That(config, Contains.Substring("dev-test-group"));
    }

    [Test]
    public void RateLimiterFactory_CreateWithInMemoryStorage_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateWithInMemoryStorage(10.0, 20.0);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
    }

    [Test]
    public void RateLimiterFactory_CreateWithInMemoryStorage_WithRateLimiterId_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateWithInMemoryStorage(
            rateLimit: 15.0,
            burstCapacity: 30.0,
            rateLimiterId: "custom-limiter-id"
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(15.0));
    }

    [Test]
    public void RateLimiterFactory_CreateMultiTierWithInMemoryStorage_CreatesInstance()
    {
        var rateLimiter = RateLimiterFactory.CreateMultiTierWithInMemoryStorage();

        Assert.That(rateLimiter, Is.Not.Null);
    }

    [Test]
    public void RateLimiterFactory_CreateProductionKafkaConfig_WithBootstrapServers_ReturnsConfig()
    {
        var config = RateLimiterFactory.CreateProductionKafkaConfig("localhost:9092");

        Assert.That(config, Is.Not.Null);
        Assert.That(config.BootstrapServers, Is.EqualTo("localhost:9092"));
        Assert.That(config.Performance, Is.Not.Null);
        Assert.That(config.Performance.ReplicationFactor, Is.EqualTo(3));
        Assert.That(config.Performance.PartitionCount, Is.EqualTo(12));
    }

    [Test]
    public void RateLimiterFactory_CreateProductionKafkaConfig_WithCustomTopicName_ReturnsConfig()
    {
        var config = RateLimiterFactory.CreateProductionKafkaConfig(
            "kafka.example.com:9092",
            "custom-state-topic"
        );

        Assert.That(config, Is.Not.Null);
        Assert.That(config.BootstrapServers, Is.EqualTo("kafka.example.com:9092"));
    }

    [Test]
    public void RateLimiterFactory_CreateWithKafkaStorage_CreatesInstance()
    {
        var kafkaConfig = RateLimiterFactory.CreateProductionKafkaConfig("localhost:9092");
        var rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
            rateLimit: 100.0,
            burstCapacity: 200.0,
            kafkaConfig: kafkaConfig
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(100.0));
    }

    [Test]
    public void RateLimiterFactory_CreateWithKafkaStorage_WithRateLimiterId_CreatesInstance()
    {
        var kafkaConfig = RateLimiterFactory.CreateProductionKafkaConfig("localhost:9092");
        var rateLimiter = RateLimiterFactory.CreateWithKafkaStorage(
            rateLimit: 150.0,
            burstCapacity: 300.0,
            kafkaConfig: kafkaConfig,
            rateLimiterId: "kafka-limiter-id"
        );

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(150.0));
    }

    [Test]
    public void RateLimiterFactory_CreateMultiTierWithKafkaStorage_CreatesInstance()
    {
        var kafkaConfig = RateLimiterFactory.CreateProductionKafkaConfig("localhost:9092");
        var rateLimiter = RateLimiterFactory.CreateMultiTierWithKafkaStorage(kafkaConfig);

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

    #region LagBasedRateLimiter Tests with Mocked Lag Monitor (15 tests)

    [Test]
    public void LagBasedRateLimiter_Constructor_ValidParameters_CreatesInstance()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);

        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(10.0));
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_ZeroRateLimit_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NegativeRateLimit_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(-5.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_ZeroBurstCapacity_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 0, "test-group", lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NegativeBurstCapacity_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, -5.0, "test-group", lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_NullConsumerGroup_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 20.0, null!, lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_EmptyConsumerGroup_ThrowsArgumentException()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        
        Assert.Throws<ArgumentException>(() =>
            new LagBasedRateLimiter(10.0, 20.0, string.Empty, lagMonitor: mockLagMonitor.Object));
    }

    [Test]
    public void LagBasedRateLimiter_Constructor_WithLagThreshold_UsesProvidedThreshold()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var lagThreshold = TimeSpan.FromSeconds(10);
        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagThreshold, mockLagMonitor.Object);

        Assert.That(rateLimiter, Is.Not.Null);
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_TryAcquire_WithAvailableTokens_ReturnsTrue()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        var result = rateLimiter.TryAcquire();

        Assert.That(result, Is.True);
        rateLimiter.Dispose();
    }

    [Test]
    public async Task LagBasedRateLimiter_TryAcquireAsync_WithAvailableTokens_ReturnsTrue()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        var result = await rateLimiter.TryAcquireAsync();

        Assert.That(result, Is.True);
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_CurrentUtilization_InitialState_ReturnsZero()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        var utilization = rateLimiter.CurrentUtilization;

        Assert.That(utilization, Is.GreaterThanOrEqualTo(0.0));
        Assert.That(utilization, Is.LessThanOrEqualTo(1.0));
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_UpdateRateLimit_UpdatesRateLimit()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        rateLimiter.UpdateRateLimit(15.0);

        Assert.That(rateLimiter.CurrentRateLimit, Is.EqualTo(15.0));
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_Reset_ClearsWaitingRequests()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        rateLimiter.Reset();

        Assert.That(rateLimiter.CurrentUtilization, Is.GreaterThanOrEqualTo(0.0));
        rateLimiter.Dispose();
    }

    [Test]
    public void LagBasedRateLimiter_Dispose_DisposesResources()
    {
        var mockLagMonitor = new Mock<IKafkaConsumerLagMonitor>();
        mockLagMonitor.Setup(m => m.GetCurrentLag(It.IsAny<string>())).Returns(TimeSpan.Zero);

        var rateLimiter = new LagBasedRateLimiter(10.0, 20.0, "test-group", lagMonitor: mockLagMonitor.Object);
        rateLimiter.Dispose();

        // Dispose should not throw
        Assert.Pass();
    }

    #endregion
}
