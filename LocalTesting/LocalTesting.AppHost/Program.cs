// Basic environment setup
Environment.SetEnvironmentVariable("ASPIRE_ALLOW_UNSECURED_TRANSPORT", "true");

var builder = DistributedApplication.CreateBuilder(args);

// Kafka (Aspire-provided resource, exposes connection string)
builder.AddKafka("kafka");

// Flink Job Gateway (standalone mode - works without Flink cluster)
// This allows the tests to validate IR generation and basic functionality
// even when full Flink cluster orchestration isn't available in CI environments
builder.AddProject("flink-job-gateway", "../../FlinkDotNet/Flink.JobGateway/Flink.JobGateway.csproj")
    .WithEnvironment("ASPNETCORE_URLS", "http://0.0.0.0:8080")
    .WithEnvironment("FLINK_CLUSTER_HOST", "localhost") 
    .WithEnvironment("FLINK_CLUSTER_PORT", "8081");

// Note: Flink containers are temporarily disabled due to Aspire orchestration limitations
// The comprehensive test suite validates:
// 1. IR generation and validation (always works)
// 2. Full Flink integration (when cluster is available)
// 3. Proper error handling and fallback scenarios
// 
// For local development with full Flink cluster, use:
// docker-compose or standalone Docker containers outside of Aspire

await builder.Build().RunAsync();
