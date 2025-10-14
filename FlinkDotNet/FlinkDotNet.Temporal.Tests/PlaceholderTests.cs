using FlinkDotNet.Temporal.Models;
using ClusterConfiguration = FlinkDotNet.ClusterManager.Models.ClusterConfiguration;

namespace FlinkDotNet.Temporal.Tests;

/// <summary>
/// Comprehensive tests for JobDistributionResult model.
/// </summary>
[TestFixture]
public class JobDistributionResultTests
{
    [Test]
    public void JobDistributionResult_DefaultInitialization_SetsEmptyCollections()
    {
        var result = new JobDistributionResult();
        
        Assert.Multiple(() =>
        {
            Assert.That(result.TotalJobs, Is.EqualTo(0));
            Assert.That(result.SuccessfulPlacements, Is.EqualTo(0));
            Assert.That(result.FailedPlacements, Is.EqualTo(0));
            Assert.That(result.Placements, Is.Not.Null);
            Assert.That(result.Placements, Is.Empty);
            Assert.That(result.ClusterDistribution, Is.Not.Null);
            Assert.That(result.ClusterDistribution, Is.Empty);
            Assert.That(result.TotalDistributionTime, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void JobDistributionResult_WithAllProperties_ReturnsCorrectValues()
    {
        var placements = new List<JobPlacementResult>
        {
            new() { JobId = "job1", ClusterId = "cluster1", Success = true },
            new() { JobId = "job2", ClusterId = "cluster2", Success = true }
        };
        var clusterDist = new Dictionary<string, int>
        {
            ["cluster1"] = 5,
            ["cluster2"] = 3
        };
        var duration = TimeSpan.FromMinutes(2);

        var result = new JobDistributionResult
        {
            TotalJobs = 10,
            SuccessfulPlacements = 8,
            FailedPlacements = 2,
            Placements = placements,
            TotalDistributionTime = duration,
            ClusterDistribution = clusterDist
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.TotalJobs, Is.EqualTo(10));
            Assert.That(result.SuccessfulPlacements, Is.EqualTo(8));
            Assert.That(result.FailedPlacements, Is.EqualTo(2));
            Assert.That(result.Placements, Has.Count.EqualTo(2));
            Assert.That(result.TotalDistributionTime, Is.EqualTo(duration));
            Assert.That(result.ClusterDistribution, Has.Count.EqualTo(2));
            Assert.That(result.ClusterDistribution["cluster1"], Is.EqualTo(5));
        });
    }

    [Test]
    public void JobDistributionResult_ImmutabilityWithKeyword_WorksCorrectly()
    {
        var result1 = new JobDistributionResult
        {
            TotalJobs = 10,
            SuccessfulPlacements = 8,
            FailedPlacements = 2
        };

        var result2 = result1 with { FailedPlacements = 3 };

        Assert.Multiple(() =>
        {
            Assert.That(result1.FailedPlacements, Is.EqualTo(2));
            Assert.That(result2.FailedPlacements, Is.EqualTo(3));
            Assert.That(result2.TotalJobs, Is.EqualTo(result1.TotalJobs));
            Assert.That(ReferenceEquals(result1, result2), Is.False);
        });
    }
}

/// <summary>
/// Comprehensive tests for JobPlacementResult model.
/// </summary>
[TestFixture]
public class JobPlacementResultTests
{
    [Test]
    public void JobPlacementResult_DefaultInitialization_SetsEmptyStrings()
    {
        var result = new JobPlacementResult();
        
        Assert.Multiple(() =>
        {
            Assert.That(result.JobId, Is.EqualTo(string.Empty));
            Assert.That(result.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.PlacementTime, Is.EqualTo(default(DateTime)));
            Assert.That(result.PlacementDuration, Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void JobPlacementResult_SuccessScenario_NoErrorMessage()
    {
        var placementTime = DateTime.UtcNow;
        var duration = TimeSpan.FromSeconds(5);

        var result = new JobPlacementResult
        {
            JobId = "job-123",
            ClusterId = "cluster-456",
            Success = true,
            PlacementTime = placementTime,
            PlacementDuration = duration
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.JobId, Is.EqualTo("job-123"));
            Assert.That(result.ClusterId, Is.EqualTo("cluster-456"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.PlacementTime, Is.EqualTo(placementTime));
            Assert.That(result.PlacementDuration, Is.EqualTo(duration));
        });
    }

    [Test]
    public void JobPlacementResult_FailureScenario_IncludesErrorMessage()
    {
        var result = new JobPlacementResult
        {
            JobId = "job-789",
            ClusterId = "cluster-999",
            Success = false,
            ErrorMessage = "Insufficient resources"
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Insufficient resources"));
        });
    }
}

/// <summary>
/// Comprehensive tests for AutoScalingConfig model.
/// </summary>
[TestFixture]
public class AutoScalingConfigTests
{
    [Test]
    public void AutoScalingConfig_DefaultValues_AreReasonable()
    {
        var config = new AutoScalingConfig();
        
        Assert.Multiple(() =>
        {
            Assert.That(config.MinClusters, Is.EqualTo(1));
            Assert.That(config.MaxClusters, Is.EqualTo(100));
            Assert.That(config.ScaleUpThreshold, Is.EqualTo(80.0));
            Assert.That(config.ScaleDownThreshold, Is.EqualTo(30.0));
            Assert.That(config.EvaluationInterval, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(config.CooldownPeriod, Is.EqualTo(TimeSpan.FromMinutes(10)));
            Assert.That(config.ScaleUpIncrement, Is.EqualTo(1));
            Assert.That(config.ScaleDownIncrement, Is.EqualTo(1));
            Assert.That(config.Metrics, Is.Not.Null);
            Assert.That(config.Metrics, Is.Empty);
        });
    }

    [Test]
    public void AutoScalingConfig_CustomValues_CanBeSet()
    {
        var metrics = new List<AutoScalingMetric>
        {
            new() { Name = "cpu", Type = AutoScalingMetricType.CpuUtilization }
        };

        var config = new AutoScalingConfig
        {
            MinClusters = 2,
            MaxClusters = 50,
            ScaleUpThreshold = 70.0,
            ScaleDownThreshold = 20.0,
            EvaluationInterval = TimeSpan.FromMinutes(3),
            CooldownPeriod = TimeSpan.FromMinutes(5),
            ScaleUpIncrement = 2,
            ScaleDownIncrement = 1,
            Metrics = metrics
        };

        Assert.Multiple(() =>
        {
            Assert.That(config.MinClusters, Is.EqualTo(2));
            Assert.That(config.MaxClusters, Is.EqualTo(50));
            Assert.That(config.ScaleUpThreshold, Is.EqualTo(70.0));
            Assert.That(config.ScaleDownThreshold, Is.EqualTo(20.0));
            Assert.That(config.EvaluationInterval, Is.EqualTo(TimeSpan.FromMinutes(3)));
            Assert.That(config.CooldownPeriod, Is.EqualTo(TimeSpan.FromMinutes(5)));
            Assert.That(config.ScaleUpIncrement, Is.EqualTo(2));
            Assert.That(config.ScaleDownIncrement, Is.EqualTo(1));
            Assert.That(config.Metrics, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void AutoScalingConfig_BoundaryValues_CanBeSet()
    {
        var config = new AutoScalingConfig
        {
            MinClusters = 0,
            MaxClusters = 1000,
            ScaleUpThreshold = 100.0,
            ScaleDownThreshold = 0.0
        };

        Assert.Multiple(() =>
        {
            Assert.That(config.MinClusters, Is.EqualTo(0));
            Assert.That(config.MaxClusters, Is.EqualTo(1000));
            Assert.That(config.ScaleUpThreshold, Is.EqualTo(100.0));
            Assert.That(config.ScaleDownThreshold, Is.EqualTo(0.0));
        });
    }
}

/// <summary>
/// Comprehensive tests for AutoScalingMetric model.
/// </summary>
[TestFixture]
public class AutoScalingMetricTests
{
    [Test]
    public void AutoScalingMetric_DefaultValues_AreSet()
    {
        var metric = new AutoScalingMetric();
        
        Assert.Multiple(() =>
        {
            Assert.That(metric.Name, Is.EqualTo(string.Empty));
            Assert.That(metric.Weight, Is.EqualTo(1.0));
            Assert.That(metric.Threshold, Is.EqualTo(0.0));
            Assert.That(metric.Type, Is.EqualTo(AutoScalingMetricType.CpuUtilization));
        });
    }

    [Test]
    public void AutoScalingMetric_AllProperties_CanBeSet()
    {
        var metric = new AutoScalingMetric
        {
            Name = "memory-usage",
            Weight = 2.5,
            Threshold = 75.0,
            Type = AutoScalingMetricType.MemoryUtilization
        };

        Assert.Multiple(() =>
        {
            Assert.That(metric.Name, Is.EqualTo("memory-usage"));
            Assert.That(metric.Weight, Is.EqualTo(2.5));
            Assert.That(metric.Threshold, Is.EqualTo(75.0));
            Assert.That(metric.Type, Is.EqualTo(AutoScalingMetricType.MemoryUtilization));
        });
    }
}

/// <summary>
/// Tests for AutoScalingMetricType enum.
/// </summary>
[TestFixture]
public class AutoScalingMetricTypeTests
{
    [Test]
    public void AutoScalingMetricType_AllValues_AreDefined()
    {
        var values = Enum.GetValues<AutoScalingMetricType>();
        
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(AutoScalingMetricType.CpuUtilization));
            Assert.That(values, Contains.Item(AutoScalingMetricType.MemoryUtilization));
            Assert.That(values, Contains.Item(AutoScalingMetricType.JobQueueLength));
            Assert.That(values, Contains.Item(AutoScalingMetricType.Throughput));
            Assert.That(values, Contains.Item(AutoScalingMetricType.BackpressureRatio));
            Assert.That(values, Contains.Item(AutoScalingMetricType.Custom));
            Assert.That(values, Has.Length.EqualTo(6));
        });
    }

    [Test]
    public void AutoScalingMetricType_CanConvertToString()
    {
        Assert.Multiple(() =>
        {
            Assert.That(AutoScalingMetricType.CpuUtilization.ToString(), Is.EqualTo("CpuUtilization"));
            Assert.That(AutoScalingMetricType.MemoryUtilization.ToString(), Is.EqualTo("MemoryUtilization"));
            Assert.That(AutoScalingMetricType.Custom.ToString(), Is.EqualTo("Custom"));
        });
    }
}

/// <summary>
/// Comprehensive tests for ClusterFailureInfo model.
/// </summary>
[TestFixture]
public class ClusterFailureInfoTests
{
    [Test]
    public void ClusterFailureInfo_DefaultValues_AreSet()
    {
        var info = new ClusterFailureInfo();
        
        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(info.FailureType, Is.EqualTo(ClusterFailureType.Unknown));
            Assert.That(info.FailureTime, Is.EqualTo(default(DateTime)));
            Assert.That(info.Description, Is.EqualTo(string.Empty));
            Assert.That(info.FailureContext, Is.Not.Null);
            Assert.That(info.FailureContext, Is.Empty);
            Assert.That(info.AffectedJobs, Is.Not.Null);
            Assert.That(info.AffectedJobs, Is.Empty);
            Assert.That(info.Severity, Is.EqualTo(FailureSeverity.Low));
        });
    }

    [Test]
    public void ClusterFailureInfo_CriticalFailure_WithAllDetails()
    {
        var failureTime = DateTime.UtcNow;
        var context = new Dictionary<string, object>
        {
            ["error_code"] = 500,
            ["memory_used"] = "95%"
        };
        var affectedJobs = new List<string> { "job1", "job2", "job3" };

        var info = new ClusterFailureInfo
        {
            ClusterId = "cluster-001",
            FailureType = ClusterFailureType.OutOfMemory,
            FailureTime = failureTime,
            Description = "Cluster ran out of memory during peak load",
            FailureContext = context,
            AffectedJobs = affectedJobs,
            Severity = FailureSeverity.Critical
        };

        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo("cluster-001"));
            Assert.That(info.FailureType, Is.EqualTo(ClusterFailureType.OutOfMemory));
            Assert.That(info.FailureTime, Is.EqualTo(failureTime));
            Assert.That(info.Description, Contains.Substring("out of memory"));
            Assert.That(info.FailureContext, Has.Count.EqualTo(2));
            Assert.That(info.AffectedJobs, Has.Count.EqualTo(3));
            Assert.That(info.Severity, Is.EqualTo(FailureSeverity.Critical));
        });
    }

    [Test]
    public void ClusterFailureInfo_AllFailureTypes_CanBeUsed()
    {
        var types = new[]
        {
            ClusterFailureType.Unknown,
            ClusterFailureType.OutOfMemory,
            ClusterFailureType.DiskFull,
            ClusterFailureType.NetworkPartition,
            ClusterFailureType.JobManagerFailure,
            ClusterFailureType.TaskManagerFailure,
            ClusterFailureType.CheckpointFailure,
            ClusterFailureType.ConfigurationError,
            ClusterFailureType.ResourceExhaustion
        };

        foreach (var type in types)
        {
            var info = new ClusterFailureInfo { FailureType = type };
            Assert.That(info.FailureType, Is.EqualTo(type));
        }
    }
}

/// <summary>
/// Tests for ClusterFailureType enum.
/// </summary>
[TestFixture]
public class ClusterFailureTypeTests
{
    [Test]
    public void ClusterFailureType_AllValues_AreDefined()
    {
        var values = Enum.GetValues<ClusterFailureType>();
        
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(ClusterFailureType.Unknown));
            Assert.That(values, Contains.Item(ClusterFailureType.OutOfMemory));
            Assert.That(values, Contains.Item(ClusterFailureType.DiskFull));
            Assert.That(values, Contains.Item(ClusterFailureType.NetworkPartition));
            Assert.That(values, Contains.Item(ClusterFailureType.JobManagerFailure));
            Assert.That(values, Contains.Item(ClusterFailureType.TaskManagerFailure));
            Assert.That(values, Contains.Item(ClusterFailureType.CheckpointFailure));
            Assert.That(values, Contains.Item(ClusterFailureType.ConfigurationError));
            Assert.That(values, Contains.Item(ClusterFailureType.ResourceExhaustion));
            Assert.That(values, Has.Length.EqualTo(9));
        });
    }
}

/// <summary>
/// Tests for FailureSeverity enum.
/// </summary>
[TestFixture]
public class FailureSeverityTests
{
    [Test]
    public void FailureSeverity_AllLevels_AreDefined()
    {
        var values = Enum.GetValues<FailureSeverity>();
        
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(FailureSeverity.Low));
            Assert.That(values, Contains.Item(FailureSeverity.Medium));
            Assert.That(values, Contains.Item(FailureSeverity.High));
            Assert.That(values, Contains.Item(FailureSeverity.Critical));
            Assert.That(values, Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void FailureSeverity_Ordering_IsLogical()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)FailureSeverity.Low, Is.LessThan((int)FailureSeverity.Medium));
            Assert.That((int)FailureSeverity.Medium, Is.LessThan((int)FailureSeverity.High));
            Assert.That((int)FailureSeverity.High, Is.LessThan((int)FailureSeverity.Critical));
        });
    }
}

/// <summary>
/// Comprehensive tests for HealthMonitoringConfig model.
/// </summary>
[TestFixture]
public class HealthMonitoringConfigTests
{
    [Test]
    public void HealthMonitoringConfig_DefaultValues_AreReasonable()
    {
        var config = new HealthMonitoringConfig();
        
        Assert.Multiple(() =>
        {
            Assert.That(config.CheckInterval, Is.EqualTo(TimeSpan.FromMinutes(1)));
            Assert.That(config.HealthTimeout, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(config.MaxConsecutiveFailures, Is.EqualTo(3));
            Assert.That(config.EnabledChecks, Is.Not.Null);
            Assert.That(config.EnabledChecks, Is.Empty);
            Assert.That(config.CheckParameters, Is.Not.Null);
            Assert.That(config.CheckParameters, Is.Empty);
        });
    }

    [Test]
    public void HealthMonitoringConfig_CustomValues_CanBeSet()
    {
        var enabledChecks = new List<HealthCheckType>
        {
            HealthCheckType.RestApiHealthCheck,
            HealthCheckType.JobManagerConnectivity
        };
        var parameters = new Dictionary<string, object>
        {
            ["retry_count"] = 5,
            ["timeout_ms"] = 10000
        };

        var config = new HealthMonitoringConfig
        {
            CheckInterval = TimeSpan.FromSeconds(30),
            HealthTimeout = TimeSpan.FromSeconds(15),
            MaxConsecutiveFailures = 5,
            EnabledChecks = enabledChecks,
            CheckParameters = parameters
        };

        Assert.Multiple(() =>
        {
            Assert.That(config.CheckInterval, Is.EqualTo(TimeSpan.FromSeconds(30)));
            Assert.That(config.HealthTimeout, Is.EqualTo(TimeSpan.FromSeconds(15)));
            Assert.That(config.MaxConsecutiveFailures, Is.EqualTo(5));
            Assert.That(config.EnabledChecks, Has.Count.EqualTo(2));
            Assert.That(config.CheckParameters, Has.Count.EqualTo(2));
        });
    }
}

/// <summary>
/// Tests for HealthCheckType enum.
/// </summary>
[TestFixture]
public class HealthCheckTypeTests
{
    [Test]
    public void HealthCheckType_AllValues_AreDefined()
    {
        var values = Enum.GetValues<HealthCheckType>();
        
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(HealthCheckType.RestApiHealthCheck));
            Assert.That(values, Contains.Item(HealthCheckType.JobManagerConnectivity));
            Assert.That(values, Contains.Item(HealthCheckType.TaskManagerStatus));
            Assert.That(values, Contains.Item(HealthCheckType.CheckpointStatus));
            Assert.That(values, Contains.Item(HealthCheckType.BackpressureMonitoring));
            Assert.That(values, Contains.Item(HealthCheckType.ResourceUtilization));
            Assert.That(values, Contains.Item(HealthCheckType.JobStatus));
            Assert.That(values, Has.Length.EqualTo(7));
        });
    }
}

/// <summary>
/// Comprehensive tests for ClusterProvisioningRequest model.
/// </summary>
[TestFixture]
public class ClusterProvisioningRequestTests
{
    [Test]
    public void ClusterProvisioningRequest_DefaultValues_AreSet()
    {
        var request = new ClusterProvisioningRequest();
        
        Assert.Multiple(() =>
        {
            Assert.That(request.RequestId, Is.EqualTo(string.Empty));
            Assert.That(request.Configuration, Is.Not.Null);
            Assert.That(request.Region, Is.EqualTo("default"));
            Assert.That(request.Zone, Is.EqualTo("default"));
            Assert.That(request.Priority, Is.EqualTo(Priority.Normal));
            Assert.That(request.Timeout, Is.EqualTo(TimeSpan.FromMinutes(15)));
            Assert.That(request.Metadata, Is.Not.Null);
            Assert.That(request.Metadata, Is.Empty);
        });
    }

    [Test]
    public void ClusterProvisioningRequest_HighPriority_WithCustomSettings()
    {
        var config = new ClusterConfiguration
        {
            Name = "test-cluster"
        };
        var metadata = new Dictionary<string, object>
        {
            ["requester"] = "admin",
            ["purpose"] = "production"
        };

        var request = new ClusterProvisioningRequest
        {
            RequestId = "req-12345",
            Configuration = config,
            Region = "us-west-2",
            Zone = "az-1",
            Priority = Priority.High,
            Timeout = TimeSpan.FromMinutes(30),
            Metadata = metadata
        };

        Assert.Multiple(() =>
        {
            Assert.That(request.RequestId, Is.EqualTo("req-12345"));
            Assert.That(request.Configuration.Name, Is.EqualTo("test-cluster"));
            Assert.That(request.Region, Is.EqualTo("us-west-2"));
            Assert.That(request.Zone, Is.EqualTo("az-1"));
            Assert.That(request.Priority, Is.EqualTo(Priority.High));
            Assert.That(request.Timeout, Is.EqualTo(TimeSpan.FromMinutes(30)));
            Assert.That(request.Metadata, Has.Count.EqualTo(2));
        });
    }
}

/// <summary>
/// Comprehensive tests for ClusterProvisioningResult model.
/// </summary>
[TestFixture]
public class ClusterProvisioningResultTests
{
    [Test]
    public void ClusterProvisioningResult_DefaultValues_AreSet()
    {
        var result = new ClusterProvisioningResult();
        
        Assert.Multiple(() =>
        {
            Assert.That(result.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.ProvisioningStartTime, Is.EqualTo(default(DateTime)));
            Assert.That(result.ProvisioningEndTime, Is.Null);
            Assert.That(result.ProvisioningDuration, Is.Null);
            Assert.That(result.ProvisioningMetadata, Is.Not.Null);
            Assert.That(result.ProvisioningMetadata, Is.Empty);
        });
    }

    [Test]
    public void ClusterProvisioningResult_SuccessScenario_WithTiming()
    {
        var startTime = DateTime.UtcNow;
        var endTime = startTime.AddMinutes(5);
        var duration = endTime - startTime;

        var result = new ClusterProvisioningResult
        {
            ClusterId = "cluster-abc",
            Success = true,
            ProvisioningStartTime = startTime,
            ProvisioningEndTime = endTime,
            ProvisioningDuration = duration
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.ClusterId, Is.EqualTo("cluster-abc"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.ProvisioningStartTime, Is.EqualTo(startTime));
            Assert.That(result.ProvisioningEndTime, Is.EqualTo(endTime));
            Assert.That(result.ProvisioningDuration, Is.EqualTo(duration));
        });
    }

    [Test]
    public void ClusterProvisioningResult_FailureScenario_WithErrorMessage()
    {
        var result = new ClusterProvisioningResult
        {
            ClusterId = "cluster-failed",
            Success = false,
            ErrorMessage = "Insufficient capacity in region",
            ProvisioningStartTime = DateTime.UtcNow
        };

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Insufficient capacity in region"));
            Assert.That(result.ProvisioningEndTime, Is.Null);
        });
    }
}

/// <summary>
/// Tests for Priority enum.
/// </summary>
[TestFixture]
public class PriorityTests
{
    [Test]
    public void Priority_AllLevels_AreDefined()
    {
        var values = Enum.GetValues<Priority>();
        
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(Priority.Low));
            Assert.That(values, Contains.Item(Priority.Normal));
            Assert.That(values, Contains.Item(Priority.High));
            Assert.That(values, Contains.Item(Priority.Critical));
            Assert.That(values, Has.Length.EqualTo(4));
        });
    }

    [Test]
    public void Priority_Ordering_IsLogical()
    {
        Assert.Multiple(() =>
        {
            Assert.That((int)Priority.Low, Is.LessThan((int)Priority.Normal));
            Assert.That((int)Priority.Normal, Is.LessThan((int)Priority.High));
            Assert.That((int)Priority.High, Is.LessThan((int)Priority.Critical));
        });
    }
}
