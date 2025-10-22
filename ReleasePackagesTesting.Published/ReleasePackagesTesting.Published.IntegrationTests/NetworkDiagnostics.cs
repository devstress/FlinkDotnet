using System.Diagnostics;
using System.Text;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Network diagnostics utilities for capturing Docker/Podman network information.
/// Writes detailed network state to test-logs/network.log.* files for debugging.
/// </summary>
public static class NetworkDiagnostics
{
    // Place logs in LocalTesting/test-logs (repository root relative path)
    private static readonly string LogDirectory = GetLogDirectory();
    
    private static string GetLogDirectory()
    {
        // Navigate from bin/Debug|Release/net9.0 to LocalTesting/test-logs
        var baseDir = AppContext.BaseDirectory;
        var localTestingRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        return Path.Combine(localTestingRoot, "test-logs");
    }
    
    /// <summary>
    /// Capture comprehensive network diagnostics to a date-stamped log file.
    /// </summary>
    /// <param name="checkpointName">Name of the checkpoint (e.g., "startup", "before-test", "after-test")</param>
    public static async Task CaptureNetworkDiagnosticsAsync(string checkpointName)
    {
        try
        {
            // Ensure log directory exists
            Directory.CreateDirectory(LogDirectory);
            
            var dateStamp = DateTime.UtcNow.ToString("yyyyMMdd");
            var timeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logFileName = $"network.log.{dateStamp}";
            var logFilePath = Path.Combine(LogDirectory, logFileName);
            
            var diagnostics = new StringBuilder();
            diagnostics.AppendLine();
            diagnostics.AppendLine("╔══════════════════════════════════════════════════════════════");
            diagnostics.AppendLine($"║ Network Diagnostics - {checkpointName}");
            diagnostics.AppendLine($"║ Timestamp: {timeStamp} UTC");
            diagnostics.AppendLine("╚══════════════════════════════════════════════════════════════");
            diagnostics.AppendLine();
            
            // Capture container information
            await CaptureContainerInfoAsync(diagnostics);
            
            // Capture network information
            await CaptureNetworkInfoAsync(diagnostics);
            
            // Capture Aspire-specific network information
            await CaptureAspireNetworksAsync(diagnostics);
            
            // Append to daily log file
            await File.AppendAllTextAsync(logFilePath, diagnostics.ToString());
            
            Console.WriteLine($"✅ Network diagnostics appended to: {logFilePath}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Failed to capture network diagnostics: {ex.Message}");
        }
    }
    
    /// <summary>
    /// Capture Docker/Podman container information.
    /// </summary>
    private static async Task CaptureContainerInfoAsync(StringBuilder diagnostics)
    {
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine("CONTAINER STATUS (docker ps / podman ps)");
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine();
        
        // Try Docker first
        var dockerPs = await TryRunCommandAsync("docker", "ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\\t{{.Networks}}\"");
        if (!string.IsNullOrWhiteSpace(dockerPs))
        {
            diagnostics.AppendLine("🐳 Docker Containers:");
            diagnostics.AppendLine(dockerPs);
            diagnostics.AppendLine();
            
            // Also capture all containers (including stopped)
            var dockerPsAll = await TryRunCommandAsync("docker", "ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\\t{{.Networks}}\"");
            if (!string.IsNullOrWhiteSpace(dockerPsAll))
            {
                diagnostics.AppendLine("🐳 All Docker Containers (including stopped):");
                diagnostics.AppendLine(dockerPsAll);
                diagnostics.AppendLine();
            }
        }
        else
        {
            // Try Podman as fallback
            var podmanPs = await TryRunCommandAsync("podman", "ps --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\\t{{.Networks}}\"");
            if (!string.IsNullOrWhiteSpace(podmanPs))
            {
                diagnostics.AppendLine("🦭 Podman Containers:");
                diagnostics.AppendLine(podmanPs);
                diagnostics.AppendLine();
                
                // Also capture all containers (including stopped)
                var podmanPsAll = await TryRunCommandAsync("podman", "ps -a --format \"table {{.Names}}\\t{{.Status}}\\t{{.Ports}}\\t{{.Networks}}\"");
                if (!string.IsNullOrWhiteSpace(podmanPsAll))
                {
                    diagnostics.AppendLine("🦭 All Podman Containers (including stopped):");
                    diagnostics.AppendLine(podmanPsAll);
                    diagnostics.AppendLine();
                }
            }
            else
            {
                diagnostics.AppendLine("⚠️ No container runtime (Docker/Podman) found or not responding");
                diagnostics.AppendLine();
            }
        }
    }
    
    /// <summary>
    /// Capture Docker/Podman network information.
    /// </summary>
    private static async Task CaptureNetworkInfoAsync(StringBuilder diagnostics)
    {
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine("NETWORK INFORMATION (docker network ls / podman network ls)");
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine();
        
        // Try Docker first
        var dockerNetworks = await TryRunCommandAsync("docker", "network ls --format \"table {{.Name}}\\t{{.Driver}}\\t{{.Scope}}\"");
        if (!string.IsNullOrWhiteSpace(dockerNetworks))
        {
            diagnostics.AppendLine("🐳 Docker Networks:");
            diagnostics.AppendLine(dockerNetworks);
            diagnostics.AppendLine();
            
            // Inspect each network for detailed information
            await InspectNetworksAsync(diagnostics, "docker", dockerNetworks);
        }
        else
        {
            // Try Podman as fallback
            var podmanNetworks = await TryRunCommandAsync("podman", "network ls --format \"table {{.Name}}\\t{{.Driver}}\"");
            if (!string.IsNullOrWhiteSpace(podmanNetworks))
            {
                diagnostics.AppendLine("🦭 Podman Networks:");
                diagnostics.AppendLine(podmanNetworks);
                diagnostics.AppendLine();
                
                // Inspect each network for detailed information
                await InspectNetworksAsync(diagnostics, "podman", podmanNetworks);
            }
            else
            {
                diagnostics.AppendLine("⚠️ No network information available");
                diagnostics.AppendLine();
            }
        }
    }
    
    /// <summary>
    /// Inspect individual networks for detailed information.
    /// </summary>
    private static async Task InspectNetworksAsync(StringBuilder diagnostics, string command, string networkList)
    {
        var lines = networkList.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Skip header line and extract network names
        var networkNames = lines
            .Skip(1)
            .Select(line => line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToList();
        
        foreach (var networkName in networkNames)
        {
            var networkInspect = await TryRunCommandAsync(command, $"network inspect {networkName}");
            if (!string.IsNullOrWhiteSpace(networkInspect))
            {
                diagnostics.AppendLine($"📋 Network Details: {networkName}");
                diagnostics.AppendLine("────────────────────────────────────────────────────────────");
                diagnostics.AppendLine(networkInspect);
                diagnostics.AppendLine();
            }
        }
    }
    
    /// <summary>
    /// Capture Aspire-specific network information (networks created by Aspire).
    /// </summary>
    private static async Task CaptureAspireNetworksAsync(StringBuilder diagnostics)
    {
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine("ASPIRE NETWORKS");
        diagnostics.AppendLine("════════════════════════════════════════════════════════════════");
        diagnostics.AppendLine();
        
        // Try to find Aspire-created networks (typically have specific patterns)
        var dockerNetworks = await TryRunCommandAsync("docker", "network ls --filter \"name=aspire\" --format \"table {{.Name}}\\t{{.Driver}}\\t{{.Scope}}\"");
        if (!string.IsNullOrWhiteSpace(dockerNetworks))
        {
            diagnostics.AppendLine("🐳 Aspire Networks (Docker):");
            diagnostics.AppendLine(dockerNetworks);
            diagnostics.AppendLine();
        }
        
        var podmanNetworks = await TryRunCommandAsync("podman", "network ls --filter \"name=aspire\" --format \"table {{.Name}}\\t{{.Driver}}\"");
        if (!string.IsNullOrWhiteSpace(podmanNetworks))
        {
            diagnostics.AppendLine("🦭 Aspire Networks (Podman):");
            diagnostics.AppendLine(podmanNetworks);
            diagnostics.AppendLine();
        }
        
        // Also check for custom networks that might be created by tests
        var customNetworks = await TryRunCommandAsync("docker", "network ls --filter \"driver=bridge\" --format \"table {{.Name}}\\t{{.Driver}}\\t{{.Scope}}\"");
        if (!string.IsNullOrWhiteSpace(customNetworks))
        {
            diagnostics.AppendLine("🌉 Bridge Networks:");
            diagnostics.AppendLine(customNetworks);
            diagnostics.AppendLine();
        }
    }
    
    /// <summary>
    /// Try to run a command and return its output, or empty string if it fails.
    /// </summary>
    private static async Task<string> TryRunCommandAsync(string command, string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return string.Empty;
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            // Return output if successful, otherwise return empty
            if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            // Also return output even if exit code is non-zero but we have output
            if (!string.IsNullOrWhiteSpace(output))
            {
                return output;
            }

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }
    
    /// <summary>
    /// Clean up old network diagnostic log files (keep only last 7 days).
    /// </summary>
    public static void CleanupOldLogs()
    {
        try
        {
            if (!Directory.Exists(LogDirectory))
            {
                return;
            }
            
            var cutoffDate = DateTime.UtcNow.AddDays(-7);
            var logFiles = Directory.GetFiles(LogDirectory, "network.log.*")
                .Where(f => File.GetCreationTime(f) < cutoffDate)
                .ToList();
            
            foreach (var file in logFiles)
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"🧹 Deleted old network log: {Path.GetFileName(file)}");
                }
                catch
                {
                    // Ignore deletion failures
                }
            }
        }
        catch
        {
            // Ignore cleanup failures
        }
    }
}