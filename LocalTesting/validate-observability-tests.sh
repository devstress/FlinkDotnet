#!/bin/bash
set -e

echo "🧪 LocalTesting Observability Test Validation Script"
echo "====================================================="

# Verify .NET 9.0 SDK
echo "🔍 Checking .NET version..."
DOTNET_VERSION=$(dotnet --version)
echo "   .NET SDK: $DOTNET_VERSION"

if [[ ! $DOTNET_VERSION == 9.* ]]; then
    echo "❌ This script requires .NET 9.0 SDK"
    echo "   Please install .NET 9.0 from: https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

echo "✅ .NET 9.0 SDK detected"

# Build LocalTesting solution
echo ""
echo "🔨 Building LocalTesting solution..."
cd "$(dirname "$0")"
dotnet build LocalTesting.sln --configuration Release --verbosity minimal

if [ $? -eq 0 ]; then
    echo "✅ LocalTesting solution built successfully"
else
    echo "❌ Build failed"
    exit 1
fi

# Run observability tests
echo ""
echo "🧪 Running observability integration tests..."
dotnet test LocalTesting.IntegrationTests --configuration Release --verbosity normal --filter "Category=observability"

if [ $? -eq 0 ]; then
    echo ""
    echo "✅ Observability tests passed successfully!"
    echo "🎉 LocalTesting infrastructure is properly integrated with observability testing"
    echo ""
    echo "Key validations completed:"
    echo "  ✓ LocalTesting Aspire infrastructure starts correctly"
    echo "  ✓ ObservabilityMetricsSteps connects to LocalTesting WebAPI"
    echo "  ✓ Flow metrics recording and validation works"
    echo "  ✓ BDD scenarios execute against real infrastructure"
    echo ""
    echo "The observability GitHub workflow should now pass!"
else
    echo "❌ Tests failed - check the output above for details"
    exit 1
fi