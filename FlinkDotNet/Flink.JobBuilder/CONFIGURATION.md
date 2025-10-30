# Flink Job Gateway Configuration

The Flink Job Gateway can be configured using either environment variables or appsettings.json (or both). The configuration follows a priority order to provide flexibility across different deployment scenarios.

## Configuration Priority

The `FlinkJobGatewayConfiguration.BaseUrl` is resolved in the following order:

1. **Explicitly set value** - Programmatically set in code
2. **Environment variable** - `FLINK_JOB_GATEWAY_URL`
3. **Appsettings.json** - `FlinkJobGateway:BaseUrl` (when using dependency injection)
4. **Exception** - Throws if none of the above are configured

## Configuration Methods

### Method 1: Environment Variable

Set the `FLINK_JOB_GATEWAY_URL` environment variable:

```bash
# Linux/macOS
export FLINK_JOB_GATEWAY_URL="http://localhost:8086/"

# Windows
set FLINK_JOB_GATEWAY_URL=http://localhost:8086/

# PowerShell
$env:FLINK_JOB_GATEWAY_URL="http://localhost:8086/"
```

### Method 2: Appsettings.json

Add configuration to your `appsettings.json`:

```json
{
  "FlinkJobGateway": {
    "BaseUrl": "http://localhost:8086/",
    "HttpTimeout": "00:05:00",
    "MaxRetries": 3,
    "RetryDelay": "00:00:01",
    "UseHttps": false
  }
}
```

### Method 3: Programmatic Configuration

```csharp
var config = new FlinkJobGatewayConfiguration
{
    BaseUrl = "http://localhost:8086/",
    HttpTimeout = TimeSpan.FromMinutes(5),
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromSeconds(1)
};

var service = new FlinkJobGatewayService(config);
```

## ASP.NET Core Integration

For ASP.NET Core applications with dependency injection:

### 1. Add to Program.cs or Startup.cs

```csharp
using Flink.JobBuilder.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add Flink Job Gateway services
builder.Services.AddFlinkJobGateway(builder.Configuration);

var app = builder.Build();
```

### 2. Configure appsettings.json

```json
{
  "FlinkJobGateway": {
    "BaseUrl": "http://localhost:8086/",
    "HttpTimeout": "00:05:00",
    "MaxRetries": 3,
    "RetryDelay": "00:00:01"
  }
}
```

### 3. Inject in Controllers or Services

```csharp
public class MyController : ControllerBase
{
    private readonly IFlinkJobGatewayService _gatewayService;
    
    public MyController(IFlinkJobGatewayService gatewayService)
    {
        _gatewayService = gatewayService;
    }
}
```

## Environment-Specific Configuration

Use environment-specific appsettings files:

- `appsettings.Development.json` - Development settings
- `appsettings.Production.json` - Production settings
- `appsettings.Staging.json` - Staging settings

Example `appsettings.Production.json`:

```json
{
  "FlinkJobGateway": {
    "BaseUrl": "https://flink-gateway.production.example.com/",
    "UseHttps": true,
    "MaxRetries": 5
  }
}
```

## Configuration for Tests

Unit tests can use environment variables for simplicity:

```csharp
[SetUp]
public void SetUp()
{
    Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", "http://localhost:8086/");
}

[TearDown]
public void TearDown()
{
    Environment.SetEnvironmentVariable("FLINK_JOB_GATEWAY_URL", null);
}
```

## Docker / Container Environments

When running in containers, use environment variables in your Dockerfile or docker-compose.yml:

### Dockerfile

```dockerfile
ENV FLINK_JOB_GATEWAY_URL=http://flink-gateway:8086/
```

### docker-compose.yml

```yaml
services:
  myapp:
    image: myapp:latest
    environment:
      - FLINK_JOB_GATEWAY_URL=http://flink-gateway:8086/
```

## Aspire Integration

When using .NET Aspire, the gateway URL is automatically discovered and set via environment variable:

```csharp
// In Aspire AppHost Program.cs
var gateway = builder.AddProject<Projects.FlinkDotNet_JobGateway>("flink-job-gateway")
    .WithHttpEndpoint(port: 8086, name: "gateway-http");

// Tests automatically discover and set FLINK_JOB_GATEWAY_URL
```

## Configuration Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| BaseUrl | string | Required | Base URL for the Flink Job Gateway |
| ApiKey | string? | null | Optional API key for authentication |
| HttpTimeout | TimeSpan | 00:05:00 | HTTP request timeout |
| UseHttps | bool | false | Whether to use HTTPS |
| MaxRetries | int | 3 | Maximum number of retry attempts |
| RetryDelay | TimeSpan | 00:00:01 | Delay between retry attempts |

## Important Notes

- **Trailing Slash**: Always include a trailing slash in the BaseUrl (e.g., `http://localhost:8086/`) for proper URL combination
- **Priority**: Appsettings values take precedence over environment variables when using dependency injection
- **Tests**: Environment variables are the simplest approach for unit tests
- **Production**: Use appsettings.json with environment-specific files for production deployments
