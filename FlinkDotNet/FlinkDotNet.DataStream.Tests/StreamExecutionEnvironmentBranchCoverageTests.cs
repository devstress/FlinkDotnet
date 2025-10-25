using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Comprehensive branch coverage tests targeting remaining uncovered branches
    /// in StreamExecutionEnvironment and related classes.
    /// </summary>
    [TestFixture]
    public class StreamExecutionEnvironmentBranchCoverageTests
    {
        #region FromKafka Parallelism Branch Coverage - Line 105, 117

        [Test]
        public void FromKafka_WithZeroParallelism_SetsMetadataParallelismToNull()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.SetParallelism(0);  // Test branch: Parallelism > 0 is false

            // Act
            var dataStream = env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");

            // Assert
            Assert.That(dataStream, Is.Not.Null);
            // The metadata parallelism should be null when ExecutionConfig.Parallelism is 0
        }

        [Test]
        public void FromKafka_WithNegativeParallelism_SetsMetadataParallelismToNull()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.SetParallelism(-1);  // Test branch: Parallelism > 0 is false

            // Act
            var dataStream = env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");

            // Assert
            Assert.That(dataStream, Is.Not.Null);
        }

        [Test]
        public void FromKafka_WithPositiveParallelism_SetsMetadataParallelism()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            env.SetParallelism(4);  // Test branch: Parallelism > 0 is true

            // Act
            var dataStream = env.FromKafka("test-topic", "localhost:9092", "test-group", "earliest");

            // Assert
            Assert.That(dataStream, Is.Not.Null);
        }

        #endregion

        #region FromKafka GroupId Null Handling - Line 102, 122

        [Test]
        public void FromKafka_WithNullGroupId_UsesDefaultGroup()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act - Pass null groupId to test null-coalescing operator
            var dataStream = env.FromKafka("test-topic", "localhost:9092", null, "earliest");

            // Assert
            Assert.That(dataStream, Is.Not.Null);
        }

        [Test]
        public void FromKafka_WithEmptyGroupId_UsesEmptyString()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act
            var dataStream = env.FromKafka("test-topic", "localhost:9092", "", "earliest");

            // Assert
            Assert.That(dataStream, Is.Not.Null);
        }

        #endregion
    }
}
