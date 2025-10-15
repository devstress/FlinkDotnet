using Serilog;

namespace Exercise102;

/// <summary>
/// Tracks and analyzes load distribution across simulated nodes
/// </summary>
public class LoadBalancer
{
    private string _currentScenario = string.Empty;
    private int _currentNodeCount = 0;
    private const int TotalPartitions = 8;

    public void SetCurrentScenario(string scenario, int nodeCount)
    {
        _currentScenario = scenario;
        _currentNodeCount = nodeCount;
    }

    /// <summary>
    /// Calculate comprehensive scaling metrics from processed events
    /// </summary>
    public ScalingMetrics CalculateScalingMetrics(
        string scenarioName,
        int nodeCount,
        DateTime startTime,
        DateTime endTime,
        long eventsGenerated,
        List<ProcessedScalingEvent> processedEvents)
    {
        var metrics = new ScalingMetrics
        {
            Scenario = scenarioName,
            NodeCount = nodeCount,
            TotalPartitions = TotalPartitions,
            StartTime = startTime,
            EndTime = endTime,
            Duration = endTime - startTime,
            EventsGenerated = eventsGenerated,
            EventsProcessed = processedEvents.Count,
            PartitionsPerNode = (double)TotalPartitions / nodeCount
        };

        // Calculate throughput
        if (metrics.Duration.TotalSeconds > 0)
        {
            metrics.ThroughputEventsPerSec = metrics.EventsProcessed / metrics.Duration.TotalSeconds;
            metrics.SuccessRate = eventsGenerated > 0 ? (double)metrics.EventsProcessed / eventsGenerated : 0;
        }

        // Calculate latency metrics
        if (processedEvents.Any())
        {
            var latencies = processedEvents
                .Select(e => e.ProcessedTimestamp - e.OriginalTimestamp)
                .OrderBy(l => l)
                .ToList();

            metrics.AverageLatency = TimeSpan.FromMilliseconds(latencies.Average(l => l.TotalMilliseconds));
            metrics.MinLatency = latencies.First();
            metrics.MaxLatency = latencies.Last();
            metrics.P95Latency = latencies[(int)(latencies.Count * 0.95)];
            metrics.P99Latency = latencies[(int)(latencies.Count * 0.99)];
        }

        // Calculate node-level metrics and load distribution
        metrics.NodeMetrics = CalculateNodeMetrics(processedEvents, nodeCount, metrics.Duration);
        
        // Calculate load distribution coefficient (coefficient of variation)
        if (metrics.NodeMetrics.Any())
        {
            var loads = metrics.NodeMetrics.Select(n => (double)n.EventsProcessed).ToList();
            var avgLoad = loads.Average();
            var variance = loads.Sum(l => Math.Pow(l - avgLoad, 2)) / loads.Count;
            var stdDev = Math.Sqrt(variance);
            metrics.LoadDistributionCoefficient = avgLoad > 0 ? stdDev / avgLoad : 0;
        }

        return metrics;
    }

    /// <summary>
    /// Calculate metrics for each node
    /// </summary>
    private List<NodeMetrics> CalculateNodeMetrics(
        List<ProcessedScalingEvent> processedEvents,
        int nodeCount,
        TimeSpan duration)
    {
        var nodeMetricsList = new List<NodeMetrics>();

        // Group events by node
        var eventsByNode = processedEvents
            .GroupBy(e => e.NodeId)
            .OrderBy(g => g.Key);

        foreach (var nodeGroup in eventsByNode)
        {
            var nodeId = nodeGroup.Key;
            var nodeEvents = nodeGroup.ToList();

            var nodeMetrics = new NodeMetrics
            {
                NodeId = nodeId,
                NodeCount = nodeCount,
                EventsProcessed = nodeEvents.Count
            };

            // Calculate throughput
            if (duration.TotalSeconds > 0)
            {
                nodeMetrics.ThroughputEventsPerSec = nodeEvents.Count / duration.TotalSeconds;
            }

            // Calculate average latency
            if (nodeEvents.Any())
            {
                var latencies = nodeEvents.Select(e => e.ProcessedTimestamp - e.OriginalTimestamp);
                nodeMetrics.AverageLatency = TimeSpan.FromMilliseconds(
                    latencies.Average(l => l.TotalMilliseconds));
            }

            // Track assigned partitions
            nodeMetrics.AssignedPartitions = nodeEvents
                .Select(e => e.Partition)
                .Distinct()
                .OrderBy(p => p)
                .ToList();

            // Calculate load percentage
            var totalEvents = processedEvents.Count;
            nodeMetrics.LoadPercentage = totalEvents > 0
                ? (double)nodeEvents.Count / totalEvents * 100
                : 0;

            nodeMetricsList.Add(nodeMetrics);
        }

        // Fill in nodes that didn't process any events
        for (int i = 0; i < nodeCount; i++)
        {
            if (!nodeMetricsList.Any(n => n.NodeId == i))
            {
                nodeMetricsList.Add(new NodeMetrics
                {
                    NodeId = i,
                    NodeCount = nodeCount,
                    EventsProcessed = 0,
                    ThroughputEventsPerSec = 0,
                    AverageLatency = TimeSpan.Zero,
                    AssignedPartitions = new List<int>(),
                    LoadPercentage = 0
                });
            }
        }

        return nodeMetricsList.OrderBy(n => n.NodeId).ToList();
    }

    /// <summary>
    /// Analyze load distribution quality
    /// </summary>
    public LoadDistributionAnalysis AnalyzeLoadDistribution(ScalingMetrics metrics)
    {
        var nodeLoads = metrics.NodeMetrics.Select(n => (double)n.EventsProcessed).ToList();
        
        var avgLoad = nodeLoads.Any() ? nodeLoads.Average() : 0;
        var minLoad = nodeLoads.Any() ? nodeLoads.Min() : 0;
        var maxLoad = nodeLoads.Any() ? nodeLoads.Max() : 0;

        // Calculate variance and coefficient of variation
        var variance = nodeLoads.Any() ? nodeLoads.Sum(l => Math.Pow(l - avgLoad, 2)) / nodeLoads.Count : 0;
        var stdDev = Math.Sqrt(variance);
        var coefficientOfVariation = avgLoad > 0 ? stdDev / avgLoad : 0;

        // Determine distribution quality
        var quality = coefficientOfVariation switch
        {
            < 0.05 => "Excellent (Near-perfect distribution)",
            < 0.10 => "Good (Well-balanced)",
            < 0.20 => "Fair (Acceptable variance)",
            < 0.30 => "Poor (Unbalanced)",
            _ => "Critical (Severe imbalance)"
        };

        return new LoadDistributionAnalysis
        {
            NodeCount = metrics.NodeCount,
            AverageLoad = avgLoad,
            MinLoad = minLoad,
            MaxLoad = maxLoad,
            LoadVariance = variance,
            CoefficientOfVariation = coefficientOfVariation,
            DistributionQuality = quality,
            NodeMetrics = metrics.NodeMetrics
        };
    }

    /// <summary>
    /// Generate load distribution report
    /// </summary>
    public void GenerateLoadDistributionReport(List<ScalingMetrics> allMetrics)
    {
        Console.WriteLine("\n📊 LOAD DISTRIBUTION ANALYSIS");
        Console.WriteLine("═══════════════════════════════════════════════════════════");

        foreach (var metrics in allMetrics.OrderBy(m => m.NodeCount))
        {
            Console.WriteLine($"\n{metrics.Scenario} (Nodes={metrics.NodeCount}, Partitions={metrics.TotalPartitions})");
            Console.WriteLine("───────────────────────────────────────────────────────────");

            var analysis = AnalyzeLoadDistribution(metrics);
            
            Console.WriteLine($"Distribution Quality: {analysis.DistributionQuality}");
            Console.WriteLine($"Coefficient of Variation: {analysis.CoefficientOfVariation:F4}");
            Console.WriteLine($"Load Range: {analysis.MinLoad:F0} - {analysis.MaxLoad:F0} events");
            Console.WriteLine($"Average Load: {analysis.AverageLoad:F1} events/node");
            Console.WriteLine();

            // Node-level breakdown
            Console.WriteLine($"{"Node",6} {"Events",10} {"Throughput",15} {"Load %",10} {"Partitions",20}");
            Console.WriteLine(new string('─', 70));

            foreach (var node in analysis.NodeMetrics.OrderBy(n => n.NodeId))
            {
                var partitions = string.Join(",", node.AssignedPartitions);
                Console.WriteLine($"{node.NodeId,6} {node.EventsProcessed,10:N0} " +
                                $"{node.ThroughputEventsPerSec,15:F1} {node.LoadPercentage,9:F1}% " +
                                $"{partitions,20}");
            }
        }
    }
}