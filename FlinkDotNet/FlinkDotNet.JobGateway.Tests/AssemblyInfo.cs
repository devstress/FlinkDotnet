// Enable parallel test execution at all levels for maximum speed
// Test fixtures use ThreadStatic fields to avoid resource conflicts  
// ProgramIntegrationTests marked NonParallelizable (uses environment variables)
[assembly: Parallelizable(ParallelScope.All)]
// Use high worker count since tests are I/O bound (mocked HTTP calls)
[assembly: LevelOfParallelism(16)]
