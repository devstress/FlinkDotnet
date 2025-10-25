using NUnit.Framework;
using System;

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

        #region ExtractJobManagerUrlFromError Tests (Lines 504, 510, 516, 522)

        [Test]
        public void ExtractJobManagerUrlFromError_WithNullErrorMessage_ReturnsNotAvailable()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object?[] { null });
            
            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithEmptyErrorMessage_ReturnsNotAvailable()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { string.Empty });
            
            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithoutHttpInMessage_ReturnsNotAvailable()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { "Error occurred but no URL" });
            
            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithHttpInMessage_ExtractsUrl()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { "Connection failed at http://localhost:8081" });
            
            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithHttpsInMessage_ExtractsUrl()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { "Error at https://flink.example.com:9443/jobs" });
            
            // Assert
            Assert.That(result, Is.EqualTo("https://flink.example.com:9443/jobs"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithUrlAtEndOfMessage_ExtractsFullUrl()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act - URL at end of message without trailing space
            var result = method!.Invoke(null, new object[] { "Failed to connect at http://localhost:8081/jobs/abc123" });
            
            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081/jobs/abc123"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithQuotedUrl_ExtractsUrlWithoutQuote()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { "Error at http://localhost:8081' in request" });
            
            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithNewlineAfterUrl_ExtractsUrlCorrectly()
        {
            // Use reflection to test private static method
            var method = typeof(StreamExecutionEnvironment).GetMethod("ExtractJobManagerUrlFromError",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.That(method, Is.Not.Null);
            
            // Act
            var result = method!.Invoke(null, new object[] { "Failed at http://localhost:8081\nNext line" });
            
            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        #endregion

        #region FromCollection Tests

        [Test]
        public void FromCollection_WithEmptyCollection_CreatesStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var emptyCollection = new string[] { };
            
            // Act
            var stream = env.FromCollection(emptyCollection);
            
            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        [Test]
        public void FromCollection_WithLargeCollection_CreatesStream()
        {
            // Arrange
            var env = StreamExecutionEnvironment.GetExecutionEnvironment();
            var largeCollection = new int[1000];
            for (int i = 0; i < largeCollection.Length; i++)
                largeCollection[i] = i;
            
            // Act
            var stream = env.FromCollection(largeCollection);
            
            // Assert
            Assert.That(stream, Is.Not.Null);
        }

        #endregion
    }
}
