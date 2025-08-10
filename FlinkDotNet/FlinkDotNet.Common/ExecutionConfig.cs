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

namespace FlinkDotNet.Common
{
    /// <summary>
    /// A config to define the behavior of the program execution.
    /// This corresponds to pyflink.common.ExecutionConfig in Python Flink.
    /// </summary>
    public class ExecutionConfig
    {
        private readonly Configuration _configuration;

        /// <summary>
        /// Creates a new ExecutionConfig with default settings.
        /// </summary>
        public ExecutionConfig() : this(new Configuration())
        {
        }

        /// <summary>
        /// Creates a new ExecutionConfig with the given configuration.
        /// </summary>
        /// <param name="configuration">The underlying configuration</param>
        public ExecutionConfig(Configuration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets or sets the parallelism for operations executed through this environment.
        /// </summary>
        public int Parallelism { get; set; } = -1;

        /// <summary>
        /// Gets or sets the maximum degree of parallelism defined for the program.
        /// </summary>
        public int MaxParallelism { get; set; } = -1;

        /// <summary>
        /// Gets or sets the interval in milliseconds between consecutive automatic watermark emissions.
        /// </summary>
        public long AutoWatermarkInterval { get; set; } = 200L;

        /// <summary>
        /// Gets or sets whether object reuse is enabled.
        /// When enabled, user functions that produce objects will be asked to reuse 
        /// objects rather than allocating new ones.
        /// </summary>
        public bool ObjectReuseEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets whether closure cleaner is enabled.
        /// The closure cleaner removes unneeded references to the outer class files of 
        /// anonymous functions inside of Flink programs.
        /// </summary>
        public bool ClosureCleanerEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the restart strategy for failed jobs.
        /// Supports Apache Flink 2.0 advanced restart strategies.
        /// </summary>
        public string RestartStrategy { get; set; } = "exponential-delay";

        /// <summary>
        /// Gets or sets the task slot sharing configuration.
        /// Used for fine-grained resource management in Apache Flink 2.0.
        /// </summary>
        public bool SlotSharingEnabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the adaptive scheduler configuration.
        /// The adaptive scheduler is a key Apache Flink 2.0 feature for intelligent scaling.
        /// </summary>
        public bool AdaptiveSchedulerEnabled { get; set; } = false;

        /// <summary>
        /// Gets or sets reactive mode configuration.
        /// Reactive mode automatically adapts to available cluster resources.
        /// </summary>
        public bool ReactiveModeEnabled { get; set; } = false;

        /// <summary>
        /// Sets the parallelism for operations executed through this environment.
        /// </summary>
        /// <param name="parallelism">The parallelism</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig SetParallelism(int parallelism)
        {
            Parallelism = parallelism;
            return this;
        }

        /// <summary>
        /// Sets the maximum degree of parallelism defined for the program.
        /// </summary>
        /// <param name="maxParallelism">Maximum degree of parallelism</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig SetMaxParallelism(int maxParallelism)
        {
            MaxParallelism = maxParallelism;
            return this;
        }

        /// <summary>
        /// Sets the interval in milliseconds between consecutive automatic watermark emissions.
        /// </summary>
        /// <param name="interval">The interval in milliseconds</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig SetAutoWatermarkInterval(long interval)
        {
            AutoWatermarkInterval = interval;
            return this;
        }

        /// <summary>
        /// Enables or disables object reuse.
        /// </summary>
        /// <param name="enabled">True to enable object reuse</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig EnableObjectReuse(bool enabled = true)
        {
            ObjectReuseEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Disables object reuse.
        /// </summary>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig DisableObjectReuse()
        {
            return EnableObjectReuse(false);
        }

        /// <summary>
        /// Enables or disables closure cleaner.
        /// </summary>
        /// <param name="enabled">True to enable closure cleaner</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig EnableClosureCleaner(bool enabled = true)
        {
            ClosureCleanerEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Disables closure cleaner.
        /// </summary>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig DisableClosureCleaner()
        {
            return EnableClosureCleaner(false);
        }

        /// <summary>
        /// Sets the restart strategy for failed jobs.
        /// Apache Flink 2.0 supports various restart strategies for enhanced fault tolerance.
        /// </summary>
        /// <param name="strategy">The restart strategy (e.g., "exponential-delay", "fixed-delay", "failure-rate")</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig SetRestartStrategy(string strategy)
        {
            RestartStrategy = strategy;
            return this;
        }

        /// <summary>
        /// Enables or disables slot sharing.
        /// Slot sharing allows different operators to share the same task slot.
        /// </summary>
        /// <param name="enabled">True to enable slot sharing</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig EnableSlotSharing(bool enabled = true)
        {
            SlotSharingEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables the Adaptive Scheduler for intelligent resource management.
        /// The Adaptive Scheduler is a key Apache Flink 2.0 feature that automatically
        /// adjusts parallelism based on workload characteristics and available resources.
        /// </summary>
        /// <param name="enabled">True to enable adaptive scheduler</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig EnableAdaptiveScheduler(bool enabled = true)
        {
            AdaptiveSchedulerEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Enables Reactive Mode for automatic scaling based on cluster resources.
        /// In Reactive Mode, Flink automatically adapts the parallelism to the available resources
        /// without requiring manual intervention. This is an Apache Flink 2.0 feature.
        /// </summary>
        /// <param name="enabled">True to enable reactive mode</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig EnableReactiveMode(bool enabled = true)
        {
            ReactiveModeEnabled = enabled;
            return this;
        }

        /// <summary>
        /// Gets the underlying configuration.
        /// </summary>
        /// <returns>The configuration object</returns>
        public Configuration GetConfiguration()
        {
            return _configuration;
        }

        /// <summary>
        /// Sets a configuration parameter.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The configuration value</param>
        /// <returns>This ExecutionConfig instance for method chaining</returns>
        public ExecutionConfig SetProperty(string key, object value)
        {
            _configuration.SetString(key, value.ToString()!);
            return this;
        }

        /// <summary>
        /// Gets a configuration parameter.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        public string GetProperty(string key, string? defaultValue = null)
        {
            return _configuration.GetString(key, defaultValue);
        }
    }
}