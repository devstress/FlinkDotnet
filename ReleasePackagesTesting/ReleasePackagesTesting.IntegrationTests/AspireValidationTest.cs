using System.Text.Json;
using Confluent.Kafka;

namespace LocalTesting.ValidationTest;

/// <summary>
/// Simple validation test to verify Aspire setup is working correctly
/// This test validates basic connectivity to all LocalTesting services
/// </summary>
public static class AspireValidationTest
{
    private static readonly HttpClient _httpClient = new();

    // Note: This is a validation utility class, not an entry point
    // Run this via: dotnet run --project LocalTesting.IntegrationTests
    public static async Task<int> ValidateAspireSetup(string[] args)
    {
        Console.WriteLine("🧪 Aspire + FlinkDotNet Setup Validation Test");
        Console.WriteLine("============================================");
        Console.WriteLine();

        var allPassed = true;

        // Test 1: Kafka Connectivity
        Console.WriteLine("1. Testing Kafka connectivity...");
        var kafkaResult = TestKafkaConnectivity();
        LogResult("Kafka", kafkaResult);
        allPassed &= kafkaResult;

        // Test 2: Flink JobManager
        Console.WriteLine("\n2. Testing Flink JobManager...");
        var flinkResult = await TestFlinkJobManager();
        LogResult("Flink JobManager", flinkResult);
        allPassed &= flinkResult;

        // Test 3: Flink Job Gateway  
        Console.WriteLine("\n3. Testing Flink Job Gateway...");
        var gatewayResult = await TestFlinkGateway();
        LogResult("Flink Job Gateway", gatewayResult);
        allPassed &= gatewayResult;

        // Final Results
        Console.WriteLine("\n" + new string('=', 50));
        Console.WriteLine($"Overall Result: {(allPassed ? "✅ SUCCESS" : "❌ FAILURE")}");
        Console.WriteLine($"Services Validated: Kafka, Flink JobManager, Job Gateway");
        Console.WriteLine();

        if (allPassed)
        {
            Console.WriteLine("🎉 Aspire setup is working correctly!");
            Console.WriteLine("   You can now run integration tests and use the FlinkDotNet services.");
            Console.WriteLine();
            Console.WriteLine("Service URLs:");
            Console.WriteLine("   • Aspire Dashboard: http://localhost:15888");
            Console.WriteLine("   • Flink JobManager UI: http://localhost:8081");
            Console.WriteLine("   • Flink Job Gateway: http://localhost:8080");
            Console.WriteLine("   • Kafka: localhost:9092");
        }
        else
        {
            Console.WriteLine("⚠️  Some services are not responding correctly.");
            Console.WriteLine("   Please check that the LocalTesting.FlinkSqlAppHost is running.");
            Console.WriteLine("   Run: dotnet run --project LocalTesting.FlinkSqlAppHost");
        }

        return allPassed ? 0 : 1;
    }

    private static bool TestKafkaConnectivity()
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = "localhost:9092",
                SocketTimeoutMs = 5000
            };

            using var admin = new AdminClientBuilder(config).Build();
            var metadata = admin.GetMetadata(TimeSpan.FromSeconds(3));
            
            if (metadata?.Brokers?.Count > 0)
            {
                Console.WriteLine($"   ✅ Connected successfully (brokers: {metadata.Brokers.Count})");
                return true;
            }
            else
            {
                Console.WriteLine("   ❌ No brokers found");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Connection failed: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> TestFlinkJobManager()
    {
        try
        {
            var response = await _httpClient.GetAsync("http://localhost:8081/v1/overview");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var hasContent = !string.IsNullOrWhiteSpace(content);
                Console.WriteLine($"   ✅ Connected successfully (status: {response.StatusCode}, has content: {hasContent})");
                return true;
            }
            else
            {
                Console.WriteLine($"   ❌ HTTP error: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Connection failed: {ex.Message}");
            return false;
        }
    }

    private static async Task<bool> TestFlinkGateway()
    {
        try
        {
            var gatewayEndpoint = await DiscoverGatewayEndpointAsync();
            var response = await _httpClient.GetAsync($"{gatewayEndpoint}api/v1/health");
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"   ✅ Connected successfully (status: {response.StatusCode})");
                if (!string.IsNullOrWhiteSpace(content))
                {
                    try
                    {
                        JsonSerializer.Deserialize<JsonElement>(content);
                        Console.WriteLine($"       Health response: {content}");
                    }
                    catch
                    {
                        Console.WriteLine($"       Response: {content}");
                    }
                }
                return true;
            }
            else
            {
                Console.WriteLine($"   ❌ HTTP error: {response.StatusCode}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ❌ Connection failed: {ex.Message}");
            return false;
        }
    }

    private static void LogResult(string serviceName, bool success)
    {
        var status = success ? "✅ PASS" : "❌ FAIL";
        Console.WriteLine($"   {serviceName}: {status}");
    }

    /// <summary>
    /// Discover the Gateway endpoint from Docker port mappings.
    /// Gateway runs as a container with dynamic port allocation in Aspire.
    /// </summary>
    private static async Task<string> DiscoverGatewayEndpointAsync()
    {
        try
        {
            var gatewayContainers = await RunDockerCommandAsync("ps --filter \"name=flink-job-gateway\" --format \"{{.Ports}}\"");

            if (!string.IsNullOrWhiteSpace(gatewayContainers))
            {
                var lines = gatewayContainers.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var match = System.Text.RegularExpressions.Regex.Match(line, @"127\.0\.0\.1:(\d+)->(\d+)/tcp");
                    if (match.Success)
                    {
                        var endpoint = $"http://localhost:{match.Groups[1].Value}/";
                        Console.WriteLine($"   🔍 Discovered Gateway endpoint: {endpoint}");
                        return endpoint;
                    }
                }
            }

            // Fallback to default port if discovery fails
            Console.WriteLine($"   ⚠️ Gateway endpoint discovery failed, using default: http://localhost:8080/");
            return "http://localhost:8080/";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"   ⚠️ Gateway endpoint discovery error: {ex.Message}, using default port");
            return "http://localhost:8080/";
        }
    }

    /// <summary>
    /// Run a Docker or Podman command and return the output.
    /// </summary>
    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        // Try Docker first, then Podman if Docker fails
        var dockerOutput = await TryRunContainerCommandAsync("docker", arguments);
        if (!string.IsNullOrWhiteSpace(dockerOutput))
        {
            return dockerOutput;
        }

        var podmanOutput = await TryRunContainerCommandAsync("podman", arguments);
        return podmanOutput ?? string.Empty;
    }

    /// <summary>
    /// Try to run a container command (docker or podman).
    /// </summary>
    private static async Task<string?> TryRunContainerCommandAsync(string command, string arguments)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(psi);
            if (process == null)
            {
                return null;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }
}
