using Flink.JobGateway.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Configuration constants
const string DefaultListenAddress = "http://0.0.0.0:8080";

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Flink Job Gateway API",
        Version = "v1",
        Description = "REST API for submitting and managing Apache Flink jobs from .NET applications"
    });
});

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.DefaultApiVersion = new Microsoft.AspNetCore.Mvc.ApiVersion(1, 0);
});

builder.Services.AddVersionedApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// Register services
builder.Services.AddHttpClient<IFlinkJobManager, FlinkJobManager>();

// Configure logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

// Explicitly bind to port 8080 for reliable startup in containerized environments
var urls = builder.Configuration["ASPNETCORE_URLS"] ?? DefaultListenAddress;
builder.WebHost.UseUrls(urls);

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Flink Job Gateway API v1");
        c.RoutePrefix = string.Empty; // Make Swagger UI the default page
    });
}

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok("OK"));
app.MapGet("/api/v1/health", () => Results.Ok(new { status = "OK", timestamp = DateTime.UtcNow }));

await app.RunAsync();
