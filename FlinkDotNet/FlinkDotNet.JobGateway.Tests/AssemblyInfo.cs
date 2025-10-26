// Enable parallel test execution at all levels for maximum speed
// Test fixtures use ThreadStatic fields to avoid resource conflicts  
// ProgramIntegrationTests marked NonParallelizable (uses environment variables)
[assembly: Parallelizable(ParallelScope.All)]
// Set the number of worker threads (0 means use number of processors)
[assembly: LevelOfParallelism(0)]
