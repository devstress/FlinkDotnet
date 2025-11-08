// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class ResourceManagerTests
{
    private readonly Mock<ILogger<ResourceManager>> _mockLogger;
    private readonly ResourceManager _resourceManager;

    public ResourceManagerTests()
    {
        _mockLogger = new Mock<ILogger<ResourceManager>>();
        _resourceManager = new ResourceManager(_mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ResourceManager(null!);
        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task RegisterTaskManagerAsync_AddsNewTaskManager()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var slotsPerTaskManager = 4;

        // Act
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, slotsPerTaskManager);

        // Assert
        var taskManagers = _resourceManager.GetRegisteredTaskManagers();
        taskManagers.Should().Contain(taskManagerId);
    }

    [Fact]
    public async Task RegisterTaskManagerAsync_CreatesCorrectNumberOfSlots()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var slotsPerTaskManager = 4;

        // Act
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, slotsPerTaskManager);

        // Assert
        var allSlots = _resourceManager.GetAllSlots();
        var tmSlots = allSlots.Where(s => s.TaskManagerId == taskManagerId).ToList();
        tmSlots.Should().HaveCount(slotsPerTaskManager);
    }

    [Fact]
    public async Task UnregisterTaskManagerAsync_RemovesTaskManager()
    {
        // Arrange
        var taskManagerId = "tm-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);

        // Act
        await _resourceManager.UnregisterTaskManagerAsync(taskManagerId);

        // Assert
        var taskManagers = _resourceManager.GetRegisteredTaskManagers();
        taskManagers.Should().NotContain(taskManagerId);
    }

    [Fact]
    public async Task AllocateSlotsAsync_AllocatesRequestedSlots()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var jobId = "job-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);

        // Act
        var allocatedSlots = await _resourceManager.AllocateSlotsAsync(jobId, 2);

        // Assert
        allocatedSlots.Should().HaveCount(2);
        allocatedSlots.All(s => s.IsAllocated).Should().BeTrue();
    }

    [Fact]
    public async Task AllocateSlotsAsync_ReducesAvailableSlots()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var jobId = "job-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);
        var initialAvailable = _resourceManager.GetAvailableSlots().Count();

        // Act
        await _resourceManager.AllocateSlotsAsync(jobId, 2);

        // Assert
        var remainingAvailable = _resourceManager.GetAvailableSlots().Count();
        remainingAvailable.Should().Be(initialAvailable - 2);
    }

    [Fact]
    public async Task AllocateSlotsAsync_WithInsufficientSlots_ReturnsEmptyList()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var jobId = "job-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 2);

        // Act
        var allocatedSlots = await _resourceManager.AllocateSlotsAsync(jobId, 10);

        // Assert
        allocatedSlots.Should().BeEmpty();
    }

    [Fact]
    public async Task ReleaseSlotAsync_ReleasesAllocatedSlot()
    {
        // Arrange
        var taskManagerId = "tm-1";
        var jobId = "job-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);
        var allocatedSlots = await _resourceManager.AllocateSlotsAsync(jobId, 2);
        var slotToRelease = allocatedSlots.First();
        var initialAvailable = _resourceManager.GetAvailableSlots().Count();

        // Act
        await _resourceManager.ReleaseSlotAsync(slotToRelease.SlotId);

        // Assert
        var currentAvailable = _resourceManager.GetAvailableSlots().Count();
        currentAvailable.Should().Be(initialAvailable + 1);
    }

    [Fact]
    public void GetRegisteredTaskManagers_ReturnsAllRegistered()
    {
        // Arrange
        _resourceManager.RegisterTaskManagerAsync("tm-1", 2).Wait();
        _resourceManager.RegisterTaskManagerAsync("tm-2", 2).Wait();

        // Act
        var taskManagers = _resourceManager.GetRegisteredTaskManagers().ToList();

        // Assert
        taskManagers.Should().HaveCount(2);
        taskManagers.Should().Contain("tm-1");
        taskManagers.Should().Contain("tm-2");
    }

    [Fact]
    public void GetAllSlots_ReturnsAllSlots()
    {
        // Arrange
        _resourceManager.RegisterTaskManagerAsync("tm-1", 2).Wait();
        _resourceManager.RegisterTaskManagerAsync("tm-2", 3).Wait();

        // Act
        var allSlots = _resourceManager.GetAllSlots().ToList();

        // Assert
        allSlots.Should().HaveCount(5);
    }

    [Fact]
    public void GetAvailableSlots_ReturnsOnlyFreeSlots()
    {
        // Arrange
        _resourceManager.RegisterTaskManagerAsync("tm-1", 4).Wait();
        _resourceManager.AllocateSlotsAsync("job-1", 2).Wait();

        // Act
        var availableSlots = _resourceManager.GetAvailableSlots().ToList();

        // Assert
        availableSlots.Should().HaveCount(2);
        availableSlots.All(s => !s.IsAllocated).Should().BeTrue();
    }

    [Fact]
    public async Task RequestSlotsAsync_ReturnsTaskSlotList()
    {
        // Arrange
        var taskManagerId = "tm-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);

        // Act
        var slots = await _resourceManager.RequestSlotsAsync("job-1", 2);

        // Assert
        slots.Should().HaveCount(2);
        slots.All(s => s.TaskManagerId == taskManagerId).Should().BeTrue();
    }

    [Fact]
    public async Task ReleaseSlotsAsync_ReleasesMultipleSlots()
    {
        // Arrange
        var taskManagerId = "tm-1";
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, 4);
        var allocatedSlots = await _resourceManager.AllocateSlotsAsync("job-1", 3);
        var initialAvailable = _resourceManager.GetAvailableSlots().Count();

        // Act
        await _resourceManager.ReleaseSlotsAsync(allocatedSlots);

        // Assert
        var currentAvailable = _resourceManager.GetAvailableSlots().Count();
        currentAvailable.Should().Be(initialAvailable + 3);
    }
}
