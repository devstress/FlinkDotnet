namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Additional tests to achieve 100% branch coverage for FlinkConfiguration class
/// </summary>
[TestFixture]
public class ConfigurationBranchCoverageTests
{
    #region GetInteger Branch Coverage

    [Test]
    public void GetInteger_WithActualIntValue_ReturnsValue()
    {
        // Arrange - Store actual int type (not string)
        var config = new FlinkConfiguration();
        _ = config.SetInteger("intKey", 42);

        // Act
        var result = config.GetInteger("intKey");

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetInteger_WithActualIntValueAndDefault_ReturnsValue()
    {
        // Arrange - Store actual int type (not string)
        var config = new FlinkConfiguration();
        _ = config.SetInteger("intKey", 123);

        // Act
        var result = config.GetInteger("intKey", 999);

        // Assert
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void GetInteger_WithStringValue_ParsesCorrectly()
    {
        // Arrange - Store string that can be parsed
        var config = new FlinkConfiguration();
        _ = config.SetString("strKey", "789");

        // Act
        var result = config.GetInteger("strKey", 999);

        // Assert
        Assert.That(result, Is.EqualTo(789));
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
    public void GetInteger_WithObjectRequiringParse_ParsesCorrectly()
    {
        // Arrange - Store object that needs ToString() + Parse
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", 789 }
        });

        // Act - Force through parse path by treating as object
        var result = config.GetInteger("objKey", 999);

        // Assert
        Assert.That(result, Is.EqualTo(789));
    }

    #endregion

    #region GetBoolean Branch Coverage

    [Test]
    public void GetBoolean_WithActualBoolValue_ReturnsValue()
    {
        // Arrange - Store actual bool type using SetBoolean
        var config = new FlinkConfiguration();
        _ = config.SetBoolean("boolKey", true);

        // Act
        var result = config.GetBoolean("boolKey");

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithActualBoolValueAndDefault_ReturnsValue()
    {
        // Arrange - Store actual bool type using SetBoolean
        var config = new FlinkConfiguration();
        _ = config.SetBoolean("boolKey", false);

        // Act
        var result = config.GetBoolean("boolKey", true);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithStringValue_ParsesCorrectly()
    {
        // Arrange - Store string that can be parsed
        var config = new FlinkConfiguration();
        _ = config.SetString("strKey", "true");

        // Act
        var result = config.GetBoolean("strKey", false);

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
    public void GetBoolean_WithObjectRequiringParse_ParsesCorrectly()
    {
        // Arrange - Store object that needs ToString() + Parse
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", true }
        });

        // Act - Force through parse path
        var result = config.GetBoolean("objKey", false);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region GetLong Branch Coverage

    [Test]
    public void GetLong_WithActualLongValue_ReturnsValue()
    {
        // Arrange - Store actual long type using SetLong
        var config = new FlinkConfiguration();
        _ = config.SetLong("longKey", 9876543210L);

        // Act
        var result = config.GetLong("longKey");

        // Assert
        Assert.That(result, Is.EqualTo(9876543210L));
    }

    [Test]
    public void GetLong_WithActualLongValueAndDefault_ReturnsValue()
    {
        // Arrange - Store actual long type using SetLong
        var config = new FlinkConfiguration();
        _ = config.SetLong("longKey", 1234567890L);

        // Act
        var result = config.GetLong("longKey", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(1234567890L));
    }

    [Test]
    public void GetLong_WithStringValue_ParsesCorrectly()
    {
        // Arrange - Store string that can be parsed
        var config = new FlinkConfiguration();
        _ = config.SetString("strKey", "5555555555");

        // Act
        var result = config.GetLong("strKey", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(5555555555L));
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
    public void GetLong_WithObjectRequiringParse_ParsesCorrectly()
    {
        // Arrange - Store object that needs ToString() + Parse
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", 5555555555L }
        });

        // Act - Force through parse path
        var result = config.GetLong("objKey", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(5555555555L));
    }

    #endregion

    #region GetString Branch Coverage

    [Test]
    public void GetString_WithNonNullValue_ReturnsToStringValue()
    {
        // Arrange - Store non-string object
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "numKey", 12345 }
        });

        // Act
        var result = config.GetString("numKey");

        // Assert
        Assert.That(result, Is.EqualTo("12345"));
    }

    [Test]
    public void GetString_WithNonNullValueAndDefault_ReturnsToStringValue()
    {
        // Arrange - Store non-string object
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "numKey", 67890 }
        });

        // Act
        var result = config.GetString("numKey", "default");

        // Assert
        Assert.That(result, Is.EqualTo("67890"));
    }

    #endregion

    #region Constructor Branch Coverage

    [Test]
    public void Constructor_WithEmptyDictionary_CreatesEmptyConfiguration()
    {
        // Arrange
        var emptyDict = new Dictionary<string, object>();

        // Act
        var config = new FlinkConfiguration(emptyDict);

        // Assert
        Assert.That(config.GetKeys(), Is.Empty);
    }

    [Test]
    public void Constructor_WithSingleItemDictionary_CopiesItem()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "singleKey", "singleValue" }
        };

        // Act
        var config = new FlinkConfiguration(dict);

        // Assert
        Assert.That(config.ContainsKey("singleKey"), Is.True);
        Assert.That(config.GetString("singleKey"), Is.EqualTo("singleValue"));
    }

    #endregion

    #region AddAll Branch Coverage

    [Test]
    public void AddAll_WithEmptyConfiguration_DoesNotModify()
    {
        // Arrange
        var config1 = new FlinkConfiguration();
        _ = config1.SetString("key1", "value1");

        var emptyConfig = new FlinkConfiguration();

        // Act
        _ = config1.AddAll(emptyConfig);

        // Assert
        Assert.That(config1.ContainsKey("key1"), Is.True);
        Assert.That(config1.GetKeys().Count(), Is.EqualTo(1));
    }

    [Test]
    public void AddAll_WithSingleKey_AddsKey()
    {
        // Arrange
        var config1 = new FlinkConfiguration();
        _ = config1.SetString("key1", "value1");

        var config2 = new FlinkConfiguration();
        _ = config2.SetString("key2", "value2");

        // Act
        _ = config1.AddAll(config2);

        // Assert
        Assert.That(config1.ContainsKey("key1"), Is.True);
        Assert.That(config1.ContainsKey("key2"), Is.True);
    }

    #endregion

    #region ParseListValue Branch Coverage

    [Test]
    public void ParseListValue_WithTrailingComma_IgnoresEmptyEntries()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("value1,value2,").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
    }

    [Test]
    public void ParseListValue_WithLeadingComma_IgnoresEmptyEntries()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue(",value1,value2").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(2));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
    }

    [Test]
    public void ParseListValue_WithMultipleCommas_IgnoresEmptyEntries()
    {
        // Act
        var result = FlinkConfiguration.ParseListValue("value1,,value2,,,value3").ToList();

        // Assert
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result[0], Is.EqualTo("value1"));
        Assert.That(result[1], Is.EqualTo("value2"));
        Assert.That(result[2], Is.EqualTo("value3"));
    }

    [Test]
    public void GetString_WithNullValue_ReturnsDefault()
    {
        // Arrange - Store null value explicitly
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "nullKey", null! }
        });

        // Act
        var result = config.GetString("nullKey", "myDefault");

        // Assert - Should return the default value when stored value is null
        Assert.That(result, Is.EqualTo("myDefault"));
    }

    [Test]
    public void GetString_WithNullValueAndNullDefault_ReturnsEmpty()
    {
        // Arrange - Store null value explicitly
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "nullKey", null! }
        });

        // Act
        var result = config.GetString("nullKey", null);

        // Assert - Should return empty string when both value and default are null
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetString_WithMissingKeyAndNullDefault_ReturnsEmpty()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetString("missingKey", null);

        // Assert - Should return empty string when key missing and default is null
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetString_WithMissingKeyNoDefault_ReturnsEmpty()
    {
        // Arrange
        var config = new FlinkConfiguration();

        // Act
        var result = config.GetString("missingKey");

        // Assert - Should return empty string when key missing and no default
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion

    #region Edge Cases for Complete Coverage

    [Test]
    public void GetInteger_WithNonIntNonStringObject_ReturnsDefault()
    {
        // Arrange - Store object that can't be parsed as int
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", new object() }
        });

        // Act
        var result = config.GetInteger("objKey", 999);

        // Assert
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetBoolean_WithNonBoolNonStringObject_ReturnsDefault()
    {
        // Arrange - Store object that can't be parsed as bool
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", new object() }
        });

        // Act
        var result = config.GetBoolean("objKey", true);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetLong_WithNonLongNonStringObject_ReturnsDefault()
    {
        // Arrange - Store object that can't be parsed as long
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "objKey", new object() }
        });

        // Act
        var result = config.GetLong("objKey", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(999L));
    }

    [Test]
    public void GetInteger_WithDoubleValue_ParsesAsInt()
    {
        // Arrange - double with whole number value
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "doubleKey", 42.0 }
        });

        // Act - ToString of double "42" can parse as int
        var result = config.GetInteger("doubleKey", 999);

        // Assert - Parses successfully as 42
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetLong_WithDoubleValue_ParsesAsLong()
    {
        // Arrange - double with whole number value
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "doubleKey", 42.0 }
        });

        // Act - ToString of double "42" can parse as long
        var result = config.GetLong("doubleKey", 999L);

        // Assert - Parses successfully as 42
        Assert.That(result, Is.EqualTo(42L));
    }

    #endregion
}
