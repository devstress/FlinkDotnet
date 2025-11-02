using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests
{
    /// <summary>
    /// Comprehensive tests for JobDefinitionValidator state and timer operations to achieve 100% branch coverage.
    /// </summary>
    [TestFixture]
    public class JobDefinitionValidatorStateAndTimerTests
    {
        #region ValidateStateOperation Tests

        [Test]
        public void ValidateStateOperation_WithValidValueState_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateStateOperation_WithValidListState_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "list",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateStateOperation_WithValidMapState_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "map",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateStateOperation_WithValidReducingState_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "reducing",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateStateOperation_WithNullStateType_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = null!,
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateType must be one of"));
        }

        [Test]
        public void ValidateStateOperation_WithEmptyStateType_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateType must be one of"));
        }

        [Test]
        public void ValidateStateOperation_WithInvalidStateType_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "invalid",
                        StateKey = "my-state"
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateType must be one of"));
        }

        [Test]
        public void ValidateStateOperation_WithNullStateKey_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = null!
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateKey is required"));
        }

        [Test]
        public void ValidateStateOperation_WithEmptyStateKey_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = ""
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("stateKey is required"));
        }

        [Test]
        public void ValidateStateOperation_WithValidTtl_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = "my-state",
                        TtlMs = 60000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateStateOperation_WithZeroTtl_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = "my-state",
                        TtlMs = 0
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("ttlMs must be > 0"));
        }

        [Test]
        public void ValidateStateOperation_WithNegativeTtl_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new StateOperationDefinition
                    {
                        StateType = "value",
                        StateKey = "my-state",
                        TtlMs = -1000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("ttlMs must be > 0"));
        }

        #endregion

        #region ValidateTimerOperation Tests

        [Test]
        public void ValidateTimerOperation_WithValidProcessingTimer_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = 5000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateTimerOperation_WithValidEventTimer_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "event",
                        DelayMs = 10000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateTimerOperation_WithNullTimerType_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = null!,
                        DelayMs = 5000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("timerType must be one of"));
        }

        [Test]
        public void ValidateTimerOperation_WithInvalidTimerType_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "invalid",
                        DelayMs = 5000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("timerType must be one of"));
        }

        [Test]
        public void ValidateTimerOperation_WithZeroDelay_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = 0
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs must be between 1 and 86400000"));
        }

        [Test]
        public void ValidateTimerOperation_WithNegativeDelay_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = -1000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs must be between 1 and 86400000"));
        }

        [Test]
        public void ValidateTimerOperation_WithDelayOver86400000_AddsError()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = 86_400_001
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("delayMs must be between 1 and 86400000"));
        }

        [Test]
        public void ValidateTimerOperation_WithDelayExactly86400000_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = 86_400_000
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateTimerOperation_WithDelayExactly1_NoErrors()
        {
            var jobDef = new JobDefinition
            {
                Metadata = new JobMetadata { Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } },
                Operations = new List<IOperationDefinition>
                {
                    new TimerOperationDefinition
                    {
                        TimerType = "processing",
                        DelayMs = 1
                    }
                }
            };

            var result = JobDefinitionValidator.Validate(jobDef);

            Assert.That(result.IsValid, Is.True);
        }

        #endregion
    }
}
