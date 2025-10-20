# Getting Started with FlinkDotnet

FlinkDotnet enables .NET developers to build and submit streaming jobs to Apache Flink clusters using a fluent C# API.

## Prerequisites

- **.NET 9.0 SDK** - Download from [here](https://dotnet.microsoft.com/download)
- **.NET Aspire Workload** - Installation requirements vary by platform:
  
  **Windows/macOS**: Aspire tooling is included with .NET SDK (.NET 8+)
  ```bash
  # If not working, try installing:
  dotnet workload install aspire
  ```
  
  **Linux**: Aspire tooling is NOT bundled with .NET SDK and must be installed manually:
  ```bash
  dotnet workload install aspire  # Required on Linux
  ```

- **Docker Desktop or Podman** - For running Apache Flink infrastructure
- **Apache Flink Cluster** - Kubernetes deployment or local installation
- **Java 17 and Maven** - Required to build `FlinkDotNet.JobGateway` which prebuilds the IR Runner jar used for submissions

## Quick Start

### 1. Clone and Build FlinkDotNet Project (prebuilds `flink-ir-runner.jar`)

```bash
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Create your application project
dotnet new console -n MyFlinkJobApp
cd MyFlinkJobApp

# Reference the FlinkDotNet projects locally
dotnet add reference ../FlinkDotNet/FlinkDotNet/FlinkDotNet.csproj

# Build the Gateway to prebuild and bundle the IR Runner jar
dotnet build ../FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj -c Release
  # disable with /p:BuildFlinkRunner=false if you provide a prebuilt jar
```

### 2. Write a Streaming Job

```csharp
using FlinkDotNet.DataStream;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Get Flink execution environment
        var env = Flink.GetExecutionEnvironment();

        // Create streaming job using DataStream API
        var orders = env.FromKafka("orders", "kafka:9093", "order-consumer-group");
        
        var highValueOrders = orders
            .Filter(order => order.Amount > 100)
            .KeyBy(order => order.Region)
            .Map(order => new { order.Region, order.Amount });

        highValueOrders.SinkToKafka("high-value-orders", "kafka:9093");

        // Execute the job
        await env.ExecuteAsync("OrderProcessingJob");
        Console.WriteLine("Job submitted successfully");
    }
}
```

### 3. Deploy Infrastructure

**Kubernetes (Recommended):**
```bash
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet
kubectl apply -f k8s/
```

**Local Development:**
```bash
cd Sample/FlinkDotNetAspire.AppHost.AppHost
dotnet run
```

### 4. Run Your Application

```bash
dotnet run
```

Your job will be submitted to the Apache Flink cluster and start processing data streams.

## Configuration

Example `appsettings.json`:

```json
{
  "Flink": {
    "JobManagerRestAddress": "http://localhost:18002",
    "KafkaConfig": {
      "BootstrapServers": "localhost:9092",
      "GroupId": "flink-dotnet-consumer-group"
    }
  }
}
```

## Monitoring

- **Flink Web UI**: `http://localhost:18002`
- **Job Gateway API**: `http://localhost:18000`

## Testing

Run LocalTesting observability tests:

```bash
cd LocalTesting
dotnet test LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj --filter "Category=observability"
```

## Next Steps

- Explore [Usage Examples](Usage-Examples.md) for detailed patterns
- Learn about [Gateway API](Gateway-API.md) for advanced integration
- See the [LearningCourse](../../LearningCourse/README.md) for comprehensive training
