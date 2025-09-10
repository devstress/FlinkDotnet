#!/bin/bash
set -e

echo "🧪 LocalTesting Observability Quick Validation Script"
echo "===================================================="

# Verify .NET 9.0 SDK
echo "🔍 Checking .NET version..."
DOTNET_VERSION=$(dotnet --version)
echo "   .NET SDK: $DOTNET_VERSION"

if [[ ! $DOTNET_VERSION == 9.* ]]; then
    echo "❌ This script requires .NET 9.0 SDK"
    exit 1
fi

echo "✅ .NET 9.0 SDK detected"

# Build LocalTesting core projects only (skip integration tests)
echo ""
echo "🔨 Building LocalTesting core projects..."
cd "$(dirname "$0")"

echo "   Building LocalTesting.Shared..."
dotnet build LocalTesting.Shared --configuration Release --verbosity minimal

echo "   Building LocalTesting.WebApi..."
dotnet build LocalTesting.WebApi --configuration Release --verbosity minimal

echo "   Building LocalTesting.AppHost..."
dotnet build LocalTesting.AppHost --configuration Release --verbosity minimal

echo "✅ LocalTesting core projects built successfully"

# Test infrastructure startup
echo ""
echo "🚀 Testing infrastructure startup..."

# Start infrastructure in background
dotnet run --project LocalTesting.AppHost --configuration Release > /tmp/aspire.log 2>&1 &
ASPIRE_PID=$!

echo "   Started LocalTesting infrastructure (PID: $ASPIRE_PID)"
echo "   Waiting for startup..."

# Wait for infrastructure to be ready
RETRY_COUNT=0
MAX_RETRIES=30

while [ $RETRY_COUNT -lt $MAX_RETRIES ]; do
    sleep 2
    RETRY_COUNT=$((RETRY_COUNT + 1))
    
    # Test health endpoint
    if curl -s http://localhost:13001/health > /dev/null 2>&1; then
        echo "✅ WebAPI health endpoint responded (attempt $RETRY_COUNT)"
        break
    fi
    
    if [ $RETRY_COUNT -eq $MAX_RETRIES ]; then
        echo "❌ Infrastructure failed to start within $((MAX_RETRIES * 2)) seconds"
        kill $ASPIRE_PID 2>/dev/null || true
        echo "Recent logs:"
        tail -20 /tmp/aspire.log
        exit 1
    fi
    
    echo "   Waiting for infrastructure startup (attempt $RETRY_COUNT/$MAX_RETRIES)..."
done

# Test observability endpoints
echo ""
echo "🔍 Testing observability endpoints..."

# Test progress endpoint
echo "   Testing progress endpoint..."
if curl -s http://localhost:13001/api/observability/progress/infrastructure-and-workload | jq .Progress.OverallPercentage > /dev/null 2>&1; then
    PROGRESS=$(curl -s http://localhost:13001/api/observability/progress/infrastructure-and-workload | jq .Progress.OverallPercentage)
    echo "✅ Progress endpoint working - Current progress: ${PROGRESS}%"
else
    echo "❌ Progress endpoint failed"
    kill $ASPIRE_PID 2>/dev/null || true
    exit 1
fi

# Test metrics endpoint
echo "   Testing metrics endpoint..."
if curl -s http://localhost:13001/metrics > /dev/null 2>&1; then
    METRIC_COUNT=$(curl -s http://localhost:13001/metrics | wc -l)
    echo "✅ Metrics endpoint working - Found $METRIC_COUNT metric lines"
else
    echo "❌ Metrics endpoint failed"
    kill $ASPIRE_PID 2>/dev/null || true
    exit 1
fi

# Test Swagger/API documentation
echo "   Testing Swagger endpoint..."
if curl -s http://localhost:13001/swagger/v1/swagger.json > /dev/null 2>&1; then
    API_COUNT=$(curl -s http://localhost:13001/swagger/v1/swagger.json | jq '.paths | length')
    echo "✅ Swagger API documentation working - Found $API_COUNT API endpoints"
else
    echo "❌ Swagger endpoint failed"
    kill $ASPIRE_PID 2>/dev/null || true
    exit 1
fi

# Clean shutdown
echo ""
echo "🛑 Shutting down infrastructure..."
kill $ASPIRE_PID 2>/dev/null || true
sleep 3

echo ""
echo "✅ Observability validation passed successfully!"
echo "🎉 LocalTesting infrastructure is properly configured and functional"
echo ""
echo "Key validations completed:"
echo "  ✓ Core projects build successfully"
echo "  ✓ LocalTesting Aspire infrastructure starts correctly"
echo "  ✓ WebAPI health endpoint responds"
echo "  ✓ Observability progress tracking works"
echo "  ✓ Prometheus metrics endpoint functions"
echo "  ✓ Swagger API documentation accessible"
echo ""
echo "The observability infrastructure is working properly!"
echo "Integration tests can be run with: dotnet test LocalTesting.IntegrationTests (may take 5+ minutes)"