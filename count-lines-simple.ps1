#!/usr/bin/env pwsh
# Quick line counter for FlinkDotNet repository
# Usage: ./count-lines-simple.ps1

$repo = "/home/runner/work/FlinkDotnet/FlinkDotnet"

Write-Host "FlinkDotNet Repository Line Count" -ForegroundColor Green
Write-Host "=================================" -ForegroundColor Green

# Count C# files
$csharpLines = (Get-ChildItem -Path $repo -Recurse -Filter "*.cs" -File | Where-Object { $_.FullName -notmatch "\.git|bin|obj" } | Get-Content | Measure-Object -Line).Lines
$csharpFiles = (Get-ChildItem -Path $repo -Recurse -Filter "*.cs" -File | Where-Object { $_.FullName -notmatch "\.git|bin|obj" }).Count

# Count Markdown files  
$markdownLines = (Get-ChildItem -Path $repo -Recurse -Filter "*.md" -File | Where-Object { $_.FullName -notmatch "\.git" } | Get-Content | Measure-Object -Line).Lines
$markdownFiles = (Get-ChildItem -Path $repo -Recurse -Filter "*.md" -File | Where-Object { $_.FullName -notmatch "\.git" }).Count

# Count other code files
$otherCodeFiles = Get-ChildItem -Path $repo -Recurse -Include "*.csproj", "*.sln", "*.json", "*.yml", "*.yaml", "*.ps1", "*.sh" -File | Where-Object { $_.FullName -notmatch "\.git|bin|obj" }
$otherCodeLines = ($otherCodeFiles | Get-Content | Measure-Object -Line).Lines
$otherCodeCount = $otherCodeFiles.Count

$totalCodeLines = $csharpLines + $otherCodeLines
$totalLines = $totalCodeLines + $markdownLines
$totalFiles = $csharpFiles + $markdownFiles + $otherCodeCount

Write-Host ""
Write-Host "📊 LINES OF CODE: $totalCodeLines" -ForegroundColor Cyan
Write-Host "   • C# Source: $csharpLines lines ($csharpFiles files)" -ForegroundColor White
Write-Host "   • Other Code: $otherCodeLines lines ($otherCodeCount files)" -ForegroundColor White
Write-Host ""
Write-Host "📚 LINES OF DOCUMENTATION: $markdownLines" -ForegroundColor Cyan  
Write-Host "   • Markdown Files: $markdownLines lines ($markdownFiles files)" -ForegroundColor White
Write-Host ""
Write-Host "📋 TOTALS:" -ForegroundColor Green
Write-Host "   • Total Files: $totalFiles" -ForegroundColor White
Write-Host "   • Total Lines: $totalLines" -ForegroundColor White
Write-Host "   • Code-to-Docs Ratio: $(if($markdownLines -gt 0) { [math]::Round($totalCodeLines / $markdownLines, 2) } else { "N/A" }):1" -ForegroundColor White
Write-Host ""