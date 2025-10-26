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

namespace FlinkDotNet.Common
{
    /// <summary>
    /// Lightweight configuration object which stores key/value pairs.
    /// This corresponds to pyflink.common.Configuration in Python Flink.
    /// </summary>
    public class FlinkConfiguration
    {
        private readonly Dictionary<string, object> _configuration = [];

        /// <summary>
        /// Creates an empty configuration.
        /// </summary>
        public FlinkConfiguration()
        {
        }

        /// <summary>
        /// Creates a configuration with the given key-value pairs.
        /// </summary>
        /// <param name="configuration">Initial configuration values</param>
        public FlinkConfiguration(IDictionary<string, object> configuration)
        {
            foreach (KeyValuePair<string, object> kvp in configuration)
            {
                this._configuration[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// Sets a string value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        /// <returns>This FlinkConfiguration instance for method chaining</returns>
        public FlinkConfiguration SetString(string key, string value)
        {
            this._configuration[key] = value;
            return this;
        }

        /// <summary>
        /// Sets an integer value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        /// <returns>This FlinkConfiguration instance for method chaining</returns>
        public FlinkConfiguration SetInteger(string key, int value)
        {
            this._configuration[key] = value;
            return this;
        }

        /// <summary>
        /// Sets a boolean value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        /// <returns>This FlinkConfiguration instance for method chaining</returns>
        public FlinkConfiguration SetBoolean(string key, bool value)
        {
            this._configuration[key] = value;
            return this;
        }

        /// <summary>
        /// Sets a long value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="value">The value to set</param>
        /// <returns>This FlinkConfiguration instance for method chaining</returns>
        public FlinkConfiguration SetLong(string key, long value)
        {
            this._configuration[key] = value;
            return this;
        }

        /// <summary>
        /// Gets a string value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        public string GetString(string key, string? defaultValue = null)
        {
            return this._configuration.TryGetValue(key, out var value)
                ? value?.ToString() ?? defaultValue ?? string.Empty
                : defaultValue ?? string.Empty;
        }

        /// <summary>
        /// Gets an integer value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        public int GetInteger(string key, int defaultValue = 0)
        {
            if (this._configuration.TryGetValue(key, out var value))
            {
                if (value is int intValue)
                {
                    return intValue;
                }

                if (int.TryParse(value.ToString(), out var parsedValue))
                {
                    return parsedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a boolean value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        public bool GetBoolean(string key, bool defaultValue = false)
        {
            if (this._configuration.TryGetValue(key, out var value))
            {
                if (value is bool boolValue)
                {
                    return boolValue;
                }

                if (bool.TryParse(value.ToString(), out var parsedValue))
                {
                    return parsedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Gets a long value for the given key.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">The default value if key is not found</param>
        /// <returns>The configuration value</returns>
        public long GetLong(string key, long defaultValue = 0L)
        {
            if (this._configuration.TryGetValue(key, out var value))
            {
                if (value is long longValue)
                {
                    return longValue;
                }

                if (long.TryParse(value.ToString(), out var parsedValue))
                {
                    return parsedValue;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// Checks if a key exists in the configuration.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <returns>True if the key exists</returns>
        public bool ContainsKey(string key) => this._configuration.ContainsKey(key);

        /// <summary>
        /// Removes a key from the configuration.
        /// </summary>
        /// <param name="key">The configuration key to remove</param>
        /// <returns>True if the key was removed</returns>
        public bool RemoveKey(string key) => this._configuration.Remove(key);

        /// <summary>
        /// Gets all configuration keys.
        /// </summary>
        /// <returns>Collection of all configuration keys</returns>
        public IEnumerable<string> GetKeys() => this._configuration.Keys;

        /// <summary>
        /// Creates a copy of this configuration.
        /// </summary>
        /// <returns>A new FlinkConfiguration instance with the same values</returns>
        public FlinkConfiguration Clone() => new(this._configuration);

        /// <summary>
        /// Adds all key-value pairs from another configuration.
        /// </summary>
        /// <param name="other">The configuration to merge from</param>
        /// <returns>This Configuration instance for method chaining</returns>
        public FlinkConfiguration AddAll(FlinkConfiguration other)
        {
            foreach (KeyValuePair<string, object> kvp in other._configuration)
            {
                this._configuration[kvp.Key] = kvp.Value;
            }
            return this;
        }

        /// <summary>
        /// Gets all configuration as a dictionary.
        /// </summary>
        /// <returns>Dictionary containing all configuration values</returns>
        public IDictionary<string, object> ToMap() => new Dictionary<string, object>(this._configuration);

        /// <summary>
        /// Parses a list value from a string (comma-separated).
        /// </summary>
        /// <param name="value">The string value to parse</param>
        /// <returns>List of string values</returns>
        public static IList<string> ParseListValue(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }
}
