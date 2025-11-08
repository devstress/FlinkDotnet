// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FluentAssertions;

namespace FlinkDotNet.JobManager.Tests;

public class ModelTests
{
    [Fact]
    public void JobVertex_DefaultProperties_SetCorrectly()
    {
        // Act
        var vertex = new JobVertex();

        // Assert
        vertex.VertexId.Should().NotBeNullOrEmpty();
        vertex.Parallelism.Should().Be(1);
    }

    [Fact]
    public void JobVertex_NameProperty_AliasesOperatorName()
    {
        // Arrange
        var vertex = new JobVertex
        {
            Name = "TestOperator"
        };

        // Assert
        vertex.OperatorName.Should().Be("TestOperator");
        vertex.Name.Should().Be("TestOperator");
    }

    [Fact]
    public void JobEdge_DefaultProperties_SetCorrectly()
    {
        // Act
        var edge = new JobEdge();

        // Assert
        edge.PartitioningStrategy.Should().Be(PartitioningStrategy.Forward);
    }

    [Fact]
    public void TaskSlot_DefaultValues_SetCorrectly()
    {
        // Act
        var slot = new TaskSlot();

        // Assert
        slot.SlotId.Should().NotBeNullOrEmpty();
        slot.IsAllocated.Should().BeFalse();
        slot.SlotNumber.Should().Be(0);
    }

    [Fact]
    public void ExecutionState_Values_AreCorrect()
    {
        // Assert
        ExecutionState.Created.Should().Be(ExecutionState.Created);
        ExecutionState.Scheduled.Should().Be(ExecutionState.Scheduled);
        ExecutionState.Deploying.Should().Be(ExecutionState.Deploying);
        ExecutionState.Running.Should().Be(ExecutionState.Running);
        ExecutionState.Finished.Should().Be(ExecutionState.Finished);
        ExecutionState.Canceled.Should().Be(ExecutionState.Canceled);
        ExecutionState.Failed.Should().Be(ExecutionState.Failed);
    }

    [Fact]
    public void JobExecutionState_Values_AreCorrect()
    {
        // Assert
        JobExecutionState.Created.Should().Be(JobExecutionState.Created);
        JobExecutionState.Running.Should().Be(JobExecutionState.Running);
        JobExecutionState.Finished.Should().Be(JobExecutionState.Finished);
        JobExecutionState.Failed.Should().Be(JobExecutionState.Failed);
        JobExecutionState.Canceling.Should().Be(JobExecutionState.Canceling);
        JobExecutionState.Canceled.Should().Be(JobExecutionState.Canceled);
    }

    [Fact]
    public void OperatorType_Values_AreCorrect()
    {
        // Assert
        OperatorType.Source.Should().Be(OperatorType.Source);
        OperatorType.Map.Should().Be(OperatorType.Map);
        OperatorType.Filter.Should().Be(OperatorType.Filter);
        OperatorType.Sink.Should().Be(OperatorType.Sink);
    }

    [Fact]
    public void PartitioningStrategy_Values_AreCorrect()
    {
        // Assert
        PartitioningStrategy.Forward.Should().Be(PartitioningStrategy.Forward);
        PartitioningStrategy.Rebalance.Should().Be(PartitioningStrategy.Rebalance);
        PartitioningStrategy.Rescale.Should().Be(PartitioningStrategy.Rescale);
        PartitioningStrategy.Broadcast.Should().Be(PartitioningStrategy.Broadcast);
    }

    [Fact]
    public void ExecutionGraph_DefaultProperties_SetCorrectly()
    {
        // Act
        var graph = new ExecutionGraph();

        // Assert
        graph.JobId.Should().BeEmpty();
        graph.ExecutionVertices.Should().NotBeNull().And.BeEmpty();
        graph.ExecutionEdges.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void ExecutionVertex_DefaultProperties_SetCorrectly()
    {
        // Act
        var vertex = new ExecutionVertex();

        // Assert
        vertex.Id.Should().NotBeNullOrEmpty();
        vertex.State.Should().Be(ExecutionState.Created);
    }

    [Fact]
    public void JobGraph_DefaultProperties_SetCorrectly()
    {
        // Act
        var graph = new JobGraph();

        // Assert
        graph.JobId.Should().NotBeNullOrEmpty(); // JobId is initialized with a Guid
        graph.MaxParallelism.Should().Be(128);
        graph.Vertices.Should().NotBeNull().And.BeEmpty();
        graph.Edges.Should().NotBeNull().And.BeEmpty();
        graph.Configuration.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void JobStatus_Properties_CanBeSet()
    {
        // Act
        var status = new JobStatus
        {
            JobId = "job-789",
            JobName = "Test Job",
            State = JobExecutionState.Running,
            StartTime = DateTime.UtcNow,
            EndTime = null
        };

        // Assert
        status.JobId.Should().Be("job-789");
        status.JobName.Should().Be("Test Job");
        status.State.Should().Be(JobExecutionState.Running);
        status.StartTime.Should().NotBeNull();
        status.EndTime.Should().BeNull();
    }
}
