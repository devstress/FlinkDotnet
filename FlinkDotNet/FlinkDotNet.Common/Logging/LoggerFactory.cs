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
//  limitations under the License.

using System;
using System.IO.Abstractions;
using Serilog;
using Serilog.Core;

namespace FlinkDotNet.Common.Logging
{
    /// <summary>
    /// Factory for creating Serilog loggers with consistent configuration.
    /// Handles log file rotation and cleanup.
    /// </summary>
    public static class LoggerFactory
    {
        /// <summary>
        /// Creates a Serilog logger with file and console output.
        /// Automatically cleans up log files older than 1 day.
        /// </summary>
        /// <param name="fileSystem">File system abstraction for testability</param>
        /// <param name="logFileNamePattern">Pattern for log file names (e.g., "FlinkDotnet.log")</param>
        /// <returns>Configured Serilog logger instance</returns>
        public static Logger CreateLogger(IFileSystem fileSystem, string logFileNamePattern = "FlinkDotnet.log")
        {
            var logFilePath = Environment.GetEnvironmentVariable("LOG_FILE_PATH") ?? "test-logs";
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var logFile = fileSystem.Path.Combine(logFilePath, $"{logFileNamePattern}.{today}");

            // Clean up old log files (older than 1 day)
            CleanupOldLogFiles(fileSystem, logFilePath, $"{logFileNamePattern}.*");

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

        /// <summary>
        /// Cleans up old log files from the specified directory.
        /// </summary>
        /// <param name="fileSystem">File system abstraction</param>
        /// <param name="logFilePath">Directory containing log files</param>
        /// <param name="searchPattern">Pattern to match log files</param>
        private static void CleanupOldLogFiles(IFileSystem fileSystem, string logFilePath, string searchPattern)
        {
            try
            {
                if (fileSystem.Directory.Exists(logFilePath))
                {
                    var logFiles = fileSystem.Directory.GetFiles(logFilePath, searchPattern);
                    foreach (var file in logFiles)
                    {
                        var fileInfo = fileSystem.FileInfo.New(file);
                        if (fileInfo.LastWriteTimeUtc < DateTime.UtcNow.AddDays(-1))
                        {
                            fileSystem.File.Delete(file);
                        }
                    }
                }
            }
            catch
            {
                // Ignore cleanup errors - logging should not fail the application
            }
        }
    }
}
