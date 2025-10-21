using Flink.JobBuilder.Models;
using FlinkDotNet.DataStream;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace LocalTesting.IntegrationTests;

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
                'format'='json'
            )",
            $@"CREATE TABLE output ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'format'='json'
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
                JobId = Guid.NewGuid().ToString(),
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
                'format'='json'
            )",
            $@"CREATE TABLE output ( `key` STRING, `transformed` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafka}',
                'format'='json'
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
                JobId = Guid.NewGuid().ToString(),
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
