using FlinkDotNet.DataStream;
using Xunit;

namespace FlinkDotNet.DataStream.Tests
{
    public class UnifiedSourceTests
    {
        [Fact]
        public void KafkaSource_Builder_SetsBootstrapServers()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal("localhost:9092", source.BootstrapServers);
        }

        [Fact]
        public void KafkaSource_Builder_SetsSingleTopic()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Single(source.Topics);
            Assert.Equal("test-topic", source.Topics[0]);
        }

        [Fact]
        public void KafkaSource_Builder_SetsMultipleTopics()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopics("topic1", "topic2", "topic3")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal(3, source.Topics.Count);
            Assert.Contains("topic1", source.Topics);
            Assert.Contains("topic2", source.Topics);
            Assert.Contains("topic3", source.Topics);
        }

        [Fact]
        public void KafkaSource_Builder_SetsGroupId()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetGroupId("test-group")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal("test-group", source.GroupId);
        }

        [Fact]
        public void KafkaSource_Builder_SetsStartingOffsets()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetStartingOffsets(KafkaStartingOffsets.Earliest)
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal(KafkaStartingOffsets.Earliest, source.StartingOffsets);
        }

        [Fact]
        public void KafkaSource_Builder_SetsStoppingOffsets()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetStoppingOffsets(KafkaStoppingOffsets.Latest)
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal(KafkaStoppingOffsets.Latest, source.StoppingOffsets);
        }

        [Fact]
        public void KafkaSource_Builder_ThrowsIfBootstrapServersNotSet()
        {
            var builder = KafkaSource<string>.Builder()
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema());

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("Bootstrap servers must be set", exception.Message);
        }

        [Fact]
        public void KafkaSource_Builder_ThrowsIfTopicsNotSet()
        {
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetDeserializer(new StringDeserializationSchema());

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("At least one topic must be set", exception.Message);
        }

        [Fact]
        public void KafkaSource_Builder_ThrowsIfDeserializerNotSet()
        {
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic");

            var exception = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.Contains("Deserializer must be set", exception.Message);
        }

        [Fact]
        public void KafkaSource_Builder_DefaultsToLatestStartingOffsets()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal(KafkaStartingOffsets.Latest, source.StartingOffsets);
        }

        [Fact]
        public void KafkaSource_Builder_DefaultsToUnboundedStoppingOffsets()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal(KafkaStoppingOffsets.Unbounded, source.StoppingOffsets);
        }

        [Fact]
        public void KafkaSource_Builder_AllowsNullGroupId()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Null(source.GroupId);
        }

        [Fact]
        public void KafkaSource_Builder_SupportsChaining()
        {
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("topic1")
                .SetTopic("topic2")
                .SetGroupId("test-group")
                .SetStartingOffsets(KafkaStartingOffsets.Earliest)
                .SetStoppingOffsets(KafkaStoppingOffsets.Latest)
                .SetDeserializer(new StringDeserializationSchema())
                .Build();

            Assert.Equal("localhost:9092", source.BootstrapServers);
            Assert.Equal(2, source.Topics.Count);
            Assert.Equal("test-group", source.GroupId);
            Assert.Equal(KafkaStartingOffsets.Earliest, source.StartingOffsets);
            Assert.Equal(KafkaStoppingOffsets.Latest, source.StoppingOffsets);
        }

        [Fact]
        public void KafkaStartingOffsets_Enum_HasAllExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(KafkaStartingOffsets), KafkaStartingOffsets.Earliest));
            Assert.True(Enum.IsDefined(typeof(KafkaStartingOffsets), KafkaStartingOffsets.Latest));
            Assert.True(Enum.IsDefined(typeof(KafkaStartingOffsets), KafkaStartingOffsets.Group));
            Assert.True(Enum.IsDefined(typeof(KafkaStartingOffsets), KafkaStartingOffsets.Timestamp));
            Assert.True(Enum.IsDefined(typeof(KafkaStartingOffsets), KafkaStartingOffsets.SpecificOffsets));
        }

        [Fact]
        public void KafkaStoppingOffsets_Enum_HasAllExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(KafkaStoppingOffsets), KafkaStoppingOffsets.Unbounded));
            Assert.True(Enum.IsDefined(typeof(KafkaStoppingOffsets), KafkaStoppingOffsets.Latest));
            Assert.True(Enum.IsDefined(typeof(KafkaStoppingOffsets), KafkaStoppingOffsets.Timestamp));
            Assert.True(Enum.IsDefined(typeof(KafkaStoppingOffsets), KafkaStoppingOffsets.SpecificOffsets));
        }

        [Fact]
        public void Boundedness_Enum_HasExpectedValues()
        {
            Assert.True(Enum.IsDefined(typeof(Boundedness), Boundedness.Bounded));
            Assert.True(Enum.IsDefined(typeof(Boundedness), Boundedness.Unbounded));
        }

        [Fact]
        public void StringDeserializationSchema_DeserializesUtf8Bytes()
        {
            var schema = new StringDeserializationSchema();
            var bytes = System.Text.Encoding.UTF8.GetBytes("test-message");

            var result = schema.Deserialize(bytes);

            Assert.Equal("test-message", result);
        }

        [Fact]
        public void StringDeserializationSchema_DoesNotProduceRowtime()
        {
            var schema = new StringDeserializationSchema();

            Assert.False(schema.ProducesRowtime);
        }

        // Helper class for testing
        private class StringDeserializationSchema : DeserializationSchema<string>
        {
            public override string Deserialize(byte[] message)
            {
                return System.Text.Encoding.UTF8.GetString(message);
            }
        }
    }
}
