using Xunit;
using Xunit.Abstractions;
using Flink.JobBuilder.Backpressure;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System;

namespace FlinkDotNet.Aspire.IntegrationTests.Unit;

/// <summary>
/// Unit tests for the simplified lag-based rate limiter.
/// Tests the simple approach with consumer lag monitoring for natural backpressure.
/// </summary>
public class ImprovedRateLimiterAsyncTests
{
    private readonly ITestOutputHelper _output;

    public ImprovedRateLimiterAsyncTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task LagBasedRateLimiter_ShouldPauseRefillWhenLagIsHigh()
    {
        // Arrange
        _output.WriteLine("🚀 Testing lag-based rate limiter with simulated high lag");
        
        // Create a mock lag monitor that simulates high lag
        var mockLagMonitor = new MockKafkaConsumerLagMonitor();
        mockLagMonitor.SetLag("test-group", TimeSpan.FromSeconds(10)); // High lag
        
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 10.0,
            burstCapacity: 10.0,
            consumerGroup: "test-group",
            lagThreshold: TimeSpan.FromSeconds(5),
            lagMonitor: mockLagMonitor);
        
        // Consume initial tokens
        var initialSuccess = rateLimiter.TryAcquire(10);
        _output.WriteLine($"📊 Initial token acquisition: {initialSuccess}");
        Assert.True(initialSuccess);
        
        // Wait for lag check to pause refilling
        await Task.Delay(1500); // Allow lag check timer to run
        
        // Act - Try to acquire more tokens (should fail since refilling is paused)
        var afterLagSuccess = rateLimiter.TryAcquire(1);
        _output.WriteLine($"📊 Token acquisition after high lag: {afterLagSuccess}");
        _output.WriteLine($"📊 Current lag: {rateLimiter.CurrentLag.TotalSeconds}s");
        _output.WriteLine($"📊 Is refill paused: {rateLimiter.IsRefillPaused}");
        
        // Assert
        Assert.False(afterLagSuccess); // Should fail due to paused refilling
        Assert.True(rateLimiter.IsRefillPaused);
        
        rateLimiter.Dispose();
    }

    [Fact]
    public async Task LagBasedRateLimiter_ShouldResumeRefillWhenLagDecreases()
    {
        // Arrange
        _output.WriteLine("🔄 Testing lag-based rate limiter recovery when lag decreases");
        
        var mockLagMonitor = new MockKafkaConsumerLagMonitor();
        mockLagMonitor.SetLag("test-group", TimeSpan.FromSeconds(10)); // Start with high lag
        
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 10.0,
            burstCapacity: 10.0,
            consumerGroup: "test-group",
            lagThreshold: TimeSpan.FromSeconds(5),
            lagMonitor: mockLagMonitor);
        
        // Consume initial tokens and wait for pause
        rateLimiter.TryAcquire(10);
        await Task.Delay(1500); // Allow lag check to pause refilling
        
        _output.WriteLine($"📊 Refill paused due to high lag: {rateLimiter.IsRefillPaused}");
        Assert.True(rateLimiter.IsRefillPaused);
        
        // Act - Reduce lag below threshold
        mockLagMonitor.SetLag("test-group", TimeSpan.FromSeconds(2)); // Low lag
        await Task.Delay(1500); // Allow lag check to resume refilling
        
        // Wait a bit for tokens to refill
        await Task.Delay(1000);
        
        // Try to acquire tokens (should succeed since refilling resumed)
        var afterRecoverySuccess = rateLimiter.TryAcquire(5);
        _output.WriteLine($"📊 Token acquisition after lag recovery: {afterRecoverySuccess}");
        _output.WriteLine($"📊 Current lag: {rateLimiter.CurrentLag.TotalSeconds}s");
        _output.WriteLine($"📊 Is refill paused: {rateLimiter.IsRefillPaused}");
        _output.WriteLine($"📊 Current tokens: {rateLimiter.CurrentTokens:F1}");
        
        // Assert
        Assert.False(rateLimiter.IsRefillPaused);
        Assert.True(afterRecoverySuccess); // Should succeed due to resumed refilling
        
        rateLimiter.Dispose();
    }

    [Fact]
    public void LagBasedRateLimiter_ShouldProvideProductionConfiguration()
    {
        // Arrange & Act
        _output.WriteLine("📋 Testing production configuration for lag-based rate limiter");
        var (rateLimiter, config) = RateLimiterFactory.CreateProductionConfiguration(
            rateLimit: 1000.0,
            burstCapacity: 2000.0,
            consumerGroup: "production-group");
        
        // Assert
        _output.WriteLine($"📊 Rate Limit: {rateLimiter.CurrentRateLimit} ops/sec");
        _output.WriteLine($"📊 Max Tokens: {rateLimiter.MaxTokens}");
        _output.WriteLine($"📊 Consumer Group: {rateLimiter.ConsumerGroup}");
        _output.WriteLine($"📊 Lag Threshold: {rateLimiter.LagThreshold.TotalSeconds}s");
        _output.WriteLine("📋 Configuration:");
        _output.WriteLine(config);
        
        Assert.Equal(1000.0, rateLimiter.CurrentRateLimit);
        Assert.Equal(2000.0, rateLimiter.MaxTokens);
        Assert.Equal("production-group", rateLimiter.ConsumerGroup);
        Assert.Equal(TimeSpan.FromSeconds(5), rateLimiter.LagThreshold);
        Assert.False(string.IsNullOrEmpty(config));
        
        rateLimiter.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_ShouldBeNonBlocking_WhenTokensAreNotAvailable()
    {
        // Arrange
        _output.WriteLine("🚀 Testing non-blocking AcquireAsync implementation");
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 2.0,
            burstCapacity: 2.0,
            consumerGroup: "test-group");
        
        // Consume initial tokens
        await rateLimiter.TryAcquireAsync(2);
        _output.WriteLine($"📊 Initial tokens consumed, remaining: {rateLimiter.CurrentTokens:F1}");
        
        // Act & Assert
        var stopwatch = Stopwatch.StartNew();
        var tasks = new Task[3];
        
        for (int i = 0; i < 3; i++)
        {
            int taskId = i + 1;
            tasks[i] = Task.Run(async () =>
            {
                var taskStopwatch = Stopwatch.StartNew();
                _output.WriteLine($"   Task {taskId}: Starting AcquireAsync at {DateTime.Now:HH:mm:ss.fff}");
                
                await rateLimiter.AcquireAsync(1);
                
                taskStopwatch.Stop();
                _output.WriteLine($"   Task {taskId}: ✅ Completed after {taskStopwatch.ElapsedMilliseconds}ms");
            });
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        _output.WriteLine($"📊 All tasks completed in {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"📊 Final utilization: {rateLimiter.CurrentUtilization:P1}");
        
        // Verify that tasks completed in a reasonable time (not immediately due to rate limiting)
        Assert.True(stopwatch.ElapsedMilliseconds >= 1000, "Tasks should be rate limited, not complete immediately");
        Assert.True(stopwatch.ElapsedMilliseconds <= 3000, "Tasks should complete within reasonable time using async queue");
        
        rateLimiter.Dispose();
    }

    [Fact]
    public async Task ConcurrentAcquireAsync_ShouldHandleHighConcurrency()
    {
        // Arrange
        _output.WriteLine("⚡ Testing concurrent operations under high load with lag-based limiting");
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 100.0,
            burstCapacity: 50.0,
            consumerGroup: "test-group");
        
        var concurrentOperations = 100;
        var successCount = 0;
        var completedCount = 0;
        
        // Act
        var stopwatch = Stopwatch.StartNew();
        var tasks = new Task[concurrentOperations];
        
        for (int i = 0; i < concurrentOperations; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                try
                {
                    if (rateLimiter.TryAcquire())
                    {
                        Interlocked.Increment(ref successCount);
                    }
                }
                finally
                {
                    Interlocked.Increment(ref completedCount);
                }
            });
        }
        
        await Task.WhenAll(tasks);
        stopwatch.Stop();
        
        // Assert
        _output.WriteLine($"📊 Concurrent operations: {concurrentOperations}");
        _output.WriteLine($"📊 Completed operations: {completedCount}");
        _output.WriteLine($"📊 Successful operations: {successCount}");
        _output.WriteLine($"📊 Success rate: {(double)successCount / concurrentOperations:P1}");
        _output.WriteLine($"📊 Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"📊 Final utilization: {rateLimiter.CurrentUtilization:P1}");
        
        Assert.Equal(concurrentOperations, completedCount);
        Assert.True(successCount > 0, "At least some operations should succeed");
        Assert.True(successCount <= 50, "Should not exceed burst capacity");
        Assert.True(stopwatch.ElapsedMilliseconds < 1000, "Should complete quickly with async patterns");
        
        rateLimiter.Dispose();
    }

    [Fact]
    public async Task AcquireAsync_ShouldRespectCancellation()
    {
        // Arrange
        _output.WriteLine("🛑 Testing cancellation token support in AcquireAsync");
        var rateLimiter = RateLimiterFactory.CreateLagBasedBucket(
            rateLimit: 1.0,
            burstCapacity: 1.0,
            consumerGroup: "test-group");
        
        // Consume initial token
        rateLimiter.TryAcquire(1);
        
        using var cts = new CancellationTokenSource();
        
        // Act & Assert
        var task = rateLimiter.AcquireAsync(1, cts.Token);
        
        // Cancel after 100ms
        cts.CancelAfter(100);
        
        await Assert.ThrowsAsync<TaskCanceledException>(() => task);
        _output.WriteLine("✅ Cancellation token was properly respected");
        
        rateLimiter.Dispose();
    }
}

/// <summary>
/// Mock implementation of Kafka consumer lag monitor for testing.
/// </summary>
public class MockKafkaConsumerLagMonitor : IKafkaConsumerLagMonitor
{
    private readonly Dictionary<string, TimeSpan> _lagData = new();

    public void SetLag(string consumerGroup, TimeSpan lag)
    {
        _lagData[consumerGroup] = lag;
    }

    public TimeSpan GetCurrentLag(string consumerGroup)
    {
        return _lagData.GetValueOrDefault(consumerGroup, TimeSpan.Zero);
    }

    public Task<TimeSpan> GetCurrentLagAsync(string consumerGroup)
    {
        return Task.FromResult(GetCurrentLag(consumerGroup));
    }

    public void Dispose()
    {
        _lagData.Clear();
    }
}