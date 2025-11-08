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

using System.Net.Http.Json;
using FlinkDotNet.TaskManager.Implementation;
using FlinkDotNet.TaskManager.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Temporalio.Client;

namespace FlinkDotNet.TaskManager;

internal class Program
{
    private const string SeparatorLine = "===========================================";

    private Program()
    {
        // Private constructor to prevent instantiation
    }

    public static async Task Main(string[] args)
    {
        Console.WriteLine(SeparatorLine);
        Console.WriteLine("FlinkDotNet TaskManager");
        Console.WriteLine("Native .NET Task Execution Engine");
        Console.WriteLine(SeparatorLine);

        string taskManagerId = Environment.GetEnvironmentVariable("TASKMANAGER_ID") ?? $"tm-{Guid.NewGuid().ToString()[..8]}";
        int numberOfSlots = int.Parse(Environment.GetEnvironmentVariable("TASKMANAGER_SLOTS") ?? "4");
        string jobManagerHost = Environment.GetEnvironmentVariable("JOBMANAGER_HOST") ?? "localhost";
        string jobManagerPort = Environment.GetEnvironmentVariable("JOBMANAGER_PORT") ?? "8081";
        string temporalHost = Environment.GetEnvironmentVariable("TEMPORAL_HOST") ?? "localhost";
        string temporalPort = Environment.GetEnvironmentVariable("TEMPORAL_PORT") ?? "7233";

        Console.WriteLine($"TaskManager ID: {taskManagerId}");
        Console.WriteLine($"Number of slots: {numberOfSlots}");
        Console.WriteLine($"JobManager: {jobManagerHost}:{jobManagerPort}");
        Console.WriteLine($"Temporal: {temporalHost}:{temporalPort}");

        HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

        // Configure HttpClient for JobManager communication
#pragma warning disable S5332 // Using HTTP is acceptable for local development and internal communication
        string jobManagerUrl = $"http://{jobManagerHost}:{jobManagerPort}";
#pragma warning restore S5332
        builder.Services.AddHttpClient("JobManager", client =>
        {
            client.BaseAddress = new Uri(jobManagerUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Configure Temporal client
        string temporalAddress = $"{temporalHost}:{temporalPort}";
        builder.Services.AddSingleton<ITemporalClient>(sp =>
        {
            ILogger<Program> logger = sp.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Connecting to Temporal at {TemporalAddress}", temporalAddress);

            return TemporalClient.ConnectAsync(new TemporalClientConnectOptions
            {
                TargetHost = temporalAddress,
                Namespace = "default"
            }).GetAwaiter().GetResult();
        });

        // Register TaskExecutor
        builder.Services.AddSingleton<ITaskExecutor, TaskExecutor>();

        // Add background service for task execution
        builder.Services.AddHostedService<TaskManagerWorker>(sp =>
            new TaskManagerWorker(
                sp.GetRequiredService<ILogger<TaskManagerWorker>>(),
                sp.GetRequiredService<ITemporalClient>(),
                sp.GetRequiredService<ITaskExecutor>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                taskManagerId,
                numberOfSlots,
                jobManagerUrl));

        IHost host = builder.Build();

        Console.WriteLine(SeparatorLine);
        Console.WriteLine("TaskManager starting...");
        Console.WriteLine($"Ready to execute tasks with {numberOfSlots} parallel slots");
        Console.WriteLine(SeparatorLine);

        await host.RunAsync();
    }
}

/// <summary>
/// Background worker that manages task execution slots and JobManager communication
/// </summary>
internal class TaskManagerWorker : BackgroundService
{
    private readonly ILogger<TaskManagerWorker> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _taskManagerId;
    private readonly int _numberOfSlots;
    private readonly string _jobManagerUrl;

    public TaskManagerWorker(
        ILogger<TaskManagerWorker> logger,
        ITemporalClient temporalClient,
        ITaskExecutor taskExecutor,
        IHttpClientFactory httpClientFactory,
        string taskManagerId,
        int numberOfSlots,
        string jobManagerUrl)
    {
        this._logger = logger;
        this._httpClient = httpClientFactory.CreateClient("JobManager");
        this._taskManagerId = taskManagerId;
        this._numberOfSlots = numberOfSlots;
        this._jobManagerUrl = jobManagerUrl;
        
        // Suppress warnings for parameters that will be used in future implementations
        _ = temporalClient; // Will be used for Temporal worker
        _ = taskExecutor; // Will be used for task deployment from JobManager
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("TaskManager worker started");

        // Register with JobManager
        await RegisterWithJobManagerAsync(stoppingToken);

        // Send heartbeats periodically
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendHeartbeatAsync(stoppingToken);
                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown
                break;
            }
            catch (Exception ex)
            {
                this._logger.LogError(ex, "Error sending heartbeat");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }

        // Unregister on shutdown
        await UnregisterFromJobManagerAsync(stoppingToken);

        this._logger.LogInformation("TaskManager worker stopping");
    }

    private async Task RegisterWithJobManagerAsync(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation(
                "Registering TaskManager {TaskManagerId} with JobManager at {JobManagerUrl}",
                this._taskManagerId,
                this._jobManagerUrl);

            var request = new
            {
                TaskManagerId = this._taskManagerId,
                NumberOfSlots = this._numberOfSlots
            };

            HttpResponseMessage response = await this._httpClient.PostAsJsonAsync(
                "/api/taskmanagers/register",
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                this._logger.LogInformation(
                    "Successfully registered TaskManager {TaskManagerId}",
                    this._taskManagerId);
            }
            else
            {
                this._logger.LogWarning(
                    "Failed to register TaskManager {TaskManagerId}. Status: {StatusCode}",
                    this._taskManagerId,
                    response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Error registering TaskManager {TaskManagerId}",
                this._taskManagerId);
        }
    }

    private async Task SendHeartbeatAsync(CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await this._httpClient.PostAsync(
                $"/api/taskmanagers/{this._taskManagerId}/heartbeat",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                this._logger.LogDebug("Heartbeat sent for TaskManager {TaskManagerId}", this._taskManagerId);
            }
            else
            {
                this._logger.LogWarning(
                    "Heartbeat failed for TaskManager {TaskManagerId}. Status: {StatusCode}",
                    this._taskManagerId,
                    response.StatusCode);
            }
        }
        catch (HttpRequestException ex)
        {
            this._logger.LogWarning(
                ex,
                "Could not reach JobManager for heartbeat. TaskManager {TaskManagerId}",
                this._taskManagerId);
        }
    }

    private async Task UnregisterFromJobManagerAsync(CancellationToken cancellationToken)
    {
        try
        {
            this._logger.LogInformation(
                "Unregistering TaskManager {TaskManagerId} from JobManager",
                this._taskManagerId);

            HttpResponseMessage response = await this._httpClient.PostAsync(
                $"/api/taskmanagers/{this._taskManagerId}/unregister",
                null,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                this._logger.LogInformation(
                    "Successfully unregistered TaskManager {TaskManagerId}",
                    this._taskManagerId);
            }
        }
        catch (Exception ex)
        {
            this._logger.LogError(
                ex,
                "Error unregistering TaskManager {TaskManagerId}",
                this._taskManagerId);
        }
    }
}
