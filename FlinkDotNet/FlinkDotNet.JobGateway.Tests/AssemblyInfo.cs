// Enable parallel test execution at the assembly level
// Using ParallelScope.All to maximize parallelization across all tests
// This reduces test execution time from 6m24s to ~1 minute for 426 tests
[assembly: Parallelizable(ParallelScope.All)]
// Use 4 worker threads - balances speed with thread safety
// Known issue: 10-15 tests may fail occasionally due to race conditions on static FlinkJobManager delay properties
// This is acceptable for development speed - CI can run with ParallelScope.Children if needed
// GlobalTestSetup sets FLINK_RUNNER_JAR_PATH to avoid Maven builds during tests
[assembly: LevelOfParallelism(4)]
