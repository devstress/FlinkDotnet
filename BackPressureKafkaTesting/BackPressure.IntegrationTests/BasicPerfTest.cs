using Confluent.Kafka;
using NUnit.Framework;
using System.Diagnostics;

namespace BackPressure.IntegrationTests;

[TestFixture]
public class BasicPerfTest : KafkaTestBase
{
	private const string TestTopicName = "kafka-perf-test-topic";
	private const int PartitionCount = 8;
	private const int TargetMessages = 10_000_000;
	private const int TestDurationSeconds = 60;

	private const int ProducerBatchSize = 2000;
	private const int PreProducerBatchSize = 10000;
	private const int ConsumerBatchSize = 500;

	[OneTimeSetUp]
	public override async Task OneTimeSetUp()
	{
		try
		{
			await base.OneTimeSetUp();
			await CreateTopicAsync(TestTopicName, PartitionCount, 1);
			TestContext.WriteLine($"🕐 INIT COMPLETE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} - BasicPerfTest initialization completed");
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"❌ INIT FAILED: {ex.Message}");
			throw;
		}
	}

	[OneTimeTearDown]
	public override async Task OneTimeTearDown()
	{
		try
		{
			await DeleteTestTopicAsync();
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"⚠️ Topic cleanup warning: {ex.Message}");
		}
		await base.OneTimeTearDown();
		TestContext.WriteLine($"🧹 CLEANUP COMPLETE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} - BasicPerfTest cleanup completed");
	}

	private async Task DeleteTestTopicAsync()
	{
		try
		{
			if (AdminClient != null)
			{
				await AdminClient.DeleteTopicsAsync(new[] { TestTopicName });
				TestContext.WriteLine($"🗑️ Test topic '{TestTopicName}' deleted successfully");
			}
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"⚠️ Warning deleting test topic: {ex.Message}");
		}
	}

	[Test]
	[Category("Performance")]
	[Description("Producer Extreme Throughput Test")]
    public async Task TestProducerThroughput()
	{
		TestContext.WriteLine("🚀 Starting Producer Extreme Throughput Test with Aspire infrastructure");
		TestContext.WriteLine($"📊 Target: {TargetMessages:N0} messages or {TestDurationSeconds}s duration");
		TestContext.WriteLine($"🔗 Kafka Connection: {KafkaConnectionString}");

		var result = await RunAsyncProducerTestWithWarmup();

		TestContext.WriteLine("");
		TestContext.WriteLine("📊 === PRODUCER PERFORMANCE RESULTS ===");
		TestContext.WriteLine($"📤 Total Messages: {result.Messages:N0}");
		TestContext.WriteLine($"⏱️ Total Time: {result.Duration:F2}s");
		TestContext.WriteLine($"🚀 Average Throughput: {result.Throughput:F0} msgs/sec");
		TestContext.WriteLine($"🔗 Infrastructure: Managed by Aspire");
		TestContext.WriteLine("==========================================");

        Assert.That(result.Messages, Is.GreaterThan(0), "Should have produced messages");

		TestContext.WriteLine($"✅ Producer test completed: {result.Messages:N0} messages at {result.Throughput:F0} msgs/sec");
	}

	[Test]
	[Category("Performance")]
	[Description("Consumer Extreme Throughput Test")]
    public async Task TestConsumerThroughput()
	{
		TestContext.WriteLine("🚀 Starting Consumer Extreme Throughput Test with Aspire infrastructure");
		TestContext.WriteLine($"📊 Target: Consume all available messages in {TestDurationSeconds}s or up to {TargetMessages:N0}");
		TestContext.WriteLine($"🔗 Kafka Connection: {KafkaConnectionString}");

		await ProduceTestMessages(TargetMessages);

		var cancellationTokenSource = new CancellationTokenSource();
		var testStopwatch = Stopwatch.StartNew();

		var consumerConfig = new ConsumerConfig
		{
			BootstrapServers = KafkaConnectionString,
			GroupId = "perf-test-consumer-group",
			AutoOffsetReset = AutoOffsetReset.Earliest,
			EnableAutoCommit = false,
			SessionTimeoutMs = 30000,
			MaxPollIntervalMs = 300000,
			FetchMinBytes = 1,
			EnablePartitionEof = true
		};

		var consumedMessages = 0L;
		var consumerTasks = new List<Task>();

		var consumerThreads = Math.Min(PartitionCount, Environment.ProcessorCount);
		for (int consumerId = 0; consumerId < consumerThreads; consumerId++)
		{
			var localConsumerId = consumerId;
			consumerTasks.Add(Task.Run(() =>
			{
                using var consumer = new ConsumerBuilder<string, string>(consumerConfig)
                .Build();
				consumer.Subscribe(TestTopicName);
				var localConsumed = 0L;
				var batchCount = 0;

				try
				{
					while (!cancellationTokenSource.Token.IsCancellationRequested &&
						   testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
					{
						var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));

						if (consumeResult != null && !consumeResult.IsPartitionEOF)
						{
							localConsumed++;
							batchCount++;

							if (batchCount >= ConsumerBatchSize)
							{
								Interlocked.Add(ref consumedMessages, batchCount);
								consumer.Commit(consumeResult);
								batchCount = 0;
							}
						}
					}

					if (batchCount > 0)
					{
						Interlocked.Add(ref consumedMessages, batchCount);
						consumer.Commit();
					}
				}
				catch (Exception ex)
				{
					TestContext.WriteLine($"Consumer {localConsumerId} error: {ex.Message}");
				}
			}, cancellationTokenSource.Token));
		}

		var monitoringTask = Task.Run(async () =>
		{
			while (!cancellationTokenSource.Token.IsCancellationRequested)
			{
				await Task.Delay(5000, cancellationTokenSource.Token);

				var currentCount = Interlocked.Read(ref consumedMessages);
				var elapsed = testStopwatch.Elapsed.TotalSeconds;

				if (currentCount >= TargetMessages || elapsed >= TestDurationSeconds)
				{
					cancellationTokenSource.Cancel();
					break;
				}
			}
		});

		await Task.WhenAll(consumerTasks.Concat(new[] { monitoringTask }));
		testStopwatch.Stop();

		var totalConsumed = Interlocked.Read(ref consumedMessages);
		var totalTime = testStopwatch.Elapsed.TotalSeconds;
		var avgThroughput = totalConsumed / totalTime;

		TestContext.WriteLine("");
		TestContext.WriteLine("📊 === CONSUMER PERFORMANCE RESULTS ===");
		TestContext.WriteLine($"📥 Total Messages: {totalConsumed:N0}");
		TestContext.WriteLine($"⏱️ Total Time: {totalTime:F2}s");
		TestContext.WriteLine($"🚀 Average Throughput: {avgThroughput:F0} msgs/sec");
		TestContext.WriteLine($"🔗 Infrastructure: Managed by Aspire");
		TestContext.WriteLine("=========================================");

        Assert.That(totalConsumed, Is.GreaterThan(0), "Should have consumed messages");

		TestContext.WriteLine($"✅ Consumer test completed: {totalConsumed:N0} messages at {avgThroughput:F0} msgs/sec");
	}

	private async Task<ProducerTestResult> RunAsyncProducerTestWithWarmup()
	{
		TestContext.WriteLine("🔥 Warming up and running Async Producer Test");
		await RunWarmupTest();
		await Task.Delay(1000);
		return await RunAsyncProducerTest();
	}

	private record ProducerTestResult
	{
		public long Messages { get; init; }
		public double Duration { get; init; }
		public double Throughput { get; init; }
	}

	private async Task<ProducerTestResult> RunAsyncProducerTest()
	{
		var testStopwatch = Stopwatch.StartNew();

		var producerConfig = new ProducerConfig
		{
			BootstrapServers = KafkaConnectionString,
			EnableIdempotence = true,
			MaxInFlight = 5,
			Acks = Acks.All,
			CompressionType = CompressionType.Snappy,
			BatchSize = 32768,
			LingerMs = 10,
			RequestTimeoutMs = 30000,
			MessageTimeoutMs = 60000,
			EnableDeliveryReports = false
		};

        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();

		var messagesSent = 0L;
		const string messageTemplate = "async-test-message-payload-optimized-for-extreme-performance";

		var keyPool = new string[ProducerBatchSize];
		for (int i = 0; i < ProducerBatchSize; i++)
		{
			keyPool[i] = $"async-key-{i}";
		}

		var messagePool = new Message<string, string>[ProducerBatchSize];
		for (int i = 0; i < ProducerBatchSize; i++)
		{
			messagePool[i] = new Message<string, string>
			{
				Key = keyPool[i],
				Value = messageTemplate
			};
		}

		try
		{
			while (messagesSent < TargetMessages && testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
			{
				var currentBatchSize = Math.Min(ProducerBatchSize, TargetMessages - messagesSent);

				for (int i = 0; i < currentBatchSize; i++)
				{
					var msgCount = messagesSent + i;
					var partition = (int)(msgCount % PartitionCount);
					var message = messagePool[i];

					producer.Produce(new TopicPartition(TestTopicName, partition), message);
				}

				messagesSent += currentBatchSize;

				if (messagesSent % 100000 == 0)
				{
					producer.Flush(TimeSpan.FromMilliseconds(50));
				}

				if (messagesSent % 1000000 == 0)
				{
					GC.Collect(0, GCCollectionMode.Optimized);
					await Task.Yield();
				}
			}
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"❌ Async producer error: {ex.Message}");
		}

		TestContext.WriteLine($"🔄 Final flush starting for {messagesSent:N0} messages...");
		producer.Flush(TimeSpan.FromSeconds(15));
		TestContext.WriteLine($"✅ Final flush completed");

		testStopwatch.Stop();

		var result = new ProducerTestResult
		{
			Messages = messagesSent,
			Duration = testStopwatch.Elapsed.TotalSeconds,
			Throughput = messagesSent / testStopwatch.Elapsed.TotalSeconds
		};

		TestContext.WriteLine($"🔥 Async Producer: {result.Messages:N0} msgs in {result.Duration:F2}s @ {result.Throughput:F0} msgs/sec");
		return result;
	}

	private async Task ProduceTestMessages(int messageCount)
	{
		TestContext.WriteLine($"📤 Pre-producing {messageCount:N0} test messages with backpressure control...");

		var producerConfig = new ProducerConfig
		{
			BootstrapServers = KafkaConnectionString,
			EnableIdempotence = true,
			MaxInFlight = 5,
			Acks = Acks.All,
			CompressionType = CompressionType.Snappy,
			BatchSize = 32768,
			LingerMs = 10,
			EnableDeliveryReports = false,

			QueueBufferingMaxMessages = 10000000,
			QueueBufferingMaxKbytes = 2097152,
			MessageTimeoutMs = 30000,
			RequestTimeoutMs = 30000
		};

        using var producer = new ProducerBuilder<string, string>(producerConfig)
            .Build();
		const string messageTemplate = "pre-produced-test-message-payload-optimized";

		var processed = 0;
		var stopwatch = Stopwatch.StartNew();
		var pendingTasks = new List<Task>();

		var keyPool = new string[PreProducerBatchSize];
		for (int i = 0; i < PreProducerBatchSize; i++)
		{
			keyPool[i] = $"test-key-{i}";
		}

		var messagePool = new Message<string, string>[PreProducerBatchSize];
		for (int i = 0; i < PreProducerBatchSize; i++)
		{
			messagePool[i] = new Message<string, string>
			{
				Key = keyPool[i],
				Value = messageTemplate
			};
		}

		try
		{
			for (int i = 0; i < messageCount; i += PreProducerBatchSize)
			{
				var currentBatchSize = Math.Min(PreProducerBatchSize, messageCount - i);

				for (int j = 0; j < currentBatchSize; j++)
				{
					var messageIndex = i + j;
					var partition = messageIndex % PartitionCount;
					var message = messagePool[j];

					var task = producer.ProduceAsync(new TopicPartition(TestTopicName, partition), message);
					pendingTasks.Add(task);

					if (pendingTasks.Count >= 1000)
					{
						await Task.WhenAll(pendingTasks);
						pendingTasks.Clear();
					}
				}

				processed += currentBatchSize;

				if (processed % 100000 == 0)
				{
					await Task.WhenAll(pendingTasks);
					pendingTasks.Clear();
					producer.Flush(TimeSpan.FromMilliseconds(100));
				}
			}

			if (pendingTasks.Count > 0)
			{
				await Task.WhenAll(pendingTasks);
			}
		}
		catch (ProduceException<string, string> ex)
		{
			TestContext.WriteLine($"❌ Produce error: {ex.Error.Reason}");
			throw;
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"❌ Unexpected error during message production: {ex.Message}");
			throw;
		}

		producer.Flush(TimeSpan.FromSeconds(30));
		stopwatch.Stop();

		var finalRate = messageCount / stopwatch.Elapsed.TotalSeconds;
		TestContext.WriteLine($"✅ Pre-production completed: {messageCount:N0} messages in {stopwatch.Elapsed.TotalSeconds:F1}s @ {finalRate:F0} msgs/sec");
	}

	private async Task RunWarmupTest()
	{
		if (Producer != null)
		{
			for (int i = 0; i < 5000; i++)
			{
				await Producer.ProduceAsync(TestTopicName, new Message<string, string>
				{
					Key = $"warmup-{i}",
					Value = "warmup-message"
				});
			}

			Producer.Flush(TimeSpan.FromSeconds(5));
			TestContext.WriteLine("✅ Warmup completed");
		}
	}
}



