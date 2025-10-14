
using FlinkDotNet.Orchestration.Models;
using FlinkDotNet.Orchestration.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace FlinkDotNet.Orchestration.Tests.Services;
/// <summary>
/// Comprehensive tests for FlinkOrchestra service.
/// Tests orchestration logic, cluster management, job submission, and error handling.
/// </summary>
[TestFixture]
public class FlinkOrchestraTests
{
    private Mock<ILogger<FlinkOrchestra>>? _mockLogger;
    private FlinkOrchestra? _orchestra;

    [SetUp]
    public void SetUp()
    {
        _mockLogger = new Mock<ILogger<FlinkOrchestra>>();
        _orchestra = new FlinkOrchestra(_mockLogger.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidLogger_InitializesSuccessfully()
    {
        // Arrange
        var mockLogger = new Mock<ILogger<FlinkOrchestra>>();

        // Act
        var orchestra = new FlinkOrchestra(mockLogger.Object);

        // Assert
        Assert.That(orchestra, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FlinkOrchestra(null!));
    }

    #endregion

    #region GetAvailableClustersAsync Tests

    [Test]
    public async Task GetAvailableClustersAsync_WithNoClusters_ReturnsEmptyArray()
    {
        // Act
        var clusters = await _orchestra!.GetAvailableClustersAsync();

        // Assert
        Assert.That(clusters, Is.Not.Null);
        Assert.That(clusters, Is.Empty);
    }

    #endregion

    #region SubmitJobAsync Tests - No Clusters Available

    [Test]
    public async Task SubmitJobAsync_WithNoClusters_ReturnsFailureResult()
    {
        // Arrange
        var job = new FlinkJobDefinition
        {
            JobId = "test-job",
            JobName = "Test Job",
            Parallelism = 4
        };

        // Act
        var result = await _orchestra!.SubmitJobAsync(job, SubmissionStrategy.BestFit);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.False);
            Assert.That(result.JobId, Is.EqualTo("test-job"));
            Assert.That(result.ErrorMessage, Is.Not.Null);
            Assert.That(result.ErrorMessage, Does.Contain("No suitable cluster"));
        });
    }

    [Test]
    public async Task SubmitJobAsync_WithAllStrategies_HandlesNoClusterGracefully()
    {
        // Arrange
        var job = new FlinkJobDefinition
        {
            JobId = "test-job",
            Parallelism = 1
        };

        var strategies = Enum.GetValues<SubmissionStrategy>();

        // Act & Assert
        foreach (var strategy in strategies)
        {
            var result = await _orchestra!.SubmitJobAsync(job, strategy);
            Assert.That(result.Success, Is.False, $"Strategy {strategy} should fail with no clusters");
        }
    }

    #endregion

    #region GetClusterHealthAsync Tests

    [Test]
    public async Task GetClusterHealthAsync_WithNoClusters_ReturnsZeroHealthScore()
    {
        // Act
        var health = await _orchestra!.GetClusterHealthAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(health, Is.Not.Null);
            Assert.That(health.TotalClusters, Is.EqualTo(0));
            Assert.That(health.HealthyClusters, Is.EqualTo(0));
            Assert.That(health.WarningClusters, Is.EqualTo(0));
            Assert.That(health.CriticalClusters, Is.EqualTo(0));
            Assert.That(health.OfflineClusters, Is.EqualTo(0));
            Assert.That(health.TotalAvailableSlots, Is.EqualTo(0));
            Assert.That(health.TotalRunningJobs, Is.EqualTo(0));
            Assert.That(health.OverallHealthScore, Is.EqualTo(0.0));
            Assert.That(health.Issues, Is.Empty);
        });
    }

    #endregion

    #region ScaleOrchestraAsync Tests

    [Test]
    public async Task ScaleOrchestraAsync_ToZero_WithNoClusters_SucceedsWithNoChanges()
    {
        // Act
        var result = await _orchestra!.ScaleOrchestraAsync(0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Success, Is.True);
            Assert.That(result.PreviousCapacity, Is.EqualTo(0));
            Assert.That(result.NewCapacity, Is.EqualTo(0));
            Assert.That(result.ClustersAdded, Is.EqualTo(0));
            Assert.That(result.ClustersRemoved, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ScaleOrchestraAsync_ToPositiveNumber_AttemptsToScaleUp()
    {
        // Note: This will attempt to provision clusters but may fail due to mock limitations
        // The test validates that the scaling logic is invoked
        // Act
        var result = await _orchestra!.ScaleOrchestraAsync(2);

        // Assert - scaling attempt is made (may succeed or fail)
        Assert.That(result, Is.Not.Null);
    }

    #endregion

    #region StartOrchestrationWorkflowAsync Tests

    [Test]
    public async Task StartOrchestrationWorkflowAsync_WithValidRequest_ReturnsWorkflowId()
    {
        // Arrange
        var request = new OrchestrationRequest
        {
            RequestId = "req-123",
            TargetClusters = 3
        };

        // Act
        var workflowId = await _orchestra!.StartOrchestrationWorkflowAsync(request);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(workflowId, Is.Not.Null);
            Assert.That(workflowId, Does.Contain("orchestra-"));
            Assert.That(workflowId, Does.Contain("req-123"));
        });
    }

    [Test]
    public async Task StartOrchestrationWorkflowAsync_WithDifferentRequests_ReturnsUniqueIds()
    {
        // Arrange
        var request1 = new OrchestrationRequest { RequestId = "req-1" };
        var request2 = new OrchestrationRequest { RequestId = "req-2" };

        // Act
        var workflowId1 = await _orchestra!.StartOrchestrationWorkflowAsync(request1);
        await Task.Delay(1); // Minimal delay to ensure unique workflow IDs
        var workflowId2 = await _orchestra!.StartOrchestrationWorkflowAsync(request2);

        // Assert
        Assert.That(workflowId1, Is.Not.EqualTo(workflowId2));
    }

    #endregion

    #region Logging Tests

    [Test]
    public async Task SubmitJobAsync_LogsJobSubmission()
    {
        // Arrange
        var job = new FlinkJobDefinition
        {
            JobId = "log-test-job",
            Parallelism = 1
        };

        // Act
        await _orchestra!.SubmitJobAsync(job, SubmissionStrategy.BestFit);

        // Assert - verify logging was called
        _mockLogger!.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("log-test-job")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task StartOrchestrationWorkflowAsync_LogsWorkflowStart()
    {
        // Arrange
        var request = new OrchestrationRequest { RequestId = "log-req" };

        // Act
        await _orchestra!.StartOrchestrationWorkflowAsync(request);

        // Assert - verify logging was called
        _mockLogger!.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("log-req")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Integration Tests with Mock Clusters

    [Test]
    public async Task ProvisionClusterAsync_WithValidConfig_CreatesCluster()
    {
        // Arrange
        var config = new ClusterConfiguration
        {
            Name = "test-cluster",
            TaskSlots = 4,
            TaskManagers = 2
        };

        // Act
        var cluster = await _orchestra!.ProvisionClusterAsync(config);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cluster, Is.Not.Null);
            Assert.That(cluster.ClusterId, Is.Not.Null);
            Assert.That(cluster.ClusterId, Is.Not.Empty);
        });
    }

    [Test]
    public async Task ProvisionClusterAsync_ThenGetAvailableClusters_ReturnsProvisionedCluster()
    {
        // Arrange
        var config = new ClusterConfiguration
        {
            Name = "integration-test-cluster",
            TaskSlots = 4,
            TaskManagers = 2
        };

        // Act
        var cluster = await _orchestra!.ProvisionClusterAsync(config);
        var availableClusters = await _orchestra!.GetAvailableClustersAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(availableClusters, Is.Not.Empty);
            Assert.That(availableClusters.Length, Is.EqualTo(1));
            Assert.That(availableClusters[0].ClusterId, Is.EqualTo(cluster.ClusterId));
        });
    }

    [Test]
    public async Task ScaleOrchestraAsync_ScaleUp_IncreasesClusterCount()
    {
        // Arrange - start with 0 clusters
        var initialClusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(initialClusters, Is.Empty);

        // Act - scale up to 2 clusters
        var result = await _orchestra!.ScaleOrchestraAsync(2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PreviousCapacity, Is.EqualTo(0));
            Assert.That(result.NewCapacity, Is.EqualTo(2));
            Assert.That(result.ClustersAdded, Is.EqualTo(2));
        });

        // Verify clusters are available
        var finalClusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(finalClusters.Length, Is.EqualTo(2));
    }

    [Test]
    public async Task ScaleOrchestraAsync_ScaleDown_DecreasesClusterCount()
    {
        // Arrange - scale up first
        await _orchestra!.ScaleOrchestraAsync(3);

        // Act - scale down to 1
        var result = await _orchestra!.ScaleOrchestraAsync(1);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PreviousCapacity, Is.EqualTo(3));
            Assert.That(result.NewCapacity, Is.EqualTo(1));
            Assert.That(result.ClustersRemoved, Is.EqualTo(2));
        });

        // Verify only 1 cluster remains
        var finalClusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(finalClusters.Length, Is.EqualTo(1));
    }

    [Test]
    public async Task GetClusterHealthAsync_WithProvisionedClusters_ReturnsHealthMetrics()
    {
        // Arrange - provision some clusters
        await _orchestra!.ScaleOrchestraAsync(2);

        // Act
        var health = await _orchestra!.GetClusterHealthAsync();

        // Assert - verify we get health report for the clusters
        Assert.Multiple(() =>
        {
            Assert.That(health.TotalClusters, Is.EqualTo(2));
            Assert.That(health.OverallHealthScore, Is.GreaterThanOrEqualTo(0.0));
        });
    }

    #endregion

    #region Edge Cases and Error Handling

    [Test]
    public async Task SubmitJobAsync_WithHighParallelism_HandlesResourceConstraints()
    {
        // Arrange
        await _orchestra!.ScaleOrchestraAsync(1); // Single small cluster

        var job = new FlinkJobDefinition
        {
            JobId = "high-parallelism-job",
            Parallelism = 1000 // Very high parallelism
        };

        // Act
        var result = await _orchestra!.SubmitJobAsync(job, SubmissionStrategy.BestFit);

        // Assert - should fail due to insufficient resources
        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task ScaleOrchestraAsync_ToNegativeCapacity_HandlesGracefully()
    {
        // Act
        var result = await _orchestra!.ScaleOrchestraAsync(-1);

        // Assert - should handle negative numbers gracefully
        Assert.That(result, Is.Not.Null);
    }

    [Test]
    public async Task ScaleOrchestraAsync_ToSameCapacity_ReturnsNoChanges()
    {
        // Arrange
        await _orchestra!.ScaleOrchestraAsync(3);

        // Act - scale to same capacity
        var result = await _orchestra!.ScaleOrchestraAsync(3);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ClustersAdded, Is.EqualTo(0));
            Assert.That(result.ClustersRemoved, Is.EqualTo(0));
        });
    }

    [Test]
    public async Task ProvisionClusterAsync_MultipleCalls_CreatesMultipleClusters()
    {
        // Arrange
        var config1 = new ClusterConfiguration { Name = "cluster-1" };
        var config2 = new ClusterConfiguration { Name = "cluster-2" };

        // Act
        var cluster1 = await _orchestra!.ProvisionClusterAsync(config1);
        var cluster2 = await _orchestra!.ProvisionClusterAsync(config2);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(cluster1.ClusterId, Is.Not.EqualTo(cluster2.ClusterId));
        });

        var clusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(clusters.Length, Is.EqualTo(2));
    }

    #endregion

    #region Concurrent Operations Tests

    [Test]
    public async Task GetAvailableClustersAsync_ConcurrentCalls_ReturnConsistentResults()
    {
        // Arrange
        await _orchestra!.ScaleOrchestraAsync(3);

        // Act - concurrent calls
        var task1 = _orchestra!.GetAvailableClustersAsync();
        var task2 = _orchestra!.GetAvailableClustersAsync();
        var task3 = _orchestra!.GetAvailableClustersAsync();

        await Task.WhenAll(task1, task2, task3);

        // Assert - all should return same count
        Assert.Multiple(() =>
        {
            Assert.That(task1.Result.Length, Is.EqualTo(3));
            Assert.That(task2.Result.Length, Is.EqualTo(3));
            Assert.That(task3.Result.Length, Is.EqualTo(3));
        });
    }

    [Test]
    public async Task StartOrchestrationWorkflowAsync_ConcurrentCalls_ReturnUniqueIds()
    {
        // Arrange
        var requests = Enumerable.Range(1, 5)
            .Select(i => new OrchestrationRequest { RequestId = $"req-{i}" })
            .ToList();

        // Act - concurrent calls
        var tasks = requests.Select(r => _orchestra!.StartOrchestrationWorkflowAsync(r)).ToList();
        var workflowIds = await Task.WhenAll(tasks);

        // Assert - all IDs should be unique
        Assert.That(workflowIds.Distinct().Count(), Is.EqualTo(5));
    }

    #endregion

    #region Performance and Timeout Tests

    [Test]
    public async Task GetAvailableClustersAsync_WithMultipleClusters_CompletesQuickly()
    {
        // Arrange
        await _orchestra!.ScaleOrchestraAsync(5);

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _orchestra!.GetAvailableClustersAsync();
        stopwatch.Stop();

        // Assert - should complete reasonably fast (within 5 seconds)
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(5000));
    }

    [Test]
    public async Task StartOrchestrationWorkflowAsync_CompletesWithinTimeout()
    {
        // Arrange
        var request = new OrchestrationRequest
        {
            RequestId = "perf-test",
            TargetClusters = 3
        };

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        await _orchestra!.StartOrchestrationWorkflowAsync(request);
        stopwatch.Stop();

        // Assert - should complete quickly (within 1 second)
        Assert.That(stopwatch.ElapsedMilliseconds, Is.LessThan(1000));
    }

    #endregion

    #region Cleanup Tests

    [Test]
    public async Task ScaleOrchestraAsync_ToZero_RemovesAllClusters()
    {
        // Arrange
        await _orchestra!.ScaleOrchestraAsync(5);
        var initialClusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(initialClusters.Length, Is.EqualTo(5));

        // Act - scale down to zero
        var result = await _orchestra!.ScaleOrchestraAsync(0);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.ClustersRemoved, Is.EqualTo(5));
        });

        var finalClusters = await _orchestra!.GetAvailableClustersAsync();
        Assert.That(finalClusters, Is.Empty);
    }

    #endregion
}
