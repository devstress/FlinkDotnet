// Disable parallel test execution for this assembly
// Reason: Some tests make external Kafka connections which can timeout
// when running in parallel, causing the test suite to hang
[assembly: Parallelizable(ParallelScope.None)]
