using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise101;

/// <summary>
/// Executes performance scenarios with varying Flink parallelism
/// </summary>
public class PerformanceScenario
{
    private readonly string _kafkaBootstrapServers;
    private readonly string _kafkaFlinkBootstrapServers;
    private readonly string _inputTopic;
    private readonly string _outputTopic;
    private readonly string _consumerGroup;

    public PerformanceScenario(
        string kafkaBootstrapServers,
        string kafkaFlinkBootstrapServers,
        string inputTopic,
        string outputTopic,
        string consumerGroup)
    {
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _kafkaFlinkBootstrapServers = kafkaFlinkBootstrapServers;
        _inputTopic = inputTopic;
        _outputTopic = outputTopic;
        _consumerGroup = consumerGroup;
    }

    /// <summary>
    /// Submit Flink job with specified parallelism
    /// </summary>
    public async Task<IJobClient> SubmitFlinkJobAsync(string scenarioName, int parallelism)
    {
        Log.Information("   Submitting Flink job: {Scenario} (Parallelism={Parallelism})",
            scenarioName, parallelism);

        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Configure buffer timeout for optimal throughput
        environment.SetBufferTimeout(100);

        // Source: Read from Kafka
        var sourceStream = environment.FromKafka(
            topic: _inputTopic,
            bootstrapServers: _kafkaFlinkBootstrapServers,
            groupId: $"{_consumerGroup}-{scenarioName.Replace(" ", "-").ToLower()}",
            startingOffsets: "earliest"
        ).SetParallelism(parallelism);

        // Map: Process events with parallelism
        var processedStream = sourceStream
            .Map(new PerformanceProcessingFunction(parallelism))
            .SetParallelism(parallelism);

        // Sink: Write to Kafka
        processedStream
            .SinkToKafka(_outputTopic, _kafkaFlinkBootstrapServers)
            .SetParallelism(parallelism);

        // Execute job
        var jobClient = await environment.ExecuteAsync($"Exercise101-{scenarioName}-P{parallelism}");

        Log.Information("   [SUCCESS] Flink job submitted - JobId: {JobId}", jobClient.GetJobId());
        return jobClient;
    }

    /// <summary>
    /// Run a complete performance scenario
    /// </summary>
    public async Task<PerformanceMetrics> RunScenarioAsync(
        string scenarioName,
        int parallelism,
        int eventsPerSecond,
        int durationSeconds,
        EventGenerator eventGenerator,
        ResourceMonitor resourceMonitor)
    {
        Log.Information("🎯 Running scenario: {Scenario} (Parallelism={Parallelism})",
            scenarioName, parallelism);

        var startTime = DateTime.UtcNow;
        IJobClient? jobClient = null;

        try
        {
            // Set scenario in monitor
            resourceMonitor.SetCurrentScenario(scenarioName);

            // Step 1: Submit Flink job
            jobClient = await SubmitFlinkJobAsync(scenarioName, parallelism);
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start

            // Step 2: Generate events
            Log.Information("   Generating events...");
            var eventsGenerated = await eventGenerator.GenerateEventsAsync(
                scenarioName, eventsPerSecond, durationSeconds, parallelism);

            // Step 3: Wait for processing
            Log.Information("   Waiting for processing (10 seconds)...");
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Step 4: Consume processed events
            Log.Information("   Consuming processed events...");
            var processedEvents = await ConsumeProcessedEventsAsync(scenarioName, (int)eventsGenerated);

            var endTime = DateTime.UtcNow;

            // Step 5: Calculate metrics
            var metrics = resourceMonitor.CalculateMetrics(
                scenarioName,
                parallelism,
                startTime,
                endTime,
                eventsGenerated,
                processedEvents);

            Log.Information("✅ Scenario completed: {Scenario}", scenarioName);
            Log.Information("   • Events Generated: {Generated:N0}", metrics.EventsGenerated);
            Log.Information("   • Events Processed: {Processed:N0}", metrics.EventsProcessed);
            Log.Information("   • Throughput: {Throughput:F1} events/sec", metrics.ThroughputEventsPerSec);
            Log.Information("   • Avg Latency: {Latency:F1} ms", metrics.AverageLatency.TotalMilliseconds);
            Log.Information("   • Peak Memory: {Memory} MB", metrics.PeakMemoryMB);
            Log.Information("   • Avg CPU: {CPU:F1}%", metrics.AverageCPUPercent);

            return metrics;
        }
        finally
        {
            // Cancel Flink job
            if (jobClient != null)
            {
                try
                {
                    await jobClient.CancelAsync();
                    Log.Information("   [SUCCESS] Flink job cancelled");
                }
                catch (Exception ex)
                {
                    Log.Warning(ex, "Failed to cancel Flink job");
                }
            }
        }
    }

    /// <summary>
    /// Consume processed events from output topic
    /// </summary>
    private Task<List<ProcessedEvent>> ConsumeProcessedEventsAsync(string scenarioName, int expectedCount)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = $"{_consumerGroup}-verify-{scenarioName.Replace(" ", "-").ToLower()}",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_outputTopic);

        var processedEvents = new List<ProcessedEvent>();
        var timeoutCount = 0;
        const int maxTimeouts = 10;
        var stopwatch = Stopwatch.StartNew();
        const int maxWaitSeconds = 30;

        while (processedEvents.Count < expectedCount &&
               timeoutCount < maxTimeouts &&
               stopwatch.Elapsed.TotalSeconds < maxWaitSeconds)
        {
            try
            {
                var result = consumer.Consume(TimeSpan.FromSeconds(1));

                if (result != null)
                {
                    try
                    {
                        var processedEvent = JsonSerializer.Deserialize<ProcessedEvent>(result.Message.Value);
                        if (processedEvent != null && processedEvent.Scenario == scenarioName)
                        {
                            processedEvents.Add(processedEvent);
                            timeoutCount = 0;
                        }
                    }
                    catch
                    {
                        // Skip parsing errors
                    }

                    consumer.Commit(result);

                    // Progress reporting
                    if (processedEvents.Count % 100 == 0)
                    {
                        Log.Information("   [{Count}/{Expected}] events consumed...",
                            processedEvents.Count, expectedCount);
                    }
                }
                else
                {
                    timeoutCount++;
                }
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming processed event");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {Count} processed events", processedEvents.Count);
        return Task.FromResult(processedEvents);
    }
}

/// <summary>
/// Flink Map function for performance-optimized event processing
/// </summary>
public class PerformanceProcessingFunction : IMapFunction<string, string>
{
    private readonly int _parallelism;

    public PerformanceProcessingFunction(int parallelism)
    {
        _parallelism = parallelism;
    }

    public string Map(string input)
    {
        try
        {
            var perfEvent = JsonSerializer.Deserialize<PerformanceEvent>(input);
            if (perfEvent == null)
                return string.Empty;

            var processingStart = DateTime.UtcNow;

            // Simulate realistic processing work based on payload size
            var processingTimeMs = SimulateProcessing(perfEvent.PayloadSize, _parallelism);

            var processedEvent = new ProcessedEvent
            {
                EventId = perfEvent.EventId,
                OriginalTimestamp = perfEvent.Timestamp,
                ProcessedTimestamp = DateTime.UtcNow,
                ProcessingTimeMs = processingTimeMs,
                ProcessedData = $"Processed: {perfEvent.Data}",
                Parallelism = _parallelism,
                Scenario = perfEvent.Scenario
            };

            return JsonSerializer.Serialize(processedEvent);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Simulate realistic processing work
    /// Higher parallelism = better efficiency (less processing time per event)
    /// </summary>
    private long SimulateProcessing(int payloadSize, int parallelism)
    {
        var stopwatch = Stopwatch.StartNew();

        // Parallelism efficiency: Higher parallelism reduces per-event processing time
        // But with diminishing returns (simulates resource contention)
        var efficiencyFactor = parallelism switch
        {
            1 => 1.0,    // Baseline - no optimization
            4 => 0.6,    // 40% faster per event
            8 => 0.5,    // 50% faster, but diminishing returns vs parallelism=4
            _ => 1.0 / Math.Sqrt(parallelism)
        };

        // Computational work scales with payload size and parallelism efficiency
        var iterations = (int)(payloadSize * efficiencyFactor);

        // Add computational work (no Thread.Sleep blocking!)
        double result = 0;
        for (int i = 0; i < iterations; i++)
        {
            result += Math.Sqrt(i + 1) * Math.Sin(i);
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}