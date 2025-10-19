#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Debug Day 05 Kafka topic metrics issue - comprehensive container inspection
.DESCRIPTION
    This script creates the topic, produces messages, and debugs why Kafka JMX metrics
    aren't showing topic-specific BrokerTopicMetrics in Prometheus
#>

param(
    [int]$MessageCount = 100,
    [string]$TopicName = "observability_input_day05"
)

$ErrorActionPreference = "Continue"

Write-Host "`n🔍 ===== DAY 05 KAFKA METRICS DEBUG =====" -ForegroundColor Cyan
Write-Host "Topic: $TopicName" -ForegroundColor Yellow
Write-Host "Messages to produce: $MessageCount`n" -ForegroundColor Yellow

# Step 1: Find Kafka container
Write-Host "📦 Step 1: Locating Kafka container..." -ForegroundColor Cyan
$kafkaContainer = docker ps --filter "ancestor=confluentinc/confluent-local:7.9.0" --format "{{.Names}}"
if (-not $kafkaContainer) {
    Write-Host "❌ Kafka container not found! Is LocalTesting running?" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Found Kafka container: $kafkaContainer`n" -ForegroundColor Green

# Step 2: Find Kafka JMX Exporter container
Write-Host "📦 Step 2: Locating Kafka JMX Exporter container..." -ForegroundColor Cyan
$jmxContainer = docker ps --filter "ancestor=bitnami/jmx-exporter:latest" --format "{{.Names}}"
if (-not $jmxContainer) {
    Write-Host "❌ Kafka JMX Exporter container not found!" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Found JMX Exporter container: $jmxContainer" -ForegroundColor Green

# Get JMX exporter port
$jmxPort = docker port $jmxContainer 5556 2>$null | Select-Object -First 1
if ($jmxPort -match "127.0.0.1:(\d+)") {
    $jmxHostPort = $matches[1]
    Write-Host "✅ JMX Exporter port: $jmxHostPort`n" -ForegroundColor Green
} else {
    Write-Host "❌ Could not determine JMX Exporter port!" -ForegroundColor Red
    exit 1
}

# Step 3: Check if topic exists
Write-Host "📋 Step 3: Checking if topic exists..." -ForegroundColor Cyan
$topicExists = docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --list 2>&1 | Select-String -Pattern "^$TopicName$"
if ($topicExists) {
    Write-Host "✅ Topic '$TopicName' already exists" -ForegroundColor Green
} else {
    Write-Host "⚠️  Topic '$TopicName' does not exist, creating..." -ForegroundColor Yellow
    docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --create --topic $TopicName --partitions 3 --replication-factor 1 2>&1
    Start-Sleep -Seconds 2
    Write-Host "✅ Topic created`n" -ForegroundColor Green
}

# Step 4: Produce messages to the topic
Write-Host "📤 Step 4: Producing $MessageCount messages to topic '$TopicName'..." -ForegroundColor Cyan
$startTime = Get-Date

for ($i = 1; $i -le $MessageCount; $i++) {
    $message = "Message-$i-$(Get-Date -Format 'HH:mm:ss.fff')"
    $key = "key-$i"
    
    # Use kafka-console-producer to send message
    $produceCmd = "echo `"$key`:$message`" | kafka-console-producer --bootstrap-server localhost:9092 --topic $TopicName --property `"parse.key=true`" --property `"key.separator=:`""
    docker exec -i $kafkaContainer sh -c $produceCmd 2>&1 | Out-Null
    
    if ($i % 25 -eq 0) {
        Write-Host "  Produced $i messages..." -ForegroundColor Gray
    }
}

$endTime = Get-Date
$duration = ($endTime - $startTime).TotalSeconds
Write-Host "✅ Produced $MessageCount messages in $($duration.ToString('F2')) seconds`n" -ForegroundColor Green

# Step 5: Verify messages in topic
Write-Host "📊 Step 5: Verifying message count in topic..." -ForegroundColor Cyan
Start-Sleep -Seconds 2
$consumerOutput = docker exec $kafkaContainer kafka-console-consumer --bootstrap-server localhost:9092 --topic $TopicName --from-beginning --max-messages $MessageCount --timeout-ms 5000 2>&1
$messageLines = ($consumerOutput | Measure-Object -Line).Lines
Write-Host "✅ Topic contains approximately $messageLines messages`n" -ForegroundColor Green

# Step 6: Check Kafka JMX port connectivity
Write-Host "🔌 Step 6: Checking Kafka JMX port (9101) connectivity..." -ForegroundColor Cyan
$jmxTest = docker exec $kafkaContainer sh -c "nc -zv localhost 9101 2>&1"
Write-Host "JMX connectivity test: $jmxTest" -ForegroundColor Gray

# Check if JMX is listening
$jmxListening = docker exec $kafkaContainer sh -c "netstat -tuln | grep 9101"
if ($jmxListening) {
    Write-Host "✅ Kafka JMX port 9101 is listening" -ForegroundColor Green
} else {
    Write-Host "❌ Kafka JMX port 9101 NOT listening!" -ForegroundColor Red
}
Write-Host ""

# Step 7: Check Kafka JMX environment variables
Write-Host "🔧 Step 7: Checking Kafka JMX environment variables..." -ForegroundColor Cyan
$kafkaEnv = docker exec $kafkaContainer env | Select-String -Pattern "KAFKA_JMX"
Write-Host "Kafka JMX Environment:" -ForegroundColor Gray
$kafkaEnv | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
Write-Host ""

# Step 8: Query JMX Exporter directly
Write-Host "📊 Step 8: Querying JMX Exporter for Kafka metrics..." -ForegroundColor Cyan
try {
    $jmxMetrics = Invoke-WebRequest -Uri "http://localhost:$jmxHostPort/metrics" -UseBasicParsing -TimeoutSec 10
    $metricsText = $jmxMetrics.Content
    
    # Count total metrics
    $totalLines = ($metricsText -split "`n").Count
    Write-Host "✅ JMX Exporter responding: $totalLines lines of metrics" -ForegroundColor Green
    
    # Search for BrokerTopicMetrics
    Write-Host "`n🔍 Searching for BrokerTopicMetrics..." -ForegroundColor Cyan
    $brokerMetrics = $metricsText -split "`n" | Select-String -Pattern "kafka_server_BrokerTopicMetrics" -CaseSensitive:$false
    
    if ($brokerMetrics) {
        Write-Host "✅ Found BrokerTopicMetrics! Count: $($brokerMetrics.Count)" -ForegroundColor Green
        Write-Host "`nSample BrokerTopicMetrics (first 10):" -ForegroundColor Yellow
        $brokerMetrics | Select-Object -First 10 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        
        # Search for specific topic
        Write-Host "`n🔍 Searching for topic '$TopicName' in metrics..." -ForegroundColor Cyan
        $topicMetrics = $metricsText -split "`n" | Select-String -Pattern $TopicName
        
        if ($topicMetrics) {
            Write-Host "✅ Found metrics for topic '$TopicName'! Count: $($topicMetrics.Count)" -ForegroundColor Green
            Write-Host "`nTopic-specific metrics:" -ForegroundColor Yellow
            $topicMetrics | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
        } else {
            Write-Host "❌ NO metrics found for topic '$TopicName'" -ForegroundColor Red
            Write-Host "`n🔍 Available topics in metrics:" -ForegroundColor Yellow
            $allTopicMetrics = $metricsText -split "`n" | Select-String -Pattern 'topic="([^"]+)"'
            $uniqueTopics = $allTopicMetrics | ForEach-Object {
                if ($_ -match 'topic="([^"]+)"') { $matches[1] }
            } | Select-Object -Unique | Sort-Object
            
            if ($uniqueTopics) {
                $uniqueTopics | ForEach-Object { Write-Host "  - $_" -ForegroundColor Gray }
            } else {
                Write-Host "  ⚠️  No topics found in any metrics!" -ForegroundColor Yellow
            }
        }
    } else {
        Write-Host "❌ NO BrokerTopicMetrics found in JMX Exporter output!" -ForegroundColor Red
        Write-Host "`n🔍 Available metric types:" -ForegroundColor Yellow
        $metricTypes = $metricsText -split "`n" | Select-String -Pattern "^kafka_" | ForEach-Object {
            $line = $_.ToString()
            if ($line -match "^(kafka_\w+)") { $matches[1] }
        } | Select-Object -Unique | Sort-Object
        
        $metricTypes | Select-Object -First 20 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    }
    
    # Check for MessagesInPerSec specifically
    Write-Host "`n🔍 Searching for MessagesInPerSec metrics..." -ForegroundColor Cyan
    $messagesInMetrics = $metricsText -split "`n" | Select-String -Pattern "messagesinpersec" -CaseSensitive:$false
    if ($messagesInMetrics) {
        Write-Host "✅ Found MessagesInPerSec metrics! Count: $($messagesInMetrics.Count)" -ForegroundColor Green
        $messagesInMetrics | Select-Object -First 5 | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }
    } else {
        Write-Host "❌ NO MessagesInPerSec metrics found!" -ForegroundColor Red
    }
    
} catch {
    Write-Host "❌ Failed to query JMX Exporter: $_" -ForegroundColor Red
}

# Step 9: Inspect JMX Exporter configuration
Write-Host "`n🔧 Step 9: Checking JMX Exporter configuration..." -ForegroundColor Cyan
$jmxConfig = docker exec $jmxContainer cat /opt/bitnami/jmx-exporter/exporter.yml 2>&1
Write-Host "JMX Exporter Config:" -ForegroundColor Gray
Write-Host $jmxConfig -ForegroundColor Gray

# Step 10: Check JMX Exporter logs
Write-Host "`n📋 Step 10: Checking JMX Exporter container logs (last 50 lines)..." -ForegroundColor Cyan
$jmxLogs = docker logs --tail 50 $jmxContainer 2>&1
Write-Host $jmxLogs -ForegroundColor Gray

# Step 11: Test direct JMX connection from exporter container
Write-Host "`n🔌 Step 11: Testing JMX connection from exporter to Kafka..." -ForegroundColor Cyan
$jmxConnTest = docker exec $jmxContainer sh -c "nc -zv kafka 9101 2>&1"
Write-Host "JMX connection test result: $jmxConnTest" -ForegroundColor Gray

# Step 12: Check Kafka broker metrics via JMX
Write-Host "`n📊 Step 12: Attempting to query Kafka JMX directly..." -ForegroundColor Cyan
Write-Host "Note: This requires JMX client tools in Kafka container" -ForegroundColor Yellow

# Summary
Write-Host "`n📝 ===== SUMMARY =====" -ForegroundColor Cyan
Write-Host "1. Kafka Container: $kafkaContainer" -ForegroundColor White
Write-Host "2. JMX Exporter Container: $jmxContainer" -ForegroundColor White
Write-Host "3. JMX Exporter Port: $jmxHostPort" -ForegroundColor White
Write-Host "4. Topic: $TopicName" -ForegroundColor White
Write-Host "5. Messages Produced: $MessageCount" -ForegroundColor White
Write-Host "6. Messages Verified: ~$messageLines" -ForegroundColor White

Write-Host "`n🔍 DIAGNOSTIC QUESTIONS:" -ForegroundColor Yellow
Write-Host "1. Are BrokerTopicMetrics present in JMX output?" -ForegroundColor White
Write-Host "2. Is the topic name '$TopicName' present in any metrics?" -ForegroundColor White
Write-Host "3. Is Kafka JMX port 9101 accessible from JMX Exporter container?" -ForegroundColor White
Write-Host "4. Does JMX Exporter config have correct patterns for BrokerTopicMetrics?" -ForegroundColor White

Write-Host "`n✅ Debug script completed!" -ForegroundColor Green
Write-Host "`n💡 Next steps:" -ForegroundColor Cyan
Write-Host "1. Review the output above to identify the root cause" -ForegroundColor White
Write-Host "2. Check if BrokerTopicMetrics require specific JMX exporter patterns" -ForegroundColor White
Write-Host "3. Verify Kafka JMX configuration is correct" -ForegroundColor White
Write-Host "4. Try querying Prometheus directly: http://localhost:9090" -ForegroundColor White