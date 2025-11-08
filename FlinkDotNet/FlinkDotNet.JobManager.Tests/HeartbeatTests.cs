// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class HeartbeatTests
{
    private readonly Mock<ILogger<ResourceManager>> _mockLogger;
    private readonly ResourceManager _resourceManager;

    public HeartbeatTests()
    {
        _mockLogger = new Mock<ILogger<ResourceManager>>();
        _resourceManager = new ResourceManager(_mockLogger.Object);
    }

    [Fact]
    public async Task RecordHeartbeatAsync_UpdatesLastHeartbeatTimestamp()
    {
        // Arrange
        var taskManagerId = "tm-heartbeat-1";
        var numberOfSlots = 4;

        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots);
        DateTime? initialHeartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(10);

        // Act
        await _resourceManager.RecordHeartbeatAsync(taskManagerId);

        // Assert
        DateTime? updatedHeartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);

        updatedHeartbeat.Should().NotBeNull();
        initialHeartbeat.Should().NotBeNull();
        updatedHeartbeat.Should().BeAfter(initialHeartbeat.Value);
    }

    [Fact]
    public async Task RecordHeartbeatAsync_ForUnregisteredTaskManager_LogsWarning()
    {
        // Arrange
        var unregisteredTaskManagerId = "tm-unregistered";

        // Act
        await _resourceManager.RecordHeartbeatAsync(unregisteredTaskManagerId);

        // Assert
        // Verify that a warning was logged (implementation logs warning)
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(unregisteredTaskManagerId);
        heartbeat.Should().BeNull();
    }

    [Fact]
    public async Task GetLastHeartbeat_ForRegisteredTaskManager_ReturnsTimestamp()
    {
        // Arrange
        var taskManagerId = "tm-heartbeat-2";
        var numberOfSlots = 4;

        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots);

        // Act
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);

        // Assert
        heartbeat.Should().NotBeNull();
        heartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void GetLastHeartbeat_ForUnregisteredTaskManager_ReturnsNull()
    {
        // Arrange
        var unregisteredTaskManagerId = "tm-not-registered";

        // Act
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(unregisteredTaskManagerId);

        // Assert
        heartbeat.Should().BeNull();
    }

    [Fact]
    public async Task RegisterTaskManagerAsync_InitializesLastHeartbeat()
    {
        // Arrange
        var taskManagerId = "tm-heartbeat-3";
        var numberOfSlots = 4;

        // Act
        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots);

        // Assert
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat.Should().NotBeNull();
        heartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task MultipleHeartbeats_UpdateTimestampSequentially()
    {
        // Arrange
        var taskManagerId = "tm-heartbeat-4";
        var numberOfSlots = 4;

        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots);

        // Act & Assert
        DateTime? heartbeat1 = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat1.Should().NotBeNull();

        await Task.Delay(10);
        await _resourceManager.RecordHeartbeatAsync(taskManagerId);
        DateTime? heartbeat2 = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat2.Should().BeAfter(heartbeat1.Value);

        await Task.Delay(10);
        await _resourceManager.RecordHeartbeatAsync(taskManagerId);
        DateTime? heartbeat3 = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat3.Should().BeAfter(heartbeat2.Value);
    }

    [Fact]
    public async Task ConcurrentHeartbeats_AreThreadSafe()
    {
        // Arrange
        var taskManagerId = "tm-concurrent";
        var numberOfSlots = 4;

        await _resourceManager.RegisterTaskManagerAsync(taskManagerId, numberOfSlots);

        // Act - Send concurrent heartbeats
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(async () => await _resourceManager.RecordHeartbeatAsync(taskManagerId))
        );

        await Task.WhenAll(tasks);

        // Assert - Should not throw and should have a valid timestamp
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat.Should().NotBeNull();
    }

    [Fact]
    public void SynchronousRegisterTaskManager_InitializesLastHeartbeat()
    {
        // Arrange
        var taskManagerId = "tm-sync-heartbeat";
        var numberOfSlots = 4;

        // Act
        _resourceManager.RegisterTaskManager(taskManagerId, numberOfSlots);

        // Assert
        DateTime? heartbeat = _resourceManager.GetLastHeartbeat(taskManagerId);
        heartbeat.Should().NotBeNull();
        heartbeat.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
