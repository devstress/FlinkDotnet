using LocalTesting.WebApi.Services;
using LocalTesting.WebApi.Services.Temporal;
using FlinkDotNet.Orchestration.Interfaces;
using FlinkDotNet.Orchestration.Services;
using FlinkDotNet.Orchestration.Models;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using StackExchange.Redis;
using System.Diagnostics.Metrics;

var builder = WebApplication.CreateBuilder(args);

// Configure IPv4-only binding compatible with Aspire orchestration
// Use a different internal port to avoid conflicts with Aspire's proxy
builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(System.Net.IPAddress.Parse("127.0.0.1"), 5001); // Internal port for Aspire
});

// Configure Flink job management defaults
builder.Configuration["Flink:UseFlinkDotNet"] = "true"; // Default to FlinkDotNet

// Configure OpenTelemetry with comprehensive observability metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource
        .AddService("LocalTesting.WebApi")
        .AddAttributes(new Dictionary<string, object>
        {
            ["deployment.environment"] = "local-testing",
            ["service.version"] = "1.0.0"
        }))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddMeter("FlinkDotNet.Kafka")
        .AddMeter("FlinkDotNet.Flink") 
        .AddMeter("FlinkDotNet.Temporal")
        .AddMeter("FlinkDotNet.Flow")
        .AddOtlpExporter())
    .WithLogging(logging => logging
        .AddOtlpExporter());

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        // Use PascalCase property names to match test expectations
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { 
        Title = "LocalTesting API - Complex Logic Stress Test Interactive Interface", 
        Version = "v1",
        Description = "Interactive API for debugging and executing Complex Logic Stress Test scenarios step by step. " +
                     "This API transforms BDD test scenarios into executable endpoints for local testing and debugging."
    });
    c.EnableAnnotations();
});

// Add Redis connection as a lazy singleton for Aspire compatibility
builder.Services.AddSingleton<Lazy<IConnectionMultiplexer>>(provider =>
{
    return new Lazy<IConnectionMultiplexer>(() =>
    {
        var logger = provider.GetRequiredService<ILogger<Program>>();
        var connectionString = builder.Configuration.GetConnectionString("redis") ?? "localhost:6379";
        
        // Retry logic for Aspire orchestration - Redis might not be ready immediately
        for (int attempt = 1; attempt <= 5; attempt++)
        {
            try
            {
                logger.LogInformation("Attempting to connect to Redis (attempt {Attempt}/5): {ConnectionString}", attempt, connectionString);
                return ConnectionMultiplexer.Connect(connectionString);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis connection attempt {Attempt} failed", attempt);
                if (attempt == 5)
                {
                    logger.LogError(ex, "Failed to connect to Redis after 5 attempts. Application will continue but Redis features may not work.");
                    throw; // Re-throw on final attempt
                }
                Thread.Sleep(attempt * 1000); // 1s, 2s, 3s, 4s delays
            }
        }
        
        throw new InvalidOperationException("This should never be reached");
    });
});

// Add IConnectionMultiplexer as a service that gets the value from the lazy instance
builder.Services.AddSingleton<IConnectionMultiplexer>(provider =>
    provider.GetRequiredService<Lazy<IConnectionMultiplexer>>().Value);

// Add custom services
builder.Services.AddSingleton<ObservabilityMetricsService>();
builder.Services.AddSingleton<IMessageStateService, MessageStateService>();
builder.Services.AddSingleton<AspireHealthCheckService>();
builder.Services.AddSingleton<ComplexLogicStressTestService>();
builder.Services.AddSingleton<SecurityTokenManagerService>();
builder.Services.AddSingleton<TemporalSecurityTokenService>();
builder.Services.AddSingleton<KafkaProducerService>();
builder.Services.AddSingleton<FlinkJobManagementService>();
builder.Services.AddSingleton<BackpressureMonitoringService>();

// Add orchestration services for latest architecture
builder.Services.AddSingleton<IFlinkOrchestra, FlinkOrchestra>();

// Add HTTP client for external calls with extended timeout for complex operations
builder.Services.AddHttpClient().ConfigureHttpClientDefaults(clientBuilder =>
{
    clientBuilder.ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(3); // 3 minutes for complex operations
    });
});

try
{
    var app = builder.Build();

    // Initialize Orchestra with test clusters for LocalTesting
    try
    {
        await InitializeOrchestraForLocalTestingAsync(app.Services);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Orchestra initialization failed but continuing: {ex.Message}");
    }

    // Configure the HTTP request pipeline
    app.UseSwagger();
    app.UseSwaggerUI(c => 
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "LocalTesting API v1");
        c.RoutePrefix = string.Empty; // Set Swagger UI at app's root
        c.DocumentTitle = "LocalTesting - Complex Logic Stress Test Interface";
        c.DefaultModelsExpandDepth(-1);
        c.DefaultModelExpandDepth(2);
    });

    app.UseAuthorization();

    // Add simple health check endpoint for LocalTesting
    app.MapGet("/health", () => Results.Ok(new { 
        Status = "Healthy", 
        Timestamp = DateTime.UtcNow, 
        Service = "LocalTesting WebAPI",
        Version = "1.0.0" 
    }));

    app.MapControllers();

    Console.WriteLine("Starting LocalTesting WebAPI application...");
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"CRITICAL ERROR: Application startup failed: {ex}");
    Environment.Exit(1);
}

/// <summary>
/// Initialize Orchestra with test clusters for LocalTesting environment
/// </summary>
static async Task InitializeOrchestraForLocalTestingAsync(IServiceProvider services)
{
    try
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
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
        
        // Provision simulated clusters for testing
        foreach (var testCluster in testClusters)
        {
            try
            {
                // Create a simulated cluster actor for LocalTesting
                var simulatedActor = new SimulatedClusterActor(
                    $"sim-cluster-{Guid.NewGuid():N}[..8]",
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
                        testCluster.Config.Name, testCluster.AvailableSlots, testCluster.TotalSlots);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to create simulated test cluster {ClusterName}, continuing with other clusters", 
                    testCluster.Config.Name);
            }
        }
        
        // Verify clusters are available
        var availableClusters = await orchestra.GetAvailableClustersAsync();
        logger.LogInformation("Orchestra initialization completed. Available clusters: {ClusterCount}", 
            availableClusters.Length);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to initialize Orchestra with test clusters");
        // Don't throw - allow the app to continue running even if Orchestra initialization fails
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
            FlinkJobId = $"flink-job-{Guid.NewGuid():N}[..8]",
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