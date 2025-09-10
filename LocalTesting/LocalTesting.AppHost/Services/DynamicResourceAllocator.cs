using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LocalTesting.AppHost.Services;

/// <summary>
/// Dynamic resource allocator that detects system resources and calculates
/// appropriate memory and CPU allocations for containers based on available hardware.
/// Replaces hardcoded values with adaptive allocation based on current machine capabilities.
/// </summary>
public static class DynamicResourceAllocator
{
    private const double SAFETY_FACTOR = 0.4; // Use 40% of available resources for safety in test environments (reduced from 70%)
    private const long MIN_AVAILABLE_MEMORY_MB = 8192; // Minimum 8GB required for operation (increased to force minimal allocation)
    private const int MIN_CPU_CORES = 1;

    /// <summary>
    /// Container resource allocation configuration
    /// </summary>
    public class ResourceAllocation
    {
        public long RedisMemoryMB { get; set; }
        public long KafkaHeapMemoryMB { get; set; }
        public long KafkaMinMemoryMB { get; set; }
        public long FlinkJobManagerTotalMemoryMB { get; set; }
        public long FlinkJobManagerMetaspaceMemoryMB { get; set; }
        public long FlinkJobManagerOverheadMemoryMB { get; set; }
        public long FlinkTaskManagerTotalMemoryMB { get; set; }
        public long FlinkTaskManagerMetaspaceMemoryMB { get; set; }
        public long FlinkTaskManagerOverheadMemoryMB { get; set; }
        public long FlinkTaskManagerFrameworkHeapMemoryMB { get; set; }
        public long FlinkTaskManagerFrameworkOffHeapMemoryMB { get; set; }
        public long FlinkTaskManagerManagedMemoryMB { get; set; }
        public long FlinkTaskManagerNetworkMemoryMB { get; set; }
        public int KafkaPartitions { get; set; }
        public int FlinkParallelism { get; set; }
        public int TaskSlots { get; set; }
        public string PrometheusRetention { get; set; } = "5m";
        public string PrometheusStorageSize { get; set; } = "100MB";
    }

    /// <summary>
    /// Detects system resources and calculates optimal container resource allocation
    /// </summary>
    public static ResourceAllocation CalculateOptimalAllocation()
    {
        var totalMemoryMB = GetTotalSystemMemoryMB();
        var cpuCores = Environment.ProcessorCount;
        
        Console.WriteLine($"🔍 System Resources Detected:");
        Console.WriteLine($"   💾 Total Memory: {totalMemoryMB:N0} MB ({totalMemoryMB / 1024.0:F1} GB)");
        Console.WriteLine($"   🖥️  CPU Cores: {cpuCores}");
        
        // Calculate available memory for containers (exclude OS and buffer)
        var availableMemoryMB = (long)(totalMemoryMB * SAFETY_FACTOR);
        
        if (availableMemoryMB < MIN_AVAILABLE_MEMORY_MB)
        {
            Console.WriteLine($"⚠️  Warning: Low memory system detected. Using minimum allocation.");
            return CreateMinimalAllocation();
        }
        
        // Distribute memory among components based on their typical resource requirements
        var allocation = CalculateMemoryDistribution(availableMemoryMB, cpuCores);
        
        Console.WriteLine($"📊 Dynamic Resource Allocation (40% of system resources for test environment stability):");
        Console.WriteLine($"   🔴 Redis: {allocation.RedisMemoryMB} MB");
        Console.WriteLine($"   📨 Kafka Heap: {allocation.KafkaHeapMemoryMB} MB (Min: {allocation.KafkaMinMemoryMB} MB)");
        Console.WriteLine($"   ⚙️  Flink JobManager: {allocation.FlinkJobManagerTotalMemoryMB} MB");
        Console.WriteLine($"      - Metaspace: {allocation.FlinkJobManagerMetaspaceMemoryMB} MB");
        Console.WriteLine($"      - Overhead: {allocation.FlinkJobManagerOverheadMemoryMB} MB");
        Console.WriteLine($"   ⚡ Flink TaskManager: {allocation.FlinkTaskManagerTotalMemoryMB} MB");
        Console.WriteLine($"      - Metaspace: {allocation.FlinkTaskManagerMetaspaceMemoryMB} MB");
        Console.WriteLine($"      - Overhead: {allocation.FlinkTaskManagerOverheadMemoryMB} MB");
        Console.WriteLine($"      - Framework Heap: {allocation.FlinkTaskManagerFrameworkHeapMemoryMB} MB");
        Console.WriteLine($"      - Framework Off-Heap: {allocation.FlinkTaskManagerFrameworkOffHeapMemoryMB} MB");
        Console.WriteLine($"      - Managed Memory: {allocation.FlinkTaskManagerManagedMemoryMB} MB");
        Console.WriteLine($"      - Network Memory: {allocation.FlinkTaskManagerNetworkMemoryMB} MB");
        Console.WriteLine($"   🔄 Parallelism: {allocation.FlinkParallelism} (CPU cores: {cpuCores})");
        Console.WriteLine($"   📦 Task Slots: {allocation.TaskSlots}");
        Console.WriteLine($"   📈 Prometheus: {allocation.PrometheusRetention} retention, {allocation.PrometheusStorageSize} storage");
        
        return allocation;
    }
    
    /// <summary>
    /// Creates minimal resource allocation for low-memory systems and test environments
    /// </summary>
    private static ResourceAllocation CreateMinimalAllocation()
    {
        return new ResourceAllocation
        {
            RedisMemoryMB = 16,        // Reduced from 32MB
            KafkaHeapMemoryMB = 128,   // Reduced from 200MB 
            KafkaMinMemoryMB = 64,     // Reduced from 100MB
            FlinkJobManagerTotalMemoryMB = 256,     // Reduced from 480MB
            FlinkJobManagerMetaspaceMemoryMB = 64,  // Reduced from 128MB
            FlinkJobManagerOverheadMemoryMB = 64,   // Reduced from 128MB
            FlinkTaskManagerTotalMemoryMB = 320,    // Reduced from 640MB
            FlinkTaskManagerMetaspaceMemoryMB = 32, // Reduced from 64MB
            FlinkTaskManagerOverheadMemoryMB = 32,  // Reduced from 64MB
            FlinkTaskManagerFrameworkHeapMemoryMB = 32,    // Reduced from 64MB
            FlinkTaskManagerFrameworkOffHeapMemoryMB = 32, // Reduced from 64MB
            FlinkTaskManagerManagedMemoryMB = 32,   // Reduced from 64MB
            FlinkTaskManagerNetworkMemoryMB = 32,   // Reduced from 64MB
            KafkaPartitions = 1,
            FlinkParallelism = 1,
            TaskSlots = 1,
            PrometheusRetention = "1m",  // Reduced from 2m
            PrometheusStorageSize = "10MB"  // Reduced from 20MB
        };
    }
    
    /// <summary>
    /// Calculates memory distribution based on available memory and CPU cores
    /// </summary>
    private static ResourceAllocation CalculateMemoryDistribution(long availableMemoryMB, int cpuCores)
    {
        // Memory allocation percentages for each component
        const double redisPercent = 0.02;      // 2% - Redis is lightweight
        const double kafkaPercent = 0.15;      // 15% - Kafka heap memory
        const double jobManagerPercent = 0.12; // 12% - Flink JobManager
        const double taskManagerPercent = 0.30; // 30% - Flink TaskManager (most memory intensive)
        const double prometheusPercent = 0.05; // 5% - Prometheus storage
        
        // Calculate base allocations
        var redisMemory = Math.Max(32, (long)(availableMemoryMB * redisPercent));
        var kafkaHeapMemory = Math.Max(200, (long)(availableMemoryMB * kafkaPercent));
        var kafkaMinMemory = kafkaHeapMemory / 2;
        
        var jobManagerTotal = Math.Max(480, (long)(availableMemoryMB * jobManagerPercent));
        var jobManagerMetaspace = Math.Max(64, jobManagerTotal / 4);
        var jobManagerOverhead = Math.Max(64, jobManagerTotal / 4);
        
        var taskManagerTotal = Math.Max(640, (long)(availableMemoryMB * taskManagerPercent));
        
        // TaskManager component allocation (must sum to total minus metaspace and overhead)
        var taskManagerMetaspace = Math.Max(64, taskManagerTotal / 10);
        var taskManagerOverhead = Math.Max(64, taskManagerTotal / 10);
        var taskManagerRemainingMemory = taskManagerTotal - taskManagerMetaspace - taskManagerOverhead;
        
        // Distribute remaining TaskManager memory among components
        var taskManagerFrameworkHeap = Math.Max(64, taskManagerRemainingMemory / 6);
        var taskManagerFrameworkOffHeap = Math.Max(64, taskManagerRemainingMemory / 6);
        var taskManagerManagedMemory = Math.Max(64, taskManagerRemainingMemory / 6);
        var taskManagerNetworkMemory = Math.Max(64, taskManagerRemainingMemory / 6);
        
        // Calculate parallelism based on CPU cores and available memory
        var optimalParallelism = Math.Max(1, Math.Min(cpuCores, (int)(taskManagerTotal / 512))); // 512MB per parallel slot
        var taskSlots = Math.Max(1, Math.Min(cpuCores, optimalParallelism));
        
        // Kafka partitions should match parallelism for optimal performance
        var kafkaPartitions = Math.Max(1, optimalParallelism);
        
        // Prometheus storage based on available memory
        var prometheusStorageMB = Math.Max(20, (long)(availableMemoryMB * prometheusPercent));
        var prometheusRetention = prometheusStorageMB > 100 ? "15m" : prometheusStorageMB > 50 ? "10m" : "5m";
        
        return new ResourceAllocation
        {
            RedisMemoryMB = redisMemory,
            KafkaHeapMemoryMB = kafkaHeapMemory,
            KafkaMinMemoryMB = kafkaMinMemory,
            FlinkJobManagerTotalMemoryMB = jobManagerTotal,
            FlinkJobManagerMetaspaceMemoryMB = jobManagerMetaspace,
            FlinkJobManagerOverheadMemoryMB = jobManagerOverhead,
            FlinkTaskManagerTotalMemoryMB = taskManagerTotal,
            FlinkTaskManagerMetaspaceMemoryMB = taskManagerMetaspace,
            FlinkTaskManagerOverheadMemoryMB = taskManagerOverhead,
            FlinkTaskManagerFrameworkHeapMemoryMB = taskManagerFrameworkHeap,
            FlinkTaskManagerFrameworkOffHeapMemoryMB = taskManagerFrameworkOffHeap,
            FlinkTaskManagerManagedMemoryMB = taskManagerManagedMemory,
            FlinkTaskManagerNetworkMemoryMB = taskManagerNetworkMemory,
            KafkaPartitions = kafkaPartitions,
            FlinkParallelism = optimalParallelism,
            TaskSlots = taskSlots,
            PrometheusRetention = prometheusRetention,
            PrometheusStorageSize = $"{prometheusStorageMB}MB"
        };
    }
    
    /// <summary>
    /// Gets total system memory in MB
    /// </summary>
    private static long GetTotalSystemMemoryMB()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return GetLinuxMemoryMB();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GetWindowsMemoryMB();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GetMacMemoryMB();
            }
            else
            {
                Console.WriteLine("⚠️  Unknown OS platform. Using fallback memory detection.");
                return GetFallbackMemoryMB();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error detecting system memory: {ex.Message}. Using fallback.");
            return GetFallbackMemoryMB();
        }
    }
    
    /// <summary>
    /// Gets memory info on Linux systems
    /// </summary>
    private static long GetLinuxMemoryMB()
    {
        try
        {
            var memInfoLines = File.ReadAllLines("/proc/meminfo");
            var totalMemoryLine = memInfoLines.FirstOrDefault(line => line.StartsWith("MemTotal:"));
            
            if (totalMemoryLine != null)
            {
                var parts = totalMemoryLine.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2 && long.TryParse(parts[1], out var memoryKB))
                {
                    return memoryKB / 1024; // Convert KB to MB
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error reading /proc/meminfo: {ex.Message}");
        }
        
        return GetFallbackMemoryMB();
    }
    
    /// <summary>
    /// Gets memory info on Windows systems
    /// </summary>
    private static long GetWindowsMemoryMB()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "wmic",
                    Arguments = "computersystem get TotalPhysicalMemory /value",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            var lines = output.Split('\n');
            var memoryLine = lines.FirstOrDefault(line => line.StartsWith("TotalPhysicalMemory="));
            
            if (memoryLine != null)
            {
                var memoryString = memoryLine.Split('=')[1].Trim();
                if (long.TryParse(memoryString, out var memoryBytes))
                {
                    return memoryBytes / (1024 * 1024); // Convert bytes to MB
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error running wmic: {ex.Message}");
        }
        
        return GetFallbackMemoryMB();
    }
    
    /// <summary>
    /// Gets memory info on macOS systems
    /// </summary>
    private static long GetMacMemoryMB()
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "sysctl",
                    Arguments = "hw.memsize",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            
            process.Start();
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();
            
            var parts = output.Split(':');
            if (parts.Length >= 2)
            {
                var memoryString = parts[1].Trim();
                if (long.TryParse(memoryString, out var memoryBytes))
                {
                    return memoryBytes / (1024 * 1024); // Convert bytes to MB
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️  Error running sysctl: {ex.Message}");
        }
        
        return GetFallbackMemoryMB();
    }
    
    /// <summary>
    /// Fallback memory detection using GC info (approximate)
    /// </summary>
    private static long GetFallbackMemoryMB()
    {
        try
        {
            // Use working set as approximation of available memory
            var currentProcess = Process.GetCurrentProcess();
            var workingSetMB = currentProcess.WorkingSet64 / (1024 * 1024);
            
            // Estimate total system memory as 10x current working set (very rough estimate)
            var estimatedTotalMB = Math.Max(4096, workingSetMB * 10); // Minimum 4GB assumption
            
            Console.WriteLine($"⚠️  Using fallback memory estimation: {estimatedTotalMB} MB");
            return estimatedTotalMB;
        }
        catch
        {
            // Last resort - assume minimum system requirements
            Console.WriteLine("⚠️  All memory detection failed. Assuming minimum 4GB system.");
            return 4096; // 4GB minimum assumption
        }
    }
}