using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class JobExecutionResultTests
    {
        [Test]
        public void JobExecutionResult_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var result = new JobExecutionResult();
            var jobName = "Test Job";
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(5);
            var error = "Test error message";

            // Act
            result.JobName = jobName;
            result.Success = true;
            result.StartTime = startTime;
            result.EndTime = endTime;
            result.Error = error;

            // Assert
            Assert.That(result.JobName, Is.EqualTo(jobName));
            Assert.That(result.Success, Is.True);
            Assert.That(result.StartTime, Is.EqualTo(startTime));
            Assert.That(result.EndTime, Is.EqualTo(endTime));
            Assert.That(result.Error, Is.EqualTo(error));
        }

        [Test]
        public void JobExecutionResult_DefaultValues_AreCorrect()
        {
            // Arrange & Act
            var result = new JobExecutionResult();

            // Assert
            Assert.That(result.JobName, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void JobExecutionResult_Success_CanBeSetToFalse()
        {
            // Arrange
            var result = new JobExecutionResult { Success = true };

            // Act
            result.Success = false;

            // Assert
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void JobExecutionResult_Error_CanBeNull()
        {
            // Arrange & Act
            var result = new JobExecutionResult
            {
                                Success = true,
                Error = null
            };

            // Assert
            Assert.That(result.Error, Is.Null);
        }

        [Test]
        public void JobExecutionResult_AllProperties_CanBeSetInObjectInitializer()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddHours(1);

            // Act
            var result = new JobExecutionResult
            {
                                JobName = "Integration Test Job",
                Success = false,
                StartTime = startTime,
                EndTime = endTime,
                Error = "Job failed due to timeout"
            };

            // Assert
            Assert.That(result.JobName, Is.EqualTo("Integration Test Job"));
            Assert.That(result.Success, Is.False);
            Assert.That(result.StartTime, Is.EqualTo(startTime));
            Assert.That(result.EndTime, Is.EqualTo(endTime));
            Assert.That(result.Error, Is.EqualTo("Job failed due to timeout"));
        }

        [Test]
        public void JobExecutionResult_StartTime_CanBeMinValue()
        {
            // Arrange & Act
            var result = new JobExecutionResult
            {
                StartTime = DateTime.MinValue
            };

            // Assert
            Assert.That(result.StartTime, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void JobExecutionResult_EndTime_CanBeMaxValue()
        {
            // Arrange & Act
            var result = new JobExecutionResult
            {
                EndTime = DateTime.MaxValue
            };

            // Assert
            Assert.That(result.EndTime, Is.EqualTo(DateTime.MaxValue));
        }
    }
}
