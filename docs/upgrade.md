# Upgrade Guide

## Upgrading to 1.0.0
- IR schema stabilized at `docs/ir-schema-v1.json`. Ensure your IR generation matches.
- Gateway now automatically builds the IR Runner jar during project build.
  - Ensure Java and Maven are installed and on PATH.
  - You can disable with `/p:BuildFlinkRunner=false` if providing a prebuilt jar.
  - `FLINK_RUNNER_JAR_PATH` remains supported as an override but is no longer required.
- Preferred usage: build jobs via the [`FlinkDotNet.DataStream`](../FlinkDotNet/FlinkDotNet/) API with direct Flink execution environment.
