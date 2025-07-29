using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Flink.JobBuilder.Extensions;

namespace FlinkJobBuilder.Sample
{
    /// <summary>
    /// Sample application demonstrating the new FlinkJobBuilder API with real Flink 2.0 integration
    /// This shows how .NET developers can write streaming jobs that execute on Apache Flink 2.0
    /// using Kubernetes deployment patterns for production-ready applications
    /// </summary>
    public class Program
    {
        protected Program() { }
        
        public static async Task Main(string[] args)
        {
            var builder = Host.CreateApplicationBuilder(args);
            
            // Configure FlinkJobBuilder with Kubernetes service discovery
            builder.Services.AddFlinkJobBuilder(config =>
            {
                // Use Kubernetes service discovery to locate the Flink Job Gateway
                // This follows the K8s setup with proper service names
                var gatewayHost = builder.Configuration.GetValue<string>("FLINK_JOB_GATEWAY_HOST") ?? "flink-job-gateway";
                var gatewayPort = builder.Configuration.GetValue<int>("FLINK_JOB_GATEWAY_PORT", 8080);
                
                config.BaseUrl = $"http://{gatewayHost}:{gatewayPort}";
                config.HttpTimeout = TimeSpan.FromMinutes(5);
                config.MaxRetries = 3;
                config.UseHttps = false; // Use HTTP for internal K8s communication
            });
            
            var host = builder.Build();
            
            using var scope = host.Services.CreateScope();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            logger.LogInformation("=== Flink.NET Job Builder Sample with Real Flink 2.0 Integration ===");
            logger.LogInformation("Demonstrates Kubernetes deployment patterns and real job execution");
            
            try
            {
                // Example 1: Basic streaming job demonstrating K8s service integration
                await RunKubernetesStreamingExample(logger, scope.ServiceProvider);
                
                // Example 2: Complex streaming job with real Flink 2.0 execution
                await RunRealFlinkExecutionExample(logger, scope.ServiceProvider);
                
                // Example 3: Production-ready windowed processing with monitoring
                await RunProductionWindowedExample(logger, scope.ServiceProvider);
                
                logger.LogInformation("All examples completed successfully!");
                logger.LogInformation("Note: For full execution, ensure Flink 2.0 cluster is running as per K8s setup");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error running examples");
                Environment.ExitCode = 1;
            }
        }

        /// <summary>
        /// Example 1: Basic streaming job demonstrating Kubernetes service integration
        /// This shows how the sample follows K8s setup patterns with proper service discovery
        /// </summary>
        private static async Task RunKubernetesStreamingExample(ILogger logger, IServiceProvider serviceProvider)
        {
            logger.LogInformation("Running Kubernetes Integration Example...");
            logger.LogInformation("This example demonstrates K8s service discovery and real Flink 2.0 job submission");
            
            try
            {
                // Create job builder using DI (follows K8s patterns)
                var jobBuilder = serviceProvider.CreateJobBuilder();
                
                var job = Flink.JobBuilder.FlinkJobBuilder
                    .FromKafka("orders")  // Uses K8s Kafka service
                    .Where("Amount > 100")
                    .GroupBy("Region")
                    .Aggregate("SUM", "Amount")
                    .ToKafka("high-value-orders");

                // Show the generated JSON IR
                var json = job.ToJson();
                logger.LogInformation("Generated IR JSON for K8s deployment:");
                logger.LogInformation(json);

                // Validate the job definition
                var jobDefinition = job.BuildJobDefinition();
                var validation = jobDefinition.Validate();
                
                if (validation.IsValid)
                {
                    logger.LogInformation("Job validation passed - ready for K8s Flink 2.0 cluster");
                    
                    // Attempt real job submission to Flink 2.0 cluster via K8s service
                    logger.LogInformation("Attempting job submission to real Flink 2.0 cluster...");
                    
                    try
                    {
                        var submissionResult = await job.Submit("KubernetesStreamingExample");
                        
                        if (submissionResult.IsSuccess)
                        {
                            logger.LogInformation("✅ Job submitted successfully to Flink 2.0 cluster!");
                            logger.LogInformation("Job ID: {JobId}, Flink Job ID: {FlinkJobId}", 
                                submissionResult.JobId, submissionResult.FlinkJobId);
                        }
                        else
                        {
                            logger.LogWarning("Job submission failed: {ErrorMessage}", submissionResult.ErrorMessage);
                            logger.LogInformation("This is expected if Flink 2.0 cluster is not running");
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Job submission encountered limitation: {Message}", ex.Message);
                        logger.LogInformation("For full execution, deploy using: kubectl apply -f k8s/");
                    }
                }
                else
                {
                    logger.LogError("Job validation failed: {Errors}", string.Join(", ", validation.Errors));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run Kubernetes streaming example");
            }
        }

        /// <summary>
        /// Example 2: Complex aggregation with real Flink 2.0 execution attempt
        /// </summary>
        private static async Task RunRealFlinkExecutionExample(ILogger logger, IServiceProvider serviceProvider)
        {
            logger.LogInformation("Running Real Flink 2.0 Execution Example...");
            logger.LogInformation("This example attempts actual job execution on Flink 2.0 cluster");
            
            try
            {
                var job = Flink.JobBuilder.FlinkJobBuilder
                    .FromKafka("user-events")
                    .Where("EventType = 'click'")
                    .Map("ExtractUserId(UserId)")
                    .GroupBy("UserId")
                    .Window("TUMBLING", 5, "MINUTES")
                    .Aggregate("COUNT", "*")
                    .ToKafka("user-click-counts");

                var json = job.ToJson();
                logger.LogInformation("Real Flink 2.0 Execution Example IR:");
                logger.LogInformation(json);

                var validation = job.BuildJobDefinition().Validate();
                logger.LogInformation("Validation result: {IsValid}", validation.IsValid);
                
                if (validation.IsValid)
                {
                    logger.LogInformation("Attempting real execution on Flink 2.0 cluster...");
                    
                    try
                    {
                        var submissionResult = await job.Submit("RealFlinkExecutionExample");
                        
                        if (submissionResult.IsSuccess)
                        {
                            logger.LogInformation("✅ Job executing on real Flink 2.0 cluster!");
                            logger.LogInformation("Monitor job at: http://flink-jobmanager-ui:8081");
                            logger.LogInformation("Flink Job ID: {FlinkJobId}", submissionResult.FlinkJobId);
                            
                            // In a real scenario, you could monitor the job
                            logger.LogInformation("Job submitted for real-time processing...");
                        }
                        else
                        {
                            logger.LogWarning("Real execution failed: {ErrorMessage}", submissionResult.ErrorMessage);
                            ShowKubernetesSetupInstructions(logger);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Real execution encountered infrastructure limitation: {Message}", ex.Message);
                        ShowKubernetesSetupInstructions(logger);
                    }
                }
                else
                {
                    logger.LogWarning("Validation errors: {Errors}", string.Join(", ", validation.Errors));
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run real Flink execution example");
            }
        }

        /// <summary>
        /// Example 3: Production-ready windowed processing with monitoring capabilities
        /// </summary>
        private static async Task RunProductionWindowedExample(ILogger logger, IServiceProvider serviceProvider)
        {
            logger.LogInformation("Running Production Windowed Example...");
            logger.LogInformation("This example demonstrates production-ready patterns for K8s deployment");
            
            try
            {
                var job = Flink.JobBuilder.FlinkJobBuilder
                    .FromKafka("sensor-data")
                    .Where("Temperature > 25.0")
                    .GroupBy("SensorId")
                    .Window("SLIDING", 10, "MINUTES")
                    .Aggregate("AVG", "Temperature")
                    .ToKafka("temperature-alerts"); // Production output

                var json = job.ToJson();
                logger.LogInformation("Production Windowed Example IR:");
                logger.LogInformation(json);

                var validation = job.BuildJobDefinition().Validate();
                logger.LogInformation("Production validation result: {IsValid}", validation.IsValid);
                
                if (validation.IsValid)
                {
                    logger.LogInformation("Attempting production job submission to Flink 2.0...");
                    
                    try
                    {
                        var submissionResult = await job.Submit("ProductionSensorMonitoring");
                        
                        if (submissionResult.IsSuccess)
                        {
                            logger.LogInformation("✅ Production job running on Flink 2.0 cluster!");
                            logger.LogInformation("Production Job ID: {FlinkJobId}", submissionResult.FlinkJobId);
                            logger.LogInformation("Monitor at: http://flink-jobmanager-ui:8081/#/job/{FlinkJobId}/overview", 
                                submissionResult.FlinkJobId);
                            
                            // Show production monitoring capabilities
                            logger.LogInformation("Production monitoring endpoints:");
                            logger.LogInformation("- Flink UI: http://flink-jobmanager-ui:8081");
                            logger.LogInformation("- Job Gateway API: http://flink-job-gateway:8080/swagger-ui.html");
                            logger.LogInformation("- Kafka UI: Available through K8s ingress");
                        }
                        else
                        {
                            logger.LogWarning("Production job submission failed: {ErrorMessage}", submissionResult.ErrorMessage);
                            ShowProductionDeploymentGuide(logger);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning("Production deployment limitation: {Message}", ex.Message);
                        ShowProductionDeploymentGuide(logger);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to run production windowed example");
            }
        }

        private static void ShowKubernetesSetupInstructions(ILogger logger)
        {
            logger.LogInformation("=== Kubernetes Setup Instructions ===");
            logger.LogInformation("To run jobs on real Flink 2.0 cluster:");
            logger.LogInformation("1. Deploy Flink cluster: kubectl apply -f k8s/");
            logger.LogInformation("2. Verify deployment: kubectl get pods -n flink-system");
            logger.LogInformation("3. Check services: kubectl get svc -n flink-system");
            logger.LogInformation("4. Access Flink UI: kubectl port-forward svc/flink-jobmanager-ui 8081:8081 -n flink-system");
            logger.LogInformation("5. Access Job Gateway: kubectl port-forward svc/flink-job-gateway 8080:8080 -n flink-system");
        }

        private static void ShowProductionDeploymentGuide(ILogger logger)
        {
            logger.LogInformation("=== Production Deployment Guide ===");
            logger.LogInformation("For production Flink 2.0 deployment:");
            logger.LogInformation("1. Build images: docker build -t flink-job-gateway:prod .");
            logger.LogInformation("2. Push to registry: docker push your-registry/flink-job-gateway:prod");
            logger.LogInformation("3. Update K8s manifests with production settings");
            logger.LogInformation("4. Deploy: kubectl apply -f k8s/");
            logger.LogInformation("5. Configure monitoring, logging, and alerting");
            logger.LogInformation("6. Set up resource limits and autoscaling");
            logger.LogInformation("7. Configure persistent storage for checkpoints");
        }
    }
}