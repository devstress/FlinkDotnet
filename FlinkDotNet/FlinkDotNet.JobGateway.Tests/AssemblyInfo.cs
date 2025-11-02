// Enable parallel test execution at the assembly level
// Using ParallelScope.All to maximize parallelization across all tests
// This reduces test execution time from 6m24s to ~1 minute for 426 tests
[assembly: Parallelizable(ParallelScope.All)]
// Use 2 worker threads to minimize race conditions while achieving good speedup
// FlinkJobManager delay properties are now thread-safe using Interlocked operations
// GlobalTestSetup sets FLINK_RUNNER_JAR_PATH to avoid Maven builds during tests
[assembly: LevelOfParallelism(2)]
