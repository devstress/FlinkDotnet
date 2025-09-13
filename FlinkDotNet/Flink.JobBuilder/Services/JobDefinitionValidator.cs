using System;
using System.Collections.Generic;
using System.Linq;
using Flink.JobBuilder.Models;

namespace Flink.JobBuilder.Services
{
    public sealed class IrValidationResult
    {
        public bool IsValid => Errors.Count == 0;
        public List<string> Errors { get; } = new();
    }

    public static class JobDefinitionValidator
    {
        public static IrValidationResult Validate(JobDefinition job)
        {
            var errors = new List<string>();

            if (job.Metadata == null)
            {
                errors.Add("metadata is required");
            }
            else
            {
                if (string.IsNullOrWhiteSpace(job.Metadata.JobId))
                    errors.Add("metadata.jobId is required");
                if (string.IsNullOrWhiteSpace(job.Metadata.Version))
                    errors.Add("metadata.version is required");
                if (job.Metadata.Parallelism.HasValue && job.Metadata.Parallelism <= 0)
                    errors.Add("metadata.parallelism must be >= 1 when provided");
            }

            if (job.Source == null)
                errors.Add("source is required");

            var isSqlJob = job.Source is SqlSourceDefinition;
            if (!isSqlJob && job.Sink == null)
                errors.Add("sink is required");

            if (job.Source != null)
                ValidateSource(job.Source, errors);

            if (job.Operations != null)
            {
                for (var i = 0; i < job.Operations.Count; i++)
                {
                    ValidateOperation(job.Operations[i], i, errors);
                }
            }

            if (job.Sink != null)
                ValidateSink(job.Sink, errors);

            var result = new IrValidationResult();
            result.Errors.AddRange(errors);
            return result;
        }

        private static void ValidateSource(ISourceDefinition source, List<string> errors)
        {
            switch (source)
            {
                case SqlSourceDefinition s:
                    if (s.Statements == null || s.Statements.Count == 0)
                        errors.Add("source.sql.statements must contain at least one statement");
                    break;
                case KafkaSourceDefinition k:
                    if (string.IsNullOrWhiteSpace(k.Topic))
                        errors.Add("source.kafka.topic is required");
                    break;
                case FileSourceDefinition f:
                    if (string.IsNullOrWhiteSpace(f.Path))
                        errors.Add("source.file.path is required");
                    if (string.IsNullOrWhiteSpace(f.Format))
                        errors.Add("source.file.format is required");
                    break;
                case HttpSourceDefinition h:
                    if (string.IsNullOrWhiteSpace(h.Url))
                        errors.Add("source.http.url is required");
                    if (h.IntervalSeconds <= 0)
                        errors.Add("source.http.intervalSeconds must be > 0");
                    break;
                case DatabaseSourceDefinition d:
                    if (string.IsNullOrWhiteSpace(d.ConnectionString))
                        errors.Add("source.database.connectionString is required");
                    if (string.IsNullOrWhiteSpace(d.Query))
                        errors.Add("source.database.query is required");
                    if (d.PollingIntervalSeconds <= 0)
                        errors.Add("source.database.pollingIntervalSeconds must be > 0");
                    break;
            }
        }

        private static void ValidateOperation(IOperationDefinition operation, int index, List<string> errors)
        {
            switch (operation)
            {
                case FilterOperationDefinition f:
                    if (string.IsNullOrWhiteSpace(f.Expression))
                        errors.Add($"operations[{index}].filter.expression is required");
                    break;
                case MapOperationDefinition m:
                    if (string.IsNullOrWhiteSpace(m.Expression))
                        errors.Add($"operations[{index}].map.expression is required");
                    break;
                case GroupByOperationDefinition g:
                    if (string.IsNullOrWhiteSpace(g.Key) && (g.Keys == null || g.Keys.Count == 0))
                        errors.Add($"operations[{index}].groupBy.key or keys is required");
                    break;
                case AggregateOperationDefinition a:
                    var allowedAgg = new[] { "SUM", "COUNT", "AVG", "MIN", "MAX" };
                    if (string.IsNullOrWhiteSpace(a.AggregationType) || !allowedAgg.Contains(a.AggregationType))
                        errors.Add($"operations[{index}].aggregate.aggregationType must be one of {string.Join(", ", allowedAgg)}");
                    if (string.IsNullOrWhiteSpace(a.Field))
                        errors.Add($"operations[{index}].aggregate.field is required");
                    break;
                case WindowOperationDefinition w:
                    var allowedUnits = new[] { "SECONDS", "MINUTES", "HOURS" };
                    var allowedWindow = new[] { "TUMBLING", "SLIDING", "SESSION" };
                    if (string.IsNullOrWhiteSpace(w.WindowType) || !allowedWindow.Contains(w.WindowType))
                        errors.Add($"operations[{index}].window.windowType must be one of {string.Join(", ", allowedWindow)}");
                    if (w.Size <= 0)
                        errors.Add($"operations[{index}].window.size must be > 0");
                    if (string.IsNullOrWhiteSpace(w.TimeUnit) || !allowedUnits.Contains(w.TimeUnit))
                        errors.Add($"operations[{index}].window.timeUnit must be one of {string.Join(", ", allowedUnits)}");
                    if (string.Equals(w.WindowType, "SLIDING", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!w.Slide.HasValue || w.Slide.Value <= 0)
                            errors.Add($"operations[{index}].window.slide is required and must be > 0 for SLIDING windows");
                    }
                    break;
                case JoinOperationDefinition j:
                    if (j.RightSource == null)
                        errors.Add($"operations[{index}].join.rightSource is required");
                    if (string.IsNullOrWhiteSpace(j.LeftKey))
                        errors.Add($"operations[{index}].join.leftKey is required");
                    if (string.IsNullOrWhiteSpace(j.RightKey))
                        errors.Add($"operations[{index}].join.rightKey is required");
                    break;
                case AsyncFunctionOperationDefinition af:
                    if (string.IsNullOrWhiteSpace(af.FunctionType))
                        errors.Add($"operations[{index}].asyncFunction.functionType is required");
                    if (af.TimeoutMs <= 0 || af.TimeoutMs > 1_200_000)
                        errors.Add($"operations[{index}].asyncFunction.timeoutMs must be between 1 and 1200000");
                    if (af.MaxRetries < 0 || af.MaxRetries > 100)
                        errors.Add($"operations[{index}].asyncFunction.maxRetries must be between 0 and 100");
                    break;
                case ProcessFunctionOperationDefinition pf:
                    if (string.IsNullOrWhiteSpace(pf.ProcessType))
                        errors.Add($"operations[{index}].processFunction.processType is required");
                    break;
                case StateOperationDefinition st:
                    var allowedState = new[] { "value", "list", "map", "reducing" };
                    if (string.IsNullOrWhiteSpace(st.StateType) || !allowedState.Contains(st.StateType))
                        errors.Add($"operations[{index}].state.stateType must be one of {string.Join(", ", allowedState)}");
                    if (string.IsNullOrWhiteSpace(st.StateKey))
                        errors.Add($"operations[{index}].state.stateKey is required");
                    if (st.TtlMs.HasValue && st.TtlMs <= 0)
                        errors.Add($"operations[{index}].state.ttlMs must be > 0 when provided");
                    break;
                case TimerOperationDefinition t:
                    var allowedTimers = new[] { "processing", "event" };
                    if (string.IsNullOrWhiteSpace(t.TimerType) || !allowedTimers.Contains(t.TimerType))
                        errors.Add($"operations[{index}].timer.timerType must be one of {string.Join(", ", allowedTimers)}");
                    if (t.DelayMs <= 0 || t.DelayMs > 86_400_000)
                        errors.Add($"operations[{index}].timer.delayMs must be between 1 and 86400000");
                    break;
                case RetryOperationDefinition r:
                    if (r.MaxRetries < 0 || r.MaxRetries > 100)
                        errors.Add($"operations[{index}].retry.maxRetries must be between 0 and 100");
                    if (r.DelayMs == null || r.DelayMs.Count == 0)
                        errors.Add($"operations[{index}].retry.delayMs must contain at least 1 value");
                    else if (r.DelayMs.Any(d => d <= 0))
                        errors.Add($"operations[{index}].retry.delayMs values must be > 0");
                    if (string.IsNullOrWhiteSpace(r.StateKey))
                        errors.Add($"operations[{index}].retry.stateKey is required");
                    break;
                case SideOutputOperationDefinition so:
                    if (string.IsNullOrWhiteSpace(so.OutputTag))
                        errors.Add($"operations[{index}].sideOutput.outputTag is required");
                    if (string.IsNullOrWhiteSpace(so.Condition))
                        errors.Add($"operations[{index}].sideOutput.condition is required");
                    if (so.SideOutputSink == null)
                        errors.Add($"operations[{index}].sideOutput.sideOutputSink is required");
                    break;
            }
        }

        private static void ValidateSink(ISinkDefinition sink, List<string> errors)
        {
            switch (sink)
            {
                case KafkaSinkDefinition k:
                    if (string.IsNullOrWhiteSpace(k.Topic))
                        errors.Add("sink.kafka.topic is required");
                    if (k.Serializer != null && k.Serializer != "json" && k.Serializer != "string")
                        errors.Add("sink.kafka.serializer must be 'json' or 'string' when provided");
                    break;
                case FileSinkDefinition f:
                    if (string.IsNullOrWhiteSpace(f.Path))
                        errors.Add("sink.file.path is required");
                    if (string.IsNullOrWhiteSpace(f.Format))
                        errors.Add("sink.file.format is required");
                    break;
                case HttpSinkDefinition h:
                    if (string.IsNullOrWhiteSpace(h.Url))
                        errors.Add("sink.http.url is required");
                    if (h.TimeoutMs <= 0 || h.TimeoutMs > 1_200_000)
                        errors.Add("sink.http.timeoutMs must be between 1 and 1200000");
                    break;
                case DatabaseSinkDefinition d:
                    if (string.IsNullOrWhiteSpace(d.ConnectionString))
                        errors.Add("sink.database.connectionString is required");
                    if (string.IsNullOrWhiteSpace(d.Table))
                        errors.Add("sink.database.table is required");
                    break;
                case RedisSinkDefinition r:
                    if (string.IsNullOrWhiteSpace(r.ConnectionString))
                        errors.Add("sink.redis.connectionString is required");
                    if (string.IsNullOrWhiteSpace(r.OperationType))
                        errors.Add("sink.redis.operationType is required");
                    break;
            }
        }
    }
}
