using System.Text.Json;
using System.Diagnostics;

namespace FlinkDotNet.Aspire.IntegrationTests;

/// <summary>
/// Generates Allure reports in C# instead of using CLI
/// </summary>
public static class AllureReportGenerator
{
    /// <summary>
    /// Generate Allure HTML report from test results
    /// </summary>
    /// <param name="allureResultsPath">Path to allure-results directory</param>
    /// <param name="outputPath">Output path for HTML report</param>
    /// <returns>True if report generated successfully</returns>
    public static async Task<bool> GenerateReportAsync(string allureResultsPath, string outputPath)
    {
        try
        {
            Console.WriteLine("📊 Generating Allure BDD Report from C#...");
            
            if (!Directory.Exists(allureResultsPath))
            {
                Console.WriteLine($"⚠️ Allure results directory not found: {allureResultsPath}");
                return false;
            }

            var resultFiles = Directory.GetFiles(allureResultsPath, "*.json");
            if (resultFiles.Length == 0)
            {
                Console.WriteLine("⚠️ No Allure result files found for BDD report generation");
                return false;
            }

            Console.WriteLine($"📈 Found {resultFiles.Length} Allure result files, generating BDD report...");

            // Create output directory
            Directory.CreateDirectory(outputPath);

            // Generate a comprehensive HTML report
            await GenerateHTMLReport(resultFiles, outputPath);

            // Generate summary JSON
            await GenerateSummaryJson(resultFiles, outputPath);

            Console.WriteLine("✅ Allure BDD report generated successfully from C#");
            Console.WriteLine("📋 Report includes:");
            Console.WriteLine("  • BDD Scenario execution details");
            Console.WriteLine("  • Given/When/Then step breakdowns");
            Console.WriteLine("  • Feature test coverage");
            Console.WriteLine("  • Performance metrics and timings");

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error generating Allure report: {ex.Message}");
            return false;
        }
    }

    private static async Task GenerateHTMLReport(string[] resultFiles, string outputPath)
    {
        var htmlContent = GenerateHTMLTemplate();
        
        // Process each result file
        var scenarios = new List<object>();
        var features = new List<object>();
        
        foreach (var file in resultFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("name", out var nameElement))
                {
                    var scenario = new
                    {
                        Name = nameElement.GetString(),
                        Status = root.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : "unknown",
                        Start = root.TryGetProperty("start", out var startElement) ? startElement.GetInt64() : 0,
                        Stop = root.TryGetProperty("stop", out var stopElement) ? stopElement.GetInt64() : 0,
                        Duration = root.TryGetProperty("stop", out var stop) && root.TryGetProperty("start", out var start) 
                                  ? stop.GetInt64() - start.GetInt64() : 0,
                        Steps = ExtractSteps(root)
                    };
                    scenarios.Add(scenario);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ Error processing result file {file}: {ex.Message}");
            }
        }

        // Replace placeholders in HTML template
        htmlContent = htmlContent.Replace("{{SCENARIOS_JSON}}", JsonSerializer.Serialize(scenarios, new JsonSerializerOptions { WriteIndented = true }));
        htmlContent = htmlContent.Replace("{{GENERATION_TIME}}", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC"));
        htmlContent = htmlContent.Replace("{{TOTAL_SCENARIOS}}", scenarios.Count.ToString());

        await File.WriteAllTextAsync(Path.Combine(outputPath, "index.html"), htmlContent);
    }

    private static object ExtractSteps(JsonElement root)
    {
        if (root.TryGetProperty("steps", out var stepsElement) && stepsElement.ValueKind == JsonValueKind.Array)
        {
            var steps = new List<object>();
            foreach (var step in stepsElement.EnumerateArray())
            {
                if (step.TryGetProperty("name", out var stepName))
                {
                    steps.Add(new
                    {
                        Name = stepName.GetString(),
                        Status = step.TryGetProperty("status", out var stepStatus) ? stepStatus.GetString() : "unknown"
                    });
                }
            }
            return steps;
        }
        return new List<object>();
    }

    private static async Task GenerateSummaryJson(string[] resultFiles, string outputPath)
    {
        var summary = new
        {
            TotalTests = resultFiles.Length,
            GeneratedAt = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
            ReportType = "BDD Scenarios with ReqNRoll",
            Files = resultFiles.Select(f => Path.GetFileName(f)).ToArray()
        };

        var summaryJson = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(Path.Combine(outputPath, "summary.json"), summaryJson);
    }

    private static string GenerateHTMLTemplate()
    {
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Allure BDD Report - Generated from C#</title>
    <style>
        body { font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif; margin: 0; padding: 20px; background: #f5f5f5; }
        .header { background: #4CAF50; color: white; padding: 20px; border-radius: 8px; margin-bottom: 20px; }
        .summary { display: flex; gap: 20px; margin-bottom: 20px; }
        .card { background: white; padding: 20px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); flex: 1; }
        .scenario { background: white; margin-bottom: 10px; padding: 15px; border-radius: 8px; border-left: 4px solid #4CAF50; }
        .scenario.failed { border-left-color: #f44336; }
        .scenario.broken { border-left-color: #ff9800; }
        .steps { margin-top: 10px; padding-left: 20px; }
        .step { padding: 5px 0; color: #666; }
        .status { font-weight: bold; text-transform: uppercase; padding: 2px 8px; border-radius: 4px; font-size: 12px; }
        .passed { background: #e8f5e8; color: #2e7d32; }
        .failed { background: #ffebee; color: #c62828; }
        .broken { background: #fff3e0; color: #ef6c00; }
        h1, h2 { margin: 0 0 10px 0; }
        .generated-info { color: #666; font-size: 14px; margin-top: 20px; }
    </style>
</head>
<body>
    <div class=""header"">
        <h1>🎭 BDD Test Report - ReqNRoll with Allure</h1>
        <p>Comprehensive BDD scenario execution report generated from C# code</p>
    </div>

    <div class=""summary"">
        <div class=""card"">
            <h2>📊 Test Summary</h2>
            <p><strong>Total Scenarios:</strong> {{TOTAL_SCENARIOS}}</p>
            <p><strong>Generated:</strong> {{GENERATION_TIME}}</p>
            <p><strong>Report Type:</strong> BDD Scenarios with ReqNRoll</p>
        </div>
        <div class=""card"">
            <h2>🎯 Features Covered</h2>
            <p>• Stress Test - High Throughput Message Processing</p>
            <p>• Reliability Test - System Resilience</p>
            <p>• Integration Test - End-to-End Workflows</p>
        </div>
    </div>

    <div class=""card"">
        <h2>🚀 BDD Scenario Results</h2>
        <div id=""scenarios"">
            <!-- Scenarios will be populated by JavaScript -->
        </div>
    </div>

    <div class=""generated-info"">
        <p>📈 This report was generated from C# code using Allure.Reqnroll integration, displaying actual BDD scenario names and step details.</p>
        <p>🔧 Generated at: {{GENERATION_TIME}}</p>
    </div>

    <script>
        const scenarios = {{SCENARIOS_JSON}};
        
        function renderScenarios() {
            const container = document.getElementById('scenarios');
            
            scenarios.forEach(scenario => {
                const div = document.createElement('div');
                div.className = `scenario ${scenario.Status.toLowerCase()}`;
                
                const statusClass = scenario.Status.toLowerCase();
                const statusBadge = `<span class=""status ${statusClass}"">${scenario.Status}</span>`;
                
                const duration = scenario.Duration > 0 ? ` (${(scenario.Duration / 1000).toFixed(2)}s)` : '';
                
                div.innerHTML = `
                    <div>
                        <strong>🎯 ${scenario.Name}</strong> ${statusBadge}${duration}
                    </div>
                    <div class=""steps"">
                        ${Array.isArray(scenario.Steps) ? scenario.Steps.map(step => 
                            `<div class=""step"">→ ${step.Name} <span class=""status ${step.Status.toLowerCase()}"">${step.Status}</span></div>`
                        ).join('') : ''}
                    </div>
                `;
                
                container.appendChild(div);
            });
        }
        
        renderScenarios();
    </script>
</body>
</html>";
    }
}