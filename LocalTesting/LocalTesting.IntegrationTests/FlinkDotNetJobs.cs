using Flink.JobBuilder.Models;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Contains various FlinkDotNet job implementations for testing different features
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
        Console.WriteLine($"[CreateUppercaseJob] START - jobName={jobName}, inputTopic={inputTopic}, outputTopic={outputTopic}, kafka={kafka}");
        
        // Use the Kafka container IP passed from test infrastructure
        // This is the bridge network IP (e.g., "172.17.0.2:9093") which Flink containers can reach
        var kafkaBootstrap = kafka;
        
        // Use DataStream API instead of JobBuilder
        Console.WriteLine($"[CreateUppercaseJob] Creating execution environment...");
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();
        
        Console.WriteLine($"[CreateUppercaseJob] Building DataStream pipeline...");
        env.FromKafka(inputTopic, kafkaBootstrap)
            .Map("upper")
            .SinkToKafka(outputTopic, kafkaBootstrap);
        
        Console.WriteLine($"[CreateUppercaseJob] Executing job async...");
        var jobClient = await env.ExecuteAsync(jobName, ct);
        
        Console.WriteLine($"[CreateUppercaseJob] Job executed successfully, JobId={jobClient.GetJobId()}");
        
        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            JobId = jobClient.GetJobId()
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
        Console.WriteLine($"[CreateFilterJob] START - jobName={jobName}");
        
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        
        // Use DataStream API instead of JobBuilder
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();
        env.FromKafka(inputTopic, kafkaBootstrap)
            .Where("nonempty")
            .SinkToKafka(outputTopic, kafkaBootstrap);
        
        Console.WriteLine($"[CreateFilterJob] Executing job async...");
        var jobClient = await env.ExecuteAsync(jobName, ct);
        
        Console.WriteLine($"[CreateFilterJob] Job executed successfully, JobId={jobClient.GetJobId()}");
        
        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            JobId = jobClient.GetJobId()
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
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        
        // Use DataStream API instead of JobBuilder
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();
        env.FromKafka(inputTopic, kafkaBootstrap)
            .Map("split:,")
            .Map("concat:-joined")
            .SinkToKafka(outputTopic, kafkaBootstrap);
        
        var jobClient = await env.ExecuteAsync(jobName, ct);
        
        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            JobId = jobClient.GetJobId()
        };
    }
    
    /// <summary>
    /// Creates a DataStream job with timer functionality
    /// </summary>
    public static async Task<JobSubmissionResult> CreateTimerJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        
        // Use DataStream API with timer support
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();
        env.FromKafka(inputTopic, kafkaBootstrap)
            .WithTimer(5000) // 5 seconds in milliseconds
            .SinkToKafka(outputTopic, kafkaBootstrap);
        
        var jobClient = await env.ExecuteAsync(jobName, ct);
        
        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            JobId = jobClient.GetJobId()
        };
    }
    
    /// <summary>
    /// Creates a SQL job that passes through data from input to output using Direct Flink SQL Gateway
    /// </summary>
    public static async Task<JobSubmissionResult> CreateDirectFlinkSQLJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        // SQL Gateway jobs run inside Flink containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        var sqlStatements = new[]
        {
            $@"CREATE TABLE input ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{inputTopic}',
                'properties.bootstrap.servers'='{kafkaBootstrap}',
                'properties.group.id'='flink-sql-test',
                'scan.startup.mode'='earliest-offset',
                'format'='json'
            )",
            $@"CREATE TABLE output ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafkaBootstrap}',
                'format'='json'
            )",
            "INSERT INTO output SELECT `key`, `value` FROM input"
        };
        
        // SQL jobs still use JobBuilder since they require special handling
        // This is acceptable as SQL doesn't map directly to DataStream operations
        var sqlJob = FlinkDotNet.Pipelines.FlinkDotNet.Sql(sqlStatements);
        var jobDef = sqlJob.BuildJobDefinition();
        if (jobDef.Source is SqlSourceDefinition sqlSource)
        {
            sqlSource.ExecutionMode = "gateway";
        }
        jobDef.Metadata.JobName = jobName;
        
        // Submit via gateway service
        var gatewayService = new Flink.JobBuilder.Services.FlinkJobGatewayService();
        return await gatewayService.SubmitJobAsync(jobDef, ct);
    }
    
    /// <summary>
    /// Creates a SQL job that transforms data
    /// </summary>
    public static async Task<JobSubmissionResult> CreateSqlTransformJob(
        string inputTopic,
        string outputTopic,
        string kafka,
        string jobName,
        CancellationToken ct)
    {
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        var sqlStatements = new[]
        {
            $@"CREATE TABLE input ( `key` STRING, `value` STRING ) WITH (
                'connector'='kafka',
                'topic'='{inputTopic}',
                'properties.bootstrap.servers'='{kafkaBootstrap}',
                'properties.group.id'='flink-sql-transform',
                'scan.startup.mode'='earliest-offset',
                'format'='json'
            )",
            $@"CREATE TABLE output ( `key` STRING, `transformed` STRING ) WITH (
                'connector'='kafka',
                'topic'='{outputTopic}',
                'properties.bootstrap.servers'='{kafkaBootstrap}',
                'format'='json'
            )",
            "INSERT INTO output SELECT `key`, UPPER(`value`) as `transformed` FROM input"
        };
        
        // SQL jobs still use JobBuilder since they require special handling
        // This is acceptable as SQL doesn't map directly to DataStream operations
        var sqlJob = FlinkDotNet.Pipelines.FlinkDotNet.Sql(sqlStatements);
        return await sqlJob.Submit(jobName, ct);
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
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        var kafkaBootstrap = kafka;
        
        // Use DataStream API for composite operations including timer
        var env = FlinkDotNet.Flink.GetExecutionEnvironment();
        env.FromKafka(inputTopic, kafkaBootstrap)
            .Map("split:,")
            .Map("concat:-tail")
            .Map("upper")
            .Where("nonempty")
            .WithTimer(5000) // 5 seconds in milliseconds
            .SinkToKafka(outputTopic, kafkaBootstrap);
        
        var jobClient = await env.ExecuteAsync(jobName, ct);
        
        return new JobSubmissionResult
        {
            Success = true,
            FlinkJobId = jobClient.GetJobId(),
            JobId = jobClient.GetJobId()
        };
    }
}



