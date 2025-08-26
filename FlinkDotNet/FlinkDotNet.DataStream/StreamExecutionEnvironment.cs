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
using FlinkDotNet.Common;
using Microsoft.Extensions.Logging;

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
        private int _bufferTimeoutMillis = 100;
        private bool _operatorChainingEnabled = true;
        private long _checkpointInterval = -1;
        private bool _adaptiveSchedulerEnabled = false;
        private bool _reactiveModeEnabled = false;
        private string? _savepointPath;

        /// <summary>
        /// Creates a new StreamExecutionEnvironment.
        /// </summary>
        /// <param name="configuration">Optional configuration</param>
        /// <param name="logger">Optional logger</param>
        protected StreamExecutionEnvironment(Configuration? configuration = null, ILogger? logger = null)
        {
            _executionConfig = new ExecutionConfig(configuration ?? new Configuration());
            _logger = logger;
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

        /// <summary>
        /// Triggers the program execution. The environment will execute all parts of the program
        /// that have resulted in a "sink" operation.
        /// </summary>
        /// <param name="jobName">Desired name of the job</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The result of the job execution</returns>
        public async Task<JobExecutionResult> ExecuteAsync(string? jobName = null, CancellationToken cancellationToken = default)
        {
            _logger?.LogInformation("Starting execution of job: {JobName}", jobName ?? "Flink Streaming Job");

            // This would integrate with the job gateway and Flink cluster
            // For now, we'll simulate execution
            await Task.Delay(100, cancellationToken);

            return new JobExecutionResult
            {
                JobId = Guid.NewGuid().ToString(),
                JobName = jobName ?? "Flink Streaming Job",
                Success = true,
                StartTime = DateTime.UtcNow,
                EndTime = DateTime.UtcNow
            };
        }

        /// <summary>
        /// Triggers the program asynchronously.
        /// </summary>
        /// <param name="jobName">Desired name of the job</param>
        /// <returns>A JobClient that can be used to communicate with the submitted job</returns>
        public Task<JobClient> ExecuteAsyncJob(string jobName = "Flink Streaming Job")
        {
            _logger?.LogInformation("Starting async execution of job: {JobName}", jobName);
            
            var jobClient = new JobClient
            {
                JobId = Guid.NewGuid().ToString(),
                JobName = jobName
            };

            return Task.FromResult(jobClient);
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

    /// <summary>
    /// Represents the result of a job execution.
    /// </summary>
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
    /// A client that can be used to communicate with a submitted job.
    /// Enhanced for Apache Flink 2.1.0 dynamic scaling capabilities.
    /// </summary>
    public class JobClient
    {
        public string JobId { get; set; } = string.Empty;
        public string JobName { get; set; } = string.Empty;

        /// <summary>
        /// Triggers a savepoint for the job.
        /// This is essential for savepoint-based scaling in Apache Flink 2.1.0.
        /// </summary>
        /// <param name="savepointPath">Optional path where the savepoint should be stored</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The savepoint result containing the savepoint path</returns>
        public async Task<SavepointResult> TriggerSavepointAsync(string? savepointPath = null, CancellationToken cancellationToken = default)
        {
            // This would trigger a savepoint via the Flink JobManager REST API
            await Task.Delay(100, cancellationToken);
            
            return new SavepointResult
            {
                SavepointPath = savepointPath ?? $"/savepoints/{JobId}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Success = true,
                TriggerId = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Cancels the job with a savepoint.
        /// This allows for graceful job termination while preserving state for later scaling.
        /// </summary>
        /// <param name="savepointPath">Optional path where the savepoint should be stored</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The savepoint result</returns>
        public async Task<SavepointResult> CancelWithSavepointAsync(string? savepointPath = null, CancellationToken cancellationToken = default)
        {
            // This would cancel the job with savepoint via the Flink JobManager REST API
            await Task.Delay(150, cancellationToken); // Different delay to distinguish from TriggerSavepointAsync
            
            return new SavepointResult
            {
                SavepointPath = savepointPath ?? $"/savepoints/{JobId}/cancel/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Success = true,
                TriggerId = Guid.NewGuid().ToString()
            };
        }

        /// <summary>
        /// Gets the current job status including parallelism information.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The job status</returns>
        public async Task<JobStatus> GetJobStatusAsync(CancellationToken cancellationToken = default)
        {
            // This would query the job status via the Flink JobManager REST API
            await Task.Delay(50, cancellationToken);
            
            return new JobStatus
            {
                JobId = JobId,
                JobName = JobName,
                State = "RUNNING",
                Parallelism = 4,
                MaxParallelism = 128,
                StartTime = DateTime.UtcNow.AddMinutes(-10)
            };
        }

        /// <summary>
        /// Stops the job gracefully by taking a savepoint and then terminating the job.
        /// This is the recommended way to stop jobs for scaling in Apache Flink 2.1.0.
        /// </summary>
        /// <param name="savepointPath">Optional path where the savepoint should be stored</param>
        /// <param name="drain">Whether to process all records before stopping</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>The stop result containing savepoint information</returns>
        public async Task<StopWithSavepointResult> StopWithSavepointAsync(string? savepointPath = null, bool drain = true, CancellationToken cancellationToken = default)
        {
            // This would stop the job with savepoint via the Flink JobManager REST API
            await Task.Delay(100, cancellationToken);
            
            return new StopWithSavepointResult
            {
                SavepointPath = savepointPath ?? $"/savepoints/{JobId}/{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}",
                Success = true,
                TriggerId = Guid.NewGuid().ToString(),
                Drained = drain
            };
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