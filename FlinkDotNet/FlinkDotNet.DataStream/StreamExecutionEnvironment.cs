//  Licensed to the Apache Software Foundation (ASF) under one
//  or more contributor license agreements.  See the NOTICE file
//  distributed with this work for additional information
//  regarding copyright ownership.  The ASF licenses this file
//  to you under the Apache License, Version 2.0 (the
//  "License"); you may not use this file except in compliance
//  with the License.  You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using FlinkDotNet.Common;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.Logging;
using Serilog;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// The StreamExecutionEnvironment is the context in which a streaming program is executed.
    /// This is the main entry point for Flink DataStream API, equivalent to 
    /// pyflink.datastream.StreamExecutionEnvironment in Python Flink.
    /// </summary>
    public class StreamExecutionEnvironment
    {
        private readonly ExecutionConfig _executionConfig;
        private readonly ILogger? _logger;
        private static readonly Serilog.ILogger _serilogLogger = CreateLogger();
        
        private static Serilog.ILogger CreateLogger()
        {
            var logFilePath = System.Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs";
            var today = System.DateTime.UtcNow.ToString("yyyyMMdd");
            var logFile = System.IO.Path.Combine(logFilePath, $"FlinkDotnet.log.{today}");
            
            // Clean up old log files (older than 1 day)
            try
            {
                if (System.IO.Directory.Exists(logFilePath))
                {
                    var logFiles = System.IO.Directory.GetFiles(logFilePath, "FlinkDotnet.log.*");
                    foreach (var file in logFiles)
                    {
                        var fileInfo = new System.IO.FileInfo(file);
                        if (fileInfo.LastWriteTimeUtc < System.DateTime.UtcNow.AddDays(-1))
                        {
                            System.IO.File.Delete(file);
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors
            }
            
            return new LoggerConfiguration()
                .WriteTo.File(
                    path: logFile,
                    rollingInterval: RollingInterval.Infinite,
                    rollOnFileSizeLimit: false,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 100_000_000,
                    shared: true)
                .WriteTo.Console()
                .MinimumLevel.Debug()
                .CreateLogger();
        }
        
        private int _bufferTimeoutMillis = 100;
        private bool _operatorChainingEnabled = true;
        private long _checkpointInterval = -1;
        private bool _adaptiveSchedulerEnabled = false;
        private bool _reactiveModeEnabled = false;
        private string? _savepointPath;
        private JobDefinition? _activeJob;
        private OperationCapture? _operationCapture;

        /// <summary>
        /// Creates a new StreamExecutionEnvironment.
        /// </summary>
        /// <param name="configuration">Optional configuration</param>
        /// <param name="logger">Optional logger</param>
        protected StreamExecutionEnvironment(Configuration? configuration = null, ILogger? logger = null)
        {
            _executionConfig = new ExecutionConfig(configuration ?? new Configuration());
            _logger = logger;
            _serilogLogger.Information("[StreamExecutionEnvironment] Created new environment instance");
        }

        internal void SetActiveJob(JobDefinition job)
        {
            _activeJob = job;
        }

        /// <summary>
        /// Gets the config object.
        /// </summary>
        /// <returns>The ExecutionConfig object</returns>
        public ExecutionConfig GetConfig()
        {
            return _executionConfig;
        }

        /// <summary>
        /// Creates a Kafka string source compatible with Apache Flink via the IR Runner.
        /// Supports both expression-based Map/Filter/SinkToKafka AND native DataStream API with IMapFunction.
        /// </summary>
        public DataStream<string> FromKafka(string topic, string? bootstrapServers = null, string? groupId = null, string startingOffsets = "latest")
        {
            _serilogLogger.Information("[FromKafka] Called with topic={Topic}, bootstrapServers={BootstrapServers}, groupId={GroupId}, startingOffsets={StartingOffsets}",
                topic, bootstrapServers, groupId, startingOffsets);
            
            if (string.IsNullOrWhiteSpace(bootstrapServers))
            {
                _serilogLogger.Error("[FromKafka] Bootstrap servers is null or whitespace!");
                throw new ArgumentException(
                    "Kafka bootstrap servers must be provided via bootstrapServers parameter.",
                    nameof(bootstrapServers));
            }
            
            // Initialize operation capture for native API usage
            _operationCapture = new OperationCapture();
            _serilogLogger.Debug("[FromKafka] Calling OperationCapture.CaptureKafkaSource with bootstrapServers={BootstrapServers}", bootstrapServers);
            _operationCapture.CaptureKafkaSource(topic, bootstrapServers, groupId ?? "default-group", startingOffsets, null);
            
            _serilogLogger.Debug("[FromKafka] Creating JobDefinition with bootstrapServers={BootstrapServers}", bootstrapServers);
            var jd = new JobDefinition
            {
                Source = new KafkaSourceDefinition
                {
                    Topic = topic,
                    BootstrapServers = bootstrapServers,
                    GroupId = groupId,
                    StartingOffsets = startingOffsets
                },
                Metadata = new JobMetadata
                {
                    JobId = Guid.NewGuid().ToString("n"),
                    Parallelism = _executionConfig.Parallelism > 0 ? _executionConfig.Parallelism : null,
                    CreatedAt = DateTime.UtcNow,
                    Version = "1.0"
                }
            };
            _serilogLogger.Information("[FromKafka] JobDefinition created with Source.BootstrapServers={BootstrapServers}", (jd.Source as KafkaSourceDefinition)?.BootstrapServers);
            SetActiveJob(jd);
            
            var dataStream = new DataStream<string>(jd, this);
            
            // Attach operation capture to enable native API (Map with IMapFunction)
            dataStream.AttachOperationCapture(_operationCapture);
            
            _serilogLogger.Information("[FromKafka] Returning DataStream with bootstrap servers={BootstrapServers}", bootstrapServers);
            return dataStream;
        }

        /// <summary>
        /// Creates a Kafka source with custom deserialization function.
        /// This enables reading custom objects (like InputMessage) from Kafka.
        /// Corresponds to Baeldung tutorial sections 7-11.
        /// </summary>
        /// <typeparam name="T">The type of elements to deserialize</typeparam>
        /// <param name="topic">Kafka topic name</param>
        /// <param name="bootstrapServers">Kafka bootstrap servers</param>
        /// <param name="groupId">Consumer group ID</param>
        /// <param name="deserializer">Deserialization function (byte[] -> T or string -> T)</param>
        /// <param name="startingOffsets">Starting offset strategy (earliest/latest)</param>
        /// <returns>DataStream of deserialized elements</returns>
        public DataStream<T> AddKafkaSource<T>(
            string topic,
            string bootstrapServers,
            string groupId,
            System.Func<string, T> deserializer,
            string startingOffsets = "earliest")
        {
            // Initialize operation capture for native API usage
            _operationCapture = new OperationCapture();
            _operationCapture.CaptureKafkaSource(topic, bootstrapServers, groupId, startingOffsets, deserializer);

            // Create a source function that uses the deserializer
            var sourceFunction = new KafkaSourceFunction<T>(topic, bootstrapServers, groupId, deserializer, startingOffsets);
            var dataStream = new DataStream<T>(sourceFunction, this, $"Kafka Source ({topic})");
            
            // Attach operation capture to the stream
            dataStream.AttachOperationCapture(_operationCapture);
            
            return dataStream;
        }

        /// <summary>
        /// Sets the parallelism for operations executed through this environment.
        /// Setting a parallelism of x here will cause all operators to run with x parallel instances.
        /// </summary>
        /// <param name="parallelism">The parallelism</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment SetParallelism(int parallelism)
        {
            _executionConfig.SetParallelism(parallelism);
            return this;
        }

        /// <summary>
        /// Sets the maximum degree of parallelism defined for the program.
        /// The upper limit (inclusive) is 32768.
        /// </summary>
        /// <param name="maxParallelism">Maximum degree of parallelism</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment SetMaxParallelism(int maxParallelism)
        {
            if (maxParallelism <= 0 || maxParallelism > 32768)
                throw new ArgumentException("Max parallelism must be between 1 and 32768");
            
            _executionConfig.SetMaxParallelism(maxParallelism);
            return this;
        }

        /// <summary>
        /// Gets the parallelism with which operations are executed by default.
        /// </summary>
        /// <returns>The parallelism used by operations</returns>
        public int GetParallelism()
        {
            return _executionConfig.Parallelism;
        }

        /// <summary>
        /// Gets the maximum degree of parallelism defined for the program.
        /// </summary>
        /// <returns>Maximum degree of parallelism</returns>
        public int GetMaxParallelism()
        {
            return _executionConfig.MaxParallelism;
        }

        /// <summary>
        /// Sets the maximum time frequency (milliseconds) for the flushing of the output buffers.
        /// </summary>
        /// <param name="timeoutMillis">The maximum time between two output flushes</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment SetBufferTimeout(int timeoutMillis)
        {
            _bufferTimeoutMillis = timeoutMillis;
            return this;
        }

        /// <summary>
        /// Gets the maximum time frequency (milliseconds) for the flushing of the output buffers.
        /// </summary>
        /// <returns>The timeout of the buffer</returns>
        public int GetBufferTimeout()
        {
            return _bufferTimeoutMillis;
        }

        /// <summary>
        /// Disables operator chaining for streaming operators.
        /// </summary>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment DisableOperatorChaining()
        {
            _operatorChainingEnabled = false;
            return this;
        }

        /// <summary>
        /// Returns whether operator chaining is enabled.
        /// </summary>
        /// <returns>True if chaining is enabled, false otherwise</returns>
        public bool IsChainingEnabled()
        {
            return _operatorChainingEnabled;
        }

        /// <summary>
        /// Enables checkpointing for the streaming job.
        /// </summary>
        /// <param name="interval">Time interval between state checkpoints in milliseconds</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment EnableCheckpointing(long interval)
        {
            _checkpointInterval = interval;
            return this;
        }

        /// <summary>
        /// Returns the checkpointing interval or -1 if checkpointing is disabled.
        /// </summary>
        /// <returns>The checkpointing interval or -1</returns>
        public long GetCheckpointInterval()
        {
            return _checkpointInterval;
        }

        /// <summary>
        /// Enables the Adaptive Scheduler for dynamic resource management.
        /// The Adaptive Scheduler automatically adjusts parallelism based on workload and available resources.
        /// This is a key feature of Apache Flink 2.1.0 for intelligent scaling.
        /// </summary>
        /// <param name="enabled">True to enable adaptive scheduler</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment EnableAdaptiveScheduler(bool enabled = true)
        {
            _adaptiveSchedulerEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Returns whether the Adaptive Scheduler is enabled.
        /// </summary>
        /// <returns>True if adaptive scheduler is enabled</returns>
        public bool IsAdaptiveSchedulerEnabled()
        {
            return _adaptiveSchedulerEnabled;
        }

        /// <summary>
        /// Enables Reactive Mode for automatic adaptation to available cluster resources.
        /// In Reactive Mode, Flink automatically adapts the parallelism to the available resources.
        /// This is a Apache Flink 2.1.0 feature for elastic scaling.
        /// </summary>
        /// <param name="enabled">True to enable reactive mode</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment EnableReactiveMode(bool enabled = true)
        {
            _reactiveModeEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Returns whether Reactive Mode is enabled.
        /// </summary>
        /// <returns>True if reactive mode is enabled</returns>
        public bool IsReactiveModeEnabled()
        {
            return _reactiveModeEnabled;
        }

        /// <summary>
        /// Sets the path to a savepoint to restore the job from.
        /// This enables savepoint-based scaling workflows in Apache Flink 2.1.0.
        /// </summary>
        /// <param name="savepointPath">Path to the savepoint</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment FromSavepoint(string savepointPath)
        {
            _savepointPath = savepointPath;
            return this;
        }

        /// <summary>
        /// Gets the savepoint path if configured.
        /// </summary>
        /// <returns>The savepoint path or null if not set</returns>
        public string? GetSavepointPath()
        {
            return _savepointPath;
        }

        /// <summary>
        /// Creates a data stream from the given collection.
        /// Note that this operation will result in a non-parallel data stream source.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="collection">The collection of elements to create the data stream from</param>
        /// <returns>The data stream representing the given collection</returns>
        public DataStream<T> FromCollection<T>(IEnumerable<T> collection)
        {
            return new DataStream<T>(collection, this);
        }

        /// <summary>
        /// Adds a data source to the streaming topology.
        /// </summary>
        /// <typeparam name="T">The type of elements produced by the source</typeparam>
        /// <param name="sourceFunction">The user defined source function</param>
        /// <param name="sourceName">Name of the data source</param>
        /// <returns>The data stream constructed</returns>
        public DataStream<T> AddSource<T>(ISourceFunction<T> sourceFunction, string sourceName = "Custom Source")
        {
            return new DataStream<T>(sourceFunction, this, sourceName);
        }

        /// <summary>
        /// Creates an execution environment that represents the context in which the program is executed.
        /// If the program is invoked standalone, this method returns a local execution environment.
        /// </summary>
        /// <param name="configuration">The configuration to instantiate the environment with</param>
        /// <returns>The execution environment of the context in which the program is executed</returns>
        public static StreamExecutionEnvironment GetExecutionEnvironment(Configuration? configuration = null)
        {
            return new StreamExecutionEnvironment(configuration);
        }

        public async Task<IJobClient> ExecuteAsync(string? jobName = null, CancellationToken cancellationToken = default)
        {
            var name = jobName ?? _activeJob?.Metadata?.JobName ?? "Flink Streaming Job";
            _logger?.LogInformation("Starting execution of job: {JobName}", name);
            _serilogLogger.Information("[ExecuteAsync] Starting execution of job: {JobName}", name);

            JobDefinition jobToSubmit;

            // Check if we have captured operations from native API usage
            if (_operationCapture != null && _operationCapture.HasOperations())
            {
                // Translate captured operations to JobDefinition
                var jobId = System.Guid.NewGuid().ToString();
                _serilogLogger.Information("[ExecuteAsync] Translating native DataStream API operations to JobDefinition with jobId={JobId}", jobId);
                jobToSubmit = _operationCapture.ToJobDefinition(jobId, name);
                _serilogLogger.Information("[ExecuteAsync] After translation: Source.BootstrapServers={BootstrapServers}", (jobToSubmit.Source as KafkaSourceDefinition)?.BootstrapServers);
                _logger?.LogInformation("Translated native DataStream API operations to JobDefinition");
            }
            else if (_activeJob != null)
            {
                // Use existing JobDefinition (IR-backed stream)
                _serilogLogger.Information("[ExecuteAsync] Using existing JobDefinition with Source.BootstrapServers={BootstrapServers}", (_activeJob.Source as KafkaSourceDefinition)?.BootstrapServers);
                jobToSubmit = _activeJob;
                jobToSubmit.Metadata.JobName = name;
            }
            else
            {
                _serilogLogger.Error("[ExecuteAsync] No Flink-compatible job defined!");
                throw new InvalidOperationException("No Flink-compatible job is defined. Use AddKafkaSource(...) or FromKafka(...) before Execute().");
            }

            _serilogLogger.Information("[ExecuteAsync] About to submit job to gateway with Source.BootstrapServers={BootstrapServers}", (jobToSubmit.Source as KafkaSourceDefinition)?.BootstrapServers);
            var gateway = new FlinkJobGatewayService();
            
            JobSubmissionResult submit;
            try
            {
                submit = await gateway.SubmitJobAsync(jobToSubmit, cancellationToken).ConfigureAwait(false);
                _serilogLogger.Information("[ExecuteAsync] Job submission completed: Success={Success}, FlinkJobId={FlinkJobId}, Error={Error}",
                    submit.Success, submit.FlinkJobId, submit.ErrorMessage);
            }
            catch (Exception ex)
            {
                _serilogLogger.Error(ex, "[ExecuteAsync] Exception during job submission to gateway for job {JobId}", jobToSubmit.Metadata.JobId);
                _logger?.LogError(ex, "Failed to submit job {JobId} to gateway", jobToSubmit.Metadata.JobId);
                throw new InvalidOperationException($"Failed to submit job {jobToSubmit.Metadata.JobId} to Flink Job Gateway", ex);
            }

            if (!submit.Success)
            {
                _serilogLogger.Error("[ExecuteAsync] Job submission failed: {ErrorMessage}", submit.ErrorMessage);
                throw new InvalidOperationException($"Job submission failed: {submit.ErrorMessage}");
            }

            // Create and return JobClient for lifecycle management
            var jobClient = new JobClient(name)
            {
                JobId = submit.FlinkJobId ?? jobToSubmit.Metadata.JobId
            };

            _serilogLogger.Information("[ExecuteAsync] Returning JobClient with JobId={JobId}", jobClient.JobId);
            return jobClient;
        }

        public Task<JobClient> ExecuteAsyncJob(string jobName = "Flink Streaming Job")
        {
            if (_activeJob == null)
                throw new InvalidOperationException("No Flink-compatible job is defined. Use FromKafka(...), Map(string), Filter(string)/Where(string), and SinkToKafka(...) before ExecuteAsyncJob().");
            _activeJob.Metadata.JobName = jobName;
            return Task.FromResult(new JobClient(jobName));
        }

        /// <summary>
        /// Configures this StreamExecutionEnvironment via the given Configuration.
        /// </summary>
        /// <param name="configuration">A configuration to read the values from</param>
        /// <returns>This object</returns>
        public StreamExecutionEnvironment Configure(Configuration configuration)
        {
            _executionConfig.GetConfiguration().AddAll(configuration);
            return this;
        }
    }

    public class JobExecutionResult
    {
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public bool Success { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Interface for managing Flink job lifecycle.
    /// Follows Apache Flink's JobClient pattern for job submission and management.
    /// </summary>
    public interface IJobClient
    {
        /// <summary>
        /// Gets the Flink job ID.
        /// </summary>
        /// <returns>The job ID as a string</returns>
        string GetJobId();

        /// <summary>
        /// Cancels the Flink job.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the cancellation operation</returns>
        Task CancelAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets the job execution result with status and metrics.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Job execution result</returns>
        Task<JobExecutionResult> GetJobExecutionResultAsync(CancellationToken cancellationToken = default);
    }

    public class JobClient : IJobClient
    {
        private readonly FlinkJobGatewayService _gateway = new();
        private readonly HttpClient _flinkHttp;
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; }

        public JobClient(string jobName)
        {
            JobName = jobName;
            var host = Environment.GetEnvironmentVariable("FLINK_CLUSTER_HOST") ?? "flink-jobmanager";
            var port = int.Parse(Environment.GetEnvironmentVariable("FLINK_CLUSTER_PORT") ?? "8081");
            _flinkHttp = new HttpClient { BaseAddress = new Uri($"http://{host}:{port}"), Timeout = TimeSpan.FromMinutes(5) };
        }

        /// <summary>
        /// Gets the Flink job ID.
        /// Implementation of IJobClient.GetJobId().
        /// </summary>
        public string GetJobId() => JobId;

        /// <summary>
        /// Cancels the Flink job using the Flink REST API.
        /// Implementation of IJobClient.CancelAsync().
        /// </summary>
        public async Task CancelAsync(CancellationToken cancellationToken = default)
        {
            var success = await _gateway.CancelJobAsync(JobId, cancellationToken);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to cancel job {JobId}");
            }
        }

        /// <summary>
        /// Gets the job execution result with current status and metrics.
        /// Implementation of IJobClient.GetJobExecutionResultAsync().
        /// </summary>
        public async Task<JobExecutionResult> GetJobExecutionResultAsync(CancellationToken cancellationToken = default)
        {
            var status = await GetJobStatusAsync(cancellationToken);
            return new JobExecutionResult
            {
                JobId = JobId,
                JobName = JobName,
                Success = status.State == "FINISHED",
                StartTime = status.StartTime,
                EndTime = status.EndTime ?? DateTime.UtcNow,
                Error = status.Error
            };
        }

        public async Task<SavepointResult> TriggerSavepointAsync(string? savepointPath = null, CancellationToken cancellationToken = default)
        {
            var payload = new { targetDirectory = savepointPath, cancelJob = false };
            var resp = await _flinkHttp.PostAsync($"/v1/jobs/{JobId}/savepoints",
                new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            var ok = resp.IsSuccessStatusCode;
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string triggerId = string.Empty;
            try { using var doc = System.Text.Json.JsonDocument.Parse(text); triggerId = doc.RootElement.TryGetProperty("request-id", out var rid) ? rid.GetString() ?? string.Empty : string.Empty; } 
            catch 
            { 
                // JSON parsing may fail if response is not valid JSON - use empty triggerId
            }
            return new SavepointResult { SavepointPath = null!, Success = ok, TriggerId = triggerId, Error = ok ? null : text };
        }

        public async Task<SavepointResult> CancelWithSavepointAsync(string? savepointPath = null, CancellationToken cancellationToken = default)
        {
            var payload = new { targetDirectory = savepointPath, cancelJob = true };
            var resp = await _flinkHttp.PostAsync($"/v1/jobs/{JobId}/savepoints",
                new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            var ok = resp.IsSuccessStatusCode;
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            string triggerId = string.Empty;
            try { using var doc = System.Text.Json.JsonDocument.Parse(text); triggerId = doc.RootElement.TryGetProperty("request-id", out var rid) ? rid.GetString() ?? string.Empty : string.Empty; } 
            catch 
            { 
                // JSON parsing may fail if response is not valid JSON - use empty triggerId
            }
            return new SavepointResult { SavepointPath = null!, Success = ok, TriggerId = triggerId, Error = ok ? null : text };
        }

        public async Task<JobStatus> GetJobStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = await _gateway.GetJobStatusAsync(JobId, cancellationToken).ConfigureAwait(false);
            return new JobStatus
            {
                JobId = JobId,
                JobName = JobName,
                State = status.State ?? "UNKNOWN",
                Parallelism = status.Metrics?.Parallelism ?? 0,
                MaxParallelism = 0,
                StartTime = status.StartTime ?? DateTime.MinValue,
                EndTime = status.EndTime,
                Error = status.ErrorMessage
            };
        }

        public async Task<StopWithSavepointResult> StopWithSavepointAsync(string? savepointPath = null, bool drain = true, CancellationToken cancellationToken = default)
        {
            var payload = new { targetDirectory = savepointPath, drain = drain };
            var resp = await _flinkHttp.PostAsync($"/v1/jobs/{JobId}/stop",
                new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json"), cancellationToken).ConfigureAwait(false);
            var ok = resp.IsSuccessStatusCode;
            var text = await resp.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            return new StopWithSavepointResult { SavepointPath = savepointPath ?? string.Empty, Success = ok, TriggerId = string.Empty, Drained = drain, Error = ok ? null : text };
        }
    }

    /// <summary>
    /// Interface for source functions that generate data streams.
    /// </summary>
    /// <typeparam name="T">The type of elements produced by this source</typeparam>
    public interface ISourceFunction<out T>
    {
        /// <summary>
        /// Starts the source function.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Enumerable of elements</returns>
        IAsyncEnumerable<T> RunAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Result of a savepoint operation.
    /// </summary>
    public class SavepointResult
    {
        public string SavepointPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string TriggerId { get; set; } = string.Empty;
        public string? Error { get; set; }
    }

    /// <summary>
    /// Result of stopping a job with a savepoint.
    /// </summary>
    public class StopWithSavepointResult
    {
        public string SavepointPath { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string TriggerId { get; set; } = string.Empty;
        public bool Drained { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// Status information for a Flink job.
    /// </summary>
    public class JobStatus
    {
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public int Parallelism { get; set; }
        public int MaxParallelism { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public string? Error { get; set; }
    }
}
