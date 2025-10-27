#!/bin/bash
# Run LocalTesting with LEARNINGCOURSE mode enabled
# This script sets the LEARNINGCOURSE environment variable and starts the Aspire host

echo "========================================"
echo "Starting LocalTesting in LearningCourse Mode"
echo "========================================"
echo ""

export LEARNINGCOURSE=true
echo "[INFO] Environment variable set: LEARNINGCOURSE=$LEARNINGCOURSE"
echo ""

echo "[INFO] Starting LocalTesting.FlinkSqlAppHost..."
echo ""

dotnet run --project LocalTesting.FlinkSqlAppHost --configuration Release

echo ""
echo "========================================"
echo "LocalTesting stopped"
echo "========================================"