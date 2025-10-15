using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Exercise103;

/// <summary>
/// Execute memory optimization scenarios with real Kafka infrastructure
/// </summary>
public class MemoryScenario
{
    private readonly string _kafkaBootstrapServers;
    private readonly string _inputTopic;
    private readonly string _outputTopic;

    public MemoryScenario(string kafkaBootstrapServers, string inputTopic, string outputTopic)
    {
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _inputTopic = inputTopic;
        _outputTopic = outputTopic;
    }

    /// <summary>
    /// Run baseline scenario without optimization
    /// </summary>
    public async Task<MemoryMetrics> RunBaselineAsync(int eventCount, MemoryMonitor monitor)
    {
        var stopwatch = Stopwatch.StartNew();
        var profile = monitor.CaptureProfile();
        monitor.StartMonitoring();

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.Leader
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise103-baseline",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        // Produce events (no optimization)
        for (int i = 0; i < eventCount; i++)
        {
            var evt = MemoryEvent.CreateSample(1024);
            var json = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(_inputTopic, new Message<string, string>
            {
                Key = evt.Id,
                Value = json
            });
        }

        producer.Flush(TimeSpan.FromSeconds(5));

        // Consume and process (no optimization)
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    // Deserialize (creates new objects each time)
                    var evt = JsonSerializer.Deserialize<MemoryEvent>(consumeResult.Message.Value);
                    
                    // Simulate processing
                    if (evt != null)
                    {
                        ProcessEvent(evt);
                        processedCount++;
                    }

                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        stopwatch.Stop();
        monitor.StopMonitoring();
        monitor.CompleteProfile(profile);

        var (avgHeapMB, peakWorkingSetMB, allocationRateMBPerSec) = monitor.GetStatistics();
        var (gen0, gen1, gen2) = profile.GetCollectionCounts();

        return new MemoryMetrics
        {
            Scenario = "Baseline (No Optimization)",
            EventsProcessed = processedCount,
            TotalAllocatedBytes = profile.GetTotalAllocated(),
            Gen0Collections = gen0,
            Gen1Collections = gen1,
            Gen2Collections = gen2,
            PeakWorkingSet = peakWorkingSetMB * 1024 * 1024,
            AverageHeapSize = avgHeapMB,
            AllocationRateMBPerSec = allocationRateMBPerSec,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds
        };
    }

    /// <summary>
    /// Run scenario with object pooling
    /// </summary>
    public async Task<MemoryMetrics> RunWithObjectPoolingAsync(int eventCount, MemoryMonitor monitor)
    {
        var stopwatch = Stopwatch.StartNew();
        var profile = monitor.CaptureProfile();
        monitor.StartMonitoring();

        // Create object pool for StringBuilder reuse
        var stringBuilderPool = new ObjectPool<StringBuilder>(
            () => new StringBuilder(2048),
            sb => sb.Clear(),
            maxPoolSize: 50);

        stringBuilderPool.Prewarm(10);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.Leader
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise103-pooling",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        // Produce events
        for (int i = 0; i < eventCount; i++)
        {
            var evt = MemoryEvent.CreateSample(1024);
            var json = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(_inputTopic, new Message<string, string>
            {
                Key = evt.Id,
                Value = json
            });
        }

        producer.Flush(TimeSpan.FromSeconds(5));

        // Consume and process with pooling
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    var evt = JsonSerializer.Deserialize<MemoryEvent>(consumeResult.Message.Value);
                    
                    if (evt != null)
                    {
                        // Use pooled StringBuilder for string operations
                        using var pooledSb = stringBuilderPool.AcquireScoped();
                        ProcessEventWithPooling(evt, pooledSb.Object);
                        processedCount++;
                    }

                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        stopwatch.Stop();
        monitor.StopMonitoring();
        monitor.CompleteProfile(profile);

        var (avgHeapMB, peakWorkingSetMB, allocationRateMBPerSec) = monitor.GetStatistics();
        var (gen0, gen1, gen2) = profile.GetCollectionCounts();
        var (poolHits, poolMisses, _, poolEfficiency) = stringBuilderPool.GetStatistics();

        return new MemoryMetrics
        {
            Scenario = "Object Pooling",
            EventsProcessed = processedCount,
            TotalAllocatedBytes = profile.GetTotalAllocated(),
            Gen0Collections = gen0,
            Gen1Collections = gen1,
            Gen2Collections = gen2,
            PeakWorkingSet = peakWorkingSetMB * 1024 * 1024,
            AverageHeapSize = avgHeapMB,
            AllocationRateMBPerSec = allocationRateMBPerSec,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            ObjectPoolHits = (int)poolHits,
            ObjectPoolMisses = (int)poolMisses
        };
    }

    /// <summary>
    /// Run scenario with LRU cache
    /// </summary>
    public async Task<MemoryMetrics> RunWithCachingAsync(int eventCount, MemoryMonitor monitor)
    {
        var stopwatch = Stopwatch.StartNew();
        var profile = monitor.CaptureProfile();
        monitor.StartMonitoring();

        // Create LRU cache for user lookups
        var userCache = new LRUCache<string, string>(capacity: 100);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.Leader
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise103-caching",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        // Produce events
        for (int i = 0; i < eventCount; i++)
        {
            var evt = MemoryEvent.CreateSample(1024);
            var json = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(_inputTopic, new Message<string, string>
            {
                Key = evt.Id,
                Value = json
            });
        }

        producer.Flush(TimeSpan.FromSeconds(5));

        // Consume and process with caching
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    var evt = JsonSerializer.Deserialize<MemoryEvent>(consumeResult.Message.Value);
                    
                    if (evt != null)
                    {
                        // Use cache for user lookups
                        var userData = userCache.GetOrAdd(evt.UserId, userId => $"UserData_{userId}");
                        ProcessEventWithCache(evt, userData);
                        processedCount++;
                    }

                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        stopwatch.Stop();
        monitor.StopMonitoring();
        monitor.CompleteProfile(profile);

        var (avgHeapMB, peakWorkingSetMB, allocationRateMBPerSec) = monitor.GetStatistics();
        var (gen0, gen1, gen2) = profile.GetCollectionCounts();
        var (cacheHits, cacheMisses, _, cacheHitRatio) = userCache.GetStatistics();

        return new MemoryMetrics
        {
            Scenario = "LRU Cache",
            EventsProcessed = processedCount,
            TotalAllocatedBytes = profile.GetTotalAllocated(),
            Gen0Collections = gen0,
            Gen1Collections = gen1,
            Gen2Collections = gen2,
            PeakWorkingSet = peakWorkingSetMB * 1024 * 1024,
            AverageHeapSize = avgHeapMB,
            AllocationRateMBPerSec = allocationRateMBPerSec,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            CacheHits = (int)cacheHits,
            CacheMisses = (int)cacheMisses
        };
    }

    /// <summary>
    /// Run scenario with combined optimizations
    /// </summary>
    public async Task<MemoryMetrics> RunCombinedOptimizationAsync(int eventCount, MemoryMonitor monitor)
    {
        var stopwatch = Stopwatch.StartNew();
        var profile = monitor.CaptureProfile();
        monitor.StartMonitoring();

        // Object pool and cache together
        var stringBuilderPool = new ObjectPool<StringBuilder>(
            () => new StringBuilder(2048),
            sb => sb.Clear(),
            maxPoolSize: 50);
        stringBuilderPool.Prewarm(10);

        var userCache = new LRUCache<string, string>(capacity: 100);

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = true,
            Acks = Acks.Leader
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise103-combined",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        // Produce events
        for (int i = 0; i < eventCount; i++)
        {
            var evt = MemoryEvent.CreateSample(1024);
            var json = JsonSerializer.Serialize(evt);
            await producer.ProduceAsync(_inputTopic, new Message<string, string>
            {
                Key = evt.Id,
                Value = json
            });
        }

        producer.Flush(TimeSpan.FromSeconds(5));

        // Consume and process with all optimizations
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    var evt = JsonSerializer.Deserialize<MemoryEvent>(consumeResult.Message.Value);
                    
                    if (evt != null)
                    {
                        using var pooledSb = stringBuilderPool.AcquireScoped();
                        var userData = userCache.GetOrAdd(evt.UserId, userId => $"UserData_{userId}");
                        ProcessEventOptimized(evt, pooledSb.Object, userData);
                        processedCount++;
                    }

                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        stopwatch.Stop();
        monitor.StopMonitoring();
        monitor.CompleteProfile(profile);

        var (avgHeapMB, peakWorkingSetMB, allocationRateMBPerSec) = monitor.GetStatistics();
        var (gen0, gen1, gen2) = profile.GetCollectionCounts();
        var (poolHits, poolMisses, _, _) = stringBuilderPool.GetStatistics();
        var (cacheHits, cacheMisses, _, _) = userCache.GetStatistics();

        return new MemoryMetrics
        {
            Scenario = "Combined (Pool + Cache)",
            EventsProcessed = processedCount,
            TotalAllocatedBytes = profile.GetTotalAllocated(),
            Gen0Collections = gen0,
            Gen1Collections = gen1,
            Gen2Collections = gen2,
            PeakWorkingSet = peakWorkingSetMB * 1024 * 1024,
            AverageHeapSize = avgHeapMB,
            AllocationRateMBPerSec = allocationRateMBPerSec,
            ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
            ObjectPoolHits = (int)poolHits,
            ObjectPoolMisses = (int)poolMisses,
            CacheHits = (int)cacheHits,
            CacheMisses = (int)cacheMisses
        };
    }

    private void ProcessEvent(MemoryEvent evt)
    {
        // Simulate processing with new allocations
        var processed = $"{evt.Id}:{evt.UserId}:{evt.Data.Length}";
        _ = processed.ToUpper();
    }

    private void ProcessEventWithPooling(MemoryEvent evt, StringBuilder sb)
    {
        // Use pooled StringBuilder
        sb.Append(evt.Id).Append(':').Append(evt.UserId).Append(':').Append(evt.Data.Length);
        _ = sb.ToString().ToUpper();
    }

    private void ProcessEventWithCache(MemoryEvent evt, string userData)
    {
        // Use cached data
        var processed = $"{evt.Id}:{userData}:{evt.Data.Length}";
        _ = processed.ToUpper();
    }

    private void ProcessEventOptimized(MemoryEvent evt, StringBuilder sb, string userData)
    {
        // Use both pool and cache
        sb.Append(evt.Id).Append(':').Append(userData).Append(':').Append(evt.Data.Length);
        _ = sb.ToString().ToUpper();
    }
}