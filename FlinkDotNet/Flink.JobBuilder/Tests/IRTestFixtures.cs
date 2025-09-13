using System;
using System.Collections.Generic;
using System.Text.Json;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder.Tests
{
    /// <summary>
    /// Test fixtures for IR validation and round-trip serialization
    /// </summary>
    public static class IRTestFixtures
    {
        /// <summary>
        /// Creates a valid minimal job definition for testing
        /// </summary>
        public static JobDefinition CreateValidMinimalJob()
        {
            return new JobDefinition
            {
                Source = new KafkaSourceDefinition
                {
                    Topic = "input-topic",
                    BootstrapServers = "localhost:9092",
                    GroupId = "test-group",
                    StartingOffsets = "latest"
                },
                Operations = new List<IOperationDefinition>(),
                Sink = new ConsoleSinkDefinition { Format = "json" },
                Metadata = new JobMetadata
                {
                    JobId = "test-job-001",
                    JobName = "Test Job",
                    CreatedAt = DateTime.UtcNow,
                    Version = "1.0.0",
                    Parallelism = 1
                }
            };
        }

        /// <summary>
        /// Tests JSON round-trip serialization
        /// </summary>
        public static (bool Success, string ErrorMessage) TestJsonRoundTrip(JobDefinition original)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(original, options);
                var deserialized = JsonSerializer.Deserialize<JobDefinition>(json, options);

                if (deserialized == null)
                {
                    return (false, "Deserialization returned null");
                }

                if (deserialized.Metadata.JobId != original.Metadata.JobId)
                {
                    return (false, "JobId mismatch after round-trip");
                }

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, $"Exception during round-trip: {ex.Message}");
            }
        }

        /// <summary>
        /// Tests validation for test fixtures
        /// </summary>
        public static Dictionary<string, ValidationResult> RunValidationTests()
        {
            var validator = new IRValidator();
            var results = new Dictionary<string, ValidationResult>();

            results["ValidMinimal"] = validator.Validate(CreateValidMinimalJob());

            return results;
        }
    }
}
