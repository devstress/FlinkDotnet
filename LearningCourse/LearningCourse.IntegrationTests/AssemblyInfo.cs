using NUnit.Framework;

// DISABLE parallel test execution to prevent TaskManager resource contention
// Running tests in parallel causes OutOfMemoryError: Metaspace crashes in TaskManager
// Sequential execution prevents resource exhaustion and ensures stable test runs
// User directive: "disable test running parallel in LearningCourse and I think that it will fix the issue"
[assembly: Parallelizable(ParallelScope.None)]