// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class HeartbeatMonitoringServiceTests
{
    private readonly Mock<IResourceManager> _mockResourceManager;
    private readonly Mock<ILogger<HeartbeatMonitoringService>> _mockLogger;
    private readonly HeartbeatConfiguration _configuration;

    public HeartbeatMonitoringServiceTests()
    {
        _mockResourceManager = new Mock<IResourceManager>();
        _mockLogger = new Mock<ILogger<HeartbeatMonitoringService>>();
        _configuration = new HeartbeatConfiguration
        {
            TimeoutSeconds = 2,  // Short timeout for testing
            CheckIntervalSeconds = 1  // Short interval for testing
        };
    }

    [Fact]
    public void Constructor_WithNullResourceManager_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new HeartbeatMonitoringService(
            null!,
            Options.Create(_configuration),
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("resourceManager");
    }

    [Fact]
    public void Constructor_WithNullConfiguration_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new HeartbeatMonitoringService(
            _mockResourceManager.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Arrange & Act & Assert
        var act = () => new HeartbeatMonitoringService(
            _mockResourceManager.Object,
            Options.Create(_configuration),
            null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public async Task HeartbeatMonitoring_DetectsTimeout_AndUnregistersTaskManager()
    {
        // Arrange
        var taskManagerId = "tm-timeout";
        var oldHeartbeat = DateTime.UtcNow.AddSeconds(-10);  // Old heartbeat (10 seconds ago)

        _mockResourceManager
            .Setup(rm => rm.GetRegisteredTaskManagers())
            .Returns(new[] { taskManagerId });

        _mockResourceManager
            .Setup(rm => rm.GetLastHeartbeat(taskManagerId))
            .Returns(oldHeartbeat);

        var service = new HeartbeatMonitoringService(
            _mockResourceManager.Object,
            Options.Create(_configuration),
            _mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));  // Wait for check interval
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockResourceManager.Verify(
            rm => rm.UnregisterTaskManagerAsync(taskManagerId, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce());
    }

    [Fact]
    public async Task HeartbeatMonitoring_WithRecentHeartbeat_DoesNotUnregister()
    {
        // Arrange
        var taskManagerId = "tm-healthy";

        _mockResourceManager
            .Setup(rm => rm.GetRegisteredTaskManagers())
            .Returns(new[] { taskManagerId });

        // Return a fresh heartbeat each time it's queried
        _mockResourceManager
            .Setup(rm => rm.GetLastHeartbeat(taskManagerId))
            .Returns(() => DateTime.UtcNow);

        var service = new HeartbeatMonitoringService(
            _mockResourceManager.Object,
            Options.Create(_configuration),
            _mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));  // Wait for check interval
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockResourceManager.Verify(
            rm => rm.UnregisterTaskManagerAsync(taskManagerId, It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public async Task HeartbeatMonitoring_WithNoTaskManagers_DoesNothing()
    {
        // Arrange
        _mockResourceManager
            .Setup(rm => rm.GetRegisteredTaskManagers())
            .Returns(Array.Empty<string>());

        var service = new HeartbeatMonitoringService(
            _mockResourceManager.Object,
            Options.Create(_configuration),
            _mockLogger.Object);

        // Act
        await service.StartAsync(CancellationToken.None);
        await Task.Delay(TimeSpan.FromSeconds(2));  // Wait for check interval
        await service.StopAsync(CancellationToken.None);

        // Assert
        _mockResourceManager.Verify(
            rm => rm.UnregisterTaskManagerAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Fact]
    public void HeartbeatConfiguration_HasCorrectDefaults()
    {
        // Arrange & Act
        var config = new HeartbeatConfiguration();

        // Assert
        config.TimeoutSeconds.Should().Be(30);
        config.CheckIntervalSeconds.Should().Be(10);
        HeartbeatConfiguration.SectionName.Should().Be("Heartbeat");
    }
}
