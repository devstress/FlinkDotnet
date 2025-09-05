using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Models;
using Microsoft.Extensions.Hosting;

namespace LocalTesting.WebApi.Services
{
    /// <summary>
    /// Background service for non-blocking Orchestra initialization
    /// </summary>
    public class OrchestraInitializationService : BackgroundService
    {
        private readonly ILogger<OrchestraInitializationService> _logger;
        private readonly IServiceProvider _serviceProvider;
        private bool _isInitialized = false;
        private readonly object _initializationLock = new();

        public OrchestraInitializationService(
            ILogger<OrchestraInitializationService> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public bool IsInitialized
        {
            get
            {
                lock (_initializationLock)
                {
                    return _isInitialized;
                }
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Starting Orchestra initialization in background...");

            try
            {
                // Wait a bit to ensure the application has started
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);

                await InitializeOrchestraForLocalTestingAsync(stoppingToken);

                lock (_initializationLock)
                {
                    _isInitialized = true;
                }

                _logger.LogInformation("Orchestra initialization completed successfully");
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Orchestra initialization was cancelled during shutdown");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Orchestra initialization failed, but application will continue");
                // Don't set _isInitialized to true, but don't crash either
            }
        }

        private async Task InitializeOrchestraForLocalTestingAsync(CancellationToken cancellationToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var services = scope.ServiceProvider;

            try
            {
                var logger = services.GetRequiredService<ILogger<OrchestraInitializationService>>();
                var orchestra = services.GetRequiredService<IFlinkOrchestra>();

                logger.LogInformation("Initializing Orchestra with test clusters for LocalTesting...");

                // Create test cluster configurations for LocalTesting
                var testClusters = new[]
                {
                    new { 
                        Config = new FlinkDotNet.Orchestration.Models.ClusterConfiguration
                        {
                            Name = "localtesting-cluster-1",
                            TaskSlots = 10,
                            TaskManagers = 2,
                            Region = "local-testing", 
                            Zone = "zone-a",
                            HighAvailability = true,
                            FlinkVersion = "2.0.0"
                        },
                        AvailableSlots = 20,
                        TotalSlots = 20
                    },
                    new {
                        Config = new FlinkDotNet.Orchestration.Models.ClusterConfiguration
                        {
                            Name = "localtesting-cluster-2",
                            TaskSlots = 8,
                            TaskManagers = 2,
                            Region = "local-testing",
                            Zone = "zone-b", 
                            HighAvailability = true,
                            FlinkVersion = "2.0.0"
                        },
                        AvailableSlots = 16,
                        TotalSlots = 16
                    },
                    new {
                        Config = new FlinkDotNet.Orchestration.Models.ClusterConfiguration
                        {
                            Name = "localtesting-cluster-3",
                            TaskSlots = 6,
                            TaskManagers = 1,
                            Region = "local-testing",
                            Zone = "zone-c",
                            HighAvailability = false,
                            FlinkVersion = "2.0.0"
                        },
                        AvailableSlots = 6,
                        TotalSlots = 6
                    }
                };

                // Provision simulated clusters for testing with timeout
                foreach (var testCluster in testClusters)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        using var clusterTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                        clusterTimeout.CancelAfter(TimeSpan.FromSeconds(10)); // 10 second timeout per cluster

                        await Task.Run(() => CreateSimulatedCluster(orchestra, testCluster, logger), clusterTimeout.Token);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        logger.LogInformation("Orchestra initialization cancelled during cluster creation");
                        break;
                    }
                    catch (OperationCanceledException)
                    {
                        logger.LogWarning("Cluster creation timed out for {ClusterName}, continuing with other clusters", 
                            testCluster.Config.Name);
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to create simulated test cluster {ClusterName}, continuing with other clusters", 
                            testCluster.Config.Name);
                    }
                }

                // Verify clusters are available with timeout
                using var verifyTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                verifyTimeout.CancelAfter(TimeSpan.FromSeconds(5));

                try
                {
                    var availableClusters = await orchestra.GetAvailableClustersAsync();
                    logger.LogInformation("Orchestra initialization completed. Available clusters: {ClusterCount}", 
                        availableClusters.Length);
                }
                catch (OperationCanceledException)
                {
                    logger.LogWarning("Orchestra cluster verification timed out");
                }
            }
            catch (Exception ex)
            {
                var logger = services.GetRequiredService<ILogger<OrchestraInitializationService>>();
                logger.LogError(ex, "Failed to initialize Orchestra with test clusters");
                throw; // Re-throw to be caught by ExecuteAsync
            }
        }

        private static void CreateSimulatedCluster(IFlinkOrchestra orchestra, dynamic testCluster, ILogger<OrchestraInitializationService> logger)
        {
            // Create a simulated cluster actor for LocalTesting
            var simulatedActor = new SimulatedClusterActor(
                $"sim-cluster-{Guid.NewGuid():N}"[..8],
                testCluster.Config.Name,
                testCluster.AvailableSlots,
                testCluster.TotalSlots
            );

            // Add directly to orchestra's internal clusters dictionary
            // Using reflection to access private field for LocalTesting
            var clustersField = orchestra.GetType().GetField("_clusters", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (clustersField?.GetValue(orchestra) is IDictionary<string, IFlinkClusterActor> clusters)
            {
                clusters[simulatedActor.ClusterId] = simulatedActor;
                logger.LogInformation("Added simulated test cluster: {ClusterName} with {AvailableSlots}/{TotalSlots} slots",
                    (string)testCluster.Config.Name, (int)testCluster.AvailableSlots, (int)testCluster.TotalSlots);
            }
        }
    }

    /// <summary>
    /// Simulated cluster actor for LocalTesting environment
    /// </summary>
    internal class SimulatedClusterActor : IFlinkClusterActor
    {
        public string ClusterId { get; }
        private readonly string _clusterName;
        private readonly int _availableSlots;
        private readonly int _totalSlots;

        public SimulatedClusterActor(string clusterId, string clusterName, int availableSlots, int totalSlots)
        {
            ClusterId = clusterId;
            _clusterName = clusterName;
            _availableSlots = availableSlots;
            _totalSlots = totalSlots;
        }

        public Task<ClusterStatus> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ClusterStatus
            {
                ClusterId = ClusterId,
                Health = ClusterHealthState.Healthy, // Always healthy for simulation
                AvailableSlots = _availableSlots,
                TotalSlots = _totalSlots,
                RunningJobs = 0,
                LastHealthCheck = DateTime.UtcNow,
                Version = "2.0.0-simulated",
                AdditionalMetrics = new Dictionary<string, object>
                {
                    ["ClusterName"] = _clusterName,
                    ["Environment"] = "LocalTesting-Simulation"
                }
            });
        }

        public Task<JobSubmissionResult> SubmitJobAsync(FlinkJobDefinition job, CancellationToken cancellationToken = default)
        {
            // Simulate successful job submission
            return Task.FromResult(new JobSubmissionResult
            {
                JobId = job.JobId,
                ClusterId = ClusterId,
                Success = true,
                FlinkJobId = $"flink-job-{Guid.NewGuid():N}"[..8],
                SubmissionTime = DateTime.UtcNow,
                PlacementInfo = new JobPlacementInfo
                {
                    ClusterId = ClusterId,
                    Reason = $"Simulated job placement on {_clusterName}",
                    AssignedSlots = job.Parallelism,
                    Strategy = SubmissionStrategy.BestFit,
                    PlacementMetadata = new Dictionary<string, object>
                    {
                        ["SimulatedCluster"] = _clusterName,
                        ["Environment"] = "LocalTesting"
                    }
                }
            });
        }

        public Task<bool> ScaleAsync(int parallelism, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true); // Simulate successful scaling
        }

        public Task RestartAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask; // Simulate successful restart
        }

        public Task ShutdownAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask; // Simulate successful shutdown
        }

        public Task StartHealthMonitoringAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask; // Simulate health monitoring start
        }

        public Task<ClusterMetrics> GetMetricsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ClusterMetrics
            {
                ClusterId = ClusterId,
                CpuUtilization = 0.65, // Simulate 65% CPU usage
                MemoryUtilization = 0.72, // Simulate 72% memory usage
                ProcessedRecords = 150000,
                Throughput = 5000.0,
                BackpressureRatio = 0.05,
                Timestamp = DateTime.UtcNow,
                CustomMetrics = new Dictionary<string, double>
                {
                    ["AvailableSlots"] = _availableSlots,
                    ["TotalSlots"] = _totalSlots,
                    ["UtilizationPercentage"] = (_totalSlots - _availableSlots) / (double)_totalSlots * 100.0
                }
            });
        }
    }
}