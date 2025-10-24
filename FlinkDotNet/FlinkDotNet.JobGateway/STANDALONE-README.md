# FlinkJobGateway - Standalone Deployment Guide

This package contains a standalone executable version of FlinkJobGateway that can run without Docker or containers.

## Quick Start

### Windows
1. Extract the ZIP file to a directory (e.g., `C:\FlinkJobGateway`)
2. Edit `start-gateway.bat` to configure your Flink cluster connection
3. Run `start-gateway.bat`
4. Access the API at `http://localhost:5000` (or configured URL)
5. View Swagger UI at `http://localhost:5000` when running in Development mode

### Linux
1. Extract the tar.gz file to a directory (e.g., `/opt/flinkjobgateway`)
2. Edit `start-gateway.sh` to configure your Flink cluster connection
3. Make executable: `chmod +x start-gateway.sh`
4. Run `./start-gateway.sh`
5. Access the API at `http://localhost:5000` (or configured URL)
6. View Swagger UI at `http://localhost:5000` when running in Development mode

## Configuration

### Required Environment Variables

Configure these variables in the startup script or set them before running:

- **FLINK_CLUSTER_HOST**: Hostname or IP of your Flink JobManager (default: `localhost`)
- **FLINK_CLUSTER_PORT**: Port of your Flink JobManager REST API (default: `8081`)

### Optional Environment Variables

- **KAFKA_BOOTSTRAP**: Kafka bootstrap servers (default: `localhost:9092`)
  - Only required if your Flink jobs use Kafka sources/sinks
  
- **LOG_FILE_PATH**: Directory for log files (default: `./logs`)
  - Logs are written as `FlinkDotNet.JobGateway.log.YYYYMMDD`
  
- **ASPNETCORE_ENVIRONMENT**: Runtime environment (default: `Production`)
  - Options: `Development`, `Production`, `Testing`
  - In Development mode, Swagger UI is enabled
  
- **ASPNETCORE_URLS**: URLs to listen on (default: `http://localhost:5000`)
  - Format: `http://hostname:port` or `https://hostname:port`
  - Multiple URLs: `http://localhost:5000;http://0.0.0.0:5000`

### Using appsettings.json

Alternatively, you can edit `appsettings.json` directly:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  }
}
```

**Note**: Environment variables take precedence over appsettings.json values.

## Connecting to Apache Flink Clusters

### Local Flink Cluster

If running Flink locally on the same machine:

```bash
# Default configuration works
export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081
```

### Remote Flink Cluster

For a Flink cluster running on another machine:

```bash
# Windows (in start-gateway.bat)
set FLINK_CLUSTER_HOST=flink.example.com
set FLINK_CLUSTER_PORT=8081

# Linux (in start-gateway.sh)
export FLINK_CLUSTER_HOST=flink.example.com
export FLINK_CLUSTER_PORT=8081
```

### Kubernetes Flink Cluster

For Flink running in Kubernetes with port-forward:

```bash
# In one terminal, forward Flink JobManager port
kubectl port-forward service/flink-jobmanager 8081:8081

# In another terminal, run gateway
export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081
./start-gateway.sh
```

### Docker Compose Flink Cluster

For Flink in Docker Compose on the same host:

```bash
# If gateway runs on host and Flink in Docker
export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081  # Mapped port

# If both in Docker network, use service name
export FLINK_CLUSTER_HOST=flink-jobmanager
export FLINK_CLUSTER_PORT=8081
```

## API Usage

Once running, submit Flink jobs via REST API:

```bash
# Health check
curl http://localhost:5000/health

# Submit a job (example)
curl -X POST http://localhost:5000/api/v1/jobs/submit \
  -H "Content-Type: application/json" \
  -d '{
    "jobName": "my-flink-job",
    "jobDefinition": { ... }
  }'

# Get job status
curl http://localhost:5000/api/v1/jobs/{jobId}/status

# View metrics (if Prometheus enabled)
curl http://localhost:5000/metrics
```

For complete API documentation, run in Development mode and visit Swagger UI:

```bash
# Windows
set ASPNETCORE_ENVIRONMENT=Development
start-gateway.bat

# Linux
export ASPNETCORE_ENVIRONMENT=Development
./start-gateway.sh

# Then open browser to http://localhost:5000
```

## Troubleshooting

### Gateway won't start

1. Check if port 5000 is already in use:
   ```bash
   # Windows
   netstat -ano | findstr :5000
   
   # Linux
   netstat -tulpn | grep :5000
   ```
   
2. Change to a different port:
   ```bash
   export ASPNETCORE_URLS=http://localhost:5001
   ```

### Cannot connect to Flink cluster

1. Verify Flink is running:
   ```bash
   curl http://your-flink-host:8081/config
   ```
   
2. Check network connectivity:
   ```bash
   # Windows
   telnet your-flink-host 8081
   
   # Linux
   nc -zv your-flink-host 8081
   ```

3. Check firewall rules allow connections to Flink port

### Logs show errors

1. Check log files in the configured LOG_FILE_PATH directory
2. Look for `FlinkDotNet.JobGateway.log.YYYYMMDD` files
3. Increase logging detail:
   - Edit `appsettings.json` and set `LogLevel.Default` to `Debug`

## Requirements

### Windows
- Windows 10 or later
- .NET 9.0 Runtime (included in self-contained executable)
- Java 17+ (auto-downloaded if not present, or set JAVA_HOME)

### Linux
- Ubuntu 20.04+ / RHEL 8+ / Debian 11+ (or compatible)
- .NET 9.0 Runtime (included in self-contained executable)
- Java 17+ (required for FlinkIRRunner, set JAVA_HOME or install via package manager)

### Network
- HTTP/HTTPS access to Apache Flink JobManager REST API (default port 8081)
- HTTP/HTTPS access to Kafka brokers (if using Kafka, default port 9092)

## Security Considerations

1. **Network Security**: 
   - Gateway exposes HTTP API on configured port
   - Use firewall rules to restrict access
   - Consider using reverse proxy (nginx, Apache) for HTTPS
   
2. **Authentication**:
   - Gateway currently does not implement authentication
   - Implement network-level security or add reverse proxy with auth
   
3. **Flink Cluster Access**:
   - Gateway requires REST API access to Flink JobManager
   - Ensure Flink security settings allow connections

## Next Steps

- See main project README for complete FlinkDotNet documentation
- Refer to ReleasePackagesTesting for full setup and testing examples
- Visit https://github.com/devstress/FlinkDotnet for latest updates
- Check Flink documentation: https://flink.apache.org/

## Support

For issues or questions:
- GitHub Issues: https://github.com/devstress/FlinkDotnet/issues
- Project Discussions: https://github.com/devstress/FlinkDotnet/discussions
