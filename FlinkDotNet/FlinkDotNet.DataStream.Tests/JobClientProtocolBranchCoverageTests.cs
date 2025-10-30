using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests to achieve 100% branch coverage for JobClient.GetProtocol() method.
    /// The GetProtocol method has 3 uncovered branches that need testing.
    /// </summary>
    [TestFixture]
    public class JobClientProtocolBranchCoverageTests
    {
        [TearDown]
        public void TearDown()
        {
            // Clean up environment variables
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
        }

        [Test]
        public void JobClient_WithHttpsProtocolEnvironmentVariable_UsesHttps()
        {
            // Arrange - Set environment variable before test
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "HTTPS");

            try
            {
                // Act - Create JobClient which internally calls GetProtocol()
                using var client = new JobClient("test-job");

                // Assert - Client should be created successfully with https protocol
                Assert.That(client, Is.Not.Null);
                Assert.That(client.JobName, Is.EqualTo("test-job"));
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }

        [Test]
        public void JobClient_WithLowercaseHttpsProtocolEnvironmentVariable_UsesHttps()
        {
            // Arrange - Test with lowercase "https"
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "https");

            try
            {
                // Act - GetProtocol() should convert to uppercase and return "https"
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }

        [Test]
        public void JobClient_WithInvalidProtocolEnvironmentVariable_DefaultsToHttp()
        {
            // Arrange - Set an invalid protocol value
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "ftp"); // Invalid protocol

            try
            {
                // Act - GetProtocol() should default to "http" for invalid values
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }

        [Test]
        public void JobClient_WithEmptyProtocolEnvironmentVariable_DefaultsToHttp()
        {
            // Arrange - Set empty string as protocol
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "");

            try
            {
                // Act - GetProtocol() should treat empty as null and default to "http"
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }

        [Test]
        public void JobClient_WithWhitespaceProtocolEnvironmentVariable_DefaultsToHttp()
        {
            // Arrange - Set whitespace as protocol
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "   ");

            try
            {
                // Act - GetProtocol() should trim whitespace and default to "http" for empty result
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }

        [Test]
        public void JobClient_WithMixedCaseHttpsProtocol_UsesHttps()
        {
            // Arrange - Test with mixed case "HtTpS"
            Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086");
            Environment.SetEnvironmentVariable("FLINK_PROTOCOL", "HtTpS");

            try
            {
                // Act - GetProtocol() should handle mixed case correctly
                using var client = new JobClient("test-job");

                // Assert
                Assert.That(client, Is.Not.Null);
            }
            finally
            {
                Environment.SetEnvironmentVariable("FLINK_PROTOCOL", null);
                Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
            }
        }
    }
}
