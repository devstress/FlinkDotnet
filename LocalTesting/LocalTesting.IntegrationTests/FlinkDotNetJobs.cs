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
        // Use the Kafka container IP passed from test infrastructure
        // This is the bridge network IP (e.g., "172.17.0.2:9093") which Flink containers can reach
        var kafkaBootstrap = kafka;
        
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafkaBootstrap)
            .Map("upper")
            .ToKafka(outputTopic, kafkaBootstrap);
        
        return await job.Submit(jobName, ct);
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
        // Flink jobs run inside containers and must use container network name 'kafka:9092'
        // NOT the host connection string (e.g., localhost:17901)
        const string kafkaBootstrap = "172.17.0.1:9093";
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafkaBootstrap)
            .Where("nonempty")
            .ToKafka(outputTopic, kafkaBootstrap);
        
        return await job.Submit(jobName, ct);
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
        const string kafkaBootstrap = "172.17.0.1:9093";
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafkaBootstrap)
            .Map("split:,")
            .Map("concat:-joined")
            .ToKafka(outputTopic, kafkaBootstrap);
        
        return await job.Submit(jobName, ct);
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
        const string kafkaBootstrap = "172.17.0.1:9093";
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafkaBootstrap)
            .WithTimer(5)
            .ToKafka(outputTopic, kafkaBootstrap);
        
        return await job.Submit(jobName, ct);
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
        const string kafkaBootstrap = "172.17.0.1:9093";
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
        
        // Use SQL Gateway for direct SQL execution
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
        const string kafkaBootstrap = "172.17.0.1:9093";
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
        const string kafkaBootstrap = "172.17.0.1:9093";
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafkaBootstrap)
            .Map("split:,")
            .Map("concat:-tail")
            .Map("upper")
            .Where("nonempty")
            .WithTimer(5)
            .ToKafka(outputTopic, kafkaBootstrap);
        
        return await job.Submit(jobName, ct);
    }
}



