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

namespace FlinkDotNet.JobManager.Models;

/// <summary>
/// Represents the physical execution plan for a Flink job.
/// Converts JobGraph to executable tasks assigned to TaskManagers.
/// Equivalent to Apache Flink's ExecutionGraph.
/// </summary>
public class ExecutionGraph
{
    /// <summary>
    /// Job identifier from JobGraph
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Job name from JobGraph
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// List of execution vertices (parallel task instances)
    /// </summary>
    public List<ExecutionVertex> ExecutionVertices { get; set; } = new();

    /// <summary>
    /// List of execution edges (data flow between vertices)
    /// </summary>
    public List<ExecutionEdge> ExecutionEdges { get; set; } = new();

    /// <summary>
    /// Current state of the job execution
    /// </summary>
    public JobExecutionState State { get; set; } = JobExecutionState.Created;

    /// <summary>
    /// Timestamp when job was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Timestamp when job started executing
    /// </summary>
    public DateTime? StartedAt
    {
        get; set;
    }

    /// <summary>
    /// Timestamp when job finished
    /// </summary>
    public DateTime? FinishedAt
    {
        get; set;
    }

    /// <summary>
    /// Error message if job failed
    /// </summary>
    public string? FailureMessage
    {
        get; set;
    }
}

/// <summary>
/// Represents a single parallel task instance
/// </summary>
public class ExecutionVertex
{
    /// <summary>
    /// Unique identifier for this execution vertex
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Alternate identifier (for backwards compatibility)
    /// </summary>
    public string ExecutionVertexId
    {
        get => Id;
        set => Id = value;
    }

    /// <summary>
    /// Reference to the job vertex this is an instance of
    /// </summary>
    public string JobVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Parallel subtask index (0 to parallelism-1)
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
    /// Operator type for this vertex
    /// </summary>
    public OperatorType OperatorType
    {
        get; set;
    }

    /// <summary>
    /// TaskManager slot where this task is deployed
    /// </summary>
    public TaskSlot? AssignedSlot
    {
        get; set;
    }

    /// <summary>
    /// Current state of this execution vertex
    /// </summary>
    public ExecutionState State { get; set; } = ExecutionState.Created;

    /// <summary>
    /// Operator name for display
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// Error message if task failed
    /// </summary>
    public string? Error
    {
        get; set;
    }
}

/// <summary>
/// Represents a task slot in a TaskManager
/// </summary>
public class TaskSlot
{
    /// <summary>
    /// Unique slot identifier
    /// </summary>
    public string SlotId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// TaskManager identifier hosting this slot
    /// </summary>
    public string TaskManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Slot number within the TaskManager
    /// </summary>
    public int SlotNumber
    {
        get; set;
    }

    /// <summary>
    /// Whether slot is currently allocated
    /// </summary>
    public bool IsAllocated
    {
        get; set;
    }

    /// <summary>
    /// Job ID that allocated this slot (if allocated)
    /// </summary>
    public string? AllocatedJobId
    {
        get; set;
    }
}

/// <summary>
/// Execution states for individual tasks
/// </summary>
public enum ExecutionState
{
    Created,
    Scheduled,
    Deploying,
    Running,
    Finished,
    Canceled,
    Failed
}

/// <summary>
/// Overall job execution states
/// </summary>
public enum JobExecutionState
{
    Created,
    Deploying,
    Running,
    Failing,
    Failed,
    Canceling,
    Canceled,
    Finished,
    Restarting,
    Suspended
}

/// <summary>
/// Represents a data flow edge between execution vertices
/// </summary>
public class ExecutionEdge
{
    /// <summary>
    /// Source execution vertex ID
    /// </summary>
    public string SourceExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Target execution vertex ID
    /// </summary>
    public string TargetExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Partitioning strategy for data distribution
    /// </summary>
    public PartitioningStrategy PartitioningStrategy
    {
        get; set;
    }
}
