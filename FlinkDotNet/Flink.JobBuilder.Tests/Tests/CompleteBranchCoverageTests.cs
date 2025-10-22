using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using NUnit.Framework;
using System.Collections.Generic;

namespace Flink.JobBuilder.Tests.Tests
{
    [TestFixture]
    public class CompleteBranchCoverageTests
    {
        [Test]
        public void ValidateGroupByOperation_WithKeysCollection_ValidatesCorrectly()
        {
            // Test case where Key is empty but Keys collection has values
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new GroupByOperationDefinition
                    {
                        Key = "",  // Empty Key
                        Keys = new List<string> { "field1", "field2" }  // But Keys collection has values
                    }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            // Should be valid because Keys collection is not empty
            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateGroupByOperation_WithEmptyKeysCollection_ReturnsError()
        {
            // Test case where both Key is empty and Keys collection is empty
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new GroupByOperationDefinition
                    {
                        Key = "",  // Empty Key
                        Keys = new List<string>()  // Empty collection
                    }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("operations[0].groupBy.key or keys is required"));
        }

        [Test]
        public void ValidateWindowOperation_WithSlidingWindow_ValidatesSlide()
        {
            // Test SLIDING window with valid slide value
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new WindowOperationDefinition
                    {
                        WindowType = "SLIDING",
                        Size = 100,
                        TimeUnit = "SECONDS",
                        Slide = 50  // Valid slide value
                    }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateWindowOperation_WithSlidingWindowNoSlide_ReturnsError()
        {
            // Test SLIDING window without slide value
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new WindowOperationDefinition
                    {
                        WindowType = "SLIDING",
                        Size = 100,
                        TimeUnit = "SECONDS"
                        // Slide not set
                    }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("slide is required"));
        }

        [Test]
        public void ValidateWindowOperation_WithSlidingWindowZeroSlide_ReturnsError()
        {
            // Test SLIDING window with zero slide value
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new WindowOperationDefinition
                    {
                        WindowType = "SLIDING",
                        Size = 100,
                        TimeUnit = "SECONDS",
                        Slide = 0  // Invalid: zero
                    }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Has.Some.Contains("slide is required"));
        }

        [Test]
        public void ValidateSqlSource_SwitchCase_Validates()
        {
            // Test to ensure SqlSourceDefinition case in switch is covered
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new SqlSourceDefinition { Statements = new List<string> { "SELECT 1" } }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateFilterOperation_SwitchCase_Validates()
        {
            // Test to ensure FilterOperationDefinition case in switch is covered
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Operations = new List<IOperationDefinition>
                {
                    new FilterOperationDefinition { Expression = "x => x > 0" }
                },
                Sink = new KafkaSinkDefinition { Topic = "output" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateKafkaSink_SwitchCase_Validates()
        {
            // Test to ensure KafkaSinkDefinition case in switch is covered
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Sink = new KafkaSinkDefinition { Topic = "output", Serializer = "json" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.True);
        }

        [Test]
        public void ValidateFileSink_MissingPath_ReturnsError()
        {
            // Test FileSinkDefinition validation
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Sink = new FileSinkDefinition { Path = "", Format = "json" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("sink.file.path is required"));
        }

        [Test]
        public void ValidateFileSink_MissingFormat_ReturnsError()
        {
            // Test FileSinkDefinition format validation
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Sink = new FileSinkDefinition { Path = "/path", Format = "" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("sink.file.format is required"));
        }

        [Test]
        public void ValidateDatabaseSink_MissingConnectionString_ReturnsError()
        {
            // Test DatabaseSinkDefinition connection string validation
            var job = new JobDefinition
            {
                Metadata = new JobMetadata { JobId = "job-123", Version = "1.0" },
                Source = new KafkaSourceDefinition { Topic = "input" },
                Sink = new DatabaseSinkDefinition { ConnectionString = "", Table = "users" }
            };

            var result = JobDefinitionValidator.Validate(job);

            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Errors, Contains.Item("sink.database.connectionString is required"));
        }
    }
}
