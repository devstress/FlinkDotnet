using Aspire.Hosting;
using Aspire.Hosting.Testing;
using System.Diagnostics;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NUnit.Framework;


namespace BackPressure.IntegrationTests;

public abstract class KafkaTestBase : IAsyncDisposable
{
	private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(60);

	protected DistributedApplication? AppHost { get; private set; }
	protected IHost? TestHost { get; private set; }
	protected IProducer<string, string>? Producer { get; private set; }
	protected IConsumer<string, string>? Consumer { get; private set; }
	protected IAdminClient? AdminClient { get; private set; }
	protected string? KafkaConnectionString { get; private set; }


	[OneTimeSetUp]
	public virtual async Task OneTimeSetUp()
	{
		var cancellationToken = TestContext.CurrentContext.CancellationToken;

		// Extended startup timeout (prior 60s) to reduce flakiness under cold Docker starts.
		var extendedTimeout = TimeSpan.FromSeconds(120);

		int attempt = 0;
		const int maxAttempts = 3;
		DistributedApplication? app = null;

		while (attempt < maxAttempts)
		{
			attempt++;
			try
			{
				TestContext.WriteLine($"🟡 [AppHost] Attempt {attempt}/{maxAttempts} creating test host (Timeout={extendedTimeout.TotalSeconds}s)");
				var swCreate = Stopwatch.StartNew();

				// TEMP: Revert to existing AppHost until Runner project is added to BackPressureExample.sln
				// var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BackPressure_Runner>(cancellationToken);
				var appHostBuilder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BackPressure_AppHost>(cancellationToken);

				TestContext.WriteLine("🟡 [AppHost] Building distributed application...");
				var buildSw = Stopwatch.StartNew();
				app = await appHostBuilder.BuildAsync(cancellationToken).WaitAsync(extendedTimeout, cancellationToken);
				buildSw.Stop();
				TestContext.WriteLine($"✅ [AppHost] Build complete in {buildSw.Elapsed.TotalSeconds:F1}s");

				TestContext.WriteLine("🟡 [AppHost] Starting distributed application...");
				var startSw = Stopwatch.StartNew();
				await app.StartAsync(cancellationToken).WaitAsync(extendedTimeout, cancellationToken);
				startSw.Stop();
				TestContext.WriteLine($"✅ [AppHost] Start complete in {startSw.Elapsed.TotalSeconds:F1}s");

				swCreate.Stop();
				TestContext.WriteLine($"✅ [AppHost] Infrastructure up in {swCreate.Elapsed.TotalSeconds:F1}s (attempt {attempt})");

				TestContext.WriteLine("🟡 [Health] Awaiting kafka resource healthy notification...");
				var healthSw = Stopwatch.StartNew();
				await app.ResourceNotifications
					.WaitForResourceHealthyAsync("kafka", cancellationToken)
					.WaitAsync(extendedTimeout, cancellationToken);
				healthSw.Stop();
				TestContext.WriteLine($"✅ [Health] Kafka reported healthy in {healthSw.Elapsed.TotalSeconds:F1}s");

				KafkaConnectionString = await app.GetConnectionStringAsync("kafka");
				TestContext.WriteLine($"✅ Kafka connection string: {KafkaConnectionString}");

				var readinessTimeout = TimeSpan.FromSeconds(45);
				TestContext.WriteLine($"🟡 [KafkaReady] Probing cluster readiness (timeout {readinessTimeout.TotalSeconds}s)...");
				await WaitForKafkaReadyAsync(KafkaConnectionString!, readinessTimeout, cancellationToken);

				await SetupKafkaClientsAsync();

				TestContext.WriteLine("✅ [Setup] Infrastructure & clients initialized successfully");
				AppHost = app;
				break; // success
			}
			catch (Exception ex) when (attempt < maxAttempts)
			{
				TestContext.WriteLine($"⚠️ [Retry] Attempt {attempt} failed to initialize infrastructure: {ex.GetType().Name} - {ex.Message}");
				if (ex is TimeoutException)
				{
					TestContext.WriteLine("ℹ️ [Retry] Detected timeout; increasing grace period before next attempt...");
				}
				var backoffMs = attempt * 3000;
				await Task.Delay(backoffMs, cancellationToken);
			}
			catch
			{
				throw;
			}
		}

		if (app == null)
		{
			throw new TimeoutException("Failed to initialize distributed application after retries.");
		}

		TestContext.WriteLine(
			$"🟢 Infrastructure initialized: Kafka={KafkaConnectionString}, " +
			$"Clients=[Producer:{(Producer!=null)}, Consumer:{(Consumer!=null)}, Admin:{(AdminClient!=null)}]");
		TestContext.WriteLine("✅ Aspire infrastructure setup completed");
	}

	[OneTimeTearDown]
	public virtual async Task OneTimeTearDown()
	{
        try
        {
            await DisposeAsync();
        }
        catch (Exception)
        {
            
        }
    }

    public async ValueTask DisposeAsync()
    {
        try { Producer?.Dispose(); } catch { }
        try { Consumer?.Close(); } catch (ObjectDisposedException) { } catch (Confluent.Kafka.KafkaException) { }
        try { Consumer?.Dispose(); } catch { }
        try { AdminClient?.Dispose(); } catch { }

        Producer = null;
        Consumer = null;
        AdminClient = null;

        if (TestHost != null)
        {
            try { await TestHost.StopAsync(); } catch { }
            TestHost.Dispose();
            TestHost = null;
        }

        if (AppHost != null)
        {
            try { await AppHost.StopAsync(); } catch { }
            try { await AppHost.DisposeAsync(); } catch { }
            AppHost = null;
        }

        TestContext.WriteLine("✅ Infrastructure cleanup completed");
        GC.SuppressFinalize(this);
    }

	protected async Task CreateTopicAsync(string topicName, int partitions = 8, short replicationFactor = 1)
	{
		if (AdminClient == null)
			throw new InvalidOperationException("AdminClient is not initialized");

		try
		{
			var topicSpec = new TopicSpecification
			{
				Name = topicName,
				NumPartitions = partitions,
				ReplicationFactor = replicationFactor,
				Configs = new Dictionary<string, string>
				{
					["min.insync.replicas"] = "1",
					["unclean.leader.election.enable"] = "true"
				}
			};

			await AdminClient.CreateTopicsAsync(new[] { topicSpec });
			TestContext.WriteLine($"✅ Topic '{topicName}' created successfully");
			await Task.Delay(2000);
		}
		catch (CreateTopicsException ex)
		{
			if (ex.Results?.Any(r => r.Error.Code == ErrorCode.TopicAlreadyExists) == true)
			{
				TestContext.WriteLine($"ℹ️ Topic '{topicName}' already exists");
			}
			else
			{
				TestContext.WriteLine($"❌ Error creating topic '{topicName}': {ex.Message}");
				throw;
			}
		}
	}

	private async Task SetupKafkaClientsAsync()
	{
		var hostBuilder = Host.CreateApplicationBuilder();

		hostBuilder.Services.AddSingleton<IProducer<string, string>>(provider =>
		{
			TestContext.WriteLine($"🟡 [Setup] Before creating global Producer (BootstrapServers={KafkaConnectionString})");
			var config = new ProducerConfig
			{
				BootstrapServers = KafkaConnectionString,
				EnableIdempotence = true,
				MaxInFlight = 5,
				Acks = Acks.All,
				CompressionType = CompressionType.Snappy,
				BatchSize = 16384,
				LingerMs = 10,
				RequestTimeoutMs = 30000,
			};
			var prod = new ProducerBuilder<string, string>(config)
				.SetErrorHandler((_, __) => { })
				.SetLogHandler((_, __) => { })
				.Build();
			TestContext.WriteLine("✅ [Setup] Global Producer created");
			return prod;
		});

		hostBuilder.Services.AddSingleton<IConsumer<string, string>>(provider =>
		{
			TestContext.WriteLine($"🟡 [Setup] Before creating global Consumer (BootstrapServers={KafkaConnectionString})");
			var config = new ConsumerConfig
			{
				BootstrapServers = KafkaConnectionString,
				GroupId = $"test-group-{Guid.NewGuid()}",
				AutoOffsetReset = AutoOffsetReset.Earliest,
				EnableAutoCommit = false,
				SessionTimeoutMs = 30000,
				MaxPollIntervalMs = 300000,
				FetchMinBytes = 1,
			};
			var cons = new ConsumerBuilder<string, string>(config)
				.SetErrorHandler((_, __) => { })
				.SetLogHandler((_, __) => { })
				.Build();
			TestContext.WriteLine("✅ [Setup] Global Consumer created");
			return cons;
		});

		hostBuilder.Services.AddSingleton<IAdminClient>(provider =>
		{
			TestContext.WriteLine($"🟡 [Setup] Before creating global AdminClient (BootstrapServers={KafkaConnectionString})");
			var config = new AdminClientConfig
			{
				BootstrapServers = KafkaConnectionString,
				SocketTimeoutMs = 60000,
			};
			var adm = new AdminClientBuilder(config)
				.SetErrorHandler((_, __) => { })
				.SetLogHandler((_, __) => { })
				.Build();
			TestContext.WriteLine("✅ [Setup] Global AdminClient created");
			return adm;
		});

		TestHost = hostBuilder.Build();
		await TestHost.StartAsync();

		Producer = TestHost.Services.GetRequiredService<IProducer<string, string>>();
		Consumer = TestHost.Services.GetRequiredService<IConsumer<string, string>>();
		AdminClient = TestHost.Services.GetRequiredService<IAdminClient>();
	}

    private static async Task WaitForKafkaReadyAsync(string bootstrapServers, TimeSpan timeout, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var attempt = 0;
        TestContext.WriteLine($"🔎 [KafkaReady] Probing broker metadata at {bootstrapServers}");
        while (sw.Elapsed < timeout)
        {
            attempt++;
            try
            {
                using var admin = new AdminClientBuilder(new AdminClientConfig
                {
                    BootstrapServers = bootstrapServers,
                    SocketTimeoutMs = 5000,
                })
                // Suppress noisy librdkafka bootstrap logs during readiness probing.
                .SetLogHandler((_, __) => { })
                .SetErrorHandler((_, __) => { })
                .Build();

                var md = admin.GetMetadata(TimeSpan.FromSeconds(3));
                if (md?.Brokers?.Count > 0)
                {
                    TestContext.WriteLine($"✅ [KafkaReady] Metadata OK (brokers={md.Brokers.Count}) after {attempt} attempt(s), {sw.Elapsed.TotalSeconds:F1}s");
                    return;
                }
            }
            catch (Exception ex)
            {
                // Print a concise context line rather than raw librdkafka output.
                TestContext.WriteLine($"⏳ [KafkaReady] Attempt {attempt} failed: {ex.Message}");
            }
            await Task.Delay(500, ct);
        }
        throw new TimeoutException("Kafka did not become ready in time.");
    }
}

