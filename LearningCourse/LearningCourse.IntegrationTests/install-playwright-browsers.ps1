# Install Playwright browsers for testing
# This script is cross-platform compatible (Windows, Linux, macOS)
# Supports both Windows PowerShell 5.1+ and PowerShell Core (pwsh) 7.0+
# This script must be run after building the project

#Requires -Version 5.1

# Detect the operating system
# PowerShell 5.1 doesn't have $IsWindows, $IsLinux, $IsMacOS variables
# so we need to handle both old and new PowerShell versions
$platform = if ($PSVersionTable.PSVersion.Major -ge 6) {
    # PowerShell Core 6+ has built-in platform detection
    if ($IsWindows) {
        "Windows"
    } elseif ($IsLinux) {
        "Linux"
    } elseif ($IsMacOS) {
        "macOS"
    } else {
        "Unknown"
    }
} else {
    # PowerShell 5.1 on Windows
    if ($PSVersionTable.PSVersion.Major -eq 5 -and [System.Environment]::OSVersion.Platform -eq 'Win32NT') {
        "Windows"
    } else {
        "Unknown"
    }
}

Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Playwright Browser Installer (Cross-Platform)" -ForegroundColor Cyan
Write-Host "==================================================" -ForegroundColor Cyan
Write-Host "Detected Platform: $platform" -ForegroundColor Green
Write-Host ""

if ($platform -eq "Unknown") {
    Write-Host "Error: Unsupported operating system" -ForegroundColor Red
    Write-Host "This script supports Windows, Linux, and macOS" -ForegroundColor Yellow
    exit 1
}

# Navigate to the bin directory where Playwright tools are located
$binDir = Join-Path $PSScriptRoot "bin"

if (-not (Test-Path $binDir)) {
    Write-Host "Error: bin directory not found. Please build the project first with:" -ForegroundColor Red
    Write-Host "  dotnet build --configuration Release" -ForegroundColor Yellow
    exit 1
}

Write-Host "Installing Playwright browsers for Chromium..." -ForegroundColor Cyan
Write-Host ""

try {
    # Save current location and navigate to bin directory
    Push-Location $binDir
    
    # Platform-specific Playwright CLI detection
    $playwrightScript = $null
    
    if ($platform -eq "Windows") {
        # On Windows, look for playwright.ps1
        Write-Host "Searching for Playwright CLI (playwright.ps1)..." -ForegroundColor Yellow
        $playwrightScript = Get-ChildItem -Recurse -Filter "playwright.ps1" -ErrorAction SilentlyContinue | Select-Object -First 1
    } else {
        # On Linux/macOS, look for playwright shell script
        Write-Host "Searching for Playwright CLI (playwright)..." -ForegroundColor Yellow
        $playwrightScript = Get-ChildItem -Recurse -Filter "playwright" -ErrorAction SilentlyContinue |
            Where-Object { -not $_.Extension -and $_.Name -eq "playwright" } |
            Select-Object -First 1
    }
    
    if ($playwrightScript) {
        Write-Host "[SUCCESS] Found Playwright CLI at: $($playwrightScript.FullName)" -ForegroundColor Green
        Write-Host "Executing: playwright install chromium" -ForegroundColor Yellow
        Write-Host ""
        
        if ($platform -eq "Windows") {
            # On Windows, execute the PowerShell script
            & $playwrightScript.FullName install chromium
        } else {
            # On Linux/macOS, ensure script is executable and run it
            chmod +x $playwrightScript.FullName
            & $playwrightScript.FullName install chromium
        }
        
        if ($LASTEXITCODE -ne 0) {
            throw "Playwright installation failed with exit code $LASTEXITCODE"
        }
    } else {
        Write-Host "[WARNING] Playwright CLI not found in bin directory." -ForegroundColor Yellow
        Write-Host "Trying alternative installation methods..." -ForegroundColor Yellow
        Write-Host ""
        
        # Alternative: Use npx if available
        if (Get-Command npx -ErrorAction SilentlyContinue) {
            Write-Host "[INFO] Found npx, using it to install Playwright browsers..." -ForegroundColor Green
            npx playwright install chromium
            
            if ($LASTEXITCODE -ne 0) {
                throw "npx playwright installation failed with exit code $LASTEXITCODE"
            }
        } else {
            Write-Host "[ERROR] Neither Playwright CLI nor npx found." -ForegroundColor Red
            Write-Host ""
            Write-Host "Please install Playwright browsers manually using one of these methods:" -ForegroundColor Yellow
            Write-Host ""
            
            if ($platform -eq "Windows") {
                Write-Host "Method 1 (Recommended for Windows):" -ForegroundColor Cyan
                Write-Host "  npm install -g playwright" -ForegroundColor White
                Write-Host "  playwright install chromium" -ForegroundColor White
                Write-Host ""
                Write-Host "Method 2 (PowerShell):" -ForegroundColor Cyan
                Write-Host "  pwsh -c 'npx playwright install chromium'" -ForegroundColor White
            } else {
                Write-Host "Method 1 (Recommended for Linux/macOS):" -ForegroundColor Cyan
                Write-Host "  npm install -g playwright" -ForegroundColor White
                Write-Host "  playwright install chromium" -ForegroundColor White
                Write-Host ""
                Write-Host "Method 2 (npx):" -ForegroundColor Cyan
                Write-Host "  npx playwright install chromium" -ForegroundColor White
                Write-Host ""
                Write-Host "Method 3 (Using dotnet tool):" -ForegroundColor Cyan
                Write-Host "  dotnet tool install --global Microsoft.Playwright.CLI" -ForegroundColor White
                Write-Host "  playwright install chromium" -ForegroundColor White
            }
            
            Pop-Location
            exit 1
        }
    }
    
    Pop-Location
    
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host "[SUCCESS] Playwright browser installation completed!" -ForegroundColor Green
    Write-Host "==================================================" -ForegroundColor Green
    Write-Host ""
    Write-Host "Platform: $platform" -ForegroundColor Cyan
    Write-Host "Browser: Chromium" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "To run the UI video tests, use:" -ForegroundColor Yellow
    Write-Host "  dotnet test --filter 'Category=ui-video'" -ForegroundColor White
    Write-Host ""
    
    exit 0
}
catch {
    Write-Host ""
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host "[ERROR] Error installing Playwright browsers" -ForegroundColor Red
    Write-Host "==================================================" -ForegroundColor Red
    Write-Host ""
    Write-Host "Error Details: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Platform: $platform" -ForegroundColor Yellow
    Write-Host ""
    
    if ($platform -eq "Linux") {
        Write-Host "Linux Troubleshooting:" -ForegroundColor Yellow
        Write-Host "  1. Ensure you have required dependencies:" -ForegroundColor White
        Write-Host "     sudo apt-get install libnss3 libnspr4 libatk1.0-0 libatk-bridge2.0-0" -ForegroundColor White
        Write-Host "     sudo apt-get install libcups2 libdrm2 libxkbcommon0 libxcomposite1" -ForegroundColor White
        Write-Host "     sudo apt-get install libxdamage1 libxfixes3 libxrandr2 libgbm1 libasound2" -ForegroundColor White
        Write-Host ""
    }
    
    Write-Host "For CI/CD environments, ensure:" -ForegroundColor Yellow
    Write-Host "  - PowerShell 7+ is installed (pwsh) on Linux/macOS" -ForegroundColor White
    Write-Host "  - Project is built before running this script" -ForegroundColor White
    Write-Host "  - npm/npx is available as fallback" -ForegroundColor White
    Write-Host ""
    
    Pop-Location
    exit 1
}