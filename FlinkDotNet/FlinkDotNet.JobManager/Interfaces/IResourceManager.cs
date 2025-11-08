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
/// ResourceManager manages TaskManager slots and resource allocation.
/// Equivalent to Apache Flink's ResourceManager component.
/// </summary>
public interface IResourceManager
{
    /// <summary>
    /// Register a TaskManager with the ResourceManager
    /// </summary>
    /// <param name="taskManagerId">Unique TaskManager identifier</param>
    /// <param name="numberOfSlots">Number of task slots available</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task RegisterTaskManagerAsync(string taskManagerId, int numberOfSlots, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unregister a TaskManager (on shutdown or failure)
    /// </summary>
    /// <param name="taskManagerId">TaskManager identifier</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task UnregisterTaskManagerAsync(string taskManagerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Request task slots for job execution
    /// </summary>
    /// <param name="jobId">Job identifier</param>
    /// <param name="numberOfSlots">Number of slots requested</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of allocated task slots</returns>
    Task<List<TaskSlot>> RequestSlotsAsync(string jobId, int numberOfSlots, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release task slots after job completion or failure
    /// </summary>
    /// <param name="slots">Slots to release</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ReleaseSlotsAsync(List<TaskSlot> slots, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current resource availability
    /// </summary>
    /// <returns>Number of available slots across all TaskManagers</returns>
    Task<int> GetAvailableSlotsAsync();
}
