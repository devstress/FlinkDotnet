using FlinkDotNet.Common;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Tests for FlinkDotNet.Common.ExecutionConfig class
/// </summary>
[TestFixture]
public class ExecutionConfigTests
{
    #region Constructor Tests

    [Test]
    public void ExecutionConfig_DefaultConstructor_CreatesInstanceWithDefaults()
    {
        var config = new ExecutionConfig();

        Assert.That(config, Is.Not.Null);
        Assert.That(config.Parallelism, Is.EqualTo(-1));
        Assert.That(config.MaxParallelism, Is.EqualTo(-1));
    }

    [Test]
    public void ExecutionConfig_ConstructorWithConfiguration_UsesProvidedConfiguration()
    {
        var configuration = new Configuration();
        configuration.SetString("test.key", "test.value");

        var config = new ExecutionConfig(configuration);

        Assert.That(config.GetConfiguration().GetString("test.key"), Is.EqualTo("test.value"));
    }

    #endregion

    #region Parallelism Tests

    [Test]
    public void SetParallelism_WithValidValue_SetsParallelism()
    {
        var config = new ExecutionConfig();

        config.SetParallelism(4);

        Assert.That(config.Parallelism, Is.EqualTo(4));
    }

    [Test]
    public void SetParallelism_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.SetParallelism(8);

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region MaxParallelism Tests

    [Test]
    public void SetMaxParallelism_WithValidValue_SetsMaxParallelism()
    {
        var config = new ExecutionConfig();

        config.SetMaxParallelism(128);

        Assert.That(config.MaxParallelism, Is.EqualTo(128));
    }

    [Test]
    public void SetMaxParallelism_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.SetMaxParallelism(256);

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region AutoWatermarkInterval Tests

    [Test]
    public void AutoWatermarkInterval_DefaultValue_Is200()
    {
        var config = new ExecutionConfig();

        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(200L));
    }

    [Test]
    public void SetAutoWatermarkInterval_WithValidValue_SetsInterval()
    {
        var config = new ExecutionConfig();

        config.SetAutoWatermarkInterval(500L);

        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(500L));
    }

    [Test]
    public void SetAutoWatermarkInterval_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.SetAutoWatermarkInterval(1000L);

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region ObjectReuse Tests

    [Test]
    public void ObjectReuseEnabled_DefaultValue_IsFalse()
    {
        var config = new ExecutionConfig();

        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void EnableObjectReuse_SetsPropertyToTrue()
    {
        var config = new ExecutionConfig();

        config.EnableObjectReuse();

        Assert.That(config.ObjectReuseEnabled, Is.True);
    }

    [Test]
    public void EnableObjectReuse_WithFalse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();

        config.EnableObjectReuse(false);

        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void EnableObjectReuse_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.EnableObjectReuse();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void DisableObjectReuse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();
        config.EnableObjectReuse();

        config.DisableObjectReuse();

        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    #endregion

    #region ClosureCleaner Tests

    [Test]
    public void ClosureCleanerEnabled_DefaultValue_IsTrue()
    {
        var config = new ExecutionConfig();

        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void EnableClosureCleaner_SetsPropertyToTrue()
    {
        var config = new ExecutionConfig();
        config.DisableClosureCleaner();

        config.EnableClosureCleaner();

        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void EnableClosureCleaner_WithFalse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();

        config.EnableClosureCleaner(false);

        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    [Test]
    public void EnableClosureCleaner_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.EnableClosureCleaner();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void DisableClosureCleaner_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();

        config.DisableClosureCleaner();

        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    #endregion

    #region RestartStrategy Tests

    [Test]
    public void RestartStrategy_DefaultValue_IsExponentialDelay()
    {
        var config = new ExecutionConfig();

        Assert.That(config.RestartStrategy, Is.EqualTo("exponential-delay"));
    }

    [Test]
    public void SetRestartStrategy_WithValidValue_SetsStrategy()
    {
        var config = new ExecutionConfig();

        config.SetRestartStrategy("fixed-delay");

        Assert.That(config.RestartStrategy, Is.EqualTo("fixed-delay"));
    }

    [Test]
    public void SetRestartStrategy_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.SetRestartStrategy("failure-rate");

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region SlotSharing Tests

    [Test]
    public void SlotSharingEnabled_DefaultValue_IsTrue()
    {
        var config = new ExecutionConfig();

        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void EnableSlotSharing_SetsPropertyToTrue()
    {
        var config = new ExecutionConfig();

        config.EnableSlotSharing();

        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void EnableSlotSharing_WithFalse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();

        config.EnableSlotSharing(false);

        Assert.That(config.SlotSharingEnabled, Is.False);
    }

    [Test]
    public void EnableSlotSharing_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.EnableSlotSharing();

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region AdaptiveScheduler Tests

    [Test]
    public void AdaptiveSchedulerEnabled_DefaultValue_IsFalse()
    {
        var config = new ExecutionConfig();

        Assert.That(config.AdaptiveSchedulerEnabled, Is.False);
    }

    [Test]
    public void EnableAdaptiveScheduler_SetsPropertyToTrue()
    {
        var config = new ExecutionConfig();

        config.EnableAdaptiveScheduler();

        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
    }

    [Test]
    public void EnableAdaptiveScheduler_WithFalse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();
        config.EnableAdaptiveScheduler();

        config.EnableAdaptiveScheduler(false);

        Assert.That(config.AdaptiveSchedulerEnabled, Is.False);
    }

    [Test]
    public void EnableAdaptiveScheduler_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.EnableAdaptiveScheduler();

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region ReactiveMode Tests

    [Test]
    public void ReactiveModeEnabled_DefaultValue_IsFalse()
    {
        var config = new ExecutionConfig();

        Assert.That(config.ReactiveModeEnabled, Is.False);
    }

    [Test]
    public void EnableReactiveMode_SetsPropertyToTrue()
    {
        var config = new ExecutionConfig();

        config.EnableReactiveMode();

        Assert.That(config.ReactiveModeEnabled, Is.True);
    }

    [Test]
    public void EnableReactiveMode_WithFalse_SetsPropertyToFalse()
    {
        var config = new ExecutionConfig();
        config.EnableReactiveMode();

        config.EnableReactiveMode(false);

        Assert.That(config.ReactiveModeEnabled, Is.False);
    }

    [Test]
    public void EnableReactiveMode_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.EnableReactiveMode();

        Assert.That(result, Is.SameAs(config));
    }

    #endregion

    #region GetConfiguration Tests

    [Test]
    public void GetConfiguration_ReturnsUnderlyingConfiguration()
    {
        var configuration = new Configuration();
        configuration.SetString("custom.key", "custom.value");
        var config = new ExecutionConfig(configuration);

        var result = config.GetConfiguration();

        Assert.That(result, Is.SameAs(configuration));
        Assert.That(result.GetString("custom.key"), Is.EqualTo("custom.value"));
    }

    #endregion

    #region SetProperty/GetProperty Tests

    [Test]
    public void SetProperty_WithValidKeyValue_StoresProperty()
    {
        var config = new ExecutionConfig();

        config.SetProperty("custom.property", "custom.value");

        Assert.That(config.GetProperty("custom.property"), Is.EqualTo("custom.value"));
    }

    [Test]
    public void SetProperty_ReturnsExecutionConfig_ForMethodChaining()
    {
        var config = new ExecutionConfig();

        var result = config.SetProperty("key", "value");

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void GetProperty_WithNonExistentKey_ReturnsDefaultValue()
    {
        var config = new ExecutionConfig();

        var result = config.GetProperty("nonexistent", "default");

        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void GetProperty_WithNonExistentKeyAndNoDefault_ReturnsEmptyString()
    {
        var config = new ExecutionConfig();

        var result = config.GetProperty("nonexistent");

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Method Chaining Tests

    [Test]
    public void ExecutionConfig_SupportsMethodChaining()
    {
        var config = new ExecutionConfig()
            .SetParallelism(4)
            .SetMaxParallelism(128)
            .SetAutoWatermarkInterval(300L)
            .EnableObjectReuse()
            .EnableClosureCleaner()
            .SetRestartStrategy("fixed-delay")
            .EnableSlotSharing()
            .EnableAdaptiveScheduler()
            .EnableReactiveMode()
            .SetProperty("custom.key", "custom.value");

        Assert.That(config.Parallelism, Is.EqualTo(4));
        Assert.That(config.MaxParallelism, Is.EqualTo(128));
        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(300L));
        Assert.That(config.ObjectReuseEnabled, Is.True);
        Assert.That(config.ClosureCleanerEnabled, Is.True);
        Assert.That(config.RestartStrategy, Is.EqualTo("fixed-delay"));
        Assert.That(config.SlotSharingEnabled, Is.True);
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
        Assert.That(config.ReactiveModeEnabled, Is.True);
        Assert.That(config.GetProperty("custom.key"), Is.EqualTo("custom.value"));
    }

    #endregion
}