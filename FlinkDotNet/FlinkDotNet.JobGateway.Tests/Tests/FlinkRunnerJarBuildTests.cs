using NUnit.Framework;

namespace FlinkDotNet.JobGateway.Tests.Tests;

public class FlinkRunnerJarBuildTests
{
    [Test]
    public void GatewayBuild_ProducesRunnerJars_InProjectAndOutput()
    {
        // Locate repo root by searching upwards for markers
        var repoRoot = FindRepoRoot(AppContext.BaseDirectory);
        Assert.That(repoRoot, Is.Not.Null, "Could not locate repository root");

        // Project directory for Flink.JobGateway
        var gatewayProjectDir = Path.Combine(repoRoot!, "FlinkDotNet", "FlinkDotNet.JobGateway");
        Assert.That(Directory.Exists(gatewayProjectDir), Is.True, $"Gateway project missing at {gatewayProjectDir}");

        // The prebuild target should copy/create jar at project root
        // Build now only produces Java 17 JAR for Flink 2.1.0 compatibility
        var localJar17 = Path.Combine(gatewayProjectDir, "flink-ir-runner-java17.jar");
        Assert.That(File.Exists(localJar17), Is.True, $"Runner (Java 17) jar not found at {localJar17}. Build should have created or copied it.");

        // And Content/CopyToOutputDirectory should copy it to the output folder
        var binDir = Path.Combine(gatewayProjectDir, "bin");
        Assert.That(Directory.Exists(binDir), Is.True, $"bin directory not found at {binDir}");

        var configDir = Directory.GetDirectories(binDir)
            .OrderByDescending(d => new DirectoryInfo(d).LastWriteTimeUtc)
            .FirstOrDefault();
        Assert.That(configDir, Is.Not.Null, $"No configuration directory found under {binDir}");

        var tfmDir = Directory.GetDirectories(configDir!)
            .FirstOrDefault(d => Path.GetFileName(d)?.StartsWith("net", StringComparison.OrdinalIgnoreCase) == true);
        Assert.That(tfmDir, Is.Not.Null, $"No target framework directory found under {configDir}");

        var outputJar17 = Path.Combine(tfmDir!, "flink-ir-runner-java17.jar");
        Assert.That(File.Exists(outputJar17), Is.True, $"Runner (Java 17) jar not copied to output at {outputJar17}");
    }

    private static string? FindRepoRoot(string start)
    {
        var dir = new DirectoryInfo(start);
        while (dir != null)
        {
            var hasGlobal = File.Exists(Path.Combine(dir.FullName, "global.json"));
            var hasPom = File.Exists(Path.Combine(dir.FullName, "FlinkIRRunner", "pom.xml"));
            if (hasGlobal && hasPom)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}
