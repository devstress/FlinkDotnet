# FlinkDotNet Docker Image User Instructions

This guide covers deploying and using the FlinkDotNet JobGateway Docker image for containerized deployments.

## Overview

The FlinkDotNet Docker image provides a pre-built, containerized version of the JobGateway service. It's ideal for cloud deployments, Kubernetes environments, and Docker Compose setups.

## Quick Start

### Pull the Image

```bash
docker pull devstress/flinkdotnet:latest
```

### Run the Container

```bash
docker run -d \
  --name flinkjobgateway \
  -p 8086:8086 \
  -e FLINK_CLUSTER_HOST=your-flink-host \
  -e FLINK_CLUSTER_PORT=8081 \
  devstress/flinkdotnet:latest
```

### Verify Deployment

```bash
# Check container status
docker ps | grep flinkjobgateway

# Check health
curl http://localhost:8086/health

# View logs
docker logs flinkjobgateway
```

## Configuration

### Environment Variables

| Variable | Description | Default | Required |
|----------|-------------|---------|----------|
| `FLINK_CLUSTER_HOST` | Flink JobManager hostname | - | Yes |
| `FLINK_CLUSTER_PORT` | Flink JobManager REST API port | `8081` | No |
| `KAFKA_BOOTSTRAP` | Kafka bootstrap servers | - | No* |
| `ASPNETCORE_URLS` | Internal listening URLs | `http://+:8086` | No |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Production` | No |
| `LOG_FILE_PATH` | Log file directory | `/app/logs` | No |

*Required if jobs use Kafka sources/sinks

### Volume Mounts

```bash
# Mount configuration
docker run -d \
  -v /path/to/appsettings.json:/app/appsettings.json:ro \
  devstress/flinkdotnet:latest

# Mount logs directory
docker run -d \
  -v /path/to/logs:/app/logs \
  devstress/flinkdotnet:latest

# Mount IR Runner jar (optional)
docker run -d \
  -v /path/to/flink-ir-runner.jar:/app/flink-ir-runner.jar:ro \
  devstress/flinkdotnet:latest
```

## Deployment Scenarios

### Scenario 1: Standalone Docker Container

```bash
docker run -d \
  --name flinkjobgateway \
  --restart unless-stopped \
  -p 8086:8086 \
  -e FLINK_CLUSTER_HOST=flink-jobmanager \
  -e FLINK_CLUSTER_PORT=8081 \
  -e KAFKA_BOOTSTRAP=kafka:9092 \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -v $(pwd)/logs:/app/logs \
  devstress/flinkdotnet:latest
```

### Scenario 2: Docker Compose

Create `docker-compose.yml`:

```yaml
version: '3.8'

services:
  flinkjobgateway:
    image: devstress/flinkdotnet:latest
    container_name: flinkjobgateway
    ports:
      - "8086:8086"
    environment:
      - FLINK_CLUSTER_HOST=flink-jobmanager
      - FLINK_CLUSTER_PORT=8081
      - KAFKA_BOOTSTRAP=kafka:9092
      - ASPNETCORE_ENVIRONMENT=Production
    volumes:
      - ./logs:/app/logs
      - ./appsettings.json:/app/appsettings.json:ro
    networks:
      - flink-network
    depends_on:
      - flink-jobmanager
      - kafka
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8086/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  flink-jobmanager:
    image: flink:2.1.0
    container_name: flink-jobmanager
    ports:
      - "8081:8081"
    command: jobmanager
    environment:
      - JOB_MANAGER_RPC_ADDRESS=flink-jobmanager
    networks:
      - flink-network

  flink-taskmanager:
    image: flink:2.1.0
    depends_on:
      - flink-jobmanager
    command: taskmanager
    environment:
      - JOB_MANAGER_RPC_ADDRESS=flink-jobmanager
    networks:
      - flink-network
    deploy:
      replicas: 3

  kafka:
    image: confluentinc/cp-kafka:latest
    container_name: kafka
    ports:
      - "9092:9092"
    environment:
      KAFKA_BROKER_ID: 1
      KAFKA_ZOOKEEPER_CONNECT: zookeeper:2181
      KAFKA_ADVERTISED_LISTENERS: PLAINTEXT://kafka:9092
    networks:
      - flink-network
    depends_on:
      - zookeeper

  zookeeper:
    image: confluentinc/cp-zookeeper:latest
    container_name: zookeeper
    environment:
      ZOOKEEPER_CLIENT_PORT: 2181
    networks:
      - flink-network

networks:
  flink-network:
    driver: bridge
```

Run with:
```bash
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f flinkjobgateway

# Stop
docker-compose down
```

### Scenario 3: Kubernetes Deployment

Create `deployment.yaml`:

```yaml
apiVersion: v1
kind: ConfigMap
metadata:
  name: flinkjobgateway-config
  namespace: default
data:
  FLINK_CLUSTER_HOST: "flink-jobmanager"
  FLINK_CLUSTER_PORT: "8081"
  KAFKA_BOOTSTRAP: "kafka:9092"
  ASPNETCORE_ENVIRONMENT: "Production"

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: flinkjobgateway
  namespace: default
  labels:
    app: flinkjobgateway
spec:
  replicas: 2
  selector:
    matchLabels:
      app: flinkjobgateway
  template:
    metadata:
      labels:
        app: flinkjobgateway
    spec:
      containers:
      - name: gateway
        image: devstress/flinkdotnet:latest
        ports:
        - containerPort: 8086
          name: http
        envFrom:
        - configMapRef:
            name: flinkjobgateway-config
        resources:
          requests:
            memory: "512Mi"
            cpu: "250m"
          limits:
            memory: "1Gi"
            cpu: "500m"
        livenessProbe:
          httpGet:
            path: /health
            port: 8086
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 8086
          initialDelaySeconds: 10
          periodSeconds: 5
        volumeMounts:
        - name: logs
          mountPath: /app/logs
      volumes:
      - name: logs
        emptyDir: {}

---
apiVersion: v1
kind: Service
metadata:
  name: flinkjobgateway
  namespace: default
spec:
  selector:
    app: flinkjobgateway
  type: LoadBalancer
  ports:
  - port: 80
    targetPort: 8086
    name: http

---
apiVersion: autoscaling/v2
kind: HorizontalPodAutoscaler
metadata:
  name: flinkjobgateway-hpa
  namespace: default
spec:
  scaleTargetRef:
    apiVersion: apps/v1
    kind: Deployment
    name: flinkjobgateway
  minReplicas: 2
  maxReplicas: 10
  metrics:
  - type: Resource
    resource:
      name: cpu
      target:
        type: Utilization
        averageUtilization: 70
  - type: Resource
    resource:
      name: memory
      target:
        type: Utilization
        averageUtilization: 80
```

Deploy:
```bash
# Apply configuration
kubectl apply -f deployment.yaml

# Check deployment
kubectl get deployments
kubectl get pods -l app=flinkjobgateway

# Check service
kubectl get services flinkjobgateway

# View logs
kubectl logs -l app=flinkjobgateway -f

# Check autoscaling
kubectl get hpa
```

### Scenario 4: Docker Swarm

Create `stack.yml`:

```yaml
version: '3.8'

services:
  flinkjobgateway:
    image: devstress/flinkdotnet:latest
    ports:
      - "8086:8086"
    environment:
      - FLINK_CLUSTER_HOST=flink-jobmanager
      - FLINK_CLUSTER_PORT=8081
      - KAFKA_BOOTSTRAP=kafka:9092
    networks:
      - flink-network
    deploy:
      replicas: 3
      update_config:
        parallelism: 1
        delay: 10s
      restart_policy:
        condition: on-failure
        delay: 5s
        max_attempts: 3
      resources:
        limits:
          cpus: '0.5'
          memory: 1G
        reservations:
          cpus: '0.25'
          memory: 512M
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8086/health"]
      interval: 30s
      timeout: 10s
      retries: 3

networks:
  flink-network:
    driver: overlay
```

Deploy:
```bash
docker stack deploy -c stack.yml flinkdotnet

# Check services
docker stack services flinkdotnet

# View logs
docker service logs flinkdotnet_flinkjobgateway -f

# Scale
docker service scale flinkdotnet_flinkjobgateway=5

# Remove
docker stack rm flinkdotnet
```

## Advanced Configuration

### Custom Configuration File

Create `custom-appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "FlinkDotNet": "Debug"
    }
  },
  "Flink": {
    "ClusterHost": "${FLINK_CLUSTER_HOST}",
    "ClusterPort": 8081,
    "ConnectionTimeout": "00:00:30",
    "RequestTimeout": "00:05:00"
  },
  "Kafka": {
    "BootstrapServers": "${KAFKA_BOOTSTRAP}",
    "SecurityProtocol": "SASL_SSL",
    "SaslMechanism": "PLAIN",
    "SaslUsername": "${KAFKA_USERNAME}",
    "SaslPassword": "${KAFKA_PASSWORD}"
  },
  "Metrics": {
    "Prometheus": {
      "Enabled": true,
      "Path": "/metrics"
    }
  }
}
```

Mount and use:
```bash
docker run -d \
  -v $(pwd)/custom-appsettings.json:/app/appsettings.Production.json:ro \
  -e ASPNETCORE_ENVIRONMENT=Production \
  -e FLINK_CLUSTER_HOST=flink-jobmanager \
  -e KAFKA_BOOTSTRAP=kafka:9092 \
  -e KAFKA_USERNAME=myuser \
  -e KAFKA_PASSWORD=mypassword \
  devstress/flinkdotnet:latest
```

### HTTPS Configuration

```bash
# Generate certificate
openssl req -x509 -newkey rsa:4096 -keyout key.pem -out cert.pem -days 365 -nodes

# Convert to PFX
openssl pkcs12 -export -out certificate.pfx -inkey key.pem -in cert.pem

# Run with HTTPS
docker run -d \
  -p 8443:8443 \
  -v $(pwd)/certificate.pfx:/app/certificate.pfx:ro \
  -e ASPNETCORE_URLS=https://+:8443 \
  -e ASPNETCORE_Kestrel__Certificates__Default__Path=/app/certificate.pfx \
  -e ASPNETCORE_Kestrel__Certificates__Default__Password=your-password \
  devstress/flinkdotnet:latest
```

## Networking

### Bridge Network (Default)

```bash
# Create custom bridge network
docker network create flink-network

# Run containers on same network
docker run -d --name flink-jobmanager --network flink-network flink:2.1.0 jobmanager
docker run -d --name flinkjobgateway --network flink-network \
  -e FLINK_CLUSTER_HOST=flink-jobmanager \
  devstress/flinkdotnet:latest
```

### Host Network

```bash
# Use host network (Linux only)
docker run -d \
  --network host \
  -e FLINK_CLUSTER_HOST=localhost \
  devstress/flinkdotnet:latest
```

### Overlay Network (Swarm)

Automatically created with Docker Swarm stack deployment.

## Monitoring

### Health Checks

```bash
# Docker healthcheck
docker run -d \
  --health-cmd="curl -f http://localhost:8086/health || exit 1" \
  --health-interval=30s \
  --health-timeout=10s \
  --health-retries=3 \
  --health-start-period=40s \
  devstress/flinkdotnet:latest

# Check health status
docker inspect --format='{{.State.Health.Status}}' flinkjobgateway
```

### Prometheus Metrics

```yaml
# Add to Prometheus configuration
scrape_configs:
  - job_name: 'flinkdotnet-gateway'
    static_configs:
      - targets: ['flinkjobgateway:8086']
    metrics_path: '/metrics'
```

### Logging

```bash
# View logs
docker logs flinkjobgateway

# Follow logs
docker logs -f flinkjobgateway

# Export logs
docker logs flinkjobgateway > gateway.log

# With Docker Compose
docker-compose logs -f flinkjobgateway

# In Kubernetes
kubectl logs -l app=flinkjobgateway -f
```

## Troubleshooting

### Container Won't Start

**Check logs**:
```bash
docker logs flinkjobgateway
```

**Common issues**:
- Port already in use: Change `-p` mapping
- Missing environment variables: Check required vars
- Volume mount issues: Verify paths exist

### Cannot Connect to Flink

**Test from container**:
```bash
docker exec flinkjobgateway curl http://flink-jobmanager:8081/config
```

**Common issues**:
- Wrong hostname: Use Docker service name
- Network isolation: Ensure containers on same network
- Firewall: Check container firewall rules

### Performance Issues

**Check resource usage**:
```bash
docker stats flinkjobgateway
```

**Increase resources**:
```bash
docker run -d \
  --memory=2g \
  --cpus=2 \
  devstress/flinkdotnet:latest
```

## Security Best Practices

1. **Run as Non-Root User**
   ```dockerfile
   # Image already runs as non-root
   # Verify:
   docker exec flinkjobgateway whoami
   ```

2. **Read-Only Filesystem**
   ```bash
   docker run -d \
     --read-only \
     --tmpfs /tmp \
     --tmpfs /app/logs \
     devstress/flinkdotnet:latest
   ```

3. **Resource Limits**
   ```bash
   docker run -d \
     --memory=1g \
     --memory-swap=1g \
     --cpus=0.5 \
     --pids-limit=100 \
     devstress/flinkdotnet:latest
   ```

4. **Security Options**
   ```bash
   docker run -d \
     --security-opt=no-new-privileges \
     --cap-drop=ALL \
     devstress/flinkdotnet:latest
   ```

## Image Variants

### Tags

- `latest`: Latest stable release
- `vX.X.X`: Specific version
- `vX.X.X-alpine`: Alpine-based image (smaller)
- `dev`: Development builds (unstable)

### Pulling Specific Version

```bash
docker pull devstress/flinkdotnet:v1.2.3
docker pull devstress/flinkdotnet:v1.2.3-alpine
```

## Building Custom Image

If you need to customize the image:

```dockerfile
# Dockerfile.custom
FROM devstress/flinkdotnet:latest

# Add custom IR Runner
COPY custom-flink-ir-runner.jar /app/flink-ir-runner.jar

# Add custom configuration
COPY custom-appsettings.json /app/appsettings.Production.json

# Add custom scripts
COPY scripts/ /app/scripts/
RUN chmod +x /app/scripts/*.sh
```

Build:
```bash
docker build -t my-flinkjobgateway:latest -f Dockerfile.custom .
```

## Next Steps

- **[FlinkDotNet Client](FlinkDotNet-Client-User-Instructions.md)** - Client library usage
- **[JobGateway Standalone](FlinkDotNet-JobGateway-User-Instructions.md)** - Non-containerized deployment
- **[Gateway API Reference](../gateway-api.md)** - API documentation
- **[Deployment Guide](../deployment.md)** - Production deployment strategies

## Support

- **Docker Hub**: https://hub.docker.com/r/devstress/flinkdotnet
- **GitHub Issues**: https://github.com/devstress/FlinkDotnet/issues
- **Discussions**: https://github.com/devstress/FlinkDotnet/discussions
