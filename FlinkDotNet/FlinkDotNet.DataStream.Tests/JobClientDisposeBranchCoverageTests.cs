using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for JobClient Dispose pattern to achieve 100% branch coverage.
    /// The Dispose method has branching logic for double-dispose protection.
    /// </summary>
    [TestFixture]
    public class JobClientDisposeBranchCoverageTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        [Test]
        public void JobClient_Dispose_CanBeCalledOnce()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            var client = new JobClient("test-job");

            // Act - Dispose should complete without error
            client.Dispose();

            // Assert - No exception thrown
            Assert.Pass("Dispose completed successfully");
        }

        [Test]
        public void JobClient_Dispose_CanBeCalledMultipleTimes()
        {
            // Arrange
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            var client = new JobClient("test-job");

            // Act - Call Dispose multiple times (tests the _disposed branch)
            client.Dispose();
            client.Dispose(); // Should be safe to call twice
            client.Dispose(); // And three times

            // Assert - No exception thrown
            Assert.Pass("Multiple Dispose calls handled correctly");
        }

        [Test]
        public void JobClient_UsingStatement_DisposesCorrectly()
        {
            // Arrange & Act - Use 'using' statement to test automatic disposal
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");

            using (var client = new JobClient("test-job"))
            {
                // Client is in use
                Assert.That(client, Is.Not.Null);
            } // Dispose is called automatically here

            // Assert - No exception was thrown during disposal
            Assert.Pass("Using statement disposed correctly");
        }

        [Test]
        public void JobClient_NestedUsingStatements_DisposeCorrectly()
        {
            // Arrange & Act - Test nested using statements
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");

            using (var client1 = new JobClient("job1"))
            {
                using (var client2 = new JobClient("job2"))
                {
                    Assert.That(client1, Is.Not.Null);
                    Assert.That(client2, Is.Not.Null);
                } // client2 disposed
            } // client1 disposed

            // Assert
            Assert.Pass("Nested using statements disposed correctly");
        }
    }
}
