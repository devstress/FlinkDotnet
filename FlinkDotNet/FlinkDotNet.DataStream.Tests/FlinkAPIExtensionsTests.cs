using System;
using System.Threading;
using System.Threading.Tasks;
using FlinkDotNet.DataStream;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class TypeInformationTests
    {
        [Test]
        public void Of_Generic_CreatesTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<int>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(int)));
        }

        [Test]
        public void Of_String_CreatesTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<string>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void Of_ComplexType_CreatesTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<TestClass>.Of();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(TestClass)));
        }

        [Test]
        public void Of_WithTypeParameter_CreatesCorrectTypeInformation()
        {
            // Act
            var typeInfo = TypeInformation<int>.Of<double>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(double)));
        }

        [Test]
        public void Of_DifferentTypes_CreatesDifferentInstances()
        {
            // Act
            var typeInfo1 = TypeInformation<int>.Of();
            var typeInfo2 = TypeInformation<string>.Of();

            // Assert
            Assert.That(typeInfo1, Is.Not.SameAs(typeInfo2));
            Assert.That(typeInfo1.GetType(), Is.Not.EqualTo(typeInfo2.GetType()));
        }

        private class TestClass { }
    }

    [TestFixture]
    public class KafkaSinkFunctionTests
    {
        [Test]
        public void Constructor_WithValidParameters_CreatesSinkFunction()
        {
            // Arrange
            var topic = "test-topic";
            var bootstrapServers = "localhost:9092";
            Func<string, byte[]> serializer = s => System.Text.Encoding.UTF8.GetBytes(s);

            // Act
            var sinkFunction = new KafkaSinkFunction<string>(topic, bootstrapServers, serializer);

            // Assert
            Assert.That(sinkFunction, Is.Not.Null);
            Assert.That(sinkFunction.Topic, Is.EqualTo(topic));
            Assert.That(sinkFunction.BootstrapServers, Is.EqualTo(bootstrapServers));
        }

        [Test]
        public void Topic_ReturnsConstructorValue()
        {
            // Arrange
            var expectedTopic = "my-topic";
            var sinkFunction = new KafkaSinkFunction<int>(expectedTopic, "localhost:9092", i => BitConverter.GetBytes(i));

            // Act
            var actualTopic = sinkFunction.Topic;

            // Assert
            Assert.That(actualTopic, Is.EqualTo(expectedTopic));
        }

        [Test]
        public void BootstrapServers_ReturnsConstructorValue()
        {
            // Arrange
            var expectedServers = "broker1:9092,broker2:9092";
            var sinkFunction = new KafkaSinkFunction<string>(
                "topic", 
                expectedServers, 
                s => System.Text.Encoding.UTF8.GetBytes(s));

            // Act
            var actualServers = sinkFunction.BootstrapServers;

            // Assert
            Assert.That(actualServers, Is.EqualTo(expectedServers));
        }

        [Test]
        public async Task InvokeAsync_WithElement_CompletesSuccessfully()
        {
            // Arrange
            var sinkFunction = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s));

            // Act & Assert - Should complete without throwing
            await sinkFunction.InvokeAsync("test-element");
        }

        [Test]
        public async Task InvokeAsync_WithCancellationToken_CompletesSuccessfully()
        {
            // Arrange
            var sinkFunction = new KafkaSinkFunction<int>(
                "test-topic",
                "localhost:9092",
                i => BitConverter.GetBytes(i));
            var cts = new CancellationTokenSource();

            // Act & Assert
            await sinkFunction.InvokeAsync(42, cts.Token);
        }

        [Test]
        public async Task InvokeAsync_WithMultipleElements_CompletesForEach()
        {
            // Arrange
            var sinkFunction = new KafkaSinkFunction<string>(
                "test-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s));

            // Act & Assert
            await sinkFunction.InvokeAsync("element1");
            await sinkFunction.InvokeAsync("element2");
            await sinkFunction.InvokeAsync("element3");
        }

        [Test]
        public void Constructor_WithComplexSerializer_CreatesSinkFunction()
        {
            // Arrange
            Func<TestMessage, byte[]> serializer = msg => 
                System.Text.Encoding.UTF8.GetBytes($"{msg.Id}:{msg.Value}");

            // Act
            var sinkFunction = new KafkaSinkFunction<TestMessage>(
                "test-topic",
                "localhost:9092",
                serializer);

            // Assert
            Assert.That(sinkFunction, Is.Not.Null);
        }

        private class TestMessage
        {
            public int Id { get; set; }
            public string Value { get; set; } = string.Empty;
        }
    }

    [TestFixture]
    public class StartingOffsetsTests
    {
        [Test]
        public void Earliest_HasCorrectValue()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
        }

        [Test]
        public void Latest_HasCorrectValue()
        {
            // Assert
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        [Test]
        public void StartingOffsets_AreNotEqual()
        {
            // Assert
            Assert.That(StartingOffsets.Earliest, Is.Not.EqualTo(StartingOffsets.Latest));
        }
    }

    [TestFixture]
    public class TimeCharacteristicTests
    {
        [Test]
        public void TimeCharacteristic_HasProcessingTime()
        {
            // Assert
            Assert.That(TimeCharacteristic.ProcessingTime, Is.EqualTo(TimeCharacteristic.ProcessingTime));
        }

        [Test]
        public void TimeCharacteristic_HasEventTime()
        {
            // Assert
            Assert.That(TimeCharacteristic.EventTime, Is.EqualTo(TimeCharacteristic.EventTime));
        }

        [Test]
        public void TimeCharacteristic_HasIngestionTime()
        {
            // Assert
            Assert.That(TimeCharacteristic.IngestionTime, Is.EqualTo(TimeCharacteristic.IngestionTime));
        }

        [Test]
        public void TimeCharacteristic_DifferentValues_AreNotEqual()
        {
            // Assert
            Assert.That(TimeCharacteristic.ProcessingTime, Is.Not.EqualTo(TimeCharacteristic.EventTime));
            Assert.That(TimeCharacteristic.EventTime, Is.Not.EqualTo(TimeCharacteristic.IngestionTime));
            Assert.That(TimeCharacteristic.IngestionTime, Is.Not.EqualTo(TimeCharacteristic.ProcessingTime));
        }
    }

    [TestFixture]
    public class StreamExecutionEnvironmentExtensionsTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void SetUp()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        [Test]
        public void SetStreamTimeCharacteristic_WithProcessingTime_SetsCharacteristic()
        {
            // Act
            var result = _env.SetStreamTimeCharacteristic(TimeCharacteristic.ProcessingTime);

            // Assert
            Assert.That(result, Is.SameAs(_env));
            var config = _env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), 
                Is.EqualTo(TimeCharacteristic.ProcessingTime.ToString()));
        }

        [Test]
        public void SetStreamTimeCharacteristic_WithEventTime_SetsCharacteristic()
        {
            // Act
            var result = _env.SetStreamTimeCharacteristic(TimeCharacteristic.EventTime);

            // Assert
            Assert.That(result, Is.SameAs(_env));
            var config = _env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), 
                Is.EqualTo(TimeCharacteristic.EventTime.ToString()));
        }

        [Test]
        public void SetStreamTimeCharacteristic_WithIngestionTime_SetsCharacteristic()
        {
            // Act
            var result = _env.SetStreamTimeCharacteristic(TimeCharacteristic.IngestionTime);

            // Assert
            Assert.That(result, Is.SameAs(_env));
            var config = _env.GetConfig().GetConfiguration();
            Assert.That(config.GetString("stream.time-characteristic", null), 
                Is.EqualTo(TimeCharacteristic.IngestionTime.ToString()));
        }

        [Test]
        public void SetStreamTimeCharacteristic_SupportsMethodChaining()
        {
            // Act
            var result = _env
                .SetStreamTimeCharacteristic(TimeCharacteristic.EventTime)
                .SetParallelism(4);

            // Assert
            Assert.That(result, Is.SameAs(_env));
        }

        [Test]
        public void AddSource_WithSourceFunction_ReturnsDataStream()
        {
            // Arrange
            var sourceFunction = new TestSourceFunction();

            // Act
            var stream = _env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void AddSource_WithKafkaSource_ReturnsDataStream()
        {
            // Arrange
            var kafkaSource = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "earliest");

            // Act
            var stream = _env.AddSource(kafkaSource);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        private class TestSourceFunction : ISourceFunction<string>
        {
            public async IAsyncEnumerable<string> RunAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return "test1";
                yield return "test2";
                await Task.CompletedTask;
            }
        }
    }

    [TestFixture]
    public class DataStreamExtensionsTests
    {
        private StreamExecutionEnvironment _env = null!;

        [SetUp]
        public void SetUp()
        {
            _env = StreamExecutionEnvironment.GetExecutionEnvironment();
        }

        [Test]
        public void AddSink_WithKafkaSinkFunction_ReturnsSameStream()
        {
            // Arrange
            var collection = new[] { "test1", "test2", "test3" };
            var stream = _env.FromCollection(collection);
            var sinkFunction = new KafkaSinkFunction<string>(
                "output-topic",
                "localhost:9092",
                s => System.Text.Encoding.UTF8.GetBytes(s));

            // Act
            var result = stream.AddSink(sinkFunction);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void AddSink_WithComplexType_ReturnsSameStream()
        {
            // Arrange
            var collection = new[] { new TestData { Id = 1 }, new TestData { Id = 2 } };
            var stream = _env.FromCollection(collection);
            var sinkFunction = new KafkaSinkFunction<TestData>(
                "output-topic",
                "localhost:9092",
                data => BitConverter.GetBytes(data.Id));

            // Act
            var result = stream.AddSink(sinkFunction);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        private class TestData
        {
            public int Id { get; set; }
        }
    }

    [TestFixture]
    public class KafkaSourceFunctionExtensionsTests
    {
        [Test]
        public void SetStartFromEarliest_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "latest");

            // Act
            var result = source.SetStartFromEarliest();

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPunctuatedAssigner_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<int>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => int.Parse(s),
                "earliest");
            var assigner = new TestPunctuatedWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void AssignTimestampsAndWatermarks_WithPeriodicAssigner_ReturnsSourceFunction()
        {
            // Arrange
            var source = new KafkaSourceFunction<int>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => int.Parse(s),
                "earliest");
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = source.AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        [Test]
        public void SetStartFromEarliest_SupportsMethodChaining()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>(
                "test-topic",
                "localhost:9092",
                "test-group",
                s => s,
                "latest");
            var assigner = new TestPeriodicWatermarkAssigner();

            // Act
            var result = source
                .SetStartFromEarliest()
                .AssignTimestampsAndWatermarks(assigner);

            // Assert
            Assert.That(result, Is.SameAs(source));
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element;
            public Watermark? CheckAndGetNextWatermark(int lastElement, long extractedTimestamp) => null;
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<int>
        {
            public long ExtractTimestamp(int element, long previousElementTimestamp) => element;
            public Watermark? GetCurrentWatermark() => null;
        }
    }
}