using Confluent.Kafka;
using Flink.JobBuilder.Backpressure;
using Microsoft.Extensions.Logging;
using Moq;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Unit tests for KafkaRateLimiterStateStorage using mocked Kafka clients.
/// Tests verify configuration, initialization, and state management logic without real Kafka connections.
/// </summary>
[TestFixture]
public class KafkaRateLimiterStateStorageTests
{
    private Mock<ILogger<KafkaRateLimiterStateStorage>> _mockLogger = null!;
    private Mock<IKafkaClientFactory> _mockKafkaFactory = null!;
    private Mock<IProducer<string, string>> _mockProducer = null!;
    private Mock<IConsumer<string, string>> _mockConsumer = null!;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<KafkaRateLimiterStateStorage>>();
        _mockKafkaFactory = new Mock<IKafkaClientFactory>();
        _mockProducer = new Mock<IProducer<string, string>>();
        _mockConsumer = new Mock<IConsumer<string, string>>();

        // Setup factory to return mocked clients
        _mockKafkaFactory
            .Setup(f => f.CreateProducer<string, string>(It.IsAny<ProducerConfig>()))
            .Returns(_mockProducer.Object);
        
        _mockKafkaFactory
            .Setup(f => f.CreateConsumer<string, string>(It.IsAny<ConsumerConfig>()))
            .Returns(_mockConsumer.Object);
    }

    [Test]
    public void Constructor_WithValidConfig_CreatesInstance()
    {
        // Arrange
        var config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092"
        };

        // Act
        using var storage = new KafkaRateLimiterStateStorage(
            config, 
            "test-topic", 
            _mockLogger.Object, 
            _mockKafkaFactory.Object);

        // Assert
        Assert.That(storage, Is.Not.Null);
        Assert.That(storage.BackendInfo, Is.Not.Null);
        Assert.That(storage.BackendInfo.BackendType, Is.EqualTo("Apache Kafka"));
        
        // Verify factory was called to create clients
        _mockKafkaFactory.Verify(f => f.CreateProducer<string, string>(It.IsAny<ProducerConfig>()), Times.Once);
        _mockKafkaFactory.Verify(f => f.CreateConsumer<string, string>(It.IsAny<ConsumerConfig>()), Times.Once);
    }

    [Test]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        // Arrange
        var config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092"
        };

        // Act
        using var storage = new KafkaRateLimiterStateStorage(
            config, 
            "test-topic", 
            null, 
            _mockKafkaFactory.Object);

        // Assert
        Assert.That(storage, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithDefaultTopicName_CreatesInstance()
    {
        // Arrange
        var config = new KafkaConfig
        {
            BootstrapServers = "localhost:9092"
        };

        // Act
        using var storage = new KafkaRateLimiterStateStorage(
            config, 
            logger: _mockLogger.Object, 
            kafkaClientFactory: _mockKafkaFactory.Object);

        // Assert
        Assert.That(storage, Is.Not.Null);
    }

    [Test]
    public void BackendInfo_ReturnsCorrectInformation()
    {
        // Arrange
        var config = new KafkaConfig { BootstrapServers = "localhost:9092" };
        using var storage = new KafkaRateLimiterStateStorage(
            config, 
            "test-topic", 
            _mockLogger.Object, 
            _mockKafkaFactory.Object);

        // Act
        var backendInfo = storage.BackendInfo;

        // Assert
        Assert.That(backendInfo, Is.Not.Null);
        Assert.That(backendInfo.BackendType, Is.EqualTo("Apache Kafka"));
        Assert.That(backendInfo.SupportsDistribution, Is.True);
        Assert.That(backendInfo.SupportsPersistence, Is.True);
        Assert.That(backendInfo.SupportsReplication, Is.True);
        Assert.That(backendInfo.TypicalLatency, Is.EqualTo(TimeSpan.FromMilliseconds(5)));
    }

    #region Configuration Tests

    [Test]
    public void KafkaConfig_HasCorrectDefaults()
    {
        // Act
        var config = new KafkaConfig();

        // Assert
        Assert.That(config.BootstrapServers, Is.EqualTo("localhost:9092"));
        Assert.That(config.Performance, Is.Not.Null);
        Assert.That(config.Security, Is.Null);
    }

    [Test]
    public void KafkaConfig_CanSetBootstrapServers()
    {
        // Act
        var config = new KafkaConfig
        {
            BootstrapServers = "kafka-1:9092,kafka-2:9092,kafka-3:9092"
        };

        // Assert
        Assert.That(config.BootstrapServers, Is.EqualTo("kafka-1:9092,kafka-2:9092,kafka-3:9092"));
    }

    [Test]
    public void KafkaConfig_CanSetSecurity()
    {
        // Act
        var config = new KafkaConfig
        {
            Security = new KafkaSecurityConfig
            {
                SecurityProtocol = "SASL_SSL",
                SaslMechanism = "PLAIN",
                SaslUsername = "testuser",
                SaslPassword = "testpass"
            }
        };

        // Assert
        Assert.That(config.Security, Is.Not.Null);
        Assert.That(config.Security!.SecurityProtocol, Is.EqualTo("SASL_SSL"));
        Assert.That(config.Security.SaslMechanism, Is.EqualTo("PLAIN"));
        Assert.That(config.Security.SaslUsername, Is.EqualTo("testuser"));
        Assert.That(config.Security.SaslPassword, Is.EqualTo("testpass"));
    }

    [Test]
    public void KafkaConfig_CanSetPerformance()
    {
        // Act
        var config = new KafkaConfig
        {
            Performance = new KafkaPerformanceConfig
            {
                ReplicationFactor = 3,
                PartitionCount = 12,
                RetentionTime = TimeSpan.FromDays(7),
                EnableCompaction = true
            }
        };

        // Assert
        Assert.That(config.Performance, Is.Not.Null);
        Assert.That(config.Performance.ReplicationFactor, Is.EqualTo(3));
        Assert.That(config.Performance.PartitionCount, Is.EqualTo(12));
        Assert.That(config.Performance.RetentionTime, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(config.Performance.EnableCompaction, Is.True);
    }

    [Test]
    public void KafkaSecurityConfig_CanSetAllProperties()
    {
        // Act
        var security = new KafkaSecurityConfig
        {
            SecurityProtocol = "SASL_PLAINTEXT",
            SaslMechanism = "SCRAM-SHA-256",
            SaslUsername = "admin",
            SaslPassword = "secret"
        };

        // Assert
        Assert.That(security.SecurityProtocol, Is.EqualTo("SASL_PLAINTEXT"));
        Assert.That(security.SaslMechanism, Is.EqualTo("SCRAM-SHA-256"));
        Assert.That(security.SaslUsername, Is.EqualTo("admin"));
        Assert.That(security.SaslPassword, Is.EqualTo("secret"));
    }

    [Test]
    public void KafkaPerformanceConfig_HasCorrectDefaults()
    {
        // Act
        var performance = new KafkaPerformanceConfig();

        // Assert
        Assert.That(performance.ReplicationFactor, Is.EqualTo(3));
        Assert.That(performance.PartitionCount, Is.EqualTo(12));
        Assert.That(performance.RetentionTime, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(performance.EnableCompaction, Is.True);
    }

    #endregion
}
