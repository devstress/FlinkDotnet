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

using FlinkDotNet.JobManager.Implementation;
using FlinkDotNet.JobManager.Interfaces;
using Temporalio.Client;

Console.WriteLine("===========================================");
Console.WriteLine("FlinkDotNet JobManager");
Console.WriteLine("Native .NET Distributed Stream Processing");
Console.WriteLine("===========================================");

var builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Temporal client
var temporalHost = Environment.GetEnvironmentVariable("TEMPORAL_HOST") ?? "localhost";
var temporalPort = Environment.GetEnvironmentVariable("TEMPORAL_PORT") ?? "7233";
var temporalAddress = $"{temporalHost}:{temporalPort}";

Console.WriteLine($"Connecting to Temporal at {temporalAddress}");

builder.Services.AddSingleton<ITemporalClient>(sp =>
{
    return TemporalClient.ConnectAsync(new TemporalClientConnectOptions
    {
        TargetHost = temporalAddress,
        Namespace = "default"
    }).GetAwaiter().GetResult();
});

// Register JobManager services
builder.Services.AddSingleton<IResourceManager, ResourceManager>();

Console.WriteLine("JobManager services registered");

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => new
{
    Component = "FlinkDotNet.JobManager",
    Status = "Running",
    Architecture = "Native .NET with Temporal",
    Version = "1.0.0"
});

// Cluster overview endpoint (like Flink's /overview)
app.MapGet("/overview", () => new
{
    TaskManagers = 0,
    SlotsTotal = 0,
    SlotsAvailable = 0,
    JobsRunning = 0,
    JobsFinished = 0,
    JobsCancelled = 0,
    JobsFailed = 0
});

Console.WriteLine("===========================================");
Console.WriteLine("JobManager REST API: http://localhost:8081");
Console.WriteLine("Health: http://localhost:8081/");
Console.WriteLine("Overview: http://localhost:8081/overview");
Console.WriteLine("===========================================");

app.Run();
