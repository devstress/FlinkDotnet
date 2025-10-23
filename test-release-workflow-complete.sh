#!/bin/bash
# Local Release Workflow Testing Script
# This script simulates the release workflow locally with fake NuGet repo and Docker registry
set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "=========================================="
echo "Local Release Workflow Testing"
echo "=========================================="
echo ""

# Configuration
VERSION="${1:-99.99.99}"  # Use 99.99.99 as default test version
DOCKER_IMAGE_NAME="flinkdotnet/jobgateway"
PACKAGES_DIR="/tmp/release-packages"
DOCKER_DIR="/tmp/release-docker"

echo -e "${BLUE}Test Configuration:${NC}"
echo "  Version: $VERSION"
echo "  Docker Image: $DOCKER_IMAGE_NAME:$VERSION"
echo "  Packages Dir: $PACKAGES_DIR"
echo "  Docker Dir: $DOCKER_DIR"
echo ""

# Clean up previous test artifacts
echo -e "${YELLOW}Step 1: Cleaning up previous test artifacts...${NC}"
rm -rf "$PACKAGES_DIR" "$DOCKER_DIR"
mkdir -p "$PACKAGES_DIR" "$DOCKER_DIR"
echo -e "${GREEN}✓ Cleanup complete${NC}"
echo ""

# Step 2: Build FlinkDotNet solution
echo -e "${YELLOW}Step 2: Building FlinkDotNet solution...${NC}"
dotnet restore FlinkDotNet/FlinkDotNet.sln
dotnet build FlinkDotNet/FlinkDotNet.sln --configuration Release --no-restore
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ FlinkDotNet build succeeded${NC}"
else
    echo -e "${RED}✗ FlinkDotNet build failed${NC}"
    exit 1
fi
echo ""

# Step 3: Create NuGet packages
echo -e "${YELLOW}Step 3: Creating NuGet packages...${NC}"
dotnet pack FlinkDotNet/FlinkDotNet.DataStream/FlinkDotNet.DataStream.csproj \
    --configuration Release \
    --output "$PACKAGES_DIR" \
    -p:PackageVersion=$VERSION
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Package created${NC}"
    ls -lh "$PACKAGES_DIR"/*.nupkg
else
    echo -e "${RED}✗ Package creation failed${NC}"
    exit 1
fi
echo ""

# Step 4: Build Docker image
echo -e "${YELLOW}Step 4: Building Docker image...${NC}"
docker build \
    -f FlinkDotNet/FlinkDotNet.JobGateway/Dockerfile \
    -t "$DOCKER_IMAGE_NAME:$VERSION" \
    -t "$DOCKER_IMAGE_NAME:latest" \
    .
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker image built${NC}"
    docker images | grep "$DOCKER_IMAGE_NAME"
else
    echo -e "${RED}✗ Docker build failed${NC}"
    exit 1
fi
echo ""

# Step 5: Save Docker image to tarball
echo -e "${YELLOW}Step 5: Saving Docker image to tarball...${NC}"
docker save "$DOCKER_IMAGE_NAME:$VERSION" -o "$DOCKER_DIR/jobgateway-$VERSION.tar"
gzip "$DOCKER_DIR/jobgateway-$VERSION.tar"
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Docker image saved${NC}"
    ls -lh "$DOCKER_DIR"/*.tar.gz
else
    echo -e "${RED}✗ Docker save failed${NC}"
    exit 1
fi
echo ""

# Step 6: Add local NuGet source
echo -e "${YELLOW}Step 6: Setting up local NuGet feed...${NC}"
dotnet nuget remove source LocalReleaseTestFeed 2>/dev/null || true
dotnet nuget add source "$PACKAGES_DIR" --name LocalReleaseTestFeed
echo -e "${GREEN}✓ Local NuGet feed configured${NC}"
dotnet nuget list source
echo ""

# Step 7: Load Docker image (simulating workflow download)
echo -e "${YELLOW}Step 7: Loading Docker image (simulating workflow)...${NC}"
# First remove existing image to simulate fresh load
docker rmi "$DOCKER_IMAGE_NAME:$VERSION" 2>/dev/null || true
docker rmi "$DOCKER_IMAGE_NAME:latest" 2>/dev/null || true
gunzip -c "$DOCKER_DIR/jobgateway-$VERSION.tar.gz" | docker load
docker tag "$DOCKER_IMAGE_NAME:$VERSION" "$DOCKER_IMAGE_NAME:latest"
echo -e "${GREEN}✓ Docker image loaded and tagged${NC}"
docker images | grep "$DOCKER_IMAGE_NAME"
echo ""

# Step 8: Run Pre-Release Validation Tests
echo -e "${YELLOW}Step 8: Running Pre-Release Validation Tests...${NC}"
echo -e "${BLUE}Testing ReleasePackagesTesting solution with local artifacts...${NC}"
cd ReleasePackagesTesting
dotnet test --configuration Release --verbosity normal
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Pre-Release Validation Tests PASSED${NC}"
else
    echo -e "${RED}✗ Pre-Release Validation Tests FAILED${NC}"
    cd ..
    exit 1
fi
cd ..
echo ""

# Step 9: Simulate publishing to fake registries
echo -e "${YELLOW}Step 9: Simulating package publication...${NC}"
echo -e "${BLUE}In real workflow, packages would be published to:${NC}"
echo "  - NuGet.org: FlinkDotnet $VERSION"
echo "  - Docker Hub: $DOCKER_IMAGE_NAME:$VERSION"
echo -e "${YELLOW}For local testing, we skip actual publishing${NC}"
echo ""

# Step 10: Clear NuGet cache (simulate fresh environment for post-release)
echo -e "${YELLOW}Step 10: Clearing NuGet cache for post-release simulation...${NC}"
dotnet nuget locals all --clear
echo -e "${GREEN}✓ NuGet cache cleared${NC}"
echo ""

# Step 11: Run Post-Release Validation Tests (simulated)
echo -e "${YELLOW}Step 11: Simulating Post-Release Validation Tests...${NC}"
echo -e "${BLUE}Testing ReleasePackagesTesting.Published solution...${NC}"
echo -e "${YELLOW}Note: This will use the local feed as a substitute for NuGet.org${NC}"
cd ReleasePackagesTesting.Published
dotnet test --configuration Release --verbosity normal
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Post-Release Validation Tests PASSED${NC}"
else
    echo -e "${RED}✗ Post-Release Validation Tests FAILED${NC}"
    cd ..
    exit 1
fi
cd ..
echo ""

# Cleanup
echo -e "${YELLOW}Step 12: Cleanup...${NC}"
dotnet nuget remove source LocalReleaseTestFeed 2>/dev/null || true
echo -e "${GREEN}✓ Removed local NuGet feed${NC}"
echo ""

echo "=========================================="
echo -e "${GREEN}✓ All release workflow tests passed!${NC}"
echo "=========================================="
echo ""
echo "Summary:"
echo "  - Built and packaged FlinkDotnet $VERSION"
echo "  - Created Docker image $DOCKER_IMAGE_NAME:$VERSION"
echo "  - Pre-release validation: PASSED"
echo "  - Post-release validation: PASSED (simulated)"
echo ""
echo -e "${BLUE}The release workflow is working correctly!${NC}"
