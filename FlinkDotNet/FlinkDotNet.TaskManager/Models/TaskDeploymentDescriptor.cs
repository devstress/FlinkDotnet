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

namespace FlinkDotNet.TaskManager.Models;

/// <summary>
/// Represents a task deployment descriptor sent from JobManager to TaskManager
/// </summary>
public class TaskDeploymentDescriptor
{
    /// <summary>
    /// Execution vertex identifier
    /// </summary>
    public string ExecutionVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Job identifier
    /// </summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>
    /// Operator name
    /// </summary>
    public string OperatorName { get; set; } = string.Empty;

    /// <summary>
    /// Serialized operator logic to execute
    /// </summary>
    public string OperatorLogic { get; set; } = string.Empty;

    /// <summary>
    /// Subtask index
    /// </summary>
    public int SubtaskIndex { get; set; }

    /// <summary>
    /// Total parallelism for this operator
    /// </summary>
    public int Parallelism { get; set; }

    /// <summary>
    /// Input channels for receiving data
    /// </summary>
    public List<InputChannel> InputChannels { get; set; } = new();

    /// <summary>
    /// Output channels for sending data
    /// </summary>
    public List<OutputChannel> OutputChannels { get; set; } = new();
}

/// <summary>
/// Input channel for receiving data from upstream tasks
/// </summary>
public class InputChannel
{
    /// <summary>
    /// Source execution vertex identifier
    /// </summary>
    public string SourceVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Source TaskManager location
    /// </summary>
    public string SourceTaskManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Whether this is a local or remote channel
    /// </summary>
    public bool IsLocal { get; set; }
}

/// <summary>
/// Output channel for sending data to downstream tasks
/// </summary>
public class OutputChannel
{
    /// <summary>
    /// Target execution vertex identifier
    /// </summary>
    public string TargetVertexId { get; set; } = string.Empty;

    /// <summary>
    /// Target TaskManager location
    /// </summary>
    public string TargetTaskManagerId { get; set; } = string.Empty;

    /// <summary>
    /// Partitioning strategy
    /// </summary>
    public string PartitioningStrategy { get; set; } = string.Empty;
}
