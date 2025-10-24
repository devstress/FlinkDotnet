@echo off
REM FlinkJobGateway Startup Script for Windows
REM This script configures and starts the FlinkJobGateway service

REM =====================================================
REM CONFIGURATION - Customize these values for your environment
REM =====================================================

REM Flink Cluster Configuration
if "%FLINK_CLUSTER_HOST%"=="" set FLINK_CLUSTER_HOST=localhost
if "%FLINK_CLUSTER_PORT%"=="" set FLINK_CLUSTER_PORT=8081

REM Kafka Bootstrap Servers (optional - only if using Kafka sources/sinks)
if "%KAFKA_BOOTSTRAP%"=="" set KAFKA_BOOTSTRAP=localhost:9092

REM Log File Path (directory where logs will be written)
if "%LOG_FILE_PATH%"=="" set LOG_FILE_PATH=.\logs

REM AspNetCore Environment (Development, Production, Testing)
if "%ASPNETCORE_ENVIRONMENT%"=="" set ASPNETCORE_ENVIRONMENT=Production

REM AspNetCore URLs (HTTP endpoints to listen on)
if "%ASPNETCORE_URLS%"=="" set ASPNETCORE_URLS=http://localhost:5000

REM Aspire Service Discovery (optional - only when using Aspire orchestration)
REM set services__flink_jobmanager__http__0=http://localhost:8081

REM =====================================================
REM DO NOT MODIFY BELOW THIS LINE
REM =====================================================

set SCRIPT_DIR=%~dp0
set GATEWAY_BINARY=%SCRIPT_DIR%FlinkDotNet.JobGateway.exe

REM Create logs directory if it doesn't exist
if not exist "%LOG_FILE_PATH%" mkdir "%LOG_FILE_PATH%"

REM Check if binary exists
if not exist "%GATEWAY_BINARY%" (
    echo ERROR: FlinkJobGateway binary not found at %GATEWAY_BINARY%
    exit /b 1
)

echo ========================================
echo FlinkJobGateway - Starting
echo ========================================
echo Flink Cluster: http://%FLINK_CLUSTER_HOST%:%FLINK_CLUSTER_PORT%
echo Kafka Bootstrap: %KAFKA_BOOTSTRAP%
echo Log Directory: %LOG_FILE_PATH%
echo Environment: %ASPNETCORE_ENVIRONMENT%
echo Listening on: %ASPNETCORE_URLS%
echo ========================================
echo.

REM Start the gateway
"%GATEWAY_BINARY%"
