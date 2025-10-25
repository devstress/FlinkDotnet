using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests
{
    /// <summary>
    /// Comprehensive tests for JobDefinitionValidator.ValidateRetryOperation to achieve 100% branch coverage.
    /// Tests all validation rules for retry operations.
    /// </summary>
    [TestFixture]
    public class JobDefinitionValidatorRetryOperationTests
    {
        [Test]
        public void ValidateRetryOperation_WithValidRetry_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000, 2000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        public void ValidateRetryOperation_WithNegativeMaxRetries_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = -1,
                        DelayMs = new List<long> { 1000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("maxRetries must be between 0 and 100"));
        }

        [Test]
        public void ValidateRetryOperation_WithMaxRetriesOver100_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 101,
                        DelayMs = new List<long> { 1000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("maxRetries must be between 0 and 100"));
        }

        [Test]
        public void ValidateRetryOperation_WithMaxRetriesExactly100_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 100,
                        DelayMs = new List<long> { 1000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateRetryOperation_WithMaxRetriesZero_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 0,
                        DelayMs = new List<long> { 1000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateRetryOperation_WithNullDelayMs_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = null!,
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs must contain at least 1 value"));
        }

        [Test]
        public void ValidateRetryOperation_WithEmptyDelayMs_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long>(),
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs must contain at least 1 value"));
        }

        [Test]
        public void ValidateRetryOperation_WithZeroDelay_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 0 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs values must be > 0"));
        }

        [Test]
        public void ValidateRetryOperation_WithNegativeDelay_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000, -500 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs values must be > 0"));
        }

        [Test]
        public void ValidateRetryOperation_WithMixedValidAndInvalidDelays_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000, 0, 2000 },
                        StateKey = "retry-state"
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs values must be > 0"));
        }

        [Test]
        public void ValidateRetryOperation_WithNullStateKey_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000 },
                        StateKey = null!
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateKey is required"));
        }

        [Test]
        public void ValidateRetryOperation_WithEmptyStateKey_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000 },
                        StateKey = ""
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateKey is required"));
        }

        [Test]
        public void ValidateRetryOperation_WithWhitespaceStateKey_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = 5,
                        DelayMs = new List<long> { 1000 },
                        StateKey = "   "
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateKey is required"));
        }

        [Test]
        public void ValidateRetryOperation_WithMultipleErrors_AddsAllErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new RetryOperationDefinition
                    {
                        MaxRetries = -1,
                        DelayMs = new List<long> { 0 },
                        StateKey = ""
                    }
                }
            };

            
            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors.Count, Is.GreaterThanOrEqualTo(3));
            Assert.That(result.Errors, Has.Some.Contains("maxRetries"));
            Assert.That(result.Errors, Has.Some.Contains("delayMs"));
            Assert.That(result.Errors, Has.Some.Contains("stateKey"));
        }
    }
}
