# Clean up existing containers
Write-Host "Cleaning up existing containers..." -ForegroundColor Yellow
$containers = docker ps -aq --filter "label=com.microsoft.developer.usvc-dev.name"
if ($containers) {
    docker stop $containers 2>$null
    docker rm $containers 2>$null
    Write-Host "✅ Cleaned up $($containers.Count) containers" -ForegroundColor Green
} else {
    Write-Host "✅ No containers to clean up" -ForegroundColor Green
}

# Wait a moment for cleanup
Start-Sleep -Seconds 2

# Run fresh test to see health check logging
Write-Host "`n🧪 Running fresh Exercise61 test to see health check logs..." -ForegroundColor Cyan
dotnet test LearningCourse/IntegrationTests.sln --filter "FullyQualifiedName~Exercise61" --configuration Release --logger "console;verbosity=detailed"