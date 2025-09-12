using BackPressure.Common;
using Confluent.Kafka;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace BackPressure.IntegrationTests;

public static class TestConfiguration
{
	public const bool USE_BINARY_SERIALIZATION = true;
	public const bool USE_BATCH_PROCESSING = true;
    public const bool USE_KAFKA_MODE = true;
	public const bool ELIMINATE_SYNC_WAITS = true;
	public const int TEST_TIMEOUT_SECONDS = 120;
	public const int TARGET_MESSAGES = 3_000_000;

	public const int BATCH_SIZE = 100;
	public const int CUSTOMERS = 300;

	public const double GATEWAY_CORES_PERCENTAGE = 0.2;
	public const double KEYGEN_CORES_PERCENTAGE = 0.4;
	public const int PROCESSOR_PARTITION_COUNT = 8;
	public const int TOPIC_PARTITION_COUNT = 8;
	public const int MAX_BATCH = 16384;
	public const int MAX_QUEUE = 2;

	public const int KEYGEN_LATENCY = 10;
	public const int PROCESSING_LATENCY = 10;
	public const int PROCESSING_TIMEOUT = 0;

	public static void LogConfiguration()
	{
        TestContext.WriteLine($"🔧 Performance Mode: Kafka=true, Binary={USE_BINARY_SERIALIZATION}, Batch={USE_BATCH_PROCESSING}, BatchSize={BATCH_SIZE}");
		TestContext.WriteLine($"🔧 Sync Mode: EliminateSyncWaits={ELIMINATE_SYNC_WAITS}");
		TestContext.WriteLine($"🔧 Scale: Customers={CUSTOMERS}, Target={TARGET_MESSAGES:N0}, Timeout={TEST_TIMEOUT_SECONDS}s");
	}

}

public class TestData
{
	public Utf8Name[] CustomerIds { get; set; } = null!;
	public DummyMessage[,] KeyGenPartitions { get; set; } = null!;
	public long[,] KeyGenIndices { get; set; } = null!;
	public DummyMessage[,] ProcessorPartitions { get; set; } = null!;
	public long[,] ProcessorIndices { get; set; } = null!;
	public PooledMetricsService MetricsService { get; set; } = new();
	public CancellationTokenSource CancellationTokenSource { get; set; } = new();
}

public class TaskFactory
{
	private readonly PerformanceMetrics _metrics;

	private static long _kafkaProduceCallCount = 0;
	private static long _kafkaProduceTotalTicks = 0;

	private static long _kafkaConsumeCallCount = 0;
	private static long _kafkaConsumeTotalTicks = 0;
	private static long _kafkaConsumeSuccessCount = 0;

	private static long _batchProcessorCallCount = 0;
	private static long _batchProcessorTotalTicks = 0;

	private static long _consumerMethodCallCount = 0;
	private static long _consumerMethodTotalTicks = 0;

	private static long _dataProcessingCallCount = 0;
	private static long _dataProcessingTotalTicks = 0;
	private static long _metricsCallCount = 0;
	private static long _metricsTotalTicks = 0;

	private static long _deserializeCallCount = 0;
	private static long _deserializeTotalTicks = 0;
	private static long _deserializeSuccessCount = 0;

	private static long _ringBufferAddCallCount = 0;
	private static long _ringBufferAddTotalTicks = 0;
	private static long _ringBufferTakeCallCount = 0;
	private static long _ringBufferTakeTotalTicks = 0;

	private static long _gatewayTotalMessages = 0;
	private static long _gatewayStartTime = 0;

	private static long _keygenTotalMessages = 0;
	private static long _keygenStartTime = 0;

	private static long _processorTotalMessages = 0;
	private static long _processorStartTime = 0;

	private static long _kafkaConsumerTotalMessages = 0;
	private static long _kafkaConsumerStartTime = 0;

	public TaskFactory(PerformanceMetrics metrics)
	{
		_metrics = metrics;
	}

	public List<Task> CreateAllTasks(TestData testData, string kafkaConnectionString,
		IProducer<string, string>? producer = null, IConsumer<string, string>? consumer = null)
	{
		var tasks = new List<Task>();

		int totalCores = Environment.ProcessorCount - 3;
		int gateways = Math.Max(1, (int)(totalCores * TestConfiguration.GATEWAY_CORES_PERCENTAGE));
		int keygens = Math.Max(1, (int)(totalCores * TestConfiguration.KEYGEN_CORES_PERCENTAGE));
		int processors = Math.Max(1, totalCores - gateways - keygens);

		TestContext.WriteLine($"ℹ️ Thread allocation: Gateways={gateways}, KeyGens={keygens}, Processors={processors}");

		var now = Stopwatch.GetTimestamp();
		_gatewayStartTime = now;
		_keygenStartTime = now;
		_processorStartTime = now;
		_kafkaConsumerStartTime = now;

		tasks.AddRange(CreateGatewayTasks(gateways, testData, kafkaConnectionString, producer));

		if (TestConfiguration.USE_KAFKA_MODE)
		{
			tasks.AddRange(CreateKafkaConsumerWithKeyGenTasks(keygens, testData, kafkaConnectionString));
		}
		else
		{
			tasks.AddRange(CreateKeyGenTasks(keygens, testData, kafkaConnectionString));
		}

		tasks.AddRange(CreateProcessorTasks(processors, testData));

		tasks.Add(CreateProgressReportingTask(testData.CancellationTokenSource));

		return tasks;
	}

	private IEnumerable<Task> CreateGatewayTasks(int gateways, TestData testData,
		string kafkaConnectionString, IProducer<string, string>? producer)
	{
		for (int i = 0; i < gateways; i++)
		{
			var gatewayId = i;
			yield return TestConfiguration.USE_KAFKA_MODE
				? CreateKafkaGatewayTask(gatewayId, gateways, testData, kafkaConnectionString, producer)
				: CreateMemoryGatewayTask(gatewayId, gateways, testData);
		}
	}

	private IEnumerable<Task> CreateKeyGenTasks(int keygens, TestData testData, string kafkaConnectionString)
	{
		for (int i = 0; i < keygens; i++)
		{
			var keyGenId = i;
			yield return TestConfiguration.USE_KAFKA_MODE
				? CreateKafkaKeyGenTask(keyGenId, keygens, testData, kafkaConnectionString)
				: CreateMemoryKeyGenTask(keyGenId, keygens, testData);
		}
	}

	private IEnumerable<Task> CreateKafkaConsumerWithKeyGenTasks(int keygens, TestData testData, string kafkaConnectionString)
	{
		var allPartitions = Enumerable.Range(0, TestConfiguration.TOPIC_PARTITION_COUNT).ToArray();
		var partitionsPerThread = TestConfiguration.TOPIC_PARTITION_COUNT / keygens;
		var extra = TestConfiguration.TOPIC_PARTITION_COUNT % keygens;

		for (int i = 0, start = 0; i < keygens; i++)
		{
			int count = partitionsPerThread + (i < extra ? 1 : 0);
			var assignedPartitions = allPartitions.Skip(start).Take(count).ToArray();
			start += count;
			yield return CreateKafkaConsumerWithKeyGenTask(assignedPartitions, testData, kafkaConnectionString);
		}
	}

	private Task CreateKafkaConsumerWithKeyGenTask(int[] assignedPartitions, TestData testData, string kafkaConnectionString)
	{
		return Task.Run(() =>
		{
			var processingMetricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(processingMetricsCache, "Processing/", testData.CancellationTokenSource.Token);

			var keyGenMetricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(keyGenMetricsCache, "KeyGen/", testData.CancellationTokenSource.Token);

			var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;

			var consumerConfig = KafkaConfigHelper.CreatePerformanceConsumerConfig(
				kafkaConnectionString,
				"backpressure-test-group",
				false);

			using var consumerThread = new ConsumerBuilder<string, string>(consumerConfig)
				.SetErrorHandler((_, e) => TestContext.WriteLine($"❌ Consumer error: {e.Reason}"))
				.Build();

			var topicPartitionList = assignedPartitions
				.Select(p => new TopicPartition("backpressure.keygen.topic", new Partition(p)))
				.ToList();
			consumerThread.Assign(topicPartitionList);

			var pausedMsgs = new Dictionary<int, RingBuffer<(string sender, string recipient, long startTime, long keyId, long timestamp)>>();
			var scratch = new (string sender, string recipient, long startTime, long keyId, long timestamp)[TestConfiguration.MAX_BATCH];

			foreach (var p in assignedPartitions)
			{
				pausedMsgs[p] = new RingBuffer<(string sender, string recipient, long startTime, long keyId, long timestamp)>(
					TestConfiguration.MAX_BATCH,
					static (ref (string sender, string recipient, long startTime, long keyId, long timestamp) item) => item.timestamp = -1,
					static item => item.timestamp != -1);
			}

			var sb = new StringBuilder();
			var consumedCount = 0;
			var kafkaErrorCount = 0;
			var lastSuccessfulConsume = Stopwatch.GetTimestamp();

			TestContext.WriteLine($"ℹ️ Kafka Consumer+KeyGen handling partitions: [{string.Join(", ", assignedPartitions)}]");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					var methodStartTicks = Stopwatch.GetTimestamp();
					Interlocked.Increment(ref _consumerMethodCallCount);

					var consumeStartTicks = Stopwatch.GetTimestamp();
					ConsumeResult<string, string>? consumeResult = null;

					try
					{
						consumeResult = consumerThread.Consume(TimeSpan.FromMilliseconds(100));

						var consumeElapsedTicks = Stopwatch.GetTimestamp() - consumeStartTicks;
						Interlocked.Increment(ref _kafkaConsumeCallCount);
						Interlocked.Add(ref _kafkaConsumeTotalTicks, consumeElapsedTicks);

						if (consumeResult != null)
						{
							Interlocked.Increment(ref _kafkaConsumeSuccessCount);
							lastSuccessfulConsume = Stopwatch.GetTimestamp();
							kafkaErrorCount = 0;

							Interlocked.Increment(ref _kafkaConsumerTotalMessages);

							var deserializeStartTicks = Stopwatch.GetTimestamp();

							if (MessageSerializer.TryDeserialize(
								consumeResult.Message.Value,
								out var sender, out var recipient, out var startTime, out var keyId,
								TestConfiguration.USE_BINARY_SERIALIZATION))
							{
								var deserializeElapsedTicks = Stopwatch.GetTimestamp() - deserializeStartTicks;
								Interlocked.Increment(ref _deserializeCallCount);
								Interlocked.Add(ref _deserializeTotalTicks, deserializeElapsedTicks);
								Interlocked.Increment(ref _deserializeSuccessCount);

								var partitionIndex = consumeResult.Partition.Value;
								var ringBuffer = pausedMsgs[partitionIndex];

								var addStartTicks = Stopwatch.GetTimestamp();
								if (!ringBuffer.TryAddItem((sender, recipient, startTime, keyId, Stopwatch.GetTimestamp())))
								{
									TestContext.WriteLine("❌ Ring buffer full in Consumer+KeyGen");
								}
								var addElapsedTicks = Stopwatch.GetTimestamp() - addStartTicks;
								Interlocked.Increment(ref _ringBufferAddCallCount);
								Interlocked.Add(ref _ringBufferAddTotalTicks, addElapsedTicks);
							}
							else
							{
								var deserializeElapsedTicks = Stopwatch.GetTimestamp() - deserializeStartTicks;
								Interlocked.Increment(ref _deserializeCallCount);
								Interlocked.Add(ref _deserializeTotalTicks, deserializeElapsedTicks);

								TestContext.WriteLine($"❌ Failed to deserialize message from partition {consumeResult.Partition.Value}");
							}
						}
					}
					catch (ConsumeException ex)
					{
						kafkaErrorCount++;
						var consumeElapsedTicks = Stopwatch.GetTimestamp() - consumeStartTicks;
						Interlocked.Increment(ref _kafkaConsumeCallCount);
						Interlocked.Add(ref _kafkaConsumeTotalTicks, consumeElapsedTicks);

						TestContext.WriteLine($"❌ Kafka consume error {kafkaErrorCount}: {ex.Error.Reason}");

						if (kafkaErrorCount >= 10)
						{
							TestContext.WriteLine("❌ Too many Kafka errors, stopping consumer");
							testData.CancellationTokenSource.Cancel();
							break;
						}

						Thread.Sleep(100);
						continue;
					}
					catch (Exception ex)
					{
						kafkaErrorCount++;
						var consumeElapsedTicks = Stopwatch.GetTimestamp() - consumeStartTicks;
						Interlocked.Increment(ref _kafkaConsumeCallCount);
						Interlocked.Add(ref _kafkaConsumeTotalTicks, consumeElapsedTicks);

						TestContext.WriteLine($"❌ Consumer exception {kafkaErrorCount}: {ex.Message}");

						if (kafkaErrorCount >= 10)
						{
							TestContext.WriteLine("❌ Too many consumer exceptions, stopping");
							testData.CancellationTokenSource.Cancel();
							break;
						}

						Thread.Sleep(1000);
						continue;
					}

					var methodElapsedTicks = Stopwatch.GetTimestamp() - methodStartTicks;
					Interlocked.Add(ref _consumerMethodTotalTicks, methodElapsedTicks);

					foreach (var kvp in pausedMsgs)
					{
						var partitionIndex = kvp.Key;
						var ringBuffer = kvp.Value;

						var takeStartTicks = Stopwatch.GetTimestamp();
						if (ringBuffer.TakeWhere(
							item => Stopwatch.GetElapsedTime(item.timestamp).TotalMilliseconds >= TestConfiguration.KEYGEN_LATENCY,
							scratch, out var statesToProcess) == 0)
						{
							var takeElapsedTicks = Stopwatch.GetTimestamp() - takeStartTicks;
							Interlocked.Increment(ref _ringBufferTakeCallCount);
							Interlocked.Add(ref _ringBufferTakeTotalTicks, takeElapsedTicks);
							continue;
						}

						var takeElapsedTicksSuccess = Stopwatch.GetTimestamp() - takeStartTicks;
						Interlocked.Increment(ref _ringBufferTakeCallCount);
						Interlocked.Add(ref _ringBufferTakeTotalTicks, takeElapsedTicksSuccess);

						foreach (var state in statesToProcess)
						{
							var dataProcessingStartTicks = Stopwatch.GetTimestamp();

							var processorMetricPath = $"Processing/{state.sender}/{state.recipient}";
							var keyGenMetricPath = $"KeyGen/{state.sender}/{state.recipient}";

							if (processingMetricsCache.TryGetValue(processorMetricPath, out long queueLength)
								&& queueLength >= TestConfiguration.MAX_QUEUE)
							{
								_metrics.IncrementBackPressure();

								if (!ringBuffer.TryAddItem(state))
								{
									TestContext.WriteLine("❌ Could not re-add item to ring buffer for backpressure");
								}
								continue;
							}

							var senderUtf8 = new Utf8Name(state.sender);
							var recipientUtf8 = new Utf8Name(state.recipient);

							var processorPartitionIndex = Math.Abs((state.sender.GetHashCode() * 397) ^ state.recipient.GetHashCode()) % TestConfiguration.PROCESSOR_PARTITION_COUNT;
							var proposedWriterIndex = Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 2]) % maxProcessorPartitionLength;

							testData.ProcessorPartitions[processorPartitionIndex, proposedWriterIndex] = new DummyMessage
							{
								Sender = senderUtf8,
								Recipient = recipientUtf8,
								StartTime = state.startTime,
								KeyId = state.keyId
							};

							while (Volatile.Read(ref testData.ProcessorIndices[processorPartitionIndex, 1]) < (proposedWriterIndex - 1))
							{
							}
							Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 1]);

							var metricsStartTicks = Stopwatch.GetTimestamp();

							PooledMetricsService.UpdateMetric(processorMetricPath, 1);
							processingMetricsCache.AddOrUpdate(processorMetricPath, 1, (key, existing) => existing + 1);

							PooledMetricsService.UpdateMetric(keyGenMetricPath, -1);
							keyGenMetricsCache.AddOrUpdate(keyGenMetricPath, 0, (key, existing) => Math.Max(0, existing - 1));

							var metricsElapsedTicks = Stopwatch.GetTimestamp() - metricsStartTicks;
							Interlocked.Increment(ref _metricsCallCount);
							Interlocked.Add(ref _metricsTotalTicks, metricsElapsedTicks);

							var dataProcessingElapsedTicks = Stopwatch.GetTimestamp() - dataProcessingStartTicks;
							Interlocked.Increment(ref _dataProcessingCallCount);
							Interlocked.Add(ref _dataProcessingTotalTicks, dataProcessingElapsedTicks);

							Interlocked.Increment(ref _keygenTotalMessages);

							consumedCount++;
						}
					}

					if (_metrics.MessagesOut >= TestConfiguration.TARGET_MESSAGES)
					{
						testData.CancellationTokenSource.Cancel();
						break;
					}
				}
			}
			catch (OperationCanceledException)
			{
				TestContext.WriteLine("ℹ️ Consumer+KeyGen cancelled");
			}
			catch (Exception ex)
			{
				TestContext.WriteLine($"❌ Consumer+KeyGen fatal error: {ex.Message}");
				testData.CancellationTokenSource.Cancel();
			}

			TestContext.WriteLine($"ℹ️ Kafka Consumer+KeyGen completed - processed {consumedCount} messages from partitions [{string.Join(", ", assignedPartitions)}], errors: {kafkaErrorCount}");
		}, testData.CancellationTokenSource.Token);
	}

	private IEnumerable<Task> CreateProcessorTasks(int processorCount, TestData testData)
	{
		if (TestConfiguration.USE_KAFKA_MODE)
		{
			yield return CreateKafkaFinalProcessorTask(testData);
		}
		else
		{
			for (int i = 0; i < processorCount; i++)
			{
				var processorId = i;
				yield return CreateMemoryProcessorTask(processorId, processorCount, testData);
			}
		}
	}

	private Task CreateKafkaGatewayTask(int gatewayId, int gateways, TestData testData,
		string kafkaConnectionString, IProducer<string, string>? producer)
	{
		return Task.Run(async () =>
		{
			var gatewayMetricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(gatewayMetricsCache, "KeyGen/", testData.CancellationTokenSource.Token);

			var senderStart = gatewayId * TestConfiguration.CUSTOMERS / gateways;
			var senderEnd = (gatewayId + 1) * TestConfiguration.CUSTOMERS / gateways;
			var messageCount = 0;
			var backPressureCount = 0;
			var sb = new StringBuilder();

			TestContext.WriteLine($"ℹ️ Kafka Gateway {gatewayId} handling customers {senderStart} to {senderEnd}");

			IProducer<string, string> producerToUse;
			if (producer != null)
			{
				producerToUse = producer;
			}
			else
			{
				var producerConfig = KafkaConfigHelper.CreatePerformanceProducerConfig(kafkaConnectionString);
				producerToUse = new ProducerBuilder<string, string>(producerConfig).Build();
			}

			var batchProcessor = new BatchMessageProcessor(
				producerToUse,
				TestConfiguration.BATCH_SIZE,
				TestConfiguration.USE_BATCH_PROCESSING,
				TestConfiguration.ELIMINATE_SYNC_WAITS);

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					for (int senderIndex = senderStart; senderIndex < senderEnd && !testData.CancellationTokenSource.Token.IsCancellationRequested; senderIndex++)
					{
						var senderId = testData.CustomerIds[senderIndex];

						for (int recipientIndex = 0; recipientIndex < TestConfiguration.CUSTOMERS && !testData.CancellationTokenSource.Token.IsCancellationRequested; recipientIndex++)
						{
							var recipientId = testData.CustomerIds[recipientIndex];

							var metricsKey = TestUtils.BuildMetricsKey(sb, "KeyGen/", senderId, recipientId);

							if (gatewayMetricsCache.TryGetValue(metricsKey, out long queueLength) && queueLength >= TestConfiguration.MAX_QUEUE)
							{
								backPressureCount++;
								_metrics.IncrementBackPressure();
								continue;
							}

							var partitionIndex = TestUtils.CalculatePartitionIndex(senderIndex, recipientIndex, TestConfiguration.TOPIC_PARTITION_COUNT);
							var keyId = senderIndex + recipientIndex;

							var message = new DummyMessage
							{
								Sender = senderId,
								Recipient = recipientId,
								StartTime = Stopwatch.GetTimestamp(),
								KeyId = keyId
							};

							var messageValue = MessageSerializer.Serialize(
								senderId, recipientId, message.StartTime, keyId,
								"Normal", partitionIndex, TestConfiguration.USE_BINARY_SERIALIZATION);

							var kafkaMsg = new Message<string, string>
							{
								Key = $"{senderIndex}-{recipientIndex}",
								Value = messageValue
							};

							var batchStartTicks = Stopwatch.GetTimestamp();
							await batchProcessor.AddMessageAsync(kafkaMsg);
							var batchElapsedTicks = Stopwatch.GetTimestamp() - batchStartTicks;
							Interlocked.Increment(ref _batchProcessorCallCount);
							Interlocked.Add(ref _batchProcessorTotalTicks, batchElapsedTicks);

							PooledMetricsService.UpdateMetric(metricsKey, 1);
							gatewayMetricsCache.AddOrUpdate(metricsKey, 1, (key, existing) => existing + 1);
							_metrics.IncrementIn();
							messageCount++;

							Interlocked.Increment(ref _gatewayTotalMessages);

							if (_metrics.MessagesIn >= TestConfiguration.TARGET_MESSAGES)
							{
								testData.CancellationTokenSource.Cancel();
								break;
							}
						}
					}
				}

				await batchProcessor.FlushAllAsync();
			}
			catch (OperationCanceledException)
			{
				await batchProcessor.FlushAllAsync();
			}
			finally
			{
				if (producer == null && producerToUse != null)
				{
					producerToUse.Dispose();
				}
			}

			TestContext.WriteLine($"ℹ️ Kafka Gateway {gatewayId} produced {messageCount} messages (back pressure: {backPressureCount})");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateMemoryGatewayTask(int gatewayId, int gateways, TestData testData)
	{
		return Task.Run(() =>
		{
			var metricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(metricsCache, "KeyGen/", testData.CancellationTokenSource.Token);

			var maxKeyGenPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var senderStart = gatewayId * TestConfiguration.CUSTOMERS / gateways;
			var senderEnd = (gatewayId + 1) * TestConfiguration.CUSTOMERS / gateways;
			var sb = new StringBuilder();

			TestContext.WriteLine($"ℹ️ Memory Gateway {gatewayId} handling customers {senderStart} to {senderEnd}");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					for (int senderIndex = senderStart; senderIndex < senderEnd; senderIndex++)
					{
						var senderId = testData.CustomerIds[senderIndex];
						for (int recipientIndex = 0; recipientIndex < TestConfiguration.CUSTOMERS; recipientIndex++)
						{
							var recipientId = testData.CustomerIds[recipientIndex];

							var metricsKey = TestUtils.BuildMetricsKey(sb, "KeyGen/", senderId, recipientId);

							if (ProduceValueMemoryMode(metricsCache, metricsKey))
							{
								var partitionIndex = TestUtils.CalculatePartitionIndex(senderIndex, recipientIndex, TestConfiguration.TOPIC_PARTITION_COUNT);
								var proposedWriterIndex = Interlocked.Increment(ref testData.KeyGenIndices[partitionIndex, 2]) % maxKeyGenPartitionLength;

								testData.KeyGenPartitions[partitionIndex, proposedWriterIndex] = new DummyMessage
								{
									Sender = senderId,
									Recipient = recipientId,
									StartTime = Stopwatch.GetTimestamp(),
									KeyId = senderIndex + recipientIndex
								};

								while (Volatile.Read(ref testData.KeyGenIndices[partitionIndex, 1]) < (proposedWriterIndex - 1))
								{
								}

								Interlocked.Increment(ref testData.KeyGenIndices[partitionIndex, 1]);
								_metrics.IncrementIn();

								Interlocked.Increment(ref _gatewayTotalMessages);
							}
							else
							{
								_metrics.IncrementBackPressure();
							}

							if (_metrics.MessagesIn >= TestConfiguration.TARGET_MESSAGES)
							{
								testData.CancellationTokenSource.Cancel();
								return;
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}

			TestContext.WriteLine($"ℹ️ Memory Gateway {gatewayId} completed");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateKafkaKeyGenTask(int keyGenId, int keygens, TestData testData, string kafkaConnectionString)
	{
		return Task.Run(() =>
		{
			var keyGenMetricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(keyGenMetricsCache, "Processing/", testData.CancellationTokenSource.Token);

			var maxKeyGenPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var partitionsStart = keyGenId * TestConfiguration.TOPIC_PARTITION_COUNT / keygens;
			var partitionsEnd = (keyGenId + 1) * TestConfiguration.TOPIC_PARTITION_COUNT / keygens;

			var pausedMsgs = new Dictionary<int, RingBuffer<(long readerIndex, long timestamp)>>();
			var scratch = new (long readerIndex, long timestamp)[TestConfiguration.MAX_BATCH];

			for (int j = partitionsStart; j < partitionsEnd; j++)
			{
				pausedMsgs.Add(j, new RingBuffer<(long readerIndex, long timestamp)>(
					TestConfiguration.MAX_BATCH,
					static (ref (long readerIndex, long timestamp) item) => item.timestamp = -1,
					static item => item.timestamp != -1));
			}

			var sb = new StringBuilder();
			var messageCount = 0;

			TestContext.WriteLine($"ℹ️ Kafka KeyGen {keyGenId} handling partitions {partitionsStart} to {partitionsEnd}");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					for (int partitionIndex = partitionsStart; partitionIndex < partitionsEnd; partitionIndex++)
					{
						var ringBuffer = pausedMsgs[partitionIndex];
						var batchCounter = 0;

						while (Volatile.Read(ref testData.KeyGenIndices[partitionIndex, 0]) != Volatile.Read(ref testData.KeyGenIndices[partitionIndex, 1])
							&& batchCounter < TestConfiguration.MAX_BATCH && !ringBuffer.IsFull)
						{
							var newReaderIndex = Interlocked.Increment(ref testData.KeyGenIndices[partitionIndex, 0]) % maxKeyGenPartitionLength;
							if (!ringBuffer.TryAddItem((newReaderIndex, Stopwatch.GetTimestamp())))
							{
								TestContext.WriteLine("❌ Ring buffer full in Kafka KeyGen");
								continue;
							}
							batchCounter++;
						}

						if (ringBuffer.TakeWhere(item => Stopwatch.GetElapsedTime(item.timestamp).TotalMilliseconds >= TestConfiguration.KEYGEN_LATENCY,
							scratch, out var statesToProcess) == 0)
						{
							continue;
						}

						foreach (var state in statesToProcess)
						{
							ref var message = ref testData.KeyGenPartitions[partitionIndex, state.readerIndex];
							var sender = TestUtils.GetUtf8String(message.Sender);
							var recipient = TestUtils.GetUtf8String(message.Recipient);

							var processorMetricPath = TestUtils.BuildMetricsKey(sb, "Processing/", message.Sender, message.Recipient);

							if (ProduceValueKafkaMode(keyGenMetricsCache, processorMetricPath, testData.MetricsService))
							{
								var keyGenMetricPath = TestUtils.BuildMetricsKey(sb, "KeyGen/", message.Sender, message.Recipient);
								PooledMetricsService.UpdateMetric(keyGenMetricPath, -1);

								var processorPartitionIndex = Math.Abs((sender.GetHashCode() * 397) ^ recipient.GetHashCode()) % TestConfiguration.PROCESSOR_PARTITION_COUNT;
								var proposedWriterIndex = Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 2]) % maxProcessorPartitionLength;
								testData.ProcessorPartitions[processorPartitionIndex, proposedWriterIndex] = message;

								while (Volatile.Read(ref testData.ProcessorIndices[processorPartitionIndex, 1]) < (proposedWriterIndex - 1))
								{
								}
								Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 1]);
								messageCount++;
							}
							else
							{
								if (!ringBuffer.TryAddItem(state))
								{
									TestContext.WriteLine("❌ Could not re-add item to ring buffer in Kafka KeyGen");
								}
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}

			TestContext.WriteLine($"ℹ️ Kafka KeyGen {keyGenId} processed {messageCount} messages");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateMemoryKeyGenTask(int keyGenId, int keygens, TestData testData)
	{
		return Task.Run(() =>
		{
			var metricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(metricsCache, "Processing/", testData.CancellationTokenSource.Token);

			var maxKeyGenPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var partitionsStart = keyGenId * TestConfiguration.TOPIC_PARTITION_COUNT / keygens;
			var partitionsEnd = (keyGenId + 1) * TestConfiguration.TOPIC_PARTITION_COUNT / keygens;

			var pausedMsgs = new Dictionary<int, RingBuffer<(long readerIndex, long timestamp)>>();
			var scratch = new (long readerIndex, long timestamp)[TestConfiguration.MAX_BATCH];

			for (int j = partitionsStart; j < partitionsEnd; j++)
			{
				pausedMsgs.Add(j, new RingBuffer<(long readerIndex, long timestamp)>(
					TestConfiguration.MAX_BATCH,
					static (ref (long readerIndex, long timestamp) item) => item.timestamp = -1,
					static item => item.timestamp != -1));
			}

			var sb = new StringBuilder();
			TestContext.WriteLine($"ℹ️ Memory KeyGen {keyGenId} handling partitions {partitionsStart} to {partitionsEnd}");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					for (int partitionIndex = partitionsStart; partitionIndex < partitionsEnd; partitionIndex++)
					{
						var ringBuffer = pausedMsgs[partitionIndex];
						var batchCounter = 0;

						while (Volatile.Read(ref testData.KeyGenIndices[partitionIndex, 0]) != Volatile.Read(ref testData.KeyGenIndices[partitionIndex, 1])
							&& batchCounter < TestConfiguration.MAX_BATCH && !ringBuffer.IsFull)
						{
							var newReaderIndex = Interlocked.Increment(ref testData.KeyGenIndices[partitionIndex, 0]) % maxKeyGenPartitionLength;
							if (!ringBuffer.TryAddItem((newReaderIndex, Stopwatch.GetTimestamp())))
							{
								TestContext.WriteLine("❌ Ring buffer full in Memory KeyGen");
								continue;
							}
							batchCounter++;
						}

						if (ringBuffer.TakeWhere(item => Stopwatch.GetElapsedTime(item.timestamp).TotalMilliseconds >= TestConfiguration.KEYGEN_LATENCY,
							scratch, out var statesToProcess) == 0)
						{
							continue;
						}

						foreach (var state in statesToProcess)
						{
							var message = testData.KeyGenPartitions[partitionIndex, state.readerIndex];
							var sender = TestUtils.GetUtf8String(message.Sender);
							var recipient = TestUtils.GetUtf8String(message.Recipient);

							var processorMetricPath = TestUtils.BuildMetricsKey(sb, "Processing/", message.Sender, message.Recipient);

							if (ProduceValueMemoryMode(metricsCache, processorMetricPath))
							{
								ConsumeValueMemoryMode(metricsCache, TestUtils.BuildMetricsKey(sb, "KeyGen/", message.Sender, message.Recipient));

								var processorPartitionIndex = Math.Abs((sender.GetHashCode() * 397) ^ recipient.GetHashCode()) % TestConfiguration.PROCESSOR_PARTITION_COUNT;
								var proposedWriterIndex = Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 2]) % maxProcessorPartitionLength;
								testData.ProcessorPartitions[processorPartitionIndex, proposedWriterIndex] = message;

								while (Volatile.Read(ref testData.ProcessorIndices[processorPartitionIndex, 1]) < (proposedWriterIndex - 1))
								{
								}
								Interlocked.Increment(ref testData.ProcessorIndices[processorPartitionIndex, 1]);

								Interlocked.Increment(ref _keygenTotalMessages);
							}
							else
							{
								if (!ringBuffer.TryAddItem(state))
								{
									TestContext.WriteLine("❌ Could not re-add item to ring buffer in Memory KeyGen");
								}
							}
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}

			TestContext.WriteLine($"ℹ️ Memory KeyGen {keyGenId} completed");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateKafkaFinalProcessorTask(TestData testData)
	{
		return Task.Run(() =>
		{
			var processed = 0;
			var sw = Stopwatch.StartNew();
			var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
			var sb = new StringBuilder();
			TestContext.WriteLine($"ℹ️ Kafka Final Processor handling {TestConfiguration.PROCESSOR_PARTITION_COUNT} partitions");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					bool anyProcessed = false;

					for (int partitionIndex = 0; partitionIndex < TestConfiguration.PROCESSOR_PARTITION_COUNT; partitionIndex++)
					{
						long readIndex = testData.ProcessorIndices[partitionIndex, 0];
						long writeIndex = testData.ProcessorIndices[partitionIndex, 1];

						while (readIndex < writeIndex)
						{
							var msg = testData.ProcessorPartitions[partitionIndex, readIndex % maxProcessorPartitionLength];

							var processingLatency = Stopwatch.GetElapsedTime(msg.StartTime).TotalMilliseconds;
							_metrics.IncrementOut(processingLatency);

							var processingMetricKey = TestUtils.BuildMetricsKey(sb, "Processing/", msg.Sender, msg.Recipient);
							PooledMetricsService.UpdateMetric(processingMetricKey, -1);

							processed++;

							Interlocked.Increment(ref _processorTotalMessages);

							readIndex = Interlocked.Increment(ref testData.ProcessorIndices[partitionIndex, 0]);
							anyProcessed = true;

							if (_metrics.MessagesOut >= TestConfiguration.TARGET_MESSAGES)
							{
								testData.CancellationTokenSource.Cancel();
								TestContext.WriteLine($"ℹ️ Target message count reached: {TestConfiguration.TARGET_MESSAGES}");
								break;
							}
						}

						if (testData.CancellationTokenSource.Token.IsCancellationRequested)
							break;
					}

					if (!anyProcessed)
					{
					}
				}
			}
			catch (OperationCanceledException)
			{
			}

			sw.Stop();
			TestContext.WriteLine($"ℹ️ Kafka Final Processor completed - processed {processed} messages in {sw.Elapsed.TotalSeconds:F2}s");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateMemoryProcessorTask(int processorId, int processorCount, TestData testData)
	{
		return Task.Run(() =>
		{
			var metricsCache = new ConcurrentDictionary<string, long>();
			_ = MetricsCacheUpdater.RunAsync(metricsCache, "Processing/", testData.CancellationTokenSource.Token);

			var partitionsStart = processorId * TestConfiguration.PROCESSOR_PARTITION_COUNT / processorCount;
			var partitionsEnd = (processorId + 1) * TestConfiguration.PROCESSOR_PARTITION_COUNT / processorCount;
			var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;

			var processRnd = new Random();
			var pausedMsgs = new Dictionary<int, RingBuffer<(long keyid, long readerIndex, long timestamp, long expire)>>();
			var scratch = new (long keyid, long readerIndex, long timestamp, long expire)[TestConfiguration.MAX_BATCH];

			for (int j = partitionsStart; j < partitionsEnd; j++)
			{
				pausedMsgs.Add(j, new RingBuffer<(long keyid, long readerIndex, long timestamp, long expire)>(
					TestConfiguration.MAX_BATCH,
					static (ref (long keyid, long readerIndex, long timestamp, long expire) item) => item.readerIndex = -1,
					static item => item.readerIndex != -1));
			}

			var sb = new StringBuilder();
			var seen = new HashSet<long>();
			var processedCount = 0;

			TestContext.WriteLine($"ℹ️ Memory Processor {processorId} handling partitions {partitionsStart} to {partitionsEnd}");

			try
			{
				while (!testData.CancellationTokenSource.Token.IsCancellationRequested)
				{
					for (int partitionIndex = partitionsStart; partitionIndex < partitionsEnd; partitionIndex++)
					{
						var ringBuffer = pausedMsgs[partitionIndex];
						seen.Clear();
						var batchCounter = 0;

						while (Volatile.Read(ref testData.ProcessorIndices[partitionIndex, 0]) != Volatile.Read(ref testData.ProcessorIndices[partitionIndex, 1])
							&& batchCounter < TestConfiguration.MAX_BATCH && !ringBuffer.IsFull)
						{
							var lag = 0;
							if (processRnd.Next(1, 1000) == 999)
							{
								lag = TestConfiguration.PROCESSING_TIMEOUT;
							}

							var newReaderIndex = Interlocked.Increment(ref testData.ProcessorIndices[partitionIndex, 0]) % maxProcessorPartitionLength;
							var msg = testData.ProcessorPartitions[partitionIndex, newReaderIndex];

							if (!ringBuffer.TryAddItem((msg.KeyId, newReaderIndex, Stopwatch.GetTimestamp(), TestConfiguration.PROCESSING_LATENCY + lag)))
							{
								TestContext.WriteLine("❌ Ring buffer full in Memory Processor");
								continue;
							}
							batchCounter++;
						}

						if (ringBuffer.TakeWhere(
							item => seen.Add(item.keyid) && Stopwatch.GetElapsedTime(item.timestamp).TotalMilliseconds >= item.expire,
							scratch,
							out var statesToProcess) == 0)
						{
							continue;
						}

						foreach (var state in statesToProcess)
						{
							var message = testData.ProcessorPartitions[partitionIndex, state.readerIndex];

							ConsumeValueMemoryMode(metricsCache, TestUtils.BuildMetricsKey(sb, "Processing/", message.Sender, message.Recipient));

							var latency = Stopwatch.GetElapsedTime(message.StartTime).TotalMilliseconds;
							_metrics.IncrementOut(latency);
							processedCount++;

							Interlocked.Increment(ref _processorTotalMessages);
						}
					}
				}
			}
			catch (OperationCanceledException)
			{
			}

			TestContext.WriteLine($"ℹ️ Memory Processor {processorId} completed - processed {processedCount} messages");
		}, testData.CancellationTokenSource.Token);
	}

	private Task CreateProgressReportingTask(CancellationTokenSource cancellationTokenSource)
	{
		return Task.Run(async () =>
		{
			var lastOut = 0L;
			var reportCount = 0;

			try
			{
				while (!cancellationTokenSource.Token.IsCancellationRequested)
				{
					await Task.Delay(1000, cancellationTokenSource.Token);
					reportCount++;

					var currentIn = _metrics.MessagesIn;
					var currentOut = _metrics.MessagesOut;
					var currentBackPressure = _metrics.BackPressureEvents;
					var throughput = currentOut - lastOut;
					var lag = currentIn - currentOut;
					var avgLatency = _metrics.GetAverageLatency();
					var maxLatency = _metrics.MaxMessageLatency;

					if (TestConfiguration.USE_KAFKA_MODE)
					{
						if (reportCount % 10 == 0)
						{
							TestContext.WriteLine($"📊 Progress: In={currentIn:N0}, Out={currentOut:N0}, " +
											  $"Throughput={throughput:F0}/sec, Lag={lag:N0}");

							if (reportCount % 30 == 0)
							{
								var backPressureRate = _metrics.GetBackPressureRate();
								TestContext.WriteLine($"📊 Back Pressure: {currentBackPressure:N0} events ({backPressureRate:F1}%), " +
												  $"Avg Latency={avgLatency:F1}ms");
							}
						}
					}
					else
					{
						TestContext.WriteLine($"📊 In: {currentIn} Out: {currentOut} Throughput: {throughput}/sec Lag: {lag} Max Latency: {maxLatency:F2} Avg: {avgLatency:F2}");

						if (reportCount % 5 == 0)
						{
							var backPressureRate = _metrics.GetBackPressureRate();
							TestContext.WriteLine($"📊 Progress: In={currentIn:N0}, Out={currentOut:N0}, " +
											  $"Throughput={throughput:F1}/sec, Avg Latency={avgLatency:F2}ms");
							TestContext.WriteLine($"📊 Back Pressure: {currentBackPressure:N0} events ({backPressureRate:F1}%), " +
											  $"Max Latency={maxLatency:F2}ms");
						}
					}
					if (reportCount % 60 == 0)
					{
						TestContext.WriteLine($"🕐 Heartbeat: Test running for {reportCount / 60:F1} minutes");

						if (currentOut == lastOut && currentOut < 100000)
						{
							TestContext.WriteLine($"❌ No progress detected - In: {currentIn}, Out: {currentOut}");
						}
					}
					lastOut = currentOut;
				}
			}
			catch (OperationCanceledException)
			{
				TestContext.WriteLine("ℹ️ Progress reporting cancelled");
			}
		}, cancellationTokenSource.Token);
	}

	private static bool ProduceValueMemoryMode(
		ConcurrentDictionary<string, long> metricsCache,
		string metricsKey)
	{
		if (metricsCache.TryGetValue(metricsKey, out long queueLength))
		{
			if (queueLength >= TestConfiguration.MAX_QUEUE)
			{
				return false;
			}
		}
		PooledMetricsService.UpdateMetric(metricsKey, 1);
		metricsCache.AddOrUpdate(metricsKey, 1, (key, existing) => existing + 1);
		return true;
	}

	private static void ConsumeValueMemoryMode(
		ConcurrentDictionary<string, long> metricsCache,
		string metricsKey)
	{
		PooledMetricsService.UpdateMetric(metricsKey, -1);
		metricsCache.AddOrUpdate(metricsKey, 0, (key, existing) => Math.Max(0, existing - 1));
	}

	private static bool ProduceValueKafkaMode(ConcurrentDictionary<string, long> metricsCache, string metricsKey, PooledMetricsService metricsService)
	{
		var startTicks = Stopwatch.GetTimestamp();

		if (metricsCache.TryGetValue(metricsKey, out long queueLength) && queueLength >= TestConfiguration.MAX_QUEUE)
		{
			var elapsed = Stopwatch.GetTimestamp() - startTicks;
			Interlocked.Increment(ref _kafkaProduceCallCount);
			Interlocked.Add(ref _kafkaProduceTotalTicks, elapsed);
			return false;
		}

		PooledMetricsService.UpdateMetric(metricsKey, 1);
		metricsCache.AddOrUpdate(metricsKey, 1, (key, existing) => existing + 1);

		var elapsedTicks = Stopwatch.GetTimestamp() - startTicks;
		Interlocked.Increment(ref _kafkaProduceCallCount);
		Interlocked.Add(ref _kafkaProduceTotalTicks, elapsedTicks);

		return true;
	}

	public static (
		double avgKafkaProduceMs, long kafkaProduceCount,
		double avgBatchProcessorMs, long batchProcessorCount,
		double avgKafkaConsumeMs, long kafkaConsumeCount, long kafkaConsumeSuccessCount, double kafkaConsumeSuccessRate,
		double avgConsumerMethodMs, long consumerMethodCount,
		double avgDataProcessingMs, long dataProcessingCount,
		double avgMetricsUpdateMs, long metricsUpdateCount,
		double avgDeserializeMs, long deserializeCount, long deserializeSuccessCount, double deserializeSuccessRate,
		double avgRingBufferAddMs, long ringBufferAddCount,
		double avgRingBufferTakeMs, long ringBufferTakeCount
	) GetKafkaTimingStats()
	{
		var produceCount = Interlocked.Read(ref _kafkaProduceCallCount);
		var produceTicks = Interlocked.Read(ref _kafkaProduceTotalTicks);
		var batchProcessorCount = Interlocked.Read(ref _batchProcessorCallCount);
		var batchProcessorTicks = Interlocked.Read(ref _batchProcessorTotalTicks);
		var consumeCount = Interlocked.Read(ref _kafkaConsumeCallCount);
		var consumeTicks = Interlocked.Read(ref _kafkaConsumeTotalTicks);
		var consumeSuccessCount = Interlocked.Read(ref _kafkaConsumeSuccessCount);
		var consumerMethodCount = Interlocked.Read(ref _consumerMethodCallCount);
		var consumerMethodTicks = Interlocked.Read(ref _consumerMethodTotalTicks);

		var dataProcessingCount = Interlocked.Read(ref _dataProcessingCallCount);
		var dataProcessingTicks = Interlocked.Read(ref _dataProcessingTotalTicks);
		var metricsUpdateCount = Interlocked.Read(ref _metricsCallCount);
		var metricsUpdateTicks = Interlocked.Read(ref _metricsTotalTicks);

		var deserializeCount = Interlocked.Read(ref _deserializeCallCount);
		var deserializeTicks = Interlocked.Read(ref _deserializeTotalTicks);
		var deserializeSuccessCount = Interlocked.Read(ref _deserializeSuccessCount);
		var ringBufferAddCount = Interlocked.Read(ref _ringBufferAddCallCount);
		var ringBufferAddTicks = Interlocked.Read(ref _ringBufferAddTotalTicks);
		var ringBufferTakeCount = Interlocked.Read(ref _ringBufferTakeCallCount);
		var ringBufferTakeTicks = Interlocked.Read(ref _ringBufferTakeTotalTicks);

		var avgProduceMs = produceCount > 0 ? (double)produceTicks / produceCount / TimeSpan.TicksPerMillisecond : 0;
		var avgBatchProcessorMs = batchProcessorCount > 0 ? (double)batchProcessorTicks / batchProcessorCount / TimeSpan.TicksPerMillisecond : 0;
		var avgConsumeMs = consumeCount > 0 ? (double)consumeTicks / consumeCount / TimeSpan.TicksPerMillisecond : 0;
		var avgConsumerMethodMs = consumerMethodCount > 0 ? (double)consumerMethodTicks / consumerMethodCount / TimeSpan.TicksPerMillisecond : 0;

		var avgDataProcessingMs = dataProcessingCount > 0 ? (double)dataProcessingTicks / dataProcessingCount / TimeSpan.TicksPerMillisecond : 0;
		var avgMetricsUpdateMs = metricsUpdateCount > 0 ? (double)metricsUpdateTicks / metricsUpdateCount / TimeSpan.TicksPerMillisecond : 0;

		var avgDeserializeMs = deserializeCount > 0 ? (double)deserializeTicks / deserializeCount / TimeSpan.TicksPerMillisecond : 0;
		var avgRingBufferAddMs = ringBufferAddCount > 0 ? (double)ringBufferAddTicks / ringBufferAddCount / TimeSpan.TicksPerMillisecond : 0;
		var avgRingBufferTakeMs = ringBufferTakeCount > 0 ? (double)ringBufferTakeTicks / ringBufferTakeCount / TimeSpan.TicksPerMillisecond : 0;

		var consumeSuccessRate = consumeCount > 0 ? (double)consumeSuccessCount / consumeCount * 100 : 0;
		var deserializeSuccessRate = deserializeCount > 0 ? (double)deserializeSuccessCount / deserializeCount * 100 : 0;

		return (
			avgProduceMs, produceCount,
			avgBatchProcessorMs, batchProcessorCount,
			avgConsumeMs, consumeCount, consumeSuccessCount, consumeSuccessRate,
			avgConsumerMethodMs, consumerMethodCount,
			avgDataProcessingMs, dataProcessingCount,
			avgMetricsUpdateMs, metricsUpdateCount,
			avgDeserializeMs, deserializeCount, deserializeSuccessCount, deserializeSuccessRate,
			avgRingBufferAddMs, ringBufferAddCount,
			avgRingBufferTakeMs, ringBufferTakeCount
		);
	}

	public static (double gatewayTps, double keygenTps, double processorTps, double kafkaConsumerTps) GetStageTpsStats()
	{
		var currentTime = Stopwatch.GetTimestamp();

		var gatewayMessages = Interlocked.Read(ref _gatewayTotalMessages);
		var gatewayElapsed = Stopwatch.GetElapsedTime(_gatewayStartTime, currentTime).TotalSeconds;
		var gatewayTps = gatewayElapsed > 0 ? gatewayMessages / gatewayElapsed : 0;

		var keygenMessages = Interlocked.Read(ref _keygenTotalMessages);
		var keygenElapsed = Stopwatch.GetElapsedTime(_keygenStartTime, currentTime).TotalSeconds;
		var keygenTps = keygenElapsed > 0 ? keygenMessages / keygenElapsed : 0;

		var processorMessages = Interlocked.Read(ref _processorTotalMessages);
		var processorElapsed = Stopwatch.GetElapsedTime(_processorStartTime, currentTime).TotalSeconds;
		var processorTps = processorElapsed > 0 ? processorMessages / processorElapsed : 0;

		var kafkaConsumerMessages = Interlocked.Read(ref _kafkaConsumerTotalMessages);
		var kafkaConsumerElapsed = Stopwatch.GetElapsedTime(_kafkaConsumerStartTime, currentTime).TotalSeconds;
		var kafkaConsumerTps = kafkaConsumerElapsed > 0 ? kafkaConsumerMessages / kafkaConsumerElapsed : 0;

		return (gatewayTps, keygenTps, processorTps, kafkaConsumerTps);
	}

	public static void ResetStageTpsStats()
	{
		var now = Stopwatch.GetTimestamp();

		Interlocked.Exchange(ref _gatewayTotalMessages, 0);
		Interlocked.Exchange(ref _gatewayStartTime, now);

		Interlocked.Exchange(ref _keygenTotalMessages, 0);
		Interlocked.Exchange(ref _keygenStartTime, now);

		Interlocked.Exchange(ref _processorTotalMessages, 0);
		Interlocked.Exchange(ref _processorStartTime, now);

		Interlocked.Exchange(ref _kafkaConsumerTotalMessages, 0);
		Interlocked.Exchange(ref _kafkaConsumerStartTime, now);
	}

	public static void ResetKafkaTimingStats()
	{
		Interlocked.Exchange(ref _kafkaProduceCallCount, 0);
		Interlocked.Exchange(ref _kafkaProduceTotalTicks, 0);
		Interlocked.Exchange(ref _batchProcessorCallCount, 0);
		Interlocked.Exchange(ref _batchProcessorTotalTicks, 0);
		Interlocked.Exchange(ref _kafkaConsumeCallCount, 0);
		Interlocked.Exchange(ref _kafkaConsumeTotalTicks, 0);
		Interlocked.Exchange(ref _kafkaConsumeSuccessCount, 0);
		Interlocked.Exchange(ref _consumerMethodCallCount, 0);
		Interlocked.Exchange(ref _consumerMethodTotalTicks, 0);
		Interlocked.Exchange(ref _dataProcessingCallCount, 0);
		Interlocked.Exchange(ref _dataProcessingTotalTicks, 0);
		Interlocked.Exchange(ref _metricsCallCount, 0);
		Interlocked.Exchange(ref _metricsTotalTicks, 0);
		Interlocked.Exchange(ref _deserializeCallCount, 0);
		Interlocked.Exchange(ref _deserializeTotalTicks, 0);
		Interlocked.Exchange(ref _deserializeSuccessCount, 0);
		Interlocked.Exchange(ref _ringBufferAddCallCount, 0);
		Interlocked.Exchange(ref _ringBufferAddTotalTicks, 0);
		Interlocked.Exchange(ref _ringBufferTakeCallCount, 0);
		Interlocked.Exchange(ref _ringBufferTakeTotalTicks, 0);

		ResetStageTpsStats();
	}
}

public class TestDataManager
{
	public TestData PrepareTestData()
	{
		TestContext.WriteLine("ℹ️ Preparing test data...");

		var testData = new TestData();

		testData.CustomerIds = GenerateCustomerIds();

		InitializePartitions(testData);

		TestContext.WriteLine($"✅ Test data prepared: {TestConfiguration.CUSTOMERS} customers");
		return testData;
	}

	private Utf8Name[] GenerateCustomerIds()
	{
		var customerIds = new Utf8Name[TestConfiguration.CUSTOMERS];
		for (int i = 0; i < TestConfiguration.CUSTOMERS; i++)
		{
			customerIds[i] = new Utf8Name(Guid.NewGuid().ToString());
		}
		return customerIds;
	}

	private void InitializePartitions(TestData testData)
	{
		var maxKeyGenPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;
		var maxProcessorPartitionLength = TestConfiguration.CUSTOMERS * 10 + 100;

		testData.KeyGenPartitions = new DummyMessage[TestConfiguration.TOPIC_PARTITION_COUNT, maxKeyGenPartitionLength];
		testData.KeyGenIndices = new long[TestConfiguration.TOPIC_PARTITION_COUNT, 3];
		testData.ProcessorPartitions = new DummyMessage[TestConfiguration.PROCESSOR_PARTITION_COUNT, maxProcessorPartitionLength];
		testData.ProcessorIndices = new long[TestConfiguration.PROCESSOR_PARTITION_COUNT, 3];
	}
}


public class TestExecutor
{
	private readonly PerformanceMetrics _metrics;

	public TestExecutor(PerformanceMetrics metrics)
	{
		_metrics = metrics;
	}

	public async Task<TestResult> ExecuteAsync(List<Task> tasks, CancellationTokenSource cancellationTokenSource)
	{
		TestContext.WriteLine("🕐 Starting test execution");

		using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(TestConfiguration.TEST_TIMEOUT_SECONDS));
		var timeout = Task.Delay(Timeout.Infinite, timeoutCts.Token);
		var allTasks = Task.WhenAll(tasks);

		var completedTask = await Task.WhenAny(allTasks, timeout);

		var result = new TestResult
		{
			Completed = completedTask == allTasks,
			TimedOut = completedTask == timeout && timeoutCts.Token.IsCancellationRequested
		};

		if (result.TimedOut)
		{
			TestContext.WriteLine("❌ Test timed out, cancelling all tasks...");
			cancellationTokenSource.Cancel();
		}
		else if (completedTask == allTasks)
		{
			TestContext.WriteLine("ℹ️ All tasks completed successfully");
		}

		try
		{
			await allTasks;
		}
		catch (OperationCanceledException)
		{
			TestContext.WriteLine("ℹ️ Tasks cancelled as expected");
		}

		_metrics.Stop();
		result.FinalMetrics = _metrics;

		return result;
	}
}

public class TestResult
{
	public bool Completed { get; set; }
	public bool TimedOut { get; set; }
	public PerformanceMetrics? FinalMetrics { get; set; }

	public bool IsSuccessful => Completed && !TimedOut &&
							   FinalMetrics?.MessagesOut > 0 &&
							   FinalMetrics?.GetThroughputPerSecond() > 0;
}

[TestFixture]
public class BackPressureTest : KafkaTestBase
{
	private readonly PerformanceMetrics _metrics = new("Back Pressure");

    [Test]
    [Category("BackPressure")]
    [Category("Kafka")]
    [Description("Back Pressure Test - Kafka Mode")]
    public async Task TestBackPressureKafka()
    {
        await RunBackPressureTest();
    }

	[OneTimeSetUp]
	public override async Task OneTimeSetUp()
	{
		await base.OneTimeSetUp();
		TestContext.WriteLine($"🔗 Using Kafka: {KafkaConnectionString}");
	}

	[OneTimeTearDown]
	public override async Task OneTimeTearDown()
	{
		await base.OneTimeTearDown();
		TestContext.WriteLine($"🧹 CLEANUP COMPLETE: {DateTime.Now:yyyy-MM-dd HH:mm:ss} - BackPressureTest cleanup completed");
	}


    private async Task RunBackPressureTest()
    {
        TestConfiguration.LogConfiguration();
        TestContext.WriteLine($"🕐 TEST START - Kafka Mode with Aspire infrastructure");
        await CreateTopicAsync("backpressure.keygen.topic", TestConfiguration.TOPIC_PARTITION_COUNT, 1);

		try
		{
			var dataManager = new TestDataManager();
			var testData = dataManager.PrepareTestData();
			_metrics.Start();

			TaskFactory.ResetKafkaTimingStats();

			var taskFactory = new TaskFactory(_metrics);
                var tasks = taskFactory.CreateAllTasks(
                    testData,
                    KafkaConnectionString!,
                    Producer,
                    Consumer
                );

			var executor = new TestExecutor(_metrics);
			var result = await executor.ExecuteAsync(tasks, testData.CancellationTokenSource);

			ValidateResults(result);
			GenerateFinalReport(result);
		}
		catch (Exception ex)
		{
			TestContext.WriteLine($"❌ Stack trace: {ex.StackTrace}");
			throw;
		}
	}

	private void ValidateResults(TestResult result)
	{
		TestContext.WriteLine("=== Test Results Summary ===");
		TestContext.WriteLine($"ℹ️ Test Completion Status: {(result.Completed ? "Completed" : "Not Completed")}");
		TestContext.WriteLine($"ℹ️ Timed Out: {(result.TimedOut ? "Yes" : "No")}");

		if (result.FinalMetrics != null)
		{
			TestContext.WriteLine($"ℹ️ Total Input Messages: {result.FinalMetrics.MessagesIn:N0}");
			TestContext.WriteLine($"ℹ️ Total Output Messages: {result.FinalMetrics.MessagesOut:N0}");
			TestContext.WriteLine($"ℹ️ Total Runtime: {result.FinalMetrics.TotalElapsed.TotalSeconds:F2} seconds");

			var targetMessages = TestConfiguration.TARGET_MESSAGES;
			var progress = (double)result.FinalMetrics.MessagesOut / targetMessages * 100;
			TestContext.WriteLine($"ℹ️ Processing Progress: {progress:F1}% ({result.FinalMetrics.MessagesOut:N0}/{targetMessages:N0})");

			var (gatewayTps, keygenTps, processorTps, kafkaConsumerTps) = TaskFactory.GetStageTpsStats();

			TestContext.WriteLine("=== Stage Performance (TPS) ===");
			TestContext.WriteLine($"📊 Gateway TPS: {gatewayTps:F1} messages/sec");

            TestContext.WriteLine($"📊 Kafka Consumer TPS: {kafkaConsumerTps:F1} messages/sec");
            TestContext.WriteLine($"📊 KeyGen+Consumer TPS: {keygenTps:F1} messages/sec");

			TestContext.WriteLine($"📊 Processor TPS: {processorTps:F1} messages/sec");
			TestContext.WriteLine($"📊 Overall System TPS: {result.FinalMetrics.GetThroughputPerSecond():F1} messages/sec");
		}
		else
		{
			TestContext.WriteLine("❌ Performance metrics are null");
		}

		TestContext.WriteLine($"ℹ️ Test Result: {(result.IsSuccessful ? "Success" : "Failed")}");
		TestContext.WriteLine("=== Test Results Summary End ===");
	}

	private void GenerateFinalReport(TestResult result)
	{
        const string testMode = "Kafka";

        TestContext.WriteLine($"🕐 Test completed with {testMode} mode");
        result.FinalMetrics?.PrintSummary(testMode);

        var (
            avgKafkaProduceMs, kafkaProduceCount,
            avgBatchProcessorMs, batchProcessorCount,
            avgKafkaConsumeMs, kafkaConsumeCount, kafkaConsumeSuccessCount, kafkaConsumeSuccessRate,
            avgConsumerMethodMs, consumerMethodCount,
            avgDataProcessingMs, dataProcessingCount,
            avgMetricsUpdateMs, metricsUpdateCount,
            avgDeserializeMs, deserializeCount, deserializeSuccessCount, deserializeSuccessRate,
            avgRingBufferAddMs, ringBufferAddCount,
            avgRingBufferTakeMs, ringBufferTakeCount
        ) = TaskFactory.GetKafkaTimingStats();

			TestContext.WriteLine("=== Kafka Mode Detailed Timing Statistics ===");

			TestContext.WriteLine("【Producer Side】");
			TestContext.WriteLine($"ℹ️ ProduceValueKafkaMode: Call Count={kafkaProduceCount:N0}, Avg Time={avgKafkaProduceMs:F3}ms");
			TestContext.WriteLine($"ℹ️ BatchProcessor.AddMessage: Call Count={batchProcessorCount:N0}, Avg Time={avgBatchProcessorMs:F3}ms");

			TestContext.WriteLine("【Consumer Side】");
			TestContext.WriteLine($"ℹ️ Consumer Method Total Time: Call Count={consumerMethodCount:N0}, Avg Time={avgConsumerMethodMs:F2}ms");
			TestContext.WriteLine($"ℹ️ Consumer.Consume(): Call Count={kafkaConsumeCount:N0}, Avg Time={avgKafkaConsumeMs:F2}ms");
			TestContext.WriteLine($"ℹ️ TryDeserialize(): Call Count={deserializeCount:N0}, Avg Time={avgDeserializeMs:F3}ms");
			TestContext.WriteLine($"ℹ️ Data Processing Logic: Call Count={dataProcessingCount:N0}, Avg Time={avgDataProcessingMs:F3}ms");
			TestContext.WriteLine($"ℹ️ Metrics Update Operations: Call Count={metricsUpdateCount:N0}, Avg Time={avgMetricsUpdateMs:F3}ms");

			TestContext.WriteLine($"ℹ️ RingBuffer.TryAddItem(): Call Count={ringBufferAddCount:N0}, Avg Time={avgRingBufferAddMs:F3}ms");
			TestContext.WriteLine($"ℹ️ RingBuffer.TakeWhere(): Call Count={ringBufferTakeCount:N0}, Avg Time={avgRingBufferTakeMs:F3}ms");

			TestContext.WriteLine("【Success Rate Statistics】");
			TestContext.WriteLine($"ℹ️ Consumer Success Rate: {kafkaConsumeSuccessRate:F1}% ({kafkaConsumeSuccessCount:N0}/{kafkaConsumeCount:N0})");
			TestContext.WriteLine($"ℹ️ Deserialization Success Rate: {deserializeSuccessRate:F1}% ({deserializeSuccessCount:N0}/{deserializeCount:N0})");

			TestContext.WriteLine("【Performance Analysis】");
			var totalConsumeTime = kafkaConsumeCount * avgKafkaConsumeMs / 1000;
			var totalMethodTime = consumerMethodCount * avgConsumerMethodMs / 1000;
			if (kafkaConsumeCount > 0 && consumerMethodCount > 0)
			{
				var consumeRatio = avgKafkaConsumeMs / Math.Max(avgConsumerMethodMs, 0.001) * 100;
				var dataProcessingRatio = avgDataProcessingMs / Math.Max(avgConsumerMethodMs, 0.001) * 100;
				var metricsUpdateRatio = avgMetricsUpdateMs / Math.Max(avgConsumerMethodMs, 0.001) * 100;

				TestContext.WriteLine($"ℹ️ Consume Time Ratio to Total Method Time: {consumeRatio:F1}%");
				TestContext.WriteLine($"ℹ️ Data Processing Time Ratio to Total Method Time: {dataProcessingRatio:F1}%");
				TestContext.WriteLine($"ℹ️ Metrics Update Time Ratio to Total Method Time: {metricsUpdateRatio:F1}%");

				var rawConsumeRate = kafkaConsumeSuccessCount / Math.Max(totalConsumeTime, 0.001);
				var actualMessageRate = result.FinalMetrics?.GetThroughputPerSecond() ?? 0;
				var messagesPerConsume = actualMessageRate / Math.Max(rawConsumeRate, 0.001);

				TestContext.WriteLine($"ℹ️ Raw Consume Calls Rate: {rawConsumeRate:F1} consume-calls/sec");
				TestContext.WriteLine($"ℹ️ Actual Message Processing Rate: {actualMessageRate:F1} messages/sec");
				TestContext.WriteLine($"ℹ️ Average Messages per Consume Call: {messagesPerConsume:F1}");

				TestContext.WriteLine("【Detailed Performance Breakdown】");
				TestContext.WriteLine($"ℹ️ Kafka Consume Calls: {kafkaConsumeCount:N0} calls");
				TestContext.WriteLine($"ℹ️ Successful Consume Calls: {kafkaConsumeSuccessCount:N0} calls");
				TestContext.WriteLine($"ℹ️ Total Messages Processed: {result.FinalMetrics?.MessagesOut:N0} messages");
				TestContext.WriteLine($"ℹ️ Consume Call Success Rate: {kafkaConsumeSuccessRate:F1}%");

				if (kafkaConsumeSuccessCount > 0)
				{
					var messagesPerSuccessfulConsume = (double)(result.FinalMetrics?.MessagesOut ?? 0) / kafkaConsumeSuccessCount;
					TestContext.WriteLine($"ℹ️ Messages per Successful Consume: {messagesPerSuccessfulConsume:F1}");

					var effectivenessRatio = actualMessageRate / Math.Max(rawConsumeRate, 0.001);
					TestContext.WriteLine($"ℹ️ Consumer Effectiveness Ratio: {effectivenessRatio:F1}x (higher is better)");
				}

				var totalAccountedTime = avgKafkaConsumeMs + avgDeserializeMs + avgDataProcessingMs +
										avgRingBufferAddMs + avgRingBufferTakeMs + avgMetricsUpdateMs;
				var unaccountedTime = Math.Max(0, avgConsumerMethodMs - totalAccountedTime);
				var unaccountedRatio = unaccountedTime / Math.Max(avgConsumerMethodMs, 0.001) * 100;

				TestContext.WriteLine($"ℹ️ Total Accounted Time: {totalAccountedTime:F3}ms, Unaccounted Time: {unaccountedTime:F3}ms ({unaccountedRatio:F1}%)");
			}
        var throughput = result.FinalMetrics?.GetThroughputPerSecond() ?? 0;
        var backPressureRate = result.FinalMetrics?.GetBackPressureRate() ?? 0;
        var messagesOut = result.FinalMetrics?.MessagesOut ?? 0;
        var backPressureEvents = result.FinalMetrics?.BackPressureEvents ?? 0;

        TestContext.WriteLine($"✅ {testMode} Back Pressure test completed: {messagesOut:N0} messages processed at {throughput:F2} msgs/sec");
        TestContext.WriteLine($"ℹ️ Back pressure activated {backPressureEvents:N0} times ({backPressureRate:F1}% of operations)");

        // Windowed TPS and latency percentiles
        if (result.FinalMetrics != null)
        {
            var tps1s = result.FinalMetrics.GetCurrentThroughputPerSecond(1);
            var tps10s = result.FinalMetrics.GetCurrentThroughputPerSecond(10);
            TestContext.WriteLine("=== Windowed Throughput (messages/sec) ===");
            TestContext.WriteLine($"⏱️ Last 1s TPS:  {tps1s:F1}");
            TestContext.WriteLine($"⏱️ Last 10s TPS: {tps10s:F1}");

            var avgLat = result.FinalMetrics.GetAverageLatency();
            var medLat = result.FinalMetrics.GetMedianLatency();
            var p90 = result.FinalMetrics.GetPercentileLatency(90);
            var p95 = result.FinalMetrics.GetPercentileLatency(95);
            var p99 = result.FinalMetrics.GetPercentileLatency(99);
            var minLat = result.FinalMetrics.MinMessageLatency;
            var maxLat = result.FinalMetrics.MaxMessageLatency;

            TestContext.WriteLine("=== Latency (ms) ===");
            TestContext.WriteLine($"Avg: {avgLat:F2} | Median: {medLat:F2} | p90: {p90:F2} | p95: {p95:F2} | p99: {p99:F2} | Min: {minLat:F2} | Max: {maxLat:F2}");
        }

		if (result.TimedOut)
		{
			TestContext.WriteLine("⚠️ Test completed due to timeout");
		}

		if (TestConfiguration.ELIMINATE_SYNC_WAITS)
		{
			TestContext.WriteLine("🚀 Sync waits elimination was ENABLED");
		}
		else
		{
			TestContext.WriteLine("⏱️ Sync waits elimination was DISABLED");
		}
	}

}
