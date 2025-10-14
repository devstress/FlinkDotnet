using NUnit.Framework;
using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    /// <summary>
    /// Tests for small DataStream classes to achieve 100% coverage
    /// Chunk 1: CapturedOperation, WindowDefinition, JobExecutionResult, SavepointResult, 
    /// StopWithSavepointResult, JobStatus, JobClient
    /// </summary>
    [TestFixture]
    public class DataStreamSmallClassesTests
    {
        #region JobExecutionResult Tests

        [Test]
        public void JobExecutionResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new JobExecutionResult();

            // Assert
            Assert.That(result.JobId, Is.EqualTo(string.Empty));
            Assert.That(result.JobName, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void JobExecutionResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new JobExecutionResult();
            var startTime = System.DateTime.UtcNow.AddMinutes(-5);
            var endTime = System.DateTime.UtcNow;

            // Act
            result.JobId = "job-123";
            result.JobName = "Test Job";
            result.Success = true;
            result.StartTime = startTime;
            result.EndTime = endTime;
            result.Error = "Test error";

            // Assert
            Assert.That(result.JobId, Is.EqualTo("job-123"));
            Assert.That(result.JobName, Is.EqualTo("Test Job"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartTime, Is.EqualTo(startTime));
            Assert.That(result.EndTime, Is.EqualTo(endTime));
            Assert.That(result.Error, Is.EqualTo("Test error"));
        }

        #endregion

        #region SavepointResult Tests

        [Test]
        public void SavepointResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new SavepointResult();

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.TriggerId, Is.EqualTo(string.Empty));
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void SavepointResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new SavepointResult();

            // Act
            result.SavepointPath = "/path/to/savepoint";
            result.Success = true;
            result.TriggerId = "trigger-456";
            result.Error = "Test error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
            Assert.That(result.Error, Is.EqualTo("Test error"));
        }

        #endregion

        #region StopWithSavepointResult Tests

        [Test]
        public void StopWithSavepointResult_DefaultConstructor_InitializesProperties()
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

        [Test]
        public void StopWithSavepointResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new StopWithSavepointResult();

            // Act
            result.SavepointPath = "/path/to/stop/savepoint";
            result.Success = true;
            result.TriggerId = "trigger-789";
            result.Drained = true;
            result.Error = "Stop error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/stop/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-789"));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.EqualTo("Stop error"));
        }

        #endregion

        #region JobStatus Tests

        [Test]
        public void JobStatus_DefaultConstructor_InitializesProperties()
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

        [Test]
        public void JobStatus_SetProperties_StoresValues()
        {
            // Arrange
            var status = new JobStatus();
            var startTime = System.DateTime.UtcNow.AddMinutes(-10);
            var endTime = System.DateTime.UtcNow;

            // Act
            status.JobId = "status-job-123";
            status.JobName = "Status Test Job";
            status.State = "RUNNING";
            status.Parallelism = 4;
            status.MaxParallelism = 8;
            status.StartTime = startTime;
            status.EndTime = endTime;
            status.Error = "Status error";

            // Assert
            Assert.That(status.JobId, Is.EqualTo("status-job-123"));
            Assert.That(status.JobName, Is.EqualTo("Status Test Job"));
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Parallelism, Is.EqualTo(4));
            Assert.That(status.MaxParallelism, Is.EqualTo(8));
            Assert.That(status.StartTime, Is.EqualTo(startTime));
            Assert.That(status.EndTime, Is.EqualTo(endTime));
            Assert.That(status.Error, Is.EqualTo("Status error"));
        }

        [Test]
        public void JobStatus_WithNullEndTime_StoresNull()
        {
            // Arrange
            var status = new JobStatus
            {
                JobId = "job-456",
                State = "RUNNING",
                EndTime = null
            };

            // Assert
            Assert.That(status.EndTime, Is.Null);
        }

        #endregion

        #region JobClient Tests

        [Test]
        public void JobClient_Constructor_InitializesJobName()
        {
            // Arrange
            var jobName = "Test Flink Job";

            // Act
            using var client = new JobClient(jobName);

            // Assert
            Assert.That(client.JobName, Is.EqualTo(jobName));
            Assert.That(client.JobId, Is.EqualTo(string.Empty));
        }

        [Test]
        public void JobClient_GetJobId_ReturnsJobId()
        {
            // Arrange
            using var client = new JobClient("Test Job");
            client.JobId = "test-job-id-123";

            // Act
            var jobId = client.GetJobId();

            // Assert
            Assert.That(jobId, Is.EqualTo("test-job-id-123"));
        }

        [Test]
        public void JobClient_SetJobId_UpdatesJobId()
        {
            // Arrange
            using var client = new JobClient("Test Job");

            // Act
            client.JobId = "new-job-id-456";

            // Assert
            Assert.That(client.JobId, Is.EqualTo("new-job-id-456"));
            Assert.That(client.GetJobId(), Is.EqualTo("new-job-id-456"));
        }

        [Test]
        public void JobClient_Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            var client = new JobClient("Test Job");

            // Act & Assert - should not throw
            Assert.DoesNotThrow(() => client.Dispose());
            Assert.DoesNotThrow(() => client.Dispose());
        }

        [Test]
        public void JobClient_ImplementsIJobClient()
        {
            // Arrange & Act
            using var client = new JobClient("Test Job");

            // Assert
            Assert.That(client, Is.InstanceOf<IJobClient>());
        }

        [Test]
        public void JobClient_ImplementsIDisposable()
        {
            // Arrange & Act
            using var client = new JobClient("Test Job");

            // Assert
            Assert.That(client, Is.InstanceOf<IDisposable>());
        }

        #endregion
    }
}
