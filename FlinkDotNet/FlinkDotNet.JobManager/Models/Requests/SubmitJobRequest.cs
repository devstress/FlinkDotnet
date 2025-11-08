// Copyright 2025 FlinkDotNet
// Licensed under the Apache License, Version 2.0.
// See LICENSE file in the project root for full license information.

namespace FlinkDotNet.JobManager.Models.Requests;

/// <summary>
/// Request to submit a new job for execution.
/// </summary>
public class SubmitJobRequest
{
    /// <summary>
    /// Name of the job for identification.
    /// </summary>
    public required string JobName { get; set; }

    /// <summary>
    /// Maximum parallelism allowed for this job.
    /// </summary>
    public int MaxParallelism { get; set; } = 128;

    /// <summary>
    /// List of vertices (operators) in the job graph.
    /// </summary>
    public List<JobVertexRequest> Vertices { get; set; } = new();

    /// <summary>
    /// List of edges (data connections) between vertices.
    /// </summary>
    public List<JobEdgeRequest> Edges { get; set; } = new();
}

/// <summary>
/// Represents a vertex (operator) in the job graph.
/// </summary>
public class JobVertexRequest
{
    /// <summary>
    /// Name of the operator.
    /// </summary>
    public required string OperatorName { get; set; }

    /// <summary>
    /// Type of operator (source, map, filter, sink, etc.).
    /// </summary>
    public required string OperatorType { get; set; }

    /// <summary>
    /// Parallelism for this operator.
    /// </summary>
    public int Parallelism { get; set; } = 1;

    /// <summary>
    /// Operator logic/configuration (serialized).
    /// </summary>
    public string? OperatorLogic { get; set; }
}

/// <summary>
/// Represents an edge (data connection) in the job graph.
/// </summary>
public class JobEdgeRequest
{
    /// <summary>
    /// Index of the source vertex in the vertices list.
    /// </summary>
    public int SourceVertexIndex { get; set; }

    /// <summary>
    /// Index of the target vertex in the vertices list.
    /// </summary>
    public int TargetVertexIndex { get; set; }

    /// <summary>
    /// Partitioning strategy for data distribution.
    /// </summary>
    public string Strategy { get; set; } = "Rebalance";
}
