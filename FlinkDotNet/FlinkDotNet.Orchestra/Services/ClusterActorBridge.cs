using FlinkDotNet.Orchestra.Interfaces;
using FlinkDotNet.Orchestra.Models;
using ClusterManagerModels = FlinkDotNet.ClusterManager.Models;
using ClusterManagerInterfaces = FlinkDotNet.ClusterManager.Interfaces;

namespace FlinkDotNet.Orchestra.Services;

/// <summary>
/// Bridge between Orchestra and ClusterManager interfaces to avoid circular dependencies.
/// </summary>
internal class ClusterActorBridge : IFlinkClusterActor
{
    private readonly ClusterManagerInterfaces.IFlinkClusterActor _clusterActor;

    public ClusterActorBridge(ClusterManagerInterfaces.IFlinkClusterActor clusterActor)
    {
        _clusterActor = clusterActor ?? throw new ArgumentNullException(nameof(clusterActor));
    }

    public string ClusterId => _clusterActor.ClusterId;

    public async Task<ClusterStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        var managerStatus = await _clusterActor.GetStatusAsync(cancellationToken);
        return new ClusterStatus
        {
            ClusterId = managerStatus.ClusterId,
            Health = (ClusterHealthState)(int)managerStatus.Health,
            AvailableSlots = managerStatus.AvailableSlots,
            TotalSlots = managerStatus.TotalSlots,
            RunningJobs = managerStatus.RunningJobs,
            LastHealthCheck = managerStatus.LastHealthCheck,
            Version = managerStatus.Version,
            AdditionalMetrics = managerStatus.AdditionalMetrics
        };
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, CancellationToken cancellationToken = default)
    {
        var managerJob = new ClusterManagerModels.FlinkJobDefinition
        {
            JobId = job.JobId,
            JobName = job.JobName,
            JobGraph = job.JobGraph,
            Parallelism = job.Parallelism,
            Configuration = job.Configuration,
            Priority = (ClusterManagerModels.JobPriority)(int)job.Priority,
            Timeout = job.Timeout,
            RequiredResources = job.RequiredResources,
            ResourceRequirements = new ClusterManagerModels.JobResourceRequirements
            {
                MinSlots = job.ResourceRequirements.MinSlots,
                MaxSlots = job.ResourceRequirements.MaxSlots,
                MemoryMB = job.ResourceRequirements.MemoryMB,
                CpuCores = job.ResourceRequirements.CpuCores,
                AdditionalRequirements = job.ResourceRequirements.AdditionalRequirements
            }
        };

        var managerResult = await _clusterActor.SubmitJobAsync(managerJob, cancellationToken);
        return new JobSubmissionResult
        {
            JobId = managerResult.JobId,
            ClusterId = managerResult.ClusterId,
            Success = managerResult.Success,
            ErrorMessage = managerResult.ErrorMessage,
            SubmissionTime = managerResult.SubmissionTime,
            FlinkJobId = managerResult.FlinkJobId,
            PlacementInfo = new JobPlacementInfo
            {
                ClusterId = managerResult.PlacementInfo.ClusterId,
                Reason = managerResult.PlacementInfo.Reason,
                AssignedSlots = managerResult.PlacementInfo.AssignedSlots,
                Strategy = (SubmissionStrategy)(int)managerResult.PlacementInfo.Strategy,
                PlacementMetadata = managerResult.PlacementInfo.PlacementMetadata
            }
        };
    }

    public async Task<bool> ScaleAsync(int parallelism, CancellationToken cancellationToken = default)
    {
        return await _clusterActor.ScaleAsync(parallelism, cancellationToken);
    }

    public async Task RestartAsync(CancellationToken cancellationToken = default)
    {
        await _clusterActor.RestartAsync(cancellationToken);
    }

    public async Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        await _clusterActor.ShutdownAsync(cancellationToken);
    }

    public async Task StartHealthMonitoringAsync(CancellationToken cancellationToken = default)
    {
        await _clusterActor.StartHealthMonitoringAsync(cancellationToken);
    }

    public async Task<ClusterMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
    {
        var managerMetrics = await _clusterActor.GetMetricsAsync(cancellationToken);
        return new ClusterMetrics
        {
            ClusterId = managerMetrics.ClusterId,
            CpuUtilization = managerMetrics.CpuUtilization,
            MemoryUtilization = managerMetrics.MemoryUtilization,
            ProcessedRecords = managerMetrics.ProcessedRecords,
            Throughput = managerMetrics.Throughput,
            BackpressureRatio = managerMetrics.BackpressureRatio,
            Timestamp = managerMetrics.Timestamp,
            CustomMetrics = managerMetrics.CustomMetrics
        };
    }
}