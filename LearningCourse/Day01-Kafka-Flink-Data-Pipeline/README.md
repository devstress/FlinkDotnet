# Day 1 — Kafka ↔ Flink Data Pipeline (FlinkDotNet)

This day mirrors Baeldung's tutorial “Kafka and Apache Flink Data Pipeline” but implemented with FlinkDotNet and the LocalTesting setup. https://www.baeldung.com/kafka-flink-data-pipeline

What you build:
- Kafka producer sending events to an input topic
- Flink job (submitted via Flink Job Gateway) reading from input, transforming, and writing to output
- Kafka consumer verifying data on the output topic

Prerequisites:
- .NET 9 SDK
- Docker Desktop
- LocalTesting Aspire AppHost (starts Kafka, Flink JM/TM, Flink Job Gateway)

Quick start:
- Start LocalTesting stack: `cd LocalTesting && dotnet run --project LocalTesting.FlinkSqlAppHost/LocalTesting.FlinkSqlAppHost.csproj`
- In a new terminal, navigate to `Exercise-Solutions/PipelineDemo`
  - Submit the Flink job: `dotnet run -- submit`
  - Produce test data: `dotnet run -- produce`
  - Consume from output: `dotnet run -- consume`

Topics used:
- Input: `lc1.flink.input`
- Output: `lc1.flink.output`

Flink job logic (IR-based):
- Source: Kafka input topic
- Transform: identity map (you can swap to `upper` if desired)
- Sink: Kafka output topic

Where is the job submitted?
- Through the Flink Job Gateway at `http://localhost:8080` to the Flink 2.1.0 cluster (JobManager at `http://localhost:8081`).

Troubleshooting tips:
- If submission fails, ensure LocalTesting AppHost is running and Flink UI is reachable.
- Ensure topics exist; the demo creates them automatically if possible.
- If using SQL-based connectors for other days, place connector JARs under `LocalTesting/connectors/flink/lib`.

Next steps:
- Try changing the `.Map("identity")` to `.Map("upper")` and observe transformed outputs.
- Add a `.Where("value IS NOT NULL")` filter.


