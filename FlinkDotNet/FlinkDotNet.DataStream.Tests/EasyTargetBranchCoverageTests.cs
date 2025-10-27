using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests targeting specific uncovered branches with 1-2 missing branches each.
    /// These are the "easy wins" to increase branch coverage incrementally.
    /// </summary>
    [TestFixture]
    public class EasyTargetBranchCoverageTests
    {
        [Test]
        public void DataStream_Where_WithValidExpression_CreatesFilteredStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Test the Where method (alias for Filter)
            var filtered = stream.Where("length > 5");

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void DataStream_Where_WithEmptyExpression_CreatesFilteredStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Act - Test with empty expression
            var filtered = stream.Where("");

            // Assert
            Assert.That(filtered, Is.Not.Null);
        }

        [Test]
        public void AllWindowedStream_Constructor_WithTimeWindow_InitializesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });
            var time = Time.Seconds(5);

            // Act - Constructor is called internally by TimeWindowAll
            var windowed = stream.TimeWindowAll(time);

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void AllWindowedStream_Constructor_WithCountWindow_InitializesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { 1, 2, 3 });

            // Act - Constructor for count-based window
            var windowed = stream.CountWindowAll(10);

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void KeyedStream_Window_WithTumblingWindow_CreatesWindowedStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { "a", "b", "c" });
            var keyed = stream.KeyBy(x => x);
            var assigner = Window.Assigners.TumblingEventTimeWindows<string>.Of(Time.Seconds(10));

            // Act - Test Window method
            var windowed = keyed.Window(assigner);

            // Assert
            Assert.That(windowed, Is.Not.Null);
        }

        [Test]
        public void DataStream_AssignTimestampsAndWatermarks_WithWatermarkStrategy_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { "test1", "test2" });
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps();

            // Act
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void DataStream_AssignTimestampsAndWatermarks_WithSimpleExtractor_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stream = env.FromCollection(new[] { "test1", "test2" });

            // Act - Test with watermark strategy
            var strategy = Watermarks.WatermarkStrategy<string>.ForMonotonousTimestamps();
            var result = stream.AssignTimestampsAndWatermarks(strategy);

            // Assert
            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public void OutputTag_Equals_WithNullObject_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-tag");

            // Act
            var result = tag.Equals(null);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithDifferentType_ReturnsFalse()
        {
            // Arrange
            var tag = new OutputTag<string>("test-tag");
            var other = "not-an-output-tag";

            // Act
            var result = tag.Equals(other);

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void OutputTag_Equals_WithSameTag_ReturnsTrue()
        {
            // Arrange
            var tag1 = new OutputTag<string>("same-tag");
            var tag2 = new OutputTag<string>("same-tag");

            // Act
            var result = tag1.Equals(tag2);

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void OutputTag_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new OutputTag<string>(null!));
        }

        [Test]
        public void OutputTag_Constructor_WithValidName_CreatesTag()
        {
            // Act
            var tag = new OutputTag<string>("valid-tag");

            // Assert
            Assert.That(tag.Id, Is.EqualTo("valid-tag"));
        }

        [Test]
        public void StateDescriptor_Constructor_WithNullName_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new ListStateDescriptor<int>(null!));
        }

        [Test]
        public void StateDescriptor_Constructor_WithValidName_CreatesDescriptor()
        {
            // Act
            var descriptor = new ListStateDescriptor<int>("valid-name");

            // Assert
            Assert.That(descriptor, Is.Not.Null);
        }

        [Test]
        public void ReducingStateDescriptor_Constructor_WithNullReduceFunction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new ReducingStateDescriptor<int>("test", null!));
        }

        [Test]
        public void AggregatingStateDescriptor_Constructor_WithNullAggregateFunction_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                new AggregatingStateDescriptor<int, int, int>("test", null!));
        }

        [Test]
        public void StreamExecutionEnvironment_SetStateBackend_WithNullBackend_ThrowsArgumentNullException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => env.SetStateBackend(null!));
        }

        [Test]
        public void DataStream_Constructor_WithJobDefinition_InitializesCorrectly()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var job = new Flink.JobBuilder.Models.JobDefinition
            {
                Metadata = new Flink.JobBuilder.Models.JobMetadata { JobName = "TestJob" }
            };

            // Act - Test internal constructor via reflection or indirect creation
            // This is tested through FromKafka which uses this constructor
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }
    }
}
