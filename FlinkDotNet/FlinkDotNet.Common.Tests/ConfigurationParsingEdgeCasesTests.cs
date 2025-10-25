using NUnit.Framework;
using FlinkDotNet.Common;

namespace FlinkDotNet.Common.Tests;

/// <summary>
/// Tests for Configuration parsing edge cases to achieve 100% branch coverage
/// Targets GetInteger, GetBoolean, and GetLong type conversion and parse failure paths
/// </summary>
[TestFixture]
public class ConfigurationParsingEdgeCasesTests
{
    #region GetInteger Edge Cases

    [Test]
    public void GetInteger_WithIntValue_ReturnsValue()
    {
        // Arrange
        var config = new Configuration();
        config.SetInteger("key", 42);

        // Act
        var result = config.GetInteger("key", 0);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetInteger_WithStringThatParsesToInt_ReturnsValue()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "123");

        // Act
        var result = config.GetInteger("key", 0);

        // Assert
        Assert.That(result, Is.EqualTo(123));
    }

    [Test]
    public void GetInteger_WithNonNumericString_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "not-a-number");

        // Act
        var result = config.GetInteger("key", 42);

        // Assert
        Assert.That(result, Is.EqualTo(42));
    }

    [Test]
    public void GetInteger_WithNonIntObject_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        var dict = config.ToMap();
        dict["key"] = new object(); // Non-int, non-parseable object

        // Act
        var result = config.GetInteger("key", 99);

        // Assert
        Assert.That(result, Is.EqualTo(99));
    }

    [Test]
    public void GetInteger_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetInteger("missing-key", 100);

        // Assert
        Assert.That(result, Is.EqualTo(100));
    }

    #endregion

    #region GetBoolean Edge Cases

    [Test]
    public void GetBoolean_WithBoolValue_ReturnsValue()
    {
        // Arrange
        var config = new Configuration();
        config.SetBoolean("key", true);

        // Act
        var result = config.GetBoolean("key", false);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithStringTrue_ReturnsTrue()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "true");

        // Act
        var result = config.GetBoolean("key", false);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithStringFalse_ReturnsFalse()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "false");

        // Act
        var result = config.GetBoolean("key", true);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithNonBooleanString_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "not-a-boolean");

        // Act
        var result = config.GetBoolean("key", true);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetBoolean_WithNonBoolObject_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        var dict = config.ToMap();
        dict["key"] = 123; // Non-bool, non-parseable object

        // Act
        var result = config.GetBoolean("key", false);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GetBoolean_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetBoolean("missing-key", true);

        // Assert
        Assert.That(result, Is.True);
    }

    #endregion

    #region GetLong Edge Cases

    [Test]
    public void GetLong_WithLongValue_ReturnsValue()
    {
        // Arrange
        var config = new Configuration();
        config.SetLong("key", 9876543210L);

        // Act
        var result = config.GetLong("key", 0L);

        // Assert
        Assert.That(result, Is.EqualTo(9876543210L));
    }

    [Test]
    public void GetLong_WithStringThatParsesToLong_ReturnsValue()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "123456789");

        // Act
        var result = config.GetLong("key", 0L);

        // Assert
        Assert.That(result, Is.EqualTo(123456789L));
    }

    [Test]
    public void GetLong_WithNonNumericString_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        config.SetString("key", "not-a-number");

        // Act
        var result = config.GetLong("key", 999L);

        // Assert
        Assert.That(result, Is.EqualTo(999L));
    }

    [Test]
    public void GetLong_WithNonLongObject_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        var dict = config.ToMap();
        dict["key"] = new object(); // Non-long, non-parseable object

        // Act
        var result = config.GetLong("key", 888L);

        // Assert
        Assert.That(result, Is.EqualTo(888L));
    }

    [Test]
    public void GetLong_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetLong("missing-key", 777L);

        // Assert
        Assert.That(result, Is.EqualTo(777L));
    }

    #endregion

    #region GetString Edge Cases

    [Test]
    public void GetString_WithNullValue_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();
        var dict = config.ToMap();
        dict["key"] = null!;

        // Act
        var result = config.GetString("key", "default");

        // Assert
        Assert.That(result, Is.EqualTo("default"));
    }

    [Test]
    public void GetString_WithMissingKey_ReturnsDefault()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetString("missing", "fallback");

        // Assert
        Assert.That(result, Is.EqualTo("fallback"));
    }

    [Test]
    public void GetString_WithMissingKeyAndNoDefault_ReturnsEmpty()
    {
        // Arrange
        var config = new Configuration();

        // Act
        var result = config.GetString("missing");

        // Assert
        Assert.That(result, Is.EqualTo(string.Empty));
    }

    #endregion
}
