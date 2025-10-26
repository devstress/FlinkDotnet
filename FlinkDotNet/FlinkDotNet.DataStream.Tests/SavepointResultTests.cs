using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class SavepointResultTests
    {
        [Test]
        public void SavepointResult_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new SavepointResult();
            var savepointPath = "/tmp/savepoints/savepoint-123";
            var triggerId = "trigger-456";
            var error = "Failed to create savepoint";

            // Act
            result.SavepointPath = savepointPath;
            result.Success = true;
            result.TriggerId = triggerId;
            result.Error = error;

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(savepointPath));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo(triggerId));
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void SavepointResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new SavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void SavepointResult_Success_CanBeSetToTrue()
        {
            // Arrange & Act
            var result = new SavepointResult { Success = true };

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SavepointResult_Error_CanBeNull()
        {
            // Arrange & Act
            var result = new SavepointResult
            {
                SavepointPath = "/tmp/savepoints/sp-1",
                Success = true,
                Error = null
            };

            // Assert
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void SavepointResult_AllProperties_CanBeSetInObjectInitializer()
        {
            // Arrange & Act
            var result = new SavepointResult
            {
                SavepointPath = "/flink/savepoints/savepoint-xyz",
                Success = false,
                TriggerId = "trigger-abc-123",
                Error = "Timeout while creating savepoint"
            };

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/flink/savepoints/savepoint-xyz"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-abc-123"));
            Assert.That(result.Error, Is.EqualTo("Timeout while creating savepoint"));
        }
    }
}
