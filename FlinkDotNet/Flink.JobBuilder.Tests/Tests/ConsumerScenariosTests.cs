using Flink.JobBuilder.Backpressure;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ConsumerScenariosTests
{
    private ConsumerScenarioExecutor _executor = null!;

    [SetUp]
    public void SetUp()
    {
        _executor = new ConsumerScenarioExecutor();
    }

    [Test]
    public void ExecuteScenario_WithValidScenario_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Basic Consumer Test",
            ConsumerCount = 3,
            ProcessingRate = 1000,
            ExpectedBehavior = "Balanced Load"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithHighConsumerCount_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "High Consumer Count Test",
            ConsumerCount = 100,
            ProcessingRate = 5000,
            ExpectedBehavior = "Efficient Distribution"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithLowProcessingRate_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Low Processing Rate Test",
            ConsumerCount = 5,
            ProcessingRate = 100,
            ExpectedBehavior = "Slow Processing"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithCustomPartitionManager_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Custom Partition Manager Test",
            ConsumerCount = 10,
            ProcessingRate = 2000,
            ExpectedBehavior = "Custom Partitioning",
            PartitionManager = new ConsistentHashPartitionManager()
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithCustomFairDistributor_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Custom Fair Distributor Test",
            ConsumerCount = 8,
            ProcessingRate = 3000,
            ExpectedBehavior = "Fair Distribution",
            FairDistributor = new FairPartitionDistributor()
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_MultipleInvocations_ReturnsConsistentResults()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Consistency Test",
            ConsumerCount = 5,
            ProcessingRate = 1500,
            ExpectedBehavior = "Consistent Results"
        };

        // Act
        var result1 = _executor.ExecuteScenario(scenario);
        var result2 = _executor.ExecuteScenario(scenario);
        var result3 = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result1.Success, Is.EqualTo(result2.Success));
        Assert.That(result2.Success, Is.EqualTo(result3.Success));
    }

    [Test]
    public void ConsumerScenario_CanSetName()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario
        {
            Name = "Test Scenario"
        };

        // Assert
        Assert.That(scenario.Name, Is.EqualTo("Test Scenario"));
    }

    [Test]
    public void ConsumerScenario_CanSetConsumerCount()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario
        {
            ConsumerCount = 42
        };

        // Assert
        Assert.That(scenario.ConsumerCount, Is.EqualTo(42));
    }

    [Test]
    public void ConsumerScenario_CanSetProcessingRate()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario
        {
            ProcessingRate = 9999
        };

        // Assert
        Assert.That(scenario.ProcessingRate, Is.EqualTo(9999));
    }

    [Test]
    public void ConsumerScenario_CanSetExpectedBehavior()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario
        {
            ExpectedBehavior = "Custom Behavior"
        };

        // Assert
        Assert.That(scenario.ExpectedBehavior, Is.EqualTo("Custom Behavior"));
    }

    [Test]
    public void ConsumerScenario_DefaultValues_AreInitialized()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario();

        // Assert
        Assert.That(scenario.Name, Is.EqualTo(string.Empty));
        Assert.That(scenario.ConsumerCount, Is.EqualTo(0));
        Assert.That(scenario.ProcessingRate, Is.EqualTo(0));
        Assert.That(scenario.ExpectedBehavior, Is.EqualTo(string.Empty));
        Assert.That(scenario.PartitionManager, Is.Not.Null);
        Assert.That(scenario.FairDistributor, Is.Not.Null);
    }

    [Test]
    public void ConsumerScenario_WithAllPropertiesSet_IsValid()
    {
        // Arrange & Act
        var scenario = new ConsumerScenario
        {
            Name = "Full Scenario",
            ConsumerCount = 20,
            ProcessingRate = 5000,
            ExpectedBehavior = "High Throughput",
            PartitionManager = new ConsistentHashPartitionManager(),
            FairDistributor = new FairPartitionDistributor()
        };

        // Assert
        Assert.That(scenario.Name, Is.EqualTo("Full Scenario"));
        Assert.That(scenario.ConsumerCount, Is.EqualTo(20));
        Assert.That(scenario.ProcessingRate, Is.EqualTo(5000));
        Assert.That(scenario.ExpectedBehavior, Is.EqualTo("High Throughput"));
        Assert.That(scenario.PartitionManager, Is.Not.Null);
        Assert.That(scenario.FairDistributor, Is.Not.Null);
    }

    [Test]
    public void ConsumerScenarioResult_CanSetSuccess()
    {
        // Arrange & Act
        var result = new ConsumerScenarioResult
        {
            Success = true
        };

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ConsumerScenarioResult_DefaultValue_IsFalse()
    {
        // Arrange & Act
        var result = new ConsumerScenarioResult();

        // Assert
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public void ExecuteScenario_WithZeroConsumers_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Zero Consumers Test",
            ConsumerCount = 0,
            ProcessingRate = 1000,
            ExpectedBehavior = "No Consumers"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithZeroProcessingRate_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Zero Processing Rate Test",
            ConsumerCount = 5,
            ProcessingRate = 0,
            ExpectedBehavior = "No Processing"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithEmptyName_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "",
            ConsumerCount = 3,
            ProcessingRate = 1000,
            ExpectedBehavior = "Anonymous Scenario"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ConsumerScenarioExecutor_CanBeInstantiatedMultipleTimes()
    {
        // Arrange & Act
        var executor1 = new ConsumerScenarioExecutor();
        var executor2 = new ConsumerScenarioExecutor();
        var executor3 = new ConsumerScenarioExecutor();

        // Assert
        Assert.That(executor1, Is.Not.Null);
        Assert.That(executor2, Is.Not.Null);
        Assert.That(executor3, Is.Not.Null);
        Assert.That(executor1, Is.Not.SameAs(executor2));
    }

    [Test]
    public void ExecuteScenario_ParallelExecution_WorksCorrectly()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Parallel Test",
            ConsumerCount = 5,
            ProcessingRate = 1000,
            ExpectedBehavior = "Thread Safe"
        };

        var tasks = new System.Threading.Tasks.Task<ConsumerScenarioResult>[10];

        // Act
        for (int i = 0; i < tasks.Length; i++)
        {
            tasks[i] = System.Threading.Tasks.Task.Run(() => _executor.ExecuteScenario(scenario));
        }

        System.Threading.Tasks.Task.WaitAll(tasks);

        // Assert
        foreach (var task in tasks)
        {
            Assert.That(task.Result.Success, Is.True);
        }
    }

    [Test]
    public void ExecuteScenario_WithVeryHighConsumerCount_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Very High Consumer Count Test",
            ConsumerCount = 10000,
            ProcessingRate = 50000,
            ExpectedBehavior = "Massive Scale"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecuteScenario_WithComplexExpectedBehavior_ReturnsSuccess()
    {
        // Arrange
        var scenario = new ConsumerScenario
        {
            Name = "Complex Behavior Test",
            ConsumerCount = 15,
            ProcessingRate = 3000,
            ExpectedBehavior = "Balanced load with automatic rebalancing and fair partition distribution"
        };

        // Act
        var result = _executor.ExecuteScenario(scenario);

        // Assert
        Assert.That(result.Success, Is.True);
    }
}
