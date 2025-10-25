# Day 3: Production-Grade Backpressure & Distributed Rate Limiting

## 🗺️ Course Navigation
**[← Day 2: AI-Enhanced Stream Processing](../Day02-AI-Stream-Processing/)** | **[Course Overview](../README.md)** | **[Next: Day 4 - Enterprise Observability →](../Day04-Enterprise-Observability/)**

---

## 🎯 Real-World Learning Objectives

Master the **"Local bucket + Regional Redis budget bank + Global controller"** pattern used by Netflix, Uber, and other scale companies for fault-tolerant distributed rate limiting with gRPC ingress.

**Time:** 6-7 hours | **Reference:** [Apache Flink AsyncSink Rate Limiting](https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/)

## 📚 Real-World Reference Pattern

This implementation follows the **exact architecture** described in production fault-tolerance playbooks from:
- **Netflix Zuul 2** - Distributed rate limiting architecture
- **Uber's API Gateway** - Regional budget bank pattern  
- **Apache Flink 2.1.0** - AsyncSink rate limiting strategies

### 🏗️ Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│           FAULT-TOLERANT DISTRIBUTED RATE LIMITING ARCHITECTURE                │
├─────────────────────────────────────────────────────────────────────────────────┤
│                                                                                 │
│  ┌─────────────────────┐    ┌─────────────────────────┐    ┌─────────────────┐ │
│  │   GLOBAL QUOTA      │    │  REGIONAL BUDGET BANK   │    │ GATEWAY CLUSTER │ │
│  │   CONTROLLER (GQC)  │    │      (RBB - Redis)      │    │   (gRPC Ingress)│ │
│  │                     │    │                         │    │                 │ │
│  │ • Epoch minting     │───▶│ • Per-region budgets    │───▶│ • Local buckets │ │
│  │ • Policy distribution│   │ • Atomic DECRBY/Lua    │    │ • Hot path rate │ │
│  │ • Cross-region sync │    │ • TTL management        │    │   limiting      │ │
│  │ • Pre-mint futures  │    │ • Failover handling     │    │ • Backpressure  │ │
│  │                     │    │                         │    │   propagation   │ │
│  └─────────────────────┘    └─────────────────────────┘    └─────────────────┘ │
│            │                           │                           │            │
│            │                           │                           │            │
│            └───── Every 250ms ─────────┼──── Background refill ────┘            │
│                                        │                                        │
│              ┌─────────────────────────────────────────────────────────────┐    │
│              │                    FAULT SCENARIOS                         │    │
│              │                                                             │    │
│              │ 1. Gateway Restart    → SEVERE pause until first grant     │    │
│              │ 2. Redis Dies         → Fail-closed with local buckets     │    │
│              │ 3. Network Partition  → Regional fallback to RBB-B         │    │
│              │ 4. GQC Unavailable    → Continue with cached budgets        │    │
│              └─────────────────────────────────────────────────────────────┘    │
│                                                                                 │
└─────────────────────────────────────────────────────────────────────────────────┘
```

## 🚀 Implementation: Real-World gRPC Ingress with Distributed Rate Limiting

### Step 1: Global Quota Controller (GQC) → **[Exercise 3.1: Netflix Global Rate Limiting Controller](Exercise-Solutions/)**

Following **Netflix's distributed architecture** patterns and implementing their Global Quota Controller used for managing API rate limits across Netflix's 2000+ microservices:

**🎯 Hands-on Implementation:** This Netflix-proven architecture is implemented in **[Exercise 3.1: Netflix Global Rate Limiting Controller](Exercise-Solutions/Exercise31/)** where you'll build Netflix-scale distributed rate limiting for their API gateway that handles 1+ billion requests daily with coordinated quota management across multiple regions.

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Grpc.Core;

namespace FlinkDotNet.Production.RateLimiting
{
    /// <summary>
    /// Global Quota Controller (GQC) - Mints signed budgets per epoch
    /// Based on Netflix Zuul 2 and Uber's API Gateway patterns
    /// 
    /// Reference: Netflix's "Zuul 2: The Netflix Journey to Asynchronous, Non-blocking Systems"
    /// https://netflixtechblog.com/zuul-2-the-netflix-journey-to-asynchronous-non-blocking-systems-45947377fb5c
    /// </summary>
    public class GlobalQuotaController : BackgroundService
    {
        private readonly ILogger<GlobalQuotaController> _logger;
        private readonly IConnectionMultiplexer _redis;
        private readonly QuotaControllerConfig _config;
        private readonly Dictionary<string, RegionalBudgetBank> _regionalBanks;
        
        // Epoch timing (Netflix pattern: 250ms epochs)
        private readonly TimeSpan _epochInterval = TimeSpan.FromMilliseconds(250);
        private readonly TimeSpan _budgetTtl = TimeSpan.FromMilliseconds(500); // 2x epoch
        
        public GlobalQuotaController(
            ILogger<GlobalQuotaController> logger,
            IConnectionMultiplexer redis,
            QuotaControllerConfig config)
        {
            _logger = logger;
            _redis = redis;
            _config = config;
            _regionalBanks = new Dictionary<string, RegionalBudgetBank>();
            
            // Initialize regional budget banks
            foreach (var region in config.Regions)
            {
                _regionalBanks[region] = new RegionalBudgetBank(redis, region, _logger);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🌍 Global Quota Controller starting with {RegionCount} regions", 
                _regionalBanks.Count);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var currentEpoch = GetCurrentEpoch();
                    
                    // Pre-mint future epochs to handle control-plane hiccups
                    await MintEpochBudgets(currentEpoch, stoppingToken);
                    await MintEpochBudgets(currentEpoch + 1, stoppingToken); // Future epoch 1
                    await MintEpochBudgets(currentEpoch + 2, stoppingToken); // Future epoch 2
                    
                    await Task.Delay(_epochInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error in Global Quota Controller epoch processing");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken); // Brief pause on error
                }
            }
        }

        /// <summary>
        /// Mint signed budgets for all {region, tenant, pipeline} combinations
        /// Implements hierarchical quota distribution following Uber's patterns
        /// </summary>
        private async Task MintEpochBudgets(long epochNumber, CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();
            
            foreach (var (region, rbb) in _regionalBanks)
            {
                foreach (var tenant in _config.Tenants)
                {
                    foreach (var pipeline in _config.Pipelines)
                    {
                        var budgetKey = $"budget:{region}:{tenant}:{pipeline}:{epochNumber}";
                        var quota = CalculateQuota(region, tenant, pipeline);
                        
                        // Create signed budget envelope (HMAC for security)
                        var budget = new EpochBudget
                        {
                            Region = region,
                            Tenant = tenant,
                            Pipeline = pipeline,
                            Epoch = epochNumber,
                            MessageTokens = quota.MessageTokens,
                            ByteTokens = quota.ByteTokens,
                            ExpiresAt = DateTimeOffset.UtcNow.Add(_budgetTtl),
                            Signature = SignBudget(region, tenant, pipeline, epochNumber, quota)
                        };

                        // Push to Regional Budget Bank
                        tasks.Add(rbb.StoreBudgetAsync(budgetKey, budget, _budgetTtl));
                    }
                }
            }

            await Task.WhenAll(tasks);
            _logger.LogDebug("✅ Minted epoch {Epoch} budgets for {RegionCount} regions", 
                epochNumber, _regionalBanks.Count);
        }

        /// <summary>
        /// Calculate hierarchical quota based on tenant priority and historical usage
        /// Implements Uber's tenant-aware resource allocation
        /// </summary>
        private QuotaAllocation CalculateQuota(string region, string tenant, string pipeline)
        {
            // Base allocation per epoch (250ms)
            var baseMessageRate = 1000; // messages per second
            var baseByteRate = 1024 * 1024; // 1MB per second
            
            // Convert to per-epoch allocation (250ms = 1/4 second)
            var epochMessages = (int)(baseMessageRate * 0.25);
            var epochBytes = (int)(baseByteRate * 0.25);
            
            // Apply tenant-specific multipliers (priority-based allocation)
            var tenantMultiplier = tenant switch
            {
                "critical" => 2.0,
                "high" => 1.5,
                "normal" => 1.0,
                "batch" => 0.5,
                _ => 1.0
            };
            
            // Apply pipeline-specific multipliers
            var pipelineMultiplier = pipeline switch
            {
                "real_time_analytics" => 2.0,
                "fraud_detection" => 1.8,
                "recommendations" => 1.2,
                "batch_processing" => 0.8,
                _ => 1.0
            };
            
            return new QuotaAllocation
            {
                MessageTokens = (int)(epochMessages * tenantMultiplier * pipelineMultiplier),
                ByteTokens = (int)(epochBytes * tenantMultiplier * pipelineMultiplier)
            };
        }

        /// <summary>
        /// Sign budget with HMAC for security (prevents budget tampering)
        /// </summary>
        private string SignBudget(string region, string tenant, string pipeline, long epoch, QuotaAllocation quota)
        {
            var payload = $"{region}:{tenant}:{pipeline}:{epoch}:{quota.MessageTokens}:{quota.ByteTokens}";
            return Convert.ToBase64String(
                System.Security.Cryptography.HMACSHA256.HashData(
                    System.Text.Encoding.UTF8.GetBytes(_config.SigningKey),
                    System.Text.Encoding.UTF8.GetBytes(payload)
                )
            )[..16]; // First 16 chars for brevity
        }

        private static long GetCurrentEpoch()
        {
            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 250; // 250ms epochs
        }
    }

### Step 2: Regional Budget Bank (RBB) → **[Exercise 3.2: Uber Regional Redis Coordination](Exercise-Solutions/)**

Following **Uber's regional budget bank patterns** used for coordinating API rate limits across their global regions handling 15+ million rides daily:

**🎯 Hands-on Implementation:** This Uber-proven coordination architecture is implemented in **[Exercise 3.2: Uber Regional Redis Coordination](Exercise-Solutions/Exercise32/)** where you'll build Uber-scale regional budget coordination using Redis for their API gateway that handles massive traffic spikes during peak hours with atomic DECRBY operations and TTL management.

    /// <summary>
    /// Regional Budget Bank (RBB) - Redis-backed budget storage
    /// Implements atomic DECRBY operations for fair allocation
    /// </summary>
    public class RegionalBudgetBank
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _database;
        private readonly string _region;
        private readonly ILogger _logger;
        
        public RegionalBudgetBank(IConnectionMultiplexer redis, string region, ILogger logger)
        {
            _redis = redis;
            _database = redis.GetDatabase();
            _region = region;
            _logger = logger;
        }

        /// <summary>
        /// Store budget in Redis with TTL (ephemeral keys pattern)
        /// </summary>
        public async Task StoreBudgetAsync(string budgetKey, EpochBudget budget, TimeSpan ttl)
        {
            var budgetJson = System.Text.Json.JsonSerializer.Serialize(budget);
            await _database.StringSetAsync(budgetKey, budgetJson, ttl);
        }

        /// <summary>
        /// Atomic grant request using Lua script (prevents double-spending)
        /// Reference: Redis atomic operations for distributed rate limiting
        /// </summary>
        public async Task<GrantResult> RequestGrantAsync(string budgetKey, int requestedMessages, int requestedBytes)
        {
            const string luaScript = @"
                local budget_key = KEYS[1]
                local requested_msg = tonumber(ARGV[1])
                local requested_bytes = tonumber(ARGV[2])
                
                local budget_json = redis.call('GET', budget_key)
                if not budget_json then
                    return {0, 0, 'BUDGET_NOT_FOUND'}
                end
                
                local budget = cjson.decode(budget_json)
                local available_msg = budget.MessageTokens
                local available_bytes = budget.ByteTokens
                
                -- Check if we can satisfy the request
                if available_msg >= requested_msg and available_bytes >= requested_bytes then
                    -- Deduct tokens atomically
                    budget.MessageTokens = available_msg - requested_msg
                    budget.ByteTokens = available_bytes - requested_bytes
                    
                    -- Update budget in Redis
                    redis.call('SET', budget_key, cjson.encode(budget), 'PX', budget.ExpiresAt)
                    
                    return {requested_msg, requested_bytes, 'GRANTED'}
                else
                    return {0, 0, 'INSUFFICIENT_QUOTA'}
                end
            ";

            try
            {
                var result = await _database.ScriptEvaluateAsync(luaScript, 
                    new RedisKey[] { budgetKey }, 
                    new RedisValue[] { requestedMessages, requestedBytes });

                var resultArray = (RedisValue[])result;
                return new GrantResult
                {
                    GrantedMessages = resultArray[0],
                    GrantedBytes = resultArray[1],
                    Status = resultArray[2]
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Redis grant request failed for key {BudgetKey}", budgetKey);
                return new GrantResult { Status = "ERROR" };
            }
        }
    }

### Step 3: gRPC Ingress Gateway → **[Exercise 3.3: LinkedIn High-Performance Gateway](Exercise-Solutions/)**

Following **LinkedIn's high-performance gateway patterns** used for handling 900+ million user requests with stateless rate limiting and local token buckets:

**🎯 Hands-on Implementation:** This LinkedIn-proven gateway architecture is implemented in **[Exercise 3.3: LinkedIn High-Performance Gateway](Exercise-Solutions/Exercise33/)** where you'll build LinkedIn-scale gRPC ingress gateway for their API that handles massive user traffic with local token buckets and hot path rate limiting.

    /// <summary>
    /// gRPC Ingress Gateway with stateless rate limiting
    /// Implements the "hot path" rate limiting with local buckets
    /// </summary>
    public class GrpcIngressGateway : Grpc.Core.ServerServiceDefinition
    {
        private readonly ILogger<GrpcIngressGateway> _logger;
        private readonly RegionalBudgetBank _budgetBank;
        private readonly BackgroundRefillService _refillService;
        
        // Local token buckets (in-memory, stateless)
        private readonly Dictionary<string, LocalTokenBucket> _localBuckets;
        private readonly object _lockObject = new();
        
        // State management
        private GatewayState _state = GatewayState.SEVERE; // Safe by default
        private DateTime _lastGrantTime = DateTime.MinValue;
        
        public GrpcIngressGateway(
            ILogger<GrpcIngressGateway> logger,
            RegionalBudgetBank budgetBank,
            BackgroundRefillService refillService)
        {
            _logger = logger;
            _budgetBank = budgetBank;
            _refillService = refillService;
            _localBuckets = new Dictionary<string, LocalTokenBucket>();
            
            // Subscribe to refill events
            _refillService.OnGrantReceived += OnGrantReceived;
            _refillService.OnGrantFailed += OnGrantFailed;
            
            _logger.LogInformation("🚪 gRPC Ingress Gateway initialized in SEVERE state (safe by default)");
        }

        /// <summary>
        /// Process incoming gRPC stream with rate limiting on hot path
        /// Implements Netflix's stateless limiter pattern
        /// </summary>
        public async Task<IngestResponse> ProcessIngestStream(
            IAsyncStreamReader<IngestRequest> requestStream,
            ServerCallContext context)
        {
            var clientInfo = ExtractClientInfo(context);
            var bucketKey = $"{clientInfo.Tenant}:{clientInfo.Pipeline}";
            
            var processedCount = 0;
            var droppedCount = 0;
            
            await foreach (var request in requestStream.ReadAllAsync())
            {
                // Hot path: check local bucket first (no Redis call)
                if (TryConsumeTokens(bucketKey, 1, request.EstimatedBytes))
                {
                    try
                    {
                        // Process the request
                        await ProcessSingleRequest(request);
                        processedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "❌ Request processing failed");
                        droppedCount++;
                    }
                }
                else
                {
                    // Rate limited - apply backpressure
                    droppedCount++;
                    
                    if (_state == GatewayState.SEVERE)
                    {
                        // In SEVERE state, stop reading from stream (HTTP/2 backpressure)
                        _logger.LogWarning("🛑 SEVERE state: stopping stream read");
                        break;
                    }
                    
                    // Apply exponential backoff for retries
                    await Task.Delay(CalculateBackoffDelay(droppedCount));
                }
            }

            return new IngestResponse
            {
                ProcessedCount = processedCount,
                DroppedCount = droppedCount,
                Status = _state.ToString()
            };
        }

        /// <summary>
        /// Hot path token consumption (stateless, in-memory only)
        /// </summary>
        private bool TryConsumeTokens(string bucketKey, int messages, int bytes)
        {
            lock (_lockObject)
            {
                if (!_localBuckets.TryGetValue(bucketKey, out var bucket))
                {
                    // Create new bucket if doesn't exist
                    bucket = new LocalTokenBucket();
                    _localBuckets[bucketKey] = bucket;
                }

                return bucket.TryConsume(messages, bytes);
            }
        }

        /// <summary>
        /// Handle successful grant from Regional Budget Bank
        /// </summary>
        private void OnGrantReceived(GrantResult grant)
        {
            lock (_lockObject)
            {
                // Flip to NORMAL/THROTTLE state after first successful grant
                if (_state == GatewayState.SEVERE)
                {
                    _state = GatewayState.NORMAL;
                    _logger.LogInformation("✅ State transition: SEVERE → NORMAL (first grant received)");
                }

                // Refill local buckets proportionally
                foreach (var (key, bucket) in _localBuckets)
                {
                    bucket.AddTokens(grant.GrantedMessages / _localBuckets.Count, 
                                   grant.GrantedBytes / _localBuckets.Count);
                }

                _lastGrantTime = DateTime.UtcNow;
            }
        }

        /// <summary>
        /// Handle grant failure - implement degradation strategy
        /// </summary>
        private void OnGrantFailed(string reason)
        {
            var timeSinceLastGrant = DateTime.UtcNow - _lastGrantTime;
            
            if (timeSinceLastGrant > TimeSpan.FromSeconds(2))
            {
                _state = GatewayState.SEVERE;
                _logger.LogWarning("⚠️ State transition: NORMAL → SEVERE (grant failure: {Reason})", reason);
            }
            else if (timeSinceLastGrant > TimeSpan.FromMilliseconds(500))
            {
                _state = GatewayState.THROTTLE;
                _logger.LogInformation("🐌 State transition: NORMAL → THROTTLE (grant delay)");
            }
        }

        private TimeSpan CalculateBackoffDelay(int attemptCount)
        {
            // Exponential backoff with jitter
            var baseDelay = TimeSpan.FromMilliseconds(Math.Min(100 * Math.Pow(2, attemptCount), 5000));
            var jitter = TimeSpan.FromMilliseconds(new Random().Next(0, 100));
            return baseDelay.Add(jitter);
        }

        private ClientInfo ExtractClientInfo(ServerCallContext context)
        {
            // Extract client information from gRPC metadata
            var headers = context.RequestHeaders;
            
            return new ClientInfo
            {
                Tenant = headers.GetValue("x-tenant") ?? "default",
                Pipeline = headers.GetValue("x-pipeline") ?? "default",
                ClientId = headers.GetValue("x-client-id") ?? "unknown",
                UserAgent = headers.GetValue("user-agent") ?? "unknown"
            };
        }

        private async Task ProcessSingleRequest(IngestRequest request)
        {
            // Simulate request processing (integrate with Flink here)
            await Task.Delay(Random.Shared.Next(1, 10)); // Simulate processing time
            
            // Forward to Flink stream processing
            // await _flinkGateway.SubmitAsync(request);
        }
    }

    /// <summary>
    /// Background service that periodically requests grants from Regional Budget Bank
    /// Implements Netflix's "small batches frequently" pattern
    /// </summary>
    public class BackgroundRefillService : BackgroundService
    {
        private readonly RegionalBudgetBank _budgetBank;
        private readonly ILogger<BackgroundRefillService> _logger;
        private readonly GatewayConfig _config;
        
        // Refill timing (Netflix pattern: 10-20% of burst every 50-150ms)
        private readonly TimeSpan _refillInterval = TimeSpan.FromMilliseconds(100);
        private readonly int _refillBatchSize;
        
        public event Action<GrantResult>? OnGrantReceived;
        public event Action<string>? OnGrantFailed;

        public BackgroundRefillService(
            RegionalBudgetBank budgetBank,
            ILogger<BackgroundRefillService> logger,
            GatewayConfig config)
        {
            _budgetBank = budgetBank;
            _logger = logger;
            _config = config;
            
            // Calculate refill batch size (1-2x per-epoch share)
            _refillBatchSize = (int)(config.LocalBurstSize * 0.15); // 15% of burst capacity
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("🔄 Background Refill Service starting");

            // Immediately request first grant on startup
            await RequestGrant("startup_grant");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await RequestGrant("periodic_refill");
                    await Task.Delay(_refillInterval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Background refill error");
                    OnGrantFailed?.Invoke($"Background service error: {ex.Message}");
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                }
            }
        }

        private async Task RequestGrant(string requestType)
        {
            var currentEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 250;
            var budgetKey = $"budget:{_config.Region}:default:default:{currentEpoch}";
            
            var result = await _budgetBank.RequestGrantAsync(budgetKey, _refillBatchSize, _refillBatchSize * 1024);
            
            if (result.Status == "GRANTED")
            {
                _logger.LogDebug("✅ Grant received: {Messages} msg, {Bytes} bytes ({Type})", 
                    result.GrantedMessages, result.GrantedBytes, requestType);
                OnGrantReceived?.Invoke(result);
            }
            else
            {
                _logger.LogWarning("❌ Grant failed: {Status} ({Type})", result.Status, requestType);
                OnGrantFailed?.Invoke(result.Status);
            }
        }
    }

    /// <summary>
    /// Local token bucket for hot path rate limiting
    /// Implements Netflix's stateless bucket pattern
    /// </summary>
    public class LocalTokenBucket
    {
        private int _messageTokens;
        private int _byteTokens;
        private readonly object _lock = new();

        public LocalTokenBucket(int initialMessages = 0, int initialBytes = 0)
        {
            _messageTokens = initialMessages;
            _byteTokens = initialBytes;
        }

        public bool TryConsume(int messages, int bytes)
        {
            lock (_lock)
            {
                if (_messageTokens >= messages && _byteTokens >= bytes)
                {
                    _messageTokens -= messages;
                    _byteTokens -= bytes;
                    return true;
                }
                return false;
            }
        }

        public void AddTokens(int messages, int bytes)
        {
            lock (_lock)
            {
                _messageTokens += messages;
                _byteTokens += bytes;
            }
        }

        public (int Messages, int Bytes) GetAvailableTokens()
        {
            lock (_lock)
            {
                return (_messageTokens, _byteTokens);
            }
        }
    }

    // Supporting data structures
    public class EpochBudget
    {
        public string Region { get; set; } = string.Empty;
        public string Tenant { get; set; } = string.Empty;
        public string Pipeline { get; set; } = string.Empty;
        public long Epoch { get; set; }
        public int MessageTokens { get; set; }
        public int ByteTokens { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class QuotaAllocation
    {
        public int MessageTokens { get; set; }
        public int ByteTokens { get; set; }
    }

    public class GrantResult
    {
        public int GrantedMessages { get; set; }
        public int GrantedBytes { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class ClientInfo
    {
        public string Tenant { get; set; } = string.Empty;
        public string Pipeline { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
    }

    public class IngestRequest
    {
        public string Data { get; set; } = string.Empty;
        public int EstimatedBytes { get; set; }
        public Dictionary<string, string> Metadata { get; set; } = new();
    }

    public class IngestResponse
    {
        public int ProcessedCount { get; set; }
        public int DroppedCount { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public enum GatewayState
    {
        SEVERE,   // Paused - no processing
        THROTTLE, // Rate limited - reduced processing
        NORMAL    // Normal operation
    }

    public class QuotaControllerConfig
    {
        public string[] Regions { get; set; } = Array.Empty<string>();
        public string[] Tenants { get; set; } = Array.Empty<string>();
        public string[] Pipelines { get; set; } = Array.Empty<string>();
        public string SigningKey { get; set; } = string.Empty;
    }

    public class GatewayConfig
    {
        public string Region { get; set; } = string.Empty;
        public int LocalBurstSize { get; set; } = 1000;
    }
}
```

## 🎯 Fault Tolerance Scenarios

### 1. Gateway Restart / Memory Cache Refresh

**Goal:** No double-spend, no surge, no stuck throttle

**Implementation:**
```csharp
public class GatewayStartupBehavior
{
    public async Task InitializeAsync()
    {
        // Safe by default: start in SEVERE state
        _state = GatewayState.SEVERE;
        _logger.LogInformation("🛑 Gateway starting in SEVERE state (safe by default)");
        
        // Background service immediately requests small grant
        var firstGrant = await _refillService.RequestFirstGrantAsync();
        
        if (firstGrant.Status == "GRANTED")
        {
            _state = GatewayState.NORMAL;
            _logger.LogInformation("✅ First grant received - transitioning to NORMAL");
        }
        else
        {
            _logger.LogWarning("❌ First grant failed - remaining in SEVERE state");
            // Will retry in background until successful
        }
    }
}
```

**What happens when one instance restarts:**
1. ✅ **Starts with 0 tokens** → immediately paused (SEVERE state)
2. ✅ **Pulls small grant from Redis** → once granted, resumes processing
3. ✅ **No other instances affected** → budgets are centrally managed in Redis
4. ✅ **No double counting** → DECRBY operations are atomic

### 2. Redis Dies or Disk Disruption

**Implementation:**
```csharp
public class RedisFailureHandling
{
    private readonly RedisFailoverConfig _failoverConfig;
    
    public async Task<GrantResult> RequestGrantWithFailover(string budgetKey, int messages, int bytes)
    {
        try
        {
            // Try primary Redis cluster
            return await _primaryRedis.RequestGrantAsync(budgetKey, messages, bytes);
        }
        catch (RedisConnectionException ex)
        {
            _logger.LogWarning("⚠️ Primary Redis unavailable: {Error}", ex.Message);
            
            // Decide fail-closed vs fail-open per tenant
            if (_failoverConfig.FailClosed)
            {
                return new GrantResult { Status = "REDIS_UNAVAILABLE" };
            }
            
            // Try regional fallback
            if (_failoverConfig.RegionalFallback != null)
            {
                try
                {
                    var fallbackResult = await _failoverConfig.RegionalFallback
                        .RequestGrantAsync(budgetKey, messages / 2, bytes / 2); // Reduced cap
                    
                    _logger.LogInformation("✅ Using regional fallback with reduced capacity");
                    return fallbackResult;
                }
                catch
                {
                    _logger.LogError("❌ Regional fallback also failed");
                }
            }
            
            // Last resort: allow limited local processing
            return new GrantResult 
            { 
                GrantedMessages = Math.Min(messages, 10), // Very limited
                GrantedBytes = Math.Min(bytes, 1024),
                Status = "LOCAL_FALLBACK" 
            };
        }
    }
}
```

**Recommended Redis Setup:**
```yaml
# Redis Cluster Configuration (Production)
redis_cluster:
  masters: 3
  replicas_per_master: 2
  memory_only: true  # No RDB/AOF for budget keys (ephemeral)
  key_ttl: 500ms     # 2x epoch duration
  security:
    tls: true
    auth: required
  failover:
    timeout: 2s
    retry_limit: 3
```

### 3. Single Source of Truth Latency (Global Coordination)

**Implementation follows Netflix's hierarchical quota pattern:**

```csharp
public class HierarchicalQuotaManagement
{
    /// <summary>
    /// Global Quota Controller mints budgets every epoch
    /// Prevents cross-region coordination in hot path
    /// </summary>
    public async Task MintHierarchicalBudgets()
    {
        // Global quotas (enterprise limits)
        var globalQuota = _config.GlobalLimits;
        
        // Distribute to regions based on capacity and demand
        foreach (var region in _config.Regions)
        {
            var regionQuota = CalculateRegionalQuota(region, globalQuota);
            
            // Further distribute to tenants within region
            foreach (var tenant in _config.Tenants)
            {
                var tenantQuota = CalculateTenantQuota(tenant, regionQuota);
                
                // Final distribution to pipelines
                foreach (var pipeline in _config.Pipelines)
                {
                    var pipelineQuota = CalculatePipelineQuota(pipeline, tenantQuota);
                    
                    // Store in Regional Budget Bank with TTL
                    await StoreRegionalBudget(region, tenant, pipeline, pipelineQuota);
                }
            }
        }
    }
    
    private QuotaAllocation CalculateRegionalQuota(string region, GlobalQuota global)
    {
        // Regions get proportional allocation based on capacity
        var regionCapacity = _config.RegionCapacity[region];
        var totalCapacity = _config.RegionCapacity.Values.Sum();
        
        return new QuotaAllocation
        {
            MessageTokens = (int)(global.MessagesPerEpoch * (regionCapacity / totalCapacity)),
            ByteTokens = (int)(global.BytesPerEpoch * (regionCapacity / totalCapacity))
        };
    }
}
```

**Concrete Production Knobs (Netflix/Uber patterns):**

```csharp
public static class ProductionConfiguration
{
    // Epoch timing (Netflix pattern)
    public static readonly TimeSpan EPOCH_DURATION = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan BUDGET_TTL = TimeSpan.FromMilliseconds(500); // 2x epoch
    
    // Local bucket sizing (Uber pattern)
    public static readonly int LOCAL_BURST_CAPACITY = 1000;  // 1-2x per-epoch share
    public static readonly double REFILL_PERCENTAGE = 0.15;  // 15% of burst per refill
    public static readonly TimeSpan REFILL_INTERVAL = TimeSpan.FromMilliseconds(100);
    
    // Degradation windows
    public static readonly TimeSpan REDIS_UNAVAILABLE_THRESHOLD = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan NO_BUDGETS_THRESHOLD = TimeSpan.FromMilliseconds(500); // 2 epochs
    
    // Rate limiting (dual tokens)
    public static class RateLimits
    {
        public static readonly int MSG_TOKENS_PER_SECOND = 1000;
        public static readonly int BYTE_TOKENS_PER_SECOND = 1024 * 1024; // 1MB/s
    }
    
    // gRPC specific behaviors
    public static class GrpcBehaviors
    {
        public static readonly TimeSpan STREAM_PAUSE_ON_THROTTLE = TimeSpan.FromMilliseconds(100);
        public static readonly int MAX_RETRY_ATTEMPTS = 3;
        public static readonly TimeSpan CIRCUIT_BREAKER_TIMEOUT = TimeSpan.FromSeconds(30);
    }
}
```

## 🧪 Testing the Real-World Pattern

### Load Test Scenario:
```bash
# Simulate Netflix-scale load testing
curl -X POST http://localhost:5000/stress/backpressure \
  --gateways 10 \
  --clients-per-gateway 100 \
  --request-rate 10000 \
  --duration 300s \
  --failure-scenarios "redis_partition,gateway_restart"
```

### Expected Behaviors:
1. **Normal Operation**: 10K req/s distributed across gateways
2. **Redis Partition**: Graceful degradation to local buckets
3. **Gateway Restart**: New instance starts SEVERE → transitions to NORMAL
4. **Budget Exhaustion**: Fair throttling across all gateways

## 📊 Monitoring and Alerting

```csharp
public class ProductionMetrics
{
    // SLI/SLO metrics (Google SRE pattern)
    [Counter] public static readonly Counter RequestsTotal = Metrics
        .CreateCounter("ingress_requests_total", "Total ingress requests", "tenant", "status");
    
    [Histogram] public static readonly Histogram RequestDuration = Metrics
        .CreateHistogram("ingress_request_duration_seconds", "Request duration");
    
    [Gauge] public static readonly Gauge GatewayState = Metrics
        .CreateGauge("ingress_gateway_state", "Gateway state (0=SEVERE, 1=THROTTLE, 2=NORMAL)");
    
    [Gauge] public static readonly Gauge LocalBucketTokens = Metrics
        .CreateGauge("ingress_local_bucket_tokens", "Available tokens in local bucket", "bucket_type");
}
```

## 🎯 Day 3 Exercises - Enterprise Production Backpressure Patterns

These exercises implement the **specific distributed rate limiting concepts** covered in today's theory using real-world business scenarios from Netflix, Uber, and LinkedIn.

### Exercise 3.1: Netflix Global Rate Limiting Controller (90 minutes)
**Business Context**: Netflix API Gateway Global Coordination
**Theory Connection**: Implements **[Step 1: Global Quota Controller (GQC)](#step-1-global-quota-controller-gqc--exercise-31-netflix-global-rate-limiting-controller)** and **[Fault-Tolerant Distributed Rate Limiting Architecture](#🏗️-architecture-overview)**

**Objective**: Build Netflix-scale Global Quota Controller for coordinating API rate limits across 2000+ microservices

**Real-World Scenario**: You're a Netflix SRE implementing their global rate limiting system that coordinates quota distribution across multiple regions for their API gateway handling 1+ billion requests daily during peak streaming hours.

**Implementation Tasks** (directly implementing theory concepts):

1. **Epoch-Based Budget Minting** (30 minutes) - implements theory: Global Quota Controller
   ```bash
   cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions/Exercise31
   dotnet build
   dotnet run --configuration=NetflixGlobalQuotaController
   
   # Verify theory implementation:
   # - Epoch-based budget minting every 250ms (theory connection)
   # - Cross-region coordination prevention (theory connection)
   # - Policy distribution to regional banks (theory connection)
   ```

2. **Pre-mint Budget Futures** (30 minutes) - implements theory: advanced coordination patterns
   ```bash
   # Test pre-minting future budgets (theory connection)
   curl -X POST http://localhost:5000/api/quota/pre-mint -d '{"region":"us-east-1","duration":"PT5M"}'
   
   # Verify theory concepts:
   # - Future budget allocation (theory connection)
   # - Regional quota coordination (theory connection)
   # - Netflix's prevention of coordination in hot path (theory connection)
   ```

3. **Fault Tolerance Testing** (30 minutes) - implements theory: production resilience patterns
   ```bash
   # Simulate Netflix-style failure scenarios (theory connection)
   # Kill Global Quota Controller and verify graceful degradation
   docker stop netflix-gqc-container
   
   # Expected behavior from theory:
   # - Regional Budget Banks continue with cached budgets (theory connection)
   # - No impact on hot path rate limiting (theory connection)
   # - Graceful recovery when GQC restarts (theory connection)
   ```

**Expected Business Value**: Netflix-level coordination with 99.99% API gateway uptime, automated quota distribution across regions, zero hot-path coordination delay.

**🔗 Theory Integration**: This exercise implements all **[Global Quota Controller (GQC)](#step-1-global-quota-controller-gqc--exercise-31-netflix-global-rate-limiting-controller)** concepts including epoch-based minting, policy distribution, and cross-region coordination prevention described in the theory.

### Exercise 3.2: Uber Regional Redis Coordination (120 minutes)
**Business Context**: Uber Regional Budget Bank System
**Theory Connection**: Implements **[Step 2: Regional Budget Bank (RBB)](#step-2-regional-budget-bank-rbb--exercise-32-uber-regional-redis-coordination)** and **[Fault Scenarios](#fault-scenarios)**

**Objective**: Build Uber-scale Regional Budget Bank using Redis for handling 15+ million ride requests with atomic budget operations

**Real-World Scenario**: You're implementing Uber's regional budget coordination system that handles massive traffic spikes during peak hours, requiring atomic DECRBY operations, TTL management, and regional failover capabilities.

**Implementation Tasks** (directly implementing theory concepts):

1. **Atomic Redis Operations** (45 minutes) - implements theory: Regional Budget Bank
   ```bash
   cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions/Exercise32
   dotnet build
   dotnet run --configuration=UberRegionalBudgetBank
   
   # Test atomic DECRBY operations (theory connection)
   redis-cli --eval atomic-budget-decrby.lua , "user:12345" "100" "300"
   
   # Verify theory implementation:
   # - Atomic budget decrements (theory connection)
   # - Fair allocation across requests (theory connection)
   # - TTL management for budget expiration (theory connection)
   ```

2. **Regional Failover Handling** (45 minutes) - implements theory: fault tolerance patterns
   ```bash
   # Simulate Redis cluster failure (theory connection)
   sudo iptables -A INPUT -s redis-cluster-ip -j DROP
   
   # Test theory concepts:
   # - Fail-closed behavior when Redis dies (theory connection)
   # - Regional fallback to RBB-B (theory connection)
   # - Network partition handling (theory connection)
   ```

3. **Performance Under Load** (30 minutes) - implements theory: Uber-scale requirements
   ```bash
   # Load test with 15M+ request simulation (theory connection)
   curl -X POST http://localhost:5000/stress/complex-logic --requests 15000000 --concurrency 10000
   
   # Monitor theory compliance:
   # - Background refill every 250ms (theory connection)
   # - Atomic operations maintaining fairness (theory connection)
   # - TTL cleanup preventing memory leaks (theory connection)
   ```

**Expected Business Value**: Uber-scale budget coordination handling 15M+ daily rides, atomic fairness preventing double-spending, regional resilience during failures.

**🔗 Theory Integration**: This exercise implements all **[Regional Budget Bank (RBB)](#step-2-regional-budget-bank-rbb--exercise-32-uber-regional-redis-coordination)** concepts including atomic Redis operations, TTL management, and failover handling described in the theory.

### Exercise 3.3: LinkedIn High-Performance Gateway (150 minutes)
**Business Context**: LinkedIn API Gateway Hot Path Processing
**Theory Connection**: Implements **[Step 3: gRPC Ingress Gateway](#step-3-grpc-ingress-gateway--exercise-33-linkedin-high-performance-gateway)** and **[Hot Path Rate Limiting](#hot-path-rate-limiting)**

**Objective**: Build LinkedIn-scale gRPC ingress gateway with stateless rate limiting for handling 900+ million user requests

**Real-World Scenario**: You're building LinkedIn's API gateway that processes user requests for their platform, requiring local token buckets, hot path optimization, and "safe by default" startup behavior during traffic spikes.

**Implementation Tasks** (directly implementing theory concepts):

1. **Local Token Bucket Implementation** (60 minutes) - implements theory: gRPC Gateway patterns
   ```bash
   cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions/Exercise33
   dotnet build
   dotnet run --configuration=LinkedInAPIGateway
   
   # Test hot path rate limiting (theory connection)
   grpcurl -d '{"user_id":"12345","request_type":"feed"}' localhost:50051 linkedin.api.ProcessRequest
   
   # Verify theory implementation:
   # - Local token buckets for hot path (theory connection)
   # - Stateless rate limiting (theory connection)
   # - Background refill from Regional Budget Bank (theory connection)
   ```

2. **"Safe by Default" Startup** (45 minutes) - implements theory: fault tolerance
   ```bash
   # Test gateway restart simulation (theory connection)
   kubectl delete pod linkedin-gateway-instance-1
   
   # Verify theory concepts:
   # - SEVERE pause until first grant (theory connection)
   # - No double-spend during startup (theory connection)
   # - Graceful degradation patterns (theory connection)
   ```

3. **Chaos Engineering Validation** (45 minutes) - implements theory: compound failure scenarios
   ```bash
   # Combine multiple failure scenarios (theory connection)
   chaos run --scenario gateway_restart,redis_partition,network_delay
   
   # Test theory resilience:
   # - System stability under compound failures (theory connection)
   # - End-to-end flow control (theory connection)
   # - Circuit breaker activation (theory connection)
   ```

**Expected Business Value**: LinkedIn-scale API gateway with 99.9% uptime during traffic spikes, sub-10ms hot path latency, automatic failure recovery.

**🔗 Theory Integration**: This exercise implements all **[gRPC Ingress Gateway](#step-3-grpc-ingress-gateway--exercise-33-linkedin-high-performance-gateway)** concepts including local token buckets, hot path optimization, and safe startup behavior described in the theory.

### Exercise 3.4: Chaos Engineering Production Validation (60 minutes)
**Business Context**: Netflix/Uber/LinkedIn Compound Failure Testing
**Theory Connection**: Implements **[Fault Scenarios](#fault-scenarios)** and **[Production Monitoring](#production-monitoring)**

**Objective**: Validate the complete distributed rate limiting system under compound failure scenarios matching production chaos engineering practices

**Real-World Scenario**: You're conducting Netflix-style chaos engineering to validate that your distributed rate limiting system maintains stability under the exact failure scenarios that occur in production environments.

**Implementation Tasks** (directly implementing theory concepts):

1. **Combined Failure Scenarios** (30 minutes) - implements theory: comprehensive fault tolerance
   ```bash
   cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions/Exercise34
   
   # Execute compound chaos scenarios (theory connection)
   curl -X POST http://localhost:5000/stress/chaos --scenario all-failures
   
   # Test theory resilience patterns:
   # - Gateway restart + Redis partition + Network delay (theory connection)
   # - Fail-closed behavior verification (theory connection)
   # - End-to-end flow control validation (theory connection)
   ```

2. **Production Monitoring Validation** (30 minutes) - implements theory: observability patterns
   ```bash
   # Validate monitoring during failures (theory connection)
   curl http://localhost:18010/dashboards/distributed-rate-limiting
   
   # Verify theory monitoring:
   # - Circuit breaker status (theory connection)
   # - Budget allocation metrics (theory connection)
   # - Regional failover detection (theory connection)
   ```

**Expected Business Value**: Production-validated resilience matching Netflix/Uber/LinkedIn standards, automated failure detection, comprehensive monitoring.

**🔗 Theory Integration**: This exercise validates all **[Fault Scenarios](#fault-scenarios)** and monitoring patterns described throughout the theory sections.

### Exercise 3.5: Simple BackpressureQueue Implementation (45 minutes)
**Business Context**: Alternative Simple Backpressure Approach
**Theory Connection**: Contrasts with distributed patterns - demonstrates when simple solutions are better

**Objective**: Implement a straightforward semaphore-based backpressure system and compare it with the complex distributed rate limiting patterns from Exercises 3.1-3.4.

**Real-World Scenario**: You need to choose between simple local backpressure vs complex distributed coordination. This exercise shows when simple solutions are more effective.

**Architecture**: `Gateway(producer) → Kafka → Flink → Temporal(processor)` with `BackpressureQueue=2` for all services

**Implementation Tasks**:

1. **Simple BackpressureQueue Implementation** (15 minutes) - demonstrates semaphore-based limiting
   ```bash
   cd LearningCourse/Day03-Production-Backpressure/Exercise-Solutions/Exercise35
   
   # Build and run the simple backpressure demo
   dotnet build
   dotnet run
   
   # Observe BackpressureQueue=2 limiting in action
   # - Gateway: max 2 concurrent sends
   # - Flink: max 2 concurrent processes  
   # - Temporal: max 2 concurrent receives
   ```

2. **Test Three Scenarios** (20 minutes) - validates different partition/load configurations
   ```bash
   # Test scenarios as specified:
   # Scenario 1: 3,000,000 messages | 300 customers | 4 partitions | BackpressureQueue=2
   # Scenario 2: 1,000,000 messages | 300 customers | 8 partitions | BackpressureQueue=2
   # Scenario 3: 1,000,000 messages | 300 customers | 16 partitions | BackpressureQueue=2
   
   # Compare throughput and backpressure behavior across scenarios
   dotnet run -- --scenario 1
   dotnet run -- --scenario 2
   dotnet run -- --scenario 3
   ```

3. **Compare Approaches** (10 minutes) - analyze simple vs complex trade-offs
   ```bash
   # Review comparison documentation
   cat Exercise35/README.md
   
   # Key comparisons:
   # - Simple: Easy to understand, fixed limits, no coordination
   # - Complex: Adaptive, global fairness, enterprise scale
   # - Use cases for each approach
   ```

**Expected Results**: Clear understanding of when to use simple backpressure vs distributed rate limiting, with concrete performance comparisons.

**Expected Business Value**: Practical guidance for choosing appropriate backpressure patterns based on system requirements and operational complexity.

**🔗 Theory Integration**: This exercise provides practical comparison between simple and complex approaches, helping determine the right tool for the right job.

## 📚 References and Further Reading

### 📖 Official Documentation
- **[Apache Flink AsyncSink Rate Limiting](https://flink.apache.org/2022/11/25/optimising-the-throughput-of-async-sinks-using-a-custom-ratelimitingstrategy/)** - Official Flink 2.1.0 rate limiting patterns
- **[Netflix Zuul 2 Architecture](https://netflixtechblog.com/zuul-2-the-netflix-journey-to-asynchronous-non-blocking-systems-45947377fb5c)** - Production distributed rate limiting
- **[Uber's Rate Limiting at Scale](https://eng.uber.com/scaling-api-with-rate-limiter/)** - Regional budget bank patterns

### 🏛️ Internal References
- **[Getting Started Guide](../../docs/getting-started.md)** - Basic setup and quick start
- **[Local Testing Setup](../../docs/local-testing-setup.md)** - Development environment

### 🔬 Academic References
- **[Distributed Rate Limiting Algorithms](https://arxiv.org/abs/1808.03559)** - Theoretical foundations
- **[Consensus in Distributed Systems](https://raft.github.io/)** - Raft consensus for coordination

## 🎯 Day 3 Completion Checklist

- [ ] Implemented Global Quota Controller with epoch-based budget minting
- [ ] Built Regional Budget Bank with atomic Redis operations
- [ ] Created gRPC Ingress Gateway with local token buckets
- [ ] Tested all three fault tolerance scenarios (restart, Redis failure, coordination)
- [ ] Validated "safe by default" startup behavior
- [ ] Configured production monitoring and alerting
- [ ] Completed chaos engineering exercises
- [ ] Implemented simple BackpressureQueue alternative approach
- [ ] Compared simple vs complex backpressure patterns
- [ ] Tested three BackpressureQueue scenarios with different configurations
- [ ] Documented lessons learned and operational procedures

## 📚 Preparation for Day 4

Tomorrow: **Enterprise Observability with OpenTelemetry** - Complete monitoring stack

**References to review:**
- [OpenTelemetry Best Practices](https://opentelemetry.io/docs/best-practices/)
- [Google SRE Monitoring](https://sre.google/sre-book/monitoring-distributed-systems/)

---

**Next**: [Day 4: Enterprise Observability with OpenTelemetry →](../Day04-Enterprise-Observability/README.md)
---

## 🗺️ Course Navigation
**[← Day 2: AI-Enhanced Stream Processing](../Day02-AI-Stream-Processing/)** | **[Course Overview](../README.md)** | **[Next: Day 4 - Enterprise Observability →](../Day04-Enterprise-Observability/)**

**Course Progress**: Day 3 of 14 Complete ✅