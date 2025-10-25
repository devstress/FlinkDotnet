namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Comprehensive tests to achieve 100% branch coverage for FlinkConfiguration class
/// Covers all conditional branches and edge cases
/// </summary>
[TestFixture]
public class ConfigurationFullBranchCoverageTests
{
    #region Constructor with IDictionary - Line 43

    [Test]
    public void Constructor_WithEmptyDictionary_CreatesEmptyConfiguration()
    {
        // Arrange
        var dict = new Dictionary<string, object>();

        // Act
        var config = new FlinkConfiguration(dict);

        // Assert
        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void Constructor_WithMultipleValues_CopiesAllValues()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "key1", "value1" },
            { "key2", 42 },
            { "key3", true },
            { "key4", 100L }
        };

        // Act
        var config = new FlinkConfiguration(dict);

        // Assert
        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(config.GetInteger("key2"), Is.EqualTo(42));
        Assert.That(config.GetBoolean("key3"), Is.True);
        Assert.That(config.GetLong("key4"), Is.EqualTo(100L));
    }

    #endregion

    #region GetString - Lines 105-109

    [Test]
    public void GetString_WithExistingNullValue_ReturnsDefault()
    {
        // Arrange - Set a key with null value
        var dict = new Dictionary<string, object>
        {
            { "nullKey", null! }
        };
        var config = new FlinkConfiguration(dict);

        // Act
        var result = config.GetString("nullKey", "defaultValue");

        // Assert
        Assert.That(result, Is.EqualTo("defaultValue"));
    }

    [Test]
    public void GetString_WithExistingNullValueNoDefault_ReturnsEmpty()
    {
        // Arrange - Set a key with null value
        var dict = new Dictionary<string, object>
        {
            { "nullKey", null! }
        };
        var config = new FlinkConfiguration(dict);

        // Act
        var result = config.GetString("nullKey");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetString_WithExistingValue_ReturnsValue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "value");

        // Act
        var result = config.GetString("key", "default");

        // Assert
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void GetString_WithMissingKeyAndDefault_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetString("missingKey", "defaultValue");

        // Assert
        Assert.That(result, Is.EqualTo("defaultValue"));
    }

    [Test]
    public void GetString_WithMissingKeyAndNullDefault_ReturnsEmpty()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetString("missingKey", null);

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetString_WithMissingKeyNoDefault_ReturnsEmpty()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetString("missingKey");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetString_WithIntValue_ConvertsToString()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetInteger("intKey", 42);

        // Act
        var result = config.GetString("intKey");

        // Assert
        Assert.That(result, Is.EqualTo("42"));
    }

    #endregion

    #region GetInteger - Lines 120-124

    [Test]
    public void GetInteger_WithExistingIntValue_ReturnsValue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetInteger("key", 42);

        // Act
        var result = config.GetInteger("key", 999);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetInteger_WithParsableStringValue_ParsesAndReturns()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "123");

        // Act
        var result = config.GetInteger("key", 999);

        // Assert
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void GetInteger_WithUnparsableValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "notAnInt");

        // Act
        var result = config.GetInteger("key", 999);

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetInteger_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetInteger("missingKey", 999);

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetInteger_WithMissingKeyNoDefault_ReturnsZero()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetInteger("missingKey");

        // Assert
        Assert.That(result, Is.EqualTo(0));
    }

    [Test]
    public void GetInteger_WithBoolValue_FailsParseReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetBoolean("key", true);

        // Act
        var result = config.GetInteger("key", 999);

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    #endregion

    #region GetBoolean - Lines 138-142

    [Test]
    public void GetBoolean_WithExistingBoolValue_ReturnsValue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetBoolean("key", true);

        // Act
        var result = config.GetBoolean("key", false);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithParsableStringValue_ParsesAndReturns()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "true");

        // Act
        var result = config.GetBoolean("key", false);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithFalseStringValue_ParsesAndReturnsFalse()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "false");

        // Act
        var result = config.GetBoolean("key", true);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithUnparsableValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "notABool");

        // Act
        var result = config.GetBoolean("key", true);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetBoolean("missingKey", true);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithMissingKeyNoDefault_ReturnsFalse()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetBoolean("missingKey");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithIntValue_FailsParseReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetInteger("key", 42);

        // Act
        var result = config.GetBoolean("key", true);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region GetLong - Lines 156-160

    [Test]
    public void GetLong_WithExistingLongValue_ReturnsValue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetLong("key", 9876543210L);

        // Act
        var result = config.GetLong("key", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(9876543210L));
    }

    [Test]
    public void GetLong_WithParsableStringValue_ParsesAndReturns()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "1234567890");

        // Act
        var result = config.GetLong("key", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(1234567890L));
    }

    [Test]
    public void GetLong_WithUnparsableValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "notALong");

        // Act
        var result = config.GetLong("key", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(999L));
    }

    [Test]
    public void GetLong_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetLong("missingKey", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(999L));
    }

    [Test]
    public void GetLong_WithMissingKeyNoDefault_ReturnsZero()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetLong("missingKey");

        // Assert
        Assert.That(result, Is.EqualTo(0L));
    }

    [Test]
    public void GetLong_WithBoolValue_FailsParseReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetBoolean("key", true);

        // Act
        var result = config.GetLong("key", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(999L));
    }

    #endregion

    #region AddAll - Line 211

    [Test]
    public void AddAll_WithEmptyConfiguration_AddsNothing()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("existing", "value");
        var other = new FlinkConfiguration();

        // Act
        _ = config.AddAll(other);

        // Assert
        Assert.That(config.GetString("existing"), Is.EqualTo("value"));
        Assert.That(config.GetKeys().Count(), Is.EqualTo(1));
    }

    [Test]
    public void AddAll_WithMultipleValues_AddsAll()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key1", "value1");

        var other = new FlinkConfiguration();
        _ = other.SetString("key2", "value2");
        _ = other.SetInteger("key3", 42);
        _ = other.SetBoolean("key4", true);

        // Act
        _ = config.AddAll(other);

        // Assert
        Assert.That(config.GetString("key1"), Is.EqualTo("value1"));
        Assert.That(config.GetString("key2"), Is.EqualTo("value2"));
        Assert.That(config.GetInteger("key3"), Is.EqualTo(42));
        Assert.That(config.GetBoolean("key4"), Is.True);
    }

    [Test]
    public void AddAll_WithOverlappingKeys_OverwritesExisting()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key1", "original");

        var other = new FlinkConfiguration();
        _ = other.SetString("key1", "updated");

        // Act
        _ = config.AddAll(other);

        // Assert
        Assert.That(config.GetString("key1"), Is.EqualTo("updated"));
    }

    #endregion

    #region ParseListValue - Line 234

    [Test]
    public void ParseListValue_WithNull_ReturnsEmptyList()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue(null);

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WithEmptyString_ReturnsEmptyList()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WithWhitespace_ReturnsEmptyList()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("   ");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void ParseListValue_WithSingleValue_ReturnsSingleItem()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("value");

        // Assert
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.EqualTo("value"));
    }

    [Test]
    public void ParseListValue_WithMultipleValues_ReturnsAllItems()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("value1,value2,value3");

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void ParseListValue_WithSpacesAroundValues_TrimsValues()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue(" value1 , value2 , value3 ");

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void ParseListValue_WithEmptyEntries_RemovesEmptyEntries()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("value1,,value2,,,value3");

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    #endregion

    #region Additional Edge Cases

    [Test]
    public void ContainsKey_WithExistingKey_ReturnsTrue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "value");

        // Act
        var result = config.ContainsKey("key");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void ContainsKey_WithMissingKey_ReturnsFalse()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.ContainsKey("missingKey");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void RemoveKey_WithExistingKey_RemovesAndReturnsTrue()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "value");

        // Act
        var result = config.RemoveKey("key");

        // Assert
        Assert.That(result, Is.True);
        Assert.That(config.ContainsKey("key"), Is.False);
    }

    [Test]
    public void RemoveKey_WithMissingKey_ReturnsFalse()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.RemoveKey("missingKey");

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "original");

        // Act
        var clone = config.Clone();
        _ = clone.SetString("key", "modified");

        // Assert
        Assert.That(config.GetString("key"), Is.EqualTo("original"));
        Assert.That(clone.GetString("key"), Is.EqualTo("modified"));
    }

    [Test]
    public void ToMap_ReturnsAllValues()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key1", "value1");
        _ = config.SetInteger("key2", 42);

        // Act
        var map = config.ToMap();

        // Assert
        Assert.That(map, Has.Count.EqualTo(2));
        Assert.That(map["key1"], Is.EqualTo("value1"));
        Assert.That(map["key2"], Is.EqualTo(42));
    }

    [Test]
    public void ToMap_CreatesIndependentDictionary()
    {
        // Arrange
        var config = new FlinkConfiguration();
        _ = config.SetString("key", "original");

        // Act
        var map = config.ToMap();
        map["key"] = "modified";

        // Assert
        Assert.That(config.GetString("key"), Is.EqualTo("original"));
    }

    #endregion
}
