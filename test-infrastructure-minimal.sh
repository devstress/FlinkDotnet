#!/bin/bash

# Minimal infrastructure test to debug connection issues
# This will help identify why the full test is failing

set -e

echo "🔍 Testing Infrastructure Startup Issues"
echo "========================================="

export PATH="$HOME/.dotnet:$PATH"
cd /home/runner/work/FlinkDotnet/FlinkDotnet/LocalTesting

echo ""
echo "📦 Building solution..."
dotnet build LocalTesting.sln --configuration Release

echo ""
echo "🐳 Testing Docker availability..."
docker --version
docker ps

echo ""
echo "🚀 Testing simple Aspire app startup (timeout after 60 seconds)..."
timeout 60s dotnet run --project LocalTesting.AppHost --configuration Release --no-build || echo "Aspire startup failed or timed out"

echo ""
echo "✅ Infrastructure test complete"