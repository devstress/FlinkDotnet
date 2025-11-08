using NUnit.Framework;

// Enable parallel test execution at assembly level
// Tests use native .NET JobManager and TaskManager with Temporal
// No Java/Flink dependencies - pure .NET distributed processing
[assembly: Parallelizable(ParallelScope.All)]
[assembly: LevelOfParallelism(4)] // Run up to 4 tests in parallel for native .NET execution
