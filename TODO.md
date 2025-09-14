# TODO

- Revise `LocalTesting` integration tests. Mirror the `BackPressureExample` environment but swap the custom consumer and back-pressure metrics with a pipeline built from FlinkDotNet, Apache Flink and Temporal. Tests should submit a real job and verify message flow and metrics rather than expecting submission failures.
- Ensure tests handle the job lifecycle correctly. Current runs can report `COMPLETED` status, causing assertions to fail; update tests to expect the proper state or wait for completion.
- Extend the FlinkDotNet Gateway so job submission automatically bundles all required JARs (IR runner, connectors and job artifacts). Remove the need for `FLINK_RUNNER_JAR_PATH` or manual build steps.
- Add test coverage for the auto-bundled gateway path to guarantee the assembled JAR runs end-to-end without manual intervention.

