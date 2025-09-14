# Gateway API

Base URL defaults to `http://localhost:8080`.

## Enhanced Job Management

### POST `/api/v1/jobs/submit`
- **Body**: `JobDefinition` (IR JSON)
- **Response**: `JobSubmissionResult`
- **Enhanced Features**:
  - Comprehensive validation with detailed error messages
  - Improved error handling with specific guidance for resolution
  - Robust submission process with retry logic
- **Notes**:
  - JobDefinitionValidator provides modular validation with improved error messages
  - For SQL jobs, set `source` to `{ "type": "sql", "statements": ["DDL/DML..."] }`
  - Enhanced fault tolerance with structured error responses

### GET `/api/v1/jobs/{flinkJobId}/status`
- **Response**: `JobStatus`
- **Enhanced Features**:
  - Real-time status updates from Flink 2.1.0 cluster
  - Improved error handling for connection issues
  - Detailed status information with context

### GET `/api/v1/jobs/{flinkJobId}/metrics`
- **Response**: `JobMetrics`
- **Enhanced Features**:
  - JobMetricsBuilder pattern for structured metrics collection
  - Comprehensive metrics from Flink vertices and checkpoints
  - Enhanced error handling for metrics collection failures
  - Improved performance with focused data collection methods
- **Metrics Structure**:
  ```json
  {
    "jobId": "string",
    "flinkJobId": "string", 
    "recordsIn": 0,
    "recordsOut": 0,
    "parallelism": 0,
    "maxParallelism": 0,
    "backpressureInfo": {...},
    "checkpointMetrics": {...}
  }
  ```

### POST `/api/v1/jobs/{flinkJobId}/cancel`
- **Response**: 200 on success
- **Enhanced Features**:
  - Graceful cancellation with proper cleanup
  - Enhanced error handling for cancellation failures
  - Improved logging for troubleshooting

### GET `/api/v1/health`
- **Response**: `OK`
- **Enhanced Features**:
  - Comprehensive health checks for all dependencies
  - Detailed health status information
  - Enhanced monitoring capabilities

## Enhanced Error Handling

The gateway now provides structured error responses with:
- **Specific error codes** for different failure types
- **Detailed error messages** with resolution guidance
- **Context information** for troubleshooting
- **Validation errors** with field-level details

## Code Quality Improvements

- **FlinkJobManager**: Restructured with builder patterns and focused methods
- **Enhanced validation**: Modular JobDefinitionValidator with cognitive complexity <15
- **Improved maintainability**: Complex operations split into testable components
- **Robust error handling**: Comprehensive exception handling and logging

Note: The IR Runner integration provides full job execution capabilities with enhanced reliability and comprehensive metrics collection.
