using FlinkDotNet.Common;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class FlinkDotNetCommonTests
{
    #region Configuration Tests

    [Test]
    public void Configuration_DefaultConstructor_CreatesEmptyConfiguration()
    {
        var config = new Configuration();
        Assert.That(config, Is.Not.Null);
        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void Configuration_DictionaryConstructor_CopiesValues()
    {
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };
        var config = new Configuration(dict);

        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(config.GetInteger("key2"), Is.EqualTo(42));
    }

    [Test]
    public void Configuration_SetString_StoresAndRetrievesValue()
    {
        var config = new Configuration();
        config.SetString("test.key", "test.value");

        Assert.That(config.GetString("test.key"), Is.EqualTo("test.value"));
    }

    [Test]
    public void Configuration_SetString_ReturnsConfigurationForChaining()
    {
        var config = new Configuration();
        var result = config.SetString("key", "value");

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void Configuration_SetInteger_StoresAndRetrievesValue()
    {
        var config = new Configuration();
        config.SetInteger("parallelism", 8);

        Assert.That(config.GetInteger("parallelism"), Is.EqualTo(8));
    }

    [Test]
    public void Configuration_SetInteger_ReturnsConfigurationForChaining()
    {
        var config = new Configuration();
        var result = config.SetInteger("key", 42);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void Configuration_SetBoolean_StoresAndRetrievesValue()
    {
        var config = new Configuration();
        config.SetBoolean("enabled", true);

        Assert.That(config.GetBoolean("enabled"), Is.True);
    }

    [Test]
    public void Configuration_SetBoolean_ReturnsConfigurationForChaining()
    {
        var config = new Configuration();
        var result = config.SetBoolean("key", true);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void Configuration_SetLong_StoresAndRetrievesValue()
    {
        var config = new Configuration();
        config.SetLong("checkpoint.interval", 60000L);

        Assert.That(config.GetLong("checkpoint.interval"), Is.EqualTo(60000L));
    }

    [Test]
    public void Configuration_SetLong_ReturnsConfigurationForChaining()
    {
        var config = new Configuration();
        var result = config.SetLong("key", 123456L);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void Configuration_GetString_ReturnsDefaultValueWhenKeyNotFound()
    {
        var config = new Configuration();

        Assert.That(config.GetString("nonexistent", "default"), Is.EqualTo("default"));
    }

    [Test]
    public void Configuration_GetString_ReturnsEmptyStringWhenNoDefault()
    {
        var config = new Configuration();

        Assert.That(config.GetString("nonexistent"), Is.EqualTo(string.Empty));
    }

    [Test]
    public void Configuration_GetInteger_ReturnsDefaultValueWhenKeyNotFound()
    {
        var config = new Configuration();

        Assert.That(config.GetInteger("nonexistent", 42), Is.EqualTo(42));
    }

    [Test]
    public void Configuration_GetInteger_ParsesStringValue()
    {
        var config = new Configuration();
        config.SetString("number", "123");

        Assert.That(config.GetInteger("number"), Is.EqualTo(123));
    }

    [Test]
    public void Configuration_GetBoolean_ReturnsDefaultValueWhenKeyNotFound()
    {
        var config = new Configuration();

        Assert.That(config.GetBoolean("nonexistent", true), Is.True);
    }

    [Test]
    public void Configuration_GetBoolean_ParsesStringValue()
    {
        var config = new Configuration();
        config.SetString("flag", "true");

        Assert.That(config.GetBoolean("flag"), Is.True);
    }

    [Test]
    public void Configuration_GetLong_ReturnsDefaultValueWhenKeyNotFound()
    {
        var config = new Configuration();

        Assert.That(config.GetLong("nonexistent", 999L), Is.EqualTo(999L));
    }

    [Test]
    public void Configuration_GetLong_ParsesStringValue()
    {
        var config = new Configuration();
        config.SetString("timeout", "30000");

        Assert.That(config.GetLong("timeout"), Is.EqualTo(30000L));
    }

    [Test]
    public void Configuration_ContainsKey_ReturnsTrueForExistingKey()
    {
        var config = new Configuration();
        config.SetString("key", "value");

        Assert.That(config.ContainsKey("key"), Is.True);
    }

    [Test]
    public void Configuration_ContainsKey_ReturnsFalseForNonexistentKey()
    {
        var config = new Configuration();

        Assert.That(config.ContainsKey("nonexistent"), Is.False);
    }

    [Test]
    public void Configuration_RemoveKey_RemovesExistingKey()
    {
        var config = new Configuration();
        config.SetString("key", "value");

        var removed = config.RemoveKey("key");

        Assert.That(removed, Is.True);
        Assert.That(config.ContainsKey("key"), Is.False);
    }

    [Test]
    public void Configuration_RemoveKey_ReturnsFalseForNonexistentKey()
    {
        var config = new Configuration();

        var removed = config.RemoveKey("nonexistent");

        Assert.That(removed, Is.False);
    }

    [Test]
    public void Configuration_GetKeys_ReturnsAllKeys()
    {
        var config = new Configuration();
        config.SetString("key1", "value1");
        config.SetInteger("key2", 42);
        config.SetBoolean("key3", true);

        var keys = config.GetKeys().ToList();

        Assert.That(keys, Has.Count.EqualTo(3));
        Assert.That(keys, Contains.Item("key1"));
        Assert.That(keys, Contains.Item("key2"));
        Assert.That(keys, Contains.Item("key3"));
    }

    [Test]
    public void Configuration_Clone_CreatesIndependentCopy()
    {
        var original = new Configuration();
        original.SetString("key", "value");

        var clone = original.Clone();
        clone.SetString("key", "modified");
        clone.SetString("newkey", "newvalue");

        Assert.That(original.GetString("key"), Is.EqualTo("value"));
        Assert.That(original.ContainsKey("newkey"), Is.False);
        Assert.That(clone.GetString("key"), Is.EqualTo("modified"));
        Assert.That(clone.ContainsKey("newkey"), Is.True);
    }

    [Test]
    public void Configuration_AddAll_MergesConfigurations()
    {
        var config1 = new Configuration();
        config1.SetString("key1", "value1");

        var config2 = new Configuration();
        config2.SetString("key2", "value2");
        config2.SetInteger("key3", 42);

        config1.AddAll(config2);

        Assert.That(config1.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(config1.GetString("key2"), Is.EqualTo("value2"));
        Assert.That(config1.GetInteger("key3"), Is.EqualTo(42));
    }

    [Test]
    public void Configuration_AddAll_OverwritesExistingKeys()
    {
        var config1 = new Configuration();
        config1.SetString("key", "original");

        var config2 = new Configuration();
        config2.SetString("key", "overwritten");

        config1.AddAll(config2);

        Assert.That(config1.GetString("key"), Is.EqualTo("overwritten"));
    }

    [Test]
    public void Configuration_AddAll_ReturnsConfigurationForChaining()
    {
        var config1 = new Configuration();
        var config2 = new Configuration();

        var result = config1.AddAll(config2);

        Assert.That(result, Is.SameAs(config1));
    }

    [Test]
    public void Configuration_ToMap_ReturnsDictionaryWithAllValues()
    {
        var config = new Configuration();
        config.SetString("key1", "value1");
        config.SetInteger("key2", 42);

        var map = config.ToMap();

        Assert.That(map, Has.Count.EqualTo(2));
        Assert.That(map["key1"], Is.EqualTo("value1"));
        Assert.That(map["key2"], Is.EqualTo(42));
    }

    [Test]
    public void Configuration_ParseListValue_ParsesCommaSeparatedString()
    {
        var result = Configuration.ParseListValue("item1,item2,item3");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("item1"));
        Assert.That(result[1], Is.EqualTo("item2"));
        Assert.That(result[2], Is.EqualTo("item3"));
    }

    [Test]
    public void Configuration_ParseListValue_TrimsWhitespace()
    {
        var result = Configuration.ParseListValue("item1 , item2 , item3");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("item1"));
        Assert.That(result[1], Is.EqualTo("item2"));
        Assert.That(result[2], Is.EqualTo("item3"));
    }

    [Test]
    public void Configuration_ParseListValue_ReturnsEmptyListForNull()
    {
        var result = Configuration.ParseListValue(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void Configuration_ParseListValue_ReturnsEmptyListForEmptyString()
    {
        var result = Configuration.ParseListValue("");

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region ExecutionConfig Tests

    [Test]
    public void ExecutionConfig_DefaultConstructor_SetsDefaultValues()
    {
        var config = new ExecutionConfig();

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
    public void ExecutionConfig_ConfigurationConstructor_UsesProvidedConfiguration()
    {
        var configuration = new Configuration();
        configuration.SetString("test.key", "test.value");

        var execConfig = new ExecutionConfig(configuration);

        Assert.That(execConfig.GetConfiguration(), Is.SameAs(configuration));
        Assert.That(execConfig.GetProperty("test.key"), Is.EqualTo("test.value"));
    }

    [Test]
    public void ExecutionConfig_SetParallelism_SetsValue()
    {
        var config = new ExecutionConfig();
        config.SetParallelism(8);

        Assert.That(config.Parallelism, Is.EqualTo(8));
    }

    [Test]
    public void ExecutionConfig_SetParallelism_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.SetParallelism(4);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_SetMaxParallelism_SetsValue()
    {
        var config = new ExecutionConfig();
        config.SetMaxParallelism(128);

        Assert.That(config.MaxParallelism, Is.EqualTo(128));
    }

    [Test]
    public void ExecutionConfig_SetMaxParallelism_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.SetMaxParallelism(64);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_SetAutoWatermarkInterval_SetsValue()
    {
        var config = new ExecutionConfig();
        config.SetAutoWatermarkInterval(5000L);

        Assert.That(config.AutoWatermarkInterval, Is.EqualTo(5000L));
    }

    [Test]
    public void ExecutionConfig_SetAutoWatermarkInterval_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.SetAutoWatermarkInterval(1000L);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_EnableObjectReuse_EnablesWhenTrue()
    {
        var config = new ExecutionConfig();
        config.EnableObjectReuse(true);

        Assert.That(config.ObjectReuseEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableObjectReuse_DisablesWhenFalse()
    {
        var config = new ExecutionConfig();
        config.EnableObjectReuse(true);
        config.EnableObjectReuse(false);

        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_EnableObjectReuse_DefaultsToTrue()
    {
        var config = new ExecutionConfig();
        config.EnableObjectReuse();

        Assert.That(config.ObjectReuseEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableObjectReuse_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.EnableObjectReuse();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_DisableObjectReuse_DisablesObjectReuse()
    {
        var config = new ExecutionConfig();
        config.EnableObjectReuse(true);
        config.DisableObjectReuse();

        Assert.That(config.ObjectReuseEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_DisableObjectReuse_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.DisableObjectReuse();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_EnableClosureCleaner_EnablesWhenTrue()
    {
        var config = new ExecutionConfig();
        config.EnableClosureCleaner(false);
        config.EnableClosureCleaner(true);

        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableClosureCleaner_DisablesWhenFalse()
    {
        var config = new ExecutionConfig();
        config.EnableClosureCleaner(false);

        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_EnableClosureCleaner_DefaultsToTrue()
    {
        var config = new ExecutionConfig();
        config.EnableClosureCleaner(false);
        config.EnableClosureCleaner();

        Assert.That(config.ClosureCleanerEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableClosureCleaner_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.EnableClosureCleaner();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_DisableClosureCleaner_DisablesClosureCleaner()
    {
        var config = new ExecutionConfig();
        config.DisableClosureCleaner();

        Assert.That(config.ClosureCleanerEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_DisableClosureCleaner_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.DisableClosureCleaner();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_SetRestartStrategy_SetsValue()
    {
        var config = new ExecutionConfig();
        config.SetRestartStrategy("fixed-delay");

        Assert.That(config.RestartStrategy, Is.EqualTo("fixed-delay"));
    }

    [Test]
    public void ExecutionConfig_SetRestartStrategy_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.SetRestartStrategy("failure-rate");

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_EnableSlotSharing_EnablesWhenTrue()
    {
        var config = new ExecutionConfig();
        config.EnableSlotSharing(false);
        config.EnableSlotSharing(true);

        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableSlotSharing_DisablesWhenFalse()
    {
        var config = new ExecutionConfig();
        config.EnableSlotSharing(false);

        Assert.That(config.SlotSharingEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_EnableSlotSharing_DefaultsToTrue()
    {
        var config = new ExecutionConfig();
        config.EnableSlotSharing(false);
        config.EnableSlotSharing();

        Assert.That(config.SlotSharingEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableSlotSharing_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.EnableSlotSharing();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_EnableAdaptiveScheduler_EnablesWhenTrue()
    {
        var config = new ExecutionConfig();
        config.EnableAdaptiveScheduler(true);

        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableAdaptiveScheduler_DisablesWhenFalse()
    {
        var config = new ExecutionConfig();
        config.EnableAdaptiveScheduler(true);
        config.EnableAdaptiveScheduler(false);

        Assert.That(config.AdaptiveSchedulerEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_EnableAdaptiveScheduler_DefaultsToTrue()
    {
        var config = new ExecutionConfig();
        config.EnableAdaptiveScheduler();

        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableAdaptiveScheduler_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.EnableAdaptiveScheduler();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_EnableReactiveMode_EnablesWhenTrue()
    {
        var config = new ExecutionConfig();
        config.EnableReactiveMode(true);

        Assert.That(config.ReactiveModeEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableReactiveMode_DisablesWhenFalse()
    {
        var config = new ExecutionConfig();
        config.EnableReactiveMode(true);
        config.EnableReactiveMode(false);

        Assert.That(config.ReactiveModeEnabled, Is.False);
    }

    [Test]
    public void ExecutionConfig_EnableReactiveMode_DefaultsToTrue()
    {
        var config = new ExecutionConfig();
        config.EnableReactiveMode();

        Assert.That(config.ReactiveModeEnabled, Is.True);
    }

    [Test]
    public void ExecutionConfig_EnableReactiveMode_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.EnableReactiveMode();

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_SetProperty_StoresValueInConfiguration()
    {
        var config = new ExecutionConfig();
        config.SetProperty("custom.key", "custom.value");

        Assert.That(config.GetProperty("custom.key"), Is.EqualTo("custom.value"));
    }

    [Test]
    public void ExecutionConfig_SetProperty_ReturnsConfigForChaining()
    {
        var config = new ExecutionConfig();
        var result = config.SetProperty("key", "value");

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void ExecutionConfig_GetProperty_ReturnsDefaultWhenNotFound()
    {
        var config = new ExecutionConfig();

        Assert.That(config.GetProperty("nonexistent", "default"), Is.EqualTo("default"));
    }

    [Test]
    public void ExecutionConfig_FluentConfiguration_SupportsMethodChaining()
    {
        var config = new ExecutionConfig()
            .SetParallelism(8)
            .SetMaxParallelism(128)
            .EnableObjectReuse()
            .EnableAdaptiveScheduler()
            .SetRestartStrategy("fixed-delay");

        Assert.That(config.Parallelism, Is.EqualTo(8));
        Assert.That(config.MaxParallelism, Is.EqualTo(128));
        Assert.That(config.ObjectReuseEnabled, Is.True);
        Assert.That(config.AdaptiveSchedulerEnabled, Is.True);
        Assert.That(config.RestartStrategy, Is.EqualTo("fixed-delay"));
    }

    [Test]
    public void Configuration_GetInteger_ReturnsDefaultForMissingKey()
    {
        var config = new Configuration();
        var result = config.GetInteger("missing.key", 999);
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void Configuration_GetBoolean_ReturnsDefaultForMissingKey()
    {
        var config = new Configuration();
        var result = config.GetBoolean("missing.key", true);
        Assert.That(result, Is.True);
    }

    [Test]
    public void Configuration_GetLong_ReturnsDefaultForMissingKey()
    {
        var config = new Configuration();
        var result = config.GetLong("missing.key", 999L);
        Assert.That(result, Is.EqualTo(999L));
    }

    #endregion
}
