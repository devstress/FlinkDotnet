using FlinkDotNet.Common;

namespace FlinkDotNet.Common.Tests;

[TestFixture]
public class ConfigurationTests
{
    [Test]
    public void DefaultConstructor_CreatesEmptyConfiguration()
    {
        // Arrange & Act
        var config = new Configuration();

        // Assert
        Assert.That(config, Is.Not.Null);
        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void ConstructorWithDictionary_CopiesAllValues()
    {
        // Arrange
        var initialConfig = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true }
        };

        // Act
        var config = new Configuration(initialConfig);

        // Assert
        Assert.That(config.ContainsKey("key1"), Is.True);
        Assert.That(config.ContainsKey("key2"), Is.True);
        Assert.That(config.ContainsKey("key3"), Is.True);
        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
    }

    [Test]
    public void SetString_StoresAndRetrievesValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.SetString("testKey", "testValue");

        // Assert
        Assert.That(result, Is.SameAs(config)); // Method chaining
        Assert.That(config.GetString("testKey"), Is.EqualTo("testValue"));
    }

    [Test]
    public void SetInteger_StoresAndRetrievesValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.SetInteger("intKey", 123);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.GetInteger("intKey"), Is.EqualTo(123));
    }

    [Test]
    public void SetBoolean_StoresAndRetrievesValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.SetBoolean("boolKey", true);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.GetBoolean("boolKey"), Is.True);
    }

    [Test]
    public void SetLong_StoresAndRetrievesValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.SetLong("longKey", 9876543210L);

        // Assert
        Assert.That(result, Is.SameAs(config));
        Assert.That(config.GetLong("longKey"), Is.EqualTo(9876543210L));
    }

    [Test]
    public void GetString_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetString("nonExistent", "default");

        // Assert
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void GetString_NonExistentKeyNoDefault_ReturnsEmptyString()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetString("nonExistent");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetInteger_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetInteger("nonExistent", 999);

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetInteger_StringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("numKey", "456");

        // Act
        var result = config.GetInteger("numKey");

        // Assert
        Assert.That(result, Is.EqualTo(456));
    }

    [Test]
    public void GetBoolean_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetBoolean("nonExistent", true);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_StringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("boolKey", "true");

        // Act
        var result = config.GetBoolean("boolKey");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetLong_NonExistentKey_ReturnsDefaultValue()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetLong("nonExistent", 12345L);

        // Assert
        Assert.That(result, Is.EqualTo(12345L));
    }

    [Test]
    public void GetLong_StringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("longKey", "9876543210");

        // Act
        var result = config.GetLong("longKey");

        // Assert
        Assert.That(result, Is.EqualTo(9876543210L));
    }

    [Test]
    public void ContainsKey_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("existingKey", "value");

        // Act & Assert
        Assert.That(config.ContainsKey("existingKey"), Is.True);
    }

    [Test]
    public void ContainsKey_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var config = new Configuration();

        // Act & Assert
        Assert.That(config.ContainsKey("nonExistent"), Is.False);
    }

    [Test]
    public void RemoveKey_ExistingKey_ReturnsTrueAndRemovesKey()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("toRemove", "value");

        // Act
        var result = config.RemoveKey("toRemove");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(config.ContainsKey("toRemove"), Is.False);
    }

    [Test]
    public void RemoveKey_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.RemoveKey("nonExistent");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetKeys_ReturnsAllConfigurationKeys()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key1", "value1");
        config.SetInteger("key2", 42);
        config.SetBoolean("key3", true);

        // Act
        var keys = config.GetKeys().ToList();

        // Assert
        Assert.That(keys, Has.Count.EqualTo(3));
        Assert.That(keys, Contains.Item("key1"));
        Assert.That(keys, Contains.Item("key2"));
        Assert.That(keys, Contains.Item("key3"));
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var original = new Configuration();
        original.SetString("key1", "value1");
        original.SetInteger("key2", 42);

        // Act
        var clone = original.Clone();
        clone.SetString("key3", "value3");

        // Assert
        Assert.That(clone.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(clone.GetInteger("key2"), Is.EqualTo(42));
        Assert.That(clone.ContainsKey("key3"), Is.True);
        Assert.That(original.ContainsKey("key3"), Is.False); // Original not affected
    }

    [Test]
    public void AddAll_MergesConfigurations()
    {
        // Arrange
        var config1 = new Configuration();
        config1.SetString("key1", "value1");
        config1.SetInteger("key2", 42);

        var config2 = new Configuration();
        config2.SetString("key3", "value3");
        config2.SetBoolean("key4", true);

        // Act
        var result = config1.AddAll(config2);

        // Assert
        Assert.That(result, Is.SameAs(config1)); // Method chaining
        Assert.That(config1.ContainsKey("key1"), Is.True);
        Assert.That(config1.ContainsKey("key2"), Is.True);
        Assert.That(config1.ContainsKey("key3"), Is.True);
        Assert.That(config1.ContainsKey("key4"), Is.True);
    }

    [Test]
    public void AddAll_OverwritesExistingKeys()
    {
        // Arrange
        var config1 = new Configuration();
        config1.SetString("key1", "original");

        var config2 = new Configuration();
        config2.SetString("key1", "updated");

        // Act
        config1.AddAll(config2);

        // Assert
        Assert.That(config1.GetString("key1"), Is.EqualTo("updated"));
    }

    [Test]
    public void ToMap_ReturnsAllConfigurationValues()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key1", "value1");
        config.SetInteger("key2", 42);
        config.SetBoolean("key3", true);

        // Act
        var map = config.ToMap();

        // Assert
        Assert.That(map, Has.Count.EqualTo(3));
        Assert.That(map["key1"], Is.EqualTo("value1"));
        Assert.That(map["key2"], Is.EqualTo(42));
        Assert.That(map["key3"], Is.EqualTo(true));
    }

    [Test]
    public void ParseListValue_NullValue_ReturnsEmptyList()
    {
        // Act
        var result = Configuration.ParseListValue(null);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_EmptyString_ReturnsEmptyList()
    {
        // Act
        var result = Configuration.ParseListValue("");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WhitespaceString_ReturnsEmptyList()
    {
        // Act
        var result = Configuration.ParseListValue("   ");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_CommaSeparatedValues_ReturnsListOfValues()
    {
        // Act
        var result = Configuration.ParseListValue("value1,value2,value3").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void ParseListValue_ValuesWithSpaces_TrimsSpaces()
    {
        // Act
        var result = Configuration.ParseListValue(" value1 , value2 , value3 ").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void ParseListValue_SingleValue_ReturnsSingleItemList()
    {
        // Act
        var result = Configuration.ParseListValue("singleValue").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("singleValue"));
    }

    [Test]
    public void MethodChaining_MultipleOperations_WorksCorrectly()
    {
        // Arrange
        var config = new Configuration();

        // Act
        config.SetString("key1", "value1")
              .SetInteger("key2", 42)
              .SetBoolean("key3", true)
              .SetLong("key4", 9876543210L);

        // Assert
        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(config.GetInteger("key2"), Is.EqualTo(42));
        Assert.That(config.GetBoolean("key3"), Is.True);
        Assert.That(config.GetLong("key4"), Is.EqualTo(9876543210L));
    }
}