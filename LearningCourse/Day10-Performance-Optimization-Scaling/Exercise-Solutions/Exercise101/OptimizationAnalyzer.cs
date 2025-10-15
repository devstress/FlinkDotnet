using Serilog;

namespace Exercise101;

/// <summary>
/// Analyzes performance metrics and provides optimization recommendations
/// </summary>
public class OptimizationAnalyzer
{
    /// <summary>
    /// Analyze multiple scenarios and provide optimization recommendations
    /// </summary>
    public OptimizationAnalysis AnalyzePerformance(List<PerformanceMetrics> allMetrics)
    {
        Log.Information("📊 Analyzing performance across {Count} scenarios...", allMetrics.Count);

        if (!allMetrics.Any())
        {
            return new OptimizationAnalysis();
        }

        var analysis = new OptimizationAnalysis();

        // Get baseline scenario (parallelism=1)
        var baseline = allMetrics.FirstOrDefault(m => m.Parallelism == 1);
        if (baseline == null)
        {
            Log.Warning("No baseline scenario found (Parallelism=1)");
            return analysis;
        }

        // Create performance comparisons
        analysis.AllComparisons = allMetrics.Select(m => CreateComparison(m, baseline)).ToList();

        // Find best scenario based on efficiency score
        analysis.BestScenario = analysis.AllComparisons
            .OrderByDescending(c => c.EfficiencyScore)
            .First();

        analysis.OptimalParallelism = analysis.BestScenario.Parallelism;

        // Calculate overall improvements
        var bestThroughput = analysis.AllComparisons.Max(c => c.ThroughputEventsPerSec);
        var baselineThroughput = baseline.ThroughputEventsPerSec;
        analysis.ThroughputImprovement = baselineThroughput > 0
            ? ((bestThroughput - baselineThroughput) / baselineThroughput) * 100
            : 0;

        // Calculate resource efficiency (throughput per resource unit)
        var bestEfficiency = analysis.AllComparisons.Max(c => c.EfficiencyScore);
        var baselineEfficiency = analysis.AllComparisons
            .First(c => c.Parallelism == 1).EfficiencyScore;
        analysis.ResourceEfficiency = baselineEfficiency > 0
            ? ((bestEfficiency - baselineEfficiency) / baselineEfficiency) * 100
            : 0;

        // Generate recommendations
        analysis.Recommendations = GenerateRecommendations(allMetrics, analysis);

        return analysis;
    }

    /// <summary>
    /// Create performance comparison for a scenario
    /// </summary>
    private PerformanceComparison CreateComparison(PerformanceMetrics metrics, PerformanceMetrics baseline)
    {
        var comparison = new PerformanceComparison
        {
            ScenarioName = metrics.Scenario,
            Parallelism = metrics.Parallelism,
            ThroughputEventsPerSec = metrics.ThroughputEventsPerSec,
            LatencyMs = metrics.AverageLatency.TotalMilliseconds,
            MemoryUsedMB = metrics.AverageMemoryMB,
            CPUPercent = metrics.AverageCPUPercent
        };

        // Calculate changes vs baseline
        if (baseline.ThroughputEventsPerSec > 0)
        {
            comparison.ThroughputGain = ((metrics.ThroughputEventsPerSec - baseline.ThroughputEventsPerSec)
                / baseline.ThroughputEventsPerSec) * 100;
        }

        if (baseline.AverageLatency.TotalMilliseconds > 0)
        {
            comparison.LatencyChange = ((metrics.AverageLatency.TotalMilliseconds - baseline.AverageLatency.TotalMilliseconds)
                / baseline.AverageLatency.TotalMilliseconds) * 100;
        }

        if (baseline.AverageMemoryMB > 0)
        {
            comparison.MemoryChange = ((metrics.AverageMemoryMB - baseline.AverageMemoryMB)
                / (double)baseline.AverageMemoryMB) * 100;
        }

        if (baseline.AverageCPUPercent > 0)
        {
            comparison.CPUChange = ((metrics.AverageCPUPercent - baseline.AverageCPUPercent)
                / baseline.AverageCPUPercent) * 100;
        }

        // Calculate efficiency score (throughput per resource unit)
        // Higher is better - maximizes throughput while minimizing resources
        var resourceCost = (metrics.AverageMemoryMB / 100.0) + (metrics.AverageCPUPercent / 10.0);
        comparison.EfficiencyScore = resourceCost > 0
            ? metrics.ThroughputEventsPerSec / resourceCost
            : 0;

        return comparison;
    }

    /// <summary>
    /// Generate optimization recommendations based on analysis
    /// </summary>
    private List<OptimizationRecommendation> GenerateRecommendations(
        List<PerformanceMetrics> allMetrics,
        OptimizationAnalysis analysis)
    {
        var recommendations = new List<OptimizationRecommendation>();

        // Analyze parallelism efficiency
        var parallelismRec = AnalyzeParallelismEfficiency(allMetrics, analysis);
        if (parallelismRec != null)
            recommendations.Add(parallelismRec);

        // Analyze memory usage
        var memoryRec = AnalyzeMemoryUsage(allMetrics);
        if (memoryRec != null)
            recommendations.Add(memoryRec);

        // Analyze CPU utilization
        var cpuRec = AnalyzeCPUUtilization(allMetrics);
        if (cpuRec != null)
            recommendations.Add(cpuRec);

        // Analyze GC pressure
        var gcRec = AnalyzeGCPressure(allMetrics);
        if (gcRec != null)
            recommendations.Add(gcRec);

        // Analyze latency characteristics
        var latencyRec = AnalyzeLatency(allMetrics);
        if (latencyRec != null)
            recommendations.Add(latencyRec);

        return recommendations.OrderBy(r => r.Priority).ToList();
    }

    private OptimizationRecommendation? AnalyzeParallelismEfficiency(
        List<PerformanceMetrics> allMetrics,
        OptimizationAnalysis analysis)
    {
        var optimal = analysis.OptimalParallelism;
        var maxParallelism = allMetrics.Max(m => m.Parallelism);

        if (optimal < maxParallelism)
        {
            return new OptimizationRecommendation
            {
                Category = "Parallelism",
                Issue = $"Higher parallelism (>{optimal}) shows diminishing returns",
                Recommendation = $"Use Parallelism={optimal} for optimal resource efficiency. " +
                                $"Beyond this point, additional resources don't proportionally improve throughput.",
                Impact = $"Save {((maxParallelism - optimal) / (double)maxParallelism * 100):F0}% resources with minimal throughput impact",
                Priority = 1
            };
        }

        return null;
    }

    private OptimizationRecommendation? AnalyzeMemoryUsage(List<PerformanceMetrics> allMetrics)
    {
        var maxMemory = allMetrics.Max(m => m.PeakMemoryMB);
        var avgMemory = allMetrics.Average(m => m.AverageMemoryMB);

        if (maxMemory > avgMemory * 1.5)
        {
            return new OptimizationRecommendation
            {
                Category = "Memory",
                Issue = $"Memory spikes detected (Peak: {maxMemory}MB vs Avg: {avgMemory:F0}MB)",
                Recommendation = "Consider implementing object pooling or reducing allocation rates. " +
                                "Memory spikes can trigger frequent GC pauses.",
                Impact = "Reduce GC pressure and improve latency consistency",
                Priority = 2
            };
        }

        return null;
    }

    private OptimizationRecommendation? AnalyzeCPUUtilization(List<PerformanceMetrics> allMetrics)
    {
        var maxCPU = allMetrics.Max(m => m.PeakCPUPercent);
        var avgCPU = allMetrics.Average(m => m.AverageCPUPercent);

        if (maxCPU > 80)
        {
            return new OptimizationRecommendation
            {
                Category = "CPU",
                Issue = $"High CPU utilization detected (Peak: {maxCPU:F1}%, Avg: {avgCPU:F1}%)",
                Recommendation = "Consider adding more CPU cores or optimizing computational workloads. " +
                                "CPU saturation can cause processing delays.",
                Impact = "Improve throughput and reduce latency under load",
                Priority = avgCPU > 70 ? 1 : 2
            };
        }
        else if (avgCPU < 30)
        {
            return new OptimizationRecommendation
            {
                Category = "CPU",
                Issue = $"Low CPU utilization (Avg: {avgCPU:F1}%)",
                Recommendation = "System may be I/O bound or waiting on external resources. " +
                                "Consider investigating network or storage bottlenecks.",
                Impact = "Identify and resolve non-CPU bottlenecks",
                Priority = 3
            };
        }

        return null;
    }

    private OptimizationRecommendation? AnalyzeGCPressure(List<PerformanceMetrics> allMetrics)
    {
        var totalGC = allMetrics.Sum(m => m.TotalGCCollections);
        var gen2Collections = allMetrics.Sum(m => m.Gen2Collections);

        if (gen2Collections > 10)
        {
            return new OptimizationRecommendation
            {
                Category = "Garbage Collection",
                Issue = $"High Gen2 GC pressure detected ({gen2Collections} collections)",
                Recommendation = "Reduce object allocations and implement object pooling. " +
                                "Gen2 collections can cause significant pause times.",
                Impact = "Reduce latency spikes and improve throughput consistency",
                Priority = 2
            };
        }

        return null;
    }

    private OptimizationRecommendation? AnalyzeLatency(List<PerformanceMetrics> allMetrics)
    {
        var p99Latencies = allMetrics.Select(m => m.P99Latency.TotalMilliseconds).ToList();
        var avgLatencies = allMetrics.Select(m => m.AverageLatency.TotalMilliseconds).ToList();

        if (p99Latencies.Any() && avgLatencies.Any())
        {
            var maxP99 = p99Latencies.Max();
            var avgAvg = avgLatencies.Average();

            if (maxP99 > avgAvg * 3)
            {
                return new OptimizationRecommendation
                {
                    Category = "Latency",
                    Issue = $"High latency variance (P99: {maxP99:F1}ms vs Avg: {avgAvg:F1}ms)",
                    Recommendation = "Investigate tail latency causes. Consider: " +
                                    "1) GC pause tuning, 2) Network timeout optimization, " +
                                    "3) Load balancing improvements.",
                    Impact = "Improve user experience with consistent response times",
                    Priority = 2
                };
            }
        }

        return null;
    }

    /// <summary>
    /// Generate comprehensive performance report
    /// </summary>
    public void GenerateReport(OptimizationAnalysis analysis)
    {
        Console.WriteLine("\n" + new string('=', 80));
        Console.WriteLine("  PERFORMANCE OPTIMIZATION REPORT");
        Console.WriteLine(new string('=', 80));

        // Summary
        Console.WriteLine("\n📈 OPTIMIZATION SUMMARY");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"  Optimal Parallelism: {analysis.OptimalParallelism}");
        Console.WriteLine($"  Throughput Improvement: {analysis.ThroughputImprovement:F1}%");
        Console.WriteLine($"  Resource Efficiency Gain: {analysis.ResourceEfficiency:F1}%");

        // Performance Comparison Table
        Console.WriteLine("\n📊 PERFORMANCE COMPARISON");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine($"{"Scenario",-20} {"Parallelism",12} {"Throughput",15} {"Latency",12} {"Efficiency",12}");
        Console.WriteLine($"{"",20} {"",12} {"(events/sec)",15} {"(ms)",12} {"Score",12}");
        Console.WriteLine(new string('─', 80));

        foreach (var comparison in analysis.AllComparisons.OrderBy(c => c.Parallelism))
        {
            var marker = comparison.Parallelism == analysis.OptimalParallelism ? "⭐" : "  ";
            Console.WriteLine($"{marker}{comparison.ScenarioName,-18} {comparison.Parallelism,12} " +
                            $"{comparison.ThroughputEventsPerSec,15:F1} {comparison.LatencyMs,12:F1} " +
                            $"{comparison.EfficiencyScore,12:F2}");

            if (comparison.Parallelism > 1)
            {
                Console.WriteLine($"{"",20} {"Gain:",12} {comparison.ThroughputGain,14:+0.0;-0.0}% " +
                                $"{comparison.LatencyChange,11:+0.0;-0.0}%");
            }
        }

        // Recommendations
        if (analysis.Recommendations.Any())
        {
            Console.WriteLine("\n💡 OPTIMIZATION RECOMMENDATIONS");
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

        // Netflix-style insights
        Console.WriteLine("\n🌟 REAL-WORLD INSIGHTS (Netflix Scale)");
        Console.WriteLine("─────────────────────────────────────────────────────────────");
        Console.WriteLine("  At Netflix scale (10B+ events/day):");
        Console.WriteLine($"  • Optimal parallelism={analysis.OptimalParallelism} could save millions in infrastructure costs");
        Console.WriteLine($"  • {analysis.ThroughputImprovement:F0}% throughput improvement = handling {analysis.ThroughputImprovement / 100 * 10:F1}B more events/day");
        Console.WriteLine($"  • Resource efficiency matters: 1% improvement = $100K+/year savings");
        Console.WriteLine("  • Netflix runs 500K+ concurrent streams optimized this way");

        Console.WriteLine("\n" + new string('=', 80));
    }
}