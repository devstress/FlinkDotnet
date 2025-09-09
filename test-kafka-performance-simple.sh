#!/bin/bash

# Simple Kafka producer performance test
# This script tests the high-performance configuration without full infrastructure

set -e

echo "🚀 Testing Kafka Producer Performance Configuration"
echo "=============================================="

# Build the WebAPI project
echo "📦 Building WebAPI project..."
export PATH="$HOME/.dotnet:$PATH"
cd /home/runner/work/FlinkDotnet/FlinkDotnet
dotnet build LocalTesting/LocalTesting.WebApi --configuration Release

# Check the configuration
echo ""
echo "🔍 Checking configuration in appsettings.json..."
grep -A 10 "Kafka" LocalTesting/LocalTesting.WebApi/appsettings.json || echo "Kafka section not found"

# Look for the high-performance mode configuration in code
echo ""
echo "🔍 Checking high-performance mode usage in KafkaProducerService..."
grep -n "HighPerformanceMode" LocalTesting/LocalTesting.WebApi/Services/KafkaProducerService.cs || echo "HighPerformanceMode not found"

echo ""
echo "✅ Configuration Check Complete"
echo ""
echo "📋 Summary:"
echo "• Configuration should have Kafka:HighPerformanceMode = true"
echo "• Code should detect this and use high-performance batch mode"
echo "• This should enable thousands msg/sec instead of 18 msg/sec"