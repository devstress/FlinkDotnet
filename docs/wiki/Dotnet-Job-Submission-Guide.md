# Dotnet Job Submission Guide

This guide provides comprehensive documentation on how to submit .NET streaming jobs to Apache Flink using the FlinkDotnet framework.

## Table of Contents

- [Overview](#overview)
- [Prerequisites](#prerequisites)
- [Quick Start](#quick-start)
- [Step-by-Step Job Submission](#step-by-step-job-submission)
- [Job Configuration Options](#job-configuration-options)
- [Monitoring and Management](#monitoring-and-management)
- [Error Handling](#error-handling)
- [Advanced Examples](#advanced-examples)
- [Troubleshooting](#troubleshooting)
- [Best Practices](#best-practices)

## Overview

FlinkDotnet enables .NET developers to create and submit streaming jobs to Apache Flink clusters using a fluent C# DSL. The framework translates your .NET code into Flink-compatible job definitions and submits them through the Flink Job Gateway.

### Architecture Flow

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   .NET Client   │───▶│ Flink Job Gateway│───▶│ Apache Flink    │
│ (FlinkJobBuilder│HTTP│  (.NET Core)     │    │   Cluster       │
│    DSL Code)    │    │                  │    │ (JobManager +   │
└─────────────────┘    │   - Parse IR     │    │  TaskManager)   │
                       │   - Validate     │    └─────────────────┘
                       │   - Submit       │
                       └──────────────────┘
```

## Prerequisites

Before submitting jobs, ensure you have:

1. **FlinkDotnet Framework**: Add the NuGet package to your project
   ```bash
   dotnet add package Flink.JobBuilder
   ```

2. **Running Infrastructure**:
   - Apache Flink cluster (JobManager + TaskManager)
   - Flink Job Gateway service
   - Kafka cluster (for streaming sources/sinks)
   - Redis (optional, for state management)

3. **Development Environment**:
   - .NET 8.0 or later
   - Visual Studio or VS Code with C# extension

## Quick Start

Here's the simplest example of submitting a streaming job:

```csharp
using Flink.JobBuilder;

// Create a basic streaming job
var job = FlinkJobBuilder
    .FromKafka("input-topic")
    .Where("amount > 100")
    .GroupBy("region")
    .Aggregate("SUM", "amount")
    .ToKafka("output-topic");

// Submit to Flink cluster
var result = await job.Submit("MyStreamingJob");

if (result.IsSuccess)
{
    Console.WriteLine($"Job submitted! Flink Job ID: {result.FlinkJobId}");
}
else
{
    Console.WriteLine($"Submission failed: {result.ErrorMessage}");
}
```

## Step-by-Step Job Submission

### Step 1: Configure FlinkJobBuilder Service

Set up dependency injection in your application:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Flink.JobBuilder.Extensions;

var builder = Host.CreateApplicationBuilder(args);

// Configure FlinkJobBuilder with Job Gateway connection
builder.Services.AddFlinkJobBuilder(config =>
{
    config.BaseUrl = "http://localhost:8080"; // Job Gateway URL
    config.HttpTimeout = TimeSpan.FromMinutes(5);
    config.MaxRetries = 3;
    config.UseHttps = false; // Use HTTPS in production
});

var host = builder.Build();
```

### Step 2: Create Your Streaming Job

Define your streaming pipeline using the fluent DSL:

```csharp
// Get job builder from DI container
var jobBuilder = serviceProvider.CreateJobBuilder();

// Define your streaming pipeline
var job = FlinkJobBuilder
    .FromKafka("user-events", "localhost:9092")
    .Where("eventType == 'purchase'")
    .Map("enrichWithUserData(userId)")
    .GroupBy("userId")
    .Window("TUMBLING", 5, "MINUTES")
    .Aggregate("SUM", "amount")
    .ToKafka("user-purchase-summary", "localhost:9092");
```

### Step 3: Validate Job Definition

Before submission, validate your job definition:

```csharp
// Build the job definition
var jobDefinition = job.BuildJobDefinition();

// Validate the definition
var validation = jobDefinition.Validate();

if (!validation.IsValid)
{
    Console.WriteLine("Job validation failed:");
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"- {error}");
    }
    return;
}

Console.WriteLine("Job validation passed!");
```

### Step 4: Submit the Job

Submit your validated job to the Flink cluster:

```csharp
try
{
    // Submit with custom job name
    var submissionResult = await job.Submit("UserPurchaseAnalysis");
    
    if (submissionResult.IsSuccess)
    {
        Console.WriteLine("✅ Job submitted successfully!");
        Console.WriteLine($"Job ID: {submissionResult.JobId}");
        Console.WriteLine($"Flink Job ID: {submissionResult.FlinkJobId}");
        Console.WriteLine($"Submission Time: {submissionResult.SubmissionTime}");
    }
    else
    {
        Console.WriteLine($"❌ Job submission failed: {submissionResult.ErrorMessage}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"💥 Exception during submission: {ex.Message}");
}
```

### Step 5: Monitor Job Status

Track your job's execution status:

```csharp
// For long-running streaming jobs, monitor status
var jobStatus = await jobBuilder.GetJobStatusAsync(submissionResult.FlinkJobId);
Console.WriteLine($"Job Status: {jobStatus.State}");

// For bounded jobs, wait for completion
var executionResult = await job.SubmitAndWait("BatchProcessingJob");
if (executionResult.Success)
{
    Console.WriteLine($"Job completed successfully at {executionResult.CompletedAt}");
}
```

## Job Configuration Options

### Parallelism and Resource Management

```csharp
var job = FlinkJobBuilder
    .FromKafka("input-topic")
    .WithParallelism(4)                    // Set parallelism level
    .WithCheckpointing(TimeSpan.FromMinutes(1))  // Enable checkpointing
    .WithRestartStrategy("fixed-delay", 3, TimeSpan.FromSeconds(10))
    .Where("status == 'active'")
    .ToKafka("output-topic");
```

### Custom Configuration

```csharp
var job = FlinkJobBuilder
    .FromKafka("events")
    .WithConfiguration(new Dictionary<string, object>
    {
        ["state.backend"] = "rocksdb",
        ["state.checkpoints.dir"] = "hdfs://checkpoints",
        ["execution.checkpointing.interval"] = "60000",
        ["restart-strategy"] = "fixed-delay",
        ["restart-strategy.fixed-delay.attempts"] = "3"
    })
    .Where("amount > 0")
    .ToKafka("processed-events");
```

### Environment-Specific Settings

```csharp
// Development environment
var devJob = FlinkJobBuilder
    .FromKafka("dev-input")
    .WithEnvironment("development")
    .WithCheckpointing(TimeSpan.FromSeconds(30))  // Faster checkpointing for dev
    .ToConsole();  // Output to console for debugging

// Production environment
var prodJob = FlinkJobBuilder
    .FromKafka("prod-input")
    .WithEnvironment("production")
    .WithCheckpointing(TimeSpan.FromMinutes(2))   // Less frequent checkpointing
    .WithBackpressure(enabled: true)              // Enable backpressure handling
    .ToKafka("prod-output");
```

## Monitoring and Management

### Job Status Monitoring

```csharp
// Check job status
var status = await jobGateway.GetJobStatusAsync(flinkJobId);
Console.WriteLine($"State: {status.State}");
Console.WriteLine($"Start Time: {status.StartTime}");
Console.WriteLine($"Duration: {status.Duration}");

// Monitor job metrics
var metrics = await jobGateway.GetJobMetricsAsync(flinkJobId);
Console.WriteLine($"Records Processed: {metrics.RecordsIn}");
Console.WriteLine($"Records Output: {metrics.RecordsOut}");
Console.WriteLine($"Throughput: {metrics.RecordsPerSecond} records/sec");
```

### Flink Web UI Integration

Access the Flink Web UI for detailed monitoring:

```
http://localhost:8081/#/job/{flinkJobId}/overview
```

Key monitoring areas:
- **Overview**: Job status, duration, parallelism
- **Vertices**: Task execution details and metrics
- **Timeline**: Job execution timeline
- **Exceptions**: Error details and stack traces
- **Checkpoints**: Checkpoint history and performance
- **Configuration**: Job configuration parameters

### Job Management Operations

```csharp
// Cancel a running job
await jobGateway.CancelJobAsync(flinkJobId);

// Stop with savepoint (graceful shutdown)
var savepoint = await jobGateway.StopJobWithSavepointAsync(flinkJobId);
Console.WriteLine($"Savepoint created: {savepoint.SavepointPath}");

// Restart from savepoint
var restartResult = await jobGateway.RestartFromSavepointAsync(
    jobDefinition, 
    savepoint.SavepointPath);
```

## Error Handling

### Common Error Scenarios

```csharp
try
{
    var result = await job.Submit("MyJob");
    
    if (!result.IsSuccess)
    {
        switch (result.ErrorCode)
        {
            case "CLUSTER_UNAVAILABLE":
                Console.WriteLine("Flink cluster is not available. Check cluster status.");
                break;
                
            case "INVALID_JOB_DEFINITION":
                Console.WriteLine($"Job definition is invalid: {result.ErrorMessage}");
                break;
                
            case "RESOURCE_EXHAUSTED":
                Console.WriteLine("Cluster resources exhausted. Try reducing parallelism.");
                break;
                
            case "GATEWAY_TIMEOUT":
                Console.WriteLine("Job Gateway timeout. The cluster may be overloaded.");
                break;
                
            default:
                Console.WriteLine($"Unknown error: {result.ErrorMessage}");
                break;
        }
    }
}
catch (HttpRequestException httpEx)
{
    Console.WriteLine($"Network error: {httpEx.Message}");
    Console.WriteLine("Check Job Gateway connectivity and network configuration.");
}
catch (TimeoutException timeoutEx)
{
    Console.WriteLine($"Request timeout: {timeoutEx.Message}");
    Console.WriteLine("The operation took longer than expected. Try increasing timeout.");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    Console.WriteLine("Please check logs for detailed error information.");
}
```

### Retry Logic Implementation

```csharp
public async Task<JobSubmissionResult> SubmitWithRetry(
    FlinkJobBuilder job, 
    string jobName, 
    int maxRetries = 3)
{
    var delay = TimeSpan.FromSeconds(5);
    
    for (int attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            var result = await job.Submit(jobName);
            if (result.IsSuccess)
            {
                return result;
            }
            
            if (attempt < maxRetries)
            {
                Console.WriteLine($"Attempt {attempt} failed: {result.ErrorMessage}");
                Console.WriteLine($"Retrying in {delay.TotalSeconds} seconds...");
                await Task.Delay(delay);
                delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds * 1.5); // Exponential backoff
            }
            else
            {
                return result; // Return the last failed result
            }
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries)
                throw;
                
            Console.WriteLine($"Attempt {attempt} threw exception: {ex.Message}");
            await Task.Delay(delay);
        }
    }
    
    throw new InvalidOperationException("Max retries exceeded");
}
```

## Advanced Examples

### Complex Multi-Stage Pipeline

```csharp
var complexJob = FlinkJobBuilder
    .FromKafka("raw-events")
    
    // Stage 1: Data Cleaning and Validation
    .Where("timestamp IS NOT NULL AND userId IS NOT NULL")
    .Map("cleanAndValidateData(payload)")
    
    // Stage 2: Enrichment with External Data
    .AsyncHttp("http://user-service/api/users/{userId}", "GET", 3000)
    .Map("enrichWithUserProfile(response)")
    
    // Stage 3: Business Logic Processing
    .GroupBy("userId")
    .Window("SESSION", 30, "MINUTES")
    .WithProcessFunction("calculateUserEngagement", new Dictionary<string, object>
    {
        ["sessionTimeoutMs"] = 1800000,
        ["minEventsPerSession"] = 3
    })
    
    // Stage 4: State Management
    .WithState("userSessionState", "map", ttlMs: 7200000)
    
    // Stage 5: Error Handling
    .WithRetry(maxRetries: 3, 
               delayPattern: new List<long> { 1000, 5000, 15000 },
               deadLetterTopic: "failed-events")
    
    // Stage 6: Multiple Outputs
    .ToKafka("processed-events");

// Add side outputs for different event types
var sideOutputSink = new KafkaSinkDefinition { Topic = "high-value-events" };
complexJob.WithSideOutput("high-value", "value > 1000", sideOutputSink);

await complexJob.Submit("ComplexUserEngagementPipeline");
```

### Batch Processing Example

```csharp
var batchJob = FlinkJobBuilder
    .FromDatabase("Server=localhost;Database=analytics;", 
                  "SELECT * FROM user_events WHERE processed = false")
    .Where("event_time >= CURRENT_DATE - INTERVAL '7' DAY")
    .Map("transformToStandardFormat(record)")
    .GroupBy("user_id, event_date")
    .Aggregate("COUNT", "*")
    .ToDatabase("Server=localhost;Database=reports;", 
                "user_daily_stats", 
                "postgresql");

// Submit and wait for completion (batch job)
var result = await batchJob.SubmitAndWait("WeeklyUserStatsBatch");
Console.WriteLine($"Batch job completed: {result.Success}");
```

### Real-time Analytics Pipeline

```csharp
var analyticsJob = FlinkJobBuilder
    .FromKafka("clickstream-events")
    
    // Real-time filtering
    .Where("event_type IN ('page_view', 'click', 'purchase')")
    
    // Sessionization
    .GroupBy("session_id")
    .Window("SESSION", 30, "MINUTES")
    
    // Real-time aggregation
    .Aggregate("COUNT", "event_id")
    .Map("calculateSessionMetrics(events)")
    
    // Real-time alerting
    .Where("session_duration > 1800 OR page_views > 50")
    .AsyncHttp("http://alert-service/api/alerts", "POST", 2000,
               headers: new Dictionary<string, string> { ["Content-Type"] = "application/json" },
               bodyTemplate: "{\"alert_type\":\"high_engagement\",\"session_data\":\"{payload}\"}")
    
    // Store processed data
    .ToRedis("session:stats:{session_id}", operationType: "set")
    .ToKafka("session-analytics");

await analyticsJob.Submit("RealTimeSessionAnalytics");
```

## Troubleshooting

### Common Issues and Solutions

#### 1. Job Submission Timeout

**Problem**: Job submission takes too long or times out

**Solution**:
```csharp
builder.Services.AddFlinkJobBuilder(config =>
{
    config.HttpTimeout = TimeSpan.FromMinutes(10); // Increase timeout
    config.MaxRetries = 5; // Increase retry attempts
});
```

#### 2. Invalid Job Definition

**Problem**: Job validation fails with cryptic errors

**Solution**:
```csharp
// Get detailed validation information
var validation = jobDefinition.Validate();
if (!validation.IsValid)
{
    Console.WriteLine("Validation Errors:");
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"- Field: {error.Field}, Error: {error.Message}");
    }
    
    // Print the full job definition for debugging
    var json = JsonSerializer.Serialize(jobDefinition, new JsonSerializerOptions { WriteIndented = true });
    Console.WriteLine($"Job Definition:\n{json}");
}
```

#### 3. Kafka Connection Issues

**Problem**: Cannot connect to Kafka topics

**Solution**:
```csharp
// Test Kafka connectivity first
try
{
    var kafkaConfig = new KafkaConfig 
    { 
        BootstrapServers = "localhost:9092",
        SecurityProtocol = "PLAINTEXT" // or SASL_SSL for secure clusters
    };
    
    // Validate topics exist
    var topicExists = await ValidateKafkaTopic("input-topic", kafkaConfig);
    if (!topicExists)
    {
        Console.WriteLine("Input topic does not exist. Create it first.");
        return;
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Kafka connectivity issue: {ex.Message}");
}
```

#### 4. Insufficient Resources

**Problem**: Job fails due to lack of cluster resources

**Solution**:
```csharp
// Reduce resource requirements
var job = FlinkJobBuilder
    .FromKafka("input")
    .WithParallelism(2) // Reduce parallelism
    .WithConfiguration(new Dictionary<string, object>
    {
        ["taskmanager.memory.process.size"] = "1024m", // Reduce memory
        ["taskmanager.numberOfTaskSlots"] = "2"        // Reduce slots
    })
    .ToKafka("output");
```

### Debug Mode

Enable detailed logging for troubleshooting:

```csharp
builder.Services.AddLogging(logging =>
{
    logging.SetMinimumLevel(LogLevel.Debug);
    logging.AddConsole();
    logging.AddFile("logs/flinkdotnet-{Date}.txt"); // If using file logging
});

// Enable detailed HTTP logging
builder.Services.AddFlinkJobBuilder(config =>
{
    config.EnableDetailedLogging = true;
    config.LogHttpRequests = true;
    config.LogJobDefinitions = true;
});
```

### Performance Monitoring

```csharp
// Monitor submission performance
var stopwatch = Stopwatch.StartNew();
var result = await job.Submit("PerformanceTestJob");
stopwatch.Stop();

Console.WriteLine($"Submission took: {stopwatch.ElapsedMilliseconds}ms");

if (result.IsSuccess)
{
    // Monitor job startup time
    var startTime = DateTime.UtcNow;
    while (true)
    {
        var status = await jobGateway.GetJobStatusAsync(result.FlinkJobId);
        if (status.State == "RUNNING")
        {
            var startupTime = DateTime.UtcNow - startTime;
            Console.WriteLine($"Job started in: {startupTime.TotalSeconds}s");
            break;
        }
        await Task.Delay(1000);
    }
}
```

## Best Practices

### 1. Job Naming and Organization

```csharp
// Use descriptive, versioned job names
var jobName = $"UserAnalytics_v{version}_{environment}_{DateTime.UtcNow:yyyyMMdd}";
await job.Submit(jobName);

// Organize jobs by domain
namespace Analytics.StreamingJobs
{
    public class UserEngagementPipeline
    {
        public static FlinkJobBuilder CreateJob()
        {
            return FlinkJobBuilder
                .FromKafka("user-events")
                .Where("engagement_score > 0.5")
                .ToKafka("high-engagement-users");
        }
    }
}
```

### 2. Configuration Management

```csharp
// Use configuration files for different environments
public class FlinkJobConfig
{
    public string Environment { get; set; } = "development";
    public string KafkaBootstrapServers { get; set; } = "localhost:9092";
    public string JobGatewayUrl { get; set; } = "http://localhost:8080";
    public int Parallelism { get; set; } = 1;
    public TimeSpan CheckpointInterval { get; set; } = TimeSpan.FromMinutes(1);
}

// Load from appsettings.json
var config = builder.Configuration.GetSection("FlinkJob").Get<FlinkJobConfig>();

var job = FlinkJobBuilder
    .FromKafka("events", config.KafkaBootstrapServers)
    .WithParallelism(config.Parallelism)
    .WithCheckpointing(config.CheckpointInterval)
    .ToKafka("processed-events", config.KafkaBootstrapServers);
```

### 3. Error Handling and Monitoring

```csharp
// Implement comprehensive error handling
public class JobSubmissionService
{
    private readonly ILogger<JobSubmissionService> _logger;
    private readonly IFlinkJobGateway _gateway;
    
    public async Task<bool> SubmitJobSafely(FlinkJobBuilder job, string jobName)
    {
        try
        {
            _logger.LogInformation("Submitting job {JobName}", jobName);
            
            var result = await job.Submit(jobName);
            
            if (result.IsSuccess)
            {
                _logger.LogInformation("Job {JobName} submitted successfully with ID {FlinkJobId}", 
                    jobName, result.FlinkJobId);
                
                // Set up monitoring
                _ = Task.Run(() => MonitorJobAsync(result.FlinkJobId));
                
                return true;
            }
            else
            {
                _logger.LogError("Job {JobName} submission failed: {Error}", 
                    jobName, result.ErrorMessage);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during job {JobName} submission", jobName);
            return false;
        }
    }
    
    private async Task MonitorJobAsync(string flinkJobId)
    {
        while (true)
        {
            try
            {
                var status = await _gateway.GetJobStatusAsync(flinkJobId);
                
                if (status.State == "FAILED")
                {
                    _logger.LogError("Job {FlinkJobId} has failed", flinkJobId);
                    // Send alert, trigger restart logic, etc.
                    break;
                }
                
                await Task.Delay(TimeSpan.FromMinutes(1));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error monitoring job {FlinkJobId}", flinkJobId);
                await Task.Delay(TimeSpan.FromMinutes(5));
            }
        }
    }
}
```

### 4. Testing Strategies

```csharp
// Unit test job definitions
[Test]
public void JobDefinition_Should_BeValid()
{
    var job = FlinkJobBuilder
        .FromKafka("test-input")
        .Where("value > 0")
        .ToKafka("test-output");
    
    var definition = job.BuildJobDefinition();
    var validation = definition.Validate();
    
    Assert.True(validation.IsValid, 
        $"Job definition should be valid. Errors: {string.Join(", ", validation.Errors)}");
}

// Integration test with test containers
[Test]
public async Task JobSubmission_Should_SucceedWithRunningCluster()
{
    // Use Testcontainers to spin up Flink cluster
    using var flinkContainer = new FlinkContainer();
    await flinkContainer.StartAsync();
    
    var job = FlinkJobBuilder
        .FromKafka("test-topic")
        .ToConsole();
    
    var result = await job.Submit("IntegrationTestJob");
    
    Assert.True(result.IsSuccess, $"Job submission should succeed: {result.ErrorMessage}");
}
```

### 5. Production Deployment

```csharp
// Production-ready job submission
public class ProductionJobDeployment
{
    public async Task<DeploymentResult> DeployJobAsync(
        FlinkJobBuilder job, 
        string jobName,
        ProductionConfig config)
    {
        // 1. Validate environment
        await ValidateProductionEnvironment(config);
        
        // 2. Run pre-deployment checks
        await RunPreDeploymentChecks(job, config);
        
        // 3. Create savepoint of existing job (if updating)
        string? savepointPath = null;
        if (config.IsUpdate)
        {
            savepointPath = await CreateSavepoint(config.ExistingJobId);
        }
        
        // 4. Submit new job
        var result = await job.Submit(jobName);
        
        if (result.IsSuccess)
        {
            // 5. Verify job health
            await VerifyJobHealth(result.FlinkJobId);
            
            // 6. Cancel old job (if updating)
            if (config.IsUpdate && config.ExistingJobId != null)
            {
                await CancelOldJob(config.ExistingJobId);
            }
            
            return new DeploymentResult { Success = true, JobId = result.FlinkJobId };
        }
        else
        {
            // Rollback if needed
            if (savepointPath != null)
            {
                await RestoreFromSavepoint(config.ExistingJobId, savepointPath);
            }
            
            return new DeploymentResult { Success = false, Error = result.ErrorMessage };
        }
    }
}
```

## Conclusion

This guide covers the complete workflow for submitting .NET streaming jobs to Apache Flink using FlinkDotnet. The framework provides:

- **Fluent C# DSL** for defining streaming pipelines
- **Automatic IR generation** and job validation
- **Robust error handling** and retry mechanisms
- **Comprehensive monitoring** and management capabilities
- **Production-ready patterns** for enterprise deployment

For more examples and advanced scenarios, see:
- [FlinkDotnet Sample Projects](../../Sample/)
- [Integration Tests](../../Sample/FlinkDotNet.Aspire.IntegrationTests/)
- [Aspire Local Development Setup](./Aspire-Local-Development-Setup.md)
- [Getting Started Guide](./Getting-Started.md)

Need help? Check the [troubleshooting section](#troubleshooting) or review the [integration tests](../../Sample/FlinkDotNet.Aspire.IntegrationTests/Features/JobSubmission.feature) for working examples.