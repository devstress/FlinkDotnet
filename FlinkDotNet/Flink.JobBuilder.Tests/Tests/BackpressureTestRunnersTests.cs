using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class BackpressureTestRunnersTests
{
    [Test]
    public void StartConsumerLagTests_ReturnsTrue()
    {
        // Act
        var result = BackpressureTestRunner.StartConsumerLagTests();

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void StartConsumerLagTests_CanBeCalledMultipleTimes()
    {
        // Act
        var result1 = BackpressureTestRunner.StartConsumerLagTests();
        var result2 = BackpressureTestRunner.StartConsumerLagTests();

        // Assert
        Assert.That(result1, Is.True);
        Assert.That(result2, Is.True);
    }

    [Test]
    public void StartConsumerLagTests_ConsistentBehavior()
    {
        // Arrange
        const int iterations = 5;

        // Act & Assert
        for (int i = 0; i < iterations; i++)
        {
            var result = BackpressureTestRunner.StartConsumerLagTests();
            Assert.That(result, Is.True, $"Iteration {i + 1} should return true");
        }
    }

    [Test]
    public void BackpressureTestRunner_IsStaticClass()
    {
        // Arrange & Act
        var type = typeof(BackpressureTestRunner);

        // Assert
        Assert.That(type.IsAbstract, Is.True);
        Assert.That(type.IsSealed, Is.True);
    }

    [Test]
    public void StartConsumerLagTests_ThreadSafe()
    {
        // Arrange
        var tasks = new System.Threading.Tasks.Task<bool>[10];

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() => BackpressureTestRunner.StartConsumerLagTests());
        }

        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.That(task.Result, Is.True);
        }
    }
}
