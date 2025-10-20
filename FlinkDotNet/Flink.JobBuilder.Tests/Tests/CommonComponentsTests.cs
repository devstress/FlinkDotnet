using FlinkDotNet.Common;

namespace Flink.JobBuilder.Tests.Tests;

/// <summary>
/// Tests for FlinkDotNet.Common.Configuration class
/// </summary>
[TestFixture]
public class CommonConfigurationTests
{
    #region Constructor Tests

    [Test]
    public void Configuration_DefaultConstructor_CreatesEmptyConfiguration()
    {
        var config = new Configuration();

        Assert.That(config, Is.Not.Null);
        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void Configuration_ConstructorWithDictionary_InitializesValues()
    {
        var initialValues = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 }
        };

        var config = new Configuration(initialValues);

        Assert.That(config.ContainsKey("key1"), Is.True);
        Assert.That(config.ContainsKey("key2"), Is.True);
        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
    }

    #endregion

    #region SetString/GetString Tests

    [Test]
    public void SetString_WithValidKey_StoresValue()
    {
        var config = new Configuration();

        config.SetString("test.key", "test.value");

        Assert.That(config.GetString("test.key"), Is.EqualTo("test.value"));
    }

    [Test]
    public void SetString_ReturnsConfiguration_ForMethodChaining()
    {
        var config = new Configuration();

        var result = config.SetString("key", "value");

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void GetString_WithNonExistentKey_ReturnsDefaultValue()
    {
        var config = new Configuration();

        var result = config.GetString("nonexistent", "default");

        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void GetString_WithNonExistentKeyAndNoDefault_ReturnsEmptyString()
    {
        var config = new Configuration();

        var result = config.GetString("nonexistent");

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region SetInteger/GetInteger Tests

    [Test]
    public void SetInteger_WithValidKey_StoresValue()
    {
        var config = new Configuration();

        config.SetInteger("parallelism", 8);

        Assert.That(config.GetInteger("parallelism"), Is.EqualTo(8));
    }

    [Test]
    public void SetInteger_ReturnsConfiguration_ForMethodChaining()
    {
        var config = new Configuration();

        var result = config.SetInteger("count", 100);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void GetInteger_WithNonExistentKey_ReturnsDefaultValue()
    {
        var config = new Configuration();

        var result = config.GetInteger("nonexistent", 42);

        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetInteger_WithNonExistentKeyAndNoDefault_ReturnsZero()
    {
        var config = new Configuration();

        var result = config.GetInteger("nonexistent");

        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetInteger_WithStringValue_ParsesCorrectly()
    {
        var config = new Configuration();
        config.SetString("number", "123");

        var result = config.GetInteger("number");

        Assert.That(result, Is.EqualTo(123));
    }

    #endregion

    #region SetBoolean/GetBoolean Tests

    [Test]
    public void SetBoolean_WithValidKey_StoresValue()
    {
        var config = new Configuration();

        config.SetBoolean("enabled", true);

        Assert.That(config.GetBoolean("enabled"), Is.True);
    }

    [Test]
    public void SetBoolean_ReturnsConfiguration_ForMethodChaining()
    {
        var config = new Configuration();

        var result = config.SetBoolean("flag", false);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void GetBoolean_WithNonExistentKey_ReturnsDefaultValue()
    {
        var config = new Configuration();

        var result = config.GetBoolean("nonexistent", true);

        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithNonExistentKeyAndNoDefault_ReturnsFalse()
    {
        var config = new Configuration();

        var result = config.GetBoolean("nonexistent");

        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithStringValue_ParsesCorrectly()
    {
        var config = new Configuration();
        config.SetString("flag", "true");

        var result = config.GetBoolean("flag");

        Assert.That(result, Is.True);
    }

    #endregion

    #region SetLong/GetLong Tests

    [Test]
    public void SetLong_WithValidKey_StoresValue()
    {
        var config = new Configuration();

        config.SetLong("timestamp", 1234567890L);

        Assert.That(config.GetLong("timestamp"), Is.EqualTo(1234567890L));
    }

    [Test]
    public void SetLong_ReturnsConfiguration_ForMethodChaining()
    {
        var config = new Configuration();

        var result = config.SetLong("id", 999L);

        Assert.That(result, Is.SameAs(config));
    }

    [Test]
    public void GetLong_WithNonExistentKey_ReturnsDefaultValue()
    {
        var config = new Configuration();

        var result = config.GetLong("nonexistent", 123L);

        Assert.That(result, Is.EqualTo(123L));
    }

    [Test]
    public void GetLong_WithNonExistentKeyAndNoDefault_ReturnsZero()
    {
        var config = new Configuration();

        var result = config.GetLong("nonexistent");

        Assert.That(result, Is.EqualTo(0L));
    }

    #endregion

    #region ContainsKey Tests

    [Test]
    public void ContainsKey_WithExistingKey_ReturnsTrue()
    {
        var config = new Configuration();
        config.SetString("existing", "value");

        var result = config.ContainsKey("existing");

        Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsKey_WithNonExistentKey_ReturnsFalse()
    {
        var config = new Configuration();

        var result = config.ContainsKey("nonexistent");

        Assert.That(result, Is.False);
    }

    #endregion

    #region RemoveKey Tests

    [Test]
    public void RemoveKey_WithExistingKey_RemovesKeyAndReturnsTrue()
    {
        var config = new Configuration();
        config.SetString("toRemove", "value");

        var result = config.RemoveKey("toRemove");

        Assert.That(result, Is.True);
        Assert.That(config.ContainsKey("toRemove"), Is.False);
    }

    [Test]
    public void RemoveKey_WithNonExistentKey_ReturnsFalse()
    {
        var config = new Configuration();

        var result = config.RemoveKey("nonexistent");

        Assert.That(result, Is.False);
    }

    #endregion

    #region GetKeys Tests

    [Test]
    public void GetKeys_WithEmptyConfiguration_ReturnsEmptyCollection()
    {
        var config = new Configuration();

        var keys = config.GetKeys();

        Assert.That(keys, Is.Empty);
    }

    [Test]
    public void GetKeys_WithMultipleKeys_ReturnsAllKeys()
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

    #endregion

    #region Clone Tests

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        var original = new Configuration();
        original.SetString("key1", "value1");
        original.SetInteger("key2", 42);

        var cloned = original.Clone();

        Assert.That(cloned, Is.Not.SameAs(original));
        Assert.That(cloned.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(cloned.GetInteger("key2"), Is.EqualTo(42));
    }

    [Test]
    public void Clone_ModifyingClone_DoesNotAffectOriginal()
    {
        var original = new Configuration();
        original.SetString("key", "original");

        var cloned = original.Clone();
        cloned.SetString("key", "modified");

        Assert.That(original.GetString("key"), Is.EqualTo("original"));
        Assert.That(cloned.GetString("key"), Is.EqualTo("modified"));
    }

    #endregion

    #region AddAll Tests

    [Test]
    public void AddAll_MergesConfigurationsCorrectly()
    {
        var config1 = new Configuration();
        config1.SetString("key1", "value1");

        var config2 = new Configuration();
        config2.SetString("key2", "value2");

        config1.AddAll(config2);

        Assert.That(config1.ContainsKey("key1"), Is.True);
        Assert.That(config1.ContainsKey("key2"), Is.True);
        Assert.That(config1.GetString("key2"), Is.EqualTo("value2"));
    }

    [Test]
    public void AddAll_OverwritesExistingKeys()
    {
        var config1 = new Configuration();
        config1.SetString("key", "original");

        var config2 = new Configuration();
        config2.SetString("key", "updated");

        config1.AddAll(config2);

        Assert.That(config1.GetString("key"), Is.EqualTo("updated"));
    }

    [Test]
    public void AddAll_ReturnsConfiguration_ForMethodChaining()
    {
        var config1 = new Configuration();
        var config2 = new Configuration();

        var result = config1.AddAll(config2);

        Assert.That(result, Is.SameAs(config1));
    }

    #endregion

    #region ToMap Tests

    [Test]
    public void ToMap_ReturnsAllConfigurationValues()
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
    public void ToMap_ReturnsIndependentCopy()
    {
        var config = new Configuration();
        config.SetString("key", "value");

        var map = config.ToMap();
        map["key"] = "modified";

        Assert.That(config.GetString("key"), Is.EqualTo("value"));
    }

    #endregion

    #region ParseListValue Tests

    [Test]
    public void ParseListValue_WithCommaSeparatedString_ReturnsListOfValues()
    {
        var result = Configuration.ParseListValue("value1,value2,value3");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void ParseListValue_WithWhitespace_TrimsValues()
    {
        var result = Configuration.ParseListValue("value1 , value2 , value3");

        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
    }

    [Test]
    public void ParseListValue_WithNullValue_ReturnsEmptyList()
    {
        var result = Configuration.ParseListValue(null);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WithEmptyString_ReturnsEmptyList()
    {
        var result = Configuration.ParseListValue("");

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WithWhitespaceOnly_ReturnsEmptyList()
    {
        var result = Configuration.ParseListValue("   ");

        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Method Chaining Tests

    [Test]
    public void Configuration_SupportsMethodChaining()
    {
        var config = new Configuration()
            .SetString("name", "test")
            .SetInteger("count", 10)
            .SetBoolean("enabled", true)
            .SetLong("timestamp", 123456L);

        Assert.That(config.GetString("name"), Is.EqualTo("test"));
        Assert.That(config.GetInteger("count"), Is.EqualTo(10));
        Assert.That(config.GetBoolean("enabled"), Is.True);
        Assert.That(config.GetLong("timestamp"), Is.EqualTo(123456L));
    }

    #endregion
}