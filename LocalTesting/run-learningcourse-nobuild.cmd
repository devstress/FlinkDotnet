@echo off
REM Run LocalTesting in LEARNINGCOURSE mode without rebuilding
REM This starts the Aspire dashboard with full observability stack (Redis, Prometheus, Grafana)

echo ========================================
echo  Starting LocalTesting (LEARNINGCOURSE Mode)
echo ========================================
echo.
echo This will start:
echo   - Flink Cluster (JobManager + TaskManager + SQL Gateway)
echo   - Kafka with JMX metrics
echo   - FlinkDotNet Gateway
echo   - Temporal workflow server
echo   - Redis (for LearningCourse exercises)
echo   - Prometheus (metrics collection)
echo   - Grafana (metrics visualization)
echo.
echo Press Ctrl+C to stop when done
echo.

set LEARNINGCOURSE=true
dotnet run --project LocalTesting.FlinkSqlAppHost --no-build --configuration Release