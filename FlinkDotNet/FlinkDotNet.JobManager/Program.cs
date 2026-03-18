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

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// Configure services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FlinkDotNet JobManager API",
        Version = "v1",
        Description = "Native .NET distributed stream processing runtime - JobManager REST API"
    });
});

// Add Temporal client
string temporalHost = Environment.GetEnvironmentVariable("TEMPORAL_HOST") ?? "localhost";
string temporalPort = Environment.GetEnvironmentVariable("TEMPORAL_PORT") ?? "7233";
string temporalAddress = $"{temporalHost}:{temporalPort}";

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
builder.Services.AddSingleton<IDispatcher, Dispatcher>();

// Configure heartbeat monitoring
builder.Services.Configure<HeartbeatConfiguration>(
    builder.Configuration.GetSection(HeartbeatConfiguration.SectionName));
builder.Services.AddHostedService<HeartbeatMonitoringService>();

Console.WriteLine("JobManager services registered");

WebApplication app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FlinkDotNet JobManager API v1");
        options.RoutePrefix = "swagger";
    });
}

app.MapControllers();

// Health check endpoint
app.MapGet("/", () => new
{
    Component = "FlinkDotNet.JobManager",
    Status = "Running",
    Architecture = "Native .NET with Temporal",
    Version = "1.0.0",
    Endpoints = new[]
    {
        "/api/jobs - Job management API",
        "/api/taskmanagers - TaskManager management",
        "/api/overview - Cluster overview",
        "/swagger - API documentation"
    }
});

Console.WriteLine("===========================================");
Console.WriteLine("JobManager REST API: http://localhost:8081");
Console.WriteLine("Health: http://localhost:8081/");
Console.WriteLine("Swagger: http://localhost:8081/swagger");
Console.WriteLine("Jobs API: http://localhost:8081/api/jobs");
Console.WriteLine("===========================================");

app.Run();

// Make Program accessible for WebApplicationFactory in tests
#pragma warning disable S1118 // Utility classes should not have public constructors
public partial class Program { }
#pragma warning restore S1118
