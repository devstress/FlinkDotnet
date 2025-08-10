using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Models;

namespace LocalTesting.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class TemporalArchitectureTestController : ControllerBase
{
    private readonly ILogger<TemporalArchitectureTestController> _logger;
    private readonly IFlinkOrchestra _flinkOrchestra;

    public TemporalArchitectureTestController(
        ILogger<TemporalArchitectureTestController> logger,
        IFlinkOrchestra flinkOrchestra)
    {
        _logger = logger;
        _flinkOrchestra = flinkOrchestra;
    }

    // ========== Multi-Cluster FlinkDotNet.Orchestration Testing ==========

    [HttpPost("orchestra/submit-job")]
    [SwaggerOperation(
        Summary = "Test FlinkDotNet.Orchestration Job Submission with Intelligent Placement",
        Description = "Submit a job to the FlinkDotNet.Orchestration using various placement strategies (BestFit, LeastLoaded, RoundRobin, LocalityFirst)"
    )]
    [SwaggerResponse(200, "Job submitted successfully")]
    [SwaggerResponse(400, "Invalid job submission")]
    [SwaggerResponse(503, "FlinkDotNet.Orchestration service unavailable")]
    public async Task<IActionResult> SubmitJobToOrchestra([FromBody] JobSubmissionRequest request)
    {
        try
        {
            _logger.LogInformation("Submitting job to FlinkDotNet.Orchestration with strategy: {Strategy}", request.Strategy);

            // Create FlinkJobDefinition from request
            var jobDefinition = new FlinkJobDefinition
            {
                JobId = $"test-job-{Guid.NewGuid():N}",
                JobName = request.JobName ?? "LocalTesting Job",
                JobGraph = request.JobGraph ?? "{ \"vertices\": [], \"edges\": [] }", // Empty job graph for testing
                Parallelism = 4,
                Priority = JobPriority.Normal,
                ResourceRequirements = new JobResourceRequirements
                {
                    CpuCores = request.CpuCores ?? 2,
                    MemoryMB = request.MemoryMb ?? 1024
                }
            };

            // Parse strategy enum
            if (!Enum.TryParse<SubmissionStrategy>(request.Strategy, true, out var strategy))
            {
                strategy = SubmissionStrategy.BestFit;
            }

            // Submit job using real orchestration service
            var result = await _flinkOrchestra.SubmitJobAsync(jobDefinition, strategy, HttpContext.RequestAborted);

            if (result.Success)
            {
                return Ok(new
                {
                    Status = "Job submitted successfully",
                    JobId = result.JobId,
                    ClusterId = result.ClusterId,
                    FlinkJobId = result.FlinkJobId,
                    Strategy = strategy.ToString(),
                    SubmissionTime = result.SubmissionTime,
                    PlacementInfo = result.PlacementInfo,
                    Message = $"Job placed using {strategy} strategy"
                });
            }
            else
            {
                return StatusCode(503, new
                {
                    Status = "Job submission failed",
                    JobId = result.JobId,
                    Error = result.ErrorMessage,
                    Message = "FlinkDotNet.Orchestration could not place the job"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting job to FlinkDotNet.Orchestration");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message,
                Message = "FlinkDotNet.Orchestration job submission failed"
            });
        }
    }

    [HttpGet("orchestra/clusters")]
    [SwaggerOperation(
        Summary = "Get Available Clusters from FlinkDotNet.Orchestration",
        Description = "Retrieve list of all clusters registered with the FlinkDotNet.Orchestration and their health status"
    )]
    [SwaggerResponse(200, "Cluster list retrieved successfully")]
    [SwaggerResponse(503, "FlinkDotNet.Orchestration service unavailable")]
    public async Task<IActionResult> GetAvailableClusters()
    {
        try
        {
            _logger.LogInformation("Retrieving available clusters from FlinkDotNet.Orchestration");

            // Get clusters using real orchestration service
            var clusters = await _flinkOrchestra.GetAvailableClustersAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                Status = "Clusters retrieved successfully",
                ClusterCount = clusters.Length,
                Clusters = clusters.Select(c => new
                {
                    c.ClusterId,
                    c.Name,
                    Health = c.Status.Health.ToString(),
                    AvailableSlots = c.Status.AvailableSlots,
                    TotalSlots = c.Status.TotalSlots,
                    UsedSlots = c.Status.TotalSlots - c.Status.AvailableSlots,
                    UtilizationPercentage = c.Status.TotalSlots > 0 
                        ? (double)(c.Status.TotalSlots - c.Status.AvailableSlots) / c.Status.TotalSlots * 100 
                        : 0,
                    RunningJobs = c.Status.RunningJobs,
                    LastHealthCheck = c.Status.LastHealthCheck,
                    Region = c.Region,
                    Zone = c.Zone,
                    CreatedAt = c.CreatedAt
                }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving clusters from FlinkDotNet.Orchestration");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpGet("orchestra/health")]
    [SwaggerOperation(
        Summary = "Get FlinkDotNet.Orchestration Health Report",
        Description = "Retrieve comprehensive health report for all clusters in the orchestration"
    )]
    [SwaggerResponse(200, "Health report retrieved successfully")]
    [SwaggerResponse(503, "FlinkDotNet.Orchestration service unavailable")]
    public async Task<IActionResult> GetOrchestraHealthReport()
    {
        try
        {
            _logger.LogInformation("Retrieving orchestra health report");

            var healthReport = await _flinkOrchestra.GetClusterHealthAsync(HttpContext.RequestAborted);

            return Ok(new
            {
                Status = "Health report retrieved successfully",
                OverallHealthScore = healthReport.OverallHealthScore,
                Summary = new
                {
                    TotalClusters = healthReport.TotalClusters,
                    HealthyClusters = healthReport.HealthyClusters,
                    WarningClusters = healthReport.WarningClusters,
                    CriticalClusters = healthReport.CriticalClusters,
                    OfflineClusters = healthReport.OfflineClusters
                },
                Resources = new
                {
                    TotalAvailableSlots = healthReport.TotalAvailableSlots,
                    TotalRunningJobs = healthReport.TotalRunningJobs
                },
                GeneratedAt = healthReport.GeneratedAt,
                Issues = healthReport.Issues.Select(issue => new
                {
                    issue.ClusterId,
                    issue.Issue,
                    issue.Severity,
                    issue.DetectedAt,
                    issue.Resolution
                }).ToArray()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving orchestra health report");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpPost("orchestra/provision-cluster")]
    [SwaggerOperation(
        Summary = "Provision New Cluster in Orchestra",
        Description = "Provision a new Flink cluster with specified configuration and add it to the orchestra"
    )]
    [SwaggerResponse(200, "Cluster provisioned successfully")]
    [SwaggerResponse(400, "Invalid cluster configuration")]
    [SwaggerResponse(503, "FlinkDotNet.Orchestration service unavailable")]
    public async Task<IActionResult> ProvisionCluster([FromBody] CreateClusterActorRequest request)
    {
        try
        {
            _logger.LogInformation("Provisioning new cluster: {ClusterId}", request.ClusterId);

            var clusterConfig = new ClusterConfiguration
            {
                Name = request.ClusterId ?? $"cluster-{Guid.NewGuid():N}",
                TaskSlots = request.SlotsPerTaskManager ?? 4,
                TaskManagers = request.TaskManagerCount ?? 1,
                Region = "local-testing",
                Zone = "default",
                HighAvailability = true
            };

            var clusterActor = await _flinkOrchestra.ProvisionClusterAsync(clusterConfig, HttpContext.RequestAborted);

            return Ok(new
            {
                Status = "Cluster provisioned successfully",
                ClusterId = clusterActor.ClusterId,
                Configuration = clusterConfig,
                Message = "Cluster actor is now monitoring cluster health"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error provisioning cluster");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    // ========== Cluster Actor Testing ==========

    [HttpPost("actor/create-cluster")]
    [SwaggerOperation(
        Summary = "Create and Test Cluster Actor",
        Description = "Create a new FlinkClusterActor and test its lifecycle management capabilities"
    )]
    [SwaggerResponse(200, "Cluster actor created successfully")]
    [SwaggerResponse(400, "Invalid cluster configuration")]
    public async Task<IActionResult> CreateClusterActor([FromBody] CreateClusterActorRequest request)
    {
        try
        {
            var clusterId = request.ClusterId ?? $"test-cluster-{Guid.NewGuid():N}";
            
            await Task.Delay(200); // Simulate actor creation

            _logger.LogInformation("Simulated creation of cluster actor for cluster: {ClusterId}", clusterId);

            return Ok(new
            {
                Status = "Cluster actor created successfully (simulated)",
                ClusterId = clusterId,
                Configuration = new
                {
                    JobManagerUrl = request.JobManagerUrl ?? "http://localhost:8081",
                    TaskManagerCount = request.TaskManagerCount ?? 1,
                    SlotsPerTaskManager = request.SlotsPerTaskManager ?? 4,
                    HealthCheckIntervalSeconds = request.HealthCheckIntervalSeconds ?? 30
                },
                Message = "Actor is now monitoring cluster health (simulation)",
                ActorCount = Random.Shared.Next(1, 10)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating cluster actor creation");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpGet("actor/health-status")]
    [SwaggerOperation(
        Summary = "Get Cluster Actor Health Status",
        Description = "Retrieve health status from all active cluster actors"
    )]
    [SwaggerResponse(200, "Health status retrieved successfully")]
    public async Task<IActionResult> GetClusterActorHealthStatus()
    {
        try
        {
            await Task.Delay(100); // Simulate health check

            var actorCount = Random.Shared.Next(3, 15);
            var healthStatuses = Enumerable.Range(1, actorCount).Select(i => new
            {
                ClusterId = $"cluster-actor-{i}",
                Health = Random.Shared.Next(0, 100) > 15 ? "Healthy" : "Warning",
                AvailableSlots = Random.Shared.Next(20, 100),
                UsedSlots = Random.Shared.Next(0, 30),
                RunningJobs = Random.Shared.Next(0, 10),
                LastHealthCheck = DateTime.UtcNow.AddMinutes(-Random.Shared.Next(0, 10)),
                UptimeMinutes = Random.Shared.Next(10, 1440)
            }).ToList();

            return Ok(new
            {
                Status = "Health status retrieved successfully (simulated)",
                TotalActors = actorCount,
                HealthyActors = healthStatuses.Count(h => h.Health == "Healthy"),
                Actors = healthStatuses
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating cluster actor health status retrieval");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    // ========== Temporal Workflow Testing ==========

    [HttpPost("temporal/start-orchestration")]
    [SwaggerOperation(
        Summary = "Start Temporal FlinkDotNet.Orchestration Workflow",
        Description = "Start a Temporal workflow for cluster orchestration and auto-scaling using real Temporal workflows"
    )]
    [SwaggerResponse(200, "Workflow started successfully")]
    [SwaggerResponse(400, "Invalid workflow configuration")]
    public async Task<IActionResult> StartOrchestrationWorkflow([FromBody] OrchestrationWorkflowRequest request)
    {
        try
        {
            _logger.LogInformation("Starting Temporal FlinkDotNet.Orchestration workflow for {TargetClusters} clusters", request.TargetClusters);

            // Create OrchestrationRequest from the workflow request
            var orchestrationRequest = new OrchestrationRequest
            {
                RequestId = $"workflow-{Guid.NewGuid():N}",
                TargetClusters = request.TargetClusters,
                MinClusters = request.MinClusters,
                MaxClusters = request.MaxClusters,
                DefaultClusterConfig = new ClusterConfiguration
                {
                    Name = "default-cluster",
                    TaskSlots = 4,
                    TaskManagers = 2,
                    Region = "local-testing",
                    Zone = "default",
                    HighAvailability = true
                }
            };

            // Start orchestration workflow using real Temporal service
            var workflowId = await _flinkOrchestra.StartOrchestrationWorkflowAsync(orchestrationRequest, HttpContext.RequestAborted);

            return Ok(new
            {
                Status = "Temporal workflow started successfully",
                WorkflowId = workflowId,
                request.TargetClusters,
                request.MinClusters,
                request.MaxClusters,
                StartTime = DateTime.UtcNow,
                OrchestrationRequest = orchestrationRequest,
                Message = "Real Temporal workflow for FlinkDotNet.Orchestration cluster orchestration is now running"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Temporal FlinkDotNet.Orchestration workflow");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpPost("temporal/start-job-distribution")]
    [SwaggerOperation(
        Summary = "Start Temporal Job Distribution Workflow", 
        Description = "Start a Temporal workflow for intelligent job distribution across multiple clusters"
    )]
    [SwaggerResponse(200, "Job distribution workflow started successfully")]
    [SwaggerResponse(400, "Invalid job distribution configuration")]
    public async Task<IActionResult> StartJobDistributionWorkflow([FromBody] JobDistributionWorkflowRequest request)
    {
        try
        {
            _logger.LogInformation("Starting Temporal job distribution workflow for {JobCount} jobs using {Strategy} strategy", 
                request.Jobs.Count, request.Strategy);

            // Create job definitions from request
            var jobDefinitions = request.Jobs.Select((job, index) => new FlinkJobDefinition
            {
                JobId = job.JobId ?? $"job-{Guid.NewGuid():N}",
                JobName = job.JobName ?? $"Job {index + 1}",
                JobGraph = job.JobGraph ?? "{ \"vertices\": [], \"edges\": [] }",
                Parallelism = job.Parallelism ?? 4,
                Priority = JobPriority.Normal,
                ResourceRequirements = new JobResourceRequirements
                {
                    CpuCores = job.CpuCores ?? 2,
                    MemoryMB = job.MemoryMb ?? 1024
                }
            }).ToList();

            // Parse strategy enum
            if (!Enum.TryParse<SubmissionStrategy>(request.Strategy, true, out var strategy))
            {
                strategy = SubmissionStrategy.BestFit;
            }

            // Get the enhanced Temporal service (requires dependency injection enhancement)
            var temporalService = HttpContext.RequestServices.GetService<LocalTesting.WebApi.Services.Temporal.TemporalSecurityTokenService>();
            if (temporalService == null)
            {
                return StatusCode(500, new
                {
                    Status = "Service unavailable",
                    Error = "Temporal service not available"
                });
            }

            // Start job distribution workflow using enhanced Temporal service
            var workflowId = await temporalService.StartJobDistributionWorkflowAsync(jobDefinitions, strategy);

            return Ok(new
            {
                Status = "Temporal job distribution workflow started successfully",
                WorkflowId = workflowId,
                JobCount = jobDefinitions.Count,
                Strategy = strategy.ToString(),
                StartTime = DateTime.UtcNow,
                Jobs = jobDefinitions.Select(j => new { j.JobId, j.JobName, j.Parallelism }).ToArray(),
                Message = "Real Temporal workflow for intelligent job distribution is now running"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting Temporal job distribution workflow");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpGet("temporal/workflow-status/{workflowId}")]
    [SwaggerOperation(
        Summary = "Get Temporal Workflow Status",
        Description = "Get the current status and execution details of a Temporal workflow"
    )]
    [SwaggerResponse(200, "Workflow status retrieved successfully")]
    [SwaggerResponse(404, "Workflow not found")]
    public async Task<IActionResult> GetWorkflowStatus(string workflowId)
    {
        try
        {
            _logger.LogInformation("Retrieving Temporal workflow status for {WorkflowId}", workflowId);

            // Get the enhanced Temporal service
            var temporalService = HttpContext.RequestServices.GetService<LocalTesting.WebApi.Services.Temporal.TemporalSecurityTokenService>();
            if (temporalService == null)
            {
                return StatusCode(500, new
                {
                    Status = "Service unavailable",
                    Error = "Temporal service not available"
                });
            }

            var workflowStatus = await temporalService.GetWorkflowStatusAsync(workflowId);

            return Ok(new
            {
                Status = "Workflow status retrieved successfully",
                WorkflowId = workflowId,
                WorkflowStatus = workflowStatus.Status,
                StartTime = workflowStatus.StartTime,
                CloseTime = workflowStatus.CloseTime,
                RunId = workflowStatus.RunId,
                IsRunning = workflowStatus.CloseTime == null,
                Duration = workflowStatus.CloseTime?.Subtract(workflowStatus.StartTime) ?? 
                          DateTime.UtcNow.Subtract(workflowStatus.StartTime)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving Temporal workflow status for {WorkflowId}", workflowId);
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    [HttpPost("temporal/cancel-workflow/{workflowId}")]
    [SwaggerOperation(
        Summary = "Cancel Temporal Workflow",
        Description = "Cancel a running Temporal workflow gracefully"
    )]
    [SwaggerResponse(200, "Workflow cancelled successfully")]
    [SwaggerResponse(404, "Workflow not found")]
    public async Task<IActionResult> CancelWorkflow(string workflowId, [FromBody] CancelWorkflowRequest? request = null)
    {
        try
        {
            _logger.LogInformation("Cancelling Temporal workflow {WorkflowId}", workflowId);

            // Get the enhanced Temporal service
            var temporalService = HttpContext.RequestServices.GetService<LocalTesting.WebApi.Services.Temporal.TemporalSecurityTokenService>();
            if (temporalService == null)
            {
                return StatusCode(500, new
                {
                    Status = "Service unavailable",
                    Error = "Temporal service not available"
                });
            }

            var reason = request?.Reason ?? "Cancelled by user via API";
            var cancelled = await temporalService.CancelWorkflowAsync(workflowId, reason);

            if (cancelled)
            {
                return Ok(new
                {
                    Status = "Workflow cancelled successfully",
                    WorkflowId = workflowId,
                    Reason = reason,
                    CancelledAt = DateTime.UtcNow
                });
            }
            else
            {
                return StatusCode(500, new
                {
                    Status = "Failed to cancel workflow",
                    WorkflowId = workflowId,
                    Error = "Workflow cancellation failed"
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling Temporal workflow {WorkflowId}", workflowId);
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    // ========== Resilience Pattern Testing ==========

    [HttpPost("resilience/test-circuit-breaker")]
    [SwaggerOperation(
        Summary = "Test Circuit Breaker Pattern",
        Description = "Test circuit breaker activation and recovery under failure conditions"
    )]
    [SwaggerResponse(200, "Circuit breaker test completed")]
    [SwaggerResponse(400, "Invalid test configuration")]
    public async Task<IActionResult> TestCircuitBreaker([FromBody] CircuitBreakerTestRequest request)
    {
        try
        {
            _logger.LogInformation("Simulating circuit breaker test with {FailureRate}% failure rate", request.FailureRate);

            await Task.Delay(500); // Simulate circuit breaker test

            var testResults = new
            {
                Status = "Circuit breaker test completed (simulated)",
                TestDurationSeconds = request.TestDurationSeconds ?? 30,
                FailureRate = request.FailureRate,
                CircuitBreakerStates = new[]
                {
                    new { State = "Closed", Duration = "0-10 seconds", Behavior = "Normal operation" },
                    new { State = "Open", Duration = "10-20 seconds", Behavior = "Fast-fail, no requests sent" },
                    new { State = "Half-Open", Duration = "20-25 seconds", Behavior = "Testing recovery" },
                    new { State = "Closed", Duration = "25-30 seconds", Behavior = "Normal operation restored" }
                },
                MetricsCollected = new
                {
                    TotalRequests = 1000,
                    SuccessfulRequests = 1000 - (1000 * request.FailureRate / 100),
                    FailedRequests = 1000 * request.FailureRate / 100,
                    CircuitBreakerActivations = 1,
                    RecoveryTime = "5 seconds"
                },
                Message = "Circuit breaker successfully prevented cascade failures (simulation)"
            };

            return Ok(testResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating circuit breaker test");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }

    // ========== Enterprise Scale Testing ==========

    [HttpPost("enterprise-scale/simulate-massive-orchestration")]
    [SwaggerOperation(
        Summary = "Simulate Enterprise-Scale Multi-Cluster FlinkDotNet.Orchestration",
        Description = "Simulate FlinkDotNet.Orchestration orchestration across thousands of clusters with intelligent placement"
    )]
    [SwaggerResponse(200, "Enterprise-scale simulation completed")]
    [SwaggerResponse(400, "Invalid scale parameters")]
    public async Task<IActionResult> SimulateEnterpriseScaleOrchestration([FromBody] EnterpriseScaleRequest request)
    {
        try
        {
            _logger.LogInformation("Simulating enterprise-scale FlinkDotNet.Orchestration with {ClusterCount} clusters and {JobCount} jobs",
                request.ClusterCount, request.JobCount);

            await Task.Delay(1000); // Simulate massive scale orchestration

            var simulationResults = new
            {
                Status = "Enterprise-scale simulation completed successfully",
                Scale = new
                {
                    TotalClusters = request.ClusterCount,
                    TotalJobs = request.JobCount,
                    AvailabilityZones = request.ClusterCount / 100, // Assume 100 clusters per AZ
                    DataCenters = request.ClusterCount / 500 // Assume 500 clusters per DC
                },
                PlacementStrategies = new[]
                {
                    new { Strategy = "BestFit", JobsPlaced = (int)(request.JobCount * 0.4), AverageUtilization = "85%" },
                    new { Strategy = "LeastLoaded", JobsPlaced = (int)(request.JobCount * 0.3), AverageUtilization = "65%" },
                    new { Strategy = "RoundRobin", JobsPlaced = (int)(request.JobCount * 0.2), AverageUtilization = "75%" },
                    new { Strategy = "LocalityFirst", JobsPlaced = (int)(request.JobCount * 0.1), AverageUtilization = "80%" }
                },
                Performance = new
                {
                    TotalProcessingTime = "15 minutes",
                    AverageJobPlacementTime = "100ms",
                    SystemAvailability = "99.999%",
                    FailedClusters = Math.Max(1, request.ClusterCount / 1000), // 0.1% failure rate
                    AutoRecoveredClusters = Math.Max(1, request.ClusterCount / 1000),
                    ThroughputMessages = request.JobCount * 10000 // 10k messages per job
                },
                Message = "Successfully demonstrated enterprise-scale FlinkDotNet.Orchestration capabilities (simulation)"
            };

            return Ok(simulationResults);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error simulating enterprise-scale FlinkDotNet.Orchestration");
            return StatusCode(500, new
            {
                Status = "Internal server error",
                Error = ex.Message
            });
        }
    }
}

// Request/Response Models for Temporal Durable Workflow Architecture Testing

public class JobSubmissionRequest
{
    public string? JobName { get; set; }
    public string? JobGraph { get; set; }
    public string Strategy { get; set; } = "BestFit";
    public int? CpuCores { get; set; }
    public int? MemoryMb { get; set; }
    public int? NetworkMbps { get; set; }
}

public class CreateClusterActorRequest
{
    public string? ClusterId { get; set; }
    public string? JobManagerUrl { get; set; }
    public int? TaskManagerCount { get; set; }
    public int? SlotsPerTaskManager { get; set; }
    public int? HealthCheckIntervalSeconds { get; set; }
}

public class OrchestrationWorkflowRequest
{
    public int TargetClusters { get; set; } = 10;
    public int MinClusters { get; set; } = 1;
    public int MaxClusters { get; set; } = 100;
}

public class CircuitBreakerTestRequest
{
    public int FailureRate { get; set; } = 20; // Percentage
    public int? TestDurationSeconds { get; set; } = 30;
}

public class EnterpriseScaleRequest
{
    public int ClusterCount { get; set; } = 1000;
    public int JobCount { get; set; } = 100000;
}

// Enhanced models for Temporal workflow operations

public class JobDistributionWorkflowRequest
{
    public List<JobDefinitionRequest> Jobs { get; set; } = new();
    public string Strategy { get; set; } = "BestFit";
}

public class JobDefinitionRequest
{
    public string? JobId { get; set; }
    public string? JobName { get; set; }
    public string? JobGraph { get; set; }
    public int? Parallelism { get; set; }
    public int? CpuCores { get; set; }
    public int? MemoryMb { get; set; }
    public string? PreferredRegion { get; set; }
    public string? PreferredZone { get; set; }
}

public class CancelWorkflowRequest
{
    public string Reason { get; set; } = "Cancelled by user";
}