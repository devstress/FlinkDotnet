// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Activities;
using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Workflows;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TaskDeploymentDescriptor = FlinkDotNet.TaskManager.Models.TaskDeploymentDescriptor;
using Temporalio.Testing;

namespace FlinkDotNet.JobManager.Tests;

/// <summary>
/// Phase 4 preparation: Unit tests for Temporal activities and workflow state.
/// Tests run without a real Temporal server using Temporalio.Testing.ActivityEnvironment
/// and direct instantiation of the workflow class.
/// </summary>
public class TemporalWorkflowPreparationTests
{
    // ─────────────────────────────────────────────────────────────────────────
    // TaskExecutionResult model tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TaskExecutionResult_DefaultValues_AreCorrect()
    {
        // Act
        var result = new TaskExecutionResult();

        // Assert
        result.ExecutionVertexId.Should().Be(string.Empty);
        result.Success.Should().BeFalse();
        result.RecordsProcessed.Should().Be(0);
        result.BytesProcessed.Should().Be(0);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void TaskExecutionResult_SuccessfulResult_PropertiesSetCorrectly()
    {
        // Act
        var result = new TaskExecutionResult
        {
            ExecutionVertexId = "vertex-123",
            Success = true,
            RecordsProcessed = 1000,
            BytesProcessed = 10000
        };

        // Assert
        result.ExecutionVertexId.Should().Be("vertex-123");
        result.Success.Should().BeTrue();
        result.RecordsProcessed.Should().Be(1000);
        result.BytesProcessed.Should().Be(10000);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void TaskExecutionResult_FailedResult_PropertiesSetCorrectly()
    {
        // Act
        var result = new TaskExecutionResult
        {
            ExecutionVertexId = "vertex-failed",
            Success = false,
            ErrorMessage = "Connection timeout"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection timeout");
        result.RecordsProcessed.Should().Be(0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // JobExecutionResult model tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void JobExecutionResult_DefaultValues_AreCorrect()
    {
        // Act
        var result = new JobExecutionResult();

        // Assert
        result.JobId.Should().Be(string.Empty);
        result.Success.Should().BeFalse();
        result.State.Should().Be(JobExecutionState.Created);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void JobExecutionResult_SuccessfulResult_PropertiesSetCorrectly()
    {
        // Act
        var result = new JobExecutionResult
        {
            JobId = "job-abc",
            Success = true,
            State = JobExecutionState.Finished
        };

        // Assert
        result.JobId.Should().Be("job-abc");
        result.Success.Should().BeTrue();
        result.State.Should().Be(JobExecutionState.Finished);
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void JobExecutionResult_FailedResult_PropertiesSetCorrectly()
    {
        // Act
        var result = new JobExecutionResult
        {
            JobId = "job-failed",
            Success = false,
            State = JobExecutionState.Failed,
            ErrorMessage = "Out of memory"
        };

        // Assert
        result.Success.Should().BeFalse();
        result.State.Should().Be(JobExecutionState.Failed);
        result.ErrorMessage.Should().Be("Out of memory");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TaskExecutionActivity tests (using Temporalio.Testing.ActivityEnvironment)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestTaskSlotsAsync_ReturnsCorrectNumberOfSlots()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act
        List<string> slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync("tm-1", 4));

        // Assert
        slots.Should().HaveCount(4);
        slots.Should().AllSatisfy(s => s.Should().StartWith("tm-1-slot-"));
    }

    [Fact]
    public async Task RequestTaskSlotsAsync_WithZeroSlots_ReturnsEmptyList()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act
        List<string> slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync("tm-1", 0));

        // Assert
        slots.Should().BeEmpty();
    }

    [Fact]
    public async Task RequestTaskSlotsAsync_SlotsHaveCorrectNamingPattern()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();
        string taskManagerId = "tm-test-42";

        // Act
        List<string> slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync(taskManagerId, 3));

        // Assert
        slots.Should().Contain($"{taskManagerId}-slot-0");
        slots.Should().Contain($"{taskManagerId}-slot-1");
        slots.Should().Contain($"{taskManagerId}-slot-2");
    }

    [Fact]
    public async Task CancelTaskAsync_ReturnsTrue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act
        bool result = await env.RunAsync(() =>
            activity.CancelTaskAsync("vertex-to-cancel"));

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CancelTaskAsync_WithAnyVertexId_AlwaysReturnsTrue()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act & Assert
        (await env.RunAsync(() => activity.CancelTaskAsync("vertex-1"))).Should().BeTrue();
        (await env.RunAsync(() => activity.CancelTaskAsync("vertex-2"))).Should().BeTrue();
        (await env.RunAsync(() => activity.CancelTaskAsync(""))).Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteTaskAsync_WithValidDescriptor_ReturnsSuccessResult()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var descriptor = new TaskDeploymentDescriptor
        {
            ExecutionVertexId = "vertex-exec-test",
            JobId = "job-1",
            JobVertexId = "job-vertex-1",
            OperatorName = "MapOperator",
            SubtaskIndex = 0,
            Parallelism = 2
        };
        var env = new ActivityEnvironment();

        // Act
        TaskExecutionResult result = await env.RunAsync(() =>
            activity.ExecuteTaskAsync(descriptor));

        // Assert
        result.Should().NotBeNull();
        result.ExecutionVertexId.Should().Be("vertex-exec-test");
        result.Success.Should().BeTrue();
        result.RecordsProcessed.Should().BeGreaterThan(0);
        result.BytesProcessed.Should().BeGreaterThan(0);
        result.ErrorMessage.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // FlinkJobWorkflow state/query tests (no Temporal runtime needed)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void FlinkJobWorkflow_InitialState_IsCreated()
    {
        // Arrange
        var workflow = new FlinkJobWorkflow();

        // Act - query initial state
        JobExecutionState state = workflow.GetJobState();

        // Assert
        state.Should().Be(JobExecutionState.Created);
    }

    [Fact]
    public void FlinkJobWorkflow_InitialTaskStates_IsEmpty()
    {
        // Arrange
        var workflow = new FlinkJobWorkflow();

        // Act
        Dictionary<string, ExecutionState> taskStates = workflow.GetTaskStates();

        // Assert
        taskStates.Should().NotBeNull();
        taskStates.Should().BeEmpty();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // TaskDeploymentDescriptor model tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void TaskDeploymentDescriptor_DefaultValues_AreCorrect()
    {
        // Act
        var descriptor = new TaskDeploymentDescriptor();

        // Assert
        descriptor.ExecutionVertexId.Should().Be(string.Empty);
        descriptor.JobId.Should().Be(string.Empty);
        descriptor.JobVertexId.Should().Be(string.Empty);
        descriptor.Parallelism.Should().Be(0);
        descriptor.SubtaskIndex.Should().Be(0);
    }

    [Fact]
    public void TaskDeploymentDescriptor_WithFullConfiguration_PropertiesSetCorrectly()
    {
        // Act
        var descriptor = new TaskDeploymentDescriptor
        {
            ExecutionVertexId = "exec-vertex-1",
            JobId = "job-123",
            JobVertexId = "job-vertex-abc",
            OperatorName = "FilterOperator",
            SubtaskIndex = 2,
            Parallelism = 4
        };

        // Assert
        descriptor.ExecutionVertexId.Should().Be("exec-vertex-1");
        descriptor.JobId.Should().Be("job-123");
        descriptor.JobVertexId.Should().Be("job-vertex-abc");
        descriptor.OperatorName.Should().Be("FilterOperator");
        descriptor.SubtaskIndex.Should().Be(2);
        descriptor.Parallelism.Should().Be(4);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Activity - multiple TaskManagers slot allocation tests
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RequestTaskSlotsAsync_MultipleTaskManagers_AllGetUniqueSlots()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act - request slots from different TaskManagers
        List<string> tm1Slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync("tm-1", 4));
        List<string> tm2Slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync("tm-2", 4));

        // Assert - all slot IDs are unique across TaskManagers
        tm1Slots.Intersect(tm2Slots).Should().BeEmpty();
    }

    [Fact]
    public async Task RequestTaskSlotsAsync_WithLargeSlotCount_ReturnsAllSlots()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<TaskExecutionActivity>>();
        var activity = new TaskExecutionActivity(mockLogger.Object);
        var env = new ActivityEnvironment();

        // Act
        List<string> slots = await env.RunAsync(() =>
            activity.RequestTaskSlotsAsync("tm-big", 128));

        // Assert
        slots.Should().HaveCount(128);
        slots.Should().OnlyHaveUniqueItems();
    }
}
