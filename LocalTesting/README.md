# LocalTesting - FlinkDotNet Observability Environment

## 1. Business Flow Diagram

```
📥 100 Customer Queues
         ↓
🔀 Kafka (20 Partitions)
   • 3-broker cluster
   • LZ4 compression
   • 128KB batches
         ↓
⚡ Flink Processing
   • JobManager + 3 TaskManagers
   • 24 total slots (8 each)
   • Real-time stream processing
         ↓
🔄 Temporal Workflows (10%)
   • First 10 customers trigger workflows
   • Complex orchestration patterns
         ↓
📤 Output Processing
   • End-to-end pipeline
   • Full observability
```

## 2. Component Configuration

### Kafka Connection Strings & Environment Variables
**File**: [`LocalTesting.AppHost/Program.cs`](LocalTesting.AppHost/Program.cs) lines 315-318, 88-141
```bash
# WebAPI Environment Variables (lines 315-318)
KAFKA_BOOTSTRAP_SERVERS="kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092"
KAFKA_DEFAULT_PARTITIONS="10"
KAFKA_REQUEST_TIMEOUT_MS="30000"
KAFKA_RETRY_BACKOFF_MS="1000"

# Kafka Broker Container Configuration (lines 88-141)
KAFKA_NODE_ID="1|2|3"
KAFKA_PROCESS_ROLES="broker,controller"
KAFKA_LISTENERS="PLAINTEXT://0.0.0.0:9092,CONTROLLER://0.0.0.0:9093"
KAFKA_ADVERTISED_LISTENERS="PLAINTEXT://kafka-broker-X:9092"
KAFKA_CONTROLLER_QUORUM_VOTERS="1@kafka-broker-1:9093,2@kafka-broker-2:9093,3@kafka-broker-3:9093"
CLUSTER_ID="LOCAL_TESTING_KRAFT_CLUSTER_2024"
KAFKA_OFFSETS_TOPIC_REPLICATION_FACTOR="3"
KAFKA_AUTO_CREATE_TOPICS_ENABLE="true"
KAFKA_NUM_PARTITIONS="10"
KAFKA_HEAP_OPTS="-Xmx512M -Xms256M"
```

**File**: [`LocalTesting.WebApi/appsettings.json`](LocalTesting.WebApi/appsettings.json) lines 13-15
```json
"Kafka": {
  "BootstrapServers": "kafka-broker-1:9092,kafka-broker-2:9092,kafka-broker-3:9092"
}
```

### Flink Connection Strings & Configuration
**File**: [`LocalTesting.AppHost/Program.cs`](LocalTesting.AppHost/Program.cs) lines 157-209, 319
```bash
# WebAPI Environment Variable (line 319)
FLINK_JOBMANAGER_URL="http://flink-jobmanager:8081"

# Flink JobManager Properties (lines 160-169)
FLINK_PROPERTIES="
jobmanager.rpc.address: flink-jobmanager
jobmanager.rpc.port: 6123
jobmanager.memory.process.size: 1024m
jobmanager.memory.off-heap.size: 64m
taskmanager.numberOfTaskSlots: 8
parallelism.default: 24
rest.bind-address: 0.0.0.0
rest.port: 8081"

# TaskManager Properties (lines 175-181, 189-195, 203-209)
taskmanager.memory.process.size: 1024m
taskmanager.numberOfTaskSlots: 8
taskmanager.host: flink-taskmanager-X
```

**File**: [`LocalTesting.WebApi/appsettings.json`](LocalTesting.WebApi/appsettings.json) lines 8-12
```json
"Flink": {
  "UseFlinkDotNet": true,
  "JobManagerUrl": "http://localhost:8081",
  "SqlGatewayUrl": "http://localhost:8083"
}
```

### Temporal Connection Strings & Configuration
**File**: [`LocalTesting.AppHost/Program.cs`](LocalTesting.AppHost/Program.cs) lines 228-248, 320
```bash
# WebAPI Environment Variable (line 320)
TEMPORAL_SERVER_URL="temporal-server:7233"

# Temporal Server Container Configuration (lines 230-247)
DB="postgres12"
DB_PORT="5432"
POSTGRES_SEEDS="temporal-postgres"
POSTGRES_USER="temporal"
POSTGRES_PWD="temporal"
DBNAME="temporal"
VISIBILITY_DBNAME="temporal_visibility"
TEMPORAL_CLI_ADDRESS="temporal-server:7233"
```

**File**: [`LocalTesting.WebApi/appsettings.json`](LocalTesting.WebApi/appsettings.json) lines 16-39
```json
"Temporal": {
  "ServerUrl": "temporal-server:7233",
  "Namespace": "default",
  "AgentOptimization": {
    "MaxConcurrentActivities": 100,
    "MaxConcurrentWorkflowTasks": 100,
    "MaxConcurrentLocalActivities": 100,
    "WorkerCount": 10,
    "ActivityTaskTimeout": "00:05:00",
    "WorkflowTaskTimeout": "00:01:00",
    "HeartbeatTimeout": "00:00:30",
    "ScheduleToCloseTimeout": "00:10:00",
    "ScheduleToStartTimeout": "00:01:00",
    "StartToCloseTimeout": "00:05:00"
  }
}
```

### Database Connection String
**File**: [`LocalTesting.AppHost/Program.cs`](LocalTesting.AppHost/Program.cs) lines 212-225
```bash
# PostgreSQL for Temporal Container (lines 213-220)
POSTGRES_DB="temporal"
POSTGRES_USER="temporal"
POSTGRES_PASSWORD="temporal"
POSTGRES_HOST_AUTH_METHOD="trust"
POSTGRES_INITDB_ARGS="--auth-host=trust"
POSTGRES_MAX_CONNECTIONS="100"
POSTGRES_SHARED_BUFFERS="128MB"
```

### OpenTelemetry & Observability Endpoints
**File**: [`LocalTesting.AppHost/Program.cs`](LocalTesting.AppHost/Program.cs) lines 321-328
```bash
# WebAPI OpenTelemetry Configuration (lines 321-328)
OTEL_EXPORTER_OTLP_ENDPOINT="http://otel-collector:4318"
OTEL_EXPORTER_OTLP_PROTOCOL="http/protobuf"
OTEL_EXPORTER_OTLP_TRACES_ENDPOINT="http://otel-collector:4317"
LOKI_ENDPOINT="http://loki:3100"
GRAFANA_URL="http://grafana:3000"
PROMETHEUS_URL="http://prometheus:9090"
ASPIRE_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:13323"
DOTNET_DASHBOARD_OTLP_ENDPOINT_URL="http://localhost:13323"
```

**Service Port Mappings** (from Program.cs WithHttpEndpoint calls):
- **Kafka UI**: http://localhost:18001 (line 146)
- **Flink UI**: http://localhost:18002 (line 158)
- **Temporal Server**: http://localhost:18003 (line 229)
- **Temporal UI**: http://localhost:18004 (line 252)
- **Loki**: http://localhost:18005 (line 259)
- **Prometheus**: http://localhost:18006 (line 268)
- **Grafana**: http://localhost:18010 (line 294)
- **WebAPI**: http://localhost:18000 (line 329)

## 3. Run Observability Tests

### Prerequisites
```bash
# Verify .NET 9.0 requirement
dotnet --version  # Must show 9.0.x
```

### Quick Test Commands
```bash
# Run Aspire environment
cd LocalTesting.AppHost && dotnet run

# Run observability tests. No need to run `cd LocalTesting.AppHost && dotnet run` first
dotnet test LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj --filter "Category=observability"
```