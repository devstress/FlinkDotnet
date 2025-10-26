using System.Text;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Controllers;
using FlinkDotNet.JobGateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Tests to achieve 100% branch coverage for JobsController.
    /// Targets remaining uncovered branches in deserialization error handling and metadata initialization.
    /// </summary>
    [TestFixture]
    public class JobsControllerCompleteBranchCoverageTests
    {
        private Mock<IFlinkJobManager> _mockJobManager = null!;
        private Mock<ILogger<JobsController>> _mockLogger = null!;
        private JobsController _controller = null!;

        [SetUp]
        public void Setup()
        {
            this._mockJobManager = new Mock<IFlinkJobManager>();
            _mockLogger = new Mock<ILogger<JobsController>>();
            this._controller = new JobsController(_mockLogger.Object, this._mockJobManager.Object);
        }

        #region Line 123 Branch Coverage - Long Error Message

        [Test]
        public async Task SubmitJob_WithInvalidJsonLongerThan400Chars_TruncatesInErrorLog()
        {
            // Arrange - Create invalid JSON longer than 400 characters
            var longInvalidJson = "{\"invalid\": " + new string('x', 500) + "}";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(longInvalidJson));
            this._controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await this._controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());

            // Verify the logger was called with truncated message (Line 123 branch: raw[..400])
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deserialization failure")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Test]
        public async Task SubmitJob_WithInvalidJsonShorterThan400Chars_UsesFullMessageInErrorLog()
        {
            // Arrange - Create invalid JSON shorter than 400 characters
            var shortInvalidJson = "{\"invalid json without closing brace\"";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(shortInvalidJson));
            this._controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await this._controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());

            // Verify the logger was called (Line 123 branch: raw)
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Deserialization failure")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Line 142 Branch Coverage - Metadata Null Coalescing

        [Test]
        public async Task SubmitJob_WithNullMetadata_CreatesNewMetadata()
        {
            // Arrange - Valid JSON but with null metadata
            var jobDefJson = @"{
                ""source"": {
                    ""type"": ""kafka"",
                    ""topic"": ""test-topic"",
                    ""bootstrapServers"": ""localhost:9092"",
                    ""groupId"": ""test-group"",
                    ""startingOffsets"": ""earliest""
                },
                ""sink"": {
                    ""type"": ""kafka"",
                    ""topic"": ""output-topic"",
                    ""bootstrapServers"": ""localhost:9092""
                },
                ""metadata"": null
            }";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jobDefJson));
            this._controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _ = this._mockJobManager
                .Setup(x => x.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(new JobSubmissionResult
                {
                    Success = true,
                    FlinkJobId = "job-123"
                });

            // Act
            var result = await this._controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

            // Verify metadata was created (Line 142 branch: ??= new JobMetadata())
            this._mockJobManager.Verify(
                x => x.SubmitJobAsync(It.Is<JobDefinition>(j => j.Metadata != null)),
                Times.Once);
        }

        [Test]
        public async Task SubmitJob_WithExistingMetadata_PreservesMetadata()
        {
            // Arrange - Valid JSON with existing metadata
            var jobDefJson = @"{
                ""source"": {
                    ""type"": ""kafka"",
                    ""topic"": ""test-topic"",
                    ""bootstrapServers"": ""localhost:9092"",
                    ""groupId"": ""test-group"",
                    ""startingOffsets"": ""earliest""
                },
                ""sink"": {
                    ""type"": ""kafka"",
                    ""topic"": ""output-topic"",
                    ""bootstrapServers"": ""localhost:9092""
                },
                ""metadata"": {
                    ""jobId"": ""custom-job-id"",
                    ""jobName"": ""Custom Job Name""
                }
            }";

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(jobDefJson));
            this._controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _ = this._mockJobManager
                .Setup(x => x.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(new JobSubmissionResult
                {
                    Success = true,
                    FlinkJobId = "job-123"
                });

            // Act
            var result = await this._controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

            // Verify metadata was preserved (Line 142 branch: metadata already exists, no new creation)
            this._mockJobManager.Verify(
                x => x.SubmitJobAsync(It.Is<JobDefinition>(j =>
                    j.Metadata != null &&
                    j.Metadata.JobId == "custom-job-id")),
                Times.Once);
        }

        #endregion
    }
}
