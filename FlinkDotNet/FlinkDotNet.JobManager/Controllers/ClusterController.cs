// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Models.Responses;

namespace FlinkDotNet.JobManager.Controllers;

/// <summary>
/// API controller for cluster and TaskManager management.
/// </summary>
[ApiController]
[Route("api")]
[Produces("application/json")]
public class ClusterController(
    IResourceManager resourceManager,
    IDispatcher dispatcher,
    ILogger<ClusterController> logger) : ControllerBase
{
    private readonly IResourceManager _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
    private readonly IDispatcher _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
    private readonly ILogger<ClusterController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Get cluster overview with resource and job statistics.
    /// </summary>
    /// <returns>Cluster overview including TaskManagers, slots, and job counts.</returns>
    [HttpGet("overview")]
    [ProducesResponseType(typeof(ClusterOverviewResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOverview()
    {
        this._logger.LogDebug("Getting cluster overview");

        List<JobStatus> jobs = await this._dispatcher.ListJobsAsync();

        ClusterOverviewResponse response = new()
        {
            TaskManagers = this._resourceManager.GetRegisteredTaskManagers().Count(),
            TotalSlots = this._resourceManager.GetAllSlots().Count(),
            AvailableSlots = this._resourceManager.GetAvailableSlots().Count(),
            RunningJobs = jobs.Count(j => j.State == Models.JobExecutionState.Running),
            FinishedJobs = jobs.Count(j => j.State == Models.JobExecutionState.Finished),
            FailedJobs = jobs.Count(j => j.State == Models.JobExecutionState.Failed),
            CanceledJobs = jobs.Count(j => j.State == Models.JobExecutionState.Canceled)
        };

        return Ok(response);
    }

    /// <summary>
    /// List all registered TaskManagers.
    /// </summary>
    /// <returns>List of TaskManagers with slot information.</returns>
    [HttpGet("taskmanagers")]
    [ProducesResponseType(typeof(TaskManagerListResponse), StatusCodes.Status200OK)]
    public IActionResult ListTaskManagers()
    {
        _logger.LogDebug("Listing TaskManagers");

        IEnumerable<string> taskManagerIds = _resourceManager.GetRegisteredTaskManagers();
        List<Models.Responses.TaskManagerInfo> taskManagers = new();

        foreach (string tmId in taskManagerIds)
        {
            IEnumerable<Models.TaskSlot> allSlots = _resourceManager.GetAllSlots()
                .Where(s => s.TaskManagerId == tmId);
            IEnumerable<Models.TaskSlot> freeSlots = _resourceManager.GetAvailableSlots()
                .Where(s => s.TaskManagerId == tmId);

            // Registration and heartbeat times retrieval from ResourceManager deferred to future iteration
            taskManagers.Add(new Models.Responses.TaskManagerInfo
            {
                TaskManagerId = tmId,
                TotalSlots = allSlots.Count(),
                FreeSlots = freeSlots.Count(),
                RegisteredAt = DateTime.UtcNow, // Placeholder
                LastHeartbeat = DateTime.UtcNow // Placeholder
            });
        }

        TaskManagerListResponse response = new()
        {
            TotalTaskManagers = taskManagers.Count,
            TotalSlots = taskManagers.Sum(tm => tm.TotalSlots),
            FreeSlots = taskManagers.Sum(tm => tm.FreeSlots),
            TaskManagers = taskManagers
        };

        return Ok(response);
    }

    /// <summary>
    /// Register a new TaskManager with the cluster.
    /// </summary>
    /// <param name="request">TaskManager registration details.</param>
    /// <returns>Registration confirmation.</returns>
    [HttpPost("taskmanagers/register")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult RegisterTaskManager([FromBody] TaskManagerRegistrationRequest request)
    {
        try
        {
            this._logger.LogInformation(
                "Registering TaskManager: {TaskManagerId} with {Slots} slots",
                request.TaskManagerId,
                request.NumberOfSlots);

            this._resourceManager.RegisterTaskManager(request.TaskManagerId, request.NumberOfSlots);

            return Ok(new
            {
                message = $"TaskManager {request.TaskManagerId} registered successfully",
                taskManagerId = request.TaskManagerId,
                slots = request.NumberOfSlots
            });
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to register TaskManager");
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Unregister a TaskManager from the cluster.
    /// </summary>
    /// <param name="taskManagerId">ID of the TaskManager to unregister.</param>
    /// <returns>Unregistration confirmation.</returns>
    [HttpPost("taskmanagers/{taskManagerId}/unregister")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult UnregisterTaskManager(string taskManagerId)
    {
        this._logger.LogInformation("Unregistering TaskManager: {TaskManagerId}", taskManagerId);

        bool unregistered = this._resourceManager.UnregisterTaskManager(taskManagerId);

        if (!unregistered)
        {
            return NotFound(new { error = $"TaskManager {taskManagerId} not found" });
        }

        return Ok(new { message = $"TaskManager {taskManagerId} unregistered successfully" });
    }
}

/// <summary>
/// Request to register a new TaskManager.
/// </summary>
public class TaskManagerRegistrationRequest
{
    /// <summary>
    /// Unique identifier for the TaskManager.
    /// </summary>
    public required string TaskManagerId { get; set; }

    /// <summary>
    /// Number of execution slots on this TaskManager.
    /// </summary>
    public int NumberOfSlots { get; set; } = 4;
}
