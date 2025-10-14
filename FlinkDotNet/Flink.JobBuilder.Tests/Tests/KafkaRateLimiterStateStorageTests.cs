using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Unit tests for Kafka configuration classes.
/// Note: Tests that require actual Kafka connections have been removed per unit test standards.
/// These tests focus on configuration and validation logic only.
/// </summary>
[TestFixture]
public class KafkaRateLimiterStateStorageTests
{
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
}
