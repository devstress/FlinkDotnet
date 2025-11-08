// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class ResourceManagerExtensionsTests
{
    private readonly Mock<ILogger<ResourceManager>> _mockLogger;
    private readonly ResourceManager _resourceManager;

    public ResourceManagerExtensionsTests()
    {
        _mockLogger = new Mock<ILogger<ResourceManager>>();
        _resourceManager = new ResourceManager(_mockLogger.Object);
    }

    [Fact]
    public void RegisterTaskManager_Synchronously_RegistersTaskManager()
    {
        // Arrange
        var taskManagerId = "tm-sync-1";
        var numberOfSlots = 4;

        // Act
        _resourceManager.RegisterTaskManager(taskManagerId, numberOfSlots);

        // Assert
        var registeredManagers = _resourceManager.GetRegisteredTaskManagers();
        registeredManagers.Should().Contain(taskManagerId);
    }

    [Fact]
    public void UnregisterTaskManager_Synchronously_UnregistersTaskManager()
    {
        // Arrange
        var taskManagerId = "tm-sync-2";
        _resourceManager.RegisterTaskManager(taskManagerId, 2);

        // Act
        var result = _resourceManager.UnregisterTaskManager(taskManagerId);

        // Assert
        result.Should().BeTrue();
        var registeredManagers = _resourceManager.GetRegisteredTaskManagers();
        registeredManagers.Should().NotContain(taskManagerId);
    }

    [Fact]
    public void GetRegisteredTaskManagers_ReturnsDistinctTaskManagerIds()
    {
        // Arrange
        _resourceManager.RegisterTaskManager("tm-1", 3);
        _resourceManager.RegisterTaskManager("tm-2", 2);
        _resourceManager.RegisterTaskManager("tm-3", 5);

        // Act
        var registeredManagers = _resourceManager.GetRegisteredTaskManagers().ToList();

        // Assert
        registeredManagers.Should().HaveCount(3);
        registeredManagers.Should().Contain("tm-1");
        registeredManagers.Should().Contain("tm-2");
        registeredManagers.Should().Contain("tm-3");
    }

    [Fact]
    public void GetRegisteredTaskManagers_WithNoRegistrations_ReturnsEmpty()
    {
        // Act
        var registeredManagers = _resourceManager.GetRegisteredTaskManagers();

        // Assert
        registeredManagers.Should().BeEmpty();
    }
}
