using System.Net;
using System.Reflection;
using FlinkDotNet.JobGateway.Services;

namespace FlinkDotNet.JobGateway.Tests
{
    /// <summary>
    /// Comprehensive tests for FlinkJobManager.TryGetJobIdFromHeaders to achieve 100% branch coverage.
    /// Tests all code paths for extracting Job IDs from HTTP response headers.
    /// </summary>
    [TestFixture]
    public class FlinkJobManagerTryGetJobIdFromHeadersTests
    {
        private static string? CallTryGetJobIdFromHeaders(HttpResponseMessage response)
        {
            var method = typeof(FlinkJobManager).GetMethod("TryGetJobIdFromHeaders",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find TryGetJobIdFromHeaders method");
            }

            return (string?) method.Invoke(null, new object[] { response });
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationHeader_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs/abc123def456") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("abc123def456"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationHeaderAndQuery_ExtractsJobIdWithoutQuery()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs/xyz789?mode=detached") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("xyz789"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationHeaderTrailingSlash_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs/test123/") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("test123"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationStringValue_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Location", "http://localhost:8081/jobs/string123");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("string123"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationEndingInJobs_ReturnsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithLocationEndingInJobsSlash_ReturnsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs/") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithXFlinkJobIDHeader_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("X-Flink-JobID", "header-job-id-123");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("header-job-id-123"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithXFlinkJobIdHeader_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("X-Flink-Job-Id", "header-job-id-456");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("header-job-id-456"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithFlinkJobIdHeader_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Flink-Job-Id", "flink-job-789");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("flink-job-789"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithFlinkJobIdHeaderCaps_ExtractsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Flink-JobId", "flink-jobid-abc");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("flink-jobid-abc"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithHeaderHavingWhitespace_TrimsJobId()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("X-Flink-JobID", "  trimmed-id-123  ");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("trimmed-id-123"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithNoHeaders_ReturnsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithEmptyLocationString_ReturnsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Location", "");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithWhitespaceLocationString_ReturnsEncodedValue()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Location", "   ");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Does.Contain("%20"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithEmptyHeaderValue_ReturnsNull()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("X-Flink-JobID", "");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void TryGetJobIdFromHeaders_LocationPriority_PrefersLocationOverCustomHeaders()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/jobs/location-id") }
            };
            _ = response.Headers.TryAddWithoutValidation("X-Flink-JobID", "header-id");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("location-id"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_MultipleLocationValues_UsesFirstValid()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            _ = response.Headers.TryAddWithoutValidation("Location", "http://localhost:8081/jobs");
            _ = response.Headers.TryAddWithoutValidation("Location", "http://localhost:8081/jobs/valid-id");

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("valid-id"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_ComplexPath_ExtractsLastSegment()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081/api/v1/jobs/complex-job-id-123") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("complex-job-id-123"));
        }

        [Test]
        public void TryGetJobIdFromHeaders_WithMultipleSlashes_HandlesCorrectly()
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Headers = { Location = new Uri("http://localhost:8081///jobs///multi-slash-id///") }
            };

            var result = CallTryGetJobIdFromHeaders(response);

            Assert.That(result, Is.EqualTo("multi-slash-id"));
        }
    }
}
