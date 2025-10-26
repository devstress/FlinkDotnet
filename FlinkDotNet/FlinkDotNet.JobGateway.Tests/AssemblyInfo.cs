// Enable parallel test execution but mark fixtures as non-parallelizable
// This allows different test classes to run in parallel (safe)
// But tests within same class run sequentially (prevents HttpClient disposal races)
[assembly: Parallelizable(ParallelScope.Fixtures)]
// Use high worker count to maximize parallel fixture execution
[assembly: LevelOfParallelism(16)]
