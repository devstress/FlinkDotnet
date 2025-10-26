// Enable parallel test execution at all levels for maximum speed
// Test fixtures use ThreadStatic fields to avoid resource conflicts  
// ProgramIntegrationTests marked NonParallelizable (uses environment variables)
[assembly: Parallelizable(ParallelScope.All)]
// Use moderate worker count to balance speed and stability
[assembly: LevelOfParallelism(8)]
