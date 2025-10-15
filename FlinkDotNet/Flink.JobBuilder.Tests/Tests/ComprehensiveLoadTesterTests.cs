using Flink.JobBuilder.Backpressure;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ComprehensiveLoadTesterTests
{
    private ComprehensiveLoadTester _tester = null!;

    [SetUp]
    public void SetUp()
    {
        _tester = new ComprehensiveLoadTester();
    }

    [Test]
    public void ExecutePhase_WithValidExecution_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            Phase = new LoadTestPhase
            {
                Phase = "Ramp-up",
                Duration = "5m",
                MessageRate = "1000/s",
                FailureInjection = "None",
                SuccessCriteria = "Latency < 100ms"
            }
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_ReturnsOptimalRebalancingPerformance()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution();

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.RebalancingPerformance, Is.EqualTo("Optimal"));
    }

    [Test]
    public void ExecutePhase_ReturnsNoisyNeighborEffectivenessTrue()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution();

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.NoisyNeighborEffectiveness, Is.True);
    }

    [Test]
    public void ExecutePhase_ReturnsRateLimitingEffectivenessTrue()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution();

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.RateLimitingEffectiveness, Is.True);
    }

    [Test]
    public void ExecutePhase_ReturnsFairDistributionMaintainedTrue()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution();

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.FairDistributionMaintained, Is.True);
    }

    [Test]
    public void ExecutePhase_WithCustomPartitionManager_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            PartitionManager = new ConsistentHashPartitionManager()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_WithCustomNoisyNeighborManager_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            NoisyNeighborManager = new NoisyNeighborManager()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_WithCustomRateLimiter_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            RateLimiter = new MultiTierRateLimiter()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_WithCustomFairDistributor_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            FairDistributor = new FairPartitionDistributor()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_WithAllComponentsCustomized_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            Phase = new LoadTestPhase
            {
                Phase = "Steady-State",
                Duration = "10m",
                MessageRate = "5000/s",
                FailureInjection = "Random Partition Failure",
                SuccessCriteria = "No data loss"
            },
            PartitionManager = new ConsistentHashPartitionManager(),
            NoisyNeighborManager = new NoisyNeighborManager(),
            RateLimiter = new MultiTierRateLimiter(),
            FairDistributor = new FairPartitionDistributor()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.RebalancingPerformance, Is.EqualTo("Optimal"));
        Assert.That(result.NoisyNeighborEffectiveness, Is.True);
        Assert.That(result.RateLimitingEffectiveness, Is.True);
        Assert.That(result.FairDistributionMaintained, Is.True);
    }

    [Test]
    public void ExecutePhase_MultipleInvocations_ReturnsConsistentResults()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution();

        // Act
        var result1 = _tester.ExecutePhase(execution);
        var result2 = _tester.ExecutePhase(execution);
        var result3 = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result1.Success, Is.EqualTo(result2.Success));
        Assert.That(result2.Success, Is.EqualTo(result3.Success));
        Assert.That(result1.RebalancingPerformance, Is.EqualTo(result2.RebalancingPerformance));
    }

    [Test]
    public void LoadTestPhaseExecution_DefaultValues_AreInitialized()
    {
        // Act
        var execution = new LoadTestPhaseExecution();

        // Assert
        Assert.That(execution.Phase, Is.Not.Null);
        Assert.That(execution.PartitionManager, Is.Not.Null);
        Assert.That(execution.NoisyNeighborManager, Is.Not.Null);
        Assert.That(execution.RateLimiter, Is.Not.Null);
        Assert.That(execution.FairDistributor, Is.Not.Null);
    }

    [Test]
    public void LoadTestPhase_CanSetAllProperties()
    {
        // Arrange & Act
        var phase = new LoadTestPhase
        {
            Phase = "Peak Load",
            Duration = "15m",
            MessageRate = "10000/s",
            FailureInjection = "Network Partition",
            SuccessCriteria = "99.9% Success Rate"
        };

        // Assert
        Assert.That(phase.Phase, Is.EqualTo("Peak Load"));
        Assert.That(phase.Duration, Is.EqualTo("15m"));
        Assert.That(phase.MessageRate, Is.EqualTo("10000/s"));
        Assert.That(phase.FailureInjection, Is.EqualTo("Network Partition"));
        Assert.That(phase.SuccessCriteria, Is.EqualTo("99.9% Success Rate"));
    }

    [Test]
    public void LoadTestResult_CanSetAllProperties()
    {
        // Arrange & Act
        var result = new LoadTestResult
        {
            Success = true,
            RebalancingPerformance = "Excellent",
            NoisyNeighborEffectiveness = true,
            RateLimitingEffectiveness = true,
            FairDistributionMaintained = true
        };

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(result.RebalancingPerformance, Is.EqualTo("Excellent"));
        Assert.That(result.NoisyNeighborEffectiveness, Is.True);
        Assert.That(result.RateLimitingEffectiveness, Is.True);
        Assert.That(result.FairDistributionMaintained, Is.True);
    }

    [Test]
    public void ExecutePhase_WithHighLoadPhase_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            Phase = new LoadTestPhase
            {
                Phase = "High Load",
                Duration = "30m",
                MessageRate = "50000/s",
                FailureInjection = "Multiple Failures",
                SuccessCriteria = "System Remains Stable"
            }
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ExecutePhase_WithRampDownPhase_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            Phase = new LoadTestPhase
            {
                Phase = "Ramp-down",
                Duration = "5m",
                MessageRate = "Decreasing to 0",
                FailureInjection = "None",
                SuccessCriteria = "Graceful Shutdown"
            }
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void ComprehensiveLoadTester_CanBeInstantiatedMultipleTimes()
    {
        // Arrange & Act
        var tester1 = new ComprehensiveLoadTester();
        var tester2 = new ComprehensiveLoadTester();
        var tester3 = new ComprehensiveLoadTester();

        // Assert
        Assert.That(tester1, Is.Not.Null);
        Assert.That(tester2, Is.Not.Null);
        Assert.That(tester3, Is.Not.Null);
        Assert.That(tester1, Is.Not.SameAs(tester2));
    }

    [Test]
    public void ExecutePhase_WithEmptyPhaseInfo_ReturnsSuccess()
    {
        // Arrange
        var execution = new LoadTestPhaseExecution
        {
            Phase = new LoadTestPhase()
        };

        // Act
        var result = _tester.ExecutePhase(execution);

        // Assert
        Assert.That(result.Success, Is.True);
    }

    [Test]
    public void LoadTestPhase_DefaultValues_AreEmptyStrings()
    {
        // Arrange & Act
        var phase = new LoadTestPhase();

        // Assert
        Assert.That(phase.Phase, Is.EqualTo(string.Empty));
        Assert.That(phase.Duration, Is.EqualTo(string.Empty));
        Assert.That(phase.MessageRate, Is.EqualTo(string.Empty));
        Assert.That(phase.FailureInjection, Is.EqualTo(string.Empty));
        Assert.That(phase.SuccessCriteria, Is.EqualTo(string.Empty));
    }

    [Test]
    public void LoadTestResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new LoadTestResult();

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.RebalancingPerformance, Is.EqualTo(string.Empty));
        Assert.That(result.NoisyNeighborEffectiveness, Is.False);
        Assert.That(result.RateLimitingEffectiveness, Is.False);
        Assert.That(result.FairDistributionMaintained, Is.False);
    }
}
