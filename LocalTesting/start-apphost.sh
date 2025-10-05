#!/bin/bash
# Start the Aspire AppHost and wait for services to be ready

cd "$(dirname "$0")"

echo "🚀 Starting Aspire AppHost..."
dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release &
APPHOST_PID=$!

# Wait for containers to start
echo "⏳ Waiting for containers to start..."
sleep 30

# Check if containers are running
if podman ps | grep -q kafka; then
    echo "✅ Kafka container is running"
    podman ps
else
    echo "❌ Kafka container failed to start"
    kill $APPHOST_PID 2>/dev/null
    exit 1
fi

echo "✅ AppHost started with PID: $APPHOST_PID"
echo "To stop: kill $APPHOST_PID"
