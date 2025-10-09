using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;
using FlinkDotNet.DataStream;

namespace Exercise2_BackupAggregator
{
    /// <summary>
    /// Native DataStream API implementation matching Baeldung Sections 7-11 exactly
    /// Reference: https://www.baeldung.com/kafka-flink-data-pipeline
    /// </summary>
    public static class BaeldungNativeApi
    {
        /// <summary>
        /// Section 7: Custom Object Deserialization
        /// Baeldung: InputMessageDeserializationSchema implements DeserializationSchema&lt;InputMessage&gt;
        /// </summary>
        public class InputMessageDeserializationSchema : IDeserializationSchema<InputMessage>
        {
            private static readonly JsonSerializerOptions Options = new()
            {
                PropertyNameCaseInsensitive = true
            };

            public InputMessage Deserialize(byte[] bytes)
            {
                return JsonSerializer.Deserialize<InputMessage>(bytes, Options) ?? new InputMessage();
            }

            public bool IsEndOfStream(InputMessage inputMessage)
            {
                return false; // No special end-of-stream condition
            }

            public TypeInformation<InputMessage> GetProducedType()
            {
                return TypeInformation<InputMessage>.Of();
            }
        }

        /// <summary>
        /// Section 8: Custom Object Serialization
        /// Baeldung: BackupSerializationSchema implements SerializationSchema&lt;Backup&gt;
        /// </summary>
        public class BackupSerializationSchema : ISerializationSchema<Backup>
        {
            private static readonly JsonSerializerOptions Options = new()
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };

            public byte[] Serialize(Backup backup)
            {
                try
                {
                    var json = JsonSerializer.Serialize(backup, Options);
                    return System.Text.Encoding.UTF8.GetBytes(json);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"Failed to parse JSON: {ex.Message}");
                    return Array.Empty<byte>();
                }
            }
        }

        /// <summary>
        /// Section 9: Timestamping Messages
        /// Baeldung: InputMessageTimestampAssigner implements AssignerWithPunctuatedWatermarks&lt;InputMessage&gt;
        /// </summary>
        public class InputMessageTimestampAssigner : IAssignerWithPunctuatedWatermarks<InputMessage>
        {
            public long ExtractTimestamp(InputMessage element, long previousElementTimestamp)
            {
                // Convert LocalDateTime to EpochSecond (milliseconds)
                var milliseconds = (long)(element.SentAt - DateTime.UnixEpoch).TotalMilliseconds;
                return milliseconds;
            }

            public Watermark? CheckAndGetNextWatermark(InputMessage lastElement, long extractedTimestamp)
            {
                // Allow 1500ms lateness
                return new Watermark(extractedTimestamp - 1500);
            }
        }

        /// <summary>
        /// Section 10-11: Aggregating Backups
        /// Baeldung: BackupAggregator implements AggregateFunction&lt;InputMessage, List&lt;InputMessage&gt;, Backup&gt;
        /// </summary>
        public class BackupAggregator : IAggregateFunction<InputMessage, List<InputMessage>, Backup>
        {
            public List<InputMessage> CreateAccumulator()
            {
                return new List<InputMessage>();
            }

            public List<InputMessage> Add(InputMessage inputMessage, List<InputMessage> accumulator)
            {
                accumulator.Add(inputMessage);
                return accumulator;
            }

            public Backup GetResult(List<InputMessage> accumulator)
            {
                return new Backup(accumulator, DateTime.UtcNow);
            }

            public List<InputMessage> Merge(List<InputMessage> a, List<InputMessage> b)
            {
                a.AddAll(b);
                return a;
            }
        }

        /// <summary>
        /// Section 11: Main CreateBackup() method - EXACT match to Baeldung Java API
        /// 
        /// Baeldung Java code:
        /// public static void createBackup() throws Exception {
        ///     String inputTopic = "flink_input";
        ///     String outputTopic = "flink_output";
        ///     String consumerGroup = "baeldung";
        ///     String kafkaAddress = "192.168.99.100:9092";
        ///     StreamExecutionEnvironment environment = StreamExecutionEnvironment.getExecutionEnvironment();
        ///     environment.setStreamTimeCharacteristic(TimeCharacteristic.EventTime);
        ///     FlinkKafkaConsumer011&lt;InputMessage&gt; flinkKafkaConsumer = createInputMessageConsumer(inputTopic, kafkaAddress, consumerGroup);
        ///     flinkKafkaConsumer.setStartFromEarliest();
        ///     flinkKafkaConsumer.assignTimestampsAndWatermarks(new InputMessageTimestampAssigner());
        ///     FlinkKafkaProducer011&lt;Backup&gt; flinkKafkaProducer = createBackupProducer(outputTopic, kafkaAddress);
        ///     DataStream&lt;InputMessage&gt; inputMessagesStream = environment.addSource(flinkKafkaConsumer);
        ///     inputMessagesStream
        ///       .timeWindowAll(Time.hours(24))
        ///       .aggregate(new BackupAggregator())
        ///       .addSink(flinkKafkaProducer);
        ///     environment.execute();
        /// }
        /// </summary>
        public static async Task CreateBackup()
        {
            string inputTopic = "flink_input";
            string outputTopic = "flink_output";
            string consumerGroup = "baeldung";
            string kafkaAddress = Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS") ?? "localhost:29092";

            // Get StreamExecutionEnvironment
            var environment = StreamExecutionEnvironment.GetExecutionEnvironment();
            
            // Set stream time characteristic to EventTime
            environment.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

            // Create Kafka consumer for InputMessage
            var flinkKafkaConsumer = CreateInputMessageConsumer(inputTopic, kafkaAddress, consumerGroup);
            flinkKafkaConsumer.SetStartFromEarliest();

            // Assign timestamps and watermarks
            flinkKafkaConsumer.AssignTimestampsAndWatermarks(new InputMessageTimestampAssigner());

            // Create Kafka producer for Backup
            var flinkKafkaProducer = CreateBackupProducer(outputTopic, kafkaAddress);

            // Add source to create DataStream
            var inputMessagesStream = environment.AddSource(flinkKafkaConsumer);

            // Apply transformations: window → aggregate → sink
            inputMessagesStream
                .TimeWindowAll(Time.Hours(24))
                .Aggregate(new BackupAggregator())
                .AddSink(flinkKafkaProducer);

            // Execute the job
            await environment.ExecuteAsync("backup-aggregator");
        }

        /// <summary>
        /// Helper: Create Kafka consumer for InputMessage with custom deserializer
        /// Baeldung equivalent: createInputMessageConsumer()
        /// </summary>
        private static KafkaSourceFunction<InputMessage> CreateInputMessageConsumer(
            string topic,
            string bootstrapServers,
            string groupId)
        {
            var deserializationSchema = new InputMessageDeserializationSchema();
            return new KafkaSourceFunction<InputMessage>(
                topic,
                bootstrapServers,
                groupId,
                (json) => deserializationSchema.Deserialize(System.Text.Encoding.UTF8.GetBytes(json)),
                StartingOffsets.Earliest
            );
        }

        /// <summary>
        /// Helper: Create Kafka producer for Backup with custom serializer
        /// Baeldung equivalent: createBackupProducer()
        /// </summary>
        private static KafkaSinkFunction<Backup> CreateBackupProducer(
            string topic,
            string bootstrapServers)
        {
            return new KafkaSinkFunction<Backup>(
                topic,
                bootstrapServers,
                new BackupSerializationSchema().Serialize
            );
        }
    }

    /// <summary>
    /// Extension for List&lt;T&gt;.AddAll() to match Java API
    /// </summary>
    public static class ListExtensions
    {
        public static void AddAll<T>(this List<T> list, List<T> items)
        {
            list.AddRange(items);
        }
    }
}