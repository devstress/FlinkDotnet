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
//  limitations under the License.

using System.Collections.Concurrent;
using System.Threading.Channels;
using FlinkDotNet.TaskManager.Interfaces;
using FlinkDotNet.TaskManager.Models;
using FlinkDotNet.TaskManager.Operators;
using Microsoft.Extensions.Logging;

namespace FlinkDotNet.TaskManager.Implementation;

/// <summary>
/// TaskExecutor executes tasks assigned to this TaskManager.
/// Manages task lifecycle, operator execution, and data channels.
/// </summary>
public class TaskExecutor : ITaskExecutor
{
    private readonly ILogger<TaskExecutor> _logger;
    private readonly ConcurrentDictionary<string, TaskExecution> _runningTasks = new();

    public TaskExecutor(ILogger<TaskExecutor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Deploy and start executing a task
    /// </summary>
    public async Task DeployTaskAsync(TaskDeploymentDescriptor descriptor, CancellationToken cancellationToken = default)
    {
        if (descriptor == null)
            throw new ArgumentNullException(nameof(descriptor));

        _logger.LogInformation(
            "Deploying task {ExecutionVertexId} for job vertex {JobVertexId}",
            descriptor.ExecutionVertexId,
            descriptor.JobVertexId);

        // Create task execution context
        TaskExecution taskExecution = new()
        {
            Descriptor = descriptor,
            State = "DEPLOYING",
            CancellationSource = new CancellationTokenSource(),
            RecordsProcessed = 0,
            BytesProcessed = 0
        };

        if (!_runningTasks.TryAdd(descriptor.ExecutionVertexId, taskExecution))
        {
            throw new InvalidOperationException($"Task {descriptor.ExecutionVertexId} is already running");
        }

        // Start task execution in background
        _ = Task.Run(async () => await ExecuteTaskAsync(taskExecution), cancellationToken);

        await Task.CompletedTask;
    }

    /// <summary>
    /// Cancel a running task
    /// </summary>
    public async Task CancelTaskAsync(string executionVertexId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling task {ExecutionVertexId}", executionVertexId);

        if (_runningTasks.TryGetValue(executionVertexId, out TaskExecution? taskExecution))
        {
            taskExecution.State = "CANCELLING";
            taskExecution.CancellationSource.Cancel();

            // Wait briefly for graceful shutdown
            await Task.Delay(100, cancellationToken);

            _runningTasks.TryRemove(executionVertexId, out _);
            _logger.LogInformation("Task {ExecutionVertexId} cancelled", executionVertexId);
        }
        else
        {
            _logger.LogWarning("Task {ExecutionVertexId} not found for cancellation", executionVertexId);
        }
    }

    /// <summary>
    /// Get task execution status
    /// </summary>
    public Task<TaskExecutionStatus> GetTaskStatusAsync(string executionVertexId)
    {
        if (_runningTasks.TryGetValue(executionVertexId, out TaskExecution? taskExecution))
        {
            return Task.FromResult(new TaskExecutionStatus
            {
                ExecutionVertexId = executionVertexId,
                State = taskExecution.State,
                RecordsProcessed = taskExecution.RecordsProcessed,
                BytesProcessed = taskExecution.BytesProcessed,
                ErrorMessage = taskExecution.ErrorMessage
            });
        }

        return Task.FromResult(new TaskExecutionStatus
        {
            ExecutionVertexId = executionVertexId,
            State = "NOT_FOUND"
        });
    }

    /// <summary>
    /// Execute a task (runs in background)
    /// </summary>
    private async Task ExecuteTaskAsync(TaskExecution taskExecution)
    {
        string vertexId = taskExecution.Descriptor.ExecutionVertexId;
        CancellationToken cancellationToken = taskExecution.CancellationSource.Token;

        try
        {
            _logger.LogInformation("Starting task execution {ExecutionVertexId}", vertexId);
            taskExecution.State = "RUNNING";

            // Create input and output channels
            Channel<StreamRecord<object>> inputChannel = Channel.CreateUnbounded<StreamRecord<object>>();
            Channel<StreamRecord<object>> outputChannel = Channel.CreateUnbounded<StreamRecord<object>>();

            // Create output collector
            ChannelOutputCollector outputCollector = new(outputChannel.Writer);

            // For now, create a simple pipeline based on operator type
            // In full implementation, this would be based on the execution graph
            await ExecuteOperatorPipelineAsync(taskExecution, inputChannel, outputChannel, outputCollector, cancellationToken);

            taskExecution.State = "FINISHED";
            _logger.LogInformation(
                "Task {ExecutionVertexId} finished. Processed {RecordCount} records",
                vertexId,
                taskExecution.RecordsProcessed);
        }
        catch (OperationCanceledException)
        {
            taskExecution.State = "CANCELED";
            _logger.LogInformation("Task {ExecutionVertexId} was cancelled", vertexId);
        }
        catch (Exception ex)
        {
            taskExecution.State = "FAILED";
            taskExecution.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Task {ExecutionVertexId} failed", vertexId);
        }
        finally
        {
            // Clean up after delay
            await Task.Delay(1000);
            _runningTasks.TryRemove(vertexId, out _);
        }
    }

    /// <summary>
    /// Execute operator pipeline (placeholder for full implementation)
    /// </summary>
    private async Task ExecuteOperatorPipelineAsync(
        TaskExecution taskExecution,
        Channel<StreamRecord<object>> inputChannel,
        Channel<StreamRecord<object>> outputChannel,
        ChannelOutputCollector outputCollector,
        CancellationToken cancellationToken)
    {
        // This is a simplified implementation
        // Full implementation would construct operator chain from ExecutionGraph
        // and use inputChannel, outputChannel, and outputCollector for data flow

        // For now, just simulate processing
        _ = inputChannel; // Will be used when connecting to upstream tasks
        _ = outputChannel; // Will be used when connecting to downstream tasks
        _ = outputCollector; // Will be used for emitting records

        await Task.Delay(100, cancellationToken);
        taskExecution.RecordsProcessed = 100; // Simulated
        taskExecution.BytesProcessed = 1000; // Simulated
    }
}

/// <summary>
/// Task execution context
/// </summary>
internal class TaskExecution
{
    public TaskDeploymentDescriptor Descriptor { get; set; } = null!;
    public string State { get; set; } = string.Empty;
    public CancellationTokenSource CancellationSource { get; set; } = null!;
    public long RecordsProcessed
    {
        get; set;
    }
    public long BytesProcessed
    {
        get; set;
    }
    public string? ErrorMessage
    {
        get; set;
    }
}

/// <summary>
/// Output collector that writes to a channel
/// </summary>
internal class ChannelOutputCollector : IOutputCollector<object>
{
    private readonly ChannelWriter<StreamRecord<object>> _writer;

    public ChannelOutputCollector(ChannelWriter<StreamRecord<object>> writer)
    {
        _writer = writer;
    }

    public async Task CollectAsync(StreamRecord<object> record, CancellationToken cancellationToken = default)
    {
        await _writer.WriteAsync(record, cancellationToken);
    }
}
