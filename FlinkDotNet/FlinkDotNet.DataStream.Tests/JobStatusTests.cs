using NUnit.Framework;
using FlinkDotNet.DataStream;
using System;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class JobStatusTests
    {
        [Test]
        public void JobStatus_Properties_CanBeSetAndRetrieved()
        {
            // Arrange
            var status = new JobStatus();
            var jobId = "test-job-789";
            var jobName = "Status Test Job";
            var state = "RUNNING";
            var parallelism = 4;
            var maxParallelism = 128;
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddMinutes(10);
            var error = "Connection timeout";

            // Act
            status.JobId = jobId;
            status.JobName = jobName;
            status.State = state;
            status.Parallelism = parallelism;
            status.MaxParallelism = maxParallelism;
            status.StartTime = startTime;
            status.EndTime = endTime;
            status.Error = error;

            // Assert
            Assert.That(status.JobId, Is.EqualTo(jobId));
            Assert.That(status.JobName, Is.EqualTo(jobName));
            Assert.That(status.State, Is.EqualTo(state));
            Assert.That(status.Parallelism, Is.EqualTo(parallelism));
            Assert.That(status.MaxParallelism, Is.EqualTo(maxParallelism));
            Assert.That(status.StartTime, Is.EqualTo(startTime));
            Assert.That(status.EndTime, Is.EqualTo(endTime));
            Assert.That(status.Error, Is.EqualTo(error));
        }

        [Test]
        public void JobStatus_DefaultValues_AreCorrect()
        {
            // Arrange & Act
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
        public void JobStatus_State_CanBeSetToCommonValues()
        {
            // Arrange
            var status = new JobStatus();
            var states = new[] { "CREATED", "RUNNING", "FINISHED", "FAILED", "CANCELED" };

            foreach (var state in states)
            {
                // Act
                status.State = state;

                // Assert
                Assert.That(status.State, Is.EqualTo(state));
            }
        }

        [Test]
        public void JobStatus_Parallelism_CanBeZero()
        {
            // Arrange & Act
            var status = new JobStatus { Parallelism = 0 };

            // Assert
            Assert.That(status.Parallelism, Is.EqualTo(0));
        }

        [Test]
        public void JobStatus_Parallelism_CanBePositive()
        {
            // Arrange & Act
            var status = new JobStatus { Parallelism = 16 };

            // Assert
            Assert.That(status.Parallelism, Is.EqualTo(16));
        }

        [Test]
        public void JobStatus_MaxParallelism_CanBeSetToHighValue()
        {
            // Arrange & Act
            var status = new JobStatus { MaxParallelism = 32768 };

            // Assert
            Assert.That(status.MaxParallelism, Is.EqualTo(32768));
        }

        [Test]
        public void JobStatus_EndTime_CanBeNull()
        {
            // Arrange & Act
            var status = new JobStatus
            {
                JobId = "running-job",
                State = "RUNNING",
                EndTime = null
            };

            // Assert
            Assert.That(status.EndTime, Is.Null);
        }

        [Test]
        public void JobStatus_Error_CanBeNull()
        {
            // Arrange & Act
            var status = new JobStatus
            {
                JobId = "successful-job",
                State = "FINISHED",
                Error = null
            };

            // Assert
            Assert.That(status.Error, Is.Null);
        }

        [Test]
        public void JobStatus_AllProperties_CanBeSetInObjectInitializer()
        {
            // Arrange
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddHours(2);

            // Act
            var status = new JobStatus
            {
                JobId = "job-xyz-123",
                JobName = "Complex Streaming Job",
                State = "FAILED",
                Parallelism = 8,
                MaxParallelism = 256,
                StartTime = startTime,
                EndTime = endTime,
                Error = "Out of memory exception"
            };

            // Assert
            Assert.That(status.JobId, Is.EqualTo("job-xyz-123"));
            Assert.That(status.JobName, Is.EqualTo("Complex Streaming Job"));
            Assert.That(status.State, Is.EqualTo("FAILED"));
            Assert.That(status.Parallelism, Is.EqualTo(8));
            Assert.That(status.MaxParallelism, Is.EqualTo(256));
            Assert.That(status.StartTime, Is.EqualTo(startTime));
            Assert.That(status.EndTime, Is.EqualTo(endTime));
            Assert.That(status.Error, Is.EqualTo("Out of memory exception"));
        }

        [Test]
        public void JobStatus_StartTime_CanBeMinValue()
        {
            // Arrange & Act
            var status = new JobStatus
            {
                StartTime = DateTime.MinValue
            };

            // Assert
            Assert.That(status.StartTime, Is.EqualTo(DateTime.MinValue));
        }

        [Test]
        public void JobStatus_EndTime_CanBeMaxValue()
        {
            // Arrange & Act
            var status = new JobStatus
            {
                EndTime = DateTime.MaxValue
            };

            // Assert
            Assert.That(status.EndTime, Is.EqualTo(DateTime.MaxValue));
        }

        [Test]
        public void JobStatus_State_CanBeEmptyString()
        {
            // Arrange & Act
            var status = new JobStatus { State = "" };

            // Assert
            Assert.That(status.State, Is.EqualTo(string.Empty));
        }
    }
}