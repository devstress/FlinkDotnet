#!/bin/bash
set -e

echo "=========================================="
echo "Local Release Workflow Simulation"
echo "=========================================="
echo ""

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Build main FlinkDotNet solution
echo -e "${YELLOW}Step 1: Building FlinkDotNet solution...${NC}"
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ FlinkDotNet build succeeded${NC}"
else
    echo -e "${RED}✗ FlinkDotNet build failed${NC}"
    exit 1
fi
echo ""

# Step 2: Run unit tests
echo -e "${YELLOW}Step 2: Running unit tests...${NC}"
dotnet test FlinkDotNet/FlinkDotNet.sln --configuration Release --no-build --verbosity minimal
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Unit tests passed${NC}"
else
    echo -e "${RED}✗ Unit tests failed${NC}"
    exit 1
fi
echo ""

# Step 3: Create NuGet package
echo -e "${YELLOW}Step 3: Creating NuGet package...${NC}"
rm -rf /tmp/release-test-packages
dotnet pack FlinkDotNet/FlinkDotNet.DataStream/FlinkDotNet.DataStream.csproj \
    --configuration Release \
    --output /tmp/release-test-packages
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Package created${NC}"
    ls -lh /tmp/release-test-packages/FlinkDotnet.*.nupkg
else
    echo -e "${RED}✗ Package creation failed${NC}"
    exit 1
fi
echo ""

# Step 4: Add local NuGet source
echo -e "${YELLOW}Step 4: Setting up local NuGet feed...${NC}"
dotnet nuget remove source LocalReleaseTestFeed 2>/dev/null || true
dotnet nuget add source /tmp/release-test-packages --name LocalReleaseTestFeed
echo -e "${GREEN}✓ Local NuGet feed configured${NC}"
echo ""

# Step 5: Clear NuGet cache
echo -e "${YELLOW}Step 5: Clearing NuGet cache...${NC}"
dotnet nuget locals all --clear
echo -e "${GREEN}✓ NuGet cache cleared${NC}"
echo ""

# Step 6: Test ReleasePackagesTesting build WITHOUT UseReleasePackages (project references)
echo -e "${YELLOW}Step 6: Testing ReleasePackagesTesting build with project references...${NC}"
dotnet build ReleasePackagesTesting/ReleasePackagesTesting.sln --configuration Release
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ ReleasePackagesTesting build (project refs) succeeded${NC}"
else
    echo -e "${RED}✗ ReleasePackagesTesting build (project refs) failed${NC}"
    exit 1
fi
echo ""

# Step 7: Test ReleasePackagesTesting build WITH UseReleasePackages (package references)
echo -e "${YELLOW}Step 7: Testing ReleasePackagesTesting build with NuGet package...${NC}"
dotnet build ReleasePackagesTesting/ReleasePackagesTesting.sln \
    --configuration Release \
    -p:UseReleasePackages=true
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ ReleasePackagesTesting build (package refs) succeeded${NC}"
else
    echo -e "${RED}✗ ReleasePackagesTesting build (package refs) failed${NC}"
    exit 1
fi
echo ""

# Step 8: Verify package contents
echo -e "${YELLOW}Step 8: Verifying package contents...${NC}"
unzip -l /tmp/release-test-packages/FlinkDotnet.*.nupkg | grep "\.dll"
EXPECTED_DLLS=("FlinkDotNet.DataStream.dll" "FlinkDotNet.Common.dll" "Flink.JobBuilder.dll" "FlinkDotNet.JobGateway.dll")
for dll in "${EXPECTED_DLLS[@]}"; do
    if unzip -l /tmp/release-test-packages/FlinkDotnet.*.nupkg | grep -q "$dll"; then
        echo -e "${GREEN}✓ Found $dll${NC}"
    else
        echo -e "${RED}✗ Missing $dll${NC}"
        exit 1
    fi
done
echo ""

# Cleanup
echo -e "${YELLOW}Cleanup: Removing local NuGet feed...${NC}"
dotnet nuget remove source LocalReleaseTestFeed
echo -e "${GREEN}✓ Cleanup complete${NC}"
echo ""

echo "=========================================="
echo -e "${GREEN}✓ All release workflow tests passed!${NC}"
echo "=========================================="
