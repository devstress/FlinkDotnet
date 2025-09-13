param(
  [string]$RunnerDir = "FlinkIRRunner",
  [string]$MavenVersion = "3.9.8"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Ensure-Maven {
  param([string]$Version, [string]$InstallDir)
  if (-not (Test-Path $InstallDir)) { New-Item -ItemType Directory -Path $InstallDir | Out-Null }
  $mvnHome = Join-Path $InstallDir "apache-maven-$Version"
  $mvnBin = Join-Path $mvnHome "bin/mvn.cmd"
  if (Test-Path $mvnBin) { return $mvnBin }
  $zipUrl = "https://archive.apache.org/dist/maven/maven-3/$Version/binaries/apache-maven-$Version-bin.zip"
  $zipPath = Join-Path $InstallDir "apache-maven-$Version-bin.zip"
  Write-Host "Downloading Maven $Version from $zipUrl"
  Invoke-WebRequest -Uri $zipUrl -OutFile $zipPath
  Write-Host "Extracting $zipPath to $InstallDir"
  Expand-Archive -Path $zipPath -DestinationPath $InstallDir -Force
  Remove-Item $zipPath -Force
  return $mvnBin
}

$toolsDir = Join-Path $PSScriptRoot "..\tools"
$mvnCmd = Ensure-Maven -Version $MavenVersion -InstallDir $toolsDir

Push-Location $RunnerDir
try {
  & $mvnCmd -q -DskipTests package
} finally {
  Pop-Location
}

Write-Host "Runner built at $RunnerDir/target/flink-ir-runner.jar (shaded)"
