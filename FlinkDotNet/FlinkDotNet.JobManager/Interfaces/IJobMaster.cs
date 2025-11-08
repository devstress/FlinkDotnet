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

namespace FlinkDotNet.JobManager.Interfaces;

/// <summary>
/// JobMaster manages the execution of a single job.
/// Equivalent to Apache Flink's JobMaster component.
/// </summary>
public interface IJobMaster
{
    /// <summary>
    /// Job identifier managed by this JobMaster
    /// </summary>
    string JobId { get; }

    /// <summary>
    /// Start job execution
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task StartJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancel the job
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    Task CancelJobAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current execution graph state
    /// </summary>
    /// <returns>Execution graph representing physical execution plan</returns>
    Task<ExecutionGraph> GetExecutionGraphAsync();

    /// <summary>
    /// Handle task status update from TaskManager
    /// </summary>
    /// <param name="executionVertexId">Execution vertex identifier</param>
    /// <param name="state">New execution state</param>
    /// <param name="error">Error message if failed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UpdateTaskStatusAsync(string executionVertexId, ExecutionState state, string? error = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trigger checkpoint for fault tolerance
    /// </summary>
    /// <param name="checkpointId">Checkpoint identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task TriggerCheckpointAsync(long checkpointId, CancellationToken cancellationToken = default);
}
