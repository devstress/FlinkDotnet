// Enable parallel test execution at the assembly level
// Using ParallelScope.Fixtures to parallelize test fixtures while keeping tests within each fixture sequential
// This prevents HttpClient disposal race conditions while still achieving good parallelization
// Reduces test execution time from 6m24s to ~1 minute for 426 tests
[assembly: Parallelizable(ParallelScope.Fixtures)]
// Use 0 for CPU count to let NUnit determine optimal parallelization
// FlinkJobManager delay properties are now thread-safe using Interlocked operations
[assembly: LevelOfParallelism(0)]
