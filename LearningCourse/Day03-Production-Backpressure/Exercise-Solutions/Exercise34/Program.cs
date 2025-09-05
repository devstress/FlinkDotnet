using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 3 Exercise 3.4: Production Deployment - Netflix/AWS Auto-scaling Patterns");
Console.WriteLine("".PadRight(85, '='));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IDeploymentOrchestrator, ProductionDeploymentOrchestrator>();
        services.AddSingleton<IHealthMonitor, HealthMonitor>();
        services.AddSingleton<IAutoScaler, AutoScaler>();
        services.AddSingleton<IAlertManager, AlertManager>();
        services.AddSingleton<ICircuitBreaker, CircuitBreaker>();
        services.AddHostedService<ProductionDeploymentService>();
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting Exercise 3.4: Production Deployment Patterns");
    
    Console.WriteLine("📊 Production deployment with enterprise patterns:");
    Console.WriteLine("   • Netflix: Blue-green deployment with canary analysis");
    Console.WriteLine("   • AWS: Auto-scaling based on real-time metrics");
    Console.WriteLine("   • Circuit breakers: Hystrix-style failure isolation");
    Console.WriteLine("   • Health monitoring: Comprehensive system observability");
    Console.WriteLine("   • Alert management: PagerDuty-style incident response");
    Console.WriteLine();
    
    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 3.4: Production Deployment");
    Console.WriteLine($"❌ Error: {ex.Message}");
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}

// Production deployment orchestration with enterprise patterns
public interface IDeploymentOrchestrator
{
    Task<DeploymentResult> DeployAsync(DeploymentConfiguration config);
    Task<RollbackResult> RollbackAsync(string deploymentId);
    DeploymentStatus GetDeploymentStatus(string deploymentId);
}

public interface IHealthMonitor
{
    Task<HealthReport> GetHealthReportAsync();
    void RegisterHealthCheck(string name, Func<Task<HealthStatus>> healthCheck);
    event Action<HealthAlert> HealthAlertRaised;
}

public interface IAutoScaler
{
    Task<ScalingDecision> EvaluateScalingAsync(SystemMetrics metrics);
    Task<ScalingResult> ScaleAsync(ScalingDecision decision);
    ScalingPolicy GetCurrentPolicy();
}

public interface IAlertManager
{
    Task SendAlertAsync(Alert alert);
    void ConfigureAlertPolicy(AlertPolicy policy);
    AlertSummary GetAlertSummary();
}

public interface ICircuitBreaker
{
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName);
    CircuitBreakerState GetState(string operationName);
    void Reset(string operationName);
}

public class ProductionDeploymentOrchestrator : IDeploymentOrchestrator
{
    private readonly IHealthMonitor _healthMonitor;
    private readonly IAutoScaler _autoScaler;
    private readonly IAlertManager _alertManager;
    private readonly ILogger<ProductionDeploymentOrchestrator> _logger;
    private readonly ConcurrentDictionary<string, DeploymentInstance> _activeDeployments = new();

    public ProductionDeploymentOrchestrator(
        IHealthMonitor healthMonitor,
        IAutoScaler autoScaler,
        IAlertManager alertManager,
        ILogger<ProductionDeploymentOrchestrator> logger)
    {
        _healthMonitor = healthMonitor;
        _autoScaler = autoScaler;
        _alertManager = alertManager;
        _logger = logger;
    }

    public async Task<DeploymentResult> DeployAsync(DeploymentConfiguration config)
    {
        var deploymentId = Guid.NewGuid().ToString("N")[..8];
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            _logger.LogInformation("Starting production deployment {DeploymentId} with strategy {Strategy}", 
                deploymentId, config.Strategy);

            var deployment = new DeploymentInstance(deploymentId, config, DateTime.UtcNow);
            _activeDeployments[deploymentId] = deployment;

            // Netflix-style deployment process
            var result = config.Strategy switch
            {
                DeploymentStrategy.BlueGreen => await ExecuteBlueGreenDeployment(deployment),
                DeploymentStrategy.Canary => await ExecuteCanaryDeployment(deployment),
                DeploymentStrategy.RollingUpdate => await ExecuteRollingUpdateDeployment(deployment),
                _ => throw new InvalidOperationException($"Unsupported deployment strategy: {config.Strategy}")
            };

            stopwatch.Stop();
            result = result with { Duration = stopwatch.Elapsed };
            
            if (result.Success)
            {
                await _alertManager.SendAlertAsync(new Alert(
                    AlertType.Information,
                    "Deployment Successful",
                    $"Deployment {deploymentId} completed successfully in {stopwatch.Elapsed.TotalMinutes:F1} minutes",
                    AlertSeverity.Low
                ));
                
                // Check scaling policy after successful deployment
                var currentPolicy = _autoScaler.GetCurrentPolicy();
                _logger.LogInformation("Deployment complete. Auto-scaling policy: Min={Min}, Max={Max}",
                    currentPolicy.MinInstances, currentPolicy.MaxInstances);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment failed for {DeploymentId}", deploymentId);
            
            await _alertManager.SendAlertAsync(new Alert(
                AlertType.Error,
                "Deployment Failed",
                $"Deployment {deploymentId} failed: {ex.Message}",
                AlertSeverity.Critical
            ));
            
            throw new InvalidOperationException($"Deployment {deploymentId} failed", ex);
        }
    }

    private async Task<DeploymentResult> ExecuteBlueGreenDeployment(DeploymentInstance deployment)
    {
        _logger.LogInformation("Executing blue-green deployment for {DeploymentId}", deployment.Id);
        
        var stages = new[]
        {
            "Preparing green environment",
            "Deploying to green environment", 
            "Running health checks on green",
            "Switching traffic to green",
            "Verifying production traffic",
            "Decommissioning blue environment"
        };

        var completedStages = new List<string>();
        
        foreach (var stage in stages)
        {
            _logger.LogInformation("Blue-green deployment stage: {Stage}", stage);
            
            // Simulate Netflix-style deployment timing
            var stageDelay = stage switch
            {
                "Preparing green environment" => 2000,        // Infrastructure provisioning
                "Deploying to green environment" => 3000,     // Application deployment
                "Running health checks on green" => 1500,     // Health verification
                "Switching traffic to green" => 500,          // Load balancer switch
                "Verifying production traffic" => 2000,       // Traffic validation
                "Decommissioning blue environment" => 1000,   // Cleanup
                _ => 1000
            };
            
            await Task.Delay(stageDelay);
            
            // Simulate health check during critical stages
            if (stage.Contains("health") || stage.Contains("traffic"))
            {
                var healthReport = await _healthMonitor.GetHealthReportAsync();
                if (healthReport.OverallStatus != HealthStatus.Healthy)
                {
                    return new DeploymentResult(
                        DeploymentId: deployment.Id,
                        Success: false,
                        Message: $"Health check failed during: {stage}",
                        CompletedStages: completedStages,
                        Duration: TimeSpan.Zero
                    );
                }
            }
            
            completedStages.Add(stage);
        }

        return new DeploymentResult(
            DeploymentId: deployment.Id,
            Success: true,
            Message: "Blue-green deployment completed successfully",
            CompletedStages: completedStages,
            Duration: TimeSpan.Zero
        );
    }

    private async Task<DeploymentResult> ExecuteCanaryDeployment(DeploymentInstance deployment)
    {
        _logger.LogInformation("Executing canary deployment for {DeploymentId}", deployment.Id);
        
        var canaryPhases = new[]
        {
            ("Deploy canary (1% traffic)", 1),
            ("Monitor canary metrics", 1),
            ("Increase to 5% traffic", 5),
            ("Monitor expanded canary", 5),
            ("Increase to 25% traffic", 25),
            ("Monitor quarter traffic", 25),
            ("Full rollout (100% traffic)", 100)
        };

        var completedStages = new List<string>();
        
        foreach (var (phase, trafficPercent) in canaryPhases)
        {
            _logger.LogInformation("Canary phase: {Phase} ({Traffic}% traffic)", phase, trafficPercent);
            
            // Simulate canary analysis time (Netflix monitors each phase)
            await Task.Delay(2000);
            
            // Simulate traffic metrics analysis
            if (phase.Contains("Monitor"))
            {
                var metrics = await SimulateCanaryMetrics(trafficPercent);
                if (!metrics.IsHealthy)
                {
                    return new DeploymentResult(
                        DeploymentId: deployment.Id,
                        Success: false,
                        Message: $"Canary metrics failed at {trafficPercent}% traffic: {metrics.FailureReason}",
                        CompletedStages: completedStages,
                        Duration: TimeSpan.Zero
                    );
                }
            }
            
            completedStages.Add(phase);
        }

        return new DeploymentResult(
            DeploymentId: deployment.Id,
            Success: true,
            Message: "Canary deployment completed successfully",
            CompletedStages: completedStages,
            Duration: TimeSpan.Zero
        );
    }

    private async Task<DeploymentResult> ExecuteRollingUpdateDeployment(DeploymentInstance deployment)
    {
        _logger.LogInformation("Executing rolling update deployment for {DeploymentId}", deployment.Id);
        
        var totalInstances = 12; // Simulate 12 instances
        var batchSize = 3;       // Update 3 at a time
        var completedStages = new List<string>();
        
        for (int batch = 0; batch < totalInstances; batch += batchSize)
        {
            var instancesInBatch = Math.Min(batchSize, totalInstances - batch);
            var stage = $"Updating instances {batch + 1}-{batch + instancesInBatch}";
            
            _logger.LogInformation("Rolling update: {Stage}", stage);
            
            // Simulate instance update time
            await Task.Delay(1500);
            
            // Health check after each batch
            var healthReport = await _healthMonitor.GetHealthReportAsync();
            if (healthReport.OverallStatus != HealthStatus.Healthy)
            {
                return new DeploymentResult(
                    DeploymentId: deployment.Id,
                    Success: false,
                    Message: $"Health check failed after updating batch: {stage}",
                    CompletedStages: completedStages,
                    Duration: TimeSpan.Zero
                );
            }
            
            completedStages.Add(stage);
        }

        return new DeploymentResult(
            DeploymentId: deployment.Id,
            Success: true,
            Message: "Rolling update completed successfully",
            CompletedStages: completedStages,
            Duration: TimeSpan.Zero
        );
    }

    private async Task<CanaryMetrics> SimulateCanaryMetrics(int trafficPercent)
    {
        await Task.Delay(500); // Simulate metrics collection
        
        // Simulate realistic canary success/failure rates
        var errorRate = trafficPercent switch
        {
            1 => 0.001,   // 0.1% error rate at 1% traffic
            5 => 0.002,   // 0.2% error rate at 5% traffic  
            25 => 0.005,  // 0.5% error rate at 25% traffic
            _ => 0.001
        };
        
        // Deterministic simulation based on time
        var currentSecond = DateTime.UtcNow.Second;
        var simulatedErrorRate = (currentSecond % 100) / 10000.0; // 0-0.99% error rate
        
        if (simulatedErrorRate > errorRate * 10) // Allow 10x normal error rate before failing
        {
            return new CanaryMetrics(false, "Error rate exceeded threshold");
        }
        
        return new CanaryMetrics(true, "Metrics within acceptable range");
    }

    public async Task<RollbackResult> RollbackAsync(string deploymentId)
    {
        await Task.Delay(100); // Simulate async operation
        
        if (!_activeDeployments.TryGetValue(deploymentId, out var deployment))
        {
            return new RollbackResult(deploymentId, false, "Deployment not found");
        }

        _logger.LogInformation("Rolling back deployment {DeploymentId}", deploymentId);
        
        // Simulate rollback time (Netflix: ~2 minutes for full rollback)
        await Task.Delay(2000);
        
        _activeDeployments.TryRemove(deploymentId, out _);
        
        return new RollbackResult(deploymentId, true, "Rollback completed successfully");
    }

    public DeploymentStatus GetDeploymentStatus(string deploymentId)
    {
        if (_activeDeployments.TryGetValue(deploymentId, out var deployment))
        {
            return new DeploymentStatus(deploymentId, "In Progress", deployment.StartTime);
        }
        
        return new DeploymentStatus(deploymentId, "Not Found", DateTime.MinValue);
    }
}

public class HealthMonitor : IHealthMonitor
{
    private readonly ConcurrentDictionary<string, Func<Task<HealthStatus>>> _healthChecks = new();
    private readonly ILogger<HealthMonitor> _logger;
    
    public event Action<HealthAlert>? HealthAlertRaised;

    public HealthMonitor(ILogger<HealthMonitor> logger)
    {
        _logger = logger;
        RegisterDefaultHealthChecks();
    }

    private void RegisterDefaultHealthChecks()
    {
        // Netflix-style health checks
        RegisterHealthCheck("database", CheckDatabaseHealth);
        RegisterHealthCheck("cache", CheckCacheHealth);
        RegisterHealthCheck("external_api", CheckExternalApiHealth);
        RegisterHealthCheck("memory", CheckMemoryHealth);
        RegisterHealthCheck("cpu", CheckCpuHealth);
    }

    public async Task<HealthReport> GetHealthReportAsync()
    {
        var checks = new Dictionary<string, HealthCheckResult>();
        var overallStatus = HealthStatus.Healthy;
        
        foreach (var (name, healthCheck) in _healthChecks)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var status = await healthCheck();
                stopwatch.Stop();
                
                checks[name] = new HealthCheckResult(status, stopwatch.ElapsedMilliseconds);
                
                if (status == HealthStatus.Critical)
                {
                    overallStatus = HealthStatus.Critical;
                    HealthAlertRaised?.Invoke(new HealthAlert(name, status, "Critical health check failure"));
                }
                else if (status == HealthStatus.Warning && overallStatus == HealthStatus.Healthy)
                {
                    overallStatus = HealthStatus.Warning;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed for {HealthCheck}", name);
                checks[name] = new HealthCheckResult(HealthStatus.Critical, 0);
                overallStatus = HealthStatus.Critical;
            }
        }

        return new HealthReport(overallStatus, checks, DateTime.UtcNow);
    }

    public void RegisterHealthCheck(string name, Func<Task<HealthStatus>> healthCheck)
    {
        _healthChecks[name] = healthCheck;
        _logger.LogInformation("Registered health check: {HealthCheck}", name);
    }

    private async Task<HealthStatus> CheckDatabaseHealth()
    {
        await Task.Delay(50); // Simulate database ping
        
        // Simulate database connection issues (5% chance)
        var hour = DateTime.UtcNow.Hour;
        var minute = DateTime.UtcNow.Minute;
        
        return (hour + minute) % 20 == 0 ? HealthStatus.Warning : HealthStatus.Healthy;
    }

    private async Task<HealthStatus> CheckCacheHealth()
    {
        await Task.Delay(25); // Simulate cache ping
        return DateTime.UtcNow.Second % 30 == 0 ? HealthStatus.Warning : HealthStatus.Healthy;
    }

    private async Task<HealthStatus> CheckExternalApiHealth()
    {
        await Task.Delay(100); // Simulate external API call
        return DateTime.UtcNow.Second % 45 == 0 ? HealthStatus.Critical : HealthStatus.Healthy;
    }

    private async Task<HealthStatus> CheckMemoryHealth()
    {
        await Task.Delay(10); // Simulate memory check
        var memoryUsage = (DateTime.UtcNow.Minute % 10) / 10.0; // 0-90% usage
        
        return memoryUsage switch
        {
            > 0.85 => HealthStatus.Critical,
            > 0.75 => HealthStatus.Warning,
            _ => HealthStatus.Healthy
        };
    }

    private async Task<HealthStatus> CheckCpuHealth()
    {
        await Task.Delay(10); // Simulate CPU check
        var cpuUsage = (DateTime.UtcNow.Second % 10) / 10.0; // 0-90% usage
        
        return cpuUsage switch
        {
            > 0.90 => HealthStatus.Critical,
            > 0.80 => HealthStatus.Warning,
            _ => HealthStatus.Healthy
        };
    }
}

public class AutoScaler : IAutoScaler
{
    private readonly ILogger<AutoScaler> _logger;
    private readonly ScalingPolicy _currentPolicy;

    public AutoScaler(ILogger<AutoScaler> logger)
    {
        _logger = logger;
        _currentPolicy = new ScalingPolicy(
            MinInstances: 3,
            MaxInstances: 20,
            TargetCpuUtilization: 0.70,
            TargetMemoryUtilization: 0.75,
            ScaleUpCooldown: TimeSpan.FromMinutes(5),
            ScaleDownCooldown: TimeSpan.FromMinutes(10)
        );
    }

    public async Task<ScalingDecision> EvaluateScalingAsync(SystemMetrics metrics)
    {
        await Task.Delay(100); // Simulate metrics analysis
        
        var cpuExceedsThreshold = metrics.CpuUtilization > _currentPolicy.TargetCpuUtilization;
        var memoryExceedsThreshold = metrics.MemoryUtilization > _currentPolicy.TargetMemoryUtilization;
        var responseTimeHigh = metrics.AverageResponseTimeMs > 500; // 500ms threshold
        
        if (cpuExceedsThreshold || memoryExceedsThreshold || responseTimeHigh)
        {
            var targetInstances = CalculateTargetInstances(metrics, ScaleDirection.Up);
            return new ScalingDecision(
                ScaleDirection.Up,
                targetInstances,
                $"Scale up due to: CPU={metrics.CpuUtilization:P1}, Memory={metrics.MemoryUtilization:P1}, ResponseTime={metrics.AverageResponseTimeMs}ms"
            );
        }
        
        // Scale down if utilization is consistently low
        var cpuLow = metrics.CpuUtilization < _currentPolicy.TargetCpuUtilization * 0.5;
        var memoryLow = metrics.MemoryUtilization < _currentPolicy.TargetMemoryUtilization * 0.5;
        var responseTimeLow = metrics.AverageResponseTimeMs < 100; // 100ms threshold
        
        if (cpuLow && memoryLow && responseTimeLow && metrics.CurrentInstances > _currentPolicy.MinInstances)
        {
            var targetInstances = CalculateTargetInstances(metrics, ScaleDirection.Down);
            return new ScalingDecision(
                ScaleDirection.Down,
                targetInstances,
                $"Scale down due to low utilization: CPU={metrics.CpuUtilization:P1}, Memory={metrics.MemoryUtilization:P1}"
            );
        }
        
        return new ScalingDecision(ScaleDirection.None, metrics.CurrentInstances, "No scaling required");
    }

    private int CalculateTargetInstances(SystemMetrics metrics, ScaleDirection direction)
    {
        return direction switch
        {
            ScaleDirection.Up => Math.Min(metrics.CurrentInstances + 2, _currentPolicy.MaxInstances),
            ScaleDirection.Down => Math.Max(metrics.CurrentInstances - 1, _currentPolicy.MinInstances),
            ScaleDirection.None => metrics.CurrentInstances,
            _ => metrics.CurrentInstances
        };
    }

    public async Task<ScalingResult> ScaleAsync(ScalingDecision decision)
    {
        if (decision.Direction == ScaleDirection.None)
        {
            return new ScalingResult(true, "No scaling required", decision.TargetInstances);
        }

        _logger.LogInformation("Scaling {Direction} to {TargetInstances} instances: {Reason}",
            decision.Direction, decision.TargetInstances, decision.Reason);

        try
        {
            // Simulate AWS auto-scaling time
            var scalingTime = decision.Direction == ScaleDirection.Up ? 3000 : 2000; // Scale up takes longer
            await Task.Delay(scalingTime);
            
            return new ScalingResult(true, $"Successfully scaled {decision.Direction} to {decision.TargetInstances} instances", decision.TargetInstances);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Scaling operation failed");
            throw new InvalidOperationException($"Scaling {decision.Direction} failed", ex);
        }
    }

    public ScalingPolicy GetCurrentPolicy() => _currentPolicy;
}

public class AlertManager : IAlertManager
{
    private readonly ILogger<AlertManager> _logger;
    private readonly ConcurrentQueue<Alert> _alertHistory = new();
    private AlertPolicy _policy;

    public AlertManager(ILogger<AlertManager> logger)
    {
        _logger = logger;
        _policy = new AlertPolicy(
            EnableEmailAlerts: true,
            EnableSlackAlerts: true,
            EnablePagerDutyAlerts: true,
            CriticalAlertThreshold: TimeSpan.FromMinutes(1),
            WarningAlertThreshold: TimeSpan.FromMinutes(5)
        );
    }

    public async Task SendAlertAsync(Alert alert)
    {
        await Task.Delay(100); // Simulate alert sending
        
        _alertHistory.Enqueue(alert);
        
        // Keep only recent alerts (last 100)
        while (_alertHistory.Count > 100)
        {
            _alertHistory.TryDequeue(out _);
        }

        _logger.LogInformation("Alert sent: {Type} - {Title} (Severity: {Severity})",
            alert.Type, alert.Title, alert.Severity);

        // Simulate different alert channels based on severity
        var channels = alert.Severity switch
        {
            AlertSeverity.Critical => "Email, Slack, PagerDuty",
            AlertSeverity.High => "Email, Slack",
            AlertSeverity.Medium => "Slack",
            AlertSeverity.Low => "Email",
            _ => "Log only"
        };
        
        _logger.LogDebug("Alert sent via channels: {Channels}", channels);
    }

    public void ConfigureAlertPolicy(AlertPolicy policy)
    {
        _policy = policy;
        _logger.LogInformation("Alert policy updated: Email={EmailEnabled}, Slack={SlackEnabled}, PagerDuty={PagerDutyEnabled}",
            _policy.EnableEmailAlerts, _policy.EnableSlackAlerts, _policy.EnablePagerDutyAlerts);
    }

    public AlertSummary GetAlertSummary()
    {
        var alerts = _alertHistory.ToArray();
        var recent = alerts.Where(a => DateTime.UtcNow - a.Timestamp < TimeSpan.FromHours(24)).ToArray();
        
        return new AlertSummary(
            TotalAlerts24h: recent.Length,
            CriticalAlerts: recent.Count(a => a.Severity == AlertSeverity.Critical),
            HighAlerts: recent.Count(a => a.Severity == AlertSeverity.High),
            MediumAlerts: recent.Count(a => a.Severity == AlertSeverity.Medium),
            LowAlerts: recent.Count(a => a.Severity == AlertSeverity.Low)
        );
    }
}

public class CircuitBreaker : ICircuitBreaker
{
    private readonly ConcurrentDictionary<string, CircuitBreakerInstance> _circuitBreakers = new();
    private readonly ILogger<CircuitBreaker> _logger;

    public CircuitBreaker(ILogger<CircuitBreaker> logger)
    {
        _logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, string operationName)
    {
        var circuitBreaker = _circuitBreakers.GetOrAdd(operationName,
            name => new CircuitBreakerInstance(name));

        if (circuitBreaker.State == CircuitBreakerState.Open)
        {
            if (DateTime.UtcNow - circuitBreaker.LastFailureTime > TimeSpan.FromMinutes(1))
            {
                circuitBreaker.State = CircuitBreakerState.HalfOpen;
                _logger.LogInformation("Circuit breaker {Operation} transitioning to half-open", operationName);
            }
            else
            {
                throw new InvalidOperationException($"Circuit breaker {operationName} is OPEN");
            }
        }

        try
        {
            var result = await operation();
            
            if (circuitBreaker.State == CircuitBreakerState.HalfOpen)
            {
                circuitBreaker.State = CircuitBreakerState.Closed;
                circuitBreaker.FailureCount = 0;
                _logger.LogInformation("Circuit breaker {Operation} reset to closed", operationName);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            circuitBreaker.FailureCount++;
            circuitBreaker.LastFailureTime = DateTime.UtcNow;
            
            if (circuitBreaker.FailureCount >= 5) // Threshold: 5 failures
            {
                circuitBreaker.State = CircuitBreakerState.Open;
                _logger.LogWarning(ex, "Circuit breaker {Operation} opened after {Failures} failures",
                    operationName, circuitBreaker.FailureCount);
            }
            
            throw new InvalidOperationException($"Operation {operationName} failed", ex);
        }
    }

    public CircuitBreakerState GetState(string operationName)
    {
        return _circuitBreakers.TryGetValue(operationName, out var cb) ? cb.State : CircuitBreakerState.Closed;
    }

    public void Reset(string operationName)
    {
        if (_circuitBreakers.TryGetValue(operationName, out var cb))
        {
            cb.State = CircuitBreakerState.Closed;
            cb.FailureCount = 0;
            _logger.LogInformation("Circuit breaker {Operation} manually reset", operationName);
        }
    }
}

// Demo service orchestrating production deployment scenarios
public class ProductionDeploymentService : BackgroundService
{
    private readonly IDeploymentOrchestrator _deploymentOrchestrator;
    private readonly IHealthMonitor _healthMonitor;
    private readonly IAutoScaler _autoScaler;
    private readonly IAlertManager _alertManager;
    private readonly ILogger<ProductionDeploymentService> _logger;

    public ProductionDeploymentService(
        IDeploymentOrchestrator deploymentOrchestrator,
        IHealthMonitor healthMonitor,
        IAutoScaler autoScaler,
        IAlertManager alertManager,
        ILogger<ProductionDeploymentService> logger)
    {
        _deploymentOrchestrator = deploymentOrchestrator;
        _healthMonitor = healthMonitor;
        _autoScaler = autoScaler;
        _alertManager = alertManager;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken); // Initial delay
        
        var deploymentStrategies = new[]
        {
            DeploymentStrategy.BlueGreen,
            DeploymentStrategy.Canary,
            DeploymentStrategy.RollingUpdate
        };

        foreach (var strategy in deploymentStrategies)
        {
            if (stoppingToken.IsCancellationRequested) break;

            try
            {
                await DisplayDeploymentInfo(strategy);
                await ExecuteDeploymentDemo(strategy, stoppingToken);
                await DisplaySystemMetrics();
                
                await Task.Delay(8000, stoppingToken); // 8 second break between deployments
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute deployment demo for {Strategy}", strategy);
            }
        }

        await DisplayFinalSummary();
    }

    private async Task ExecuteDeploymentDemo(DeploymentStrategy strategy, CancellationToken cancellationToken)
    {
        var config = new DeploymentConfiguration(
            ApplicationName: "FlinkDotNet-StreamProcessor",
            Version: "v2.1.0",
            Strategy: strategy,
            HealthCheckTimeout: TimeSpan.FromMinutes(5),
            RollbackOnFailure: true
        );

        try
        {
            var result = await _deploymentOrchestrator.DeployAsync(config);
            
            await DisplayDeploymentResult(result);
            
            // Simulate post-deployment monitoring
            await Task.Delay(3000, cancellationToken);
            await SimulatePostDeploymentMonitoring();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Deployment failed for strategy {Strategy}", strategy);
            await DisplayDeploymentFailure(strategy, ex.Message);
        }
    }

    private async Task SimulatePostDeploymentMonitoring()
    {
        // Generate some load to trigger auto-scaling
        var metrics = new SystemMetrics(
            CpuUtilization: 0.85,      // High CPU to trigger scale-up
            MemoryUtilization: 0.78,   // High memory
            AverageResponseTimeMs: 450, // Elevated response time
            CurrentInstances: 5,
            RequestsPerSecond: 1200
        );

        var scalingDecision = await _autoScaler.EvaluateScalingAsync(metrics);
        if (scalingDecision.Direction != ScaleDirection.None)
        {
            var scalingResult = await _autoScaler.ScaleAsync(scalingDecision);
            _logger.LogInformation("Auto-scaling completed: {Result}", scalingResult.Message);
        }
    }

    private async Task DisplayDeploymentInfo(DeploymentStrategy strategy)
    {
        Console.Clear();
        Console.WriteLine("🚀 Production Deployment - Enterprise Patterns Demo");
        Console.WriteLine("".PadRight(85, '='));
        Console.WriteLine($"📊 Deployment Strategy: {strategy}");
        Console.WriteLine($"📝 Application: FlinkDotNet-StreamProcessor v2.1.0");
        Console.WriteLine();
        
        var description = strategy switch
        {
            DeploymentStrategy.BlueGreen => "Netflix-style blue-green deployment with instant traffic switching",
            DeploymentStrategy.Canary => "Gradual rollout with automated canary analysis and rollback",
            DeploymentStrategy.RollingUpdate => "Zero-downtime rolling update with health monitoring",
            _ => "Enterprise deployment pattern"
        };
        
        Console.WriteLine($"📋 Strategy Description: {description}");
        Console.WriteLine();
        Console.WriteLine("⏳ Deployment in progress...");
        
        await Task.Delay(100);
    }

    private async Task DisplayDeploymentResult(DeploymentResult result)
    {
        Console.Clear();
        Console.WriteLine("🚀 Production Deployment - Results");
        Console.WriteLine("".PadRight(85, '='));
        Console.WriteLine($"📊 Deployment ID: {result.DeploymentId}");
        Console.WriteLine($"✅ Status: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");
        Console.WriteLine($"⏱️ Duration: {result.Duration.TotalMinutes:F1} minutes");
        Console.WriteLine($"💬 Message: {result.Message}");
        Console.WriteLine();
        
        Console.WriteLine("📋 Completed Stages:");
        foreach (var stage in result.CompletedStages)
        {
            Console.WriteLine($"   ✓ {stage}");
        }
        
        await Task.Delay(100);
    }

    private async Task DisplayDeploymentFailure(DeploymentStrategy strategy, string error)
    {
        Console.Clear();
        Console.WriteLine("🚀 Production Deployment - Failure Detected");
        Console.WriteLine("".PadRight(85, '='));
        Console.WriteLine($"❌ Deployment Strategy: {strategy}");
        Console.WriteLine($"💥 Error: {error}");
        Console.WriteLine();
        Console.WriteLine("🔄 Automatic rollback initiated...");
        
        await Task.Delay(100);
    }

    private async Task DisplaySystemMetrics()
    {
        var healthReport = await _healthMonitor.GetHealthReportAsync();
        var alertSummary = _alertManager.GetAlertSummary();
        
        Console.WriteLine();
        Console.WriteLine("🖥️ System Health Status:");
        Console.WriteLine($"   Overall Status: {GetHealthStatusIcon(healthReport.OverallStatus)} {healthReport.OverallStatus}");
        
        foreach (var (name, result) in healthReport.HealthChecks)
        {
            Console.WriteLine($"   {GetHealthStatusIcon(result.Status)} {name}: {result.Status} ({result.ResponseTimeMs}ms)");
        }
        
        Console.WriteLine();
        Console.WriteLine("🚨 Alert Summary (24h):");
        Console.WriteLine($"   Total Alerts: {alertSummary.TotalAlerts24h}");
        Console.WriteLine($"   🔴 Critical: {alertSummary.CriticalAlerts}");
        Console.WriteLine($"   🟠 High: {alertSummary.HighAlerts}");
        Console.WriteLine($"   🟡 Medium: {alertSummary.MediumAlerts}");
        Console.WriteLine($"   🟢 Low: {alertSummary.LowAlerts}");
        
        await Task.Delay(100);
    }

    private static string GetHealthStatusIcon(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => "🟢",
            HealthStatus.Warning => "🟡",
            HealthStatus.Critical => "🔴",
            _ => "⚪"
        };
    }

    private async Task DisplayFinalSummary()
    {
        Console.WriteLine();
        Console.WriteLine("🎯 Production Deployment Complete!");
        Console.WriteLine("".PadRight(50, '='));
        Console.WriteLine("💡 Enterprise Deployment Patterns Demonstrated:");
        Console.WriteLine("   • Blue-Green: Zero-downtime instant traffic switching");
        Console.WriteLine("   • Canary: Risk-reduced gradual rollout with automated analysis");
        Console.WriteLine("   • Rolling Update: Instance-by-instance updates with health checks");
        Console.WriteLine("   • Auto-scaling: Dynamic capacity management based on metrics");
        Console.WriteLine("   • Circuit Breakers: Failure isolation and system resilience");
        Console.WriteLine("   • Comprehensive monitoring: Health checks and alerting systems");
        
        await Task.Delay(100);
    }
}

// Data models for production deployment
public record DeploymentConfiguration(
    string ApplicationName,
    string Version,
    DeploymentStrategy Strategy,
    TimeSpan HealthCheckTimeout,
    bool RollbackOnFailure
);

public record DeploymentResult(
    string DeploymentId,
    bool Success,
    string Message,
    List<string> CompletedStages,
    TimeSpan Duration
);

public record RollbackResult(string DeploymentId, bool Success, string Message);

public record DeploymentStatus(string DeploymentId, string Status, DateTime StartTime);

public record DeploymentInstance(string Id, DeploymentConfiguration Config, DateTime StartTime);

public record HealthReport(HealthStatus OverallStatus, Dictionary<string, HealthCheckResult> HealthChecks, DateTime Timestamp);

public record HealthCheckResult(HealthStatus Status, long ResponseTimeMs);

public record HealthAlert(string HealthCheck, HealthStatus Status, string Message);

public record SystemMetrics(double CpuUtilization, double MemoryUtilization, double AverageResponseTimeMs, int CurrentInstances, double RequestsPerSecond);

public record ScalingDecision(ScaleDirection Direction, int TargetInstances, string Reason);

public record ScalingResult(bool Success, string Message, int FinalInstances);

public record ScalingPolicy(int MinInstances, int MaxInstances, double TargetCpuUtilization, double TargetMemoryUtilization, TimeSpan ScaleUpCooldown, TimeSpan ScaleDownCooldown);

public record Alert(AlertType Type, string Title, string Message, AlertSeverity Severity)
{
    public DateTime Timestamp { get; } = DateTime.UtcNow;
}

public record AlertPolicy(bool EnableEmailAlerts, bool EnableSlackAlerts, bool EnablePagerDutyAlerts, TimeSpan CriticalAlertThreshold, TimeSpan WarningAlertThreshold);

public record AlertSummary(int TotalAlerts24h, int CriticalAlerts, int HighAlerts, int MediumAlerts, int LowAlerts);

public record CanaryMetrics(bool IsHealthy, string FailureReason);

public class CircuitBreakerInstance
{
    public string Name { get; }
    public CircuitBreakerState State { get; set; } = CircuitBreakerState.Closed;
    public int FailureCount { get; set; }
    public DateTime LastFailureTime { get; set; }

    public CircuitBreakerInstance(string name)
    {
        Name = name;
    }
}

// Enums for deployment and monitoring
public enum DeploymentStrategy
{
    BlueGreen,
    Canary,
    RollingUpdate
}

public enum HealthStatus
{
    Healthy,
    Warning,
    Critical
}

public enum ScaleDirection
{
    None,
    Up,
    Down
}

public enum AlertType
{
    Information,
    Warning,
    Error,
    Critical
}

public enum AlertSeverity
{
    Low,
    Medium,
    High,
    Critical
}

public enum CircuitBreakerState
{
    Closed,
    Open,
    HalfOpen
}
