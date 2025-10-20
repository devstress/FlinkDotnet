using Serilog;

namespace Exercise102;

/// <summary>
/// Analyzes horizontal scaling efficiency and provides recommendations
/// </summary>
public class ScalingAnalyzer
{
    /// <summary>
    /// Analyze scaling performance across multiple scenarios
    /// </summary>
    public ScalingAnalysis AnalyzeScaling(List<ScalingMetrics> allMetrics)
    {
        Log.Information("📊 Analyzing horizontal scaling across {Count} scenarios...", allMetrics.Count);

        if (!allMetrics.Any())
        {
            return new ScalingAnalysis();
        }

        var analysis = new ScalingAnalysis();

        // Get baseline scenario (1 node)
        var baseline = allMetrics.FirstOrDefault(m => m.NodeCount == 1);
        if (baseline == null)
        {
            Log.Warning("No baseline scenario found (NodeCount=1)");
            return analysis;
        }

        // Create scaling comparisons
        analysis.AllComparisons = allMetrics.Select(m => CreateScalingComparison(m, baseline)).ToList();

        // Find optimal node count (best scaling efficiency while maintaining throughput)
        var bestComparison = analysis.AllComparisons
            .Where(c => c.ScalingEfficiency >= 70) // At least 70% efficiency
            .OrderByDescending(c => c.ThroughputEventsPerSec)
            .FirstOrDefault() ?? analysis.AllComparisons.OrderByDescending(c => c.ThroughputEventsPerSec).First();

        analysis.OptimalNodeCount = bestComparison.NodeCount;
        analysis.BestThroughput = bestComparison.ThroughputEventsPerSec;
        analysis.BestScalingEfficiency = bestComparison.ScalingEfficiency;

        // Determine scaling pattern
        analysis.ScalingPattern = DetermineScalingPattern(analysis.AllComparisons);

        // Generate recommendations
        analysis.Recommendations = GenerateRecommendations(allMetrics, analysis);

        return analysis;
    }

    /// <summary>
    /// Create scaling comparison for a scenario
    /// </summary>
    private ScalingComparison CreateScalingComparison(ScalingMetrics metrics, ScalingMetrics baseline)
    {
        var comparison = new ScalingComparison
        {
            ScenarioName = metrics.Scenario,
            NodeCount = metrics.NodeCount,
            ThroughputEventsPerSec = metrics.ThroughputEventsPerSec,
            LatencyMs = metrics.AverageLatency.TotalMilliseconds,
            LoadDistributionCV = metrics.LoadDistributionCoefficient,
            PartitionsPerNode = metrics.PartitionsPerNode
        };

        // Calculate speedup vs baseline
        if (baseline.ThroughputEventsPerSec > 0)
        {
            comparison.SpeedupVsBaseline = metrics.ThroughputEventsPerSec / baseline.ThroughputEventsPerSec;
        }

        // Ideal speedup is linear (e.g., 4 nodes = 4x speedup)
        comparison.IdealSpeedup = metrics.NodeCount;

        // Scaling efficiency = actual speedup / ideal speedup
        comparison.ScalingEfficiency = comparison.IdealSpeedup > 0
            ? (comparison.SpeedupVsBaseline / comparison.IdealSpeedup) * 100
            : 0;

        // Detect bottlenecks
        comparison.BottleneckIndicator = DetectBottleneck(metrics, comparison);

        return comparison;
    }

    /// <summary>
    /// Detect performance bottlenecks
    /// </summary>
    private string DetectBottleneck(ScalingMetrics metrics, ScalingComparison comparison)
    {
        var indicators = new List<string>();

        // Check scaling efficiency
        if (comparison.ScalingEfficiency < 50)
        {
            indicators.Add("Severe scaling degradation");
        }
        else if (comparison.ScalingEfficiency < 70)
        {
            indicators.Add("Sub-optimal scaling");
        }

        // Check load distribution
        if (metrics.LoadDistributionCoefficient > 0.20)
        {
            indicators.Add("Poor load distribution");
        }
        else if (metrics.LoadDistributionCoefficient > 0.10)
        {
            indicators.Add("Uneven load distribution");
        }

        // Check partition-to-node ratio
        if (metrics.PartitionsPerNode < 1.0)
        {
            indicators.Add($"Over-provisioned ({metrics.NodeCount} nodes > {metrics.TotalPartitions} partitions)");
        }

        return indicators.Any() ? string.Join(", ", indicators) : "No bottlenecks detected";
    }

    /// <summary>
    /// Determine overall scaling pattern
    /// </summary>
    private string DetermineScalingPattern(List<ScalingComparison> comparisons)
    {
        var orderedComparisons = comparisons.OrderBy(c => c.NodeCount).ToList();
        
        if (orderedComparisons.Count < 2)
            return "Insufficient data";

        // Check if scaling efficiency is consistently high (>90%)
        var avgEfficiency = orderedComparisons.Average(c => c.ScalingEfficiency);
        if (avgEfficiency > 90)
            return "Near-Linear Scaling";

        // Check if efficiency degrades significantly
        var firstEfficiency = orderedComparisons.First().ScalingEfficiency;
        var lastEfficiency = orderedComparisons.Last().ScalingEfficiency;
        var efficiencyDrop = firstEfficiency - lastEfficiency;

        if (efficiencyDrop > 30)
            return "Diminishing Returns";
        else if (avgEfficiency > 70)
            return "Sub-Linear Scaling (Acceptable)";
        else
            return "Poor Scaling";
    }

    /// <summary>
    /// Generate scaling recommendations
    /// </summary>
    private List<ScalingRecommendation> GenerateRecommendations(
        List<ScalingMetrics> allMetrics,
        ScalingAnalysis analysis)
    {
        var recommendations = new List<ScalingRecommendation>();

        // Analyze optimal node count
        var optimalRec = AnalyzeOptimalNodeCount(allMetrics, analysis);
        if (optimalRec != null)
            recommendations.Add(optimalRec);

        // Analyze partition configuration
        var partitionRec = AnalyzePartitionConfiguration(allMetrics);
        if (partitionRec != null)
            recommendations.Add(partitionRec);

        // Analyze load distribution
        var loadDistRec = AnalyzeLoadDistribution(allMetrics);
        if (loadDistRec != null)
            recommendations.Add(loadDistRec);

        // Analyze scaling pattern
        var scalingRec = AnalyzeScalingPattern(analysis);
        if (scalingRec != null)
            recommendations.Add(scalingRec);

        return recommendations.OrderBy(r => r.Priority).ToList();
    }

    private ScalingRecommendation? AnalyzeOptimalNodeCount(
        List<ScalingMetrics> allMetrics,
        ScalingAnalysis analysis)
    {
        var maxNodes = allMetrics.Max(m => m.NodeCount);
        var optimal = analysis.OptimalNodeCount;

        if (optimal < maxNodes)
        {
            var optimalMetrics = allMetrics.First(m => m.NodeCount == optimal);
            var maxMetrics = allMetrics.First(m => m.NodeCount == maxNodes);
            
            var throughputDiff = ((maxMetrics.ThroughputEventsPerSec - optimalMetrics.ThroughputEventsPerSec) 
                / optimalMetrics.ThroughputEventsPerSec) * 100;

            return new ScalingRecommendation
            {
                Category = "Node Count Optimization",
                Issue = $"Adding nodes beyond {optimal} shows diminishing returns (only {throughputDiff:F1}% gain)",
                Recommendation = $"Deploy with {optimal} nodes for optimal cost-efficiency. " +
                                $"Beyond this point, additional nodes provide minimal throughput improvement.",
                Impact = $"Save {((maxNodes - optimal) / (double)maxNodes * 100):F0}% infrastructure costs with <{throughputDiff:F0}% throughput impact",
                Priority = 1
            };
        }

        return null;
    }

    private ScalingRecommendation? AnalyzePartitionConfiguration(List<ScalingMetrics> allMetrics)
    {
        var totalPartitions = allMetrics.First().TotalPartitions;
        var maxNodes = allMetrics.Max(m => m.NodeCount);

        if (maxNodes > totalPartitions)
        {
            return new ScalingRecommendation
            {
                Category = "Partition Configuration",
                Issue = $"Node count ({maxNodes}) exceeds partition count ({totalPartitions})",
                Recommendation = $"Increase Kafka topic partitions to at least {maxNodes * 2} to fully utilize all nodes. " +
                                $"Rule of thumb: partitions >= 2x node count for flexibility.",
                Impact = "Enable better load distribution and support future scaling",
                Priority = 2
            };
        }

        return null;
    }

    private ScalingRecommendation? AnalyzeLoadDistribution(List<ScalingMetrics> allMetrics)
    {
        var poorDistribution = allMetrics.Where(m => m.LoadDistributionCoefficient > 0.15).ToList();

        if (poorDistribution.Any())
        {
            var worst = poorDistribution.OrderByDescending(m => m.LoadDistributionCoefficient).First();
            
            return new ScalingRecommendation
            {
                Category = "Load Distribution",
                Issue = $"Uneven load distribution detected (CV={worst.LoadDistributionCoefficient:F3} in {worst.Scenario})",
                Recommendation = "Ensure: 1) Proper partition key distribution, " +
                                "2) Consumer group balanced assignment, " +
                                "3) No partition hotspots. " +
                                "Consider repartitioning strategy.",
                Impact = "Improve overall throughput and prevent node saturation",
                Priority = 2
            };
        }

        return null;
    }

    private ScalingRecommendation? AnalyzeScalingPattern(ScalingAnalysis analysis)
    {
        if (analysis.ScalingPattern.Contains("Poor") || analysis.ScalingPattern.Contains("Diminishing"))
        {
            return new ScalingRecommendation
            {
                Category = "Scaling Pattern",
                Issue = $"Detected {analysis.ScalingPattern} - efficiency drops significantly with more nodes",
                Recommendation = "Investigate: 1) Network bottlenecks, 2) Shared resource contention, " +
                                "3) Serialization overhead, 4) Coordinator overhead. " +
                                "Consider architectural changes for better horizontal scalability.",
                Impact = "Improve scaling efficiency to support larger deployments",
                Priority = 1
            };
        }

        return null;
    }

    /// <summary>
    /// Generate comprehensive scaling report
    /// </summary>
    public void GenerateReport(ScalingAnalysis analysis)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("  HORIZONTAL SCALING ANALYSIS REPORT");
        Console.WriteLine(new string('=', 80));

        // Summary
        Console.WriteLine("\n📈 SCALING SUMMARY");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"  Optimal Node Count: {analysis.OptimalNodeCount}");
        Console.WriteLine($"  Best Throughput: {analysis.BestThroughput:F1} events/sec");
        Console.WriteLine($"  Best Scaling Efficiency: {analysis.BestScalingEfficiency:F1}%");
        Console.WriteLine($"  Scaling Pattern: {analysis.ScalingPattern}");

        // Scaling Comparison Table
        Console.WriteLine("\n📊 SCALING PERFORMANCE COMPARISON");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"{"Scenario",-20} {"Nodes",7} {"Throughput",15} {"Speedup",10} {"Efficiency",12} {"Load CV",10}");
        Console.WriteLine($"{"",20} {"",7} {"(events/sec)",15} {"(vs 1N)",10} {"(%)",12} {"",10}");
        Console.WriteLine(new string('─', 80));

        foreach (var comparison in analysis.AllComparisons.OrderBy(c => c.NodeCount))
        {
            var marker = comparison.NodeCount == analysis.OptimalNodeCount ? "⭐" : "  ";
            Console.WriteLine($"{marker}{comparison.ScenarioName,-18} {comparison.NodeCount,7} " +
                            $"{comparison.ThroughputEventsPerSec,15:F1} {comparison.SpeedupVsBaseline,9:F2}x " +
                            $"{comparison.ScalingEfficiency,11:F1}% {comparison.LoadDistributionCV,10:F3}");

            if (!string.IsNullOrEmpty(comparison.BottleneckIndicator) && 
                comparison.BottleneckIndicator != "No bottlenecks detected")
            {
                Console.WriteLine($"{"",28} ⚠️  {comparison.BottleneckIndicator}");
            }
        }

        // Recommendations
        if (analysis.Recommendations.Any())
        {
            Console.WriteLine("\n💡 SCALING RECOMMENDATIONS");
            Console.WriteLine("─────────────────────────────────────────────────────────────");

            foreach (var rec in analysis.Recommendations)
            {
                var priorityLabel = rec.Priority switch
                {
                    1 => "🔴 CRITICAL",
                    2 => "🟠 HIGH",
                    3 => "🟡 MEDIUM",
                    _ => "🟢 LOW"
                };

                Console.WriteLine($"\n{priorityLabel} - {rec.Category}");
                Console.WriteLine($"  Issue: {rec.Issue}");
                Console.WriteLine($"  Recommendation: {rec.Recommendation}");
                Console.WriteLine($"  Impact: {rec.Impact}");
            }
        }

        // LinkedIn-style insights
        Console.WriteLine("\n🌟 REAL-WORLD INSIGHTS (LinkedIn Scale)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine("  At LinkedIn scale (processing billions of events/day):");
        Console.WriteLine($"  • Optimal node count={analysis.OptimalNodeCount} prevents over-provisioning wastage");
        Console.WriteLine($"  • {analysis.BestScalingEfficiency:F0}% efficiency = ${100 - analysis.BestScalingEfficiency:F0}K wasted per 100 nodes");
        Console.WriteLine($"  • Scaling pattern '{analysis.ScalingPattern}' determines architecture viability");
        Console.WriteLine("  • LinkedIn's Kafka clusters handle 7+ trillion messages/day with similar patterns");
        Console.WriteLine("  • Proper partitioning strategy is critical for horizontal scalability");

        Console.WriteLine("\n" + new string('=', 80));
    }
}