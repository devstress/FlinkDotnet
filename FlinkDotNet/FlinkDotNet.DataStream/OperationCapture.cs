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
using Flink.JobBuilder.Models;
using Serilog;

namespace FlinkDotNet.DataStream
{
    /// <summary>
    /// Captures DataStream API operations for translation to JobDefinition.
    /// This enables the native Flink DataStream API to work with the Gateway infrastructure.
    /// </summary>
    internal class OperationCapture
    {
        private static readonly ILogger _logger = new LoggerConfiguration()
            .WriteTo.File(
                path: "LocalTesting/test-logs/flink-dotnet-.log",
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                fileSizeLimitBytes: 100_000_000,
                retainedFileCountLimit: 30,
                shared: true)
            .WriteTo.Console()
            .MinimumLevel.Debug()
            .CreateLogger();
        
        private readonly List<CapturedOperation> _operations = new();
        private KafkaSourceDefinition? _kafkaSource;
        private KafkaSinkDefinition? _kafkaSink;
        private bool _hasTimestampAssigner;
        private WindowDefinition? _windowDefinition;
        private object? _deserializationFunction;
        private object? _serializationFunction;

        public void CaptureKafkaSource(string topic, string bootstrapServers, string groupId, string startingOffsets, object? deserializer = null)
        {
            _logger.Information("[OperationCapture.CaptureKafkaSource] Capturing Kafka source: topic={Topic}, bootstrapServers={BootstrapServers}, groupId={GroupId}, startingOffsets={StartingOffsets}",
                topic, bootstrapServers, groupId, startingOffsets);
            
            _kafkaSource = new KafkaSourceDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers,
                GroupId = groupId,
                StartingOffsets = startingOffsets
            };
            _deserializationFunction = deserializer;
            
            _logger.Information("[OperationCapture.CaptureKafkaSource] Created KafkaSourceDefinition with BootstrapServers={BootstrapServers}", _kafkaSource.BootstrapServers);
        }

        public void CaptureMapOperation(string operationType, object? function = null)
        {
            _operations.Add(new CapturedOperation
            {
                Type = "Map",
                OperationType = operationType,
                Function = function
            });
        }

        public void CaptureFilterOperation(object? function = null)
        {
            _operations.Add(new CapturedOperation
            {
                Type = "Filter",
                Function = function
            });
        }

        public void CaptureFlatMapOperation(object? function = null)
        {
            _operations.Add(new CapturedOperation
            {
                Type = "FlatMap",
                Function = function
            });
        }

        public void CaptureTimestampAssigner(object assigner)
        {
            _hasTimestampAssigner = true;
            _operations.Add(new CapturedOperation
            {
                Type = "AssignTimestampsAndWatermarks",
                Function = assigner
            });
        }

        public void CaptureTimeWindow(Time windowSize)
        {
            _windowDefinition = new WindowDefinition
            {
                WindowType = "TUMBLING",
                Size = windowSize.ToMilliseconds(),
                TimeUnit = "MILLISECONDS"
            };
            
            _operations.Add(new CapturedOperation
            {
                Type = "TimeWindowAll",
                Function = windowSize
            });
        }

        public void CaptureAggregateOperation(object aggregateFunction)
        {
            _operations.Add(new CapturedOperation
            {
                Type = "Aggregate",
                Function = aggregateFunction
            });
        }

        public void CaptureKafkaSink(string topic, string bootstrapServers, object? serializer = null)
        {
            _logger.Information("[OperationCapture.CaptureKafkaSink] Capturing Kafka sink: topic={Topic}, bootstrapServers={BootstrapServers}",
                topic, bootstrapServers);
            
            _kafkaSink = new KafkaSinkDefinition
            {
                Topic = topic,
                BootstrapServers = bootstrapServers
            };
            _serializationFunction = serializer;
            
            _logger.Information("[OperationCapture.CaptureKafkaSink] Created KafkaSinkDefinition with BootstrapServers={BootstrapServers}", _kafkaSink.BootstrapServers);
        }

        public JobDefinition ToJobDefinition(string jobId, string jobName)
        {
            _logger.Information("[OperationCapture.ToJobDefinition] Starting translation to JobDefinition: jobId={JobId}, jobName={JobName}", jobId, jobName);
            _logger.Information("[OperationCapture.ToJobDefinition] Current _kafkaSource.BootstrapServers={BootstrapServers}", _kafkaSource?.BootstrapServers);
            
            if (_kafkaSource == null)
            {
                _logger.Error("[OperationCapture.ToJobDefinition] No Kafka source defined!");
                throw new System.InvalidOperationException("No Kafka source defined. Use AddKafkaSource() or FromKafka() before executing.");
            }

            var jobDef = CreateJobDefinition(jobId, jobName);
            _logger.Information("[OperationCapture.ToJobDefinition] After CreateJobDefinition: Source.BootstrapServers={BootstrapServers}", (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);
            
            ConfigureJobMetadata(jobDef);
            _logger.Information("[OperationCapture.ToJobDefinition] After ConfigureJobMetadata: Source.BootstrapServers={BootstrapServers}", (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);
            
            TranslateOperations(jobDef);
            _logger.Information("[OperationCapture.ToJobDefinition] After TranslateOperations: Source.BootstrapServers={BootstrapServers}", (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);
            
            _logger.Information("[OperationCapture.ToJobDefinition] Final JobDefinition: Source.BootstrapServers={BootstrapServers}, Sink.BootstrapServers={SinkBootstrapServers}",
                (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers, (jobDef.Sink as KafkaSinkDefinition)?.BootstrapServers);

            return jobDef;
        }

        private JobDefinition CreateJobDefinition(string jobId, string jobName)
        {
            _logger.Debug("[OperationCapture.CreateJobDefinition] Creating JobDefinition with _kafkaSource.BootstrapServers={BootstrapServers}", _kafkaSource?.BootstrapServers);
            
            var jobDef = new JobDefinition
            {
                Source = _kafkaSource!,
                Operations = new List<IOperationDefinition>(),
                Sink = _kafkaSink,
                Metadata = new JobMetadata
                {
                    JobId = jobId,
                    JobName = jobName,
                    CreatedAt = System.DateTime.UtcNow,
                    Version = "1.0",
                    Properties = new Dictionary<string, string>()
                }
            };
            
            _logger.Debug("[OperationCapture.CreateJobDefinition] Created JobDefinition.Source.BootstrapServers={BootstrapServers}", (jobDef.Source as KafkaSourceDefinition)?.BootstrapServers);
            return jobDef;
        }

        private void ConfigureJobMetadata(JobDefinition jobDef)
        {
            if (_hasTimestampAssigner)
            {
                jobDef.Metadata.Properties["timeCharacteristic"] = "EventTime";
            }

            if (_deserializationFunction != null)
            {
                jobDef.Metadata.Properties["deserializationFunction"] = _deserializationFunction.GetType().FullName ?? "Unknown";
            }

            if (_serializationFunction != null)
            {
                jobDef.Metadata.Properties["serializationFunction"] = _serializationFunction.GetType().FullName ?? "Unknown";
            }
        }

        private void TranslateOperations(JobDefinition jobDef)
        {
            foreach (var operation in _operations)
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
                        TranslateWindowOperation(jobDef);
                        break;
                    case "Aggregate":
                        TranslateAggregateOperation(jobDef, operation);
                        break;
                }
            }
        }

        private void TranslateMapOperation(JobDefinition jobDef, CapturedOperation operation)
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
                jobDef.Operations.Add(new MapOperationDefinition
                {
                    Expression = $"function:{operation.Function.GetType().FullName}"
                });
            }
        }

        private void TranslateFilterOperation(JobDefinition jobDef, CapturedOperation operation)
        {
            if (operation.Function != null)
            {
                jobDef.Operations.Add(new FilterOperationDefinition
                {
                    Expression = $"function:{operation.Function.GetType().FullName}"
                });
            }
        }

        private void TranslateWindowOperation(JobDefinition jobDef)
        {
            if (_windowDefinition != null)
            {
                jobDef.Operations.Add(new WindowOperationDefinition
                {
                    WindowType = _windowDefinition.WindowType,
                    Size = (int)(_windowDefinition.Size / 3600000), // Convert milliseconds to hours
                    TimeUnit = "HOURS",
                    TimeField = "sentAt" // This should be extracted from TimestampAssigner
                });
            }
        }

        private void TranslateAggregateOperation(JobDefinition jobDef, CapturedOperation operation)
        {
            if (operation.Function != null)
            {
                jobDef.Metadata.Properties["aggregateFunction"] = operation.Function.GetType().FullName ?? "Unknown";
            }
            
            jobDef.Operations.Add(new AggregateOperationDefinition
            {
                AggregationType = "COLLECT",
                Field = "*"
            });
        }

        public bool HasOperations() => _operations.Count > 0 || _kafkaSource != null;
    }

    internal class CapturedOperation
    {
        public string Type { get; set; } = string.Empty;
        public string? OperationType { get; set; }
        public object? Function { get; set; }
    }

    internal class WindowDefinition
    {
        public string WindowType { get; set; } = string.Empty;
        public long Size { get; set; }
        public string TimeUnit { get; set; } = string.Empty;
    }
}