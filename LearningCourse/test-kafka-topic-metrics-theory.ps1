#!/usr/bin/env pwsh
# Test theory: Kafka JMX metrics appear only after consumption

Write-Host "=== TESTING KAFKA TOPIC METRICS THEORY ===" -ForegroundColor Cyan

# Get Kafka container
$kafkaContainer = docker ps --filter "name=kafka" --format "{{.Names}}" | Select-Object -First 1
if (-not $kafkaContainer) {
    Write-Host "❌ Kafka container not found" -ForegroundColor Red
    exit 1
}
Write-Host "✅ Kafka container: $kafkaContainer" -ForegroundColor Green

# Get JMX exporter container and port
$jmxContainer = docker ps --filter "name=kafka-exporter" --format "{{.Names}}" | Select-Object -First 1
if (-not $jmxContainer) {
    Write-Host "❌ JMX Exporter container not found" -ForegroundColor Red
    exit 1
}
$jmxPort = docker port $jmxContainer 5556 2>$null | ForEach-Object { $_ -replace '.*:', '' } | Select-Object -First 1
if (-not $jmxPort) {
    Write-Host "❌ JMX Exporter port not found" -ForegroundColor Red
    exit 1
}
Write-Host "✅ JMX Exporter port: $jmxPort" -ForegroundColor Green

$testTopic = "metrics-theory-test-$(Get-Random -Maximum 99999)"
Write-Host "`n📝 Test topic: $testTopic" -ForegroundColor Yellow

# Step 1: Create topic
Write-Host "`n1️⃣ Creating topic..." -ForegroundColor Cyan
docker exec $kafkaContainer kafka-topics --create --topic $testTopic --bootstrap-server localhost:9092 --partitions 1 --replication-factor 1 2>&1 | Out-Null
Start-Sleep -Seconds 2

# Step 2: Check metrics BEFORE producing (should have no topic label)
Write-Host "`n2️⃣ Checking metrics BEFORE producing messages..." -ForegroundColor Cyan
$beforeProduce = curl -s "http://localhost:$jmxPort/metrics" | Select-String -Pattern "kafka_server_brokertopicmetrics.*$testTopic"
if ($beforeProduce) {
    Write-Host "   Found metrics (unexpected):" -ForegroundColor Yellow
    $beforeProduce | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   ✅ No metrics found (EXPECTED - topic exists but no activity)" -ForegroundColor Green
}

# Step 3: Produce 10 messages
Write-Host "`n3️⃣ Producing 10 messages..." -ForegroundColor Cyan
for ($i = 1; $i -le 10; $i++) {
    $msg = "test-message-$i"
    echo $msg | docker exec -i $kafkaContainer kafka-console-producer --topic $testTopic --bootstrap-server localhost:9092 2>&1 | Out-Null
}
Start-Sleep -Seconds 3

# Step 4: Check metrics AFTER producing (might appear now)
Write-Host "`n4️⃣ Checking metrics AFTER producing messages..." -ForegroundColor Cyan
$afterProduce = curl -s "http://localhost:$jmxPort/metrics" | Select-String -Pattern "kafka_server_brokertopicmetrics.*$testTopic"
if ($afterProduce) {
    Write-Host "   ✅ Found metrics (production triggered JMX):" -ForegroundColor Green
    $afterProduce | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   ❌ No metrics found (production alone does not trigger topic metrics)" -ForegroundColor Yellow
}

# Step 5: Start a consumer (consume messages)
Write-Host "`n5️⃣ Consuming messages (starting consumer)..." -ForegroundColor Cyan
$consumeJob = Start-Job -ScriptBlock {
    param($container, $topic)
    docker exec $container kafka-console-consumer --topic $topic --bootstrap-server localhost:9092 --from-beginning --max-messages 10 2>&1
} -ArgumentList $kafkaContainer, $testTopic

# Wait for consumer to finish
Wait-Job $consumeJob -Timeout 10 | Out-Null
$consumeOutput = Receive-Job $consumeJob
Remove-Job $consumeJob -Force

$messagesConsumed = ($consumeOutput | Where-Object { $_ -match "test-message" }).Count
Write-Host "   📨 Messages consumed: $messagesConsumed" -ForegroundColor Cyan

# Step 6: Wait for JMX to update
Write-Host "`n6️⃣ Waiting 10 seconds for JMX metrics to update..." -ForegroundColor Cyan
Start-Sleep -Seconds 10

# Step 7: Check metrics AFTER consuming
Write-Host "`n7️⃣ Checking metrics AFTER consuming messages..." -ForegroundColor Cyan
$afterConsume = curl -s "http://localhost:$jmxPort/metrics" | Select-String -Pattern "kafka_server_brokertopicmetrics.*$testTopic"
if ($afterConsume) {
    Write-Host "   ✅ METRICS FOUND AFTER CONSUMPTION!" -ForegroundColor Green
    Write-Host "   Theory CONFIRMED: Consumption triggers topic-specific metrics`n" -ForegroundColor Green
    $afterConsume | ForEach-Object { Write-Host "   $_" }
} else {
    Write-Host "   ❌ No metrics found even after consumption" -ForegroundColor Red
    Write-Host "   Theory REJECTED: May need longer wait or different trigger`n" -ForegroundColor Red
}

# Step 8: Clean up test topic
Write-Host "`n8️⃣ Cleaning up test topic..." -ForegroundColor Cyan
docker exec $kafkaContainer kafka-topics --delete --topic $testTopic --bootstrap-server localhost:9092 2>&1 | Out-Null
Write-Host "   ✅ Test topic deleted" -ForegroundColor Green

Write-Host "`n=== TEST COMPLETE ===" -ForegroundColor Cyan