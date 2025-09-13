# Troubleshooting

Common issues and fixes:

- Runner jar not found
  - Ensure `scripts/build_runner.ps1` can download Maven and build.
  - Set `FLINK_RUNNER_JAR_PATH` pointing to a built `flink-ir-runner.jar`.

- Gateway cannot reach Flink
  - Set `FLINK_CLUSTER_HOST=localhost` when exposing JM port to host (LocalTesting).
  - Verify JobManager is up: http://localhost:8081

- Kafka connectivity
  - Verify Kafka bootstrap servers and topics exist.
  - Check network from Flink containers to Kafka.

- Metrics empty
  - Ensure Flink REST metrics endpoints are reachable from the gateway.
  - Enable PrometheusReporter for richer metrics if needed.
