#!/bin/bash

# Test Observability Metrics Script
# This script helps validate that observability metrics are working correctly

set -e

echo "🔍 Testing FlinkDotNet Observability Metrics"
echo "============================================="

# Function to check if a service is responding
check_service() {
    local url=$1
    local name=$2
    echo -n "Checking $name... "
    if curl -s -f "$url" > /dev/null 2>&1; then
        echo "✅ OK"
        return 0
    else
        echo "❌ FAILED"
        return 1
    fi
}

# Check all required services
echo "📋 Checking service availability:"
check_service "http://localhost:5000/health" "LocalTesting API"
check_service "http://localhost:18006/-/healthy" "Prometheus"
check_service "http://localhost:18010/api/health" "Grafana"
check_service "http://localhost:4318/v1/metrics" "OpenTelemetry Collector"

echo ""
echo "🚀 Generating observability metrics..."

# Execute metrics simulation
response=$(curl -s -X POST http://localhost:5000/api/observability/metrics/simulate \
  -H "Content-Type: application/json" \
  -d '{"kafkaMessages": 5000, "flinkJobs": 2, "temporalWorkflows": 3}' || echo "FAILED")

if [[ "$response" == "FAILED" ]]; then
    echo "❌ Failed to execute metrics simulation"
    exit 1
fi

echo "✅ Metrics simulation executed successfully"
echo ""

# Wait for metrics to propagate
echo "⏳ Waiting for metrics to propagate (30 seconds)..."
sleep 30

echo ""
echo "📊 Checking metrics availability..."

# Check API metrics
echo "🔍 Checking API metrics..."
api_metrics=$(curl -s http://localhost:5000/api/observability/metrics/messages-per-second || echo "FAILED")

if [[ "$api_metrics" == "FAILED" ]]; then
    echo "❌ Failed to retrieve API metrics"
else
    echo "✅ API metrics available"
    
    # Extract summary information
    total_metrics=$(echo "$api_metrics" | jq -r '.Summary.TotalMetricsTracked // 0' 2>/dev/null || echo "0")
    active_flows=$(echo "$api_metrics" | jq -r '.Summary.ActiveFlows // 0' 2>/dev/null || echo "0")
    metrics_source=$(echo "$api_metrics" | jq -r '.Summary.MetricsSource // "Unknown"' 2>/dev/null || echo "Unknown")
    
    echo "  📈 Total metrics tracked: $total_metrics"
    echo "  🔄 Active flows: $active_flows"
    echo "  💾 Metrics source: $metrics_source"
fi

echo ""

# Check Prometheus metrics
echo "🔍 Checking Prometheus metrics..."
kafka_metrics=$(curl -s "http://localhost:18006/api/v1/query?query=kafka_producer_messages_total" | jq -r '.data.result | length' 2>/dev/null || echo "0")
flink_metrics=$(curl -s "http://localhost:18006/api/v1/query?query=flink_job_messages_in_total" | jq -r '.data.result | length' 2>/dev/null || echo "0")
temporal_metrics=$(curl -s "http://localhost:18006/api/v1/query?query=temporal_workflow_executions_total" | jq -r '.data.result | length' 2>/dev/null || echo "0")

echo "  📨 Kafka metrics in Prometheus: $kafka_metrics"
echo "  ⚙️ Flink metrics in Prometheus: $flink_metrics"
echo "  🔄 Temporal metrics in Prometheus: $temporal_metrics"

echo ""

# Provide recommendations
if [[ "$total_metrics" -gt 0 || "$kafka_metrics" -gt 0 ]]; then
    echo "🎉 SUCCESS: Metrics are working!"
    echo ""
    echo "📊 View your metrics in Grafana:"
    echo "   🌐 URL: http://localhost:18010"
    echo "   👤 Login: admin/admin"
    echo "   📋 Dashboard: FlinkDotNet Observability Metrics"
    echo ""
    echo "🔍 Or check Prometheus directly:"
    echo "   🌐 URL: http://localhost:18006"
    echo "   📊 Try queries like: rate(kafka_producer_messages_total[5m])"
else
    echo "⚠️  WARNING: No metrics found!"
    echo ""
    echo "🔧 Troubleshooting suggestions:"
    echo "   1. Ensure LocalTesting is fully started: dotnet run --project LocalTesting.AppHost"
    echo "   2. Run simulation again with more messages:"
    echo "      curl -X POST http://localhost:5000/api/observability/metrics/simulate \\"
    echo "        -H 'Content-Type: application/json' \\"
    echo "        -d '{\"kafkaMessages\": 20000}'"
    echo "   3. Wait 1-2 minutes for metrics to propagate"
    echo "   4. Check service logs for any errors"
fi

echo ""
echo "✅ Test complete!"