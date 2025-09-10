#!/bin/bash

# Test minimal infrastructure setup to identify bottlenecks

echo "🔍 Testing minimal infrastructure startup..."
echo "📅 $(date)"

cd /home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting/LocalTesting.AppHost

export PATH="$HOME/.dotnet:$PATH"
export TESTING_MODE=true

echo "🚀 Starting infrastructure..."
timeout 120s dotnet run --configuration Release &
APPHOST_PID=$!

echo "⏱️ Waiting for services to become available..."

# Check each service as it becomes available
check_service() {
    local name=$1
    local url=$2
    local timeout=${3:-30}
    
    echo "🔍 Checking $name at $url (timeout: ${timeout}s)..."
    
    for i in $(seq 1 $timeout); do
        if curl -f -s "$url" > /dev/null 2>&1; then
            echo "✅ $name is ready (${i}s)"
            return 0
        fi
        sleep 1
        if [ $i -eq 10 ] || [ $i -eq 20 ] || [ $i -eq 30 ]; then
            echo "   ... still waiting for $name (${i}s elapsed)"
        fi
    done
    
    echo "❌ $name failed to start within ${timeout}s"
    return 1
}

# Wait for basic startup (infrastructure logs)
sleep 5

# Check services in dependency order
check_service "Redis" "http://localhost:6379" 30 || echo "⚠️ Redis not accessible (may be OK)"
check_service "Kafka" "http://localhost:9092" 45 || echo "⚠️ Kafka not accessible via HTTP (expected)"
check_service "Prometheus" "http://localhost:18006" 30
check_service "WebAPI Health" "http://localhost:18000/health" 60

# If WebAPI is available, test the observability endpoint
if check_service "WebAPI Health" "http://localhost:18000/health" 5; then
    echo "🧪 Testing observability endpoint..."
    curl -s "http://localhost:18000/api/observability/metrics/messages-per-second" | head -200
fi

echo "🛑 Stopping infrastructure..."
kill $APPHOST_PID 2>/dev/null
wait $APPHOST_PID 2>/dev/null

echo "📊 Test completed at $(date)"