using Microsoft.Playwright;
using NUnit.Framework;

namespace LearningCourse.IntegrationTests;

/// <summary>
/// Playwright fixture for managing browser lifecycle in NUnit tests.
/// Provides browser instances with video recording capability for UI testing.
/// </summary>
[SetUpFixture]
public class PlaywrightFixture
{
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;
    private static string? _videoPath;
    private static bool _initialized = false;
    private static readonly SemaphoreSlim _initLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Gets the shared Playwright instance for all tests.
    /// Initializes on first access (lazy initialization).
    /// </summary>
    public static IPlaywright Playwright
    {
        get
        {
            if (_playwright == null)
            {
                throw new InvalidOperationException(
                    "Playwright not initialized. Use CreateContextWithVideoAsync to initialize automatically.");
            }
            return _playwright;
        }
    }

    /// <summary>
    /// Gets the shared browser instance for all tests.
    /// Initializes on first access (lazy initialization).
    /// </summary>
    public static IBrowser Browser
    {
        get
        {
            if (_browser == null)
            {
                throw new InvalidOperationException(
                    "Browser not initialized. Use CreateContextWithVideoAsync to initialize automatically.");
            }
            return _browser;
        }
    }

    /// <summary>
    /// Gets the video recording path for test recordings.
    /// </summary>
    public static string VideoPath => _videoPath
        ?? throw new InvalidOperationException("Video path not initialized. Use CreateContextWithVideoAsync first.");

    /// <summary>
    /// This is now a no-op since we use lazy initialization.
    /// Initialization happens when first UI video test runs.
    /// </summary>
    [OneTimeSetUp]
    public Task OneTimeSetUp()
    {
        // Initialization moved to lazy loading in CreateContextWithVideoAsync
        // This ensures browser installation only happens when UI video tests actually run
        return Task.CompletedTask;
    }

    /// <summary>
    /// Initialize Playwright and browser on first use (lazy initialization).
    /// This ensures browser installation only happens when UI video tests run.
    /// </summary>
    private static async Task EnsureInitializedAsync()
    {
        if (_initialized)
        {
            return;
        }

        await _initLock.WaitAsync();
        try
        {
            // Double-check after acquiring lock
            if (_initialized)
            {
                return;
            }

            TestContext.WriteLine("🎭 Initializing Playwright for UI video tests (first use)...");

            // Find repository root for video storage
            var repoRoot = FindRepositoryRoot();
            if (repoRoot == null)
            {
                throw new InvalidOperationException("Could not find repository root");
            }

            // Install Playwright browsers if not already installed
            await InstallPlaywrightBrowsersAsync(repoRoot);

            // Create video directory
            _videoPath = Path.Combine(repoRoot, "LocalTesting", "test-logs", "playwright-videos");
            Directory.CreateDirectory(_videoPath);
            TestContext.WriteLine($"📹 Video recordings will be saved to: {_videoPath}");

            // Initialize Playwright
            _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            TestContext.WriteLine("✅ Playwright initialized");

            // Launch browser in headless mode for CI compatibility
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = true, // Use headless mode for CI/CD
                Args = new[] { "--disable-dev-shm-usage" } // Prevent /dev/shm issues in containers
            });
            TestContext.WriteLine("✅ Browser launched (Chromium, headless)");

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    /// <summary>
    /// Cleanup Playwright and browser after all tests complete.
    /// Also collects diagnostic information from observability containers.
    /// </summary>
    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        TestContext.WriteLine("🧹 Cleaning up Playwright resources...");

        // NOTE: Diagnostics collection removed to prevent teardown blocking
        // Diagnostics should be collected manually if needed for debugging

        if (_browser != null)
        {
            await _browser.CloseAsync();
            await _browser.DisposeAsync();
            _browser = null;
            TestContext.WriteLine("✅ Browser closed");
        }

        if (_playwright != null)
        {
            _playwright.Dispose();
            _playwright = null;
            TestContext.WriteLine("✅ Playwright disposed");
        }
        
        TestContext.WriteLine("✅ Playwright teardown complete");
    }

    /// <summary>
    /// Install Playwright browsers programmatically if not already installed.
    /// Executes the install-playwright-browsers.ps1 script using platform-appropriate PowerShell.
    /// </summary>
    private static async Task InstallPlaywrightBrowsersAsync(string repoRoot)
    {
        TestContext.WriteLine("🔍 Checking Playwright browser installation...");

        // Check if browsers are already installed by looking for Chromium executable
        var browsersPath = GetPlaywrightBrowsersPath();
        var chromiumInstalled = CheckChromiumInstalled(browsersPath);

        if (chromiumInstalled)
        {
            TestContext.WriteLine("✅ Playwright browsers already installed, skipping installation");
            return;
        }

        TestContext.WriteLine("📦 Playwright browsers not found, installing...");

        // Get the installation script path
        var testProjectDir = Path.Combine(repoRoot, "LearningCourse", "LearningCourse.IntegrationTests");
        var installScript = Path.Combine(testProjectDir, "install-playwright-browsers.ps1");

        if (!File.Exists(installScript))
        {
            throw new InvalidOperationException($"Installation script not found: {installScript}");
        }

        // Detect OS and choose appropriate PowerShell command
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);
        var powershellCommand = isWindows ? "powershell" : "pwsh";

        TestContext.WriteLine($"💻 Platform: {(isWindows ? "Windows" : "Linux/macOS")}");
        TestContext.WriteLine($"🔧 Using PowerShell command: {powershellCommand}");
        TestContext.WriteLine($"📄 Script: {installScript}");

        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = powershellCommand,
                Arguments = $"-ExecutionPolicy Bypass -File \"{installScript}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = testProjectDir
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null)
            {
                throw new InvalidOperationException("Failed to start Playwright installation process");
            }

            // Read output while process is running
            var outputTask = Task.Run(async () =>
            {
                while (!process.StandardOutput.EndOfStream)
                {
                    var line = await process.StandardOutput.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        TestContext.WriteLine($"  {line}");
                    }
                }
            });

            var errorTask = Task.Run(async () =>
            {
                while (!process.StandardError.EndOfStream)
                {
                    var line = await process.StandardError.ReadLineAsync();
                    if (!string.IsNullOrWhiteSpace(line))
                    {
                        TestContext.WriteLine($"  ⚠️ {line}");
                    }
                }
            });

            await Task.WhenAll(outputTask, errorTask);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"Playwright browser installation failed with exit code {process.ExitCode}. " +
                    "Please check the output above for details.");
            }

            TestContext.WriteLine("✅ Playwright browser installation completed successfully");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"❌ Error installing Playwright browsers: {ex.Message}");
            throw new InvalidOperationException(
                "Failed to install Playwright browsers. " +
                "Please run the installation manually: " +
                $"pwsh -ExecutionPolicy Bypass -File \"{installScript}\"", ex);
        }
    }

    /// <summary>
    /// Get the Playwright browsers installation path based on OS.
    /// </summary>
    private static string GetPlaywrightBrowsersPath()
    {
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);

        if (isWindows)
        {
            // Windows: %USERPROFILE%\AppData\Local\ms-playwright
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "ms-playwright");
        }
        else
        {
            // Linux/macOS: ~/.cache/ms-playwright
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".cache", "ms-playwright");
        }
    }

    /// <summary>
    /// Check if Chromium browser is installed in the Playwright browsers path.
    /// </summary>
    private static bool CheckChromiumInstalled(string browsersPath)
    {
        if (!Directory.Exists(browsersPath))
        {
            return false;
        }

        // Look for chromium directory (any version)
        var chromiumDirs = Directory.GetDirectories(browsersPath, "chromium-*");
        if (chromiumDirs.Length == 0)
        {
            return false;
        }

        // Check if executable exists in any chromium directory
        var isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
            System.Runtime.InteropServices.OSPlatform.Windows);
        var executableName = isWindows ? "chrome.exe" : "chrome";

        foreach (var chromiumDir in chromiumDirs)
        {
            // Search recursively for the executable
            var executables = Directory.GetFiles(chromiumDir, executableName, SearchOption.AllDirectories);
            if (executables.Length > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Collect diagnostic information from Temporal and Prometheus containers.
    /// Inspects configurations, collects logs, and saves them for troubleshooting.
    /// </summary>
    private static async Task CollectObservabilityDiagnosticsAsync()
    {
        TestContext.WriteLine("🔍 Collecting observability diagnostics...");

        var repoRoot = FindRepositoryRoot();
        if (repoRoot == null)
        {
            TestContext.WriteLine("⚠️ Could not find repository root for diagnostics");
            return;
        }

        var temporalLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs", "temporal-logs");
        var prometheusLogsDir = Path.Combine(repoRoot, "LocalTesting", "test-logs", "prometheus-logs");
        Directory.CreateDirectory(temporalLogsDir);
        Directory.CreateDirectory(prometheusLogsDir);

        // Collect Temporal diagnostics (priority per user request)
        await CollectTemporalDiagnosticsAsync(temporalLogsDir);

        // Collect Prometheus diagnostics
        await CollectPrometheusDiagnosticsAsync(prometheusLogsDir);

        TestContext.WriteLine("✅ Observability diagnostics collection complete");
    }

    /// <summary>
    /// Collect Temporal container diagnostics and logs.
    /// </summary>
    private static async Task CollectTemporalDiagnosticsAsync(string logsDir)
    {
        TestContext.WriteLine("🔍 Investigating Temporal container...");

        try
        {
            // Find Temporal container with timeout
            var containerNameResult = await ExecuteCommandAsync("docker ps --filter name=temporal --format {{.Names}}", ignoreErrors: true);
            if (string.IsNullOrWhiteSpace(containerNameResult))
            {
                TestContext.WriteLine("⚠️ No Temporal container found (may have been cleared already)");
                return;
            }

            var containerName = containerNameResult.Trim().Split('\n')[0].Trim();
            if (string.IsNullOrWhiteSpace(containerName))
            {
                TestContext.WriteLine("⚠️ No Temporal container found (may have been cleared already)");
                return;
            }
            
            TestContext.WriteLine($"📦 Found Temporal container: {containerName}");

            // Get container details
            var inspectResult = await ExecuteCommandAsync($"docker inspect {containerName}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "container-inspect.json"), inspectResult);
            TestContext.WriteLine($"✅ Saved container inspect to: container-inspect.json");

            // Get Temporal logs (last 500 lines)
            var logsResult = await ExecuteCommandAsync($"docker logs {containerName} --tail 500");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "temporal.log"), logsResult);
            TestContext.WriteLine($"✅ Saved Temporal logs to: temporal.log");

            // Check if Temporal Web UI is accessible
            var portResult = await ExecuteCommandAsync($"docker port {containerName}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "port-mappings.txt"), portResult);
            TestContext.WriteLine($"✅ Saved port mappings to: port-mappings.txt");

            // Try to get Temporal config if accessible
            var configResult = await ExecuteCommandAsync($"docker exec {containerName} cat /etc/temporal/config/config.yml", ignoreErrors: true);
            if (!string.IsNullOrWhiteSpace(configResult))
            {
                await File.WriteAllTextAsync(Path.Combine(logsDir, "config.yml"), configResult);
                TestContext.WriteLine($"✅ Saved Temporal config to: config.yml");
            }

            TestContext.WriteLine("✅ Temporal diagnostics collected");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error collecting Temporal diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Collect Prometheus container diagnostics and logs.
    /// </summary>
    private static async Task CollectPrometheusDiagnosticsAsync(string logsDir)
    {
        TestContext.WriteLine("🔍 Investigating Prometheus container...");

        try
        {
            // Find Prometheus container with timeout
            var containerNameResult = await ExecuteCommandAsync("docker ps --filter name=prometheus --format {{.Names}}", ignoreErrors: true);
            if (string.IsNullOrWhiteSpace(containerNameResult))
            {
                TestContext.WriteLine("⚠️ No Prometheus container found (may have been cleared already)");
                return;
            }

            var containerName = containerNameResult.Trim().Split('\n')[0].Trim();
            if (string.IsNullOrWhiteSpace(containerName))
            {
                TestContext.WriteLine("⚠️ No Prometheus container found (may have been cleared already)");
                return;
            }
            
            TestContext.WriteLine($"📦 Found Prometheus container: {containerName}");

            // Get container details
            var inspectResult = await ExecuteCommandAsync($"docker inspect {containerName}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "container-inspect.json"), inspectResult);
            TestContext.WriteLine($"✅ Saved container inspect to: container-inspect.json");

            // Get Prometheus logs (last 500 lines)
            var logsResult = await ExecuteCommandAsync($"docker logs {containerName} --tail 500");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "prometheus.log"), logsResult);
            TestContext.WriteLine($"✅ Saved Prometheus logs to: prometheus.log");

            // Check port mappings
            var portResult = await ExecuteCommandAsync($"docker port {containerName}");
            await File.WriteAllTextAsync(Path.Combine(logsDir, "port-mappings.txt"), portResult);
            TestContext.WriteLine($"✅ Saved port mappings to: port-mappings.txt");

            // Try to get Prometheus config
            var configPaths = new[] { "/etc/prometheus/prometheus.yml", "/prometheus/prometheus.yml" };
            foreach (var configPath in configPaths)
            {
                var configResult = await ExecuteCommandAsync($"docker exec {containerName} cat {configPath}", ignoreErrors: true);
                if (!string.IsNullOrWhiteSpace(configResult) && !configResult.Contains("No such file"))
                {
                    await File.WriteAllTextAsync(Path.Combine(logsDir, "prometheus.yml"), configResult);
                    TestContext.WriteLine($"✅ Saved Prometheus config from: {configPath}");
                    break;
                }
            }

            TestContext.WriteLine("✅ Prometheus diagnostics collected");
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error collecting Prometheus diagnostics: {ex.Message}");
        }
    }

    /// <summary>
    /// Execute a shell command and return its output.
    /// </summary>
    private static async Task<string> ExecuteCommandAsync(string command, bool ignoreErrors = false)
    {
        try
        {
            var processStartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(processStartInfo);
            if (process == null)
            {
                return ignoreErrors ? "" : "Failed to start process";
            }

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!ignoreErrors && process.ExitCode != 0)
            {
                TestContext.WriteLine($"⚠️ Command failed (exit {process.ExitCode}): {command}");
                TestContext.WriteLine($"Error: {error}");
            }

            return string.IsNullOrWhiteSpace(output) ? error : output;
        }
        catch (Exception ex)
        {
            if (!ignoreErrors)
            {
                TestContext.WriteLine($"⚠️ Exception executing command '{command}': {ex.Message}");
            }
            return "";
        }
    }

    /// <summary>
    /// Create a new browser context with video recording enabled.
    /// Each test should create its own context for isolation.
    /// This method triggers lazy initialization of Playwright and browser installation.
    /// </summary>
    /// <param name="testName">Name of the test for video filename</param>
    /// <param name="recordVideo">Whether to record video (default: true)</param>
    /// <returns>Browser context configured with video recording</returns>
    public static async Task<IBrowserContext> CreateContextWithVideoAsync(string testName, bool recordVideo = true)
    {
        // Ensure Playwright and browser are initialized (lazy initialization)
        // This is when browser installation happens - only when UI video tests run
        await EnsureInitializedAsync();

        var contextOptions = new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            RecordVideoDir = recordVideo ? VideoPath : null,
            RecordVideoSize = recordVideo ? new RecordVideoSize { Width = 1280, Height = 720 } : null
        };

        var context = await Browser.NewContextAsync(contextOptions);
        
        if (recordVideo)
        {
            TestContext.WriteLine($"📹 Video recording enabled for test: {testName}");
        }

        return context;
    }

    /// <summary>
    /// Save video from context and close it properly.
    /// Call this at the end of each test to finalize video recording.
    /// </summary>
    /// <param name="context">Browser context to close</param>
    /// <param name="testName">Name of the test for final video filename</param>
    public static async Task<string?> CloseContextAndSaveVideoAsync(IBrowserContext context, string testName)
    {
        try
        {
            // Close all pages to finalize video - use ToList() to avoid collection modification during enumeration
            var pages = context.Pages.ToList();
            foreach (var page in pages)
            {
                await page.CloseAsync();
            }

            // Close context to finalize video recording
            await context.CloseAsync();

            // Wait a moment for video file to be written
            await Task.Delay(1000); // Increased from 500ms to ensure file is fully written

            // Rename video file to include test name
            var videoFiles = Directory.GetFiles(VideoPath, "*.webm")
                .OrderByDescending(f => new FileInfo(f).LastWriteTime)
                .ToList();

            if (videoFiles.Count > 0)
            {
                var latestVideo = videoFiles[0];
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var newVideoName = $"{testName}_{timestamp}.webm";
                var newVideoPath = Path.Combine(VideoPath, newVideoName);

                // Rename the video file
                File.Move(latestVideo, newVideoPath, overwrite: true);
                
                var fileInfo = new FileInfo(newVideoPath);
                TestContext.WriteLine($"✅ Video saved: {newVideoName} ({fileInfo.Length / 1024} KB)");
                
                // Return the path to the saved video for verification
                return newVideoPath;
            }
            else
            {
                TestContext.WriteLine("⚠️ No video file found after context close");
                return null;
            }
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"⚠️ Error saving video: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Find the repository root by looking for global.json
    /// </summary>
    private static string? FindRepositoryRoot()
    {
        var dir = new DirectoryInfo(Environment.CurrentDirectory);
        while (dir != null)
        {
            if (File.Exists(Path.Combine(dir.FullName, "global.json")))
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }
        return null;
    }
}