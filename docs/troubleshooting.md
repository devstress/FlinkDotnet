# Troubleshooting

Common issues and fixes:

- Runner jar not found
  - Build `FlinkDotNet.JobGateway` which prebuilds and copies the runner jar.
    - `dotnet build FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj -c Release`
  - Ensure Java and Maven are installed and available on PATH (required by the prebuild).
  - Optional: disable prebuild with `/p:BuildFlinkRunner=false` and either place a jar at `FlinkDotNet/FlinkDotNet.JobGateway/flink-ir-runner.jar` or set `FLINK_RUNNER_JAR_PATH`.

- Gateway cannot reach Flink
  - Set `FLINK_CLUSTER_HOST=localhost` when exposing JM port to host (LocalTesting).
  - Verify JobManager is up: http://localhost:8081

- Kafka connectivity
  - Verify Kafka bootstrap servers and topics exist.
  - Check network from Flink containers to Kafka.

- Metrics empty
  - Ensure Flink REST metrics endpoints are reachable from the gateway.
  - Enable PrometheusReporter for richer metrics if needed.
