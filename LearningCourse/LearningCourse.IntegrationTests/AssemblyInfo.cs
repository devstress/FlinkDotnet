using NUnit.Framework;

// Enable parallel test execution at assembly level
// Tests will reuse shared infrastructure (Aspire AppHost with Kafka + Flink + Gateway)
// Each test class uses unique topics via TestContext to avoid conflicts
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(5)] // Run up to 5 tests in parallel (sufficient for current test count)