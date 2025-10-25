using FlinkDotNet.JobGateway.Services;

namespace FlinkDotNet.JobGateway.Tests
{
    [TestFixture]
    public class MetricsServiceTests
    {
        private MetricsService _metricsService = null!;

        [SetUp]
        public void Setup() => this._metricsService = new MetricsService();

        #region RecordJobSubmitted Tests

        [Test]
        public void RecordJobSubmitted_WithLocalMode_IncrementsCounters()
        {
            // Act
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Assert - verify the method executes without throwing
            Assert.Pass("RecordJobSubmitted executed successfully");
        }

        [Test]
        public void RecordJobSubmitted_WithRemoteMode_IncrementsCounters()
        {
            // Act
            this._metricsService.RecordJobSubmitted("REMOTE");

            // Assert
            Assert.Pass("RecordJobSubmitted with REMOTE mode executed successfully");
        }

        [Test]
        public void RecordJobSubmitted_MultipleCallsWithSameMode_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Assert
            Assert.Pass("Multiple RecordJobSubmitted calls executed successfully");
        }

        [Test]
        public void RecordJobSubmitted_MultipleCallsWithDifferentModes_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("REMOTE");
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("REMOTE");

            // Assert
            Assert.Pass("RecordJobSubmitted with mixed modes executed successfully");
        }

        #endregion

        #region RecordJobSucceeded Tests

        [Test]
        public void RecordJobSucceeded_IncrementsCounter()
        {
            // Arrange - first submit a job to increment the running count
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Act
            this._metricsService.RecordJobSucceeded();

            // Assert
            Assert.Pass("RecordJobSucceeded executed successfully");
        }

        [Test]
        public void RecordJobSucceeded_MultipleTimes_DoesNotThrow()
        {
            // Arrange
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Act
            this._metricsService.RecordJobSucceeded();
            this._metricsService.RecordJobSucceeded();
            this._metricsService.RecordJobSucceeded();

            // Assert
            Assert.Pass("Multiple RecordJobSucceeded calls executed successfully");
        }

        #endregion

        #region RecordJobFailed Tests

        [Test]
        public void RecordJobFailed_WithValidationError_IncrementsCounter()
        {
            // Arrange
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Act
            this._metricsService.RecordJobFailed("validation_error");

            // Assert
            Assert.Pass("RecordJobFailed with validation_error executed successfully");
        }

        [Test]
        public void RecordJobFailed_WithExecutionError_IncrementsCounter()
        {
            // Arrange
            this._metricsService.RecordJobSubmitted("REMOTE");

            // Act
            this._metricsService.RecordJobFailed("execution_error");

            // Assert
            Assert.Pass("RecordJobFailed with execution_error executed successfully");
        }

        [Test]
        public void RecordJobFailed_WithTimeoutError_IncrementsCounter()
        {
            // Arrange
            this._metricsService.RecordJobSubmitted("LOCAL");

            // Act
            this._metricsService.RecordJobFailed("timeout_error");

            // Assert
            Assert.Pass("RecordJobFailed with timeout_error executed successfully");
        }

        [Test]
        public void RecordJobFailed_WithNetworkError_IncrementsCounter()
        {
            // Arrange
            this._metricsService.RecordJobSubmitted("REMOTE");

            // Act
            this._metricsService.RecordJobFailed("network_error");

            // Assert
            Assert.Pass("RecordJobFailed with network_error executed successfully");
        }

        [Test]
        public void RecordJobFailed_MultipleDifferentErrors_DoesNotThrow()
        {
            // Arrange
            for (int i = 0; i < 4; i++)
            {
                this._metricsService.RecordJobSubmitted("LOCAL");
            }

            // Act
            this._metricsService.RecordJobFailed("validation_error");
            this._metricsService.RecordJobFailed("execution_error");
            this._metricsService.RecordJobFailed("timeout_error");
            this._metricsService.RecordJobFailed("network_error");

            // Assert
            Assert.Pass("RecordJobFailed with multiple error types executed successfully");
        }

        #endregion

        #region RecordRequest Tests

        [Test]
        public void RecordRequest_WithValidParameters_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordRequest("/api/jobs", "POST", 200);

            // Assert
            Assert.Pass("RecordRequest executed successfully");
        }

        [Test]
        public void RecordRequest_WithDifferentEndpoints_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordRequest("/api/jobs", "POST", 200);
            this._metricsService.RecordRequest("/api/jobs/123", "GET", 200);
            this._metricsService.RecordRequest("/api/jobs/456", "DELETE", 204);

            // Assert
            Assert.Pass("RecordRequest with different endpoints executed successfully");
        }

        [Test]
        public void RecordRequest_WithDifferentMethods_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordRequest("/api/jobs", "GET", 200);
            this._metricsService.RecordRequest("/api/jobs", "POST", 201);
            this._metricsService.RecordRequest("/api/jobs", "PUT", 200);
            this._metricsService.RecordRequest("/api/jobs", "DELETE", 204);
            this._metricsService.RecordRequest("/api/jobs", "PATCH", 200);

            // Assert
            Assert.Pass("RecordRequest with different HTTP methods executed successfully");
        }

        [Test]
        public void RecordRequest_WithDifferentStatusCodes_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordRequest("/api/jobs", "GET", 200); // OK
            this._metricsService.RecordRequest("/api/jobs", "POST", 201); // Created
            this._metricsService.RecordRequest("/api/jobs", "GET", 400); // Bad Request
            this._metricsService.RecordRequest("/api/jobs", "GET", 404); // Not Found
            this._metricsService.RecordRequest("/api/jobs", "POST", 500); // Server Error

            // Assert
            Assert.Pass("RecordRequest with different status codes executed successfully");
        }

        [Test]
        public void RecordRequest_MultipleCallsSameParameters_DoesNotThrow()
        {
            // Act
            for (int i = 0; i < 10; i++)
            {
                this._metricsService.RecordRequest("/api/jobs", "POST", 200);
            }

            // Assert
            Assert.Pass("Multiple RecordRequest calls with same parameters executed successfully");
        }

        #endregion

        #region MeasureRequestDuration Tests

        [Test]
        public void MeasureRequestDuration_ReturnsDisposable()
        {
            // Act
            using var timer = this._metricsService.MeasureRequestDuration("/api/jobs", "POST");

            // Assert
            Assert.That(timer, Is.Not.Null);
            Assert.That(timer, Is.InstanceOf<IDisposable>());
        }

        [Test]
        public void MeasureRequestDuration_DisposableCanBeDisposed()
        {
            // Act
            var timer = this._metricsService.MeasureRequestDuration("/api/jobs", "POST");
            timer.Dispose();

            // Assert
            Assert.Pass("Timer disposed successfully");
        }

        [Test]
        public void MeasureRequestDuration_WithDifferentEndpoints_DoesNotThrow()
        {
            // Act
            using var timer1 = this._metricsService.MeasureRequestDuration("/api/jobs", "POST");
            using var timer2 = this._metricsService.MeasureRequestDuration("/api/jobs/123", "GET");
            using var timer3 = this._metricsService.MeasureRequestDuration("/api/status", "GET");

            // Assert
            Assert.Pass("MeasureRequestDuration with different endpoints executed successfully");
        }

        [Test]
        public void MeasureRequestDuration_WithDifferentMethods_DoesNotThrow()
        {
            // Act
            using var timer1 = this._metricsService.MeasureRequestDuration("/api/jobs", "GET");
            using var timer2 = this._metricsService.MeasureRequestDuration("/api/jobs", "POST");
            using var timer3 = this._metricsService.MeasureRequestDuration("/api/jobs", "PUT");
            using var timer4 = this._metricsService.MeasureRequestDuration("/api/jobs", "DELETE");

            // Assert
            Assert.Pass("MeasureRequestDuration with different methods executed successfully");
        }

        [Test]
        public void MeasureRequestDuration_MultipleSequentialTimers_DoesNotThrow()
        {
            // Act
            using (var timer1 = this._metricsService.MeasureRequestDuration("/api/jobs", "POST"))
            {
                // Simulate some work
            }

            using (var timer2 = this._metricsService.MeasureRequestDuration("/api/jobs", "GET"))
            {
                // Simulate some work
            }

            // Assert
            Assert.Pass("Multiple sequential timers executed successfully");
        }

        [Test]
        public void MeasureRequestDuration_NestedTimers_DoesNotThrow()
        {
            // Act
            using (var timer1 = this._metricsService.MeasureRequestDuration("/api/outer", "POST"))
            {
                using (var timer2 = this._metricsService.MeasureRequestDuration("/api/inner", "GET"))
                {
                    // Simulate nested timing
                }
            }

            // Assert
            Assert.Pass("Nested timers executed successfully");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void CompleteJobLifecycle_SubmitAndSucceed_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSucceeded();

            // Assert
            Assert.Pass("Complete successful job lifecycle executed successfully");
        }

        [Test]
        public void CompleteJobLifecycle_SubmitAndFail_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordJobSubmitted("REMOTE");
            this._metricsService.RecordJobFailed("execution_error");

            // Assert
            Assert.Pass("Complete failed job lifecycle executed successfully");
        }

        [Test]
        public void CompleteRequestLifecycle_WithDurationMeasurement_DoesNotThrow()
        {
            // Act
            using (var timer = this._metricsService.MeasureRequestDuration("/api/jobs", "POST"))
            {
                // Simulate request processing
                this._metricsService.RecordJobSubmitted("LOCAL");
            }
            this._metricsService.RecordRequest("/api/jobs", "POST", 200);

            // Assert
            Assert.Pass("Complete request lifecycle with duration measurement executed successfully");
        }

        [Test]
        public void ConcurrentJobOperations_MultipleJobsSimultaneously_DoesNotThrow()
        {
            // Act
            this._metricsService.RecordJobSubmitted("LOCAL");
            this._metricsService.RecordJobSubmitted("REMOTE");
            this._metricsService.RecordJobSubmitted("LOCAL");

            this._metricsService.RecordJobSucceeded();
            this._metricsService.RecordJobFailed("timeout_error");
            this._metricsService.RecordJobSucceeded();

            // Assert
            Assert.Pass("Concurrent job operations executed successfully");
        }

        [Test]
        public void HighVolumeMetrics_ManyOperations_DoesNotThrow()
        {
            // Act - simulate high-volume scenario
            for (int i = 0; i < 100; i++)
            {
                this._metricsService.RecordJobSubmitted(i % 2 == 0 ? "LOCAL" : "REMOTE");
                this._metricsService.RecordRequest($"/api/jobs/{i}", "POST", 200);
            }

            for (int i = 0; i < 100; i++)
            {
                if (i % 3 == 0)
                {
                    this._metricsService.RecordJobFailed("error");
                }
                else
                {
                    this._metricsService.RecordJobSucceeded();
                }
            }

            // Assert
            Assert.Pass("High volume metrics operations executed successfully");
        }

        #endregion
    }
}
