using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for model classes property getters and setters to achieve 100% coverage.
    /// These tests ensure all auto-properties are exercised.
    /// </summary>
    [TestFixture]
    public class ModelPropertyTests
    {
        [SetUp]
        public void SetUp()
        {
            // Set environment variable required by FlinkJobGatewayConfiguration
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8080");
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        #region JobExecutionResult Tests

        [Test]
        public void JobExecutionResult_JobId_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();
            var expected = "test-job-id-123";

            // Act
            result.JobId = expected;

            // Assert
            Assert.That(result.JobId, Is.EqualTo(expected));
        }

        [Test]
        public void JobExecutionResult_JobName_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();
            var expected = "Test Job Name";

            // Act
            result.JobName = expected;

            // Assert
            Assert.That(result.JobName, Is.EqualTo(expected));
        }

        [Test]
        public void JobExecutionResult_Success_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();

            // Act
            result.Success = true;

            // Assert
            Assert.That(result.Success, Is.True);

            // Act
            result.Success = false;

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void JobExecutionResult_StartTime_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();
            var expected = DateTime.UtcNow.AddMinutes(-5);

            // Act
            result.StartTime = expected;

            // Assert
            Assert.That(result.StartTime, Is.EqualTo(expected));
        }

        [Test]
        public void JobExecutionResult_EndTime_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();
            var expected = DateTime.UtcNow;

            // Act
            result.EndTime = expected;

            // Assert
            Assert.That(result.EndTime, Is.EqualTo(expected));
        }

        [Test]
        public void JobExecutionResult_Error_ShouldGetAndSet()
        {
            // Arrange
            var result = new JobExecutionResult();
            var expected = "Test error message";

            // Act
            result.Error = expected;

            // Assert
            Assert.That(result.Error, Is.EqualTo(expected));
        }

        [Test]
        public void JobExecutionResult_Error_ShouldAcceptNull()
        {
            // Arrange
            var result = new JobExecutionResult { Error = "Initial error" };

            // Act
            result.Error = null;

            // Assert
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void JobExecutionResult_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var result = new JobExecutionResult();

            // Assert
            Assert.That(result.JobId, Is.EqualTo(string.Empty));
            Assert.That(result.JobName, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        #endregion

        #region JobStatus Tests

        [Test]
        public void JobStatus_JobId_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = "test-job-id-456";

            // Act
            status.JobId = expected;

            // Assert
            Assert.That(status.JobId, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_JobName_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = "Test Job Status Name";

            // Act
            status.JobName = expected;

            // Assert
            Assert.That(status.JobName, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_State_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = "RUNNING";

            // Act
            status.State = expected;

            // Assert
            Assert.That(status.State, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_Parallelism_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = 4;

            // Act
            status.Parallelism = expected;

            // Assert
            Assert.That(status.Parallelism, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_MaxParallelism_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = 128;

            // Act
            status.MaxParallelism = expected;

            // Assert
            Assert.That(status.MaxParallelism, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_StartTime_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = DateTime.UtcNow.AddMinutes(-10);

            // Act
            status.StartTime = expected;

            // Assert
            Assert.That(status.StartTime, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_EndTime_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = DateTime.UtcNow;

            // Act
            status.EndTime = expected;

            // Assert
            Assert.That(status.EndTime, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_EndTime_ShouldAcceptNull()
        {
            // Arrange
            var status = new JobStatus { EndTime = DateTime.UtcNow };

            // Act
            status.EndTime = null;

            // Assert
            Assert.That(status.EndTime, Is.Null);
        }

        [Test]
        public void JobStatus_Error_ShouldGetAndSet()
        {
            // Arrange
            var status = new JobStatus();
            var expected = "Job failed due to network error";

            // Act
            status.Error = expected;

            // Assert
            Assert.That(status.Error, Is.EqualTo(expected));
        }

        [Test]
        public void JobStatus_Error_ShouldAcceptNull()
        {
            // Arrange
            var status = new JobStatus { Error = "Initial error" };

            // Act
            status.Error = null;

            // Assert
            Assert.That(status.Error, Is.Null);
        }

        [Test]
        public void JobStatus_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var status = new JobStatus();

            // Assert
            Assert.That(status.JobId, Is.EqualTo(string.Empty));
            Assert.That(status.JobName, Is.EqualTo(string.Empty));
            Assert.That(status.State, Is.EqualTo(string.Empty));
            Assert.That(status.Parallelism, Is.EqualTo(0));
            Assert.That(status.MaxParallelism, Is.EqualTo(0));
            Assert.That(status.EndTime, Is.Null);
            Assert.That(status.Error, Is.Null);
        }

        #endregion

        #region SavepointResult Tests

        [Test]
        public void SavepointResult_SavepointPath_ShouldGetAndSet()
        {
            // Arrange
            var result = new SavepointResult();
            var expected = "/path/to/savepoint";

            // Act
            result.SavepointPath = expected;

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(expected));
        }

        [Test]
        public void SavepointResult_Success_ShouldGetAndSet()
        {
            // Arrange
            var result = new SavepointResult();

            // Act
            result.Success = true;

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void SavepointResult_TriggerId_ShouldGetAndSet()
        {
            // Arrange
            var result = new SavepointResult();
            var expected = "trigger-123-abc";

            // Act
            result.TriggerId = expected;

            // Assert
            Assert.That(result.TriggerId, Is.EqualTo(expected));
        }

        [Test]
        public void SavepointResult_Error_ShouldGetAndSet()
        {
            // Arrange
            var result = new SavepointResult();
            var expected = "Savepoint failed";

            // Act
            result.Error = expected;

            // Assert
            Assert.That(result.Error, Is.EqualTo(expected));
        }

        [Test]
        public void SavepointResult_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var result = new SavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Error, Is.Null);
        }

        #endregion

        #region StopWithSavepointResult Tests

        [Test]
        public void StopWithSavepointResult_SavepointPath_ShouldGetAndSet()
        {
            // Arrange
            var result = new StopWithSavepointResult();
            var expected = "/path/to/stop/savepoint";

            // Act
            result.SavepointPath = expected;

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(expected));
        }

        [Test]
        public void StopWithSavepointResult_Success_ShouldGetAndSet()
        {
            // Arrange
            var result = new StopWithSavepointResult();

            // Act
            result.Success = true;

            // Assert
            Assert.That(result.Success, Is.True);
        }

        [Test]
        public void StopWithSavepointResult_TriggerId_ShouldGetAndSet()
        {
            // Arrange
            var result = new StopWithSavepointResult();
            var expected = "stop-trigger-456";

            // Act
            result.TriggerId = expected;

            // Assert
            Assert.That(result.TriggerId, Is.EqualTo(expected));
        }

        [Test]
        public void StopWithSavepointResult_Drained_ShouldGetAndSet()
        {
            // Arrange
            var result = new StopWithSavepointResult();

            // Act
            result.Drained = true;

            // Assert
            Assert.That(result.Drained, Is.True);
        }

        [Test]
        public void StopWithSavepointResult_Error_ShouldGetAndSet()
        {
            // Arrange
            var result = new StopWithSavepointResult();
            var expected = "Stop with savepoint failed";

            // Act
            result.Error = expected;

            // Assert
            Assert.That(result.Error, Is.EqualTo(expected));
        }

        [Test]
        public void StopWithSavepointResult_DefaultValues_ShouldBeCorrect()
        {
            // Act
            var result = new StopWithSavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Drained, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        #endregion

        #region JobClient Property Tests

        [Test]
        public void JobClient_JobName_ShouldGetAndSet()
        {
            // Arrange
            using var client = new JobClient("Initial Name");
            var expected = "Updated Job Name";

            // Act
            client.JobName = expected;

            // Assert
            Assert.That(client.JobName, Is.EqualTo(expected));
        }

        [Test]
        public void JobClient_JobId_ShouldGetAndSet()
        {
            // Arrange
            using var client = new JobClient("Test Job");
            var expected = "job-id-789";

            // Act
            client.JobId = expected;

            // Assert
            Assert.That(client.JobId, Is.EqualTo(expected));
        }

        [Test]
        public void JobClient_GetJobId_ShouldReturnJobId()
        {
            // Arrange
            using var client = new JobClient("Test Job");
            var expected = "job-id-abc-123";
            client.JobId = expected;

            // Act
            var actual = client.GetJobId();

            // Assert
            Assert.That(actual, Is.EqualTo(expected));
        }

        #endregion
    }
}
