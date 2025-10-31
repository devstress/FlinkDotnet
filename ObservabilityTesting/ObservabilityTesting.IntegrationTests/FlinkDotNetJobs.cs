using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

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
    /// Generates a Flink-compatible job ID.
    /// Flink job IDs are 16 bytes (128-bit) shown as 32 lowercase hexadecimal characters without dashes.
    /// This matches the format used by Flink's JobID.generate() and expected by the REST API.
    /// </summary>
    /// <returns>32 lowercase hexadecimal characters (e.g., "6511c409ff584d1e93b3638f56d14e01")</returns>
    private static string GenerateFlinkJobId()
    {
        // Generate a GUID and convert to Flink format (32 hex chars, lowercase, no dashes)
        return Guid.NewGuid().ToString("N").ToLowerInvariant();
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
            .Map(s => s.ToUpperInvariant())
            .SinkToKafka(outputTopic, kafka);

        var jobClient = await environment.ExecuteAsync(jobName, ct);

        return new JobSubmissionResult
        {
            Success = true,
            JobId = jobClient.GetJobId(),
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
            .Map(s => s.ToUpperInvariant())
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
            JobId = jobClient.GetJobId(),
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
            JobId = jobClient.GetJobId(),
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
            JobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a SQL job that passes through data from input to output using Direct Flink SQL Gateway
    /// </summary>
    public static async Task<JobSubmissionResult> CreateDirectFlinkSQLJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string sqlGatewayUrl,
        string jobName,
        CancellationToken ct)
    {
        var sqlStatements = new[]
        {
            $@"CREATE TABLE input ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{inputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'properties.group.id'='flink-sql-test',
                'scan.startup.mode'='earliest-offset',
                'value.format'='json',
                'value.json.fail-on-missing-field'='false',
                'value.json.ignore-parse-errors'='true'
            )",
            $@"CREATE TABLE output ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'value.format'='json',
                'value.json.timestamp-format.standard'='ISO-8601'
            )",
            "INSERT INTO output SELECT `key`, `value` FROM input"
        };

        var jobDef = new JobDefinition
        {
            Source = new SqlSourceDefinition
            {
                Statements = new List<string>(sqlStatements),
                Mode = "streaming",
                ExecutionMode = "gateway"
            },
            Metadata = new JobMetadata
            {
                JobId = GenerateFlinkJobId(),
                JobName = jobName,
                CreatedAt = DateTime.UtcNow,
                Version = "1.0"
            }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Flink:SqlGateway:BaseUrl"] = sqlGatewayUrl
            })
            .Build();

        var jobManager = new FlinkDotNet.JobGateway.Services.FlinkJobManager(
            NullLogger<FlinkDotNet.JobGateway.Services.FlinkJobManager>.Instance,
            configuration,
            new HttpClient());

        return await jobManager.SubmitJobAsync(jobDef);
    }

    /// <summary>
    /// Creates a SQL job that transforms data
    /// </summary>
    public static async Task<JobSubmissionResult> CreateSqlTransformJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string sqlGatewayUrl,
        string jobName,
        CancellationToken ct)
    {
        var sqlStatements = new[]
        {
            $@"CREATE TABLE input ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{inputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'properties.group.id'='flink-sql-transform',
                'scan.startup.mode'='earliest-offset',
                'value.format'='json',
                'value.json.fail-on-missing-field'='false',
                'value.json.ignore-parse-errors'='true'
            )",
            $@"CREATE TABLE output ( `key` STRING, `transformed` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'value.format'='json',
                'value.json.timestamp-format.standard'='ISO-8601'
            )",
            "INSERT INTO output SELECT `key`, UPPER(`value`) as `transformed` FROM input"
        };

        var jobDef = new JobDefinition
        {
            Source = new SqlSourceDefinition
            {
                Statements = new List<string>(sqlStatements),
                Mode = "streaming",
                ExecutionMode = "gateway"
            },
            Metadata = new JobMetadata
            {
                JobId = GenerateFlinkJobId(),
                JobName = jobName,
                CreatedAt = DateTime.UtcNow,
                Version = "1.0"
            }
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Flink:SqlGateway:BaseUrl"] = sqlGatewayUrl
            })
            .Build();

        var jobManager = new FlinkDotNet.JobGateway.Services.FlinkJobManager(
            NullLogger<FlinkDotNet.JobGateway.Services.FlinkJobManager>.Instance,
            configuration,
            new HttpClient());

        return await jobManager.SubmitJobAsync(jobDef);
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
            JobId = jobClient.GetJobId(),
            SubmittedAt = DateTime.UtcNow
        };
    }
}
