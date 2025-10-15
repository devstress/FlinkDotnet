using FlinkDotNet.ClusterManager.Models;

namespace FlinkDotNet.ClusterManager.Tests.Models;

/// <summary>
/// Tests for ClusterManager model classes, enums, and records.
/// </summary>
[TestFixture]
public class ClusterModelsTests
{
    [Test]
    public void ClusterStatus_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var status = new ClusterStatus();

        // Assert
        Assert.That(status.ClusterId, Is.EqualTo(string.Empty));
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Unknown));
        Assert.That(status.AvailableSlots, Is.EqualTo(0));
        Assert.That(status.TotalSlots, Is.EqualTo(0));
        Assert.That(status.RunningJobs, Is.EqualTo(0));
        Assert.That(status.Version, Is.EqualTo(string.Empty));
        Assert.That(status.AdditionalMetrics, Is.Not.Null);
        Assert.That(status.AdditionalMetrics, Is.Empty);
    }

    [Test]
    public void ClusterStatus_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var metrics = new Dictionary<string, object> { ["test"] = "value" };

        // Act
        var status = new ClusterStatus
        {
            ClusterId = "cluster-1",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            LastHealthCheck = timestamp,
            Version = "1.18.0",
            AdditionalMetrics = metrics
        };

        // Assert
        Assert.That(status.ClusterId, Is.EqualTo("cluster-1"));
        Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Healthy));
        Assert.That(status.AvailableSlots, Is.EqualTo(10));
        Assert.That(status.TotalSlots, Is.EqualTo(20));
        Assert.That(status.RunningJobs, Is.EqualTo(5));
        Assert.That(status.LastHealthCheck, Is.EqualTo(timestamp));
        Assert.That(status.Version, Is.EqualTo("1.18.0"));
        Assert.That(status.AdditionalMetrics, Is.EqualTo(metrics));
    }

    [Test]
    public void ClusterMetrics_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var metrics = new ClusterMetrics();

        // Assert
        Assert.That(metrics.ClusterId, Is.EqualTo(string.Empty));
        Assert.That(metrics.CpuUtilization, Is.EqualTo(0.0));
        Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.0));
        Assert.That(metrics.ProcessedRecords, Is.EqualTo(0));
        Assert.That(metrics.Throughput, Is.EqualTo(0.0));
        Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.0));
        Assert.That(metrics.CustomMetrics, Is.Not.Null);
        Assert.That(metrics.CustomMetrics, Is.Empty);
    }

    [Test]
    public void ClusterMetrics_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var customMetrics = new Dictionary<string, double> { ["latency"] = 125.5 };

        // Act
        var metrics = new ClusterMetrics
        {
            ClusterId = "cluster-1",
            CpuUtilization = 0.75,
            MemoryUtilization = 0.65,
            ProcessedRecords = 1000000,
            Throughput = 5000.5,
            BackpressureRatio = 0.15,
            Timestamp = timestamp,
            CustomMetrics = customMetrics
        };

        // Assert
        Assert.That(metrics.ClusterId, Is.EqualTo("cluster-1"));
        Assert.That(metrics.CpuUtilization, Is.EqualTo(0.75));
        Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.65));
        Assert.That(metrics.ProcessedRecords, Is.EqualTo(1000000));
        Assert.That(metrics.Throughput, Is.EqualTo(5000.5));
        Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.15));
        Assert.That(metrics.Timestamp, Is.EqualTo(timestamp));
        Assert.That(metrics.CustomMetrics, Is.EqualTo(customMetrics));
    }

    [Test]
    public void FlinkJobDefinition_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var jobDef = new FlinkJobDefinition();

        // Assert
        Assert.That(jobDef.JobId, Is.EqualTo(string.Empty));
        Assert.That(jobDef.JobName, Is.EqualTo(string.Empty));
        Assert.That(jobDef.JobGraph, Is.EqualTo(string.Empty));
        Assert.That(jobDef.Parallelism, Is.EqualTo(1));
        Assert.That(jobDef.Configuration, Is.Not.Null);
        Assert.That(jobDef.Configuration, Is.Empty);
        Assert.That(jobDef.Priority, Is.EqualTo(JobPriority.Normal));
        Assert.That(jobDef.Timeout, Is.Null);
        Assert.That(jobDef.RequiredResources, Is.Not.Null);
        Assert.That(jobDef.RequiredResources, Is.Empty);
        Assert.That(jobDef.ResourceRequirements, Is.Not.Null);
    }

    [Test]
    public void FlinkJobDefinition_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var config = new Dictionary<string, string> { ["key"] = "value" };
        var timeout = TimeSpan.FromMinutes(30);
        var resources = new List<string> { "gpu", "ssd" };
        var requirements = new JobResourceRequirements { MinSlots = 4 };

        // Act
        var jobDef = new FlinkJobDefinition
        {
            JobId = "job-1",
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
        Assert.That(jobDef.JobId, Is.EqualTo("job-1"));
        Assert.That(jobDef.JobName, Is.EqualTo("Test Job"));
        Assert.That(jobDef.JobGraph, Is.EqualTo("graph-data"));
        Assert.That(jobDef.Parallelism, Is.EqualTo(8));
        Assert.That(jobDef.Configuration, Is.EqualTo(config));
        Assert.That(jobDef.Priority, Is.EqualTo(JobPriority.High));
        Assert.That(jobDef.Timeout, Is.EqualTo(timeout));
        Assert.That(jobDef.RequiredResources, Is.EqualTo(resources));
        Assert.That(jobDef.ResourceRequirements, Is.EqualTo(requirements));
    }

    [Test]
    public void JobSubmissionResult_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var result = new JobSubmissionResult();

        // Assert
        Assert.That(result.JobId, Is.EqualTo(string.Empty));
        Assert.That(result.ClusterId, Is.EqualTo(string.Empty));
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.FlinkJobId, Is.Null);
        Assert.That(result.PlacementInfo, Is.Not.Null);
    }

    [Test]
    public void JobSubmissionResult_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var placementInfo = new JobPlacementInfo { ClusterId = "cluster-1" };

        // Act
        var result = new JobSubmissionResult
        {
            JobId = "job-1",
            ClusterId = "cluster-1",
            Success = true,
            ErrorMessage = null,
            SubmissionTime = timestamp,
            FlinkJobId = "flink-job-1",
            PlacementInfo = placementInfo
        };

        // Assert
        Assert.That(result.JobId, Is.EqualTo("job-1"));
        Assert.That(result.ClusterId, Is.EqualTo("cluster-1"));
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.SubmissionTime, Is.EqualTo(timestamp));
        Assert.That(result.FlinkJobId, Is.EqualTo("flink-job-1"));
        Assert.That(result.PlacementInfo, Is.EqualTo(placementInfo));
    }

    [Test]
    public void JobResourceRequirements_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var requirements = new JobResourceRequirements();

        // Assert
        Assert.That(requirements.MinSlots, Is.EqualTo(1));
        Assert.That(requirements.MaxSlots, Is.EqualTo(int.MaxValue));
        Assert.That(requirements.MemoryMB, Is.EqualTo(1024));
        Assert.That(requirements.CpuCores, Is.EqualTo(1.0));
        Assert.That(requirements.AdditionalRequirements, Is.Not.Null);
        Assert.That(requirements.AdditionalRequirements, Is.Empty);
    }

    [Test]
    public void JobResourceRequirements_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var additional = new Dictionary<string, object> { ["gpu"] = true };

        // Act
        var requirements = new JobResourceRequirements
        {
            MinSlots = 4,
            MaxSlots = 16,
            MemoryMB = 8192,
            CpuCores = 4.0,
            AdditionalRequirements = additional
        };

        // Assert
        Assert.That(requirements.MinSlots, Is.EqualTo(4));
        Assert.That(requirements.MaxSlots, Is.EqualTo(16));
        Assert.That(requirements.MemoryMB, Is.EqualTo(8192));
        Assert.That(requirements.CpuCores, Is.EqualTo(4.0));
        Assert.That(requirements.AdditionalRequirements, Is.EqualTo(additional));
    }

    [Test]
    public void JobPlacementInfo_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var info = new JobPlacementInfo();

        // Assert
        Assert.That(info.ClusterId, Is.EqualTo(string.Empty));
        Assert.That(info.Reason, Is.EqualTo(string.Empty));
        Assert.That(info.AssignedSlots, Is.EqualTo(0));
        Assert.That(info.Strategy, Is.EqualTo(SubmissionStrategy.BestFit));
        Assert.That(info.PlacementMetadata, Is.Not.Null);
        Assert.That(info.PlacementMetadata, Is.Empty);
    }

    [Test]
    public void JobPlacementInfo_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var metadata = new Dictionary<string, object> { ["score"] = 95 };

        // Act
        var info = new JobPlacementInfo
        {
            ClusterId = "cluster-1",
            Reason = "Best resource match",
            AssignedSlots = 8,
            Strategy = SubmissionStrategy.LeastLoaded,
            PlacementMetadata = metadata
        };

        // Assert
        Assert.That(info.ClusterId, Is.EqualTo("cluster-1"));
        Assert.That(info.Reason, Is.EqualTo("Best resource match"));
        Assert.That(info.AssignedSlots, Is.EqualTo(8));
        Assert.That(info.Strategy, Is.EqualTo(SubmissionStrategy.LeastLoaded));
        Assert.That(info.PlacementMetadata, Is.EqualTo(metadata));
    }

    [Test]
    public void ClusterConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new ClusterConfiguration();

        // Assert
        Assert.That(config.Name, Is.EqualTo(string.Empty));
        Assert.That(config.TaskSlots, Is.EqualTo(4));
        Assert.That(config.TaskManagers, Is.EqualTo(2));
        Assert.That(config.FlinkVersion, Is.EqualTo("1.18.0"));
        Assert.That(config.Properties, Is.Not.Null);
        Assert.That(config.Properties, Is.Empty);
        Assert.That(config.ResourceLimits, Is.Not.Null);
        Assert.That(config.Region, Is.EqualTo("default"));
        Assert.That(config.Zone, Is.EqualTo("default"));
        Assert.That(config.HighAvailability, Is.True);
    }

    [Test]
    public void ClusterConfiguration_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange
        var props = new Dictionary<string, string> { ["restart.delay.seconds"] = "10" };
        var limits = new ResourceLimits { MaxMemoryMB = 16384 };

        // Act
        var config = new ClusterConfiguration
        {
            Name = "test-cluster",
            TaskSlots = 8,
            TaskManagers = 4,
            FlinkVersion = "1.19.0",
            Properties = props,
            ResourceLimits = limits,
            Region = "us-west-2",
            Zone = "us-west-2a",
            HighAvailability = false
        };

        // Assert
        Assert.That(config.Name, Is.EqualTo("test-cluster"));
        Assert.That(config.TaskSlots, Is.EqualTo(8));
        Assert.That(config.TaskManagers, Is.EqualTo(4));
        Assert.That(config.FlinkVersion, Is.EqualTo("1.19.0"));
        Assert.That(config.Properties, Is.EqualTo(props));
        Assert.That(config.ResourceLimits, Is.EqualTo(limits));
        Assert.That(config.Region, Is.EqualTo("us-west-2"));
        Assert.That(config.Zone, Is.EqualTo("us-west-2a"));
        Assert.That(config.HighAvailability, Is.False);
    }

    [Test]
    public void ResourceLimits_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var limits = new ResourceLimits();

        // Assert
        Assert.That(limits.MaxMemoryMB, Is.EqualTo(8192));
        Assert.That(limits.MaxCpuCores, Is.EqualTo(4.0));
        Assert.That(limits.MaxDiskGB, Is.EqualTo(100));
        Assert.That(limits.MaxJobs, Is.EqualTo(50));
    }

    [Test]
    public void ResourceLimits_WithInitializer_SetsPropertiesCorrectly()
    {
        // Arrange & Act
        var limits = new ResourceLimits
        {
            MaxMemoryMB = 32768,
            MaxCpuCores = 16.0,
            MaxDiskGB = 500,
            MaxJobs = 100
        };

        // Assert
        Assert.That(limits.MaxMemoryMB, Is.EqualTo(32768));
        Assert.That(limits.MaxCpuCores, Is.EqualTo(16.0));
        Assert.That(limits.MaxDiskGB, Is.EqualTo(500));
        Assert.That(limits.MaxJobs, Is.EqualTo(100));
    }

    [TestCase(ClusterHealthState.Unknown)]
    [TestCase(ClusterHealthState.Healthy)]
    [TestCase(ClusterHealthState.Warning)]
    [TestCase(ClusterHealthState.Critical)]
    [TestCase(ClusterHealthState.Offline)]
    public void ClusterHealthState_AllValuesAreDefined(ClusterHealthState state)
    {
        // Assert - Verify all enum values can be used
        Assert.That(Enum.IsDefined(typeof(ClusterHealthState), state), Is.True);
    }

    [TestCase(JobPriority.Low)]
    [TestCase(JobPriority.Normal)]
    [TestCase(JobPriority.High)]
    [TestCase(JobPriority.Critical)]
    public void JobPriority_AllValuesAreDefined(JobPriority priority)
    {
        // Assert - Verify all enum values can be used
        Assert.That(Enum.IsDefined(typeof(JobPriority), priority), Is.True);
    }

    [TestCase(SubmissionStrategy.BestFit)]
    [TestCase(SubmissionStrategy.LeastLoaded)]
    [TestCase(SubmissionStrategy.RoundRobin)]
    [TestCase(SubmissionStrategy.LocalityFirst)]
    [TestCase(SubmissionStrategy.HighAvailability)]
    public void SubmissionStrategy_AllValuesAreDefined(SubmissionStrategy strategy)
    {
        // Assert - Verify all enum values can be used
        Assert.That(Enum.IsDefined(typeof(SubmissionStrategy), strategy), Is.True);
    }

    [Test]
    public void ClusterStatus_PropertiesAreSetCorrectly()
    {
        // Arrange
        var timestamp = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var status1 = new ClusterStatus
        {
            ClusterId = "cluster-1",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            LastHealthCheck = timestamp,
            Version = "1.18.0"
        };

        var status2 = new ClusterStatus
        {
            ClusterId = "cluster-1",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            LastHealthCheck = timestamp,
            Version = "1.18.0"
        };

        var status3 = new ClusterStatus
        {
            ClusterId = "cluster-2",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            LastHealthCheck = timestamp,
            Version = "1.18.0"
        };

        // Assert - Verify individual properties match
        Assert.That(status1.ClusterId, Is.EqualTo(status2.ClusterId));
        Assert.That(status1.Health, Is.EqualTo(status2.Health));
        Assert.That(status1.AvailableSlots, Is.EqualTo(status2.AvailableSlots));
        Assert.That(status1.TotalSlots, Is.EqualTo(status2.TotalSlots));
        Assert.That(status1.RunningJobs, Is.EqualTo(status2.RunningJobs));
        Assert.That(status1.LastHealthCheck, Is.EqualTo(status2.LastHealthCheck));
        Assert.That(status1.Version, Is.EqualTo(status2.Version));

        // Verify different cluster IDs are not equal
        Assert.That(status1.ClusterId, Is.Not.EqualTo(status3.ClusterId));
    }

    [Test]
    public void ClusterStatus_WithModifier_CreatesNewInstance()
    {
        // Arrange
        var original = new ClusterStatus
        {
            ClusterId = "cluster-1",
            Health = ClusterHealthState.Healthy,
            AvailableSlots = 10
        };

        // Act
        var modified = original with
        {
            Health = ClusterHealthState.Warning
        };

        // Assert
        Assert.That(modified.ClusterId, Is.EqualTo(original.ClusterId));
        Assert.That(modified.Health, Is.EqualTo(ClusterHealthState.Warning));
        Assert.That(original.Health, Is.EqualTo(ClusterHealthState.Healthy));
    }
}
