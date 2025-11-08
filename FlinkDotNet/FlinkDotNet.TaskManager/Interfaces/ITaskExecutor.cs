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

using FlinkDotNet.TaskManager.Models;

namespace FlinkDotNet.TaskManager.Interfaces;

/// <summary>
/// TaskExecutor executes tasks assigned to this TaskManager.
/// Equivalent to Apache Flink's TaskExecutor component.
/// </summary>
public interface ITaskExecutor
{
    /// <summary>
    /// Deploy a task to this TaskManager
    /// </summary>
    /// <param name="descriptor">Task deployment descriptor</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task DeployTaskAsync(TaskDeploymentDescriptor descriptor, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel a running task
    /// </summary>
    /// <param name="executionVertexId">Execution vertex identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelTaskAsync(string executionVertexId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current task execution status
    /// </summary>
    /// <param name="executionVertexId">Execution vertex identifier</param>
    /// <returns>Task status information</returns>
    Task<TaskExecutionStatus> GetTaskStatusAsync(string executionVertexId);
}

/// <summary>
/// Task execution status
/// </summary>
public class TaskExecutionStatus
{
    /// <summary>
    /// Execution vertex identifier
    /// </summary>
    public string ExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Current execution state
    /// </summary>
    public string State { get; set; } = string.Empty;

    /// <summary>
    /// Records processed
    /// </summary>
    public long RecordsProcessed { get; set; }

    /// <summary>
    /// Bytes processed
    /// </summary>
    public long BytesProcessed { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}
