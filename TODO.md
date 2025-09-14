# TODO

## LocalTesting integration tests
- Replace `LocalTesting/Projects.BackPressure_AppHost` with a new host that provisions Kafka, Flink JobManager/TaskManager, Temporal server, and the `Flink.JobGateway` project.  The gateway must be started without `FLINK_RUNNER_JAR_PATH`.
- Update `FlinkDotNetIntegrationTest`:
  - Drop the branch that expects submission failure.
  - After `Submit`, poll `GetJobStatusAsync` until the job is `RUNNING` or `FINISHED` before sending data.
  - Produce to the input topic, consume from the output topic, and assert Flink metrics (`RecordsIn`, `RecordsOut`, `Checkpoints`) and Temporal workflow completions via `TemporalClient`.
- Update `FlinkSqlIntegrationTest` to rely on the same host and automatic connector/jar bundling.  Validate end‑to‑end SQL execution and Temporal integration.

## Gateway jar bundling
- In `Flink.JobGateway/Services/FlinkJobManager.cs`, refactor `EnsureRunnerJarAsync`:
  - Build `flink-ir-runner.jar` on demand (invoke Maven directly) when not present.
  - Collect connector JARs from `/opt/flink/lib` or a configured path and stage them in a temp directory.
  - Assemble a shaded JAR that combines the IR runner, connectors, and job artifacts, then upload via `/v1/jars/upload`.
  - Remove dependency on `FLINK_RUNNER_JAR_PATH` and `scripts/build_runner.ps1`.

## AppHost cleanup
- Remove the explicit `FLINK_RUNNER_JAR_PATH` environment variable from `LocalTesting/BackPressure.AppHost/Program.cs`; gateway should determine paths internally.
- Expose configuration for connector locations through the application model rather than env vars.

## Job lifecycle handling
- Tests should actively wait for terminal states instead of asserting that the job is not `COMPLETED`.  Implement a retry loop around `GetJobStatusAsync` with timeout.

## Test coverage
- Add an integration test that starts the app host with no prebuilt runner JAR or env vars, submits a job, and verifies that the gateway’s automatic bundling path produces a running job.
