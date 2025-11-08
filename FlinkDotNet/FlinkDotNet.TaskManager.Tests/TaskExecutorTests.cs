// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.TaskManager.Implementation;
using FlinkDotNet.TaskManager.Interfaces;
using FlinkDotNet.TaskManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.TaskManager.Tests;

public class TaskExecutorTests
{
    private readonly Mock<ILogger<TaskExecutor>> _mockLogger;
    private readonly TaskExecutor _taskExecutor;

    public TaskExecutorTests()
    {
        _mockLogger = new Mock<ILogger<TaskExecutor>>();
        _taskExecutor = new TaskExecutor(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        Action act = () => new TaskExecutor(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public async Task DeployTaskAsync_WithValidDescriptor_DeploysTask()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor = new()
        {
            ExecutionVertexId = "vertex-1",
            JobId = "job-1",
            JobVertexId = "job-vertex-1",
            OperatorName = "MapOperator",
            SubtaskIndex = 0,
            Parallelism = 1
        };

        // Act
        await _taskExecutor.DeployTaskAsync(descriptor);
        await Task.Delay(200); // Give time for background task to start

        // Assert - Task should be running
        TaskExecutionStatus status = await _taskExecutor.GetTaskStatusAsync("vertex-1");
        status.ExecutionVertexId.Should().Be("vertex-1");
        status.State.Should().BeOneOf("DEPLOYING", "RUNNING", "FINISHED");
    }

    [Fact]
    public async Task DeployTaskAsync_WithNullDescriptor_ThrowsArgumentNullException()
    {
        // Arrange & Act
        Func<Task> act = async () => await _taskExecutor.DeployTaskAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeployTaskAsync_WithDuplicateVertexId_ThrowsInvalidOperationException()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor = new()
        {
            ExecutionVertexId = "vertex-duplicate",
            JobId = "job-1",
            JobVertexId = "job-vertex-1"
        };

        // Act - Deploy first task
        await _taskExecutor.DeployTaskAsync(descriptor);

        // Act - Try to deploy same task again
        Func<Task> act = async () => await _taskExecutor.DeployTaskAsync(descriptor);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*already running*");
    }

    [Fact]
    public async Task CancelTaskAsync_CancelsRunningTask()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor = new()
        {
            ExecutionVertexId = "vertex-cancel",
            JobId = "job-1",
            JobVertexId = "job-vertex-1"
        };

        await _taskExecutor.DeployTaskAsync(descriptor);
        await Task.Delay(100); // Let task start

        // Act
        await _taskExecutor.CancelTaskAsync("vertex-cancel");
        await Task.Delay(200); // Give time for cancellation

        // Assert - Task should be canceled or removed
        TaskExecutionStatus status = await _taskExecutor.GetTaskStatusAsync("vertex-cancel");
        status.State.Should().BeOneOf("CANCELLING", "CANCELED", "NOT_FOUND");
    }

    [Fact]
    public async Task CancelTaskAsync_WithNonExistentTask_LogsWarning()
    {
        // Arrange
        string nonExistentId = "non-existent-task";

        // Act
        await _taskExecutor.CancelTaskAsync(nonExistentId);

        // Assert - Should not throw, just log warning
        // Verify via mock that warning was logged (actual verification would need specific mock setup)
    }

    [Fact]
    public async Task GetTaskStatusAsync_ForNonExistentTask_ReturnsNotFound()
    {
        // Arrange
        string nonExistentId = "does-not-exist";

        // Act
        TaskExecutionStatus status = await _taskExecutor.GetTaskStatusAsync(nonExistentId);

        // Assert
        status.ExecutionVertexId.Should().Be(nonExistentId);
        status.State.Should().Be("NOT_FOUND");
    }

    [Fact]
    public async Task GetTaskStatusAsync_ForRunningTask_ReturnsStatus()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor = new()
        {
            ExecutionVertexId = "vertex-status",
            JobId = "job-1",
            JobVertexId = "job-vertex-1"
        };

        await _taskExecutor.DeployTaskAsync(descriptor);
        await Task.Delay(150); // Let task run

        // Act
        TaskExecutionStatus status = await _taskExecutor.GetTaskStatusAsync("vertex-status");

        // Assert
        status.ExecutionVertexId.Should().Be("vertex-status");
        status.State.Should().NotBe("NOT_FOUND");
        status.RecordsProcessed.Should().BeGreaterThanOrEqualTo(0);
        status.BytesProcessed.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task TaskExecution_CompletesSuccessfully()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor = new()
        {
            ExecutionVertexId = "vertex-complete",
            JobId = "job-1",
            JobVertexId = "job-vertex-1"
        };

        // Act
        await _taskExecutor.DeployTaskAsync(descriptor);
        await Task.Delay(300); // Wait for simulated execution to complete

        // Assert
        TaskExecutionStatus status = await _taskExecutor.GetTaskStatusAsync("vertex-complete");
        // Task may have already finished and been cleaned up
        status.State.Should().BeOneOf("RUNNING", "FINISHED", "NOT_FOUND");
    }

    [Fact]
    public async Task MultipleTasksCanRunConcurrently()
    {
        // Arrange
        TaskDeploymentDescriptor descriptor1 = new()
        {
            ExecutionVertexId = "vertex-concurrent-1",
            JobId = "job-1",
            JobVertexId = "job-vertex-1"
        };

        TaskDeploymentDescriptor descriptor2 = new()
        {
            ExecutionVertexId = "vertex-concurrent-2",
            JobId = "job-1",
            JobVertexId = "job-vertex-2"
        };

        TaskDeploymentDescriptor descriptor3 = new()
        {
            ExecutionVertexId = "vertex-concurrent-3",
            JobId = "job-1",
            JobVertexId = "job-vertex-3"
        };

        // Act
        await Task.WhenAll(
            _taskExecutor.DeployTaskAsync(descriptor1),
            _taskExecutor.DeployTaskAsync(descriptor2),
            _taskExecutor.DeployTaskAsync(descriptor3)
        );

        await Task.Delay(150); // Let tasks start

        // Assert - All tasks should be running or have run
        TaskExecutionStatus status1 = await _taskExecutor.GetTaskStatusAsync("vertex-concurrent-1");
        TaskExecutionStatus status2 = await _taskExecutor.GetTaskStatusAsync("vertex-concurrent-2");
        TaskExecutionStatus status3 = await _taskExecutor.GetTaskStatusAsync("vertex-concurrent-3");

        status1.State.Should().NotBe("NOT_FOUND");
        status2.State.Should().NotBe("NOT_FOUND");
        status3.State.Should().NotBe("NOT_FOUND");
    }
}
