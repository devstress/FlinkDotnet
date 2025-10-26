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

using System.Collections.Generic;
using System.IO.Abstractions;
using Flink.JobBuilder.Models;
using FlinkDotNet.Common.Logging;

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Captures DataStream API operations for translation to JobDefinition.
    /// This enables the native Flink DataStream API to work with the Gateway infrastructure.
    /// </summary>
    internal class OperationCapture
    {
        private static readonly IFileSystem _fileSystem = new FileSystem();
        private static readonly Serilog.Core.Logger _logger = LoggerFactory.CreateLogger(_fileSystem);

        private readonly List<CapturedOperation> _operations = [];
        private KafkaSourceDefinition? _kafkaSource;
        private KafkaSinkDefinition? _kafkaSink;
        private bool _hasTimestampAssigner;
        private WindowDefinition? _windowDefinition;
        private object? _deserializationFunction;
        private object? _serializationFunction;

        public void CaptureKafkaSource(string topic, string bootstrapServers, string groupId, string startingOffsets, object? deserializer = null)
        {
            _logger.Information(
                "[OperationCapture.CaptureKafkaSource] Capturing Kafka source: topic={Topic}, bootstrapServers={BootstrapServers}, groupId={GroupId}, startingOffsets={StartingOffsets}",
                topic, bootstrapServers, groupId, startingOffsets);

            this._kafkaSource = new KafkaSourceDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                StartingOffsets = startingOffsets
            };
            this._deserializationFunction = deserializer;

            _logger.Information("[OperationCapture.CaptureKafkaSource] Created KafkaSourceDefinition with BootstrapServers={BootstrapServers}", this._kafkaSource.BootstrapServers);
        }

        public void CaptureMapOperation(string operationType, object? function = null)
        {
            this._operations.Add(new CapturedOperation
            {
                Type = "Map",
                OperationType = operationType,
                Function = function
            });
        }

        public void CaptureTimestampAssigner(object assigner)
        {
            this._hasTimestampAssigner = true;
            this._operations.Add(new CapturedOperation
            {
                Type = "AssignTimestampsAndWatermarks",
                Function = assigner
            });
        }

        public void CaptureTimeWindow(Time windowSize)
        {
            this._windowDefinition = new WindowDefinition
            {
                WindowType = "TUMBLING",
                Size = windowSize.ToMilliseconds(),
                TimeUnit = "MILLISECONDS",
                IsCountBased = false
            };

            this._operations.Add(new CapturedOperation
            {
                Type = "TimeWindowAll",
                Function = windowSize
            });
        }

        public void CaptureCountWindow(int windowSize)
        {
            this._windowDefinition = new WindowDefinition
            {
                WindowType = "TUMBLING",
                Size = windowSize,
                TimeUnit = "COUNT",
                IsCountBased = true
            };

            this._operations.Add(new CapturedOperation
            {
                Type = "CountWindowAll",
                Function = windowSize
            });
        }

        public void CaptureAggregateOperation(object aggregateFunction)
        {
            this._operations.Add(new CapturedOperation
            {
                Type = "Aggregate",
                Function = aggregateFunction
            });
        }

        public void CaptureKafkaSink(string topic, string bootstrapServers, object? serializer = null)
        {
            _logger.Information("[OperationCapture.CaptureKafkaSink] Capturing Kafka sink: topic={Topic}, bootstrapServers={BootstrapServers}",
                topic, bootstrapServers);

            this._kafkaSink = new KafkaSinkDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers
            };
            this._serializationFunction = serializer;

            _logger.Information("[OperationCapture.CaptureKafkaSink] Created KafkaSinkDefinition with BootstrapServers={BootstrapServers}", this._kafkaSink.BootstrapServers);
        }

        public JobDefinition ToJobDefinition(string jobId, string jobName)
        {
            _logger.Debug("[OperationCapture.ToJobDefinition] Starting translation - jobId={JobId}, jobName={JobName}, kafkaSource.BootstrapServers={BootstrapServers}",
                jobId, jobName, this._kafkaSource?.BootstrapServers);

            if (this._kafkaSource == null)
            {
                _logger.Error("[OperationCapture.ToJobDefinition] No Kafka source defined!");
                throw new System.InvalidOperationException("No Kafka source defined. Use AddKafkaSource() or FromKafka() before executing.");
            }

            JobDefinition jobDef = this.CreateJobDefinition(jobId, jobName);
            _logger.Debug("[OperationCapture.ToJobDefinition] After CreateJobDefinition - Source.BootstrapServers={BootstrapServers}",
                (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);

            this.ConfigureJobMetadata(jobDef);
            _logger.Debug("[OperationCapture.ToJobDefinition] After ConfigureJobMetadata - Source.BootstrapServers={BootstrapServers}",
                (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);

            this.TranslateOperations(jobDef);
            _logger.Information("[OperationCapture.ToJobDefinition] Translation complete - Source.BootstrapServers={SourceBootstrapServers}, Sink.BootstrapServers={SinkBootstrapServers}",
                (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers, (jobDef.Sink as KafkaSinkDefinition)?.BootstrapServers);

            return jobDef;
        }

        private JobDefinition CreateJobDefinition(string jobId, string jobName)
        {
            _logger.Debug("[OperationCapture.CreateJobDefinition] Creating JobDefinition with _kafkaSource.BootstrapServers={BootstrapServers}", this._kafkaSource?.BootstrapServers);

            JobDefinition jobDef = new()
            {
                Source = this._kafkaSource!,
                Operations = [],
                Sink = this._kafkaSink,
                Metadata = new JobMetadata
                {
                    JobId = jobId,
                    JobName = jobName,
                    CreatedAt = System.DateTime.UtcNow,
                    Version = "1.0",
                    Properties = []
                }
            };

            _logger.Debug("[OperationCapture.CreateJobDefinition] Created JobDefinition.Source.BootstrapServers={BootstrapServers}", (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);
            return jobDef;
        }

        private void ConfigureJobMetadata(JobDefinition jobDef)
        {
            if (this._hasTimestampAssigner)
            {
                jobDef.Metadata.Properties["timeCharacteristic"] = "EventTime";
            }

            if (this._deserializationFunction != null)
            {
                jobDef.Metadata.Properties["deserializationFunction"] = this._deserializationFunction.GetType().FullName ?? "Unknown";
            }

            if (this._serializationFunction == null)
            {
                return;
            }

            jobDef.Metadata.Properties["serializationFunction"] = this._serializationFunction.GetType().FullName ?? "Unknown";
        }

        private void TranslateOperations(JobDefinition jobDef)
        {
            foreach (CapturedOperation operation in this._operations)
            {
                switch (operation.Type)
                {
                    case "Map":
                        TranslateMapOperation(jobDef, operation);
                        break;
                    case "Filter":
                        TranslateFilterOperation(jobDef, operation);
                        break;
                    case "TimeWindowAll":
                    case "CountWindowAll":
                        // Window information is captured but NOT added as a separate operation
                        // It will be incorporated into the AggregateOperationDefinition
                        // DO NOT call TranslateWindowOperation() here!
                        break;
                    case "Aggregate":
                        this.TranslateAggregateOperation(jobDef, operation);
                        break;
                    default:
                        _logger.Warning("[OperationCapture.TranslateOperations] Unknown operation type: {OperationType}", operation.Type);
                        break;
                }
            }
        }

        private static void TranslateMapOperation(JobDefinition jobDef, CapturedOperation operation)
        {
            if (operation.OperationType == "upper")
            {
                jobDef.Operations.Add(new MapOperationDefinition { Expression = "upper" });
            }
            else if (operation.OperationType == "lower")
            {
                jobDef.Operations.Add(new MapOperationDefinition { Expression = "lower" });
            }
            else if (operation.Function != null)
            {
                // Check if the function is a known IMapFunction implementation
                string functionTypeName = operation.Function.GetType().Name;
                string functionFullName = operation.Function.GetType().FullName ?? "";

                // Map WordsCapitalizer and other uppercase functions to "upper"
                if (functionTypeName.Contains("Capitalizer", System.StringComparison.OrdinalIgnoreCase) ||
                    functionTypeName.Contains("Upper", System.StringComparison.OrdinalIgnoreCase) ||
                    functionFullName.Contains("WordsCapitalizer"))
                {
                    _logger.Information("[OperationCapture.TranslateMapOperation] Translating {FunctionType} to 'upper' expression", functionTypeName);
                    jobDef.Operations.Add(new MapOperationDefinition { Expression = "upper" });
                }
                // Map lowercase functions to "lower"
                else if (functionTypeName.Contains("Lower", System.StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Information("[OperationCapture.TranslateMapOperation] Translating {FunctionType} to 'lower' expression", functionTypeName);
                    jobDef.Operations.Add(new MapOperationDefinition { Expression = "lower" });
                }
                else
                {
                    // For unknown functions, pass the type name to FlinkJobRunner
                    // FlinkJobRunner will use identity transformation if not recognized
                    _logger.Warning("[OperationCapture.TranslateMapOperation] Unknown map function type: {FunctionType}, will use identity transformation", functionTypeName);
                    jobDef.Operations.Add(new MapOperationDefinition
                    {
                        Expression = $"function:{functionFullName}"
                    });
                }
            }
        }

        private static void TranslateFilterOperation(JobDefinition jobDef, CapturedOperation operation)
        {
            if (operation.Function == null)
            {
                return;
            }

            jobDef.Operations.Add(new FilterOperationDefinition
            {
                Expression = $"function:{operation.Function.GetType().FullName}"
            });
        }

        // NOTE: TranslateWindowOperation was removed as dead code.
        // Window information is now incorporated directly into AggregateOperationDefinition
        // in TranslateAggregateOperation() method. See line 271-276 for explanation.

        private void TranslateAggregateOperation(JobDefinition jobDef, CapturedOperation operation)
        {
            if (operation.Function != null)
            {
                jobDef.Metadata.Properties["aggregateFunction"] = operation.Function.GetType().FullName ?? "Unknown";
            }

            long? windowSeconds = null;
            int? windowCount = null;

            if (this._windowDefinition != null)
            {
                if (this._windowDefinition.IsCountBased)
                {
                    // COUNT-BASED WINDOW
                    windowCount = (int) this._windowDefinition.Size;
                    _logger.Information("[OperationCapture.TranslateAggregateOperation] Using COUNT-based window: {WindowCount} messages",
                        windowCount);
                }
                else
                {
                    // TIME-BASED WINDOW - Convert window size from milliseconds to seconds
                    windowSeconds = this._windowDefinition.Size / 1000;
                    _logger.Information("[OperationCapture.TranslateAggregateOperation] Using TIME-based window: {WindowSeconds} seconds (from {WindowSize} ms)",
                        windowSeconds, this._windowDefinition.Size);
                }
            }
            else
            {
                _logger.Warning("[OperationCapture.TranslateAggregateOperation] No window defined");
            }

            AggregateOperationDefinition aggDef = new()
            {
                AggregationType = "COLLECT",
                Field = "*",
                WindowSeconds = windowSeconds,
                WindowCount = windowCount
            };

            jobDef.Operations.Add(aggDef);
            _logger.Information("[OperationCapture.TranslateAggregateOperation] Created AggregateOperationDefinition with WindowSeconds={WindowSeconds}, WindowCount={WindowCount}",
                windowSeconds, windowCount);
        }

        public bool HasOperations() => this._operations.Count > 0 || this._kafkaSource != null;
    }

    internal class CapturedOperation
    {
        public string Type { get; set; } = string.Empty;
        public string? OperationType
        {
            get; set;
        }
        public object? Function
        {
            get; set;
        }
    }

    internal class WindowDefinition
    {
        public string WindowType { get; set; } = string.Empty;
        public long Size
        {
            get; set;
        }
        public string TimeUnit { get; set; } = string.Empty;
        public bool IsCountBased
        {
            get; set;
        }
    }
}
