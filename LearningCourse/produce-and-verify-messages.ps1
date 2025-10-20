#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Produce messages to Kafka and verify they're actually there
#>

param(
    [int]$MessageCount = 100,
    [string]$TopicName = "observability_input_day05"
)

Write-Host "`n====== KAFKA MESSAGE PRODUCER & VERIFIER ======" -ForegroundColor Cyan
Write-Host "Topic: $TopicName" -ForegroundColor Yellow
Write-Host "Messages: $MessageCount`n" -ForegroundColor Yellow

# Find Kafka container
$kafkaContainer = docker ps --filter "ancestor=confluentinc/confluent-local:7.9.0" --format "{{.Names}}"
if (-not $kafkaContainer) {
    Write-Host "ERROR: Kafka container not found!" -ForegroundColor Red
    exit 1
}
Write-Host "Kafka container: $kafkaContainer`n" -ForegroundColor Green

# List existing topics
Write-Host "Existing topics:" -ForegroundColor Cyan
docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --list 2>&1
Write-Host ""

# Delete topic if exists to start fresh
Write-Host "Deleting topic if exists..." -ForegroundColor Yellow
docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --delete --topic $TopicName 2>&1 | Out-Null
Start-Sleep -Seconds 2

# Create topic with explicit config
Write-Host "Creating topic '$TopicName'..." -ForegroundColor Cyan
$createResult = docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --create --topic $TopicName --partitions 3 --replication-factor 1 2>&1
Write-Host $createResult
Start-Sleep -Seconds 3

# Verify topic exists
Write-Host "`nVerifying topic exists..." -ForegroundColor Cyan
$topicList = docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --list 2>&1
if ($topicList -match $TopicName) {
    Write-Host "Topic verified!`n" -ForegroundColor Green
} else {
    Write-Host "ERROR: Topic not found after creation!" -ForegroundColor Red
    exit 1
}

# Describe topic
Write-Host "Topic details:" -ForegroundColor Cyan
docker exec $kafkaContainer kafka-topics --bootstrap-server localhost:9092 --describe --topic $TopicName 2>&1
Write-Host ""

# Produce messages using a file (more reliable)
Write-Host "Producing $MessageCount messages..." -ForegroundColor Cyan
$tempFile = "/tmp/messages-$([guid]::NewGuid()).txt"

# Create message file
$messages = 1..$MessageCount | ForEach-Object { "Message number $_" }
$messagesText = $messages -join "`n"

# Write messages to container's filesystem
$writeCmd = @"
cat > $tempFile << 'EOF'
$messagesText
EOF
"@

docker exec -i $kafkaContainer sh -c $writeCmd

# Produce from file
Write-Host "Sending messages from file..." -ForegroundColor Gray
$produceResult = docker exec $kafkaContainer sh -c "cat $tempFile | kafka-console-producer --bootstrap-server localhost:9092 --topic $TopicName 2>&1"
Write-Host $produceResult

# Clean up temp file
docker exec $kafkaContainer rm $tempFile 2>&1 | Out-Null

# Wait for messages to be written
Write-Host "`nWaiting 5 seconds for messages to be committed..." -ForegroundColor Yellow
Start-Sleep -Seconds 5

# Verify messages were received
Write-Host "`nVerifying messages in topic..." -ForegroundColor Cyan
$verifyCmd = "kafka-console-consumer --bootstrap-server localhost:9092 --topic $TopicName --from-beginning --max-messages $MessageCount --timeout-ms 10000 2>&1"
$consumeResult = docker exec $kafkaContainer sh -c $verifyCmd

$receivedCount = ($consumeResult | Where-Object { $_ -notmatch "Processed" }).Count
Write-Host "Messages verified in topic: $receivedCount" -ForegroundColor Green

if ($receivedCount -lt $MessageCount) {
    Write-Host "WARNING: Expected $MessageCount but found $receivedCount!" -ForegroundColor Yellow
} else {
    Write-Host "SUCCESS: All messages verified!`n" -ForegroundColor Green
}

# Check lag/offset
Write-Host "Checking consumer group offsets..." -ForegroundColor Cyan
docker exec $kafkaContainer kafka-consumer-groups --bootstrap-server localhost:9092 --all-groups --describe 2>&1 | Select-Object -First 20

Write-Host "`nDone! Messages should now be in Kafka." -ForegroundColor Green
Write-Host "Wait 15-30 seconds for JMX exporter to pick up the metrics." -ForegroundColor Yellow