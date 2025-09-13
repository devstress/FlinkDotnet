using Flink.JobBuilder;

namespace FlinkDotNet.Pipelines
{
    /// <summary>
    /// Convenience helpers for common FlinkDotNet pipelines
    /// </summary>
    public static class FlinkDotNet
    {
        /// <summary>
        /// Kafka → Kafka pass-through with optional simple map expression
        /// </summary>
        public static FlinkJobBuilder KafkaToKafka(string inputTopic, string outputTopic, string? bootstrapServers = null, string mapExpression = "identity")
        {
            return FlinkJobBuilder
                .FromKafka(inputTopic, bootstrapServers)
                .Map(mapExpression)
                .ToKafka(outputTopic, bootstrapServers);
        }

        /// <summary>
        /// Kafka → Console for debugging
        /// </summary>
        public static FlinkJobBuilder KafkaToConsole(string inputTopic, string? bootstrapServers = null, string mapExpression = "identity")
        {
            return FlinkJobBuilder
                .FromKafka(inputTopic, bootstrapServers)
                .Map(mapExpression)
                .ToConsole();
        }

        /// <summary>
        /// Flink SQL job from embedded statements. Define sources, sinks and INSERT statements in SQL.
        /// </summary>
        public static FlinkJobBuilder Sql(params string[] statements)
        {
            return FlinkJobBuilder.FromSql(statements ?? new string[0]);
        }
    }
}
