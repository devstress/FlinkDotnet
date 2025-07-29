using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Flink.JobBuilder.Models
{
    /// <summary>
    /// Complete job definition containing source, operations, and sink
    /// </summary>
    public class JobDefinition
    {
        public ISourceDefinition Source { get; set; } = null!;
        public List<IOperationDefinition> Operations { get; set; } = new();
        public ISinkDefinition Sink { get; set; } = null!;
        public JobMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the job
    /// </summary>
    public class JobMetadata
    {
        public string JobId { get; set; } = string.Empty;
        public string? JobName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Version { get; set; } = string.Empty;
        public int? Parallelism { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Base interface for all source definitions
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(KafkaSourceDefinition), "kafka")]
    [JsonDerivedType(typeof(FileSourceDefinition), "file")]
    [JsonDerivedType(typeof(HttpSourceDefinition), "http")]
    [JsonDerivedType(typeof(DatabaseSourceDefinition), "database")]
    public interface ISourceDefinition
    {
        string Type { get; }
    }

    /// <summary>
    /// Kafka source definition
    /// </summary>
    public class KafkaSourceDefinition : ISourceDefinition
    {
        public string Type => "kafka";
        public string Topic { get; set; } = string.Empty;
        public string? BootstrapServers { get; set; }
        public string? GroupId { get; set; }
        public string? StartingOffsets { get; set; } = "latest"; // latest, earliest, or specific offsets
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// File source definition
    /// </summary>
    public class FileSourceDefinition : ISourceDefinition
    {
        public string Type => "file";
        public string Path { get; set; } = string.Empty;
        public string Format { get; set; } = "text"; // text, json, csv, etc.
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// HTTP source definition for REST API calls
    /// </summary>
    public class HttpSourceDefinition : ISourceDefinition
    {
        public string Type => "http";
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? Body { get; set; }
        public int IntervalSeconds { get; set; } = 60; // Polling interval for continuous requests
        public string? AuthTokenStateKey { get; set; } // Key for cached auth token
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Database source definition
    /// </summary>
    public class DatabaseSourceDefinition : ISourceDefinition
    {
        public string Type => "database";
        public string ConnectionString { get; set; } = string.Empty;
        public string Query { get; set; } = string.Empty;
        public string? DatabaseType { get; set; } = "postgresql";
        public int PollingIntervalSeconds { get; set; } = 30;
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Base interface for all operation definitions
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(FilterOperationDefinition), "filter")]
    [JsonDerivedType(typeof(MapOperationDefinition), "map")]
    [JsonDerivedType(typeof(GroupByOperationDefinition), "groupBy")]
    [JsonDerivedType(typeof(AggregateOperationDefinition), "aggregate")]
    [JsonDerivedType(typeof(WindowOperationDefinition), "window")]
    [JsonDerivedType(typeof(JoinOperationDefinition), "join")]
    [JsonDerivedType(typeof(AsyncFunctionOperationDefinition), "asyncFunction")]
    [JsonDerivedType(typeof(ProcessFunctionOperationDefinition), "processFunction")]
    [JsonDerivedType(typeof(StateOperationDefinition), "state")]
    [JsonDerivedType(typeof(TimerOperationDefinition), "timer")]
    [JsonDerivedType(typeof(RetryOperationDefinition), "retry")]
    [JsonDerivedType(typeof(SideOutputOperationDefinition), "sideOutput")]
    public interface IOperationDefinition
    {
        string Type { get; }
    }

    /// <summary>
    /// Filter operation definition
    /// </summary>
    public class FilterOperationDefinition : IOperationDefinition
    {
        public string Type => "filter";
        public string Expression { get; set; } = string.Empty;
    }

    /// <summary>
    /// Map/transform operation definition
    /// </summary>
    public class MapOperationDefinition : IOperationDefinition
    {
        public string Type => "map";
        public string Expression { get; set; } = string.Empty;
        public string? OutputType { get; set; }
    }

    /// <summary>
    /// Group by operation definition
    /// </summary>
    public class GroupByOperationDefinition : IOperationDefinition
    {
        public string Type => "groupBy";
        public string Key { get; set; } = string.Empty;
        public List<string>? Keys { get; set; } // For multi-key grouping
    }

    /// <summary>
    /// Aggregation operation definition
    /// </summary>
    public class AggregateOperationDefinition : IOperationDefinition
    {
        public string Type => "aggregate";
        public string AggregationType { get; set; } = string.Empty; // SUM, COUNT, AVG, MIN, MAX
        public string Field { get; set; } = string.Empty;
        public string? Alias { get; set; }
    }

    /// <summary>
    /// Windowing operation definition
    /// </summary>
    public class WindowOperationDefinition : IOperationDefinition
    {
        public string Type => "window";
        public string WindowType { get; set; } = string.Empty; // TUMBLING, SLIDING, SESSION
        public int Size { get; set; }
        public string TimeUnit { get; set; } = "MINUTES"; // SECONDS, MINUTES, HOURS
        public int? Slide { get; set; } // For sliding windows
        public string? TimeField { get; set; } // Field to use for event time
    }

    /// <summary>
    /// Join operation definition
    /// </summary>
    public class JoinOperationDefinition : IOperationDefinition
    {
        public string Type => "join";
        public string JoinType { get; set; } = "INNER"; // INNER, LEFT, RIGHT, FULL
        public ISourceDefinition RightSource { get; set; } = null!;
        public string LeftKey { get; set; } = string.Empty;
        public string RightKey { get; set; } = string.Empty;
        public WindowOperationDefinition? Window { get; set; }
    }

    /// <summary>
    /// Async function operation for non-blocking I/O operations
    /// </summary>
    public class AsyncFunctionOperationDefinition : IOperationDefinition
    {
        public string Type => "asyncFunction";
        public string FunctionType { get; set; } = string.Empty; // http, database, etc.
        public string Url { get; set; } = string.Empty; // For HTTP calls
        public string Method { get; set; } = "GET";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? BodyTemplate { get; set; } // Template for request body
        public string? ConnectionString { get; set; } // For database calls
        public string? Query { get; set; } // For database queries
        public int TimeoutMs { get; set; } = 5000;
        public int MaxRetries { get; set; } = 3;
        public string? StateKey { get; set; } // For caching results
        public long? CacheTtlMs { get; set; } // Cache time-to-live
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Process function operation for complex stateful logic
    /// </summary>
    public class ProcessFunctionOperationDefinition : IOperationDefinition
    {
        public string Type => "processFunction";
        public string ProcessType { get; set; } = string.Empty; // authTokenManager, retryHandler, etc.
        public Dictionary<string, object> Parameters { get; set; } = new();
        public List<string> StateKeys { get; set; } = new();
        public List<string> TimerNames { get; set; } = new();
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// State operation for managing stateful data
    /// </summary>
    public class StateOperationDefinition : IOperationDefinition
    {
        public string Type => "state";
        public string StateType { get; set; } = "value"; // value, list, map, reducing
        public string StateKey { get; set; } = string.Empty;
        public string? ValueType { get; set; } = "string";
        public long? TtlMs { get; set; } // Time-to-live for state cleanup
        public string? DefaultValue { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Timer operation for scheduled processing
    /// </summary>
    public class TimerOperationDefinition : IOperationDefinition
    {
        public string Type => "timer";
        public string TimerType { get; set; } = "processing"; // processing, event
        public long DelayMs { get; set; }
        public string? TimerName { get; set; }
        public string? Action { get; set; } // What to do when timer fires
        public Dictionary<string, object> Parameters { get; set; } = new();
    }

    /// <summary>
    /// Retry operation with exponential backoff
    /// </summary>
    public class RetryOperationDefinition : IOperationDefinition
    {
        public string Type => "retry";
        public int MaxRetries { get; set; } = 5;
        public List<long> DelayMs { get; set; } = new() { 300000, 600000, 1800000, 3600000, 86400000 }; // 5min, 10min, 30min, 1hr, 1day
        public string? RetryCondition { get; set; } // Condition to determine if retry is needed
        public string? DeadLetterTopic { get; set; } // Topic for permanent failures
        public string StateKey { get; set; } = "retry_state";
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Side output operation for error handling and dead letter patterns
    /// </summary>
    public class SideOutputOperationDefinition : IOperationDefinition
    {
        public string Type => "sideOutput";
        public string OutputTag { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty; // When to route to side output
        public ISinkDefinition SideOutputSink { get; set; } = null!;
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Base interface for all sink definitions
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(KafkaSinkDefinition), "kafka")]
    [JsonDerivedType(typeof(ConsoleSinkDefinition), "console")]
    [JsonDerivedType(typeof(FileSinkDefinition), "file")]
    [JsonDerivedType(typeof(DatabaseSinkDefinition), "database")]
    [JsonDerivedType(typeof(HttpSinkDefinition), "http")]
    [JsonDerivedType(typeof(RedisSinkDefinition), "redis")]
    public interface ISinkDefinition
    {
        string Type { get; }
    }

    /// <summary>
    /// Kafka sink definition
    /// </summary>
    public class KafkaSinkDefinition : ISinkDefinition
    {
        public string Type => "kafka";
        public string Topic { get; set; } = string.Empty;
        public string? BootstrapServers { get; set; }
        public string? Serializer { get; set; } = "json";
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Console sink definition (for debugging)
    /// </summary>
    public class ConsoleSinkDefinition : ISinkDefinition
    {
        public string Type => "console";
        public string? Format { get; set; } = "json";
    }

    /// <summary>
    /// File sink definition
    /// </summary>
    public class FileSinkDefinition : ISinkDefinition
    {
        public string Type => "file";
        public string Path { get; set; } = string.Empty;
        public string Format { get; set; } = "json"; // json, csv, parquet, etc.
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Database sink definition
    /// </summary>
    public class DatabaseSinkDefinition : ISinkDefinition
    {
        public string Type => "database";
        public string ConnectionString { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
        public string? DatabaseType { get; set; } = "postgresql"; // postgresql, mysql, etc.
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// HTTP sink definition for REST API calls
    /// </summary>
    public class HttpSinkDefinition : ISinkDefinition
    {
        public string Type => "http";
        public string Url { get; set; } = string.Empty;
        public string Method { get; set; } = "POST";
        public Dictionary<string, string> Headers { get; set; } = new();
        public string? BodyTemplate { get; set; } // Template for request body
        public string? AuthTokenStateKey { get; set; } // Key for cached auth token
        public int TimeoutMs { get; set; } = 5000;
        public Dictionary<string, string> Properties { get; set; } = new();
    }

    /// <summary>
    /// Redis sink definition for atomic operations and exactly-once semantics
    /// </summary>
    public class RedisSinkDefinition : ISinkDefinition
    {
        public string Type => "redis";
        public string ConnectionString { get; set; } = string.Empty;
        public string? Key { get; set; } // Redis key for operations
        public string OperationType { get; set; } = "increment"; // increment, set, sadd, etc.
        public Dictionary<string, object> Configuration { get; set; } = new();
    }
}