using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for simple data classes with 0% coverage to achieve 100% branch coverage.
    /// These are primarily simple POCOs with properties.
    /// </summary>
    [TestFixture]
    public class DataClassesBranchCoverageTests
    {
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
            result.TriggerId = "trigger-123";
            result.Error = "Some error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-123"));
            Assert.That(result.Error, Is.EqualTo("Some error"));
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
            result.SavepointPath = "/path/to/savepoint";
            result.Success = true;
            result.TriggerId = "trigger-456";
            result.Drained = true;
            result.Error = "Another error";

            // Assert
            Assert.That(result.SavepointPath, Is.EqualTo("/path/to/savepoint"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.TriggerId, Is.EqualTo("trigger-456"));
            Assert.That(result.Drained, Is.True);
            Assert.That(result.Error, Is.EqualTo("Another error"));
        }

        #endregion

        #region JobStatus Tests

        [Test]
        public void JobStatus_DefaultConstructor_InitializesProperties()
        {
            // Act
            var status = new JobStatus();

            // Assert
            Assert.That(status.FlinkJobId, Is.EqualTo(string.Empty));
            Assert.That(status.JobName, Is.EqualTo(string.Empty));
            Assert.That(status.State, Is.EqualTo(string.Empty));
            Assert.That(status.Parallelism, Is.EqualTo(0));
        }

        [Test]
        public void JobStatus_SetProperties_StoresValues()
        {
            // Arrange
            var status = new JobStatus();

            // Act
            status.FlinkJobId = "job-789";
            status.JobName = "Test Job";
            status.State = "RUNNING";
            status.Parallelism = 4;

            // Assert
            Assert.That(status.FlinkJobId, Is.EqualTo("job-789"));
            Assert.That(status.JobName, Is.EqualTo("Test Job"));
            Assert.That(status.State, Is.EqualTo("RUNNING"));
            Assert.That(status.Parallelism, Is.EqualTo(4));
        }

        #endregion

        #region JobExecutionResult Tests

        [Test]
        public void JobExecutionResult_DefaultConstructor_InitializesProperties()
        {
            // Act
            var result = new JobExecutionResult();

            // Assert
            Assert.That(result.JobName, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
        }

        [Test]
        public void JobExecutionResult_SetProperties_StoresValues()
        {
            // Arrange
            var result = new JobExecutionResult();

            // Act
            result.JobName = "exec-job-123";
            result.Success = true;

            // Assert
            Assert.That(result.JobName, Is.EqualTo("exec-job-123"));
            Assert.That(result.Success, Is.True);
        }

        #endregion

        // CapturedOperation is internal and cannot be tested directly from test assembly
        // WindowDefinition is internal and cannot be tested directly from test assembly
    }
}
