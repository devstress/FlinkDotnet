using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;

namespace ObservabilityTesting.IntegrationTests;

/// <summary>
/// Contains various FlinkDotNet job implementations for testing different features.
/// Uses modern DataStream API pattern:
/// 1. StreamExecutionEnvironment.GetExecutionEnvironment()
/// 2. environment.FromKafka() to create stream
/// 3. Stream transformation methods (.Map, .Filter, etc.)
/// 4. .SinkToKafka() to write output
/// 5. environment.ExecuteAsync() to submit job
/// </summary>
public static class FlinkDotNetJobs
{
    /// <summary>
    /// Mapper function that converts strings to uppercase.
    /// Using IMapFunction instead of lambda for proper serialization.
    /// </summary>
    public class UppercaseMapper : IMapFunction<string, string>
    {
        public string Map(string value) => value.ToUpperInvariant();
    }

    /// <summary>
    /// Creates a simple DataStream job that converts input strings to uppercase
    /// </summary>
    public static async Task<JobSubmissionResult> CreateUppercaseJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "uppercase-job", startingOffsets: "earliest")
            .Map(new UppercaseMapper())
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a JobDefinition for an uppercase job (for Gateway submission)
    /// </summary>
    public static JobDefinition CreateUppercaseJobDefinition(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "uppercase-job", startingOffsets: "earliest")
            .Map(new UppercaseMapper())
            .SinkToKafka(outputTopic, kafka);

        // Get the JobDefinition without executing
        var jobDefinition = environment.GetJobDefinition(jobName);
        return jobDefinition;
    }

    /// <summary>
    /// Creates a DataStream job with filtering
    /// </summary>
    public static async Task<JobSubmissionResult> CreateFilterJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "filter-job", startingOffsets: "earliest")
            .Filter(s => !string.IsNullOrWhiteSpace(s))
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a JobDefinition for a filter job (for Gateway submission)
    /// </summary>
    public static JobDefinition CreateFilterJobDefinition(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "filter-job", startingOffsets: "earliest")
            .Filter(s => !string.IsNullOrWhiteSpace(s))
            .SinkToKafka(outputTopic, kafka);

        return environment.GetJobDefinition(jobName);
    }

    /// <summary>
    /// Creates a DataStream job with string splitting and concatenation
    /// </summary>
    public static async Task<JobSubmissionResult> CreateSplitConcatJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "splitconcat-job", startingOffsets: "earliest")
            .FlatMap(s => s.Split(','))
            .Map(s => s + "-joined")
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a DataStream job with timer functionality
    /// Note: Timer functionality needs special windowing - simplified version here
    /// </summary>
    public static async Task<JobSubmissionResult> CreateTimerJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        // Simple pass-through for timer test (actual timer logic would require more complex windowing)
        environment.FromKafka(inputTopic, kafka, groupId: "timer-job", startingOffsets: "earliest")
            .Map(s => $"[Timed] {s}")
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a composite job that combines multiple operations
    /// </summary>
    public static async Task<JobSubmissionResult> CreateCompositeJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        var environment = StreamExecutionEnvironment.GetExecutionEnvironment();

        environment.FromKafka(inputTopic, kafka, groupId: "composite-job", startingOffsets: "earliest")
            .FlatMap(s => s.Split(','))
            .Map(s => s + "-tail")
            .Map(s => s.ToUpperInvariant())
            .Filter(s => !string.IsNullOrWhiteSpace(s))
            .Map(s => $"[Processed] {s}")
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }
}
