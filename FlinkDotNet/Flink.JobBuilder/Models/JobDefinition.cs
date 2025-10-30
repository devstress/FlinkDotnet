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
        /// <summary>
        /// Data source for the streaming job
        /// </summary>
        public ISourceDefinition Source { get; set; } = null!;

        /// <summary>
        /// List of operations to apply to the data stream
        /// </summary>
        public List<IOperationDefinition> Operations { get; init; } = [];

        /// <summary>
        /// Data sink for the streaming job (nullable for pure SQL jobs)
        /// </summary>
        public ISinkDefinition? Sink
        {
            get; set;
        } // nullable to allow pure SQL jobs

        /// <summary>
        /// Job metadata including ID, name, version, and properties
        /// </summary>
        public JobMetadata Metadata { get; set; } = new();
    }

    /// <summary>
    /// Metadata about the job
    /// </summary>
    public class JobMetadata
    {
        /// <summary>
        /// Unique identifier for the job
        /// </summary>
        public string JobId { get; set; } = string.Empty;

        /// <summary>
        /// Human-readable name for the job
        /// </summary>
        public string? JobName
        {
            get; set;
        }

        /// <summary>
        /// Timestamp when the job was created
        /// </summary>
        public DateTime CreatedAt
        {
            get; set;
        }

        /// <summary>
        /// Version of the job definition
        /// </summary>
        public string Version { get; set; } = string.Empty;

        /// <summary>
        /// Parallelism level for job execution
        /// </summary>
        public int? Parallelism
        {
            get; set;
        }

        /// <summary>
        /// Additional properties for job configuration
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];

        /// <summary>
        /// Optional state backend configuration for performance tuning (Flink 2.1+)
        /// </summary>
        public StateBackendConfig? StateBackendConfig
        {
            get; set;
        }

        /// <summary>
        /// Optional execution plan format configuration (Flink 2.1+)
        /// </summary>
        public ExecutionPlanConfig? ExecutionPlanConfig
        {
            get; set;
        }

        /// <summary>
        /// Optional optimizer configuration for query optimization (Flink 2.1+)
        /// </summary>
        public OptimizerConfig? OptimizerConfig
        {
            get; set;
        }
    }

    /// <summary>
    /// Base interface for all source definitions
    /// </summary>
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
    [JsonDerivedType(typeof(KafkaSourceDefinition), "kafka")]
    [JsonDerivedType(typeof(FileSourceDefinition), "file")]
    [JsonDerivedType(typeof(HttpSourceDefinition), "http")]
    [JsonDerivedType(typeof(DatabaseSourceDefinition), "database")]
    [JsonDerivedType(typeof(SqlSourceDefinition), "sql")]
    [JsonDerivedType(typeof(MaterializedTableDefinition), "materialized_table")]
    [JsonDerivedType(typeof(ModelDefinition), "model")]
    [JsonDerivedType(typeof(PaimonTableDefinition), "paimon_table")]
    [JsonDerivedType(typeof(TableSourceDefinition), "table")]
    public interface ISourceDefinition
    {
        /// <summary>
        /// Type discriminator for the source (kafka, file, http, database, sql, materialized_table)
        /// </summary>
        public string Type
        {
            get;
        }
    }

    /// <summary>
    /// Kafka source definition
    /// </summary>
    public class KafkaSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "kafka";
        /// <summary>
        /// Gets or sets the topic
        /// </summary>
        public string Topic { get; set; } = string.Empty;
        public string? BootstrapServers
        {
            get; set;
        }
        public string? GroupId
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the starting offsets
        /// </summary>
        public string? StartingOffsets { get; set; } = "earliest"; // earliest, latest, or specific offsets
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// File source definition
    /// </summary>
    public class FileSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "file";
        /// <summary>
        /// Gets or sets the path
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the format
        /// </summary>
        public string Format { get; set; } = "text"; // text, json, csv, etc.
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// HTTP source definition for REST API calls
    /// </summary>
    public class HttpSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "http";
        /// <summary>
        /// Gets or sets the url
        /// </summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the method
        /// </summary>
        public string Method { get; set; } = "GET";
        /// <summary>
        /// Gets or sets the headers
        /// </summary>
        public Dictionary<string, string> Headers { get; init; } = [];
        public string? Body
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the interval seconds
        /// </summary>
        public int IntervalSeconds { get; set; } = 60; // Polling interval for continuous requests
        public string? AuthTokenStateKey
        {
            get; set;
        } // Key for cached auth token
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Database source definition
    /// </summary>
    public class DatabaseSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "database";
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the query
        /// </summary>
        public string Query { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the database type
        /// </summary>
        public string? DatabaseType { get; set; } = "postgresql";
        /// <summary>
        /// Gets or sets the polling interval seconds
        /// </summary>
        public int PollingIntervalSeconds { get; set; } = 30;
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// SQL job: a list of Flink SQL statements (DDL/DML) executed by Table API or SQL Gateway
    /// </summary>
    public class SqlSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "sql";
        /// <summary>
        /// Gets or sets the statements
        /// </summary>
        public List<string> Statements { get; init; } = [];
        /// <summary>
        /// Gets or sets the mode
        /// </summary>
        public string Mode { get; set; } = "streaming"; // streaming or batch (future)

        /// <summary>
        /// Execution mode: "tableenv" (default, uses TableEnvironment) or "gateway" (uses Flink SQL Gateway REST API)
        /// </summary>
        public string ExecutionMode { get; set; } = "tableenv";

        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Materialized Table definition for Flink 1.20+ (FLIP-435)
    /// Represents a declarative SQL pattern for both batch and streaming ETL with automatic refresh management
    /// </summary>
    public class MaterializedTableDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "materialized_table";

        /// <summary>
        /// Name of the materialized table
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// SQL query defining the materialized table content
        /// </summary>
        public string Query { get; set; } = string.Empty;

        /// <summary>
        /// Refresh mode: "FULL" (complete refresh) or "CONTINUOUS" (streaming)
        /// </summary>
        public string RefreshMode { get; set; } = "CONTINUOUS";

        /// <summary>
        /// Freshness interval (e.g., "INTERVAL '3' MINUTE", "INTERVAL '1' HOUR")
        /// </summary>
        public string? FreshnessInterval
        {
            get; set;
        }

        /// <summary>
        /// Primary key columns (NOT ENFORCED in Flink)
        /// </summary>
        public List<string> PrimaryKey { get; init; } = [];

        /// <summary>
        /// Partition columns
        /// </summary>
        public List<string> PartitionBy { get; init; } = [];

        /// <summary>
        /// Schema definition for the materialized table (column_name: data_type)
        /// </summary>
        public Dictionary<string, string> Schema { get; init; } = [];

        /// <summary>
        /// Operation to perform: "CREATE", "SUSPEND", "RESUME", "REFRESH", "DROP"
        /// </summary>
        public string Operation { get; set; } = "CREATE";

        /// <summary>
        /// Partition filter for REFRESH operation (e.g., "ds='2024-10-27'")
        /// </summary>
        public string? PartitionFilter
        {
            get; set;
        }

        /// <summary>
        /// Additional table properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];

        /// <summary>
        /// Execution mode: "tableenv" (default) or "gateway" (uses Flink SQL Gateway REST API)
        /// </summary>
        public string ExecutionMode { get; set; } = "gateway";
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
    [JsonDerivedType(typeof(TableOperationDefinition), "table")]
    [JsonDerivedType(typeof(ParseJsonOperationDefinition), "parseJson")]
    [JsonDerivedType(typeof(ProcessTableFunctionDefinition), "processTableFunction")]
    [JsonDerivedType(typeof(WindowTvfOperationDefinition), "windowTvf")]
    [JsonDerivedType(typeof(MLPredictDefinition), "ml_predict")]
    public interface IOperationDefinition
    {
        public string Type
        {
            get;
        }
    }

    /// <summary>
    /// Filter operation definition
    /// </summary>
    public class FilterOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "filter";
        /// <summary>
        /// Gets or sets the expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;
    }

    /// <summary>
    /// Map/transform operation definition
    /// </summary>
    public class MapOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "map";
        /// <summary>
        /// Gets or sets the expression
        /// </summary>
        public string Expression { get; set; } = string.Empty;
        public string? OutputType
        {
            get; set;
        }
    }

    /// <summary>
    /// Group by operation definition
    /// </summary>
    public class GroupByOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "groupBy";
        /// <summary>
        /// Gets or sets the key
        /// </summary>
        public string Key { get; set; } = string.Empty;
        public List<string>? Keys
        {
            get; init;
        } // For multi-key grouping
    }

    /// <summary>
    /// Aggregation operation definition
    /// </summary>
    public class AggregateOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "aggregate";
        /// <summary>
        /// Gets or sets the aggregation type
        /// </summary>
        public string AggregationType { get; set; } = string.Empty; // SUM, COUNT, AVG, MIN, MAX, COLLECT
        /// <summary>
        /// Gets or sets the field
        /// </summary>
        public string Field { get; set; } = string.Empty;
        public string? Alias
        {
            get; set;
        }

        /// <summary>
        /// Window duration in seconds for time-based windowed aggregation.
        /// Used by FlinkJobRunner to configure TumblingProcessingTimeWindows.
        /// For testing: 10 seconds
        /// For production: 86400 seconds (24 hours)
        /// </summary>
        public long? WindowSeconds
        {
            get; set;
        }

        /// <summary>
        /// Window count for count-based windowed aggregation (Baeldung Exercise 2 pattern).
        /// Used by FlinkJobRunner to configure countWindow.
        /// Example: 50 messages aggregate into 1 Backup object
        /// </summary>
        public int? WindowCount
        {
            get; set;
        }
    }

    /// <summary>
    /// Windowing operation definition
    /// </summary>
    public class WindowOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "window";
        /// <summary>
        /// Gets or sets the window type
        /// </summary>
        public string WindowType { get; set; } = string.Empty; // TUMBLING, SLIDING, SESSION
        public int Size
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the time unit
        /// </summary>
        public string TimeUnit { get; set; } = "MINUTES"; // SECONDS, MINUTES, HOURS
        public int? Slide
        {
            get; set;
        } // For sliding windows
        public string? TimeField
        {
            get; set;
        } // Field to use for event time
    }

    /// <summary>
    /// Join operation definition
    /// </summary>
    public class JoinOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "join";
        /// <summary>
        /// Gets or sets the join type
        /// </summary>
        public string JoinType { get; set; } = "INNER"; // INNER, LEFT, RIGHT, FULL
        /// <summary>
        /// Gets or sets the right source
        /// </summary>
        public ISourceDefinition RightSource { get; set; } = null!;
        /// <summary>
        /// Gets or sets the left key
        /// </summary>
        public string LeftKey { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the right key
        /// </summary>
        public string RightKey { get; set; } = string.Empty;
        public WindowOperationDefinition? Window
        {
            get; set;
        }
    }

    /// <summary>
    /// Async function operation for non-blocking I/O operations
    /// </summary>
    public class AsyncFunctionOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "asyncFunction";
        /// <summary>
        /// Gets or sets the function type
        /// </summary>
        public string FunctionType { get; set; } = string.Empty; // http, database, etc.
        /// <summary>
        /// Gets or sets the url
        /// </summary>
        public string Url { get; set; } = string.Empty; // For HTTP calls
        /// <summary>
        /// Gets or sets the method
        /// </summary>
        public string Method { get; set; } = "GET";
        /// <summary>
        /// Gets or sets the headers
        /// </summary>
        public Dictionary<string, string> Headers { get; init; } = [];
        public string? BodyTemplate
        {
            get; set;
        } // Template for request body
        public string? ConnectionString
        {
            get; set;
        } // For database calls
        public string? Query
        {
            get; set;
        } // For database queries
        /// <summary>
        /// Gets or sets the timeout ms
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;
        /// <summary>
        /// Gets or sets the max retries
        /// </summary>
        public int MaxRetries { get; set; } = 3;
        public string? StateKey
        {
            get; set;
        } // For caching results
        public long? CacheTtlMs
        {
            get; set;
        } // Cache time-to-live
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Process function operation for complex stateful logic
    /// </summary>
    public class ProcessFunctionOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "processFunction";
        /// <summary>
        /// Gets or sets the process type
        /// </summary>
        public string ProcessType { get; set; } = string.Empty; // authTokenManager, retryHandler, etc.
        /// <summary>
        /// Gets or sets the parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = [];
        /// <summary>
        /// Gets or sets the state keys
        /// </summary>
        public List<string> StateKeys { get; init; } = [];
        /// <summary>
        /// Gets or sets the timer names
        /// </summary>
        public List<string> TimerNames { get; init; } = [];
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// State operation for managing stateful data
    /// </summary>
    public class StateOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "state";
        /// <summary>
        /// Gets or sets the state type
        /// </summary>
        public string StateType { get; set; } = "value"; // value, list, map, reducing
        /// <summary>
        /// Gets or sets the state key
        /// </summary>
        public string StateKey { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the value type
        /// </summary>
        public string? ValueType { get; set; } = "string";
        public long? TtlMs
        {
            get; set;
        } // Time-to-live for state cleanup
        public string? DefaultValue
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Timer operation for scheduled processing
    /// </summary>
    public class TimerOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "timer";
        /// <summary>
        /// Gets or sets the timer type
        /// </summary>
        public string TimerType { get; set; } = "processing"; // processing, event
        public long DelayMs
        {
            get; set;
        }
        public string? TimerName
        {
            get; set;
        }
        public string? Action
        {
            get; set;
        } // What to do when timer fires
        /// <summary>
        /// Gets or sets the parameters
        /// </summary>
        public Dictionary<string, object> Parameters { get; init; } = [];
    }

    /// <summary>
    /// Retry operation with exponential backoff
    /// </summary>
    public class RetryOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "retry";
        /// <summary>
        /// Gets or sets the max retries
        /// </summary>
        public int MaxRetries { get; set; } = 5;
        /// <summary>
        /// Gets or sets the delay ms
        /// </summary>
        public List<long> DelayMs { get; init; } = [300_000, 600_000, 1_800_000, 3_600_000, 86_400_000]; // 5min, 10min, 30min, 1hr, 1day
        public string? RetryCondition
        {
            get; set;
        } // Condition to determine if retry is needed
        public string? DeadLetterTopic
        {
            get; set;
        } // Topic for permanent failures
        /// <summary>
        /// Gets or sets the state key
        /// </summary>
        public string StateKey { get; set; } = "retry_state";
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Side output operation for error handling and dead letter patterns
    /// </summary>
    public class SideOutputOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "sideOutput";
        /// <summary>
        /// Gets or sets the output tag
        /// </summary>
        public string OutputTag { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the condition
        /// </summary>
        public string Condition { get; set; } = string.Empty; // When to route to side output
        /// <summary>
        /// Gets or sets the side output sink
        /// </summary>
        public ISinkDefinition SideOutputSink { get; set; } = null!;
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
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
    [JsonDerivedType(typeof(UnifiedSinkV2Definition), "unified_sink_v2")]
    public interface ISinkDefinition
    {
        public string Type
        {
            get;
        }
    }

    /// <summary>
    /// Kafka sink definition
    /// </summary>
    public class KafkaSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "kafka";
        /// <summary>
        /// Gets or sets the topic
        /// </summary>
        public string Topic { get; set; } = string.Empty;
        public string? BootstrapServers
        {
            get; set;
        }
        /// <summary>
        /// Gets or sets the serializer
        /// </summary>
        public string? Serializer { get; set; } = "json";
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Console sink definition (for debugging)
    /// </summary>
    public class ConsoleSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "console";
        /// <summary>
        /// Gets or sets the format
        /// </summary>
        public string? Format { get; set; } = "json";
    }

    /// <summary>
    /// File sink definition
    /// </summary>
    public class FileSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "file";
        /// <summary>
        /// Gets or sets the path
        /// </summary>
        public string Path { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the format
        /// </summary>
        public string Format { get; set; } = "json"; // json, csv, parquet, etc.
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Database sink definition
    /// </summary>
    public class DatabaseSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "database";
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the table
        /// </summary>
        public string Table { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the database type
        /// </summary>
        public string? DatabaseType { get; set; } = "postgresql"; // postgresql, mysql, etc.
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// HTTP sink definition for REST API calls
    /// </summary>
    public class HttpSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "http";
        /// <summary>
        /// Gets or sets the url
        /// </summary>
        public string Url { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the method
        /// </summary>
        public string Method { get; set; } = "POST";
        /// <summary>
        /// Gets or sets the headers
        /// </summary>
        public Dictionary<string, string> Headers { get; init; } = [];
        public string? BodyTemplate
        {
            get; set;
        } // Template for request body
        public string? AuthTokenStateKey
        {
            get; set;
        } // Key for cached auth token
        /// <summary>
        /// Gets or sets the timeout ms
        /// </summary>
        public int TimeoutMs { get; set; } = 5000;
        /// <summary>
        /// Gets or sets the properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Redis sink definition for atomic operations and exactly-once semantics
    /// </summary>
    public class RedisSinkDefinition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "redis";
        /// <summary>
        /// Gets or sets the connection string
        /// </summary>
        public string ConnectionString { get; set; } = string.Empty;
        public string? Key
        {
            get; set;
        } // Redis key for operations
        /// <summary>
        /// Gets or sets the operation type
        /// </summary>
        public string OperationType { get; set; } = "increment"; // increment, set, sadd, etc.
        /// <summary>
        /// Gets or sets the configuration
        /// </summary>
        public Dictionary<string, object> Configuration { get; init; } = [];
    }

    /// <summary>
    /// Unified Sink API v2 definition (Flink 1.20+) for modern sink pattern with exactly-once semantics
    /// </summary>
    public class UnifiedSinkV2Definition : ISinkDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "unified_sink_v2";

        /// <summary>
        /// Type of sink (kafka, file, database, http, custom)
        /// </summary>
        public string SinkType { get; set; } = string.Empty;

        /// <summary>
        /// Writer configuration
        /// </summary>
        public SinkWriterConfig WriterConfig { get; set; } = new();

        /// <summary>
        /// Committer configuration (optional, for exactly-once semantics)
        /// </summary>
        public SinkCommitterConfig? CommitterConfig
        {
            get; set;
        }

        /// <summary>
        /// Delivery semantics: exactly-once or at-least-once
        /// </summary>
        public string Semantics { get; set; } = "at-least-once";

        /// <summary>
        /// Whether sink writer is stateful (supports state snapshots)
        /// </summary>
        public bool Stateful
        {
            get; set;
        }

        /// <summary>
        /// Additional properties for sink configuration
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Configuration for Unified Sink v2 writer
    /// </summary>
    public class SinkWriterConfig
    {
        /// <summary>
        /// Writer class name (for custom sinks)
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Writer-specific properties
        /// </summary>
        public Dictionary<string, object> Properties { get; init; } = [];

        /// <summary>
        /// Optional batching configuration for async sink performance optimization
        /// </summary>
        public BatchingConfig? BatchingConfig
        {
            get; set;
        }
    }

    /// <summary>
    /// Configuration for Unified Sink v2 committer (two-phase commit)
    /// </summary>
    public class SinkCommitterConfig
    {
        /// <summary>
        /// Whether committer is enabled
        /// </summary>
        public bool Enabled
        {
            get; set;
        }

        /// <summary>
        /// Committer class name (for custom committers)
        /// </summary>
        public string? ClassName
        {
            get; set;
        }

        /// <summary>
        /// Committer-specific properties
        /// </summary>
        public Dictionary<string, object> Properties { get; init; } = [];
    }

    /// <summary>
    /// Batching configuration for async sink performance optimization (Flink 2.1+)
    /// </summary>
    public class BatchingConfig
    {
        /// <summary>
        /// Maximum number of records per batch
        /// </summary>
        public int? MaxBatchSize
        {
            get; set;
        }

        /// <summary>
        /// Maximum batch size in bytes
        /// </summary>
        public long? MaxBatchSizeInBytes
        {
            get; set;
        }

        /// <summary>
        /// Maximum time in milliseconds to buffer records before flushing
        /// </summary>
        public int? MaxTimeInBufferMs
        {
            get; set;
        }

        /// <summary>
        /// Maximum number of in-flight requests
        /// </summary>
        public int? MaxInFlightRequests
        {
            get; set;
        }

        /// <summary>
        /// Maximum number of buffered requests
        /// </summary>
        public int? MaxBufferedRequests
        {
            get; set;
        }
    }

    /// <summary>
    /// State backend configuration for performance tuning (Flink 2.1+)
    /// </summary>
    public class StateBackendConfig
    {
        /// <summary>
        /// State backend type: "rocksdb", "hashmap", "filesystem"
        /// </summary>
        public string Type { get; set; } = "rocksdb";

        /// <summary>
        /// Checkpoint directory URI (s3://, hdfs://, file://)
        /// </summary>
        public string? CheckpointDir
        {
            get; set;
        }

        /// <summary>
        /// Enable incremental checkpointing (RocksDB only)
        /// </summary>
        public bool? IncrementalCheckpoints
        {
            get; set;
        }

        /// <summary>
        /// Predefined RocksDB profile: "default", "flash_ssd_optimized", "spinning_disk_optimized"
        /// </summary>
        public string? PredefinedProfile
        {
            get; set;
        }

        /// <summary>
        /// RocksDB database options
        /// </summary>
        public Dictionary<string, object>? DbOptions
        {
            get; init;
        }

        /// <summary>
        /// RocksDB column family options
        /// </summary>
        public Dictionary<string, object>? ColumnFamilyOptions
        {
            get; init;
        }
    }

    /// <summary>
    /// Execution plan format configuration (Flink 2.1+)
    /// Configures plan serialization format for compiled execution plans
    /// </summary>
    public class ExecutionPlanConfig
    {
        /// <summary>
        /// Plan serialization format: "json" or "smile" (binary JSON)
        /// </summary>
        public string Format { get; set; } = "json";

        /// <summary>
        /// Enable compression for plan serialization
        /// </summary>
        public bool? EnableCompression
        {
            get; set;
        }

        /// <summary>
        /// Additional format-specific properties
        /// </summary>
        public Dictionary<string, object>? Properties
        {
            get; init;
        }
    }

    /// <summary>
    /// Optimizer configuration for query optimization (Flink 2.1+)
    /// Provides hints and controls for Flink's query optimizer
    /// </summary>
    public class OptimizerConfig
    {
        /// <summary>
        /// Enable multi-join optimization for cascaded joins
        /// </summary>
        public bool? EnableMultiJoinOptimization
        {
            get; set;
        }

        /// <summary>
        /// Join reordering strategy: "none", "cost_based", "bushy", "left_deep"
        /// </summary>
        public string? JoinReorderingStrategy
        {
            get; set;
        }

        /// <summary>
        /// Enable join predicate pushdown optimization
        /// </summary>
        public bool? EnableJoinPredicatePushdown
        {
            get; set;
        }

        /// <summary>
        /// Enable filter pushdown optimization
        /// </summary>
        public bool? EnableFilterPushdown
        {
            get; set;
        }

        /// <summary>
        /// Additional optimizer hints and properties
        /// </summary>
        public Dictionary<string, object>? Properties
        {
            get; init;
        }
    }

    /// <summary>
    /// AI/ML Model definition for Flink 2.1+ MODEL DDL support
    /// </summary>
    public class ModelDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "model";

        /// <summary>
        /// Name of the AI/ML model
        /// </summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>
        /// Input schema definition (column_name: data_type)
        /// </summary>
        public Dictionary<string, string> InputSchema
        {
            get; init;
        } = [];

        /// <summary>
        /// Output schema definition (column_name: data_type)
        /// </summary>
        public Dictionary<string, string> OutputSchema
        {
            get; init;
        } = [];

        /// <summary>
        /// AI provider: "openai", "azure_openai", "custom", etc.
        /// </summary>
        public string Provider
        {
            get; set;
        } = string.Empty;

        /// <summary>
        /// Provider-specific properties (e.g., API keys, endpoints, model names)
        /// </summary>
        public Dictionary<string, string> Properties
        {
            get; init;
        } = [];

        /// <summary>
        /// Operation to perform: "CREATE", "ALTER", "DROP", "SHOW", "DESCRIBE"
        /// </summary>
        public string Operation
        {
            get; set;
        } = "CREATE";

        /// <summary>
        /// Execution mode: "gateway" (SQL Gateway execution)
        /// </summary>
        public string ExecutionMode
        {
            get; set;
        } = "gateway";
    }

    /// <summary>
    /// Table API operation definition for fluent table transformations
    /// Supports programmatic table operations without SQL strings
    /// </summary>
    public class TableOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "table";

        /// <summary>
        /// Operation type: "select", "where", "groupBy", "join", "window", "aggregate"
        /// </summary>
        public string OperationType { get; set; } = string.Empty;

        /// <summary>
        /// Column/field selections or expressions
        /// </summary>
        public List<string> Columns { get; init; } = [];

        /// <summary>
        /// Filter condition (for where operations)
        /// </summary>
        public string? Condition
        {
            get; set;
        }

        /// <summary>
        /// Group by keys (for groupBy operations)
        /// </summary>
        public List<string> GroupByKeys { get; init; } = [];

        /// <summary>
        /// Aggregation functions (for aggregate operations)
        /// Format: "function(column) AS alias"
        /// </summary>
        public List<string> Aggregations { get; init; } = [];

        /// <summary>
        /// Table-specific properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// PARSE_JSON operation definition for converting JSON strings to VARIANT type
    /// Supports both PARSE_JSON (strict) and TRY_PARSE_JSON (lenient) functions
    /// </summary>
    public class ParseJsonOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "parseJson";

        /// <summary>
        /// Function type: "PARSE_JSON" (throws on invalid JSON) or "TRY_PARSE_JSON" (returns NULL on invalid JSON)
        /// </summary>
        public string FunctionType { get; set; } = "TRY_PARSE_JSON";

        /// <summary>
        /// Source field containing JSON string
        /// </summary>
        public string SourceField { get; set; } = string.Empty;

        /// <summary>
        /// Target field for VARIANT result
        /// </summary>
        public string TargetField { get; set; } = string.Empty;

        /// <summary>
        /// JSON path for extracting nested values (e.g., "$.user.name", "$['metadata']['tags'][0]")
        /// If null or empty, parses entire JSON
        /// </summary>
        public string? JsonPath
        {
            get; set;
        }

        /// <summary>
        /// Output data type hint (e.g., "STRING", "INT", "DOUBLE")
        /// Used when JSON path points to a specific primitive type
        /// </summary>
        public string? OutputType
        {
            get; set;
        }
    }

    /// <summary>
    /// Apache Paimon catalog definition for lakehouse table storage
    /// </summary>
    public class PaimonCatalogDefinition
    {
        /// <summary>
        /// Name of the Paimon catalog
        /// </summary>
        public string CatalogName { get; set; } = string.Empty;

        /// <summary>
        /// Catalog type: "paimon" (filesystem) or "paimon-generic" (Hive metastore)
        /// </summary>
        public string CatalogType { get; set; } = "paimon";

        /// <summary>
        /// Warehouse path for Paimon tables (file://, hdfs://, s3://, oss://)
        /// </summary>
        public string Warehouse { get; set; } = string.Empty;

        /// <summary>
        /// Optional Hive configuration directory for Hive metastore integration
        /// </summary>
        public string? HiveConfDir
        {
            get; set;
        }

        /// <summary>
        /// Optional Hadoop configuration directory for HDFS integration
        /// </summary>
        public string? HadoopConfDir
        {
            get; set;
        }

        /// <summary>
        /// Additional catalog properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Apache Paimon table definition with ACID semantics
    /// </summary>
    public class PaimonTableDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "paimon_table";

        /// <summary>
        /// Name of the catalog containing this table
        /// </summary>
        public string CatalogName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the Paimon table
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Schema definition for the table (column_name: data_type)
        /// </summary>
        public Dictionary<string, string> Schema { get; init; } = [];

        /// <summary>
        /// Primary key columns (required for ACID semantics)
        /// </summary>
        public List<string> PrimaryKey { get; init; } = [];

        /// <summary>
        /// Partition columns for data organization
        /// </summary>
        public List<string> PartitionKeys { get; init; } = [];

        /// <summary>
        /// Number of buckets for parallelism (optional)
        /// </summary>
        public int? Buckets
        {
            get; set;
        }

        /// <summary>
        /// Changelog producer mode: "none", "input", "lookup", "full-compaction"
        /// </summary>
        public string ChangelogProducerMode { get; set; } = "none";

        /// <summary>
        /// Table properties (e.g., compaction settings, snapshot retention)
        /// </summary>
        public Dictionary<string, string> TableProperties { get; init; } = [];

        /// <summary>
        /// Operation to perform: "CREATE", "DROP", "INSERT", "QUERY"
        /// </summary>
        public string Operation { get; set; } = "CREATE";
    }

    /// <summary>
    /// Table source definition for Table API operations
    /// Represents a table in Flink's catalog that can be queried programmatically
    /// </summary>
    public class TableSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "table";

        /// <summary>
        /// Table name in the catalog
        /// </summary>
        public string TableName { get; set; } = string.Empty;

        /// <summary>
        /// Catalog name (optional, uses default catalog if not specified)
        /// </summary>
        public string? CatalogName
        {
            get; set;
        }

        /// <summary>
        /// Database name (optional, uses default database if not specified)
        /// </summary>
        public string? DatabaseName
        {
            get; set;
        }

        /// <summary>
        /// Schema definition (column_name: data_type including VARIANT)
        /// </summary>
        public Dictionary<string, string> Schema { get; init; } = [];

        /// <summary>
        /// Additional table properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Unified Source API (FLIP-27) definition for modern source connectors (Flink 1.12+)
    /// Supports split discovery, enumeration, and exactly-once semantics
    /// </summary>
    public class UnifiedSourceDefinition : ISourceDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "unifiedSource";

        /// <summary>
        /// Source type (kafka, file, custom, etc.)
        /// </summary>
        public string SourceType { get; set; } = string.Empty;

        /// <summary>
        /// Boundedness of the source (bounded or unbounded)
        /// </summary>
        public string Boundedness { get; set; } = "unbounded";

        /// <summary>
        /// Bootstrap servers for Kafka sources
        /// </summary>
        public string? BootstrapServers { get; set; }

        /// <summary>
        /// Topics to consume from (Kafka sources)
        /// </summary>
        public List<string> Topics { get; init; } = [];

        /// <summary>
        /// Consumer group ID (Kafka sources)
        /// </summary>
        public string? GroupId { get; set; }

        /// <summary>
        /// Starting offsets strategy (earliest, latest, group, timestamp, specific)
        /// </summary>
        public string StartingOffsets { get; set; } = "latest";

        /// <summary>
        /// Stopping offsets strategy for bounded sources (latest, timestamp, specific, unbounded)
        /// </summary>
        public string StoppingOffsets { get; set; } = "unbounded";

        /// <summary>
        /// File paths for file sources
        /// </summary>
        public List<string> FilePaths { get; init; } = [];

        /// <summary>
        /// File format (csv, json, parquet, etc.) for file sources
        /// </summary>
        public string? FileFormat { get; set; }

        /// <summary>
        /// Additional source properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];

        /// <summary>
        /// Schema definition for deserialization
        /// </summary>
        public Dictionary<string, string> Schema { get; init; } = [];
    }

    /// <summary>
    /// Process Table Function (PTF) operation definition for stateful table UDFs
    /// Supports advanced stateful processing with timers and managed state
    /// </summary>
    public class ProcessTableFunctionDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "processTableFunction";

        /// <summary>
        /// Function name for registration in table environment
        /// </summary>
        public string FunctionName { get; set; } = string.Empty;

        /// <summary>
        /// Fully qualified class name of the PTF implementation
        /// </summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>
        /// Input column names that the function processes
        /// </summary>
        public List<string> InputColumns { get; init; } = [];

        /// <summary>
        /// Output column names that the function produces
        /// </summary>
        public List<string> OutputColumns { get; init; } = [];

        /// <summary>
        /// State descriptors for managed state used by the function
        /// Key: state name, Value: state type (e.g., "ValueState&lt;SessionData&gt;")
        /// </summary>
        public Dictionary<string, string> StateDescriptors { get; init; } = [];

        /// <summary>
        /// Whether the function uses event-time timers
        /// </summary>
        public bool UsesEventTimeTimers
        {
            get; set;
        }

        /// <summary>
        /// Whether the function uses processing-time timers
        /// </summary>
        public bool UsesProcessingTimeTimers
        {
            get; set;
        }

        /// <summary>
        /// Function-specific properties and configuration
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Window Table-Valued Function (TVF) operation definition
    /// Supports modern SQL window functions: TUMBLE, HOP, CUMULATE
    /// </summary>
    public class WindowTvfOperationDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier
        /// </summary>
        [JsonIgnore]
        public string Type => "windowTvf";

        /// <summary>
        /// Window function type: "TUMBLE", "HOP", or "CUMULATE"
        /// </summary>
        public string WindowType { get; set; } = string.Empty;

        /// <summary>
        /// Time attribute column for windowing (e.g., "event_time")
        /// </summary>
        public string TimeColumn { get; set; } = string.Empty;

        /// <summary>
        /// Window size (e.g., "INTERVAL '1' HOUR")
        /// </summary>
        public string WindowSize { get; set; } = string.Empty;

        /// <summary>
        /// Window slide/hop interval (for HOP windows)
        /// </summary>
        public string? SlideInterval
        {
            get; set;
        }

        /// <summary>
        /// Maximum window size (for CUMULATE windows)
        /// </summary>
        public string? MaxWindowSize
        {
            get; set;
        }

        /// <summary>
        /// Group by columns after windowing
        /// </summary>
        public List<string> GroupByColumns { get; init; } = [];

        /// <summary>
        /// Aggregation functions to apply
        /// </summary>
        public List<string> Aggregations { get; init; } = [];

        /// <summary>
        /// Additional window properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// ML_PREDICT Table Value Function operation definition (Flink 2.1+)
    /// Enables real-time AI inference on streaming data using registered models.
    /// </summary>
    public class MLPredictDefinition : IOperationDefinition
    {
        /// <summary>
        /// Gets the type identifier for ML_PREDICT operation
        /// </summary>
        [JsonIgnore]
        public string Type => "ml_predict";

        /// <summary>
        /// Name of the registered model to use for predictions
        /// </summary>
        public string ModelName
        {
            get; set;
        } = string.Empty;

        /// <summary>
        /// List of input column names to pass to the model (used in DESCRIPTOR clause)
        /// </summary>
        public List<string> InputColumns
        {
            get; init;
        } = [];

        /// <summary>
        /// List of output column names from the model prediction
        /// </summary>
        public List<string> OutputColumns
        {
            get; init;
        } = [];

        /// <summary>
        /// Optional output prefix/alias for ML_PREDICT result (e.g., "ml" in "AS ml")
        /// </summary>
        public string? OutputPrefix
        {
            get; set;
        }
    }

    /// <summary>
    /// Generic catalog definition for Flink 1.10+ Catalog API
    /// Supports Hive, JDBC, and GenericInMemory catalog types
    /// </summary>
    public class CatalogDefinition
    {
        /// <summary>
        /// Name of the catalog
        /// </summary>
        public string CatalogName { get; set; } = string.Empty;

        /// <summary>
        /// Catalog type: "hive", "jdbc", or "generic_in_memory"
        /// </summary>
        public string CatalogType { get; set; } = string.Empty;

        /// <summary>
        /// Default database name (optional)
        /// </summary>
        public string? DefaultDatabase
        {
            get; set;
        }

        /// <summary>
        /// Catalog-specific properties (e.g., hive-conf-dir, jdbc.url, jdbc.username)
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }

    /// <summary>
    /// Database definition for CREATE DATABASE operations
    /// </summary>
    public class DatabaseDefinition
    {
        /// <summary>
        /// Name of the catalog containing this database
        /// </summary>
        public string CatalogName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the database
        /// </summary>
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
        /// Whether to ignore if database already exists
        /// </summary>
        public bool IfNotExists { get; set; }

        /// <summary>
        /// Optional comment for the database
        /// </summary>
        public string? Comment
        {
            get; set;
        }

        /// <summary>
        /// Database properties
        /// </summary>
        public Dictionary<string, string> Properties { get; init; } = [];
    }
}
