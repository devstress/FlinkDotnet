using Flink.JobBuilder.Models;
using NUnit.Framework;

namespace Flink.JobBuilder.Tests.Tests;

[TestFixture]
public class ModelIntegrationTests
{
    #region Complex Job Definition Scenarios

    [Test]
    public void JobDefinition_CompleteStreamingJob_AllComponentsPresent()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition
            {
                Topic = "input",
                BootstrapServers = "localhost:9092",
                GroupId = "consumer-group"
            },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "value > 100" },
                new MapOperationDefinition { Expression = "x => x * 2", OutputType = "Integer" },
                new GroupByOperationDefinition { Key = "userId" },
                new AggregateOperationDefinition
                {
                    AggregationType = "SUM",
                    Field = "amount",
                    WindowSeconds = 60
                }
            },
            Sink = new KafkaSinkDefinition
            {
                Topic = "output",
                BootstrapServers = "localhost:9092"
            },
            Metadata = new JobMetadata
            {
                JobId = "streaming-job-1",
                JobName = "Streaming Aggregation Job",
                Parallelism = 4,
                Version = "1.0.0"
            }
        };
        
        Assert.That(jobDef.Source, Is.InstanceOf<KafkaSourceDefinition>());
        Assert.That(jobDef.Operations, Has.Count.EqualTo(4));
        Assert.That(jobDef.Sink, Is.InstanceOf<KafkaSinkDefinition>());
        Assert.That(jobDef.Metadata.Parallelism, Is.EqualTo(4));
    }

    [Test]
    public void JobDefinition_SqlJob_NoSinkRequired()
    {
        var jobDef = new JobDefinition
        {
            Source = new SqlSourceDefinition
            {
                Statements = new List<string>
                {
                    "CREATE TABLE users (id INT, name STRING)",
                    "INSERT INTO output_table SELECT * FROM users"
                },
                Mode = "streaming",
                ExecutionMode = "tableenv"
            },
            Sink = null,
            Metadata = new JobMetadata
            {
                JobId = "sql-job-1",
                JobName = "Pure SQL Job"
            }
        };
        
        Assert.That(jobDef.Source, Is.InstanceOf<SqlSourceDefinition>());
        Assert.That(jobDef.Sink, Is.Null);
        var sqlSource = (SqlSourceDefinition)jobDef.Source;
        Assert.That(sqlSource.Statements, Has.Count.EqualTo(2));
    }

    [Test]
    public void JobDefinition_MultiSourceJoin_ConfiguredCorrectly()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "left-stream" },
            Operations = new List<IOperationDefinition>
            {
                new JoinOperationDefinition
                {
                    JoinType = "INNER",
                    RightSource = new KafkaSourceDefinition { Topic = "right-stream" },
                    LeftKey = "userId",
                    RightKey = "id",
                    Window = new WindowOperationDefinition
                    {
                        WindowType = "TUMBLING",
                        Size = 60,
                        TimeUnit = "SECONDS"
                    }
                }
            },
            Sink = new ConsoleSinkDefinition { Format = "json" }
        };
        
        var joinOp = (JoinOperationDefinition)jobDef.Operations[0];
        Assert.That(joinOp.RightSource, Is.InstanceOf<KafkaSourceDefinition>());
        Assert.That(joinOp.Window, Is.Not.Null);
        Assert.That(joinOp.Window!.Size, Is.EqualTo(60));
    }

    [Test]
    public void JobDefinition_WithSideOutput_ErrorHandling()
    {
        var jobDef = new JobDefinition
        {
            Source = new HttpSourceDefinition
            {
                Url = "https://api.example.com/data",
                IntervalSeconds = 60
            },
            Operations = new List<IOperationDefinition>
            {
                new SideOutputOperationDefinition
                {
                    OutputTag = "errors",
                    Condition = "status >= 400",
                    SideOutputSink = new KafkaSinkDefinition { Topic = "error-dlq" }
                },
                new MapOperationDefinition { Expression = "x => process(x)" }
            },
            Sink = new DatabaseSinkDefinition
            {
                ConnectionString = "Server=localhost;Database=db",
                Table = "processed_data"
            }
        };
        
        var sideOutput = (SideOutputOperationDefinition)jobDef.Operations[0];
        Assert.That(sideOutput.SideOutputSink, Is.InstanceOf<KafkaSinkDefinition>());
        Assert.That(sideOutput.Condition, Contains.Substring("status"));
    }

    #endregion

    #region Complex Operation Chains

    [Test]
    public void JobDefinition_StatefulProcessing_WithTimers()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "events" },
            Operations = new List<IOperationDefinition>
            {
                new StateOperationDefinition
                {
                    StateType = "value",
                    StateKey = "user_session",
                    TtlMs = 3600000
                },
                new TimerOperationDefinition
                {
                    TimerType = "processing",
                    DelayMs = 300000,
                    Action = "cleanup"
                },
                new ProcessFunctionOperationDefinition
                {
                    ProcessType = "sessionManager",
                    StateKeys = new List<string> { "user_session" },
                    TimerNames = new List<string> { "cleanup_timer" }
                }
            }
        };
        
        Assert.That(jobDef.Operations, Has.Count.EqualTo(3));
        var processOp = (ProcessFunctionOperationDefinition)jobDef.Operations[2];
        Assert.That(processOp.StateKeys, Contains.Item("user_session"));
    }

    [Test]
    public void JobDefinition_AsyncEnrichment_WithCaching()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "raw-events" },
            Operations = new List<IOperationDefinition>
            {
                new AsyncFunctionOperationDefinition
                {
                    FunctionType = "http",
                    Url = "https://enrichment-service.example.com",
                    Method = "POST",
                    TimeoutMs = 3000,
                    MaxRetries = 3,
                    StateKey = "enrichment_cache",
                    CacheTtlMs = 600000
                }
            },
            Sink = new KafkaSinkDefinition { Topic = "enriched-events" }
        };
        
        var asyncOp = (AsyncFunctionOperationDefinition)jobDef.Operations[0];
        Assert.That(asyncOp.CacheTtlMs, Is.EqualTo(600000));
        Assert.That(asyncOp.MaxRetries, Is.EqualTo(3));
    }

    [Test]
    public void JobDefinition_RetryWithBackoff_ToDeadLetter()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "events" },
            Operations = new List<IOperationDefinition>
            {
                new RetryOperationDefinition
                {
                    MaxRetries = 3,
                    DelayMs = new List<long> { 5000, 15000, 60000 },
                    RetryCondition = "error.retryable == true",
                    DeadLetterTopic = "failed-events"
                }
            }
        };
        
        var retryOp = (RetryOperationDefinition)jobDef.Operations[0];
        Assert.That(retryOp.DelayMs, Has.Count.EqualTo(3));
        Assert.That(retryOp.DeadLetterTopic, Is.EqualTo("failed-events"));
    }

    #endregion

    #region Multiple Source Types

    [Test]
    public void JobDefinition_DatabaseSource_WithPolling()
    {
        var jobDef = new JobDefinition
        {
            Source = new DatabaseSourceDefinition
            {
                ConnectionString = "Server=db.example.com;Database=prod;",
                Query = "SELECT * FROM events WHERE processed = 0",
                DatabaseType = "postgresql",
                PollingIntervalSeconds = 30
            },
            Operations = new List<IOperationDefinition>
            {
                new MapOperationDefinition { Expression = "x => markProcessed(x)" }
            },
            Sink = new DatabaseSinkDefinition
            {
                ConnectionString = "Server=db.example.com;Database=analytics;",
                Table = "processed_events"
            }
        };
        
        var dbSource = (DatabaseSourceDefinition)jobDef.Source;
        Assert.That(dbSource.PollingIntervalSeconds, Is.EqualTo(30));
    }

    [Test]
    public void JobDefinition_FileSource_BatchProcessing()
    {
        var jobDef = new JobDefinition
        {
            Source = new FileSourceDefinition
            {
                Path = "/data/input/*.json",
                Format = "json"
            },
            Operations = new List<IOperationDefinition>
            {
                new FilterOperationDefinition { Expression = "isValid" }
            },
            Sink = new FileSinkDefinition
            {
                Path = "/data/output/",
                Format = "parquet"
            }
        };
        
        var fileSource = (FileSourceDefinition)jobDef.Source;
        var fileSink = (FileSinkDefinition)jobDef.Sink!;
        
        Assert.That(fileSource.Format, Is.EqualTo("json"));
        Assert.That(fileSink.Format, Is.EqualTo("parquet"));
    }

    [Test]
    public void JobDefinition_HttpSource_PeriodicPolling()
    {
        var jobDef = new JobDefinition
        {
            Source = new HttpSourceDefinition
            {
                Url = "https://api.example.com/stream",
                Method = "GET",
                Headers = new Dictionary<string, string>
                {
                    { "Authorization", "Bearer token" }
                },
                IntervalSeconds = 120
            }
        };
        
        var httpSource = (HttpSourceDefinition)jobDef.Source;
        Assert.That(httpSource.IntervalSeconds, Is.EqualTo(120));
        Assert.That(httpSource.Headers, Contains.Key("Authorization"));
    }

    #endregion

    #region Window Operations

    [Test]
    public void JobDefinition_TimeBasedAggregation_TumblingWindow()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "metrics" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "sensor_id" },
                new AggregateOperationDefinition
                {
                    AggregationType = "AVG",
                    Field = "temperature",
                    WindowSeconds = 300,
                    Alias = "avg_temp"
                }
            }
        };
        
        var aggOp = (AggregateOperationDefinition)jobDef.Operations[1];
        Assert.That(aggOp.WindowSeconds, Is.EqualTo(300));
        Assert.That(aggOp.Alias, Is.EqualTo("avg_temp"));
    }

    [Test]
    public void JobDefinition_CountBasedAggregation_CountWindow()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "transactions" },
            Operations = new List<IOperationDefinition>
            {
                new GroupByOperationDefinition { Key = "user_id" },
                new AggregateOperationDefinition
                {
                    AggregationType = "SUM",
                    Field = "amount",
                    WindowCount = 50,
                    Alias = "batch_total"
                }
            }
        };
        
        var aggOp = (AggregateOperationDefinition)jobDef.Operations[1];
        Assert.That(aggOp.WindowCount, Is.EqualTo(50));
    }

    [Test]
    public void JobDefinition_SlidingWindow_WithOverlap()
    {
        var jobDef = new JobDefinition
        {
            Source = new KafkaSourceDefinition { Topic = "events" },
            Operations = new List<IOperationDefinition>
            {
                new WindowOperationDefinition
                {
                    WindowType = "SLIDING",
                    Size = 60,
                    Slide = 30,
                    TimeUnit = "SECONDS"
                }
            }
        };
        
        var windowOp = (WindowOperationDefinition)jobDef.Operations[0];
        Assert.That(windowOp.Slide, Is.EqualTo(30));
        Assert.That(windowOp.Size, Is.EqualTo(60));
    }

    #endregion

    #region Metadata Scenarios

    [Test]
    public void JobMetadata_WithCustomProperties_ComplexScenario()
    {
        var metadata = new JobMetadata
        {
            JobId = "complex-job-123",
            JobName = "Production Data Pipeline",
            CreatedAt = DateTime.UtcNow,
            Version = "2.1.0",
            Parallelism = 16,
            Properties = new Dictionary<string, string>
            {
                { "environment", "production" },
                { "team", "data-engineering" },
                { "owner", "john.doe@example.com" },
                { "cost-center", "eng-123" },
                { "sla", "99.9" }
            }
        };
        
        Assert.That(metadata.Properties, Has.Count.EqualTo(5));
        Assert.That(metadata.Properties["environment"], Is.EqualTo("production"));
        Assert.That(metadata.Properties["sla"], Is.EqualTo("99.9"));
        Assert.That(metadata.Parallelism, Is.EqualTo(16));
    }

    [Test]
    public void JobMetadata_MinimalConfiguration_ValidJob()
    {
        var metadata = new JobMetadata
        {
            JobId = "minimal-job"
        };
        
        Assert.That(metadata.JobId, Is.EqualTo("minimal-job"));
        Assert.That(metadata.JobName, Is.Null);
        Assert.That(metadata.Parallelism, Is.Null);
    }

    #endregion

    #region Multi-Key GroupBy

    [Test]
    public void GroupByOperation_MultipleKeys_ConfiguredCorrectly()
    {
        var groupBy = new GroupByOperationDefinition
        {
            Key = "primary_key",
            Keys = new List<string> { "user_id", "region", "device_type" }
        };
        
        Assert.That(groupBy.Keys, Has.Count.EqualTo(3));
        Assert.That(groupBy.Keys, Contains.Item("user_id"));
        Assert.That(groupBy.Keys, Contains.Item("region"));
    }

    [Test]
    public void GroupByOperation_SingleKey_UsingKeyProperty()
    {
        var groupBy = new GroupByOperationDefinition
        {
            Key = "customer_id"
        };
        
        Assert.That(groupBy.Key, Is.EqualTo("customer_id"));
        Assert.That(groupBy.Keys, Is.Null);
    }

    #endregion

    #region Job Results Integration

    [Test]
    public void JobStatus_CompleteLifecycle_DurationCalculated()
    {
        var startTime = DateTime.UtcNow.AddHours(-2);
        var endTime = DateTime.UtcNow;
        
        var status = new JobStatus
        {
            JobId = "job-123",
            FlinkJobId = "flink-456",
            State = "FINISHED",
            StartTime = startTime,
            EndTime = endTime,
            Metrics = new JobMetrics
            {
                RecordsRead = 1000000,
                RecordsWritten = 950000
            }
        };
        
        Assert.That(status.Duration, Is.Not.Null);
        Assert.That(status.Duration!.Value.TotalHours, Is.EqualTo(2).Within(0.1));
        Assert.That(status.Metrics!.RecordsRead, Is.EqualTo(1000000));
    }

    [Test]
    public void JobSubmissionResult_SuccessWithMetadata_CompleteInfo()
    {
        var result = JobSubmissionResult.CreateSuccess("job-123", "flink-456");
        result.Metadata["cluster"] = "prod-cluster-1";
        result.Metadata["namespace"] = "data-pipelines";
        
        Assert.That(result.Success, Is.True);
        Assert.That(result.Metadata, Has.Count.EqualTo(2));
        Assert.That(result.Metadata["cluster"], Is.EqualTo("prod-cluster-1"));
    }

    #endregion

    #region Sink Configurations

    [Test]
    public void RedisSink_AtomicOperations_ExactlyOnceSemantics()
    {
        var sink = new RedisSinkDefinition
        {
            ConnectionString = "redis://prod-redis:6379",
            Key = "counter:{user_id}",
            OperationType = "increment",
            Configuration = new Dictionary<string, object>
            {
                { "ttl", 86400 },
                { "db", 1 },
                { "pipeline", true }
            }
        };
        
        Assert.That(sink.Configuration["ttl"], Is.EqualTo(86400));
        Assert.That(sink.Configuration["pipeline"], Is.EqualTo(true));
    }

    [Test]
    public void HttpSink_WithRetryAndAuth_CompleteConfiguration()
    {
        var sink = new HttpSinkDefinition
        {
            Url = "https://webhook.example.com/events",
            Method = "POST",
            Headers = new Dictionary<string, string>
            {
                { "Content-Type", "application/json" },
                { "X-API-Version", "v2" }
            },
            BodyTemplate = "{\"event\": \"{event}\", \"timestamp\": \"{ts}\"}",
            AuthTokenStateKey = "webhook_auth",
            TimeoutMs = 5000,
            Properties = new Dictionary<string, string>
            {
                { "retry_count", "3" },
                { "retry_delay", "1000" }
            }
        };
        
        Assert.That(sink.Headers, Has.Count.EqualTo(2));
        Assert.That(sink.BodyTemplate, Contains.Substring("{event}"));
        Assert.That(sink.Properties["retry_count"], Is.EqualTo("3"));
    }

    #endregion
}
