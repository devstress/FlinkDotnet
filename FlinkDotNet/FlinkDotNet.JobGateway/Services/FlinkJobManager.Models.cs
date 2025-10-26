using System.Text.Json.Serialization;
using Flink.JobBuilder.Models;

namespace FlinkDotNet.JobGateway.Services;

/// <summary>
/// Manages Apache Flink job lifecycle including submission, status monitoring, and cancellation.
/// Note: This gateway intentionally converts exceptions into domain objects with selective rethrowing.
/// </summary>
public partial class FlinkJobManager
{
    private sealed class JobInfo
    {
        public string JobId { get; set; } = string.Empty;
        public string FlinkJobId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmissionTime { get; set; }
        public JobDefinition JobDefinition { get; set; } = null!;
    }

    private sealed class JobValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    private sealed class FlinkRunResponse
    {
        public string JobId { get; set; } = string.Empty;
    }

    private sealed class FlinkJarsList
    {
        public List<FlinkJarFile> Files { get; set; } = new();
    }

    private sealed class FlinkJarUploadResponse
    {
        [JsonPropertyName("filename")]
        public string? Filename { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }
    }

    private sealed class FlinkJarFile
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("uploaded")]
        public long Uploaded { get; set; }
    }

    private sealed class FlinkMetricEntry
    {
        public string Id { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
    }

    private sealed class JobMetricsBuilder(string flinkJobId)
    {
        private readonly string _flinkJobId = flinkJobId;
        private long _recordsIn;
        private long _recordsOut;
        private int _parallelism;
        private int _checkpoints;
        private DateTime? _lastCheckpoint;
        private string _backpressureLevel = "UNKNOWN";

        public void AddRecordsIn(long value) => this._recordsIn += value;
        public void AddRecordsOut(long value) => this._recordsOut += value;
        public void UpdateMaxParallelism(int value) => this._parallelism = Math.Max(this._parallelism, value);
        public void SetCheckpoints(int value) => this._checkpoints = value;
        public void SetLastCheckpoint(DateTime value) => this._lastCheckpoint = value;
        public void UpdateWorstBackpressure(string level) => this._backpressureLevel = WorstBackpressure(this._backpressureLevel, level);

        private static string WorstBackpressure(string current, string candidate)
        {
            static int Rank(string s) => s?.ToUpperInvariant() switch
            {
                "HIGH" => 3,
                "LOW" => 2,
                "OK" => 1,
                _ => 0
            };

            return Rank(candidate) > Rank(current) ? candidate : current;
        }

        public JobMetrics Build() => new()
        {
            FlinkJobId = this._flinkJobId,
            RecordsIn = this._recordsIn,
            RecordsOut = this._recordsOut,
            Parallelism = this._parallelism,
            Checkpoints = this._checkpoints,
            LastCheckpoint = this._lastCheckpoint,
            CustomMetrics = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["backpressureLevel"] = this._backpressureLevel
            }
        };
    }
}
