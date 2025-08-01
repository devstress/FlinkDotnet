using Aspire.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

// Start with just Redis to test if DCP works at all
var redis = builder.AddRedis("redis");

// Add only the LocalTesting Web API first
var localTestingApi = builder.AddProject("localtesting-webapi", "../LocalTesting.WebApi/LocalTesting.WebApi.csproj")
    .WithReference(redis)
    .WithHttpEndpoint(port: 5000, name: "http");

builder.Build().Run();