@echo off
REM Flink.NET Build Script - Windows CMD Wrapper
REM This batch file runs the cross-platform PowerShell build script

setlocal enabledelayedexpansion

echo ================================================
echo 🔨 Flink.NET Build Script
echo ================================================
echo.

REM Store original arguments for help detection and no-pause detection
set "help_requested="
set "no_pause="
for %%a in (%*) do (
    if /i "%%a"=="-Help" set "help_requested=true"
    if /i "%%a"=="--help" set "help_requested=true"
    if "%%a"=="/?" set "help_requested=true"
    if "%%a"=="-h" set "help_requested=true"
    if /i "%%a"=="-NoPause" set "no_pause=true"
    if /i "%%a"=="--no-pause" set "no_pause=true"
)

REM Check if running in automated environment (CI/CD)
if defined CI set "no_pause=true"
if defined GITHUB_ACTIONS set "no_pause=true"
if defined BUILD_BUILDID set "no_pause=true"
if defined AGENT_ID set "no_pause=true"

REM Check if PowerShell Core (pwsh) is available
where pwsh >nul 2>&1
if %errorlevel% == 0 (
    echo Using PowerShell Core ^(pwsh^)...
    echo.
    pwsh -ExecutionPolicy Bypass -File "%~dp0build-all.ps1" %*
    set "ps_exit_code=%errorlevel%"
) else (
    REM Check if Windows PowerShell is available
    where powershell >nul 2>&1
    if %errorlevel% == 0 (
        echo Using Windows PowerShell...
        echo.
        powershell -ExecutionPolicy Bypass -File "%~dp0build-all.ps1" %*
        set "ps_exit_code=%errorlevel%"
    ) else (
        echo ERROR: No PowerShell found on this system.
        echo Please install PowerShell Core from: https://github.com/PowerShell/PowerShell
        echo Or ensure Windows PowerShell is available.
        echo.
        if not defined no_pause (
            echo Press any key to exit...
            pause >nul
        )
        exit /b 1
    )
)

REM Show completion message and wait for user input (unless help was requested or no-pause is set)
echo.
if "%ps_exit_code%"=="0" (
    echo ================================================
    echo ✅ Build script completed successfully!
    echo ================================================
) else (
    echo ================================================
    echo ❌ Build script completed with errors ^(Exit Code: %ps_exit_code%^)
    echo ================================================
)

REM Don't pause if help was requested, no-pause flag is set, or in automated environment
if not defined help_requested if not defined no_pause (
    echo.
    echo Press any key to exit...
    pause >nul
)

REM Pass through the exit code from PowerShell
exit /b %ps_exit_code%