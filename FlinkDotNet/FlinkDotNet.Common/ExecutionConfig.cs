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