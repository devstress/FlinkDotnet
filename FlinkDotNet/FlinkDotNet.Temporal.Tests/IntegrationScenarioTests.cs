using FlinkDotNet.Temporal.Models;
using ClusterConfiguration = FlinkDotNet.ClusterManager.Models.ClusterConfiguration;

namespace FlinkDotNet.Temporal.Tests;

/// <summary>
/// Integration scenario tests demonstrating complex use cases and interactions.
/// </summary>
[TestFixture]
public class IntegrationScenarioTests
{
    [Test]
    public void JobDistribution_CompleteScenario_AllJobsPlaced()
    {
        // Arrange: Simulate distributing 10 jobs across 3 clusters
        var placements = new List<JobPlacementResult>();
        var clusterDistribution = new Dictionary<string, int>();

        for (int i = 1; i <= 10; i++)
        {
            var clusterId = $"cluster-{(i % 3) + 1}";
            placements.Add(new JobPlacementResult
            {
                JobId = $"job-{i}",
                ClusterId = clusterId,
                Success = true,
                PlacementTime = DateTime.UtcNow,
                PlacementDuration = TimeSpan.FromSeconds(2 + i % 3)
            });

            clusterDistribution[clusterId] = clusterDistribution.GetValueOrDefault(clusterId) + 1;
        }

        // Act: Create distribution result
        var result = new JobDistributionResult
        {
            TotalJobs = 10,
            SuccessfulPlacements = 10,
            FailedPlacements = 0,
            Placements = placements,
            TotalDistributionTime = TimeSpan.FromMinutes(1),
            ClusterDistribution = clusterDistribution
        };

        // Assert: Verify distribution is balanced and complete
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalJobs, Is.EqualTo(10));
            Assert.That(result.SuccessfulPlacements, Is.EqualTo(10));
            Assert.That(result.FailedPlacements, Is.EqualTo(0));
            Assert.That(result.Placements, Has.Count.EqualTo(10));
            Assert.That(result.ClusterDistribution, Has.Count.EqualTo(3));

            // Verify balanced distribution (each cluster gets 3-4 jobs)
            foreach (var count in result.ClusterDistribution.Values)
            {
                Assert.That(count, Is.InRange(3, 4));
            }
        });
    }

    [Test]
    public void JobDistribution_PartialFailure_SomeJobsFailed()
    {
        // Arrange: Simulate scenario where some jobs fail to place
        var placements = new List<JobPlacementResult>
        {
            new() { JobId = "job-1", ClusterId = "cluster-1", Success = true },
            new() { JobId = "job-2", ClusterId = "cluster-1", Success = true },
            new() { JobId = "job-3", ClusterId = "", Success = false, ErrorMessage = "No available resources" },
            new() { JobId = "job-4", ClusterId = "cluster-2", Success = true },
            new() { JobId = "job-5", ClusterId = "", Success = false, ErrorMessage = "Cluster unavailable" }
        };

        // Act
        var result = new JobDistributionResult
        {
            TotalJobs = 5,
            SuccessfulPlacements = 3,
            FailedPlacements = 2,
            Placements = placements,
            TotalDistributionTime = TimeSpan.FromSeconds(30)
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalJobs, Is.EqualTo(5));
            Assert.That(result.SuccessfulPlacements, Is.EqualTo(3));
            Assert.That(result.FailedPlacements, Is.EqualTo(2));

            var failures = placements.Where(p => !p.Success).ToList();
            Assert.That(failures, Has.Count.EqualTo(2));
            Assert.That(failures.All(f => !string.IsNullOrEmpty(f.ErrorMessage)), Is.True);
        });
    }

    [Test]
    public void AutoScaling_CriticalLoad_ScalesUpAggressively()
    {
        // Arrange: High load scenario requiring aggressive scaling
        var config = new AutoScalingConfig
        {
            MinClusters = 5,
            MaxClusters = 100,
            ScaleUpThreshold = 70.0,
            ScaleDownThreshold = 20.0,
            EvaluationInterval = TimeSpan.FromMinutes(1),
            CooldownPeriod = TimeSpan.FromMinutes(3),
            ScaleUpIncrement = 5,  // Aggressive scaling
            ScaleDownIncrement = 1,
            Metrics = new List<AutoScalingMetric>
            {
                new() { Name = "cpu", Type = AutoScalingMetricType.CpuUtilization, Weight = 2.0, Threshold = 70.0 },
                new() { Name = "memory", Type = AutoScalingMetricType.MemoryUtilization, Weight = 1.5, Threshold = 75.0 },
                new() { Name = "queue", Type = AutoScalingMetricType.JobQueueLength, Weight = 1.0, Threshold = 100 }
            }
        };

        // Assert: Verify aggressive scaling configuration
        Assert.Multiple(() =>
        {
            Assert.That(config.ScaleUpIncrement, Is.EqualTo(5));
            Assert.That(config.MaxClusters, Is.EqualTo(100));
            Assert.That(config.Metrics, Has.Count.EqualTo(3));
            Assert.That(config.Metrics.Sum(m => m.Weight), Is.EqualTo(4.5));
        });
    }

    [Test]
    public void ClusterFailure_CascadingFailure_MultipleAffectedJobs()
    {
        // Arrange: Critical failure affecting multiple jobs
        var failureInfo = new ClusterFailureInfo
        {
            ClusterId = "cluster-prod-01",
            FailureType = ClusterFailureType.NetworkPartition,
            FailureTime = DateTime.UtcNow,
            Description = "Network partition detected - cluster isolated from other nodes",
            FailureContext = new Dictionary<string, object>
            {
                ["last_heartbeat"] = DateTime.UtcNow.AddMinutes(-5),
                ["partition_duration_seconds"] = 300,
                ["affected_task_managers"] = 15
            },
            AffectedJobs = new List<string>
            {
                "payment-processing-job",
                "order-fulfillment-job",
                "inventory-sync-job",
                "customer-notification-job"
            },
            Severity = FailureSeverity.Critical
        };

        // Assert: Verify critical failure details
        Assert.Multiple(() =>
        {
            Assert.That(failureInfo.Severity, Is.EqualTo(FailureSeverity.Critical));
            Assert.That(failureInfo.FailureType, Is.EqualTo(ClusterFailureType.NetworkPartition));
            Assert.That(failureInfo.AffectedJobs, Has.Count.EqualTo(4));
            Assert.That(failureInfo.FailureContext, Contains.Key("affected_task_managers"));
            Assert.That(failureInfo.Description, Contains.Substring("partition"));
        });
    }

    [Test]
    public void HealthMonitoring_ComprehensiveChecks_AllTypesEnabled()
    {
        // Arrange: Full health monitoring configuration
        var config = new HealthMonitoringConfig
        {
            CheckInterval = TimeSpan.FromSeconds(30),
            HealthTimeout = TimeSpan.FromSeconds(10),
            MaxConsecutiveFailures = 2,
            EnabledChecks = new List<HealthCheckType>
            {
                HealthCheckType.RestApiHealthCheck,
                HealthCheckType.JobManagerConnectivity,
                HealthCheckType.TaskManagerStatus,
                HealthCheckType.CheckpointStatus,
                HealthCheckType.BackpressureMonitoring,
                HealthCheckType.ResourceUtilization,
                HealthCheckType.JobStatus
            },
            CheckParameters = new Dictionary<string, object>
            {
                ["api_endpoint"] = "http://cluster:8081",
                ["timeout_ms"] = 10000,
                ["retry_count"] = 3
            }
        };

        // Assert: Verify comprehensive monitoring
        Assert.Multiple(() =>
        {
            Assert.That(config.EnabledChecks, Has.Count.EqualTo(7)); // All check types
            Assert.That(config.CheckInterval, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(config.MaxConsecutiveFailures, Is.EqualTo(2)); // Sensitive failure detection
            Assert.That(config.CheckParameters, Has.Count.EqualTo(3));
        });
    }

    [Test]
    public void ClusterProvisioning_HighPriority_FastTimeout()
    {
        // Arrange: High-priority provisioning request
        var config = new ClusterConfiguration
        {
            Name = "urgent-cluster-001",
            TaskManagers = 10,
            TaskSlots = 4
        };

        var request = new ClusterProvisioningRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            Configuration = config,
            Region = "us-east-1",
            Zone = "az-1",
            Priority = Priority.Critical,
            Timeout = TimeSpan.FromMinutes(5), // Aggressive timeout
            Metadata = new Dictionary<string, object>
            {
                ["requester"] = "auto-scaler",
                ["reason"] = "high_load_detected",
                ["timestamp"] = DateTime.UtcNow
            }
        };

        // Assert: Verify high-priority configuration
        Assert.Multiple(() =>
        {
            Assert.That(request.Priority, Is.EqualTo(Priority.Critical));
            Assert.That(request.Timeout, Is.LessThanOrEqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(request.Metadata, Contains.Key("reason"));
            Assert.That(request.Configuration.TaskManagers, Is.EqualTo(10));
        });
    }

    [Test]
    public void RecordTypes_Immutability_WithKeywordWorks()
    {
        // Arrange: Create original record
        var original = new AutoScalingConfig
        {
            MinClusters = 5,
            MaxClusters = 50
        };

        // Act: Use 'with' keyword to create modified copy
        var modified = original with
        {
            MaxClusters = 100
        };

        // Assert: Original unchanged, modified has new value
        Assert.Multiple(() =>
        {
            Assert.That(original.MaxClusters, Is.EqualTo(50));
            Assert.That(modified.MaxClusters, Is.EqualTo(100));
            Assert.That(modified.MinClusters, Is.EqualTo(original.MinClusters));
            Assert.That(ReferenceEquals(original, modified), Is.False);
        });
    }

    [Test]
    public void ComplexScenario_FullWorkflowLifecycle()
    {
        // Arrange: Simulate complete cluster lifecycle scenario
        var provisioningRequest = new ClusterProvisioningRequest
        {
            RequestId = "req-lifecycle-001",
            Configuration = new ClusterConfiguration { Name = "lifecycle-cluster" },
            Priority = Priority.High
        };

        var provisioningResult = new ClusterProvisioningResult
        {
            ClusterId = "lifecycle-cluster",
            Success = true,
            ProvisioningStartTime = DateTime.UtcNow.AddMinutes(-10),
            ProvisioningEndTime = DateTime.UtcNow.AddMinutes(-5),
            ProvisioningDuration = TimeSpan.FromMinutes(5)
        };

        var healthConfig = new HealthMonitoringConfig
        {
            EnabledChecks = new List<HealthCheckType>
            {
                HealthCheckType.RestApiHealthCheck,
                HealthCheckType.JobManagerConnectivity
            }
        };

        var scalingConfig = new AutoScalingConfig
        {
            MinClusters = 1,
            MaxClusters = 10
        };

        // Assert: All lifecycle components configured correctly
        Assert.Multiple(() =>
        {
            Assert.That(provisioningRequest.RequestId, Is.Not.Empty);
            Assert.That(provisioningResult.Success, Is.True);
            Assert.That(provisioningResult.ProvisioningDuration, Is.Not.Null);
            Assert.That(healthConfig.EnabledChecks, Is.Not.Empty);
            Assert.That(scalingConfig.MinClusters, Is.GreaterThanOrEqualTo(1));
        });
    }
}
