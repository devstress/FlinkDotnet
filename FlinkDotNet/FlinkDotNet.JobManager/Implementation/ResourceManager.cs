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

using System.Collections.Concurrent;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;

namespace FlinkDotNet.JobManager.Implementation;

/// <summary>
/// Resource manager implementation for managing TaskManager slots.
/// Thread-safe implementation using concurrent collections.
/// </summary>
public class ResourceManager : IResourceManager
{
    private readonly ILogger<ResourceManager> _logger;
    private readonly ConcurrentDictionary<string, TaskManagerInfo> _taskManagers = new();
    private readonly ConcurrentDictionary<string, List<TaskSlot>> _jobAllocations = new();

    /// <summary>
    /// Constructor for ResourceManager
    /// </summary>
    /// <param name="logger">Logger instance</param>
    public ResourceManager(ILogger<ResourceManager> logger)
    {
        this._logger = logger;
    }

    /// <inheritdoc/>
    public Task RegisterTaskManagerAsync(string taskManagerId, int numberOfSlots, CancellationToken cancellationToken = default)
    {
        TaskManagerInfo info = new()
        {
            TaskManagerId = taskManagerId,
            TotalSlots = numberOfSlots,
            AvailableSlots = numberOfSlots,
            RegisteredAt = DateTime.UtcNow
        };

        if (this._taskManagers.TryAdd(taskManagerId, info))
        {
            this._logger.LogInformation(
                "TaskManager {TaskManagerId} registered with {NumberOfSlots} slots",
                taskManagerId,
                numberOfSlots);
        }
        else
        {
            this._logger.LogWarning(
                "TaskManager {TaskManagerId} already registered, updating slot count",
                taskManagerId);
            this._taskManagers[taskManagerId] = info;
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UnregisterTaskManagerAsync(string taskManagerId, CancellationToken cancellationToken = default)
    {
        if (this._taskManagers.TryRemove(taskManagerId, out TaskManagerInfo? _))
        {
            this._logger.LogInformation(
                "TaskManager {TaskManagerId} unregistered",
                taskManagerId);
        }
        else
        {
            this._logger.LogWarning(
                "Attempted to unregister unknown TaskManager {TaskManagerId}",
                taskManagerId);
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<List<TaskSlot>> RequestSlotsAsync(string jobId, int numberOfSlots, CancellationToken cancellationToken = default)
    {
        this._logger.LogInformation(
            "Job {JobId} requesting {NumberOfSlots} slots",
            jobId,
            numberOfSlots);

        List<TaskSlot> allocatedSlots = new();
        int remainingSlots = numberOfSlots;

        // Allocate slots from available TaskManagers
        foreach (KeyValuePair<string, TaskManagerInfo> tm in this._taskManagers)
        {
            if (remainingSlots == 0)
                break;

            TaskManagerInfo info = tm.Value;
            int slotsToAllocate = Math.Min(remainingSlots, info.AvailableSlots);

            for (int i = 0; i < slotsToAllocate; i++)
            {
                TaskSlot slot = new()
                {
                    TaskManagerId = info.TaskManagerId,
                    SlotNumber = info.TotalSlots - info.AvailableSlots + i,
                    IsAllocated = true
                };
                allocatedSlots.Add(slot);
            }

            info.AvailableSlots -= slotsToAllocate;
            remainingSlots -= slotsToAllocate;
        }

        if (remainingSlots > 0)
        {
            this._logger.LogWarning(
                "Could not allocate all requested slots for job {JobId}. Still need {RemainingSlots} slots",
                jobId,
                remainingSlots);
        }

        this._jobAllocations[jobId] = allocatedSlots;

        this._logger.LogInformation(
            "Allocated {AllocatedCount} slots for job {JobId}",
            allocatedSlots.Count,
            jobId);

        return Task.FromResult(allocatedSlots);
    }

    /// <inheritdoc/>
    public Task ReleaseSlotsAsync(List<TaskSlot> slots, CancellationToken cancellationToken = default)
    {
        foreach (TaskSlot slot in slots)
        {
            if (this._taskManagers.TryGetValue(slot.TaskManagerId, out TaskManagerInfo? info))
            {
                info.AvailableSlots++;
                this._logger.LogDebug(
                    "Released slot {SlotNumber} on TaskManager {TaskManagerId}",
                    slot.SlotNumber,
                    slot.TaskManagerId);
            }
        }

        this._logger.LogInformation("Released {SlotCount} slots", slots.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<int> GetAvailableSlotsAsync()
    {
        int totalAvailable = this._taskManagers.Values.Sum(tm => tm.AvailableSlots);
        return Task.FromResult(totalAvailable);
    }

    /// <inheritdoc/>
    public IEnumerable<TaskSlot> GetAvailableSlots()
    {
        List<TaskSlot> availableSlots = new();
        foreach (KeyValuePair<string, TaskManagerInfo> tm in _taskManagers)
        {
            for (int i = 0; i < tm.Value.AvailableSlots; i++)
            {
                availableSlots.Add(new TaskSlot
                {
                    TaskManagerId = tm.Key,
                    SlotNumber = i
                });
            }
        }
        return availableSlots;
    }

    /// <inheritdoc/>
    public IEnumerable<TaskSlot> GetAllSlots()
    {
        List<TaskSlot> allSlots = new();
        foreach (KeyValuePair<string, TaskManagerInfo> tm in _taskManagers)
        {
            for (int i = 0; i < tm.Value.TotalSlots; i++)
            {
                allSlots.Add(new TaskSlot
                {
                    TaskManagerId = tm.Key,
                    SlotNumber = i
                });
            }
        }
        return allSlots;
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetRegisteredTaskManagers()
    {
        return this._taskManagers.Keys;
    }

    /// <inheritdoc/>
    public void RegisterTaskManager(string taskManagerId, int numberOfSlots)
    {
        TaskManagerInfo info = new()
        {
            TaskManagerId = taskManagerId,
            TotalSlots = numberOfSlots,
            AvailableSlots = numberOfSlots,
            RegisteredAt = DateTime.UtcNow
        };

        if (this._taskManagers.TryAdd(taskManagerId, info))
        {
            this._logger.LogInformation(
                "TaskManager {TaskManagerId} registered with {NumberOfSlots} slots",
                taskManagerId,
                numberOfSlots);
        }
        else
        {
            this._logger.LogWarning(
                "TaskManager {TaskManagerId} already registered",
                taskManagerId);
        }
    }

    /// <inheritdoc/>
    public bool UnregisterTaskManager(string taskManagerId)
    {
        if (this._taskManagers.TryRemove(taskManagerId, out _))
        {
            this._logger.LogInformation(
                "TaskManager {TaskManagerId} unregistered",
                taskManagerId);
            return true;
        }

        this._logger.LogWarning(
            "TaskManager {TaskManagerId} not found for unregistration",
            taskManagerId);
        return false;
    }

    /// <inheritdoc/>
    public async Task<List<TaskSlot>> AllocateSlotsAsync(string jobId, int numberOfSlots, CancellationToken cancellationToken = default)
    {
        // This is the same as RequestSlotsAsync - delegate to it
        return await RequestSlotsAsync(jobId, numberOfSlots, cancellationToken);
    }

    /// <inheritdoc/>
    public Task ReleaseSlotAsync(string slotId, CancellationToken cancellationToken = default)
    {
        // Find the slot by ID and release it
        // In a full implementation, we'd track slots by ID
        // For now, just log and return
        this._logger.LogDebug("Releasing slot {SlotId}", slotId);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Information about a registered TaskManager
/// </summary>
internal class TaskManagerInfo
{
    /// <summary>
    /// TaskManager identifier
    /// </summary>
    public string TaskManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Total number of slots
    /// </summary>
    public int TotalSlots
    {
        get; set;
    }

    /// <summary>
    /// Currently available slots
    /// </summary>
    public int AvailableSlots
    {
        get; set;
    }

    /// <summary>
    /// Registration timestamp
    /// </summary>
    public DateTime RegisteredAt
    {
        get; set;
    }
}

// Extension methods for synchronous API compatibility
public static class ResourceManagerExtensions
{
    /// <summary>
    /// Register a TaskManager synchronously.
    /// </summary>
    public static void RegisterTaskManager(this IResourceManager resourceManager, string taskManagerId, int numberOfSlots)
    {
        resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Unregister a TaskManager synchronously.
    /// </summary>
    public static bool UnregisterTaskManager(this IResourceManager resourceManager, string taskManagerId)
    {
        resourceManager.UnregisterTaskManagerAsync(taskManagerId).GetAwaiter().GetResult();
        return true; // Task<T> returns void, assume success if no exception
    }

    /// <summary>
    /// Get list of registered TaskManager IDs.
    /// </summary>
    public static IEnumerable<string> GetRegisteredTaskManagers(this IResourceManager resourceManager)
    {
        // Get from GetAllSlots
        return resourceManager.GetAllSlots()
            .Select(s => s.TaskManagerId)
            .Distinct();
    }
}
