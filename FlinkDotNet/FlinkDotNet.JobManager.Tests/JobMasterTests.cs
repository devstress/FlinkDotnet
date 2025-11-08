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

public class JobMasterTests
{
    private readonly Mock<IResourceManager> _mockResourceManager;
    private readonly Mock<ITemporalClient> _mockTemporalClient;
    private readonly Mock<ILogger<JobMaster>> _mockLogger;
    private readonly string _jobId;
    private readonly JobGraph _jobGraph;

    public JobMasterTests()
    {
        _mockResourceManager = new Mock<IResourceManager>();
        _mockTemporalClient = new Mock<ITemporalClient>();
        _mockLogger = new Mock<ILogger<JobMaster>>();
        _jobId = "test-job-1";
        
        _jobGraph = new JobGraph
        {
            JobName = "Test Job",
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

    [Fact]
    public void Constructor_WithNullJobId_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobMaster(
            null!,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("jobId");
    }

    [Fact]
    public void Constructor_WithNullJobGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobMaster(
            _jobId,
            null!,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("jobGraph");
    }

    [Fact]
    public void Constructor_WithNullResourceManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobMaster(
            _jobId,
            _jobGraph,
            null!,
            _mockTemporalClient.Object,
            _mockLogger.Object);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceManager");
    }

    [Fact]
    public void Constructor_WithNullTemporalClient_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            null!,
            _mockLogger.Object);
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            null!);
        
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public void JobId_ReturnsCorrectJobId()
    {
        // Arrange
        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        // Act & Assert
        jobMaster.JobId.Should().Be(_jobId);
    }

    [Fact]
    public async Task StartJobAsync_CreatesExecutionGraph()
    {
        // Arrange
        var slots = Enumerable.Range(0, 4).Select(i => new TaskSlot
        {
            SlotId = $"slot-{i}",
            TaskManagerId = "tm-1",
            IsAllocated = true
        }).ToList();

        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        // Act
        await jobMaster.StartJobAsync();

        // Assert
        var executionGraph = await jobMaster.GetExecutionGraphAsync();
        executionGraph.Should().NotBeNull();
        executionGraph.JobId.Should().Be(_jobId);
        executionGraph.ExecutionVertices.Should().HaveCount(4); // 2 + 2 parallelism
    }

    [Fact]
    public async Task StartJobAsync_AllocatesRequiredSlots()
    {
        // Arrange
        var slots = Enumerable.Range(0, 4).Select(i => new TaskSlot
        {
            SlotId = $"slot-{i}",
            TaskManagerId = "tm-1",
            IsAllocated = true
        }).ToList();

        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        // Act
        await jobMaster.StartJobAsync();

        // Assert
        _mockResourceManager.Verify(
            rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartJobAsync_WithInsufficientResources_ThrowsInvalidOperationException()
    {
        // Arrange
        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TaskSlot> { new TaskSlot { SlotId = "slot-1", TaskManagerId = "tm-1" } });

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await jobMaster.StartJobAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient resources*");
    }

    [Fact]
    public async Task CancelJobAsync_ReleasesAllocatedResources()
    {
        // Arrange
        var slots = Enumerable.Range(0, 4).Select(i => new TaskSlot
        {
            SlotId = $"slot-{i}",
            TaskManagerId = "tm-1",
            IsAllocated = true
        }).ToList();

        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        await jobMaster.StartJobAsync();

        // Act
        await jobMaster.CancelJobAsync();

        // Assert
        _mockResourceManager.Verify(
            rm => rm.ReleaseSlotAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetExecutionGraphAsync_BeforeStart_ThrowsInvalidOperationException()
    {
        // Arrange
        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        // Act
        var act = async () => await jobMaster.GetExecutionGraphAsync();

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*ExecutionGraph not yet created*");
    }

    [Fact]
    public async Task UpdateTaskStatusAsync_UpdatesVertexState()
    {
        // Arrange
        var slots = Enumerable.Range(0, 4).Select(i => new TaskSlot
        {
            SlotId = $"slot-{i}",
            TaskManagerId = "tm-1",
            IsAllocated = true
        }).ToList();

        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        await jobMaster.StartJobAsync();
        var executionGraph = await jobMaster.GetExecutionGraphAsync();
        var vertexId = executionGraph.ExecutionVertices.First().Id;

        // Act
        await jobMaster.UpdateTaskStatusAsync(vertexId, ExecutionState.Running);

        // Assert
        var updatedGraph = await jobMaster.GetExecutionGraphAsync();
        var updatedVertex = updatedGraph.ExecutionVertices.First(v => v.Id == vertexId);
        updatedVertex.State.Should().Be(ExecutionState.Running);
    }

    [Fact]
    public async Task TriggerCheckpointAsync_LogsCheckpointRequest()
    {
        // Arrange
        var slots = Enumerable.Range(0, 4).Select(i => new TaskSlot
        {
            SlotId = $"slot-{i}",
            TaskManagerId = "tm-1",
            IsAllocated = true
        }).ToList();

        _mockResourceManager
            .Setup(rm => rm.AllocateSlotsAsync(_jobId, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(slots);

        var jobMaster = new JobMaster(
            _jobId,
            _jobGraph,
            _mockResourceManager.Object,
            _mockTemporalClient.Object,
            _mockLogger.Object);

        await jobMaster.StartJobAsync();

        // Act
        var act = async () => await jobMaster.TriggerCheckpointAsync(12345);

        // Assert
        await act.Should().NotThrowAsync();
    }
}
