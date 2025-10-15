using System.Diagnostics;
using System.Text.Json;
using Confluent.Kafka;
using FlinkDotNet.DataStream;
using Serilog;

namespace Exercise102;

/// <summary>
/// Simulates multiple processing nodes via Flink parallelism and tracks node-level metrics
/// </summary>
public class NodeSimulator
{
    private readonly string _kafkaBootstrapServers;
    private readonly string _kafkaFlinkBootstrapServers;
    private readonly string _inputTopic;
    private readonly string _outputTopic;
    private readonly string _consumerGroup;

    public NodeSimulator(
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
    /// Submit Flink job with specified node count (parallelism)
    /// </summary>
    public async Task<IJobClient> SubmitFlinkJobAsync(string scenarioName, int nodeCount)
    {
        Log.Information("   Submitting Flink job: {Scenario} (Nodes={NodeCount})",
            scenarioName, nodeCount);

        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Configure buffer timeout for optimal throughput
        environment.SetBufferTimeout(100);

        // Source: Read from Kafka with parallelism matching node count
        var sourceStream = environment.FromKafka(
            topic: _inputTopic,
            bootstrapServers: _kafkaFlinkBootstrapServers,
            groupId: $"{_consumerGroup}-{scenarioName.Replace(" ", "-").ToLower()}",
            startingOffsets: "earliest"
        ).SetParallelism(nodeCount);

        // Map: Process events with node simulation
        var processedStream = sourceStream
            .Map(new NodeProcessingFunction(nodeCount))
            .SetParallelism(nodeCount);

        // Sink: Write to Kafka
        processedStream
            .SinkToKafka(_outputTopic, _kafkaFlinkBootstrapServers)
            .SetParallelism(nodeCount);

        // Execute job
        var jobClient = await environment.ExecuteAsync($"Exercise102-{scenarioName}-N{nodeCount}");

        Log.Information("   [SUCCESS] Flink job submitted - JobId: {JobId}", jobClient.GetJobId());
        return jobClient;
    }

    /// <summary>
    /// Run a complete scaling scenario
    /// </summary>
    public async Task<ScalingMetrics> RunScalingScenarioAsync(
        string scenarioName,
        int nodeCount,
        int eventsPerSecond,
        int durationSeconds,
        EventGenerator eventGenerator,
        LoadBalancer loadBalancer)
    {
        Log.Information("🎯 Running scenario: {Scenario} (Nodes={NodeCount})",
            scenarioName, nodeCount);

        var startTime = DateTime.UtcNow;
        IJobClient? jobClient = null;

        try
        {
            // Set scenario in load balancer
            loadBalancer.SetCurrentScenario(scenarioName, nodeCount);

            // Step 1: Submit Flink job
            jobClient = await SubmitFlinkJobAsync(scenarioName, nodeCount);
            await Task.Delay(TimeSpan.FromSeconds(5)); // Wait for job to start

            // Step 2: Generate events
            Log.Information("   Generating events...");
            var eventsGenerated = await eventGenerator.GenerateEventsAsync(
                scenarioName, eventsPerSecond, durationSeconds, nodeCount);

            // Step 3: Wait for processing
            Log.Information("   Waiting for processing (10 seconds)...");
            await Task.Delay(TimeSpan.FromSeconds(10));

            // Step 4: Consume processed events
            Log.Information("   Consuming processed events...");
            var processedEvents = await ConsumeProcessedEventsAsync(scenarioName, (int)eventsGenerated);

            var endTime = DateTime.UtcNow;

            // Step 5: Calculate metrics
            var metrics = loadBalancer.CalculateScalingMetrics(
                scenarioName,
                nodeCount,
                startTime,
                endTime,
                eventsGenerated,
                processedEvents);

            Log.Information("✅ Scenario completed: {Scenario}", scenarioName);
            Log.Information("   • Events Generated: {Generated:N0}", metrics.EventsGenerated);
            Log.Information("   • Events Processed: {Processed:N0}", metrics.EventsProcessed);
            Log.Information("   • Throughput: {Throughput:F1} events/sec", metrics.ThroughputEventsPerSec);
            Log.Information("   • Avg Latency: {Latency:F1} ms", metrics.AverageLatency.TotalMilliseconds);
            Log.Information("   • Load Distribution CV: {CV:F3}", metrics.LoadDistributionCoefficient);
            Log.Information("   • Partitions/Node: {PartitionsPerNode:F1}", metrics.PartitionsPerNode);

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
    private async Task<List<ProcessedScalingEvent>> ConsumeProcessedEventsAsync(string scenarioName, int expectedCount)
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

        var processedEvents = new List<ProcessedScalingEvent>();
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
                var result = consumer.Consume(TimeSpan.FromMilliseconds(100));

                if (result != null)
                {
                    try
                    {
                        var processedEvent = JsonSerializer.Deserialize<ProcessedScalingEvent>(result.Message.Value);
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

                // Yield to other async operations every iteration
                await Task.Yield();
            }
            catch (ConsumeException ex)
            {
                Log.Error(ex, "Error consuming processed event");
                break;
            }
        }

        consumer.Close();
        Log.Information("   [SUCCESS] Consumed {Count} processed events", processedEvents.Count);
        return processedEvents;
    }
}

/// <summary>
/// Flink Map function that simulates node processing with partition awareness
/// </summary>
public class NodeProcessingFunction : IMapFunction<string, string>
{
    private readonly int _nodeCount;
    private static int _instanceCounter = 0;
    private readonly int _nodeId;

    public NodeProcessingFunction(int nodeCount)
    {
        _nodeCount = nodeCount;
        _nodeId = Interlocked.Increment(ref _instanceCounter) % nodeCount;
    }

    public string Map(string input)
    {
        try
        {
            var scalingEvent = JsonSerializer.Deserialize<ScalingEvent>(input);
            if (scalingEvent == null)
                return string.Empty;

            var processingStart = DateTime.UtcNow;

            // Simulate realistic processing work
            var processingTimeMs = SimulateProcessing(scalingEvent.PayloadSize, _nodeCount);

            // Determine which node should handle this event based on partition
            var assignedNodeId = scalingEvent.PartitionKey % _nodeCount;

            var processedEvent = new ProcessedScalingEvent
            {
                EventId = scalingEvent.EventId,
                OriginalTimestamp = scalingEvent.Timestamp,
                ProcessedTimestamp = DateTime.UtcNow,
                ProcessingTimeMs = processingTimeMs,
                ProcessedData = $"Processed by Node-{assignedNodeId}: {scalingEvent.Data}",
                NodeId = assignedNodeId,
                Partition = scalingEvent.PartitionKey,
                Scenario = scalingEvent.Scenario
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
    /// More nodes = better efficiency per event (LinkedIn pattern)
    /// </summary>
    private long SimulateProcessing(int payloadSize, int nodeCount)
    {
        var stopwatch = Stopwatch.StartNew();

        // Base processing time
        var baseProcessingMs = payloadSize / 10;

        // Efficiency factor: More nodes = slightly better per-event processing
        // But with diminishing returns to simulate resource contention
        var efficiencyFactor = nodeCount switch
        {
            1 => 1.0,    // Baseline
            2 => 0.8,    // 20% faster per event
            4 => 0.65,   // 35% faster, diminishing returns
            8 => 0.55,   // 45% faster, more diminishing returns
            _ => 1.0 / Math.Sqrt(nodeCount)
        };

        var adjustedProcessingMs = (int)(baseProcessingMs * efficiencyFactor);

        // Simulate CPU work
        Thread.Sleep(Math.Max(3, adjustedProcessingMs));

        // Add computational work
        double result = 0;
        for (int i = 0; i < payloadSize / 2; i++)
        {
            result += Math.Sqrt(i) * Math.Sin(i);
        }

        stopwatch.Stop();
        return stopwatch.ElapsedMilliseconds;
    }
}