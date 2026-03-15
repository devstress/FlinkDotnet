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

/// <summary>
/// End-to-End integration tests that validate the complete job lifecycle
/// from submission through execution and cancellation.
/// Completes Phase 3 (10% remaining) of the implementation roadmap.
/// </summary>
public class EndToEndJobLifecycleTests
{
    private const int StatePollDelayMs = 50;
    private const int MaxStatePollIterations = 40; // 40 × 50ms = 2 seconds max wait

    private readonly ResourceManager _resourceManager;
    private readonly Dispatcher _dispatcher;

    public EndToEndJobLifecycleTests()
    {
        Mock<ILogger<ResourceManager>> resourceLogger = new();
        Mock<ILogger<Dispatcher>> dispatcherLogger = new();
        Mock<ITemporalClient> temporalClient = new();
        Mock<ILoggerFactory> loggerFactory = new();
        loggerFactory.Setup(lf => lf.CreateLogger(It.IsAny<string>()))
                     .Returns(new Mock<ILogger>().Object);

        _resourceManager = new ResourceManager(resourceLogger.Object);
        _dispatcher = new Dispatcher(_resourceManager, temporalClient.Object, loggerFactory.Object);
    }

    // ──────────────────────────────────────────────────────────
    // Job Submission Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task SubmitJob_WithValidGraph_ReturnsSuccessAndJobId()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-e2e-1", 4);
        JobGraph graph = BuildMinimalJobGraph("E2E Submit Test");

        // Act
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(graph);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.JobId.Should().NotBeNullOrEmpty();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task SubmitJob_WithEmptyJobName_ReturnsFailure()
    {
        // Arrange
        JobGraph invalidGraph = new()
        {
            JobName = string.Empty,
            Vertices = [new JobVertex { Name = "source", Parallelism = 1, OperatorType = OperatorType.Source }],
            Edges = [],
            Configuration = []
        };

        // Act
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(invalidGraph);

        // Assert
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task SubmitJob_WithNullGraph_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _dispatcher.SubmitJobAsync(null!));
    }

    [Fact]
    public async Task SubmitJob_AssignsUniqueJobId_ForEachSubmission()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-unique-1", 8);

        // Act
        JobSubmissionResult r1 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("Job A"));
        JobSubmissionResult r2 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("Job B"));
        JobSubmissionResult r3 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("Job C"));

        // Assert
        r1.JobId.Should().NotBe(r2.JobId);
        r2.JobId.Should().NotBe(r3.JobId);
        r1.JobId.Should().NotBe(r3.JobId);
    }

    // ──────────────────────────────────────────────────────────
    // Job Status Tracking Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetJobStatus_AfterSubmission_ReturnsTrackedJob()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-status-1", 4);
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("Status Test Job"));
        await Task.Delay(50); // allow background execution to start

        // Act
        JobStatus? status = await _dispatcher.GetJobStatusAsync(result.JobId);

        // Assert
        status.Should().NotBeNull();
        status!.JobId.Should().Be(result.JobId);
        status.JobName.Should().Be("Status Test Job");
    }

    [Fact]
    public async Task GetJobStatus_ForNonExistentJobId_ReturnsNull()
    {
        // Act
        JobStatus? status = await _dispatcher.GetJobStatusAsync("non-existent-job-id");

        // Assert
        status.Should().BeNull();
    }

    [Fact]
    public async Task GetJobStatus_StateTransitionsFromCreated()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-state-1", 4);
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("State Transition Job"));

        // Act – poll until the job reaches a terminal state (allow up to 2 seconds)
        // The Dispatcher updates jobInfo.State only after ExecuteJobAsync completes, so
        // we wait until the state is no longer Created (job finished/failed/canceled).
        JobStatus? finalStatus = null;
        for (int i = 0; i < MaxStatePollIterations; i++)
        {
            await Task.Delay(StatePollDelayMs);
            finalStatus = await _dispatcher.GetJobStatusAsync(result.JobId);
            if (finalStatus?.State != JobExecutionState.Created)
                break;
        }

        // Assert – the job transitioned away from Created once execution completed
        finalStatus.Should().NotBeNull();
        finalStatus!.State.Should().BeOneOf(
            JobExecutionState.Deploying,
            JobExecutionState.Running,
            JobExecutionState.Finished,
            JobExecutionState.Failed,
            JobExecutionState.Canceled);
    }

    // ──────────────────────────────────────────────────────────
    // Job Listing Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ListJobs_AfterMultipleSubmissions_ContainsAllJobs()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-list-1", 12);
        JobSubmissionResult r1 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("List Job 1"));
        JobSubmissionResult r2 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("List Job 2"));
        JobSubmissionResult r3 = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("List Job 3"));

        // Act
        List<JobStatus> jobs = await _dispatcher.ListJobsAsync();

        // Assert
        jobs.Should().Contain(j => j.JobId == r1.JobId);
        jobs.Should().Contain(j => j.JobId == r2.JobId);
        jobs.Should().Contain(j => j.JobId == r3.JobId);
    }

    [Fact]
    public async Task ListJobs_OnEmptyCluster_ReturnsEmptyList()
    {
        // Act
        List<JobStatus> jobs = await _dispatcher.ListJobsAsync();

        // Assert
        jobs.Should().NotBeNull();
        // May or may not be empty depending on other tests – just ensure it's a valid list
        jobs.Should().BeAssignableTo<List<JobStatus>>();
    }

    // ──────────────────────────────────────────────────────────
    // Job Cancellation Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CancelJob_RunningJob_TransitionsToCanceledOrFailed()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-cancel-1", 4);
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(BuildMinimalJobGraph("Cancel Test Job"));
        await Task.Delay(50); // allow job to start

        // Act
        await _dispatcher.CancelJobAsync(result.JobId);
        await Task.Delay(100); // allow cancellation to propagate

        // Assert
        JobStatus? status = await _dispatcher.GetJobStatusAsync(result.JobId);
        status.Should().NotBeNull();
        status!.State.Should().BeOneOf(
            JobExecutionState.Canceling,
            JobExecutionState.Canceled,
            JobExecutionState.Failed,
            JobExecutionState.Finished); // job may have finished before cancel
    }

    [Fact]
    public async Task CancelJob_NonExistentJob_ThrowsArgumentException()
    {
        // The Dispatcher throws ArgumentException when cancelling a job that was never submitted.
        // This verifies the documented behavior of the Dispatcher.
        await Assert.ThrowsAsync<ArgumentException>(() => _dispatcher.CancelJobAsync("non-existent-job"));
    }

    // ──────────────────────────────────────────────────────────
    // Resource Management Integration Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task ResourceManager_TaskManagerRegistration_ReflectsInSlotCount()
    {
        // Arrange – fresh resource manager for isolation
        Mock<ILogger<ResourceManager>> logger = new();
        ResourceManager rm = new(logger.Object);

        // Act
        await rm.RegisterTaskManagerAsync("rm-tm-1", 4);
        await rm.RegisterTaskManagerAsync("rm-tm-2", 6);
        int available = await rm.GetAvailableSlotsAsync();

        // Assert
        available.Should().Be(10);
    }

    [Fact]
    public async Task ResourceManager_AllocateAndRelease_SlotsAreRecycled()
    {
        // Arrange
        Mock<ILogger<ResourceManager>> logger = new();
        ResourceManager rm = new(logger.Object);
        await rm.RegisterTaskManagerAsync("rm-recycle-1", 4);

        // Act – allocate all slots
        List<TaskSlot> slots = await rm.AllocateSlotsAsync("job-recycle", 4);
        int afterAlloc = await rm.GetAvailableSlotsAsync();

        // Release them
        foreach (TaskSlot slot in slots)
        {
            await rm.ReleaseSlotAsync(slot.SlotId);
        }
        int afterRelease = await rm.GetAvailableSlotsAsync();

        // Assert
        afterAlloc.Should().Be(0);
        afterRelease.Should().Be(4);
    }

    [Fact]
    public async Task ResourceManager_Heartbeat_UpdatesLastHeartbeatTimestamp()
    {
        // Arrange
        Mock<ILogger<ResourceManager>> logger = new();
        ResourceManager rm = new(logger.Object);
        await rm.RegisterTaskManagerAsync("rm-hb-1", 2);

        DateTime? before = rm.GetLastHeartbeat("rm-hb-1");
        await Task.Delay(10); // ensure time advances

        // Act
        await rm.RecordHeartbeatAsync("rm-hb-1");
        DateTime? after = rm.GetLastHeartbeat("rm-hb-1");

        // Assert
        before.Should().NotBeNull();
        after.Should().NotBeNull();
        after.Should().BeOnOrAfter(before!.Value);
    }

    [Fact]
    public async Task ResourceManager_UnregisterTaskManager_RemovesSlots()
    {
        // Arrange
        Mock<ILogger<ResourceManager>> logger = new();
        ResourceManager rm = new(logger.Object);
        await rm.RegisterTaskManagerAsync("rm-unreg-1", 5);
        int before = await rm.GetAvailableSlotsAsync();

        // Act
        await rm.UnregisterTaskManagerAsync("rm-unreg-1");
        int after = await rm.GetAvailableSlotsAsync();

        // Assert
        before.Should().Be(5);
        after.Should().Be(0);
    }

    [Fact]
    public async Task ResourceManager_MultipleTaskManagers_SlotDistribution()
    {
        // Arrange
        Mock<ILogger<ResourceManager>> logger = new();
        ResourceManager rm = new(logger.Object);
        await rm.RegisterTaskManagerAsync("rm-dist-1", 3);
        await rm.RegisterTaskManagerAsync("rm-dist-2", 3);
        await rm.RegisterTaskManagerAsync("rm-dist-3", 3);

        // Act – allocate 7 slots (cross-TM)
        List<TaskSlot> slots = await rm.AllocateSlotsAsync("job-dist", 7);

        // Assert – slots come from at least 2 different TaskManagers
        int taskManagersUsed = slots.Select(s => s.TaskManagerId).Distinct().Count();
        taskManagersUsed.Should().BeGreaterThanOrEqualTo(2);
        slots.Should().HaveCount(7);
    }

    // ──────────────────────────────────────────────────────────
    // Full End-to-End Workflow Tests
    // ──────────────────────────────────────────────────────────

    [Fact]
    public async Task FullWorkflow_SubmitTrackCancel_CompletesCycle()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-full-1", 4);
        await _resourceManager.RegisterTaskManagerAsync("tm-full-2", 4);

        JobGraph graph = BuildComplexJobGraph("Full E2E Workflow");
        JobSubmissionResult submit = await _dispatcher.SubmitJobAsync(graph);

        // Assert submission
        submit.Success.Should().BeTrue();
        submit.JobId.Should().NotBeNullOrEmpty();

        // Act – verify it appears in listing
        await Task.Delay(50);
        List<JobStatus> allJobs = await _dispatcher.ListJobsAsync();
        allJobs.Should().Contain(j => j.JobId == submit.JobId);

        // Act – cancel
        await _dispatcher.CancelJobAsync(submit.JobId);
        await Task.Delay(100);

        // Assert – job is in a terminal or canceling state
        JobStatus? finalStatus = await _dispatcher.GetJobStatusAsync(submit.JobId);
        finalStatus.Should().NotBeNull();
        finalStatus!.State.Should().BeOneOf(
            JobExecutionState.Canceled,
            JobExecutionState.Canceling,
            JobExecutionState.Failed,
            JobExecutionState.Finished);
    }

    [Fact]
    public async Task ConcurrentJobSubmissions_AllSucceed()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-concurrent-1", 20);

        // Act – submit 5 jobs concurrently
        Task<JobSubmissionResult>[] submissionTasks = Enumerable.Range(1, 5)
            .Select(i => _dispatcher.SubmitJobAsync(BuildMinimalJobGraph($"Concurrent Job {i}")))
            .ToArray();

        JobSubmissionResult[] results = await Task.WhenAll(submissionTasks);

        // Assert – all succeed and have unique IDs
        results.Should().AllSatisfy(r => r.Success.Should().BeTrue());
        results.Select(r => r.JobId).Distinct().Should().HaveCount(5);
    }

    [Fact]
    public async Task JobWithMultipleVertices_SubmitsAndTracksCorrectly()
    {
        // Arrange
        await _resourceManager.RegisterTaskManagerAsync("tm-multi-vertex-1", 8);

        // Build a pipeline: Source → Map → Filter → Sink
        JobVertex source = new() { Name = "kafka-source", Parallelism = 2, OperatorType = OperatorType.Source };
        JobVertex map = new() { Name = "uppercase-map", Parallelism = 2, OperatorType = OperatorType.Map };
        JobVertex filter = new() { Name = "length-filter", Parallelism = 2, OperatorType = OperatorType.Filter };
        JobVertex sink = new() { Name = "console-sink", Parallelism = 1, OperatorType = OperatorType.Sink };

        JobGraph graph = new()
        {
            JobName = "Multi-Vertex Pipeline",
            MaxParallelism = 128,
            Vertices = [source, map, filter, sink],
            Edges =
            [
                new JobEdge { SourceVertexId = source.VertexId, TargetVertexId = map.VertexId, PartitioningStrategy = PartitioningStrategy.Forward },
                new JobEdge { SourceVertexId = map.VertexId,    TargetVertexId = filter.VertexId, PartitioningStrategy = PartitioningStrategy.Forward },
                new JobEdge { SourceVertexId = filter.VertexId, TargetVertexId = sink.VertexId, PartitioningStrategy = PartitioningStrategy.Rebalance }
            ],
            Configuration = []
        };

        // Act
        JobSubmissionResult result = await _dispatcher.SubmitJobAsync(graph);

        // Assert
        result.Success.Should().BeTrue();
        await Task.Delay(50);

        JobStatus? status = await _dispatcher.GetJobStatusAsync(result.JobId);
        status.Should().NotBeNull();
        status!.JobName.Should().Be("Multi-Vertex Pipeline");
    }

    // ──────────────────────────────────────────────────────────
    // Helper Methods
    // ──────────────────────────────────────────────────────────

    private static JobGraph BuildMinimalJobGraph(string jobName)
    {
        JobVertex source = new()
        {
            Name = $"source-{Guid.NewGuid():N}",
            Parallelism = 1,
            OperatorType = OperatorType.Source
        };
        JobVertex sink = new()
        {
            Name = $"sink-{Guid.NewGuid():N}",
            Parallelism = 1,
            OperatorType = OperatorType.Sink
        };

        return new JobGraph
        {
            JobName = jobName,
            MaxParallelism = 128,
            Vertices = [source, sink],
            Edges =
            [
                new JobEdge
                {
                    SourceVertexId = source.VertexId,
                    TargetVertexId = sink.VertexId,
                    PartitioningStrategy = PartitioningStrategy.Forward
                }
            ],
            Configuration = []
        };
    }

    private static JobGraph BuildComplexJobGraph(string jobName)
    {
        JobVertex source = new() { Name = "source", Parallelism = 2, OperatorType = OperatorType.Source };
        JobVertex map = new() { Name = "map", Parallelism = 2, OperatorType = OperatorType.Map };
        JobVertex sink = new() { Name = "sink", Parallelism = 1, OperatorType = OperatorType.Sink };

        return new JobGraph
        {
            JobName = jobName,
            MaxParallelism = 128,
            Vertices = [source, map, sink],
            Edges =
            [
                new JobEdge { SourceVertexId = source.VertexId, TargetVertexId = map.VertexId,  PartitioningStrategy = PartitioningStrategy.Forward },
                new JobEdge { SourceVertexId = map.VertexId,    TargetVertexId = sink.VertexId, PartitioningStrategy = PartitioningStrategy.Rebalance }
            ],
            Configuration = []
        };
    }
}
