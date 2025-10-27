# NuGet Trusted Publishing Configuration Guide

## What Was Fixed

The release workflows were failing with a 401 Unauthorized error when publishing to NuGet.org. The issue was that while the workflows had the `id-token: write` permission enabled, they were missing the crucial `NuGet/login@v1` action that exchanges the GitHub OIDC token for a temporary NuGet API key.

## Changes Made

All four release workflows have been updated:
- ✅ `.github/workflows/release-major.yml`
- ✅ `.github/workflows/release-minor.yml`
- ✅ `.github/workflows/release-patch.yml`
- ✅ `.github/workflows/retry-publish.yml`

Each workflow now includes:
```yaml
- name: NuGet login (OIDC → temp API key)
  uses: NuGet/login@v1
  id: nuget-login
  with:
    user: ${{ secrets.NUGET_USER }}

- name: Publish to NuGet.org
  run: |
    dotnet nuget push "./packages/*.nupkg" \
      --api-key ${{ steps.nuget-login.outputs.NUGET_API_KEY }} \
      --source https://api.nuget.org/v3/index.json \
      --skip-duplicate
```

## Required Configuration Steps

To complete the setup, you need to configure **TWO** things:

### 1. GitHub Repository Secret

Add the `NUGET_USER` secret to this repository:

**Steps:**
1. Go to **Settings** → **Secrets and variables** → **Actions**
2. Click **"New repository secret"**
3. **Name:** `NUGET_USER`
4. **Value:** Your NuGet.org **username** (profile name, **NOT** email address)
   - Example: If your NuGet.org profile URL is `https://www.nuget.org/profiles/devstress`, your username is `devstress`
5. Click **"Add secret"**

### 2. NuGet.org Trusted Publishers Configuration

Configure Trusted Publishers on NuGet.org for each workflow:

**Steps:**
1. Go to [NuGet.org](https://www.nuget.org/) and sign in
2. Navigate to your account settings
3. Select the **FlinkDotnet** package (or create it if it doesn't exist yet)
4. Go to **"Trusted Publishers"** section
5. Add **FOUR** trusted publishers (one for each workflow):

   **Publisher 1:**
   - **Source:** GitHub Actions
   - **Owner:** devstress
   - **Repository:** FlinkDotnet
   - **Workflow:** release-major.yml

   **Publisher 2:**
   - **Source:** GitHub Actions
   - **Owner:** devstress
   - **Repository:** FlinkDotnet
   - **Workflow:** release-minor.yml

   **Publisher 3:**
   - **Source:** GitHub Actions
   - **Owner:** devstress
   - **Repository:** FlinkDotnet
   - **Workflow:** release-patch.yml

   **Publisher 4:**
   - **Source:** GitHub Actions
   - **Owner:** devstress
   - **Repository:** FlinkDotnet
   - **Workflow:** retry-publish.yml

## How to Verify

After configuring both the GitHub secret and NuGet Trusted Publishers:

1. Trigger one of the release workflows (e.g., Release - Patch Version)
2. The workflow should:
   - ✅ Build successfully
   - ✅ Authenticate with NuGet.org via OIDC
   - ✅ Publish packages without 401 errors
   - ✅ Complete successfully

## Troubleshooting

### If you get 401 Unauthorized after configuration:

1. **Check secret name:** Make sure it's exactly `NUGET_USER` (case-sensitive)
2. **Check secret value:** Must be your NuGet.org profile name, not email
3. **Check Trusted Publishers:** Workflow names must match exactly (including `.yml` extension)
4. **Check repository/owner:** Must be `devstress/FlinkDotnet` exactly
5. **Wait a moment:** NuGet.org configuration may take a minute to propagate

### If you get "package does not exist" error:

- You may need to create the package manually on NuGet.org first, or
- The first publish might require a different approach
- Contact NuGet.org support if needed

## Benefits of This Approach

✅ **No API keys to manage** - OIDC tokens are temporary and auto-generated  
✅ **More secure** - Tokens are scoped to specific workflows  
✅ **Better audit trail** - NuGet.org logs which workflow published each version  
✅ **Automatic rotation** - No need to rotate keys manually  

## Documentation

The following documentation has been updated:
- `docs/release-workflows.md` - Updated with secret configuration instructions
- `WIs/WI1_nuget-trusted-publishing-fix.md` - Complete work item with implementation details

## Questions?

If you have any questions or encounter issues:
1. Check the Work Item: `WIs/WI1_nuget-trusted-publishing-fix.md`
2. Review official docs: https://learn.microsoft.com/en-us/nuget/nuget-org/publish-a-package#publish-with-github-actions
3. Check GitHub Actions logs for detailed error messages
