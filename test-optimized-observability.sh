#!/bin/bash

# Test script for optimized observability architecture
# Validates that native Prometheus endpoints are working

echo "🔍 Testing Optimized Observability Architecture"
echo "=============================================="

# Function to check if endpoint is responding
check_endpoint() {
    local name=$1
    local url=$2
    echo "⏳ Checking $name at $url..."
    
    if curl -s --connect-timeout 5 --max-time 10 "$url" > /dev/null; then
        echo "✅ $name endpoint is responding"
        return 0
    else
        echo "❌ $name endpoint is not responding"
        return 1
    fi
}

# Function to check Prometheus metrics endpoint
check_metrics_endpoint() {
    local name=$1
    local url=$2
    echo "📊 Checking $name metrics at $url..."
    
    local response=$(curl -s --connect-timeout 5 --max-time 10 "$url")
    if [[ $? -eq 0 && -n "$response" ]]; then
        local metric_count=$(echo "$response" | grep -c '^[a-zA-Z].*[0-9]')
        if [[ $metric_count -gt 0 ]]; then
            echo "✅ $name has $metric_count metrics available"
            echo "   Sample metrics:"
            echo "$response" | head -3 | sed 's/^/     /'
            return 0
        else
            echo "❌ $name endpoint responding but no metrics found"
            return 1
        fi
    else
        echo "❌ $name metrics endpoint not responding"
        return 1
    fi
}

echo ""
echo "🎯 Testing Native Prometheus Endpoints (NEW ARCHITECTURE)"
echo "--------------------------------------------------------"

# Test Flink JobManager native Prometheus endpoint
check_metrics_endpoint "Flink JobManager" "http://localhost:18050"

# Test Flink TaskManager native Prometheus endpoint  
check_metrics_endpoint "Flink TaskManager" "http://localhost:18051"

# Test Temporal Server native Prometheus endpoint
check_metrics_endpoint "Temporal Server" "http://localhost:18052/metrics"

echo ""
echo "🔄 Testing OTel Collector (OPTIMIZED ARCHITECTURE)"
echo "------------------------------------------------"

# Test OTel Collector (now handles fewer components)
check_metrics_endpoint "OTel Collector" "http://localhost:18008/metrics"

echo ""
echo "📈 Testing Prometheus Scraping Configuration"
echo "------------------------------------------"

# Test Prometheus itself
check_endpoint "Prometheus UI" "http://localhost:18006"

# Test if Prometheus can access its targets
if curl -s --connect-timeout 5 "http://localhost:18006/api/v1/targets" | grep -q "flink-jobmanager"; then
    echo "✅ Prometheus configured to scrape Flink JobManager directly"
else
    echo "❌ Prometheus not configured for Flink JobManager direct scraping"
fi

if curl -s --connect-timeout 5 "http://localhost:18006/api/v1/targets" | grep -q "temporal-server"; then
    echo "✅ Prometheus configured to scrape Temporal Server directly"
else
    echo "❌ Prometheus not configured for Temporal Server direct scraping"
fi

echo ""
echo "🏗️ Architecture Summary"
echo "--------------------"
echo "✅ NATIVE PROMETHEUS SCRAPING:"
echo "   • Flink JobManager:  Direct scraping (port 18050)"
echo "   • Flink TaskManager: Direct scraping (port 18051)"  
echo "   • Temporal Server:   Direct scraping (port 18052)"
echo ""
echo "🔄 OTEL COLLECTOR HANDLING:"
echo "   • .NET WebAPI metrics/traces"
echo "   • Kafka metrics (no native Prometheus)"
echo "   • Redis metrics (no native Prometheus)"
echo "   • PostgreSQL metrics (no native Prometheus)"
echo "   • Centralized logging"
echo ""
echo "📊 BENEFITS:"
echo "   • Reduced OTel Collector load"
echo "   • Eliminated single point of failure"
echo "   • Direct metrics collection (better performance)"
echo "   • Native application instrumentation"

echo ""
echo "🎉 Optimized Observability Architecture Test Complete"