#!/bin/bash
# Test script to validate observability test exit code propagation
# This script simulates infrastructure failure scenarios to verify test failures properly propagate

set -e

echo "🧪 TESTING: Observability Test Exit Code Propagation Validation"
echo "=============================================================="
echo ""

# Test configuration  
TEST_PROJECT="LocalTesting/LocalTesting.IntegrationTests/LocalTesting.IntegrationTests.csproj"
TEST_FILTER="Category=observability"
TEST_CONFIG="Release"

echo "📋 Test Configuration:"
echo "   Project: $TEST_PROJECT"
echo "   Filter: $TEST_FILTER"
echo "   Configuration: $TEST_CONFIG"
echo ""

# Function to run test and capture exit code
run_test_and_check_exit_code() {
    local test_name="$1"
    local expected_exit_code="$2"
    local additional_env="$3"
    
    echo "🔍 TEST: $test_name"
    echo "   Expected exit code: $expected_exit_code"
    
    # Set environment variables if provided
    if [ -n "$additional_env" ]; then
        echo "   Environment: $additional_env"
        export $additional_env
    fi
    
    # Run test and capture exit code (allow failure)
    set +e
    cd LocalTesting
    timeout 120 dotnet test "$TEST_PROJECT" \
        --configuration "$TEST_CONFIG" \
        --logger "console;verbosity=minimal" \
        --no-build \
        --filter "$TEST_FILTER" > test-output-validation.log 2>&1
    
    actual_exit_code=$?
    set -e
    
    echo "   Actual exit code: $actual_exit_code"
    
    # Check if exit code matches expectation
    if [ "$actual_exit_code" -eq "$expected_exit_code" ]; then
        echo "   ✅ PASS: Exit code matches expectation"
    else
        echo "   ❌ FAIL: Exit code mismatch!"
        echo "   Expected: $expected_exit_code"  
        echo "   Actual: $actual_exit_code"
        echo ""
        echo "📋 Test output (last 20 lines):"
        tail -20 test-output-validation.log
        return 1
    fi
    
    echo ""
    cd ..
}

# Test 1: Normal execution (should succeed if infrastructure is available)
echo "🧪 Test 1: Normal Execution Test"
echo "================================="
run_test_and_check_exit_code "Normal infrastructure startup" 0 ""

# Test 2: Forced failure test by creating a scenario that will timeout quickly
echo "🧪 Test 2: Infrastructure Timeout Simulation"  
echo "============================================="
echo "⚠️ This test simulates infrastructure timeout by setting a very short timeout"
echo "⚠️ Expected behavior: Test should fail with non-zero exit code due to timeout"

# Create a modified test file that has a very short timeout for testing failure propagation
# We'll temporarily modify the timeout in the source code to force a failure scenario

# Backup original file
cp "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs" \
   "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs.backup"

# Modify timeout to be extremely short to force failure
sed -i 's/TimeSpan.FromMinutes(10)/TimeSpan.FromSeconds(1)/g' \
    "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs"
sed -i 's/TimeSpan.FromMinutes(2)/TimeSpan.FromSeconds(1)/g' \
    "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs"

# Rebuild with forced timeout
echo "🔨 Rebuilding with forced timeout for failure testing..."
cd LocalTesting
dotnet build LocalTesting.sln --configuration Release --verbosity quiet
cd ..

# Run test expecting failure
run_test_and_check_exit_code "Forced infrastructure timeout" 1 ""

# Restore original file
mv "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs.backup" \
   "LocalTesting/LocalTesting.IntegrationTests/StepDefinitions/ObservabilityMetricsSteps.cs"

# Rebuild with original code
echo "🔨 Restoring original code and rebuilding..."
cd LocalTesting  
dotnet build LocalTesting.sln --configuration Release --verbosity quiet
cd ..

echo ""
echo "🎉 VALIDATION COMPLETE"
echo "======================"
echo "✅ Test exit code propagation validation completed successfully"
echo "✅ Infrastructure timeout failures properly return non-zero exit codes"
echo "✅ GitHub workflow should now properly detect test failures"
echo ""
echo "📋 Summary:"
echo "   - Normal test execution: Expected to pass (exit code 0)"
echo "   - Infrastructure timeout: Expected to fail (exit code 1)" 
echo "   - Test failure propagation: Validated ✅"
echo ""
echo "🚀 Ready for GitHub Actions deployment"