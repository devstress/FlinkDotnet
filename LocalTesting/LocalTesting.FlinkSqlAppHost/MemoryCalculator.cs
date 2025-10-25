namespace LocalTesting.FlinkSqlAppHost;

/// <summary>
/// Calculates appropriate memory allocations for Flink components based on available system memory.
/// Ensures compatibility with resource-constrained environments like GitHub Actions (2-4GB RAM).
/// </summary>
public static class MemoryCalculator
{
    private const long MinimumSystemMemoryMb = 4096; // 4GB minimum required

    /// <summary>
    /// Gets total available physical memory in MB.
    /// Returns 0 if detection fails (will use fallback values).
    /// </summary>
    public static long GetTotalPhysicalMemoryMb()
    {
        try
        {
            // Use GC.GetGCMemoryInfo for cross-platform memory detection
            var gcMemoryInfo = GC.GetGCMemoryInfo();
            var totalMemoryBytes = gcMemoryInfo.TotalAvailableMemoryBytes;

            // Convert bytes to MB
            var totalMemoryMb = totalMemoryBytes / (1024 * 1024);

            Console.WriteLine($"📊 Detected system memory: {totalMemoryMb:N0} MB ({totalMemoryMb / 1024.0:F1} GB)");

            return totalMemoryMb;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Unable to detect system memory: {ex.Message}");
            return 0; // Signal to use fallback values
        }
    }

    /// <summary>
    /// Calculates appropriate TaskManager process memory based on available system RAM.
    /// Uses conservative allocations to work on resource-constrained environments.
    /// 
    /// Memory allocation strategy:
    /// - ≤8GB RAM: 1.5GB TaskManager (minimal, for CI/testing)
    /// - 8-16GB RAM: 3GB TaskManager (standard development)
    /// - ≥16GB RAM: 4GB TaskManager (optimal)
    /// </summary>
    public static int CalculateTaskManagerProcessMemoryMb()
    {
        var totalMemoryMb = GetTotalPhysicalMemoryMb();

        // Fallback: Use minimal allocation if detection fails
        if (totalMemoryMb == 0)
        {
            Console.WriteLine("⚙️ Using fallback TaskManager memory: 1536 MB (1.5GB) - Safe minimum");
            return 1536; // 1.5GB safe minimum for unknown environments
        }

        // Calculate based on available RAM
        var totalMemoryGb = totalMemoryMb / 1024.0;

        if (totalMemoryGb <= 8.0)
        {
            // Resource-constrained: GitHub Actions standard runners (4GB-7GB)
            var allocated = 1536; // 1.5GB
            Console.WriteLine($"⚙️ TaskManager memory: {allocated} MB (1.5GB) - Resource-constrained mode (≤8GB RAM)");
            return allocated;
        }
        else if (totalMemoryGb <= 16.0)
        {
            // Standard development: Most developer machines (8-16GB)
            var allocated = 3072; // 3GB
            Console.WriteLine($"⚙️ TaskManager memory: {allocated} MB (3GB) - Standard development mode (8-16GB RAM)");
            return allocated;
        }
        else
        {
            // Optimal: High-end machines (16GB+)
            var allocated = 4096; // 4GB
            Console.WriteLine($"⚙️ TaskManager memory: {allocated} MB (4GB) - Optimal mode (≥16GB RAM)");
            return allocated;
        }
    }

    /// <summary>
    /// Calculates appropriate JVM metaspace size based on TaskManager process memory.
    /// Metaspace should be ~25% of process memory for class loading overhead.
    /// 
    /// Allocation strategy:
    /// - 1.5GB process: 384MB metaspace (minimal)
    /// - 3GB process: 768MB metaspace (standard)
    /// - 4GB+ process: 1024MB metaspace (optimal)
    /// </summary>
    public static int CalculateTaskManagerMetaspaceMb(int processMemoryMb)
    {
        // Metaspace = 25% of process memory (safe allocation for class loading)
        var metaspaceMb = processMemoryMb / 4;

        // Apply bounds: 384MB minimum, 1024MB maximum
        metaspaceMb = Math.Max(384, Math.Min(1024, metaspaceMb));

        Console.WriteLine($"⚙️ TaskManager metaspace: {metaspaceMb} MB (25% of process memory)");
        return metaspaceMb;
    }

    /// <summary>
    /// Calculates appropriate JobManager process memory.
    /// JobManager is less memory-intensive than TaskManager (no data processing).
    /// Fixed at 2GB for consistency across all environments.
    /// </summary>
    public static int CalculateJobManagerProcessMemoryMb()
    {
        const int jobManagerMemory = 2048; // 2GB - sufficient for all environments
        Console.WriteLine($"⚙️ JobManager memory: {jobManagerMemory} MB (2GB) - Fixed allocation");
        return jobManagerMemory;
    }

    /// <summary>
    /// Validates that system has minimum required memory for Flink operations.
    /// </summary>
    public static bool ValidateMinimumMemory()
    {
        var totalMemoryMb = GetTotalPhysicalMemoryMb();

        // If detection fails, assume valid (fallback values will handle it)
        if (totalMemoryMb == 0)
        {
            Console.WriteLine("ℹ️ Unable to validate minimum memory - proceeding with fallback values");
            return true;
        }

        if (totalMemoryMb < MinimumSystemMemoryMb)
        {
            Console.WriteLine($"❌ Insufficient system memory: {totalMemoryMb}MB < {MinimumSystemMemoryMb}MB required");
            Console.WriteLine($"   Flink requires at least 4GB RAM for stable operation");
            return false;
        }

        Console.WriteLine($"✅ System memory validation passed: {totalMemoryMb}MB ≥ {MinimumSystemMemoryMb}MB required");
        return true;
    }
}
