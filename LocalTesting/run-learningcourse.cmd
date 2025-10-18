@echo off
REM Run LocalTesting with LEARNINGCOURSE mode enabled
REM This script sets the LEARNINGCOURSE environment variable and starts the Aspire host

echo ========================================
echo Starting LocalTesting in LearningCourse Mode
echo ========================================
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