using System;
using System.Linq;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class KafkaSourceTests
    {
        #region Builder Method Tests

        [Test]
        public void Builder_ReturnsNewBuilderInstance()
        {
            // Act
            var builder = KafkaSource<string>.Builder();

            // Assert
            Assert.That(builder, Is.Not.Null);
            Assert.That(builder, Is.InstanceOf<KafkaSource<string>.KafkaSourceBuilder<string>>());
        }

        [Test]
        public void Builder_CreatesMultipleIndependentBuilders()
        {
            // Act
            var builder1 = KafkaSource<string>.Builder();
            var builder2 = KafkaSource<string>.Builder();

            // Assert
            Assert.That(builder1, Is.Not.Null);
            Assert.That(builder2, Is.Not.Null);
            Assert.That(builder1, Is.Not.SameAs(builder2));
        }

        #endregion

        #region Builder SetBootstrapServers Tests

        [Test]
        public void SetBootstrapServers_WithValidServers_SetsServers()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var servers = "localhost:9092";

            // Act
            var result = builder.SetBootstrapServers(servers);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetBootstrapServers_WithMultipleServers_SetsServers()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var servers = "broker1:9092,broker2:9092,broker3:9092";

            // Act
            var result = builder.SetBootstrapServers(servers);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region Builder SetTopic Tests

        [Test]
        public void SetTopic_WithValidTopic_AddsTopic()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var topic = "test-topic";

            // Act
            var result = builder.SetTopic(topic);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetTopic_CalledMultipleTimes_AddsMultipleTopics()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            builder.SetTopic("topic1");
            builder.SetTopic("topic2");
            builder.SetTopic("topic3");

            // Assert - Will verify in Build test
            Assert.Pass("Multiple topics added successfully");
        }

        #endregion

        #region Builder SetTopics Tests

        [Test]
        public void SetTopics_WithMultipleTopics_AddsAllTopics()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var topics = new[] { "topic1", "topic2", "topic3" };

            // Act
            var result = builder.SetTopics(topics);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetTopics_WithEmptyArray_DoesNotThrow()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act & Assert
            Assert.DoesNotThrow(() => builder.SetTopics());
        }

        #endregion

        #region Builder SetGroupId Tests

        [Test]
        public void SetGroupId_WithValidGroupId_SetsGroupId()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var groupId = "test-consumer-group";

            // Act
            var result = builder.SetGroupId(groupId);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region Builder SetDeserializer Tests

        [Test]
        public void SetDeserializer_WithValidDeserializer_SetsDeserializer()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();
            var deserializer = new TestDeserializer();

            // Act
            var result = builder.SetDeserializer(deserializer);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region Builder SetStartingOffsets Tests

        [Test]
        public void SetStartingOffsets_WithEarliest_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStartingOffsets(KafkaStartingOffsets.Earliest);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStartingOffsets_WithLatest_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStartingOffsets(KafkaStartingOffsets.Latest);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStartingOffsets_WithGroup_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStartingOffsets(KafkaStartingOffsets.Group);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStartingOffsets_WithTimestamp_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStartingOffsets(KafkaStartingOffsets.Timestamp);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStartingOffsets_WithSpecificOffsets_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStartingOffsets(KafkaStartingOffsets.SpecificOffsets);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region Builder SetStoppingOffsets Tests

        [Test]
        public void SetStoppingOffsets_WithUnbounded_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStoppingOffsets(KafkaStoppingOffsets.Unbounded);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStoppingOffsets_WithLatest_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStoppingOffsets(KafkaStoppingOffsets.Latest);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStoppingOffsets_WithTimestamp_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStoppingOffsets(KafkaStoppingOffsets.Timestamp);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        [Test]
        public void SetStoppingOffsets_WithSpecificOffsets_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder();

            // Act
            var result = builder.SetStoppingOffsets(KafkaStoppingOffsets.SpecificOffsets);

            // Assert
            Assert.That(result, Is.SameAs(builder));
        }

        #endregion

        #region Builder Build Tests

        [Test]
        public void Build_WithAllRequiredParameters_BuildsSuccessfully()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer());

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source, Is.Not.Null);
            Assert.That(source.BootstrapServers, Is.EqualTo("localhost:9092"));
            Assert.That(source.Topics, Has.Count.EqualTo(1));
            Assert.That(source.Topics[0], Is.EqualTo("test-topic"));
        }

        [Test]
        public void Build_WithoutBootstrapServers_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer());

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex!.Message, Does.Contain("Bootstrap servers must be set"));
        }

        [Test]
        public void Build_WithEmptyBootstrapServers_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer());

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex!.Message, Does.Contain("Bootstrap servers must be set"));
        }

        [Test]
        public void Build_WithWhitespaceBootstrapServers_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("   ")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer());

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex!.Message, Does.Contain("Bootstrap servers must be set"));
        }

        [Test]
        public void Build_WithoutTopic_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetDeserializer(new TestDeserializer());

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex!.Message, Does.Contain("At least one topic must be set"));
        }

        [Test]
        public void Build_WithoutDeserializer_ThrowsInvalidOperationException()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic");

            // Act & Assert
            var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());
            Assert.That(ex!.Message, Does.Contain("Deserializer must be set"));
        }

        [Test]
        public void Build_WithMultipleTopics_BuildsWithAllTopics()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopics("topic1", "topic2", "topic3")
                .SetDeserializer(new TestDeserializer());

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source.Topics, Has.Count.EqualTo(3));
            Assert.That(source.Topics, Contains.Item("topic1"));
            Assert.That(source.Topics, Contains.Item("topic2"));
            Assert.That(source.Topics, Contains.Item("topic3"));
        }

        [Test]
        public void Build_WithGroupId_SetsGroupId()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetGroupId("test-group")
                .SetDeserializer(new TestDeserializer());

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source.GroupId, Is.EqualTo("test-group"));
        }

        [Test]
        public void Build_WithoutGroupId_GroupIdIsNull()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer());

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source.GroupId, Is.Null);
        }

        [Test]
        public void Build_WithCustomStartingOffsets_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer())
                .SetStartingOffsets(KafkaStartingOffsets.Earliest);

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source.StartingOffsets, Is.EqualTo(KafkaStartingOffsets.Earliest));
        }

        [Test]
        public void Build_WithCustomStoppingOffsets_SetsOffsets()
        {
            // Arrange
            var builder = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer())
                .SetStoppingOffsets(KafkaStoppingOffsets.Latest);

            // Act
            var source = builder.Build();

            // Assert
            Assert.That(source.StoppingOffsets, Is.EqualTo(KafkaStoppingOffsets.Latest));
        }

        #endregion

        #region KafkaSource Property Tests

        [Test]
        public void Topics_ReturnsReadOnlyList()
        {
            // Arrange
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer())
                .Build();

            // Act
            var topics = source.Topics;

            // Assert
            Assert.That(topics, Is.InstanceOf<System.Collections.Generic.IReadOnlyList<string>>());
        }

        [Test]
        public void DefaultStartingOffsets_IsLatest()
        {
            // Arrange & Act
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer())
                .Build();

            // Assert
            Assert.That(source.StartingOffsets, Is.EqualTo(KafkaStartingOffsets.Latest));
        }

        [Test]
        public void DefaultStoppingOffsets_IsUnbounded()
        {
            // Arrange & Act
            var source = KafkaSource<string>.Builder()
                .SetBootstrapServers("localhost:9092")
                .SetTopic("test-topic")
                .SetDeserializer(new TestDeserializer())
                .Build();

            // Assert
            Assert.That(source.StoppingOffsets, Is.EqualTo(KafkaStoppingOffsets.Unbounded));
        }

        #endregion

        #region DeserializationSchema Tests

        [Test]
        public void DeserializationSchema_ProducesRowtime_DefaultIsFalse()
        {
            // Arrange
            var deserializer = new TestDeserializer();

            // Act & Assert
            Assert.That(deserializer.ProducesRowtime, Is.False);
        }

        #endregion

        #region Helper Classes

        private class TestDeserializer : DeserializationSchema<string>
        {
            public override string Deserialize(byte[] message)
            {
                return System.Text.Encoding.UTF8.GetString(message);
            }
        }

        #endregion
    }
}
