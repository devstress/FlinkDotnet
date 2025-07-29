using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;

namespace Flink.JobBuilder
{
    /// <summary>
    /// Fluent C# DSL for building Apache Flink streaming jobs
    /// </summary>
    public class FlinkJobBuilder
    {
        private readonly List<IOperationDefinition> _operations = new();
        private ISourceDefinition? _source;
        private ISinkDefinition? _sink;
        private readonly ILogger? _logger;
        private readonly IFlinkJobGatewayService _gatewayService;

        public FlinkJobBuilder(IFlinkJobGatewayService? gatewayService = null, ILogger? logger = null)
        {
            _gatewayService = gatewayService ?? new FlinkJobGatewayService();
            _logger = logger;
        }

        /// <summary>
        /// Create a Kafka source for the streaming job
        /// </summary>
        /// <param name="topic">Kafka topic name</param>
        /// <param name="bootstrapServers">Kafka bootstrap servers (optional, can be configured via gateway)</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public static FlinkJobBuilder FromKafka(string topic, string? bootstrapServers = null)
        {
            var builder = new FlinkJobBuilder();
            builder._source = new KafkaSourceDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers
            };
            return builder;
        }

        /// <summary>
        /// Create an HTTP source for REST API polling
        /// </summary>
        /// <param name="url">HTTP URL to poll</param>
        /// <param name="method">HTTP method (default: GET)</param>
        /// <param name="intervalSeconds">Polling interval in seconds</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public static FlinkJobBuilder FromHttp(string url, string method = "GET", int intervalSeconds = 60)
        {
            var builder = new FlinkJobBuilder();
            builder._source = new HttpSourceDefinition
            {
                Url = url,
                Method = method,
                IntervalSeconds = intervalSeconds
            };
            return builder;
        }

        /// <summary>
        /// Create a database source for polling queries
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="pollingIntervalSeconds">Polling interval in seconds</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public static FlinkJobBuilder FromDatabase(string connectionString, string query, int pollingIntervalSeconds = 30)
        {
            var builder = new FlinkJobBuilder();
            builder._source = new DatabaseSourceDefinition
            {
                ConnectionString = connectionString,
                Query = query,
                PollingIntervalSeconds = pollingIntervalSeconds
            };
            return builder;
        }

        /// <summary>
        /// Add a filter operation to the job
        /// </summary>
        /// <param name="expression">Filter expression (e.g., "Amount > 100")</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Where(string expression)
        {
            _operations.Add(new FilterOperationDefinition
            {
                Expression = expression
            });
            return this;
        }

        /// <summary>
        /// Add a filter operation with a lambda expression
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <param name="predicate">Filter predicate</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Where<T>(Func<T, bool> predicate)
        {
            // Convert lambda to expression string (simplified approach)
            // In a real implementation, this would use expression trees for better translation
            var expression = predicate.Method.Name; // Placeholder - would need expression parsing
            return Where(expression);
        }

        /// <summary>
        /// Add a group by operation
        /// </summary>
        /// <param name="keyField">Field to group by (e.g., "Region")</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder GroupBy(string keyField)
        {
            _operations.Add(new GroupByOperationDefinition
            {
                Key = keyField
            });
            return this;
        }

        /// <summary>
        /// Add a group by operation with a lambda expression
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <typeparam name="TKey">Type of the key</typeparam>
        /// <param name="keySelector">Key selector function</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder GroupBy<T, TKey>(Func<T, TKey> keySelector)
        {
            // Convert lambda to expression string (simplified approach)
            var keyField = keySelector.Method.Name; // Placeholder
            return GroupBy(keyField);
        }

        /// <summary>
        /// Add an aggregation operation
        /// </summary>
        /// <param name="aggregationType">Type of aggregation (SUM, COUNT, AVG, etc.)</param>
        /// <param name="field">Field to aggregate</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Aggregate(string aggregationType, string field)
        {
            _operations.Add(new AggregateOperationDefinition
            {
                AggregationType = aggregationType,
                Field = field
            });
            return this;
        }

        /// <summary>
        /// Add an aggregation operation with a lambda expression
        /// </summary>
        /// <typeparam name="T">Type of the data</typeparam>
        /// <typeparam name="TResult">Type of the result</typeparam>
        /// <param name="aggregationType">Type of aggregation</param>
        /// <param name="fieldSelector">Field selector function</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Aggregate<T, TResult>(string aggregationType, Func<T, TResult> fieldSelector)
        {
            // Convert lambda to expression string (simplified approach)
            var field = fieldSelector.Method.Name; // Placeholder
            return Aggregate(aggregationType, field);
        }

        /// <summary>
        /// Add a map/transform operation
        /// </summary>
        /// <param name="expression">Transformation expression</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Map(string expression)
        {
            _operations.Add(new MapOperationDefinition
            {
                Expression = expression
            });
            return this;
        }

        /// <summary>
        /// Add a windowing operation
        /// </summary>
        /// <param name="windowType">Type of window (TUMBLING, SLIDING, etc.)</param>
        /// <param name="size">Window size</param>
        /// <param name="timeUnit">Time unit (SECONDS, MINUTES, etc.)</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder Window(string windowType, int size, string timeUnit = "MINUTES")
        {
            _operations.Add(new WindowOperationDefinition
            {
                WindowType = windowType,
                Size = size,
                TimeUnit = timeUnit
            });
            return this;
        }

        /// <summary>
        /// Add an async HTTP operation for non-blocking API calls
        /// </summary>
        /// <param name="url">HTTP URL to call</param>
        /// <param name="method">HTTP method</param>
        /// <param name="timeoutMs">Request timeout in milliseconds</param>
        /// <param name="headers">HTTP headers</param>
        /// <param name="bodyTemplate">Request body template</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder AsyncHttp(string url, string method = "GET", int timeoutMs = 5000, 
            Dictionary<string, string>? headers = null, string? bodyTemplate = null)
        {
            _operations.Add(new AsyncFunctionOperationDefinition
            {
                FunctionType = "http",
                Url = url,
                Method = method,
                TimeoutMs = timeoutMs,
                Headers = headers ?? new Dictionary<string, string>(),
                BodyTemplate = bodyTemplate
            });
            return this;
        }

        /// <summary>
        /// Add an async database operation
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="query">SQL query to execute</param>
        /// <param name="timeoutMs">Query timeout in milliseconds</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder AsyncDatabase(string connectionString, string query, int timeoutMs = 5000)
        {
            _operations.Add(new AsyncFunctionOperationDefinition
            {
                FunctionType = "database",
                ConnectionString = connectionString,
                Query = query,
                TimeoutMs = timeoutMs
            });
            return this;
        }

        /// <summary>
        /// Add state management for caching data
        /// </summary>
        /// <param name="stateKey">Key for the state</param>
        /// <param name="stateType">Type of state (value, list, map)</param>
        /// <param name="ttlMs">Time-to-live in milliseconds</param>
        /// <param name="defaultValue">Default value if state is empty</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder WithState(string stateKey, string stateType = "value", long? ttlMs = null, string? defaultValue = null)
        {
            _operations.Add(new StateOperationDefinition
            {
                StateKey = stateKey,
                StateType = stateType,
                TtlMs = ttlMs,
                DefaultValue = defaultValue
            });
            return this;
        }

        /// <summary>
        /// Add a timer for scheduled operations
        /// </summary>
        /// <param name="delayMs">Delay in milliseconds</param>
        /// <param name="timerName">Name of the timer</param>
        /// <param name="action">Action to perform when timer fires</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder WithTimer(long delayMs, string? timerName = null, string? action = null)
        {
            _operations.Add(new TimerOperationDefinition
            {
                DelayMs = delayMs,
                TimerName = timerName,
                Action = action
            });
            return this;
        }

        /// <summary>
        /// Add retry logic with exponential backoff
        /// </summary>
        /// <param name="maxRetries">Maximum number of retries</param>
        /// <param name="delayPattern">Delay pattern in milliseconds</param>
        /// <param name="retryCondition">Condition to determine if retry is needed</param>
        /// <param name="deadLetterTopic">Topic for permanent failures</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder WithRetry(int maxRetries = 5, 
            List<long>? delayPattern = null, 
            string? retryCondition = null, 
            string? deadLetterTopic = null)
        {
            _operations.Add(new RetryOperationDefinition
            {
                MaxRetries = maxRetries,
                DelayMs = delayPattern ?? new List<long> { 300000, 600000, 1800000, 3600000, 86400000 },
                RetryCondition = retryCondition,
                DeadLetterTopic = deadLetterTopic
            });
            return this;
        }

        /// <summary>
        /// Add a process function for complex stateful logic
        /// </summary>
        /// <param name="processType">Type of process function</param>
        /// <param name="parameters">Function parameters</param>
        /// <param name="stateKeys">State keys used by the function</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder WithProcessFunction(string processType, 
            Dictionary<string, object>? parameters = null, 
            List<string>? stateKeys = null)
        {
            _operations.Add(new ProcessFunctionOperationDefinition
            {
                ProcessType = processType,
                Parameters = parameters ?? new Dictionary<string, object>(),
                StateKeys = stateKeys ?? new List<string>()
            });
            return this;
        }

        /// <summary>
        /// Add side output for error handling
        /// </summary>
        /// <param name="outputTag">Tag for the side output</param>
        /// <param name="condition">Condition for routing to side output</param>
        /// <param name="sideOutputSink">Sink for the side output</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder WithSideOutput(string outputTag, string condition, ISinkDefinition sideOutputSink)
        {
            _operations.Add(new SideOutputOperationDefinition
            {
                OutputTag = outputTag,
                Condition = condition,
                SideOutputSink = sideOutputSink
            });
            return this;
        }

        /// <summary>
        /// Set the output to a Kafka topic
        /// </summary>
        /// <param name="topic">Kafka topic name</param>
        /// <param name="bootstrapServers">Kafka bootstrap servers (optional)</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder ToKafka(string topic, string? bootstrapServers = null)
        {
            _sink = new KafkaSinkDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers
            };
            return this;
        }

        /// <summary>
        /// Set the output to console (for debugging)
        /// </summary>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder ToConsole()
        {
            _sink = new ConsoleSinkDefinition();
            return this;
        }

        /// <summary>
        /// Set the output to an HTTP endpoint
        /// </summary>
        /// <param name="url">HTTP URL to send data to</param>
        /// <param name="method">HTTP method (default: POST)</param>
        /// <param name="headers">HTTP headers</param>
        /// <param name="bodyTemplate">Request body template</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder ToHttp(string url, string method = "POST", 
            Dictionary<string, string>? headers = null, string? bodyTemplate = null)
        {
            _sink = new HttpSinkDefinition
            {
                Url = url,
                Method = method,
                Headers = headers ?? new Dictionary<string, string>(),
                BodyTemplate = bodyTemplate
            };
            return this;
        }

        /// <summary>
        /// Set the output to a database
        /// </summary>
        /// <param name="connectionString">Database connection string</param>
        /// <param name="table">Target table name</param>
        /// <param name="databaseType">Database type (postgresql, mysql, etc.)</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder ToDatabase(string connectionString, string table, string databaseType = "postgresql")
        {
            _sink = new DatabaseSinkDefinition
            {
                ConnectionString = connectionString,
                Table = table,
                DatabaseType = databaseType
            };
            return this;
        }

        /// <summary>
        /// Set the output to Redis for atomic operations and exactly-once semantics
        /// </summary>
        /// <param name="key">Redis key for operations</param>
        /// <param name="connectionString">Redis connection string (optional)</param>
        /// <param name="operationType">Redis operation type (increment, set, sadd, etc.)</param>
        /// <returns>FlinkJobBuilder for method chaining</returns>
        public FlinkJobBuilder ToRedis(string key, string? connectionString = null, string operationType = "increment")
        {
            _sink = new RedisSinkDefinition
            {
                Key = key,
                ConnectionString = connectionString ?? "localhost:6379",
                OperationType = operationType,
                Configuration = new Dictionary<string, object>
                {
                    ["exactly_once"] = true,
                ["atomic_operations"] = true
                }
            };
            return this;
        }

        /// <summary>
        /// Generate the Intermediate Representation (IR) for this job
        /// </summary>
        /// <returns>JobDefinition containing the IR</returns>
        public JobDefinition BuildJobDefinition()
        {
            if (_source == null)
                throw new InvalidOperationException("Job must have a source. Use FromKafka() or similar method.");

            if (_sink == null)
                throw new InvalidOperationException("Job must have a sink. Use ToKafka(), ToConsole(), or similar method.");

            return new JobDefinition
            {
                Source = _source,
                Operations = _operations,
                Sink = _sink,
                Metadata = new JobMetadata
                {
                    JobId = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Version = "1.0"
                }
            };
        }

        /// <summary>
        /// Generate the JSON Intermediate Representation
        /// </summary>
        /// <returns>JSON string representing the job</returns>
        public string ToJson()
        {
            var jobDefinition = BuildJobDefinition();
            return JsonSerializer.Serialize(jobDefinition, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
        }

        /// <summary>
        /// Submit the job to the Flink Job Gateway
        /// </summary>
        /// <param name="jobName">Name for the job (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job submission result</returns>
        public async Task<JobSubmissionResult> Submit(string? jobName = null, CancellationToken cancellationToken = default)
        {
            var jobDefinition = BuildJobDefinition();
            if (!string.IsNullOrEmpty(jobName))
            {
                jobDefinition.Metadata.JobName = jobName;
            }

            _logger?.LogInformation("Submitting job to Flink Job Gateway: {JobId}", jobDefinition.Metadata.JobId);

            try
            {
                var result = await _gatewayService.SubmitJobAsync(jobDefinition, cancellationToken);
                _logger?.LogInformation("Job submitted successfully. Job ID: {JobId}, Flink Job ID: {FlinkJobId}", 
                    jobDefinition.Metadata.JobId, result.FlinkJobId);
                return result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to submit job {JobId} to gateway at {GatewayUrl}", 
                    jobDefinition.Metadata.JobId, _gatewayService.GetType().Name);
                throw new InvalidOperationException($"Job submission failed for JobId '{jobDefinition.Metadata.JobId}'. See inner exception for details.", ex);
            }
        }

        /// <summary>
        /// Submit the job and wait for completion (for bounded jobs)
        /// </summary>
        /// <param name="jobName">Name for the job (optional)</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job execution result</returns>
        public async Task<JobExecutionResult> SubmitAndWait(string? jobName = null, CancellationToken cancellationToken = default)
        {
            var submissionResult = await Submit(jobName, cancellationToken);
            
            _logger?.LogInformation("Waiting for job completion: {FlinkJobId}", submissionResult.FlinkJobId);

            // Poll for job completion
            while (!cancellationToken.IsCancellationRequested)
            {
                var status = await _gatewayService.GetJobStatusAsync(submissionResult.FlinkJobId, cancellationToken);
                
                if (status.State == "FINISHED")
                {
                    return new JobExecutionResult
                    {
                        JobId = submissionResult.JobId,
                        FlinkJobId = submissionResult.FlinkJobId,
                        State = status.State,
                        Success = true,
                        CompletedAt = DateTime.UtcNow
                    };
                }
                else if (status.State == "FAILED" || status.State == "CANCELED")
                {
                    return new JobExecutionResult
                    {
                        JobId = submissionResult.JobId,
                        FlinkJobId = submissionResult.FlinkJobId,
                        State = status.State,
                        Success = false,
                        Error = status.ErrorMessage,
                        CompletedAt = DateTime.UtcNow
                    };
                }

                // Wait before next poll
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }

            throw new OperationCanceledException("Job execution was canceled");
        }
    }
}