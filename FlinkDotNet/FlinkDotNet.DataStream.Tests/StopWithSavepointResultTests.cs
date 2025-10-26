using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class StopWithSavepointResultTests
    {
        [Test]
        public void StopWithSavepointResult_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new StopWithSavepointResult();
            var savepointPath = "/tmp/savepoints/stop-savepoint-123";
            var triggerId = "stop-trigger-456";
            var error = "Failed to stop with savepoint";

            // Act
            result.SavepointPath = savepointPath;
            result.Success = true;
            result.TriggerId = triggerId;
            result.Drained = true;
            result.Error = error;

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(savepointPath));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo(triggerId));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void StopWithSavepointResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Drained, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void StopWithSavepointResult_Drained_CanBeSetToTrue()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult { Drained = true };

            // Assert
            Assert.That(result.Drained, Is.True);
        }

        [Test]
        public void StopWithSavepointResult_Error_CanBeNull()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult
            {
                SavepointPath = "/tmp/savepoints/sp-stop-1",
                Success = true,
                Error = null
            };

            // Assert
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void StopWithSavepointResult_AllProperties_CanBeSetInObjectInitializer()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult
            {
                SavepointPath = "/flink/savepoints/stop-savepoint-xyz",
                Success = false,
                TriggerId = "stop-trigger-abc-123",
                Drained = false,
                Error = "Job was not stopped"
            };

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/flink/savepoints/stop-savepoint-xyz"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo("stop-trigger-abc-123"));
            Assert.That(result.Drained, Is.False);
            Assert.That(result.Error, Is.EqualTo("Job was not stopped"));
        }

        [Test]
        public void StopWithSavepointResult_SuccessWithDrained_WorksCorrectly()
        {
            // Arrange & Act
            var result = new StopWithSavepointResult
            {
                SavepointPath = "/flink/savepoints/drained-sp",
                Success = true,
                Drained = true,
                TriggerId = "drain-trigger-1"
            };

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.Null);
        }
    }
}
