# Day 1 — Exercise Solutions

Commands
- Start LocalTesting stack: `cd LocalTesting && dotnet run --project BackPressure.AppHost/BackPressure.AppHost.csproj`
- Submit Flink job: `cd LearningCourse/Day01-Kafka-Flink-Data-Pipeline/Exercise-Solutions/PipelineDemo && dotnet run -- submit`
- Produce test data: `dotnet run -- produce`
- Consume output: `dotnet run -- consume`

Expected result
- Produced messages on `lc1.flink.input` are forwarded by Flink to `lc1.flink.output`.
- The consumer displays a non-zero number of consumed messages.

