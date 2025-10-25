# FlinkDotNet.JobGateway User Instructions

This guide covers deploying and using the FlinkDotNet.JobGateway service for submitting and managing Flink jobs via REST API.

## Overview

FlinkDotNet.JobGateway is a REST API service that acts as a bridge between .NET applications and Apache Flink clusters. It receives job definitions in JSON format (IR), validates them, and submits them to the Flink cluster.

## Deployment Options

### Option 1: Standalone Executable (Recommended for Production)

Download the pre-built standalone package from [GitHub Releases](https://github.com/devstress/FlinkDotnet/releases).

#### Windows Deployment

1. **Download and Extract**
   ```powershell
   # Download the Windows package
   Invoke-WebRequest -Uri "https://github.com/devstress/FlinkDotnet/releases/download/vX.X.X/jobgateway-win-x64-X.X.X.zip" -OutFile "jobgateway.zip"
   
   # Extract to installation directory
   Expand-Archive -Path jobgateway.zip -DestinationPath "C:\FlinkJobGateway"
   cd C:\FlinkJobGateway
   ```

2. **Configure**
   
   Edit `start-gateway.bat`:
   ```batch
   @echo off
   REM Configure Flink cluster connection
   set FLINK_CLUSTER_HOST=localhost
   set FLINK_CLUSTER_PORT=8081
   
   REM Configure Kafka (optional)
   set KAFKA_BOOTSTRAP=localhost:9092
   
   REM Configure gateway
   set ASPNETCORE_URLS=http://localhost:5000
   set ASPNETCORE_ENVIRONMENT=Production
   
   REM Start the gateway
   FlinkDotNet.JobGateway.exe
   ```

3. **Run**
   ```powershell
   .\start-gateway.bat
   ```

4. **Verify**
   ```powershell
   # Check health
   curl http://localhost:5000/health
   
   # View metrics
   curl http://localhost:5000/metrics
   ```

#### Linux Deployment

1. **Download and Extract**
   ```bash
   # Download the Linux package
   wget https://github.com/devstress/FlinkDotnet/releases/download/vX.X.X/jobgateway-linux-x64-X.X.X.tar.gz
   
   # Extract to installation directory
   tar -xzf jobgateway-linux-x64-X.X.X.tar.gz -C /opt/
   cd /opt/FlinkJobGateway
   ```

2. **Configure**
   
   Edit `start-gateway.sh`:
   ```bash
   #!/bin/bash
   
   # Configure Flink cluster connection
   export FLINK_CLUSTER_HOST=localhost
   export FLINK_CLUSTER_PORT=8081
   
   # Configure Kafka (optional)
   export KAFKA_BOOTSTRAP=localhost:9092
   
   # Configure gateway
   export ASPNETCORE_URLS=http://localhost:5000
   export ASPNETCORE_ENVIRONMENT=Production
   
   # Configure logging
   export LOG_FILE_PATH=./logs
   
   # Start the gateway
   ./FlinkDotNet.JobGateway
   ```

3. **Make Executable and Run**
   ```bash
   chmod +x start-gateway.sh
   ./start-gateway.sh
   ```

4. **Verify**
   ```bash
   # Check health
   curl http://localhost:5000/health
   
   # View metrics
   curl http://localhost:5000/metrics
   ```

### Option 2: Build from Source

```bash
# Clone repository
git clone https://github.com/devstress/FlinkDotnet.git
cd FlinkDotnet

# Build the gateway
dotnet build FlinkDotNet/FlinkDotNet.JobGateway/FlinkDotNet.JobGateway.csproj -c Release

# Run the gateway
cd FlinkDotNet/FlinkDotNet.JobGateway
dotnet run --configuration Release
```

### Option 3: Run with .NET CLI

```bash
# Install as global tool (if published)
dotnet tool install -g FlinkDotNet.JobGateway

# Run
flinkjobgateway --flink-host localhost --flink-port 8081
```

## Configuration

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `FLINK_CLUSTER_HOST` | Flink JobManager hostname | `localhost` | Yes |
| `FLINK_CLUSTER_PORT` | Flink JobManager REST API port | `8081` | Yes |
| `KAFKA_BOOTSTRAP` | Kafka bootstrap servers | `localhost:9092` | No* |
| `ASPNETCORE_URLS` | URLs to listen on | `http://localhost:5000` | No |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | No |
| `LOG_FILE_PATH` | Log file directory | `./logs` | No |

*Required if jobs use Kafka sources/sinks

### Configuration File (appsettings.json)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "FlinkDotNet": "Debug"
    }
  },
  "Flink": {
    "ClusterHost": "localhost",
    "ClusterPort": 8081,
    "ConnectionTimeout": "00:00:30",
    "RequestTimeout": "00:05:00"
  },
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "SecurityProtocol": "PLAINTEXT"
  },
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  },
  "AllowedHosts": "*"
}
```

**Note**: Environment variables take precedence over `appsettings.json`.

### Flink Cluster Configuration

#### Local Flink Cluster

```bash
export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081
```

#### Remote Flink Cluster

```bash
export FLINK_CLUSTER_HOST=flink.example.com
export FLINK_CLUSTER_PORT=8081
```

#### Kubernetes Flink Cluster

```bash
# Option 1: Port-forward
kubectl port-forward service/flink-jobmanager 8081:8081

export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081

# Option 2: Direct service access (if gateway runs in same cluster)
export FLINK_CLUSTER_HOST=flink-jobmanager.default.svc.cluster.local
export FLINK_CLUSTER_PORT=8081
```

#### Docker Compose Flink Cluster

```bash
# If gateway runs on host
export FLINK_CLUSTER_HOST=localhost
export FLINK_CLUSTER_PORT=8081  # Mapped port

# If gateway runs in Docker network
export FLINK_CLUSTER_HOST=flink-jobmanager
export FLINK_CLUSTER_PORT=8081
```

## API Reference

Base URL: `http://localhost:5000` (or configured URL)

### Submit Job

**Endpoint**: `POST /api/v1/jobs/submit`

**Request Body**:
```json
{
  "jobName": "my-streaming-job",
  "jobDefinition": {
    "metadata": {
      "jobId": "unique-job-id",
      "version": "1.0",
      "parallelism": 4
    },
    "source": {
      "type": "kafka",
      "topic": "input-topic",
      "bootstrapServers": "kafka:9092",
      "groupId": "consumer-group"
    },
    "operations": [
      {
        "type": "map",
        "expression": "x => x.ToUpper()"
      },
      {
        "type": "filter",
        "expression": "x => x.Length > 5"
      }
    ],
    "sink": {
      "type": "kafka",
      "topic": "output-topic",
      "bootstrapServers": "kafka:9092"
    }
  }
}
```

**Response**:
```json
{
  "success": true,
  "jobId": "job-12345",
  "flinkJobId": "flink-67890",
  "submissionTime": "2024-10-25T15:00:00Z",
  "message": "Job submitted successfully"
}
```

**cURL Example**:
```bash
curl -X POST http://localhost:5000/api/v1/jobs/submit \
  -H "Content-Type: application/json" \
  -d @job-definition.json
```

### Get Job Status

**Endpoint**: `GET /api/v1/jobs/{flinkJobId}/status`

**Response**:
```json
{
  "jobId": "flink-67890",
  "status": "RUNNING",
  "startTime": "2024-10-25T15:00:00Z",
  "endTime": null,
  "duration": "PT5M30S",
  "parallelism": 4
}
```

**cURL Example**:
```bash
curl http://localhost:5000/api/v1/jobs/flink-67890/status
```

### Get Job Metrics

**Endpoint**: `GET /api/v1/jobs/{flinkJobId}/metrics`

**Response**:
```json
{
  "jobId": "flink-67890",
  "recordsIn": 1000000,
  "recordsOut": 950000,
  "throughput": 5000.0,
  "parallelism": 4,
  "maxParallelism": 8,
  "backpressureInfo": {
    "status": "OK",
    "level": 0.1
  },
  "checkpointMetrics": {
    "totalCheckpoints": 10,
    "completedCheckpoints": 10,
    "failedCheckpoints": 0
  }
}
```

**cURL Example**:
```bash
curl http://localhost:5000/api/v1/jobs/flink-67890/metrics
```

### Cancel Job

**Endpoint**: `POST /api/v1/jobs/{flinkJobId}/cancel`

**Response**:
```json
{
  "success": true,
  "message": "Job cancelled successfully"
}
```

**cURL Example**:
```bash
curl -X POST http://localhost:5000/api/v1/jobs/flink-67890/cancel
```

### Health Check

**Endpoint**: `GET /health`

**Response**:
```json
{
  "status": "Healthy",
  "flinkConnection": "Connected",
  "uptime": "PT2H30M",
  "version": "1.0.0"
}
```

### Metrics (Prometheus)

**Endpoint**: `GET /metrics`

**Response**: Prometheus-formatted metrics

```
# HELP flinkdotnet_jobs_submitted_total Total number of jobs submitted
# TYPE flinkdotnet_jobs_submitted_total counter
flinkdotnet_jobs_submitted_total 42

# HELP flinkdotnet_jobs_running Current number of running jobs
# TYPE flinkdotnet_jobs_running gauge
flinkdotnet_jobs_running 5
```

## Swagger UI (Development Mode)

Enable Swagger UI for API exploration:

```bash
# Set environment to Development
export ASPNETCORE_ENVIRONMENT=Development

# Start gateway
./start-gateway.sh

# Open browser to http://localhost:5000
```

Swagger UI provides:
- Interactive API documentation
- Request/response examples
- API testing interface

## Monitoring and Observability

### Log Files

Logs are written to `LOG_FILE_PATH` directory:

```
logs/
├── FlinkDotNet.JobGateway.log.20241025
├── FlinkDotNet.JobGateway.log.20241024
└── ...
```

**Log Levels**:
- `Error`: Critical issues
- `Warning`: Potential problems
- `Information`: Normal operations
- `Debug`: Detailed debugging (enable via configuration)

### Prometheus Metrics

Configure Prometheus to scrape metrics:

```yaml
# prometheus.yml
scrape_configs:
  - job_name: 'flinkdotnet-gateway'
    static_configs:
      - targets: ['localhost:5000']
    metrics_path: '/metrics'
```

### Health Checks

Monitor gateway health:

```bash
# Simple health check
curl http://localhost:5000/health

# Monitor in script
while true; do
  curl -f http://localhost:5000/health || echo "Gateway unhealthy!"
  sleep 30
done
```

## Troubleshooting

### Gateway Won't Start

**Problem**: Port already in use

**Solution**:
```bash
# Check what's using the port
# Linux
netstat -tulpn | grep :5000

# Windows
netstat -ano | findstr :5000

# Use different port
export ASPNETCORE_URLS=http://localhost:5001
```

**Problem**: Cannot find .NET runtime

**Solution**:
```bash
# Verify .NET is installed
dotnet --version

# Standalone executables include runtime
# but may need dependencies
```

### Cannot Connect to Flink

**Problem**: Connection refused

**Solution**:
```bash
# Verify Flink is running
curl http://your-flink-host:8081/config

# Test network connectivity
nc -zv your-flink-host 8081

# Check firewall rules
# Linux
sudo iptables -L | grep 8081

# Verify configuration
echo $FLINK_CLUSTER_HOST
echo $FLINK_CLUSTER_PORT
```

**Problem**: Timeout connecting to Flink

**Solution**:
```json
// Increase timeout in appsettings.json
{
  "Flink": {
    "ConnectionTimeout": "00:01:00",
    "RequestTimeout": "00:10:00"
  }
}
```

### Job Submission Fails

**Problem**: Validation errors

**Solution**:
```bash
# Enable debug logging
export Logging__LogLevel__FlinkDotNet=Debug

# Check logs for detailed error messages
tail -f logs/FlinkDotNet.JobGateway.log.*
```

**Problem**: IR Runner not found

**Solution**:
```bash
# Ensure Java is installed
java -version

# Set JAVA_HOME if needed
export JAVA_HOME=/path/to/java

# Rebuild IR Runner
cd FlinkDotNet/FlinkDotNet.JobGateway
dotnet build -c Release /p:BuildFlinkRunner=true
```

## Production Deployment

### Systemd Service (Linux)

Create `/etc/systemd/system/flinkjobgateway.service`:

```ini
[Unit]
Description=FlinkDotNet Job Gateway
After=network.target

[Service]
Type=simple
User=flinkgateway
WorkingDirectory=/opt/FlinkJobGateway
ExecStart=/opt/FlinkJobGateway/FlinkDotNet.JobGateway
Restart=always
RestartSec=10
Environment="FLINK_CLUSTER_HOST=flink-jobmanager"
Environment="FLINK_CLUSTER_PORT=8081"
Environment="ASPNETCORE_URLS=http://0.0.0.0:5000"
Environment="ASPNETCORE_ENVIRONMENT=Production"

[Install]
WantedBy=multi-user.target
```

Enable and start:
```bash
sudo systemctl daemon-reload
sudo systemctl enable flinkjobgateway
sudo systemctl start flinkjobgateway
sudo systemctl status flinkjobgateway
```

### Windows Service

Use NSSM (Non-Sucking Service Manager):

```powershell
# Download NSSM
# Install as service
nssm install FlinkJobGateway "C:\FlinkJobGateway\FlinkDotNet.JobGateway.exe"

# Configure environment
nssm set FlinkJobGateway AppEnvironmentExtra FLINK_CLUSTER_HOST=localhost
nssm set FlinkJobGateway AppEnvironmentExtra FLINK_CLUSTER_PORT=8081

# Start service
nssm start FlinkJobGateway
```

### Reverse Proxy (HTTPS)

Configure nginx for HTTPS:

```nginx
server {
    listen 443 ssl;
    server_name gateway.example.com;
    
    ssl_certificate /path/to/cert.pem;
    ssl_certificate_key /path/to/key.pem;
    
    location / {
        proxy_pass http://localhost:5000;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
    }
}
```

## Security Considerations

1. **Network Security**
   - Use firewall to restrict access
   - Deploy behind reverse proxy with HTTPS
   - Limit allowed origins for CORS

2. **Authentication**
   - Gateway doesn't include built-in auth
   - Implement at reverse proxy level
   - Use API gateway for enterprise auth

3. **Flink Cluster Access**
   - Use network policies to restrict access
   - Enable Flink authentication if available
   - Audit job submissions

## Best Practices

1. **High Availability**
   - Deploy multiple gateway instances
   - Use load balancer
   - Configure health checks

2. **Resource Limits**
   - Set memory limits for container deployment
   - Configure connection pooling
   - Implement rate limiting

3. **Monitoring**
   - Monitor gateway health
   - Track job submission metrics
   - Alert on failures

4. **Logging**
   - Centralize logs (e.g., ELK stack)
   - Rotate log files
   - Set appropriate log levels

## Next Steps

- **[Gateway API Reference](../gateway-api.md)** - Detailed API documentation
- **[FlinkDotNet Client](FlinkDotNet-Client-User-Instructions.md)** - Client library usage
- **[Docker Deployment](FlinkDotNet-Docker-Image-User-Instructions.md)** - Container deployment
- **[Troubleshooting](../troubleshooting.md)** - Common issues

## Support

- **GitHub Issues**: https://github.com/devstress/FlinkDotnet/issues
- **Discussions**: https://github.com/devstress/FlinkDotnet/discussions
