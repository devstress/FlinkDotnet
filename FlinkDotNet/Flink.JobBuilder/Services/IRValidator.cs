using System;
using System.Collections.Generic;
using System.Linq;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Services
{
    /// <summary>
    /// Validation service for IR (Intermediate Representation) job definitions
    /// Enforces v1.0 schema compliance and business rules
    /// </summary>
    public class IRValidator
    {
        private readonly List<string> _errors = new();
        private readonly List<string> _warnings = new();

        /// <summary>
        /// Validates a job definition against IR v1.0 schema and business rules
        /// </summary>
        /// <param name="jobDefinition">The job definition to validate</param>
        /// <returns>Validation result with errors and warnings</returns>
        public ValidationResult Validate(JobDefinition jobDefinition)
        {
            _errors.Clear();
            _warnings.Clear();

            if (jobDefinition == null)
            {
                _errors.Add("JobDefinition cannot be null");
                return new ValidationResult(_errors, _warnings);
            }

            ValidateMetadata(jobDefinition.Metadata);
            ValidateSource(jobDefinition.Source);
            ValidateOperations(jobDefinition.Operations);
            ValidateSink(jobDefinition.Sink);
            ValidateBusinessRules(jobDefinition);

            return new ValidationResult(_errors, _warnings);
        }

        private void ValidateMetadata(JobMetadata metadata)
        {
            if (metadata == null)
            {
                _errors.Add("JobMetadata is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(metadata.JobId))
            {
                _errors.Add("JobMetadata.JobId is required and cannot be empty");
            }

            if (string.IsNullOrWhiteSpace(metadata.Version))
            {
                _errors.Add("JobMetadata.Version is required and cannot be empty");
            }
            else if (!IsValidSemVer(metadata.Version))
            {
                _errors.Add($"JobMetadata.Version '{metadata.Version}' is not a valid semantic version (e.g., 1.0.0)");
            }

            if (metadata.Parallelism.HasValue)
            {
                if (metadata.Parallelism < 1 || metadata.Parallelism > 1000)
                {
                    _errors.Add("JobMetadata.Parallelism must be between 1 and 1000");
                }
            }

            if (metadata.CreatedAt == default)
            {
                _warnings.Add("JobMetadata.CreatedAt should be set to job creation time");
            }
        }

        private void ValidateSource(ISourceDefinition source)
        {
            if (source == null)
            {
                _errors.Add("Source is required");
                return;
            }

            switch (source)
            {
                case KafkaSourceDefinition kafka:
                    ValidateKafkaSource(kafka);
                    break;
                case FileSourceDefinition file:
                    ValidateFileSource(file);
                    break;
                case HttpSourceDefinition http:
                    ValidateHttpSource(http);
                    break;
                case DatabaseSourceDefinition db:
                    ValidateDatabaseSource(db);
                    break;
                default:
                    _errors.Add($"Unknown source type: {source.GetType().Name}");
                    break;
            }
        }

        private void ValidateKafkaSource(KafkaSourceDefinition kafka)
        {
            if (string.IsNullOrWhiteSpace(kafka.Topic))
            {
                _errors.Add("KafkaSource.Topic is required");
            }
            else if (!IsValidKafkaTopicName(kafka.Topic))
            {
                _errors.Add($"KafkaSource.Topic '{kafka.Topic}' contains invalid characters. Use only alphanumeric, dots, underscores, and hyphens");
            }

            if (kafka.StartingOffsets != null && 
                kafka.StartingOffsets != "latest" && 
                kafka.StartingOffsets != "earliest")
            {
                _errors.Add("KafkaSource.StartingOffsets must be 'latest' or 'earliest'");
            }

            if (string.IsNullOrWhiteSpace(kafka.BootstrapServers))
            {
                _warnings.Add("KafkaSource.BootstrapServers should be specified for production use");
            }
        }

        private void ValidateFileSource(FileSourceDefinition file)
        {
            if (string.IsNullOrWhiteSpace(file.Path))
            {
                _errors.Add("FileSource.Path is required");
            }

            var validFormats = new[] { "text", "json", "csv", "parquet" };
            if (!validFormats.Contains(file.Format))
            {
                _errors.Add($"FileSource.Format '{file.Format}' is not supported. Valid formats: {string.Join(", ", validFormats)}");
            }
        }

        private void ValidateHttpSource(HttpSourceDefinition http)
        {
            if (string.IsNullOrWhiteSpace(http.Url))
            {
                _errors.Add("HttpSource.Url is required");
            }
            else if (!Uri.TryCreate(http.Url, UriKind.Absolute, out _))
            {
                _errors.Add($"HttpSource.Url '{http.Url}' is not a valid URI");
            }

            var validMethods = new[] { "GET", "POST", "PUT", "DELETE" };
            if (!validMethods.Contains(http.Method.ToUpper()))
            {
                _errors.Add($"HttpSource.Method '{http.Method}' is not supported. Valid methods: {string.Join(", ", validMethods)}");
            }

            if (http.IntervalSeconds < 1 || http.IntervalSeconds > 86400)
            {
                _errors.Add("HttpSource.IntervalSeconds must be between 1 and 86400 (1 day)");
            }
        }

        private void ValidateDatabaseSource(DatabaseSourceDefinition db)
        {
            if (string.IsNullOrWhiteSpace(db.ConnectionString))
            {
                _errors.Add("DatabaseSource.ConnectionString is required");
            }

            if (string.IsNullOrWhiteSpace(db.Query))
            {
                _errors.Add("DatabaseSource.Query is required");
            }

            if (db.PollingIntervalSeconds < 1 || db.PollingIntervalSeconds > 3600)
            {
                _errors.Add("DatabaseSource.PollingIntervalSeconds must be between 1 and 3600 (1 hour)");
            }

            var validDbTypes = new[] { "postgresql", "mysql", "sqlserver", "oracle" };
            if (db.DatabaseType != null && !validDbTypes.Contains(db.DatabaseType.ToLower()))
            {
                _errors.Add($"DatabaseSource.DatabaseType '{db.DatabaseType}' is not supported. Valid types: {string.Join(", ", validDbTypes)}");
            }
        }

        private void ValidateOperations(List<IOperationDefinition> operations)
        {
            if (operations == null)
            {
                _warnings.Add("Operations list is null, no transformations will be applied");
                return;
            }

            for (int i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];
                if (operation == null)
                {
                    _errors.Add($"Operation at index {i} is null");
                    continue;
                }

                ValidateOperation(operation, i);
            }

            ValidateOperationSequence(operations);
        }

        private void ValidateOperation(IOperationDefinition operation, int index)
        {
            var prefix = $"Operation[{index}]";

            switch (operation)
            {
                case FilterOperationDefinition filter:
                    if (string.IsNullOrWhiteSpace(filter.Expression))
                    {
                        _errors.Add($"{prefix}.Filter.Expression is required");
                    }
                    break;

                case MapOperationDefinition map:
                    if (string.IsNullOrWhiteSpace(map.Expression))
                    {
                        _errors.Add($"{prefix}.Map.Expression is required");
                    }
                    break;

                case WindowOperationDefinition window:
                    ValidateWindowOperation(window, prefix);
                    break;

                case TimerOperationDefinition timer:
                    ValidateTimerOperation(timer, prefix);
                    break;

                case AsyncFunctionOperationDefinition asyncFunc:
                    ValidateAsyncFunctionOperation(asyncFunc, prefix);
                    break;

                case RetryOperationDefinition retry:
                    ValidateRetryOperation(retry, prefix);
                    break;

                default:
                    // Other operations have minimal validation requirements
                    break;
            }
        }

        private void ValidateWindowOperation(WindowOperationDefinition window, string prefix)
        {
            var validWindowTypes = new[] { "TUMBLING", "SLIDING", "SESSION" };
            if (!validWindowTypes.Contains(window.WindowType.ToUpper()))
            {
                _errors.Add($"{prefix}.Window.WindowType '{window.WindowType}' is not supported. Valid types: {string.Join(", ", validWindowTypes)}");
            }

            if (window.Size < 1 || window.Size > 86400)
            {
                _errors.Add($"{prefix}.Window.Size must be between 1 and 86400");
            }

            var validTimeUnits = new[] { "SECONDS", "MINUTES", "HOURS" };
            if (!validTimeUnits.Contains(window.TimeUnit.ToUpper()))
            {
                _errors.Add($"{prefix}.Window.TimeUnit '{window.TimeUnit}' is not supported. Valid units: {string.Join(", ", validTimeUnits)}");
            }

            if (window.WindowType.ToUpper() == "SLIDING" && (!window.Slide.HasValue || window.Slide <= 0))
            {
                _errors.Add($"{prefix}.Window.Slide is required for sliding windows and must be positive");
            }
        }

        private void ValidateTimerOperation(TimerOperationDefinition timer, string prefix)
        {
            if (timer.DelayMs < 1 || timer.DelayMs > 86400000) // 1 day in ms
            {
                _errors.Add($"{prefix}.Timer.DelayMs must be between 1 and 86400000 (1 day)");
            }

            var validTimerTypes = new[] { "processing", "event" };
            if (!validTimerTypes.Contains(timer.TimerType.ToLower()))
            {
                _errors.Add($"{prefix}.Timer.TimerType '{timer.TimerType}' is not supported. Valid types: {string.Join(", ", validTimerTypes)}");
            }
        }

        private void ValidateAsyncFunctionOperation(AsyncFunctionOperationDefinition asyncFunc, string prefix)
        {
            if (asyncFunc.TimeoutMs < 100 || asyncFunc.TimeoutMs > 300000) // 5 minutes max
            {
                _errors.Add($"{prefix}.AsyncFunction.TimeoutMs must be between 100 and 300000 (5 minutes)");
            }

            if (asyncFunc.MaxRetries < 0 || asyncFunc.MaxRetries > 10)
            {
                _errors.Add($"{prefix}.AsyncFunction.MaxRetries must be between 0 and 10");
            }

            if (asyncFunc.FunctionType == "http" && string.IsNullOrWhiteSpace(asyncFunc.Url))
            {
                _errors.Add($"{prefix}.AsyncFunction.Url is required for HTTP function type");
            }

            if (asyncFunc.FunctionType == "database" && string.IsNullOrWhiteSpace(asyncFunc.ConnectionString))
            {
                _errors.Add($"{prefix}.AsyncFunction.ConnectionString is required for database function type");
            }
        }

        private void ValidateRetryOperation(RetryOperationDefinition retry, string prefix)
        {
            if (retry.MaxRetries < 1 || retry.MaxRetries > 20)
            {
                _errors.Add($"{prefix}.Retry.MaxRetries must be between 1 and 20");
            }

            if (retry.DelayMs == null || retry.DelayMs.Count == 0)
            {
                _errors.Add($"{prefix}.Retry.DelayMs array is required and cannot be empty");
            }
            else
            {
                for (int i = 0; i < retry.DelayMs.Count; i++)
                {
                    if (retry.DelayMs[i] < 1000) // Minimum 1 second
                    {
                        _errors.Add($"{prefix}.Retry.DelayMs[{i}] must be at least 1000ms (1 second)");
                    }
                }
            }

            if (string.IsNullOrWhiteSpace(retry.StateKey))
            {
                _errors.Add($"{prefix}.Retry.StateKey is required for tracking retry attempts");
            }
        }

        private void ValidateOperationSequence(List<IOperationDefinition> operations)
        {
            // Check for logical operation ordering issues
            bool hasWindow = false;
            bool hasGroupBy = false;

            for (int i = 0; i < operations.Count; i++)
            {
                var operation = operations[i];

                if (operation is GroupByOperationDefinition)
                {
                    hasGroupBy = true;
                }
                else if (operation is WindowOperationDefinition)
                {
                    hasWindow = true;
                    if (!hasGroupBy)
                    {
                        _warnings.Add($"Window operation at index {i} without preceding GroupBy may not behave as expected");
                    }
                }
                else if (operation is AggregateOperationDefinition && !hasGroupBy && !hasWindow)
                {
                    _warnings.Add($"Aggregate operation at index {i} without GroupBy or Window may not behave as expected");
                }
            }
        }

        private void ValidateSink(ISinkDefinition sink)
        {
            if (sink == null)
            {
                _errors.Add("Sink is required");
                return;
            }

            switch (sink)
            {
                case KafkaSinkDefinition kafka:
                    ValidateKafkaSink(kafka);
                    break;
                case DatabaseSinkDefinition db:
                    ValidateDatabaseSink(db);
                    break;
                case HttpSinkDefinition http:
                    ValidateHttpSink(http);
                    break;
                case FileSinkDefinition file:
                    ValidateFileSink(file);
                    break;
                // Console and Redis sinks have minimal validation requirements
            }
        }

        private void ValidateKafkaSink(KafkaSinkDefinition kafka)
        {
            if (string.IsNullOrWhiteSpace(kafka.Topic))
            {
                _errors.Add("KafkaSink.Topic is required");
            }
            else if (!IsValidKafkaTopicName(kafka.Topic))
            {
                _errors.Add($"KafkaSink.Topic '{kafka.Topic}' contains invalid characters");
            }

            if (kafka.Serializer != null)
            {
                var validSerializers = new[] { "json", "avro", "string" };
                if (!validSerializers.Contains(kafka.Serializer.ToLower()))
                {
                    _errors.Add($"KafkaSink.Serializer '{kafka.Serializer}' is not supported. Valid serializers: {string.Join(", ", validSerializers)}");
                }
            }
        }

        private void ValidateDatabaseSink(DatabaseSinkDefinition db)
        {
            if (string.IsNullOrWhiteSpace(db.ConnectionString))
            {
                _errors.Add("DatabaseSink.ConnectionString is required");
            }

            if (string.IsNullOrWhiteSpace(db.Table))
            {
                _errors.Add("DatabaseSink.Table is required");
            }
        }

        private void ValidateHttpSink(HttpSinkDefinition http)
        {
            if (string.IsNullOrWhiteSpace(http.Url))
            {
                _errors.Add("HttpSink.Url is required");
            }
            else if (!Uri.TryCreate(http.Url, UriKind.Absolute, out _))
            {
                _errors.Add($"HttpSink.Url '{http.Url}' is not a valid URI");
            }

            var validMethods = new[] { "POST", "PUT", "PATCH" };
            if (!validMethods.Contains(http.Method.ToUpper()))
            {
                _errors.Add($"HttpSink.Method '{http.Method}' is not supported for sinks. Valid methods: {string.Join(", ", validMethods)}");
            }

            if (http.TimeoutMs < 100 || http.TimeoutMs > 300000)
            {
                _errors.Add("HttpSink.TimeoutMs must be between 100 and 300000 (5 minutes)");
            }
        }

        private void ValidateFileSink(FileSinkDefinition file)
        {
            if (string.IsNullOrWhiteSpace(file.Path))
            {
                _errors.Add("FileSink.Path is required");
            }

            var validFormats = new[] { "json", "csv", "parquet", "text" };
            if (!validFormats.Contains(file.Format.ToLower()))
            {
                _errors.Add($"FileSink.Format '{file.Format}' is not supported. Valid formats: {string.Join(", ", validFormats)}");
            }
        }

        private void ValidateBusinessRules(JobDefinition jobDefinition)
        {
            // Check for circular dependencies in joins
            CheckForCircularJoinDependencies(jobDefinition.Operations);

            // Validate state key uniqueness
            ValidateStateKeyUniqueness(jobDefinition.Operations);

            // Check async operation timeout consistency
            ValidateAsyncTimeoutConsistency(jobDefinition.Operations);
        }

        private void CheckForCircularJoinDependencies(List<IOperationDefinition> operations)
        {
            var joinSources = operations
                .OfType<JoinOperationDefinition>()
                .Select(j => j.RightSource)
                .ToList();

            // For now, just warn about complex join scenarios
            if (joinSources.Count > 1)
            {
                _warnings.Add("Multiple join operations detected. Verify that join dependencies are not circular");
            }
        }

        private void ValidateStateKeyUniqueness(List<IOperationDefinition> operations)
        {
            var stateKeys = new List<string>();

            foreach (var operation in operations)
            {
                var keys = new List<string>();

                if (operation is StateOperationDefinition state)
                {
                    keys.Add(state.StateKey);
                }
                else if (operation is AsyncFunctionOperationDefinition asyncFunc && !string.IsNullOrWhiteSpace(asyncFunc.StateKey))
                {
                    keys.Add(asyncFunc.StateKey);
                }
                else if (operation is RetryOperationDefinition retry)
                {
                    keys.Add(retry.StateKey);
                }
                else if (operation is ProcessFunctionOperationDefinition process)
                {
                    keys.AddRange(process.StateKeys);
                }

                foreach (var key in keys.Where(k => !string.IsNullOrWhiteSpace(k)))
                {
                    if (stateKeys.Contains(key))
                    {
                        _errors.Add($"Duplicate state key '{key}' found. State keys must be unique within a job");
                    }
                    else
                    {
                        stateKeys.Add(key);
                    }
                }
            }
        }

        private void ValidateAsyncTimeoutConsistency(List<IOperationDefinition> operations)
        {
            var asyncTimeouts = operations
                .OfType<AsyncFunctionOperationDefinition>()
                .Select(a => a.TimeoutMs)
                .ToList();

            if (asyncTimeouts.Any() && asyncTimeouts.Max() > 30000) // 30 seconds
            {
                _warnings.Add("Async operation timeout exceeds 30 seconds. Consider using shorter timeouts with retry logic for better resilience");
            }
        }

        private static bool IsValidSemVer(string version)
        {
            var parts = version.Split('.');
            return parts.Length == 3 && parts.All(p => int.TryParse(p, out _));
        }

        private static bool IsValidKafkaTopicName(string topicName)
        {
            return !string.IsNullOrWhiteSpace(topicName) &&
                   topicName.All(c => char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-');
        }
    }

    /// <summary>
    /// Result of IR validation
    /// </summary>
    public class ValidationResult
    {
        public ValidationResult(List<string> errors, List<string> warnings)
        {
            Errors = errors ?? new List<string>();
            Warnings = warnings ?? new List<string>();
        }

        public List<string> Errors { get; }
        public List<string> Warnings { get; }
        public bool IsValid => Errors.Count == 0;
        public bool HasWarnings => Warnings.Count > 0;

        public override string ToString()
        {
            var result = $"Validation Result: {(IsValid ? "VALID" : "INVALID")}";
            
            if (Errors.Any())
            {
                result += $"\nErrors ({Errors.Count}):\n  - " + string.Join("\n  - ", Errors);
            }

            if (Warnings.Any())
            {
                result += $"\nWarnings ({Warnings.Count}):\n  - " + string.Join("\n  - ", Warnings);
            }

            return result;
        }
    }
}