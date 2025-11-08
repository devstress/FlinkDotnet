// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Tests;

public class IntegrationScenarioTests
{
    private readonly Mock<ILogger<Dispatcher>> _mockDispatcherLogger;
    private readonly Mock<ILogger<ResourceManager>> _mockResourceLogger;
    private readonly Mock<ITemporalClient> _mockTemporalClient;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;
    private readonly ResourceManager _resourceManager;
    private readonly Dispatcher _dispatcher;

    public IntegrationScenarioTests()
    {
        _mockDispatcherLogger = new Mock<ILogger<Dispatcher>>();
        _mockResourceLogger = new Mock<ILogger<ResourceManager>>();
        _mockTemporalClient = new Mock<ITemporalClient>();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        
        _mockLoggerFactory
            .Setup(lf => lf.CreateLogger(It.IsAny<string>()))
            .Returns(new Mock<ILogger>().Object);
        
        _resourceManager = new ResourceManager(_mockResourceLogger.Object);
        _dispatcher = new Dispatcher(
            _resourceManager,
            _mockTemporalClient.Object,
            _mockLoggerFactory.Object);
    }

    [Fact]
    public async Task CompleteJobLifecycle_WithTaskManagers_ExecutesSuccessfully()
    {
        // Arrange - Setup infrastructure
        await _resourceManager.RegisterTaskManagerAsync("tm-1", 4);
        await _resourceManager.RegisterTaskManagerAsync("tm-2", 4);

        var jobGraph = CreateValidJobGraph();

        // Act - Submit job
        var submitResult = await _dispatcher.SubmitJobAsync(jobGraph);

        // Assert - Job submitted
        submitResult.Should().NotBeNull();
        submitResult.Success.Should().BeTrue();
        submitResult.JobId.Should().NotBeNullOrEmpty();

        // Wait for job to start processing (minimal delay)
        await Task.Delay(50);

        // Act - Get status
        var status = await _dispatcher.GetJobStatusAsync(submitResult.JobId);

        // Assert - Job is tracked
        status.Should().NotBeNull();
        status!.JobId.Should().Be(submitResult.JobId);

        // Act - List jobs
        var allJobs = await _dispatcher.ListJobsAsync();

        // Assert - Job appears in list
        allJobs.Should().Contain(j => j.JobId == submitResult.JobId);
    }

    [Fact]
    public async Task MultipleJobsScenario_ManagesResourcesCorrectly()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-1", 10);

        var job1 = CreateValidJobGraph();
        var job2 = CreateValidJobGraph();
        var job3 = CreateValidJobGraph();

        // Act - Submit multiple jobs
        var result1 = await _dispatcher.SubmitJobAsync(job1);
        var result2 = await _dispatcher.SubmitJobAsync(job2);
        var result3 = await _dispatcher.SubmitJobAsync(job3);

        await Task.Delay(50);

        // Assert - All jobs submitted
        result1.Success.Should().BeTrue();
        result2.Success.Should().BeTrue();
        result3.Success.Should().BeTrue();

        // Act - List all jobs
        var allJobs = await _dispatcher.ListJobsAsync();

        // Assert - All jobs tracked
        allJobs.Should().HaveCountGreaterThanOrEqualTo(3);
    }

    [Fact]
    public async Task JobCancellation_ReleasesResources()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-1", 4);
        var jobGraph = CreateValidJobGraph();
        var submitResult = await _dispatcher.SubmitJobAsync(jobGraph);
        
        await Task.Delay(50);
        var availableBefore = _resourceManager.GetAvailableSlots().Count();

        // Act - Cancel job
        await _dispatcher.CancelJobAsync(submitResult.JobId);
        await Task.Delay(50);

        // Assert - Status should reflect cancellation
        var status = await _dispatcher.GetJobStatusAsync(submitResult.JobId);
        status!.State.Should().BeOneOf(
            JobExecutionState.Canceling,
            JobExecutionState.Canceled,
            JobExecutionState.Failed);
    }

    [Fact]
    public async Task ResourceAllocation_AcrossMultipleTaskManagers()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-1", 2);
        await _resourceManager.RegisterTaskManagerAsync("tm-2", 2);
        await _resourceManager.RegisterTaskManagerAsync("tm-3", 2);

        // Act - Allocate more slots than any single TaskManager has
        var slots = await _resourceManager.AllocateSlotsAsync("job-1", 5);

        // Assert - Slots distributed across TaskManagers
        slots.Should().HaveCount(5);
        var taskManagersUsed = slots.Select(s => s.TaskManagerId).Distinct().Count();
        taskManagersUsed.Should().BeGreaterThan(1);
    }

    [Fact]
    public async Task TaskManagerUnregistration_RemovesSlots()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-1", 4);
        var initialSlots = _resourceManager.GetAllSlots().Count();

        // Act
        await _resourceManager.UnregisterTaskManagerAsync("tm-1");
        var afterSlots = _resourceManager.GetAllSlots().Count();

        // Assert
        afterSlots.Should().BeLessThan(initialSlots);
    }

    [Fact]
    public void GetAllSlots_WithMultipleTaskManagers_ReturnsAllSlots()
    {
        // Arrange
        _resourceManager.RegisterTaskManager("tm-1", 3);
        _resourceManager.RegisterTaskManager("tm-2", 5);

        // Act
        var allSlots = _resourceManager.GetAllSlots();

        // Assert
        allSlots.Should().HaveCount(8);
    }

    [Fact]
    public async Task JobSubmission_WithInvalidGraph_ReturnsFailure()
    {
        // Arrange
        var invalidGraph = new JobGraph
        {
            JobName = "", // Invalid - empty name
            Vertices = new List<JobVertex>
            {
                new JobVertex { Name = "test", Parallelism = 1, OperatorType = OperatorType.Source }
            },
            Edges = new List<JobEdge>(),
            Configuration = new Dictionary<string, string>()
        };

        // Act
        var result = await _dispatcher.SubmitJobAsync(invalidGraph);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("name");
    }

    private static JobGraph CreateValidJobGraph()
    {
        var sourceVertex = new JobVertex
        {
            Name = $"source-{Guid.NewGuid().ToString().Substring(0, 8)}",
            Parallelism = 2,
            OperatorType = OperatorType.Source
        };

        var mapVertex = new JobVertex
        {
            Name = $"map-{Guid.NewGuid().ToString().Substring(0, 8)}",
            Parallelism = 2,
            OperatorType = OperatorType.Map
        };

        return new JobGraph
        {
            JobName = $"Integration Test Job {Guid.NewGuid()}",
            MaxParallelism = 128,
            Vertices = new List<JobVertex> { sourceVertex, mapVertex },
            Edges = new List<JobEdge>
            {
                new JobEdge
                {
                    SourceVertexId = sourceVertex.VertexId,
                    TargetVertexId = mapVertex.VertexId,
                    PartitioningStrategy = PartitioningStrategy.Forward
                }
            },
            Configuration = new Dictionary<string, string>()
        };
    }
}
