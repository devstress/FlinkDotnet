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
using Temporalio.Activities;

namespace FlinkDotNet.JobManager.Activities;

/// <summary>
/// Temporal activity for executing a single task on a TaskManager.
/// Represents the actual data processing execution (map, filter, etc.).
/// </summary>
public class TaskExecutionActivity
{
    private readonly ILogger<TaskExecutionActivity> _logger;

    /// <summary>
    /// Constructor for TaskExecutionActivity
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public TaskExecutionActivity(ILogger<TaskExecutionActivity> logger)
    {
        this._logger = logger;
    }

    /// <summary>
    /// Execute a task deployment on a TaskManager
    /// </summary>
    /// <param name="descriptor">Task deployment descriptor</param>
    /// <returns>Task execution result</returns>
    [Activity]
    public async Task<TaskExecutionResult> ExecuteTaskAsync(FlinkDotNet.TaskManager.Models.TaskDeploymentDescriptor descriptor)
    {
        this._logger.LogInformation(
            "Executing task {ExecutionVertexId} on TaskManager (subtask {SubtaskIndex}/{Parallelism})",
            descriptor.ExecutionVertexId,
            descriptor.SubtaskIndex,
            descriptor.Parallelism);

        try
        {
            // Heartbeat to Temporal to show activity is alive
            ActivityExecutionContext.Current.Heartbeat();

            // Simulate task execution
            // In real implementation, this would:
            // 1. Deserialize operator logic
            // 2. Set up input/output channels
            // 3. Process data stream
            // 4. Handle backpressure
            // 5. Report progress to JobMaster

            await Task.Delay(TimeSpan.FromSeconds(1)); // Simulate processing

            // Send heartbeat periodically for long-running tasks
            ActivityExecutionContext.Current.Heartbeat(new
            {
                Progress = 0.5
            });

            await Task.Delay(TimeSpan.FromSeconds(1)); // Simulate more processing

            this._logger.LogInformation(
                "Task {ExecutionVertexId} completed successfully",
                descriptor.ExecutionVertexId);

            return new TaskExecutionResult
            {
                ExecutionVertexId = descriptor.ExecutionVertexId,
                Success = true,
                RecordsProcessed = 1000, // Simulated
                BytesProcessed = 10000 // Simulated
            };
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex,
                "Task {ExecutionVertexId} failed: {ErrorMessage}",
                descriptor.ExecutionVertexId,
                ex.Message);

            return new TaskExecutionResult
            {
                ExecutionVertexId = descriptor.ExecutionVertexId,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Request task slots from ResourceManager
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="numberOfSlots">Number of slots to request</param>
    /// <returns>List of allocated task slots</returns>
    [Activity]
    public async Task<List<TaskSlot>> RequestTaskSlotsAsync(string jobId, int numberOfSlots)
    {
        this._logger.LogInformation(
            "Requesting {NumberOfSlots} slots for job {JobId}",
            numberOfSlots,
            jobId);

        // In real implementation, this would call ResourceManager via HTTP
        // For now, simulate slot allocation across TaskManagers
        List<TaskSlot> allocatedSlots = new();
        for (int i = 0; i < numberOfSlots; i++)
        {
            allocatedSlots.Add(new TaskSlot
            {
                TaskManagerId = $"tm-{i % 4}", // Distribute across 4 TaskManagers
                SlotNumber = i / 4,
                IsAllocated = true,
                SlotId = $"slot-{i}",
                AllocatedJobId = jobId
            });
        }

        await Task.CompletedTask;
        return allocatedSlots;
    }

    /// <summary>
    /// Cancel a running task
    /// </summary>
    /// <param name="executionVertexId">Task execution vertex ID</param>
    /// <returns>Whether cancellation succeeded</returns>
    [Activity]
    public async Task<bool> CancelTaskAsync(string executionVertexId)
    {
        this._logger.LogInformation(
            "Canceling task {ExecutionVertexId}",
            executionVertexId);

        // In real implementation, send cancellation signal to TaskManager
        await Task.CompletedTask;
        return true;
    }
}

/// <summary>
/// Result of task execution
/// </summary>
public class TaskExecutionResult
{
    /// <summary>
    /// Execution vertex identifier
    /// </summary>
    public string ExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Whether execution succeeded
    /// </summary>
    public bool Success
    {
        get; set;
    }

    /// <summary>
    /// Number of records processed
    /// </summary>
    public long RecordsProcessed
    {
        get; set;
    }

    /// <summary>
    /// Bytes processed
    /// </summary>
    public long BytesProcessed
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
