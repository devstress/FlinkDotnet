// Enable parallel test execution at the assembly level
// Tests that require external connections (Kafka) are designed to handle unavailable services gracefully
[assembly: Parallelizable(ParallelScope.Children)]
// Set the number of worker threads (0 means use number of processors)
[assembly: LevelOfParallelism(0)]
