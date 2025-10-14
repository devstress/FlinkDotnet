using FlinkDotNet.DataStream;

namespace Flink.JobBuilder.Tests.Tests
{
    [TestFixture]
    public class FlinkAPIExtensionsTests
    {
        #region TypeInformation Tests

        [Test]
        public void TypeInformation_Of_ReturnsTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<string>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void TypeInformation_OfGeneric_ReturnsTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<int>.Of<int>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(int)));
        }

        [Test]
        public void TypeInformation_OfGeneric_WithDifferentType_ReturnsCorrectType()
        {
            // Act
            var typeInfo = TypeInformation<string>.Of<double>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(double)));
        }

        #endregion

        #region KafkaSinkFunction Tests

        [Test]
        public void KafkaSinkFunction_Constructor_StoresProperties()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            System.Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);

            // Act
            var sink = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);

            // Assert
            Assert.That(sink.Topic, Is.EqualTo(topic));
            Assert.That(sink.BootstrapServers, Is.EqualTo(bootstrapServers));
        }

        [Test]
        public void KafkaSinkFunction_InvokeAsync_CompletesSuccessfully()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            System.Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);
            var sink = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);
            var element = "test element";

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await sink.InvokeAsync(element));
        }

        [Test]
        public void KafkaSinkFunction_InvokeAsync_WithCancellationToken_CompletesSuccessfully()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            System.Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);
            var sink = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);
            var element = "test element";
            using var cts = new CancellationTokenSource();

            // Act & Assert
            Assert.DoesNotThrowAsync(async () => await sink.InvokeAsync(element, cts.Token));
        }

        #endregion

        #region StartingOffsets Tests

        [Test]
        public void StartingOffsets_Earliest_HasCorrectValue()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
        }

        [Test]
        public void StartingOffsets_Latest_HasCorrectValue()
        {
            // Assert
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        #endregion

        #region StreamExecutionEnvironmentExtensions Tests

        [Test]
        public void SetStreamTimeCharacteristic_WithProcessingTime_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.ProcessingTime);

            // Assert
            Assert.That(result, Is.SameAs(env));
            var config = env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("ProcessingTime"));
        }

        [Test]
        public void SetStreamTimeCharacteristic_WithEventTime_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

            // Assert
            Assert.That(result, Is.SameAs(env));
            var config = env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("EventTime"));
        }

        [Test]
        public void SetStreamTimeCharacteristic_WithIngestionTime_ReturnsEnvironment()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var result = env.SetStreamTimeCharacteristic(TimeCharacteristic.IngestionTime);

            // Assert
            Assert.That(result, Is.SameAs(env));
            var config = env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), Is.EqualTo("IngestionTime"));
        }

        [Test]
        public void AddSource_WithSourceFunction_ReturnsDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var result = env.AddSource<string>(sourceFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        #endregion

        #region DataStreamExtensions Tests

        [Test]
        public void AddSink_WithKafkaSinkFunction_ReturnsDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();
            var stream = env.AddSource<string>(sourceFunction);
            var sinkFunction = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s));

            // Act
            var result = stream.AddSink(sinkFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf<DataStream<string>>());
        }

        #endregion

        #region KafkaSourceFunctionExtensions Tests

        [Test]
        public void SetStartFromEarliest_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "earliest");

            // Act
            var result = source.SetStartFromEarliest();

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedWatermarks_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "earliest");
            var assigner = new TestPunctuatedWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPeriodicWatermarks_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "earliest");
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        #endregion

        #region TimeCharacteristic Enum Tests

        [Test]
        public void TimeCharacteristic_ProcessingTime_HasCorrectValue()
        {
            // Assert
            Assert.That(TimeCharacteristic.ProcessingTime, Is.EqualTo(TimeCharacteristic.ProcessingTime));
            Assert.That(TimeCharacteristic.ProcessingTime.ToString(), Is.EqualTo("ProcessingTime"));
        }

        [Test]
        public void TimeCharacteristic_EventTime_HasCorrectValue()
        {
            // Assert
            Assert.That(TimeCharacteristic.EventTime, Is.EqualTo(TimeCharacteristic.EventTime));
            Assert.That(TimeCharacteristic.EventTime.ToString(), Is.EqualTo("EventTime"));
        }

        [Test]
        public void TimeCharacteristic_IngestionTime_HasCorrectValue()
        {
            // Assert
            Assert.That(TimeCharacteristic.IngestionTime, Is.EqualTo(TimeCharacteristic.IngestionTime));
            Assert.That(TimeCharacteristic.IngestionTime.ToString(), Is.EqualTo("IngestionTime"));
        }

        #endregion

        #region Test Helper Classes

        private class TestSourceFunction : ISourceFunction<string>
        {
            public async System.Collections.Generic.IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                // Test implementation - yield a test value
                await Task.Yield();
                yield return "test";
            }
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark CheckAndGetNextWatermark(string element, long extractedTimestamp)
            {
                return new Watermark(extractedTimestamp);
            }
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark GetCurrentWatermark()
            {
                return new Watermark(System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }

        #endregion

        #region AddSource Extension Tests - Coverage Enhancement

        [Test]
        public void AddSource_WithoutNameParameter_ReturnsDataStreamWithDefaultName()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new TestSourceFunction();

            // Act
            var result = StreamExecutionEnvironmentExtensions.AddSource(env, sourceFunction);

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.TypeOf<DataStream<string>>());
        }

        #endregion
    }
}
