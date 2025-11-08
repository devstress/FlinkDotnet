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
/// Represents the logical execution plan for a Flink job.
/// This is the user-defined DAG of operations before physical execution planning.
/// Equivalent to Apache Flink's JobGraph.
/// </summary>
public class JobGraph
{
    /// <summary>
    /// Unique identifier for the job
    /// </summary>
    public string JobId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable job name
    /// </summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>
    /// List of vertices in the job graph (operators/transformations)
    /// </summary>
    public List<JobVertex> Vertices { get; set; } = new();

    /// <summary>
    /// Edges connecting vertices (data flow)
    /// </summary>
    public List<JobEdge> Edges { get; set; } = new();

    /// <summary>
    /// Job configuration parameters
    /// </summary>
    public Dictionary<string, string> Configuration { get; set; } = new();

    /// <summary>
    /// Maximum parallelism for the job
    /// </summary>
    public int MaxParallelism { get; set; } = 128;
}

/// <summary>
/// Represents a vertex (operator) in the job graph
/// </summary>
public class JobVertex
{
    /// <summary>
    /// Unique identifier for the vertex
    /// </summary>
    public string VertexId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Name of the vertex (for display and identification)
    /// </summary>
    public string Name
    {
        get => OperatorName;
        set => OperatorName = value;
    }

    /// <summary>
    /// Name of the operator (e.g., "Map", "Filter", "Source")
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// Parallelism for this operator (number of parallel instances)
    /// </summary>
    public int Parallelism { get; set; } = 1;

    /// <summary>
    /// Operator type identifier
    /// </summary>
    public OperatorType Type { get; set; }

    /// <summary>
    /// Operator type (alternative property name)
    /// </summary>
    public OperatorType OperatorType
    {
        get => Type;
        set => Type = value;
    }

    /// <summary>
    /// Serialized operator logic (lambda expressions, function references)
    /// </summary>
    public string OperatorLogic { get; set; } = string.Empty;
}

/// <summary>
/// Types of operators in the job graph
/// </summary>
public enum OperatorType
{
    Source,
    Map,
    FlatMap,
    Filter,
    KeyBy,
    Window,
    Reduce,
    Sink,
    Join,
    CoGroup,
    Union
}

/// <summary>
/// Represents an edge (data flow) between vertices
/// </summary>
public class JobEdge
{
    /// <summary>
    /// Source vertex ID
    /// </summary>
    public string SourceVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Target vertex ID
    /// </summary>
    public string TargetVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Partitioning strategy for data flow
    /// </summary>
    public PartitioningStrategy Strategy { get; set; } = PartitioningStrategy.Forward;

    /// <summary>
    /// Partitioning strategy (alternative property name)
    /// </summary>
    public PartitioningStrategy PartitioningStrategy
    {
        get => Strategy;
        set => Strategy = value;
    }
}

/// <summary>
/// Data partitioning strategies between operators
/// </summary>
public enum PartitioningStrategy
{
    /// <summary>
    /// Forward data to the next operator in pipeline
    /// </summary>
    Forward,

    /// <summary>
    /// Hash partition by key
    /// </summary>
    Hash,

    /// <summary>
    /// Rebalance (round-robin) across parallel instances
    /// </summary>
    Rebalance,

    /// <summary>
    /// Broadcast to all parallel instances
    /// </summary>
    Broadcast,

    /// <summary>
    /// Rescale to a subset of parallel instances
    /// </summary>
    Rescale
}
