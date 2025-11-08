// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Implementation;

/// <summary>
/// JobMaster manages the execution of a single job.
/// Coordinates ExecutionGraph creation, task deployment, and monitoring.
/// </summary>
public class JobMaster : IJobMaster
{
    private readonly string _jobId;
    private readonly JobGraph _jobGraph;
    private readonly IResourceManager _resourceManager;
    private readonly ITemporalClient _temporalClient;
    private readonly ILogger<JobMaster> _logger;

    private ExecutionGraph? _executionGraph;
    private readonly ConcurrentDictionary<string, ExecutionState> _taskStates = new();
    private JobExecutionState _jobState = JobExecutionState.Created;
    private CancellationTokenSource? _executionCts;

    public string JobId => _jobId;

    public JobMaster(
        string jobId,
        JobGraph jobGraph,
        IResourceManager resourceManager,
        ITemporalClient temporalClient,
        ILogger<JobMaster> logger)
    {
        _jobId = jobId ?? throw new ArgumentNullException(nameof(jobId));
        _jobGraph = jobGraph ?? throw new ArgumentNullException(nameof(jobGraph));
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _temporalClient = temporalClient ?? throw new ArgumentNullException(nameof(temporalClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartJobAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting job {JobId}", _jobId);

        try
        {
            // Update state to deploying
            _jobState = JobExecutionState.Deploying;

            // Step 1: Create ExecutionGraph from JobGraph (logical → physical plan)
            _executionGraph = await CreateExecutionGraphAsync(cancellationToken);
            _logger.LogDebug("ExecutionGraph created with {VertexCount} vertices", _executionGraph.ExecutionVertices.Count);

            // Step 2: Request resources from ResourceManager
            List<TaskSlot> allocatedSlots = await RequestResourcesAsync(cancellationToken);
            _logger.LogDebug("Allocated {SlotCount} slots for job execution", allocatedSlots.Count);

            // Step 3: Deploy tasks to TaskManagers
            await DeployTasksAsync(allocatedSlots, cancellationToken);

            // Step 4: Start execution monitoring
            _jobState = JobExecutionState.Running;
            _executionCts = new CancellationTokenSource();
            _ = Task.Run(() => MonitorExecutionAsync(_executionCts.Token), _executionCts.Token);

            _logger.LogInformation("Job {JobId} started successfully", _jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start job {JobId}", _jobId);
            _jobState = JobExecutionState.Failed;
            throw;
        }
    }

    public async Task CancelJobAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Canceling job {JobId}", _jobId);

        try
        {
            _jobState = JobExecutionState.Canceling;

            // Stop execution monitoring
            _executionCts?.Cancel();

            // Cancel all running tasks
            if (_executionGraph != null)
            {
                List<Task> cancelTasks = new();
                foreach (ExecutionVertex vertex in _executionGraph.ExecutionVertices)
                {
                    if (vertex.State == ExecutionState.Running || vertex.State == ExecutionState.Scheduled)
                    {
                        cancelTasks.Add(CancelTaskAsync(vertex.Id, cancellationToken));
                    }
                }

                await Task.WhenAll(cancelTasks);
            }

            // Release allocated resources
            await ReleaseResourcesAsync(cancellationToken);

            _jobState = JobExecutionState.Canceled;
            _logger.LogInformation("Job {JobId} canceled successfully", _jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel job {JobId}", _jobId);
            throw;
        }
    }

    public Task<ExecutionGraph> GetExecutionGraphAsync()
    {
        if (_executionGraph == null)
        {
            throw new InvalidOperationException($"ExecutionGraph not yet created for job {_jobId}");
        }

        return Task.FromResult(_executionGraph);
    }

    public async Task UpdateTaskStatusAsync(string executionVertexId, ExecutionState state, string? error = null, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Task {VertexId} status updated to {State}", executionVertexId, state);

        _taskStates[executionVertexId] = state;

        if (_executionGraph != null)
        {
            ExecutionVertex? vertex = _executionGraph.ExecutionVertices.FirstOrDefault(v => v.Id == executionVertexId);
            if (vertex != null)
            {
                vertex.State = state;
                vertex.Error = error;

                // Handle task failures
                if (state == ExecutionState.Failed)
                {
                    await HandleTaskFailureAsync(vertex, cancellationToken);
                }
                // Check if job is complete
                else if (state == ExecutionState.Finished)
                {
                    await CheckJobCompletionAsync(cancellationToken);
                }
            }
        }
    }

    public async Task TriggerCheckpointAsync(long checkpointId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Triggering checkpoint {CheckpointId} for job {JobId}", checkpointId, _jobId);

        try
        {
            // In a full implementation, this would:
            // 1. Coordinate checkpoint across all tasks
            // 2. Use Temporal workflow to persist state
            // 3. Track checkpoint progress
            // 4. Handle checkpoint completion/failure

            // For now, just log the checkpoint request
            _logger.LogDebug("Checkpoint {CheckpointId} coordination started", checkpointId);

            // TODO: Implement full checkpoint coordination with Temporal
            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to trigger checkpoint {CheckpointId} for job {JobId}", checkpointId, _jobId);
            throw;
        }
    }

    private async Task<ExecutionGraph> CreateExecutionGraphAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Creating ExecutionGraph from JobGraph");

        ExecutionGraph executionGraph = new()
        {
            JobId = _jobId,
            JobName = _jobGraph.JobName,
            ExecutionVertices = new List<ExecutionVertex>(),
            ExecutionEdges = new List<ExecutionEdge>()
        };

        // Create execution vertices (parallelized instances of job vertices)
        foreach (JobVertex jobVertex in _jobGraph.Vertices)
        {
            int parallelism = jobVertex.Parallelism;

            for (int i = 0; i < parallelism; i++)
            {
                ExecutionVertex executionVertex = new()
                {
                    Id = $"{jobVertex.Name}_{i}",
                    JobVertexId = jobVertex.Name,
                    SubtaskIndex = i,
                    Parallelism = parallelism,
                    OperatorType = jobVertex.OperatorType,
                    State = ExecutionState.Created,
                    AssignedSlot = null
                };

                executionGraph.ExecutionVertices.Add(executionVertex);
            }
        }

        // Create execution edges (data flow connections)
        foreach (JobEdge jobEdge in _jobGraph.Edges)
        {
            // Find source and target execution vertices
            List<ExecutionVertex> sourceVertices = executionGraph.ExecutionVertices
                .Where(v => v.JobVertexId == jobEdge.SourceVertexId)
                .ToList();

            List<ExecutionVertex> targetVertices = executionGraph.ExecutionVertices
                .Where(v => v.JobVertexId == jobEdge.TargetVertexId)
                .ToList();

            // Create edges based on partitioning strategy
            CreateExecutionEdges(executionGraph, sourceVertices, targetVertices, jobEdge.PartitioningStrategy);
        }

        _logger.LogDebug("ExecutionGraph created: {VertexCount} vertices, {EdgeCount} edges",
            executionGraph.ExecutionVertices.Count, executionGraph.ExecutionEdges.Count);

        return await Task.FromResult(executionGraph);
    }

    private void CreateExecutionEdges(
        ExecutionGraph executionGraph,
        List<ExecutionVertex> sourceVertices,
        List<ExecutionVertex> targetVertices,
        PartitioningStrategy strategy)
    {
        switch (strategy)
        {
            case PartitioningStrategy.Forward:
                // One-to-one connections (same parallelism required)
                for (int i = 0; i < Math.Min(sourceVertices.Count, targetVertices.Count); i++)
                {
                    executionGraph.ExecutionEdges.Add(new ExecutionEdge
                    {
                        SourceExecutionVertexId = sourceVertices[i].Id,
                        TargetExecutionVertexId = targetVertices[i].Id,
                        PartitioningStrategy = strategy
                    });
                }
                break;

            case PartitioningStrategy.Rebalance:
            case PartitioningStrategy.Hash:
                // All-to-all connections (any parallelism)
                foreach (ExecutionVertex source in sourceVertices)
                {
                    foreach (ExecutionVertex target in targetVertices)
                    {
                        executionGraph.ExecutionEdges.Add(new ExecutionEdge
                        {
                            SourceExecutionVertexId = source.Id,
                            TargetExecutionVertexId = target.Id,
                            PartitioningStrategy = strategy
                        });
                    }
                }
                break;

            case PartitioningStrategy.Broadcast:
                // Each source sends to all targets
                foreach (ExecutionVertex source in sourceVertices)
                {
                    foreach (ExecutionVertex target in targetVertices)
                    {
                        executionGraph.ExecutionEdges.Add(new ExecutionEdge
                        {
                            SourceExecutionVertexId = source.Id,
                            TargetExecutionVertexId = target.Id,
                            PartitioningStrategy = strategy
                        });
                    }
                }
                break;
        }
    }

    private async Task<List<TaskSlot>> RequestResourcesAsync(CancellationToken cancellationToken)
    {
        if (_executionGraph == null)
        {
            throw new InvalidOperationException("ExecutionGraph not created");
        }

        int requiredSlots = _executionGraph.ExecutionVertices.Count;
        _logger.LogDebug("Requesting {SlotCount} slots from ResourceManager", requiredSlots);

        List<TaskSlot> allocatedSlots = await _resourceManager.AllocateSlotsAsync(_jobId, requiredSlots, cancellationToken);

        if (allocatedSlots.Count < requiredSlots)
        {
            throw new InvalidOperationException(
                $"Insufficient resources: requested {requiredSlots} slots, got {allocatedSlots.Count}");
        }

        return allocatedSlots;
    }

    private async Task DeployTasksAsync(List<TaskSlot> slots, CancellationToken cancellationToken)
    {
        if (_executionGraph == null)
        {
            throw new InvalidOperationException("ExecutionGraph not created");
        }

        _logger.LogDebug("Deploying {TaskCount} tasks to TaskManagers", _executionGraph.ExecutionVertices.Count);

        List<Task> deploymentTasks = new();

        for (int i = 0; i < _executionGraph.ExecutionVertices.Count; i++)
        {
            ExecutionVertex vertex = _executionGraph.ExecutionVertices[i];
            TaskSlot slot = slots[i];

            vertex.AssignedSlot = slot;
            vertex.State = ExecutionState.Scheduled;

            // Create deployment descriptor
            TaskDeploymentDescriptor descriptor = new()
            {
                ExecutionVertexId = vertex.Id,
                JobVertexId = vertex.JobVertexId,
                SubtaskIndex = vertex.SubtaskIndex,
                Parallelism = vertex.Parallelism,
                OperatorType = vertex.OperatorType,
                AssignedSlot = slot,
                Configuration = new Dictionary<string, object>()
            };

            // Deploy to TaskManager
            deploymentTasks.Add(DeployTaskToTaskManagerAsync(descriptor, slot.TaskManagerId, cancellationToken));
        }

        await Task.WhenAll(deploymentTasks);
        _logger.LogInformation("All tasks deployed successfully");
    }

    private async Task DeployTaskToTaskManagerAsync(TaskDeploymentDescriptor descriptor, string taskManagerId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deploying task {VertexId} to TaskManager {TaskManagerId}",
            descriptor.ExecutionVertexId, taskManagerId);

        // In a full implementation, this would:
        // 1. Send deployment descriptor to TaskManager via RPC/HTTP
        // 2. Wait for acknowledgment
        // 3. Handle deployment failures

        // For now, simulate deployment
        await Task.Delay(100, cancellationToken);

        _logger.LogDebug("Task {VertexId} deployed to TaskManager {TaskManagerId}",
            descriptor.ExecutionVertexId, taskManagerId);
    }

    private async Task MonitorExecutionAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Starting execution monitoring for job {JobId}", _jobId);

        try
        {
            while (!cancellationToken.IsCancellationRequested && _jobState == JobExecutionState.Running)
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

                // Check for task failures or completion
                if (_executionGraph != null)
                {
                    int failedTasks = _executionGraph.ExecutionVertices.Count(v => v.State == ExecutionState.Failed);
                    if (failedTasks > 0)
                    {
                        _logger.LogWarning("Job {JobId} has {FailedTasks} failed tasks", _jobId, failedTasks);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Execution monitoring canceled for job {JobId}", _jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in execution monitoring for job {JobId}", _jobId);
        }
    }

    private async Task HandleTaskFailureAsync(ExecutionVertex vertex, CancellationToken cancellationToken)
    {
        _logger.LogError("Task {VertexId} failed: {Error}", vertex.Id, vertex.Error);

        // In a full implementation, this would:
        // 1. Determine if task should be restarted (based on restart strategy)
        // 2. Coordinate with Temporal for fault tolerance
        // 3. Potentially fail the entire job if restart limit exceeded

        // For now, mark job as failed if any task fails
        _jobState = JobExecutionState.Failed;
        _executionCts?.Cancel();

        await Task.CompletedTask;
    }

    private async Task CheckJobCompletionAsync(CancellationToken cancellationToken)
    {
        if (_executionGraph == null)
        {
            return;
        }

        // Check if all tasks are finished
        bool allFinished = _executionGraph.ExecutionVertices.All(v => v.State == ExecutionState.Finished);

        if (allFinished)
        {
            _logger.LogInformation("All tasks finished for job {JobId}", _jobId);
            _jobState = JobExecutionState.Finished;
            _executionCts?.Cancel();

            // Release resources
            await ReleaseResourcesAsync(cancellationToken);
        }
    }

    private async Task CancelTaskAsync(string executionVertexId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Canceling task {VertexId}", executionVertexId);

        // In a full implementation, this would:
        // 1. Send cancel request to TaskManager
        // 2. Wait for acknowledgment
        // 3. Handle cancellation failures

        await Task.Delay(100, cancellationToken);

        _taskStates[executionVertexId] = ExecutionState.Canceled;
    }

    private async Task ReleaseResourcesAsync(CancellationToken cancellationToken)
    {
        if (_executionGraph == null)
        {
            return;
        }

        _logger.LogDebug("Releasing {SlotCount} slots for job {JobId}",
            _executionGraph.ExecutionVertices.Count, _jobId);

        List<string> slotIds = _executionGraph.ExecutionVertices
            .Where(v => v.AssignedSlot != null)
            .Select(v => v.AssignedSlot!.SlotId)
            .ToList();

        foreach (string slotId in slotIds)
        {
            await _resourceManager.ReleaseSlotAsync(slotId, cancellationToken);
        }

        _logger.LogDebug("Resources released for job {JobId}", _jobId);
    }
}
