// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Tests;

public class DispatcherTests
{
    private readonly Mock<IResourceManager> _mockResourceManager;
    private readonly Mock<ITemporalClient> _mockTemporalClient;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly Mock<ILogger<JobMaster>> _mockLogger;
    private readonly Dispatcher _dispatcher;

    public DispatcherTests()
    {
        _mockResourceManager = new Mock<IResourceManager>();
        _mockTemporalClient = new Mock<ITemporalClient>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLogger = new Mock<ILogger<JobMaster>>();
        
        _mockLoggerFactory
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(_mockLogger.Object);

        _dispatcher = new Dispatcher(
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLoggerFactory.Object);
    }

    [Fact]
    public void Constructor_WithNullResourceManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new Dispatcher(
            null!,
            _mockTemporalClient.Object,
            _mockLoggerFactory.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceManager");
    }

    [Fact]
    public void Constructor_WithNullTemporalClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new Dispatcher(
            _mockResourceManager.Object,
            null!,
            _mockLoggerFactory.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("temporalClient");
    }

    [Fact]
    public void Constructor_WithNullLoggerFactory_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new Dispatcher(
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            null!);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("loggerFactory");
    }

    [Fact]
    public async Task SubmitJobAsync_WithNullJobGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = async () => await _dispatcher.SubmitJobAsync(null!);
        await act.Should().ThrowAsync<ArgumentNullException>().WithParameterName("jobGraph");
    }

    [Fact]
    public async Task SubmitJobAsync_WithValidJobGraph_ReturnsSuccess()
    {
        // Arrange
        var jobGraph = CreateValidJobGraph();

        // Act
        var result = await _dispatcher.SubmitJobAsync(jobGraph);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.JobId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitJobAsync_AssignsJobId()
    {
        // Arrange
        var jobGraph = CreateValidJobGraph();

        // Act
        var result = await _dispatcher.SubmitJobAsync(jobGraph);

        // Assert
        jobGraph.JobId.Should().NotBeNullOrEmpty();
        jobGraph.JobId.Should().Be(result.JobId);
    }

    [Fact]
    public async Task GetJobStatusAsync_ForExistingJob_ReturnsStatus()
    {
        // Arrange
        var jobGraph = CreateValidJobGraph();
        var submitResult = await _dispatcher.SubmitJobAsync(jobGraph);

        // Act
        var status = await _dispatcher.GetJobStatusAsync(submitResult.JobId);

        // Assert
        status.Should().NotBeNull();
        status.JobId.Should().Be(submitResult.JobId);
        status.JobName.Should().Be(jobGraph.JobName);
    }

    [Fact]
    public async Task GetJobStatusAsync_ForNonExistentJob_ThrowsKeyNotFoundException()
    {
        // Act
        var act = async () => await _dispatcher.GetJobStatusAsync("non-existent-job");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task ListJobsAsync_ReturnsAllJobs()
    {
        // Arrange
        var jobGraph1 = CreateValidJobGraph();
        var jobGraph2 = CreateValidJobGraph();
        await _dispatcher.SubmitJobAsync(jobGraph1);
        await _dispatcher.SubmitJobAsync(jobGraph2);

        // Act
        var jobs = await _dispatcher.ListJobsAsync();

        // Assert
        jobs.Should().HaveCount(2);
    }

    [Fact]
    public async Task CancelJobAsync_WithExistingJob_UpdatesJobState()
    {
        // Arrange
        var jobGraph = CreateValidJobGraph();
        var submitResult = await _dispatcher.SubmitJobAsync(jobGraph);

        // Give the job a moment to start
        await Task.Delay(100);

        // Act
        await _dispatcher.CancelJobAsync(submitResult.JobId);

        // Assert
        var status = await _dispatcher.GetJobStatusAsync(submitResult.JobId);
        status.State.Should().BeOneOf(
            JobExecutionState.Canceling,
            JobExecutionState.Canceled);
    }

    [Fact]
    public async Task CancelJobAsync_WithNonExistentJob_ThrowsKeyNotFoundException()
    {
        // Act
        var act = async () => await _dispatcher.CancelJobAsync("non-existent-job");

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task SubmitJobAsync_WithInvalidJobGraph_ReturnsFailure()
    {
        // Arrange - Create invalid job graph with null job name
        var jobGraph = new JobGraph
        {
            JobName = null!, // Invalid
            Vertices = new List<JobVertex>(),
            Edges = new List<JobEdge>(),
            Configuration = new Dictionary<string, string>()
        };

        // Act
        var result = await _dispatcher.SubmitJobAsync(jobGraph);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitJobAsync_WithEmptyVertices_ReturnsFailure()
    {
        // Arrange
        var jobGraph = new JobGraph
        {
            JobName = "Test Job",
            Vertices = new List<JobVertex>(), // Empty - invalid
            Edges = new List<JobEdge>(),
            Configuration = new Dictionary<string, string>()
        };

        // Act
        var result = await _dispatcher.SubmitJobAsync(jobGraph);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    private static JobGraph CreateValidJobGraph()
    {
        return new JobGraph
        {
            JobName = $"Test Job {Guid.NewGuid()}",
            Vertices = new List<JobVertex>
            {
                new JobVertex
                {
                    Name = "source",
                    Parallelism = 2,
                    OperatorType = OperatorType.Source
                },
                new JobVertex
                {
                    Name = "map",
                    Parallelism = 2,
                    OperatorType = OperatorType.Map
                }
            },
            Edges = new List<JobEdge>
            {
                new JobEdge
                {
                    SourceVertexId = "source",
                    TargetVertexId = "map",
                    PartitioningStrategy = PartitioningStrategy.Forward
                }
            },
            Configuration = new Dictionary<string, string>()
        };
    }
}
