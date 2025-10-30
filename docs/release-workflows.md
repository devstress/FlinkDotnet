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
- Creates NuGet package:
  - FlinkDotnet (unified package with DataStream API, Common components, and JobBuilder)
- Builds Docker image for JobGateway with the new version tag
- Creates a GitHub release with all artifacts
- Publishes NuGet package to NuGet.org
- Publishes Docker image to Docker Hub (with version tag and `latest`)

**How to trigger**:
1. Go to Actions tab in GitHub
2. Select "Release - Major Version"
3. Click "Run workflow"
4. Click "Run workflow" to confirm

**Note**: The workflow automatically detects the latest release version from git tags. If no previous releases exist, it starts from 1.0.0. For example:
- First major release (no tags): Creates v1.0.0
- Subsequent major releases: Bumps major version (e.g., v1.x.x → v2.0.0)

### 2. Release - Minor Version

**When to use**: New features that are backward compatible.

**What it does**:
- Automatically detects the latest release version from git tags
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
4. Click "Run workflow" to confirm

### 3. Release - Patch Version

**When to use**: Bug fixes and minor improvements.

**What it does**:
- Automatically detects the latest release version from git tags
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
4. Click "Run workflow" to confirm

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

## Required Configuration

The workflows require the following configuration:

### NuGet Publishing (Trusted Publishing)
The workflows use **NuGet Trusted Publishing** via OpenID Connect (OIDC), which eliminates the need for long-lived API keys.

**Setup Steps:**

**Configure Trusted Publishing on NuGet.org**
1. Go to https://www.nuget.org/
2. Navigate to your account settings
3. Select the package you want to configure
4. Go to "Trusted Publishers" section
5. Add a new trusted publisher with:
   - **Source**: GitHub Actions
   - **Owner**: devstress
   - **Repository**: FlinkDotnet
   - **Workflow**: The workflow file name (e.g., `release-major.yml`, `release-minor.yml`, `release-patch.yml`, `retry-publish.yml`)

**Benefits:**
- ✅ No long-lived API keys to manage or rotate
- ✅ More secure - tokens are short-lived and scoped to specific workflows
- ✅ Better audit trail
- ✅ Automatic authentication via GitHub OIDC

### Docker Publishing
The workflows require Docker Hub credentials to publish container images.

**Required Secrets:**
- **DOCKER_USERNAME**: Docker Hub username
- **DOCKER_PASSWORD**: Docker Hub password or access token
  - Get from: https://hub.docker.com/settings/security

**How to add Docker secrets:**
1. Go to repository Settings
2. Navigate to Secrets and variables → Actions
3. Click "New repository secret"
4. Add each required secret

## Required Secrets Summary

The release workflows require the following GitHub repository secrets:

| Secret Name | Description | Where to Get |
|------------|-------------|--------------|
| `DOCKER_USERNAME` | Docker Hub username | Your Docker Hub account username |
| `DOCKER_PASSWORD` | Docker Hub password or token | Docker Hub Settings → Security → Access Tokens |

**Note:** The NuGet.org username is hardcoded in the workflows as `DarrenDatBui` - no secret configuration needed.

## Release Artifacts

Each release includes:

### NuGet Packages
- `FlinkDotnet.{version}.nupkg` - Complete unified package with DataStream API, Kafka connectors, common components, and job builder

### Docker Image
- `jobgateway-{version}.tar.gz` - Docker image tarball for JobGateway
- Published to Docker Hub as:
  - `devstress/flinkdotnet:{version}`
  - `devstress/flinkdotnet:latest`

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

**If NuGet publish fails due to authentication:**
1. Verify Trusted Publishing is configured correctly on NuGet.org
2. Check that the workflow name matches exactly in the NuGet Trusted Publisher configuration
3. Ensure the repository owner and name are correct in the configuration
4. Verify that the workflow has `id-token: write` permission

**If NuGet publish fails but release was created:**
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
dotnet add package FlinkDotnet --version 1.0.0
```

### Docker Image
```bash
# Download from release
wget https://github.com/devstress/FlinkDotnet/releases/download/v1.0.0/jobgateway-1.0.0.tar.gz

# Load image
docker load < jobgateway-1.0.0.tar.gz

# Run container
docker run -p 8086:8086 devstress/flinkdotnet:1.0.0
```

### From Docker Hub
```bash
docker pull devstress/flinkdotnet:1.0.0
docker run -p 8086:8086 devstress/flinkdotnet:1.0.0
```

## Additional Notes

- All workflows are triggered manually via `workflow_dispatch`
- Workflows run on `ubuntu-latest` runners
- .NET 9.0.x is used for building
- Java 17 is required for Maven build
- Docker Buildx is used for multi-platform support
- Workflows include retry logic with `--skip-duplicate` for NuGet
