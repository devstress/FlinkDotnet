using System;
using System.Collections.Generic;
using System.Linq;
using Flink.JobBuilder.Models;
using Flink.JobBuilder.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Flink.JobBuilder.Extensions
{
    /// <summary>
    /// Extension methods for dependency injection
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Add Flink JobBuilder services to the DI container
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configuration">Gateway configuration (optional)</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFlinkJobBuilder(this IServiceCollection services, FlinkJobGatewayConfiguration? configuration = null)
        {
            if (configuration != null)
            {
                services.AddSingleton(configuration);
            }
            else
            {
                services.AddSingleton<FlinkJobGatewayConfiguration>();
            }

            services.AddHttpClient<IFlinkJobGatewayService, FlinkJobGatewayService>();
            services.AddTransient<FlinkJobBuilder>();

            return services;
        }

        /// <summary>
        /// Add Flink JobBuilder services with configuration action
        /// </summary>
        /// <param name="services">Service collection</param>
        /// <param name="configureOptions">Configuration action</param>
        /// <returns>Service collection for chaining</returns>
        public static IServiceCollection AddFlinkJobBuilder(this IServiceCollection services, Action<FlinkJobGatewayConfiguration> configureOptions)
        {
            var configuration = new FlinkJobGatewayConfiguration();
            configureOptions(configuration);
            return services.AddFlinkJobBuilder(configuration);
        }
    }

    /// <summary>
    /// Extension methods for job builder
    /// </summary>
    public static class FlinkJobBuilderExtensions
    {
        /// <summary>
        /// Create a new FlinkJobBuilder instance
        /// </summary>
        /// <param name="serviceProvider">Service provider</param>
        /// <returns>FlinkJobBuilder instance</returns>
        public static FlinkJobBuilder CreateJobBuilder(this IServiceProvider serviceProvider)
        {
            var gatewayService = serviceProvider.GetRequiredService<IFlinkJobGatewayService>();
            var logger = serviceProvider.GetService<ILogger<FlinkJobBuilder>>();
            return new FlinkJobBuilder(gatewayService, logger);
        }
    }

    /// <summary>
    /// Extension methods for job definition validation
    /// </summary>
    public static class JobDefinitionExtensions
    {
        /// <summary>
        /// Validate the job definition
        /// </summary>
        /// <param name="jobDefinition">Job definition to validate</param>
        /// <returns>Validation result</returns>
        public static JobValidationResult Validate(this JobDefinition jobDefinition)
        {
            var result = new JobValidationResult();

            // Validate source
            if (jobDefinition.Source == null)
            {
                result.Errors.Add("Job must have a source");
            }
            else
            {
                result.Errors.AddRange(ValidateSource(jobDefinition.Source));
            }

            // Validate sink
            if (jobDefinition.Sink == null)
            {
                result.Errors.Add("Job must have a sink");
            }
            else
            {
                result.Errors.AddRange(ValidateSink(jobDefinition.Sink));
            }

            // Validate operations
            result.Errors.AddRange(ValidateOperations(jobDefinition.Operations));

            // Validate metadata
            if (string.IsNullOrEmpty(jobDefinition.Metadata.JobId))
            {
                result.Errors.Add("Job must have a valid JobId");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        private static List<string> ValidateSource(ISourceDefinition source)
        {
            var errors = new List<string>();

            switch (source)
            {
                case KafkaSourceDefinition kafkaSource:
                    if (string.IsNullOrEmpty(kafkaSource.Topic))
                        errors.Add("Kafka source must specify a topic");
                    break;
                case FileSourceDefinition fileSource:
                    if (string.IsNullOrEmpty(fileSource.Path))
                        errors.Add("File source must specify a path");
                    break;
            }

            return errors;
        }

        private static List<string> ValidateSink(ISinkDefinition sink)
        {
            var errors = new List<string>();

            switch (sink)
            {
                case KafkaSinkDefinition kafkaSink:
                    if (string.IsNullOrEmpty(kafkaSink.Topic))
                        errors.Add("Kafka sink must specify a topic");
                    break;
                case FileSinkDefinition fileSink:
                    if (string.IsNullOrEmpty(fileSink.Path))
                        errors.Add("File sink must specify a path");
                    break;
                case DatabaseSinkDefinition dbSink:
                    if (string.IsNullOrEmpty(dbSink.ConnectionString))
                        errors.Add("Database sink must specify a connection string");
                    if (string.IsNullOrEmpty(dbSink.Table))
                        errors.Add("Database sink must specify a table");
                    break;
            }

            return errors;
        }

        private static List<string> ValidateOperations(List<IOperationDefinition> operations)
        {
            var errors = new List<string>();

            foreach (var operation in operations)
            {
                errors.AddRange(ValidateOperation(operation));
            }

            return errors;
        }

        private static IEnumerable<string> ValidateOperation(IOperationDefinition operation)
        {
            return operation switch
            {
                FilterOperationDefinition filterOp => ValidateFilterOperation(filterOp),
                MapOperationDefinition mapOp => ValidateMapOperation(mapOp),
                GroupByOperationDefinition groupByOp => ValidateGroupByOperation(groupByOp),
                AggregateOperationDefinition aggOp => ValidateAggregateOperation(aggOp),
                WindowOperationDefinition windowOp => ValidateWindowOperation(windowOp),
                _ => Enumerable.Empty<string>()
            };
        }

        private static IEnumerable<string> ValidateFilterOperation(FilterOperationDefinition filterOp)
        {
            if (string.IsNullOrEmpty(filterOp.Expression))
                yield return "Filter operation must have an expression";
        }

        private static IEnumerable<string> ValidateMapOperation(MapOperationDefinition mapOp)
        {
            if (string.IsNullOrEmpty(mapOp.Expression))
                yield return "Map operation must have an expression";
        }

        private static IEnumerable<string> ValidateGroupByOperation(GroupByOperationDefinition groupByOp)
        {
            if (string.IsNullOrEmpty(groupByOp.Key) && (groupByOp.Keys == null || groupByOp.Keys.Count == 0))
                yield return "GroupBy operation must specify at least one key";
        }

        private static IEnumerable<string> ValidateAggregateOperation(AggregateOperationDefinition aggOp)
        {
            if (string.IsNullOrEmpty(aggOp.AggregationType))
                yield return "Aggregate operation must specify aggregation type";
            if (string.IsNullOrEmpty(aggOp.Field))
                yield return "Aggregate operation must specify field";
        }

        private static IEnumerable<string> ValidateWindowOperation(WindowOperationDefinition windowOp)
        {
            if (string.IsNullOrEmpty(windowOp.WindowType))
                yield return "Window operation must specify window type";
            if (windowOp.Size <= 0)
                yield return "Window operation must have a positive size";
        }
    }

    /// <summary>
    /// Job validation result
    /// </summary>
    public class JobValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}