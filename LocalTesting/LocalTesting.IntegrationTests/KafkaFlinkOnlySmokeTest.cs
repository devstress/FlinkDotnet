using Aspire.Hosting.Testing;
using Confluent.Kafka;
using NUnit.Framework;

namespace LocalTesting.IntegrationTests;

[TestFixture, NonParallelizable]
[Category("kafka-flink-only")]
public class KafkaFlinkOnlySmokeTest
{
    [Test]
    public async Task KafkaAndFlink_StartWithoutGateway_Succeeds()
    {
        using var disableGateway = new EnvironmentVariableScope("INCLUDE_FLINK_GATEWAY", "0");

        TestPrerequisites.EnsureDockerAvailable();

        var baseToken = TestContext.CurrentContext.CancellationToken;
        using var testTimeout = new System.Threading.CancellationTokenSource(TimeSpan.FromMinutes(15));
        using var linkedCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(baseToken, testTimeout.Token);
        var ct = linkedCts.Token;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.LocalTesting_FlinkSqlAppHost>(ct);
        var app = await appHost.BuildAsync(ct);
        using var startCts = System.Threading.CancellationTokenSource.CreateLinkedTokenSource(ct);
        startCts.CancelAfter(TimeSpan.FromMinutes(15));
        await app.StartAsync(startCts.Token);

        try
        {
            await app.ResourceNotifications
                .WaitForResourceHealthyAsync("kafka", ct)
                .WaitAsync(TimeSpan.FromSeconds(120), ct);

            var kafka = await app.GetConnectionStringAsync("kafka", ct);

            var kafkaReadyTask = WaitForKafkaReady(kafka!, TimeSpan.FromSeconds(180), ct);
            var jmBaseTask = GetContainerHttpBaseAsync("flink-jobmanager", 8081, ct);

            await Task.WhenAll(kafkaReadyTask, jmBaseTask);

            var jmBase = jmBaseTask.Result;

            await WaitForFlinkReadyAsync($"{jmBase}v1/overview", TimeSpan.FromSeconds(180), ct);

            var gatewayPresence = RunProcess("docker", "ps -q --filter name=flink-job-gateway");
            Assert.That(string.IsNullOrWhiteSpace(gatewayPresence), Is.True, "Gateway container should not start when INCLUDE_FLINK_GATEWAY=0");
        }
        finally
        {
            try { await app.DisposeAsync(); } catch { }
        }
    }

    private static async Task WaitForKafkaReady(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig { BootstrapServers = bootstrapServers }).Build();
                var metadata = admin.GetMetadata(TimeSpan.FromSeconds(5));
                if (metadata?.Brokers.Count > 0)
                {
                    TestContext.WriteLine($"✅ Kafka ready at {bootstrapServers}");
                    return;
                }
            }
            catch
            {
                await Task.Delay(1000, ct);
            }
        }

        throw new TimeoutException($"Kafka did not become ready within {timeout.TotalSeconds:F0}s at {bootstrapServers}");
    }

    private static async Task WaitForFlinkReadyAsync(string overviewUrl, TimeSpan timeout, CancellationToken ct)
    {
        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            try
            {
                var resp = await http.GetAsync(overviewUrl, ct);
                if (resp.IsSuccessStatusCode)
                {
                    TestContext.WriteLine($"✅ Flink JobManager ready at {overviewUrl}");
                    return;
                }
            }
            catch (Exception ex)
            {
                TestContext.WriteLine($"🟡 Flink readiness check failed ({ex.GetType().Name}) - elapsed: {sw.Elapsed.TotalSeconds:F1}s");
            }

            await Task.Delay(1000, ct);
        }

        throw new TimeoutException($"Flink JobManager not ready within {timeout.TotalSeconds:F0}s at {overviewUrl}");
    }

    
    private static async Task<string> GetContainerHttpBaseAsync(string nameFilter, int containerPort, CancellationToken ct)
    {
        var deadline = DateTime.UtcNow.AddSeconds(120);
        while (DateTime.UtcNow < deadline)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var id = await Task.Run(() => RunProcess("docker", $"ps -q --filter name={nameFilter}"), ct);
                var containerId = id.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                if (!string.IsNullOrEmpty(containerId))
                {
                    var portOutput = await Task.Run(() => RunProcess("docker", $"port {containerId} {containerPort}/tcp"), ct);
                    var hostPort = portOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Trim();
                    if (!string.IsNullOrEmpty(hostPort))
                    {
                        var candidate = hostPort.Split(':').Last().Trim();
                        if (!string.IsNullOrEmpty(candidate))
                        {
                            return $"http://localhost:{candidate}/";
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                // ignore and retry until deadline
            }

            await Task.Delay(1000, ct);
        }

        return $"http://localhost:{containerPort}/";
    }

    private static string RunProcess(string fileName, string arguments)
    {
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(psi)!;
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit(10000);
        return output;
    }
}












