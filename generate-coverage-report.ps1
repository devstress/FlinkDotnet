#!/usr/bin/env pwsh
# Generate HTML Coverage Report from Cobertura XML

param(
    [string]$CoberturaFile = "coverage/coverage.cobertura.xml",
    [string]$OutputDir = "coverage/html-report"
)

Write-Host "🎨 Generating HTML Coverage Report" -ForegroundColor Cyan
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor DarkGray

# Verify input file exists
if (-not (Test-Path $CoberturaFile)) {
    Write-Host "❌ Coverage file not found: $CoberturaFile" -ForegroundColor Red
    exit 1
}

Write-Host "📊 Input:  $CoberturaFile" -ForegroundColor Gray
Write-Host "📁 Output: $OutputDir" -ForegroundColor Gray
Write-Host ""

# Install ReportGenerator if not already installed
Write-Host "🔧 Checking ReportGenerator tool..." -ForegroundColor Yellow
$toolCheck = dotnet tool list --global | Select-String "reportgenerator"
if (-not $toolCheck) {
    Write-Host "📦 Installing ReportGenerator..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-reportgenerator-globaltool
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Failed to install ReportGenerator" -ForegroundColor Red
        exit 1
    }
    Write-Host "✅ ReportGenerator installed" -ForegroundColor Green
} else {
    Write-Host "✅ ReportGenerator already installed" -ForegroundColor Green
}

Write-Host ""

# Generate HTML report
Write-Host "🎨 Generating HTML report..." -ForegroundColor Yellow
reportgenerator -reports:"$CoberturaFile" -targetdir:"$OutputDir" -reporttypes:"Html;HtmlSummary;Badges" -verbosity:"Info"

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Report generation failed" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "✅ HTML report generated successfully!" -ForegroundColor Green
Write-Host ""
Write-Host "📄 View the report:" -ForegroundColor Cyan
Write-Host "   File: $OutputDir\index.html" -ForegroundColor White
Write-Host ""
Write-Host "✅ Report generation complete!" -ForegroundColor Green