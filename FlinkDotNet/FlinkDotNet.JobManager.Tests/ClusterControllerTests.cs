// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using FlinkDotNet.JobManager.Controllers;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models;
using FlinkDotNet.JobManager.Models.Responses;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.JobManager.Tests;

public class ClusterControllerTests
{
    private readonly Mock<IResourceManager> _mockResourceManager;
    private readonly Mock<IDispatcher> _mockDispatcher;
    private readonly Mock<ILogger<ClusterController>> _mockLogger;
    private readonly ClusterController _controller;

    public ClusterControllerTests()
    {
        _mockResourceManager = new Mock<IResourceManager>();
        _mockDispatcher = new Mock<IDispatcher>();
        _mockLogger = new Mock<ILogger<ClusterController>>();
        _controller = new ClusterController(
            _mockResourceManager.Object,
            _mockDispatcher.Object,
            _mockLogger.Object);
    }

    [Fact]
    public void Constructor_WithNullResourceManager_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ClusterController(
            null!,
            _mockDispatcher.Object,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("resourceManager");
    }

    [Fact]
    public void Constructor_WithNullDispatcher_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ClusterController(
            _mockResourceManager.Object,
            null!,
            _mockLogger.Object);

        act.Should().Throw<ArgumentNullException>().WithParameterName("dispatcher");
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        var act = () => new ClusterController(
            _mockResourceManager.Object,
            _mockDispatcher.Object,
            null!);

        act.Should().Throw<ArgumentNullException>().WithParameterName("logger");
    }

    [Fact]
    public async Task GetOverview_ReturnsClusterOverview()
    {
        // Arrange
        var taskManagerIds = new List<string> { "tm-1", "tm-2" };
        var allSlots = new List<TaskSlot>
        {
            new TaskSlot { SlotId = "slot-1", TaskManagerId = "tm-1", IsAllocated = true },
            new TaskSlot { SlotId = "slot-2", TaskManagerId = "tm-1", IsAllocated = false },
            new TaskSlot { SlotId = "slot-3", TaskManagerId = "tm-2", IsAllocated = true },
            new TaskSlot { SlotId = "slot-4", TaskManagerId = "tm-2", IsAllocated = false }
        };
        var availableSlots = allSlots.Where(s => !s.IsAllocated).ToList();

        var jobs = new List<JobStatus>
        {
            new JobStatus { JobId = "job-1", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-2", State = JobExecutionState.Finished },
            new JobStatus { JobId = "job-3", State = JobExecutionState.Failed },
            new JobStatus { JobId = "job-4", State = JobExecutionState.Canceled }
        };

        _mockResourceManager.Setup(rm => rm.GetRegisteredTaskManagers()).Returns(taskManagerIds);
        _mockResourceManager.Setup(rm => rm.GetAllSlots()).Returns(allSlots);
        _mockResourceManager.Setup(rm => rm.GetAvailableSlots()).Returns(availableSlots);
        _mockDispatcher.Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ClusterOverviewResponse;

        response.Should().NotBeNull();
        response!.TaskManagers.Should().Be(2);
        response.TotalSlots.Should().Be(4);
        response.AvailableSlots.Should().Be(2);
        response.RunningJobs.Should().Be(1);
        response.FinishedJobs.Should().Be(1);
        response.FailedJobs.Should().Be(1);
        response.CanceledJobs.Should().Be(1);
    }

    [Fact]
    public void ListTaskManagers_ReturnsTaskManagerList()
    {
        // Arrange
        var taskManagerIds = new List<string> { "tm-1", "tm-2" };
        var allSlots = new List<TaskSlot>
        {
            new TaskSlot { SlotId = "slot-1", TaskManagerId = "tm-1", IsAllocated = true },
            new TaskSlot { SlotId = "slot-2", TaskManagerId = "tm-1", IsAllocated = false },
            new TaskSlot { SlotId = "slot-3", TaskManagerId = "tm-2", IsAllocated = true },
            new TaskSlot { SlotId = "slot-4", TaskManagerId = "tm-2", IsAllocated = false },
            new TaskSlot { SlotId = "slot-5", TaskManagerId = "tm-2", IsAllocated = false }
        };
        var availableSlots = allSlots.Where(s => !s.IsAllocated).ToList();

        _mockResourceManager.Setup(rm => rm.GetRegisteredTaskManagers()).Returns(taskManagerIds);
        _mockResourceManager.Setup(rm => rm.GetAllSlots()).Returns(allSlots);
        _mockResourceManager.Setup(rm => rm.GetAvailableSlots()).Returns(availableSlots);

        // Act
        var result = _controller.ListTaskManagers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TaskManagerListResponse;

        response.Should().NotBeNull();
        response!.TaskManagers.Should().HaveCount(2);

        var tm1 = response.TaskManagers.First(tm => tm.TaskManagerId == "tm-1");
        tm1.TotalSlots.Should().Be(2);
        tm1.FreeSlots.Should().Be(1);

        var tm2 = response.TaskManagers.First(tm => tm.TaskManagerId == "tm-2");
        tm2.TotalSlots.Should().Be(3);
        tm2.FreeSlots.Should().Be(2);
    }

    [Fact]
    public async Task GetOverview_WithNoJobs_ReturnsZeroJobCounts()
    {
        // Arrange
        var taskManagerIds = new List<string> { "tm-1" };
        var allSlots = new List<TaskSlot>
        {
            new TaskSlot { SlotId = "slot-1", TaskManagerId = "tm-1", IsAllocated = false }
        };
        var jobs = new List<JobStatus>();

        _mockResourceManager.Setup(rm => rm.GetRegisteredTaskManagers()).Returns(taskManagerIds);
        _mockResourceManager.Setup(rm => rm.GetAllSlots()).Returns(allSlots);
        _mockResourceManager.Setup(rm => rm.GetAvailableSlots()).Returns(allSlots);
        _mockDispatcher.Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ClusterOverviewResponse;

        response!.RunningJobs.Should().Be(0);
        response.FinishedJobs.Should().Be(0);
        response.FailedJobs.Should().Be(0);
        response.CanceledJobs.Should().Be(0);
    }

    [Fact]
    public void ListTaskManagers_WithNoTaskManagers_ReturnsEmptyList()
    {
        // Arrange
        var taskManagerIds = new List<string>();
        var allSlots = new List<TaskSlot>();
        var availableSlots = new List<TaskSlot>();

        _mockResourceManager.Setup(rm => rm.GetRegisteredTaskManagers()).Returns(taskManagerIds);
        _mockResourceManager.Setup(rm => rm.GetAllSlots()).Returns(allSlots);
        _mockResourceManager.Setup(rm => rm.GetAvailableSlots()).Returns(availableSlots);

        // Act
        var result = _controller.ListTaskManagers();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as TaskManagerListResponse;

        response!.TaskManagers.Should().BeEmpty();
    }

    [Fact]
    public async Task GetOverview_WithMultipleJobStates_CountsCorrectly()
    {
        // Arrange
        var taskManagerIds = new List<string> { "tm-1" };
        var allSlots = new List<TaskSlot>
        {
            new TaskSlot { SlotId = "slot-1", TaskManagerId = "tm-1", IsAllocated = false }
        };

        var jobs = new List<JobStatus>
        {
            new JobStatus { JobId = "job-1", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-2", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-3", State = JobExecutionState.Running },
            new JobStatus { JobId = "job-4", State = JobExecutionState.Finished },
            new JobStatus { JobId = "job-5", State = JobExecutionState.Failed },
            new JobStatus { JobId = "job-6", State = JobExecutionState.Failed },
            new JobStatus { JobId = "job-7", State = JobExecutionState.Canceled }
        };

        _mockResourceManager.Setup(rm => rm.GetRegisteredTaskManagers()).Returns(taskManagerIds);
        _mockResourceManager.Setup(rm => rm.GetAllSlots()).Returns(allSlots);
        _mockResourceManager.Setup(rm => rm.GetAvailableSlots()).Returns(allSlots);
        _mockDispatcher.Setup(d => d.ListJobsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(jobs);

        // Act
        var result = await _controller.GetOverview();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        var response = okResult!.Value as ClusterOverviewResponse;

        response!.RunningJobs.Should().Be(3);
        response.FinishedJobs.Should().Be(1);
        response.FailedJobs.Should().Be(2);
        response.CanceledJobs.Should().Be(1);
    }
}
