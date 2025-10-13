using NUnit.Framework;

// Enable parallel test execution at assembly level
// Tests will reuse shared infrastructure (LocalTesting AppHost with Kafka + Flink + Gateway)
// Each test uses unique topics to avoid conflicts
// TaskManager has 10 slots configured in LocalTesting AppHost (line 97 in Program.cs)
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(10)] // Match TaskManager slot count for maximum parallelism