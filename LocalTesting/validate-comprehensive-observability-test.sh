#!/bin/bash

# validate-comprehensive-observability-test.sh
# Validation script for simplified observability test with single comprehensive scenario
# Requires .NET 9.0 SDK with Aspire workload installed

set -e

echo "🚀 Validating Single Comprehensive Observability Test with Aspire Testing Framework"
echo "==================================================================================="

# Check .NET version
echo "📋 Checking .NET version..."
DOTNET_VERSION=$(dotnet --version)
echo "   .NET SDK Version: $DOTNET_VERSION"

if [[ ! "$DOTNET_VERSION" =~ ^9\. ]]; then
    echo "❌ ERROR: This requires .NET 9.0 SDK, but found $DOTNET_VERSION"
    echo "   Install .NET 9.0 SDK from: https://dotnet.microsoft.com/download/dotnet/9.0"
    exit 1
fi

# Check if Aspire workload is installed
echo "📋 Checking Aspire workload..."
if dotnet workload list | grep -q "aspire"; then
    echo "✅ Aspire workload is installed"
else
    echo "⚠️ Installing Aspire workload..."
    dotnet workload install aspire
    echo "✅ Aspire workload installed"
fi

# Build LocalTesting solution
echo "🔨 Building LocalTesting solution..."
dotnet build LocalTesting/LocalTesting.sln --configuration Release --verbosity minimal
echo "✅ Build completed successfully"

# Run the single comprehensive observability test
echo "🧪 Running single comprehensive observability test with Aspire testing framework..."
echo "   This will test the complete Kafka → Flink → Temporal pipeline with 1M messages"
echo "   Infrastructure is automatically managed - no manual startup required"

# Run the tests with filters and detailed output
dotnet test LocalTesting/LocalTesting.IntegrationTests \
    --filter "Category=observability" \
    --configuration Release \
    --logger "console;verbosity=detailed" \
    --no-build \
    --results-directory ./TestResults \
    --collect:"XPlat Code Coverage"

echo ""
echo "🎯 Test Results Summary:"
echo "==================================================================================="

# Check if tests passed
if [ $? -eq 0 ]; then
    echo "✅ COMPREHENSIVE OBSERVABILITY TEST PASSED!"
    echo "✅ Complete Kafka → Flink → Temporal pipeline validated"
    echo "✅ 1M messages processed successfully with metrics collection"
    echo "✅ All components (Kafka, Flink, Temporal) metrics validated"
    echo ""
    echo "🔍 Key Validations Completed:"
    echo "   • Kafka producer messages per second > 0"
    echo "   • Flink job processing rate metrics recorded"
    echo "   • Temporal workflow execution rate metrics recorded"
    echo "   • End-to-end flow rate metrics show total throughput"
    echo "   • Prometheus successfully scrapes all observability metrics"
else
    echo "❌ COMPREHENSIVE TEST FAILED - Check the detailed output above"
    echo "🔧 Common troubleshooting steps:"
    echo "   • Ensure Docker Desktop is running with sufficient resources (8GB+ RAM recommended)"
    echo "   • Check that no other processes are using required ports"
    echo "   • Verify Aspire workload is properly installed"
    echo "   • Run 'docker system prune -f' to clean up any orphaned containers"
    echo "   • For 1M messages test, ensure sufficient disk space and memory"
    exit 1
fi

echo ""
echo "📊 Coverage Report:"
if [ -d "./TestResults" ]; then
    echo "   Test results saved to: ./TestResults/"
    echo "   Coverage data available for analysis"
else
    echo "   No coverage data generated"
fi

echo ""
echo "🎉 Single Comprehensive Observability Test Validation Complete!"
echo "   Reduced from 10 scenarios to 1 comprehensive end-to-end test"
echo "   Tests complete Kafka → Flink → Temporal pipeline with 1M messages"
echo "   All observability metrics validated in single efficient test scenario"