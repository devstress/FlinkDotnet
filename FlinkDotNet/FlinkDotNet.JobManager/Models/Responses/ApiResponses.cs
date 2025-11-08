// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace FlinkDotNet.JobManager.Models.Responses;

/// <summary>
/// Response after submitting a job.
/// </summary>
public class SubmitJobResponse
{
    /// <summary>
    /// Unique identifier for the submitted job.
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Current state of the job.
    /// </summary>
    public JobExecutionState State { get; set; }

    /// <summary>
    /// Timestamp when the job was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Message describing the submission result.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// Response for job status query.
/// </summary>
public class JobStatusResponse
{
    /// <summary>
    /// Unique identifier for the job.
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Name of the job.
    /// </summary>
    public required string JobName { get; set; }

    /// <summary>
    /// Current execution state.
    /// </summary>
    public JobExecutionState State { get; set; }

    /// <summary>
    /// When the job was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// When the job started executing (if started).
    /// </summary>
    public DateTime? StartedAt { get; set; }

    /// <summary>
    /// When the job finished (if finished/failed/canceled).
    /// </summary>
    public DateTime? FinishedAt { get; set; }

    /// <summary>
    /// Duration of execution (if finished).
    /// </summary>
    public TimeSpan? Duration { get; set; }

    /// <summary>
    /// Error message if job failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Number of tasks in the job.
    /// </summary>
    public int TotalTasks { get; set; }

    /// <summary>
    /// Number of running tasks.
    /// </summary>
    public int RunningTasks { get; set; }

    /// <summary>
    /// Number of completed tasks.
    /// </summary>
    public int CompletedTasks { get; set; }

    /// <summary>
    /// Number of failed tasks.
    /// </summary>
    public int FailedTasks { get; set; }
}

/// <summary>
/// Response for listing all jobs.
/// </summary>
public class JobListResponse
{
    /// <summary>
    /// Total number of jobs.
    /// </summary>
    public int TotalJobs { get; set; }

    /// <summary>
    /// List of jobs.
    /// </summary>
    public List<JobSummary> Jobs { get; set; } = new();
}

/// <summary>
/// Summary information about a job.
/// </summary>
public class JobSummary
{
    /// <summary>
    /// Job identifier.
    /// </summary>
    public required string JobId { get; set; }

    /// <summary>
    /// Job name.
    /// </summary>
    public required string JobName { get; set; }

    /// <summary>
    /// Current state.
    /// </summary>
    public JobExecutionState State { get; set; }

    /// <summary>
    /// Submission timestamp.
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Duration (if completed).
    /// </summary>
    public TimeSpan? Duration { get; set; }
}

/// <summary>
/// Response for TaskManager list.
/// </summary>
public class TaskManagerListResponse
{
    /// <summary>
    /// Total number of registered TaskManagers.
    /// </summary>
    public int TotalTaskManagers { get; set; }

    /// <summary>
    /// Total number of available slots.
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Number of free slots.
    /// </summary>
    public int FreeSlots { get; set; }

    /// <summary>
    /// List of TaskManagers.
    /// </summary>
    public List<TaskManagerInfo> TaskManagers { get; set; } = new();
}

/// <summary>
/// Information about a TaskManager.
/// </summary>
public class TaskManagerInfo
{
    /// <summary>
    /// TaskManager identifier.
    /// </summary>
    public required string TaskManagerId { get; set; }

    /// <summary>
    /// Number of slots on this TaskManager.
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Number of free slots.
    /// </summary>
    public int FreeSlots { get; set; }

    /// <summary>
    /// When the TaskManager registered.
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Last heartbeat timestamp.
    /// </summary>
    public DateTime LastHeartbeat { get; set; }
}

/// <summary>
/// Cluster overview response.
/// </summary>
public class ClusterOverviewResponse
{
    /// <summary>
    /// Number of registered TaskManagers.
    /// </summary>
    public int TaskManagers { get; set; }

    /// <summary>
    /// Total slot capacity.
    /// </summary>
    public int TotalSlots { get; set; }

    /// <summary>
    /// Available slots.
    /// </summary>
    public int AvailableSlots { get; set; }

    /// <summary>
    /// Number of running jobs.
    /// </summary>
    public int RunningJobs { get; set; }

    /// <summary>
    /// Number of finished jobs.
    /// </summary>
    public int FinishedJobs { get; set; }

    /// <summary>
    /// Number of failed jobs.
    /// </summary>
    public int FailedJobs { get; set; }

    /// <summary>
    /// Number of canceled jobs.
    /// </summary>
    public int CanceledJobs { get; set; }
}
