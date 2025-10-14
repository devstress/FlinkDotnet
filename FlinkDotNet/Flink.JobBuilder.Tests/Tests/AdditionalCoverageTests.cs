using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Additional tests to push coverage above 80%
/// Targeting specific uncovered edge cases and error paths
/// </summary>
[TestFixture]
public class AdditionalCoverageTests
{
    #region TokenBucketRateLimiter Edge Cases

    [Test]
    public void TokenBucketRateLimiter_Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var rateLimiter = new TokenBucketRateLimiter(10.0, 20.0);
        
        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            rateLimiter.Dispose();
            rateLimiter.Dispose(); // Second dispose should be safe
        });
    }

    #endregion

    #region RateLimiterFactory Edge Cases

    [Test]
    public void RateLimiterFactory_CreateWithInMemoryStorage_ValidParameters_CreatesInstance()
    {
        // Arrange & Act
        var rateLimiter = RateLimiterFactory.CreateWithInMemoryStorage(
            rateLimit: 100.0,
            burstCapacity: 200.0,
            rateLimiterId: "test-limiter"
        );

        // Assert
        Assert.That(rateLimiter, Is.Not.Null);
        Assert.That(rateLimiter, Is.TypeOf<TokenBucketRateLimiter>());
        
        // Cleanup
        rateLimiter.Dispose();
    }

    [Test]
    public void RateLimiterFactory_CreateProductionKafkaConfig_CreatesValidConfig()
    {
        // Arrange & Act
        var config = RateLimiterFactory.CreateProductionKafkaConfig(
            bootstrapServers: "localhost:9092",
            topicName: "test-topic"
        );

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.BootstrapServers, Is.EqualTo("localhost:9092"));
    }

    #endregion

    #region KafkaRateLimiterStateStorage Edge Cases

    [Test]
    public void KafkaRateLimiterStateStorage_Constructor_WithValidConfig_CreatesInstance()
    {
        // Arrange
        var config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092"
        };
        
        // Act
        var storage = new KafkaRateLimiterStateStorage(config);
        
        // Assert
        Assert.That(storage, Is.Not.Null);
        
        // Cleanup
        storage.Dispose();
    }

    [Test]
    public void KafkaRateLimiterStateStorage_Dispose_MultipleTimes_DoesNotThrow()
    {
        // Arrange
        var config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092"
        };
        
        var storage = new KafkaRateLimiterStateStorage(config);
        
        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            storage.Dispose();
            storage.Dispose(); // Second dispose should be safe
        });
    }

    #endregion
}
