#!/bin/bash
# Run SonarQube analysis locally without needing to visit SonarCloud
# This script runs a local SonarQube analysis on the FlinkDotNet solution.
# Results are saved to the .sonarqube directory for offline review.
#
# Usage:
#   ./run-sonar-analysis.sh                    # Local analysis only
#   ./run-sonar-analysis.sh <sonar-token>      # Analysis with upload to SonarCloud
#   ./run-sonar-analysis.sh --skip-tests       # Skip tests and coverage

set -e

SONAR_TOKEN="${1:-}"
SKIP_TESTS=false

if [ "$1" = "--skip-tests" ]; then
    SKIP_TESTS=true
    SONAR_TOKEN="${2:-}"
fi

echo "=================================================="
echo "  FlinkDotNet Local SonarQube Analysis"
echo "=================================================="
echo ""

# Check if dotnet-sonarscanner is installed
if ! command -v dotnet-sonarscanner &> /dev/null; then
    echo "Installing dotnet-sonarscanner..."
    dotnet tool install --global dotnet-sonarscanner || \
    dotnet tool update --global dotnet-sonarscanner
fi

echo "✓ SonarScanner installed"

# Clean previous build artifacts
echo ""
echo "Cleaning previous builds..."
dotnet clean FlinkDotNet.sln --configuration Release -v quiet

# Prepare SonarScanner arguments
BEGIN_ARGS=(
    "begin"
    "/k:devstress_flinkdotnet"
    "/o:devstress"
    "/d:sonar.host.url=https://sonarcloud.io"
)

if [ -n "$SONAR_TOKEN" ]; then
    echo "✓ Using SonarCloud token for upload"
    BEGIN_ARGS+=("/d:sonar.token=$SONAR_TOKEN")
else
    echo "⚠ No SonarCloud token provided - local analysis only"
    echo "  Results will be saved locally but not uploaded to SonarCloud"
fi

# Add coverage settings
if [ "$SKIP_TESTS" = false ]; then
    BEGIN_ARGS+=(
        "/d:sonar.cs.opencover.reportsPaths=**/TestResults/**/coverage.opencover.xml"
        "/d:sonar.cs.vscoveragexml.reportsPaths=**/TestResults/**/coverage.cobertura.xml"
    )
fi

# Begin SonarScanner
echo ""
echo "Starting SonarQube analysis..."
dotnet-sonarscanner "${BEGIN_ARGS[@]}"

# Build the solution
echo ""
echo "Building FlinkDotNet solution..."
dotnet build FlinkDotNet.sln --configuration Release

echo "✓ Build successful"

# Run tests with coverage
if [ "$SKIP_TESTS" = false ]; then
    echo ""
    echo "Running tests with coverage..."
    dotnet test FlinkDotNet.sln \
        --configuration Release \
        --no-build \
        --collect:"XPlat Code Coverage" \
        --settings ../coverlet.runsettings \
        --logger "console;verbosity=minimal" || echo "⚠ Some tests failed, but continuing with analysis..."
    
    echo "✓ Tests completed"
fi

# End SonarScanner
echo ""
echo "Completing SonarQube analysis..."
END_ARGS=("end")
if [ -n "$SONAR_TOKEN" ]; then
    END_ARGS+=("/d:sonar.token=$SONAR_TOKEN")
fi

dotnet-sonarscanner "${END_ARGS[@]}"

echo ""
echo "=================================================="
echo "  Analysis Complete!"
echo "=================================================="
echo ""

if [ -n "$SONAR_TOKEN" ]; then
    echo "Results uploaded to: https://sonarcloud.io/dashboard?id=devstress_flinkdotnet"
else
    echo "Local analysis results saved to: .sonarqube/"
    echo ""
    echo "To view issues locally, check the .sonarqube directory"
    echo "To upload results to SonarCloud, rerun with token: ./run-sonar-analysis.sh <token>"
fi

echo ""
