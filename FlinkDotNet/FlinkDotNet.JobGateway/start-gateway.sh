#!/bin/bash
# FlinkJobGateway Startup Script for Linux
# This script configures and starts the FlinkJobGateway service

# =====================================================
# CONFIGURATION - Customize these values for your environment
# =====================================================

# Flink Cluster Configuration
export FLINK_CLUSTER_HOST="${FLINK_CLUSTER_HOST:-localhost}"
export FLINK_CLUSTER_PORT="${FLINK_CLUSTER_PORT:-8081}"

# Kafka Bootstrap Servers (optional - only if using Kafka sources/sinks)
export KAFKA_BOOTSTRAP="${KAFKA_BOOTSTRAP:-localhost:9092}"

# Log File Path (directory where logs will be written)
export LOG_FILE_PATH="${LOG_FILE_PATH:-./logs}"

# AspNetCore Environment (Development, Production, Testing)
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Production}"

# AspNetCore URLs (HTTP endpoints to listen on)
export ASPNETCORE_URLS="${ASPNETCORE_URLS:-http://localhost:5000}"

# Aspire Service Discovery (optional - only when using Aspire orchestration)
# export services__flink_jobmanager__http__0="http://localhost:8081"

# =====================================================
# DO NOT MODIFY BELOW THIS LINE
# =====================================================

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
GATEWAY_BINARY="$SCRIPT_DIR/FlinkDotNet.JobGateway"

# Create logs directory if it doesn't exist
mkdir -p "$LOG_FILE_PATH"

# Check if binary exists
if [[ ! -f "$GATEWAY_BINARY" ]]; then
    echo "ERROR: FlinkJobGateway binary not found at $GATEWAY_BINARY"
    exit 1
fi

# Make binary executable if not already
chmod +x "$GATEWAY_BINARY"

echo "========================================"
echo "FlinkJobGateway - Starting"
echo "========================================"
echo "Flink Cluster: http://$FLINK_CLUSTER_HOST:$FLINK_CLUSTER_PORT"
echo "Kafka Bootstrap: $KAFKA_BOOTSTRAP"
echo "Log Directory: $LOG_FILE_PATH"
echo "Environment: $ASPNETCORE_ENVIRONMENT"
echo "Listening on: $ASPNETCORE_URLS"
echo "========================================"
echo ""

# Start the gateway
exec "$GATEWAY_BINARY"
