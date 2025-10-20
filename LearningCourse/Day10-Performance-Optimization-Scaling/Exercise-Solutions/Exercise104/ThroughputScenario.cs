using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Confluent.Kafka;

namespace Exercise104;

/// <summary>
/// Execute throughput optimization scenarios with real Kafka infrastructure
/// </summary>
public class ThroughputScenario
{
    private readonly string _kafkaBootstrapServers;
    private readonly string _inputTopic;

    public ThroughputScenario(string kafkaBootstrapServers, string inputTopic)
    {
        _kafkaBootstrapServers = kafkaBootstrapServers;
        _inputTopic = inputTopic;
    }

    /// <summary>
    /// Run baseline scenario with JSON and no optimization
    /// </summary>
    public async Task<ThroughputMetrics> RunBaselineAsync(int eventCount)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new ThroughputMetrics
        {
            Scenario = "Baseline (JSON, No Compression, Batch=1)",
            EventsProcessed = 0,
            BatchSize = 1
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = false, // Disable for throughput benchmarking
            Acks = Acks.Leader, // Faster than Acks.All for benchmarking
            LingerMs = 0, // No batching
            BatchSize = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise104-baseline",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, string>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        long totalSerializedSize = 0;
        var serializationStopwatch = Stopwatch.StartNew();

        // Produce events with JSON serialization
        for (int i = 0; i < eventCount; i++)
        {
            var evt = ThroughputEvent.CreateSample();
            var json = JsonSerializer.Serialize(evt);
            totalSerializedSize += json.Length;

            await producer.ProduceAsync(_inputTopic, new Message<string, string>
            {
                Key = evt.Id,
                Value = json
            });

            // Progress logging to prevent timeout
            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"   Produced {i + 1}/{eventCount} events...");
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        serializationStopwatch.Stop();

        // Consume events
        var deserializationStopwatch = Stopwatch.StartNew();
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    _ = JsonSerializer.Deserialize<ThroughputEvent>(consumeResult.Message.Value);
                    processedCount++;
                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        deserializationStopwatch.Stop();
        stopwatch.Stop();

        metrics.EventsProcessed = processedCount;
        metrics.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        metrics.SerializedSizeBytes = totalSerializedSize;
        metrics.SerializationTimeMs = serializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.DeserializationTimeMs = deserializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.CompressionRatio = 1.0;
        metrics.CalculateThroughput();

        return metrics;
    }

    /// <summary>
    /// Run scenario with Binary serialization
    /// </summary>
    public async Task<ThroughputMetrics> RunBinarySerializationAsync(int eventCount)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new ThroughputMetrics
        {
            Scenario = "Binary Serialization",
            EventsProcessed = 0,
            BatchSize = 1
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = false, // Disable for throughput benchmarking
            Acks = Acks.Leader, // Faster than Acks.All for benchmarking
            LingerMs = 0,
            BatchSize = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise104-binary",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        long totalSerializedSize = 0;
        var serializationStopwatch = Stopwatch.StartNew();

        // Produce events with Binary serialization
        for (int i = 0; i < eventCount; i++)
        {
            var evt = ThroughputEvent.CreateSample();
            var (data, serMs, _) = SerializationTester.TestBinary(evt);
            totalSerializedSize += data.Length;

            await producer.ProduceAsync(_inputTopic, new Message<string, byte[]>
            {
                Key = evt.Id,
                Value = data
            });

            // Progress logging to prevent timeout
            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"   Produced {i + 1}/{eventCount} events...");
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        serializationStopwatch.Stop();

        // Consume events
        var deserializationStopwatch = Stopwatch.StartNew();
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    processedCount++;
                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        deserializationStopwatch.Stop();
        stopwatch.Stop();

        metrics.EventsProcessed = processedCount;
        metrics.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        metrics.SerializedSizeBytes = totalSerializedSize;
        metrics.SerializationTimeMs = serializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.DeserializationTimeMs = deserializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.CompressionRatio = 1.0;
        metrics.CalculateThroughput();

        return metrics;
    }

    /// <summary>
    /// Run scenario with MessagePack serialization
    /// </summary>
    public async Task<ThroughputMetrics> RunMessagePackAsync(int eventCount)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new ThroughputMetrics
        {
            Scenario = "MessagePack Serialization",
            EventsProcessed = 0,
            BatchSize = 1
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = false, // Disable for throughput benchmarking
            Acks = Acks.Leader, // Faster than Acks.All for benchmarking
            LingerMs = 0,
            BatchSize = 1
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise104-msgpack",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        long totalSerializedSize = 0;
        var serializationStopwatch = Stopwatch.StartNew();

        // Produce events with MessagePack
        for (int i = 0; i < eventCount; i++)
        {
            var evt = ThroughputEvent.CreateSample();
            var (data, serMs, _) = SerializationTester.TestMessagePack(evt);
            totalSerializedSize += data.Length;

            await producer.ProduceAsync(_inputTopic, new Message<string, byte[]>
            {
                Key = evt.Id,
                Value = data
            });

            // Progress logging to prevent timeout
            if ((i + 1) % 100 == 0)
            {
                Console.WriteLine($"   Produced {i + 1}/{eventCount} events...");
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        serializationStopwatch.Stop();

        // Consume events
        var deserializationStopwatch = Stopwatch.StartNew();
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    processedCount++;
                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        deserializationStopwatch.Stop();
        stopwatch.Stop();

        metrics.EventsProcessed = processedCount;
        metrics.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        metrics.SerializedSizeBytes = totalSerializedSize;
        metrics.SerializationTimeMs = serializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.DeserializationTimeMs = deserializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.CompressionRatio = 1.0;
        metrics.CalculateThroughput();

        return metrics;
    }

    /// <summary>
    /// Run optimized scenario with MessagePack + GZip compression + batching
    /// </summary>
    public async Task<ThroughputMetrics> RunOptimizedAsync(int eventCount, int batchSize = 100)
    {
        var stopwatch = Stopwatch.StartNew();
        var metrics = new ThroughputMetrics
        {
            Scenario = $"Optimized (MessagePack + GZip + Batch={batchSize})",
            EventsProcessed = 0,
            BatchSize = batchSize
        };

        var producerConfig = new ProducerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            EnableIdempotence = false, // Disable for throughput benchmarking
            Acks = Acks.Leader, // Faster than Acks.All for benchmarking
            LingerMs = 10, // Enable batching
            BatchSize = batchSize * 1024, // Approximate batch size
            CompressionType = Confluent.Kafka.CompressionType.Gzip
        };

        var consumerConfig = new ConsumerConfig
        {
            BootstrapServers = _kafkaBootstrapServers,
            GroupId = "exercise104-optimized",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var producer = new ProducerBuilder<string, byte[]>(producerConfig).Build();
        using var consumer = new ConsumerBuilder<string, byte[]>(consumerConfig).Build();
        consumer.Subscribe(_inputTopic);

        long totalSerializedSize = 0;
        long totalCompressedSize = 0;
        var serializationStopwatch = Stopwatch.StartNew();

        // Produce events in batches
        var batch = new List<ThroughputEvent>();
        for (int i = 0; i < eventCount; i++)
        {
            batch.Add(ThroughputEvent.CreateSample());

            if (batch.Count >= batchSize || i == eventCount - 1)
            {
                // Serialize batch with MessagePack
                var data = SerializationTester.BatchSerialize(batch, SerializationFormat.MessagePack);
                totalSerializedSize += data.Length;

                // Compress
                var (compressedData, ratio, _) = SerializationTester.TestCompression(data, CompressionType.GZip);
                totalCompressedSize += compressedData.Length;

                // Send batch
                await producer.ProduceAsync(_inputTopic, new Message<string, byte[]>
                {
                    Key = Guid.NewGuid().ToString(),
                    Value = compressedData
                });

                batch.Clear();
            }
        }

        producer.Flush(TimeSpan.FromSeconds(5));
        serializationStopwatch.Stop();

        // Consume batches
        var deserializationStopwatch = Stopwatch.StartNew();
        int processedCount = 0;
        var timeout = TimeSpan.FromSeconds(15);
        var cts = new CancellationTokenSource(timeout);

        try
        {
            while (processedCount < eventCount && !cts.Token.IsCancellationRequested)
            {
                var consumeResult = consumer.Consume(TimeSpan.FromMilliseconds(100));
                if (consumeResult != null)
                {
                    var decompressed = SerializationTester.Decompress(consumeResult.Message.Value, CompressionType.GZip);
                    var events = SerializationTester.BatchDeserialize(decompressed, SerializationFormat.MessagePack);
                    processedCount += events.Count;
                    consumer.Commit(consumeResult);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timeout reached
        }

        deserializationStopwatch.Stop();
        stopwatch.Stop();

        metrics.EventsProcessed = processedCount;
        metrics.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;
        metrics.SerializedSizeBytes = totalSerializedSize;
        metrics.SerializationTimeMs = serializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.DeserializationTimeMs = deserializationStopwatch.Elapsed.TotalMilliseconds;
        metrics.CompressionRatio = totalSerializedSize > 0 ? (double)totalSerializedSize / totalCompressedSize : 1.0;
        metrics.CalculateThroughput();

        return metrics;
    }
}