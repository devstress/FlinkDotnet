using NUnit.Framework;

// Enable parallel test execution at assembly level
// Tests will reuse the shared GlobalTestInfrastructure (Kafka + Flink + Gateway)
// Each test uses unique topics via TestContext.CurrentContext.Test.ID to avoid conflicts
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(10)] // Run up to 10 tests in parallel (more than test count for max parallelism)