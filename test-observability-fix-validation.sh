#!/bin/bash

# Test script to verify observability test failure detection improvements
# This will run a short version of the observability test to validate the fixes

echo "🧪 Testing Observability Test Failure Detection Fix"
echo "================================================="

cd "$(dirname "$0")/LocalTesting"

# Set shorter timeout for testing
export TESTING_MODE="true"

echo "📊 Running observability test with improved failure detection..."
echo "   - Infrastructure health checks enabled"
echo "   - Connection failure detection enhanced" 
echo "   - Results file creation requires all validation flags"
echo ""

# Run the test with a 10-minute timeout
timeout 600 dotnet test LocalTesting.IntegrationTests \
  --configuration Release \
  --logger "console;verbosity=detailed" \
  --filter "FullyQualifiedName~Simple" \
  2>&1 | tee test-fix-output.log

TEST_EXIT_CODE=$?

echo ""
echo "📋 Test Completion Analysis"
echo "=========================="
echo "Exit Code: $TEST_EXIT_CODE"

# Check if results file was created
if [ -f "Bin/observability-test-result.txt" ]; then
    echo "✅ Results file created: Test passed all validations including infrastructure health"
    echo "📊 File size: $(stat -c%s "Bin/observability-test-result.txt") bytes"
    echo ""
    echo "🔍 Validation Success Indicators in Output:"
    grep -i "validation passed\|infrastructure health\|all critical infrastructure" test-fix-output.log | tail -5
else
    echo "❌ Results file NOT created: Test failed validation or infrastructure health check"
    echo "   This is the correct behavior when infrastructure issues occur"
    echo ""
    echo "🔍 Failure Indicators in Output:"
    grep -i "infrastructure failure\|connection reset\|critical error\|validation failure" test-fix-output.log | tail -5
fi

# Check for connection reset errors
if grep -q "Connection reset by peer" test-fix-output.log; then
    echo ""
    echo "🔍 Connection Issues Detected:"
    echo "   The test detected 'Connection reset by peer' errors"
    
    if [ -f "Bin/observability-test-result.txt" ]; then
        echo "❌ BUG: Results file was created despite connection errors - fix needed"
        exit 1
    else
        echo "✅ CORRECT: Results file was NOT created due to connection errors"
        echo "   The fix is working - GitHub workflow will fail as expected"
    fi
fi

echo ""
echo "📝 Summary:"
echo "   Exit Code: $TEST_EXIT_CODE"
echo "   Results File: $([ -f "Bin/observability-test-result.txt" ] && echo "Created ✅" || echo "Not Created ❌")"
echo "   Expected Behavior: Results file should only be created when ALL infrastructure is healthy"
echo ""

exit $TEST_EXIT_CODE