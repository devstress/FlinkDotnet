using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    [TestFixture]
    public class KafkaSourceFunctionTests
    {
        #region Constructor Tests

        [Test]
        public void Constructor_WithValidParameters_CreatesInstance()
        {
            // Arrange
            var topic = "test-topic";
            var servers = "localhost:9092";
            var groupId = "test-group";
            Func<string, string> deserializer = s => s.ToUpper();
            var offsets = "earliest";

            // Act
            var source = new KafkaSourceFunction<string>(topic, servers, groupId, deserializer, offsets);

            // Assert
            Assert.That(source, Is.Not.Null);
            Assert.That(source.Topic, Is.EqualTo(topic));
            Assert.That(source.BootstrapServers, Is.EqualTo(servers));
            Assert.That(source.GroupId, Is.EqualTo(groupId));
            Assert.That(source.StartingOffsets, Is.EqualTo(offsets));
        }

        [Test]
        public void Constructor_WithNullTopic_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, string> deserializer = s => s;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new KafkaSourceFunction<string>(null!, "localhost:9092", "group", deserializer, "earliest"));
            Assert.That(ex!.ParamName, Is.EqualTo("topic"));
        }

        [Test]
        public void Constructor_WithNullBootstrapServers_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, string> deserializer = s => s;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new KafkaSourceFunction<string>("topic", null!, "group", deserializer, "earliest"));
            Assert.That(ex!.ParamName, Is.EqualTo("bootstrapServers"));
        }

        [Test]
        public void Constructor_WithNullGroupId_ThrowsArgumentNullException()
        {
            // Arrange
            Func<string, string> deserializer = s => s;

            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new KafkaSourceFunction<string>("topic", "localhost:9092", null!, deserializer, "earliest"));
            Assert.That(ex!.ParamName, Is.EqualTo("groupId"));
        }

        [Test]
        public void Constructor_WithNullDeserializer_ThrowsArgumentNullException()
        {
            // Act & Assert
            var ex = Assert.Throws<ArgumentNullException>(() =>
                new KafkaSourceFunction<string>("topic", "localhost:9092", "group", null!, "earliest"));
            Assert.That(ex!.ParamName, Is.EqualTo("deserializer"));
        }

        #endregion

        #region Property Tests

        [Test]
        public void Properties_ReturnConstructorValues()
        {
            // Arrange
            var topic = "my-topic";
            var servers = "broker1:9092,broker2:9092";
            var groupId = "consumer-group-1";
            var offsets = "latest";
            Func<string, int> deserializer = s => s.Length;

            // Act
            var source = new KafkaSourceFunction<int>(topic, servers, groupId, deserializer, offsets);

            // Assert
            Assert.That(source.Topic, Is.EqualTo(topic));
            Assert.That(source.BootstrapServers, Is.EqualTo(servers));
            Assert.That(source.GroupId, Is.EqualTo(groupId));
            Assert.That(source.StartingOffsets, Is.EqualTo(offsets));
        }

        [Test]
        public void Properties_AreReadOnly()
        {
            // Arrange
            Func<string, string> deserializer = s => s;
            _ = new KafkaSourceFunction<string>("topic", "servers", "group", deserializer, "earliest");

            // Act & Assert - Verify properties are get-only
            var topicProp = typeof(KafkaSourceFunction<string>).GetProperty("Topic");
            var serversProp = typeof(KafkaSourceFunction<string>).GetProperty("BootstrapServers");
            var groupProp = typeof(KafkaSourceFunction<string>).GetProperty("GroupId");
            var offsetsProp = typeof(KafkaSourceFunction<string>).GetProperty("StartingOffsets");

            Assert.That(topicProp!.CanWrite, Is.False);
            Assert.That(serversProp!.CanWrite, Is.False);
            Assert.That(groupProp!.CanWrite, Is.False);
            Assert.That(offsetsProp!.CanWrite, Is.False);
        }

        #endregion

        #region RunAsync Tests

        [Test]
        public async Task RunAsync_ReturnsEmptySequence()
        {
            // Arrange
            Func<string, string> deserializer = s => s;
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", deserializer, "earliest");

            // Act
            var results = new System.Collections.Generic.List<string>();
            await foreach (var item in source.RunAsync(CancellationToken.None))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.Empty);
        }

        [Test]
        public async Task RunAsync_WithCancellationToken_ReturnsEmptySequence()
        {
            // Arrange
            Func<string, string> deserializer = s => s;
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", deserializer, "earliest");
            using var cts = new CancellationTokenSource();
            cts.CancelAfter(TimeSpan.FromMilliseconds(100));

            // Act
            var results = new System.Collections.Generic.List<string>();
            await foreach (var item in source.RunAsync(cts.Token))
            {
                results.Add(item);
            }

            // Assert
            Assert.That(results, Is.Empty);
        }

        #endregion

        #region Type Parameter Tests

        [Test]
        public void KafkaSourceFunction_SupportsStringType()
        {
            // Arrange
            Func<string, string> deserializer = s => s;

            // Act
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", deserializer, "earliest");

            // Assert
            Assert.That(source, Is.InstanceOf<ISourceFunction<string>>());
        }

        [Test]
        public void KafkaSourceFunction_SupportsIntType()
        {
            // Arrange
            Func<string, int> deserializer = s => int.Parse(s);

            // Act
            var source = new KafkaSourceFunction<int>("topic", "localhost:9092", "group", deserializer, "earliest");

            // Assert
            Assert.That(source, Is.InstanceOf<ISourceFunction<int>>());
        }

        [Test]
        public void KafkaSourceFunction_SupportsComplexType()
        {
            // Arrange
            Func<string, TestMessage> deserializer = s => new TestMessage { Value = s };

            // Act
            var source = new KafkaSourceFunction<TestMessage>("topic", "localhost:9092", "group", deserializer, "earliest");

            // Assert
            Assert.That(source, Is.InstanceOf<ISourceFunction<TestMessage>>());
        }

        private class TestMessage
        {
            public string Value { get; set; } = string.Empty;
        }

        #endregion

        #region Starting Offsets Tests

        [Test]
        public void StartingOffsets_CanBeEarliest()
        {
            // Arrange & Act
            Func<string, string> deserializer = s => s;
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", deserializer, "earliest");

            // Assert
            Assert.That(source.StartingOffsets, Is.EqualTo("earliest"));
        }

        [Test]
        public void StartingOffsets_CanBeLatest()
        {
            // Arrange & Act
            Func<string, string> deserializer = s => s;
            var source = new KafkaSourceFunction<string>("topic", "localhost:9092", "group", deserializer, "latest");

            // Assert
            Assert.That(source.StartingOffsets, Is.EqualTo("latest"));
        }

        #endregion
    }
}
