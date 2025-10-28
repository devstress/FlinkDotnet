# Docker Publishing Retry Mechanism

## Overview
The FlinkDotNet release workflows include automatic retry logic for Docker Hub publishing to handle transient network failures and authentication issues. This ensures reliable deployment of Docker images even when temporary issues occur.

## Problem Solved
Docker Hub publishing was occasionally failing with "denied: requested access to the resource is denied" errors. This typically occurs due to:
- Transient network failures
- Session expiry between login and push
- Rate limiting from Docker Hub
- Temporary authentication issues

## Solution
All release workflows (major, minor, and patch) now include:
1. **Automatic retry logic** - Up to 3 attempts for each Docker push
2. **Re-authentication** - Fresh Docker Hub login before each retry
3. **Comprehensive logging** - Detailed output for troubleshooting
4. **Authentication verification** - Confirms Docker Hub login before pushing

## How It Works

### Automatic Retry in Release Workflows
When a release workflow runs, the Docker push operation automatically:

1. **First attempt**: Try to push the image
2. **On failure**: Wait 10 seconds and re-authenticate
3. **Retry**: Attempt push again (up to 3 total attempts)
4. **Success or fail**: Exit with appropriate status

Each retry includes:
- Fresh authentication with Docker Hub
- 10-second delay between attempts
- Clear logging of attempt number and status

### Manual Retry Workflow
If a release workflow fails completely, you can use the **Retry Publish** workflow to retry just the publishing step.

#### How to Use Retry Publish Workflow

1. Navigate to **Actions** → **Retry Publish (NuGet & Docker)**
2. Click **Run workflow**
3. Fill in the required inputs:
   - **version**: The version number (e.g., `1.0.0`)
   - **release_tag**: The Git tag (e.g., `v1.0.0`)
   - **skip_nuget** (optional): Set to `yes` to skip NuGet publishing
   - **skip_docker** (optional): Set to `yes` to skip Docker publishing

#### Retry Workflow Features
- ✅ Input validation (version and tag format)
- ✅ Selective publishing (skip NuGet or Docker as needed)
- ✅ Downloads artifacts from the specified release
- ✅ Same retry logic as release workflows
- ✅ Comprehensive error messages

## Technical Details

### Retry Function
```bash
push_with_retry() {
  local image=$1
  local max_attempts=3
  local attempt=1
  
  while [ $attempt -le $max_attempts ]; do
    echo "Attempt $attempt of $max_attempts: Pushing $image..."
    
    if docker push "$image"; then
      echo "✅ Successfully pushed $image"
      return 0
    else
      echo "⚠️  Push failed for $image (attempt $attempt/$max_attempts)"
      if [ $attempt -lt $max_attempts ]; then
        echo "Waiting 10 seconds before retry..."
        sleep 10
        
        # Re-authenticate before retry
        # Note: In workflows, uses ${{ secrets.DOCKER_PASSWORD }} and ${{ secrets.DOCKER_USERNAME }}
        echo "Re-authenticating with Docker Hub..."
        echo "<password>" | docker login -u "<username>" --password-stdin
      fi
    fi
    
    attempt=$((attempt + 1))
  done
  
  echo "❌ Failed to push $image after $max_attempts attempts"
  return 1
}
```

**Note**: In the actual workflow implementation, the credentials are accessed via GitHub Actions secrets:
- `${{ secrets.DOCKER_PASSWORD }}` for the password
- `${{ secrets.DOCKER_USERNAME }}` for the username

### Affected Workflows
- `.github/workflows/release-major.yml`
- `.github/workflows/release-minor.yml`
- `.github/workflows/release-patch.yml`
- `.github/workflows/retry-publish.yml`

## Monitoring and Troubleshooting

### Success Indicators
- ✅ "Successfully pushed" messages in workflow logs
- ✅ Images available on Docker Hub
- ✅ Version and latest tags both pushed

### Failure Indicators
- ❌ "Failed to push after 3 attempts" in logs
- ⚠️  Multiple retry attempts visible in logs

### Common Issues and Solutions

#### Authentication Failures
**Symptom**: All 3 attempts fail with authentication errors

**Solution**: 
- Verify Docker Hub secrets are configured correctly
- Check `DOCKER_USERNAME` and `DOCKER_PASSWORD` in repository secrets
- Ensure Docker Hub account has push permissions

#### Network Timeouts
**Symptom**: Push starts but times out

**Solution**:
- Use the retry-publish workflow to try again
- Check Docker Hub status page for outages
- Consider increasing timeout in workflow if needed

#### Rate Limiting
**Symptom**: "Too many requests" errors

**Solution**:
- Wait a few minutes before retrying
- Use retry-publish workflow with delay
- Contact Docker Hub support if persistent

## Best Practices

1. **Monitor workflow runs** - Check for retry attempts in logs
2. **Use retry-publish for manual fixes** - Don't re-run entire release workflow
3. **Report persistent failures** - Create issues for recurring problems
4. **Verify secrets** - Ensure Docker Hub credentials are up to date

## Examples

### Successful Push (First Attempt)
```
Loading Docker image...
✅ Docker image loaded successfully
Attempt 1 of 3: Pushing devstress/flinkdotnet:1.0.0...
✅ Successfully pushed devstress/flinkdotnet:1.0.0
```

### Successful Push (After Retry)
```
Loading Docker image...
✅ Docker image loaded successfully
Attempt 1 of 3: Pushing devstress/flinkdotnet:1.0.0...
⚠️  Push failed for devstress/flinkdotnet:1.0.0 (attempt 1/3)
Waiting 10 seconds before retry...
Re-authenticating with Docker Hub...
Attempt 2 of 3: Pushing devstress/flinkdotnet:1.0.0...
✅ Successfully pushed devstress/flinkdotnet:1.0.0
```

### Failed After All Retries
```
Loading Docker image...
✅ Docker image loaded successfully
Attempt 1 of 3: Pushing devstress/flinkdotnet:1.0.0...
⚠️  Push failed for devstress/flinkdotnet:1.0.0 (attempt 1/3)
Waiting 10 seconds before retry...
Re-authenticating with Docker Hub...
Attempt 2 of 3: Pushing devstress/flinkdotnet:1.0.0...
⚠️  Push failed for devstress/flinkdotnet:1.0.0 (attempt 2/3)
Waiting 10 seconds before retry...
Re-authenticating with Docker Hub...
Attempt 3 of 3: Pushing devstress/flinkdotnet:1.0.0...
⚠️  Push failed for devstress/flinkdotnet:1.0.0 (attempt 3/3)
❌ Failed to push devstress/flinkdotnet:1.0.0 after 3 attempts
```

## Related Documentation
- [Release Quick Reference](RELEASE-QUICK-REF.md)
- [Release Package Validation](RELEASE_PACKAGE_VALIDATION.md)
- [GitHub Actions Workflows](../.github/workflows/)

## Support
If you encounter persistent issues with Docker publishing:
1. Check workflow logs for specific error messages
2. Verify Docker Hub credentials in repository secrets
3. Try using the retry-publish workflow
4. Create an issue with logs and error details
