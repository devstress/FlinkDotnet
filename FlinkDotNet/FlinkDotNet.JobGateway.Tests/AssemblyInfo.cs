// Enable parallel test execution at the assembly level
[assembly: Parallelizable(ParallelScope.Children)]
// Set the number of worker threads (0 means use number of processors)
// Tests use mocked HttpClient instances which are not fully thread-safe at high parallelism
[assembly: LevelOfParallelism(0)]
