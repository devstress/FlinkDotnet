using Aspire.Hosting;
using Aspire.Hosting.Testing;
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
		var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.BackPressure_AppHost>(cancellationToken);
		var app = await appHost.BuildAsync(cancellationToken).WaitAsync(defaultTimeout, cancellationToken);
		await app.StartAsync(cancellationToken).WaitAsync(defaultTimeout, cancellationToken);

		await app.ResourceNotifications
			.WaitForResourceHealthyAsync("kafka", cancellationToken)
			.WaitAsync(defaultTimeout, cancellationToken);

		KafkaConnectionString = await app.GetConnectionStringAsync("kafka");
		//KafkaConnectionString = "kafka-broker-4:9092,kafka-broker-5:9092,kafka-broker-6:9092,kafka-broker-7:9092";
		TestContext.WriteLine($"✅ Kafka connection string: {KafkaConnectionString}");

		await SetupKafkaClientsAsync();

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
            // Don't rethrow, we want to ensure cleanup completes as much as possible
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
				//MessageTimeoutMs = 60000,
				//SecurityProtocol = SecurityProtocol.SaslPlaintext,
				//SaslMechanism = SaslMechanism.Plain,
				//SaslUsername = "admin",
				//SaslPassword = "admin123",
			};
			return new ProducerBuilder<string, string>(config).Build();
		});

		hostBuilder.Services.AddSingleton<IConsumer<string, string>>(provider =>
		{
			var config = new ConsumerConfig
			{
				BootstrapServers = KafkaConnectionString,
				GroupId = $"test-group-{Guid.NewGuid()}",
				AutoOffsetReset = AutoOffsetReset.Earliest,
				EnableAutoCommit = false,
				SessionTimeoutMs = 30000,
				MaxPollIntervalMs = 300000,
				FetchMinBytes = 1,
				//SecurityProtocol = SecurityProtocol.SaslPlaintext,
				//SaslMechanism = SaslMechanism.Plain,
				//SaslUsername = "admin",
				//SaslPassword = "admin123",
			};
			return new ConsumerBuilder<string, string>(config).Build();
		});

		hostBuilder.Services.AddSingleton<IAdminClient>(provider =>
		{
			var config = new AdminClientConfig
			{
				BootstrapServers = KafkaConnectionString,
				SocketTimeoutMs = 60000,
				//ApiVersionRequestTimeoutMs = 10000,
				//SecurityProtocol = SecurityProtocol.SaslPlaintext,
				//SaslMechanism = SaslMechanism.Plain,
				//SaslUsername = "admin",
				//SaslPassword = "admin123",
			};
			return new AdminClientBuilder(config).Build();
		});

		TestHost = hostBuilder.Build();
		await TestHost.StartAsync();

		Producer = TestHost.Services.GetRequiredService<IProducer<string, string>>();
		Consumer = TestHost.Services.GetRequiredService<IConsumer<string, string>>();
		AdminClient = TestHost.Services.GetRequiredService<IAdminClient>();
	}
}
