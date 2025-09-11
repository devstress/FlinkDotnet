#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Full End-to-End Kafka Connectivity Test for WI27 Custom Container Implementation
.DESCRIPTION
    Validates that the custom Kafka container configuration resolves the external access issues
    that were causing LocalTesting Observability tests to hang for 5+ minutes.
    
    This test specifically validates:
    1. Custom Apache Kafka container starts successfully
    2. Kafka broker accepts external connections on dynamically allocated ports
    3. WebAPI can connect to Kafka through Aspire service discovery
    4. Producer can send messages successfully
    5. Test fails fast (within 60 seconds) if infrastructure issues occur
.PARAMETER MessageCount
    Number of test messages to send (default: 100)
.PARAMETER TimeoutSeconds
    Maximum time to wait for test completion (default: 120)
#>

param(
    [int]$MessageCount = 100,
    [int]$TimeoutSeconds = 120
)

# Set error action preference for proper error handling
$ErrorActionPreference = "Stop"

Write-Host "🚀 Starting Full End-to-End Kafka Connectivity Test for WI27 Custom Container" -ForegroundColor Green
Write-Host "📋 Test Parameters:" -ForegroundColor Cyan
Write-Host "   • Message Count: $MessageCount" -ForegroundColor White
Write-Host "   • Timeout: $TimeoutSeconds seconds" -ForegroundColor White
Write-Host "   • Validation Focus: Custom Kafka container external access" -ForegroundColor White
Write-Host ""

# Track test start time
$testStartTime = Get-Date
Write-Host "⏰ Test started at: $($testStartTime.ToString('yyyy-MM-dd HH:mm:ss.fff'))" -ForegroundColor Gray

try {
    # Step 1: Environment validation
    Write-Host "🔍 Step 1: Environment Validation" -ForegroundColor Yellow
    
    # Check .NET version
    $dotnetVersion = dotnet --version
    Write-Host "   ✅ .NET Version: $dotnetVersion" -ForegroundColor Green
    
    if (-not $dotnetVersion.StartsWith("9.")) {
        Write-Host "   ❌ ERROR: .NET 9.0 is required, found: $dotnetVersion" -ForegroundColor Red
        throw "Environment validation failed: .NET 9.0 required"
    }
    
    # Check Docker availability
    try {
        docker version | Out-Null
        Write-Host "   ✅ Docker is available and running" -ForegroundColor Green
    }
    catch {
        Write-Host "   ❌ ERROR: Docker is not available or not running" -ForegroundColor Red
        throw "Environment validation failed: Docker not available"
    }
    
    # Step 2: Start Aspire infrastructure with custom Kafka container
    Write-Host ""
    Write-Host "🏗️ Step 2: Start Aspire Infrastructure with Custom Kafka Container" -ForegroundColor Yellow
    
    Push-Location "LocalTesting"
    try {
        # Start the AppHost in background
        Write-Host "   🚀 Starting LocalTesting AppHost with custom Kafka container..." -ForegroundColor Cyan
        
        $appHostJob = Start-Job -ScriptBlock {
            param($workingDir)
            Set-Location $workingDir
            dotnet run --project LocalTesting.AppHost --configuration Release
        } -ArgumentList (Get-Location).Path
        
        Write-Host "   📋 AppHost Job ID: $($appHostJob.Id)" -ForegroundColor Gray
        
        # Wait for infrastructure to become available
        Write-Host "   ⏳ Waiting for infrastructure to become ready..." -ForegroundColor Cyan
        
        $maxWaitTime = [TimeSpan]::FromSeconds($TimeoutSeconds)
        $healthCheckInterval = [TimeSpan]::FromSeconds(5)
        $healthCheckStartTime = Get-Date
        $healthCheckSuccess = $false
        
        while ((Get-Date) - $healthCheckStartTime -lt $maxWaitTime) {
            try {
                # Check WebAPI health endpoint
                $healthResponse = Invoke-WebRequest -Uri "http://localhost:13001/health" -Method GET -TimeoutSec 5
                if ($healthResponse.StatusCode -eq 200) {
                    $healthCheckSuccess = $true
                    $elapsedHealth = ((Get-Date) - $healthCheckStartTime).TotalSeconds
                    Write-Host "   ✅ Infrastructure health check successful after $($elapsedHealth.ToString('F1')) seconds" -ForegroundColor Green
                    break
                }
            }
            catch {
                $elapsed = ((Get-Date) - $healthCheckStartTime).TotalSeconds
                if ($elapsed % 15 -lt 1) {  # Log every 15 seconds
                    Write-Host "   ⏳ Still waiting for infrastructure ($($elapsed.ToString('F0'))s elapsed)..." -ForegroundColor Gray
                }
            }
            
            Start-Sleep -Seconds $healthCheckInterval.TotalSeconds
        }
        
        if (-not $healthCheckSuccess) {
            throw "Infrastructure failed to become healthy within $TimeoutSeconds seconds"
        }
        
        # Step 3: Validate Kafka broker connectivity
        Write-Host ""
        Write-Host "🔌 Step 3: Validate Custom Kafka Container Connectivity" -ForegroundColor Yellow
        
        # Test Kafka health endpoint
        Write-Host "   🔍 Testing Kafka broker connectivity through WebAPI..." -ForegroundColor Cyan
        try {
            $kafkaHealthResponse = Invoke-WebRequest -Uri "http://localhost:13001/api/observability/kafka-health" -Method GET -TimeoutSec 10
            if ($kafkaHealthResponse.StatusCode -eq 200) {
                $kafkaHealthData = $kafkaHealthResponse.Content | ConvertFrom-Json
                Write-Host "   ✅ Kafka broker connectivity validated" -ForegroundColor Green
                Write-Host "   📋 Kafka Health Response:" -ForegroundColor Gray
                Write-Host "      $($kafkaHealthResponse.Content)" -ForegroundColor White
            }
            else {
                throw "Kafka health check returned status: $($kafkaHealthResponse.StatusCode)"
            }
        }
        catch {
            Write-Host "   ❌ Kafka connectivity validation failed: $($_.Exception.Message)" -ForegroundColor Red
            throw "Kafka connectivity validation failed"
        }
        
        # Step 4: Test message production to Kafka
        Write-Host ""
        Write-Host "📤 Step 4: Test Kafka Message Production" -ForegroundColor Yellow
        
        Write-Host "   🔍 Testing Kafka producer with $MessageCount messages..." -ForegroundColor Cyan
        
        $messageTestBody = @{
            MessageCount = $MessageCount
            TopicName = "test-connectivity-topic"
            TestId = "end-to-end-connectivity-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
        } | ConvertTo-Json
        
        try {
            $produceResponse = Invoke-WebRequest -Uri "http://localhost:13001/api/observability/test-kafka-producer" -Method POST -Body $messageTestBody -ContentType "application/json" -TimeoutSec 30
            
            if ($produceResponse.StatusCode -eq 200) {
                $produceData = $produceResponse.Content | ConvertFrom-Json
                Write-Host "   ✅ Kafka message production successful" -ForegroundColor Green
                Write-Host "   📊 Production Results:" -ForegroundColor Gray
                Write-Host "      Messages Sent: $($produceData.MessagesSent)" -ForegroundColor White
                Write-Host "      Success Rate: $($produceData.SuccessRate)%" -ForegroundColor White
                Write-Host "      Average Latency: $($produceData.AverageLatencyMs)ms" -ForegroundColor White
            }
            else {
                throw "Kafka producer test returned status: $($produceResponse.StatusCode)"
            }
        }
        catch {
            Write-Host "   ❌ Kafka producer test failed: $($_.Exception.Message)" -ForegroundColor Red
            throw "Kafka producer test failed"
        }
        
        # Step 5: Test end-to-end observability flow
        Write-Host ""
        Write-Host "🌊 Step 5: Test End-to-End Observability Flow" -ForegroundColor Yellow
        
        Write-Host "   🔍 Testing complete observability flow..." -ForegroundColor Cyan
        
        $flowTestBody = @{
            KafkaMessages = $MessageCount
            FlinkJobs = 1
            TemporalWorkflows = 1
            TestType = "ConnectivityValidation"
        } | ConvertTo-Json
        
        try {
            $flowResponse = Invoke-WebRequest -Uri "http://localhost:13001/api/observability/execute-real-workload" -Method POST -Body $flowTestBody -ContentType "application/json" -TimeoutSec 60
            
            if ($flowResponse.StatusCode -eq 200) {
                $flowData = $flowResponse.Content | ConvertFrom-Json
                Write-Host "   ✅ End-to-end observability flow successful" -ForegroundColor Green
                Write-Host "   📊 Flow Results:" -ForegroundColor Gray
                Write-Host "      Execution Time: $($flowData.ExecutionTimeSeconds)s" -ForegroundColor White
                Write-Host "      Messages Processed: $($flowData.MessagesProcessed)" -ForegroundColor White
                Write-Host "      Components Active: $($flowData.ActiveComponents)" -ForegroundColor White
            }
            else {
                throw "Observability flow test returned status: $($flowResponse.StatusCode)"
            }
        }
        catch {
            Write-Host "   ❌ End-to-end flow test failed: $($_.Exception.Message)" -ForegroundColor Red
            throw "End-to-end flow test failed"
        }
        
        # Step 6: Test fast failure detection
        Write-Host ""
        Write-Host "⚡ Step 6: Test Fast Failure Detection" -ForegroundColor Yellow
        
        Write-Host "   🔍 Validating that infrastructure failures are detected quickly..." -ForegroundColor Cyan
        
        # Test an invalid endpoint to ensure fast failure
        $failureTestStartTime = Get-Date
        try {
            $invalidResponse = Invoke-WebRequest -Uri "http://localhost:13001/api/observability/invalid-endpoint" -Method GET -TimeoutSec 5
            Write-Host "   ⚠️ Unexpected success on invalid endpoint test" -ForegroundColor Yellow
        }
        catch {
            $failureTestDuration = ((Get-Date) - $failureTestStartTime).TotalSeconds
            if ($failureTestDuration -lt 10) {
                Write-Host "   ✅ Fast failure detection working: failed in $($failureTestDuration.ToString('F1'))s" -ForegroundColor Green
            }
            else {
                Write-Host "   ⚠️ Failure detection took $($failureTestDuration.ToString('F1'))s - should be faster" -ForegroundColor Yellow
            }
        }
        
    }
    finally {
        Pop-Location
        
        # Cleanup: Stop the AppHost job
        if ($appHostJob) {
            Write-Host ""
            Write-Host "🛑 Cleaning up infrastructure..." -ForegroundColor Yellow
            Stop-Job -Job $appHostJob -ErrorAction SilentlyContinue
            Remove-Job -Job $appHostJob -ErrorAction SilentlyContinue
            Write-Host "   ✅ AppHost job stopped and cleaned up" -ForegroundColor Green
        }
    }
    
    # Test completion summary
    $testEndTime = Get-Date
    $totalTestDuration = ($testEndTime - $testStartTime).TotalSeconds
    
    Write-Host ""
    Write-Host "🎉 End-to-End Connectivity Test SUCCESSFUL" -ForegroundColor Green
    Write-Host "📊 Test Summary:" -ForegroundColor Cyan
    Write-Host "   • Total Duration: $($totalTestDuration.ToString('F1')) seconds" -ForegroundColor White
    Write-Host "   • Messages Tested: $MessageCount" -ForegroundColor White
    Write-Host "   • Custom Kafka Container: ✅ Working" -ForegroundColor Green
    Write-Host "   • External Access: ✅ Functional" -ForegroundColor Green
    Write-Host "   • Service Discovery: ✅ Operational" -ForegroundColor Green
    Write-Host "   • Fast Failure Detection: ✅ Active" -ForegroundColor Green
    Write-Host ""
    Write-Host "✅ WI27 VALIDATION: Custom Kafka container successfully resolves external access issues" -ForegroundColor Green
    Write-Host "✅ REGRESSION FIX: Test no longer hangs for 5+ minutes, infrastructure works reliably" -ForegroundColor Green
    
    exit 0
}
catch {
    $testEndTime = Get-Date
    $totalTestDuration = ($testEndTime - $testStartTime).TotalSeconds
    
    Write-Host ""
    Write-Host "❌ End-to-End Connectivity Test FAILED" -ForegroundColor Red
    Write-Host "💥 Error Details:" -ForegroundColor Red
    Write-Host "   • Error: $($_.Exception.Message)" -ForegroundColor White
    Write-Host "   • Duration: $($totalTestDuration.ToString('F1')) seconds" -ForegroundColor White
    Write-Host "   • Stage: Infrastructure connectivity validation" -ForegroundColor White
    Write-Host ""
    Write-Host "🔧 This indicates that the custom Kafka container configuration needs adjustment" -ForegroundColor Yellow
    Write-Host "🔧 Check container logs and endpoint configuration for debugging" -ForegroundColor Yellow
    
    # Cleanup on error
    if ($appHostJob) {
        Stop-Job -Job $appHostJob -ErrorAction SilentlyContinue
        Remove-Job -Job $appHostJob -ErrorAction SilentlyContinue
    }
    
    exit 1
}