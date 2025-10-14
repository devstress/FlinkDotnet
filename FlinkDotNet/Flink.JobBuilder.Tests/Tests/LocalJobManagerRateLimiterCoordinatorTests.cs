using Flink.JobBuilder.Backpressure;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class LocalJobManagerRateLimiterCoordinatorTests
{
    private LocalJobManagerRateLimiterCoordinator _coordinator = null!;

    [SetUp]
    public void SetUp()
    {
        _coordinator = new LocalJobManagerRateLimiterCoordinator();
    }

    [TearDown]
    public void TearDown()
    {
        _coordinator?.Dispose();
    }

    [Test]
    public void CoordinateRateLimitAsync_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-1";
        var newRateLimit = 100.0;

        // Act & Assert - Should complete without errors
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.CoordinateRateLimitAsync(rateLimiterId, newRateLimit));
    }

    [Test]
    public void CoordinateRateLimitAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-2";
        var newRateLimit = 200.0;
        using var cts = new System.Threading.CancellationTokenSource();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.CoordinateRateLimitAsync(rateLimiterId, newRateLimit, cts.Token));
    }

    [Test]
    public async Task RegisterRateLimiterAsync_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-3";
        var callbackInvoked = false;
        Action<double> callback = (newLimit) => { callbackInvoked = true; };

        // Act
        await _coordinator.RegisterRateLimiterAsync(rateLimiterId, callback);

        // Assert - Local coordinator doesn't actually invoke callbacks
        // but registration should complete without errors
        Assert.That(callbackInvoked, Is.False);
    }

    [Test]
    public void RegisterRateLimiterAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-4";
        using var cts = new System.Threading.CancellationTokenSource();
        Action<double> callback = (newLimit) => { };

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.RegisterRateLimiterAsync(rateLimiterId, callback, cts.Token));
    }

    [Test]
    public void UnregisterRateLimiterAsync_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-5";

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.UnregisterRateLimiterAsync(rateLimiterId));
    }

    [Test]
    public void UnregisterRateLimiterAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-6";
        using var cts = new System.Threading.CancellationTokenSource();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.UnregisterRateLimiterAsync(rateLimiterId, cts.Token));
    }

    [Test]
    public async Task GetCoordinatedRateLimitAsync_ReturnsMaxValue()
    {
        // Arrange
        var rateLimiterId = "test-limiter-7";

        // Act
        var rateLimit = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId);

        // Assert - Local coordinator returns double.MaxValue
        Assert.That(rateLimit, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public async Task GetCoordinatedRateLimitAsync_WithCancellationToken_ReturnsMaxValue()
    {
        // Arrange
        var rateLimiterId = "test-limiter-8";
        using var cts = new System.Threading.CancellationTokenSource();

        // Act
        var rateLimit = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId, cts.Token);

        // Assert
        Assert.That(rateLimit, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public void ReportUtilizationAsync_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-9";
        var utilization = 0.75;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.ReportUtilizationAsync(rateLimiterId, utilization));
    }

    [Test]
    public void ReportUtilizationAsync_WithCancellationToken_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-10";
        var utilization = 0.85;
        using var cts = new System.Threading.CancellationTokenSource();

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.ReportUtilizationAsync(rateLimiterId, utilization, cts.Token));
    }

    [Test]
    public void ReportUtilizationAsync_WithDifferentUtilizationValues_CompletesSuccessfully()
    {
        // Arrange
        var rateLimiterId = "test-limiter-11";

        // Act & Assert - Test various utilization values
        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.ReportUtilizationAsync(rateLimiterId, 0.0));

        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.ReportUtilizationAsync(rateLimiterId, 0.5));

        Assert.DoesNotThrowAsync(async () =>
            await _coordinator.ReportUtilizationAsync(rateLimiterId, 1.0));
    }

    [Test]
    public void Dispose_CompletesSuccessfully()
    {
        // Act & Assert
        Assert.DoesNotThrow(() => _coordinator.Dispose());
    }

    [Test]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        // Act & Assert
        _coordinator.Dispose();
        Assert.DoesNotThrow(() => _coordinator.Dispose());
        Assert.DoesNotThrow(() => _coordinator.Dispose());
    }

    [Test]
    public async Task AllMethods_WorkAfterMultipleOperations()
    {
        // Arrange
        var rateLimiterId = "test-limiter-12";
        Action<double> callback = (newLimit) => { };

        // Act - Perform multiple operations in sequence
        await _coordinator.RegisterRateLimiterAsync(rateLimiterId, callback);
        await _coordinator.CoordinateRateLimitAsync(rateLimiterId, 100.0);
        await _coordinator.ReportUtilizationAsync(rateLimiterId, 0.5);
        var rateLimit = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId);
        await _coordinator.UnregisterRateLimiterAsync(rateLimiterId);

        // Assert
        Assert.That(rateLimit, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public void CoordinateRateLimitAsync_WithMultipleLimiters_WorksIndependently()
    {
        // Arrange
        var limiter1 = "limiter-1";
        var limiter2 = "limiter-2";
        var limiter3 = "limiter-3";

        // Act & Assert - Should handle multiple rate limiters independently
        Assert.DoesNotThrowAsync(async () =>
        {
            await _coordinator.CoordinateRateLimitAsync(limiter1, 50.0);
            await _coordinator.CoordinateRateLimitAsync(limiter2, 100.0);
            await _coordinator.CoordinateRateLimitAsync(limiter3, 150.0);
        });
    }

    [Test]
    public async Task GetCoordinatedRateLimitAsync_ConsistentlyReturnsMaxValue()
    {
        // Arrange
        var rateLimiterId = "test-limiter-13";

        // Act - Call multiple times
        var rateLimit1 = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId);
        var rateLimit2 = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId);
        var rateLimit3 = await _coordinator.GetCoordinatedRateLimitAsync(rateLimiterId);

        // Assert - Should always return the same value
        Assert.That(rateLimit1, Is.EqualTo(double.MaxValue));
        Assert.That(rateLimit2, Is.EqualTo(double.MaxValue));
        Assert.That(rateLimit3, Is.EqualTo(double.MaxValue));
    }

    [Test]
    public void Constructor_CreatesValidInstance()
    {
        // Act
        using var coordinator = new LocalJobManagerRateLimiterCoordinator();

        // Assert
        Assert.That(coordinator, Is.Not.Null);
        Assert.That(coordinator, Is.InstanceOf<IJobManagerRateLimiterCoordinator>());
    }

    [Test]
    public void ImplementsIJobManagerRateLimiterCoordinator()
    {
        // Assert
        Assert.That(_coordinator, Is.InstanceOf<IJobManagerRateLimiterCoordinator>());
    }

    [Test]
    public void ImplementsIDisposable()
    {
        // Assert
        Assert.That(_coordinator, Is.InstanceOf<IDisposable>());
    }
}
