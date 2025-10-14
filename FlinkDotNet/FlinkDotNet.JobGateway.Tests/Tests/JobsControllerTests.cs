using System.Text;
using Flink.JobBuilder.Models;
using FlinkDotNet.JobGateway.Controllers;
using FlinkDotNet.JobGateway.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace FlinkDotNet.JobGateway.Tests
{
    [TestFixture]
    public class JobsControllerTests
    {
        private Mock<IFlinkJobManager> _mockJobManager = null!;
        private Mock<ILogger<JobsController>> _mockLogger = null!;

        [SetUp]
        public void Setup()
        {
            _mockJobManager = new Mock<IFlinkJobManager>();
            _mockLogger = new Mock<ILogger<JobsController>>();
        }

        #region SubmitJob Tests

        [Test]
        public async Task SubmitJob_WithEmptyBody_ReturnsBadRequest()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(""));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequest = (BadRequestObjectResult)result.Result!;
            Assert.That(badRequest.Value, Is.Not.Null);
        }

        [Test]
        public async Task SubmitJob_WithWhitespaceBody_ReturnsBadRequest()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("   "));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SubmitJob_WithInvalidJson_ReturnsBadRequest()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("{ invalid json }"));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SubmitJob_WithNullDeserialization_ReturnsBadRequest()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes("null"));
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task SubmitJob_WithValidJobDefinition_ReturnsOk()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "test-job-1",
                    JobName = "Test Job",
                    Version = "1.0"
                },
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic",
                    BootstrapServers = "localhost:9092"
                },
                Sink = new ConsoleSinkDefinition()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jobDefinition, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var expectedResult = JobSubmissionResult.CreateSuccess("test-job-1", "flink-job-1");
            _mockJobManager
                .Setup(m => m.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult)result.Result!;
            var submissionResult = (JobSubmissionResult)okResult.Value!;
            Assert.That(submissionResult.IsSuccess, Is.True);
            Assert.That(submissionResult.JobId, Is.EqualTo("test-job-1"));
        }

        [Test]
        public async Task SubmitJob_WithJobDefinitionMissingMetadata_CreatesMetadata()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            
            var jobDefinition = new JobDefinition
            {
                Source = new KafkaSourceDefinition
                {
                    Topic = "test-topic"
                },
                Sink = new ConsoleSinkDefinition()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jobDefinition, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var expectedResult = JobSubmissionResult.CreateSuccess("generated-id", "flink-job-1");
            _mockJobManager
                .Setup(m => m.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            _mockJobManager.Verify(m => m.SubmitJobAsync(It.Is<JobDefinition>(
                j => j.Metadata != null && !string.IsNullOrEmpty(j.Metadata.JobId)
            )), Times.Once);
        }

        [Test]
        public async Task SubmitJob_WithSqlSourceAndNoSink_Succeeds()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "sql-job-1",
                    Version = "1.0"
                },
                Source = new SqlSourceDefinition
                {
                    Statements = new List<string> { "CREATE TABLE test_table ..." }
                },
                Sink = null
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jobDefinition, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var expectedResult = JobSubmissionResult.CreateSuccess("sql-job-1", "flink-sql-job-1");
            _mockJobManager
                .Setup(m => m.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        }

        [Test]
        public async Task SubmitJob_WhenFlinkJobManagerFails_ReturnsBadRequest()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "failing-job",
                    Version = "1.0"
                },
                Source = new KafkaSourceDefinition { Topic = "test" },
                Sink = new ConsoleSinkDefinition()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jobDefinition, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            var failureResult = JobSubmissionResult.CreateFailure("failing-job", "Flink cluster unreachable");
            _mockJobManager
                .Setup(m => m.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ReturnsAsync(failureResult);

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            var badRequest = (BadRequestObjectResult)result.Result!;
            var submissionResult = (JobSubmissionResult)badRequest.Value!;
            Assert.That(submissionResult.IsSuccess, Is.False);
            Assert.That(submissionResult.ErrorMessage, Is.EqualTo("Flink cluster unreachable"));
        }

        [Test]
        public async Task SubmitJob_WhenFlinkJobManagerThrowsException_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            
            var jobDefinition = new JobDefinition
            {
                Metadata = new JobMetadata
                {
                    JobId = "exception-job",
                    Version = "1.0"
                },
                Source = new KafkaSourceDefinition { Topic = "test" },
                Sink = new ConsoleSinkDefinition()
            };

            var json = System.Text.Json.JsonSerializer.Serialize(jobDefinition, new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            });

            var httpContext = new DefaultHttpContext();
            httpContext.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(json));
            httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Loopback;
            controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

            _mockJobManager
                .Setup(m => m.SubmitJobAsync(It.IsAny<JobDefinition>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await controller.SubmitJob();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<ObjectResult>());
            var objectResult = (ObjectResult)result.Result!;
            Assert.That(objectResult.StatusCode, Is.EqualTo(500));
            var submissionResult = (JobSubmissionResult)objectResult.Value!;
            Assert.That(submissionResult.IsSuccess, Is.False);
            Assert.That(submissionResult.ErrorMessage, Does.Contain("Internal server error"));
        }

        #endregion

        #region GetJobStatus Tests

        [Test]
        public async Task GetJobStatus_WithValidJobId_ReturnsOkResult()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            var jobId = "test-job-1";
            var expectedStatus = new JobStatus
            {
                FlinkJobId = jobId,
                State = "RUNNING"
            };

            _mockJobManager
                .Setup(m => m.GetJobStatusAsync(jobId))
                .ReturnsAsync(expectedStatus);

            // Act
            var result = await controller.GetJobStatus(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult) result.Result!;
            var actualStatus = (JobStatus) okResult.Value!;
            Assert.That(actualStatus.FlinkJobId, Is.EqualTo(jobId));
            Assert.That(actualStatus.State, Is.EqualTo("RUNNING"));
        }

        [Test]
        public async Task GetJobStatus_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            var jobId = "non-existent-job";

            _mockJobManager
                .Setup(m => m.GetJobStatusAsync(jobId))
                .ReturnsAsync((JobStatus) null!);

            // Act
            var result = await controller.GetJobStatus(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetJobStatus_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var jobId = "test-job-1";

            _mockJobManager
                .Setup(m => m.GetJobStatusAsync(jobId))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            var result = await controller.GetJobStatus(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<StatusCodeResult>());
            var statusCodeResult = (StatusCodeResult)result.Result!;
            Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region GetJobMetrics Tests

        [Test]
        public async Task GetJobMetrics_WithValidJobId_ReturnsOkResult()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            var jobId = "test-job-1";
            var expectedMetrics = new JobMetrics
            {
                RecordsIn = 1000,
                RecordsOut = 950
            };

            _mockJobManager
                .Setup(m => m.GetJobMetricsAsync(jobId))
                .ReturnsAsync(expectedMetrics);

            // Act
            var result = await controller.GetJobMetrics(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult) result.Result!;
            var actualMetrics = (JobMetrics) okResult.Value!;
            Assert.That(actualMetrics.RecordsIn, Is.EqualTo(1000));
        }

        [Test]
        public async Task GetJobMetrics_WithInvalidJobId_ReturnsNotFound()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var jobId = "non-existent-job";

            _mockJobManager
                .Setup(m => m.GetJobMetricsAsync(jobId))
                .ReturnsAsync((JobMetrics)null!);

            // Act
            var result = await controller.GetJobMetrics(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task GetJobMetrics_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var jobId = "test-job-1";

            _mockJobManager
                .Setup(m => m.GetJobMetricsAsync(jobId))
                .ThrowsAsync(new Exception("Metrics service unavailable"));

            // Act
            var result = await controller.GetJobMetrics(jobId);

            // Assert
            Assert.That(result.Result, Is.InstanceOf<StatusCodeResult>());
            var statusCodeResult = (StatusCodeResult)result.Result!;
            Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region CancelJob Tests

        [Test]
        public async Task CancelJob_WithValidJobId_ReturnsOk()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            var jobId = "test-job-1";

            _mockJobManager
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
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            var jobId = "non-existent-job";

            _mockJobManager
                .Setup(m => m.CancelJobAsync(jobId))
                .ReturnsAsync(false);

            // Act
            var result = await controller.CancelJob(jobId);

            // Assert
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }

        [Test]
        public async Task CancelJob_WhenExceptionThrown_ReturnsInternalServerError()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);
            var jobId = "test-job-1";

            _mockJobManager
                .Setup(m => m.CancelJobAsync(jobId))
                .ThrowsAsync(new Exception("Cancellation service failed"));

            // Act
            var result = await controller.CancelJob(jobId);

            // Assert
            Assert.That(result, Is.InstanceOf<StatusCodeResult>());
            var statusCodeResult = (StatusCodeResult)result;
            Assert.That(statusCodeResult.StatusCode, Is.EqualTo(500));
        }

        #endregion

        #region HealthCheck Tests

        [Test]
        public void HealthCheck_ReturnsOk()
        {
            // Arrange
            var controller = new JobsController(_mockLogger.Object, _mockJobManager.Object);

            // Act
            var result = controller.HealthCheck();

            // Assert
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            var okResult = (OkObjectResult) result.Result!;
            Assert.That(okResult.Value, Is.EqualTo("OK"));
        }

        #endregion
    }
}
