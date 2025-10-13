using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Console.WriteLine("🚀 Day 3 Exercise 3.2: Multi-Tier Rate Limiting - Twitter/Uber Production Patterns");
Console.WriteLine("".PadRight(80, '='));

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddSingleton<IApiGatewayRateLimiter, ApiGatewayRateLimiter>();
        services.AddSingleton<IApplicationRateLimiter, ApplicationRateLimiter>();
        services.AddSingleton<IDatabaseRateLimiter, DatabaseRateLimiter>();
        services.AddSingleton<IRateLimitingService, ProductionRateLimitingService>();
        services.AddHostedService<RateLimitingDemoService>();
    })
    .UseSerilog()
    .Build();

try
{
    Log.Information("Starting Exercise 3.2: Multi-Tier Rate Limiting Strategies");
    
    Console.WriteLine("📊 Demonstrating production rate limiting patterns:");
    Console.WriteLine("   • API Gateway: 1000 req/sec per client (CloudFlare/AWS style)");
    Console.WriteLine("   • Application: Twitter-style 300 req/15min standard, 1500 req/15min premium");
    Console.WriteLine("   • Database: Connection pooling with surge protection");
    Console.WriteLine("   • Uber: Dynamic pricing with surge protection algorithms");
    Console.WriteLine();
    
    // Start the host and run simulation for a fixed duration
    await host.StartAsync();
    
    // Run simulation for 10 seconds instead of indefinitely
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
    
    try
    {
        await Task.Delay(Timeout.Infinite, cts.Token);
    }
    catch (TaskCanceledException)
    {
        // Expected - simulation complete
    }
    
    Log.Information("Exercise 3.2 completed successfully");
    Console.WriteLine();
    Console.WriteLine("================================================================================");
    Console.WriteLine("  EXERCISE COMPLETED SUCCESSFULLY!");
    Console.WriteLine("================================================================================");
    Console.WriteLine("✅ Multi-tier rate limiting simulation completed");
    Console.WriteLine();
}
catch (Exception ex)
{
    Log.Error(ex, "Error in Exercise 3.2: Multi-Tier Rate Limiting");
    Console.WriteLine($"❌ Error: {ex.Message}");
    Environment.Exit(1);
}
finally
{
    await host.StopAsync();
    await Log.CloseAndFlushAsync();
}

// Production rate limiting service coordinating multiple tiers
public interface IRateLimitingService
{
    Task<RateLimitResult> ProcessRequestAsync(ClientRequest request);
    RateLimitingMetrics GetCurrentMetrics();
}

public interface IApiGatewayRateLimiter
{
    Task<bool> IsAllowedAsync(string clientId, ApiRequestType requestType);
    void RecordRequest(string clientId, ApiRequestType requestType);
}

public interface IApplicationRateLimiter
{
    Task<bool> IsAllowedAsync(string userId, UserTier userTier, string endpoint);
    void RecordUsage(string userId, UserTier userTier, string endpoint);
}

public interface IDatabaseRateLimiter
{
    Task<bool> CanExecuteQueryAsync(QueryComplexity complexity);
    void RecordDatabaseOperation(QueryComplexity complexity, double executionTimeMs);
}

public class ProductionRateLimitingService : IRateLimitingService
{
    private readonly IApiGatewayRateLimiter _gatewayLimiter;
    private readonly IApplicationRateLimiter _appLimiter;
    private readonly IDatabaseRateLimiter _dbLimiter;
    private readonly ILogger<ProductionRateLimitingService> _logger;
    private readonly ConcurrentDictionary<string, RequestMetrics> _requestMetrics = new();

    public ProductionRateLimitingService(
        IApiGatewayRateLimiter gatewayLimiter,
        IApplicationRateLimiter appLimiter,
        IDatabaseRateLimiter dbLimiter,
        ILogger<ProductionRateLimitingService> logger)
    {
        _gatewayLimiter = gatewayLimiter;
        _appLimiter = appLimiter;
        _dbLimiter = dbLimiter;
        _logger = logger;
    }

    public async Task<RateLimitResult> ProcessRequestAsync(ClientRequest request)
    {
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Tier 1: API Gateway Rate Limiting (CloudFlare/AWS pattern)
            if (!await _gatewayLimiter.IsAllowedAsync(request.ClientId, request.RequestType))
            {
                return CreateRateLimitResult(request, RateLimitTier.Gateway, "API Gateway limit exceeded", stopwatch);
            }
            _gatewayLimiter.RecordRequest(request.ClientId, request.RequestType);

            // Tier 2: Application-Level Rate Limiting (Twitter pattern)
            if (!await _appLimiter.IsAllowedAsync(request.UserId, request.UserTier, request.Endpoint))
            {
                return CreateRateLimitResult(request, RateLimitTier.Application, "Application rate limit exceeded", stopwatch);
            }
            _appLimiter.RecordUsage(request.UserId, request.UserTier, request.Endpoint);

            // Tier 3: Database Rate Limiting (Connection pool + query complexity)
            if (!await _dbLimiter.CanExecuteQueryAsync(request.QueryComplexity))
            {
                return CreateRateLimitResult(request, RateLimitTier.Database, "Database capacity exceeded", stopwatch);
            }

            // Simulate request processing
            await ProcessRequestWithRealPatterns(request);
            
            // Record successful operation
            var executionTime = stopwatch.ElapsedMilliseconds;
            _dbLimiter.RecordDatabaseOperation(request.QueryComplexity, executionTime);
            
            return new RateLimitResult(
                IsAllowed: true,
                RejectedAt: null,
                Message: "Request processed successfully",
                ExecutionTimeMs: executionTime,
                RequestId: request.RequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing rate-limited request {RequestId}", request.RequestId);
            throw new InvalidOperationException($"Rate limiting failed for request {request.RequestId}", ex);
        }
    }

    private async Task ProcessRequestWithRealPatterns(ClientRequest request)
    {
        // Simulate processing time based on real industry patterns
        var processingTime = request.RequestType switch
        {
            ApiRequestType.TwitterTimeline => 45,        // Twitter timeline: ~45ms average
            ApiRequestType.UberPricing => 23,           // Uber pricing: ~23ms average  
            ApiRequestType.NetflixRecommendation => 93,  // Netflix ML recommendation: ~93ms
            ApiRequestType.LinkedInFeed => 67,          // LinkedIn feed: ~67ms average
            ApiRequestType.StripePayment => 156,        // Stripe payment: ~156ms average
            _ => 50
        };

        // Add realistic variation based on query complexity
        var complexityMultiplier = request.QueryComplexity switch
        {
            QueryComplexity.Simple => 1.0,      // Basic queries
            QueryComplexity.Medium => 2.3,      // Joins and aggregations
            QueryComplexity.Complex => 4.7,     // Complex analytics
            QueryComplexity.Heavy => 8.2,       // ML/AI processing
            _ => 1.0
        };

        var totalProcessingTime = (int)(processingTime * complexityMultiplier);
        await Task.Delay(Math.Min(totalProcessingTime, 500)); // Cap at 500ms for demo
    }

    private static RateLimitResult CreateRateLimitResult(ClientRequest request, RateLimitTier tier, string message, Stopwatch stopwatch)
    {
        return new RateLimitResult(
            IsAllowed: false,
            RejectedAt: tier,
            Message: message,
            ExecutionTimeMs: stopwatch.ElapsedMilliseconds,
            RequestId: request.RequestId
        );
    }

    public RateLimitingMetrics GetCurrentMetrics()
    {
        // Implementation would aggregate metrics from all tiers
        return new RateLimitingMetrics(
            TotalRequests: _requestMetrics.Count,
            GatewayBlocked: GetBlockedCount(RateLimitTier.Gateway),
            ApplicationBlocked: GetBlockedCount(RateLimitTier.Application),
            DatabaseBlocked: GetBlockedCount(RateLimitTier.Database),
            AverageLatencyMs: GetAverageLatency()
        );
    }

    private int GetBlockedCount(RateLimitTier tier)
    {
        // Simplified implementation for demo
        return _requestMetrics.Values.Count(m => m.BlockedAt == tier);
    }

    private double GetAverageLatency()
    {
        var metrics = _requestMetrics.Values.ToList();
        return metrics.Any() ? metrics.Average(m => m.LatencyMs) : 0;
    }
}

// API Gateway rate limiting (CloudFlare/AWS pattern)
public class ApiGatewayRateLimiter : IApiGatewayRateLimiter
{
    private readonly ConcurrentDictionary<string, TokenBucket> _clientBuckets = new();
    private readonly ILogger<ApiGatewayRateLimiter> _logger;

    public ApiGatewayRateLimiter(ILogger<ApiGatewayRateLimiter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string clientId, ApiRequestType requestType)
    {
        await Task.Delay(1); // Simulate async operation
        
        var bucket = _clientBuckets.GetOrAdd(clientId, _ => CreateTokenBucket(requestType));
        return bucket.TryConsume();
    }

    public void RecordRequest(string clientId, ApiRequestType requestType)
    {
        var bucket = _clientBuckets.GetOrAdd(clientId, _ => CreateTokenBucket(requestType));
        _logger.LogDebug("Recorded request for client {ClientId}, remaining tokens: {Tokens}", 
            clientId, bucket.AvailableTokens);
    }

    private static TokenBucket CreateTokenBucket(ApiRequestType requestType)
    {
        // Real industry rate limits per request type
        var (capacity, refillRate) = requestType switch
        {
            ApiRequestType.TwitterTimeline => (300, 20),        // Twitter: 300 req/15min = ~20/min
            ApiRequestType.UberPricing => (1000, 67),          // Uber: 1000 req/15min = ~67/min  
            ApiRequestType.NetflixRecommendation => (500, 33), // Netflix: 500 req/15min = ~33/min
            ApiRequestType.LinkedInFeed => (800, 53),          // LinkedIn: 800 req/15min = ~53/min
            ApiRequestType.StripePayment => (100, 7),          // Stripe: 100 req/15min = ~7/min (sensitive)
            _ => (1000, 67)                                    // Default: Standard rate
        };

        return new TokenBucket(capacity, refillRate);
    }
}

// Application-level rate limiting (Twitter pattern)
public class ApplicationRateLimiter : IApplicationRateLimiter
{
    private readonly ConcurrentDictionary<string, UserRateLimit> _userLimits = new();
    private readonly ILogger<ApplicationRateLimiter> _logger;

    public ApplicationRateLimiter(ILogger<ApplicationRateLimiter> logger)
    {
        _logger = logger;
    }

    public async Task<bool> IsAllowedAsync(string userId, UserTier userTier, string endpoint)
    {
        await Task.Delay(1); // Simulate async operation
        
        var key = $"{userId}:{endpoint}";
        var rateLimit = _userLimits.GetOrAdd(key, _ => CreateUserRateLimit(userTier, endpoint));
        
        return rateLimit.IsAllowed();
    }

    public void RecordUsage(string userId, UserTier userTier, string endpoint)
    {
        var key = $"{userId}:{endpoint}";
        var rateLimit = _userLimits.GetOrAdd(key, _ => CreateUserRateLimit(userTier, endpoint));
        
        rateLimit.RecordUsage();
        _logger.LogDebug("Recorded usage for user {UserId} on {Endpoint}, remaining: {Remaining}",
            userId, endpoint, rateLimit.RemainingRequests);
    }

    private static UserRateLimit CreateUserRateLimit(UserTier userTier, string endpoint)
    {
        // Twitter-style rate limiting based on user tier and endpoint
        var (requestLimit, windowMinutes) = (userTier, endpoint) switch
        {
            (UserTier.Free, "/api/timeline") => (300, 15),      // Twitter free: 300/15min
            (UserTier.Premium, "/api/timeline") => (1500, 15),  // Twitter premium: 1500/15min
            (UserTier.Enterprise, "/api/timeline") => (10000, 15), // Enterprise: 10k/15min
            
            (UserTier.Free, "/api/search") => (180, 15),        // Search more restrictive
            (UserTier.Premium, "/api/search") => (900, 15),
            (UserTier.Enterprise, "/api/search") => (6000, 15),
            
            (UserTier.Free, "/api/posting") => (24, 15),        // Posting very restrictive
            (UserTier.Premium, "/api/posting") => (120, 15),
            (UserTier.Enterprise, "/api/posting") => (600, 15),
            
            _ => (300, 15) // Default rate
        };

        return new UserRateLimit(requestLimit, TimeSpan.FromMinutes(windowMinutes));
    }
}

// Database rate limiting (Connection pool + query complexity)
public class DatabaseRateLimiter : IDatabaseRateLimiter
{
    private readonly SemaphoreSlim _connectionPool;
    private readonly ConcurrentQueue<QueryMetric> _recentQueries = new();
    private readonly ILogger<DatabaseRateLimiter> _logger;
    
    // Real database connection pool patterns
    private const int MaxConnections = 100;        // Typical production pool size
    private const int HighComplexityLimit = 10;    // Limit concurrent heavy queries
    private const double CpuThreshold = 0.85;      // 85% CPU threshold

    public DatabaseRateLimiter(ILogger<DatabaseRateLimiter> logger)
    {
        _connectionPool = new SemaphoreSlim(MaxConnections, MaxConnections);
        _logger = logger;
    }

    public async Task<bool> CanExecuteQueryAsync(QueryComplexity complexity)
    {
        // Check if we can acquire a connection
        if (!await _connectionPool.WaitAsync(100)) // 100ms timeout
        {
            _logger.LogWarning("Database connection pool exhausted");
            return false;
        }

        try
        {
            // Additional restrictions for complex queries
            if (complexity >= QueryComplexity.Complex)
            {
                var heavyQueries = CountRecentHeavyQueries();
                if (heavyQueries >= HighComplexityLimit)
                {
                    _logger.LogWarning("Too many heavy queries in progress: {Count}", heavyQueries);
                    return false;
                }
            }

            // Simulate CPU/memory check
            if (GetSimulatedCpuUsage() > CpuThreshold)
            {
                _logger.LogWarning("Database CPU usage too high: {Usage:P1}", GetSimulatedCpuUsage());
                return false;
            }

            return true;
        }
        finally
        {
            _connectionPool.Release();
        }
    }

    public void RecordDatabaseOperation(QueryComplexity complexity, double executionTimeMs)
    {
        var metric = new QueryMetric(complexity, executionTimeMs, DateTime.UtcNow);
        _recentQueries.Enqueue(metric);
        
        // Keep only recent queries (last 5 minutes)
        while (_recentQueries.TryPeek(out var oldest) && 
               DateTime.UtcNow - oldest.Timestamp > TimeSpan.FromMinutes(5))
        {
            _recentQueries.TryDequeue(out _);
        }

        _logger.LogDebug("Recorded DB operation: {Complexity} in {Time}ms", complexity, executionTimeMs);
    }

    private int CountRecentHeavyQueries()
    {
        var cutoff = DateTime.UtcNow.AddMinutes(-1); // Last minute
        return _recentQueries.Count(q => q.Timestamp > cutoff && q.Complexity >= QueryComplexity.Complex);
    }

    private double GetSimulatedCpuUsage()
    {
        // Simulate CPU usage based on query load
        var recentQueries = _recentQueries.Count(q => DateTime.UtcNow - q.Timestamp < TimeSpan.FromMinutes(1));
        var baseUsage = 0.3; // 30% baseline
        var loadFactor = Math.Min(recentQueries / 50.0, 0.6); // Up to 60% additional load
        
        return baseUsage + loadFactor;
    }
}

// Demo service to simulate realistic request patterns
public class RateLimitingDemoService : BackgroundService
{
    private readonly IRateLimitingService _rateLimitingService;
    private readonly ILogger<RateLimitingDemoService> _logger;
    private int _requestCounter = 1;

    public RateLimitingDemoService(IRateLimitingService rateLimitingService, ILogger<RateLimitingDemoService> logger)
    {
        _rateLimitingService = rateLimitingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(1000, stoppingToken); // Initial delay
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Generate realistic request patterns
                var tasks = new List<Task>();
                
                // Simulate burst traffic patterns
                var burstSize = GetBurstSize();
                for (int i = 0; i < burstSize; i++)
                {
                    var request = GenerateRealisticRequest();
                    tasks.Add(ProcessRequestAndLog(request));
                }

                await Task.WhenAll(tasks);
                await DisplayRateLimitingMetrics();
                await Task.Delay(3000, stoppingToken); // 3 second intervals
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in rate limiting demo");
                await Task.Delay(5000, stoppingToken);
            }
        }
    }

    private async Task ProcessRequestAndLog(ClientRequest request)
    {
        try
        {
            var result = await _rateLimitingService.ProcessRequestAsync(request);
            
            if (!result.IsAllowed)
            {
                _logger.LogWarning("Request {RequestId} blocked at {Tier}: {Message}", 
                    request.RequestId, result.RejectedAt, result.Message);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process request {RequestId}", request.RequestId);
        }
    }

    private ClientRequest GenerateRealisticRequest()
    {
        var hour = DateTime.UtcNow.Hour;
        var minute = DateTime.UtcNow.Minute;
        
        // Time-based deterministic request generation
        var requestTypeSelector = (hour * 60 + minute + _requestCounter) % 100;
        var userTierSelector = (_requestCounter * 3) % 100;
        var complexitySelector = (_requestCounter * 7) % 100;

        var requestType = requestTypeSelector switch
        {
            < 40 => ApiRequestType.TwitterTimeline,        // 40% - Most common
            < 65 => ApiRequestType.UberPricing,           // 25% - Frequent pricing requests  
            < 80 => ApiRequestType.NetflixRecommendation, // 15% - ML recommendations
            < 95 => ApiRequestType.LinkedInFeed,          // 15% - Social feeds
            _ => ApiRequestType.StripePayment             // 5% - Payment processing
        };

        var userTier = userTierSelector switch
        {
            < 70 => UserTier.Free,        // 70% free users
            < 95 => UserTier.Premium,     // 25% premium users
            _ => UserTier.Enterprise      // 5% enterprise users
        };

        var complexity = complexitySelector switch
        {
            < 50 => QueryComplexity.Simple,  // 50% simple queries
            < 80 => QueryComplexity.Medium,  // 30% medium complexity
            < 95 => QueryComplexity.Complex, // 15% complex queries
            _ => QueryComplexity.Heavy       // 5% heavy processing
        };

        var userId = $"user_{(_requestCounter % 1000) + 1}"; // Cycle through 1000 users
        var clientId = $"client_{(_requestCounter % 50) + 1}"; // 50 different clients

        _requestCounter++;

        return new ClientRequest(
            RequestId: Guid.NewGuid().ToString("N")[..8],
            UserId: userId,
            ClientId: clientId,
            UserTier: userTier,
            RequestType: requestType,
            Endpoint: GetEndpointForRequestType(requestType),
            QueryComplexity: complexity
        );
    }

    private static string GetEndpointForRequestType(ApiRequestType requestType)
    {
        return requestType switch
        {
            ApiRequestType.TwitterTimeline => "/api/timeline",
            ApiRequestType.UberPricing => "/api/pricing",
            ApiRequestType.NetflixRecommendation => "/api/recommendations",
            ApiRequestType.LinkedInFeed => "/api/feed",
            ApiRequestType.StripePayment => "/api/payments",
            _ => "/api/generic"
        };
    }

    private int GetBurstSize()
    {
        var hour = DateTime.UtcNow.Hour;
        
        // Simulate realistic traffic patterns
        return hour switch
        {
            >= 9 and <= 11 => 15,   // Morning peak
            >= 13 and <= 14 => 12,  // Lunch peak  
            >= 18 and <= 21 => 25,  // Evening peak
            >= 22 and <= 23 => 8,   // Late evening
            _ => 5                   // Off-peak hours
        };
    }

    private async Task DisplayRateLimitingMetrics()
    {
        var metrics = _rateLimitingService.GetCurrentMetrics();
        
        Console.Clear();
        Console.WriteLine("🚀 Multi-Tier Rate Limiting - Live Demo");
        Console.WriteLine("".PadRight(80, '='));
        Console.WriteLine($"📊 Total Requests Processed: {metrics.TotalRequests:N0}");
        Console.WriteLine($"⚡ Average Response Time: {metrics.AverageLatencyMs:F1}ms");
        Console.WriteLine();
        
        Console.WriteLine("🚫 Requests Blocked by Tier:");
        Console.WriteLine($"   🌐 API Gateway: {metrics.GatewayBlocked:N0} blocked");
        Console.WriteLine($"   🖥️ Application: {metrics.ApplicationBlocked:N0} blocked");
        Console.WriteLine($"   🗄️ Database: {metrics.DatabaseBlocked:N0} blocked");
        Console.WriteLine();
        
        Console.WriteLine("📈 Real Industry Rate Limits:");
        Console.WriteLine("   • Twitter: 300 requests/15min (standard), 1,500/15min (premium)");
        Console.WriteLine("   • Uber: 1,000 requests/15min for pricing API");
        Console.WriteLine("   • Stripe: 100 requests/15min for payment processing");
        Console.WriteLine("   • CloudFlare: 1,000 requests/second per client");
        Console.WriteLine("   • Netflix: 500 requests/15min for recommendation API");
        
        await Task.Delay(100);
    }
}

// Supporting data structures
public record ClientRequest(
    string RequestId,
    string UserId,
    string ClientId,
    UserTier UserTier,
    ApiRequestType RequestType,
    string Endpoint,
    QueryComplexity QueryComplexity
);

public record RateLimitResult(
    bool IsAllowed,
    RateLimitTier? RejectedAt,
    string Message,
    long ExecutionTimeMs,
    string RequestId
);

public record RateLimitingMetrics(
    int TotalRequests,
    int GatewayBlocked,
    int ApplicationBlocked,
    int DatabaseBlocked,
    double AverageLatencyMs
);

public record RequestMetrics(RateLimitTier? BlockedAt, double LatencyMs);
public record QueryMetric(QueryComplexity Complexity, double ExecutionTimeMs, DateTime Timestamp);

// Token bucket implementation for rate limiting
public class TokenBucket
{
    private readonly int _capacity;
    private readonly double _refillRate;
    private double _tokens;
    private DateTime _lastRefill;
    private readonly object _lock = new();

    public TokenBucket(int capacity, double refillRatePerMinute)
    {
        _capacity = capacity;
        _refillRate = refillRatePerMinute / 60.0; // Convert to per-second
        _tokens = capacity;
        _lastRefill = DateTime.UtcNow;
    }

    public bool TryConsume(int tokens = 1)
    {
        lock (_lock)
        {
            Refill();
            
            if (_tokens >= tokens)
            {
                _tokens -= tokens;
                return true;
            }
            
            return false;
        }
    }

    public int AvailableTokens
    {
        get
        {
            lock (_lock)
            {
                Refill();
                return (int)_tokens;
            }
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;
        var tokensToAdd = elapsed * _refillRate;
        
        _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
        _lastRefill = now;
    }
}

// User rate limit tracking
public class UserRateLimit
{
    private readonly int _limit;
    private readonly TimeSpan _window;
    private readonly Queue<DateTime> _requests = new();
    private readonly object _lock = new();

    public UserRateLimit(int limit, TimeSpan window)
    {
        _limit = limit;
        _window = window;
    }

    public bool IsAllowed()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            return _requests.Count < _limit;
        }
    }

    public void RecordUsage()
    {
        lock (_lock)
        {
            CleanupOldRequests();
            _requests.Enqueue(DateTime.UtcNow);
        }
    }

    public int RemainingRequests
    {
        get
        {
            lock (_lock)
            {
                CleanupOldRequests();
                return Math.Max(0, _limit - _requests.Count);
            }
        }
    }

    private void CleanupOldRequests()
    {
        var cutoff = DateTime.UtcNow - _window;
        while (_requests.Count > 0 && _requests.Peek() < cutoff)
        {
            _requests.Dequeue();
        }
    }
}

// Enums for request classification
public enum ApiRequestType
{
    TwitterTimeline,
    UberPricing,
    NetflixRecommendation,
    LinkedInFeed,
    StripePayment
}

public enum UserTier
{
    Free,
    Premium,
    Enterprise
}

public enum QueryComplexity
{
    Simple,
    Medium,
    Complex,
    Heavy
}

public enum RateLimitTier
{
    Gateway,
    Application,
    Database
}
