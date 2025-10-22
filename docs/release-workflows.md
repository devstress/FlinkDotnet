# Release Workflows Documentation

This document describes the GitHub Actions workflows for creating and publishing FlinkDotNet releases.

## Overview

There are 4 manual workflows for managing FlinkDotNet releases:

1. **Release - Major Version** (`release-major.yml`) - For breaking changes
2. **Release - Minor Version** (`release-minor.yml`) - For new features
3. **Release - Patch Version** (`release-patch.yml`) - For bug fixes
4. **Retry Publish** (`retry-publish.yml`) - For retrying failed publishes

## Workflows

### 1. Release - Major Version

**When to use**: Breaking changes, major refactoring, or significant architectural updates.

**What it does**:
- Calculates the new version by bumping the major number (e.g., 1.0.0 → 2.0.0)
- Updates the `<PackageVersion>` in project files (.csproj) to the new version
- Commits the version changes to the repository
- Creates a git tag (e.g., `v2.0.0`)
- Builds and tests the solution
- Creates NuGet packages for:
  - FlinkDotNet.Common
  - FlinkDotNet.DataStream
  - Flink.JobBuilder
- Builds Docker image for JobGateway with the new version tag
- Creates a GitHub release with all artifacts
- Publishes NuGet packages to NuGet.org
- Publishes Docker image to Docker Hub (with version tag and `latest`)

**How to trigger**:
1. Go to Actions tab in GitHub
2. Select "Release - Major Version"
3. Click "Run workflow"
4. Enter the current version (e.g., `1.0.0`)
5. Click "Run workflow"

### 2. Release - Minor Version

**When to use**: New features that are backward compatible.

**What it does**:
- Calculates the new version by bumping the minor number (e.g., 1.0.0 → 1.1.0)
- Updates the `<PackageVersion>` in project files (.csproj) to the new version
- Commits the version changes to the repository
- Creates a git tag (e.g., `v1.1.0`)
- Same build and publish steps as major version release
- Creates a "Feature Release" with appropriate release notes

**How to trigger**:
1. Go to Actions tab in GitHub
2. Select "Release - Minor Version"
3. Click "Run workflow"
4. Enter the current version (e.g., `1.0.0`)
5. Click "Run workflow"

### 3. Release - Patch Version

**When to use**: Bug fixes and minor improvements.

**What it does**:
- Calculates the new version by bumping the patch number (e.g., 1.0.0 → 1.0.1)
- Updates the `<PackageVersion>` in project files (.csproj) to the new version
- Commits the version changes to the repository
- Creates a git tag (e.g., `v1.0.1`)
- Same build and publish steps as major version release
- Creates a "Bug Fix Release" with appropriate release notes

**How to trigger**:
1. Go to Actions tab in GitHub
2. Select "Release - Patch Version"
3. Click "Run workflow"
4. Enter the current version (e.g., `1.0.0`)
5. Click "Run workflow"

### 4. Retry Publish (Bonus Workflow)

**When to use**: When publishing fails after the release was created successfully.

**What it does**:
- Downloads artifacts from an existing GitHub release
- Republishes NuGet packages to NuGet.org
- Republishes Docker image to Docker Hub

**How to trigger**:
1. Go to Actions tab in GitHub
2. Select "Retry Publish (NuGet & Docker)"
3. Click "Run workflow"
4. Enter the version to publish (e.g., `1.0.0`)
5. Enter the release tag (e.g., `v1.0.0`)
6. Click "Run workflow"

## Required Secrets

The workflows require the following repository secrets to be configured:

### NuGet Publishing
- **NUGET_API_KEY**: API key for publishing to NuGet.org
  - Get from: https://www.nuget.org/account/apikeys
  - Permissions needed: Push packages

### Docker Publishing
- **DOCKER_USERNAME**: Docker Hub username
- **DOCKER_PASSWORD**: Docker Hub password or access token
  - Get from: https://hub.docker.com/settings/security

### How to add secrets:
1. Go to repository Settings
2. Navigate to Secrets and variables → Actions
3. Click "New repository secret"
4. Add each required secret

## Release Artifacts

Each release includes:

### NuGet Packages
- `FlinkDotNet.Common.{version}.nupkg` - Core common components
- `FlinkDotNet.DataStream.{version}.nupkg` - DataStream API
- `Flink.JobBuilder.{version}.nupkg` - Job builder with JSON IR

### Docker Image
- `jobgateway-{version}.tar.gz` - Docker image tarball for JobGateway
- Published to Docker Hub as:
  - `flinkdotnet/jobgateway:{version}`
  - `flinkdotnet/jobgateway:latest`

## Workflow Steps

All version bump workflows follow these steps:

1. **Calculate Version**: Determines the new version number based on input
2. **Update Version**: Updates `<PackageVersion>` in .csproj files, commits changes, and creates git tag
3. **Build and Package**: Builds solution, runs tests, creates NuGet packages with the new version
4. **Build Docker Image**: Builds and saves Docker image for JobGateway with the new version tag
5. **Create Release**: Uses GitHub CLI to create GitHub release with all artifacts
6. **Publish Packages**: Publishes NuGet packages to NuGet.org and Docker image to Docker Hub

**Important**: The workflow automatically commits the version changes to your repository and creates a git tag. This ensures that the source code version stays in sync with the released version.

## Version Bumping Examples

| Current Version | Major | Minor | Patch |
|----------------|-------|-------|-------|
| 1.0.0          | 2.0.0 | 1.1.0 | 1.0.1 |
| 1.2.3          | 2.0.0 | 1.3.0 | 1.2.4 |
| 2.5.7          | 3.0.0 | 2.6.0 | 2.5.8 |

## Troubleshooting

### NuGet Publish Fails
If NuGet publish fails but release was created:
1. Use the "Retry Publish" workflow
2. Enter the version and release tag
3. The workflow will download artifacts from the release and retry publishing

### Docker Push Fails
If Docker push fails but release was created:
1. Verify Docker Hub credentials are correct
2. Use the "Retry Publish" workflow
3. The workflow will retry both NuGet and Docker publishing

### Build Fails
If the build fails:
1. Fix the build issues locally
2. Commit and push fixes
3. Delete the failed release if one was created
4. Trigger the workflow again

## Best Practices

1. **Test Before Release**: Always ensure tests pass before triggering a release
2. **Version Tracking**: Keep track of the current version in project documentation
3. **Release Notes**: The workflows generate basic release notes, but consider editing them after creation to add more details
4. **Breaking Changes**: Use major version bumps for breaking changes
5. **Semantic Versioning**: Follow semantic versioning principles (semver.org)

## Manual Installation from Release

### NuGet Packages
```bash
dotnet add package FlinkDotNet.Common --version 1.0.0
dotnet add package FlinkDotNet.DataStream --version 1.0.0
```

### Docker Image
```bash
# Download from release
wget https://github.com/devstress/FlinkDotnet/releases/download/v1.0.0/jobgateway-1.0.0.tar.gz

# Load image
docker load < jobgateway-1.0.0.tar.gz

# Run container
docker run -p 8080:8080 flinkdotnet/jobgateway:1.0.0
```

### From Docker Hub
```bash
docker pull flinkdotnet/jobgateway:1.0.0
docker run -p 8080:8080 flinkdotnet/jobgateway:1.0.0
```

## Additional Notes

- All workflows are triggered manually via `workflow_dispatch`
- Workflows run on `ubuntu-latest` runners
- .NET 9.0.x is used for building
- Java 17 is required for Maven build
- Docker Buildx is used for multi-platform support
- Workflows include retry logic with `--skip-duplicate` for NuGet
