using NUnit.Framework;

// Enable parallel test execution at the assembly level
[assembly: Parallelizable(ParallelScope.Children)]
// Set the number of worker threads (0 means use number of processors)
[assembly: LevelOfParallelism(0)]
