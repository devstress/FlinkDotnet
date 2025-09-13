# FlinkDotNet Quick Start Guide

This guide gets you running FlinkDotNet locally in 5 minutes.

## Prerequisites

Ensure you have these installed:

- **.NET 9.0 SDK** - [Download here](https://dotnet.microsoft.com/download/dotnet/9.0)
- **Docker Desktop** - [Download here](https://www.docker.com/products/docker-desktop)  
- **Java 17+** - Required for building IR Runner JAR

### Verify Prerequisites

```bash
# Check .NET version (should be 9.0.x)
dotnet --version

# Check Docker is running
docker ps

# Check Java version (should be 17+)
java -version
```

## Step 1: Clone and Setup

```bash
# Clone the repository
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Install .NET dependencies
dotnet restore

# Install Aspire workload
dotnet workload install aspire

# Verify everything builds
./scripts/validate-build-and-tests.ps1
```

## Step 2: Start Local Environment

FlinkDotNet uses Aspire to orchestrate a complete local development environment with Kafka, Flink, and the Job Gateway.

```bash
# Navigate to LocalTesting
cd LocalTesting

# Start the complete environment
dotnet run --project BackPressure.AppHost
```

This starts:
- **Kafka** on `localhost:9092`
- **Flink JobManager** on `localhost:8081` 
- **Flink TaskManager** on `localhost:8082`
- **Job Gateway API** on `localhost:8080`
- **Aspire Dashboard** on `https://localhost:17109`

### Verify Services Are Running

Open these URLs in your browser:
- [Aspire Dashboard](https://localhost:17109) - Overall orchestration view
- [Flink Web UI](http://localhost:8081) - Flink cluster management
- [Job Gateway Health](http://localhost:8080/api/v1/health) - Should return "OK"

## Step 3: Build IR Runner JAR

The IR Runner JAR converts FlinkDotNet IR to actual Flink DataStream jobs.

```bash
# Navigate to IR Runner
cd FlinkDotNet/Flink.IRRunner

# Build the JAR
./gradlew build

# Test the JAR
java -jar build/libs/flink-ir-runner-1.0.0.jar --help
```

## Step 4: Run Your First Job

Create a simple console application:

```bash
# Create new console app
dotnet new console -n MyFlinkApp
cd MyFlinkApp

# Add FlinkDotNet package
dotnet add reference ../../FlinkDotNet/Flink.JobBuilder/Flink.JobBuilder.csproj
```

Replace `Program.cs` with:

```csharp
using Flink.JobBuilder;
using Flink.JobBuilder.Services;

Console.WriteLine("FlinkDotNet Quick Start");

// Create a simple Kafka-to-Console pipeline
var job = FlinkJobBuilder
    .FromKafka("quickstart-input", "localhost:9092")
    .Filter("data.length > 5")
    .Map("data.ToUpper()")
    .WithTimer(5000) // 5 second processing window
    .ToConsole();

// Configure gateway connection
var gateway = new FlinkJobGatewayService(new()
{
    BaseUrl = "http://localhost:8080"
});

// Submit the job
Console.WriteLine("Submitting job to Flink...");
var result = await gateway.SubmitJobAsync(job.BuildJobDefinition());

if (result.Success)
{
    Console.WriteLine($"✅ Job submitted successfully!");
    Console.WriteLine($"   Job ID: {result.JobId}");
    Console.WriteLine($"   Flink Job ID: {result.FlinkJobId}");
    
    // Monitor the job
    Console.WriteLine("\\nJob Status:");
    var status = await gateway.GetJobStatusAsync(result.FlinkJobId);
    Console.WriteLine($"   State: {status.State}");
    
    var metrics = await gateway.GetJobMetricsAsync(result.FlinkJobId);
    Console.WriteLine($"   Records In: {metrics.RecordsIn}");
    Console.WriteLine($"   Records Out: {metrics.RecordsOut}");
}
else
{
    Console.WriteLine($"❌ Job submission failed: {result.ErrorMessage}");
}
```

Run the application:

```bash
dotnet run
```

## Step 5: Send Test Data

In another terminal, send some test data to Kafka:

```bash
# Navigate back to FlinkDotnet root
cd ../../

# Create test topic and send data
docker exec -it $(docker ps --filter "name=kafka" --format "{{.ID}}") kafka-console-producer.sh \
  --bootstrap-server localhost:9092 \
  --topic quickstart-input

# Type some messages (press Enter after each):
# hello world
# test message
# short
# another longer message
# exit with Ctrl+C
```

You should see the filtered and transformed messages appear in the Flink Web UI and job logs.

## Step 6: Run Integration Tests

Verify everything works with the comprehensive integration test suite:

```bash
# Run LocalTesting integration tests
cd LocalTesting
dotnet test LocalTesting.IntegrationTests --filter Category=observability -v normal
```

This test:
1. ✅ Starts Kafka, Flink, and Gateway
2. ✅ Creates input/output topics
3. ✅ Submits a FlinkDotNet job
4. ✅ Produces test messages
5. ✅ Consumes output messages  
6. ✅ Validates job metrics

## Step 7: Explore More Features

### Advanced Pipeline Example

```csharp
var advancedJob = FlinkJobBuilder
    .FromKafka("events", "localhost:9092")
    
    // Data validation and transformation
    .Filter("event.timestamp > 0")
    .Map("event.normalize()")
    
    // Async enrichment
    .AsyncHttp("https://api.service.com/enrich", 
               method: "POST", 
               timeoutMs: 3000,
               bodyTemplate: "{\"id\": \"${event.id}\"}")
    
    // State management for sessionization
    .WithState("user-sessions", "map", ttlMs: 1800000) // 30 min TTL
    
    // Windowing and aggregation
    .GroupBy("event.userId")
    .Window("TUMBLING", 60, "SECONDS")
    .Aggregate("COUNT", "events")
    
    // Error handling
    .WithRetry(maxRetries: 3, 
               delayPattern: new List<long> { 1000, 5000, 15000 },
               deadLetterTopic: "failed-events")
    
    // Multiple outputs
    .WithSideOutput("audit", "event.type == 'sensitive'")
    .ToKafka("processed-events", "localhost:9092");
```

### Check Job Status

```csharp
// Get detailed job information
var status = await gateway.GetJobStatusAsync(flinkJobId);
Console.WriteLine($"Job State: {status.State}");
Console.WriteLine($"Runtime: {status.Duration}");

// Get comprehensive metrics
var metrics = await gateway.GetJobMetricsAsync(flinkJobId);
Console.WriteLine($"Throughput: {metrics.RecordsIn}/{metrics.RecordsOut}");
Console.WriteLine($"Parallelism: {metrics.Parallelism}");
Console.WriteLine($"Checkpoints: {metrics.Checkpoints}");
Console.WriteLine($"Last Checkpoint: {metrics.LastCheckpoint}");
```

## Troubleshooting

### Common Issues

1. **Port already in use**
   ```bash
   # Check what's using port 8080 or 9092
   netstat -tulpn | grep :8080
   ```

2. **Docker not running**
   ```bash
   # Start Docker Desktop or Docker daemon
   sudo systemctl start docker  # Linux
   ```

3. **.NET 9.0 not found**
   ```bash
   # Verify global.json specifies correct version
   cat global.json
   ```

4. **Gradle build fails**
   ```bash
   # Clean and rebuild IR Runner
   cd FlinkDotNet/Flink.IRRunner
   ./gradlew clean build
   ```

### Getting Help

- **View Logs**: Check Aspire Dashboard at https://localhost:17109
- **Flink UI**: Check job details at http://localhost:8081  
- **Gateway API**: Test endpoints at http://localhost:8080/swagger
- **Integration Tests**: Run with `-v normal` for detailed output

## Next Steps

- 📖 **[DSL Guide](dsl-guide.md)** - Learn all available operations and patterns
- 🔧 **[Gateway API](gateway-api.md)** - REST API reference for job management  
- 🚀 **[Deployment](deployment.md)** - Deploy to production Kubernetes/Cloud
- 🐛 **[Troubleshooting](troubleshooting.md)** - Solve common issues

You're now ready to build production streaming applications with FlinkDotNet! 🎉