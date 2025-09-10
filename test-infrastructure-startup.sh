#!/bin/bash
# Quick infrastructure startup test to identify bottlenecks

echo "🔍 Starting infrastructure startup analysis..."

# Export testing environment variables
export TESTING_MODE=true
export PATH="/home/runner/.dotnet:$PATH"

cd /home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting

echo "⏱️  Starting infrastructure at $(date +"%H:%M:%S")"

# Start the AppHost with timeout
timeout 120 dotnet run --project LocalTesting.AppHost --configuration Release &
APP_PID=$!

echo "📊 Monitoring infrastructure startup (PID: $APP_PID)..."

# Monitor for 120 seconds
for i in {1..120}; do
    sleep 1
    
    # Check if process is still running
    if ! kill -0 $APP_PID 2>/dev/null; then
        echo "❌ AppHost process ended unexpectedly at $i seconds"
        break
    fi
    
    # Every 10 seconds, show a progress update
    if [ $((i % 10)) -eq 0 ]; then
        echo "⏳ $i seconds elapsed..."
    fi
    
    # Try to check WebAPI health
    if [ $i -gt 30 ]; then
        if curl -f -s http://localhost:18000/health > /dev/null 2>&1; then
            echo "✅ Infrastructure ready at $i seconds!"
            kill $APP_PID 2>/dev/null
            exit 0
        fi
    fi
done

echo "❌ Timeout: Infrastructure did not become ready within 120 seconds"
kill $APP_PID 2>/dev/null
exit 1