using System;
using System.Collections.Generic;
using System.Linq;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Services
{
    public sealed class IrValidationResult
    {
        public bool IsValid => this.Errors.Count == 0;
        public List<string> Errors { get; } = [];
    }

    public static class JobDefinitionValidator
    {
        public static IrValidationResult Validate(JobDefinition job)
        {
            var errors = new List<string>();

            ValidateMetadata(job.Metadata, errors);
            ValidateJobStructure(job, errors);

            if (job.Source != null)
            {
                ValidateSource(job.Source, errors);
            }

            if (job.Operations != null)
            {
                for (var i = 0; i < job.Operations.Count; i++)
                {
                    ValidateOperation(job.Operations[i], i, errors);
                }
            }

            if (job.Sink != null)
            {
                ValidateSink(job.Sink, errors);
            }

            var result = new IrValidationResult();
            result.Errors.AddRange(errors);
            return result;
        }

        private static void ValidateMetadata(JobMetadata metadata, List<string> errors)
        {
            if (metadata == null)
            {
                errors.Add("metadata is required");
                return;
            }

            if (string.IsNullOrWhiteSpace(metadata.JobId))
            {
                errors.Add("metadata.jobId is required");
            }

            if (string.IsNullOrWhiteSpace(metadata.Version))
            {
                errors.Add("metadata.version is required");
            }

            if (metadata.Parallelism.HasValue && metadata.Parallelism <= 0)
            {
                errors.Add("metadata.parallelism must be >= 1 when provided");
            }
        }

        private static void ValidateJobStructure(JobDefinition job, List<string> errors)
        {
            if (job.Source == null)
            {
                errors.Add("source is required");
            }

            var isSqlJob = job.Source is SqlSourceDefinition;
            if (!isSqlJob && job.Sink == null)
            {
                errors.Add("sink is required");
            }
        }

        private static void ValidateSource(ISourceDefinition source, List<string> errors)
        {
            switch (source)
            {
                case SqlSourceDefinition s:
                    ValidateSqlSource(s, errors);
                    break;
                case KafkaSourceDefinition k:
                    ValidateKafkaSource(k, errors);
                    break;
                case FileSourceDefinition f:
                    ValidateFileSource(f, errors);
                    break;
                case HttpSourceDefinition h:
                    ValidateHttpSource(h, errors);
                    break;
                case DatabaseSourceDefinition d:
                    ValidateDatabaseSource(d, errors);
                    break;
                default:
                    // Unknown source types are not validated (extensibility)
                    break;
            }
        }

        private static void ValidateSqlSource(SqlSourceDefinition source, List<string> errors)
        {
            if (source.Statements == null || source.Statements.Count == 0)
            {
                errors.Add("source.sql.statements must contain at least one statement");
            }
        }

        private static void ValidateKafkaSource(KafkaSourceDefinition source, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(source.Topic))
            {
                errors.Add("source.kafka.topic is required");
            }
        }

        private static void ValidateFileSource(FileSourceDefinition source, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(source.Path))
            {
                errors.Add("source.file.path is required");
            }

            if (string.IsNullOrWhiteSpace(source.Format))
            {
                errors.Add("source.file.format is required");
            }
        }

        private static void ValidateHttpSource(HttpSourceDefinition source, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(source.Url))
            {
                errors.Add("source.http.url is required");
            }

            if (source.IntervalSeconds <= 0)
            {
                errors.Add("source.http.intervalSeconds must be > 0");
            }
        }

        private static void ValidateDatabaseSource(DatabaseSourceDefinition source, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(source.ConnectionString))
            {
                errors.Add("source.database.connectionString is required");
            }

            if (string.IsNullOrWhiteSpace(source.Query))
            {
                errors.Add("source.database.query is required");
            }

            if (source.PollingIntervalSeconds <= 0)
            {
                errors.Add("source.database.pollingIntervalSeconds must be > 0");
            }
        }

        private static void ValidateOperation(IOperationDefinition operation, int index, List<string> errors)
        {
            switch (operation)
            {
                case FilterOperationDefinition f:
                    ValidateFilterOperation(f, index, errors);
                    break;
                case MapOperationDefinition m:
                    ValidateMapOperation(m, index, errors);
                    break;
                case GroupByOperationDefinition g:
                    ValidateGroupByOperation(g, index, errors);
                    break;
                case AggregateOperationDefinition a:
                    ValidateAggregateOperation(a, index, errors);
                    break;
                case WindowOperationDefinition w:
                    ValidateWindowOperation(w, index, errors);
                    break;
                case JoinOperationDefinition j:
                    ValidateJoinOperation(j, index, errors);
                    break;
                case AsyncFunctionOperationDefinition af:
                    ValidateAsyncFunctionOperation(af, index, errors);
                    break;
                case ProcessFunctionOperationDefinition pf:
                    ValidateProcessFunctionOperation(pf, index, errors);
                    break;
                case StateOperationDefinition st:
                    ValidateStateOperation(st, index, errors);
                    break;
                case TimerOperationDefinition t:
                    ValidateTimerOperation(t, index, errors);
                    break;
                case RetryOperationDefinition r:
                    ValidateRetryOperation(r, index, errors);
                    break;
                case SideOutputOperationDefinition so:
                    ValidateSideOutputOperation(so, index, errors);
                    break;
                default:
                    // Unknown operation types are not validated (extensibility)
                    break;
            }
        }

        private static void ValidateFilterOperation(FilterOperationDefinition filter, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(filter.Expression))
            {
                errors.Add($"operations[{index}].filter.expression is required");
            }
        }

        private static void ValidateMapOperation(MapOperationDefinition map, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(map.Expression))
            {
                errors.Add($"operations[{index}].map.expression is required");
            }
        }

        private static void ValidateGroupByOperation(GroupByOperationDefinition groupBy, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(groupBy.Key) && (groupBy.Keys == null || groupBy.Keys.Count == 0))
            {
                errors.Add($"operations[{index}].groupBy.key or keys is required");
            }
        }

        private static void ValidateAggregateOperation(AggregateOperationDefinition aggregate, int index, List<string> errors)
        {
            var allowedAgg = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX", "COLLECT" };
            if (string.IsNullOrWhiteSpace(aggregate.AggregationType) || !allowedAgg.Contains(aggregate.AggregationType))
            {
                errors.Add($"operations[{index}].aggregate.aggregationType must be one of {string.Join(", ", allowedAgg)}");
            }

            if (string.IsNullOrWhiteSpace(aggregate.Field))
            {
                errors.Add($"operations[{index}].aggregate.field is required");
            }
        }

        private static void ValidateWindowOperation(WindowOperationDefinition window, int index, List<string> errors)
        {
            ValidateWindowType(window, index, errors);
            ValidateWindowSize(window, index, errors);
            ValidateWindowTimeUnit(window, index, errors);
            ValidateWindowSliding(window, index, errors);
        }

        private static void ValidateWindowType(WindowOperationDefinition window, int index, List<string> errors)
        {
            var allowedWindow = new[] { "TUMBLING", "SLIDING", "SESSION" };
            if (string.IsNullOrWhiteSpace(window.WindowType) || !allowedWindow.Contains(window.WindowType))
            {
                errors.Add($"operations[{index}].window.windowType must be one of {string.Join(", ", allowedWindow)}");
            }
        }

        private static void ValidateWindowSize(WindowOperationDefinition window, int index, List<string> errors)
        {
            if (window.Size <= 0)
            {
                errors.Add($"operations[{index}].window.size must be > 0");
            }
        }

        private static void ValidateWindowTimeUnit(WindowOperationDefinition window, int index, List<string> errors)
        {
            var allowedUnits = new[] { "SECONDS", "MINUTES", "HOURS" };
            if (string.IsNullOrWhiteSpace(window.TimeUnit) || !allowedUnits.Contains(window.TimeUnit))
            {
                errors.Add($"operations[{index}].window.timeUnit must be one of {string.Join(", ", allowedUnits)}");
            }
        }

        private static void ValidateWindowSliding(WindowOperationDefinition window, int index, List<string> errors)
        {
            if (string.Equals(window.WindowType, "SLIDING", StringComparison.OrdinalIgnoreCase) && (!window.Slide.HasValue || window.Slide.Value <= 0))
            {
                errors.Add($"operations[{index}].window.slide is required and must be > 0 for SLIDING windows");
            }
        }

        private static void ValidateJoinOperation(JoinOperationDefinition join, int index, List<string> errors)
        {
            if (join.RightSource == null)
            {
                errors.Add($"operations[{index}].join.rightSource is required");
            }

            if (string.IsNullOrWhiteSpace(join.LeftKey))
            {
                errors.Add($"operations[{index}].join.leftKey is required");
            }

            if (string.IsNullOrWhiteSpace(join.RightKey))
            {
                errors.Add($"operations[{index}].join.rightKey is required");
            }
        }

        private static void ValidateAsyncFunctionOperation(AsyncFunctionOperationDefinition asyncFunction, int index, List<string> errors)
        {
            ValidateAsyncFunctionType(asyncFunction, index, errors);
            ValidateAsyncFunctionTimeout(asyncFunction, index, errors);
            ValidateAsyncFunctionRetries(asyncFunction, index, errors);
        }

        private static void ValidateAsyncFunctionType(AsyncFunctionOperationDefinition asyncFunction, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(asyncFunction.FunctionType))
            {
                errors.Add($"operations[{index}].asyncFunction.functionType is required");
            }
        }

        private static void ValidateAsyncFunctionTimeout(AsyncFunctionOperationDefinition asyncFunction, int index, List<string> errors)
        {
            if (asyncFunction.TimeoutMs <= 0 || asyncFunction.TimeoutMs > 1_200_000)
            {
                errors.Add($"operations[{index}].asyncFunction.timeoutMs must be between 1 and 1200000");
            }
        }

        private static void ValidateAsyncFunctionRetries(AsyncFunctionOperationDefinition asyncFunction, int index, List<string> errors)
        {
            if (asyncFunction.MaxRetries < 0 || asyncFunction.MaxRetries > 100)
            {
                errors.Add($"operations[{index}].asyncFunction.maxRetries must be between 0 and 100");
            }
        }

        private static void ValidateProcessFunctionOperation(ProcessFunctionOperationDefinition processFunction, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(processFunction.ProcessType))
            {
                errors.Add($"operations[{index}].processFunction.processType is required");
            }
        }

        private static void ValidateStateOperation(StateOperationDefinition state, int index, List<string> errors)
        {
            var allowedState = new[] { "value", "list", "map", "reducing" };
            if (string.IsNullOrWhiteSpace(state.StateType) || !allowedState.Contains(state.StateType))
            {
                errors.Add($"operations[{index}].state.stateType must be one of {string.Join(", ", allowedState)}");
            }

            if (string.IsNullOrWhiteSpace(state.StateKey))
            {
                errors.Add($"operations[{index}].state.stateKey is required");
            }

            if (state.TtlMs.HasValue && state.TtlMs <= 0)
            {
                errors.Add($"operations[{index}].state.ttlMs must be > 0 when provided");
            }
        }

        private static void ValidateTimerOperation(TimerOperationDefinition timer, int index, List<string> errors)
        {
            var allowedTimers = new[] { "processing", "event" };
            if (string.IsNullOrWhiteSpace(timer.TimerType) || !allowedTimers.Contains(timer.TimerType))
            {
                errors.Add($"operations[{index}].timer.timerType must be one of {string.Join(", ", allowedTimers)}");
            }

            if (timer.DelayMs <= 0 || timer.DelayMs > 86_400_000)
            {
                errors.Add($"operations[{index}].timer.delayMs must be between 1 and 86400000");
            }
        }

        private static void ValidateRetryOperation(RetryOperationDefinition retry, int index, List<string> errors)
        {
            if (retry.MaxRetries < 0 || retry.MaxRetries > 100)
            {
                errors.Add($"operations[{index}].retry.maxRetries must be between 0 and 100");
            }

            if (retry.DelayMs == null || retry.DelayMs.Count == 0)
            {
                errors.Add($"operations[{index}].retry.delayMs must contain at least 1 value");
            }
            else if (retry.DelayMs.Exists(d => d <= 0))
            {
                errors.Add($"operations[{index}].retry.delayMs values must be > 0");
            }

            if (string.IsNullOrWhiteSpace(retry.StateKey))
            {
                errors.Add($"operations[{index}].retry.stateKey is required");
            }
        }

        private static void ValidateSideOutputOperation(SideOutputOperationDefinition sideOutput, int index, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sideOutput.OutputTag))
            {
                errors.Add($"operations[{index}].sideOutput.outputTag is required");
            }

            if (string.IsNullOrWhiteSpace(sideOutput.Condition))
            {
                errors.Add($"operations[{index}].sideOutput.condition is required");
            }

            if (sideOutput.SideOutputSink == null)
            {
                errors.Add($"operations[{index}].sideOutput.sideOutputSink is required");
            }
        }

        private static void ValidateSink(ISinkDefinition sink, List<string> errors)
        {
            switch (sink)
            {
                case KafkaSinkDefinition k:
                    ValidateKafkaSink(k, errors);
                    break;
                case FileSinkDefinition f:
                    ValidateFileSink(f, errors);
                    break;
                case HttpSinkDefinition h:
                    ValidateHttpSink(h, errors);
                    break;
                case DatabaseSinkDefinition d:
                    ValidateDatabaseSink(d, errors);
                    break;
                case RedisSinkDefinition r:
                    ValidateRedisSink(r, errors);
                    break;
                case ConsoleSinkDefinition c:
                    ValidateConsoleSink(c, errors);
                    break;
                default:
                    // Unknown sink types are not validated (extensibility)
                    break;
            }
        }

        private static void ValidateKafkaSink(KafkaSinkDefinition sink, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sink.Topic))
            {
                errors.Add("sink.kafka.topic is required");
            }

            if (sink.Serializer != null && sink.Serializer != "json" && sink.Serializer != "string")
            {
                errors.Add("sink.kafka.serializer must be 'json' or 'string' when provided");
            }
        }

        private static void ValidateFileSink(FileSinkDefinition sink, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sink.Path))
            {
                errors.Add("sink.file.path is required");
            }

            if (string.IsNullOrWhiteSpace(sink.Format))
            {
                errors.Add("sink.file.format is required");
            }
        }

        private static void ValidateHttpSink(HttpSinkDefinition sink, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sink.Url))
            {
                errors.Add("sink.http.url is required");
            }

            if (sink.TimeoutMs <= 0 || sink.TimeoutMs > 1_200_000)
            {
                errors.Add("sink.http.timeoutMs must be between 1 and 1200000");
            }
        }

        private static void ValidateDatabaseSink(DatabaseSinkDefinition sink, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sink.ConnectionString))
            {
                errors.Add("sink.database.connectionString is required");
            }

            if (string.IsNullOrWhiteSpace(sink.Table))
            {
                errors.Add("sink.database.table is required");
            }
        }

        private static void ValidateRedisSink(RedisSinkDefinition sink, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(sink.ConnectionString))
            {
                errors.Add("sink.redis.connectionString is required");
            }

            if (string.IsNullOrWhiteSpace(sink.OperationType))
            {
                errors.Add("sink.redis.operationType is required");
            }
        }

        private static void ValidateConsoleSink(ConsoleSinkDefinition sink, List<string> errors)
        {
            // ConsoleSink has optional format field, no required validation
            // Valid formats would be "json", "string", etc., but format is optional
        }
    }
}
