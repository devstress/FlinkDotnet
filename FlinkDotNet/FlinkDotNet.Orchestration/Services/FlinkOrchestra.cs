using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Models;
using Microsoft.Extensions.Logging;
using ClusterManagerModels = FlinkDotNet.ClusterManager.Models;

namespace FlinkDotNet.Orchestration.Services;

/// <summary>
/// Main orchestration service that manages multiple Flink clusters and distributes jobs.
/// Implements enterprise multi-cluster orchestration patterns.
/// </summary>
public class FlinkOrchestra : IFlinkOrchestra
{
    private readonly ILogger<FlinkOrchestra> _logger;
    private readonly Dictionary<string, IFlinkClusterActor> _clusters = new();

    public FlinkOrchestra(ILogger<FlinkOrchestra> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<JobSubmissionResult> SubmitJobAsync(
        FlinkJobDefinition job, 
        SubmissionStrategy strategy, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting job {JobId} using strategy {Strategy}", job.JobId, strategy);

        var selectedCluster = await SelectClusterAsync(job, strategy, cancellationToken);
        if (selectedCluster == null)
        {
            return new JobSubmissionResult
            {
                JobId = job.JobId,
                Success = false,
                ErrorMessage = "No suitable cluster available for job submission",
                SubmissionTime = DateTime.UtcNow
            };
        }

        return await selectedCluster.SubmitJobAsync(job, cancellationToken);
    }

    public async Task<ClusterInfo[]> GetAvailableClustersAsync(CancellationToken cancellationToken = default)
    {
        var clusterInfos = new List<ClusterInfo>();
        
        foreach (var kvp in _clusters)
        {
            try
            {
                var status = await kvp.Value.GetStatusAsync(cancellationToken);
                clusterInfos.Add(new ClusterInfo
                {
                    ClusterId = kvp.Key,
                    Name = kvp.Key,
                    Status = status,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdateAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get status for cluster {ClusterId}", kvp.Key);
            }
        }

        return clusterInfos.ToArray();
    }

    public async Task<IFlinkClusterActor> ProvisionClusterAsync(
        ClusterConfiguration config, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Provisioning new cluster with name {Name}", config.Name);

        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        var clusterId = $"cluster-{Guid.NewGuid():N}[..8]";
        
        var httpClient = new HttpClient();
        var loggerFactory = Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
        var clusterLogger = loggerFactory.CreateLogger<FlinkDotNet.ClusterManager.Actors.FlinkClusterActor>();
        
        var clusterConfig = new ClusterManagerModels.ClusterConfiguration
        {
            Name = config.Name,
            TaskSlots = config.TaskSlots,
            TaskManagers = config.TaskManagers,
            FlinkVersion = config.FlinkVersion,
            Properties = config.Properties,
            ResourceLimits = new ClusterManagerModels.ResourceLimits
            {
                MaxMemoryMB = config.ResourceLimits.MaxMemoryMB,
                MaxCpuCores = config.ResourceLimits.MaxCpuCores,
                MaxDiskGB = config.ResourceLimits.MaxDiskGB,
                MaxJobs = config.ResourceLimits.MaxJobs
            },
            Region = config.Region,
            Zone = config.Zone,
            HighAvailability = config.HighAvailability
        };

        var clusterActor = new FlinkDotNet.ClusterManager.Actors.FlinkClusterActor(
            clusterId, clusterConfig, httpClient, clusterLogger);

        // Create a bridge to match the Orchestra interface
        var actor = new ClusterActorBridge(clusterActor);

        _clusters[clusterId] = actor;

        _logger.LogInformation("Successfully provisioned cluster {ClusterId}", clusterId);
        return actor;
    }

    public async Task<HealthReport> GetClusterHealthAsync(CancellationToken cancellationToken = default)
    {
        var healthyClusters = 0;
        var warningClusters = 0;
        var criticalClusters = 0;
        var offlineClusters = 0;
        var totalAvailableSlots = 0;
        var totalRunningJobs = 0;
        var issues = new List<ClusterHealthIssue>();

        foreach (var kvp in _clusters)
        {
            try
            {
                var status = await kvp.Value.GetStatusAsync(cancellationToken);
                
                switch (status.Health)
                {
                    case ClusterHealthState.Healthy:
                        healthyClusters++;
                        break;
                    case ClusterHealthState.Warning:
                        warningClusters++;
                        break;
                    case ClusterHealthState.Critical:
                        criticalClusters++;
                        issues.Add(new ClusterHealthIssue
                        {
                            ClusterId = kvp.Key,
                            Issue = "Cluster in critical state",
                            Severity = "Critical",
                            DetectedAt = DateTime.UtcNow
                        });
                        break;
                    case ClusterHealthState.Offline:
                        offlineClusters++;
                        issues.Add(new ClusterHealthIssue
                        {
                            ClusterId = kvp.Key,
                            Issue = "Cluster is offline",
                            Severity = "High",
                            DetectedAt = DateTime.UtcNow
                        });
                        break;
                }

                totalAvailableSlots += status.AvailableSlots;
                totalRunningJobs += status.RunningJobs;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get health status for cluster {ClusterId}", kvp.Key);
                offlineClusters++;
            }
        }

        var totalClusters = _clusters.Count;
        var overallHealthScore = totalClusters > 0 
            ? (double)healthyClusters / totalClusters * 100.0 
            : 0.0;

        return new HealthReport
        {
            TotalClusters = totalClusters,
            HealthyClusters = healthyClusters,
            WarningClusters = warningClusters,
            CriticalClusters = criticalClusters,
            OfflineClusters = offlineClusters,
            TotalAvailableSlots = totalAvailableSlots,
            TotalRunningJobs = totalRunningJobs,
            OverallHealthScore = overallHealthScore,
            GeneratedAt = DateTime.UtcNow,
            Issues = issues
        };
    }

    public async Task<ScalingResult> ScaleOrchestraAsync(
        int targetCapacity, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Scaling orchestra to target capacity {TargetCapacity}", targetCapacity);

        var currentCapacity = _clusters.Count;
        var startTime = DateTime.UtcNow;
        var actions = new List<string>();

        try
        {
            if (targetCapacity > currentCapacity)
            {
                // Scale up - add clusters
                var clustersToAdd = targetCapacity - currentCapacity;
                for (int i = 0; i < clustersToAdd; i++)
                {
                    var config = new ClusterConfiguration
                    {
                        Name = $"auto-scaled-cluster-{DateTime.UtcNow:yyyyMMddHHmmss}-{i}",
                        TaskSlots = 4,
                        TaskManagers = 2
                    };

                    await ProvisionClusterAsync(config, cancellationToken);
                    actions.Add($"Provisioned cluster {config.Name}");
                }
            }
            else if (targetCapacity < currentCapacity)
            {
                // Scale down - remove clusters
                var clustersToRemove = currentCapacity - targetCapacity;
                var clustersToShutdown = _clusters.Keys.Take(clustersToRemove).ToList();
                
                foreach (var clusterId in clustersToShutdown)
                {
                    if (_clusters.TryGetValue(clusterId, out var cluster))
                    {
                        await cluster.ShutdownAsync(cancellationToken);
                        _clusters.Remove(clusterId);
                        actions.Add($"Shutdown cluster {clusterId}");
                    }
                }
            }

            var endTime = DateTime.UtcNow;
            var newCapacity = _clusters.Count;

            return new ScalingResult
            {
                Success = true,
                PreviousCapacity = currentCapacity,
                NewCapacity = newCapacity,
                ClustersAdded = Math.Max(0, newCapacity - currentCapacity),
                ClustersRemoved = Math.Max(0, currentCapacity - newCapacity),
                Duration = endTime - startTime,
                Actions = actions
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to scale orchestra to capacity {TargetCapacity}", targetCapacity);
            
            return new ScalingResult
            {
                Success = false,
                PreviousCapacity = currentCapacity,
                NewCapacity = _clusters.Count,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime,
                Actions = actions
            };
        }
    }

    public async Task<string> StartOrchestrationWorkflowAsync(
        OrchestrationRequest request, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting orchestration workflow for request {RequestId}", request.RequestId);

        var workflowId = $"orchestra-{request.RequestId}-{DateTime.UtcNow:yyyyMMddHHmmss}";
        
        await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        
        _logger.LogInformation("Started orchestration workflow {WorkflowId}", workflowId);
        return workflowId;
    }

    private async Task<IFlinkClusterActor?> SelectClusterAsync(
        FlinkJobDefinition job, 
        SubmissionStrategy strategy, 
        CancellationToken cancellationToken)
    {
        var healthyClusters = new List<(IFlinkClusterActor Actor, ClusterStatus Status)>();

        foreach (var cluster in _clusters.Values)
        {
            try
            {
                var status = await cluster.GetStatusAsync(cancellationToken);
                if (status.Health == ClusterHealthState.Healthy && 
                    status.AvailableSlots >= job.Parallelism)
                {
                    healthyClusters.Add((cluster, status));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get status for cluster {ClusterId}", cluster.ClusterId);
            }
        }

        if (!healthyClusters.Any())
        {
            return null;
        }

        return strategy switch
        {
            SubmissionStrategy.BestFit => healthyClusters
                .OrderBy(c => c.Status.AvailableSlots - job.Parallelism)
                .First().Actor,
            
            SubmissionStrategy.LeastLoaded => healthyClusters
                .OrderBy(c => (double)c.Status.RunningJobs / c.Status.TotalSlots)
                .First().Actor,
            
            SubmissionStrategy.RoundRobin => healthyClusters
                [new Random().Next(healthyClusters.Count)].Actor,
            
            SubmissionStrategy.LocalityFirst => healthyClusters[0].Actor,
            
            SubmissionStrategy.HighAvailability => healthyClusters[0].Actor,
            
            _ => healthyClusters[0].Actor
        };
    }
}
