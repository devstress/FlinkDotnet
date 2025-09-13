using Confluent.Kafka;
using Microsoft.Extensions.ObjectPool;
using NUnit.Framework;
using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Text.Json;

namespace BackPressure.IntegrationTests;

public struct Utf8Name
{
	private readonly string _value;

	public Utf8Name(string value)
	{
		_value = value ?? string.Empty;
	}

	public static implicit operator string(Utf8Name utf8Name) => utf8Name._value;
	public static implicit operator Utf8Name(string value) => new(value);

	public override string ToString() => _value ?? string.Empty;
}

public struct DummyMessage
{
	public Utf8Name Sender;
	public Utf8Name Recipient;
	public long StartTime;
	public long KeyId;
}

public class RingBuffer<T> where T : struct
{
	public delegate void InvalidateItem(ref T item);
	int head;
	int tail;
	public int count;
	T[] values;
	readonly int maxSize;
	readonly InvalidateItem invalidateItem;
	readonly Predicate<T> isItemValid;

	public bool IsFull => count > 0 && tail == head;
	public int Count => count;

	public RingBuffer(int size, InvalidateItem invalidateItem, Predicate<T> isItemValid)
	{
		maxSize = (int)BitOperations.RoundUpToPowerOf2((uint)size);
		values = new T[maxSize];
		this.invalidateItem = invalidateItem;
		this.isItemValid = isItemValid;
	}

	public bool TryAddItem(T item)
	{
		if (IsFull)
		{
			if (count < maxSize)
			{
				Compact();
			}
			else
			{
				return false;
			}
		}
		ref T indexedItem = ref values[tail];
		indexedItem = item;
		tail = (tail + 1) % maxSize;
		count++;
		return true;
	}

	void Compact()
	{
		if (count == 0)
		{
			return;
		}

		int mask = maxSize - 1;
		ref var item = ref values[head];
		while (!isItemValid(item))
		{
			head = (head + 1) & mask;
			item = ref values[head];
		}

		item = ref values[tail];
		while (!isItemValid(item))
		{
			tail = (tail - 1) & mask;
			item = ref values[tail];
		}

		var pos = tail;
		var nextPos = (tail - 1) & mask;

		do
		{
			while (isItemValid(values[pos]))
			{
				pos = (pos - 1) & mask;
				nextPos = (nextPos - 1) & mask;
			}

			while (!isItemValid(values[nextPos]))
			{
				nextPos = (nextPos - 1) & mask;
			}

			ref var takeItem = ref values[nextPos];
			ref var replaceItem = ref values[pos];
			replaceItem = values[nextPos];
			invalidateItem(ref takeItem);
		} while (nextPos != head);

		head = pos;
	}

	public int TakeWhere(Predicate<T> predicate, T[] tempBuffer, out T[] removed)
	{
		var removedList = new List<T>();
		if (count == 0)
		{
			removed = removedList.ToArray();
			return 0;
		}

		var pos = head;
		int tempBufferPos = 0;
		do
		{
			ref T item = ref values[pos];
			if (isItemValid(item) && predicate(item))
			{
				count--;
				if (tempBufferPos < tempBuffer.Length)
				{
					tempBuffer[tempBufferPos] = item;
					removedList.Add(item);
					tempBufferPos = (tempBufferPos + 1) % maxSize;
				}
				invalidateItem(ref item);
			}
			else
			{
				pos = (pos + 1) % maxSize;
				continue;
			}

			if (pos == head)
			{
				do
				{
					if (isItemValid(item))
					{
						break;
					}

					head = (head + 1) % maxSize;
					pos = (pos + 1) % maxSize;
					item = ref values[pos];
				} while (pos != tail);
			}
			else
			{
				pos = (pos + 1) % maxSize;
			}
		} while (pos != tail);

		removed = removedList.ToArray();
		return removed.Length;
	}
}

public class PooledMetricsService
{
	private static readonly ConcurrentDictionary<string, long> metrics = new();
	private static readonly ObjectPool<Dictionary<string, long>> _pool =
		new DefaultObjectPool<Dictionary<string, long>>(
			new DefaultPooledObjectPolicy<Dictionary<string, long>>());
	public static Dictionary<string, long> GetMetrics(string prefix)
	{
		var result = _pool.Get();
		try
		{
			result.Clear();
			result.EnsureCapacity(16);

			foreach (var kvp in metrics)
				if (kvp.Key.StartsWith(prefix, StringComparison.Ordinal))
					result[kvp.Key] = kvp.Value;

			var copy = new Dictionary<string, long>(result);
			return copy;
		}
		finally
		{
			_pool.Return(result);
		}
	}
	public static void UpdateMetric(string key, int delta)
	{
		while (true)
		{
			if (metrics.TryGetValue(key, out var current))
			{
				if (metrics.TryUpdate(key, current + delta, current))
					return;
			}
			else if (metrics.TryAdd(key, delta))
			{
				return;
			}
		}
	}
	public Dictionary<string, long> GetMetricsInstance(string prefix) => GetMetrics(prefix);
	public void UpdateMetricInstance(string key, int delta) => UpdateMetric(key, delta);
}

public static class MetricsCacheUpdater
{
	public static Task RunAsync(
		ConcurrentDictionary<string, long> cache,
		string prefix,
		CancellationToken cancellationToken)
	{
		TaskCompletionSource taskCompletionSource = new();
		Thread updaterThread = new(() =>
		{
			try
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					var metrics = PooledMetricsService.GetMetrics(prefix);

					foreach (var kvp in metrics)
					{
						cache[kvp.Key] = kvp.Value;
					}
					Thread.Sleep(100);
				}
			}
			catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
			{
			}

			taskCompletionSource.TrySetResult();
		});

		updaterThread.Start();
		return taskCompletionSource.Task;
	}
}

public static class MessageSerializer
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = null,
		WriteIndented = false
	};

	public static string Serialize(in Utf8Name sender, in Utf8Name recipient, long startTime,
		long keyId, string producerMode, int partitionIndex, bool useBinarySerialization)
	{
		return useBinarySerialization
			? SerializeBinary(sender, recipient, startTime, keyId, producerMode, partitionIndex)
			: SerializeJson(sender, recipient, startTime, keyId, producerMode, partitionIndex);
	}

	public static bool TryDeserialize(string message, out string sender, out string recipient,
		out long startTime, out long keyId, bool useBinarySerialization)
	{
		return useBinarySerialization
			? TryDeserializeBinary(message, out sender, out recipient, out startTime, out keyId)
			: TryDeserializeJson(message, out sender, out recipient, out startTime, out keyId);
	}

	private static string SerializeBinary(in Utf8Name sender, in Utf8Name recipient, long startTime, long keyId, string producerMode, int partitionIndex)
	{
		var senderStr = TestUtils.GetUtf8String(sender);
		var recipientStr = TestUtils.GetUtf8String(recipient);
		var sb = new StringBuilder(256);
		return sb.Append(senderStr).Append('|')
				 .Append(recipientStr).Append('|')
				 .Append(startTime).Append('|')
				 .Append(keyId).Append('|')
				 .Append(producerMode).Append('|')
				 .Append(partitionIndex).ToString();
	}

	private static string SerializeJson(in Utf8Name sender, in Utf8Name recipient, long startTime, long keyId, string producerMode, int partitionIndex)
	{
		var messageObject = new
		{
			Sender = TestUtils.GetUtf8String(sender),
			Recipient = TestUtils.GetUtf8String(recipient),
			StartTime = startTime,
			KeyId = keyId,
			ProducerMode = producerMode,
			PartitionIndex = partitionIndex
		};
		return JsonSerializer.Serialize(messageObject, JsonOptions);
	}

	private static bool TryDeserializeBinary(string message, out string sender, out string recipient, out long startTime, out long keyId)
	{
		sender = recipient = string.Empty;
		startTime = keyId = 0;
		var parts = message.Split('|');
		if (parts.Length >= 4)
		{
			sender = parts[0];
			recipient = parts[1];
			return long.TryParse(parts[2], out startTime) && long.TryParse(parts[3], out keyId);
		}
		return false;
	}

	private static bool TryDeserializeJson(string message, out string sender, out string recipient, out long startTime, out long keyId)
	{
		sender = recipient = string.Empty;
		startTime = keyId = 0;
		try
		{
			var messageData = JsonSerializer.Deserialize<Dictionary<string, object>>(message);
			if (messageData != null &&
				messageData.TryGetValue("StartTime", out var startTimeObj) &&
				messageData.TryGetValue("KeyId", out var keyIdObj) &&
				messageData.TryGetValue("Sender", out var senderObj) &&
				messageData.TryGetValue("Recipient", out var recipientObj))
			{
				startTime = ((JsonElement)startTimeObj).GetInt64();
				keyId = ((JsonElement)keyIdObj).GetInt64();
				sender = ((JsonElement)senderObj).GetString()!;
				recipient = ((JsonElement)recipientObj).GetString()!;
				return true;
			}
		}
		catch
		{
		}
		return false;
	}
}

public static class TestUtils
{
	public static string BuildMetricsKey(StringBuilder sb, string prefix, in Utf8Name sender, in Utf8Name recipient)
	{
		sb.Clear();
		return sb.Append(prefix).Append(sender.ToString()).Append('/').Append(recipient.ToString()).ToString();
	}

	public static int CalculatePartitionIndex(int senderIndex, int recipientIndex, int partitionCount)
	{
		return Math.Abs((senderIndex * 397) ^ recipientIndex) % partitionCount;
	}

	public static string GetUtf8String(in Utf8Name utf8Name)
	{
		return utf8Name.ToString();
	}
}

public class BatchMessageProcessor
{
	private readonly List<Message<string, string>> _batch = new();
	private readonly IProducer<string, string> _producer;
	private readonly int _batchSize;
	private readonly bool _useBatchProcessing;
	private readonly bool _eliminateSyncWaits;
	private readonly string _topicName;

	public BatchMessageProcessor(IProducer<string, string> producer, int batchSize,
		bool useBatchProcessing, bool eliminateSyncWaits, string topicName = "backpressure.keygen.topic")
	{
		_producer = producer;
		_batchSize = batchSize;
		_useBatchProcessing = useBatchProcessing;
		_eliminateSyncWaits = eliminateSyncWaits;
		_topicName = topicName;
	}

	public async Task AddMessageAsync(Message<string, string> message)
	{
		if (_useBatchProcessing)
		{
			_batch.Add(message);
			if (_batch.Count >= _batchSize)
			{
				await FlushBatchAsync();
			}
		}
		else
		{
			await _producer.ProduceAsync(_topicName, message);
		}
	}

	public async Task FlushBatchAsync()
	{
		if (_batch.Count == 0) return;

		if (_useBatchProcessing)
		{
			if (_eliminateSyncWaits)
			{
				var tasks = _batch.Select(msg =>
					_producer.ProduceAsync(_topicName, msg)).ToArray();
				_batch.Clear();

				_ = Task.Run(async () =>
				{
					try
					{
						await Task.WhenAll(tasks);
					}
					catch (ProduceException<string, string> ex) when (ex.Error.Code == ErrorCode.Local_QueueFull)
					{
						
					}
					catch (Exception ex)
					{
						TestContext.WriteLine($"❌ Unexpected error: {ex.Message}");
					}
				});
			}
			else
			{
				var tasks = _batch.Select(msg =>
					_producer.ProduceAsync(_topicName, msg)).ToArray();
				_producer.Flush(TimeSpan.FromMilliseconds(1));
				_batch.Clear();
				_ = Task.WhenAll(tasks);
			}
		}
	}

	public async Task FlushAllAsync()
	{
		await FlushBatchAsync();
		if (!_eliminateSyncWaits)
		{
			_producer.Flush(TimeSpan.FromMilliseconds(100));
		}
	}
}

public static class KafkaConfigHelper
{
    public static ProducerConfig CreatePerformanceProducerConfig(string bootstrapServers)
    {
        var cfg = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            Acks = Acks.All,
            LingerMs = 50,
            BatchSize = 65536,
            CompressionType = CompressionType.Zstd,
            QueueBufferingMaxMessages = 1000000,
            QueueBufferingMaxKbytes = 2097152,
            DeliveryReportFields = "none",
            MessageSendMaxRetries = 3,
            RequestTimeoutMs = 5000,
            MessageTimeoutMs = 10000,
            SecurityProtocol = SecurityProtocol.Plaintext,
            EnableIdempotence = true,
            EnableDeliveryReports = false
        };
        TestContext.WriteLine($"🟡 [Config] Created ProducerConfig BootstrapServers={cfg.BootstrapServers}, SecurityProtocol={cfg.SecurityProtocol}, Idempotence={cfg.EnableIdempotence}");
        return cfg;
    }

	public static ConsumerConfig CreatePerformanceConsumerConfig(string bootstrapServers, string groupId, bool enableAutoCommit = true)
	{
        var cfg = new ConsumerConfig
        {
            BootstrapServers = bootstrapServers,
            GroupId = groupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = enableAutoCommit,
            FetchMinBytes = enableAutoCommit ? 1 : 1024 * 16,
            FetchMaxBytes = 1024 * 1024,
            MaxPartitionFetchBytes = 1024 * 1024,
            FetchWaitMaxMs = 10,
            SecurityProtocol = SecurityProtocol.Plaintext,
            SessionTimeoutMs = 30000,
            HeartbeatIntervalMs = 3000,
            MaxPollIntervalMs = 300000
        };
        TestContext.WriteLine($"🟡 [Config] Created ConsumerConfig BootstrapServers={cfg.BootstrapServers}, GroupId={cfg.GroupId}, SecurityProtocol={cfg.SecurityProtocol}");
        return cfg;
    }
}
