using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for simple POCO classes with init-only properties and minimal logic
    /// </summary>
    [TestFixture]
    public sealed class AdditionalCoverageFor95PercentTests
    {
        #region ModelDescription Tests

        [Test]
        public void ModelDescription_InitOnlyProperties_CanBeSetInInitializer()
        {
            // Arrange & Act
            var description = new ModelDescription
            {
                ModelName = "test-model",
                Provider = "openai",
                InputSchema = new Dictionary<string, string>
                {
                    { "text", "STRING" },
                    { "value", "INTEGER" }
                }
            };

            // Assert
            Assert.That(description.ModelName, Is.EqualTo("test-model"));
            Assert.That(description.Provider, Is.EqualTo("openai"));
            Assert.That(description.InputSchema, Is.Not.Null);
            Assert.That(description.InputSchema.Count, Is.EqualTo(2));
            Assert.That(description.InputSchema["text"], Is.EqualTo("STRING"));
        }

        [Test]
        public void ModelDescription_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var description = new ModelDescription();

            // Assert
            Assert.That(description.ModelName, Is.EqualTo(string.Empty));
            Assert.That(description.Provider, Is.EqualTo(string.Empty));
            Assert.That(description.InputSchema, Is.Not.Null);
            Assert.That(description.InputSchema.Count, Is.EqualTo(0));
        }

        [Test]
        public void ModelDescription_WithComplexSchema_InitializesCorrectly()
        {
            // Arrange & Act
            var description = new ModelDescription
            {
                ModelName = "gpt-4",
                Provider = "azure-openai",
                InputSchema = new Dictionary<string, string>
                {
                    { "prompt", "STRING" },
                    { "temperature", "DOUBLE" },
                    { "max_tokens", "INTEGER" },
                    { "top_p", "DOUBLE" }
                }
            };

            // Assert
            Assert.That(description.InputSchema.Count, Is.EqualTo(4));
            Assert.That(description.InputSchema.ContainsKey("prompt"), Is.True);
            Assert.That(description.InputSchema.ContainsKey("temperature"), Is.True);
        }

        #endregion

        #region RocksDBOptions Additional Tests

        [Test]
        public void RocksDBOptions_WithCustomProperties_InitializesCorrectly()
        {
            // Arrange & Act
            var options = new RocksDBOptions
            {
                Properties = new Dictionary<string, string>
                {
                    { "compaction.style", "universal" },
                    { "write.buffer.size", "134217728" }
                }
            };

            // Assert
            Assert.That(options.Properties, Is.Not.Null);
            Assert.That(options.Properties.Count, Is.EqualTo(2));
            Assert.That(options.Properties["compaction.style"], Is.EqualTo("universal"));
        }

        #endregion

        #region SavepointResult Additional Tests

        [Test]
        public void SavepointResult_WithS3Path_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new SavepointResult
            {
                SavepointPath = "s3://bucket/savepoints/savepoint-123",
                TriggerId = "trigger-456",
                Success = true
            };

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("s3://bucket/savepoints/savepoint-123"));
            Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SavepointResult_WithError_InitializesCorrectly()
        {
            // Arrange & Act
            var result = new SavepointResult
            {
                SavepointPath = "",
                TriggerId = "trigger-789",
                Success = false,
                Error = "Failed to create savepoint"
            };

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.EqualTo("Failed to create savepoint"));
        }

        #endregion

        #region StopWithSavepointResult Additional Tests

        [Test]
        public void StopWithSavepointResult_SavepointPath_CanBeSet()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult
            {
                SavepointPath = "s3://bucket/savepoints/savepoint-789",
                Success = true
            };

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("s3://bucket/savepoints/savepoint-789"));
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void StopWithSavepointResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
        }

        #endregion
    }
}
