using Confluent.Kafka;
using Confluent.Kafka.Admin;
using System.Diagnostics;
using NUnit.Framework;

namespace BackPressure.IntegrationTests;

[TestFixture]
public class TransactionPerfTest : KafkaTestBase
{
	private const int PartitionCount = 8;
	private const int TargetMessages = 1_000_000;
	private const int TestDurationSeconds = 60;
	private const string TopicA = "perf-topic-a";
	private const string TopicB = "perf-topic-b";

	private const int ProducerThreadCount = 8;
	private const int BatchSize = 200;

	[OneTimeSetUp]
	public override async Task OneTimeSetUp()
	{
		await base.OneTimeSetUp();
		await CreateTestTopicsAsync();
	}

	[OneTimeTearDown]
	public override async Task OneTimeTearDown()
	{
		await DeleteTestTopicsAsync();
		await base.OneTimeTearDown();
	}

	private async Task CreateTestTopicsAsync()
	{
		if (AdminClient == null)
		{
			TestContext.WriteLine("❌ AdminClient is not available");
			throw new InvalidOperationException("AdminClient is not initialized");
		}

		var topicSpecs = new[]
		{
			new TopicSpecification { Name = TopicA, NumPartitions = PartitionCount, ReplicationFactor = 1 },
			new TopicSpecification { Name = TopicB, NumPartitions = PartitionCount, ReplicationFactor = 1 }
		};

		try
		{
			await AdminClient.CreateTopicsAsync(topicSpecs);
			TestContext.WriteLine($"✅ Test topics created successfully");
			await Task.Delay(2000);
		}
		catch (CreateTopicsException ex)
		{
			if (ex.Results?.Any(r => r.Error.Code != ErrorCode.TopicAlreadyExists) == true)
			{
				TestContext.WriteLine($"❌ Error creating test topics: {ex.Message}");
				throw;
			}
			else
			{
				TestContext.WriteLine($"✅ Test topics already exist");
			}
		}
	}

	private async Task DeleteTestTopicsAsync()
	{
		try
		{
			if (AdminClient != null)
			{
				await AdminClient.DeleteTopicsAsync(new[] { TopicA, TopicB });
			}
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"⚠️ Warning deleting test topics: {ex.Message}");
		}
	}

	[Test]
	[Category("Performance")]
	[Category("Transaction")]
	[Description("Batch vs Flush vs Transactional Performance Comparison Test")]
    public async Task BatchVsFlushVsTransactionalPerformanceTest()
	{
		TestContext.WriteLine("🚀 Starting comprehensive performance comparison test");
		TestContext.WriteLine($"📊 Configuration: {ProducerThreadCount} threads, {PartitionCount} partitions, batch size {BatchSize}");

		ValidateThreadPartitionRatio();

		var normalBatchResults = await RunNormalProducerTest(enableFlush: false);
		await Task.Delay(2000);
		GC.Collect();
		GC.WaitForPendingFinalizers();

		var normalFlushResults = await RunNormalProducerTest(enableFlush: true);
		await Task.Delay(2000);
		GC.Collect();
		GC.WaitForPendingFinalizers();

		var transactionalResults = await RunTransactionalProducerTest();

		OutputResults(normalBatchResults, normalFlushResults, transactionalResults);

		Assert.That(normalBatchResults.Messages, Is.GreaterThan(0), "Normal batch producer should have produced messages");
		Assert.That(normalFlushResults.Messages, Is.GreaterThan(0), "Normal flush producer should have produced messages");
		Assert.That(transactionalResults.Messages, Is.GreaterThan(0), "Transactional producer should have produced messages");

		TestContext.WriteLine("✅ All performance tests completed successfully");
	}

	private void ValidateThreadPartitionRatio()
	{
		if (ProducerThreadCount % PartitionCount != 0 && PartitionCount % ProducerThreadCount != 0)
		{
			throw new ArgumentException($"Thread count ({ProducerThreadCount}) must have integer ratio with partition count ({PartitionCount})");
		}
		TestContext.WriteLine($"✅ Thread-partition ratio validated: {ProducerThreadCount} threads, {PartitionCount} partitions");
	}

	private async Task<ProducerTestResult> RunNormalProducerTest(bool enableFlush)
	{
		var mode = enableFlush ? "Normal Flush" : "Normal Async";

		var testStopwatch = Stopwatch.StartNew();
		var totalMessagesSent = 0L;
		var totalBatches = 0L;
		var totalFlushTimeMs = 0L;
		var totalBatchTimeMs = 0L;

		var partitionAssignments = GetPartitionAssignments();
		var producerTasks = new List<Task>();

		for (int threadId = 0; threadId < ProducerThreadCount; threadId++)
		{
			var localThreadId = threadId;
			var assignedPartitions = partitionAssignments[localThreadId];

			var task = Task.Run(async () =>
			{
				var producerConfig = KafkaConfigHelper.CreatePerformanceProducerConfig(KafkaConnectionString!);

				using var producer = new ProducerBuilder<string, string>(producerConfig).Build();

				var threadMessagesSent = 0L;
				var threadBatches = 0L;
				var threadFlushTimeMs = 0L;
				var threadBatchTimeMs = 0L;
				var targetMessagesPerThread = TargetMessages / ProducerThreadCount;

				var messagePool = CreateMessagePool(localThreadId);

				while (threadMessagesSent < targetMessagesPerThread &&
					   testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
				{
					var currentBatchSize = Math.Min(BatchSize, targetMessagesPerThread - threadMessagesSent);
					var batchStopwatch = Stopwatch.StartNew();

					for (int i = 0; i < currentBatchSize; i++)
					{
						var partition = assignedPartitions[i % assignedPartitions.Length];
						var message = messagePool[i % messagePool.Length];

						producer.Produce(new TopicPartition(TopicA, partition), message);
						producer.Produce(new TopicPartition(TopicB, partition), message);
					}

					if (enableFlush)
					{
						var flushStopwatch = Stopwatch.StartNew();
						producer.Flush(TimeSpan.FromSeconds(10));
						flushStopwatch.Stop();
						threadFlushTimeMs += (long)flushStopwatch.Elapsed.TotalMilliseconds;
					}

					batchStopwatch.Stop();
					threadBatchTimeMs += (long)batchStopwatch.Elapsed.TotalMilliseconds;
					threadMessagesSent += currentBatchSize;
					threadBatches++;
				}

				if (!enableFlush)
				{
					var finalFlushStopwatch = Stopwatch.StartNew();
					producer.Flush(TimeSpan.FromSeconds(15));
					finalFlushStopwatch.Stop();
					threadFlushTimeMs += (long)finalFlushStopwatch.Elapsed.TotalMilliseconds;
				}

				Interlocked.Add(ref totalMessagesSent, threadMessagesSent);
				Interlocked.Add(ref totalBatches, threadBatches);
				Interlocked.Add(ref totalFlushTimeMs, threadFlushTimeMs);
				Interlocked.Add(ref totalBatchTimeMs, threadBatchTimeMs);
			});

			producerTasks.Add(task);
		}

		await Task.WhenAll(producerTasks);
		testStopwatch.Stop();

		var finalMessagesSent = Interlocked.Read(ref totalMessagesSent);
		var finalBatches = Interlocked.Read(ref totalBatches);
		var finalFlushTimeMs = Interlocked.Read(ref totalFlushTimeMs);
		var finalBatchTimeMs = Interlocked.Read(ref totalBatchTimeMs);

		var result = new ProducerTestResult
		{
			Messages = finalMessagesSent * 2,
			Duration = testStopwatch.Elapsed.TotalSeconds,
			Throughput = (finalMessagesSent * 2) / testStopwatch.Elapsed.TotalSeconds,
			BatchCount = finalBatches,
			AvgBatchTime = finalBatches > 0 ? (double)finalBatchTimeMs / finalBatches : 0,
			AvgFlushTime = enableFlush && finalBatches > 0 ? (double)finalFlushTimeMs / finalBatches : 0,
			TotalFlushTime = finalFlushTimeMs,
			Mode = mode
		};

		TestContext.WriteLine($"✅ {mode} test completed: {result.Messages:N0} messages at {result.Throughput:F0} msgs/sec");
		return result;
	}

	private async Task<ProducerTestResult> RunTransactionalProducerTest()
	{
		TestContext.WriteLine($"🔒 Running Transactional producer test...");

		var testStopwatch = Stopwatch.StartNew();
		var totalMessagesSent = 0L;
		var totalTransactions = 0L;
		var totalTxTimeMs = 0L;

		var partitionAssignments = GetPartitionAssignments();
		var producerTasks = new List<Task>();

		for (int threadId = 0; threadId < ProducerThreadCount; threadId++)
		{
			var localThreadId = threadId;
			var assignedPartitions = partitionAssignments[localThreadId];

			var task = Task.Run(async () =>
			{
				var producerConfig = KafkaConfigHelper.CreatePerformanceProducerConfig(KafkaConnectionString!);
				producerConfig.TransactionalId = $"partition-aware-tx-{localThreadId}-{Guid.NewGuid()}";
				producerConfig.CompressionType = CompressionType.Zstd;
				producerConfig.EnableIdempotence = true;

				using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
				producer.InitTransactions(TimeSpan.FromSeconds(15));

				var threadMessagesSent = 0L;
				var threadTransactions = 0L;
				var threadTxTimeMs = 0L;
				var targetMessagesPerThread = TargetMessages / ProducerThreadCount;

				var messagePool = CreateMessagePool(localThreadId);

				while (threadMessagesSent < targetMessagesPerThread &&
					   testStopwatch.Elapsed.TotalSeconds < TestDurationSeconds)
				{
					var currentBatchSize = Math.Min(BatchSize, targetMessagesPerThread - threadMessagesSent);
					var txStopwatch = Stopwatch.StartNew();

					producer.BeginTransaction();
					try
					{
						for (int i = 0; i < currentBatchSize; i++)
						{
							var partition = assignedPartitions[i % assignedPartitions.Length];
							var message = messagePool[i % messagePool.Length];

							producer.Produce(new TopicPartition(TopicA, partition), message);
							producer.Produce(new TopicPartition(TopicB, partition), message);
						}

						producer.CommitTransaction();
						txStopwatch.Stop();

						threadTxTimeMs += (long)txStopwatch.Elapsed.TotalMilliseconds;
						threadMessagesSent += currentBatchSize;
						threadTransactions++;
					}
					catch (Exception ex)
					{
						producer.AbortTransaction();
						txStopwatch.Stop();
						TestContext.WriteLine($"❌ Transaction aborted in thread {localThreadId}: {ex.Message}");
						throw;
					}
				}

				Interlocked.Add(ref totalMessagesSent, threadMessagesSent);
				Interlocked.Add(ref totalTransactions, threadTransactions);
				Interlocked.Add(ref totalTxTimeMs, threadTxTimeMs);
			});

			producerTasks.Add(task);
		}

		await Task.WhenAll(producerTasks);
		testStopwatch.Stop();

		var finalMessagesSent = Interlocked.Read(ref totalMessagesSent);
		var finalTransactions = Interlocked.Read(ref totalTransactions);
		var finalTxTimeMs = Interlocked.Read(ref totalTxTimeMs);

		var result = new ProducerTestResult
		{
			Messages = finalMessagesSent * 2,
			Duration = testStopwatch.Elapsed.TotalSeconds,
			Throughput = (finalMessagesSent * 2) / testStopwatch.Elapsed.TotalSeconds,
			TransactionCount = finalTransactions,
			AvgTxTime = finalTransactions > 0 ? (double)finalTxTimeMs / finalTransactions : 0,
			TxPerSecond = finalTransactions / testStopwatch.Elapsed.TotalSeconds,
			Mode = "Transactional"
		};

		TestContext.WriteLine($"✅ Transactional test completed: {result.Messages:N0} messages at {result.Throughput:F0} msgs/sec");
		return result;
	}

	private int[][] GetPartitionAssignments()
	{
		var assignments = new int[ProducerThreadCount][];

		if (ProducerThreadCount <= PartitionCount)
		{
			var partitionsPerThread = PartitionCount / ProducerThreadCount;
			for (int i = 0; i < ProducerThreadCount; i++)
			{
				var startPartition = i * partitionsPerThread;
				assignments[i] = Enumerable.Range(startPartition, partitionsPerThread).ToArray();
			}
		}

		return assignments;
	}

	private Message<string, string>[] CreateMessagePool(int threadId)
	{
		var pool = new Message<string, string>[BatchSize];
		for (int i = 0; i < BatchSize; i++)
		{
			pool[i] = new Message<string, string>
			{
				Key = $"thread-{threadId}-key-{i}",
				Value = $"partition-aware-test-message-thread-{threadId}-{i}"
			};
		}
		return pool;
	}

	private void OutputResults(ProducerTestResult normalBatchResults, ProducerTestResult normalFlushResults, ProducerTestResult transactionalResults)
	{
		TestContext.WriteLine("");
		TestContext.WriteLine("═══════════════════════════════════════════════════════");
		TestContext.WriteLine("            PARTITION-AWARE PERFORMANCE TEST            ");
		TestContext.WriteLine("═══════════════════════════════════════════════════════");
		TestContext.WriteLine($"Configuration:");
		TestContext.WriteLine($"  Threads: {ProducerThreadCount} | Partitions: {PartitionCount} | Batch Size: {BatchSize:N0}");
		TestContext.WriteLine($"  Infrastructure: Managed by Aspire/Docker");
		TestContext.WriteLine("");
		TestContext.WriteLine($"📈 THROUGHPUT COMPARISON: Batch {normalBatchResults.Throughput:F0} | Flush {normalFlushResults.Throughput:F0} | TX {transactionalResults.Throughput:F0} msgs/sec");
		TestContext.WriteLine($"⏱️ AVG BATCH TIME: Batch {normalBatchResults.AvgBatchTime:F2} | Flush {normalFlushResults.AvgBatchTime:F2} | TX {transactionalResults.AvgTxTime:F2} ms");
		TestContext.WriteLine($"🔄 BATCH/TX PER SEC: Batch {(normalBatchResults.BatchCount / normalBatchResults.Duration):F1} | Flush {(normalFlushResults.BatchCount / normalFlushResults.Duration):F1} | TX {transactionalResults.TxPerSecond:F1}");
		TestContext.WriteLine("");

		TestContext.WriteLine($"🚀 NORMAL BATCH PRODUCER:");
		TestContext.WriteLine($"  Throughput: {normalBatchResults.Throughput:F0} msgs/sec");
		TestContext.WriteLine($"  Total Messages: {normalBatchResults.Messages:N0}");
		TestContext.WriteLine($"  Duration: {normalBatchResults.Duration:F2}s");
		TestContext.WriteLine($"  Batches: {normalBatchResults.BatchCount:N0}");
		TestContext.WriteLine($"  Avg Batch Time: {normalBatchResults.AvgBatchTime:F2}ms");
		TestContext.WriteLine($"  Final Flush Time: {normalBatchResults.TotalFlushTime:F0}ms");
		TestContext.WriteLine("");

		TestContext.WriteLine($"🔥 NORMAL FLUSH PRODUCER:");
		TestContext.WriteLine($"  Throughput: {normalFlushResults.Throughput:F0} msgs/sec");
		TestContext.WriteLine($"  Total Messages: {normalFlushResults.Messages:N0}");
		TestContext.WriteLine($"  Duration: {normalFlushResults.Duration:F2}s");
		TestContext.WriteLine($"  Batches: {normalFlushResults.BatchCount:N0}");
		TestContext.WriteLine($"  Avg Batch Time: {normalFlushResults.AvgBatchTime:F2}ms");
		TestContext.WriteLine($"  Avg Flush Time: {normalFlushResults.AvgFlushTime:F2}ms");
		TestContext.WriteLine("");

		TestContext.WriteLine($"🔒 TRANSACTIONAL PRODUCER:");
		TestContext.WriteLine($"  Throughput: {transactionalResults.Throughput:F0} msgs/sec");
		TestContext.WriteLine($"  Total Messages: {transactionalResults.Messages:N0}");
		TestContext.WriteLine($"  Duration: {transactionalResults.Duration:F2}s");
		TestContext.WriteLine($"  Transactions: {transactionalResults.TransactionCount:N0}");
		TestContext.WriteLine($"  TX/sec: {transactionalResults.TxPerSecond:F1}");
		TestContext.WriteLine($"  Avg TX Time: {transactionalResults.AvgTxTime:F2}ms");
		TestContext.WriteLine($"  Msgs per TX: {(transactionalResults.Messages / Math.Max(transactionalResults.TransactionCount, 1)):F1}");
		TestContext.WriteLine("");

		TestContext.WriteLine($"📊 PERFORMANCE RANKING:");
		var results = new[] { normalBatchResults, normalFlushResults, transactionalResults }
			.OrderByDescending(r => r.Throughput)
			.ToArray();

		for (int i = 0; i < results.Length; i++)
		{
			var rank = i == 0 ? "🥇" : i == 1 ? "🥈" : "🥉";
			TestContext.WriteLine($"  {rank} {results[i].Mode}: {results[i].Throughput:F0} msgs/sec");
		}
		TestContext.WriteLine("═══════════════════════════════════════════════════════");
	}

	private record ProducerTestResult
	{
		public long Messages { get; init; }
		public double Duration { get; init; }
		public double Throughput { get; init; }
		public long TransactionCount { get; init; } = 0;
		public long BatchCount { get; init; } = 0;
		public double TxPerSecond { get; init; } = 0;
		public double AvgTxTime { get; init; } = 0;
		public double AvgBatchTime { get; init; } = 0;
		public double AvgFlushTime { get; init; } = 0;
		public long TotalFlushTime { get; init; } = 0;
		public string Mode { get; init; } = "";
	}
}
