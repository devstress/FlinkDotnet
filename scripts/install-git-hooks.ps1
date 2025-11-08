# FlinkDotNet Git Hooks Installation Script (Windows)
# Installs pre-commit hook for automatic code formatting

Write-Host "Installing FlinkDotNet Git hooks..." -ForegroundColor Yellow

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "Error: Not in a git repository root directory." -ForegroundColor Red
    Write-Host "Please run this script from the repository root." -ForegroundColor Red
    exit 1
}

# Check if dotnet is available
try {
    $dotnetVersion = dotnet --version
    Write-Host "Found .NET SDK version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "Warning: dotnet CLI not found." -ForegroundColor Red
    Write-Host "Please install .NET SDK 9.0 or later." -ForegroundColor Yellow
    Write-Host "The hook will be installed but won't work until .NET is available." -ForegroundColor Yellow
}

# Create hooks directory if it doesn't exist
$hooksDir = ".git\hooks"
if (-not (Test-Path $hooksDir)) {
    New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null
}

# Source hook file
$sourceHook = "scripts\pre-commit"

# Destination hook file
$destHook = ".git\hooks\pre-commit"

# Check if source hook exists
if (-not (Test-Path $sourceHook)) {
    Write-Host "Error: Source hook file not found: $sourceHook" -ForegroundColor Red
    exit 1
}

# Backup existing hook if it exists
if (Test-Path $destHook) {
    $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
    $backupFile = "$destHook.backup.$timestamp"
    Write-Host "Backing up existing pre-commit hook to: $backupFile" -ForegroundColor Yellow
    Copy-Item $destHook $backupFile
}

# Copy the hook
Write-Host "Installing pre-commit hook..." -ForegroundColor Yellow
Copy-Item $sourceHook $destHook -Force

# For Windows, we need to ensure Git can execute the bash script
# Git for Windows includes bash, so the script should work as-is
# We just need to make sure it has the right line endings

# Convert line endings to Unix format (LF) to ensure compatibility with Git Bash
$hookContent = Get-Content $destHook -Raw
$hookContent = $hookContent -replace "`r`n", "`n"
Set-Content -Path $destHook -Value $hookContent -NoNewline

# Verify installation
if (Test-Path $destHook) {
    Write-Host "✓ Pre-commit hook installed successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "The hook will now automatically run 'dotnet format' before each commit." -ForegroundColor Green
    Write-Host ""
    Write-Host "To test the hook:" -ForegroundColor Yellow
    Write-Host "  1. Make a change to a .cs file"
    Write-Host "  2. Stage the file: git add <file>"
    Write-Host "  3. Commit: git commit -m 'Test commit'"
    Write-Host "  4. The hook will automatically format your code"
    Write-Host ""
    Write-Host "To bypass the hook (not recommended):" -ForegroundColor Yellow
    Write-Host "  git commit --no-verify -m 'Your message'"
    Write-Host ""
} else {
    Write-Host "✗ Failed to install pre-commit hook." -ForegroundColor Red
    exit 1
}

exit 0
