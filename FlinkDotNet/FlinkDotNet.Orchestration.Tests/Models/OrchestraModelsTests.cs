
using FlinkDotNet.Orchestration.Models;

namespace FlinkDotNet.Orchestration.Tests.Models;
/// <summary>
/// Comprehensive tests for Orchestra model types.
/// Tests record initialization, properties, and enum validation.
/// </summary>
[TestFixture]
public class OrchestraModelsTests
{
    #region ClusterStatus Tests

    [Test]
    public void ClusterStatus_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var status = new ClusterStatus();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(status.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Unknown));
            Assert.That(status.AvailableSlots, Is.EqualTo(0));
            Assert.That(status.TotalSlots, Is.EqualTo(0));
            Assert.That(status.RunningJobs, Is.EqualTo(0));
            Assert.That(status.Version, Is.EqualTo(string.Empty));
            Assert.That(status.AdditionalMetrics, Is.Not.Null);
            Assert.That(status.AdditionalMetrics, Is.Empty);
        });
    }

    [Test]
    public void ClusterStatus_WithInitializer_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, object> { { "cpu", 0.5 } };

        // Act
        var status = new ClusterStatus
        {
            ClusterId = "cluster-123",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            LastHealthCheck = timestamp,
            Version = "1.18.0",
            AdditionalMetrics = metrics
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(status.ClusterId, Is.EqualTo("cluster-123"));
            Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Healthy));
            Assert.That(status.AvailableSlots, Is.EqualTo(10));
            Assert.That(status.TotalSlots, Is.EqualTo(20));
            Assert.That(status.RunningJobs, Is.EqualTo(5));
            Assert.That(status.LastHealthCheck, Is.EqualTo(timestamp));
            Assert.That(status.Version, Is.EqualTo("1.18.0"));
            Assert.That(status.AdditionalMetrics, Is.EqualTo(metrics));
        });
    }

    #endregion

    #region ClusterMetrics Tests

    [Test]
    public void ClusterMetrics_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var metrics = new ClusterMetrics();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metrics.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(metrics.CpuUtilization, Is.EqualTo(0.0));
            Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.0));
            Assert.That(metrics.ProcessedRecords, Is.EqualTo(0));
            Assert.That(metrics.Throughput, Is.EqualTo(0.0));
            Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.0));
            Assert.That(metrics.CustomMetrics, Is.Not.Null);
            Assert.That(metrics.CustomMetrics, Is.Empty);
        });
    }

    [Test]
    public void ClusterMetrics_WithInitializer_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var customMetrics = new Dictionary<string, double> { { "latency", 45.5 } };

        // Act
        var metrics = new ClusterMetrics
        {
            ClusterId = "cluster-456",
            CpuUtilization = 0.75,
            MemoryUtilization = 0.60,
            ProcessedRecords = 1000000,
            Throughput = 5000.0,
            BackpressureRatio = 0.1,
            Timestamp = timestamp,
            CustomMetrics = customMetrics
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metrics.ClusterId, Is.EqualTo("cluster-456"));
            Assert.That(metrics.CpuUtilization, Is.EqualTo(0.75));
            Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.60));
            Assert.That(metrics.ProcessedRecords, Is.EqualTo(1000000));
            Assert.That(metrics.Throughput, Is.EqualTo(5000.0));
            Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.1));
            Assert.That(metrics.Timestamp, Is.EqualTo(timestamp));
            Assert.That(metrics.CustomMetrics, Is.EqualTo(customMetrics));
        });
    }

    #endregion

    #region FlinkJobDefinition Tests

    [Test]
    public void FlinkJobDefinition_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var job = new FlinkJobDefinition();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(job.JobId, Is.EqualTo(string.Empty));
            Assert.That(job.JobName, Is.EqualTo(string.Empty));
            Assert.That(job.JobGraph, Is.EqualTo(string.Empty));
            Assert.That(job.Parallelism, Is.EqualTo(1));
            Assert.That(job.Priority, Is.EqualTo(JobPriority.Normal));
            Assert.That(job.Timeout, Is.Null);
            Assert.That(job.Configuration, Is.Not.Null);
            Assert.That(job.RequiredResources, Is.Not.Null);
            Assert.That(job.ResourceRequirements, Is.Not.Null);
        });
    }

    [Test]
    public void FlinkJobDefinition_WithInitializer_SetsAllProperties()
    {
        // Arrange
        var config = new Dictionary<string, string> { { "key", "value" } };
        var resources = new List<string> { "resource1" };
        var requirements = new JobResourceRequirements { MinSlots = 2 };
        var timeout = TimeSpan.FromMinutes(30);

        // Act
        var job = new FlinkJobDefinition
        {
            JobId = "job-789",
            JobName = "Test Job",
            JobGraph = "graph-data",
            Parallelism = 8,
            Configuration = config,
            Priority = JobPriority.High,
            Timeout = timeout,
            RequiredResources = resources,
            ResourceRequirements = requirements
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(job.JobId, Is.EqualTo("job-789"));
            Assert.That(job.JobName, Is.EqualTo("Test Job"));
            Assert.That(job.JobGraph, Is.EqualTo("graph-data"));
            Assert.That(job.Parallelism, Is.EqualTo(8));
            Assert.That(job.Configuration, Is.EqualTo(config));
            Assert.That(job.Priority, Is.EqualTo(JobPriority.High));
            Assert.That(job.Timeout, Is.EqualTo(timeout));
            Assert.That(job.RequiredResources, Is.EqualTo(resources));
            Assert.That(job.ResourceRequirements, Is.EqualTo(requirements));
        });
    }

    #endregion

    #region JobSubmissionResult Tests

    [Test]
    public void JobSubmissionResult_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var result = new JobSubmissionResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.JobId, Is.EqualTo(string.Empty));
            Assert.That(result.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.FlinkJobId, Is.Null);
            Assert.That(result.PlacementInfo, Is.Not.Null);
        });
    }

    [Test]
    public void JobSubmissionResult_SuccessfulSubmission_HasCorrectProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var placementInfo = new JobPlacementInfo { ClusterId = "cluster-1" };

        // Act
        var result = new JobSubmissionResult
        {
            JobId = "job-001",
            ClusterId = "cluster-1",
            Success = true,
            SubmissionTime = timestamp,
            FlinkJobId = "flink-job-001",
            PlacementInfo = placementInfo
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.JobId, Is.EqualTo("job-001"));
            Assert.That(result.ClusterId, Is.EqualTo("cluster-1"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.SubmissionTime, Is.EqualTo(timestamp));
            Assert.That(result.FlinkJobId, Is.EqualTo("flink-job-001"));
            Assert.That(result.PlacementInfo, Is.EqualTo(placementInfo));
        });
    }

    [Test]
    public void JobSubmissionResult_FailedSubmission_HasErrorMessage()
    {
        // Act
        var result = new JobSubmissionResult
        {
            JobId = "job-002",
            Success = false,
            ErrorMessage = "No available slots"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("No available slots"));
        });
    }

    #endregion

    #region HealthReport Tests

    [Test]
    public void HealthReport_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var report = new HealthReport();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(report.TotalClusters, Is.EqualTo(0));
            Assert.That(report.HealthyClusters, Is.EqualTo(0));
            Assert.That(report.WarningClusters, Is.EqualTo(0));
            Assert.That(report.CriticalClusters, Is.EqualTo(0));
            Assert.That(report.OfflineClusters, Is.EqualTo(0));
            Assert.That(report.TotalAvailableSlots, Is.EqualTo(0));
            Assert.That(report.TotalRunningJobs, Is.EqualTo(0));
            Assert.That(report.OverallHealthScore, Is.EqualTo(0.0));
            Assert.That(report.Issues, Is.Not.Null);
            Assert.That(report.Issues, Is.Empty);
        });
    }

    [Test]
    public void HealthReport_WithMultipleClusters_CalculatesCorrectMetrics()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var issues = new List<ClusterHealthIssue>
        {
            new() { ClusterId = "cluster-1", Issue = "High CPU", Severity = "Warning" }
        };

        // Act
        var report = new HealthReport
        {
            TotalClusters = 10,
            HealthyClusters = 7,
            WarningClusters = 2,
            CriticalClusters = 1,
            OfflineClusters = 0,
            TotalAvailableSlots = 100,
            TotalRunningJobs = 45,
            OverallHealthScore = 70.0,
            GeneratedAt = timestamp,
            Issues = issues
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(report.TotalClusters, Is.EqualTo(10));
            Assert.That(report.HealthyClusters, Is.EqualTo(7));
            Assert.That(report.WarningClusters, Is.EqualTo(2));
            Assert.That(report.CriticalClusters, Is.EqualTo(1));
            Assert.That(report.OfflineClusters, Is.EqualTo(0));
            Assert.That(report.TotalAvailableSlots, Is.EqualTo(100));
            Assert.That(report.TotalRunningJobs, Is.EqualTo(45));
            Assert.That(report.OverallHealthScore, Is.EqualTo(70.0));
            Assert.That(report.GeneratedAt, Is.EqualTo(timestamp));
            Assert.That(report.Issues, Has.Count.EqualTo(1));
        });
    }

    #endregion

    #region ScalingResult Tests

    [Test]
    public void ScalingResult_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var result = new ScalingResult();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.PreviousCapacity, Is.EqualTo(0));
            Assert.That(result.NewCapacity, Is.EqualTo(0));
            Assert.That(result.ClustersAdded, Is.EqualTo(0));
            Assert.That(result.ClustersRemoved, Is.EqualTo(0));
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Actions, Is.Not.Null);
            Assert.That(result.Actions, Is.Empty);
        });
    }

    [Test]
    public void ScalingResult_SuccessfulScaleUp_HasCorrectProperties()
    {
        // Arrange
        var duration = TimeSpan.FromSeconds(30);
        var actions = new List<string> { "Added cluster-1", "Added cluster-2" };

        // Act
        var result = new ScalingResult
        {
            Success = true,
            PreviousCapacity = 3,
            NewCapacity = 5,
            ClustersAdded = 2,
            ClustersRemoved = 0,
            Duration = duration,
            Actions = actions
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.PreviousCapacity, Is.EqualTo(3));
            Assert.That(result.NewCapacity, Is.EqualTo(5));
            Assert.That(result.ClustersAdded, Is.EqualTo(2));
            Assert.That(result.ClustersRemoved, Is.EqualTo(0));
            Assert.That(result.ErrorMessage, Is.Null);
            Assert.That(result.Duration, Is.EqualTo(duration));
            Assert.That(result.Actions, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void ScalingResult_FailedScaling_HasErrorMessage()
    {
        // Act
        var result = new ScalingResult
        {
            Success = false,
            PreviousCapacity = 5,
            NewCapacity = 5,
            ErrorMessage = "Scaling failed due to insufficient resources"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.False);
            Assert.That(result.ErrorMessage, Is.EqualTo("Scaling failed due to insufficient resources"));
        });
    }

    #endregion

    #region ClusterHealthIssue Tests

    [Test]
    public void ClusterHealthIssue_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var issue = new ClusterHealthIssue();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(issue.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(issue.Issue, Is.EqualTo(string.Empty));
            Assert.That(issue.Severity, Is.EqualTo(string.Empty));
            Assert.That(issue.Resolution, Is.Null);
        });
    }

    [Test]
    public void ClusterHealthIssue_WithData_SetsAllProperties()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;

        // Act
        var issue = new ClusterHealthIssue
        {
            ClusterId = "cluster-999",
            Issue = "Memory leak detected",
            Severity = "Critical",
            DetectedAt = timestamp,
            Resolution = "Restart scheduled"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(issue.ClusterId, Is.EqualTo("cluster-999"));
            Assert.That(issue.Issue, Is.EqualTo("Memory leak detected"));
            Assert.That(issue.Severity, Is.EqualTo("Critical"));
            Assert.That(issue.DetectedAt, Is.EqualTo(timestamp));
            Assert.That(issue.Resolution, Is.EqualTo("Restart scheduled"));
        });
    }

    #endregion

    #region Enum Tests

    [Test]
    public void ClusterHealthState_HasAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<ClusterHealthState>();
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(ClusterHealthState.Unknown));
            Assert.That(values, Contains.Item(ClusterHealthState.Healthy));
            Assert.That(values, Contains.Item(ClusterHealthState.Warning));
            Assert.That(values, Contains.Item(ClusterHealthState.Critical));
            Assert.That(values, Contains.Item(ClusterHealthState.Offline));
        });
    }

    [Test]
    public void JobPriority_HasAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<JobPriority>();
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(JobPriority.Low));
            Assert.That(values, Contains.Item(JobPriority.Normal));
            Assert.That(values, Contains.Item(JobPriority.High));
            Assert.That(values, Contains.Item(JobPriority.Critical));
        });
    }

    [Test]
    public void SubmissionStrategy_HasAllExpectedValues()
    {
        // Assert
        var values = Enum.GetValues<SubmissionStrategy>();
        Assert.Multiple(() =>
        {
            Assert.That(values, Contains.Item(SubmissionStrategy.BestFit));
            Assert.That(values, Contains.Item(SubmissionStrategy.LeastLoaded));
            Assert.That(values, Contains.Item(SubmissionStrategy.RoundRobin));
            Assert.That(values, Contains.Item(SubmissionStrategy.LocalityFirst));
            Assert.That(values, Contains.Item(SubmissionStrategy.HighAvailability));
        });
    }

    #endregion

    #region Configuration Tests

    [Test]
    public void ClusterConfiguration_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var config = new ClusterConfiguration();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(config.Name, Is.EqualTo(string.Empty));
            Assert.That(config.TaskSlots, Is.EqualTo(4));
            Assert.That(config.TaskManagers, Is.EqualTo(2));
            Assert.That(config.FlinkVersion, Is.EqualTo("1.18.0"));
            Assert.That(config.Properties, Is.Not.Null);
            Assert.That(config.ResourceLimits, Is.Not.Null);
            Assert.That(config.Region, Is.EqualTo("default"));
            Assert.That(config.Zone, Is.EqualTo("default"));
            Assert.That(config.HighAvailability, Is.True);
        });
    }

    [Test]
    public void ResourceLimits_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var limits = new ResourceLimits();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(limits.MaxMemoryMB, Is.EqualTo(8192));
            Assert.That(limits.MaxCpuCores, Is.EqualTo(4.0));
            Assert.That(limits.MaxDiskGB, Is.EqualTo(100));
            Assert.That(limits.MaxJobs, Is.EqualTo(50));
        });
    }

    [Test]
    public void JobResourceRequirements_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var requirements = new JobResourceRequirements();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(requirements.MinSlots, Is.EqualTo(1));
            Assert.That(requirements.MaxSlots, Is.EqualTo(int.MaxValue));
            Assert.That(requirements.MemoryMB, Is.EqualTo(1024));
            Assert.That(requirements.CpuCores, Is.EqualTo(1.0));
            Assert.That(requirements.AdditionalRequirements, Is.Not.Null);
        });
    }

    #endregion

    #region OrchestrationRequest Tests

    [Test]
    public void OrchestrationRequest_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var request = new OrchestrationRequest();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(request.RequestId, Is.EqualTo(string.Empty));
            Assert.That(request.TargetClusters, Is.EqualTo(0));
            Assert.That(request.MinClusters, Is.EqualTo(1));
            Assert.That(request.MaxClusters, Is.EqualTo(100));
            Assert.That(request.DefaultClusterConfig, Is.Not.Null);
            Assert.That(request.WorkflowParameters, Is.Not.Null);
            Assert.That(request.MaxDuration, Is.Null);
        });
    }

    [Test]
    public void OrchestrationRequest_WithData_SetsAllProperties()
    {
        // Arrange
        var config = new ClusterConfiguration { Name = "test-cluster" };
        var parameters = new Dictionary<string, object> { { "key", "value" } };
        var maxDuration = TimeSpan.FromHours(2);

        // Act
        var request = new OrchestrationRequest
        {
            RequestId = "req-123",
            TargetClusters = 5,
            MinClusters = 2,
            MaxClusters = 10,
            DefaultClusterConfig = config,
            WorkflowParameters = parameters,
            MaxDuration = maxDuration
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(request.RequestId, Is.EqualTo("req-123"));
            Assert.That(request.TargetClusters, Is.EqualTo(5));
            Assert.That(request.MinClusters, Is.EqualTo(2));
            Assert.That(request.MaxClusters, Is.EqualTo(10));
            Assert.That(request.DefaultClusterConfig, Is.EqualTo(config));
            Assert.That(request.WorkflowParameters, Is.EqualTo(parameters));
            Assert.That(request.MaxDuration, Is.EqualTo(maxDuration));
        });
    }

    #endregion

    #region ClusterInfo Tests

    [Test]
    public void ClusterInfo_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var info = new ClusterInfo();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(info.Name, Is.EqualTo(string.Empty));
            Assert.That(info.Status, Is.Not.Null);
            Assert.That(info.Configuration, Is.Not.Null);
            Assert.That(info.Region, Is.EqualTo(string.Empty));
            Assert.That(info.Zone, Is.EqualTo(string.Empty));
        });
    }

    [Test]
    public void ClusterInfo_WithData_SetsAllProperties()
    {
        // Arrange
        var status = new ClusterStatus { ClusterId = "cluster-1" };
        var config = new ClusterConfiguration { Name = "test-cluster" };
        var createdAt = DateTime.UtcNow;
        var lastUpdate = DateTime.UtcNow;

        // Act
        var info = new ClusterInfo
        {
            ClusterId = "cluster-1",
            Name = "Test Cluster",
            Status = status,
            Configuration = config,
            CreatedAt = createdAt,
            LastUpdateAt = lastUpdate,
            Region = "us-east-1",
            Zone = "us-east-1a"
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo("cluster-1"));
            Assert.That(info.Name, Is.EqualTo("Test Cluster"));
            Assert.That(info.Status, Is.EqualTo(status));
            Assert.That(info.Configuration, Is.EqualTo(config));
            Assert.That(info.CreatedAt, Is.EqualTo(createdAt));
            Assert.That(info.LastUpdateAt, Is.EqualTo(lastUpdate));
            Assert.That(info.Region, Is.EqualTo("us-east-1"));
            Assert.That(info.Zone, Is.EqualTo("us-east-1a"));
        });
    }

    #endregion

    #region JobPlacementInfo Tests

    [Test]
    public void JobPlacementInfo_DefaultInitialization_SetsCorrectDefaults()
    {
        // Arrange & Act
        var info = new JobPlacementInfo();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo(string.Empty));
            Assert.That(info.Reason, Is.EqualTo(string.Empty));
            Assert.That(info.AssignedSlots, Is.EqualTo(0));
            Assert.That(info.Strategy, Is.EqualTo(SubmissionStrategy.BestFit));
            Assert.That(info.PlacementMetadata, Is.Not.Null);
            Assert.That(info.PlacementMetadata, Is.Empty);
        });
    }

    [Test]
    public void JobPlacementInfo_WithData_SetsAllProperties()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { { "priority", "high" } };

        // Act
        var info = new JobPlacementInfo
        {
            ClusterId = "cluster-2",
            Reason = "Best resource match",
            AssignedSlots = 8,
            Strategy = SubmissionStrategy.LeastLoaded,
            PlacementMetadata = metadata
        };

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(info.ClusterId, Is.EqualTo("cluster-2"));
            Assert.That(info.Reason, Is.EqualTo("Best resource match"));
            Assert.That(info.AssignedSlots, Is.EqualTo(8));
            Assert.That(info.Strategy, Is.EqualTo(SubmissionStrategy.LeastLoaded));
            Assert.That(info.PlacementMetadata, Is.EqualTo(metadata));
        });
    }

    #endregion
}
