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

Console.WriteLine("===========================================");
Console.WriteLine("FlinkDotNet TaskManager");
Console.WriteLine("Native .NET Task Execution Engine");
Console.WriteLine("===========================================");

var taskManagerId = Environment.GetEnvironmentVariable("TASKMANAGER_ID") ?? $"tm-{Guid.NewGuid().ToString()[..8]}";
var numberOfSlots = int.Parse(Environment.GetEnvironmentVariable("TASKMANAGER_SLOTS") ?? "4");
var jobManagerHost = Environment.GetEnvironmentVariable("JOBMANAGER_HOST") ?? "localhost";
var jobManagerPort = Environment.GetEnvironmentVariable("JOBMANAGER_PORT") ?? "8081";
var temporalHost = Environment.GetEnvironmentVariable("TEMPORAL_HOST") ?? "localhost";
var temporalPort = Environment.GetEnvironmentVariable("TEMPORAL_PORT") ?? "7233";

Console.WriteLine($"TaskManager ID: {taskManagerId}");
Console.WriteLine($"Number of slots: {numberOfSlots}");
Console.WriteLine($"JobManager: {jobManagerHost}:{jobManagerPort}");
Console.WriteLine($"Temporal: {temporalHost}:{temporalPort}");

var builder = Host.CreateApplicationBuilder(args);

// Configure Temporal client
var temporalAddress = $"{temporalHost}:{temporalPort}";
builder.Services.AddSingleton<ITemporalClient>(sp =>
{
    var logger = sp.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Connecting to Temporal at {TemporalAddress}", temporalAddress);
    
    return TemporalClient.ConnectAsync(new TemporalClientConnectOptions
    {
        TargetHost = temporalAddress,
        Namespace = "default"
    }).GetAwaiter().GetResult();
});

// Add background service for task execution
builder.Services.AddHostedService<TaskManagerWorker>();

var host = builder.Build();

Console.WriteLine("===========================================");
Console.WriteLine("TaskManager starting...");
Console.WriteLine($"Ready to execute tasks with {numberOfSlots} parallel slots");
Console.WriteLine("===========================================");

await host.RunAsync();

/// <summary>
/// Background worker that manages task execution slots
/// </summary>
internal class TaskManagerWorker : BackgroundService
{
    private readonly ILogger<TaskManagerWorker> _logger;

    public TaskManagerWorker(ILogger<TaskManagerWorker> logger, ITemporalClient temporalClient)
    {
        _logger = logger;
        _ = temporalClient; // Will be used for Temporal worker in future implementation
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("TaskManager worker started");

        // Register with JobManager
        // TODO: Implement registration via HTTP call to JobManager

        // Start Temporal worker to execute activities
        // TODO: Start Temporal worker listening for task execution activities

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            _logger.LogDebug("TaskManager heartbeat");
        }

        _logger.LogInformation("TaskManager worker stopping");
    }
}
