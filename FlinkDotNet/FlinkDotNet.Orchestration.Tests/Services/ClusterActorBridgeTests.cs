
using FlinkDotNet.Orchestration.Models;
using FlinkDotNet.Orchestration.Services;
using Moq;
using ClusterManagerInterfaces = FlinkDotNet.ClusterManager.Interfaces;
using ClusterManagerModels = FlinkDotNet.ClusterManager.Models;

namespace FlinkDotNet.Orchestration.Tests.Services;
/// <summary>
/// Comprehensive tests for ClusterActorBridge.
/// Tests bridging between Orchestra and ClusterManager interfaces.
/// </summary>
[TestFixture]
public class ClusterActorBridgeTests
{
    private Mock<ClusterManagerInterfaces.IFlinkClusterActor>? _mockClusterActor;
    private ClusterActorBridge? _bridge;

    [SetUp]
    public void SetUp()
    {
        _mockClusterActor = new Mock<ClusterManagerInterfaces.IFlinkClusterActor>();
        _mockClusterActor.Setup(x => x.ClusterId).Returns("test-cluster-123");
        _bridge = new ClusterActorBridge(_mockClusterActor.Object);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_WithValidClusterActor_InitializesSuccessfully()
    {
        // Arrange
        var mockActor = new Mock<ClusterManagerInterfaces.IFlinkClusterActor>();
        mockActor.Setup(x => x.ClusterId).Returns("cluster-1");

        // Act
        var bridge = new ClusterActorBridge(mockActor.Object);

        // Assert
        Assert.That(bridge, Is.Not.Null);
        Assert.That(bridge.ClusterId, Is.EqualTo("cluster-1"));
    }

    [Test]
    public void Constructor_WithNullClusterActor_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClusterActorBridge(null!));
    }

    #endregion

    #region ClusterId Tests

    [Test]
    public void ClusterId_ReturnsUnderlyingClusterId()
    {
        // Assert
        Assert.That(_bridge!.ClusterId, Is.EqualTo("test-cluster-123"));
    }

    #endregion

    #region GetStatusAsync Tests

    [Test]
    public async Task GetStatusAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        var managerStatus = new ClusterManagerModels.ClusterStatus
        {
            ClusterId = "test-cluster",
            Health = ClusterManagerModels.ClusterHealthState.Healthy,
            AvailableSlots = 10,
            TotalSlots = 20,
            RunningJobs = 5,
            Version = "1.18.0"
        };

        _mockClusterActor!.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerStatus);

        // Act
        var status = await _bridge!.GetStatusAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(status.ClusterId, Is.EqualTo("test-cluster"));
            Assert.That(status.Health, Is.EqualTo(ClusterHealthState.Healthy));
            Assert.That(status.AvailableSlots, Is.EqualTo(10));
            Assert.That(status.TotalSlots, Is.EqualTo(20));
            Assert.That(status.RunningJobs, Is.EqualTo(5));
            Assert.That(status.Version, Is.EqualTo("1.18.0"));
        });

        _mockClusterActor.Verify(x => x.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var managerStatus = new ClusterManagerModels.ClusterStatus
        {
            ClusterId = "test-cluster"
        };

        _mockClusterActor!.Setup(x => x.GetStatusAsync(cts.Token))
            .ReturnsAsync(managerStatus);

        // Act
        await _bridge!.GetStatusAsync(cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.GetStatusAsync(cts.Token), Times.Once);
    }

    [Test]
    public async Task GetStatusAsync_MapsAllHealthStates()
    {
        // Test all health state mappings
        var healthStates = new[]
        {
            (ClusterManagerModels.ClusterHealthState.Unknown, ClusterHealthState.Unknown),
            (ClusterManagerModels.ClusterHealthState.Healthy, ClusterHealthState.Healthy),
            (ClusterManagerModels.ClusterHealthState.Warning, ClusterHealthState.Warning),
            (ClusterManagerModels.ClusterHealthState.Critical, ClusterHealthState.Critical),
            (ClusterManagerModels.ClusterHealthState.Offline, ClusterHealthState.Offline)
        };

        foreach (var (managerHealth, orchestraHealth) in healthStates)
        {
            // Arrange
            var managerStatus = new ClusterManagerModels.ClusterStatus
            {
                Health = managerHealth
            };
            _mockClusterActor!.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(managerStatus);

            // Act
            var status = await _bridge!.GetStatusAsync();

            // Assert
            Assert.That(status.Health, Is.EqualTo(orchestraHealth),
                $"Health state {managerHealth} should map to {orchestraHealth}");
        }
    }

    #endregion

    #region SubmitJobAsync Tests

    [Test]
    public async Task SubmitJobAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        var orchestraJob = new FlinkJobDefinition
        {
            JobId = "job-123",
            JobName = "Test Job",
            Parallelism = 4
        };

        var managerResult = new ClusterManagerModels.JobSubmissionResult
        {
            JobId = "job-123",
            ClusterId = "test-cluster",
            Success = true,
            FlinkJobId = "flink-job-123"
        };

        _mockClusterActor!.Setup(x => x.SubmitJobAsync(
            It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerResult);

        // Act
        var result = await _bridge!.SubmitJobAsync(orchestraJob);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(result.JobId, Is.EqualTo("job-123"));
            Assert.That(result.ClusterId, Is.EqualTo("test-cluster"));
            Assert.That(result.Success, Is.True);
            Assert.That(result.FlinkJobId, Is.EqualTo("flink-job-123"));
        });

        _mockClusterActor.Verify(
            x => x.SubmitJobAsync(
                It.Is<ClusterManagerModels.FlinkJobDefinition>(j => j.JobId == "job-123"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task SubmitJobAsync_MapsAllJobPriorities()
    {
        // Test all priority mappings
        var priorities = new[]
        {
            (JobPriority.Low, ClusterManagerModels.JobPriority.Low),
            (JobPriority.Normal, ClusterManagerModels.JobPriority.Normal),
            (JobPriority.High, ClusterManagerModels.JobPriority.High),
            (JobPriority.Critical, ClusterManagerModels.JobPriority.Critical)
        };

        foreach (var (orchestraPriority, managerPriority) in priorities)
        {
            // Arrange
            var orchestraJob = new FlinkJobDefinition
            {
                JobId = $"job-{orchestraPriority}",
                Priority = orchestraPriority
            };

            ClusterManagerModels.FlinkJobDefinition? capturedJob = null;
            _mockClusterActor!.Setup(x => x.SubmitJobAsync(
                It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
                It.IsAny<CancellationToken>()))
                .Callback<ClusterManagerModels.FlinkJobDefinition, CancellationToken>((j, ct) => capturedJob = j)
                .ReturnsAsync(new ClusterManagerModels.JobSubmissionResult());

            // Act
            await _bridge!.SubmitJobAsync(orchestraJob);

            // Assert
            Assert.That(capturedJob!.Priority, Is.EqualTo(managerPriority),
                $"Priority {orchestraPriority} should map to {managerPriority}");
        }
    }

    [Test]
    public async Task SubmitJobAsync_MapsResourceRequirements()
    {
        // Arrange
        var orchestraJob = new FlinkJobDefinition
        {
            JobId = "resource-test",
            ResourceRequirements = new JobResourceRequirements
            {
                MinSlots = 2,
                MaxSlots = 10,
                MemoryMB = 2048,
                CpuCores = 4.0
            }
        };

        ClusterManagerModels.FlinkJobDefinition? capturedJob = null;
        _mockClusterActor!.Setup(x => x.SubmitJobAsync(
            It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
            It.IsAny<CancellationToken>()))
            .Callback<ClusterManagerModels.FlinkJobDefinition, CancellationToken>((j, ct) => capturedJob = j)
            .ReturnsAsync(new ClusterManagerModels.JobSubmissionResult());

        // Act
        await _bridge!.SubmitJobAsync(orchestraJob);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(capturedJob!.ResourceRequirements.MinSlots, Is.EqualTo(2));
            Assert.That(capturedJob.ResourceRequirements.MaxSlots, Is.EqualTo(10));
            Assert.That(capturedJob.ResourceRequirements.MemoryMB, Is.EqualTo(2048));
            Assert.That(capturedJob.ResourceRequirements.CpuCores, Is.EqualTo(4.0));
        });
    }

    [Test]
    public async Task SubmitJobAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var job = new FlinkJobDefinition { JobId = "cancel-test" };
        var cts = new CancellationTokenSource();

        _mockClusterActor!.Setup(x => x.SubmitJobAsync(
            It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
            cts.Token))
            .ReturnsAsync(new ClusterManagerModels.JobSubmissionResult());

        // Act
        await _bridge!.SubmitJobAsync(job, cts.Token);

        // Assert
        _mockClusterActor.Verify(
            x => x.SubmitJobAsync(
                It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
                cts.Token),
            Times.Once);
    }

    #endregion

    #region ScaleAsync Tests

    [Test]
    public async Task ScaleAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.ScaleAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _bridge!.ScaleAsync(8);

        // Assert
        Assert.That(result, Is.True);
        _mockClusterActor.Verify(x => x.ScaleAsync(8, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ScaleAsync_WithFailure_ReturnsFalse()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.ScaleAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _bridge!.ScaleAsync(4);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task ScaleAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockClusterActor!.Setup(x => x.ScaleAsync(5, cts.Token))
            .ReturnsAsync(true);

        // Act
        await _bridge!.ScaleAsync(5, cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.ScaleAsync(5, cts.Token), Times.Once);
    }

    #endregion

    #region RestartAsync Tests

    [Test]
    public async Task RestartAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.RestartAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.RestartAsync();

        // Assert
        _mockClusterActor.Verify(x => x.RestartAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task RestartAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockClusterActor!.Setup(x => x.RestartAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.RestartAsync(cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.RestartAsync(cts.Token), Times.Once);
    }

    #endregion

    #region ShutdownAsync Tests

    [Test]
    public async Task ShutdownAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.ShutdownAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.ShutdownAsync();

        // Assert
        _mockClusterActor.Verify(x => x.ShutdownAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task ShutdownAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockClusterActor!.Setup(x => x.ShutdownAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.ShutdownAsync(cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.ShutdownAsync(cts.Token), Times.Once);
    }

    #endregion

    #region StartHealthMonitoringAsync Tests

    [Test]
    public async Task StartHealthMonitoringAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.StartHealthMonitoringAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.StartHealthMonitoringAsync();

        // Assert
        _mockClusterActor.Verify(
            x => x.StartHealthMonitoringAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task StartHealthMonitoringAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        _mockClusterActor!.Setup(x => x.StartHealthMonitoringAsync(cts.Token))
            .Returns(Task.CompletedTask);

        // Act
        await _bridge!.StartHealthMonitoringAsync(cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.StartHealthMonitoringAsync(cts.Token), Times.Once);
    }

    #endregion

    #region GetMetricsAsync Tests

    [Test]
    public async Task GetMetricsAsync_DelegatesToUnderlyingActor()
    {
        // Arrange
        var managerMetrics = new ClusterManagerModels.ClusterMetrics
        {
            ClusterId = "test-cluster",
            CpuUtilization = 0.75,
            MemoryUtilization = 0.60,
            ProcessedRecords = 1000000,
            Throughput = 5000.0,
            BackpressureRatio = 0.1
        };

        _mockClusterActor!.Setup(x => x.GetMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerMetrics);

        // Act
        var metrics = await _bridge!.GetMetricsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metrics.ClusterId, Is.EqualTo("test-cluster"));
            Assert.That(metrics.CpuUtilization, Is.EqualTo(0.75));
            Assert.That(metrics.MemoryUtilization, Is.EqualTo(0.60));
            Assert.That(metrics.ProcessedRecords, Is.EqualTo(1000000));
            Assert.That(metrics.Throughput, Is.EqualTo(5000.0));
            Assert.That(metrics.BackpressureRatio, Is.EqualTo(0.1));
        });

        _mockClusterActor.Verify(x => x.GetMetricsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task GetMetricsAsync_WithCancellationToken_PassesToUnderlyingActor()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        var managerMetrics = new ClusterManagerModels.ClusterMetrics();

        _mockClusterActor!.Setup(x => x.GetMetricsAsync(cts.Token))
            .ReturnsAsync(managerMetrics);

        // Act
        await _bridge!.GetMetricsAsync(cts.Token);

        // Assert
        _mockClusterActor.Verify(x => x.GetMetricsAsync(cts.Token), Times.Once);
    }

    [Test]
    public async Task GetMetricsAsync_MapsCustomMetrics()
    {
        // Arrange
        var customMetrics = new Dictionary<string, double>
        {
            { "latency", 45.5 },
            { "throughput_peak", 10000.0 }
        };

        var managerMetrics = new ClusterManagerModels.ClusterMetrics
        {
            CustomMetrics = customMetrics
        };

        _mockClusterActor!.Setup(x => x.GetMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(managerMetrics);

        // Act
        var metrics = await _bridge!.GetMetricsAsync();

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(metrics.CustomMetrics, Is.EqualTo(customMetrics));
            Assert.That(metrics.CustomMetrics.Count, Is.EqualTo(2));
            Assert.That(metrics.CustomMetrics["latency"], Is.EqualTo(45.5));
        });
    }

    #endregion

    #region Error Propagation Tests

    [Test]
    public void GetStatusAsync_WhenUnderlyingActorThrows_PropagatesException()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cluster unavailable"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _bridge!.GetStatusAsync());
        Assert.That(ex!.Message, Does.Contain("Cluster unavailable"));
    }

    [Test]
    public void SubmitJobAsync_WhenUnderlyingActorThrows_PropagatesException()
    {
        // Arrange
        var job = new FlinkJobDefinition { JobId = "error-test" };
        _mockClusterActor!.Setup(x => x.SubmitJobAsync(
            It.IsAny<ClusterManagerModels.FlinkJobDefinition>(),
            It.IsAny<CancellationToken>()))
            .ThrowsAsync(new TimeoutException("Job submission timeout"));

        // Act & Assert
        var ex = Assert.ThrowsAsync<TimeoutException>(
            async () => await _bridge!.SubmitJobAsync(job));
        Assert.That(ex!.Message, Does.Contain("timeout"));
    }

    [Test]
    public void ScaleAsync_WhenUnderlyingActorThrows_PropagatesException()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.ScaleAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot scale"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _bridge!.ScaleAsync(5));
    }

    [Test]
    public void RestartAsync_WhenUnderlyingActorThrows_PropagatesException()
    {
        // Arrange
        _mockClusterActor!.Setup(x => x.RestartAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cannot restart"));

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _bridge!.RestartAsync());
    }

    #endregion

    #region Multiple Calls Tests

    [Test]
    public async Task GetStatusAsync_MultipleCalls_EachCallInvokesUnderlyingActor()
    {
        // Arrange
        var status = new ClusterManagerModels.ClusterStatus();
        _mockClusterActor!.Setup(x => x.GetStatusAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        // Act
        await _bridge!.GetStatusAsync();
        await _bridge!.GetStatusAsync();
        await _bridge!.GetStatusAsync();

        // Assert
        _mockClusterActor.Verify(x => x.GetStatusAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Test]
    public async Task GetMetricsAsync_MultipleCalls_EachCallInvokesUnderlyingActor()
    {
        // Arrange
        var metrics = new ClusterManagerModels.ClusterMetrics();
        _mockClusterActor!.Setup(x => x.GetMetricsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(metrics);

        // Act
        await _bridge!.GetMetricsAsync();
        await _bridge!.GetMetricsAsync();

        // Assert
        _mockClusterActor.Verify(x => x.GetMetricsAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion
}
