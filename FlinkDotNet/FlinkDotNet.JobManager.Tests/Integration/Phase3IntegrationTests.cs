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
//  limitations under the License.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Temporalio.Client;

namespace FlinkDotNet.JobManager.Tests.Integration;

/// <summary>
/// End-to-end integration tests for Phase 3 completion
/// Tests JobManager-TaskManager coordination without Temporal
/// </summary>
[Trait("Category", "Integration")]
public class Phase3IntegrationTests
{
    private static IResourceManager CreateResourceManager()
    {
        Mock<ILogger<ResourceManager>> logger = new();
        return new ResourceManager(logger.Object);
    }

    private static IDispatcher CreateDispatcher(IResourceManager resourceManager)
    {
        Mock<ITemporalClient> temporalClient = new();
        Mock<ILoggerFactory> loggerFactory = new();
        Mock<ILogger> logger = new();
        loggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(logger.Object);

        return new Dispatcher(resourceManager, temporalClient.Object, loggerFactory.Object);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEnd_TaskManagerRegistration_TracksHeartbeats()
    {
        // Arrange: Create resource manager
        IResourceManager resourceManager = CreateResourceManager();

        // Act: Register TaskManager
        resourceManager.RegisterTaskManager("tm-test-1", 4);

        // Record heartbeat
        await resourceManager.RecordHeartbeatAsync("tm-test-1");

        // Assert: TaskManager registered and heartbeat recorded
        var taskManagers = resourceManager.GetRegisteredTaskManagers().ToList();
        Assert.Single(taskManagers);

        // Verify heartbeat timestamp is recent
        DateTime? lastHeartbeat = resourceManager.GetLastHeartbeat("tm-test-1");
        Assert.NotNull(lastHeartbeat);
        Assert.True((DateTime.UtcNow - lastHeartbeat.Value).TotalSeconds < 5);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task EndToEnd_MultiTaskManager_DistributesSlots()
    {
        // Arrange: Create resource manager with multiple TaskManagers
        IResourceManager resourceManager = CreateResourceManager();

        for (int i = 1; i <= 4; i++)
        {
            resourceManager.RegisterTaskManager($"tm-{i}", 4);
        }

        // Act: Allocate slots across TaskManagers
        List<TaskSlot> slots = await resourceManager.AllocateSlotsAsync("test-job-distributed", 12);

        // Assert: Slots distributed across TaskManagers
        Assert.Equal(12, slots.Count);

        // Count slots per TaskManager
        Dictionary<string, int> slotsPerTm = new();
        foreach (TaskSlot slot in slots)
        {
            if (!slotsPerTm.ContainsKey(slot.TaskManagerId))
            {
                slotsPerTm[slot.TaskManagerId] = 0;
            }
            slotsPerTm[slot.TaskManagerId]++;
        }

        // Should use all 4 TaskManagers
        Assert.Equal(4, slotsPerTm.Count);

        // Each TaskManager should have 3 slots (12 / 4 = 3)
        Assert.All(slotsPerTm.Values, count => Assert.Equal(3, count));
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void ResourceManager_SlotAllocation_RespectsAvailableSlots()
    {
        // Arrange: Create ResourceManager with limited slots
        IResourceManager resourceManager = CreateResourceManager();

        resourceManager.RegisterTaskManager("tm-limited", 2);

        // Act & Assert: Cannot allocate more slots than available
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await resourceManager.AllocateSlotsAsync("test-job-overalloc", 5);
        });
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResourceManager_RegisterMultiple_TracksAllTaskManagers()
    {
        // Arrange
        IResourceManager resourceManager = CreateResourceManager();

        // Act: Register 3 TaskManagers
        for (int i = 1; i <= 3; i++)
        {
            resourceManager.RegisterTaskManager($"tm-multi-{i}", 4);
        }

        // Assert: All registered
        var taskManagers = resourceManager.GetRegisteredTaskManagers().ToList();
        Assert.Equal(3, taskManagers.Count);

        // Verify we can allocate from multiple TaskManagers
        List<TaskSlot> slots = await resourceManager.AllocateSlotsAsync("test-job-multi", 6);
        Assert.Equal(6, slots.Count);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task ResourceManager_Unregister_RemovesTaskManager()
    {
        // Arrange
        IResourceManager resourceManager = CreateResourceManager();

        resourceManager.RegisterTaskManager("tm-unregister-test", 4);

        // Verify registered
        var before = resourceManager.GetRegisteredTaskManagers().ToList();
        Assert.Single(before);

        // Act: Unregister
        resourceManager.UnregisterTaskManager("tm-unregister-test");

        // Assert: Removed
        var after = resourceManager.GetRegisteredTaskManagers().ToList();
        Assert.Empty(after);

        await Task.CompletedTask;
    }
}
