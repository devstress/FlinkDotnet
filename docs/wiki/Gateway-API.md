# FlinkDotnet Gateway API

The FlinkDotnet Gateway provides a REST API that bridges .NET applications with Apache Flink clusters.

## Architecture

```
┌─────────────────┐    HTTP     ┌─────────────────┐    REST     ┌─────────────────┐
│   .NET App      │─────────────▶│ FlinkDotNet     │─────────────▶│ Apache Flink    │
│                 │             │ Gateway         │             │ JobManager      │
│  DataStream API │◀─────────────│                 │◀─────────────│                 │
└─────────────────┘   JSON IR   └─────────────────┘  JobGraph   └─────────────────┘
```

## Communication Flow

### Job Submission

1. **.NET Application** creates job using FlinkDotnet DataStream API
2. **JSON IR Generation** - Job serialized to intermediate representation
3. **HTTP Request** - IR sent to Gateway via REST API
4. **Translation** - Gateway converts JSON IR to Flink JobGraph
5. **Submission** - Gateway submits JobGraph to Flink cluster
6. **Response** - Gateway returns job ID and status

### Job Monitoring

```
.NET App ──HTTP──▶ Gateway ──REST──▶ Flink JobManager
         ◀────────         ◀───────
         Job Status         Job Metrics
```

## API Endpoints

### Job Management

#### Submit Job
```http
POST /api/jobs
Content-Type: application/json

{
  "jobName": "OrderProcessing",
  "jobDefinition": {
    "sources": [
      {
        "type": "kafka",
        "topic": "orders",
        "properties": {...}
      }
    ],
    "transformations": [...],
    "sinks": [...]
  }
}
```

**Response:**
```json
{
  "jobId": "flink-job-12345",
  "status": "RUNNING",
  "submissionTime": "2024-09-02T14:06:00Z"
}
```

#### Get Job Status
```http
GET /api/jobs/{jobId}/status
```

**Response:**
```json
{
  "jobId": "flink-job-12345",
  "status": "RUNNING",
  "startTime": "2024-09-02T14:06:00Z",
  "duration": "PT5M30S"
}
```

#### Cancel Job
```http
DELETE /api/jobs/{jobId}
```

### Job Metrics

#### Get Job Metrics
```http
GET /api/jobs/{jobId}/metrics
```

**Response:**
```json
{
  "recordsIn": 1000000,
  "recordsOut": 950000,
  "throughput": 5000.0,
  "latency": {
    "p50": 10,
    "p95": 25,
    "p99": 50
  }
}
```

## Configuration

### Gateway Configuration

```json
{
  "FlinkGateway": {
    "FlinkJobManagerUrl": "http://flink-jobmanager:8081",
    "Port": 8080,
    "EnableSwagger": true,
    "Cors": {
      "AllowedOrigins": ["http://localhost:3000"]
    }
  }
}
```

### Security

The Gateway supports:
- **Authentication**: JWT tokens for API access
- **Authorization**: Role-based access control
- **TLS**: HTTPS encryption for secure communication

Example authentication:
```http
POST /api/jobs
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

## Error Handling

### Common Error Responses

#### Invalid Job Definition
```json
{
  "error": "ValidationError",
  "message": "Invalid job definition: missing required field 'sources'",
  "details": [...]
}
```

#### Flink Connection Error
```json
{
  "error": "FlinkConnectionError", 
  "message": "Unable to connect to Flink JobManager",
  "retryable": true
}
```

#### Resource Limits
```json
{
  "error": "ResourceLimitExceeded",
  "message": "Maximum number of concurrent jobs exceeded",
  "limit": 10
}
```

## Monitoring

### Health Checks

```http
GET /health
```

**Response:**
```json
{
  "status": "Healthy",
  "flinkConnection": "Connected",
  "uptime": "PT2H30M",
  "version": "1.0.0"
}
```

### Metrics Endpoint

```http
GET /metrics
```

Returns Prometheus-compatible metrics for monitoring gateway performance.

## Client Libraries

### .NET Client

```csharp
// Job Gateway configuration for job submission
services.AddHttpClient("FlinkGateway", client =>
{
    client.BaseAddress = new Uri("http://localhost:18000");
    config.BaseUrl = "http://localhost:8080";
    config.ApiKey = "your-api-key";
    config.Timeout = TimeSpan.FromSeconds(30);
});
```

For complete integration examples, see [Usage Examples](Usage-Examples.md).