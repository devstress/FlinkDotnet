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

namespace FlinkDotNet.DataStream.Checkpoint
{
    /// <summary>
    /// Configuration for checkpointing behavior.
    /// This corresponds to org.apache.flink.streaming.api.environment.CheckpointConfig in Apache Flink.
    /// <para>
    /// Checkpoint configuration controls:
    /// - Where checkpoints are stored (checkpoint storage)
    /// - Checkpoint timeouts and failure tolerance
    /// - Concurrent checkpoint limits
    /// - Minimum pause between checkpoints
    /// </para>
    /// </summary>
    public class CheckpointConfig
    {
        private ICheckpointStorage? _checkpointStorage;
        private string? _checkpointStoragePath;
        /// <summary>
        /// Checkpoint timeout in milliseconds. Default: 10 minutes (600000 ms).
        /// </summary>
        private long _checkpointTimeout = 600000;
        private long _minPauseBetweenCheckpoints;
        private int _maxConcurrentCheckpoints = 1;
        private int _tolerableCheckpointFailureNumber = int.MaxValue;
        private bool _externalizedCheckpointsEnabled;
        private ExternalizedCheckpointCleanup _externalizedCheckpointCleanup = ExternalizedCheckpointCleanup.DELETE_ON_CANCELLATION;

        /// <summary>
        /// Creates a new CheckpointConfig with default settings.
        /// </summary>
        public CheckpointConfig()
        {
        }

        /// <summary>
        /// Sets the checkpoint storage to a file system path.
        /// This is a convenience method that creates a FileSystemCheckpointStorage internally.
        /// </summary>
        /// <param name="path">The checkpoint storage path (local, HDFS, S3, etc.)</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetCheckpointStorage(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new System.ArgumentException("Checkpoint storage path cannot be null or empty", nameof(path));
            }
            this._checkpointStoragePath = path;
            this._checkpointStorage = new FileSystemCheckpointStorage(path);
            return this;
        }

        /// <summary>
        /// Sets the checkpoint storage implementation.
        /// </summary>
        /// <param name="storage">The checkpoint storage implementation</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetCheckpointStorage(ICheckpointStorage storage)
        {
            this._checkpointStorage = storage ?? throw new System.ArgumentNullException(nameof(storage));
            this._checkpointStoragePath = storage.GetCheckpointPath();
            return this;
        }

        /// <summary>
        /// Gets the configured checkpoint storage implementation.
        /// </summary>
        /// <returns>The checkpoint storage, or null if not configured</returns>
        public ICheckpointStorage? GetCheckpointStorage() => this._checkpointStorage;

        /// <summary>
        /// Gets the checkpoint storage path.
        /// </summary>
        /// <returns>The storage path, or null if not configured</returns>
        public string? GetCheckpointStoragePath() => this._checkpointStoragePath;

        /// <summary>
        /// Sets the maximum time that a checkpoint may take before being aborted.
        /// </summary>
        /// <param name="timeoutMs">The checkpoint timeout in milliseconds</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetCheckpointTimeout(long timeoutMs)
        {
            if (timeoutMs <= 0)
            {
                throw new System.ArgumentException("Checkpoint timeout must be positive", nameof(timeoutMs));
            }
            this._checkpointTimeout = timeoutMs;
            return this;
        }

        /// <summary>
        /// Gets the checkpoint timeout in milliseconds.
        /// </summary>
        /// <returns>The checkpoint timeout</returns>
        public long GetCheckpointTimeout() => this._checkpointTimeout;

        /// <summary>
        /// Sets the minimal pause between consecutive checkpoint attempts.
        /// This defines how soon the checkpoint coordinator may trigger another checkpoint
        /// after the last checkpoint has completed.
        /// </summary>
        /// <param name="pauseMs">The minimum pause in milliseconds</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetMinPauseBetweenCheckpoints(long pauseMs)
        {
            if (pauseMs < 0)
            {
                throw new System.ArgumentException("Minimum pause must be non-negative", nameof(pauseMs));
            }
            this._minPauseBetweenCheckpoints = pauseMs;
            return this;
        }

        /// <summary>
        /// Gets the minimum pause between checkpoints in milliseconds.
        /// </summary>
        /// <returns>The minimum pause</returns>
        public long GetMinPauseBetweenCheckpoints() => this._minPauseBetweenCheckpoints;

        /// <summary>
        /// Sets the maximum number of concurrent checkpoint attempts that may be in progress at the same time.
        /// For most setups, one concurrent checkpoint is sufficient and preferred for consistency.
        /// </summary>
        /// <param name="maxConcurrent">The maximum number of concurrent checkpoints</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetMaxConcurrentCheckpoints(int maxConcurrent)
        {
            if (maxConcurrent < 1)
            {
                throw new System.ArgumentException("Max concurrent checkpoints must be at least 1", nameof(maxConcurrent));
            }
            this._maxConcurrentCheckpoints = maxConcurrent;
            return this;
        }

        /// <summary>
        /// Gets the maximum number of concurrent checkpoints.
        /// </summary>
        /// <returns>The maximum concurrent checkpoints</returns>
        public int GetMaxConcurrentCheckpoints() => this._maxConcurrentCheckpoints;

        /// <summary>
        /// Sets the tolerable checkpoint failure number.
        /// If this value is exceeded, the job fails.
        /// </summary>
        /// <param name="tolerableFailures">The number of tolerable checkpoint failures</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig SetTolerableCheckpointFailureNumber(int tolerableFailures)
        {
            if (tolerableFailures < 0)
            {
                throw new System.ArgumentException("Tolerable failures must be non-negative", nameof(tolerableFailures));
            }
            this._tolerableCheckpointFailureNumber = tolerableFailures;
            return this;
        }

        /// <summary>
        /// Gets the tolerable checkpoint failure number.
        /// </summary>
        /// <returns>The tolerable failure count</returns>
        public int GetTolerableCheckpointFailureNumber() => this._tolerableCheckpointFailureNumber;

        /// <summary>
        /// Enables externalized checkpoints, which persist checkpoints after job termination.
        /// Externalized checkpoints can be used to recover from job failures or for manual savepoints.
        /// </summary>
        /// <param name="cleanup">The cleanup behavior for externalized checkpoints</param>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig EnableExternalizedCheckpoints(ExternalizedCheckpointCleanup cleanup)
        {
            this._externalizedCheckpointsEnabled = true;
            this._externalizedCheckpointCleanup = cleanup;
            return this;
        }

        /// <summary>
        /// Disables externalized checkpoints.
        /// </summary>
        /// <returns>This CheckpointConfig instance for method chaining</returns>
        public CheckpointConfig DisableExternalizedCheckpoints()
        {
            this._externalizedCheckpointsEnabled = false;
            return this;
        }

        /// <summary>
        /// Gets whether externalized checkpoints are enabled.
        /// </summary>
        /// <returns>True if externalized checkpoints are enabled</returns>
        public bool IsExternalizedCheckpointsEnabled() => this._externalizedCheckpointsEnabled;

        /// <summary>
        /// Gets the externalized checkpoint cleanup behavior.
        /// </summary>
        /// <returns>The cleanup behavior</returns>
        public ExternalizedCheckpointCleanup GetExternalizedCheckpointCleanup() => this._externalizedCheckpointCleanup;
    }

    /// <summary>
    /// Cleanup behavior for externalized checkpoints.
    /// Corresponds to org.apache.flink.streaming.api.environment.CheckpointConfig.ExternalizedCheckpointCleanup.
    /// </summary>
    public enum ExternalizedCheckpointCleanup
    {
        /// <summary>
        /// Delete externalized checkpoints when the job is cancelled.
        /// The checkpoints will be kept when the job fails.
        /// </summary>
        DELETE_ON_CANCELLATION,

        /// <summary>
        /// Retain externalized checkpoints when the job is cancelled or fails.
        /// The checkpoints need to be deleted manually.
        /// </summary>
        RETAIN_ON_CANCELLATION
    }
}
