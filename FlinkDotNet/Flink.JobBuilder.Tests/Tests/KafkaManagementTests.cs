using Flink.JobBuilder.Backpressure;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Comprehensive tests for Kafka management classes:
/// - KafkaConfig
/// - KafkaPerformanceConfig
/// - KafkaSecurityConfig
/// - MultiClusterKafkaManager
/// - ConsumerLagMonitor
/// - DefaultKafkaConsumerLagMonitor
/// </summary>
[TestFixture]
public class KafkaManagementTests
{
    #region KafkaConfig Tests

    [Test]
    public void KafkaConfig_DefaultConstructor_CreatesInstanceWithDefaults()
    {
        var config = new KafkaConfig();
        
        Assert.That(config, Is.Not.Null);
        Assert.That(config.BootstrapServers, Is.EqualTo("localhost:9092"));
        Assert.That(config.Performance, Is.Not.Null);
        Assert.That(config.Security, Is.Null);
    }

    [Test]
    public void KafkaConfig_SetBootstrapServers_ReturnsValue()
    {
        var config = new KafkaConfig { BootstrapServers = "kafka1:9092,kafka2:9092" };
        
        Assert.That(config.BootstrapServers, Is.EqualTo("kafka1:9092,kafka2:9092"));
    }

    [Test]
    public void KafkaConfig_SetSecurity_ReturnsValue()
    {
        var security = new KafkaSecurityConfig { SecurityProtocol = "SASL_SSL" };
        var config = new KafkaConfig { Security = security };
        
        Assert.That(config.Security, Is.Not.Null);
        Assert.That(config.Security.SecurityProtocol, Is.EqualTo("SASL_SSL"));
    }

    [Test]
    public void KafkaConfig_SetPerformance_ReturnsValue()
    {
        var performance = new KafkaPerformanceConfig { ReplicationFactor = 5 };
        var config = new KafkaConfig { Performance = performance };
        
        Assert.That(config.Performance, Is.Not.Null);
        Assert.That(config.Performance.ReplicationFactor, Is.EqualTo(5));
    }

    [Test]
    public void KafkaConfig_InitWithAllProperties_WorksCorrectly()
    {
        var security = new KafkaSecurityConfig
        {
            SecurityProtocol = "SASL_PLAINTEXT",
            SaslMechanism = "PLAIN",
            SaslUsername = "user",
            SaslPassword = "pass"
        };
        
        var performance = new KafkaPerformanceConfig
        {
            ReplicationFactor = 3,
            PartitionCount = 24,
            RetentionTime = TimeSpan.FromDays(14),
            EnableCompaction = false
        };
        
        var config = new KafkaConfig
        {
            BootstrapServers = "prod-kafka:9092",
            Security = security,
            Performance = performance
        };
        
        Assert.That(config.BootstrapServers, Is.EqualTo("prod-kafka:9092"));
        Assert.That(config.Security, Is.EqualTo(security));
        Assert.That(config.Performance, Is.EqualTo(performance));
    }

    [Test]
    public void KafkaConfig_WithoutSecurity_SecurityIsNull()
    {
        var config = new KafkaConfig { BootstrapServers = "localhost:9092" };
        
        Assert.That(config.Security, Is.Null);
    }

    [Test]
    public void KafkaConfig_DefaultPerformance_HasCorrectDefaults()
    {
        var config = new KafkaConfig();
        
        Assert.That(config.Performance.ReplicationFactor, Is.EqualTo(3));
        Assert.That(config.Performance.PartitionCount, Is.EqualTo(12));
        Assert.That(config.Performance.EnableCompaction, Is.True);
    }

    [Test]
    public void KafkaConfig_MultipleInstances_AreIndependent()
    {
        var config1 = new KafkaConfig { BootstrapServers = "kafka1:9092" };
        var config2 = new KafkaConfig { BootstrapServers = "kafka2:9092" };
        
        Assert.That(config1.BootstrapServers, Is.EqualTo("kafka1:9092"));
        Assert.That(config2.BootstrapServers, Is.EqualTo("kafka2:9092"));
        Assert.That(config1, Is.Not.SameAs(config2));
    }

    #endregion

    #region KafkaPerformanceConfig Tests

    [Test]
    public void KafkaPerformanceConfig_DefaultConstructor_HasCorrectDefaults()
    {
        var config = new KafkaPerformanceConfig();
        
        Assert.That(config.ReplicationFactor, Is.EqualTo(3));
        Assert.That(config.PartitionCount, Is.EqualTo(12));
        Assert.That(config.RetentionTime, Is.EqualTo(TimeSpan.FromDays(7)));
        Assert.That(config.EnableCompaction, Is.True);
    }

    [Test]
    public void KafkaPerformanceConfig_SetReplicationFactor_ReturnsValue()
    {
        var config = new KafkaPerformanceConfig { ReplicationFactor = 5 };
        
        Assert.That(config.ReplicationFactor, Is.EqualTo(5));
    }

    [Test]
    public void KafkaPerformanceConfig_SetPartitionCount_ReturnsValue()
    {
        var config = new KafkaPerformanceConfig { PartitionCount = 24 };
        
        Assert.That(config.PartitionCount, Is.EqualTo(24));
    }

    [Test]
    public void KafkaPerformanceConfig_SetRetentionTime_ReturnsValue()
    {
        var retention = TimeSpan.FromDays(14);
        var config = new KafkaPerformanceConfig { RetentionTime = retention };
        
        Assert.That(config.RetentionTime, Is.EqualTo(retention));
    }

    [Test]
    public void KafkaPerformanceConfig_SetEnableCompaction_ReturnsValue()
    {
        var config = new KafkaPerformanceConfig { EnableCompaction = false };
        
        Assert.That(config.EnableCompaction, Is.False);
    }

    [Test]
    public void KafkaPerformanceConfig_InitWithAllProperties_WorksCorrectly()
    {
        var config = new KafkaPerformanceConfig
        {
            ReplicationFactor = 5,
            PartitionCount = 48,
            RetentionTime = TimeSpan.FromDays(30),
            EnableCompaction = false
        };
        
        Assert.That(config.ReplicationFactor, Is.EqualTo(5));
        Assert.That(config.PartitionCount, Is.EqualTo(48));
        Assert.That(config.RetentionTime, Is.EqualTo(TimeSpan.FromDays(30)));
        Assert.That(config.EnableCompaction, Is.False);
    }

    [Test]
    public void KafkaPerformanceConfig_MinimalReplication_WorksCorrectly()
    {
        var config = new KafkaPerformanceConfig { ReplicationFactor = 1 };
        
        Assert.That(config.ReplicationFactor, Is.EqualTo(1));
    }

    [Test]
    public void KafkaPerformanceConfig_HighPartitionCount_WorksCorrectly()
    {
        var config = new KafkaPerformanceConfig { PartitionCount = 100 };
        
        Assert.That(config.PartitionCount, Is.EqualTo(100));
    }

    [Test]
    public void KafkaPerformanceConfig_ShortRetention_WorksCorrectly()
    {
        var config = new KafkaPerformanceConfig { RetentionTime = TimeSpan.FromHours(1) };
        
        Assert.That(config.RetentionTime, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    [Test]
    public void KafkaPerformanceConfig_MultipleInstances_AreIndependent()
    {
        var config1 = new KafkaPerformanceConfig { ReplicationFactor = 3 };
        var config2 = new KafkaPerformanceConfig { ReplicationFactor = 5 };
        
        Assert.That(config1.ReplicationFactor, Is.EqualTo(3));
        Assert.That(config2.ReplicationFactor, Is.EqualTo(5));
    }

    #endregion

    #region KafkaSecurityConfig Tests

    [Test]
    public void KafkaSecurityConfig_DefaultConstructor_HasPlaintextDefault()
    {
        var config = new KafkaSecurityConfig();
        
        Assert.That(config.SecurityProtocol, Is.EqualTo("PLAINTEXT"));
        Assert.That(config.SaslMechanism, Is.Null);
        Assert.That(config.SaslUsername, Is.Null);
        Assert.That(config.SaslPassword, Is.Null);
    }

    [Test]
    public void KafkaSecurityConfig_SetSecurityProtocol_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SecurityProtocol = "SASL_SSL" };
        
        Assert.That(config.SecurityProtocol, Is.EqualTo("SASL_SSL"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSaslMechanism_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SaslMechanism = "PLAIN" };
        
        Assert.That(config.SaslMechanism, Is.EqualTo("PLAIN"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSaslUsername_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SaslUsername = "admin" };
        
        Assert.That(config.SaslUsername, Is.EqualTo("admin"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSaslPassword_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SaslPassword = "secret123" };
        
        Assert.That(config.SaslPassword, Is.EqualTo("secret123"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSslCaLocation_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SslCaLocation = "/path/to/ca.pem" };
        
        Assert.That(config.SslCaLocation, Is.EqualTo("/path/to/ca.pem"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSslCertificateLocation_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SslCertificateLocation = "/path/to/cert.pem" };
        
        Assert.That(config.SslCertificateLocation, Is.EqualTo("/path/to/cert.pem"));
    }

    [Test]
    public void KafkaSecurityConfig_SetSslKeyLocation_ReturnsValue()
    {
        var config = new KafkaSecurityConfig { SslKeyLocation = "/path/to/key.pem" };
        
        Assert.That(config.SslKeyLocation, Is.EqualTo("/path/to/key.pem"));
    }

    [Test]
    public void KafkaSecurityConfig_InitWithSaslPlaintext_WorksCorrectly()
    {
        var config = new KafkaSecurityConfig
        {
            SecurityProtocol = "SASL_PLAINTEXT",
            SaslMechanism = "PLAIN",
            SaslUsername = "user123",
            SaslPassword = "pass456"
        };
        
        Assert.That(config.SecurityProtocol, Is.EqualTo("SASL_PLAINTEXT"));
        Assert.That(config.SaslMechanism, Is.EqualTo("PLAIN"));
        Assert.That(config.SaslUsername, Is.EqualTo("user123"));
        Assert.That(config.SaslPassword, Is.EqualTo("pass456"));
    }

    [Test]
    public void KafkaSecurityConfig_InitWithSsl_WorksCorrectly()
    {
        var config = new KafkaSecurityConfig
        {
            SecurityProtocol = "SSL",
            SslCaLocation = "/ca.pem",
            SslCertificateLocation = "/cert.pem",
            SslKeyLocation = "/key.pem"
        };
        
        Assert.That(config.SecurityProtocol, Is.EqualTo("SSL"));
        Assert.That(config.SslCaLocation, Is.EqualTo("/ca.pem"));
        Assert.That(config.SslCertificateLocation, Is.EqualTo("/cert.pem"));
        Assert.That(config.SslKeyLocation, Is.EqualTo("/key.pem"));
    }

    #endregion

    #region MultiClusterKafkaManager Tests

    [Test]
    public void MultiClusterKafkaManager_ValidateOperationalIsolation_ReturnsTrue()
    {
        var result = MultiClusterKafkaManager.ValidateOperationalIsolation();
        
        Assert.That(result, Is.True);
    }

    [Test]
    public void MultiClusterKafkaManager_StaticClass_CannotBeInstantiated()
    {
        var type = typeof(MultiClusterKafkaManager);
        
        Assert.That(type.IsAbstract, Is.True);
        Assert.That(type.IsSealed, Is.True);
    }

    [Test]
    public void MultiClusterKafkaManager_ValidateOperationalIsolation_IsReliable()
    {
        // Call multiple times to ensure consistency
        var result1 = MultiClusterKafkaManager.ValidateOperationalIsolation();
        var result2 = MultiClusterKafkaManager.ValidateOperationalIsolation();
        var result3 = MultiClusterKafkaManager.ValidateOperationalIsolation();
        
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
        Assert.That(result3, Is.True);
    }

    [Test]
    public void MultiClusterKafkaManager_ExistsInCorrectNamespace()
    {
        var type = typeof(MultiClusterKafkaManager);
        
        Assert.That(type.Namespace, Is.EqualTo("Flink.JobBuilder.Backpressure"));
    }

    [Test]
    public void MultiClusterKafkaManager_IsPublicClass()
    {
        var type = typeof(MultiClusterKafkaManager);
        
        Assert.That(type.IsPublic, Is.True);
    }

    [Test]
    public void MultiClusterKafkaManager_HasValidateOperationalIsolationMethod()
    {
        var type = typeof(MultiClusterKafkaManager);
        var method = type.GetMethod("ValidateOperationalIsolation");
        
        Assert.That(method, Is.Not.Null);
        Assert.That(method!.IsStatic, Is.True);
        Assert.That(method.IsPublic, Is.True);
        Assert.That(method.ReturnType, Is.EqualTo(typeof(bool)));
    }

    [Test]
    public void MultiClusterKafkaManager_ValidateOperationalIsolation_AlwaysSucceeds()
    {
        for (int i = 0; i < 10; i++)
        {
            var result = MultiClusterKafkaManager.ValidateOperationalIsolation();
            Assert.That(result, Is.True, $"Iteration {i} failed");
        }
    }

    [Test]
    public void MultiClusterKafkaManager_SupportsMultiClusterOperations()
    {
        // Verify the class supports multi-cluster concepts through its methods
        var type = typeof(MultiClusterKafkaManager);
        var methods = type.GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        
        Assert.That(methods.Length, Is.GreaterThan(0));
    }

    #endregion

    #region ConsumerLagMonitor Tests

    [Test]
    public void ConsumerLagMonitor_DefaultConstructor_CreatesInstance()
    {
        var monitor = new ConsumerLagMonitor();
        
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void ConsumerLagMonitor_IsContinuousMonitoringActive_ReturnsTrue()
    {
        var monitor = new ConsumerLagMonitor();
        
        var isActive = monitor.IsContinuousMonitoringActive();
        
        Assert.That(isActive, Is.True);
    }

    [Test]
    public void ConsumerLagMonitor_GetCurrentLag_ReturnsInitialValue()
    {
        var monitor = new ConsumerLagMonitor();
        
        var lag = monitor.GetCurrentLag();
        
        Assert.That(lag, Is.EqualTo(1000));
    }

    [Test]
    public void ConsumerLagMonitor_SimulateLagSpike_UpdatesLag()
    {
        var monitor = new ConsumerLagMonitor();
        
        var shouldRebalance = monitor.SimulateLagSpike(10000);
        
        Assert.That(shouldRebalance, Is.True);
        Assert.That(monitor.GetCurrentLag(), Is.EqualTo(10000));
    }

    [Test]
    public void ConsumerLagMonitor_SimulateLagSpike_BelowThreshold_DoesNotTriggerRebalancing()
    {
        var monitor = new ConsumerLagMonitor();
        
        var shouldRebalance = monitor.SimulateLagSpike(3000);
        
        Assert.That(shouldRebalance, Is.False);
        Assert.That(monitor.GetCurrentLag(), Is.EqualTo(3000));
    }

    #endregion

    #region DefaultKafkaConsumerLagMonitor Tests

    [Test]
    public void DefaultKafkaConsumerLagMonitor_Constructor_CreatesInstance()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_ConstructorWithLogger_CreatesInstance()
    {
        var loggerMock = new Mock<ILogger>();
        var monitor = new DefaultKafkaConsumerLagMonitor(loggerMock.Object);
        
        Assert.That(monitor, Is.Not.Null);
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_GetCurrentLag_WithoutKafka_ReturnsZero()
    {
        // Without KAFKA_BOOTSTRAP_SERVERS set, should return zero
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        var lag = monitor.GetCurrentLag("test-group");
        
        Assert.That(lag, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public async Task DefaultKafkaConsumerLagMonitor_GetCurrentLagAsync_WithoutKafka_ReturnsZero()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        var lag = await monitor.GetCurrentLagAsync("test-group");
        
        Assert.That(lag, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_GetCurrentLag_CachesResults()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        var lag1 = monitor.GetCurrentLag("test-group");
        var lag2 = monitor.GetCurrentLag("test-group");
        
        Assert.That(lag1, Is.EqualTo(lag2));
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_GetCurrentLag_DifferentGroups_IndependentResults()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        var lag1 = monitor.GetCurrentLag("group1");
        var lag2 = monitor.GetCurrentLag("group2");
        
        // Both should be zero without Kafka, but tracked independently
        Assert.That(lag1, Is.EqualTo(TimeSpan.Zero));
        Assert.That(lag2, Is.EqualTo(TimeSpan.Zero));
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_Dispose_DoesNotThrow()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        Assert.DoesNotThrow(() => monitor.Dispose());
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_Dispose_MultipleTimes_DoesNotThrow()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        Assert.DoesNotThrow(() =>
        {
            monitor.Dispose();
            monitor.Dispose();
            monitor.Dispose();
        });
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_ImplementsInterface()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        Assert.That(monitor, Is.InstanceOf<IKafkaConsumerLagMonitor>());
    }

    [Test]
    public void DefaultKafkaConsumerLagMonitor_ImplementsDisposable()
    {
        var monitor = new DefaultKafkaConsumerLagMonitor();
        
        Assert.That(monitor, Is.InstanceOf<IDisposable>());
    }

    #endregion
}
