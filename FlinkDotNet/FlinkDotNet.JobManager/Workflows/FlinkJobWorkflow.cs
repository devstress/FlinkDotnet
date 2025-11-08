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
        _ = jobId; // Parameter reserved for future use - will be used for resource management tracking
        // This would call ResourceManager activity
        // For now, simulate slot allocation
        List<TaskSlot> slots = new();
        for (int i = 0; i < executionGraph.ExecutionVertices.Count; i++)
        {
            slots.Add(new TaskSlot
            {
                TaskManagerId = $"tm-{i % 4}", // Distribute across 4 TaskManagers
                SlotNumber = i / 4,
                IsAllocated = true
            });
        }
        return await Task.FromResult(slots);
    }

    private Task DeployTasksAsync(ExecutionGraph executionGraph, List<TaskSlot> allocatedSlots)
    {
        // Deploy each execution vertex to its assigned slot
        for (int i = 0; i < executionGraph.ExecutionVertices.Count; i++)
        {
            ExecutionVertex vertex = executionGraph.ExecutionVertices[i];
            vertex.AssignedSlot = allocatedSlots[i];
            vertex.State = ExecutionState.Scheduled;
            _taskStates[vertex.ExecutionVertexId] = ExecutionState.Scheduled;
            _deployedTasks.Add(vertex.ExecutionVertexId);
        }
        return Task.CompletedTask;
    }

    private async Task MonitorTaskExecutionAsync(string jobId)
    {
        _ = jobId; // Parameter reserved for future use - will be used for monitoring and logging
                   // Monitor task execution and handle failures
                   // This would poll task status or receive updates
                   // Implement fault tolerance and recovery here

        // Simulate task execution
        foreach (string taskId in _deployedTasks)
        {
            _taskStates[taskId] = ExecutionState.Running;
            await Workflow.DelayAsync(TimeSpan.FromMilliseconds(100)); // Simulate work
            _taskStates[taskId] = ExecutionState.Finished;
        }
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
