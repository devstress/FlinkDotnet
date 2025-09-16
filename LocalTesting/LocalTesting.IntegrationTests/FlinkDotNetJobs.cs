using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LocalTesting.IntegrationTests;

/// <summary>
/// Contains various FlinkDotNet job implementations for testing different features
/// </summary>
public static class FlinkDotNetJobs
{
    /// <summary>
    /// Creates a simple DataStream job that converts input strings to uppercase
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateUppercaseJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
        string jobName, 
        CancellationToken ct)
    {
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafka)
            .Map("upper")
            .ToKafka(outputTopic, kafka);
        
        return await job.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a DataStream job with filtering
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateFilterJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
        string jobName, 
        CancellationToken ct)
    {
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafka)
            .Where("nonempty")
            .ToKafka(outputTopic, kafka);
        
        return await job.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a DataStream job with string splitting and concatenation
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateSplitConcatJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
        string jobName, 
        CancellationToken ct)
    {
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafka)
            .Map("split:,")
            .Map("concat:-joined")
            .ToKafka(outputTopic, kafka);
        
        return await job.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a DataStream job with timer functionality
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateTimerJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
        string jobName, 
        CancellationToken ct)
    {
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafka)
            .WithTimer(5)
            .ToKafka(outputTopic, kafka);
        
        return await job.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a SQL job that passes through data from input to output
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateSqlPassthroughJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
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
        
        var sqlJob = FlinkDotNet.Pipelines.FlinkDotNet.Sql(sqlStatements);
        return await sqlJob.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a SQL job that transforms data
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateSqlTransformJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
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
        
        var sqlJob = FlinkDotNet.Pipelines.FlinkDotNet.Sql(sqlStatements);
        return await sqlJob.Submit(jobName, ct);
    }
    
    /// <summary>
    /// Creates a composite job that combines multiple operations
    /// </summary>
    public static async Task<FlinkDotNet.Pipelines.SubmitResult> CreateCompositeJob(
        string inputTopic, 
        string outputTopic, 
        string kafka, 
        string jobName, 
        CancellationToken ct)
    {
        var job = FlinkDotNet.Flink.JobBuilder
            .FromKafka(inputTopic, kafka)
            .Map("split:,")
            .Map("concat:-tail")
            .Map("upper")
            .Where("nonempty")
            .WithTimer(5)
            .ToKafka(outputTopic, kafka);
        
        return await job.Submit(jobName, ct);
    }
}