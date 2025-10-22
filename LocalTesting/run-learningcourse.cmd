@echo off
REM Run LocalTesting with LEARNINGCOURSE mode enabled
REM This script sets the LEARNINGCOURSE environment variable and starts the Aspire host

echo ========================================
echo Starting LocalTesting in LearningCourse Mode
echo ========================================
echo.

REM Kill any existing dotnet.exe processes to ensure clean start
echo [INFO] Killing existing dotnet.exe processes...
taskkill /F /IM dotnet.exe >nul 2>&1
if %ERRORLEVEL% EQU 0 (
    echo [INFO] Existing processes terminated
) else (
    echo [INFO] No existing processes found
)
echo.

set LEARNINGCOURSE=true
echo [INFO] Environment variable set: LEARNINGCOURSE=%LEARNINGCOURSE%
echo.

echo [INFO] Starting LocalTesting.FlinkSqlAppHost...
echo.

dotnet run --project LocalTesting.FlinkSqlAppHost

echo.
echo ========================================
echo LocalTesting stopped
echo ========================================