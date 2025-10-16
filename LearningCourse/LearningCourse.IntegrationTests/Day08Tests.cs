using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Integration tests for Day 8: Stress Testing exercises
/// Tests real Kafka/FlinkDotNet infrastructure for high-volume event processing
/// </summary>
[NonParallelizable]
public class Day08Tests : LearningCourseTestBase
{
    private static readonly TimeSpan StressTestTimeout = TimeSpan.FromSeconds(30);

    [Test]
    public async Task Exercise81_StressTestingWithRealKafka_ShouldProcessHighVolumeEvents()
    {
        // Arrange
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Exercise 8.1: Stress Testing with Real Infrastructure");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("");
        TestContext.WriteLine("Test Objectives:");
        TestContext.WriteLine("  ✓ High-volume Kafka message production");
        TestContext.WriteLine("  ✓ Real Flink stream processing under load");
        TestContext.WriteLine("  ✓ Performance monitoring and benchmarking");
        TestContext.WriteLine("  ✓ Throughput and latency analysis");
        TestContext.WriteLine("");
        
        // Act - Use regular execution with adequate timeout for slow message production
        TestContext.WriteLine("Executing Exercise81...");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            "Day08-Stress-Testing/Exercise-Solutions/Exercise81",
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minute timeout for slow production + Flink processing
        
        // Assert
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise81 should complete successfully. Error output: {error}");
        
        TestContext.WriteLine("");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("[SUCCESS] Exercise 8.1 completed - Real infrastructure stress testing validated");
        TestContext.WriteLine("================================================================================");
    }

    [Test]
    public async Task Exercise82_BackpressureMonitoringWithRealKafka_ShouldProcessVariableLoadScenarios()
    {
        // Arrange
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Exercise 8.2: Backpressure Monitoring with Real Infrastructure");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("");
        TestContext.WriteLine("Test Objectives:");
        TestContext.WriteLine("  ✓ Real-time backpressure detection via Kafka consumer lag");
        TestContext.WriteLine("  ✓ Flink stream processing under variable load");
        TestContext.WriteLine("  ✓ Backpressure scenario testing (normal, overload, recovery)");
        TestContext.WriteLine("  ✓ Production-ready backpressure handling patterns");
        TestContext.WriteLine("");
        
        // Act - Use regular execution with adequate timeout for slow message production
        TestContext.WriteLine("Executing Exercise82...");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            "Day08-Stress-Testing/Exercise-Solutions/Exercise82",
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minute timeout for slow production + Flink processing
        
        // Assert
        Assert.That(exitCode, Is.EqualTo(0), 
            $"Exercise82 should complete successfully. Error output: {error}");
        
        TestContext.WriteLine("");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("[SUCCESS] Exercise 8.2 completed - Real infrastructure backpressure monitoring validated");
        TestContext.WriteLine("================================================================================");
    }

    [Test]
    public async Task Exercise83_PerformanceBenchmarkingWithRealKafka_ShouldExecuteBenchmarkScenarios()
    {
        // Arrange
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Exercise 8.3: Performance Benchmarking with Real Infrastructure");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("");
        TestContext.WriteLine("Test Objectives:");
        TestContext.WriteLine("  ✓ Multi-scenario performance benchmarking");
        TestContext.WriteLine("  ✓ Real Flink stream processing under benchmark workloads");
        TestContext.WriteLine("  ✓ Latency, throughput, memory, and CPU testing");
        TestContext.WriteLine("  ✓ Performance metrics collection and reporting");
        TestContext.WriteLine("");
        
        // Act - Use regular execution with adequate timeout for slow message production
        TestContext.WriteLine("Executing Exercise83...");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            "Day08-Stress-Testing/Exercise-Solutions/Exercise83",
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minute timeout for slow production + Flink processing
        
        // Assert
        Assert.That(exitCode, Is.EqualTo(0), 
            $"Exercise83 should complete successfully. Error output: {error}");
        
        TestContext.WriteLine("");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("[SUCCESS] Exercise 8.3 completed - Real infrastructure performance benchmarking validated");
        TestContext.WriteLine("================================================================================");
    }

    [Test]
    public async Task Exercise84_ResourceMonitoringWithRealKafka_ShouldAnalyzeCapacityPlanning()
    {
        // Arrange
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("Exercise 8.4: Resource Monitoring & Capacity Planning with Real Infrastructure");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("");
        TestContext.WriteLine("Test Objectives:");
        TestContext.WriteLine("  ✓ Real-time resource monitoring during Flink processing");
        TestContext.WriteLine("  ✓ Multi-scenario workload testing (Light, Normal, Heavy)");
        TestContext.WriteLine("  ✓ Capacity planning analysis with production metrics");
        TestContext.WriteLine("  ✓ Resource optimization recommendations");
        TestContext.WriteLine("");
        
        // Act - Use regular execution with adequate timeout for slow message production
        TestContext.WriteLine("Executing Exercise84...");
        var (exitCode, output, error) = await ExecuteExerciseAsync(
            "Day08-Stress-Testing/Exercise-Solutions/Exercise84",
            Array.Empty<string>(),
            TimeSpan.FromMinutes(3)); // 3 minute timeout for slow production + Flink processing
        
        // Assert
        Assert.That(exitCode, Is.EqualTo(0),
            $"Exercise84 should complete successfully. Error output: {error}");
        
        TestContext.WriteLine("");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("[SUCCESS] Exercise 8.4 completed - Real infrastructure resource monitoring validated");
        TestContext.WriteLine("================================================================================");
        TestContext.WriteLine("");
        TestContext.WriteLine("🎉 Day 8: Stress Testing - ALL EXERCISES COMPLETED");
        TestContext.WriteLine("   ✓ Exercise 8.1: Stress testing with circuit breaker");
        TestContext.WriteLine("   ✓ Exercise 8.2: Backpressure monitoring");
        TestContext.WriteLine("   ✓ Exercise 8.3: Performance benchmarking");
        TestContext.WriteLine("   ✓ Exercise 8.4: Resource monitoring & capacity planning");
        TestContext.WriteLine("================================================================================");
    }
}