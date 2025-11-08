// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

#nullable enable

using FlinkDotNet.JobManager.Controllers;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Models.Requests;
using FlinkDotNet.JobManager.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class JobsControllerTests
{
    private readonly Mock<IDispatcher> _mockDispatcher;
    private readonly Mock<ILogger<JobsController>> _mockLogger;
    private readonly JobsController _controller;

    public JobsControllerTests()
    {
        _mockDispatcher = new Mock<IDispatcher>();
        _mockLogger = new Mock<ILogger<JobsController>>();
        _controller = new JobsController(_mockDispatcher.Object, _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobsController(null!, _mockLogger.Object);
        act.Should().Throw<ArgumentNullException>().WithParameterName("dispatcher");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobsController(_mockDispatcher.Object, null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task SubmitJob_WithValidRequest_ReturnsOkResult()
    {
        // Arrange
        var request = CreateValidSubmitJobRequest();
        var submitResult = new JobSubmissionResult
        {
            JobId = "job-123",
            Success = true
        };

        _mockDispatcher
            .Setup(d => d.SubmitJobAsync(It.IsAny<JobGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitResult);

        // Act
        var result = await _controller.SubmitJob(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as SubmitJobResponse;
        response!.JobId.Should().Be("job-123");
        response.State.Should().Be(JobExecutionState.Created);
    }

    [Fact]
    public async Task SubmitJob_WithFailedSubmission_ReturnsBadRequest()
    {
        // Arrange
        var request = CreateValidSubmitJobRequest();
        var submitResult = new JobSubmissionResult
        {
            JobId = "job-123",
            Success = false,
            ErrorMessage = "Invalid job graph"
        };

        _mockDispatcher
            .Setup(d => d.SubmitJobAsync(It.IsAny<JobGraph>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(submitResult);

        // Act
        var result = await _controller.SubmitJob(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetJobStatus_WithExistingJob_ReturnsOkResult()
    {
        // Arrange
        var jobId = "job-123";
        var jobStatus = new JobStatus
        {
            JobId = jobId,
            JobName = "Test Job",
            State = JobExecutionState.Running
        };

        _mockDispatcher
            .Setup(d => d.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobStatus);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as JobStatusResponse;
        response!.JobId.Should().Be(jobId);
        response.State.Should().Be(JobExecutionState.Running);
    }

    [Fact]
    public async Task GetJobStatus_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var jobId = "non-existent-job";

        _mockDispatcher
            .Setup(d => d.GetJobStatusAsync(jobId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobStatus?)null);

        // Act
        var result = await _controller.GetJobStatus(jobId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task ListJobs_ReturnsOkResultWithJobList()
    {
        // Arrange
        var jobs = new List<JobStatus>
        {
            new JobStatus { JobId = "job-1", JobName = "Job 1", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-2", JobName = "Job 2", State = JobExecutionState.Finished }
        };

        _mockDispatcher
            .Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.ListJobs();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as JobListResponse;
        response!.Jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task CancelJob_WithExistingJob_ReturnsOkResult()
    {
        // Arrange
        var jobId = "job-123";

        _mockDispatcher
            .Setup(d => d.CancelJobAsync(jobId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task CancelJob_WithNonExistentJob_ReturnsNotFound()
    {
        // Arrange
        var jobId = "non-existent-job";

        _mockDispatcher
            .Setup(d => d.CancelJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException($"Job {jobId} not found", nameof(jobId)));

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task CancelJob_WithException_ReturnsBadRequest()
    {
        // Arrange
        var jobId = "job-123";

        _mockDispatcher
            .Setup(d => d.CancelJobAsync(jobId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot cancel job in current state"));

        // Act
        var result = await _controller.CancelJob(jobId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task ListJobs_WithStateFilter_FiltersJobs()
    {
        // Arrange
        var jobs = new List<JobStatus>
        {
            new JobStatus { JobId = "job-1", JobName = "Job 1", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-2", JobName = "Job 2", State = JobExecutionState.Finished },
            new JobStatus { JobId = "job-3", JobName = "Job 3", State = JobExecutionState.Running }
        };

        _mockDispatcher
            .Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.ListJobs("Running");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as JobListResponse;
        response!.Jobs.Should().HaveCount(2);
        response.Jobs.Should().OnlyContain(j => j.State == JobExecutionState.Running);
    }

    [Fact]
    public async Task ListJobs_WithInvalidStateFilter_ReturnsAllJobs()
    {
        // Arrange
        var jobs = new List<JobStatus>
        {
            new JobStatus { JobId = "job-1", JobName = "Job 1", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-2", JobName = "Job 2", State = JobExecutionState.Finished }
        };

        _mockDispatcher
            .Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(jobs);

        // Act
        var result = await _controller.ListJobs("InvalidState");

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as JobListResponse;
        response!.Jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task SubmitJob_WithException_ReturnsInternalServerError()
    {
        // Arrange
        var request = CreateValidSubmitJobRequest();

        _mockDispatcher
            .Setup(d => d.SubmitJobAsync(It.IsAny<JobGraph>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        // Act
        var result = await _controller.SubmitJob(request);

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
    }

    private static SubmitJobRequest CreateValidSubmitJobRequest()
    {
        return new SubmitJobRequest
        {
            JobName = "Test Job",
            MaxParallelism = 128,
            Vertices = new List<JobVertexRequest>
            {
                new JobVertexRequest
                {
                    OperatorName = "source",
                    Parallelism = 2,
                    OperatorType = "Source"
                },
                new JobVertexRequest
                {
                    OperatorName = "map",
                    Parallelism = 2,
                    OperatorType = "Map"
                }
            },
            Edges = new List<JobEdgeRequest>
            {
                new JobEdgeRequest
                {
                    SourceVertexIndex = 0,
                    TargetVertexIndex = 1,
                    Strategy = "Forward"
                }
            }
        };
    }
}
