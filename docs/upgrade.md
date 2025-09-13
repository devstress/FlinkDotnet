# Upgrade Guide

## Upgrading to 1.0.0
- IR schema stabilized at `docs/ir-schema-v1.json`. Ensure your IR generation matches.
- Gateway now requires IR Runner jar for submissions. Configure:
  - `FLINK_CLUSTER_HOST`, `FLINK_CLUSTER_PORT`
  - `FLINK_RUNNER_JAR_PATH` (optional if `scripts/build_runner.ps1` is available)
- Preferred usage: build jobs via `Flink.JobBuilder` or `FlinkDotNet.Pipelines` helpers.

