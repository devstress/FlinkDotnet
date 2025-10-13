using NUnit.Framework;
using Moq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using FlinkDotNet.JobGateway.Controllers;
using FlinkDotNet.JobGateway.Services;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.JobGateway.Tests
{
    [TestFixture]
    public class JobsControllerTests
    {
        [Test]
        public async Task GetJobStatus_WithValidJobId_ReturnsOkResult()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);
            
            var jobId = "test-job-1";
            var expectedStatus = new JobStatus
            {
                FlinkJobId = jobId,
                State = "RUNNING"
            };

            mockJobManager
                .Setup(m => m.GetJobStatusAsync(jobId))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await controller.GetJobStatus(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var actualStatus = (JobStatus)okResult.Value!;
            Assert.That(actualStatus.FlinkJobId, Is.EqualTo(jobId));
            Assert.That(actualStatus.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatus_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);
            
            var jobId = "non-existent-job";

            mockJobManager
                .Setup(m => m.GetJobStatusAsync(jobId))
                .ReturnsAsync((JobStatus)null!);

            // Act
            var result = await controller.GetJobStatus(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetJobMetrics_WithValidJobId_ReturnsOkResult()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);
            
            var jobId = "test-job-1";
            var expectedMetrics = new JobMetrics
            {
                RecordsIn = 1000,
                RecordsOut = 950
            };

            mockJobManager
                .Setup(m => m.GetJobMetricsAsync(jobId))
                .ReturnsAsync(expectedMetrics);

            // Act
            var result = await controller.GetJobMetrics(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var actualMetrics = (JobMetrics)okResult.Value!;
            Assert.That(actualMetrics.RecordsIn, Is.EqualTo(1000));
        }

        [Test]
        public async Task CancelJob_WithValidJobId_ReturnsOk()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);
            
            var jobId = "test-job-1";

            mockJobManager
                .Setup(m => m.CancelJobAsync(jobId))
                .ReturnsAsync(true);

            // Act
            var result = await controller.CancelJob(jobId);

            // Assert
            Assert.That(result, Is.InstanceOf<OkResult>());
        }

        [Test]
        public async Task CancelJob_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);
            
            var jobId = "non-existent-job";

            mockJobManager
                .Setup(m => m.CancelJobAsync(jobId))
                .ReturnsAsync(false);

            // Act
            var result = await controller.CancelJob(jobId);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public void HealthCheck_ReturnsOk()
        {
            // Arrange
            var mockJobManager = new Mock<IFlinkJobManager>();
            var mockLogger = new Mock<ILogger<JobsController>>();
            var controller = new JobsController(mockLogger.Object, mockJobManager.Object);

            // Act
            var result = controller.HealthCheck();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            Assert.That(okResult.Value, Is.EqualTo("OK"));
        }
    }
}
