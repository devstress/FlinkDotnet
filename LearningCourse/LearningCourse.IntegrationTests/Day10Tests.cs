using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 10: Performance Optimization & Scaling
///
/// These tests validate exercises for performance optimization patterns:
/// - Exercise 101: Resource Optimization (Latency Profiling)
/// - Exercise 102: Watermark Tuning
/// - Exercise 103: Memory Management (Object Pooling & LRU Cache)
/// - Exercise 104: Throughput Tuning (Serialization & Compression)
///
/// Implementation: Uses real Kafka/Flink infrastructure with performance profiling
/// </summary>
[TestFixture]
[Category("day10-performance-optimization-scaling")]
[Category("integration")]
public class Day10Tests : LearningCourseTestBase
{
    private const string Exercise1Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise101";
    private const string Exercise2Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise102";
    private const string Exercise3Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise103";
    private const string Exercise4Path = "Day10-Performance-Optimization-Scaling/Exercise-Solutions/Exercise104";
    private static readonly TimeSpan ExerciseTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Exercise102Timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Exercise103Timeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan Exercise104Timeout = TimeSpan.FromSeconds(30);

    [Test]
    [Description("Exercise 101: Resource Optimization - Latency Profiling")]
    public async Task Exercise101_ResourceOptimization_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 101: Resource Optimization - Latency Profiling");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise1Path, Array.Empty<string>(), ExerciseTimeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 101 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!"), "Exercise should include success marker");
        TestContext.WriteLine("✅ Exercise 101 completed successfully");
    }

    [Test]
    [Description("Exercise 102: Watermark Tuning")]
    public async Task Exercise102_WatermarkTuning_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 102: Watermark Tuning");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise2Path, Array.Empty<string>(), Exercise102Timeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 102 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!"), "Exercise should include success marker");
        TestContext.WriteLine("✅ Exercise 102 completed successfully");
    }

    [Test]
    [Description("Exercise 103: Memory Management - Object Pooling & LRU Cache")]
    public async Task Exercise103_MemoryManagement_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 103: Memory Management");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise3Path, Array.Empty<string>(), Exercise103Timeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 103 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!"), "Exercise should include success marker");
        Assert.That(output, Does.Contain("Object Pooling").Or.Contains("LRU Cache"), "Exercise should test memory optimization patterns");
        TestContext.WriteLine("✅ Exercise 103 completed successfully");
    }

    [Test]
    [Description("Exercise 104: Throughput Tuning - Serialization & Compression")]
    public async Task Exercise104_ThroughputTuning_ShouldExecuteSuccessfully()
    {
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("  Exercise 104: Throughput Tuning");
        TestContext.WriteLine("================================================================================");

        var (exitCode, output, error) = await ExecuteExerciseAsync(Exercise4Path, Array.Empty<string>(), Exercise104Timeout);

        Assert.That(exitCode, Is.EqualTo(0), $"Exercise 104 should complete successfully. Exit code: {exitCode}\nError: {error}");
        Assert.That(output, Does.Contain("[SUCCESS] EXERCISE COMPLETED SUCCESSFULLY!"), "Exercise should include success marker");
        Assert.That(output, Does.Contain("MessagePack").Or.Contains("Throughput"), "Exercise should test serialization optimization");
        TestContext.WriteLine("✅ Exercise 104 completed successfully");
    }
}