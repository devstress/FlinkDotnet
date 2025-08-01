# LocalTesting - Complex Logic Stress Test Interactive Interface

This solution provides an interactive Swagger-based API for debugging and executing Complex Logic Stress Test scenarios step by step in a local development environment.

## Overview

The LocalTesting solution transforms the BDD test scenarios from `docs/wiki/Complex-Logic-Stress-Tests.md` into executable API endpoints, allowing developers to:

- Debug each step of the stress test individually
- Monitor real-time system behavior through dashboards
- Test the latest lag-based backpressure implementation
- Interact with a full Aspire environment including Kafka 3-broker cluster, Flink, OpenTelemetry, and more

## Architecture

### Infrastructure Components

- **3 Kafka Brokers**: Full KRaft cluster for production-like testing
- **Apache Flink Cluster**: JobManager + 3 TaskManagers with 10 slots each
- **Flink SQL Gateway**: Interactive SQL query interface
- **Redis**: Caching and state management
- **Temporal Server + UI**: Durable workflow execution for long-running processes
- **OpenTelemetry Collector**: Distributed tracing and metrics collection
- **Prometheus**: Metrics storage and querying
- **Grafana**: Visualization dashboards
- **Kafka UI**: Cluster management and monitoring

### Technology Decision Guide

For guidance on when to use Flink vs Temporal for different processing requirements, see our comprehensive [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md).

### API Endpoints

The Web API exposes the following step-by-step endpoints:

#### Step 1: Environment Setup
- `POST /api/ComplexLogicStressTest/step1/setup-environment`
- Validates that all Aspire services are running

#### Step 2: Security Token Configuration  
- `POST /api/ComplexLogicStressTest/step2/configure-security-tokens`
- `GET /api/ComplexLogicStressTest/step2/token-status`
- Configures token renewal every N messages (default: 10,000)

#### Step 3: Backpressure Configuration
- `POST /api/ComplexLogicStressTest/step3/configure-backpressure`
- `GET /api/ComplexLogicStressTest/step3/backpressure-status`
- Sets up lag-based rate limiter that **stops token bucket refilling when consumer lag exceeds threshold**

#### Step 4: Message Production
- `POST /api/ComplexLogicStressTest/step4/produce-messages`
- Generates messages with unique correlation IDs and sends to Kafka

#### Step 5: Flink Job Management
- `POST /api/ComplexLogicStressTest/step5/start-flink-job`
- `GET /api/ComplexLogicStressTest/step5/flink-jobs`
- `GET /api/ComplexLogicStressTest/step5/flink-job/{jobId}`
- Starts and monitors Apache Flink streaming jobs

#### Step 6: Batch Processing
- `POST /api/ComplexLogicStressTest/step6/process-batches`
- Processes messages in batches through HTTP endpoint with correlation tracking

#### Step 7: Message Verification
- `POST /api/ComplexLogicStressTest/step7/verify-messages`
- Verifies correlation ID matching and displays top/last processed messages

#### Full Automation
- `POST /api/ComplexLogicStressTest/run-full-stress-test`
- `GET /api/ComplexLogicStressTest/test-status/{testId}`
- `GET /api/ComplexLogicStressTest/test-status`
- Executes complete stress test or monitors running tests

## Latest Backpressure Implementation

The system now includes **LagBasedRateLimiter** which implements the "stops refill bucket when reaching threshold" behavior:

### How It Works

1. **Token Bucket**: Maintains a bucket of tokens that refills at a configured rate
2. **Lag Monitoring**: Continuously monitors Kafka consumer group lag
3. **Threshold Control**: When lag > threshold (default: 5 seconds):
   - **Stops token bucket refilling** (key feature)
   - Existing tokens can still be consumed
   - Creates natural backpressure
4. **Recovery**: When lag ≤ threshold:
   - **Resumes normal token bucket refilling**
   - System returns to normal operation

### Configuration Example

```json
{
  "consumerGroup": "stress-test-group",
  "lagThresholdSeconds": 5.0,
  "rateLimit": 1000.0,
  "burstCapacity": 5000.0
}
```

## Getting Started

### Prerequisites

- .NET 8.0 SDK
- Docker Desktop
- At least 16GB RAM (for all containers)

### Running the Environment

1. **Start Aspire Host**:
   ```bash
   cd LocalTesting/LocalTesting.AppHost
   dotnet run
   ```

2. **Access Dashboards**:
   - **Swagger API**: http://localhost:5000
   - **Kafka UI**: http://localhost:8080
   - **Flink UI**: http://localhost:8081
   - **Grafana**: http://localhost:3000 (admin/admin)
   - **Prometheus**: http://localhost:9090

3. **Execute Tests**:
   - Open Swagger UI at http://localhost:5000
   - Follow the step-by-step endpoints or use the full automation

### Example: Running a Complete Stress Test

```bash
# Using curl to start a full stress test
curl -X POST "http://localhost:5000/api/ComplexLogicStressTest/run-full-stress-test" \
  -H "Content-Type: application/json" \
  -d '{
    "messageCount": 100000,
    "batchSize": 100,
    "tokenRenewalInterval": 10000,
    "consumerGroup": "stress-test-group",
    "lagThresholdSeconds": 5.0,
    "rateLimit": 1000.0,
    "burstCapacity": 5000.0
  }'

# Check test status
curl "http://localhost:5000/api/ComplexLogicStressTest/test-status/{testId}"
```

## Monitoring and Debugging

### Real-Time Monitoring

- **Backpressure Status**: Monitor lag-based rate limiting in real-time
- **Token Bucket State**: See current tokens, refill status, and utilization
- **Security Token Renewals**: Track token renewal count and timing
- **Kafka Metrics**: Monitor topic throughput, consumer lag, and cluster health
- **Flink Metrics**: Track job performance, checkpointing, and backpressure

### Debug Features

- **Step-by-Step Execution**: Execute each test phase individually
- **Interactive Configuration**: Adjust parameters dynamically through API
- **Message Correlation Tracking**: Follow messages through the entire pipeline
- **Error Handling**: Detailed error reporting and recovery suggestions

## Development Notes

### Key Improvements Over BDD Tests

1. **Interactive Debugging**: Step through each phase manually
2. **Real-Time Monitoring**: Live dashboards and metrics
3. **Dynamic Configuration**: Change parameters without restarting tests
4. **Production-Like Environment**: 3-broker Kafka cluster with full observability
5. **Latest Backpressure**: Includes the newest lag-based rate limiting implementation

### Extending the API

To add new test scenarios:

1. Add endpoints to `ComplexLogicStressTestController`
2. Implement business logic in the service classes
3. Update Swagger documentation with `[SwaggerOperation]` attributes
4. Test through the interactive UI

## Troubleshooting

### Common Issues

- **Docker Memory**: Ensure Docker has at least 16GB RAM allocated
- **Port Conflicts**: Check that ports 5000, 8080, 8081, 3000, 9090 are available
- **Container Startup**: Allow 2-3 minutes for all containers to start properly
- **Kafka Connectivity**: Verify Kafka brokers are healthy in Kafka UI

### Performance Tuning

- Adjust JVM heap settings in `Program.cs` for Kafka brokers
- Modify Flink TaskManager memory and slots for different workloads
- Configure backpressure thresholds based on your system capacity

## Related Documentation

- [**LocalTesting Interactive Environment - Complete UI Guide**](../docs/wiki/LocalTesting-Interactive-Environment.md) - ⭐ **Comprehensive guide with screenshots** of all monitoring interfaces and UI components
- [Complex Logic Stress Tests Documentation](../docs/wiki/Complex-Logic-Stress-Tests.md)
- [Rate Limiting Implementation Tutorial](../docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Aspire Local Development Setup](../docs/wiki/Aspire-Local-Development-Setup.md)