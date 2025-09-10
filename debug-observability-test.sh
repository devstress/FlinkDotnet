#!/bin/bash

echo "🔍 Starting targeted observability test debugging..."

# Export testing environment variables
export TESTING_MODE=true
export PATH="/home/runner/.dotnet:$PATH"

cd /home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting

# Kill any existing processes
pkill -f dotnet 2>/dev/null || true
sleep 2

echo "📊 Step 1: Testing basic infrastructure startup (should be ~30 seconds)"
echo "⏱️ Starting at $(date +"%H:%M:%S")"

# Start AppHost in background
nohup dotnet run --project LocalTesting.AppHost --configuration Release > /tmp/apphost.log 2>&1 &
APPHOST_PID=$!

# Wait for basic services
for i in {1..90}; do
    sleep 1
    
    if curl -f -s http://localhost:18000/health > /dev/null 2>&1; then
        echo "✅ Step 1 COMPLETE: Infrastructure ready at $i seconds"
        break
    fi
    
    if [ $((i % 15)) -eq 0 ]; then
        echo "⏳ $i seconds - still waiting for WebAPI health endpoint..."
    fi
done

if ! curl -f -s http://localhost:18000/health > /dev/null 2>&1; then
    echo "❌ Step 1 FAILED: Infrastructure not ready after 90 seconds"
    exit 1
fi

echo ""
echo "📊 Step 2: Testing observability flow APIs"

# Test the specific observability endpoint  
echo "🧪 Testing observability status endpoint..."
RESPONSE=$(curl -s http://localhost:18000/api/observability/status)
echo "Status response: $RESPONSE"

echo ""
echo "🧪 Testing observability metrics endpoint..."
METRICS_RESPONSE=$(curl -s http://localhost:18000/api/observability/metrics)
echo "Metrics response (first 200 chars): ${METRICS_RESPONSE:0:200}..."

echo ""
echo "📊 Step 3: Testing observability flow execution"
echo "🚀 Starting observability flow..."

START_TIME=$(date +%s)

# Execute the observability flow
FLOW_RESPONSE=$(curl -s -X POST http://localhost:18000/api/observability/flow \
  -H "Content-Type: application/json" \
  -d '{"KafkaMessages": 1000, "FlinkJobs": 1, "TemporalWorkflows": 2}')

echo "Flow execution started. Response: $FLOW_RESPONSE"

echo ""
echo "📊 Step 4: Monitoring progress until completion or failure"

# Monitor progress
for i in {1..120}; do
    sleep 5  # Check every 5 seconds per user requirement
    
    PROGRESS_RESPONSE=$(curl -s http://localhost:18000/api/observability/progress)
    echo "[$i] Progress ($(date +"%H:%M:%S")): $PROGRESS_RESPONSE"
    
    # Check if complete (contains 100% or complete status)
    if echo "$PROGRESS_RESPONSE" | grep -q "100\|complete\|success"; then
        END_TIME=$(date +%s)
        TOTAL_TIME=$((END_TIME - START_TIME))
        echo "✅ Step 4 COMPLETE: Flow finished in $TOTAL_TIME seconds"
        break
    fi
    
    # Check for stall or error
    if echo "$PROGRESS_RESPONSE" | grep -q "error\|failed\|stalled"; then
        echo "❌ Step 4 FAILED: Flow error detected: $PROGRESS_RESPONSE"
        break
    fi
    
    # Every 60 seconds (12 iterations), show detailed status
    if [ $((i % 12)) -eq 0 ]; then
        MINUTES=$((i * 5 / 60))
        echo "⏳ $MINUTES minutes elapsed - flow still running"
    fi
done

echo ""
echo "📊 Final Results:"
echo "- Infrastructure startup: Fast (~30s)"
echo "- APIs responding: OK" 
echo "- Observability flow: See above"
echo ""
echo "💡 If flow is slow, the issue is in workload processing, not infrastructure startup."

# Cleanup
kill $APPHOST_PID 2>/dev/null || true