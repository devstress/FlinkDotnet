# LocalTesting SQL Connectors

To run Flink SQL with external connectors (e.g., Kafka) in LocalTesting:

1) Download connector JARs compatible with your Flink version (e.g., 2.1.x).
2) Place them under `LocalTesting/connectors/flink/lib/` (create the folders if needed).
3) LocalTesting AppHost automatically mounts this directory into Flink JobManager/TaskManager at `/opt/flink/lib`.

Example structure:
```
LocalTesting/connectors/flink/lib/
  flink-connector-kafka-<version>.jar
  flink-json-<version>.jar
```

Then run the SQL integration test which uses `source.sql.statements` with Kafka DDL/DML.

