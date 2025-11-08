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

        // Add background service for task execution
        builder.Services.AddHostedService<TaskManagerWorker>();

        IHost host = builder.Build();

        Console.WriteLine(SeparatorLine);
        Console.WriteLine("TaskManager starting...");
        Console.WriteLine($"Ready to execute tasks with {numberOfSlots} parallel slots");
        Console.WriteLine(SeparatorLine);

        await host.RunAsync();
    }
}

/// <summary>
/// Background worker that manages task execution slots
/// </summary>
internal class TaskManagerWorker : BackgroundService
{
    private readonly ILogger<TaskManagerWorker> _logger;

    public TaskManagerWorker(ILogger<TaskManagerWorker> logger, ITemporalClient temporalClient)
    {
        this._logger = logger;
        _ = temporalClient; // Will be used for Temporal worker in future implementation
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("TaskManager worker started");

        // Register with JobManager - Implementation deferred to future iteration
        // Registration will be implemented via HTTP call to JobManager REST API

        // Start Temporal worker to execute activities - Implementation deferred to future iteration
        // Temporal worker will listen for task execution activities from workflow orchestration

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            this._logger.LogDebug("TaskManager heartbeat");
        }

        this._logger.LogInformation("TaskManager worker stopping");
    }
}
