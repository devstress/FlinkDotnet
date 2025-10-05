using System.Diagnostics;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Diagnostic test to validate Docker network connectivity between Flink and Kafka containers.
/// This test proves whether the infrastructure networking is working before testing job execution.
/// </summary>
[TestFixture, NonParallelizable]
[Category("diagnostic")]
[Category("infrastructure")]
public class DockerNetworkDiagnosticTest : LocalTestingTestBase
{
    [Test]
    public async Task DockerNetwork_FlinkCanReachKafka_ShouldSucceed()
    {
        var ct = TestContext.CurrentContext.CancellationToken;
        
        TestContext.WriteLine("========================================");
        TestContext.WriteLine("🔍 DOCKER NETWORK DIAGNOSTIC TEST");
        TestContext.WriteLine("========================================");
        TestContext.WriteLine("Purpose: Validate Flink containers can reach Kafka at kafka:9093");
        TestContext.WriteLine("");

        try
        {
            // Wait for infrastructure
            TestContext.WriteLine("⏳ Waiting for infrastructure...");
            await WaitForFullInfrastructureAsync(includeGateway: false, ct);
            TestContext.WriteLine("✅ Infrastructure ready");
            TestContext.WriteLine("");

            // Get container names
            var kafkaContainer = await GetKafkaContainerNameAsync();
            var flinkTaskManagerContainer = await GetFlinkTaskManagerContainerNameAsync();
            
            TestContext.WriteLine($"📦 Kafka container: {kafkaContainer}");
            TestContext.WriteLine($"📦 Flink TaskManager container: {flinkTaskManagerContainer}");
            TestContext.WriteLine("");

            // Test 1: Check if containers are on the same network
            TestContext.WriteLine("1️⃣ Checking if containers share a Docker network...");
            var kafkaNetworks = await GetContainerNetworksAsync(kafkaContainer);
            var flinkNetworks = await GetContainerNetworksAsync(flinkTaskManagerContainer);
            
            TestContext.WriteLine($"   Kafka networks: {string.Join(", ", kafkaNetworks)}");
            TestContext.WriteLine($"   Flink networks: {string.Join(", ", flinkNetworks)}");
            
            var sharedNetworks = kafkaNetworks.Intersect(flinkNetworks).ToList();
            if (sharedNetworks.Any())
            {
                TestContext.WriteLine($"   ✅ Containers share network(s): {string.Join(", ", sharedNetworks)}");
            }
            else
            {
                TestContext.WriteLine("   ❌ Containers are on DIFFERENT networks!");
                TestContext.WriteLine("   This is the root cause - containers cannot communicate!");
                Assert.Fail("Containers must share a Docker network for Kafka connectivity");
            }
            TestContext.WriteLine("");

            // Test 2: DNS resolution test
            TestContext.WriteLine("2️⃣ Testing DNS resolution from Flink to Kafka...");
            var dnsResult = await RunDockerExecAsync(flinkTaskManagerContainer, "getent hosts kafka");
            
            if (dnsResult.ExitCode == 0 && !string.IsNullOrWhiteSpace(dnsResult.Output))
            {
                TestContext.WriteLine($"   ✅ DNS resolution successful: {dnsResult.Output.Trim()}");
            }
            else
            {
                TestContext.WriteLine($"   ❌ DNS resolution FAILED");
                TestContext.WriteLine($"   Exit code: {dnsResult.ExitCode}");
                TestContext.WriteLine($"   Output: {dnsResult.Output}");
                TestContext.WriteLine($"   Error: {dnsResult.Error}");
                Assert.Fail("Flink container cannot resolve 'kafka' hostname - DNS issue detected");
            }
            TestContext.WriteLine("");

            TestContext.WriteLine("========================================");
            TestContext.WriteLine("✅ ALL NETWORK DIAGNOSTIC TESTS PASSED");
            TestContext.WriteLine("========================================");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine("");
            TestContext.WriteLine("========================================");
            TestContext.WriteLine("❌ NETWORK DIAGNOSTIC TEST FAILED");
            TestContext.WriteLine("========================================");
            TestContext.WriteLine($"Error: {ex.Message}");
            throw;
        }
    }

    private static async Task<string> GetKafkaContainerNameAsync()
    {
        var result = await RunDockerCommandAsync("ps --filter name=kafka --format {{.Names}} --no-trunc");
        var containerName = result.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("No Kafka container found");
        }
        
        return containerName.Trim();
    }

    private static async Task<string> GetFlinkTaskManagerContainerNameAsync()
    {
        var result = await RunDockerCommandAsync("ps --filter name=flink-taskmanager --format {{.Names}} --no-trunc");
        var containerName = result.Split('\n', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        
        if (string.IsNullOrWhiteSpace(containerName))
        {
            throw new InvalidOperationException("No Flink TaskManager container found");
        }
        
        return containerName.Trim();
    }

    private static async Task<List<string>> GetContainerNetworksAsync(string containerName)
    {
        var result = await RunDockerCommandAsync("inspect " + containerName + " --format \"{{range $key, $value := .NetworkSettings.Networks}}{{$key}} {{end}}\"");
        return result.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(n => n.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToList();
    }

    private static async Task<string> RunDockerCommandAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"Docker command failed (exit code {process.ExitCode}): {error}");
        }

        return output;
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunDockerExecAsync(string containerName, string command)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"exec {containerName} sh -c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi);
        if (process == null)
        {
            throw new InvalidOperationException("Failed to start docker exec process");
        }

        var output = await process.StandardOutput.ReadToEndAsync();
        var error = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        return (process.ExitCode, output, error);
    }
}