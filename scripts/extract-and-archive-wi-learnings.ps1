#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Automated Work Item Learning Extraction and Archival System
    
.DESCRIPTION
    This script implements Rule 10 from the enforcement rules to automatically:
    1. Extract learnings from Work Items older than 1 month
    2. Consolidate learnings into AI-Learning/ folder
    3. Archive or remove outdated Work Items
    4. Prevent repeated mistakes by creating searchable learning repository
    
.PARAMETER Force
    Force extraction and archival even for newer WIs
    
.PARAMETER DryRun
    Show what would be extracted/archived without making changes
    
.EXAMPLE
    ./extract-and-archive-wi-learnings.ps1
    
.EXAMPLE
    ./extract-and-archive-wi-learnings.ps1 -DryRun
#>

param(
    [switch]$Force,
    [switch]$DryRun
)

# Colors for output
$Green = "`e[32m"
$Red = "`e[31m"
$Yellow = "`e[33m"
$Blue = "`e[34m"
$Reset = "`e[0m"

function Write-Success {
    param([string]$Message)
    Write-Host "${Green}✅ $Message${Reset}"
}

function Write-Error {
    param([string]$Message)
    Write-Host "${Red}❌ $Message${Reset}"
}

function Write-Warning {
    param([string]$Message)
    Write-Host "${Yellow}⚠️ $Message${Reset}"
}

function Write-Info {
    param([string]$Message)
    Write-Host "${Blue}ℹ️ $Message${Reset}"
}

function Get-WIAge {
    param([string]$FilePath)
    
    $createdDate = $null
    $content = Get-Content $FilePath -Raw
    
    # Extract creation date from WI content
    if ($content -match '\*\*Created\*\*:\s*(\d{4}-\d{2}-\d{2})') {
        $createdDate = [DateTime]::Parse($matches[1])
    } elseif ($content -match 'Created.*(\d{4}-\d{2}-\d{2})') {
        $createdDate = [DateTime]::Parse($matches[1])
    } else {
        # Fallback to file creation time
        $createdDate = (Get-Item $FilePath).CreationTime
    }
    
    return ((Get-Date) - $createdDate).Days
}

function Extract-WILearnings {
    param(
        [string]$WIPath,
        [string]$TopicName
    )
    
    $content = Get-Content $WIPath -Raw
    $learnings = @()
    
    # Extract lessons learned sections
    if ($content -match '## Lessons Learned & Future Reference \(MANDATORY\)(.*?)(?=##|$)') {
        $learningsSection = $matches[1]
        
        # Extract specific subsections
        if ($learningsSection -match '### What Worked Well(.*?)(?=###|$)') {
            $learnings += "**What Worked Well:**`n$($matches[1].Trim())"
        }
        
        if ($learningsSection -match '### What Could Be Improved(.*?)(?=###|$)') {
            $learnings += "**What Could Be Improved:**`n$($matches[1].Trim())"
        }
        
        if ($learningsSection -match '### Key Insights for Similar Tasks(.*?)(?=###|$)') {
            $learnings += "**Key Insights:**`n$($matches[1].Trim())"
        }
        
        if ($learningsSection -match '### Specific Problems to Avoid in Future(.*?)(?=###|$)') {
            $learnings += "**Problems to Avoid:**`n$($matches[1].Trim())"
        }
        
        if ($learningsSection -match '### Reference for Future WIs(.*?)(?=###|$)') {
            $learnings += "**Future Reference:**`n$($matches[1].Trim())"
        }
    }
    
    # Extract debug information patterns
    if ($content -match '### Debug Information \(MANDATORY.*?\)(.*?)(?=###|$)') {
        $debugSection = $matches[1]
        $learnings += "**Debug Patterns:**`n$($debugSection.Trim())"
    }
    
    return $learnings
}

Write-Info "🔍 Starting Work Item Learning Extraction and Archival Process"
Write-Info "Implementing Rule 10: Automatic Archiving & Learning Enforcement"

# Ensure directories exist
$WIsPath = "WIs"
$AILearningPath = "AI-Learning"

if (-not (Test-Path $AILearningPath)) {
    Write-Info "Creating AI-Learning directory..."
    New-Item -ItemType Directory -Path $AILearningPath -Force | Out-Null
}

if (-not (Test-Path $WIsPath)) {
    Write-Warning "WIs directory not found. Nothing to process."
    exit 0
}

# Get all Work Item files
$wiFiles = Get-ChildItem -Path $WIsPath -Filter "*.md" | Where-Object { $_.Name -notlike "WI_CONSOLIDATED_*" }

Write-Info "Found $($wiFiles.Count) Work Item files to analyze"

$oldWIs = @()
$currentWIs = @()

foreach ($wiFile in $wiFiles) {
    $age = Get-WIAge -FilePath $wiFile.FullName
    Write-Info "  $($wiFile.Name) - Age: $age days"
    
    if ($age -gt 30 -or $Force) {
        $oldWIs += $wiFile
    } else {
        $currentWIs += $wiFile
    }
}

if ($oldWIs.Count -eq 0) {
    Write-Success "No Work Items older than 30 days found. Learning extraction not needed."
    exit 0
}

Write-Warning "Found $($oldWIs.Count) Work Items older than 30 days requiring learning extraction"

# Group WIs by topic for consolidation
$topics = @{}

foreach ($wiFile in $oldWIs) {
    $fileName = $wiFile.Name
    $topic = "General"
    
    # Extract topic from filename patterns
    if ($fileName -match "observability") { $topic = "Observability_Testing" }
    elseif ($fileName -match "aspire") { $topic = "Aspire_Integration" }
    elseif ($fileName -match "learning-course") { $topic = "Learning_Course" }
    elseif ($fileName -match "build|validation") { $topic = "Build_Validation" }
    elseif ($fileName -match "documentation") { $topic = "Documentation" }
    
    if (-not $topics.ContainsKey($topic)) {
        $topics[$topic] = @()
    }
    $topics[$topic] += $wiFile
}

# Extract and consolidate learnings by topic
foreach ($topic in $topics.Keys) {
    $topicFile = Join-Path $AILearningPath "$topic.md"
    $consolidatedLearnings = @()
    
    Write-Info "📝 Extracting learnings for topic: $topic"
    
    $consolidatedLearnings += "# $($topic.Replace('_', ' ')) - Consolidated Learnings"
    $consolidatedLearnings += ""
    $consolidatedLearnings += "**Generated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $consolidatedLearnings += "**Source Work Items**: $($topics[$topic] | ForEach-Object { $_.Name } | Join-String -Separator ', ')"
    $consolidatedLearnings += ""
    
    foreach ($wiFile in $topics[$topic]) {
        Write-Info "  Extracting from: $($wiFile.Name)"
        
        $learnings = Extract-WILearnings -WIPath $wiFile.FullName -TopicName $topic
        
        if ($learnings.Count -gt 0) {
            $consolidatedLearnings += "## Learnings from $($wiFile.Name)"
            $consolidatedLearnings += ""
            $consolidatedLearnings += $learnings
            $consolidatedLearnings += ""
        }
    }
    
    # Add prevention checklist
    $consolidatedLearnings += "## Prevention Checklist for Future $($topic.Replace('_', ' ')) Work"
    $consolidatedLearnings += ""
    $consolidatedLearnings += "Before starting any new Work Item in this area:"
    $consolidatedLearnings += "- [ ] Review this learning document completely"
    $consolidatedLearnings += "- [ ] Apply all 'Problems to Avoid' lessons"
    $consolidatedLearnings += "- [ ] Reference successful patterns from 'What Worked Well'"
    $consolidatedLearnings += "- [ ] Follow debug patterns from previous investigations"
    $consolidatedLearnings += "- [ ] Update this document with new learnings after completion"
    $consolidatedLearnings += ""
    
    if (-not $DryRun) {
        $consolidatedLearnings | Out-File -FilePath $topicFile -Encoding UTF8
        Write-Success "  Created learning document: $topicFile"
    } else {
        Write-Info "  [DRY RUN] Would create: $topicFile"
    }
}

# Archive old Work Items
Write-Info "📦 Archiving old Work Items"

$archivePath = Join-Path $WIsPath "Archived"
if (-not (Test-Path $archivePath)) {
    if (-not $DryRun) {
        New-Item -ItemType Directory -Path $archivePath -Force | Out-Null
    }
}

foreach ($wiFile in $oldWIs) {
    $archiveFile = Join-Path $archivePath $wiFile.Name
    
    if (-not $DryRun) {
        Move-Item -Path $wiFile.FullName -Destination $archiveFile
        Write-Success "  Archived: $($wiFile.Name)"
    } else {
        Write-Info "  [DRY RUN] Would archive: $($wiFile.Name)"
    }
}

# Create or update master learning index
$indexFile = Join-Path $AILearningPath "README.md"
$indexContent = @()

$indexContent += "# AI Learning Repository"
$indexContent += ""
$indexContent += "This directory contains consolidated learnings extracted from Work Items to prevent repeating mistakes."
$indexContent += ""
$indexContent += "## How to Use This Repository"
$indexContent += ""
$indexContent += "1. **Before starting any new Work Item**: Search this repository for related topics"
$indexContent += "2. **Review relevant learning documents**: Apply lessons from previous work"
$indexContent += "3. **After completing Work Items**: Update relevant documents with new learnings"
$indexContent += ""
$indexContent += "## Available Learning Topics"
$indexContent += ""

$learningFiles = Get-ChildItem -Path $AILearningPath -Filter "*.md" | Where-Object { $_.Name -ne "README.md" }
foreach ($learningFile in $learningFiles) {
    $topicName = $learningFile.BaseName.Replace('_', ' ')
    $indexContent += "- [$topicName]($($learningFile.Name))"
}

$indexContent += ""
$indexContent += "## Enforcement Rule 10 Compliance"
$indexContent += ""
$indexContent += "This repository implements Rule 10: Automatic Archiving & Learning Enforcement"
$indexContent += "- Work Items older than 1 month are automatically processed"
$indexContent += "- Learnings are extracted and consolidated by topic"
$indexContent += "- Old Work Items are archived to maintain repository cleanliness"
$indexContent += "- Searchable knowledge base prevents repeating solved problems"
$indexContent += ""
$indexContent += "**Last Updated**: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

if (-not $DryRun) {
    $indexContent | Out-File -FilePath $indexFile -Encoding UTF8
    Write-Success "Updated learning repository index: $indexFile"
}

Write-Success "🎉 Learning extraction and archival complete!"
Write-Info "📊 Summary:"
Write-Info "  - Processed $($oldWIs.Count) old Work Items"
Write-Info "  - Created/updated $($topics.Keys.Count) learning topics"
Write-Info "  - Current active Work Items: $($currentWIs.Count)"

if ($DryRun) {
    Write-Warning "DRY RUN mode - no changes were made. Run without -DryRun to apply changes."
}