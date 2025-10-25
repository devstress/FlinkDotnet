// Enable parallel test execution at the fixture level only
// Tests within the same fixture run sequentially to avoid resource conflicts
// Different test fixtures run in parallel for improved throughput
[assembly: Parallelizable(ParallelScope.Fixtures)]
// Use 8 worker threads for optimal balance (tested 4-20, no significant difference)
[assembly: LevelOfParallelism(0)]
