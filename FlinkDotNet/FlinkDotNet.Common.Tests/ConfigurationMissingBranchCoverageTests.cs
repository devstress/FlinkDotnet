namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Additional tests to cover remaining uncovered branches in FlinkConfiguration class
/// </summary>
[TestFixture]
public class ConfigurationMissingBranchCoverageTests
{
    #region Constructor with Dictionary - Additional Coverage

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
    public void Constructor_WithNullableTypesInDictionary_StoresValues()
    {
        // Arrange
        var dict = new Dictionary<string, object>
        {
            { "nullableInt", (int?)42 },
            { "nullableBool", (bool?)true },
            { "nullableLong", (long?)1000L }
        };

        // Act
        var config = new FlinkConfiguration(dict);

        // Assert
        Assert.That(config.ContainsKey("nullableInt"), Is.True);
        Assert.That(config.ContainsKey("nullableBool"), Is.True);
        Assert.That(config.ContainsKey("nullableLong"), Is.True);
    }

    #endregion

    #region GetInteger - Missing Branch Coverage

    [Test]
    public void GetInteger_WithDoubleValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "doubleKey", 123.45 }
        });

        // Act
        var result = config.GetInteger("doubleKey", 999);

        // Assert - double is not int, ToString() parsing should fail
        Assert.That(result, Is.EqualTo(999));
    }

    [Test]
    public void GetInteger_WithNegativeStringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "negKey", "-42" }
        });

        // Act
        var result = config.GetInteger("negKey", 999);

        // Assert - negative string should parse correctly
        Assert.That(result, Is.EqualTo(-42));
    }

    #endregion

    #region GetBoolean - Missing Branch Coverage

    [Test]
    public void GetBoolean_WithIntValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "intKey", 42 }
        });

        // Act
        var result = config.GetBoolean("intKey", true);

        // Assert - int is not bool, ToString() parsing should fail
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithFalseStringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "falseKey", "False" }
        });

        // Act
        var result = config.GetBoolean("falseKey", true);

        // Assert - "False" string should parse correctly
        Assert.That(result, Is.False);
    }

    #endregion

    #region GetLong - Missing Branch Coverage

    [Test]
    public void GetLong_WithDoubleValue_ReturnsDefault()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "doubleKey", 123.45 }
        });

        // Act
        var result = config.GetLong("doubleKey", 999L);

        // Assert - double is not long, ToString() parsing should fail  
        Assert.That(result, Is.EqualTo(999L));
    }

    [Test]
    public void GetLong_WithIntValue_ConvertsToLong()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "intKey", 42 }
        });

        // Act
        var result = config.GetLong("intKey", 999L);

        // Assert - int should be parseable as long
        Assert.That(result, Is.EqualTo(42L));
    }

    [Test]
    public void GetLong_WithNegativeStringValue_ParsesCorrectly()
    {
        // Arrange
        var config = new FlinkConfiguration(new Dictionary<string, object>
        {
            { "negKey", "-12345" }
        });

        // Act
        var result = config.GetLong("negKey", 999L);

        // Assert - negative string should parse correctly
        Assert.That(result, Is.EqualTo(-12345L));
    }

    #endregion

    #region ParseListValue - Edge Cases

    [Test]
    public void ParseListValue_WithMixedWhitespace_HandlesCorrectly()
    {
        // Arrange & Act
        var result = FlinkConfiguration.ParseListValue("  value1  ,  value2  ,  value3  ");

        // Assert - should trim whitespace
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Does.Contain("value1"));
        Assert.That(result, Does.Contain("value2"));
        Assert.That(result, Does.Contain("value3"));
    }

    [Test]
    public void ParseListValue_WithConsecutiveCommas_SkipsEmptyEntries()
    {
        // Arrange & Act
        var result = FlinkConfiguration.ParseListValue("value1,,value2,,,value3");

        // Assert - empty entries should be removed
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result, Does.Contain("value1"));
        Assert.That(result, Does.Contain("value2"));
        Assert.That(result, Does.Contain("value3"));
    }

    #endregion
}
