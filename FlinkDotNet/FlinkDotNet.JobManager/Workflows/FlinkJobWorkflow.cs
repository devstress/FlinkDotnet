//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
// limitations under the License.

using FlinkDotNet.JobManager.Activities;
using FlinkDotNet.JobManager.Models;
using Temporalio.Workflows;

namespace FlinkDotNet.JobManager.Workflows;

/// <summary>
/// Temporal workflow for executing a Flink job.
/// Orchestrates task execution across TaskManagers with fault tolerance.
/// Replaces Apache Flink's job execution coordination with Temporal durable execution.
/// </summary>
[Workflow]
public class FlinkJobWorkflow
{
    private JobExecutionState _currentState = JobExecutionState.Created;
    private List<string> _deployedTasks = new();
    private Dictionary<string, ExecutionState> _taskStates = new();

    /// <summary>
    /// Main workflow entry point - executes the Flink job
    /// </summary>
    /// <param name="jobGraph">Job graph defining the job structure</param>
    /// <returns>Job execution result</returns>
    [WorkflowRun]
    public async Task<JobExecutionResult> ExecuteJobAsync(JobGraph jobGraph)
    {
        _currentState = JobExecutionState.Running;

        try
        {
            // Convert JobGraph to ExecutionGraph (add parallel task instances)
            ExecutionGraph executionGraph = await this.CreateExecutionGraphAsync(jobGraph);

            // Request resources from ResourceManager
            List<TaskSlot> allocatedSlots = await this.RequestResourcesAsync(jobGraph.JobId, executionGraph);

            // Deploy tasks to TaskManagers
            await this.DeployTasksAsync(executionGraph, allocatedSlots);

            // Monitor task execution and handle failures
            await this.MonitorTaskExecutionAsync(jobGraph.JobId);

            _currentState = JobExecutionState.Finished;
            return new JobExecutionResult
            {
                JobId = jobGraph.JobId,
                Success = true,
                State = _currentState
            };
        }
        catch (Exception ex)
        {
            _currentState = JobExecutionState.Failed;
            return new JobExecutionResult
            {
                JobId = jobGraph.JobId,
                Success = false,
                State = _currentState,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Signal to cancel the job
    /// </summary>
    [WorkflowSignal]
    public async Task CancelJobSignalAsync()
    {
        _currentState = JobExecutionState.Canceling;

        // Cancel all running tasks
        foreach (string taskId in _deployedTasks)
        {
            // Activity to cancel task would be called here
            _taskStates[taskId] = ExecutionState.Canceled;
        }

        _currentState = JobExecutionState.Canceled;
        await Task.CompletedTask;
    }

    /// <summary>
    /// Query for current job state
    /// </summary>
    [WorkflowQuery]
    public JobExecutionState GetJobState() => _currentState;

    /// <summary>
    /// Query for task execution states
    /// </summary>
    [WorkflowQuery]
    public Dictionary<string, ExecutionState> GetTaskStates() => _taskStates;

    private Task<ExecutionGraph> CreateExecutionGraphAsync(JobGraph jobGraph)
    {
        // Convert logical JobGraph to physical ExecutionGraph
        // Each JobVertex with parallelism N becomes N ExecutionVertices
        ExecutionGraph executionGraph = new()
        {
            JobId = jobGraph.JobId,
            State = JobExecutionState.Running,
            StartedAt = DateTime.UtcNow
        };

        foreach (JobVertex vertex in jobGraph.Vertices)
        {
            for (int i = 0; i < vertex.Parallelism; i++)
            {
                ExecutionVertex execVertex = new()
                {
                    ExecutionVertexId = $"{vertex.VertexId}-{i}",
                    JobVertexId = vertex.VertexId,
                    SubtaskIndex = i,
                    OperatorName = vertex.OperatorName,
                    State = ExecutionState.Created
                };
                executionGraph.ExecutionVertices.Add(execVertex);
                _taskStates[execVertex.ExecutionVertexId] = ExecutionState.Created;
            }
        }

        return Task.FromResult(executionGraph);
    }

    private async Task<List<TaskSlot>> RequestResourcesAsync(string jobId, ExecutionGraph executionGraph)
    {
        // Call ResourceManager activity to allocate slots
        List<TaskSlot> slots = await Workflow.ExecuteActivityAsync(
            (TaskExecutionActivity act) => act.RequestTaskSlotsAsync(jobId, executionGraph.ExecutionVertices.Count),
            new ActivityOptions
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(2),
                RetryPolicy = new()
                {
                    InitialInterval = TimeSpan.FromSeconds(1),
                    MaximumInterval = TimeSpan.FromSeconds(30),
                    BackoffCoefficient = 2.0f,
                    MaximumAttempts = 3
                }
            });

        return slots;
    }

    private async Task DeployTasksAsync(ExecutionGraph executionGraph, List<TaskSlot> allocatedSlots)
    {
        // Deploy each execution vertex to its assigned slot via activity
        for (int i = 0; i < executionGraph.ExecutionVertices.Count; i++)
        {
            ExecutionVertex vertex = executionGraph.ExecutionVertices[i];
            vertex.AssignedSlot = allocatedSlots[i];
            vertex.State = ExecutionState.Scheduled;
            this._taskStates[vertex.ExecutionVertexId] = ExecutionState.Scheduled;
            this._deployedTasks.Add(vertex.ExecutionVertexId);

            // Create task deployment descriptor
            FlinkDotNet.TaskManager.Models.TaskDeploymentDescriptor descriptor = new()
            {
                ExecutionVertexId = vertex.ExecutionVertexId,
                JobId = executionGraph.JobId,
                JobVertexId = vertex.JobVertexId,
                SubtaskIndex = vertex.SubtaskIndex,
                Parallelism = vertex.Parallelism,
                OperatorName = vertex.OperatorName
            };

            // Deploy task via activity (async, don't wait for completion here)
            _ = Workflow.ExecuteActivityAsync(
                (TaskExecutionActivity act) => act.ExecuteTaskAsync(descriptor),
                new ActivityOptions
                {
                    StartToCloseTimeout = TimeSpan.FromMinutes(30),
                    HeartbeatTimeout = TimeSpan.FromSeconds(30),
                    RetryPolicy = new()
                    {
                        InitialInterval = TimeSpan.FromSeconds(2),
                        MaximumInterval = TimeSpan.FromMinutes(1),
                        BackoffCoefficient = 2.0f,
                        MaximumAttempts = 5
                    }
                });
        }

        await Task.CompletedTask;
    }

    private async Task MonitorTaskExecutionAsync(string jobId)
    {
        _ = jobId; // Parameter used for context
        
        // Monitor task execution - update states as tasks progress
        // In a real implementation, this would receive status updates from activities
        // For now, simulate monitoring by waiting for tasks to reach expected state
        
        foreach (string taskId in this._deployedTasks)
        {
            this._taskStates[taskId] = ExecutionState.Running;
        }

        // Wait for all tasks to complete or fail
        // In production, this would be event-driven based on activity completion
        await Workflow.DelayAsync(TimeSpan.FromSeconds(5));

        // Update task states based on job state
        foreach (string taskId in this._deployedTasks)
        {
            if (this._currentState == JobExecutionState.Canceling ||
                this._currentState == JobExecutionState.Canceled)
            {
                this._taskStates[taskId] = ExecutionState.Canceled;
            }
            else
            {
                // Assume tasks complete successfully if not canceled
                this._taskStates[taskId] = ExecutionState.Finished;
            }
        }

        await Task.CompletedTask;
    }
}

/// <summary>
/// Result of job execution
/// </summary>
public class JobExecutionResult
{
    /// <summary>
    /// Job identifier
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Whether job succeeded
    /// </summary>
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Final job state
    /// </summary>
    public JobExecutionState State
    {
        get; set;
    }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage
    {
        get; set;
    }
}
