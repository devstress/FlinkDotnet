# IR Runner

The IR Runner is a shaded Flink jar that reads a `JobDefinition` JSON, constructs the DataStream topology, manages connectors, and emits consolidated metrics.

Responsibilities:
- Parse `docs/ir-schema-v1.json`-compatible IR
- Build source → ops → sink graph
- Support Kafka source/sink, map/filter, timers, keyed windows
- Expose metrics (records in/out, parallelism, checkpoints)

Packaging:
- Java 17, Flink 2.x
- Uses Kafka client directly (no external connector runtime deps)
- Published as `flink-ir-runner.jar` in releases/CI

Build locally:
- Windows/PowerShell: `./scripts/build_runner.ps1`
- Linux/macOS (with Maven installed): `cd FlinkIRRunner && mvn -DskipTests package`
