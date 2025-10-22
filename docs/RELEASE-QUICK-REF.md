# Quick Reference: Release Workflows

## TL;DR - How to Create a Release

1. Go to **Actions** tab in GitHub
2. Choose the appropriate workflow:
   - **Major** (2.0.0) for breaking changes
   - **Minor** (1.1.0) for new features
   - **Patch** (1.0.1) for bug fixes
3. Click **Run workflow**
4. Enter current version (e.g., `1.0.0`)
5. Click **Run workflow** button

## What You Need

### Before First Release
Add these secrets in repository Settings → Secrets → Actions:
- `NUGET_API_KEY` - From https://www.nuget.org/account/apikeys
- `DOCKER_USERNAME` - Your Docker Hub username
- `DOCKER_PASSWORD` - Your Docker Hub password/token

## If Publishing Fails

Use the **Retry Publish** workflow:
1. Actions → "Retry Publish (NuGet & Docker)"
2. Run workflow
3. Enter version: `1.0.0`
4. Enter release tag: `v1.0.0`

## What Gets Released

Each release creates:
- ✅ GitHub Release with tag (e.g., `v1.0.0`)
- ✅ 3 NuGet packages (Common, DataStream, JobBuilder)
- ✅ Docker image (`flinkdotnet/jobgateway:version`)
- ✅ Docker image tarball as release asset

## Semantic Versioning

| Version Type | When to Use | Example |
|-------------|-------------|---------|
| Major | Breaking changes | 1.0.0 → 2.0.0 |
| Minor | New features | 1.0.0 → 1.1.0 |
| Patch | Bug fixes | 1.0.0 → 1.0.1 |

## Workflow Jobs

All release workflows have these jobs:
1. **Calculate Version** - Bumps version number
2. **Build and Package** - Creates NuGet packages
3. **Build Docker Image** - Creates Docker image tarball
4. **Create Release** - Creates GitHub release with assets
5. **Publish Packages** - Publishes to NuGet.org and Docker Hub

## Full Documentation

See [docs/release-workflows.md](./release-workflows.md) for complete details.
