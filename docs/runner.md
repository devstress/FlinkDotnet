# IR Runner

The IR Runner is a shaded Flink jar that reads a `JobDefinition` JSON, constructs the DataStream topology, manages connectors, and emits consolidated metrics.

Responsibilities:
- Parse `docs/ir-schema-v1.json`-compatible IR
- Build source → ops → sink graph
- Support Kafka source/sink, map/filter, timers, keyed windows
- Expose metrics (records in/out, parallelism, checkpoints)

Packaging:
- Builds Java 17 for Flink 2.1.0 compatibility
- Default local `mvn package` compiles with `--release 17`
- Uses Kafka client directly (no external connector runtime deps)
- Published as `flink-ir-runner.jar` in releases/CI

Build locally:
- Recommended: build the gateway which pre-builds the runner jars automatically
  - `dotnet build FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj -c Release`
  - Outputs in project root and bin output:
    - `flink-ir-runner.jar` (Java 25 when JDK 25 available, otherwise Java 17 fallback)
    - `flink-ir-runner-java17.jar` (compat)
- Alternative (manual): with Maven installed
  - `cd FlinkIRRunner && mvn -DskipTests package`  # builds Java 17 by default
  - To build both explicitly: `mvn -DskipTests clean package -Pjava25 && mvn -DskipTests package -Pjava17`
  - Copy `FlinkIRRunner/target/*.jar` to `FlinkDotNet/Flink.JobGateway/` or set `FLINK_RUNNER_JAR_PATH`

Notes:
- Building via the gateway requires Java 25 and Maven on PATH.
- Runner classfiles target Java 25.
- You can disable the prebuild with `/p:BuildFlinkRunner=false` if you supply prebuilt jars.
