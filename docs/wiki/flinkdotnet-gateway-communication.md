# FlinkDotNet.Gateway to Apache Flink Communication

This document details how FlinkDotNet.Gateway communicates with Apache Flink clusters to submit and manage streaming jobs.

## Architecture Overview

FlinkDotNet.Gateway acts as a bridge between .NET applications and Apache Flink, providing a seamless integration layer that handles job submission, monitoring, and management.

```
┌─────────────────┐    HTTP     ┌─────────────────┐    REST     ┌─────────────────┐
│   .NET App      │─────────────▶│ FlinkDotNet     │─────────────▶│ Apache Flink    │
│                 │             │ Gateway         │             │ JobManager      │
│ FlinkJobBuilder │◀─────────────│                 │◀─────────────│                 │
└─────────────────┘   JSON IR   └─────────────────┘  JobGraph   └─────────────────┘
```

## Communication Flow

### 1. Job Submission Process

1. **.NET Application** creates job definition using FlinkJobBuilder DSL
2. **JSON IR Generation** - Job definition is serialized to intermediate representation
3. **HTTP Request** - IR is sent to FlinkDotNet.Gateway via REST API
4. **IR Translation** - Gateway converts JSON IR to Flink JobGraph
5. **Flink Submission** - Gateway submits JobGraph to Flink JobManager via REST API
6. **Response Handling** - Gateway returns job ID and status to .NET application

### 2. Job Monitoring

```
.NET App ──HTTP──▶ Gateway ──REST──▶ Flink JobManager
         ◀────────         ◀───────
         Job Status         Job Metrics
```

The gateway continuously monitors job status by:
- Polling Flink JobManager REST API
- Retrieving job metrics and execution statistics
- Providing real-time status updates to .NET applications

### 3. Error Handling and Recovery

The gateway implements robust error handling:
- **Connection Failures**: Automatic retry with exponential backoff
- **Job Failures**: Graceful error reporting and recovery options
- **Timeout Handling**: Configurable timeouts for long-running operations

## API Endpoints

### Job Management

```http
POST /api/v1/jobs
Content-Type: application/json

{
  "jobDefinition": {
    "source": { "type": "kafka", "topic": "input" },
    "operations": [
      { "type": "map", "expression": "value * 2" },
      { "type": "filter", "expression": "value > 10" }
    ],
    "sink": { "type": "kafka", "topic": "output" }
  },
  "jobName": "Processing Job",
  "parallelism": 4
}
```

Response:
```json
{
  "jobId": "flink-job-12345",
  "status": "RUNNING",
  "submissionTime": "2024-01-01T12:00:00Z"
}
```

### Job Status

```http
GET /api/v1/jobs/{jobId}/status
```

Response:
```json
{
  "jobId": "flink-job-12345",
  "status": "RUNNING",
  "startTime": "2024-01-01T12:00:00Z",
  "duration": "00:05:30",
  "parallelism": 4,
  "metrics": {
    "recordsProcessed": 150000,
    "recordsPerSecond": 500,
    "latency": "45ms"
  }
}
```

### Job Control

```http
POST /api/v1/jobs/{jobId}/cancel
POST /api/v1/jobs/{jobId}/stop
POST /api/v1/jobs/{jobId}/restart
```

## Flink REST API Integration

The gateway communicates with Flink JobManager using the standard Flink REST API:

### Job Submission
```
POST http://flink-jobmanager:8081/v1/jobs
Content-Type: application/vnd.flink.req.jobgraph+json
```

### Job Status Monitoring
```
GET http://flink-jobmanager:8081/v1/jobs/{jobId}
GET http://flink-jobmanager:8081/v1/jobs/{jobId}/metrics
```

### Job Control Operations
```
PATCH http://flink-jobmanager:8081/v1/jobs/{jobId}?mode=cancel
PATCH http://flink-jobmanager:8081/v1/jobs/{jobId}?mode=stop
```

## Configuration

### Gateway Configuration

```yaml
# appsettings.json
{
  "FlinkGateway": {
    "FlinkJobManagerUrl": "http://localhost:8081",
    "ConnectionTimeout": "00:00:30",
    "RequestTimeout": "00:02:00",
    "RetryPolicy": {
      "MaxRetries": 3,
      "BackoffMultiplier": 2,
      "InitialDelay": "00:00:01"
    },
    "Monitoring": {
      "StatusPollingInterval": "00:00:05",
      "MetricsPollingInterval": "00:00:10"
    }
  }
}
```

### Flink Cluster Configuration

Ensure Flink JobManager REST API is accessible:

```yaml
# flink-conf.yaml
jobmanager.rpc.address: localhost
jobmanager.rpc.port: 6123
jobmanager.web.port: 8081
rest.port: 8081
rest.address: 0.0.0.0
```

## Security Considerations

### Authentication
- Gateway supports multiple authentication methods
- JWT tokens for .NET application access
- API keys for service-to-service communication
- Integration with enterprise identity providers

### Network Security
- TLS encryption for all HTTP communications
- Network segmentation between gateway and Flink cluster
- Firewall rules to restrict access to Flink REST API

### Authorization
- Role-based access control (RBAC)
- Job-level permissions
- Resource quota enforcement

## Monitoring and Observability

### Gateway Metrics
- Request/response times
- Error rates and types
- Active job counts
- Resource utilization

### Flink Integration Metrics
- Job submission success rate
- Communication latency to Flink cluster
- Failed job recovery statistics

### Logging
- Structured logging with correlation IDs
- Integration with centralized logging systems
- Debug logs for troubleshooting

## Performance Optimization

### Connection Pooling
```csharp
services.Configure<FlinkGatewayOptions>(options =>
{
    options.ConnectionPool = new HttpClientPoolOptions
    {
        MaxConnections = 50,
        ConnectionTimeout = TimeSpan.FromSeconds(30),
        KeepAliveInterval = TimeSpan.FromMinutes(2)
    };
});
```

### Caching
- Job definition caching for reused patterns
- Status response caching with TTL
- Metrics aggregation and caching

### Batching
- Batch multiple job submissions
- Aggregate status requests
- Bulk metrics retrieval

## Troubleshooting

### Common Issues

**Gateway Connection Failures**
```
Error: Unable to connect to Flink JobManager at http://localhost:8081
Solution: Verify Flink cluster is running and REST API is accessible
```

**Job Submission Timeouts**
```
Error: Job submission timeout after 120 seconds
Solution: Increase RequestTimeout in configuration or check Flink cluster resources
```

**Invalid JobGraph Errors**
```
Error: Failed to convert IR to JobGraph
Solution: Validate job definition structure and operation compatibility
```

### Debug Mode

Enable detailed logging:
```json
{
  "Logging": {
    "LogLevel": {
      "FlinkDotNet.Gateway": "Debug",
      "FlinkDotNet.Gateway.FlinkClient": "Trace"
    }
  }
}
```

## Examples

### Complete Integration Example

```csharp
// Configure gateway client
services.AddFlinkGateway(options =>
{
    options.GatewayUrl = "http://localhost:5000";
    options.ApiKey = "your-api-key";
});

// Build and submit job
var job = Flink.JobBuilder
    .FromKafka("orders")
    .Map("enriched = enrichOrder(data)")
    .Filter("enriched.isValid")
    .GroupBy("customerId")
    .Window("TUMBLING", 1, "MINUTES")
    .Aggregate("SUM", "amount")
    .ToKafka("customer-totals");

// Submit job through gateway
var jobId = await job.SubmitAsync("Customer Order Totals");

// Monitor job status
var status = await Flink.Jobs.GetStatusAsync(jobId);
Console.WriteLine($"Job {jobId} is {status.State}");

// Get job metrics
var metrics = await Flink.Jobs.GetMetricsAsync(jobId);
Console.WriteLine($"Processing {metrics.RecordsPerSecond} records/sec");
```

This architecture ensures reliable, scalable, and maintainable integration between .NET applications and Apache Flink clusters.