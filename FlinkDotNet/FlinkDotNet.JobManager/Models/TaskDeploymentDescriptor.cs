// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace FlinkDotNet.JobManager.Models;

/// <summary>
/// Describes how a task should be deployed to a TaskManager.
/// Contains all information needed for TaskManager to execute the task.
/// </summary>
public class TaskDeploymentDescriptor
{
    /// <summary>
    /// Execution vertex ID for this task
    /// </summary>
    public string ExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Job vertex ID this task belongs to
    /// </summary>
    public string JobVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Subtask index within the operator parallelism
    /// </summary>
    public int SubtaskIndex
    {
        get; set;
    }

    /// <summary>
    /// Total parallelism of the operator
    /// </summary>
    public int Parallelism
    {
        get; set;
    }

    /// <summary>
    /// Operator type to execute
    /// </summary>
    public OperatorType OperatorType
    {
        get; set;
    }

    /// <summary>
    /// Assigned task slot
    /// </summary>
    public TaskSlot? AssignedSlot
    {
        get; set;
    }

    /// <summary>
    /// Configuration parameters for the task
    /// </summary>
    public Dictionary<string, object> Configuration { get; set; } = new();
}
