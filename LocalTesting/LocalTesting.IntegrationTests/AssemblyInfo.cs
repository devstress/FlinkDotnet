using NUnit.Framework;

// Enable parallel test execution at assembly level
// Tests will reuse the shared GlobalTestInfrastructure (Kafka + Flink + Gateway)
// Each test uses unique topics via TestContext.CurrentContext.Test.ID to avoid conflicts
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(7)] // Run up to 7 tests in parallel