#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Debug Day 05 Kafka topic metrics issue
.DESCRIPTION
    Creates topic, produces messages, and checks JMX metrics
#>

param(
    [int]$MessageCount = 100,
    [string]$TopicName = "observability_input_day05"
)

$ErrorActionPreference = "Continue"

Write-Host "`n====== DAY 05 KAFKA METRICS DEBUG ======" -ForegroundColor Cyan
Write-Host "Topic: $TopicName" -ForegroundColor Yellow
Write-Host "Messages: $MessageCount`n" -ForegroundColor Yellow

# Find Kafka container
Write-Host "Step 1: Finding Kafka container..." -ForegroundColor Cyan
$kafkaContainer = docker ps --filter "ancestor=confluentinc/confluent-local:7.9.0" --format "{{.Names}}"
if (-not $kafkaContainer) {
    Write-Host "ERROR: Kafka container not found!" -ForegroundColor Red
    exit 1
}
Write-Host "Found: $kafkaContainer`n" -ForegroundColor Green

# Find JMX Exporter container
Write-Host "Step 2: Finding JMX Exporter container..." -ForegroundColor Cyan
$jmxContainer = docker ps --filter "ancestor=bitnami/jmx-exporter:latest" --format "{{.Names}}"
if (-not $jmxContainer) {
    Write-Host "ERROR: JMX Exporter container not found!" -ForegroundColor Red
    exit 1
}
Write-Host "Found: $jmxContainer" -ForegroundColor Green

# Get JMX exporter port
$jmxPort = docker port $jmxContainer 5556 2>$null | Select-Object -First 1
if ($jmxPort -match "127.0.0.1:(\d+)") {
    $jmxHostPort = $matches[1]
    Write-Host "Port: $jmxHostPort`n" -ForegroundColor Green
} else {
    Write-Host "ERROR: Could not find JMX Exporter port!" -ForegroundColor Red
    exit 1
}

# Create topic if needed
Write-Host "Step 3: Checking topic..." -ForegroundColor Cyan
$topicList = docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --list 2>&1
if ($topicList -match $TopicName) {
    Write-Host "Topic exists`n" -ForegroundColor Green
} else {
    Write-Host "Creating topic..." -ForegroundColor Yellow
    docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --create --topic $TopicName --partitions 3 --replication-factor 1 2>&1 | Out-Null
    Start-Sleep -Seconds 2
    Write-Host "Topic created`n" -ForegroundColor Green
}

# Produce messages
Write-Host "Step 4: Producing $MessageCount messages..." -ForegroundColor Cyan
$startTime = Get-Date

for ($i = 1; $i -le $MessageCount; $i++) {
    $message = "Message-$i-$(Get-Date -Format 'HH:mm:ss.fff')"
    $produceCmd = "echo '$message' | kafka-console-producer --bootstrap-server localhost:9092 --topic $TopicName"
    docker exec -i $kafkaContainer sh -c $produceCmd 2>&1 | Out-Null
    
    if ($i % 25 -eq 0) {
        Write-Host "  Produced $i messages..." -ForegroundColor Gray
    }
}

$duration = ((Get-Date) - $startTime).TotalSeconds
Write-Host "Completed in $($duration.ToString('F2'))s`n" -ForegroundColor Green

# Wait for metrics to update
Write-Host "Waiting 5 seconds for metrics..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Query JMX Exporter
Write-Host "`nStep 5: Querying JMX Exporter..." -ForegroundColor Cyan
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$jmxHostPort/metrics" -UseBasicParsing -TimeoutSec 10
    $metrics = $response.Content
    
    $totalLines = ($metrics -split "`n").Count
    Write-Host "Total metrics lines: $totalLines" -ForegroundColor Green
    
    # Search for BrokerTopicMetrics
    Write-Host "`nSearching for BrokerTopicMetrics..." -ForegroundColor Cyan
    $brokerLines = $metrics -split "`n" | Where-Object { $_ -match "BrokerTopicMetrics" }
    
    if ($brokerLines) {
        Write-Host "Found $($brokerLines.Count) BrokerTopicMetrics lines" -ForegroundColor Green
        Write-Host "`nFirst 10 BrokerTopicMetrics:" -ForegroundColor Yellow
        $brokerLines | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
    } else {
        Write-Host "NO BrokerTopicMetrics found!" -ForegroundColor Red
    }
    
    # Search for specific topic
    Write-Host "`nSearching for topic '$TopicName'..." -ForegroundColor Cyan
    $topicLines = $metrics -split "`n" | Where-Object { $_ -match $TopicName }
    
    if ($topicLines) {
        Write-Host "Found $($topicLines.Count) lines with topic name" -ForegroundColor Green
        Write-Host "`nTopic metrics:" -ForegroundColor Yellow
        $topicLines | ForEach-Object { Write-Host "  $_" }
    } else {
        Write-Host "NO metrics found for topic '$TopicName'!" -ForegroundColor Red
        
        # Show what topics are available
        Write-Host "`nSearching for ANY topics in metrics..." -ForegroundColor Yellow
        $allTopics = $metrics -split "`n" | Where-Object { $_ -match 'topic=' }
        if ($allTopics) {
            Write-Host "Found metrics with topics:" -ForegroundColor Green
            $allTopics | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" }
        } else {
            Write-Host "NO topic labels found in any metrics!" -ForegroundColor Red
        }
    }
    
    # Search for MessagesInPerSec
    Write-Host "`nSearching for MessagesInPerSec..." -ForegroundColor Cyan
    $msgLines = $metrics -split "`n" | Where-Object { $_ -match "messagesinpersec" -or $_ -match "MessagesInPerSec" }
    
    if ($msgLines) {
        Write-Host "Found $($msgLines.Count) MessagesInPerSec metrics" -ForegroundColor Green
        $msgLines | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" }
    } else {
        Write-Host "NO MessagesInPerSec metrics found!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "ERROR querying JMX Exporter: $_" -ForegroundColor Red
}

# Check JMX Exporter config
Write-Host "`nStep 6: Checking JMX Exporter config..." -ForegroundColor Cyan
$config = docker exec $jmxContainer cat /opt/bitnami/jmx-exporter/exporter.yml 2>&1
Write-Host $config -ForegroundColor Gray

# Check JMX Exporter logs
Write-Host "`nStep 7: JMX Exporter logs (last 30 lines)..." -ForegroundColor Cyan
$logs = docker logs --tail 30 $jmxContainer 2>&1
Write-Host $logs -ForegroundColor Gray

# Check Kafka JMX environment
Write-Host "`nStep 8: Kafka JMX environment..." -ForegroundColor Cyan
$kafkaEnv = docker exec $kafkaContainer env | Select-String "KAFKA_JMX"
$kafkaEnv | ForEach-Object { Write-Host "  $_" }

Write-Host "`n====== SUMMARY ======" -ForegroundColor Cyan
Write-Host "Kafka: $kafkaContainer" -ForegroundColor White
Write-Host "JMX Exporter: $jmxContainer (port $jmxHostPort)" -ForegroundColor White
Write-Host "Topic: $TopicName" -ForegroundColor White
Write-Host "Messages: $MessageCount produced" -ForegroundColor White

Write-Host "`nDone! Check output above for issues." -ForegroundColor Green