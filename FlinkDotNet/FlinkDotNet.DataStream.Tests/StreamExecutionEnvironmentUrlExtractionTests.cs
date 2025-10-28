using System;
using System.Reflection;
using NUnit.Framework;

namespace FlinkDotNet.DataStream.Tests
{
    /// <summary>
    /// Tests for StreamExecutionEnvironment's private ExtractJobManagerUrlFromError method.
    /// Uses reflection to test this private method for complete code coverage.
    /// </summary>
    [TestFixture]
    public class StreamExecutionEnvironmentUrlExtractionTests
    {
        private MethodInfo _extractUrlMethod;

        [SetUp]
        public void Setup()
        {
            // Get the private static method using reflection
            var type = typeof(StreamExecutionEnvironment);
            _extractUrlMethod = type.GetMethod("ExtractJobManagerUrlFromError",
                BindingFlags.NonPublic | BindingFlags.Static);
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithNullErrorMessage_ReturnsNotAvailable()
        {
            // Arrange
            string errorMessage = null;

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithEmptyErrorMessage_ReturnsNotAvailable()
        {
            // Arrange
            string errorMessage = string.Empty;

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithNoHttpInMessage_ReturnsNotAvailable()
        {
            // Arrange
            string errorMessage = "Connection failed without any URL information";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithValidHttpUrl_ExtractsUrl()
        {
            // Arrange
            string errorMessage = "Failed to connect at http://localhost:8081/jobs";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081/jobs"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithValidHttpsUrl_ExtractsUrl()
        {
            // Arrange
            string errorMessage = "Connection timeout at https://flink-jobmanager:8443/api/v1";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("https://flink-jobmanager:8443/api/v1"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithUrlFollowedBySpace_ExtractsCorrectUrl()
        {
            // Arrange
            string errorMessage = "Error at http://localhost:8081 with message failed";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithUrlFollowedByNewline_ExtractsCorrectUrl()
        {
            // Arrange
            string errorMessage = "Error at http://localhost:8081\nNext line";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithUrlAfterQuote_ReturnsNotAvailable()
        {
            // Arrange - URL comes after "at http" but message doesn't contain "at http"
            string errorMessage = "Failed \"http://localhost:8081\" connection";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert - Should return not available because no "at http" prefix
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithUrlAtEnd_ExtractsFullUrl()
        {
            // Arrange
            string errorMessage = "Connection failed at http://localhost:8081";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert - Should extract URL to end of string
            Assert.That(result, Is.EqualTo("http://localhost:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithMultipleUrls_ExtractsFirstUrlUntilSpace()
        {
            // Arrange
            string errorMessage = "Tried at http://first:8081 then http://second:8082";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert - Should extract first URL until space
            Assert.That(result, Is.EqualTo("http://first:8081"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithHttpButNoUrl_ReturnsNotAvailable()
        {
            // Arrange - "http" exists but not "at http"
            string errorMessage = "http protocol failed but no URL";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert - Returns not available since no "at http" prefix
            Assert.That(result, Is.EqualTo("(not available in error message)"));
        }

        [Test]
        public void ExtractJobManagerUrlFromError_WithAtHttpButNoActualUrl_ExtractsHttp()
        {
            // Arrange - "at http" is found and "http" after it is extracted
            string errorMessage = "Error at http";

            // Act
            var result = _extractUrlMethod?.Invoke(null, new object[] { errorMessage }) as string;

            // Assert - Extracts "http" to end of string
            Assert.That(result, Is.EqualTo("http"));
        }
    }
}
