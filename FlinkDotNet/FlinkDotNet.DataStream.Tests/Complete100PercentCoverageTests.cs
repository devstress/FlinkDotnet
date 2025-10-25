using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive tests to achieve 100% code coverage for FlinkDotNet.DataStream.
    /// Focuses on previously uncovered edge cases, error paths, and boundary conditions.
    /// </summary>
    [TestFixture]
    public class Complete100PercentCoverageTests
    {
        [Test]
        public void DataStream_AddSink_WithNullSinkFunction_ShouldReturnThis()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Call AddSink with null to test the null check path
            var result = stream.AddSink(null!);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void DataStream_AddSink_WithNonKafkaSinkFunction_ShouldReturnThis()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var customSink = new CustomSinkWithoutKafkaProperties();

            // Act - This tests the path where Topic/BootstrapServers properties don't exist
            var result = stream.AddSink(customSink);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void DataStream_AddSink_WithKafkaSinkHavingNullTopic_ShouldReturnThis()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var kafkaSink = new KafkaSinkWithNullProperties();

            // Act - This tests the path where properties exist but return null
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void DataStream_AddSink_WithKafkaSinkHavingEmptyTopic_ShouldReturnThis()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");
            var kafkaSink = new KafkaSinkWithEmptyProperties();

            // Act - This tests the path where properties are empty strings
            var result = stream.AddSink(kafkaSink);

            // Assert
            Assert.That(result, Is.SameAs(stream));
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithGenericSourceFunction_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new GenericTestSourceFunction();

            // Act - This ensures AddSource extension method is called
            var stream = env.AddSource(sourceFunction);

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void StreamExecutionEnvironmentExtensions_AddSource_WithCustomName_ShouldCreateDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var sourceFunction = new GenericTestSourceFunction();

            // Act - Test with explicit source name
            var stream = env.AddSource(sourceFunction, "Custom Source Name");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void TypeInformation_Of_WithTypeParameter_ShouldReturnTypeInformation()
        {
            // Arrange & Act
            var typeInfo = TypeInformation<int>.Of<string>();

            // Assert
            Assert.That(typeInfo, Is.Not.Null);
            Assert.That(typeInfo.GetType(), Is.EqualTo(typeof(string)));
        }

        [Test]
        public void StreamExecutionEnvironment_FromKafka_WithEmptyBootstrapServers_ShouldThrowArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Test with empty string
            var ex = Assert.Throws<ArgumentException>(() =>
                env.FromKafka("test-topic", "", "test-group"));
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void StreamExecutionEnvironment_FromKafka_WithWhitespaceBootstrapServers_ShouldThrowArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Test with whitespace
            var ex = Assert.Throws<ArgumentException>(() =>
                env.FromKafka("test-topic", "   ", "test-group"));
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void KafkaSourceFunctionExtensions_AllMethods_ShouldReturnSameInstance()
        {
            // Arrange
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", s => s, "latest");
            var punctuatedAssigner = new TestPunctuatedWatermarkAssigner();
            var periodicAssigner = new TestPeriodicWatermarkAssigner();

            // Act - Test all extension methods
            var result1 = source.SetStartFromEarliest();
            var result2 = source.AssignTimestampsAndWatermarks(punctuatedAssigner);
            var result3 = source.AssignTimestampsAndWatermarks(periodicAssigner);

            // Assert - All should return the same instance
            Assert.That(result1, Is.SameAs(source));
            Assert.That(result2, Is.SameAs(source));
            Assert.That(result3, Is.SameAs(source));
        }

        [Test]
        public void StartingOffsets_Constants_ShouldHaveExpectedValues()
        {
            // Act & Assert
            Assert.That(StartingOffsets.Earliest, Is.EqualTo("earliest"));
            Assert.That(StartingOffsets.Latest, Is.EqualTo("latest"));
        }

        [Test]
        public void IDeserializationSchema_Methods_ShouldBeImplementable()
        {
            // Arrange
            var schema = new TestDeserializationSchema();
            var testData = System.Text.Encoding.UTF8.GetBytes("test");

            // Act
            var result = schema.Deserialize(testData);
            var isEnd = schema.IsEndOfStream(result);
            var typeInfo = schema.GetProducedType();

            // Assert
            Assert.That(result, Is.EqualTo("test"));
            Assert.That(isEnd, Is.False);
            Assert.That(typeInfo, Is.Not.Null);
        }

        [Test]
        public void ISerializationSchema_Serialize_ShouldBeImplementable()
        {
            // Arrange
            var schema = new TestSerializationSchema();

            // Act
            var result = schema.Serialize("test-data");

            // Assert
            Assert.That(result, Is.Not.Null);
            Assert.That(System.Text.Encoding.UTF8.GetString(result), Is.EqualTo("test-data"));
        }

        // Helper classes for testing
        private class CustomSinkWithoutKafkaProperties : ISinkFunction<string>
        {
            public Task InvokeAsync(string element, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class KafkaSinkWithNullProperties : ISinkFunction<string>
        {
            // Properties accessed via reflection by DataStream.AddSink
            public string? Topic { get; } = null;
            public string? BootstrapServers { get; } = null;

            public Task InvokeAsync(string element, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class KafkaSinkWithEmptyProperties : ISinkFunction<string>
        {
            // Properties accessed via reflection by DataStream.AddSink
            public string Topic { get; } = "";
            public string BootstrapServers { get; } = "";

            public Task InvokeAsync(string element, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }

        private class GenericTestSourceFunction : ISourceFunction<string>
        {
            public async System.Collections.Generic.IAsyncEnumerable<string> RunAsync(
                [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
            {
                yield return "test-data";
                await Task.CompletedTask;
            }
        }

        private class TestPunctuatedWatermarkAssigner : IAssignerWithPunctuatedWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark? CheckAndGetNextWatermark(string lastElement, long extractedTimestamp)
            {
                return new Watermark(extractedTimestamp);
            }
        }

        private class TestPeriodicWatermarkAssigner : IAssignerWithPeriodicWatermarks<string>
        {
            public long ExtractTimestamp(string element, long previousElementTimestamp)
            {
                return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            public Watermark? GetCurrentWatermark()
            {
                return new Watermark(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }
        }

        private class TestDeserializationSchema : IDeserializationSchema<string>
        {
            public string Deserialize(byte[] bytes)
            {
                return System.Text.Encoding.UTF8.GetString(bytes);
            }

            public bool IsEndOfStream(string element)
            {
                return false;
            }

            public TypeInformation<string> GetProducedType()
            {
                return TypeInformation<string>.Of();
            }
        }

        private class TestSerializationSchema : ISerializationSchema<string>
        {
            public byte[] Serialize(string element)
            {
                return System.Text.Encoding.UTF8.GetBytes(element);
            }
        }
    }
}