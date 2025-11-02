// Enable parallel test execution at the assembly level
// Changed from ParallelScope.Children to ParallelScope.All to parallelize test fixtures
// This reduces test execution time from 6m24s to <1 minute for 426 tests
[assembly: Parallelizable(ParallelScope.All)]
// Set the number of worker threads (0 means use number of processors)
// Tests use mocked HttpClient instances which are thread-safe with isolated instances per test
// GlobalTestSetup sets FLINK_RUNNER_JAR_PATH to avoid Maven builds during tests
[assembly: LevelOfParallelism(0)]
