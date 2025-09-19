param(
    [string]$RunnerDir = (Join-Path (Join-Path $PSScriptRoot '..') 'FlinkIRRunner'),
    [switch]$Force
)
$ErrorActionPreference = 'Continue'
$IsWindows = $env:OS -like '*Windows*' -or $PSVersionTable.Platform -eq 'Win32NT'
$IsMacOS = $IsWindows -eq $false -and (Test-Path /System/Library/CoreServices)
Write-Host "[ensure-flink-runner] Runner directory: $RunnerDir (IsWindows=$IsWindows IsMacOS=$IsMacOS)"
if (!(Test-Path $RunnerDir)) { Write-Warning "[ensure-flink-runner] Runner directory missing; creating."; New-Item -ItemType Directory -Path $RunnerDir | Out-Null }
$jarPath = Join-Path (Join-Path $RunnerDir 'target') 'flink-ir-runner.jar'
$pomPath = Join-Path $RunnerDir 'pom.xml'

function New-PlaceholderJar {
    param($Reason)
    Write-Warning "[ensure-flink-runner] Creating placeholder JAR due to: $Reason"
    $targetDir = Split-Path $jarPath -Parent
    if (!(Test-Path $targetDir)) { New-Item -ItemType Directory -Path $targetDir | Out-Null }
    Set-Content -Path $jarPath -Value "// Placeholder JAR marker - $Reason `n" -Encoding UTF8
    Write-Host "[ensure-flink-runner] Placeholder JAR written: $jarPath"
}

function Test-IsStaleJar {
    if (!(Test-Path $jarPath)) { return $true }
    if (!(Test-Path $pomPath)) { return $false }
    $jarTime = (Get-Item $jarPath).LastWriteTimeUtc
    $pomTime = (Get-Item $pomPath).LastWriteTimeUtc
    if ($pomTime -gt $jarTime) { return $true }
    $src = Join-Path $RunnerDir 'src'
    if (Test-Path $src) {
        $srcNewest = Get-ChildItem -Recurse $src -Include *.java | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
        if ($srcNewest -and $srcNewest.LastWriteTimeUtc -gt $jarTime) { return $true }
    }
    return $false
}

$needsBuild = $Force -or (Test-IsStaleJar)
if (-not $needsBuild) { Write-Host "[ensure-flink-runner] Existing jar up-to-date: $jarPath"; exit 0 }
if (Test-Path $jarPath) { Write-Host "[ensure-flink-runner] Rebuilding jar (stale or -Force)." }

function Ensure-Java17 {
    try {
        $verOutput = & java -version 2>&1
        if ($LASTEXITCODE -eq 0 -and ($verOutput -match 'version "(?<v>[0-9]+)')) {
            $major = [int]$Matches['v']
            if ($major -ge 17) { Write-Host "[ensure-flink-runner] Found Java $major"; return $true }
        }
    } catch { }
    Write-Host "[ensure-flink-runner] Java 17 not present - attempting portable install"
    try {
        $jdkDir = Join-Path $RunnerDir '.jdk'
        if (Test-Path $jdkDir) { Remove-Item $jdkDir -Recurse -Force }
        New-Item -ItemType Directory -Path $jdkDir | Out-Null
        if ($IsWindows) {
            $jdkZip = Join-Path $env:TEMP 'temurin17.zip'
            $url = 'https://api.adoptium.net/v3/binary/latest/17/ga/windows/x64/jdk/hotspot/normal/eclipse'
            Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $jdkZip
            Expand-Archive -Path $jdkZip -DestinationPath $jdkDir -Force
            Remove-Item $jdkZip -Force
        } else {
            $jdkTar = '/tmp/temurin17.tar.gz'
            if ($IsMacOS) { $url = 'https://api.adoptium.net/v3/binary/latest/17/ga/mac/aarch64/jdk/hotspot/normal/eclipse' } else { $url = 'https://api.adoptium.net/v3/binary/latest/17/ga/linux/x64/jdk/hotspot/normal/eclipse' }
            Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $jdkTar
            tar -xf $jdkTar -C $jdkDir --strip-components=1
            rm $jdkTar
        }
        $env:JAVA_HOME = $jdkDir
        $env:Path = (Join-Path $jdkDir 'bin') + [IO.Path]::PathSeparator + $env:Path
        Write-Host "[ensure-flink-runner] Installed portable JDK 17"
        return $true
    } catch {
        Write-Warning "[ensure-flink-runner] Failed to install Java 17: $_"
        return $false
    }
}

function Ensure-Maven {
    try { & mvn -v | Out-Null; if ($LASTEXITCODE -eq 0) { return $true } } catch { }
    Write-Host "[ensure-flink-runner] Maven not present - attempting portable install"
    try {
        $mvnDir = Join-Path $RunnerDir '.maven'
        if (Test-Path $mvnDir) { Remove-Item $mvnDir -Recurse -Force }
        New-Item -ItemType Directory -Path $mvnDir | Out-Null
        $mvnVersion = '3.9.6'
        if ($IsWindows) {
            $zip = Join-Path $env:TEMP 'maven.zip'
            $url = "https://archive.apache.org/dist/maven/maven-3/$mvnVersion/binaries/apache-maven-$mvnVersion-bin.zip"
            Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $zip
            Expand-Archive -Path $zip -DestinationPath $mvnDir -Force
            Remove-Item $zip -Force
            $inner = Get-ChildItem $mvnDir | Where-Object { $_.PsIsContainer } | Select-Object -First 1
            if ($inner) { Get-ChildItem $inner.FullName -Force | Move-Item -Destination $mvnDir -Force }
        } else {
            $tar = "/tmp/maven.tar.gz"
            $url = "https://archive.apache.org/dist/maven/maven-3/$mvnVersion/binaries/apache-maven-$mvnVersion-bin.tar.gz"
            Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $tar
            tar -xf $tar -C $mvnDir --strip-components=1
            rm $tar
        }
        $env:MAVEN_HOME = $mvnDir
        $env:Path = (Join-Path $mvnDir 'bin') + [IO.Path]::PathSeparator + $env:Path
        return $true
    } catch {
        Write-Warning "[ensure-flink-runner] Failed to install Maven: $_"
        return $false
    }
}

$javaOk = Ensure-Java17
$mavenOk = Ensure-Maven
if (-not ($javaOk -and $mavenOk)) {
    New-PlaceholderJar "Missing toolchain (JavaOk=$javaOk MavenOk=$mavenOk)"
    exit 0
}

Write-Host '[ensure-flink-runner] Building shaded JAR via Maven'
try {
    Push-Location $RunnerDir
    & mvn -q -DskipTests package
    Pop-Location
} catch {
    Write-Warning "[ensure-flink-runner] Maven build failed: $_"
    New-PlaceholderJar "Maven build failed"
    exit 0
}

if (!(Test-Path $jarPath)) { New-PlaceholderJar "Jar missing after build" } else { Write-Host "[ensure-flink-runner] Built JAR: $jarPath" }
exit 0
