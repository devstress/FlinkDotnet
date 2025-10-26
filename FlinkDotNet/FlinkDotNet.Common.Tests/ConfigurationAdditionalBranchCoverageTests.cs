using System;
using System.Collections.Generic;
using NUnit.Framework;
using FlinkDotNet.Common;

namespace FlinkDotNet.Common.Tests
{
    /// <summary>
    /// Additional tests to reach 100% branch coverage for FlinkConfiguration class.
    /// Targets specific uncovered edge cases in getter methods.
    /// </summary>
    [TestFixture]
    public class ConfigurationAdditionalBranchCoverageTests
    {
        [Test]
        public void GetString_WithNullValueInDictionary_AndNullDefault_ReturnsEmpty()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", null! } });

            // Act
            var result = config.GetString("key", null);

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetString_WithNullValueInDictionary_AndNoDefault_ReturnsEmpty()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", null! } });

            // Act
            var result = config.GetString("key");

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void GetInteger_WithUnparseableString_ReturnsDefault()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", "not-a-number" } });

            // Act
            var result = config.GetInteger("key", 999);

            // Assert
            Assert.That(result, Is.EqualTo(999));
        }

        [Test]
        public void GetBoolean_WithUnparseableString_ReturnsDefault()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", "not-a-boolean" } });

            // Act
            var result = config.GetBoolean("key", true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetLong_WithUnparseableString_ReturnsDefault()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", "not-a-long" } });

            // Act
            var result = config.GetLong("key", 999L);

            // Assert
            Assert.That(result, Is.EqualTo(999L));
        }

        [Test]
        public void GetString_WithObjectValue_ConvertsToString()
        {
            // Arrange
            var testObj = new { Name = "Test", Value = 42 };
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", testObj } });

            // Act
            var result = config.GetString("key");

            // Assert
            Assert.That(result, Does.Contain("Name"));
        }

        [Test]
        public void GetInteger_WithOverflowValue_ReturnsDefault()
        {
            // Arrange - long.MaxValue will overflow int parsing
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", long.MaxValue.ToString() } });

            // Act
            var result = config.GetInteger("key", 999);

            // Assert
            Assert.That(result, Is.EqualTo(999));
        }

        [Test]
        public void GetBoolean_WithNumericString_ReturnsDefault()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", "123" } });

            // Act
            var result = config.GetBoolean("key", true);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void GetLong_WithInvalidFormat_ReturnsDefault()
        {
            // Arrange
            var config = new FlinkConfiguration(new Dictionary<string, object> { { "key", "123.456" } });

            // Act
            var result = config.GetLong("key", 999L);

            // Assert
            Assert.That(result, Is.EqualTo(999L));
        }

        [Test]
        public void AddAll_WithOverlappingKeys_MergesCorrectly()
        {
            // Arrange
            var config = new FlinkConfiguration();
            config.SetString("existing", "value1");
            
            var other = new FlinkConfiguration();
            other.SetString("existing", "value2");
            other.SetString("newKey", "newValue");

            // Act
            config.AddAll(other);

            // Assert
            Assert.That(config.GetString("existing"), Is.EqualTo("value2"));
            Assert.That(config.GetString("newKey"), Is.EqualTo("newValue"));
        }

        [Test]
        public void ParseListValue_WithOnlyCommas_ReturnsEmpty()
        {
            // Act
            var result = FlinkConfiguration.ParseListValue(",,,");

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ParseListValue_WithSpacesAndCommas_ReturnsEmpty()
        {
            // Act
            var result = FlinkConfiguration.ParseListValue(" , , , ");

            // Assert
            Assert.That(result, Is.Empty);
        }

        [Test]
        public void ParseListValue_WithTrailingComma_IgnoresTrailing()
        {
            // Act
            var result = FlinkConfiguration.ParseListValue("value1,value2,");

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0], Is.EqualTo("value1"));
            Assert.That(result[1], Is.EqualTo("value2"));
        }

        [Test]
        public void ParseListValue_WithLeadingComma_IgnoresLeading()
        {
            // Act
            var result = FlinkConfiguration.ParseListValue(",value1,value2");

            // Assert
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0], Is.EqualTo("value1"));
            Assert.That(result[1], Is.EqualTo("value2"));
        }
    }
}
