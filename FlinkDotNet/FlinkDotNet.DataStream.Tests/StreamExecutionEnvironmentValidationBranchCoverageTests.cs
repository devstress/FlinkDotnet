using System;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for StreamExecutionEnvironment validation methods to achieve branch coverage.
    /// Focuses on input validation branches.
    /// </summary>
    [TestFixture]
    public class StreamExecutionEnvironmentValidationBranchCoverageTests
    {
        [Test]
        public void SetMaxParallelism_WithValidValue_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act - Valid value in range [1, 32768]
            var result = env.SetMaxParallelism(100);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void SetMaxParallelism_WithMinimumValidValue_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act - Minimum valid value
            var result = env.SetMaxParallelism(1);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void SetMaxParallelism_WithMaximumValidValue_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act - Maximum valid value
            var result = env.SetMaxParallelism(32768);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void SetMaxParallelism_WithZero_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Zero is invalid (tests maxParallelism <= 0 branch)
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(0));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithNegativeValue_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Negative value is invalid (tests maxParallelism <= 0 branch)
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(-1));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithValueAboveMaximum_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Value above 32768 is invalid (tests maxParallelism > 32768 branch)
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(32769));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetMaxParallelism_WithLargeInvalidValue_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Large value is invalid (tests maxParallelism > 32768 branch)
            var ex = Assert.Throws<ArgumentException>(() => env.SetMaxParallelism(100000));
            Assert.That(ex!.Message, Does.Contain("Max parallelism must be between 1 and 32768"));
        }

        [Test]
        public void SetStateBackend_WithValidStateBackend_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stateBackend = new FlinkDotNet.DataStream.State.HashMapStateBackend();

            // Act - Test SetStateBackend branch
            var result = env.SetStateBackend(stateBackend);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void SetStateBackend_WithRocksDB_Succeeds()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var stateBackend = new FlinkDotNet.DataStream.State.EmbeddedRocksDBStateBackend();

            // Act
            var result = env.SetStateBackend(stateBackend);

            // Assert
            Assert.That(result, Is.SameAs(env));
        }

        [Test]
        public void FromKafka_WithNullBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Null bootstrap servers should throw (tests bootstrapServers null check)
            var ex = Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", null, "test-group"));
            Assert.That(ex!.Message, Does.Contain("Kafka bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithEmptyBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Empty bootstrap servers should throw (tests string.IsNullOrWhiteSpace)
            var ex = Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", "", "test-group"));
            Assert.That(ex!.Message, Does.Contain("Kafka bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithWhitespaceBootstrapServers_ThrowsArgumentException()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act & Assert - Whitespace bootstrap servers should throw
            var ex = Assert.Throws<ArgumentException>(() => env.FromKafka("test-topic", "   ", "test-group"));
            Assert.That(ex!.Message, Does.Contain("Kafka bootstrap servers"));
        }

        [Test]
        public void FromKafka_WithValidParameters_CreatesDataStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();

            // Act - Valid parameters should succeed
            var stream = env.FromKafka("test-topic", "localhost:9092", "test-group");

            // Assert
            Assert.That(stream, Is.Not.Null);
        }
    }
}
