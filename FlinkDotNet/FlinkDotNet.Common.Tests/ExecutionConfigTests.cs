namespace FlinkDotNet.Common.Tests;

[TestFixture]
public class ExecutionConfigTests
{
    [Test]
    public void DefaultConstructor_CreatesInstanceWithDefaults()
    {
        // Act
        var config = new ExecutionConfig();

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.Parallelism, Is.EqualTo(-1));
        Assert.That(config.MaxParallelism, Is.EqualTo(-1));
        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(200L));
        Assert.That(config.ObjectReuseEnabled, Is.False);
        Assert.That(config.ClosureCleanerEnabled, Is.True);
        Assert.That(config.RestartStrategy, Is.EqualTo("exponential-delay"));
        Assert.That(config.SlotSharingEnabled, Is.True);
        Assert.That(config.AdaptiveSchedulerEnabled, Is.False);
        Assert.That(config.ReactiveModeEnabled, Is.False);
    }

    [Test]
    public void ConstructorWithConfiguration_UsesProvidedConfiguration()
    {
        // Arrange
        var underlyingConfig = new Configuration();
        underlyingConfig.SetString("test-key", "test-value");

        // Act
        var config = new ExecutionConfig(underlyingConfig);

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.GetConfiguration(), Is.SameAs(underlyingConfig));
        Assert.That(config.GetProperty("test-key"), Is.EqualTo("test-value"));
    }

    [Test]
    public void SetParallelism_UpdatesValueAndReturnsInstance()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetParallelism(8);

        // Assert
        Assert.That(result, Is.SameAs(config)); // Method chaining
        Assert.That(config.Parallelism, Is.EqualTo(8));
    }

    [Test]
    public void SetMaxParallelism_UpdatesValueAndReturnsInstance()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetMaxParallelism(16);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.MaxParallelism, Is.EqualTo(16));
    }

    [Test]
    public void SetAutoWatermarkInterval_UpdatesValueAndReturnsInstance()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetAutoWatermarkInterval(5000L);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(5000L));
    }

    [Test]
    public void EnableObjectReuse_WithTrue_EnablesObjectReuse()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableObjectReuse(true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ObjectReuseEnabled, Is.True);
    }

    [Test]
    public void EnableObjectReuse_WithFalse_DisablesObjectReuse()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableObjectReuse(true); // First enable it

        // Act
        var result = config.EnableObjectReuse(false);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void EnableObjectReuse_WithoutParameter_EnablesObjectReuse()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableObjectReuse();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ObjectReuseEnabled, Is.True);
    }

    [Test]
    public void DisableObjectReuse_DisablesObjectReuse()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableObjectReuse(true);

        // Act
        var result = config.DisableObjectReuse();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void EnableClosureCleaner_WithTrue_EnablesClosureCleaner()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableClosureCleaner(false); // First disable it

        // Act
        var result = config.EnableClosureCleaner(true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void EnableClosureCleaner_WithFalse_DisablesClosureCleaner()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableClosureCleaner(false);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    [Test]
    public void EnableClosureCleaner_WithoutParameter_EnablesClosureCleaner()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableClosureCleaner(false);

        // Act
        var result = config.EnableClosureCleaner();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void DisableClosureCleaner_DisablesClosureCleaner()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.DisableClosureCleaner();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    [Test]
    public void SetRestartStrategy_UpdatesStrategy()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetRestartStrategy("fixed-delay");

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.RestartStrategy, Is.EqualTo("fixed-delay"));
    }

    [Test]
    public void SetRestartStrategy_FailureRateStrategy_UpdatesCorrectly()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetRestartStrategy("failure-rate");

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.RestartStrategy, Is.EqualTo("failure-rate"));
    }

    [Test]
    public void EnableSlotSharing_WithTrue_EnablesSlotSharing()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableSlotSharing(false);

        // Act
        var result = config.EnableSlotSharing(true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void EnableSlotSharing_WithFalse_DisablesSlotSharing()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableSlotSharing(false);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.SlotSharingEnabled, Is.False);
    }

    [Test]
    public void EnableSlotSharing_WithoutParameter_EnablesSlotSharing()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableSlotSharing(false);

        // Act
        var result = config.EnableSlotSharing();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void EnableAdaptiveScheduler_WithTrue_EnablesAdaptiveScheduler()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableAdaptiveScheduler(true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
    }

    [Test]
    public void EnableAdaptiveScheduler_WithFalse_DisablesAdaptiveScheduler()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableAdaptiveScheduler(true);

        // Act
        var result = config.EnableAdaptiveScheduler(false);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.AdaptiveSchedulerEnabled, Is.False);
    }

    [Test]
    public void EnableAdaptiveScheduler_WithoutParameter_EnablesAdaptiveScheduler()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableAdaptiveScheduler();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
    }

    [Test]
    public void EnableReactiveMode_WithTrue_EnablesReactiveMode()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableReactiveMode(true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ReactiveModeEnabled, Is.True);
    }

    [Test]
    public void EnableReactiveMode_WithFalse_DisablesReactiveMode()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.EnableReactiveMode(true);

        // Act
        var result = config.EnableReactiveMode(false);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ReactiveModeEnabled, Is.False);
    }

    [Test]
    public void EnableReactiveMode_WithoutParameter_EnablesReactiveMode()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.EnableReactiveMode();

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.ReactiveModeEnabled, Is.True);
    }

    [Test]
    public void GetConfiguration_ReturnsUnderlyingConfiguration()
    {
        // Arrange
        var underlyingConfig = new Configuration();
        underlyingConfig.SetString("key1", "value1");
        var config = new ExecutionConfig(underlyingConfig);

        // Act
        var retrievedConfig = config.GetConfiguration();

        // Assert
        Assert.That(retrievedConfig, Is.SameAs(underlyingConfig));
        Assert.That(retrievedConfig.GetString("key1"), Is.EqualTo("value1"));
    }

    [Test]
    public void SetProperty_AddsPropertyToUnderlyingConfiguration()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var result = config.SetProperty("custom-key", "custom-value");

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.GetProperty("custom-key"), Is.EqualTo("custom-value"));
    }

    [Test]
    public void SetProperty_WithObjectValue_ConvertsToString()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        config.SetProperty("number-key", 42);

        // Assert
        Assert.That(config.GetProperty("number-key"), Is.EqualTo("42"));
    }

    [Test]
    public void GetProperty_ExistingKey_ReturnsValue()
    {
        // Arrange
        var config = new ExecutionConfig();
        config.SetProperty("test-key", "test-value");

        // Act
        var value = config.GetProperty("test-key");

        // Assert
        Assert.That(value, Is.EqualTo("test-value"));
    }

    [Test]
    public void GetProperty_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var value = config.GetProperty("non-existent", "default-value");

        // Assert
        Assert.That(value, Is.EqualTo("default-value"));
    }

    [Test]
    public void GetProperty_NonExistentKeyNoDefault_ReturnsEmptyString()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        var value = config.GetProperty("non-existent");

        // Assert
        Assert.That(value, Is.EqualTo(string.Empty));
    }

    [Test]
    public void MethodChaining_ConfigureMultipleSettings_WorksCorrectly()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        config.SetParallelism(4)
              .SetMaxParallelism(128)
              .SetAutoWatermarkInterval(1000L)
              .EnableObjectReuse()
              .EnableAdaptiveScheduler()
              .EnableReactiveMode()
              .SetRestartStrategy("failure-rate")
              .SetProperty("custom.key", "custom.value");

        // Assert
        Assert.That(config.Parallelism, Is.EqualTo(4));
        Assert.That(config.MaxParallelism, Is.EqualTo(128));
        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(1000L));
        Assert.That(config.ObjectReuseEnabled, Is.True);
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
        Assert.That(config.ReactiveModeEnabled, Is.True);
        Assert.That(config.RestartStrategy, Is.EqualTo("failure-rate"));
        Assert.That(config.GetProperty("custom.key"), Is.EqualTo("custom.value"));
    }

    [Test]
    public void PropertySetters_DirectAssignment_WorkCorrectly()
    {
        // Arrange
        var config = new ExecutionConfig();

        // Act
        config.Parallelism = 16;
        config.MaxParallelism = 256;
        config.AutoWatermarkInterval = 3000L;
        config.ObjectReuseEnabled = true;
        config.ClosureCleanerEnabled = false;
        config.RestartStrategy = "no-restart";
        config.SlotSharingEnabled = false;
        config.AdaptiveSchedulerEnabled = true;
        config.ReactiveModeEnabled = true;

        // Assert
        Assert.That(config.Parallelism, Is.EqualTo(16));
        Assert.That(config.MaxParallelism, Is.EqualTo(256));
        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(3000L));
        Assert.That(config.ObjectReuseEnabled, Is.True);
        Assert.That(config.ClosureCleanerEnabled, Is.False);
        Assert.That(config.RestartStrategy, Is.EqualTo("no-restart"));
        Assert.That(config.SlotSharingEnabled, Is.False);
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
        Assert.That(config.ReactiveModeEnabled, Is.True);
    }
}