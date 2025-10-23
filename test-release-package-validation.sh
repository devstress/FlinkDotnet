#!/bin/bash
# Test script for Release Package Validation workflow
# This simulates the GitHub Actions workflow locally
set -e

# Colors for output
GREEN='\033[0;32m'
RED='\033[0;31m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo "=========================================="
echo "Release Package Validation Test"
echo "=========================================="
echo ""

# Configuration
VERSION="${1:-99.99.99}"
DOCKER_IMAGE_NAME="flinkdotnet/jobgateway"
PACKAGES_DIR="/tmp/test-packages"
DOCKER_DIR="/tmp/test-docker"

echo -e "${BLUE}Test Configuration:${NC}"
echo "  Version: $VERSION"
echo "  Docker Image: $DOCKER_IMAGE_NAME:$VERSION"
echo "  Packages Dir: $PACKAGES_DIR"
echo "  Docker Dir: $DOCKER_DIR"
echo ""

# Check prerequisites
echo -e "${YELLOW}Checking prerequisites...${NC}"
command -v dotnet >/dev/null 2>&1 || { echo -e "${RED}✗ dotnet not found${NC}"; exit 1; }
command -v docker >/dev/null 2>&1 || { echo -e "${RED}✗ docker not found${NC}"; exit 1; }
command -v mvn >/dev/null 2>&1 || { echo -e "${RED}✗ maven not found${NC}"; exit 1; }
echo -e "${GREEN}✓ Prerequisites OK${NC}"
echo ""

# Clean up previous test artifacts
echo -e "${YELLOW}Step 1: Cleaning up previous test artifacts...${NC}"
rm -rf "$PACKAGES_DIR" "$DOCKER_DIR"
mkdir -p "$PACKAGES_DIR" "$DOCKER_DIR"
# Clean up any existing NuGet source
dotnet nuget remove source LocalTestFeed 2>/dev/null || true
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

# Step 3: Run unit tests
echo -e "${YELLOW}Step 3: Running unit tests...${NC}"
dotnet test FlinkDotNet/FlinkDotNet.sln --configuration Release --no-build --verbosity minimal
if [ $? -eq 0 ]; then
    echo -e "${GREEN}✓ Unit tests passed${NC}"
else
    echo -e "${RED}✗ Unit tests failed${NC}"
    exit 1
fi
echo ""

# Step 4: Create NuGet packages
echo -e "${YELLOW}Step 4: Creating NuGet packages...${NC}"
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

# Step 5: Build Docker image
echo -e "${YELLOW}Step 5: Building Docker image...${NC}"
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

# Step 6: Save Docker image to tarball
echo -e "${YELLOW}Step 6: Saving Docker image to tarball...${NC}"
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

# Step 7: Add local NuGet source
echo -e "${YELLOW}Step 7: Setting up local NuGet feed...${NC}"
dotnet nuget add source "$PACKAGES_DIR" --name LocalTestFeed
echo -e "${GREEN}✓ Local NuGet feed configured${NC}"
dotnet nuget list source
echo ""

# Step 8: Load Docker image
echo -e "${YELLOW}Step 8: Loading Docker image...${NC}"
# First remove existing image to simulate fresh load
docker rmi "$DOCKER_IMAGE_NAME:$VERSION" 2>/dev/null || true
docker rmi "$DOCKER_IMAGE_NAME:latest" 2>/dev/null || true
gunzip -c "$DOCKER_DIR/jobgateway-$VERSION.tar.gz" | docker load
docker tag "$DOCKER_IMAGE_NAME:$VERSION" "$DOCKER_IMAGE_NAME:latest"
echo -e "${GREEN}✓ Docker image loaded and tagged${NC}"
docker images | grep "$DOCKER_IMAGE_NAME"
echo ""

# Step 9: Run Pre-Release Validation Tests
echo -e "${YELLOW}Step 9: Running Pre-Release Validation Tests...${NC}"
echo -e "${BLUE}Testing with local NuGet packages and Docker image...${NC}"
cd ReleasePackagesTesting
dotnet test --configuration Release --verbosity normal
PRE_RELEASE_RESULT=$?
cd ..

if [ $PRE_RELEASE_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Pre-Release Validation Tests PASSED${NC}"
else
    echo -e "${RED}✗ Pre-Release Validation Tests FAILED${NC}"
    echo -e "${YELLOW}Showing Docker container logs for debugging...${NC}"
    docker ps -a
    docker logs $(docker ps -aq --filter "name=flink-job-gateway") 2>&1 | tail -100 || true
    exit 1
fi
echo ""

# Step 10: Clear NuGet cache (simulate fresh environment)
echo -e "${YELLOW}Step 10: Clearing NuGet cache for post-release simulation...${NC}"
dotnet nuget locals all --clear
echo -e "${GREEN}✓ NuGet cache cleared${NC}"
echo ""

# Step 11: Run Post-Release Validation Tests
echo -e "${YELLOW}Step 11: Running Post-Release Validation Tests...${NC}"
echo -e "${BLUE}Testing with local packages (simulating published packages)...${NC}"
cd ReleasePackagesTesting.Published
dotnet test --configuration Release --verbosity normal
POST_RELEASE_RESULT=$?
cd ..

if [ $POST_RELEASE_RESULT -eq 0 ]; then
    echo -e "${GREEN}✓ Post-Release Validation Tests PASSED${NC}"
else
    echo -e "${RED}✗ Post-Release Validation Tests FAILED${NC}"
    echo -e "${YELLOW}Showing Docker container logs for debugging...${NC}"
    docker ps -a
    docker logs $(docker ps -aq --filter "name=flink-job-gateway") 2>&1 | tail -100 || true
    exit 1
fi
echo ""

# Cleanup
echo -e "${YELLOW}Step 12: Cleaning up...${NC}"
docker ps -a | grep -E 'flink|kafka|redis|postgres|temporal' | awk '{print $1}' | xargs -r docker rm -f 2>/dev/null || true
dotnet nuget remove source LocalTestFeed 2>/dev/null || true
echo -e "${GREEN}✓ Cleanup complete${NC}"
echo ""

# Summary
echo "=========================================="
echo -e "${GREEN}✅ All Release Package Validations PASSED${NC}"
echo "=========================================="
echo ""
echo "Summary:"
echo "  ✅ Build and packaging successful"
echo "  ✅ Docker image created and validated"
echo "  ✅ Pre-release validation passed"
echo "  ✅ Post-release validation passed"
echo ""
echo "The release packages are ready for publishing!"
