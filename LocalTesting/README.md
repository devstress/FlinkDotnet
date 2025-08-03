# LocalTesting - Interactive Stress Test Environment

LocalTesting provides an interactive API environment for debugging and executing Complex Logic Stress Test scenarios with real-time monitoring through Aspire dashboard and specialized UIs.

## Business Flow

The LocalTesting environment implements an 8-step business flow for comprehensive stress testing:

1. **Configure Backpressure**: Set 100 messages/second rate limit per logical queue using Kafka headers
2. **Temporal Message Submission**: Submit job to Temporal to produce 1 million messages to Kafka with 100 partitions and 1000 logical queues. Backpressure blocks submission when hitting rate limits; Temporal retries until downstream processing catches up
3. **Temporal Message Processing**: Submit job to Temporal to process Kafka messages using existing security token logic and correlation ID handling
4. **Flink Concat Job**: Submit Flink job to concatenate 100 messages using saved security tokens, sending to LocalTesting API via Kafka out sink
5. **Kafka In Sink**: Create Kafka in sink to retrieve processed messages from LocalTesting API
6. **Flink Split Job**: Submit Flink job to split messages, adding sending ID and logical queue name using correlation ID matching
7. **Response Output**: Write processed messages to `sample_response` Kafka topic
8. **Message Verification**: Verify top 10 and last 10 messages including both headers and content

## BDD Explanation

The LocalTesting environment transforms BDD (Behavior-Driven Development) test scenarios into executable API endpoints. This approach allows:

- **Step-by-Step Debugging**: Execute each test phase individually through interactive API endpoints
- **Real-Time Monitoring**: Monitor test progress through multiple specialized dashboards
- **Correlation Tracking**: End-to-end tracking of 1 million messages with unique correlation IDs
- **Integration Testing**: Validate complex enterprise streaming scenarios combining Flink, Kafka, Temporal, and HTTP processing

### BDD Features Covered
- **ComplexLogicStressTest.feature**: 1M message processing with correlation ID tracking, security token management, and HTTP batch processing
- **BackpressureTest.feature**: Consumer lag-based flow control following LinkedIn best practices
- **ReliabilityTest.feature**: System reliability and error handling scenarios
- **StressTest.feature**: High-volume performance validation

## Services and Their Purpose

### Core Services

| Service | Purpose | Key Features |
|---------|---------|--------------|
| **ComplexLogicStressTestService** | Orchestrates complete stress test workflow | Message production, correlation tracking, metrics |
| **SecurityTokenManagerService** | Manages token lifecycle and renewal | Auto-renewal every 10,000 messages, thread-safe operations |
| **BackpressureMonitoringService** | Implements lag-based rate limiting | Token bucket refill control, consumer lag monitoring |
| **KafkaProducerService** | High-performance message production | Kafka integration, partition management |
| **FlinkJobManagementService** | Flink job lifecycle management | Job submission, monitoring, status tracking |
| **TemporalSecurityTokenService** | Temporal workflow integration | Durable token workflows, retry handling |
| **AspireHealthCheckService** | System health monitoring | Service health checks, resource monitoring |

### Infrastructure Components

| Component | Configuration | Purpose |
|-----------|---------------|---------|
| **Kafka Cluster** | 3 brokers with KRaft | Production-like messaging with 100 partitions per topic |
| **Flink Cluster** | JobManager + 3 TaskManagers (30 slots) | Stream processing with 1000 logical queues |
| **Temporal Server** | Workflow orchestration | Durable job execution and retry logic |
| **Redis** | Caching layer | State management and token storage |
| **Monitoring Stack** | Grafana, Prometheus, OpenTelemetry | Real-time observability and metrics |

## Quick Start

### Prerequisites
- **.NET 9.0 SDK** (9.0.303 or later)
- **Docker Desktop** (16GB+ RAM recommended)
- **Aspire Workload**: `dotnet workload install aspire`

### Running the Environment

1. **Start Aspire Host**:
   ```bash
   cd LocalTesting/LocalTesting.AppHost
   dotnet run
   ```

2. **Access Interfaces**:
   - **API & Swagger**: http://localhost:5000
   - **Aspire Dashboard**: http://localhost:18888
   - **Kafka UI**: http://localhost:8082
   - **Flink Dashboard**: http://localhost:8081
   - **Grafana**: http://localhost:3000 (admin/admin)
   - **Temporal UI**: http://localhost:8084

### API Endpoints

Execute the 8-step business flow through interactive endpoints:

| Step | Endpoint | Description |
|------|----------|-------------|
| 1 | `POST /api/ComplexLogicStressTest/step1/setup-environment` | Environment validation |
| 2 | `POST /api/ComplexLogicStressTest/step2/configure-security-tokens` | Token service setup |
| 3 | `POST /api/ComplexLogicStressTest/step3/configure-backpressure` | Lag-based rate limiting |
| 4 | `POST /api/ComplexLogicStressTest/step4/produce-messages` | Message production via Temporal |
| 5 | `POST /api/ComplexLogicStressTest/step5/start-flink-job` | Flink streaming jobs |
| 6 | `POST /api/ComplexLogicStressTest/step6/process-batches` | Batch processing workflows |
| 7 | `POST /api/ComplexLogicStressTest/step7/verify-messages` | Top/last 10 message verification |
| - | `POST /api/ComplexLogicStressTest/run-full-stress-test` | Complete automated execution |

## Monitoring Workflow

1. **Pre-Test**: Monitor service health in Aspire Dashboard
2. **Production**: Watch message flow in Kafka UI + Grafana metrics
3. **Processing**: Monitor Flink jobs and Temporal workflows
4. **Verification**: Check correlation ID matching and data integrity

## Troubleshooting

### Common Issues
- **Services Degraded**: Check Aspire Dashboard logs, wait 2-3 minutes for full startup
- **No Messages in Kafka**: Verify broker status and topic creation
- **Flink Job Failures**: Check TaskManager resources and job logs
- **High Consumer Lag**: Monitor backpressure configuration and rate limits

### Resource Requirements
- **Memory**: 16GB+ for all containers (3 Kafka brokers, 3 TaskManagers, monitoring stack)
- **CPU**: 8+ cores recommended for optimal performance
- **Storage**: Adequate disk space for Kafka data retention

## Related Documentation
- [Complex Logic Stress Tests Documentation](../docs/wiki/Complex-Logic-Stress-Tests.md)
- [Rate Limiting Implementation Tutorial](../docs/wiki/Rate-Limiting-Implementation-Tutorial.md)
- [Flink vs Temporal Decision Guide](../docs/flink-vs-temporal-decision-guide.md)