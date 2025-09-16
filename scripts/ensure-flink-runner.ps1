param(
    [string]$RunnerDir = (Join-Path $PSScriptRoot '..' 'FlinkIRRunner')
)
$ErrorActionPreference = 'Stop'
Write-Host "[ensure-flink-runner] Runner directory: $RunnerDir"
if (!(Test-Path $RunnerDir)) { Write-Error "Runner directory not found: $RunnerDir" }
$jarPath = Join-Path $RunnerDir 'target' 'flink-ir-runner.jar'
if (Test-Path $jarPath) {
    Write-Host "[ensure-flink-runner] JAR already present: $jarPath"; exit 0
}
# Ensure target folder exists after build
# 1. JAVA
function Ensure-Java17 {
    try {
        $verOutput = & java -version 2>&1
        if ($LASTEXITCODE -eq 0) {
            if ($verOutput -match 'version "(?<v>[0-9]+)') {
                $major = [int]$Matches['v']
                if ($major -ge 17) { Write-Host "[ensure-flink-runner] Found Java $major"; return }
            }
            Write-Host "[ensure-flink-runner] Existing Java insufficient; downloading portable JDK 17"
        } else { Write-Host "[ensure-flink-runner] java command failed; installing JDK 17" }
    } catch { Write-Host "[ensure-flink-runner] Java not found; installing JDK 17" }
    $jdkDir = Join-Path $RunnerDir '.jdk'
    if (Test-Path $jdkDir) { Remove-Item $jdkDir -Recurse -Force }
    New-Item -ItemType Directory -Path $jdkDir | Out-Null
    if ($IsWindows) {
        $jdkZip = Join-Path $env:TEMP 'temurin17.zip'
        $url = 'https://github.com/adoptium/temurin17-binaries/releases/latest/download/OpenJDK17U-jdk_x64_windows_hotspot.zip'
    } elseif ($IsLinux) {
        $jdkZip = '/tmp/temurin17.tar.gz'
        $url = 'https://github.com/adoptium/temurin17-binaries/releases/latest/download/OpenJDK17U-jdk_x64_linux_hotspot.tar.gz'
    } else { $jdkZip = '/tmp/temurin17.tar.gz'; $url='https://github.com/adoptium/temurin17-binaries/releases/latest/download/OpenJDK17U-jdk_aarch64_mac_hotspot.tar.gz' }
    Write-Host "[ensure-flink-runner] Downloading JDK from $url"
    Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $jdkZip
    if ($IsWindows) {
        Expand-Archive -Path $jdkZip -DestinationPath $jdkDir -Force
        Remove-Item $jdkZip -Force
        $inner = Get-ChildItem $jdkDir | Where-Object { $_.PsIsContainer } | Select-Object -First 1
        if ($inner) { Get-ChildItem $inner.FullName -Force | Move-Item -Destination $jdkDir -Force }
    } else {
        tar -xf $jdkZip -C $jdkDir --strip-components=1
        rm $jdkZip
    }
    $env:JAVA_HOME = $jdkDir
    $env:Path = (Join-Path $jdkDir 'bin') + [IO.Path]::PathSeparator + $env:Path
    Write-Host "[ensure-flink-runner] Installed portable JDK 17 at $jdkDir"
}
# 2. Maven
function Ensure-Maven {
    try { & mvn -v | Out-Null; if ($LASTEXITCODE -eq 0) { Write-Host "[ensure-flink-runner] Maven present."; return } } catch {}
    $mvnDir = Join-Path $RunnerDir '.maven'
    if (Test-Path $mvnDir) { Remove-Item $mvnDir -Recurse -Force }
    New-Item -ItemType Directory -Path $mvnDir | Out-Null
    $mvnVersion = '3.9.6'
    if ($IsWindows) {
        $zip = Join-Path $env:TEMP 'maven.zip'
        $url = "https://dlcdn.apache.org/maven/maven-3/$mvnVersion/binaries/apache-maven-$mvnVersion-bin.zip"
        Write-Host "[ensure-flink-runner] Downloading Maven $mvnVersion from $url"
        Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $zip
        Expand-Archive -Path $zip -DestinationPath $mvnDir -Force
        Remove-Item $zip -Force
        $inner = Get-ChildItem $mvnDir | Where-Object { $_.PsIsContainer } | Select-Object -First 1
        if ($inner) { Get-ChildItem $inner.FullName -Force | Move-Item -Destination $mvnDir -Force }
    } else {
        $tar = "/tmp/maven.tar.gz"
        $url = "https://dlcdn.apache.org/maven/maven-3/$mvnVersion/binaries/apache-maven-$mvnVersion-bin.tar.gz"
        Write-Host "[ensure-flink-runner] Downloading Maven $mvnVersion from $url"
        Invoke-WebRequest -UseBasicParsing -Uri $url -OutFile $tar
        tar -xf $tar -C $mvnDir --strip-components=1
        rm $tar
    }
    $env:MAVEN_HOME = $mvnDir
    $env:Path = (Join-Path $mvnDir 'bin') + [IO.Path]::PathSeparator + $env:Path
    Write-Host "[ensure-flink-runner] Installed portable Maven at $mvnDir"
}
Ensure-Java17
Ensure-Maven
Write-Host '[ensure-flink-runner] Building shaded JAR via Maven'
Push-Location $RunnerDir
try { & mvn -q -DskipTests package } finally { Pop-Location }
if (!(Test-Path $jarPath)) { Write-Error "Failed to build flink-ir-runner.jar" }
Write-Host "[ensure-flink-runner] Built JAR: $jarPath"
