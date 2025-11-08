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

using FlinkDotNet.JobManager.Activities;
using FlinkDotNet.JobManager.Interfaces;
using FlinkDotNet.JobManager.Workflows;
using Temporalio.Client;
using Temporalio.Worker;

namespace FlinkDotNet.JobManager.Services;

/// <summary>
/// Hosted service for running Temporal worker that processes workflows and activities.
/// Manages the lifecycle of the Temporal worker, ensuring graceful startup and shutdown.
/// Phase 4: Complete implementation with proper dependency injection
/// </summary>
public class TemporalWorkerService : IHostedService
{
    private readonly ITemporalClient _client;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TemporalWorkerService> _logger;
    private TemporalWorker? _worker;
    private Task? _workerTask;
    private readonly CancellationTokenSource _shutdownCts = new();

    /// <summary>
    /// Task queue name for Flink job workflows
    /// </summary>
    public const string TaskQueueName = "flink-job-queue";

    public TemporalWorkerService(
        ITemporalClient client,
        IServiceProvider serviceProvider,
        ILogger<TemporalWorkerService> logger)
    {
        this._client = client;
        this._serviceProvider = serviceProvider;
        this._logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Starting Temporal worker on task queue: {TaskQueue}", TaskQueueName);

        try
        {
            // Create activity instance with all required dependencies
            TaskExecutionActivity activity = new(
                this._serviceProvider.GetRequiredService<ILogger<TaskExecutionActivity>>(),
                this._serviceProvider.GetRequiredService<IHttpClientFactory>(),
                this._serviceProvider.GetRequiredService<IResourceManager>());

            // Configure worker with workflows and activities
            TemporalWorkerOptions options = new TemporalWorkerOptions(TaskQueueName)
                .AddWorkflow<FlinkJobWorkflow>()
                .AddAllActivities(activity);

            // Create worker
            this._worker = new TemporalWorker(this._client, options);

            // Start worker execution in background
            this._workerTask = Task.Run(async () =>
            {
                try
                {
                    this._logger.LogInformation("Temporal worker started successfully");
                    await this._worker.ExecuteAsync(this._shutdownCts.Token);
                }
                catch (OperationCanceledException)
                {
                    this._logger.LogInformation("Temporal worker execution cancelled");
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex, "Temporal worker execution failed");
                }
            }, cancellationToken);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Failed to start Temporal worker");
            return Task.FromException(ex);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("Stopping Temporal worker...");

        try
        {
            // Signal shutdown
            this._shutdownCts.Cancel();

            // Wait for worker to finish with timeout
            if (this._workerTask != null)
            {
                using CancellationTokenSource timeoutCts = new(TimeSpan.FromSeconds(30));
                using CancellationTokenSource linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                try
                {
                    await this._workerTask.WaitAsync(linkedCts.Token);
                }
                catch (OperationCanceledException)
                {
                    this._logger.LogWarning("Temporal worker shutdown timed out");
                }
            }

            // Worker disposal is automatic when task completes
            this._logger.LogInformation("Temporal worker stopped successfully");
        }
        catch (Exception ex)
        {
            this._logger.LogError(ex, "Error stopping Temporal worker");
        }
        finally
        {
            this._shutdownCts.Dispose();
        }
    }
}
